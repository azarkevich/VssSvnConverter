using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Diagnostics;
using SourceSafeTypeLib;

namespace VssSvnConverter
{
	class FileRevision
	{
		public string FileSpec;
		public string User;
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

					var v = new FileRevision
					{
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

		IEnumerable<List<string>> Split(IEnumerable<string> source, int parts)
		{
			return source
				.Select((x, i) => new { Index = i, Value = x })
				.GroupBy(x => x.Index % parts)
				.Select(x => x.Select(v => v.Value).ToList())
			;
		}

		public void Build(Options opts, List<string> files)
		{
			var lockHandle = new object();

			var threadIndex = 0;

			var stopWatch = new Stopwatch();
			stopWatch.Start();

			using(var wr = File.CreateText(DataFileName))
			using(var log = File.CreateText(LogFileName))
			{
				Split(files, opts.VersionFetchThreads)
					.Select(slice => BuildListAsync(opts, threadIndex++, slice, log, r => {
						lock(lockHandle)
						{
							foreach (var rev in r)
							{
								wr.WriteLine("Ver:{0}	Spec:{1}	Phys:{2}	User:{3}	At:{4}	DT:{5}	Comment:{6}",
									rev.VssVersion, rev.FileSpec, rev.Physical, rev.User, rev.At.Ticks, rev.At, rev.Comment.Replace("\r\n", "\n").Replace('\r', '\n').Replace('\n', '\u0001'));
							}
						}
					}))
					.ToList()
					.ForEach(t => t.Join())
				;
			}

			stopWatch.Stop();
			Console.WriteLine("Build files versions list complete. Take: {0}", stopWatch.Elapsed);
		}

		Thread BuildListAsync(Options opts, int index, ICollection<string> files, TextWriter log, Action<List<FileRevision>> result)
		{
			var t = new Thread(delegate(object state) {
	
				var db = opts.DB;

				var revisions = new List<FileRevision>();
				var findex = 0;
				foreach (var spec in files)
				{
					try{
						IVSSItem item = db.VSSItem[spec];

						Console.WriteLine("[{0}] [{1,5}/{2,5}] {3}", (char)('a' + index), ++findex, files.Count, item.Spec);

						foreach (IVSSVersion ver in item.Versions)
						{
							if (ver.Action.StartsWith("Labeled ") || ver.Action.StartsWith("Branched "))
								continue;

							if(!ver.Action.StartsWith("Checked in ") && !ver.Action.StartsWith("Created ") && !ver.Action.StartsWith("Archived ") && !ver.Action.StartsWith("Rollback to"))
							{
								log.WriteLine("Unknown action: " + ver.Action);
							}

							var user = ver.Username.ToLowerInvariant();

							// map user name
							string u;
							if(opts.UserMappings.TryGetValue(user, out u))
								user = u;

							revisions.Add(new FileRevision { FileSpec = item.Spec, At = ver.Date.ToUniversalTime(), Comment = ver.Comment, VssVersion = ver.VersionNumber, User = user, Physical = ver.VSSItem.Physical });
						}
					}
					catch(Exception ex)
					{
						log.WriteLine("ERROR: {0}", spec);
						log.WriteLine(ex.ToString());
					}
				}

				result(revisions);
			});

			t.Start();

			return t;
		}
	}
}
