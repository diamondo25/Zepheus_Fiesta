using System.Collections.Generic;
using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Networking;
using Zepheus.Util;
using Zepheus.Zone.Game;
using Zepheus.Zone.Networking;

namespace Zepheus.Zone.Handlers
{
    public sealed class Handler7
    {
        public static Packet MultiObjectList(List<MapObject> objs, int start, int end)
        {
            Packet packet = new Packet(SH7Type.SpawnMultiObject);
            packet.WriteByte((byte)(end - start));
            Log.WriteLine(LogLevel.Debug, "Sending {0} objects ({1} - {2})", end - start, start, end);
            for (int i = start; i < end; i++)
            {
                var obj = objs[i];
                if (obj is NPC)
                {
                    ((NPC)obj).Write(packet);
                }
                else if (obj is Mob) // Just to be sure
                {
                    ((Mob)obj).Write(packet);
                }
                else
                {
                    Log.WriteLine(LogLevel.Warn, "What the F is {0} doing here lol", obj.ToString());
                }
             }
            return packet;
        }

        public static Packet Unequip(ZoneCharacter character, Equip equip)
        {
            Packet packet = new Packet(SH7Type.ShowUnequip);
            packet.WriteUShort(character.MapObjectID);
            packet.WriteByte((byte)equip.Info.Slot);
            return packet;
        }

        public static Packet ShowDrop(Drop drop)
        {
            Packet packet = new Packet(SH7Type.ShowDrop);
            drop.Write(packet);
            return packet;
        }

        public static Packet ShowDrops(List<Drop> drops)
        {
            Packet packet = new Packet(SH7Type.ShowDrops);
            packet.WriteByte((byte)drops.Count);
            drops.ForEach(d => d.Write(packet));
            return packet;
        }

        public static Packet Equip(ZoneCharacter character, Equip equip)
        {
            //B2 00 - AB 38 - 07 - 0D 00 04
            Packet packet = new Packet(SH7Type.ShowEquip);
            packet.WriteUShort(character.MapObjectID);
            packet.WriteUShort(equip.ItemID);
            packet.WriteByte(equip.Upgrades);
            packet.Fill(3, 0xff);
            return packet;
        }

        public static Packet SpawnSinglePlayer(ZoneCharacter character)
        {
            Packet packet = new Packet(SH7Type.SpawnSinglePlayer);
            character.WriteCharacterDisplay(packet);
            return packet;
        }

        public static Packet SpawnMultiPlayer(List<ZoneCharacter> characters, ZoneCharacter exclude = null)
        {
            Packet packet = new Packet(SH7Type.SpawnMultiPlayer);
            packet.WriteByte(exclude == null ? (byte)characters.Count : (byte)(characters.Count - 1));
            foreach (var character in characters)
            {
                if (character == exclude) continue;
                character.WriteCharacterDisplay(packet);
            }
            return packet;
        }

        public static Packet RemoveObject(MapObject obj)
        {
            Packet packet = new Packet(SH7Type.RemoveObject);
            packet.WriteUShort(obj.MapObjectID);
            return packet;
        }

        [PacketHandler(CH7Type.UnknownSomethingWithMobs)]
        public static void HandleUnknown(ZoneClient client, Packet packet)
        {
            // I have no idea what this does... Maybe some aggro request?
        }
    }
}
