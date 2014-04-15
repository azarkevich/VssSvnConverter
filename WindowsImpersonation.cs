using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace VssSvnConverter
{
	public class WindowsImpersonation
	{
		static readonly Regex Login = new Regex(@"(^(?<domain>[^\\]+)\\(?<login>.+)$)|(^(?<login>[^@]+)@(?<domain>.+)$)|(?<login>.*)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

		const int LOGON32_LOGON_INTERACTIVE = 2;
		const int LOGON32_PROVIDER_DEFAULT = 0;

		[DllImport("advapi32.dll", SetLastError = true)]
		static extern int LogonUserA(String user, String domain, String password, int logonType, int logonProvider, ref IntPtr token);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern int DuplicateToken(IntPtr token, int impersonationLevel, ref IntPtr newToken);

		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern bool RevertToSelf();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		static extern bool CloseHandle(IntPtr handle);

		public static bool SplitLogin(string login, out string domain, out string user)
		{
			var m = Login.Match(login);
			domain = m.Groups["domain"].Value;
			user = m.Groups["login"].Value;
			return string.IsNullOrEmpty(domain);
		}

		public static WindowsImpersonationContext Impersonate(string login, string password)
		{
			string domain, user;

			SplitLogin(login, out domain, out user);

			return Impersonate(user, domain, password);
		}

		[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
		public static WindowsImpersonationContext Impersonate(string user, string domain, string password)
		{
			var token = IntPtr.Zero;
			var tokenDuplicate = IntPtr.Zero;

			try
			{
				if (RevertToSelf())
				{
					if (LogonUserA(user, domain, password, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, ref token) != 0)
					{
						if (DuplicateToken(token, 2, ref tokenDuplicate) != 0)
							return WindowsIdentity.Impersonate(tokenDuplicate);
					}
				}
				var err = Marshal.GetLastWin32Error();
				throw new Win32Exception(err);
			}
			finally
			{
				if (token != IntPtr.Zero)
					CloseHandle(token);
				if (tokenDuplicate != IntPtr.Zero)
					CloseHandle(tokenDuplicate);
			}
		}

		public static IDisposable Impersonate(NetworkCredential creds)
		{
			if (string.IsNullOrWhiteSpace(creds.Domain))
				return Impersonate(creds.UserName, creds.Password);

			return Impersonate(creds.UserName, creds.Domain, creds.Password);
		}
	}
}
