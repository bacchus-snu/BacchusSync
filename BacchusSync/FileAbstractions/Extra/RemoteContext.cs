using pGina.Plugin.BacchusSync.Exceptions;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pGina.Plugin.BacchusSync.FileAbstractions.Extra
{
    internal class RemoteContext : IDisposable
    {
        internal readonly SshClient ssh;
        internal readonly SftpClient sftp;

        internal RemoteContext(ConnectionInfo connectionInfo)
        {
            ssh = new SshClient(connectionInfo);
            sftp = new SftpClient(connectionInfo);
        }

        internal RemoteContext(ConnectionInfo connectionInfo, byte[] expectedHostKey) : this(connectionInfo)
        {
            ssh.HostKeyReceived += (sender, e) => VerifyHostKey(sender, e, expectedHostKey);
            sftp.HostKeyReceived += (sender, e) => VerifyHostKey(sender, e, expectedHostKey);
        }

        internal void VerifyHostKey(object sender, HostKeyEventArgs e, byte[] expectedHostKey)
        {
            if (expectedHostKey.SequenceEqual(e.HostKey))
            {
                e.CanTrust = true;
            }
            else
            {
                e.CanTrust = false;
                throw new HostKeyMismatchException();
            }
        }

        internal void Connect()
        {
            ssh.Connect();
            sftp.Connect();
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ssh.Dispose();
                    sftp.Dispose();
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
