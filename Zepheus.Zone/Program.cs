using System;
using System.Collections.Concurrent;
using System.Linq;
using Zepheus.Database;
using Zepheus.FiestaLib.Data;
using Zepheus.Util;
using Zepheus.Zone.InterServer;
using Zepheus.Zone.Networking;

namespace Zepheus.Zone
{
    class Program
    {
        public static ZoneData serviceInfo { get { return Zones[0]; } set { Zones[0] = value; } }
        public static ConcurrentDictionary<byte, ZoneData> Zones { get; set; }
        public static WorldEntity Entity { get; set; }
        public static Random Randomizer { get; set; }
        public static DateTime CurrentTime { get; set; }
        public static bool Shutdown { get; private set; }

        static void Main(string[] args)
        {
            Console.Title = "Zepheus.Zone[Registering]";
            // Lets wait a sec
            System.Threading.Thread.Sleep(2000);

            Zones = new ConcurrentDictionary<byte, ZoneData>();
            Zones.TryAdd(0, new ZoneData());

            if (Load())
            {
                // Start Worker thread.
                Worker.Load();

                while (true)
                {
                    string cmd = Console.ReadLine();
                    string[] arguments = cmd.Split(' ');
                    switch (arguments[0])
                    {
                        case "shutdown": 
                            Shutdown = true;
                            Log.WriteLine(LogLevel.Info, "Disconnecting from world.");
                            WorldConnector.Instance.Disconnect();
                            Log.WriteLine(LogLevel.Info, "Stopping client acceptor");
                            ZoneAcceptor.Instance.Stop();
                            Log.WriteLine(LogLevel.Info, "Stopping worker thread");
                            Worker.Instance.Stop();
                            Log.WriteLine(LogLevel.Info, "Disconnecting all clients");
                            ClientManager.Instance.DisconnectAll();
                            Log.WriteLine(LogLevel.Info, "Saving everything a last time");
                            Entity.SaveChanges();
                            Log.WriteLine(LogLevel.Info, "Bay.");
                            Environment.Exit(1);
                            break;
                    }
                }
            }
            else
            {
                Console.WriteLine("There was an error during load. Please press RETURN to exit.");
                Console.ReadLine();
            }
        }

        private static bool Load()
        {
            if (!Settings.Load("Zone.xml"))
            {
                CreateDefaultSettings();
                Settings.Instance.Save("Zone.xml");
            }
            Log.SetLogToFile(string.Format(@"Logs\Zone\{0}.log", DateTime.Now.ToString("yyyy-MM-dd HHmmss")));
            Randomizer = new Random();
            Log.IsDebug = Settings.Instance.Debug;

            try
            {
                if (Reflector.GetInitializerMethods().Any(method => !method.Invoke()))
                {
                    Log.WriteLine(LogLevel.Error, "Server could not be started. Errors occured.");
                    return false;
                }
                else return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "Error loading Initializer methods: {0}", ex.ToString());
                return false;
            }
        }

        private static void CreateDefaultSettings()
        {
            Settings.Instance = new Settings()
            {
                IP = "127.0.0.1",
                Debug = true,

                WorkInterval = 1,
                TransferTimeout = 10000,

                WorldServiceURI = "net.pipe://localhost/WorldService",
                InterPassword = "buttsecks",
                DataFolder = "Data",
                UseSHBD = true
            };
        }

        public static ZoneData GetZoneForMap(ushort mapid)
        {
            foreach (var v in Zones.Values)
            {
                if (v.MapsToLoad.Count(m => m.ID == mapid) > 0) return v;
            }
            return null;
        }

        public static MapInfo GetMapInfo(ushort mapid)
        {
            foreach (var v in Zones.Values)
            {
                MapInfo mi = v.MapsToLoad.Find(m => m.ID == mapid);
                if (mi != null)
                {
                    return mi;
                }
            }
            return null;
        }

        public static bool IsLoaded(ushort mapid)
        {
            try
            {
                return serviceInfo.MapsToLoad.Count(m => m.ID == mapid) > 0;
            }
            catch { return false; }
        }
    }
}
