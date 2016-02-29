using System;
using System.IO;
using System.Linq;

namespace VssSvnConverter.Core
{
	class GitDriver : IDestinationDriver
	{
		readonly TextWriter _log;
		readonly string _defaultEmailDomain;
		readonly GitExecHelper _gitHelper;

		public GitDriver(string gitExe, string workingCopy, string defaultEmailDomain, TextWriter log)
		{
			_defaultEmailDomain = defaultEmailDomain;
			_log = log;

			_gitHelper = new GitExecHelper(gitExe, workingCopy, log);
			_gitHelper.CheckRepositoryValid();

			CheckWorkingCopyStatus();
		}

		public string WorkingCopy
		{
			get { return _gitHelper.WorkTree; }
		}

		public void CleanupWorkingTree()
		{
			throw new NotImplementedException();
		}

		public void StartRevision()
		{
			CheckWorkingCopyStatus();
		}

		public void AddDirectory(string dir)
		{
			_log.WriteLine("Add dir '{0}'", dir);
			Directory.CreateDirectory(dir);
		}

		public void AddFiles(params string[] files)
		{
			foreach (var chunk in files.Partition(25))
			{
				_gitHelper.Exec("add -f -- " + string.Join(" ", chunk.Select(file => '"' + file + '"')));
			}
		}

		public string GetDiff(string file)
		{
			var r = _gitHelper.Exec(string.Format("diff --unified=0 -- \"{0}\"", file));

			return r.StdOut;
		}

		public void Revert(string file)
		{
			_gitHelper.Exec(string.Format("reset HEAD -- \"{0}\"", file));
			_gitHelper.Exec(string.Format("checkout -- \"{0}\"", file));
		}

		public void CommitRevision(string author, string comment, DateTime time)
		{
			var commitMessageFile = Path.Combine(_gitHelper.GitDir, "IMPORT_COMMIT_MESSAGE");

			File.WriteAllText(commitMessageFile, comment);

			if(author.IndexOf('<') == -1 || author.IndexOf('>') == -1)
			{
				var authorName = author;
				var authorEmail = author;

				// strip @ from name
				if (authorName.IndexOf('@') != -1)
					authorName = authorName.Substring(0, author.IndexOf('@'));

				// add @domain to mail
				if (authorEmail.IndexOf('@') == -1)
					authorEmail = authorEmail + _defaultEmailDomain;

				author = string.Format("{0} <{1}>", authorName, authorEmail);
			}

			var cmd = string.Format("commit --all --file=\"{0}\" --allow-empty-message --author=\"{1}\" --date={2}", commitMessageFile, author, time.ToString("o"));

			_gitHelper.ExecCommit(cmd);
		}

		public static void Create(string gitExe, string repoDir)
		{
			if (Directory.Exists(repoDir))
			{
				foreach (var file in Directory.GetFiles(repoDir, "*.*", SearchOption.AllDirectories))
				{
					File.SetAttributes(file, FileAttributes.Normal);
					File.Delete(file);
				}

				foreach (var dir in Directory.GetDirectories(repoDir))
				{
					Directory.Delete(dir, true);
				}
			}

			Directory.CreateDirectory(repoDir);

			new GitExecHelper(gitExe, repoDir, Console.Out).Exec("init");
		}

		void CheckWorkingCopyStatus()
		{
			var r = _gitHelper.Exec("status --porcelain");

			if(r.StdOut.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length > 0)
				throw new ApplicationException("Working tree does should be clean. Status say:\n" + r.StdOut);

			r.ForStdError(s => { throw new ApplicationException("Status say in stderr:\n" + s); });
		}
	}
}
