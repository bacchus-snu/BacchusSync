using System;
using System.IO;

namespace pGina.Plugin.BacchusSync.FileAbstractions
{
    internal abstract class AbstractFile : IComparable<AbstractFile>
    {
        internal string Path
        {
            get;
            private protected set;
        }
        internal abstract string Name
        {
            get;
        }
        internal abstract bool Exists
        {
            get;
        }
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

        internal abstract void Remove();
        internal abstract void CopyTo(AbstractDirectory destination);
        internal abstract void Copy(AbstractFile destination);
        internal abstract void Create();

        public virtual int CompareTo(AbstractFile other)
        {
            return string.Compare(Name, other.Name);
        }

        /// <summary>
        /// Apply original's atime, mtime, and Windows attributes to this file.
        /// </summary>
        internal virtual void SetAllAttributes(AbstractFile original)
        {
            LastAccessTime = original.LastAccessTime;
            LastWriteTime = original.LastWriteTime;
            WindowsAttributes = original.WindowsAttributes;
        }
    }
}
