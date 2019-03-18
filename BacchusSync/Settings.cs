using pGina.Shared.Settings;

namespace pGina.Plugin.BacchusSync
{
    internal static class Settings
    {
        private const string KEY_SERVER_ADDRESS = "ServerAddress";
        private const string KEY_SERVER_PORT = "ServerPort";
        private const string KEY_HOST_KEY = "HostKey";
        private const string KEY_SERVER_BASE_DIRECTORY = "ServerBaseDirectory";
        private const string KEY_AUTHENTICATION_SERVER_ADDRESS = "AuthenticationServerAddress";

        private static pGinaDynamicSettings settings = new pGinaDynamicSettings(PluginImpl.UUID);

        static Settings()
        {
            settings.SetDefault(KEY_SERVER_ADDRESS, "localhost");
            settings.SetDefault(KEY_SERVER_PORT, "22");
            settings.SetDefault(KEY_HOST_KEY, "");
            settings.SetDefault(KEY_SERVER_BASE_DIRECTORY, "/srv/profiles");
            settings.SetDefault(KEY_AUTHENTICATION_SERVER_ADDRESS, "https://localhost");
        }

        internal static string ServerAddress
        {
            get => settings.GetSetting(KEY_SERVER_ADDRESS).RawValue as string;
            set => settings.SetSetting(KEY_SERVER_ADDRESS, value);
        }

        internal static ushort ServerPort
        {
            get => ushort.Parse(settings.GetSetting(KEY_SERVER_PORT).RawValue as string);
            set => settings.SetSetting(KEY_SERVER_PORT, value.ToString());
        }

        internal static string HostKey
        {
            get => settings.GetSetting(KEY_HOST_KEY).RawValue as string;
            set => settings.SetSetting(KEY_HOST_KEY, value);
        }

        internal static string ServerBaseDirectory
        {
            get => settings.GetSetting(KEY_SERVER_BASE_DIRECTORY).RawValue as string;
            set => settings.SetSetting(KEY_SERVER_BASE_DIRECTORY, value);
        }

        internal static string AuthenticationServerAddress
        {
            get => settings.GetSetting(KEY_AUTHENTICATION_SERVER_ADDRESS).RawValue as string;
            set => settings.SetSetting(KEY_AUTHENTICATION_SERVER_ADDRESS, value);
        }
    }
}
