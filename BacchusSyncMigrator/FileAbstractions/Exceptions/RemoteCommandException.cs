using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pGina.Plugin.BacchusSync.FileAbstractions.Exceptions
{
    internal class RemoteCommandException : Exception
    {
        internal RemoteCommandException(string message) : base(message)
        {
        }
    }
}
