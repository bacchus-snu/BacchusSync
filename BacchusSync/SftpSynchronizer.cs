using pGina.Plugin.BacchusSync.FileAbstractions;
using pGina.Plugin.BacchusSync.Exceptions;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using pGina.Plugin.BacchusSync.FileAbstractions.Exceptions;
using pGina.Plugin.BacchusSync.FileAbstractions.Extra;

namespace pGina.Plugin.BacchusSync
{
    internal class SftpSynchronizer : IDisposable
    {
        private delegate void SyncMethod<T>(T source, T destination) where T : AbstractFile;

        private const string SYNC_INFORMATION_DIRECTORY = ".sync";

        private readonly RemoteContext remote;
        private readonly string username;
        private readonly string serverBaseDirectory;
        private readonly string[] uploadExclusionList;
        private readonly LocalDirectory localProfile;
        private readonly RemoteDirectory remoteProfile;

        internal SftpSynchronizer(string username, string password)
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
            serverBaseDirectory = Settings.ServerBaseDirectory;

            string localProfilePath = GetLocalProfilePath(username, password);
            string remoteProfilePath = string.Format("{0}/{1}", serverBaseDirectory, username);

            uploadExclusionList = CreateUploadExclusionList(localProfilePath);
            localProfile = new LocalDirectory(localProfilePath, uploadExclusionList);
            remoteProfile = new RemoteDirectory(remote, remoteProfilePath);
        }

        internal static string GetLocalProfilePath(string username, string password)
        {
            IntPtr hToken = Abstractions.WindowsApi.pInvokes.GetUserToken(username, null, password);
            if (hToken != IntPtr.Zero)
            {
                string path = Abstractions.WindowsApi.pInvokes.GetUserProfilePath(hToken);
                if (string.IsNullOrEmpty(path))
                {
                    path = Abstractions.WindowsApi.pInvokes.GetUserProfileDir(hToken);
                }
                Abstractions.WindowsApi.pInvokes.CloseHandle(hToken);

                return path;
            }
            else
            {
                throw new CannotGetUserProfilePathException(username);
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
            SyncInformation syncInformation = GetSyncInformation();

            switch(syncInformation.Status)
            {
                case SyncInformation.SyncStatus.DoesNotExist:
                    SaveSyncInformation(SyncInformation.SyncStatus.LoggedOn);
                    break;
                case SyncInformation.SyncStatus.LoggedOn:
                case SyncInformation.SyncStatus.Uploading:
                    throw new UserNotLoggedOutException(syncInformation.LastHost);
                case SyncInformation.SyncStatus.LoggedOut:
                    SyncDirectory(remoteProfile, localProfile);
                    SaveSyncInformation(SyncInformation.SyncStatus.LoggedOn);
                    break;
                default:
                    throw new Exception("Unhandled status : " + syncInformation.Status.ToString());
            }
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
                return new SyncInformation(SyncInformation.SyncStatus.DoesNotExist, string.Empty);
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
                var syncInformation = new SyncInformation(status, Environment.MachineName);
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
