using System;
using System.Collections.Generic;
using System.IO;

namespace VssSvnConverter.Core
{
	class GitExecHelper
	{
		public readonly string WorkTree;
		public readonly string GitDir;

		readonly ExecHelper _execHelper;
		readonly Dictionary<string, string> _envVars;

		public GitExecHelper(string gitExe, string workingCopy, TextWriter log)
		{
			_execHelper = new ExecHelper(gitExe, log, false);
			WorkTree = workingCopy;
			GitDir = Path.Combine(WorkTree, ".git");

			_envVars = new Dictionary<string, string>
			{
				{ "GIT_DIR", GitDir },
				{ "GIT_WORK_TREE", WorkTree }
			};
		}

		public void CheckRepositoryValid()
		{
			if (!Directory.Exists(WorkTree))
				throw new ApplicationException("Work tree does not exists: " + WorkTree);

			if (!Directory.Exists(GitDir))
				throw new ApplicationException("Git dir not found: " + GitDir);
		}

		public ExecHelper.ExecResult Exec(string args)
		{
			var r = _execHelper.Exec(args, _envVars);
			ExecHelper.ValidateResult(r, args);
			return r;
		}

		public ExecHelper.ExecResult ExecCommit(string args)
		{
			var r = _execHelper.Exec(args, _envVars);

			// nothing to commit - ok valid
			if (r.ExitCode == 1 && r.StdOut.Contains("nothing to commit, working directory clean"))
				return r;

			ExecHelper.ValidateResult(r, args);

			return r;
		}
	}
}
