using pGina.Plugin.BacchusSync.Exceptions;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace pGina.Plugin.BacchusSync
{
    internal class SftpSynchronizer : IDisposable
    {
        private const string SYNC_INFORMATION_DIRECTORY = ".sync";

        private readonly SftpClient client;
        private readonly string username;
        private readonly string baseDirectory;

        internal SftpSynchronizer(string username, string password)
        {
            client = new SftpClient(Settings.ServerAddress, Settings.ServerPort, username, password);
            this.username = username;
            baseDirectory = Settings.ServerBaseDirectory;
        }

        internal void UploadProfile()
        {
            SaveSyncInformation(SyncInformation.SyncStatus.Uploading);
            // TODO : Upload profile
            SaveSyncInformation(SyncInformation.SyncStatus.LoggedOut);
        }

        internal void DownloadProfile()
        {
            SyncInformation syncInformation = GetSyncInformation();

            switch(syncInformation.Status)
            {
                case SyncInformation.SyncStatus.DoesNotExist:
                    return;
                case SyncInformation.SyncStatus.LoggedOn:
                case SyncInformation.SyncStatus.Uploading:
                    throw new UserNotLoggedOutException(syncInformation.LastHost);
                case SyncInformation.SyncStatus.LoggedOut:
                    // TODO : Download profile
                    break;
                default:
                    throw new Exception("Unhandled status : " + syncInformation.Status.ToString());
            }
        }

        internal SyncInformation GetSyncInformation()
        {
            string syncInformationDirectoryPath = string.Format("{0}/{1}", baseDirectory, SYNC_INFORMATION_DIRECTORY);
            string syncInformationPath = string.Format("{0}/{1}/{2}", baseDirectory, SYNC_INFORMATION_DIRECTORY, username);

            if (!client.Exists(syncInformationDirectoryPath))
            {
                client.CreateDirectory(syncInformationDirectoryPath);
                client.ChangePermissions(syncInformationDirectoryPath, 01777);
            }

            if (client.Exists(string.Format("{0}/{1}/{2}", baseDirectory, SYNC_INFORMATION_DIRECTORY, username)))
            {
                using (var stream = client.OpenRead(syncInformationPath))
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
            string syncInformationDirectoryPath = string.Format("{0}/{1}", baseDirectory, SYNC_INFORMATION_DIRECTORY);
            string syncInformationPath = string.Format("{0}/{1}/{2}", baseDirectory, SYNC_INFORMATION_DIRECTORY, username);

            if (!client.Exists(syncInformationDirectoryPath))
            {
                client.CreateDirectory(syncInformationDirectoryPath);
                client.ChangePermissions(syncInformationDirectoryPath, 01777);
            }

            using (var stream = client.OpenWrite(syncInformationPath))
            {
                var syncInformation = new SyncInformation(status, Environment.MachineName);
                syncInformation.Save(stream);
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
                    client.Dispose();
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
