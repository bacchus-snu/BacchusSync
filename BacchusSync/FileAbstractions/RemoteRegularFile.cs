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
        private readonly string path;

        internal RemoteRegularFile(SftpClient client, string path)
        {
            if (path.Contains('\\'))
            {
                throw new InvalidCharacterException("Remote path contains reverse slash.");
            }
            this.client = client;
            this.path = path;
        }

        internal override DateTime LastWriteTime => client.GetLastWriteTime(path);

        internal override string Name => path.Split('/').Last();

        internal override bool Exists => client.Exists(path);

        internal override void Create()
        {
            client.Create(path);
        }

        internal override Stream OpenRead()
        {
            return client.OpenRead(path);
        }

        internal override Stream OpenWrite()
        {
            return client.OpenWrite(path);
        }

        internal override void Remove()
        {
            client.Delete(path);
        }
    }
}
