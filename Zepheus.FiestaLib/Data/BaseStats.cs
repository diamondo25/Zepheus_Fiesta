using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;

using Zepheus.Util;

namespace Zepheus.FiestaLib.Data
{
    public sealed class BaseStatsEntry
    {
        public byte Level { get; set; }
        public ushort Str { get; set; }
        public ushort End { get; set; }
        public ushort Dex { get; set; }
        public ushort Int { get; set; }
        public ushort Spr { get; set; }
        public ushort MaxHPStones { get; set; }
        public ushort MaxSPStones { get; set; }
        public ushort MaxHP { get; set; }
        public ushort MaxSP { get; set; }
    }

    //to update player stats, have to find out more later
    public enum StatsByte : byte
    {
        MinMelee = 0x06,
        MaxMelee = 0x07,
        WDef = 0x08,

        Aim = 0x09,
        Evasion = 0x0a,

        MinMagic = 0x0b,
        MaxMagic = 0x0c,
        MDef = 0x0d,

        StrBonus = 0x13,
        EndBonus = 0x19
    }

    public sealed class BaseStats
    {
        /*
         * 
        public static int GetStatValue(WorldCharacter pCharacter, StatsByte pByte)
        {
            switch (pByte)
            {
                case StatsByte.MinMelee:
                    return pCharacter.MinDamage;
                case StatsByte.MaxMelee:
                    return pCharacter.MaxDamage;
                case StatsByte.MinMagic:
                    return pCharacter.MinMagic;
                case StatsByte.MaxMagic:
                    return pCharacter.MaxMagic;
                case StatsByte.WDef:
                    return pCharacter.WeaponDef;
                case StatsByte.MDef:
                    return pCharacter.MagicDef;
                case StatsByte.Aim:
                    return 5; //TODO load additional equip stats
                case StatsByte.Evasion:
                    return 5;
                case StatsByte.StrBonus:
                    return pCharacter.StrBonus;
                case StatsByte.EndBonus:
                    return pCharacter.EndBonus;
                default:
                    return 0;
            }
        }
        */

        public BaseStatsEntry this[byte level] {
            get
            {
                if (entries.ContainsKey(level))
                    return entries[level];
                else return null;
            }
        }
        public Job Job { get; set; }
        public SerializableDictionary<byte, BaseStatsEntry> entries = new SerializableDictionary<byte, BaseStatsEntry>();

        public BaseStats()
        {

        }

        public BaseStats(Job pJob)
        {
            this.Job = pJob;
        }

        public bool GetEntry(byte pLevel, out BaseStatsEntry pEntry)
        {
            return this.entries.TryGetValue(pLevel, out pEntry);
        }

        public static bool TryLoad(string pFile, out BaseStats pStats)
        {
            pStats = new BaseStats();
            try
            {
                using (var file = File.Open(pFile, FileMode.Open))
                {
                    XmlSerializer xser = new XmlSerializer(typeof(BaseStats));
                    pStats = (BaseStats)xser.Deserialize(file);
                   // Log.WriteLine(LogLevel.Info, "Job {0} loaded! Data for {1} levels.", pStats.Job.ToString(), pStats.entries.Count);
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "Exception while loading stats from job {0}: {1}", pFile, ex.ToString());
                return false;
            }
        }
    }
}
