using System;
using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Data;
using Zepheus.FiestaLib.Networking;
using Zepheus.Util;
using Zepheus.World.Data;

namespace Zepheus.World.Handlers
{
    public sealed class PacketHelper
    {
        public static void WriteBasicCharInfo(WorldCharacter wchar, Packet packet)
        {
            packet.WriteInt(wchar.Character.ID);
            packet.WriteString(wchar.Character.Name, 16);
            packet.WriteUShort(wchar.Character.CharLevel);
            packet.WriteByte(wchar.Character.Slot);
            MapInfo mapinfo;
            if (!DataProvider.Instance.Maps.TryGetValue(wchar.Character.Map, out mapinfo))
            {
                Log.WriteLine(LogLevel.Warn, "{0} has an invalid MapID ({1})", wchar.Character.Name, wchar.Character.Map);
                wchar.Character.Map = 0;//we reset
                packet.WriteString("Rou", 12);
            }
            else
            {
                packet.WriteString(mapinfo.ShortName, 12);
            }
            packet.WriteByte(0);               // UNK
            packet.WriteInt(0x00000000);       // Random seed
            WriteLook(wchar, packet);
            WriteEquipment(wchar, packet);
            WriteRefinement(wchar, packet);
            packet.Fill(4, 0xff);      		// UNK
            packet.WriteString("Rou", 12); //TODO: load from mapinfo.shn
            packet.WriteInt(0);				// X, doesn't matter
            packet.WriteInt(0);        		// Y, neither

            packet.WriteInt(0x63dd45ca);
            packet.WriteByte(0);
            packet.WriteInt(100);      		// Test later!
            packet.WriteByte(0);
            wchar.Detach();
        }

        public static void WriteLook(WorldCharacter wchar, Packet packet)
        {
            packet.WriteByte(Convert.ToByte(0x01 | (wchar.Character.Job << 2) | (Convert.ToByte(wchar.Character.Male)) << 7));
            packet.WriteByte(wchar.Character.Hair);
            packet.WriteByte(wchar.Character.HairColor);
            packet.WriteByte(wchar.Character.Face);
        }

        public static void WriteEquipment(WorldCharacter wchar, Packet packet)
        {
            packet.WriteUShort(wchar.GetEquipBySlot(ItemSlot.Helm));
            packet.WriteUShort(Settings.Instance.ShowEquips ? wchar.GetEquipBySlot(ItemSlot.Weapon) : (ushort)0xffff);
            packet.WriteUShort(wchar.GetEquipBySlot(ItemSlot.Armor));
            packet.WriteUShort(Settings.Instance.ShowEquips ? wchar.GetEquipBySlot(ItemSlot.Weapon2) : (ushort)0xffff);
            packet.WriteUShort(wchar.GetEquipBySlot(ItemSlot.Pants));
            packet.WriteUShort(wchar.GetEquipBySlot(ItemSlot.Boots));
            packet.WriteUShort(wchar.GetEquipBySlot(ItemSlot.CostumeBoots));
            packet.WriteUShort(wchar.GetEquipBySlot(ItemSlot.CostumePants));
            packet.WriteUShort(wchar.GetEquipBySlot(ItemSlot.CostumeArmor));
            packet.Fill(6, 0xff);              // UNK
            packet.WriteUShort(wchar.GetEquipBySlot(ItemSlot.Glasses));
            packet.WriteUShort(wchar.GetEquipBySlot(ItemSlot.CostumeHelm));
            packet.Fill(2, 0xff);              // UNK
            packet.WriteUShort(wchar.GetEquipBySlot(ItemSlot.CostumeWeapon));
            packet.WriteUShort(wchar.GetEquipBySlot(ItemSlot.Wing));
            packet.Fill(2, 0xff);              // UNK
            packet.WriteUShort(wchar.GetEquipBySlot(ItemSlot.Tail));
            packet.WriteUShort(wchar.GetEquipBySlot(ItemSlot.Pet));
        }

        public static void WriteRefinement(WorldCharacter wchar, Packet pPacket)
        {
            //TODO: pPacket.WriteByte(Convert.ToByte(this.Inventory.GetEquippedUpgradesByType(ItemType.Weapon) << 4 | this.Inventory.GetEquippedUpgradesByType(ItemType.Shield))); 
            pPacket.WriteByte(0xff); //this must be the above, but currently not cached
            pPacket.WriteByte(0xff);    		// UNK
            pPacket.WriteByte(0xff);    		// UNK
        }
    }
}
