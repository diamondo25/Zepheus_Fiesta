using System.IO;
using System.Xml.Serialization;

using Zepheus.Util;

namespace Zepheus.World
{
    public sealed class Settings
    {
        public const int SettingsVersion = 2;

        public int? Version { get; set; }
        public string WorldName { get; set; }
        public byte ID { get; set; }
        public string IP { get; set; }
        public ushort Port { get; set; }
        public ushort ZoneBasePort { get; set; }
        public ushort ZoneCount { get; set; }

        public int TransferTimeout { get; set; }
        public bool Debug { get; set; }
        public int WorkInterval { get; set; }

        public string LoginServiceURI { get; set; }
        public string WorldServiceURI { get; set; }
        public string GameServiceURI { get; set; }
        public string InterPassword { get; set; }
        public string LoginServerIP { get; set; }
        public ushort LoginServerPort { get; set; }
        public ushort InterServerPort { get; set; }

        public string DataFolder { get; set; }
        public bool ShowEquips { get; set; }

        public EntitySetting Entity { get; set; }

        public static Settings Instance { get; set; }
        public void Save(string path)
        {
            using (var file = File.Create(path))
            {
                XmlSerializer xser = new XmlSerializer(typeof(Settings));
                xser.Serialize(file, this);
            }
        }

        public static bool Load(string path)
        {
            if (!File.Exists(path)) return false;
            Settings obj;
            try
            {
                using (var file = File.Open(path, FileMode.Open))
                {
                    XmlSerializer xser = new XmlSerializer(typeof(Settings));
                    obj = (Settings)xser.Deserialize(file);
                }
                if (!obj.Version.HasValue || obj.Version.Value != SettingsVersion)
                {
                    if (!obj.Version.HasValue)
                    {
                        // V.1
                        obj.LoginServerIP = "127.0.0.1";
                        obj.LoginServerPort = 10000;
                        obj.InterServerPort = 11000;
                    }

                    obj.Version = SettingsVersion;
                    obj.Save(path);
                }
                Settings.Instance = obj;
                return true;
            }
            catch
            {
                return false;
            }

        }
    }
}
