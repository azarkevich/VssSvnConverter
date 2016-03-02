using System;
using System.Diagnostics;
using System.IO;

namespace vsslib
{
	public class SSExeHelper
	{
		static string _ssExePath;

		public static void SetupSS(string ssPath, string sourceSafeIni, string sourceSafeUser, string sourceSafePassword)
		{
			Environment.SetEnvironmentVariable("SSDIR", Path.GetDirectoryName(sourceSafeIni), EnvironmentVariableTarget.Process);
			Environment.SetEnvironmentVariable("SSUSER", sourceSafeUser, EnvironmentVariableTarget.Process);
			Environment.SetEnvironmentVariable("SSPWD", sourceSafePassword, EnvironmentVariableTarget.Process);

			_ssExePath = ssPath;
		}

		public string Get(string spec, int version, string dstDir)
		{
			// -I- -W -GL"dest path" -GWR

			var dstFile = Path.Combine(dstDir, Path.GetFileName(spec));
			if(File.Exists(dstFile))
				File.Delete(dstFile);

			var args = string.Format("get \"{0}\" -V{1} -I- -W -GWR -GF- -GL\"{2}\"", spec, version, dstDir);

			var psi = new ProcessStartInfo(_ssExePath, args);
			psi.CreateNoWindow = true;
			psi.UseShellExecute = false;

			using(var p = Process.Start(psi))
			{
				p.WaitForExit();

				if ((p.ExitCode != 0) || !File.Exists(dstFile))
					return null;

				return dstFile;
			}
		}
	}
}
