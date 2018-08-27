using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.ServiceProcess;

namespace Updater
{
    class Program
    {
        private const string REGISTRY_PATH = @"SOFTWARE\Bacchus\BacchusSync";
        private const string REGISTRY_KEY_LAST_UPDATE = "LastUpdate";
        private const string UPDATE_VERSION_PATH = "/update/sync/version.txt";
        private const string UPDATE_FILE_PATH = "/update/sync/update.gz";
        private const string PGINA_SERVICE_NAME = "pGina";
        private const string PLUGIN_FILE_NAME = "pGina.Plugin.BacchusSync.dll";

        private static string UpdateServerAddress;

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Updater [ServerAddress] [PluginPath]");
                Environment.Exit(1);
            }

            UpdateServerAddress = args[0];

            if (GetLastUpdateTime() < GetUpdateReleaseTime())
            {
                StopServiceAndWait();
                UpdateDll(args[1]);
                StartService();
                SetLastUpdateTime(DateTime.Now);
            }
        }

        private static DateTime GetLastUpdateTime()
        {
            using (RegistryKey registry = Registry.LocalMachine.OpenSubKey(REGISTRY_PATH))
            {
                if (registry == null)
                {
                    return DateTime.MinValue;
                }
                else
                {
                    return new DateTime((long)registry.GetValue(REGISTRY_KEY_LAST_UPDATE, DateTime.MinValue.Ticks));
                }
            }
        }

        private static void SetLastUpdateTime(DateTime time)
        {
            using (RegistryKey registry = Registry.LocalMachine.CreateSubKey(REGISTRY_PATH))
            {
                registry.SetValue(REGISTRY_KEY_LAST_UPDATE, time.Ticks, RegistryValueKind.QWord);
            }
        }

        private static DateTime GetUpdateReleaseTime()
        {
            using (WebClient webClient = new WebClient())
            {
                webClient.BaseAddress = UpdateServerAddress;

                string versionString = webClient.DownloadString(UPDATE_VERSION_PATH);
                return DateTime.Parse(versionString);
            }
        }

        private static void StopServiceAndWait()
        {
            using (var service = new ServiceController(PGINA_SERVICE_NAME))
            {
                if (service.Status == ServiceControllerStatus.Running)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped);
                }
            }
        }

        private static void UpdateDll(string pluginDirectoryPath)
        {
            using (WebClient webClient = new WebClient())
            {
                webClient.BaseAddress = UpdateServerAddress;

                using (var compressedStream = webClient.OpenRead(UPDATE_FILE_PATH))
                {
                    using (var newDllFileStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                    {
                        using (var oldDllFileStream = File.OpenWrite(Path.Combine(pluginDirectoryPath, PLUGIN_FILE_NAME)))
                        {
                            oldDllFileStream.SetLength(0);
                            newDllFileStream.CopyTo(oldDllFileStream);
                        }
                    }
                }
            }
        }

        private static void StartService()
        {
            using (var service = new ServiceController(PGINA_SERVICE_NAME))
            {
                service.Start();
            }
        }
    }
}
