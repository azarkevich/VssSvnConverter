using System;
using System.IO;

namespace VssSvnConverter.Core
{
	class TfExecHelper
	{
		public readonly string WorkTree;
		public readonly string TfDir;

		readonly ExecHelper _execHelper;

		public TfExecHelper(string tfExe, string workingCopy, TextWriter log, TimeSpan? hungDetection = null)
		{
			_execHelper = new ExecHelper(tfExe, log, false, hungDetection);
			WorkTree = workingCopy;
			TfDir = Path.Combine(WorkTree, "$tf");
		}

		public void CheckRepositoryValid()
		{
			if (!Directory.Exists(WorkTree))
				throw new ApplicationException("Work tree does not exists: " + WorkTree);

			if (!Directory.Exists(TfDir))
				throw new ApplicationException("$tf dir was not found: " + TfDir);
		}

		public ExecHelper.ExecResult Exec(string args)
		{
			var r = _execHelper.Exec(args, null, WorkTree);
			ExecHelper.ValidateResult(r, args);
			return r;
		}

		public ExecHelper.ExecResult ExecCommit(string args)
		{
			var r = _execHelper.Exec(args, null, WorkTree);

			// ok to 'noting to commit'
			if (r.ExitCode == 1 && r.StdErr.Trim() == "There are no pending changes.")
				return r;

			ExecHelper.ValidateResult(r, args);

			return r;
		}
	}
}
