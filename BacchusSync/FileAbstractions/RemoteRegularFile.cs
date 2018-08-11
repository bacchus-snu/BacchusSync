using pGina.Plugin.BacchusSync.FileAbstractions.Exceptions;
using Renci.SshNet;
using System;
using System.IO;
using System.Linq;

namespace pGina.Plugin.BacchusSync.FileAbstractions
{
    internal class RemoteRegularFile : AbstractRegularFile
    {
        private readonly SftpClient client;

        internal RemoteRegularFile(SftpClient client, string path)
        {
            if (path.Contains('\\'))
            {
                throw new InvalidCharacterException("Remote path contains reverse slash.");
            }
            this.client = client;
            Path = path;
        }

        internal override DateTime LastWriteTime => client.GetLastWriteTime(Path);

        internal override string Name => Path.Split('/').Last();

        internal override bool Exists => client.Exists(Path);

        internal override void Create()
        {
            client.Create(Path);
        }

        internal override Stream OpenRead()
        {
            return client.OpenRead(Path);
        }

        internal override Stream OpenWrite()
        {
            return client.OpenWrite(Path);
        }

        internal override void Remove()
        {
            client.Delete(Path);
        }
    }
}
