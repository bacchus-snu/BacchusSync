using System;

namespace pGina.Plugin.BacchusSync.Exceptions
{
    internal class UserNotLoggedOutException : Exception
    {
        private readonly string lastHost;

        internal UserNotLoggedOutException(string lastHost)
        {
            this.lastHost = lastHost;
        }

        public override string Message => "You are not completely logged out from " + lastHost;
    }
}
