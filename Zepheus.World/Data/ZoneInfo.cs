using System.Collections.Generic;
using Zepheus.FiestaLib.Data;

namespace Zepheus.World.Data
{
    public sealed class ZoneInfo
    {
        public byte ID { get; set; }
        public ushort Port { get; set; }
        public string IP { get; set; }
        public List<MapInfo> Maps { get; set; }
    }
}
