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

        internal RemoteDirectory(SftpClient client, string path)
        {
            if (path.Contains('\\'))
            {
                throw new InvalidCharacterException("Remote path contains reverse slash.");
            }
            this.client = client;
            Path = path;
        }

        internal override string Name => Path.Split('/').Last();

        internal override bool Exists => client.Exists(Path);

        internal override void Create()
        {
            client.CreateDirectory(Path);
        }

        internal override SortedSet<AbstractDirectory> GetDirectories()
        {
            var directories = new SortedSet<AbstractDirectory>();

           foreach (var file in client.ListDirectoryAlmostAll(Path))
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
            string targetPath = string.Format("{0}/{1}", Path, fileName);
            var file = new RemoteRegularFile(client, targetPath);
            return file;
        }

        internal override SortedSet<AbstractRegularFile> GetRegularFiles()
        {
            var files = new SortedSet<AbstractRegularFile>();

            foreach (var file in client.ListDirectoryAlmostAll(Path))
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
            string targetPath = string.Format("{0}/{1}", Path, directoryName);
            var directory = new RemoteDirectory(client, targetPath);
            return directory;
        }

        internal override void Remove()
        {
            Remove(client.Get(Path));
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
