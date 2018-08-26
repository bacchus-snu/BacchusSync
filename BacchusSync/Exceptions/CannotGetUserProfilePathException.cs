using System;

namespace pGina.Plugin.BacchusSync.Exceptions
{
    internal class CannotGetUserProfilePathException : Exception
    {
        internal CannotGetUserProfilePathException(string username) : base(string.Format("Cannot get profile of user {0}.", username))
        {
        }
    }
}
