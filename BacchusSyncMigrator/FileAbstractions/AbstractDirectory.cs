using pGina.Plugin.BacchusSync.FileAbstractions.Exceptions;
using System.Collections.Generic;

namespace pGina.Plugin.BacchusSync.FileAbstractions
{
    internal abstract class AbstractDirectory : AbstractFile
    {
        internal abstract SortedSet<AbstractDirectory> GetDirectories();
        internal abstract SortedSet<AbstractRegularFile> GetRegularFiles();
        internal abstract AbstractDirectory GetSubDirectory(string directoryName);
        internal abstract AbstractFile GetFile(string fileName);

        internal override void CopyTo(AbstractDirectory destination)
        {
            var target = destination.GetSubDirectory(Name);
            Copy(target);
        }

        internal override void Copy(AbstractFile destination)
        {
            Copy(destination, true);
        }

        internal virtual void Copy(AbstractFile destination, bool ignoreErrors)
        {
            Log.DebugFormat("Copy directory {0} to {1}", Path, destination.Path);
            if (!(destination is AbstractDirectory))
            {
                throw new CopyTypeException("Destination type is not directory.");
            }

            var destinationDirectory = destination as AbstractDirectory;

            if (!destination.Exists)
            {
                destination.Create();
            }

            foreach (var directory in GetDirectories())
            {
                try
                {
                    var targetDirectory = destinationDirectory.GetSubDirectory(directory.Name);
                    directory.Copy(targetDirectory);
                }
                catch (AccessDeniedException e)
                {
                    Log.Warn(e.Message);
                    if (!ignoreErrors)
                    {
                        throw e;
                    }
                }
            }

            foreach (var file in GetRegularFiles())
            {
                try
                {
                    var targetFile = destinationDirectory.GetFile(file.Name);
                    file.Copy(targetFile);
                }
                catch (AccessDeniedException e)
                {
                    Log.Warn(e.Message);
                    if (!ignoreErrors)
                    {
                        throw e;
                    }
                }
            }

            destinationDirectory.SetAllAttributes(this);
        }
    }
}
