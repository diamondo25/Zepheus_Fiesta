using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

using Zepheus.InterLib.Networking;

namespace Zepheus.InterLib.NetworkObjects
{
    public class AbstractConnector
    {
        public string IpAddress { get; private set; }
        public int Port { get; private set; }

        protected InterClient client;
        public bool Pong { get; private set; }
        public bool ForcedClose { get; private set; }

        public void Connect(string ip, int port)
        {
            IpAddress = ip;
            Port = port;
            ForcedClose = false;
            Connect();
        }

        public void Connect()
        {
            TcpClient tcpClient = new TcpClient();
            tcpClient.Connect(IpAddress, Port);
            client = new InterClient(tcpClient.Client);
            client.OnPacket += new EventHandler<InterPacketReceivedEventArgs>(client_OnPacket);
        }

        void client_OnPacket(object sender, InterPacketReceivedEventArgs e)
        {
            if (e.Packet.OpCode == InterHeader.PING)
            {
                SendPong();
            }
            else if (e.Packet.OpCode == InterHeader.PONG)
            {
                Pong = true;
            }

        }

        public void SendPing()
        {
            Pong = false;
            using (var packet = new InterPacket(InterHeader.PING))
            {
                client.SendPacket(packet);
            }
        }

        public void SendPong()
        {
            using (var packet = new InterPacket(InterHeader.PONG))
            {
                client.SendPacket(packet);
            }
        }
    }
}
