using System;

namespace pGina.Plugin.BacchusSync.Exceptions
{
    internal class ApiException : Exception
    {
        internal ApiException(string message) : base(message)
        {
        }
    }
}
