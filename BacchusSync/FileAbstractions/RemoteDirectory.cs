using pGina.Plugin.BacchusSync.FileAbstractions.Exceptions;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System.Collections.Generic;
using System.Linq;

namespace pGina.Plugin.BacchusSync.FileAbstractions
{
    internal class RemoteDirectory : AbstractDirectory
    {
        private readonly SftpClient client;
        private readonly string path;

        internal RemoteDirectory(SftpClient client, string path)
        {
            if (path.Contains('\\'))
            {
                throw new InvalidCharacterException("Remote path contains reverse slash.");
            }
            this.client = client;
            this.path = path;
        }

        internal override string Name => path.Split('/').Last();

        internal override bool Exists => client.Exists(path);

        internal override void Create()
        {
            client.CreateDirectory(path);
        }

        internal override SortedSet<AbstractDirectory> GetDirectories()
        {
            var directories = new SortedSet<AbstractDirectory>();

           foreach (var file in client.ListDirectoryAlmostAll(path))
            {
                if (file.IsDirectory)
                {
                    var directory = new RemoteDirectory(client, file.FullName);
                    directories.Add(directory);
                }
            }

            return directories;
        }

        internal override AbstractFile GetFile(string fileName)
        {
            string targetPath = string.Format("{0}/{1}", path, fileName);
            var file = new RemoteRegularFile(client, targetPath);
            return file;
        }

        internal override SortedSet<AbstractRegularFile> GetRegularFiles()
        {
            var files = new SortedSet<AbstractRegularFile>();

            foreach (var file in client.ListDirectoryAlmostAll(path))
            {
                if (file.IsRegularFile)
                {
                    var regularFile = new RemoteRegularFile(client, file.FullName);
                    files.Add(regularFile);
                }
            }

            return files;
        }

        internal override AbstractDirectory GetSubDirectory(string directoryName)
        {
            string targetPath = string.Format("{0}/{1}", path, directoryName);
            var directory = new RemoteDirectory(client, targetPath);
            return directory;
        }

        internal override void Remove()
        {
            Remove(client.Get(path));
        }

        private void Remove(SftpFile sftpFile)
        {
            if (sftpFile.IsDirectory)
            {
                foreach (var file in client.ListDirectoryAlmostAll(sftpFile.FullName))
                {
                    Remove(file);
                }
            }
            sftpFile.Delete();
        }
    }
}
