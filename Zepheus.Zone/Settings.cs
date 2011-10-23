using System.IO;
using System.Xml.Serialization;


namespace Zepheus.Zone
{
    public sealed class Settings
    {
        public const int SettingsVersion = 2;

        public int? Version { get; set; }
        public string IP { get; set; }
        public bool Debug { get; set; }
        public int WorkInterval { get; set; }
        public int TransferTimeout { get; set; }

        public string WorldServiceURI { get; set; }
        public string InterPassword { get; set; }
        public string WorldServerIP { get; set; }
        public ushort WorldServerPort { get; set; }
        public ushort InterServerPort { get; set; }

        public string DataFolder { get; set; }
        public bool UseSHBD { get; set; }

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
                        obj.WorldServerIP = "127.0.0.1";
                        obj.WorldServerPort = 11000;
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
