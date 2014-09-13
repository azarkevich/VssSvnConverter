﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using SharpSvn;
using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using SourceSafeTypeLib;
using vsslib;

namespace VssSvnConverter
{
	class Importer
	{
		public const string DataFileName = "6-import.txt";
		public const string LogFileName = "log-6-import.txt";

		IVSSDatabase _db;
		Uri _svnUri;
		VssFileCache _cache;

		Options _opts;

		ILookup<string, Regex> _unimportants;

		class CensoreGroup
		{
			public string Name;
			public Regex[] FileNameRegex;
			public IList<Tuple<Regex, string>> Replacement;
			public Encoding Encoding;
		}

		IList<CensoreGroup> _censors;

		public void Import(Options opts, List<Commit> commits)
		{
			_db = opts.DB;
			_svnUri = opts.SvnRepoUri;
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
			_censors = opts
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
				.ToList()
			;

			var fromCommit = 0;

			if(File.Exists(DataFileName))
				fromCommit = File.ReadAllLines(DataFileName).Select(Int32.Parse).DefaultIfEmpty(0).Last();

			using(_cache = new VssFileCache(opts.CacheDir, _db.SrcSafeIni))
			using(var log = File.CreateText(LogFileName))
			{
				try
				{
					using (var svn = new SvnClient())
					{
						Collection<SvnStatusEventArgs> statuses;
						if(!svn.GetStatus("svn-wc", out statuses))
							throw new ApplicationException("SvnClient.GetStatus returns false");

						if (statuses.Count > 0)
							throw new ApplicationException("SVN working copy (svn-wc) has files/directories in non-normal state. Make reverts/remove unversioned files and try again with stage import");

						for (var i = fromCommit; i < commits.Count; i++)
						{
							var c = commits[i];
							Console.WriteLine("[{2,6}/{3}] Start import commit: {0}, by {1}", c.At, c.User, i, commits.Count);

							var cr = LoadRevision(svn, c, log);

							File.AppendAllText(DataFileName, i + "\n");

							if (cr == null)
							{
								Console.WriteLine("	Nothing to commit. Seems this revision was already added or contains only unimportant chnages ?");
								continue;
							}

							try
							{
								var revision = new SvnRevision(cr.Revision);

								svn.SetRevisionProperty(_svnUri, revision, "svn:author", commits[i].User);
								svn.SetRevisionProperty(_svnUri, revision, "svn:log", c.Comment);
								svn.SetRevisionProperty(_svnUri, revision, "svn:date", commits[i].At.ToString("o"));
							}
							catch (Exception ex)
							{
								log.WriteLine("Change props error: {0}", ex);
							}
						}
					}
				}
				catch (Exception ex)
				{
					log.WriteLine(ex.ToString());
					log.Flush();
					throw;
				}
			}
			Console.WriteLine("Import complete.");
		}
		[DllImport("Kernel32.dll", CharSet = CharSet.Unicode )]
		static extern bool CreateHardLink(string lpFileName, string lpExistingFileName,IntPtr lpSecurityAttributes);

		bool _useHardLink = true;

		SvnCommitResult LoadRevision(SvnClient svn, Commit commit, StreamWriter log)
		{
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

				// special mode for check unimportant differenrces
				if (_opts.ImportUnimportantOnly && !_unimportants[relPath.ToLowerInvariant().Replace('\\', '/').Trim('/')].Any())
					continue;

				log.WriteLine("Load: {0} -> {1}", file, relPath);

				var dstPath = Path.Combine(Path.Combine(Environment.CurrentDirectory, "svn-wc"), relPath);

				var dstDir = Path.GetDirectoryName(dstPath);
				Debug.Assert(dstDir != null);
				if(!Directory.Exists(dstDir))
					svn.CreateDirectory(dstDir, new SvnCreateDirectoryArgs { CreateParents = true });

				var addToSvn = !File.Exists(dstPath);

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

				if(addToSvn)
					svn.Add(dstPath);

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

				DoCensoring(dstPath, relPath, prepareForModifyInplace);

				if (!addToSvn && _unimportants.Count > 0)
					RevertUnimportant(svn, dstPath, relPath, prepareForModifyInplace);
			}

			var commitArgs = new SvnCommitArgs { LogMessage = "author: " + commit.User + "\n" + commit.Comment };

			SvnCommitResult cr;
			svn.Commit("svn-wc", commitArgs, out cr);
			return cr;
		}

		void DoCensoring(string dstPath, string relPath, Action<bool> prepareFileForModifications)
		{
			var censors = _censors.Where(cg => cg.FileNameRegex.Any(rx => rx.IsMatch(relPath))).ToArray();

			if (censors.Length == 0)
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

			Console.WriteLine("	Censored: {0}", relPath);
		}

		void RevertUnimportant(SvnClient svn, string path, string relPath, Action<bool> prepareFileForModifications)
		{
			var relNorm = relPath.ToLowerInvariant().Replace('\\', '/').Trim('/');

			var unimportantPatterns = _unimportants[relNorm].ToArray();

			if (unimportantPatterns.Length == 0)
				return;

			var ms = new MemoryStream();
			svn.Diff(
				new SvnPathTarget(path, SvnRevisionType.Base),
				new SvnPathTarget(path, SvnRevisionType.Working), new SvnDiffArgs {NoProperties = true},
				ms
			);

			if (ms.Length == 0)
				return;

			ms.Position = 0;
			var diffLines = new StreamReader(ms, Encoding.UTF8)
				.ReadToEnd()
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
				svn.Revert(path);

				Console.WriteLine("	Skip unimportant: {0}", relPath);
			}
		}
	}
}