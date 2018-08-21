using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceProcess;
using log4net;
using pGina.Plugin.BacchusSync.Extra;
using pGina.Shared.Interfaces;
using pGina.Shared.Types;

namespace pGina.Plugin.BacchusSync
{
    public class PluginImpl : IPluginAuthenticationGateway, IPluginEventNotifications, IPluginLogoffRequestAddTime, IPluginConfiguration
    {
        internal static readonly Guid UUID = new Guid("a56d528b-c55e-4553-8a73-4a7aa2e3850c");

        public string Name => "Bacchus sync";
        public string Description => "Bacchus user profile sync plugin";
        public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public Guid Uuid => UUID;

        private readonly SessionTracker sessionTracker;

        public PluginImpl()
        {
            sessionTracker = new SessionTracker();
            Log.Instantiate();
        }

        public void Starting()
        {
        }

        public void Stopping()
        {
        }

        public BooleanResult AuthenticatedUserGateway(SessionProperties properties)
        {
            var userInformation = properties.GetTrackedSingle<UserInformation>();

            try
            {
                if (!sessionTracker.IsUploadingProfile(userInformation.Username))
                {
                    using (var synchronizer = new SftpSynchronizer(userInformation.Username, userInformation.Password, userInformation.SID.Value))
                    {
                        synchronizer.DownloadProfile();
                    }
                }

                sessionTracker.UserGatewayPassed(userInformation.Username, userInformation.SID, userInformation.Password);
                return new BooleanResult { Success = true };
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
                return new BooleanResult { Success = false, Message = e.Message };
            }
        }

        public void SessionChange(int sessionId, SessionChangeReason evnt, SessionProperties properties)
        {
            if (evnt == SessionChangeReason.SessionLogon)
            {
                string username = Utils.GetUserFromSession(sessionId).GetUsername();
                sessionTracker.UserLoggedOn(username, sessionId);
            }
            else if (evnt == SessionChangeReason.SessionLogoff)
            {
                SessionTracker.Information information = sessionTracker.GetInformation(sessionId);
                if (information == null)
                {
                    return;
                }

                try
                {
                    sessionTracker.StartedProfileUploading(information.Username);
                    using (var synchronizer = new SftpSynchronizer(information.Username, information.Password, information.Sid.Value))
                    {
                        synchronizer.UploadProfile();
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e.Message);
                    Log.Error(e.StackTrace);
                }
                finally
                {
                    sessionTracker.UserLoggedOff(information.Username, sessionId);
                }
            }
        }

        /// <summary>
        /// Called during a shutdown, after the GPO shutdown scripts
        /// it requires a bool true to wait a while longer
        /// </summary>
        public bool LogoffRequestAddTime()
        {
            return sessionTracker.ProfileUploadProgressing;
        }

        /// <summary>
        /// Called prior to authentication for every login.
        /// to check if the user that tries to login is still logged out
        /// bool false means the user can login
        /// </summary>
        public bool LoginUserRequest(string username)
        {
            return sessionTracker.IsUploadingProfile(username);
        }

        public void Configure()
        {
            var configurationForm = new ConfigurationForm();
            configurationForm.ShowDialog();
        }
    }
}
