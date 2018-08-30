using pGina.Plugin.BacchusSync.Extra;
using pGina.Plugin.BacchusSync.FileAbstractions.Exceptions;
using pGina.Plugin.BacchusSync.FileAbstractions.Streams;
using System;
using System.IO;
using System.Security.AccessControl;

namespace pGina.Plugin.BacchusSync.FileAbstractions
{
    internal class LocalRegularFile : AbstractRegularFile
    {
        private readonly string owner;

        internal LocalRegularFile(string path, string owner)
        {
            Path = path;
            this.owner = owner;
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

        internal override FileAttributes WindowsAttributes
        {
            get => File.GetAttributes(Path);
            set
            {
                File.SetAttributes(Path, value);
            }
        }

        internal override string Name => System.IO.Path.GetFileName(Path);

        internal override bool Exists => File.Exists(Path);

        internal override bool IsReadOnly
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

        internal override FileSystemSecurity WindowsAccessControlList
        {
            get => File.GetAccessControl(Path);
            set => File.SetAccessControl(Path, value as FileSecurity);
        }

        internal override void Create()
        {
            try
            {
                File.Create(Path).Close();
                ApiUtils.SetOwner(Path, owner);
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
                return new PrivilegedFileStream(Path, FileAccess.Read, FileShare.Read, FileMode.Open);
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
                return new PrivilegedFileStream(Path, FileAccess.Write, FileShare.None, FileMode.OpenOrCreate);
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

        internal override void Truncate()
        {
            new PrivilegedFileStream(Path, FileAccess.Write, FileShare.None, FileMode.Truncate).Close();
        }
    }
}
