using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace VssSvnConverter.Core
{
	class TfsDriver : IDestinationDriver
	{
		readonly TfExecHelper _tfHelper;
		readonly string _workingCopy;
		readonly string _commitMessageFile;

		public TfsDriver(string tfPath, string workingCopy, TextWriter log)
		{
			_workingCopy = workingCopy;
			_tfHelper = new TfExecHelper(tfPath, workingCopy, log);

			_commitMessageFile = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

			CheckWorkingCopyStatus();
		}

		public string WorkingCopy
		{
			get { return _workingCopy; }
		}

		public void CleanupWorkingTree()
		{
			if (IsWorkingCopyClean(false))
				return;

			_tfHelper.Exec(string.Format("undo . /noprompt /recursive"));
		}

		public void StartRevision()
		{
			CheckWorkingCopyStatus();
		}

		void CheckWorkingCopyStatus()
		{
			if(!IsWorkingCopyClean(true))
				throw new ApplicationException("Working tree does should be clean");
		}

		bool IsWorkingCopyClean(bool failOnEventInStdErr)
		{
			var r = _tfHelper.Exec("status");

			if (r.StdOut.Trim() == "There are no pending changes.")
				return true;

			if (failOnEventInStdErr)
				r.ForStdError(s => { throw new ApplicationException("Status say in stderr:\n" + s); });

			return false;
		}

		public void AddDirectory(string dir)
		{
			Directory.CreateDirectory(dir);
			_tfHelper.Exec(string.Format("add \"{0}\" /noprompt /recursive", dir));
		}

		public void AddFiles(params string[] files)
		{
			foreach (var chunk in files.Partition(25))
			{
				var cmdLine = string.Format("add {0} /noprompt", string.Join(" ", chunk.Select(file => '"' + file + '"')));
				_tfHelper.Exec(cmdLine);
			}
		}

		public string GetDiff(string file)
		{
			throw new NotImplementedException();
		}

		public void Revert(string file)
		{
			_tfHelper.Exec(string.Format("undo /recursive \"{0}\" /noprompt", file));
		}

		public void CommitRevision(string author, string comment, DateTime time)
		{
			var sb = new StringBuilder("checkin /bypass /force /noprompt /recursive");
			if (!string.IsNullOrWhiteSpace(author))
			{
				try
				{
					var ma = new MailAddress(author);
					sb.AppendFormat(" /author:\"{0}\"", ma.DisplayName);
				}
				catch
				{
					sb.AppendFormat(" /author:\"{0}\"", author);
				}
			}

			if(!string.IsNullOrWhiteSpace(comment))
			{
				File.WriteAllText(_commitMessageFile, comment);
				sb.AppendFormat(" /comment:@\"{0}\"", _commitMessageFile);
			}

			_tfHelper.ExecCommit(sb.ToString());
		}
	}
}
