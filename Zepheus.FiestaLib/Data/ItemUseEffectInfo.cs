using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Zepheus.Util;
using Zepheus.FiestaLib.SHN;

namespace Zepheus.FiestaLib.Data
{
    public sealed class ItemUseEffectInfo
    {
        public ushort ID { get;  set; }
        public string AbState { get; private set; }
        public List<ItemEffect> Effects { get; private set; }

        public ItemUseEffectInfo()
        {
            Effects = new List<ItemEffect>();
        }

        public static ItemUseEffectInfo Load(DataTableReaderEx reader, out string InxName)
        {
            ItemUseEffectInfo info = new ItemUseEffectInfo();
            InxName = reader.GetString("ItemIndex");

            ItemUseEffectType typeA = (ItemUseEffectType)reader.GetUInt32("UseEffectA");
            if (typeA != ItemUseEffectType.None)
            {
                ItemEffect effect = new ItemEffect();
                effect.Type = typeA;
                effect.Value = reader.GetUInt32("UseValueA");
                info.Effects.Add(effect);
            }

            ItemUseEffectType typeB = (ItemUseEffectType)reader.GetUInt32("UseEffectB");
            if (typeB != ItemUseEffectType.None)
            {
                ItemEffect effect = new ItemEffect();
                effect.Type = typeB;
                effect.Value = reader.GetUInt32("UseValueB");
                info.Effects.Add(effect);
            }

            ItemUseEffectType typeC = (ItemUseEffectType)reader.GetUInt32("UseEffectC");
            if (typeC != ItemUseEffectType.None)
            {
                ItemEffect effect = new ItemEffect();
                effect.Type = typeC;
                effect.Value = reader.GetUInt32("UseValueC");
                info.Effects.Add(effect);
            }
            info.AbState = reader.GetString("UseAbStateName");
            return info;
        }
    }

    public struct ItemEffect
    {
        public ItemUseEffectType Type { get; set; }
        public uint Value { get; set; }
    }

    public enum ItemUseEffectType : byte
    {
        HP = 0,
        SP = 1,
        AbState = 4,
        ScrollTier = 5,
        None = 6,
    }
}
