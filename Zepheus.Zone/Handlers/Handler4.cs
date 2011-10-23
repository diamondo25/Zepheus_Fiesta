
using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Data;
using Zepheus.FiestaLib.Networking;
using Zepheus.Util;
using Zepheus.Zone.Data;
using Zepheus.Zone.Game;
using Zepheus.Zone.Networking;

namespace Zepheus.Zone.Handlers
{
    public sealed class Handler4
    {
        [PacketHandler(CH4Type.ReviveToTown)]
        public static void HandleReviveToTown(ZoneClient character, Packet packet)
        {
            if (character.Character.IsDead)
            {
                // Lets revive.
                MapInfo mi;
                if (DataProvider.Instance.MapsByID.TryGetValue(character.Character.MapID, out mi))
                {
                    SendRevive(character, mi.ID, mi.RegenX, mi.RegenY); // Will resend the loaded packet
                }
            }
        }

        public static void SendCharacterInfo(ZoneCharacter character)
        {
            using (var packet = new Packet(SH4Type.CharacterInfo))
            {
                character.WriteDetailedInfo(packet);
                character.Client.SendPacket(packet);
            }
        }

        public static void SendCharacterLook(ZoneCharacter character)
        {
            using (var packet = new Packet(SH4Type.CharacterLook))
            {
                character.WriteLook(packet);
                character.Client.SendPacket(packet);
            }
        }

        public static void SendQuestListBusy(ZoneCharacter character)
        {
            using (var packet = new Packet(SH4Type.CharacterQuestsBusy))
            {
                packet.WriteInt(character.ID);
                packet.WriteByte(1); //enabled list
                packet.WriteByte(0); //count (analyzed in separate TXT)
                //TODO: load quest list from database
                character.Client.SendPacket(packet);
            }
        }

        public static void SendQuestListDone(ZoneCharacter character)
        {
            using (var packet = new Packet(SH4Type.CharacterQuestsDone))
            {
                packet.WriteInt(character.ID);
                packet.WriteShort(0); //quest count
                character.Client.SendPacket(packet);
            }
        }

        public static void SendActiveSkillList(ZoneCharacter character)
        {
            using (var packet = new Packet(SH4Type.CharacterActiveSkillList))
            {
                var list = character.SkillsActive.Values;
                packet.WriteByte(0);
                packet.WriteInt(character.ID);
                packet.WriteUShort((ushort)list.Count); // Skill count (max 300)
                foreach (var skill in list)
                {
                    skill.Write(packet);
                }
                character.Client.SendPacket(packet);
            }
        }

        public static void SendPassiveSkillList(ZoneCharacter character)
        {
            using (var packet = new Packet(SH4Type.CharacterPassiveSkillList))
            {
                var list = character.SkillsPassive.Keys;
                packet.WriteUShort((ushort)list.Count); //count (max 300)
                foreach (var skill in list)
                {
                    packet.WriteUShort(skill);
                }
                character.Client.SendPacket(packet);
            }
        }

        public static void SendEquippedList(ZoneCharacter character)
        {
            using (var packet = new Packet(SH4Type.CharacterItemList))
            {
                packet.WriteByte((byte)character.EquippedItems.Count);
                packet.WriteByte(0x08);        // Inventory number
                packet.WriteByte(215);         // UNK    (In newest client it exists, in bit older, not) // might be shit from old buffers lol
                foreach (var eqp in character.EquippedItems.Values)
                {
                    eqp.WriteInfo(packet);
                }
                character.Client.SendPacket(packet);
            }
        }

        public static void SendInventoryList(ZoneCharacter character)
        {
            using (var packet = new Packet(SH4Type.CharacterItemList))
            {
                packet.WriteByte((byte)character.InventoryItems.Count);
                packet.WriteByte(0x09);         // Inventory number
                packet.WriteByte(0xB7);         // UNK    (In newest client it exists, in bit older, not)
                foreach (var item in character.InventoryItems.Values)
                {
                    item.WriteInfo(packet);
                }
                character.Client.SendPacket(packet);
            }
        }

        public static void SendHouseList(ZoneCharacter character)
        {
            //TODO: house loading
            using (var packet = new Packet(SH4Type.CharacterItemList))
            {
                byte count = 0;
                packet.WriteByte(count);   // Count
                packet.WriteByte(0x0C);    // Inventory
                packet.WriteByte(0x35);    // UNK    (In newest client it exists, in bit older, not)
                for (byte i = 0; i < count; i++)
                {
                    packet.WriteByte(8);           // Item Data Length
                    packet.WriteByte(i);           // Slot
                    packet.WriteByte(0x30);        // UNK
                    packet.WriteUShort((ushort)(31000 + i * 3));  // Item ID
                    packet.WriteUInt(1992027391);  // Expiring Time (1992027391 - Permanent)
                }
                character.Client.SendPacket(packet);
            }
        }

        public static void SendPremiumEmotions(ZoneCharacter character)
        {
            using (var packet = new Packet(SH4Type.CharacterItemList))
            {
                byte count = 0;
                packet.WriteByte(count);   // Count
                packet.WriteByte(0x0F);    // Inventory
                packet.WriteByte(0xCB);    // UNK    (In newest client it exists, in bit older, not)
                for (byte i = 0; i < count; i++)
                {
                    packet.WriteByte(8);           // Item Data Length
                    packet.WriteByte(i);           // Slot
                    packet.WriteByte(0x3C);        // Inventory thing
                    packet.WriteUShort((ushort)(31500 + (16 < i ? 1 : 0) + i * 2));  // Item ID
                    packet.WriteUInt(1992027391);  // Expiring Time (1992027391 - Permanent)
                }
                character.Client.SendPacket(packet);
            }
        }

        public static void SendPremiumItemList(ZoneCharacter character)
        {
            using(var packet = new Packet(SH4Type.CharacterItemList)) {
                ushort count = 0;
                packet.WriteUShort(count);   // Count
                for (ushort i = 0; i < count; i++)
                {
                    packet.WriteUShort(0x0010);    // Inventory
                    packet.WriteUShort(0x0000);    // Slot
                    packet.WriteUShort(0x033B);    // Item Handle      // (Iron Case, to add 4 extra inventories :P)
                    packet.WriteUInt(1992027391);  // Activation Time (1992027391 - Permanent)
                    packet.WriteUInt(1992027391);  // Expiring Time (1992027391 - Permanent)
                }
                character.Client.SendPacket(packet);
            }
        }

        public static void SendTitleList(ZoneCharacter character)
        {
            using (var packet = new Packet(SH4Type.CharacterTitles))
            {
                packet.WriteInt(0); //current title ID
                packet.WriteShort(0); //title count
                //here comes shit loop (see old zepheus)

                character.Client.SendPacket(packet);
            }
        }

        public static void SendCharacterChunkEnd(ZoneCharacter character)
        {
            using (var packet = new Packet(SH4Type.CharacterInfoEnd))
            {
                packet.WriteUShort(0xFFFF);
                character.Client.SendPacket(packet);
            }
        }

        public static void SendConnectError(ZoneClient client, ConnectErrors error)
        {
            using (var packet = new Packet(SH4Type.ConnectError))
            {
                packet.WriteUShort((ushort)error);
                client.SendPacket(packet);
            }
        }

        public static void SendReviveWindow(ZoneClient client, byte minutesTillExpire)
        {
            using (var packet = new Packet(SH4Type.ReviveWindow))
            {
                packet.WriteByte(minutesTillExpire); // It's not a short, the byte after it is buffershit
                client.SendPacket(packet);
            }
        }

        public static void SendRevive(ZoneClient client, ushort mapid, int x, int y)
        {
            using (var packet = new Packet(SH4Type.Revive))
            {
                packet.WriteUShort(mapid);
                packet.WriteInt(x);
                packet.WriteInt(y);
                client.SendPacket(packet);
            }
        }

        public static void SendUsablePoints(ZoneClient client)
        {
            using (var packet = new Packet(SH4Type.CharacterPoints))
            {
                packet.WriteByte(client.Character.character.UsablePoints);
                client.SendPacket(packet);
            }
        }

        [PacketHandler(CH4Type.SetPointOnStat)]
        public static void HandleSetStatPoint(ZoneClient client, Packet packet)
        {
            byte stat;
            if (!packet.TryReadByte(out stat))
            {
                Log.WriteLine(LogLevel.Warn, "Couldn't read HandleSetStatPoint packet. {0}", client);
                return;
            }

            if (client.Character.character.UsablePoints == 0)
            {
                Log.WriteLine(LogLevel.Warn, "User tried to set stat point while not having any left. {0}", client);
            }
            else
            {
                // LETS DO ET
                switch (stat)
                {
                    case 0: client.Character.Str++; break;
                    case 1: client.Character.Dex++; break;
                    case 2: client.Character.End++; break;
                    case 3: client.Character.Int++; break;
                    case 4: client.Character.Spr++; break;
                    default:
                        {
                            Log.WriteLine(LogLevel.Warn, "User tried to set stat point on unknown stat {0} {1}", stat, client);
                            return;
                        }
                }
                client.Character.character.UsablePoints--;
                Program.Entity.SaveChanges();
                SendSetUsablePoint(client, stat);
            }
        }

        public static void SendSetUsablePoint(ZoneClient client, byte stat)
        {
            using (var packet = new Packet(SH4Type.SetPointOnStat))
            {
                packet.WriteByte(stat); // amount
                client.SendPacket(packet);
            }
        }
    }
}
