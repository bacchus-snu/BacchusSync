using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace pGina.Plugin.BacchusSync.FileAbstractions
{
    internal class LocalDirectory : AbstractDirectory
    {
        private readonly string path;
        private readonly string[] exclusionList;

        internal LocalDirectory(string path, string[] exclusionList)
        {
            this.path = path;
            this.exclusionList = exclusionList;
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
                if (exclusionList == null || !exclusionList.Contains(directoryPath))
                {
                    var directory = new LocalDirectory(directoryPath, exclusionList);
                    directories.Add(directory);
                }
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
            return new LocalDirectory(targetPath, exclusionList);
        }

        internal override void Remove()
        {
            Directory.Delete(path, true);
        }
    }
}
