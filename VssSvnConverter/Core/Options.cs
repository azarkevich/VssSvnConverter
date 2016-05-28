using System;
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

		public Lazy<VSSDatabase> DB;

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
		public bool CommentAddVssFilesInfo;
		public bool CommentAddUserTime;
		public bool UserMappingStrict;
		public bool CommentAddCommitNumber;

		// import

		// git tfs svn
		public string ImportDriver;

		// GIT
		public string GitExe;
		public string GitDefaultAuthorDomain;
		public string GitRepoDir;
		public bool IsGitRepoDirExternal;
		public string GitStartAfterImport;
		public string GitStartAfterImportArgs;

		// TFS
		public string TfExe;
		public string TfsWorkTreeDir;
		public bool TfNoCheckStatusBeforeStartRevision;

		// SVN
		public string SvnRepoUrl;
		public string SvnWorkTreeDir;
		public bool IsSvnRepoDirExternal;

		// directories, which will be created as revision 1, before first import
		public string[] PreCreateDirs;

		// if specified, ss.exe will be used for retrieve files with known problems
		public string SSPath;

		// if specified, ss.exe will be used for retrieve files with known problems
		public List<Tuple<Regex, string>> MangleImportPath;

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

			UserMappingStrict = Config["user-mapping-strict"]
				.DefaultIfEmpty("false")
				.Select(bool.Parse)
				.First()
			;

			CommentAddVssFilesInfo = Config["comment-add-vss-files-info"]
				.DefaultIfEmpty("false")
				.Select(bool.Parse)
				.First()
			;

			CommentAddUserTime = Config["comment-add-user-time"]
				.DefaultIfEmpty("false")
				.Select(bool.Parse)
				.First()
			;

			CommentAddCommitNumber = Config["comment-add-commit-number"]
				.DefaultIfEmpty("false")
				.Select(bool.Parse)
				.First()
			;

			ImportDriver = Config["import-driver"]
				.DefaultIfEmpty("svn")
				.First()
				.ToLowerInvariant()
			;

			if (ImportDriver == "git")
			{
				GitExe = Config["git-exe"]
					.DefaultIfEmpty("git.exe")
					.First()
				;

				GitRepoDir = Config["git-repo-dir"]
					.Select(p => Path.Combine(Environment.CurrentDirectory, p))
					.FirstOrDefault()
				;

				if (string.IsNullOrWhiteSpace(GitRepoDir))
				{
					GitRepoDir = Path.Combine(Environment.CurrentDirectory, "_git_repo");
				}
				else
				{
					IsGitRepoDirExternal = true;
				}

				GitDefaultAuthorDomain = Config["git-default-author-domain"]
					.DefaultIfEmpty("@dummy-email.org")
					.First()
				;

				GitStartAfterImport = Config["git-start-after-import"]
					.FirstOrDefault()
				;

				GitStartAfterImportArgs = Config["git-start-after-import-args"]
					.FirstOrDefault() ?? ""
				;
			}

			if (ImportDriver == "tfs")
			{
				TfExe = Config["tf-exe"]
					.DefaultIfEmpty("tf.exe")
					.First()
				;

				TfsWorkTreeDir = Config["tfs-worktree-dir"]
					.Select(p => Path.Combine(Environment.CurrentDirectory, p))
					.FirstOrDefault()
				;

				TfNoCheckStatusBeforeStartRevision = Config["tfs-no-check-status-every-revision"]
					.Select(p => p.ToLowerInvariant())
					.Select(p => p == "1" || p == "yes" || p == "true")
					.FirstOrDefault()
				;
			}

			if (ImportDriver == "svn")
			{
				SvnRepoUrl = Config["svn-repo-url"]
					.FirstOrDefault()
				;

				if (string.IsNullOrWhiteSpace(SvnRepoUrl))
				{
					SvnRepoUrl = Path.Combine(Environment.CurrentDirectory, "_svn_repo");
				}
				else
				{
					IsSvnRepoDirExternal = true;
				}

				SvnWorkTreeDir = Config["svn-worktree-dir"]
					.Select(p => Path.Combine(Environment.CurrentDirectory, p))
					.FirstOrDefault()
				;

				if (string.IsNullOrWhiteSpace(SvnWorkTreeDir))
				{
					SvnWorkTreeDir = Path.Combine(Environment.CurrentDirectory, "_svn_wc");
				}
			}

			MangleImportPath = Config["mangle-import-path"]
				.Select(p => Tuple.Create(new Regex(p.Split(':')[0], RegexOptions.IgnoreCase), p.Split(':')[1]))
				.ToList()
			;

			// open VSS DB
			if (DB != null && DB.IsValueCreated)
			{
				DB.Value.Close();
				DB = null;
			}

			SourceSafeIni = Config["source-safe-ini"].DefaultIfEmpty("srcsafe.ini").First();
			SourceSafeUser = Config["source-safe-user"].DefaultIfEmpty("").First();
			SourceSafePassword = Config["source-safe-password"].DefaultIfEmpty("").First();
			DB = new Lazy<VSSDatabase>(() => {
				Console.WriteLine("Initialize VSS driver....");
				var db = new VSSDatabase();
				db.Open(SourceSafeIni, SourceSafeUser, SourceSafePassword);
				Console.WriteLine("VSS driver initialized");
				return db;
			});

			SSExeHelper.SetupSS(SourceSafeIni, SourceSafeUser, SourceSafePassword);

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
