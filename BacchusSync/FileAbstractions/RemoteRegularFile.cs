using pGina.Plugin.BacchusSync.FileAbstractions.Extra;
using pGina.Plugin.BacchusSync.FileAbstractions.Exceptions;
using System;
using System.IO;
using System.Linq;

namespace pGina.Plugin.BacchusSync.FileAbstractions
{
    internal class RemoteRegularFile : AbstractRegularFile
    {
        private readonly RemoteContext remote;

        internal RemoteRegularFile(RemoteContext remote, string path)
        {
            if (path.Contains('\\'))
            {
                throw new InvalidCharacterException("Remote path contains reverse slash.");
            }
            this.remote = remote;
            Path = path;
        }

        internal override DateTime LastAccessTime
        {
            get => remote.sftp.GetLastAccessTime(Path);
            set => SetTime(true, value);
        }

        internal override DateTime LastWriteTime
        {
            get => remote.sftp.GetLastWriteTime(Path);
            set => SetTime(false, value);
        }

        /// <summary>
        /// Set atime or mtime.
        /// </summary>
        /// <param name="setAccessTime">True to set atime, false to set mtime.</param>
        /// <param name="time">Time to set.</param>
        private void SetTime(bool setAccessTime, DateTime time)
        {
            DateTime u = time.ToUniversalTime();
            string type = setAccessTime ? "-a" : "-m";
            string commandText = string.Format("touch -c {0} -d \"{1}-{2}-{3} {4}:{5}:{6}.{7} +0000\" \"{8}\"", type, u.Year, u.Month, u.Day, u.Hour, u.Minute, u.Second, u.Millisecond, Path);
            var command = remote.ssh.RunCommand(commandText);
            if (command.ExitStatus != 0)
            {
                throw new RemoteCommandException(string.Format("Modifing time failed.\nCommand : {0}\nExit code : {1}", commandText, command.ExitStatus));
            }
        }

        internal override string Name => Path.Split('/').Last();

        internal override bool Exists => remote.sftp.Exists(Path);

        internal override FileAttributes WindowsAttributes
        {
            get
            {
                var command = remote.ssh.RunCommand(string.Format("getfattr -n user.WinAttr --only-values \"{0}\"", Path));
                if (command.ExitStatus == 0)
                {
                    return (FileAttributes)int.Parse(command.Result);
                }
                else if (command.Result.EndsWith("No such attribute"))
                {
                    Log.WarnFormat("{0} doesn't have windows attributes.", Path);
                    return FileAttributes.Normal;
                }
                else
                {
                    throw new RemoteCommandException(string.Format("getfattr failed with exit code {0} while processing {1}", command.ExitStatus, Path));
                }
            }
            set
            {
                var command = remote.ssh.RunCommand(string.Format("setfattr -n user.WinAttr -v \"{0}\" \"{1}\"", (int)value, Path));
                if (command.ExitStatus != 0)
                {
                    throw new RemoteCommandException(string.Format("setfattr failed with exit code {0} while processing {1}", command.ExitStatus, Path));
                }
            }
        }

        internal override void Create()
        {
            remote.sftp.Create(Path);
        }

        internal override Stream OpenRead()
        {
            return remote.sftp.OpenRead(Path);
        }

        internal override Stream OpenWrite()
        {
            return remote.sftp.OpenWrite(Path);
        }

        internal override void Remove()
        {
            Log.DebugFormat("Removing {0}", Path);
            remote.sftp.Delete(Path);
        }
    }
}
