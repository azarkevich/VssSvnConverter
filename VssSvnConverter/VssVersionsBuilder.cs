using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using SourceSafeTypeLib;
using vsslib;
using VssSvnConverter.Core;

namespace VssSvnConverter
{
	class FileRevision
	{
		public string FileSpec;
		public string User;
		public string User4Comment;
		public DateTime At;
		public int VssVersion;
		public string Comment;
		public string Physical;
	}

	class VssVersionsBuilder
	{
		const string DataFileName = "2-raw-versions-list.txt";
		const string LogFileName = "log-2-raw-versions-list.txt";

		readonly Regex _versionRx = new Regex(@"^Ver:(?<ver>[0-9]+)\tSpec:(?<spec>[^\t]+)\tPhys:(?<phys>[^\t]+)\tUser:(?<user>[^\t]+)\tAt:(?<at>[0-9]+)\tDT:(?<dt>[^\t]+)\tComment:(?<comment>.*)$");

		public List<FileRevision> Load(string file = DataFileName)
		{
			var list = new List<FileRevision>();
			using(var r = File.OpenText(file))
			{
				string line;
				while((line = r.ReadLine()) != null)
				{
					var m = _versionRx.Match(line);
					if(!m.Success)
						continue;

					var v = new FileRevision {
						At = new DateTime(long.Parse(m.Groups["at"].Value), DateTimeKind.Utc),
						User = m.Groups["user"].Value,
						FileSpec = m.Groups["spec"].Value,
						VssVersion = int.Parse(m.Groups["ver"].Value),
						Physical = m.Groups["phys"].Value,
						Comment = m.Groups["comment"].Value.Replace('\u0001', '\n')
					};

					list.Add(v);
				}
			}

			return list;
		}

		public void Build(Options opts, IList<Tuple<string, int>> files)
		{
			var stopWatch = new Stopwatch();
			stopWatch.Start();

			using (var cache = new VssFileCache(opts.CacheDir + "-revs", opts.SourceSafeIni))
			using(var wr = File.CreateText(DataFileName))
			using(var log = File.CreateText(LogFileName))
			{
				wr.AutoFlush = log.AutoFlush = true;

				var db = opts.DB.Value;

				var findex = 0;
				foreach (var spec in files.Select(t => t.Item1))
				{
					findex++;

					try{
						IVSSItem item = db.VSSItem[spec];
						var head = item.VersionNumber;

						var timestamp = item.VSSVersion.Date.Ticks;

						var cachedData = cache.GetFilePath(spec, head, timestamp);
						if (cachedData != null)
						{
							Console.Write("c");
							Save(wr, Load(cachedData));
							// next file
							continue;
						}

						Console.Write("[{0,5}/{1,5}] {2} ", findex, files.Count, item.Spec);

						var rotationIndex = 0;
						var rotationArray = @"|/-\|/-\".ToCharArray();

						var itemRevisions = new List<FileRevision>();
						foreach (IVSSVersion ver in item.Versions)
						{
							Console.Write("{0}\b", rotationArray[rotationIndex++ % rotationArray.Length]);

							if (ver.Action.StartsWith("Labeled ") || ver.Action.StartsWith("Branched "))
								continue;

							if (!ver.Action.StartsWith("Checked in ") && !ver.Action.StartsWith("Created ") && !ver.Action.StartsWith("Archived ") && !ver.Action.StartsWith("Rollback to"))
							{
								log.WriteLine("Unknown action: " + ver.Action);
							}

							var user = ver.Username.ToLowerInvariant();

							var fileVersionInfo = new FileRevision {
								FileSpec = item.Spec,
								At = ver.Date.ToUniversalTime(),
								Comment = ver.Comment,
								VssVersion = ver.VersionNumber,
								User = user
							};
							try
							{
								// can throw exception, but it is not critical
								fileVersionInfo.Physical = ver.VSSItem.Physical;
							}
							catch (Exception ex)
							{
								Console.WriteLine("ERROR: Get Physical: " + ex.Message);
								log.WriteLine("ERROR: Get Physical: {0}", spec);
								log.WriteLine(ex.ToString());
								fileVersionInfo.Physical = "_UNKNOWN_";
							}
							itemRevisions.Add(fileVersionInfo);

							Console.Write('.');
						}

						Console.WriteLine(" ");

						if (itemRevisions.Count > 0)
						{
							// some time date of items wrong, but versions - correct.
							// sort items in correct order and fix dates
							itemRevisions = itemRevisions.OrderBy(i => i.VssVersion).ToList();

							// fix time. make time of each next item greater than all previous
							var notEarlierThan = itemRevisions[0].At;
							for (int i = 1; i < itemRevisions.Count; i++)
							{
								if (itemRevisions[i].At < notEarlierThan)
								{
									itemRevisions[i].At = notEarlierThan + TimeSpan.FromMilliseconds(1);
									itemRevisions[i].Comment += "\n! Time was fixed during VSS -> SVN conversion. Time can be incorrect !\n";
									itemRevisions[i].Comment = itemRevisions[i].Comment.Trim();
								}

								notEarlierThan = itemRevisions[i].At;
							}
						}

						Save(wr, itemRevisions);

						var tempFile = Path.GetTempFileName();
						try
						{
							using (var sw = new StreamWriter(tempFile, false, Encoding.UTF8))
								Save(sw, itemRevisions);

							cache.AddFile(spec, head, timestamp, tempFile, false);
						}
						finally
						{
							if (File.Exists(tempFile))
								File.Delete(tempFile);
						}
					}
					catch(Exception ex)
					{
						Console.WriteLine("ERROR: {0}", spec);
						log.WriteLine("ERROR: {0}", spec);
						log.WriteLine(ex.ToString());
					}
				}
			}

			stopWatch.Stop();
			Console.WriteLine("Build files versions list complete. Take: {0}", stopWatch.Elapsed);
		}

		static void Save(TextWriter wr, IEnumerable<FileRevision> r)
		{
			foreach (var rev in r)
			{
				wr.WriteLine("Ver:{0}	Spec:{1}	Phys:{2}	User:{3}	At:{4}	DT:{5}	Comment:{6}",
					rev.VssVersion, rev.FileSpec, rev.Physical, rev.User, rev.At.Ticks, rev.At,
					rev.Comment.Replace("\r\n", "\n").Replace('\r', '\n').Replace('\n', '\u0001'));
			}
		}
	}
}
