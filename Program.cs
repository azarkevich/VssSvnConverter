using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace VssSvnConverter
{
	class Program
	{
		static Options _opts;

		static Int32 Main(string[] args)
		{
			_opts = new Options(args);

			try{
				if (args.Length == 0)
				{
					args = new [] { "ui" };
				}

				if(args.Any(a => a.StartsWith("/help")) || args.Any(a => a.StartsWith("-h")) || args.Any(a => a.StartsWith("--help")))
				{
					ShowHelp();
					return -1;
				}

				var verbs = args
					.Where(a => !a.StartsWith("-"))
					.Select(a => a.ToLowerInvariant())
					.SelectMany(a => {
						if(a== "all")
							return new[] { "build-list", "build-list-stats", "build-versions", "build-links", "build-cache", "build-commits", "build-wc", "import", "build-scripts" };

						return Enumerable.Repeat(a, 1);
					})
					.ToList()
				;
			
				if(verbs.Count == 0)
				{
					ShowHelp();
					return -1;
				}

				var unkVerb = verbs.FirstOrDefault(v => v != "ui" && v != "build-list" && v != "build-list-stats" && v != "build-versions" && v != "build-links" && v != "build-cache" && v != "build-commits" && v != "build-wc" && v != "import" && v != "build-scripts");
				if(unkVerb != null)
				{
					ShowHelp(unkVerb);
					return -1;
				}

				verbs.ForEach(ProcessStage);
			}
			catch(ApplicationException ex)
			{
				Console.Error.WriteLine(ex.Message);
				if (_opts.Ask)
				{
					Console.WriteLine("Press any key...");
					Console.ReadKey();
				}
				return 1;
			}
			catch(Exception ex)
			{
				Console.Error.WriteLine(ex.ToString());
				if (_opts.Ask)
				{
					Console.WriteLine("Press any key...");
					Console.ReadKey();
				}
				return 1;
			}

			return 0;
		}

		public static void ProcessStage(string verb)
		{
			Console.WriteLine("*** Stage: " + verb + " ***");

			// read config
			_opts.ReadConfig(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "VssSvnConverter.conf"));

			switch (verb)
			{
				case "ui":
					Application.EnableVisualStyles();
					Application.SetCompatibleTextRenderingDefault(false);
					Application.Run(new SimpleUI());
					break;

				case "build-list":
					new ImportListBuilder().Build(_opts);
					Console.WriteLine("Next: build-versions");
					break;

				case "build-list-stats":
					new ImportListStatsBuilder().Build(_opts, new ImportListBuilder().Load());
					Console.WriteLine("Next: build-versions");
					break;

				case "build-versions":
					new VssVersionsBuilder().Build(_opts, new ImportListBuilder().Load());
					Console.WriteLine("Next: build-cache");
					break;

				case "build-links":
					new LinksBuilder().Build(_opts, new ImportListBuilder().Load());
					Console.WriteLine("Next: build-cache");
					break;

				case "build-cache":
					new CacheBuilder(_opts).Build(new VssVersionsBuilder().Load());
					Console.WriteLine("Next: build-commits");
					break;

				case "build-commits":
					new CommitsBuilder().Build(_opts, new CacheBuilder(_opts).Load());
					Console.WriteLine("Next: build-wc");
					break;

				case "build-wc":
					new WcBuilder().Build(_opts);
					Console.WriteLine("Next: import");
					break;

				case "import":
					new Importer().Import(_opts, new CommitsBuilder().Load());
					break;

				case "build-scripts":
					new ScriptsBuilder().Build(_opts, new ImportListBuilder().Load(), new ImportListBuilder().LoadRootTypes());
					break;

				default:
					throw new ApplicationException("Unknown stage: " + verb);
			}

			Console.WriteLine("");

			if(_opts.Ask)
			{
				Console.WriteLine("Press any key...");
				Console.ReadKey();
			}
		}

		private static void ShowHelp(string unkVerb = null)
		{
			if(unkVerb != null)
				Console.WriteLine("Unknown verb: {0}\n", unkVerb);

			Console.WriteLine(@"Usage: VssSvnConvert stage [options]
where
	stage - conversion stage:
		ui - show simple UI with all available stages
		all - perform all stages. With 5 second timeout between.
		build-list - build list of files for import. After building, it can be edited by hand to remove *.exe for example
		build-list-stats - build statistic for list of import
		build-versions - build list of all versions of selected files
		build-links - build list of linked files
		build-cache - get all required versions to local cache
		build-commits - build list of commits:. Also, can be edited by hand for edit user names, for examle. DateTime in ticks, UTC.
		build-wc - Checkout specified URL.
		import - import commits to SVN working copy
		build-scripts - generate some useful scripts

	each stage suppose, that previous stage results was already build and available.

Options for
	build-list-stats:
		--prefix=$/Project/xxxx - calculate statistic only for files with specified prefix
		--filter=Project/[^/]+$ - calculate statistic only for files with specified regex

Setup config VssSvnConvert.conf before run converter.

notes:
	!!! SVN repositiory should allow change revision properties. This need for set correct user and date per commit.

example:
	VssSvnConvert all
");
		}
	}
}
