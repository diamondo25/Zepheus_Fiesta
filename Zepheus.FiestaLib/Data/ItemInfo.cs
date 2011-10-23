using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Zepheus.Util;
using Zepheus.FiestaLib.SHN;

namespace Zepheus.FiestaLib.Data
{
    public sealed class ItemInfo
    {
        public ushort ItemID { get; private set; }
        public ItemSlot Slot { get; private set; }
        public bool TwoHand { get; private set; }
        public string InxName { get; private set; }
        public byte MaxLot { get; private set; }
        public ushort AttackSpeed { get; private set; }
        public byte Level { get; private set; }
        public ItemType Type { get; private set; }
        public ItemClass Class { get; private set; }
        public byte UpgradeLimit { get; private set; }
        public Job Jobs { get; private set; }
        public ushort MinMagic { get; private set; }
        public ushort MaxMagic { get; private set; }
        public ushort MinMelee { get; private set; }
        public ushort MaxMelee { get; private set; }
        public ushort WeaponDef { get; private set; }
        public ushort MagicDef { get; private set; }

        //item upgrade
        public ushort UpSucRation { get; private set; }
        public ushort UpResource { get; private set; }

        /// <summary>
        /// Needs serious fixing in the reader, as it throws invalid casts (files all use uint, but fuck those)
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static ItemInfo Load(DataTableReaderEx reader)
        {
            ItemInfo itemInfo = new ItemInfo
            {
                ItemID = reader.GetUInt16("id"),
                Slot = (ItemSlot)reader.GetUInt32("equip"),
                InxName = reader.GetString("inxname"),
                MaxLot = (byte)reader.GetUInt32("maxlot"),
                AttackSpeed = (ushort)reader.GetUInt32("atkspeed"),
                Level = (byte)reader.GetUInt32("demandlv"),
                Type = (ItemType)reader.GetUInt32("type"),
                Class = (ItemClass)reader.GetUInt32("class"),
                UpgradeLimit = reader.GetByte("uplimit"),
                Jobs = UnpackWhoEquip(reader.GetUInt32("whoequip")),
                TwoHand = reader.GetBoolean("TwoHand"),
                MinMagic = (ushort)reader.GetUInt32("minma"),
                MaxMagic = (ushort)reader.GetUInt32("maxma"),
                MinMelee = (ushort)reader.GetUInt32("minwc"),
                MaxMelee = (ushort)reader.GetUInt32("maxwc"),
                WeaponDef = (ushort)reader.GetUInt32("ac"),
                MagicDef = (ushort)reader.GetUInt32("mr"),
                UpSucRation = reader.GetUInt16("UpSucRatio"),
                UpResource = reader.GetByte("UpResource")
            };
            return itemInfo;
        }

       // [Obsolete("Too slow / incorrect?")]
        private static Job UnpackWhoEquip(uint value)
        {
            Job job = Job.None;
          //  string jobnames = "";
            for (int i = 0; i < 26; i++)
            {
                if ((value & (uint)Math.Pow(2, i)) != 0)
                {
                    job |= (Job)i;
            //        jobnames += ((Job)i).ToString() + " ";
                }
            }
            return job;
        }
    }

    public enum ItemType : byte
    {
        Equip = 0,
        Useable = 1,
        Etc = 2,
        Unknown = 3,
        Unknown2 = 5,
    }

    public enum ItemClass : byte
    {
        PremiumItem = 6,
        Shield = 7,
        BootsHelmet = 8,
        Furniture = 9,
        Accessory = 10,
        Skillbook = 11,
        ReturnScroll = 12,
        SilverWingsOnly = 13, // Csharp note: lelijk
        CraftStones = 14,
        PresentBox = 15,
        House = 18,
        RiderFood = 22,
        Rider = 23,
        Crafting = 24,
        Overlay = 26,
        Emotion = 27,

    }
}
