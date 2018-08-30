using pGina.Plugin.BacchusSync;
using pGina.Plugin.BacchusSync.Extra;
using System;
using System.IO;
using System.Threading;

namespace BacchusSyncMigrator
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 4 || !Directory.Exists(args[0]))
            {
                Console.Error.WriteLine("BacchusSyncMigrator [ProfilesPath] [ServerAddress] [Username] [Password]");
                Environment.Exit(1);
            }

            Settings.ServerAddress = args[1];
            Worker[] workers = new Worker[Environment.ProcessorCount];
            for (int i = 0; i < workers.Length; i++)
            {
                workers[i] = new Worker(args[2], args[3]);
            }

            ApiUtils.GetSeBackupPrivilege();
            ApiUtils.GetSeRestorePrivilege();

            foreach (string profile in Directory.GetDirectories(args[0]))
            {
                if (!profile.EndsWith(".V6"))
                {
                    continue;
                }
                
                while (true)
                {
                    foreach (Worker worker in workers)
                    {
                        if (!worker.IsWorking)
                        {
                            worker.Migrate(profile);
                            goto outer;
                        }
                    }
                    Thread.Sleep(200);
                }
                outer:;
            }

            foreach (Worker worker in workers)
            {
                worker.Wait();
            }
        }
    }

    class Worker
    {
        private readonly SftpSynchronizer sftpSynchronizer;
        private bool working = false;
        private string target;

        internal bool IsWorking
        {
            get
            {
                lock (this)
                {
                    return working;
                }
            }
        }

        internal Worker(string username, string password)
        {
            sftpSynchronizer = new SftpSynchronizer(username, password);
        }

        internal void Migrate(string profile)
        {
            lock (this)
            {
                working = true;
                target = profile;
            }

            Console.WriteLine("Sending " + profile);
            new Thread(Work).Start();
        }

        private void Work()
        {
            string target;

            lock (this)
            {
                target = this.target;
            }

            sftpSynchronizer.Migrate(target);

            lock (this)
            {
                working = false;
            }
        }

        internal void Wait()
        {
            while (true)
            {
                lock (this)
                {
                    if (!IsWorking)
                    {
                        return;
                    }
                }

                Thread.Sleep(200);
            }
        }
    }
}
