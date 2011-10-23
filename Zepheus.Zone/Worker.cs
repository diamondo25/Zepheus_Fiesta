using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

using Zepheus.Database;
using Zepheus.Util;

namespace Zepheus.Zone
{
    [ServerModule(Util.InitializationStage.DataStore)]
    public sealed class Worker
    {
        public static Worker Instance { get; private set; }
        private ConcurrentQueue<Action> callbacks = new ConcurrentQueue<Action>();
        private Thread main;
        private int sleep = 1;
        private ulong ticksToSleep = 200;
        public ulong TicksPerSecond { get; set; }
        public bool IsRunning { get; set; }

        public Worker()
        {
            main = new Thread(Work);
            TicksPerSecond = 0;
            IsRunning = true;
            main.Start();
        }

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
            Program.Entity = EntityFactory.GetWorldEntity(Program.serviceInfo.EntityString);
        }

        public void Stop()
        {
            if (main != null)
            {
                main.Abort();
            }
        }

        private void Work()
        {
            while (Program.serviceInfo == null)
            {
                System.Threading.Thread.Sleep(200); // Wait..
            }
            try
            {
                ConnectEntity();
                Program.Entity.Characters.Count(); //test if database is online
                Log.WriteLine(LogLevel.Info, "Database Initialized.");
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "Error initializing database: {0}", ex.ToString());
                return;
            }
            Action action;
            ulong last = 0;
            DateTime lastCheck = DateTime.Now;
            DateTime lastPing = DateTime.Now;
            DateTime lastGC = DateTime.Now;
            DateTime lastWorldPing = DateTime.Now;
            for (ulong i = 0; ; i++)
            {
                if (!this.IsRunning)
                {
                    break;
                }

                try
                {
                    DateTime now = Program.CurrentTime;

                    while (callbacks.TryDequeue(out action))
                    {
                        action();
                    }

                    if (now.Subtract(lastCheck).TotalSeconds >= 1)
                    {
                        TicksPerSecond = i - last;
                        last = i;
                        lastCheck = now;
                        //Log.WriteLine(LogLevel.Debug, "TicksPerSecond: {0}", TicksPerSecond);
                        if (TicksPerSecond <= 100)
                        {
                            Log.WriteLine(LogLevel.Warn, "Server overload! Only {0} ticks per second!", TicksPerSecond);
                        }
                    }

                    if (now.Subtract(lastPing).TotalSeconds >= 30)
                    {
                        ClientManager.Instance.PingCheck();
                        lastPing = now;
                    }

                    if (now.Subtract(lastGC).TotalSeconds >= 60)
                    {
                        GC.Collect();
                        lastGC = now;
                    }

                    if (i % 2000 == 0 && MapManager.Instance != null)
                    {
                        foreach (var val in MapManager.Instance.Maps)
                        {
                            foreach (var map in val.Value)
                            {
                                map.Update(now);
                            }
                        }
                    }

                    if (i % ticksToSleep == 0) // No max load but most ticks to be parsed. Epic win!
                    {
                        Program.CurrentTime = DateTime.Now; // Laaast update
                        Thread.Sleep(sleep);
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine(LogLevel.Exception, "Ohgod. {0}", ex.ToString());
                }
            }
            Log.WriteLine(LogLevel.Info, "Server stopped handling callbacks.");
        }
    }
}
