using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using SourceSafeTypeLib;

namespace VssSvnConverter
{
	class Options
	{
		public ILookup<string, string> Config;

		public bool Force;
		public bool Ask;

		public VSSDatabase DB;

		// import
		public Func<string, bool> IncludePredicate;

		// cache
		public string CacheDir;

		public readonly HashSet<string> LatestOnly = new HashSet<string>();

		public Regex[] LatestOnlyRx;

		// build commits
		public TimeSpan SilentSpan;

		public bool MergeChanges;

		public bool OverlapCommits;

		public Dictionary<string, string> UserMappings;

		// export
		public Uri SvnRepoUri;
		public string SvnRepo;

		// directories, which will be created as revision 1, before first import
		public string[] PreCreateDirs;
		
		public int VersionFetchThreads = 1;

		public Options(IEnumerable<string> args)
		{
			Force = args.Any(a => a == "--force");
			Ask = args.Any(a => a == "--ask");
		}

		public void ReadConfig(string conf)
		{
			Config = File.ReadAllLines(conf)
				.Where(l => !string.IsNullOrEmpty(l))
				.Select(l => l.Trim())
				.Where(l => !l.StartsWith("#"))
				.Select(l => {
					var pos = l.IndexOf('=');
					if(pos == -1)
					{
						Console.WriteLine("Wrong line in config: {0}", l);
						Environment.Exit(-1);
					}

					return new { Key = l.Substring(0, pos).Trim(), Value = l.Substring(pos + 1).Trim()};
				})
				.ToLookup(p => p.Key, p => p.Value)
			;

			VersionFetchThreads = Config["versions-fetch-threads"].DefaultIfEmpty("1").Select(Int32.Parse).First();

			// cache 
			CacheDir = Config["cache-dir"].DefaultIfEmpty(".cache").First();

			foreach (var v in Config["latest-only"])
			{
				LatestOnly.Add(v.ToLowerInvariant().Replace('\\', '/'));
			}

			LatestOnlyRx = Config["latest-only-rx"].Select(rx => new Regex(rx, RegexOptions.IgnoreCase)).ToArray();

			// commit setup

			// silent period
			SilentSpan = Config["commit-silent-period"]
				.DefaultIfEmpty("120")
				.Select(Double.Parse)
				.Select(TimeSpan.FromMinutes)
				.First()
			;

			// overlapping
			OverlapCommits = Config["overlapped-commits"]
				.DefaultIfEmpty("false")
				.Select(bool.Parse)
				.First()
			;

			// merge changes
			MergeChanges = Config["merge-changes"]
				.DefaultIfEmpty("false")
				.Select(bool.Parse)
				.First()
			;

			UserMappings = Config["user-mapping"]
				.Select(m => m.Split(':'))
				.Where(a => a.Length == 2)
				.ToDictionary(a => a[0].ToLowerInvariant(), a => a[1])
			;

			SvnRepo = Path.Combine(Environment.CurrentDirectory, "_repository");
			SvnRepoUri = new Uri("file:///" + SvnRepo.Replace('\\', '/'));

			PreCreateDirs = Config["pre-create-dir"].ToArray();

			// open VSS DB
			var ssIni = Config["source-safe-ini"].DefaultIfEmpty("srcsafe.ini").First();
			var ssUser = Config["source-safe-user"].DefaultIfEmpty("").First();
			var ssPwd = Config["source-safe-password"].DefaultIfEmpty("").First();
			DB = new VSSDatabase();
			DB.Open(ssIni, ssUser, ssPwd);

			// include/exclude checks
			var checks = Config["import-pattern"]
				.Select(pat => new { Rx = new Regex(pat.Substring(1), RegexOptions.IgnoreCase), Exclude = pat.StartsWith("-") })
				.ToArray()
			;

			IncludePredicate = path => {
				var m = checks.FirstOrDefault(c => c.Rx.IsMatch(path));
				if (m == null)
					return true;

				return !m.Exclude;
			};
		}
	}
}
