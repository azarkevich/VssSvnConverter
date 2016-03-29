using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
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
		readonly TimeSpan? _hungDetection;
		readonly bool _restartOnHung;

		public ExecHelper(string exe, TextWriter log, bool validate, TimeSpan? hungDetection = null, bool restartOnHung = false)
		{
			_log = log;
			_validate = validate;
			_exe = exe;
			_hungDetection = hungDetection;
			_restartOnHung = restartOnHung;
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
			if (!_hungDetection.HasValue)
				return TryExec(args, envVars, workingDir, CancellationToken.None);

			while (true)
			{
				try
				{
					var cts = new CancellationTokenSource(_hungDetection.Value);
					return TryExec(args, envVars, workingDir, cts.Token);
				}
				catch (OperationCanceledException)
				{
					if (_restartOnHung)
					{
						_log.WriteLine("WARNING: Process hung. Restart");
					}
					else
					{
						_log.WriteLine("WARNING: Process hung. Exception");
						throw;
					}
				}
			}
		}

		ExecResult TryExec(string args, IDictionary<string, string> envVars, string workingDir, CancellationToken ct)
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
					psi.EnvironmentVariables.Add(envVar.Key, envVar.Value);
			}

			_log.WriteLine("START: {0} {1}", psi.FileName, psi.Arguments);

			Process p = null;

			IDisposable cancellationRegistration = null;
			if(ct.CanBeCanceled)
			{
				cancellationRegistration = ct.Register(() => {
					p.Kill();
				});
			}

			p = Process.Start(psi);
			Debug.Assert(p != null);

			var stdOut = "";
			var t1 = Task.Factory.StartNew(() => {
				var sb = new StringBuilder();

				while (!p.StandardOutput.EndOfStream)
				{
					var line = p.StandardOutput.ReadLine();
					sb.AppendLine(line);
					if (_log != null)
						_log.WriteLine("STDOUT: " + line);
				}
				stdOut = sb.ToString();
			});

			var stdErr = "";
			var t2 = Task.Factory.StartNew(() => {
				var sb = new StringBuilder();
				while (!p.StandardError.EndOfStream)
				{
					var line = p.StandardError.ReadLine();
					sb.AppendLine(line);
					if (_log != null)
						_log.WriteLine("STDERR: " + line);
				}
				stdErr = sb.ToString();
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

			p = null;
			if (cancellationRegistration != null)
				cancellationRegistration.Dispose();

			ct.ThrowIfCancellationRequested();

			if (_validate)
				ValidateResult(r, args);

			return r;
		}
	}
}
