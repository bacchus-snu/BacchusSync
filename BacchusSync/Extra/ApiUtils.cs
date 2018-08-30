using pGina.Plugin.BacchusSync.Exceptions;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace pGina.Plugin.BacchusSync.Extra
{
    internal static class ApiUtils
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

        private enum TokenInformationClass : int
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            MaxTokenInfoClass
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct SidAndAttributes
        {
            internal IntPtr Sid;
            internal int Attributes;
        }

        private struct TokenUser
        {
            internal SidAndAttributes User;
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

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSQueryUserToken(int sessionId, out IntPtr token);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetTokenInformation(IntPtr tokenHandle, TokenInformationClass tokenInformationClass, IntPtr tokenInformation, int tokenInformationLength, out int returnLength);
        #endregion

        private static readonly object privilegeLock = new object();
        private static bool hasRestorePrivilege = false;
        private static bool hasBackupPrivilege = false;

        /// <summary>
        /// returns GetLastWin32Error as string
        /// </summary>
        /// <returns></returns>
        public static string LastError()
        {
            return new Win32Exception(Marshal.GetLastWin32Error()).Message;
        }

        internal static void SetOwner(string path, string username)
        {
            GetSeRestorePrivilege();

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

        internal static void GetSeRestorePrivilege()
        {
            lock (privilegeLock)
            {
                if (hasRestorePrivilege)
                {
                    return;
                }

                GetPrivilege(TokenAccessRights.SE_RESTORE_NAME);
                hasRestorePrivilege = true;
            }
        }

        internal static void GetSeBackupPrivilege()
        {
            lock (privilegeLock)
            {
                if (hasBackupPrivilege)
                {
                    return;
                }

                GetPrivilege(TokenAccessRights.SE_BACKUP_NAME);
                hasBackupPrivilege = true;
            }
        }

        private static void GetPrivilege(string privilegeName)
        {
            IntPtr processToken = IntPtr.Zero;

            try
            {
                if (!OpenProcessToken(GetCurrentProcess(), TokenAccessRights.TOKEN_ADJUST_PRIVILEGES | TokenAccessRights.TOKEN_QUERY, out processToken))
                {
                    throw new ApiException("Open process token failed: " + LastError());
                }

                if (!LookupPrivilegeValue(null, privilegeName, out Luid luid))
                {
                    throw new ApiException("LookupPrivilegeValue failed: " + LastError());
                }

                TokenPrivileges privilege = new TokenPrivileges
                {
                    Attr = TokenAccessRights.SE_PRIVILEGE_ENABLED,
                    Luid = luid,
                    Count = 1,
                };

                if (!AdjustTokenPrivileges(processToken, false, ref privilege, 0, IntPtr.Zero, IntPtr.Zero))
                {
                    throw new ApiException("AdjustTokenPrivilege failed: " + LastError());
                }
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

        internal static void ResetUserRegistryPermission(string username, string profilePath)
        {
            GetSeRestorePrivilege();

            const int maxRetry = 3;
            string registryFilePath = Path.Combine(profilePath, "NTUSER.DAT");

            if (!Abstractions.WindowsApi.pInvokes.RegistryLoad(Abstractions.WindowsApi.pInvokes.structenums.RegistryLocation.HKEY_USERS, username, registryFilePath))
            {
                throw new ApiException("Registry hive loading failed.");
            }
            else if (!Abstractions.Windows.Security.RegSec(Abstractions.WindowsApi.pInvokes.structenums.RegistryLocation.HKEY_USERS, username, username))
            {
                throw new ApiException("Registry permission setting failed.");
            }

            int tryCount = 0;
            while (true)
            {
                if (Abstractions.WindowsApi.pInvokes.RegistryUnLoad(Abstractions.WindowsApi.pInvokes.structenums.RegistryLocation.HKEY_USERS, username))
                {
                    break;
                }
                else
                {
                    tryCount++;
                    if (tryCount >= maxRetry)
                    {
                        throw new ApiException("Registry hive unloading failed.");
                    }
                    else
                    {
                        Thread.Sleep(3000);
                    }
                }
            }
        }

        internal static NTAccount GetUserFromSession(int sessionId)
        {
            IntPtr userToken = IntPtr.Zero;
            IntPtr information = IntPtr.Zero;

            try
            {
                if (!WTSQueryUserToken(sessionId, out userToken))
                {
                    throw new ApiException("WTSQueryUserToken failed: " + LastError());
                }
                GetTokenInformation(userToken, TokenInformationClass.TokenUser, IntPtr.Zero, 0, out int memoryRequired);

                if (Marshal.GetLastWin32Error() != 122 /*ERROR_INSUFFICIENT_BUFFER*/)
                {
                    throw new ApiException("GetTokenInformation failed: " + LastError());
                }

                information = Marshal.AllocHGlobal(memoryRequired);
                if (!GetTokenInformation(userToken, TokenInformationClass.TokenUser, information, memoryRequired, out memoryRequired))
                {
                    throw new ApiException("GetTokenInformation failed: " + LastError());
                }

                TokenUser tokenUser = (TokenUser)Marshal.PtrToStructure(information, typeof(TokenUser));

                return (NTAccount) new SecurityIdentifier(tokenUser.User.Sid).Translate(typeof(NTAccount));
            }
            finally
            {
                if (information != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(information);
                }
                if (userToken != IntPtr.Zero)
                {
                    if (!CloseHandle(userToken))
                    {
                        Log.Warn("Cannot close process token.");
                    }
                }
            }
        }

        internal static string GetUsername(this NTAccount account)
        {
            string fullName = account.Value;
            if (fullName.Contains("\\"))
            {
                return fullName.Split('\\').Last();
            }
            else
            {
                return fullName;
            }
        }
    }
}
