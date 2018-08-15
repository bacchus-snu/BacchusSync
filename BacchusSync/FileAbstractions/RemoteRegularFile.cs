using pGina.Plugin.BacchusSync.FileAbstractions.Extra;
using pGina.Plugin.BacchusSync.FileAbstractions.Exceptions;
using System;
using System.IO;
using System.Linq;

namespace pGina.Plugin.BacchusSync.FileAbstractions
{
    internal class RemoteRegularFile : AbstractRegularFile
    {
        private readonly RemoteContext remote;

        internal RemoteRegularFile(RemoteContext remote, string path)
        {
            if (path.Contains('\\'))
            {
                throw new InvalidCharacterException("Remote path contains reverse slash.");
            }
            this.remote = remote;
            Path = path;
        }

        internal override DateTime LastAccessTime
        {
            get => Utils.RemoteGetTime(remote.ssh, Path, true);
            set => Utils.RemoteSetTime(remote.ssh, Path, true, value);
        }

        internal override DateTime LastWriteTime
        {
            get => Utils.RemoteGetTime(remote.ssh, Path, false);
            set => Utils.RemoteSetTime(remote.ssh, Path, false, value);
        }

        internal override FileAttributes WindowsAttributes
        {
            get => Utils.GetRemoteWindowsAttributes(remote.ssh, Path);
            set => Utils.SetRemoteWindowsAttributes(remote.ssh, Path, value);
        }

        internal override string Name => Path.Split('/').Last();

        internal override bool Exists => remote.sftp.Exists(Path);

        internal override void Create()
        {
            remote.sftp.Create(Path);
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
            var command = remote.ssh.RunCommand(string.Format("truncate -c -s 0 \"{0}\"", Path));
            if (command.ExitStatus != 0)
            {
                throw new RemoteCommandException(string.Format("truncate failed with exit code {0} while processing {1}", command.ExitStatus, Path));
            }
        }
    }
}
