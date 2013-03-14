using System;
using System.Linq;
using System.IO;
using System.Reflection;

namespace VssSvnConverter
{
	class Program
	{
		static Int32 Main(string[] args)
		{
			try{
				if(args.Length == 0 || args.Any(a => a.StartsWith("/help")) || args.Any(a => a.StartsWith("-h")) || args.Any(a => a.StartsWith("--help")))
				{
					ShowHelp();
					return -1;
				}
			
				var opts = new Options(args);
				if(string.IsNullOrEmpty(opts.Stage))
				{
					ShowHelp();
					return -1;
				}

				opts.ReadConfig(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "VssSvnConverter.conf"));

				if(opts.Stage.ToLower() == "all")
				{
					ProcessStage(opts, "build-list");
					Console.WriteLine("Press any key for start next stage");
					Wait(5);

					ProcessStage(opts, "build-versions");
					Console.WriteLine("Press any key for start next stage");
					Wait(5);

					ProcessStage(opts, "build-cache");
					Console.WriteLine("Press any key for start next stage");
					Wait(5);

					ProcessStage(opts, "build-commits");
					Console.WriteLine("Press any key for start next stage");
					Wait(5);

					ProcessStage(opts, "build-wc");
					Console.WriteLine("Press any key for start next stage");
					Wait(5);

					ProcessStage(opts, "import");
				}
				else
				{
					ProcessStage(opts, opts.Stage);
				}
			}
			catch(ApplicationException ex)
			{
				Console.Error.WriteLine(ex.Message);
				return 1;
			}
			catch(Exception ex)
			{
				Console.Error.WriteLine(ex.ToString());
				return 1;
			}

			Console.WriteLine("Press any key...");
			Console.ReadKey();

			return 0;
		}

		static void Wait(int seconds)
		{
			Console.WriteLine("Press ctr+c for abort... Will be continued after {0} seconds", seconds);
		}

		private static void ProcessStage(Options opts, string stage)
		{
			switch (stage)
			{
				case "build-list":
					new ImportListBuilder().Build(opts);
					Console.WriteLine("Next: build-versions");
					break;

				case "build-versions":
					new VssVersionsBuilder().Build(opts, new ImportListBuilder().Load());
					Console.WriteLine("Next: build-cache");
					break;

				case "build-cache":
					new CacheBuilder().Build(opts, new VssVersionsBuilder().Load());
					Console.WriteLine("Next: build-commits");
					break;

				case "build-commits":
					new CommitsBuilder().Build(opts, new CacheBuilder().Load());
					Console.WriteLine("Next: build-wc");
					break;

				case "build-wc":
					new WcBuilder().Build(opts);
					Console.WriteLine("Next: import");
					break;

				case "import":
					new Importer().Import(opts, new CommitsBuilder().Load());
					break;

				default:
					throw new ApplicationException("Unknown stage: " + opts.Stage);
			}
		}

		private static void ShowHelp()
		{
			Console.WriteLine(@"Usage: VssSvnConvert stage [options]
where
	stage - conversion stage:
		all - perform all stages. With 5 second timeout between.
		build-list - build list of files for import: '1-import-list.txt'. After building, it can be edited by hand to remove *.exe for example
		build-versions - build list of all versions of selected files. See '2-versions-list.txt'
		build-cache - get all required versions to local cache. See '2b-cached-versions-list.txt'
		build-commits - build list of commits: 3-commits-list.txt. Also, can be edited by hand for edit user names, for examle. DateTime in ticks, UTC.
		build-wc - Checkout specified URL.
		import - import commits to SVN working copy. Used file 4-import.txt to track last commited commit.

	each stage suppose, that previous stage results was already build and available.

Setup config VssSvnConvert.conf before run converter.

notes:
	!!! SVN repositiory should allow change revision properties. This need for set correct user and date per commit.

example:
	VssSvnConvert all
");
		}
	}
}
