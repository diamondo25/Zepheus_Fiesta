using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Networking;
using Zepheus.Util;
using Zepheus.World.Data;
using Zepheus.World.InterServer;
using Zepheus.World.Networking;

namespace Zepheus.World.Handlers
{
    public sealed class Handler4
    {
        [PacketHandler(CH4Type.CharSelect)]
        public static void CharacterSelectHandler(WorldClient client, Packet packet)
        {
            byte slot;
            if (!packet.TryReadByte(out slot) || slot > 10 || !client.Characters.ContainsKey(slot))
            {
                Log.WriteLine(LogLevel.Warn, "{0} selected an invalid character.", client.Username);
                return;
            }

            WorldCharacter character;
            if (client.Characters.TryGetValue(slot, out character))
            {
                //generate transfer
                
                ZoneConnection zone = Program.GetZoneByMap(character.Character.Map);
                if (zone != null)
                {
                    client.Characters.Clear(); //we clear the other ones from memory
                    client.Character = character; //only keep the one selected
                    zone.SendTransferClientFromZone(client.AccountID, client.Username, client.Character.Character.Name, client.RandomID, client.Admin, client.Host);

                    ClientManager.Instance.AddClientByName(client); //so we can look them up fast using charname later.
                    SendZoneServerIP(client, zone);
                }
                else
                {
                    Log.WriteLine(LogLevel.Warn, "Character tried to join unloaded map: {0}", character.Character.Map);
                    SendConnectError(client, ConnectErrors.MapUnderMaintenance);
                }
            }
        }

        public static void SendZoneServerIP(WorldClient client, ZoneConnection info)
        {
            using (var packet = new Packet(SH4Type.ServerIP))
            {
                packet.WriteString(client.Host == "127.0.0.1" ? "127.0.0.1" : info.IP, 16);
                packet.WriteUShort(info.Port);
                client.SendPacket(packet);
            }
        }

        public static void SendConnectError(WorldClient client, ConnectErrors error)
        {
            using (var packet = new Packet(SH4Type.ConnectError))
            {
                packet.WriteUShort((ushort)error);
                client.SendPacket(packet);
            }
        }
    }
}
