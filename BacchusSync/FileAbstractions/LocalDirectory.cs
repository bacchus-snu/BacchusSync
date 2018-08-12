using pGina.Plugin.BacchusSync.FileAbstractions.Exceptions;
using System;
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
            try
            {
                var directories = new SortedSet<AbstractDirectory>();

                foreach (string directoryPath in Directory.GetDirectories(Path))
                {
                    if (exclusionList == null || !exclusionList.Contains(directoryPath))
                    {
                        if (!(new DirectoryInfo(directoryPath)).IsReparsePoint())
                        {
                            var directory = new LocalDirectory(directoryPath, exclusionList);
                            directories.Add(directory);
                        }
                    }
                }

                return directories;
            }
            catch (UnauthorizedAccessException e)
            {
                throw new AccessDeniedException(Path, e);
            }
        }

        internal override AbstractFile GetFile(string fileName)
        {
            string targetPath = System.IO.Path.Combine(Path, fileName);
            return new LocalRegularFile(targetPath);
        }

        internal override SortedSet<AbstractRegularFile> GetRegularFiles()
        {
            try
            {
                var regularFiles = new SortedSet<AbstractRegularFile>();

                foreach (string filePath in Directory.GetFiles(Path))
                {
                    if (!(new FileInfo(filePath)).IsReparsePoint())
                    {
                        var file = new LocalRegularFile(filePath);
                        regularFiles.Add(file);
                    }
                }

                return regularFiles;
            }
            catch (UnauthorizedAccessException e)
            {
                throw new AccessDeniedException(Path, e);
            }
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
