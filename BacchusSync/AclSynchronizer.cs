using pGina.Plugin.BacchusSync.Extra;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace pGina.Plugin.BacchusSync
{
    /// <summary>
    /// Class to synchronize ACL(Access Control List)
    /// </summary>
    internal class AclSynchronizer
    {
        internal static readonly string TEMP_DIRECTORY = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
        private static readonly List<string> temporaryFiles = new List<string>();

        /// <summary>
        /// Save access control list to a file using icacls.
        /// </summary>
        /// <param name="path">Path of directory to save ACL.</param>
        /// <returns>Stream to acl file.</returns>
        /// <exception cref="DirectoryNotFoundException">If <paramref name="path"/> does not refer to an existing directory.</exception>
        internal static Stream Save(string path)
        {
            // Remove trailing \ to prevent closing " parsed as normal character in arguments.
            if (path[path.Length - 1] == '\\')
            {
                path = path.Substring(0, path.Length - 1);
            }

            // Path must exist and refers to a directory. Otherwise, other users' files can be save to acl file.
            if (Directory.Exists(path))
            {
                string tempAclFilePath = Path.Combine(TEMP_DIRECTORY, Path.GetFileName(path) + ".acl");
                var startInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.SystemDirectory, "icacls.exe"),
                    Arguments = string.Format("\"{0}\" /save \"{1}\" /T /C /L /Q", path, tempAclFilePath),
                };
                var process = Process.Start(startInfo);

                process.WaitForExit();
                Utils.RestrictUserAccessToFile(tempAclFilePath);

                lock (temporaryFiles)
                {
                    temporaryFiles.Add(tempAclFilePath);
                }

                return File.Open(tempAclFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
            }
            else
            {
                throw new DirectoryNotFoundException(string.Format("Cannot save acl to file. Directory {0} does not exist.", path));
            }
        }

        /// <summary>
        /// Restore acl from file using icacls.
        /// </summary>
        /// <param name="parentOfTarget">Path to parent directory of target.</param>
        /// <param name="aclFile">Path to acl file.</param>
        internal static void Restore(string parentOfTarget, string aclFile)
        {
            // Remove trailing \ to prevent closing " parsed as normal character in arguments.
            if (parentOfTarget[parentOfTarget.Length - 1] == '\\')
            {
                parentOfTarget = parentOfTarget.Substring(0, parentOfTarget.Length - 1);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "icacls.exe"),
                Arguments = string.Format("\"{0}\" /restore \"{1}\" /C /L /Q", parentOfTarget, aclFile),
            };
            var process = Process.Start(startInfo);

            process.WaitForExit();
        }

        /// <summary>
        /// Remove all temporary files.
        /// </summary>
        internal static void CleanUp()
        {
            lock (temporaryFiles)
            {
                foreach (string file in temporaryFiles)
                {
                    File.Delete(file);
                }
                temporaryFiles.Clear();
            }
        }
    }
}
