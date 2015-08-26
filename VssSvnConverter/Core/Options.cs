﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SourceSafeTypeLib;
using vsslib;

namespace VssSvnConverter.Core
{
	public class Options
	{
		public ILookup<string, string> Config;

		public bool Force;
		public bool Ask;
		public bool ImportUnimportantOnly;

		public string Prefix;
		public Regex FilterRx;

		public string SourceSafeIni;
		public string SourceSafeUser;
		public string SourceSafePassword;

		public VSSDatabase DB;

		// import
		public Func<string, bool> IncludePredicate;

		// cache
		public string CacheDir;
		public int CacheParallelMaxDegree;

		public readonly HashSet<string> LatestOnly = new HashSet<string>();

		public Regex[] LatestOnlyRx;

		// build commits
		public TimeSpan SilentSpan;

		public bool MergeChanges;

		public bool OverlapCommits;

		public Dictionary<string, string> UserMappings;

		// import
		public string RepoDir;
		public bool UseGit;
		public bool GitRepoInit;
		public string GitExe;
		public string GitDefaultAuthorDomain;

		public bool EnhanceComments;

		// directories, which will be created as revision 1, before first import
		public string[] PreCreateDirs;
		
		// if specified, ss.exe will be used for retrieve files with known problems
		public string SSPath;

		public Options(string[] args)
		{
			Force = args.Any(a => a == "--force");
			Ask = args.Any(a => a == "--ask");
			ImportUnimportantOnly = args.Any(a => a == "--unimportant-only");
			Prefix = args.Where(a => a.StartsWith("--prefix=")).Select(a => a.Substring("--prefix=".Length)).FirstOrDefault();

			var filterRx = args.Where(a => a.StartsWith("--filter=")).Select(a => a.Substring("--filter=".Length)).FirstOrDefault();
			if(filterRx != null)
				FilterRx = new Regex(filterRx, RegexOptions.IgnoreCase);
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

			SSPath = Config["ss.exe"].FirstOrDefault();

			// cache
			CacheDir = Config["cache-dir"].DefaultIfEmpty(".cache").First();

			CacheParallelMaxDegree = Config["cache-parallel-max-degree"].DefaultIfEmpty("0").Select(Int32.Parse).First();

			LatestOnly.Clear();
			foreach (var v in Config["latest-only"])
			{
				LatestOnly.Add(v);
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

			foreach (var mappingFile in Config["user-mapping-file"])
			{
				foreach (var line in File.ReadAllLines(mappingFile))
				{
					var arr = line.Split('\t');
					if (arr.Length == 0)
						continue;

					if(arr.Length != 2)
						throw new Exception("Invalid user mapping file: " + mappingFile);

					UserMappings[arr[0].ToLowerInvariant()] = arr[1];
				}
			}

			EnhanceComments = Config["enhance-comments"]
				.DefaultIfEmpty("false")
				.Select(bool.Parse)
				.First()
			;

			UseGit = Config["git-enable"]
				.DefaultIfEmpty("false")
				.Select(bool.Parse)
				.First()
			;

			if (UseGit)
			{
				GitRepoInit = Config["git-init-repo"]
					.DefaultIfEmpty("false")
					.Select(bool.Parse)
					.First()
				;

				GitExe = Config["git-exe"]
					.DefaultIfEmpty("git.exe")
					.First()
				;

				GitDefaultAuthorDomain = Config["git-default-author-domain"]
					.DefaultIfEmpty("")
					.First()
				;
				
			}

			RepoDir = Config["repo-dir"]
				.DefaultIfEmpty("_repository")
				.Select(p => Path.Combine(Environment.CurrentDirectory, p))
				.First()
			;

			// open VSS DB
			if (DB != null)
			{
				DB.Close();
				DB = null;
			}

			SourceSafeIni = Config["source-safe-ini"].DefaultIfEmpty("srcsafe.ini").First();
			SourceSafeUser = Config["source-safe-user"].DefaultIfEmpty("").First();
			SourceSafePassword = Config["source-safe-password"].DefaultIfEmpty("").First();
			DB = new VSSDatabase();
			DB.Open(SourceSafeIni, SourceSafeUser, SourceSafePassword);

			SSExeHelper.SetupSS(SSPath, SourceSafeIni, SourceSafeUser, SourceSafePassword);

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