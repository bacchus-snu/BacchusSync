using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Principal;

namespace pGina.Plugin.BacchusSync
{
    internal class SessionTracker
    {
        internal class Information
        {
            internal readonly string Username;
            internal readonly SecurityIdentifier Sid;
            internal readonly string Password;
            internal readonly List<int> SessionIds = new List<int>();
            internal bool Uploading = false;

            internal Information(string username, IdentityReference identity, string password)
            {
                Username = username;
                if (identity is SecurityIdentifier)
                {
                    Sid = (SecurityIdentifier)identity;
                }
                else
                {
                    Sid = (SecurityIdentifier)identity.Translate(typeof(SecurityIdentifier));
                }
                Password = password;
            }

            internal Information(Information original)
            {
                Username = original.Username;
                Sid = original.Sid;
                Password = original.Password;
                SessionIds = new List<int>(original.SessionIds);
                Uploading = original.Uploading;
            }

            internal bool NeedTracked => SessionIds.Count != 0 || Uploading;
        }

        private readonly Dictionary<string, Information> information = new Dictionary<string, Information>();

        /// <summary>
        /// True if one or more users are uploading profile, false otherwise.
        /// </summary>
        internal bool ProfileUploadProgressing
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                foreach (var i in information)
                {
                    if (i.Value.Uploading)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void UserGatewayPassed(string username, SecurityIdentifier sid, string password)
        {
            string key = username.ToLower();

            if (!information.ContainsKey(key))
            {
                information.Add(key, new Information(username, sid, password));
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void UserLoggedOn(string username, int sessionId)
        {
            string key = username.ToLower();

            if (information.ContainsKey(key))
            {
                information[key].SessionIds.Add(sessionId);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void StartedProfileUploading(string username)
        {
            string key = username.ToLower();

            if (information.ContainsKey(key))
            {
                information[key].Uploading = true;
            }
        }

        /// <summary>
        /// Method to be called when profile uploading is done and user logged off.
        /// </summary>
        /// <param name="username">Name of user logged off.</param>
        /// <param name="sessionId">ID of session where user logged off.</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void UserLoggedOff(string username, int sessionId)
        {
            string key = username.ToLower();

            if (information.ContainsKey(key))
            {
                information[key].Uploading = false;
                information[key].SessionIds.Remove(sessionId);
                if (!information[key].NeedTracked)
                {
                    information.Remove(key);
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal bool IsUploadingProfile(string username)
        {
            string key = username.ToLower();

            if (information.ContainsKey(key))
            {
                return information[key].Uploading;
            }
            else
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal Information GetInformation(int sessionId)
        {
            foreach (var i in information)
            {
                if (i.Value.SessionIds.Contains(sessionId))
                {
                    return new Information(i.Value);
                }
            }

            return null;
        }

        
    }
}
