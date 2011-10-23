using System;
using Zepheus.Util;

namespace Zepheus.FiestaLib.Data
{
    public sealed class MobInfoServer
    {
        public UInt32 ID { get; private set; }
        public String InxName { get; private set; }
        public Byte Visible { get; private set; }
        public UInt16 AC { get; private set; }
        public UInt16 TB { get; private set; }
        public UInt16 MR { get; private set; }
        public UInt16 MB { get; private set; }
        public UInt32 EnemyDetectType { get; private set; }
        public UInt32 MobKillInx { get; private set; }
        public UInt32 MonEXP { get; private set; }
        public UInt16 EXPRange { get; private set; }
        public UInt16 DetectCha { get; private set; }
        public Byte ResetInterval { get; private set; }
        public UInt16 CutInterval { get; private set; }
        public UInt32 CutNonAT { get; private set; }
        public UInt32 FollowCha { get; private set; }
        public UInt16 PceHPRcvDly { get; private set; }
        public UInt16 PceHPRcv { get; private set; }
        public UInt16 AtkHPRcvDly { get; private set; }
        public UInt16 AtkHPRcv { get; private set; }
        public UInt16 Str { get; private set; }
        public UInt16 Dex { get; private set; }
        public UInt16 Con { get; private set; }
        public UInt16 Int { get; private set; }
        public UInt16 Men { get; private set; }
        public UInt32 MobRaceType { get; private set; }
        public Byte Rank { get; private set; }
        public UInt32 FamilyArea { get; private set; }
        public UInt32 FamilyRescArea { get; private set; }
        public Byte FamilyRescCount { get; private set; }
        public UInt16 BloodingResi { get; private set; }
        public UInt16 StunResi { get; private set; }
        public UInt16 MoveSpeedResi { get; private set; }
        public UInt16 FearResi { get; private set; }
        public String ResIndex { get; private set; }
        public UInt16 KQKillPoint { get; private set; }
        public Byte Return2Regen { get; private set; }
        public Byte IsRoaming { get; private set; }
        public Byte RoamingNumber { get; private set; }
        public UInt16 RoamingDistance { get; private set; }
        public UInt16 MaxSP { get; private set; }
        public Byte BroadAtDead { get; private set; }
        public UInt16 TurnSpeed { get; private set; }
        public UInt16 WalkChase { get; private set; }
        public Byte AllCanLoot { get; private set; }
        public UInt16 DmgByHealMin { get; private set; }
        public UInt16 DmgByHealMax { get; private set; }

        public static MobInfoServer Load(DataTableReaderEx reader)
        {
            MobInfoServer info = new MobInfoServer
            {
                ID = reader.GetUInt32("ID"),
                InxName = reader.GetString("InxName"),
                Visible = reader.GetByte("Visible"),
                AC = reader.GetUInt16("AC"),
                TB = reader.GetUInt16("TB"),
                MR = reader.GetUInt16("MR"),
                MB = reader.GetUInt16("MB"),
                EnemyDetectType = reader.GetUInt32("EnemyDetectType"),
                MobKillInx = reader.GetUInt32("MobKillInx"),
                MonEXP = reader.GetUInt32("MonEXP"),
                EXPRange = reader.GetUInt16("EXPRange"),
                DetectCha = reader.GetUInt16("DetectCha"),
                ResetInterval = reader.GetByte("ResetInterval"),
                CutInterval = reader.GetUInt16("CutInterval"),
                CutNonAT = reader.GetUInt32("CutNonAT"),
                FollowCha = reader.GetUInt32("FollowCha"),
                PceHPRcvDly = reader.GetUInt16("PceHPRcvDly"),
                PceHPRcv = reader.GetUInt16("PceHPRcv"),
                AtkHPRcvDly = reader.GetUInt16("AtkHPRcvDly"),
                AtkHPRcv = reader.GetUInt16("AtkHPRcv"),
                Str = reader.GetUInt16("Str"),
                Dex = reader.GetUInt16("Dex"),
                Con = reader.GetUInt16("Con"),
                Int = reader.GetUInt16("Int"),
                Men = reader.GetUInt16("Men"),
                MobRaceType = reader.GetUInt32("MobRaceType"),
                Rank = reader.GetByte("Rank"),
                FamilyArea = reader.GetUInt32("FamilyArea"),
                FamilyRescArea = reader.GetUInt32("FamilyRescArea"),
                FamilyRescCount = reader.GetByte("FamilyRescCount"),
                BloodingResi = reader.GetUInt16("BloodingResi"),
                StunResi = reader.GetUInt16("StunResi"),
                MoveSpeedResi = reader.GetUInt16("MoveSpeedResi"),
                FearResi = reader.GetUInt16("FearResi"),
                ResIndex = reader.GetString("ResIndex"),
                KQKillPoint = reader.GetUInt16("KQKillPoint"),
                Return2Regen = reader.GetByte("Return2Regen"),
                IsRoaming = reader.GetByte("IsRoaming"),
                RoamingNumber = reader.GetByte("RoamingNumber"),
                RoamingDistance = reader.GetUInt16("RoamingDistance"),
                MaxSP = reader.GetUInt16("MaxSP"),
                BroadAtDead = reader.GetByte("BroadAtDead"),
                TurnSpeed = reader.GetUInt16("TurnSpeed"),
                WalkChase = reader.GetUInt16("WalkChase"),
                AllCanLoot = reader.GetByte("AllCanLoot"),
                DmgByHealMin = reader.GetUInt16("DmgByHealMin"),
                DmgByHealMax = reader.GetUInt16("DmgByHealMax"),
            };
            return info;
        }
    }
}
