using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using SourceSafeTypeLib;
using vsslib;

namespace VssSvnConverter
{
	class CacheBuilder
	{
		IVSSDatabase _db;
		VssFileCache _cache;
		StreamWriter _log;
		StreamWriter _versionsList;
		readonly Stopwatch _stopwatch = new Stopwatch();

		readonly HashSet<string> _skipFiles = new HashSet<string>();

		// file spec -> message which should be added to comment
		readonly Dictionary<string, List<string>> _absentRevisions = new Dictionary<string, List<string>>();

		readonly Options _options;

		public CacheBuilder(Options opts)
		{
			_options = opts;
		}
		
		public List<FileRevision> Load()
		{
			return new VssVersionsBuilder().Load("2b-cached-versions-list.txt");
		}

		public void Build(List<FileRevision> versions)
		{
			_db = _options.DB;

			versions = versions.OrderBy(v => v.FileSpec).ToList();

			using(_cache = new VssFileCache(_options.CacheDir, _db.SrcSafeIni))
			using(_log = File.CreateText("2b-cache.txt.log"))
			using(_versionsList = File.CreateText("2b-cached-versions-list.txt"))
			{
				_log.AutoFlush = true;

				_stopwatch.Start();

				for(var i=0;i<versions.Count;i++)
				{
					var file = versions[i];

					if(_skipFiles.Contains(file.FileSpec))
						continue;

					Process(file, i, versions.Count);
				}
			}
			_stopwatch.Stop();

			Console.WriteLine("Building cache complete. Take {0}", _stopwatch.Elapsed);
		}

		void Process(FileRevision file, int pos, int count)
		{
			if (!_options.Force && _cache.GetFilePath(file.FileSpec, file.VssVersion) != null)
			{
				Console.Write("c");
				_log.WriteLine("Already in cache: {0}@{1}", file.FileSpec, file.VssVersion);
			}
			else if (!_options.Force && _cache.GetFileError(file.FileSpec, file.VssVersion) == "singleton")
			{
				Console.Write("e");
				_log.WriteLine("Already in cache (singleton): {0}@{1}", file.FileSpec, file.VssVersion);
				return;
			}
			else if (!_options.Force && _cache.GetFileError(file.FileSpec, file.VssVersion) == "broken-revision")
			{
				Console.Write("e");
				_log.WriteLine("Already in cache (broken version): {0}@{1}", file.FileSpec, file.VssVersion);
				HandleAbsentFileVersion(file);
				return;
			}
			else
			{
				if(!GetFromVss(file))
					return;

				_log.WriteLine("Get: {0}@{1}", file.FileSpec, file.VssVersion);
				Console.WriteLine("[{2}/{3}] Get: {0}@{1}", file.FileSpec, file.VssVersion, pos, count);
			}

			// try get notes about absent revisions:
			List<string> absentRevs;
			if(_absentRevisions.TryGetValue(file.FileSpec, out absentRevs))
			{
				if(file.Comment.Length > 0)
					file.Comment += "\n";

				file.Comment += "Blame can be incorrect because some file revisions was not retrieved:\n" + string.Join("\n", absentRevs.ToArray());

				_absentRevisions.Remove(file.FileSpec);
			}

			_versionsList.WriteLine("Ver:{0}	Spec:{1}	User:{2}	At:{3}	DT:{4}	Comment:{5}",
				file.VssVersion,
				file.FileSpec,
				file.User,
				file.At.Ticks,
				file.At,
				file.Comment.Replace('\n', '\u0001')
			);
		}

		bool GetFromVss(FileRevision file)
		{
			var lastVersion = false;
			try
			{
				var vssItem = _db.VSSItem[file.FileSpec];
				lastVersion = vssItem.VersionNumber == file.VssVersion;

				try
				{
					GetFromVss(vssItem, file);
				}
				catch (Exception ex)
				{
					if (ex.Message.Contains("does not retain old versions of itself"))
					{
						// try get latest only
						Console.WriteLine("\n{0} has only last revision", file.FileSpec);

						lastVersion = true;
						file.VssVersion = vssItem.VersionNumber;
						_skipFiles.Add(file.FileSpec);
						GetFromVss(vssItem, file);

						// add for all other revisions error 'singleton'
						for (var i = 1; i < file.VssVersion; i++)
						{
							_cache.AddError(file.FileSpec, i, "singleton");
						}
					}
					else
					{
						throw;
					}
				}
			}
			catch (Exception ex)
			{
				// last revision shall always present
				// known error
				if (!lastVersion && ex.Message.Contains("SourceSafe was unable to finish writing a file.  Check your available disk space, and ask the administrator to analyze your SourceSafe database."))
				{
					Console.Error.WriteLine("\nAbsent file revision: {0}@{1}", file.FileSpec, file.VssVersion);

					_cache.AddError(file.FileSpec, file.VssVersion, "broken-revision");

					HandleAbsentFileVersion(file);

					return false;
				}

				// unknown error or last version
				_log.WriteLine("ERROR: {0}", file.FileSpec);
				_log.WriteLine("	Is Last Version: {0}", lastVersion);
				_log.WriteLine(ex.ToString());
				Console.Error.WriteLine("\n!!! Unrecoverable error.\n{0}@{1} (Last Version: {2})\n ERROR: {3}", file.FileSpec, file.VssVersion, lastVersion, ex.Message);
				Environment.Exit(1);
			}

			return true;
		}

		private void HandleAbsentFileVersion(FileRevision file)
		{
			List<string> absent;
			if (!_absentRevisions.TryGetValue(file.FileSpec, out absent))
			{
				absent = new List<string>();
				_absentRevisions[file.FileSpec] = absent;
			}

			absent.Add(string.Format("{0}@{1}", file.FileSpec, file.VssVersion));
		}

		readonly SHA1Managed _hashAlgo = new SHA1Managed();

		void GetFromVss(VSSItem vssItem, FileRevision file)
		{
			// move to correct veriosn
			if (vssItem.VersionNumber != file.VssVersion)
				vssItem = vssItem.Version[file.VssVersion];

			var relPath = file.FileSpec.TrimStart('$', '/', '\\');

			var path = Path.Combine(Path.Combine(Environment.CurrentDirectory, "vss-temp"), Path.GetFileName(relPath));

			vssItem.Get(path, (int)(VSSFlags.VSSFLAG_FORCEDIRNO | VSSFlags.VSSFLAG_USERRONO | VSSFlags.VSSFLAG_REPREPLACE));

			// in force mode check if file already in cache and coincidence by hash
			if(_options.Force)
			{
				var ce = _cache.GetFileInfo(file.FileSpec, file.VssVersion);
				if(ce != null)
				{
					string hash;

					using(var s = new FileStream(path, FileMode.Open, FileAccess.Read))
						hash = Convert.ToBase64String(_hashAlgo.ComputeHash(s));

					if(hash != ce.Sha1Hash)
					{
						_log.WriteLine("!!! Cache contains different content for: " + file.FileSpec);
						_log.WriteLine("{0} != {1}", hash, ce.Sha1Hash);
						_cache.AddFile(file.FileSpec, file.VssVersion, path, false);
					}
					return;
				}
			}
			_cache.AddFile(file.FileSpec, file.VssVersion, path, false);
		}
	}
}
