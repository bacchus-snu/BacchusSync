using System;

namespace pGina.Plugin.BacchusSync.Exceptions
{
    internal class SyncFileFormatException : Exception
    {
        internal SyncFileFormatException(string message) : base(message)
        {
        }
    }
}
