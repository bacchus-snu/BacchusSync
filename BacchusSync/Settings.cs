using pGina.Shared.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pGina.Plugin.BacchusSync
{
    internal static class Settings
    {
        private const string KEY_SERVER_ADDRESS = "ServerAddress";
        private const string KEY_SERVER_PORT = "ServerPort";
        private const string KEY_SERVER_FINGERPRINT = "ServerFingerprint";
        private const string KEY_SERVER_BASE_DIRECTORY = "ServerBaseDirectory";

        private static pGinaDynamicSettings settings = new pGinaDynamicSettings(PluginImpl.UUID);

        static Settings()
        {
            settings.SetDefault(KEY_SERVER_ADDRESS, "localhost");
            settings.SetDefault(KEY_SERVER_PORT, "22");
            settings.SetDefault(KEY_SERVER_FINGERPRINT, "");
            settings.SetDefault(KEY_SERVER_BASE_DIRECTORY, "/srv/profiles");
        }

        internal static string ServerAddress
        {
            get => settings.GetSetting(KEY_SERVER_ADDRESS).RawValue as string;
            set => settings.SetSetting(KEY_SERVER_ADDRESS, value);
        }

        internal static int ServerPort
        {
            get => (int) settings.GetSetting(KEY_SERVER_PORT).RawValue;
            set => settings.SetSetting(KEY_SERVER_PORT, value);
        }

        internal static string ServerFingerprint
        {
            get => settings.GetSetting(KEY_SERVER_FINGERPRINT).RawValue as string;
            set => settings.SetSetting(KEY_SERVER_FINGERPRINT, value);
        }

        internal static string ServerBaseDirectory
        {
            get => settings.GetSetting(KEY_SERVER_BASE_DIRECTORY).RawValue as string;
            set => settings.SetSetting(KEY_SERVER_BASE_DIRECTORY, value);
        }
    }
}
