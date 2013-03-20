using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SharpSvn;
using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using SourceSafeTypeLib;
using vcslib;
using vsslib;

namespace VssSvnConverter
{
	class Importer
	{
		IVSSDatabase _db;
		Uri _svnUri;
		VssFileCache _cache;

		public void Import(Options opts, List<Commit> commits)
		{
			_db = opts.DB;
			_svnUri = opts.SvnRepoUri;

			var fromCommit = 0;

			if(File.Exists("4-import.txt"))
				fromCommit = File.ReadAllLines("4-import.txt").Select(Int32.Parse).DefaultIfEmpty(0).Last();

			using(_cache = new VssFileCache(opts.CacheDir, _db.SrcSafeIni))
			using(var log = File.CreateText("4-import.txt.log"))
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
					log.WriteLine("File {0}@{1} absent in cache. Rerun 'VssSvnConverter build-cache'", file.FileSpec, file.VssVersion);
					Console.Error.WriteLine("File {0}@{1} absent in cache. Rerun 'VssSvnConverter build-cache'", file.FileSpec, file.VssVersion);
					Environment.Exit(1);
				}

				var relPath = file.FileSpec.TrimStart('$', '/', '\\');

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
			}

			var commitArgs = new SvnCommitArgs { LogMessage = "author: " + commit.User + "\n" + commit.Comment };

			SvnCommitResult cr;
			svn.Commit("svn-wc", commitArgs, out cr);
			return cr;
		}
	}
}
