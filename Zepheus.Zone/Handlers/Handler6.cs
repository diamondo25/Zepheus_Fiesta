using System;
using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Data;
using Zepheus.FiestaLib.Networking;
using Zepheus.Services.DataContracts;
using Zepheus.Util;
using Zepheus.Zone.Data;
using Zepheus.Zone.Game;
using Zepheus.Zone.Networking;

namespace Zepheus.Zone.Handlers
{
    public sealed class Handler6
    {
        [PacketHandler(CH6Type.TransferKey)]
        public static void TransferKeyHandler(ZoneClient client, Packet packet)
        {
            ushort randomID;
            string characterName, checksums; //TODO: check in securityclient
            if (!packet.TryReadUShort(out randomID) || !packet.TryReadString(out characterName, 16) ||
                !packet.TryReadString(out checksums, 832))
            {
                Log.WriteLine(LogLevel.Warn, "Invalid game transfer.");
                return;
            }
            ClientTransfer transfer = ClientManager.Instance.GetTransfer(characterName);
            if (transfer == null || transfer.HostIP != client.Host || transfer.RandID != randomID)
            {
                Log.WriteLine(LogLevel.Warn, "{0} tried to login without a valid client transfer.", client.Host);
                //Handler3.SendError(client, ServerError.INVALID_CREDENTIALS);
                Handler4.SendConnectError(client, ConnectErrors.RequestedCharacterIDNotMatching);
                return;
            }

            try
            {
                ClientManager.Instance.RemoveTransfer(characterName);
                ZoneCharacter character = new ZoneCharacter(characterName);
                if (character.AccountID != transfer.AccountID)
                {
                    Log.WriteLine(LogLevel.Warn, "Character is logging in with wrong account ID.");
                    Handler4.SendConnectError(client, ConnectErrors.RequestedCharacterIDNotMatching);
                    //Handler3.SendError(client, ServerError.INVALID_CREDENTIALS);
                    return;
                }
                client.Authenticated = true;
                client.Admin = transfer.Admin;
                client.AccountID = transfer.AccountID;
                client.Username = transfer.Username;
                client.Character = character;
                character.Client = client;
                if (ClientManager.Instance.AddClient(client))
                {
                    character.SendGetIngameChunk(); //TODO: world server notification over WCF?
                    Log.WriteLine(LogLevel.Debug, "{0} logged in successfully!", character.Name);
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "Error loading character {0}: {1} - {2}", characterName, ex.ToString(), ex.StackTrace);
                Handler4.SendConnectError(client, ConnectErrors.ErrorInCharacterInfo);
            }
        }

        [PacketHandler(CH6Type.ClientReady)]
        public static void ClientReadyHandler(ZoneClient client, Packet packet)
        {
            if (client.Admin > 0)
            {
                client.Character.DropMessage("AdminLevel = {0}; ClientLoad = {1};", client.Admin, ClientManager.Instance.ZoneLoad);
            }

            Handler4.SendUsablePoints(client);

            if (!client.Character.IsDead)
            {
                // Just logged on.
                client.Character.Map.FinalizeAdd(client.Character);
            }
            else
            {
                // Reviving, not readding for this one!
                MapInfo mi;
                if (DataProvider.Instance.MapsByID.TryGetValue(client.Character.MapID, out mi))
                {
                    client.Character.State = PlayerState.Normal;
                    client.Character.Map.SendCharacterLeftMap(client.Character, false);
                    client.Character.Position.X = mi.RegenX;
                    client.Character.Position.Y = mi.RegenY;
                    client.Character.Map.SendCharacterEnteredMap(client.Character);
                }
                client.Character.SetHP(client.Character.MaxHP / 4);
            }
        }

        public static Packet RemoveDrop(Drop drop)
        {
            Packet packet = new Packet(SH6Type.RemoveDrop);
            packet.WriteUShort(drop.ID);
            return packet;
        }

        public static void SendDetailedCharacterInfo(ZoneCharacter character)
        {
            using (var packet = new Packet(SH6Type.DetailedCharacterInfo))
            {
                character.WriteDetailedInfoExtra(packet);
                character.Client.SendPacket(packet);
            }
        }

        public static void SendChangeMap(ZoneCharacter character, ushort mapid, int x, int y)
        {
            using (var packet = new Packet(SH6Type.ChangeMap))
            {
                packet.WriteUShort(mapid);
                packet.WriteInt(x);
                packet.WriteInt(y);
                character.Client.SendPacket(packet);
            }
        }

        public static void SendChangeZone(ZoneCharacter character, ushort mapid, int x, int y, string IP, ushort port, ushort randomid)
        {
            using (var packet = new Packet(SH6Type.ChangeZone))
            {
                packet.WriteUShort(mapid);
                packet.WriteInt(x);
                packet.WriteInt(y);

                packet.WriteString(character.Client.Host == "127.0.0.1" ? "127.0.0.1" : IP, 16);
                packet.WriteUShort(port);
                packet.WriteUShort(randomid);
                character.Client.SendPacket(packet);
            }
        }

        public static void SendError(ZoneCharacter character)
        {
            using (var packet = new Packet(SH6Type.Error))
            {

                character.Client.SendPacket(packet);
            }
        }
    }
}
