using System.Collections.Generic;
using System.IO;

namespace pGina.Plugin.BacchusSync.FileAbstractions
{
    internal class LocalDirectory : AbstractDirectory
    {
        private readonly string path;

        internal LocalDirectory(string path)
        {
            this.path = path;
        }

        internal override string Name => Path.GetFileName(path);

        internal override bool Exists => Directory.Exists(path);


        internal override void Create()
        {
            Directory.CreateDirectory(path);
        }

        internal override SortedSet<AbstractDirectory> GetDirectories()
        {
            var directories = new SortedSet<AbstractDirectory>();

            foreach (string directoryPath in Directory.GetDirectories(path))
            {
                var directory = new LocalDirectory(directoryPath);
                directories.Add(directory);
            }

            return directories;
        }

        internal override AbstractFile GetFile(string fileName)
        {
            string targetPath = string.Format("{0}{1}{2}", path, Path.PathSeparator, fileName);
            return new LocalRegularFile(targetPath);
        }

        internal override SortedSet<AbstractRegularFile> GetRegularFiles()
        {
            var regularFiles = new SortedSet<AbstractRegularFile>();

            foreach (string filePath in Directory.GetFiles(path))
            {
                var file = new LocalRegularFile(filePath);
                regularFiles.Add(file);
            }

            return regularFiles;
        }

        internal override AbstractDirectory GetSubDirectory(string directoryName)
        {
            string targetPath = string.Format("{0}{1}{2}", path, Path.PathSeparator, directoryName);
            return new LocalDirectory(targetPath);
        }

        internal override void Remove()
        {
            Directory.Delete(path, true);
        }
    }
}
