using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using SharpSvn;

namespace VssSvnConverter.Core
{
	class SvnDriver : IDestinationDriver
	{
		readonly string _wc;
		readonly Uri _svnUri;
		readonly TextWriter _log;

		public SvnDriver(string workingCopy, TextWriter log)
		{
			_log = log;
			_wc = workingCopy;

			if(!Directory.Exists(_wc))
				throw new ApplicationException("Working copy does not exists: " + _wc);

			if (!Directory.Exists(Path.Combine(_wc, ".svn")))
				throw new ApplicationException("Working copy has no .svn: " + _wc);

			using (var svn = new SvnClient())
			{
				SvnInfoEventArgs info;
				svn.GetInfo(new SvnPathTarget(_wc), out info);

				_svnUri = info.Uri;

				CheckWcStatus(svn);
			}
		}

		public string WorkingCopy
		{
			get { return _wc; }
		}

		public void CleanupWorkingTree()
		{
			throw new NotImplementedException();
		}

		public void StartRevision()
		{
#if DEBUG
			using (var svn = new SvnClient())
			{
				CheckWcStatus(svn);
			}
#endif
		}

		public void AddDirectory(string dir)
		{
			using (var svn = new SvnClient())
				svn.CreateDirectory(dir, new SvnCreateDirectoryArgs { CreateParents = true });
		}

		public void AddFile(string file)
		{
			using (var svn = new SvnClient())
				svn.Add(file);
		}

		public string GetDiff(string file)
		{
			using (var svn = new SvnClient())
			{
				var ms = new MemoryStream();

				svn.Diff(
					new SvnPathTarget(file, SvnRevisionType.Base),
					new SvnPathTarget(file, SvnRevisionType.Working), new SvnDiffArgs {NoProperties = true},
					ms
				);

				if (ms.Length == 0)
					return "";

				ms.Position = 0;

				return new StreamReader(ms, Encoding.UTF8).ReadToEnd();
			}
		}

		public void Revert(string file)
		{
			using (var svn = new SvnClient())
			{
				svn.Revert(file);
			}
		}

		public void CommitRevision(string author, string comment, DateTime time)
		{
			using (var svn = new SvnClient())
			{
				// commit
				var commitArgs = new SvnCommitArgs { LogMessage = "author: " + author + "\n" + comment };

				SvnCommitResult cr;
				svn.Commit(_wc, commitArgs, out cr);

				if (cr == null)
				{
					Console.WriteLine("	Nothing to commit. Seems this revision was already added or contains only unimportant chnages ?");
					return;
				}

				try
				{
					var revision = new SvnRevision(cr.Revision);

					svn.SetRevisionProperty(_svnUri, revision, "svn:author", author);
					svn.SetRevisionProperty(_svnUri, revision, "svn:log", comment);
					svn.SetRevisionProperty(_svnUri, revision, "svn:date", time.ToString("o"));
				}
				catch (Exception ex)
				{
					_log.WriteLine("Change props error: {0}", ex);
				}
			}
		}

		public static void Create(string repoDir, string wcDir)
		{
			if (Directory.Exists(wcDir))
			{
				Directory.Delete(wcDir, true);
			}

			using (var svn = new SvnRepositoryClient())
			{
				if (Directory.Exists(repoDir))
					svn.DeleteRepository(repoDir);

				svn.CreateRepository(repoDir);
			}

			// create hooks
			File.WriteAllText(Path.Combine(repoDir, "hooks/post-revprop-change.bat"), "exit 0");
			File.WriteAllText(Path.Combine(repoDir, "hooks/pre-revprop-change.bat"), "exit 0");

			using (var svn = new SvnClient())
			{
				var repoUri = new Uri("file:///" + repoDir.Replace('\\', '/'));

				svn.CheckOut(new SvnUriTarget(repoUri), wcDir);

				foreach (var fse in Directory.EnumerateFileSystemEntries(wcDir))
				{
					if (Path.GetFileName(fse).ToLowerInvariant() == ".svn")
						continue;

					svn.Add(fse, SvnDepth.Infinity);
				}

				svn.Commit(wcDir, new SvnCommitArgs { LogMessage = "PreCreate revision" });
			}
		}

		void CheckWcStatus(SvnClient svn)
		{
			Collection<SvnStatusEventArgs> statuses;
			if (!svn.GetStatus(_wc, out statuses))
				throw new ApplicationException("SvnClient.GetStatus returns false");

			if (statuses.Count > 0)
				throw new ApplicationException("SVN working copy has files/directories in non-normal state.\nMake reverts/remove unversioned files and try again with stage import.\n" + _wc);
		}
	}
}
