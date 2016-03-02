using System;
using System.IO;
using VssSvnConverter.Core;

namespace vsslib
{
	public class SSExeHelper
	{
		public static void SetupSS(string sourceSafeIni, string sourceSafeUser, string sourceSafePassword)
		{
			Environment.SetEnvironmentVariable("SSDIR", Path.GetDirectoryName(sourceSafeIni), EnvironmentVariableTarget.Process);
			Environment.SetEnvironmentVariable("SSUSER", sourceSafeUser, EnvironmentVariableTarget.Process);
			Environment.SetEnvironmentVariable("SSPWD", sourceSafePassword, EnvironmentVariableTarget.Process);
		}

		readonly ExecHelper _execHelper;

		public SSExeHelper(string ssExePath, TextWriter log)
		{
			_execHelper = new ExecHelper(ssExePath, log, false);
		}

		public string Get(string spec, int version, string dstDir)
		{
			var dstFile = Path.Combine(dstDir, Path.GetFileName(spec));
			if(File.Exists(dstFile))
				File.Delete(dstFile);

			var args = string.Format("get \"{0}\" -V{1} -I- -W -GWR -GF- -GL\"{2}\"", spec, version, dstDir);

			var r = _execHelper.Exec(args);

			if ((r.ExitCode != 0) || !File.Exists(dstFile))
				return null;

			return dstFile;
		}
	}
}
