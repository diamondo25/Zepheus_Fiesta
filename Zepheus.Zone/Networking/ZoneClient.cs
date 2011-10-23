using System;
using System.Net.Sockets;
using System.Reflection;
using Zepheus.FiestaLib.Networking;
using Zepheus.Util;
using Zepheus.Zone.Game;
using Zepheus.Zone.Handlers;

namespace Zepheus.Zone.Networking
{
    public sealed class ZoneClient : Client
    {
        public bool Authenticated { get; set; }
        public string Username { get; set; }
        public int AccountID { get; set; }
        public byte Admin { get; set; }

        public bool HasPong { get; set; }

        public ZoneCharacter Character { get; set; }
        
        public ZoneClient(Socket socket)
            : base(socket)
        {
            base.OnDisconnect += new EventHandler<SessionCloseEventArgs>(ZoneClient_OnDisconnect);
            base.OnPacket += new EventHandler<PacketReceivedEventArgs>(ZoneClient_OnPacket);

            HasPong = true;
            Authenticated = false;
        }

        void ZoneClient_OnPacket(object sender, PacketReceivedEventArgs e)
        {
            if (!Authenticated && !(e.Packet.Header == 6 && e.Packet.Type == 1)) return; //do not handle packets if not authenticated!
            MethodInfo method = HandlerStore.GetHandler(e.Packet.Header, e.Packet.Type);
            if (method != null)
            {
                Action action = HandlerStore.GetCallback(method, this, e.Packet);
                Worker.Instance.AddCallback(action);
            }
            else
            {
                Log.WriteLine(LogLevel.Debug, "Unhandled packet: {0}|{1}", e.Packet.Header, e.Packet.Type);
                Console.WriteLine(e.Packet.Dump());
            }
        }

        void ZoneClient_OnDisconnect(object sender, SessionCloseEventArgs e)
        {
            ClientManager.Instance.RemoveClient(this);
            if (Character != null)
            {
                Character.Save();
                Character.RemoveFromMap();
            }
            Log.WriteLine(LogLevel.Debug, "Client disconnected.");
        }

        public override string ToString()
        {
            if (Character != null)
            {
                return "ZoneClient|Character:" + Character.ToString();
            }
            else
            {
                return "ZoneClient|NoChar";
            }
        }
    }
}
