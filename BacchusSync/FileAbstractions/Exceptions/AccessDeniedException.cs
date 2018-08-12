using System;
using System.Runtime.Serialization;

namespace pGina.Plugin.BacchusSync.FileAbstractions.Exceptions
{
    internal class AccessDeniedException : Exception
    {
        public AccessDeniedException(string path, Exception innerException) : base(string.Format("Cannot access to {0}", path), innerException)
        {
        }
    }
}
