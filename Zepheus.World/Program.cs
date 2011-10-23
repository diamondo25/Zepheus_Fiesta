using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Zepheus.Database;
using Zepheus.Util;

using Zepheus.World.InterServer;

namespace Zepheus.World
{
    class Program
    {
        public static bool Maintenance { get; set; }
        public static WorldEntity Entity { get; set; }
        public static ConcurrentDictionary<byte, ZoneConnection> Zones { get; private set; }

        static void Main(string[] args)
        {
            Console.Title = "Zepheus.World";
#if DEBUG
            Thread.Sleep(980);//give loginserver some time to start.
            //File.Delete("World.xml");
#endif

            if (Load())
            {
                Log.IsDebug = Settings.Instance.Debug;
                Zones = new ConcurrentDictionary<byte, ZoneConnection>();
                Console.Title = "Zepheus.World[" + Settings.Instance.ID + "]";

                while (true)
                {
                    string line = Console.ReadLine();
                    try
                    {
                        HandleCommand(line);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(LogLevel.Exception, "Could not parse: {0}; Error: {1}", line, ex.ToString());
                    }
                }
            }
            else
            {
                Log.WriteLine(LogLevel.Error, "Errors occured starting server. Press RETURN to exit.");
                Console.ReadLine();
            }
        }

        public static ZoneConnection GetZoneByMap(ushort id)
        {
            try
            {
                return Zones.Values.First(z => z.Maps.Count(m => m.ID == id) > 0);
            }
            catch
            {
                Log.WriteLine(LogLevel.Exception, "No zones are active at the moment.");
                return null;
            }
        }

        public static void HandleCommand(string line)
        {
            string[] command = line.Split(' ');
            switch (command[0].ToLower())
            {
                case "maintenance":
                    if (command.Length >= 2)
                    {
                        Program.Maintenance = bool.Parse(command[1]);
                    }
                    break;

                default:
                    Console.WriteLine("Command not recognized.");
                    break;
            }
        }

        public static bool Load()
        {
            if (!Settings.Load("World.xml"))
            {
                CreateDefaultSettings();
                Settings.Instance.Save("World.xml");
            }

            Log.SetLogToFile(string.Format(@"Logs\World\{0}.log", DateTime.Now.ToString("d_M_yyyy HH_mm_ss")));

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
                Log.WriteLine(LogLevel.Exception, "Fatal exception while load: {0}:{1}", ex.ToString(), ex.StackTrace);
                return false;
            }
        }

        public static byte GetFreeZoneID()
        {
            for (byte i = 0; i < 3; i++)
            {
                if (Zones.ContainsKey(i)) continue;
                return i;
            }
            return 255;
        }

        private static void CreateDefaultSettings()
        {
            Settings.Instance = new Settings()
            {
                Port = 9110,
                ZoneBasePort = 9210,
                ZoneCount = 3,
                IP = "127.0.0.1",
                Debug = true,

                WorkInterval = 1,
                TransferTimeout = 30000,

                WorldName = "Teva",
                ID = 0,

                LoginServiceURI = "net.pipe://localhost/LoginService",
                WorldServiceURI = "net.pipe://localhost/WorldService",
                GameServiceURI = "net.pipe://localhost/GameService",
                InterPassword = "buttsecks",
                DataFolder = "Data",
            };


            Settings.Instance.Entity = new EntitySetting()
            {
                DataCatalog = "Zepheus_World",
                DataSource = @"CSHARP-PC\SQLEXPRESS",
                Metadata = @"res://*/World.csdl|res://*/World.ssdl|res://*/World.msl",
            };
        }
    }
}
