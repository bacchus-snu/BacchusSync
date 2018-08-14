using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace pGina.Plugin.BacchusSync
{
    internal static class Utils
    {
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
    }
}
