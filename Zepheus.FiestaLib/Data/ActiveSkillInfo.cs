using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Zepheus.FiestaLib.SHN;
using Zepheus.Util;

namespace Zepheus.FiestaLib.Data
{
    public sealed class ActiveSkillInfo
    {
        public ushort ID { get; private set; }
        public string Name { get; private set; }
        public byte Step { get; private set; }
        public string Required { get; private set; }
        public ushort SP { get; private set; }
        public ushort HP { get; private set; }
        public ushort Range { get; private set; }
        public uint CoolTime { get; private set; }
        public uint CastTime { get; private set; }
        public ushort SkillAniTime { get; set; }
        public ushort MinDamage { get; private set; }
        public ushort MaxDamage { get; private set; }
        public bool IsMagic { get; private set; }
        public byte DemandType { get; private set; }
        public byte MaxTargets { get; private set; }

        public static ActiveSkillInfo Load(DataTableReaderEx reader)
        {
            ActiveSkillInfo inf = new ActiveSkillInfo
            {
                ID = reader.GetUInt16("ID"),
                Name = reader.GetString("InxName"),
                Step = reader.GetByte("Step"),
                Required = reader.GetString("DemandSk"),
                SP = reader.GetUInt16("SP"),
                HP = reader.GetUInt16("HP"),
                Range = reader.GetUInt16("Range"),
                CoolTime = reader.GetUInt32("DlyTime"),
                CastTime = reader.GetUInt32("CastTime"),
                DemandType = reader.GetByte("DemandType"),
                MaxTargets = reader.GetByte("TargetNumber"),
            };

            ushort maxdamage = (ushort)reader.GetUInt32("MaxWC");
            if (maxdamage == 0)
            {
                inf.IsMagic = true;
                inf.MinDamage = (ushort)reader.GetUInt32("MinMA");
                inf.MaxDamage = (ushort)reader.GetUInt32("MaxMA");
            }
            else
            {
                inf.MaxDamage = maxdamage;
                inf.MinDamage = (ushort)reader.GetUInt32("MinWC");
            }
            return inf;
        }
    }
}
