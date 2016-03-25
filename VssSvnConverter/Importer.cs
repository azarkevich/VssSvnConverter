using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using vsslib;
using VssSvnConverter.Core;

namespace VssSvnConverter
{
	class Importer
	{
		public const string DataFileName = "6-import.txt";
		public const string LogFileName = "log-6-import.txt";

		VssFileCache _cache;

		Options _opts;

		ILookup<string, Regex> _unimportants;

		public class CensoreGroup
		{
			public string Name;
			public Regex[] FileNameRegex;
			public IList<Tuple<Regex, string>> Replacement;
			public Encoding Encoding;
		}

		IList<CensoreGroup> _censors;

		public static volatile bool StopImport;

		public void Import(Options opts, List<Commit> commits, bool startNewSession)
		{
			StopImport = false;

			if(startNewSession && File.Exists(DataFileName))
				File.Delete(DataFileName);

			_opts = opts;

			_unimportants = opts
				.Config["unimportant-diff"]
				.Select(v => {
					var sep = v.IndexOf('?');
					if (sep == -1)
						throw new ApplicationException("Incorrect unimportant-diff: " + v + "\nAbsent separator '?' between filename and unimportant regex");

					var fileName = v.Substring(0, sep).ToLowerInvariant().Replace('\\', '/').Trim('/');
					var regex = new Regex(v.Substring(sep + 1), RegexOptions.IgnoreCase);

					return new { K = fileName, V = regex };
				})
				.ToLookup(kv => kv.K, kv => kv.V)
			;

			// load censores
			_censors = LoadCensors(opts);

			var fromCommit = 0;

			if (!startNewSession)
			{
				if (File.Exists(DataFileName))
				{
					fromCommit = File.ReadAllLines(DataFileName)
						.Select(x => Int32.Parse(x) + 1)
						.DefaultIfEmpty(0)
						.Last()
					;
				}

				if (MessageBox.Show(string.Format("Cleanu work tree and start import from commit #{0} by {1}", fromCommit, commits[fromCommit].User), "Confirmation", MessageBoxButtons.OKCancel) != DialogResult.OK)
					return;

				if (opts.ImportDriver == "tfs")
				{
					new TfsDriver(opts, Console.Out, false).CleanupWorkingTree();
				}
			}

			using (_cache = new VssFileCache(opts.CacheDir, _opts.SourceSafeIni))
			using(var log = File.CreateText(LogFileName))
			{
				log.AutoFlush = true;

				try
				{
					IDestinationDriver driver;
					if (opts.ImportDriver == "git")
					{
						driver = new GitDriver(opts.GitExe, opts.GitRepoDir, opts.GitDefaultAuthorDomain, log);
					}
					else if (opts.ImportDriver == "tfs")
					{
						driver = new TfsDriver(opts, log, true);
					}
					else
					{
						driver = new SvnDriver(opts.SvnWorkTreeDir, log);
					}

					for (var i = fromCommit; i < commits.Count; i++)
					{
						var c = commits[i];

						Console.WriteLine("[{2,6}/{3}] Import: {0:yyyy-MMM-dd HH:ss:mm}, by {1}", c.At, c.User, i, commits.Count);

						driver.StartRevision();

						LoadRevision(driver, c, log);

						driver.CommitRevision(commits[i].User, c.Comment, commits[i].At);

						// OK
						File.AppendAllText(DataFileName, i + "\n");

						if (StopImport)
							break;
					}
				}
				catch (Exception ex)
				{
					log.WriteLine(ex.ToString());
					throw;
				}
			}

			if (StopImport)
			{
				Console.WriteLine("Import interrupted.");
			}
			else
			{
				Console.WriteLine("Import complete.");
			}
		}

		public static List<CensoreGroup> LoadCensors(Options opts)
		{
			return opts
				.Config["censore-group"]
				.Select(v => {

					var fileRxs = opts.Config[string.Format("censore-{0}-file-rx", v)].Select(x => new Regex(x, RegexOptions.IgnoreCase)).ToArray();

					var encoding = Encoding.UTF8;

					var encodingStr = opts.Config[string.Format("censore-{0}-encoding", v)].FirstOrDefault();
					if (encodingStr != null)
					{
						int codePage;
						if(int.TryParse(encodingStr, out codePage))
							encoding = Encoding.GetEncoding(codePage);
						else if (encodingStr == "utf-8-no-bom")
							encoding = new UTF8Encoding(false);
						else
							encoding = Encoding.GetEncoding(encodingStr);
					}

					var replacements = new List<Tuple<Regex, string>>();
					for (var i = 0; ; i++)
					{
						var rx = opts.Config[string.Format("censore-{0}-match{1}", v, i)].Select(x => new Regex(x, RegexOptions.IgnoreCase)).FirstOrDefault();
						var replace = opts.Config[string.Format("censore-{0}-replace{1}", v, i)].FirstOrDefault();

						if (i >= 1 && rx == null && replace == null)
							break;

						if (rx == null && replace == null)
							continue;

						var rpl = Tuple.Create(rx, replace);

						replacements.Add(rpl);
					}

					return new CensoreGroup { Name = v, FileNameRegex = fileRxs, Replacement = replacements, Encoding = encoding };
				})
				.ToList();
		}

		[DllImport("Kernel32.dll", CharSet = CharSet.Unicode )]
		static extern bool CreateHardLink(string lpFileName, string lpExistingFileName,IntPtr lpSecurityAttributes);

		bool _useHardLink = true;

		void LoadRevision(IDestinationDriver driver, Commit commit, StreamWriter log)
		{
			var added = new List<string>();

			foreach (var file in commit.Files)
			{
				var filePath = _cache.GetFilePath(file.FileSpec, file.VssVersion);
				if(filePath == null)
				{
					Debugger.Break();
					log.WriteLine("File {0}@{1} absent in cache. Rerun 'VssSvnConverter build-cache'", file.FileSpec, file.VssVersion);
					Console.Error.WriteLine("File {0}@{1} absent in cache. Rerun 'VssSvnConverter build-cache'", file.FileSpec, file.VssVersion);
					Environment.Exit(1);
				}

				var relPath = file.FileSpec.TrimStart('$', '/', '\\');

				// mangle path
				if (_opts.MangleImportPath.Count > 0)
				{
					foreach (var manglePair in _opts.MangleImportPath)
					{
						relPath = manglePair.Item1.Replace(relPath, manglePair.Item2);
					}
				}

				// special mode for check unimportant differenrces
				if (_opts.ImportUnimportantOnly && !_unimportants[relPath.ToLowerInvariant().Replace('\\', '/').Trim('/')].Any())
					continue;

				log.WriteLine("Load: {0} -> {1}", file, relPath);

				var dstPath = Path.Combine(driver.WorkingCopy, relPath);

				var dstDir = Path.GetDirectoryName(dstPath);
				Debug.Assert(dstDir != null);
				if(!Directory.Exists(dstDir))
					driver.AddDirectory(dstDir);

				var addToVcs = !File.Exists(dstPath);

				if(File.Exists(dstPath))
				{
					File.Delete(dstPath);
					log.WriteLine("Deleted: {0}", dstPath);
				}

				if(_useHardLink)
				{
					_useHardLink = CreateHardLink(dstPath, filePath, IntPtr.Zero);
					log.WriteLine("CreateHl: {0} -> {1} result: {2}", filePath, dstPath, _useHardLink);
				}

				if(!_useHardLink)
				{
					File.Copy(filePath, dstPath, true);
					log.WriteLine("Copy: {0} -> {1}", filePath, dstPath);
				}

				// git can not detect modifications if MTime not updated
				File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow);

				if(addToVcs)
					added.Add(dstPath);

				// file can be modified in place if it is not hardlink
				var canBeModifiedInplace = !_useHardLink;
				Action<bool> prepareForModifyInplace = recuireContent => {

					if (canBeModifiedInplace)
						return;

					// make copy of file instead of hardlink
					File.Delete(dstPath);

					if (recuireContent)
					{
						File.Copy(filePath, dstPath, true);
						log.WriteLine("Copy: {0} -> {1}", filePath, dstPath);
					}
					canBeModifiedInplace = true;

				};

				DoCensoring(driver.WorkingCopy, dstPath, _censors, prepareForModifyInplace);

				if (!addToVcs && _unimportants.Count > 0)
					RevertUnimportant(driver, dstPath, relPath, prepareForModifyInplace);
			}

			if(added.Count > 0)
				driver.AddFiles(added.ToArray());
		}

		public static void DoCensoring(string rootDir, string dstPath, IList<CensoreGroup> censors, Action<bool> prepareFileForModifications)
		{
			var testPath = dstPath.Substring(rootDir.Length).TrimStart('\\', '/').Replace('/', '\\');

			censors = censors.Where(cg => cg.FileNameRegex.Any(rx => rx.IsMatch(testPath))).ToList();

			if (censors.Count == 0)
				return;

			var enc = censors.Select(cg => cg.Encoding).DefaultIfEmpty(Encoding.ASCII).First();

			var modified = false;
			var lines = File.ReadAllLines(dstPath, enc);

			foreach (var cs in censors)
			{
				foreach (var replacement in cs.Replacement)
				{
					for (var i = 0; i < lines.Length; i++)
					{
						var newStr = replacement.Item1.Replace(lines[i], replacement.Item2);
						if (newStr != lines[i])
						{
							lines[i] = newStr;
							modified = true;
						}
					}
				}
			}

			if (!modified)
				return;

			prepareFileForModifications(false);

			File.WriteAllLines(dstPath, lines, enc);

			Console.WriteLine("	Censored: {0}", testPath);
		}

		void RevertUnimportant(IDestinationDriver driver, string path, string relPath, Action<bool> prepareFileForModifications)
		{
			var relNorm = relPath.ToLowerInvariant().Replace('\\', '/').Trim('/');

			var unimportantPatterns = _unimportants[relNorm].ToArray();

			if (unimportantPatterns.Length == 0)
				return;

			var diff = driver.GetDiff(path);

			var diffLines = diff
				.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
				// skip header up to @@ marker
				.SkipWhile(s => !s.StartsWith("@@"))
				// get only real diff
				.Where(s => s.StartsWith("-") || s.StartsWith("+"))
			;

			// check if all differences matched to any pattern
			if (diffLines.All(diffLine => unimportantPatterns.Any(rx => rx.IsMatch(diffLine))))
			{
				// file contains only unimportant changes and will not be included in commit
				prepareFileForModifications(false);

				// revert all unimportant changes
				driver.Revert(path);

				Console.WriteLine("	Skip unimportant: {0}", relPath);
			}
		}
	}
}
