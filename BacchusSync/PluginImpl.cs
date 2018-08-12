using System;
using System.Collections.Generic;
using System.Reflection;
using System.ServiceProcess;
using log4net;
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

        private readonly List<string> uploadTasks;

        public PluginImpl()
        {
            uploadTasks = new List<string>();
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
            bool uploadProgressing = false;

            try
            {
                lock (uploadTasks)
                {
                    uploadProgressing = uploadTasks.Contains(userInformation.Username.ToLower());
                }

                if (!uploadProgressing)
                {
                    var synchronizer = new SftpSynchronizer(userInformation.Username, userInformation.Password);
                    synchronizer.DownloadProfile();
                }
                return new BooleanResult { Success = true };
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
                return new BooleanResult { Success = false, Message = e.Message };
            }
        }

        public void SessionChange(int sessionID, SessionChangeReason evnt, SessionProperties properties)
        {
            var userInformation = properties.GetTrackedSingle<UserInformation>();

            if (evnt == SessionChangeReason.SessionUnlock)
            {
                try
                {
                    lock (uploadTasks)
                    {
                        uploadTasks.Add(userInformation.Username.ToLower());
                    }

                    var synchronizer = new SftpSynchronizer(userInformation.Username, userInformation.Password);
                    synchronizer.UploadProfile();
                }
                catch (Exception e)
                {
                    Log.Error(e.Message);
                    Log.Error(e.StackTrace);
                }
                finally
                {
                    lock (uploadTasks)
                    {
                        uploadTasks.Remove(userInformation.Username.ToLower());
                    }
                }
            }
        }

        /// <summary>
        /// Called during a shutdown, after the GPO shutdown scripts
        /// it requires a bool true to wait a while longer
        /// </summary>
        public bool LogoffRequestAddTime()
        {
            lock (uploadTasks)
            {
                return uploadTasks.Count > 0;
            }
        }

        /// <summary>
        /// Called prior to authentication for every login.
        /// to check if the user that tries to login is still logged out
        /// bool false means the user can login
        /// </summary>
        public bool LoginUserRequest(string username)
        {
            lock (uploadTasks)
            {
                return uploadTasks.Contains(username.ToLower());
            }
        }

        public void Configure()
        {
            var configurationForm = new ConfigurationForm();
            configurationForm.ShowDialog();
        }
    }
}
