using pGina.Plugin.BacchusSync.Exceptions;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace pGina.Plugin.BacchusSync.Extra
{
    internal static class Utils
    {
        #region Windows API
        private struct TokenAccessRights
        {
            internal const string SE_RESTORE_NAME = "SeRestorePrivilege";
            internal const string SE_BACKUP_NAME = "SeBackupPrivilege";
            internal const UInt32 SE_PRIVILEGE_ENABLED = 0x00000002;
            internal const UInt32 STANDARD_RIGHTS_REQUIRED = 0x000F0000;
            internal const UInt32 STANDARD_RIGHTS_READ = 0x00020000;
            internal const UInt32 TOKEN_ASSIGN_PRIMARY = 0x0001;
            internal const UInt32 TOKEN_DUPLICATE = 0x0002;
            internal const UInt32 TOKEN_IMPERSONATE = 0x0004;
            internal const UInt32 TOKEN_QUERY = 0x0008;
            internal const UInt32 TOKEN_QUERY_SOURCE = 0x0010;
            internal const UInt32 TOKEN_ADJUST_PRIVILEGES = 0x0020;
            internal const UInt32 TOKEN_ADJUST_GROUPS = 0x0040;
            internal const UInt32 TOKEN_ADJUST_DEFAULT = 0x0080;
            internal const UInt32 TOKEN_ADJUST_SESSIONID = 0x0100;
            internal const UInt32 TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
            internal const UInt32 TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE | TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Luid
        {
            internal uint LowPart;
            internal int HighPart;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TokenPrivileges
        {
            internal int Count;
            internal Luid Luid;
            internal UInt32 Attr;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(IntPtr processHandle, UInt32 desiredAccess, out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out Luid lpLuid);

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAllPrivileges, ref TokenPrivileges newState, int bufferLength, IntPtr previousState, IntPtr returnLength);
        #endregion
        private static object restoreNameLock = new object();
        private static bool hasRestoreNamePrivilege = false;

        /// <summary>
        /// returns GetLastWin32Error as string
        /// </summary>
        /// <returns></returns>
        public static string LastError()
        {
            return new Win32Exception(Marshal.GetLastWin32Error()).Message;
        }

        /// <summary>
        /// Set file to be accessd by only NT AUTHORITY\SYSTEM and Adminisrators group.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        internal static void RestrictUserAccessToFile(string path)
        {
            GetSeRestoreNamePrivilege();

            var accessControl = File.GetAccessControl(path);
            accessControl.SetOwner(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null));
            accessControl.SetAccessRuleProtection(true, false);
            var allowSystem = new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), FileSystemRights.FullControl, AccessControlType.Allow);
            var allowAdminGroup = new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null), FileSystemRights.FullControl, AccessControlType.Allow);
            accessControl.SetAccessRule(allowSystem);
            accessControl.SetAccessRule(allowAdminGroup);
            File.SetAccessControl(path, accessControl);
        }

        internal static void SetOwner(string path, string username)
        {
            GetSeRestoreNamePrivilege();

            if (Directory.Exists(path))
            {
                var info = new DirectoryInfo(path);
                var accessControl = info.GetAccessControl();
                accessControl.SetOwner(new NTAccount(username));
                info.SetAccessControl(accessControl);
            }
            else
            {
                var info = new FileInfo(path);
                var accessControl = info.GetAccessControl();
                accessControl.SetOwner(new NTAccount(username));
                info.SetAccessControl(accessControl);
            }
        }

        internal static void GetSeRestoreNamePrivilege()
        {
            lock (restoreNameLock)
            {
                if (hasRestoreNamePrivilege)
                {
                    return;
                }

                IntPtr processToken = IntPtr.Zero;

                try
                {
                    if (!OpenProcessToken(GetCurrentProcess(), TokenAccessRights.TOKEN_ADJUST_PRIVILEGES | TokenAccessRights.TOKEN_QUERY, out processToken))
                    {
                        throw new ApiException("Open process token failed: " + LastError());
                    }

                    if (!LookupPrivilegeValue(null, TokenAccessRights.SE_RESTORE_NAME, out Luid restoreLuid))
                    {
                        throw new ApiException("LookupPrivilegeValue failed: " + LastError());
                    }

                    TokenPrivileges restorePrivilege = new TokenPrivileges
                    {
                        Attr = TokenAccessRights.SE_PRIVILEGE_ENABLED,
                        Luid = restoreLuid,
                        Count = 1,
                    };

                    if (!AdjustTokenPrivileges(processToken, false, ref restorePrivilege, 0, IntPtr.Zero, IntPtr.Zero))
                    {
                        throw new ApiException("AdjustTokenPrivilege failed: " + LastError());
                    }

                    hasRestoreNamePrivilege = true;
                }
                finally
                {
                    if (processToken != IntPtr.Zero)
                    {
                        if (!CloseHandle(processToken))
                        {
                            Log.Warn("Cannot close process token.");
                        }
                    }
                }
            }
        }
    }
}
