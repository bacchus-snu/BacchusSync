namespace pGina.Plugin.BacchusSync
{
    internal static class Settings
    {
        internal static string ServerAddress;

        internal static ushort ServerPort => 22;

        internal static string HostKey => string.Empty;

        internal static string ServerBaseDirectory => "/srv/profiles";
    }
}
