using System;

namespace pGina.Plugin.BacchusSync.Exceptions
{
    internal class HostKeyMismatchException : Exception
    {
        internal HostKeyMismatchException() : base("Cannot connect to server. Host key doesn't match.")
        {
        }
    }
}
