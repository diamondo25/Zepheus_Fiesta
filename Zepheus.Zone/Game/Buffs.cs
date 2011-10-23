using System;
using System.Collections.Generic;

using Zepheus.FiestaLib.Data;

namespace Zepheus.Zone.Game
{
    public class Buff
    {
        public DateTime Ends { get; set; }
        public ZoneCharacter Characte { get; set; }
        public Dictionary<StatsByte, int> Modifiers { get; set; }
    }


    public class Buffs
    {
        private ZoneCharacter _character {get;set;}

        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public int MinMagic { get; set; }
        public int MaxMagic { get; set; }
        public int WeaponDefense { get; set; }
        public int WeaponDamage { get; set; }
        public int MagicDefense { get; set; }
        public int MagicDamage { get; set; }
        public int Evasion { get; set; }
        public int Str { get; set; }
        public int End { get; set; }
        public int Dex { get; set; }
        public int Int { get; set; }
        public int Spr { get; set; }
        public int MaxHP { get; set; }
        public int MaxSP { get; set; }

        private List<Buff> _currentBuffs { get; set; }

        public Buffs(ZoneCharacter pChar)
        {
            _character = pChar;
            _currentBuffs = new List<Buff>();
        }


    }
}
