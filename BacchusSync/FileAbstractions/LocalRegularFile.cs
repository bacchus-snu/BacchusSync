using pGina.Plugin.BacchusSync.FileAbstractions.Exceptions;
using System;
using System.IO;

namespace pGina.Plugin.BacchusSync.FileAbstractions
{
    internal class LocalRegularFile : AbstractRegularFile
    {
        internal bool IsReadOnly
        {
            get => File.GetAttributes(Path).HasFlag(FileAttributes.ReadOnly);
            set
            {
                var attributes = File.GetAttributes(Path);
                if (value)
                {
                    attributes = attributes | FileAttributes.ReadOnly;
                }
                else
                {
                    attributes = attributes & (~FileAttributes.ReadOnly);
                }
                File.SetAttributes(Path, attributes);
            }
        }

        internal LocalRegularFile(string path)
        {
            Path = path;
        }

        internal override DateTime LastAccessTime
        {
            get => File.GetLastAccessTime(Path);
            set => File.SetLastAccessTime(Path, value);
        }

        internal override DateTime LastWriteTime
        {
            get => File.GetLastWriteTime(Path);
            set => File.SetLastWriteTime(Path, value);
        }

        internal override string Name => System.IO.Path.GetFileName(Path);

        internal override bool Exists => File.Exists(Path);

        internal override FileAttributes WindowsAttributes
        {
            get => File.GetAttributes(Path);
            set
            {
                File.SetAttributes(Path, value);
            }
        }

        internal override void Create()
        {
            try
            {
                File.Create(Path).Close();
            }
            catch (UnauthorizedAccessException e)
            {
                throw new AccessDeniedException(Path, e);
            }
        }

        internal override Stream OpenRead()
        {
            try
            {
                return File.OpenRead(Path);
            }
            catch (UnauthorizedAccessException e)
            {
                throw new AccessDeniedException(Path, e);
            }
        }

        internal override Stream OpenWrite()
        {
            try
            {
                return File.OpenWrite(Path);
            }
            catch (UnauthorizedAccessException e)
            {
                throw new AccessDeniedException(Path, e);
            }
        }

        internal override void Remove()
        {
            try
            {
                Log.DebugFormat("Removing {0}", Path);
                IsReadOnly = false;
                File.Delete(Path);
            }
            catch (UnauthorizedAccessException e)
            {
                throw new AccessDeniedException(Path, e);
            }
        }
    }
}
