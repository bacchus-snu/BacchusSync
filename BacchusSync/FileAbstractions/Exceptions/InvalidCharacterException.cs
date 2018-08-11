using System;

namespace pGina.Plugin.BacchusSync.FileAbstractions.Exceptions
{
    internal class InvalidCharacterException : Exception
    {
        internal InvalidCharacterException(string message) : base(message)
        {
        }
    }
}
