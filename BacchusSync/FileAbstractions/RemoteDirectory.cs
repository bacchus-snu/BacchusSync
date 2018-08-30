using pGina.Plugin.BacchusSync.FileAbstractions.Extra;
using pGina.Plugin.BacchusSync.FileAbstractions.Exceptions;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Security.AccessControl;

namespace pGina.Plugin.BacchusSync.FileAbstractions
{
    internal class RemoteDirectory : AbstractDirectory
    {
        private readonly RemoteContext remote;
        private readonly string oldSid;
        private readonly string newSid;

        internal RemoteDirectory(RemoteContext remote, string path, string oldSid, string newSid)
        {
            if (path.Contains('\\'))
            {
                throw new InvalidCharacterException("Remote path contains reverse slash.");
            }
            this.remote = remote;
            Path = path;
            this.oldSid = oldSid;
            this.newSid = newSid;
        }

        internal string EscapedPath => Path.Replace("'", "\\'");

        internal override string Name => Path.Split('/').Last();

        internal override bool Exists => remote.sftp.Exists(Path);

        internal override DateTime LastAccessTime
        {
            get => Utils.RemoteGetTime(remote.ssh, EscapedPath, true);
            set => Utils.RemoteSetTime(remote.ssh, EscapedPath, true, value);
        }

        internal override DateTime LastWriteTime
        {
            get => Utils.RemoteGetTime(remote.ssh, EscapedPath, false);
            set => Utils.RemoteSetTime(remote.ssh, EscapedPath, false, value);
        }

        internal override FileAttributes WindowsAttributes
        {
            get => Utils.GetRemoteWindowsAttributes(remote.ssh, EscapedPath);
            set => Utils.SetRemoteWindowsAttributes(remote.ssh, EscapedPath, value);
        }

        internal override FileSystemSecurity WindowsAccessControlList
        {
            get => Utils.GetRemoteWindowsAccessControlList<DirectorySecurity>(remote.ssh, EscapedPath, oldSid, newSid);
            set => Utils.SetRemoteWindowsAccessControlList(remote.ssh, EscapedPath, value);
        }

        internal override void Create()
        {
            remote.sftp.CreateDirectory(Path);
            var attributes = remote.sftp.GetAttributes(Path);
            attributes.SetPermissions(0700);
            remote.sftp.SetAttributes(Path, attributes);
        }

        internal override SortedSet<AbstractDirectory> GetDirectories()
        {
            try
            {
                var directories = new SortedSet<AbstractDirectory>();

                foreach (var file in remote.sftp.ListDirectoryAlmostAll(Path))
                {
                    if (file.IsDirectory)
                    {
                        var directory = new RemoteDirectory(remote, file.FullName, oldSid, newSid);
                        directories.Add(directory);
                    }
                }

                return directories;
            }
            catch (SftpPermissionDeniedException e)
            {
                throw new AccessDeniedException(Path, e);
            }
        }

        internal override AbstractFile GetFile(string fileName)
        {
            string targetPath = string.Format("{0}/{1}", Path, fileName);
            var file = new RemoteRegularFile(remote, targetPath, oldSid, newSid);
            return file;
        }

        internal override SortedSet<AbstractRegularFile> GetRegularFiles()
        {
            try
            {
                var files = new SortedSet<AbstractRegularFile>();

                foreach (var file in remote.sftp.ListDirectoryAlmostAll(Path))
                {
                    if (file.IsRegularFile)
                    {
                        var regularFile = new RemoteRegularFile(remote, file.FullName, oldSid, newSid);
                        files.Add(regularFile);
                    }
                }

                return files;
            }
            catch (SftpPermissionDeniedException e)
            {
                throw new AccessDeniedException(Path, e);
            }
        }

        internal override AbstractDirectory GetSubDirectory(string directoryName)
        {
            string targetPath = string.Format("{0}/{1}", Path, directoryName);
            var directory = new RemoteDirectory(remote, targetPath, oldSid, newSid);
            return directory;
        }

        internal override void Remove()
        {
            Remove(remote.sftp.Get(Path));
        }

        private void Remove(SftpFile sftpFile)
        {
            Log.DebugFormat("Removing {0}", Path);
            try
            {
                if (sftpFile.IsDirectory)
                {
                    foreach (var file in remote.sftp.ListDirectoryAlmostAll(sftpFile.FullName))
                    {
                        Remove(file);
                    }
                }
                sftpFile.Delete();
            }
            catch (SftpPermissionDeniedException e)
            {
                throw new AccessDeniedException(Path, e);
            }
        }
    }
}
