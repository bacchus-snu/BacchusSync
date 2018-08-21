using pGina.Plugin.BacchusSync.Extra;
using pGina.Plugin.BacchusSync.FileAbstractions.Exceptions;
using pGina.Plugin.BacchusSync.FileAbstractions.Extra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace pGina.Plugin.BacchusSync.FileAbstractions
{
    internal class LocalDirectory : AbstractDirectory
    {
        private readonly string[] exclusionList;
        private readonly string owner;

        internal LocalDirectory(string path, string[] exclusionList, string owner)
        {
            Path = path;
            this.exclusionList = exclusionList;
            this.owner = owner;
        }

        internal override string Name => System.IO.Path.GetFileName(Path);

        internal override bool Exists => Directory.Exists(Path);

        internal override DateTime LastAccessTime
        {
            get => Directory.GetLastAccessTime(Path);
            set => Directory.SetLastAccessTime(Path, value);
        }

        internal override DateTime LastWriteTime
        {
            get => Directory.GetLastWriteTime(Path);
            set => Directory.SetLastWriteTime(Path, value);
        }

        internal override FileAttributes WindowsAttributes
        {
            get => File.GetAttributes(Path);
            set => File.SetAttributes(Path, value);
        }

        internal override void Create()
        {
            Directory.CreateDirectory(Path);
            ApiUtils.SetOwner(Path, owner);
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
                            var directory = new LocalDirectory(directoryPath, exclusionList, owner);
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
            return new LocalRegularFile(targetPath, owner);
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
                        var file = new LocalRegularFile(filePath, owner);
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
            return new LocalDirectory(targetPath, exclusionList, owner);
        }

        internal override void Remove()
        {
            Log.DebugFormat("Removing {0} recursively", Path);
            Directory.Delete(Path, true);
        }
    }
}
