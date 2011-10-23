using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

using Zepheus.FiestaLib.Networking;
using Zepheus.Login.Handlers;
using Zepheus.FiestaLib;
using Zepheus.Util;
using Zepheus.Services.DataContracts;

namespace Zepheus.Login.Networking
{
    public sealed class LoginClient : Client
    {
        public bool IsAuthenticated { get; set; }
        public int AccountID { get; set; }
        public string Username { get; set; }
        public byte Admin { get; set; }
        public bool IsTransferring { get; set; }

        public LoginClient(Socket sock)
            : base(sock)
        {
            base.OnPacket += new EventHandler<PacketReceivedEventArgs>(LoginClient_OnPacket);
            base.OnDisconnect += new EventHandler<SessionCloseEventArgs>(LoginClient_OnDisconnect);
        }

        public ClientTransfer GenerateTransfer()
        {
            if (!IsAuthenticated) return null;
            string hash = Guid.NewGuid().ToString().Replace("-", "");
            return new ClientTransfer(AccountID, Username, Admin, this.Host, hash);
        }

        void LoginClient_OnDisconnect(object sender, SessionCloseEventArgs e)
        {
            Log.WriteLine(LogLevel.Debug, "{0} Disconnected.", base.Host);
            ClientManager.Instance.RemoveClient(this);
        }

        void LoginClient_OnPacket(object sender, PacketReceivedEventArgs e)
        {
            MethodInfo method = HandlerStore.GetHandler(e.Packet.Header, e.Packet.Type);
            if (method != null)
            {
                Action action = HandlerStore.GetCallback(method, this, e.Packet);
                Worker.Instance.AddCallback(action);
            }
            else
            {
                Log.WriteLine(LogLevel.Debug, "Unhandled packet: {0}", e.Packet);
            }
        }
    }
}
