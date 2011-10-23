using System;
using Zepheus.Util;

namespace Zepheus.FiestaLib.Data
{
    public sealed class FiestaBaseStat
    {
        public Job Job { get; private set; }
        public Int32 Level { get; private set; }
        public Int32 Strength { get; private set; }
        public Int32 Endurance { get; private set; }
        public Int32 Intelligence { get; private set; }
        public Int32 Dexterity { get; private set; }
        public Int32 Spirit { get; private set; }
        public Int32 SoulHP { get; private set; }
        public Int32 MAXSoulHP { get; private set; }
        public Int32 PriceHPStone { get; private set; }
        public Int32 SoulSP { get; private set; }
        public Int32 MAXSoulSP { get; private set; }
        public Int32 PriceSPStone { get; private set; }
        public Int32 AtkPerAP { get; private set; }
        public Int32 DmgPerAP { get; private set; }
        public Int32 MaxPwrStone { get; private set; }
        public Int32 NumPwrStone { get; private set; }
        public Int32 PricePwrStone { get; private set; }
        public Int32 PwrStoneWC { get; private set; }
        public Int32 PwrStoneMA { get; private set; }
        public Int32 MaxGrdStone { get; private set; }
        public Int32 NumGrdStone { get; private set; }
        public Int32 PriceGrdStone { get; private set; }
        public Int32 GrdStoneAC { get; private set; }
        public Int32 GrdStoneMR { get; private set; }
        public Int32 PainRes { get; private set; }
        public Int32 RestraintRes { get; private set; }
        public Int32 CurseRes { get; private set; }
        public Int32 ShockRes { get; private set; }
        public UInt32 MaxHP { get; private set; }
        public UInt32 MaxSP { get; private set; }
        public Int32 CharTitlePt { get; private set; }
        public Int32 SkillPwrPt { get; private set; }

        public static FiestaBaseStat Load(DataTableReaderEx reader, Job job)
        {
            FiestaBaseStat info = new FiestaBaseStat
            {
                Job = job,
                Level = reader.GetInt32("Level"),
                Strength = reader.GetInt32("Strength"),
                Endurance = reader.GetInt32("Constitution"),
                Intelligence = reader.GetInt32("Intelligence"),
                Dexterity = reader.GetInt32("Dexterity"),
                Spirit = reader.GetInt32("MentalPower"),
                SoulHP = reader.GetInt32("SoulHP"),
                MAXSoulHP = reader.GetInt32("MAXSoulHP"),
                PriceHPStone = reader.GetInt32("PriceHPStone"),
                SoulSP = reader.GetInt32("SoulSP"),
                MAXSoulSP = reader.GetInt32("MAXSoulSP"),
                PriceSPStone = reader.GetInt32("PriceSPStone"),
                AtkPerAP = reader.GetInt32("AtkPerAP"),
                DmgPerAP = reader.GetInt32("DmgPerAP"),
                MaxPwrStone = reader.GetInt32("MaxPwrStone"),
                NumPwrStone = reader.GetInt32("NumPwrStone"),
                PricePwrStone = reader.GetInt32("PricePwrStone"),
                PwrStoneWC = reader.GetInt32("PwrStoneWC"),
                PwrStoneMA = reader.GetInt32("PwrStoneMA"),
                MaxGrdStone = reader.GetInt32("MaxGrdStone"),
                NumGrdStone = reader.GetInt32("NumGrdStone"),
                PriceGrdStone = reader.GetInt32("PriceGrdStone"),
                GrdStoneAC = reader.GetInt32("GrdStoneAC"),
                GrdStoneMR = reader.GetInt32("GrdStoneMR"),
                PainRes = reader.GetInt32("PainRes"),
                RestraintRes = reader.GetInt32("RestraintRes"),
                CurseRes = reader.GetInt32("CurseRes"),
                ShockRes = reader.GetInt32("ShockRes"),
                MaxHP = (UInt32)reader.GetInt16("MaxHP"),
                MaxSP = (UInt32)reader.GetInt16("MaxSP"),
                CharTitlePt = reader.GetInt32("CharTitlePt"),
                SkillPwrPt = reader.GetInt32("SkillPwrPt"),
            };
            return info;
        }
    }
}
