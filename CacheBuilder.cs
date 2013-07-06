using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;
using SourceSafeTypeLib;
using vsslib;
using System.Text.RegularExpressions;

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

			// order - from recent (high versions) to ancient (version 1)
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
					_pinned.Clear();

					using(var sw = File.CreateText(DataFileName))
					{
						foreach (var fileGroup in versions.GroupBy(v => v.FileSpec))
						{
							// get in ascending order
							var fileVersions = fileGroup.OrderBy(f => f.VssVersion).ToList();

							var brokenVersions = new List<int>();
							var notRetainedVersions = new List<int>();
							var otherErrors = new Dictionary<int, string>();

							foreach (var file in fileVersions)
							{
								if(!IsShouldBeProcessed(file))
									continue;

								var inf = _cache.GetFileInfo(file.FileSpec, file.VssVersion);

								if(inf == null)
									throw new ApplicationException(string.Format("No in cache, but should be: {0}@{1}", file.FileSpec, file.VssVersion));

								if(inf.ContentPath != null)
								{
									if(brokenVersions.Count > 0 || notRetainedVersions.Count > 0 || otherErrors.Count > 0)
									{
										var commentPlus = string.Format("\nBalme for '{0}' can be incorrect, because:", file.FileSpec);
										if(brokenVersions.Count > 0)
										{
											commentPlus += "\n\tbroken vss versions: " + string.Join(", ", brokenVersions.Select(v => v.ToString(CultureInfo.InvariantCulture)).ToArray());
										}
										if(notRetainedVersions.Count > 0)
										{
											commentPlus += "\n\tnot retained versions: " + string.Join(", ", notRetainedVersions.Select(v => v.ToString(CultureInfo.InvariantCulture)).ToArray());
										}
										if(otherErrors.Count > 0)
										{
											foreach (var otherErrorsGroup in otherErrors.GroupBy(kvp => kvp.Value))
											{
												var revs = otherErrorsGroup.Select(kvp => kvp.Key.ToString(CultureInfo.InvariantCulture)).ToArray();
												commentPlus += string.Format("\n\tError in VSS versions {0}: '{1}'", string.Join(", ", revs), otherErrorsGroup.Key);
											}
										}

										file.Comment += commentPlus;
									}

									sw.WriteLine("Ver:{0}	Spec:{1}	Phys:{2}	User:{3}	At:{4}	DT:{5}	Comment:{6}",
										file.VssVersion,
										file.FileSpec,
										file.Physical,
										file.User,
										file.At.Ticks,
										file.At,
										file.Comment.Replace('\n', '\u0001')
									);

									notRetainedVersions.Clear();
									brokenVersions.Clear();
									otherErrors.Clear();
								}
								else if(inf.Notes == "not-retained")
								{
									notRetainedVersions.Add(file.VssVersion);
								}
								else if(inf.Notes == "broken-revision")
								{
									brokenVersions.Add(file.VssVersion);
								}
								else if(!string.IsNullOrWhiteSpace(inf.Notes))
								{
									otherErrors[file.VssVersion] = inf.Notes;
								}
							}

							if(brokenVersions.Count > 0 || notRetainedVersions.Count > 0 || otherErrors.Count > 0)
								throw new ApplicationException(string.Format("Absent content for latest file version: {0}", fileGroup.Key));
						}
					}
				}
			}

			_stopwatch.Stop();

			Console.WriteLine();
			Console.WriteLine("Building cache complete. Take {0}", _stopwatch.Elapsed);
		}

		void Process(FileRevision file, int pos, int count)
		{
			if(!IsShouldBeProcessed(file))
				return;

			var alreadyInCache = false;
			if(_cache.GetFilePath(file.FileSpec, file.VssVersion) != null)
			{
				alreadyInCache = true;
				Console.Write("c");
				_log.WriteLine("Already in cache: {0}@{1}", file.FileSpec, file.VssVersion);
			}
			else if(!string.IsNullOrWhiteSpace(_cache.GetFileError(file.FileSpec, file.VssVersion)))
			{
				alreadyInCache = true;
				Console.Write("e");
				_log.WriteLine("Already in cache (error): {0}@{1}", file.FileSpec, file.VssVersion);
			}

			if(alreadyInCache && !_options.Force)
				return;

			GetFromVss(file);

			_log.WriteLine("Get: {0}@{1}", file.FileSpec, file.VssVersion);
			Console.WriteLine("[{2}/{3}] Get: {0}@{1}", file.FileSpec, file.VssVersion, pos, count);
		}
		
		// when file request to being 'latest only' - its path added to this set and do not processed further
		readonly HashSet<string> _pinned = new HashSet<string>();

		bool IsShouldBeProcessed(FileRevision file)
		{
			var key = file.FileSpec.ToLowerInvariant().Replace('\\', '/');

			// skip file if requested only latest version, and it was already produced
			if(_pinned.Contains(key))
				return false;

			if(IsShouldBePinned(key))
			{
				_pinned.Add(key);
				_log.WriteLine("Pinned: {0}", key);
			}

			return true;
		}

		bool IsShouldBePinned(string key)
		{
			return _options.LatestOnly.Contains(key) || _options.LatestOnlyRx.Any(rx => rx.IsMatch(key));
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

				var tempDir = Path.Combine(Environment.CurrentDirectory, "vss-temp");
				var path = Path.Combine(tempDir, DateTimeOffset.UtcNow.Ticks + "-" + dstFileName);

				try
				{
					vssItem.Get(path, (int)VSSFlags.VSSFLAG_FORCEDIRNO | (int)VSSFlags.VSSFLAG_USERRONO | (int)VSSFlags.VSSFLAG_REPREPLACE);
				}
				catch(Exception ex)
				{
					if (string.IsNullOrWhiteSpace(_options.SSPath))
						throw;

					// special case when physical file not correspond to 
					var m = Regex.Match(ex.Message, "File ['\"](?<phys>[^'\"]+)['\"] not found");
					if (!m.Success)
						throw;

					if (m.Groups["phys"].Value == vssItem.Physical)
						throw;

					Console.WriteLine("\nPhysical file mismatch. Try get with ss.exe");

					path = new SSExeHelper().Get(file.FileSpec, file.VssVersion, tempDir);
					if (path == null)
					{
						Console.WriteLine("Get with ss.exe failed");
						throw;
					}
				}

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

				_cache.AddError(file.FileSpec, file.VssVersion, ex.Message);

				UnrecognizedError(file, ex);
			}
		}

		void UnrecognizedError(FileRevision file, Exception ex = null)
		{
			_log.WriteLine("UNRECOGNIZED ERROR: {0}", file.FileSpec);
			Console.Error.WriteLine("\n!!! Unrecognized error. See logs.\n{0}@{1}", file.FileSpec, file.VssVersion);
			if(ex != null)
			{
				_log.WriteLine(ex.ToString());
				Console.Error.WriteLine(" ERROR: {0}", ex.Message);
			}
		}

		readonly SHA1Managed _hashAlgo = new SHA1Managed();
	}
}
