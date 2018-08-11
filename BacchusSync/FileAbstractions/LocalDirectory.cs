using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace pGina.Plugin.BacchusSync.FileAbstractions
{
    internal class LocalDirectory : AbstractDirectory
    {
        private readonly string[] exclusionList;

        internal LocalDirectory(string path, string[] exclusionList)
        {
            Path = path;
            this.exclusionList = exclusionList;
        }

        internal override string Name => System.IO.Path.GetFileName(Path);

        internal override bool Exists => Directory.Exists(Path);


        internal override void Create()
        {
            Directory.CreateDirectory(Path);
        }

        internal override SortedSet<AbstractDirectory> GetDirectories()
        {
            var directories = new SortedSet<AbstractDirectory>();

            foreach (string directoryPath in Directory.GetDirectories(Path))
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
            string targetPath = System.IO.Path.Combine(Path, fileName);
            return new LocalRegularFile(targetPath);
        }

        internal override SortedSet<AbstractRegularFile> GetRegularFiles()
        {
            var regularFiles = new SortedSet<AbstractRegularFile>();

            foreach (string filePath in Directory.GetFiles(Path))
            {
                var file = new LocalRegularFile(filePath);
                regularFiles.Add(file);
            }

            return regularFiles;
        }

        internal override AbstractDirectory GetSubDirectory(string directoryName)
        {
            string targetPath = System.IO.Path.Combine(Path, directoryName);
            return new LocalDirectory(targetPath, exclusionList);
        }

        internal override void Remove()
        {
            Directory.Delete(Path, true);
        }
    }
}
