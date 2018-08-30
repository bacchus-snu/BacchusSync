using pGina.Plugin.BacchusSync.FileAbstractions.Extra;
using pGina.Plugin.BacchusSync.FileAbstractions.Exceptions;
using System;
using System.IO;
using System.Linq;
using System.Security.AccessControl;

namespace pGina.Plugin.BacchusSync.FileAbstractions
{
    internal class RemoteRegularFile : AbstractRegularFile
    {
        private readonly RemoteContext remote;
        private readonly string oldSid;
        private readonly string newSid;

        internal RemoteRegularFile(RemoteContext remote, string path, string oldSid, string newSid)
        {
            if (path.Contains('\\'))
            {
                throw new InvalidCharacterException("Remote path contains reverse slash.");
            }
            this.remote = remote;
            Path = path;
            this.oldSid = oldSid;
            this.newSid = newSid;
        }

        internal string EscapedPath => Path.Replace("'", "\\'");

        internal override DateTime LastAccessTime
        {
            get => Utils.RemoteGetTime(remote.ssh, EscapedPath, true);
            set => Utils.RemoteSetTime(remote.ssh, EscapedPath, true, value);
        }

        internal override DateTime LastWriteTime
        {
            get => Utils.RemoteGetTime(remote.ssh, EscapedPath, false);
            set => Utils.RemoteSetTime(remote.ssh, EscapedPath, false, value);
        }

        internal override FileAttributes WindowsAttributes
        {
            get => Utils.GetRemoteWindowsAttributes(remote.ssh, EscapedPath);
            set => Utils.SetRemoteWindowsAttributes(remote.ssh, EscapedPath, value);
        }

        internal override FileSystemSecurity WindowsAccessControlList
        {
            get => Utils.GetRemoteWindowsAccessControlList<FileSecurity>(remote.ssh, EscapedPath, oldSid, newSid);
            set => Utils.SetRemoteWindowsAccessControlList(remote.ssh, EscapedPath, value);
        }

        internal override string Name => Path.Split('/').Last();

        internal override bool Exists => remote.sftp.Exists(Path);

        internal override bool IsReadOnly
        {
            get => !remote.sftp.GetAttributes(Path).OwnerCanWrite;
            set => remote.sftp.GetAttributes(Path).OwnerCanWrite = !value;
        }

        internal override void Create()
        {
            remote.sftp.Create(Path).Close();
            var attributes = remote.sftp.GetAttributes(Path);
            attributes.SetPermissions(0600);
            remote.sftp.SetAttributes(Path, attributes);
        }

        internal override Stream OpenRead()
        {
            return remote.sftp.OpenRead(Path);
        }

        internal override Stream OpenWrite()
        {
            return remote.sftp.OpenWrite(Path);
        }

        internal override void Remove()
        {
            Log.DebugFormat("Removing {0}", Path);
            remote.sftp.Delete(Path);
        }

        internal override void Truncate()
        {
            var command = remote.ssh.RunCommand(string.Format("truncate -c -s 0 $'{0}'", EscapedPath));
            if (command.ExitStatus != 0)
            {
                throw new RemoteCommandException(string.Format("truncate failed with exit code {0} while processing {1}", command.ExitStatus, Path));
            }
        }
    }
}
