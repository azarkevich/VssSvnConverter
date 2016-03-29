using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace VssSvnConverter.Core
{
	class TfsDriver : IDestinationDriver
	{
		readonly ExecHelper _execHelper;
		readonly string _commitMessageFile;

		readonly Options _opts;

		public TfsDriver(Options opts, TextWriter log, bool checkWcStatus)
		{
			_opts = opts;
			_execHelper = new ExecHelper(opts.TfExe, log, false, TimeSpan.FromSeconds(60), true);

			_commitMessageFile = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

			CheckRepositoryValid();

			if (checkWcStatus)
				CheckWorkingCopyStatus();
		}

		void CheckRepositoryValid()
		{
			if (!Directory.Exists(_opts.TfsWorkTreeDir))
				throw new ApplicationException("Work tree does not exists: " + _opts.TfsWorkTreeDir);

			//var tfDir = Path.Combine(_opts.TfsWorkTreeDir, "$tf");

			//if (!Directory.Exists(tfDir))
			//	throw new ApplicationException("$tf dir was not found: " + tfDir);
		}

		ExecHelper.ExecResult Exec(string args)
		{
			var r = _execHelper.Exec(args, null, _opts.TfsWorkTreeDir);
			ExecHelper.ValidateResult(r, args);
			return r;
		}

		ExecHelper.ExecResult ExecCommit(string args)
		{
			var r = _execHelper.Exec(args, null, _opts.TfsWorkTreeDir);

			// ok to 'noting to commit'
			if (r.ExitCode == 1 && r.StdErr.Trim() == "There are no pending changes.")
				return r;

			ExecHelper.ValidateResult(r, args);

			return r;
		}

		public string WorkingCopy
		{
			get { return _opts.TfsWorkTreeDir; }
		}

		public void CleanupWorkingTree()
		{
			if (IsWorkingCopyClean(false))
				return;

			Exec(string.Format("undo . /noprompt /recursive"));
			CheckWorkingCopyStatus();
		}

		public void StartRevision()
		{
			if (!_opts.TfNoCheckStatusBeforeStartRevision)
				CheckWorkingCopyStatus();
		}

		void CheckWorkingCopyStatus()
		{
			if(!IsWorkingCopyClean(true))
				throw new ApplicationException("Working tree does should be clean");
		}

		bool IsWorkingCopyClean(bool failOnEventInStdErr)
		{
			// tf status often hung
			var r = Exec("status . /recursive /noprompt");

			if (r.StdOut.Trim() == "There are no pending changes.")
				return true;

			if (failOnEventInStdErr)
				r.ForStdError(s => { throw new ApplicationException("Status say in stderr:\n" + s); });

			return false;
		}

		public void AddDirectory(string dir)
		{
			Directory.CreateDirectory(dir);
			Exec(string.Format("add \"{0}\" /noprompt /recursive", dir));
		}

		public void AddFiles(params string[] files)
		{
			foreach (var chunk in files.Partition(25))
			{
				var cmdLine = string.Format("add {0} /noprompt", string.Join(" ", chunk.Select(file => '"' + file + '"')));
				Exec(cmdLine);
			}
		}

		public string GetDiff(string file)
		{
			throw new NotImplementedException();
		}

		public void Revert(string file)
		{
			Exec(string.Format("undo /recursive \"{0}\" /noprompt", file));
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

			ExecCommit(sb.ToString());
		}
	}
}
