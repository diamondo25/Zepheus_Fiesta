using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Zepheus.FiestaLib.SHN;
using Zepheus.Util;

namespace Zepheus.FiestaLib.Data
{
    public sealed class MobInfo
    {
        public string Name { get; private set; }
        public ushort ID { get; private set; }
        public byte Level { get; private set; }
        public uint MaxHP { get; private set; }
        public ushort RunSpeed { get; private set; }
        public bool IsNPC { get; private set; }
        public bool IsAggro { get; private set; }
        public byte Type { get; private set; }
        public ushort Size { get; private set; }

        public List<DropInfo> Drops { get; private set; }

        public byte MinDropLevel { get; set; }
        public byte MaxDropLevel { get; set; }

        public static MobInfo Load(DataTableReaderEx reader)
        {
            MobInfo inf = new MobInfo
            {
                Name = reader.GetString("InxName"),
                ID = reader.GetUInt16("ID"),
                Level = (byte)reader.GetUInt32("Level"),
                MaxHP = reader.GetUInt32("MaxHP"),
                RunSpeed = (ushort)reader.GetUInt32("RunSpeed"),
                IsNPC = Convert.ToBoolean(reader.GetByte("IsNPC")),
                Size = (ushort)reader.GetUInt32("Size"),
                Type = (byte)reader.GetUInt32("Type"),
                IsAggro = Convert.ToBoolean(reader.GetByte("IsPlayerSide")),
                Drops = new List<DropInfo>()
            };
            return inf;
        }
    }
}
