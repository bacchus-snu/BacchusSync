using pGina.Plugin.BacchusSync.FileAbstractions;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using pGina.Plugin.BacchusSync.FileAbstractions.Extra;
using System.IO.Compression;
using System.Text;
using System.Security.Principal;
using pGina.Plugin.BacchusSync.Extra;

namespace pGina.Plugin.BacchusSync
{
    internal class SftpSynchronizer : IDisposable
    {
        private delegate void SyncMethod<T>(T source, T destination) where T : AbstractFile;

        private const string SYNC_INFORMATION_DIRECTORY = ".sync";

        private readonly RemoteContext remote;
        private readonly string serverBaseDirectory;

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

            serverBaseDirectory = Settings.ServerBaseDirectory;
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

        internal void Migrate(string targetProfilePath)
        {
            try
            {
                string username = Path.GetFileNameWithoutExtension(targetProfilePath);
                string[] uploadExclusionList = CreateUploadExclusionList(targetProfilePath);
                var account = new NTAccount("CSE", username);
                var localProfile = new LocalDirectory(targetProfilePath, uploadExclusionList, username);
                var remoteProfile = new RemoteDirectory(remote, string.Format("{0}/{1}", serverBaseDirectory, username), "INVALID-SID", "INVALID-SID");
                
                SyncDirectory(localProfile, remoteProfile);
                SaveSyncInformation(account, SyncInformation.SyncStatus.LoggedOut);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Migrating for profile {1} failed.{0}{2}:{3}{0}{4}", Environment.NewLine, targetProfilePath, e.GetType().Name, e.ToString(), e.StackTrace);
                using (StreamWriter failList = new StreamWriter(File.Open("FailedProfiles.txt", FileMode.Append, FileAccess.Write, FileShare.None), Encoding.Unicode))
                {
                    failList.WriteLine(targetProfilePath);
                }
            }
        }

        private bool IsInUploadExclusionList(string path, string[] uploadExclusionList)
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

        internal void SaveSyncInformation(NTAccount user, SyncInformation.SyncStatus status)
        {
            string syncInformationDirectoryPath = string.Format("{0}/{1}", serverBaseDirectory, SYNC_INFORMATION_DIRECTORY);
            string syncInformationPath = string.Format("{0}/{1}/{2}", serverBaseDirectory, SYNC_INFORMATION_DIRECTORY, user.GetUsername());

            if (!remote.sftp.Exists(syncInformationDirectoryPath))
            {
                remote.sftp.CreateDirectory(syncInformationDirectoryPath);
                remote.sftp.ChangePermissions(syncInformationDirectoryPath, 01777);
            }

            using (var stream = remote.sftp.OpenWrite(syncInformationPath))
            {
                var syncInformation = new SyncInformation(status, Environment.MachineName, user.Translate(typeof(SecurityIdentifier)).Value);
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
