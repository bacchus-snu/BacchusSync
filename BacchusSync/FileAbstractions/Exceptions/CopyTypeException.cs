using System;

namespace pGina.Plugin.BacchusSync.FileAbstractions.Exceptions
{
    internal class CopyTypeException : Exception
    {
        internal CopyTypeException(string message) : base(message)
        {
        }
    }
}
