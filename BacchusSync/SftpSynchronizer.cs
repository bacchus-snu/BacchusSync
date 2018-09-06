using pGina.Plugin.BacchusSync.FileAbstractions;
using pGina.Plugin.BacchusSync.Exceptions;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using pGina.Plugin.BacchusSync.FileAbstractions.Extra;
using System.IO.Compression;
using System.Text;
using System.Security.Principal;
using pGina.Plugin.BacchusSync.Extra;
using Microsoft.Win32;

namespace pGina.Plugin.BacchusSync
{
    internal class SftpSynchronizer : IDisposable
    {
        private delegate void SyncMethod<T>(T source, T destination) where T : AbstractFile;

        private const string SYNC_INFORMATION_DIRECTORY = ".sync";

        private readonly RemoteContext remote;
        private readonly string username;
        private readonly string userSid;
        private readonly string serverBaseDirectory;
        private readonly string[] uploadExclusionList;
        private readonly LocalDirectory localProfile;
        private readonly RemoteDirectory remoteProfile;

        internal SftpSynchronizer(string username, string password, SecurityIdentifier userSid)
        {
            ConnectionInfo connectionInfo = new ConnectionInfo(Settings.ServerAddress, Settings.ServerPort, username, new PasswordAuthenticationMethod(username, password));
            if (Settings.HostKey == string.Empty)
            {
                remote = new RemoteContext(connectionInfo);
            }
            else
            {
                byte[] expectedHostKey = Convert.FromBase64String(Settings.HostKey);
                remote = new RemoteContext(connectionInfo, expectedHostKey);
            }
            remote.Connect();

            this.username = username;
            this.userSid = userSid.Value;
            serverBaseDirectory = Settings.ServerBaseDirectory;

            SyncInformation syncInformation = GetSyncInformation();
            bool remoteProfileExist = syncInformation.Status != SyncInformation.SyncStatus.DoesNotExist;
            string localProfilePath = GetLocalProfilePath(username, password, userSid, remoteProfileExist);
            string remoteProfilePath = string.Format("{0}/{1}", serverBaseDirectory, username);

            uploadExclusionList = CreateUploadExclusionList(localProfilePath);
            localProfile = new LocalDirectory(localProfilePath, uploadExclusionList, username);
            remoteProfile = new RemoteDirectory(remote, remoteProfilePath, syncInformation.SidInLastHost, this.userSid);
        }

        internal static string GetLocalProfilePath(string username, string password, SecurityIdentifier sid, bool remoteProfileExist)
        {
            IntPtr hToken = Abstractions.WindowsApi.pInvokes.GetUserToken(username, null, password);
            try
            {
                if (hToken != IntPtr.Zero)
                {
                    string path = Abstractions.WindowsApi.pInvokes.GetUserProfilePath(hToken);
                    if (string.IsNullOrEmpty(path))
                    {
                        path = Abstractions.WindowsApi.pInvokes.GetUserProfileDir(hToken);
                    }
                    if (string.IsNullOrEmpty(path))
                    {
                        if (remoteProfileExist)
                        {
                            path = CreateUserProfile(username, sid);
                        }
                        else
                        {
                            path = Abstractions.WindowsApi.pInvokes.CreateUserProfileDir(hToken, username);
                        }
                    }
                    if (string.IsNullOrEmpty(path))
                    {
                        throw new CannotGetUserProfilePathException(username);
                    }

                    return path;
                }
                else
                {
                    throw new CannotGetUserProfilePathException(username);
                }
            }
            finally
            {
                Abstractions.WindowsApi.pInvokes.CloseHandle(hToken);
            }
        }

        /// <summary>
        /// Create user profile directory and set registry.
        /// This method does not change owner nor grant access to user.
        /// </summary>
        /// <param name="username">Name of user to create profile directory.</param>
        /// <param name="sid">SID of user.</param>
        /// <returns>Path to created profile directory.</returns>
        /// <exception cref="ProfileOperationException">If user already has profile directory, creating directory failed, or accessing registry failed.</exception>
        internal static string CreateUserProfile(string username, SecurityIdentifier sid)
        {
            if (ProfileExists(sid))
            {
                throw new ProfileOperationException(string.Format("Cannot create profile. Profile for sid {0} exists.", sid.Value));
            }

            string profilesDirectory = GetProfilesDirectory();
            string profilePath = null;

            if (!Directory.Exists(Path.Combine(profilesDirectory, username)))
            {
                profilePath = Path.Combine(profilesDirectory, username);
            }
            else
            {
                for (int i = 0; i < 1000; i++)
                {
                    string pathToTest = Path.Combine(profilesDirectory, string.Format("{0}.{1:000}", username, i));
                    if (!Directory.Exists(pathToTest))
                    {
                        profilePath = pathToTest;
                        break;
                    }
                }

                if (profilePath == null)
                {
                    throw new ProfileOperationException("Cannot create profile. Reached maximum profile name trial.");
                }
            }

            Directory.CreateDirectory(profilePath);

            using (RegistryKey registry = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\" + sid.Value))
            {
                registry.SetValue("Flags", 0x0, RegistryValueKind.DWord);
                registry.SetValue("FullProfile", 0x1, RegistryValueKind.DWord);
                registry.SetValue("ProfileAttemptedProfileDownloadTimeHigh", 0x0, RegistryValueKind.DWord);
                registry.SetValue("ProfileAttemptedProfileDownloadTimeLow", 0x0, RegistryValueKind.DWord);
                registry.SetValue("ProfileImagePath", profilePath, RegistryValueKind.ExpandString);
                registry.SetValue("ProfileLoadTimeHigh", 0x0, RegistryValueKind.DWord);
                registry.SetValue("ProfileLoadTimeLow", 0x0, RegistryValueKind.DWord);
                registry.SetValue("RunLogonScriptSync", 0x0, RegistryValueKind.DWord);

                byte[] binarySid = new byte[sid.BinaryLength];
                sid.GetBinaryForm(binarySid, 0);
                registry.SetValue("Sid", binarySid, RegistryValueKind.Binary);

                registry.SetValue("State", 0x0, RegistryValueKind.DWord);
            }

            return profilePath;
        }

        internal static bool ProfileExists(SecurityIdentifier sid)
        {
            using (RegistryKey registry = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\" + sid.Value))
            {
                return registry?.GetValue("ProfileImagePath", null) != null;
            }
        }

        internal static string GetProfilesDirectory()
        {
            using (RegistryKey registry = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList"))
            {
                string profilesDirectory = (string)registry.GetValue("ProfilesDirectory");
                if (profilesDirectory == null)
                {
                    throw new ProfileOperationException("Cannot get profiles directory from registry.");
                }
                else
                {
                    return Environment.ExpandEnvironmentVariables(profilesDirectory);
                }
            }
        }

        internal static string[] CreateUploadExclusionList(string localProfilePath)
        {
            return new string[]
            {
                Path.Combine(localProfilePath, "AppData", "Local"),
                Path.Combine(localProfilePath, "AppData", "LocalLow"),
                Path.Combine(localProfilePath, "Searches"),
            };
        }

        internal void UploadProfile()
        {
            var syncInformation = GetSyncInformation();

            ApiUtils.GetSeBackupPrivilege();
            ApiUtils.GetSeRestorePrivilege();

            if (syncInformation.Status == SyncInformation.SyncStatus.LoggedOn && syncInformation.LastHost == Environment.MachineName)
            {
                SaveSyncInformation(SyncInformation.SyncStatus.Uploading);
                SyncDirectory(localProfile, remoteProfile);
                SaveSyncInformation(SyncInformation.SyncStatus.LoggedOut);
            }
            else
            {
                Log.Warn("Uploading canceled. User was not successfully logged on to this computer.");
            }
        }

        internal void DownloadProfile()
        {
            var syncInformation = GetSyncInformation();

            ApiUtils.GetSeBackupPrivilege();
            ApiUtils.GetSeRestorePrivilege();

            switch (syncInformation.Status)
            {
                case SyncInformation.SyncStatus.DoesNotExist:
                    SaveSyncInformation(SyncInformation.SyncStatus.LoggedOn);
                    break;
                case SyncInformation.SyncStatus.LoggedOn:
                case SyncInformation.SyncStatus.Uploading:
                    if (syncInformation.LastHost == Environment.MachineName && ProfileExists(new SecurityIdentifier(syncInformation.SidInLastHost)))
                    {
                        SaveSyncInformation(SyncInformation.SyncStatus.LoggedOn);
                        break;
                    }
                    else
                    {
                        throw new UserNotLoggedOutException(syncInformation.LastHost);
                    }
                case SyncInformation.SyncStatus.LoggedOut:
                    SyncDirectory(remoteProfile, localProfile);
                    ApiUtils.SetOwner(localProfile.Path, username);
                    ApiUtils.ResetUserRegistryPermission(username, localProfile.Path);
                    SaveSyncInformation(SyncInformation.SyncStatus.LoggedOn);
                    break;
                default:
                    throw new Exception("Unhandled status : " + syncInformation.Status.ToString());
            }
        }

        private bool IsInUploadExclusionList(string path)
        {
            foreach (string excludedDirectory in uploadExclusionList)
            {
                if (path.StartsWith(excludedDirectory))
                {
                    return true;
                }
            }

            return false;
        }

        internal SyncInformation GetSyncInformation()
        {
            string syncInformationDirectoryPath = string.Format("{0}/{1}", serverBaseDirectory, SYNC_INFORMATION_DIRECTORY);
            string syncInformationPath = string.Format("{0}/{1}/{2}", serverBaseDirectory, SYNC_INFORMATION_DIRECTORY, username);

            if (!remote.sftp.Exists(syncInformationDirectoryPath))
            {
                remote.sftp.CreateDirectory(syncInformationDirectoryPath);
                remote.sftp.ChangePermissions(syncInformationDirectoryPath, 01777);
            }

            if (remote.sftp.Exists(string.Format("{0}/{1}/{2}", serverBaseDirectory, SYNC_INFORMATION_DIRECTORY, username)))
            {
                using (var stream = remote.sftp.OpenRead(syncInformationPath))
                {
                    return new SyncInformation(stream);
                }
            }
            else
            {
                return new SyncInformation(SyncInformation.SyncStatus.DoesNotExist, string.Empty, string.Empty);
            }
        }

        internal void SaveSyncInformation(SyncInformation.SyncStatus status)
        {
            string syncInformationDirectoryPath = string.Format("{0}/{1}", serverBaseDirectory, SYNC_INFORMATION_DIRECTORY);
            string syncInformationPath = string.Format("{0}/{1}/{2}", serverBaseDirectory, SYNC_INFORMATION_DIRECTORY, username);

            if (!remote.sftp.Exists(syncInformationDirectoryPath))
            {
                remote.sftp.CreateDirectory(syncInformationDirectoryPath);
                remote.sftp.ChangePermissions(syncInformationDirectoryPath, 01777);
            }

            using (var stream = remote.sftp.OpenWrite(syncInformationPath))
            {
                var syncInformation = new SyncInformation(status, Environment.MachineName, userSid);
                syncInformation.Save(stream);
            }
        }

        private void SyncDirectory(AbstractDirectory source, AbstractDirectory destination)
        {
            if (destination.Exists)
            {
                var directoriesInSource = source.GetDirectories().GetEnumerator();
                var directoriesInDestination = destination.GetDirectories().GetEnumerator();

                Sync(directoriesInSource, directoriesInDestination, destination, SyncDirectory);

                var regularFilesInSource = source.GetRegularFiles().GetEnumerator();
                var regularFilesInDestination = destination.GetRegularFiles().GetEnumerator();

                Sync(regularFilesInSource, regularFilesInDestination, destination, SyncRegularFile);
                destination.SetAllAttributes(source);
            }
            else
            {
                source.Copy(destination);
            }
        }

        private void Sync<T>(IEnumerator<T> filesInSource, IEnumerator<T> filesInDestination, AbstractDirectory destination, SyncMethod<T> sync) where T : AbstractFile
        {
            bool moveSourceNext = true;
            bool moveDestinationNext = true;

            while (true)
            {
                if (moveSourceNext && !filesInSource.MoveNext())
                {
                    // Source reached end first. Remove remaining files in destination.
                    while (filesInDestination.MoveNext())
                    {
                        filesInDestination.Current.Remove();
                    }
                    break;
                }
                else if (moveDestinationNext && !filesInDestination.MoveNext())
                {
                    // Destination reached end first. Copy remaining files in source to destination.
                    // Note that filesInSource already did MoveNext().
                    do
                    {
                        filesInSource.Current.CopyTo(destination);
                    }
                    while (filesInSource.MoveNext());
                    break;
                }
                else
                {
                    T sourceCurrent = filesInSource.Current;
                    T destinationCurrent = filesInDestination.Current;

                    if (string.Compare(sourceCurrent.Name, destinationCurrent.Name) < 0)
                    {
                        sourceCurrent.CopyTo(destination);
                        moveSourceNext = true;
                        moveDestinationNext = false;
                    }
                    else if (string.Compare(sourceCurrent.Name, destinationCurrent.Name) > 0)
                    {
                        destinationCurrent.Remove();
                        moveSourceNext = false;
                        moveDestinationNext = true;
                    }
                    else
                    {
                        sync(sourceCurrent, destinationCurrent);
                        moveSourceNext = true;
                        moveDestinationNext = true;
                    }
                }
            }
        }

        private void SyncRegularFile(AbstractRegularFile source, AbstractRegularFile destination)
        {
            if (!destination.Exists || source.LastWriteTime > destination.LastWriteTime)
            {
                source.Copy(destination);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    remote.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
