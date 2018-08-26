using System;

namespace pGina.Plugin.BacchusSync.Exceptions
{
    internal class ProfileOperationException : Exception
    {
        internal ProfileOperationException(string message) : base(message)
        {
        }
    }
}
