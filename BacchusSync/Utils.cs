using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
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

    }
}
