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
            File.Create(Path).Close();
        }

        internal override Stream OpenRead()
        {
            return File.OpenRead(Path);
        }

        internal override Stream OpenWrite()
        {
            return File.OpenWrite(Path);
        }

        internal override void Remove()
        {
            File.Delete(Path);
        }
    }
}
