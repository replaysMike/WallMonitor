#if OS_WINDOWS
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace WallMonitor.Monitors.Windows
{
    /// <summary>
    /// Impersonate a windows user
    /// </summary>
    public class Impersonator
    {
        public const int Logon32LogonInteractive = 2;
        public const int Logon32ProviderDefault = 0;

        public Impersonator(Action method, string username, string password, string? domain = null)
        {
            if (string.IsNullOrEmpty(username))
                username = Environment.UserName;
            if (string.IsNullOrEmpty(domain))
                domain = Environment.UserDomainName;
            var returnValue = LogonUser(username, domain, password, Logon32LogonInteractive, Logon32ProviderDefault, out var safeAccessTokenHandle);
            if (!returnValue)
                throw new ApplicationException("Could not impersonate user");

            WindowsIdentity.RunImpersonated(safeAccessTokenHandle, method);
        }

        #region Interop imports/constants
        
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]  
        public static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, out SafeAccessTokenHandle phToken);  

        #endregion
    }
}
#endif