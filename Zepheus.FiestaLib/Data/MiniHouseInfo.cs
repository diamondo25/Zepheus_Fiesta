using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Zepheus.FiestaLib.SHN;
using Zepheus.Util;

namespace Zepheus.FiestaLib.Data
{
    public sealed class MiniHouseInfo
    {
        public ushort ID { get; private set; }
        public ushort KeepTime_Hour { get; private set; }
        public ushort HPTick { get; private set; }
        public ushort SPTick { get; private set; }
        public ushort HPRecovery { get; private set; }
        public ushort SPRecovery { get; private set; }

        // public int Slot { get; set; } // No idea, only 5 or 10
        // public string Name { get; set; } // Not needed for now
        // public ushort CastTime { get; set; } // Not needed for now

        public MiniHouseInfo(DataTableReaderEx reader)
        {
            ID = reader.GetUInt16("Handle");
            KeepTime_Hour = reader.GetUInt16("KeepTime_Hour");
            HPTick = reader.GetUInt16("HPTick");
            SPTick = reader.GetUInt16("SPTick");
            HPRecovery = reader.GetUInt16("HPRecovery");
            SPRecovery = reader.GetUInt16("SPRecovery");
        }
    }
}
