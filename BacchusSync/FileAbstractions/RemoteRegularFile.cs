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

        internal override DateTime LastWriteTime => remote.sftp.GetLastWriteTime(Path);

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
    }
}
