using System;
using System.Collections.Generic;
using System.Linq;
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
		IVSSDatabase _db;
		Uri _svnUri;
		FileCache _cache;

		public void Import(Options opts, List<Commit> commits)
		{
			_db = opts.DB;
			_svnUri = opts.SvnRepoUri;

			var fromCommit = 0;

			if(File.Exists("4-import.txt"))
				fromCommit = File.ReadAllLines("4-import.txt").Select(Int32.Parse).DefaultIfEmpty(0).Last();

			using(_cache = new FileCache(_db.SrcSafeIni, opts.CacheDir))
			using(var log = File.CreateText("4-import.txt.log"))
			{
				try
				{
					using (var svn = new SvnClient())
					{
						Collection<SvnStatusEventArgs> statuses;
						svn.GetStatus("svn-wc", out statuses);

						if (statuses.Count > 0)
							throw new ApplicationException("SVN working copy (svn-wc) has files/directories in non-normal state. Make reverts/remove unversioned files and try again with stage import");

						for (var i = fromCommit; i < commits.Count; i++)
						{
							var c = commits[i];
							Console.WriteLine("[{3:D5}/{4:D5}] Start import commit: {0}, by {1}, Comment: {2}", c.At, c.User, string.Join("; ", c.Comments.ToArray()), i, commits.Count);

							var cr = LoadRevision(svn, c, log);

							File.AppendAllText("4-import.txt", i + "\n");

							if (cr == null)
							{
								Console.WriteLine("Nothing to commit. Seems this revision was already added ?");
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

		SvnCommitResult LoadRevision(SvnClient svn, Commit commit, StreamWriter log)
		{
			foreach (var file in commit.Files)
			{
				var filePath = _cache.GetFilePath(file.FileSpec, file.VssVersion);
				if(filePath == null)
				{
					log.WriteLine("File {0}@{1} absent in cache. Rerun 'VssSvnConverter build-cache'", file.FileSpec, file.VssVersion);
					Console.Error.WriteLine("File {0}@{1} absent in cache. Rerun 'VssSvnConverter build-cache'", file.FileSpec, file.VssVersion);
					Environment.Exit(1);
				}

				var relPath = file.FileSpec.TrimStart('$', '/', '\\');

				var dstPath = Path.Combine(Path.Combine(Environment.CurrentDirectory, "svn-wc"), relPath);

				var dstDir = Path.GetDirectoryName(dstPath);
				Debug.Assert(dstDir != null);
				if(!Directory.Exists(dstDir))
					svn.CreateDirectory(dstDir, new SvnCreateDirectoryArgs { CreateParents = true });

				var addToSvn = !File.Exists(dstPath);

				File.Copy(filePath, dstPath, true);

				if(addToSvn)
					svn.Add(dstPath);
			}

			var commitArgs = new SvnCommitArgs { LogMessage = "author: " + commit.User + "\n" + commit.Comment };

			SvnCommitResult cr;
			svn.Commit("svn-wc", commitArgs, out cr);
			return cr;
		}
	}
}
