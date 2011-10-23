
using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Networking;
using Zepheus.Util;
using Zepheus.Zone.Game;
using Zepheus.Zone.Networking;

namespace Zepheus.Zone.Handlers
{
    public sealed class Handler12
    {
        [PacketHandler(CH12Type.Unequip)]
        public static void UnequipHandler(ZoneClient client, Packet packet)
        {
            ZoneCharacter character = client.Character;

            byte sourceSlot;
            sbyte destinationSlot; //not so sure about this one anymore
            if (!packet.TryReadByte(out sourceSlot) ||
                !packet.TryReadSByte(out destinationSlot))
            {
                Log.WriteLine(LogLevel.Warn, "Could not read unequip values from {0}.", character.Name);
                return;
            }
            character.UnequipItem((ItemSlot)sourceSlot, destinationSlot);
        }

        [PacketHandler(CH12Type.LootItem)]
        public static void LootHandler(ZoneClient client, Packet packet)
        {
            ushort id;
            if (!packet.TryReadUShort(out id))
            {
                Log.WriteLine(LogLevel.Warn, "Invalid loot request.");
                return;
            }
            client.Character.LootItem(id);
        }

        [PacketHandler(CH12Type.UseItem)]
        public static void UseHandler(ZoneClient client, Packet packet)
        {
            sbyte slot;
            if (!packet.TryReadSByte(out slot))
            {
                Log.WriteLine(LogLevel.Warn, "Error reading used item slot.");
                return;
            }
            client.Character.UseItem(slot);
        }

        public static void SendItemUseOK(ZoneCharacter character)
        {
            using (var packet = new Packet(SH12Type.ItemUsedOk))
            {
                character.Client.SendPacket(packet);
            }
        }

        public static void SendItemUsed(ZoneCharacter character, Item item, ushort error = 1792)
        {
            if (error == 1792)
            {
                SendItemUseOK(character);
            }

            using (var packet = new Packet(SH12Type.ItemUseEffect))
            {
                packet.WriteUShort(error); //when not ok, it'll tell you there will be no effect
                packet.WriteUShort(item.ItemID);
                character.Client.SendPacket(packet);
            }
        }


        public static void ObtainedItem(ZoneCharacter character, DroppedItem item, ObtainedItemStatus status)
        {
            using (var packet = new Packet(SH12Type.ObtainedItem))
            {
                packet.WriteUShort(item.ItemID);
                packet.WriteInt(item.Amount);
                packet.WriteUShort((ushort)status);
                packet.WriteUShort(0xffff);
                character.Client.SendPacket(packet);
            }
        }

        [PacketHandler(CH12Type.Equip)]
        public static void EquipHandler(ZoneClient client, Packet packet)
        {
            sbyte slot;
            if (!packet.TryReadSByte(out slot))
            {
                Log.WriteLine(LogLevel.Warn, "Error reading equipping slot.");
                return;
            }
            Item item;
            if (client.Character.InventoryItems.TryGetValue(slot, out item))
            {
                if (item is Equip)
                {
                    if (((Equip)item).Info.Level > client.Character.Level)
                    {
                        FailedEquip(client.Character, 645); // 85 02
                    }
                    else
                    {
                        client.Character.EquipItem((Equip)item);
                    }
                }
                else
                {
                    FailedEquip(client.Character);
                    Log.WriteLine(LogLevel.Warn, "{0} equippped an item. What a moron.", client.Character.Name);
                }
            }
        }

        [PacketHandler(CH12Type.MoveItem)]
        public static void MoveItemHandler(ZoneClient client, Packet packet)
        {
            byte from, oldstate, to, newstate;
            if(!packet.TryReadByte(out from) ||
                !packet.TryReadByte(out oldstate) ||
                !packet.TryReadByte(out to) ||
                !packet.TryReadByte(out newstate))
            {
                    Log.WriteLine(LogLevel.Warn, "Invalid item move received.");
                    return;
            }
            client.Character.MoveItem((sbyte)from, (sbyte)to);
        }

        [PacketHandler(CH12Type.DropItem)]
        public static void DropItemHandler(ZoneClient client, Packet packet)
        {
            sbyte slot;
            if (!packet.TryReadSByte(out slot))
            {
                Log.WriteLine(LogLevel.Warn, "Invalid drop request.");
                return;
            }
            client.Character.DropItemRequest(slot);
        }

        [PacketHandler(CH12Type.ItemEnhance)]
        public static void EnhancementHandler(ZoneClient client, Packet packet)
        {
            sbyte weapslot, stoneslot;
            if (!packet.TryReadSByte(out weapslot) ||
                !packet.TryReadSByte(out stoneslot))
            {
                Log.WriteLine(LogLevel.Warn, "Invalid item enhance request.");
                return;
            }
            client.Character.UpgradeItem(weapslot, stoneslot);
        }

        public static void SendUpgradeResult(ZoneCharacter character, bool success)
        {
            using (var packet = new Packet(SH12Type.ItemUpgrade))
            {
                packet.WriteUShort(success ? (ushort)2243 : (ushort)2245);
                character.Client.SendPacket(packet);
            }
        }

        public static void InventoryFull(ZoneCharacter character)
        {
            using (var packet = new Packet(SH12Type.InventoryFull))
            {
                packet.WriteUShort(522);
                character.Client.SendPacket(packet);
            }
        }

        public static void FailedUnequip(ZoneCharacter character)
        {
            using (var packet = new Packet(SH12Type.FailedUnequip))
            {
                packet.WriteUShort(706);
                character.Client.SendPacket(packet);
            }
        }

        public static void FailedEquip(ZoneCharacter character, ushort val = 0)
        {
            using (var packet = new Packet(SH12Type.FailedEquip))
            {
                packet.WriteUShort(val);
                character.Client.SendPacket(packet);
            }
        }

        public static void ModifyEquipSlot(ZoneCharacter character, byte modifyslot, byte otherslot, Equip equip)
        {
            using (var packet = new Packet(SH12Type.ModifyEquipSlot))
            {
                packet.WriteByte(otherslot);
                packet.WriteByte(0x24); //aka the 'equipped' bool
                packet.WriteByte(modifyslot);

                if (equip == null)
                {
                    packet.WriteUShort(ushort.MaxValue);
                }
                else
                {
                    equip.WriteEquipStats(packet);
                }
                character.Client.SendPacket(packet);
            }
        }

        public static void ModifyInventorySlot(ZoneCharacter character, byte inventory, byte newslot, byte oldslot, Item item)
        {
            using (var packet = new Packet(SH12Type.ModifyItemSlot))
            {
                packet.WriteByte(oldslot);
                packet.WriteByte(inventory); //aka 'unequipped' bool
                packet.WriteByte(newslot);
                packet.WriteByte(0x24);
                if (item == null)
                {
                    packet.WriteUShort(0xffff);
                }
                else
                {
                    if (item is Equip)
                    {
                        ((Equip)item).WriteEquipStats(packet);
                    }
                    else
                    {
                        item.WriteItemStats(packet);
                    }
                }
                character.Client.SendPacket(packet);
            }
        }

        public static void ResetInventorySlot(ZoneCharacter character, byte slot)
        {
            using (var packet = new Packet(SH12Type.ModifyItemSlot))
            {
                packet.WriteByte(0);
                packet.WriteByte(0x20);
                packet.WriteByte(slot);
                packet.WriteByte(0x24);
                packet.WriteUShort(0xffff);
                character.Client.SendPacket(packet);
            }
        }
    }
}
