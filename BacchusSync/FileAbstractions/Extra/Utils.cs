using pGina.Plugin.BacchusSync.FileAbstractions.Exceptions;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;

namespace pGina.Plugin.BacchusSync.FileAbstractions.Extra
{
    internal static class Utils
    {
        private static readonly char[] CHARACTERS_TO_TRIM = new char[] { ' ', '\n' };

        internal static IEnumerable<SftpFile> ListDirectoryAlmostAll(this SftpClient client, string path)
        {
            var rawList = client.ListDirectory(path);
            var newList = new List<SftpFile>();

            foreach (var file in rawList)
            {
                if (file.Name != "." && file.Name != "..")
                {
                    newList.Add(file);
                }
            }

            return newList;
        }

        /// <summary>
        /// Check if a file or directory is reparse point(Directory junction or symbolic link).
        /// </summary>
        /// <param name="fileSystemInfo">File or directory info to check</param>
        /// <returns>True if reparse point, false otherwise</returns>
        internal static bool IsReparsePoint(this FileSystemInfo fileSystemInfo)
        {
            return fileSystemInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }

        internal static DateTime RemoteGetTime(SshClient ssh, string path, bool getAccessTime)
        {
            string type = getAccessTime ? "%x" : "%y";
            string commandText = string.Format("stat \"--printf={0}\" \"{1}\"", type, path);
            var command = ssh.RunCommand(commandText);
            if (command.ExitStatus != 0)
            {
                throw new RemoteCommandException(string.Format("Getting time failed.\nCommand : {0}\nExit code : {1}", commandText, command.ExitStatus));
            }
            else if (DateTime.TryParse(command.Result, out DateTime time))
            {
                return time;
            }
            else
            {
                throw new FormatException("Cannot parse time format from remote server.\nTime string : " + command.Result);
            }
        }

        /// <summary>
        /// Set atime or mtime.
        /// </summary>
        /// <param name="ssh">SSH client to remote server.</param>
        /// <param name="path">Path of the remote file.</param>
        /// <param name="setAccessTime">True to set atime, false to set mtime.</param>
        /// <param name="time">Time to set.</param>
        internal static void RemoteSetTime(SshClient ssh, string path, bool setAccessTime, DateTime time)
        {
            DateTime u = time.ToUniversalTime();
            string type = setAccessTime ? "-a" : "-m";
            string commandText = string.Format("touch -c {0} -d \"{1}-{2}-{3} {4}:{5}:{6}.{7} +0000\" \"{8}\"", type, u.Year, u.Month, u.Day, u.Hour, u.Minute, u.Second, u.Ticks % 10000000, path);
            var command = ssh.RunCommand(commandText);
            if (command.ExitStatus != 0)
            {
                throw new RemoteCommandException(string.Format("Modifing time failed.\nCommand : {0}\nExit code : {1}", commandText, command.ExitStatus));
            }
        }

        internal static FileAttributes GetRemoteWindowsAttributes(SshClient ssh, string path)
        {
            var command = ssh.RunCommand(string.Format("getfattr -n user.WinAttr --only-values \"{0}\"", path));
            if (command.ExitStatus == 0)
            {
                return (FileAttributes)int.Parse(command.Result);
            }
            else if (command.Error.TrimEnd(CHARACTERS_TO_TRIM).EndsWith("No such attribute"))
            {
                Log.WarnFormat("{0} doesn't have windows attributes.", path);
                return FileAttributes.Normal;
            }
            else
            {
                throw new RemoteCommandException(string.Format("getfattr failed with exit code {0} while processing {1}", command.ExitStatus, path));
            }
        }

        internal static void SetRemoteWindowsAttributes(SshClient ssh, string path, FileAttributes attributes)
        {
            var command = ssh.RunCommand(string.Format("setfattr -n user.WinAttr -v \"{0}\" \"{1}\"", (int)attributes, path));
            if (command.ExitStatus != 0)
            {
                throw new RemoteCommandException(string.Format("setfattr failed with exit code {0} while processing {1}", command.ExitStatus, path));
            }
        }

        internal static T GetRemoteWindowsAccessControlList<T>(SshClient ssh, string path, string oldSid, string newSid) where T : FileSystemSecurity, new()
        {
            var command = ssh.RunCommand(string.Format("getfattr -n user.WinACL --only-values \"{0}\"", path));
            if (command.ExitStatus == 0)
            {
                T accessControlList = new T();
                accessControlList.SetSecurityDescriptorSddlForm(command.Result.Replace(oldSid, newSid));
                return accessControlList;
            }
            else if (command.Error.TrimEnd(CHARACTERS_TO_TRIM).EndsWith("No such attribute"))
            {
                Log.WarnFormat("{0} doesn't have windows attributes.", path);
                T accessControl = new T();
                accessControl.SetAccessRuleProtection(false, true);
                return accessControl;
            }
            else
            {
                throw new RemoteCommandException(string.Format("getfattr failed with exit code {0} while processing {1}", command.ExitStatus, path));
            }
        }

        internal static void SetRemoteWindowsAccessControlList(SshClient ssh, string path, FileSystemSecurity accessControlList)
        {
            var command = ssh.RunCommand(string.Format("setfattr -n user.WinACL -v \"{0}\" \"{1}\"", accessControlList.GetSecurityDescriptorSddlForm(AccessControlSections.All), path));
            if (command.ExitStatus != 0)
            {
                throw new RemoteCommandException(string.Format("setfattr failed with exit code {0} while processing {1}", command.ExitStatus, path));
            }
        }
    }
}
