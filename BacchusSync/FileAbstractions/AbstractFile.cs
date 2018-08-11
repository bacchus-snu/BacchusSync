using System;

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

        internal abstract void Remove();
        internal abstract void CopyTo(AbstractDirectory destination);
        internal abstract void Copy(AbstractFile destination);
        internal abstract void Create();

        public virtual int CompareTo(AbstractFile other)
        {
            return string.Compare(Name, other.Name);
        }
    }
}
