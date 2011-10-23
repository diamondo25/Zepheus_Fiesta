using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zepheus.FiestaLib.Data
{
        public sealed class SpawnNPCPoint
        {
            public ushort ID { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public byte Rotation { get; set; }
            public LinkTable Gate { get; set; }
        }

        public sealed class GateInfo
        {
            public string Name { get; set; }
            public string ToMap { get; set; }
            public int ToX { get; set; }
            public int ToY { get; set; }
        }
}
