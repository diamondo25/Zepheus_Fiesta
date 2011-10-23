
using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Networking;
using Zepheus.World.Networking;

namespace Zepheus.World.Handlers
{
    public sealed class Handler2
    {
        //this is incorrect, somehow?
        [PacketHandler(CH2Type.Pong)]
        public static void Pong(WorldClient client, Packet packet)
        {
            client.Pong = true;
        }

        public static void SendPing(WorldClient client)
        {
            using (var packet = new Packet(SH2Type.Ping))
            {
                client.SendPacket(packet);
            }
        }
    }
}
