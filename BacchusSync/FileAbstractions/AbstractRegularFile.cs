using pGina.Plugin.BacchusSync.FileAbstractions.Exceptions;
using System;
using System.IO;

namespace pGina.Plugin.BacchusSync.FileAbstractions
{
    internal abstract class AbstractRegularFile : AbstractFile
    {
        internal abstract DateTime LastWriteTime
        {
            get;
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
            if (!(destination is AbstractRegularFile))
            {
                throw new CopyTypeException("Destination type is not regular file.");
            }

            var destinationRegularFile = destination as AbstractRegularFile;

            if (destinationRegularFile.Exists)
            {
                destinationRegularFile.Remove();
            }
            destinationRegularFile.Create();

            using (var sourceStream = OpenRead())
            {
                using (var destinationStream = destinationRegularFile.OpenWrite())
                {
                    sourceStream.CopyTo(destinationStream);
                }
            }
        }
    }
}
