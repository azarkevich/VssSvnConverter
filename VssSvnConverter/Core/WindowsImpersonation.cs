using System.Runtime.InteropServices;
using System.Security.Principal;

namespace VssSvnConverter.Core;

public class WindowsImpersonation
{
	[DllImport("advapi32.dll", SetLastError = true)]
	private static extern bool LogonUser(
		string lpszUsername,
		string lpszDomain,
		string lpszPassword,
		int dwLogonType,
		int dwLogonProvider,
		out IntPtr phToken);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool CloseHandle(IntPtr hHandle);

	public static IDisposable Impersonate(string username, string password, string domain)
	{
		const int LOGON32_LOGON_INTERACTIVE = 2;
		const int LOGON32_PROVIDER_DEFAULT = 0;

		IntPtr token = IntPtr.Zero;
		try
		{
			if (LogonUser(
				username,
				domain,
				password,
				LOGON32_LOGON_INTERACTIVE,
				LOGON32_PROVIDER_DEFAULT,
				out token))
			{
				// Create WindowsIdentity from the token
				var identity = new WindowsIdentity(token);

				// Return a custom disposable that handles impersonation
				return new ImpersonationContext(identity);
			}
			else
			{
				int error = Marshal.GetLastWin32Error();
				throw new System.ComponentModel.Win32Exception(error, "LogonUser failed");
			}
		}
		finally
		{
			if (token != IntPtr.Zero)
			{
				CloseHandle(token);
			}
		}
	}

	// Custom implementation of impersonation context
	private class ImpersonationContext : IDisposable
	{
		private WindowsIdentity _identity;
		private WindowsIdentity? _previousIdentity;

		public ImpersonationContext(WindowsIdentity identity)
		{
			_identity = identity;
			// Store the current identity and set the new one
			_previousIdentity = WindowsIdentity.GetCurrent();
			Thread.CurrentPrincipal = new WindowsPrincipal(identity);
		}

		public void Dispose()
		{
			// Revert to previous identity
			if (_previousIdentity != null)
			{
				Thread.CurrentPrincipal = new WindowsPrincipal(_previousIdentity);
			}

			// Dispose of the identity
			_identity?.Dispose();
			_previousIdentity?.Dispose();
		}
	}

	// Method to impersonate current user
	public static IDisposable ImpersonateCurrentUser()
	{
		return new ImpersonationContext(WindowsIdentity.GetCurrent());
	}
}
