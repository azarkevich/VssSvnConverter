using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.SourceSafe.Interop;
using System.IO;

namespace VssSvnConverter
{
	class Options
	{
		// stage
		public string Stage;

		public IVSSDatabase DB;

		// import
		public Func<string, bool> IncludePredicate;

		public string[] VssRoots;

		// cache
		public string CacheDir;

		// build commits
		public TimeSpan SilentSpan;

		public bool MergeChanges;

		public bool OverlapCommits;

		public Dictionary<string, string> UserMappings;

		// export
		public Uri SvnRepoUri;
		public string SvnRepo;

		public Options(IEnumerable<string> args)
		{
			Stage = args
				.Where(a => !a.StartsWith("--"))
				.DefaultIfEmpty("all")
				.First()
			;
		}

		public void ReadConfig(string conf)
		{
			var confLookup = File.ReadAllLines(conf)
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

			VssRoots = confLookup["import-root"].ToArray();

			// cache 
			CacheDir = confLookup["cache-dir"].DefaultIfEmpty(".cache").First();

			// commit setup

			// silent period
			SilentSpan = confLookup["commit-silent-period"]
				.DefaultIfEmpty("120")
				.Select(Double.Parse)
				.Select(TimeSpan.FromMinutes)
				.First()
			;

			// overlapping
			OverlapCommits = confLookup["overlapped-commits"]
				.DefaultIfEmpty("false")
				.Select(bool.Parse)
				.First()
			;

			// merge changes
			MergeChanges = confLookup["merge-changes"]
				.DefaultIfEmpty("false")
				.Select(bool.Parse)
				.First()
			;

			UserMappings = confLookup["user-mapping"]
				.Select(m => m.Split(':'))
				.Where(a => a.Length == 2)
				.ToDictionary(a => a[0].ToLowerInvariant(), a => a[1])
			;

			SvnRepo = Path.Combine(Environment.CurrentDirectory, "_repository");
			SvnRepoUri = new Uri("file:///" + SvnRepo.Replace('\\', '/'));

			// open VSS DB
			var ssIni = confLookup["source-safe-ini"].DefaultIfEmpty("srcsafe.ini").First();
			var ssUser = confLookup["source-safe-user"].DefaultIfEmpty("").First();
			var ssPwd = confLookup["source-safe-password"].DefaultIfEmpty("").First();
			DB = new VSSDatabase();
			DB.Open(ssIni, ssUser, ssPwd);

			// include/exclude checks
			var checks = confLookup["import-pattern"]
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
