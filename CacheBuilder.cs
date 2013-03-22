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
		const string DataFileName = "4-cached-versions-list.txt";
		const string LogFileName = "log-4-cached-versions-list.txt";

		IVSSDatabase _db;
		VssFileCache _cache;
		StreamWriter _log;
		readonly Stopwatch _stopwatch = new Stopwatch();

		readonly Options _options;

		public CacheBuilder(Options opts)
		{
			_options = opts;
		}
		
		public List<FileRevision> Load()
		{
			return new VssVersionsBuilder().Load(DataFileName);
		}

		public void Build(List<FileRevision> versions)
		{
			_db = _options.DB;

			versions = versions.OrderBy(f => f.FileSpec).ThenBy(f => -f.VssVersion).ToList();

			using(_cache = new VssFileCache(_options.CacheDir, _db.SrcSafeIni))
			{
				using(_log = File.CreateText(LogFileName))
				{
					_log.AutoFlush = true;

					_stopwatch.Start();

					// cache
					for(var i=0;i<versions.Count;i++)
					{
						Process(versions[i], i, versions.Count);
					}

					// build cached versions list
					using(var sw = File.CreateText(DataFileName))
					{
						foreach (var fileGroup in versions.GroupBy(v => v.FileSpec))
						{
							// get in ascending order
							var fileVersions = fileGroup.OrderBy(f => f.VssVersion).ToList();

							var brokenVersions = new List<int>();
							var notRetainedVersions = new List<int>();

							foreach (var file in fileVersions)
							{
								var inf = _cache.GetFileInfo(file.FileSpec, file.VssVersion);

								if(inf == null)
									throw new ApplicationException(string.Format("No in cache, but should be: {0}@{1}", file.FileSpec, file.VssVersion));

								if(inf.ContentPath != null)
								{
									if(brokenVersions.Count > 0 || notRetainedVersions.Count > 0)
									{
										var commentPlus = string.Format("\nBalme for '{0}' can be incorrect, because:", file.FileSpec);
										if(brokenVersions.Count > 0)
										{
											commentPlus += "\n\tbroken vss versions: " + string.Join(", ", brokenVersions.Select(v => v.ToString()).ToArray());
										}
										if(notRetainedVersions.Count > 0)
										{
											commentPlus += "\n\tnot retained versions: " + string.Join(", ", notRetainedVersions.Select(v => v.ToString()).ToArray());
										}

										file.Comment += commentPlus;
									}

									sw.WriteLine("Ver:{0}	Spec:{1}	User:{2}	At:{3}	DT:{4}	Comment:{5}",
										file.VssVersion,
										file.FileSpec,
										file.User,
										file.At.Ticks,
										file.At,
										file.Comment.Replace('\n', '\u0001')
									);

									notRetainedVersions.Clear();
									brokenVersions.Clear();
								}
								else if(inf.Notes == "not-retained")
								{
									notRetainedVersions.Add(file.VssVersion);
								}
								else if(inf.Notes == "broken-revision")
								{
									brokenVersions.Add(file.VssVersion);
								}
							}

							if(brokenVersions.Count > 0 || notRetainedVersions.Count > 0)
								throw new ApplicationException(string.Format("Absent content for latest file version: {0}", fileGroup.Key));
						}
					}
				}
			}

			_stopwatch.Stop();

			Console.WriteLine("Building cache complete. Take {0}", _stopwatch.Elapsed);
		}

		void Process(FileRevision file, int pos, int count)
		{
			var alreadyInCache = false;
			if(_cache.GetFilePath(file.FileSpec, file.VssVersion) != null)
			{
				alreadyInCache = true;
				Console.Write("c");
				_log.WriteLine("Already in cache: {0}@{1}", file.FileSpec, file.VssVersion);
			}
			else if(_cache.GetFileError(file.FileSpec, file.VssVersion) == "not-retained")
			{
				alreadyInCache = true;
				Console.Write("e");
				_log.WriteLine("Already in cache (singleton): {0}@{1}", file.FileSpec, file.VssVersion);
			}
			else if(_cache.GetFileError(file.FileSpec, file.VssVersion) == "broken-revision")
			{
				alreadyInCache = true;
				Console.Write("e");
				_log.WriteLine("Already in cache (broken version): {0}@{1}", file.FileSpec, file.VssVersion);
			}

			if(alreadyInCache && !_options.Force)
				return;

			GetFromVss(file);

			_log.WriteLine("Get: {0}@{1}", file.FileSpec, file.VssVersion);
			Console.WriteLine("[{2}/{3}] Get: {0}@{1}", file.FileSpec, file.VssVersion, pos, count);
		}

		void GetFromVss(FileRevision file)
		{
			try
			{
				var vssItem = _db.VSSItem[file.FileSpec];

				// move to correct veriosn
				if (vssItem.VersionNumber != file.VssVersion)
					vssItem = vssItem.Version[file.VssVersion];

				var dstFileName = Path.GetFileName(file.FileSpec.TrimStart('$', '/', '\\'));

				var path = Path.Combine(Path.Combine(Environment.CurrentDirectory, "vss-temp"), DateTimeOffset.UtcNow.Ticks + "-" + dstFileName);

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
			catch (Exception ex)
			{
				if (ex.Message.Contains("does not retain old versions of itself"))
				{
					Console.WriteLine("{0} hasn't retain version {1}.", file.FileSpec, file.VssVersion);

					_cache.AddError(file.FileSpec, file.VssVersion, "not-retained");

					return;
				}

				// last revision shall always present
				// known error
				if (ex.Message.Contains("SourceSafe was unable to finish writing a file.  Check your available disk space, and ask the administrator to analyze your SourceSafe database."))
				{
					Console.Error.WriteLine("\nAbsent file revision: {0}@{1}", file.FileSpec, file.VssVersion);

					_cache.AddError(file.FileSpec, file.VssVersion, "broken-revision");

					return;
				}

				CriticalError(file, ex);
			}
		}

		void CriticalError(FileRevision file, Exception ex = null)
		{
			_log.WriteLine("CRITICAL ERROR: {0}", file.FileSpec);
			_log.WriteLine(ex.ToString());
			Console.Error.WriteLine("\n!!! Unrecoverable error.\n{0}@{1}\n ERROR: {2}", file.FileSpec, file.VssVersion, ex.Message);

			Environment.Exit(1);
		}

		readonly SHA1Managed _hashAlgo = new SHA1Managed();
	}
}
