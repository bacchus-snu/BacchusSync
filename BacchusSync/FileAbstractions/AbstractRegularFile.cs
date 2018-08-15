using pGina.Plugin.BacchusSync.FileAbstractions.Exceptions;
using System;
using System.IO;

namespace pGina.Plugin.BacchusSync.FileAbstractions
{
    internal abstract class AbstractRegularFile : AbstractFile
    {
        internal abstract DateTime LastAccessTime
        {
            get;
            set;
        }
        internal abstract DateTime LastWriteTime
        {
            get;
            set;
        }
        internal abstract FileAttributes WindowsAttributes
        {
            get;
            set;
        }

        internal abstract Stream OpenRead();
        internal abstract Stream OpenWrite();

        internal override void CopyTo(AbstractDirectory destination)
        {
            var target = destination.GetFile(Name);
            Copy(target);
        }

        internal override void Copy(AbstractFile destination)
        {
            Log.DebugFormat("Copy file {0} to {1}", Path, destination.Path);
            if (!(destination is AbstractRegularFile))
            {
                throw new CopyTypeException("Destination type is not regular file.");
            }

            var destinationRegularFile = destination as AbstractRegularFile;

            if (destinationRegularFile.Exists)
            {
                destinationRegularFile.Truncate();
            }
            destinationRegularFile.Create();

            using (var sourceStream = OpenRead())
            {
                using (var destinationStream = destinationRegularFile.OpenWrite())
                {
                    sourceStream.CopyTo(destinationStream);
                }
            }

            destinationRegularFile.SetAllAttributes(this);
        }

        internal virtual void SetAllAttributes(AbstractRegularFile original)
        {
            LastAccessTime = original.LastAccessTime;
            LastWriteTime = original.LastWriteTime;
            WindowsAttributes = original.WindowsAttributes;
        }

        /// <summary>
        /// Remove all contents in file and set file size to 0, without changing file attributes.
        /// </summary>
        internal abstract void Truncate();
    }
}
