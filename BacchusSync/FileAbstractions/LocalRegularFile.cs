using pGina.Plugin.BacchusSync.FileAbstractions.Exceptions;
using System;
using System.IO;

namespace pGina.Plugin.BacchusSync.FileAbstractions
{
    internal class LocalRegularFile : AbstractRegularFile
    {
        internal LocalRegularFile(string path)
        {
            Path = path;
        }

        internal override DateTime LastWriteTime => File.GetLastWriteTime(Path);

        internal override string Name => System.IO.Path.GetFileName(Path);

        internal override bool Exists => File.Exists(Path);

        internal override void Create()
        {
            try
            {
                File.Create(Path).Close();
            }
            catch (UnauthorizedAccessException e)
            {
                throw new AccessDeniedException(Path, e);
            }
        }

        internal override Stream OpenRead()
        {
            try
            {
                return File.OpenRead(Path);
            }
            catch (UnauthorizedAccessException e)
            {
                throw new AccessDeniedException(Path, e);
            }
        }

        internal override Stream OpenWrite()
        {
            try
            {
                return File.OpenWrite(Path);
            }
            catch (UnauthorizedAccessException e)
            {
                throw new AccessDeniedException(Path, e);
            }
        }

        internal override void Remove()
        {
            try
            {
                File.Delete(Path);
            }
            catch (UnauthorizedAccessException e)
            {
                throw new AccessDeniedException(Path, e);
            }
        }
    }
}
