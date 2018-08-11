using pGina.Plugin.BacchusSync.Exceptions;
using System.IO;
using System.Text;

namespace pGina.Plugin.BacchusSync
{
    internal class SyncInformation
    {
        private const string MAGIC_NUMBER = "bsync";
        private const int FILE_FORMAT_VERSION = 1;

        internal enum SyncStatus
        {
            /// <summary>
            /// User profile doesn't exist on the server.
            /// </summary>
            DoesNotExist,
            /// <summary>
            /// The user is logged on.
            /// </summary>
            LoggedOn,
            /// <summary>
            /// The user has logged out and profile is now uploading.
            /// </summary>
            Uploading,
            /// <summary>
            /// The user has successfully logged out.
            /// </summary>
            LoggedOut,
        }

        internal SyncStatus Status;
        internal string LastHost;


        internal SyncInformation(SyncStatus status, string lastHost)
        {
            Status = status;
            LastHost = lastHost;
        }

        /// <summary>
        /// Load sync status from stream.
        /// </summary>
        /// <param name="stream">Stream to read</param>
        internal SyncInformation(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                if (reader.ReadLine() != MAGIC_NUMBER)
                {
                    throw new SyncFileFormatException("SyncInformation file magic number mismatch.");
                }

                int fileVersion = int.Parse(reader.ReadLine());
                if (fileVersion <= 0 || fileVersion > FILE_FORMAT_VERSION)
                {
                    throw new SyncFileFormatException("Unsupported SyncInformation version : " + fileVersion);
                }

                Status = (SyncStatus) int.Parse(reader.ReadLine());
                LastHost = reader.ReadLine();
            }
        }

        /// <summary>
        /// Save sync status to a stream.
        /// </summary>
        /// <param name="stream">Stream to write</param>
        internal void Save(Stream stream)
        {
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.NewLine = "\n";
                writer.WriteLine(MAGIC_NUMBER);
                writer.WriteLine(FILE_FORMAT_VERSION);
                writer.WriteLine((int) Status);
                writer.WriteLine(LastHost);
            }
        }
    }
}
