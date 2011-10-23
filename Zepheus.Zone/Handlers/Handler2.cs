
using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Networking;
using Zepheus.Zone.Game;
using Zepheus.Zone.Networking;

namespace Zepheus.Zone.Handlers
{
    public static class Handler2
    {
        public static void SendChatBlock(ZoneCharacter character, int seconds)
        {
            using (var packet = new Packet(SH2Type.Chatblock))
            {
                packet.WriteInt(seconds);
                character.Client.SendPacket(packet);
            }
        }

        [PacketHandler(CH2Type.Pong)]
        public static void HandlePong(ZoneClient character, Packet packet)
        {
            character.HasPong = true;
        }

        public static void SendPing(ZoneClient character)
        {
            using (var packet = new Packet(SH2Type.Ping))
            {
                character.SendPacket(packet);
            }
        }
    }
}
