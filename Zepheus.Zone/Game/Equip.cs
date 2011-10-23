using System;
using Zepheus.Database;
using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Networking;
using Zepheus.Zone.Data;

namespace Zepheus.Zone.Game
{
    public sealed class Equip : Item
    {
        private DatabaseEquip equip;
        public int ID { get { return equip.ID; } }
        public override short Amount { get { return 1; } }
        public override DateTime? Expires { get { return equip.Expires; } set { equip.Expires = value; } }
        public byte Dex { get { return equip.IncDex; } set { equip.IncDex = value; } }
        public byte Str { get { return equip.IncStr; } set { equip.IncStr = value; } }
        public byte End { get { return equip.IncEnd; } set { equip.IncEnd = value; } }
        public byte Int { get { return equip.IncInt; } set { equip.IncInt = value; } }
        public byte Spr { get { return equip.IncSpr; } set { equip.IncSpr = value; } }
        public byte Upgrades { get { return equip.Upgrades; } set { equip.Upgrades = value; } }
        public override sbyte Slot { get { return (sbyte)equip.Slot; } set { equip.Slot = (sbyte)value; } }
        public override Character Owner { get { return equip.Character; } set { equip.Character = value; } }
        public ItemSlot SlotType
        {
            get
            {
                return DataProvider.Instance.GetItemInfo(ItemID).Slot;
            }
        }
        public bool IsEquipped { get { return Slot < 0; } set { Slot = value ? (sbyte)(-1 * Math.Abs(Slot)) : Math.Abs(Slot); } }

        public Equip(DatabaseEquip eqp)
        {
            equip = eqp;
            ItemID = (ushort)eqp.EquipID;
        }

        public Equip(DroppedEquip pBase, ZoneCharacter pNewOwner, sbyte pSlot)
        {
            DatabaseEquip dbeq = new DatabaseEquip();
            dbeq.IncDex = pBase.Dex;
            dbeq.IncStr = pBase.Str;
            dbeq.IncEnd = pBase.End;
            dbeq.IncInt = pBase.Int;
            dbeq.IncSpr = pBase.Spr;
            dbeq.Upgrades = pBase.Upgrades;
            dbeq.EquipID = pBase.ItemID;
            dbeq.Slot = pSlot;
            dbeq.Character = pNewOwner.character;

            Program.Entity.AddToDatabaseEquips(dbeq);
            equip = dbeq;
            ItemID = (ushort)dbeq.EquipID;
            pNewOwner.InventoryItems.Add(pSlot, this);
            pNewOwner.Save();
        }

        public byte CalculateDataLen()
        {
            byte length = 0;
            switch (this.SlotType)
            {
                case ItemSlot.Weapon:
                    length = 53;                // Base data length
                    break;
                case ItemSlot.Weapon2:
                    if (Info.TwoHand)
                    {
                        //bow
                        length = 53;
                    }
                    else
                    {
                        //shield
                        length = 16;                // Base data length
                    }
                    break;
                case ItemSlot.Helm:
                case ItemSlot.Armor:
                case ItemSlot.Pants:
                case ItemSlot.Boots:
                    length = 16;                // Base data length
                    break;
                case ItemSlot.Necklace:
                case ItemSlot.Earings:
                case ItemSlot.Ring:
                    length = 26;                // Base data length
                    break;
                case ItemSlot.CostumeWeapon:
                case ItemSlot.CostumeShield:
                    length = 8;                 // Base data length
                    break;
                case ItemSlot.Pet:
                    length = 16;                // Base data length
                    break;
                default:
                    length = 12;                // Base data length
                    break;
            }
            return length;
        }

        public override void WriteInfo(Packet packet)
        {
            byte StatCount = 0;
            if (Str > 0) StatCount++;
            if (End > 0) StatCount++;
            if (Dex > 0) StatCount++;
            if (Spr > 0) StatCount++;
            if (Int > 0) StatCount++;

            byte length = CalculateDataLen();
            length += (byte)(StatCount * 3);    // Stat data length
            packet.WriteByte(length);
            packet.WriteByte((byte)Math.Abs(this.Slot));
            packet.WriteByte(IsEquipped ? (byte)0x20 : (byte)0x24);
            WriteEquipStats(packet);
        }

        private uint GetExpiringTime()
        {
            if (Expires == null)
            {
                return 0;
            }
            else
            {
                return Expires.Value.ToFiestaTime();
            }
        }

        public override void Remove()
        {
            if (equip != null)
            {
                Program.Entity.DeleteObject(equip);
                Program.Entity.SaveChanges();
            }
        }

        public void WriteEquipStats(Packet packet)
        {
            byte StatCount = 0;
            if (Str > 0) StatCount++;
            if (End > 0) StatCount++;
            if (Dex > 0) StatCount++;
            if (Spr > 0) StatCount++;
            if (Int > 0) StatCount++;

            packet.WriteUShort(ItemID);
            switch (this.SlotType)
            {
                case ItemSlot.Helm:
                case ItemSlot.Armor:
                case ItemSlot.Pants:
                case ItemSlot.Boots:
                // case ItemSlot.Bow: // Shield = same
                case ItemSlot.Weapon2:
                case ItemSlot.Weapon:
                    packet.WriteByte(this.Upgrades);   // Refinement
                    packet.WriteByte(0);
                    packet.WriteShort(0); // Or int?
                    packet.WriteShort(0);
                    if (this.SlotType == ItemSlot.Weapon || (this.SlotType == ItemSlot.Weapon2 && Info.TwoHand))
                    {
                        packet.WriteByte(0);
                        // Licence data
                        packet.WriteUShort(0xFFFF);    // Nr.1 - Mob ID
                        packet.WriteUInt(0);           // Nr.1 - Kill count
                        packet.WriteUShort(0xFFFF);    // Nr.2 - Mob ID
                        packet.WriteUInt(0);           // Nr.2 - Kill count
                        packet.WriteUShort(0xFFFF);    // Nr.3 - Mob ID
                        packet.WriteUInt(0);           // Nr.3 - Kill count
                        packet.WriteUShort(0xFFFF);        // UNK
                        packet.WriteString("", 16);    // First licence adder name
                    }
                    packet.WriteByte(0);
                    packet.WriteUInt(GetExpiringTime());               // Expiring time (1992027391 -  never expires)
                    //packet.WriteShort(0);
                    break;
                case ItemSlot.Pet:
                    packet.WriteByte(this.Upgrades);   // Pet Refinement Lol
                    packet.Fill(2, 0);                     // UNK
                    packet.WriteUInt(GetExpiringTime());               // Expiring time (1992027391 -  never expires)
                    packet.WriteUInt(0);               // Time? (1992027391 -  never expires)
                    break;
                case ItemSlot.Earings:
                case ItemSlot.Necklace:
                case ItemSlot.Ring:
                    packet.WriteUInt(GetExpiringTime());               // Expiring time (1992027391 -  never expires)
                    packet.WriteUInt(0);               // Time? (1992027391 -  never expires)
                    packet.WriteByte(this.Upgrades);   // Refinement
                    // Stats added while refining
                    packet.WriteUShort(0);             // it may be byte + byte too (some kind of counter when item downgrades)
                    packet.WriteUShort(0);             // STR
                    packet.WriteUShort(0);             // END
                    packet.WriteUShort(0);             // DEX
                    packet.WriteUShort(0);             // INT
                    packet.WriteUShort(0);             // SPR
                    break;
                case ItemSlot.CostumeWeapon:
                case ItemSlot.CostumeShield:
                    packet.WriteUInt(25000);           // Skin Durability
                    break;
                default:
                    packet.WriteUInt(GetExpiringTime());      // Expiring time (1992027391 -  never expires)
                    packet.WriteUInt(0);                        // Time? (1992027391 -  never expires)
                    break;
            }

            // Random stats data (Not those what were added in refinement)
            switch (this.SlotType)
            {                       // Stat count (StatCount << 1 | Visible(0 or 1 are stats shown or not))
                case ItemSlot.Earings:
                    packet.WriteByte((byte)(StatCount << 1 | 1));
                    break;
                case ItemSlot.Necklace:
                case ItemSlot.Ring:
                    packet.WriteByte((byte)(StatCount << 1 | 1));
                    break;
                case ItemSlot.Helm:
                case ItemSlot.Armor:
                case ItemSlot.Pants:
                case ItemSlot.Boots:
                case ItemSlot.Weapon2:
                case ItemSlot.Weapon:
                    packet.WriteByte((byte)(StatCount << 1 | 1));
                    break;
                case ItemSlot.Pet:          // Yes!! Its possible to give stats to pet also (It overrides default one(s)).
                    packet.WriteByte((byte)(StatCount << 1 | 1));
                    break;
            }
            // foreach stat
            //pPacket.WriteByte(type);              // Stat type ( 0 = STR, 1 = END, 2 = DEX, 3 = INT, 4 = SPR )
            //pPacket.WriteUShort(amount);          // Amount
            // end foreach
            if (Str > 0) { packet.WriteByte(0); packet.WriteUShort(Str); }
            if (End > 0) { packet.WriteByte(1); packet.WriteUShort(End); }
            if (Dex > 0) { packet.WriteByte(2); packet.WriteUShort(Dex); }
            if (Spr > 0) { packet.WriteByte(3); packet.WriteUShort(Spr); }
            if (Int > 0) { packet.WriteByte(4); packet.WriteUShort(Int); }
        }

        //this is used by the smaller writer (e.g. additem, unequip, equip)
        public void WriteSmallInfo(Packet packet)
        {
            packet.WriteUShort(this.ItemID);
            switch (SlotType)
            {
                case ItemSlot.Helm:
                case ItemSlot.Armor:
                case ItemSlot.Pants:
                case ItemSlot.Boots:
                case ItemSlot.Weapon2:
                case ItemSlot.Weapon:
                    packet.WriteByte(Upgrades);
                    packet.Fill(6, 0);
                    packet.WriteUShort(ushort.MaxValue); //unk
                    packet.WriteUInt(GetExpiringTime());
                    packet.WriteUShort(ushort.MaxValue);
                    break;
              
                default:
                    packet.WriteUInt(GetExpiringTime());
                    packet.WriteInt(0); //unk
                    break;
            }
        }
    }
}
