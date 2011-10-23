using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

using Zepheus.Database;
using Zepheus.Util;
using Zepheus.Util.DUpdater;

namespace Zepheus.Login
{
    [ServerModule(Util.InitializationStage.DataStore)]
    public sealed class Worker
    {
        public static Worker Instance { get; private set; }
        public bool IsRunning { get; set; }

        private ConcurrentQueue<Action> callbacks = new ConcurrentQueue<Action>();
        private Thread main;
        private int sleep = 1;


        public Worker()
        {
            main = new Thread(Work);
            IsRunning = true;
            main.Start();
        }

        [InitializerMethod]
        public static bool Load()
        {
            try
            {
                Instance = new Worker();
                Instance.sleep = Settings.Instance.WorkInterval;
                return true;
            }
            catch { return false; }
        }

        public void AddCallback(Action pCallback)
        {
            callbacks.Enqueue(pCallback);
        }

        private void ConnectEntity()
        {
            Program.Entity = EntityFactory.GetAccountEntity(Settings.Instance.Entity);
            // Try to update...
            DatabaseUpdater du = new DatabaseUpdater(Settings.Instance.Entity, DatabaseUpdater.DatabaseTypes.Login);
            du.Update();
        }

        private void Work()
        {
            try
            {
                ConnectEntity(); //we do this here to ensure single threaded on handle!
                Program.Entity.Users.Count(); //force connection to be open & test
                Log.WriteLine(LogLevel.Info, "Database Initialized at {0}", Settings.Instance.Entity.DataCatalog);
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "Error initializing database: {0}", ex.ToString());
                return;
            }
            Action action;
            while (this.IsRunning)
            {
                while (callbacks.TryDequeue(out action))
                {
                    action();
                }
                Thread.Sleep(sleep); 
            }
            Log.WriteLine(LogLevel.Info, "Server stopped handling callbacks.");
        }
    }
}
