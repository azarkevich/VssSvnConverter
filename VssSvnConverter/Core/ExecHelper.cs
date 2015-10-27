using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace VssSvnConverter.Core
{
	class ExecHelper
	{
		public class ExecResult
		{
			public readonly int ExitCode;
			public readonly string StdOut;
			public readonly string StdErr;

			public ExecResult(int exitCode, string stdOut, string stdErr)
			{
				ExitCode = exitCode;
				StdOut = stdOut;
				StdErr = stdErr;
			}

			public void ForStdOut(Action<string> a)
			{
				if (!string.IsNullOrWhiteSpace(StdOut))
					a(StdOut);
			}

			public void ForStdError(Action<string> a)
			{
				if (!string.IsNullOrWhiteSpace(StdErr))
					a(StdErr);
			}
		}

		readonly string _exe;
		readonly TextWriter _log;
		readonly bool _validate;

		public ExecHelper(string exe, TextWriter log, bool validate)
		{
			_log = log;
			_validate = validate;
			_exe = exe;
		}

		public static void ValidateResult(ExecResult r, string args)
		{
			if (r.ExitCode != 0)
			{
				var sb = new StringBuilder("Exec failed: ");
				sb.AppendLine("cmd: " + args);
				sb.AppendLine("exitcode: " + r.ExitCode);
				r.ForStdError(s => sb.AppendLine("stderr: " + s));
				r.ForStdOut(s => sb.AppendLine("stdout: " + s));

				throw new ApplicationException(sb.ToString());
			}
		}

		public ExecResult Exec(string args, IDictionary<string, string> envVars = null, string workingDir = null)
		{
			var psi = new ProcessStartInfo(_exe, args);
			psi.CreateNoWindow = true;
			psi.RedirectStandardError = true;
			psi.RedirectStandardOutput = true;
			psi.RedirectStandardInput = true;
			psi.UseShellExecute = false;
			psi.WorkingDirectory = workingDir;

			if (envVars != null)
			{
				foreach (var envVar in envVars)
				{
					psi.EnvironmentVariables.Add(envVar.Key, envVar.Value);
				}
			}

			var p = Process.Start(psi);

			Debug.Assert(p != null);

			var stdOut = "";
			var t1 = Task.Factory.StartNew(() =>
			{
				stdOut = p.StandardOutput.ReadToEnd();
			});

			var stdErr = "";
			var t2 = Task.Factory.StartNew(() =>
			{
				stdErr = p.StandardError.ReadToEnd();
			});

			p.WaitForExit();
			t1.Wait();
			t2.Wait();

			if (_log != null)
			{
				_log.WriteLine("{0} {1}: {2}", Path.GetFileName(_exe), args, p.ExitCode);
				if (!string.IsNullOrWhiteSpace(stdOut))
					_log.WriteLine(stdOut);
				if (!string.IsNullOrWhiteSpace(stdErr))
					_log.WriteLine("stderr: " + stdErr);
			}

			var r = new ExecResult(p.ExitCode, stdOut, stdErr);

			if (_validate)
				ValidateResult(r, args);

			return r;
		}
	}
}
