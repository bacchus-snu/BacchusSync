using System;
using System.IO;

namespace pGina.Plugin.BacchusSync.FileAbstractions
{
    internal class LocalRegularFile : AbstractRegularFile
    {
        private readonly string path;

        internal LocalRegularFile(string path)
        {
            this.path = path;
        }

        internal override DateTime LastWriteTime => File.GetLastWriteTime(path);

        internal override string Name => Path.GetFileName(path);

        internal override bool Exists => File.Exists(path);

        internal override void Create()
        {
            File.Create(path).Close();
        }

        internal override Stream OpenRead()
        {
            return File.OpenRead(path);
        }

        internal override Stream OpenWrite()
        {
            return File.OpenWrite(path);
        }

        internal override void Remove()
        {
            File.Delete(path);
        }
    }
}
