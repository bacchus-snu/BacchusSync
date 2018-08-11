using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pGina.Plugin.BacchusSync.Exceptions
{
    internal class SyncFileFormatException : Exception
    {
        internal SyncFileFormatException(string message) : base(message)
        {
        }
    }
}
