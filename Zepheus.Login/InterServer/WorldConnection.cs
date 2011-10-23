using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Reflection;

using Zepheus.Database;
using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Networking;
using Zepheus.Login.InterServer;
using Zepheus.InterLib.Networking;
using Zepheus.InterLib.NetworkObjects;
using Zepheus.Services.DataContracts;
using Zepheus.Util;

namespace Zepheus.Login.InterServer
{
    public sealed class WorldConnection : InterClient
    {
        public bool IsAWorld { get; set; }
        public WorldStatus Status { get; set; }
        public string Name { get; set; }
        public byte ID { get; set; }
        public string IP { get; set; }
        public ushort Port { get; set; }
        public int Load { get; private set; }

        public WorldConnection(Socket sock) : base(sock)
        {
            Status = WorldStatus.LOW;
            IsAWorld = false;
            this.OnPacket += new EventHandler<InterPacketReceivedEventArgs>(WorldConnection_OnPacket);
            this.OnDisconnect += new EventHandler<InterLib.Networking.SessionCloseEventArgs>(WorldConnection_OnDisconnect);
        }

        void WorldConnection_OnDisconnect(object sender, InterLib.Networking.SessionCloseEventArgs e)
        {
            if (IsAWorld)
            {
                this.OnPacket -= new EventHandler<InterPacketReceivedEventArgs>(WorldConnection_OnPacket);
                this.OnDisconnect -= new EventHandler<InterLib.Networking.SessionCloseEventArgs>(WorldConnection_OnDisconnect);
                WorldConnection derp;
                if (WorldManager.Instance.Worlds.TryRemove(ID, out derp))
                {
                    Log.WriteLine(LogLevel.Info, "World {0} disconnected.", ID);
                }
                else
                {
                    Log.WriteLine(LogLevel.Info, "Could not remove world {0}!?", ID);
                }
            }
        }

        void WorldConnection_OnPacket(object sender, InterPacketReceivedEventArgs e)
        {
            if (e.Client.Assigned == false)
            {
                if (e.Packet.OpCode == InterHeader.AUTH)
                {
                    string pass;
                    if (!e.Packet.TryReadString(out pass))
                    {
                        Log.WriteLine(LogLevel.Error, "Couldn't read pass from inter packet.");
                        e.Client.Disconnect();
                        return;
                    }

                    if (!pass.Equals(Settings.Instance.InterPassword))
                    {
                        Log.WriteLine(LogLevel.Error, "Inter password incorrect");
                        e.Client.Disconnect();
                        return;
                    }
                    else
                    {
                        e.Client.Assigned = true;
                    }
                }
                else
                {
                    Log.WriteLine(LogLevel.Info, "Not authenticated and no auth packet first.");
                    e.Client.Disconnect();
                    return;
                }
            }
            else
            {
                MethodInfo method = InterHandlerStore.GetHandler(e.Packet.OpCode);
                if (method != null)
                {
                    Action action = InterHandlerStore.GetCallback(method, this, e.Packet);
                    if (Worker.Instance == null)
                    {
                        action();
                    }
                    else
                    {
                        Worker.Instance.AddCallback(action);
                    }
                }
                else
                {
                    Log.WriteLine(LogLevel.Debug, "Unhandled interpacket: {0}", e.Packet);
                }
            }
        }

        public void SendTransferClientFromWorld(int accountID, string userName, byte admin, string hostIP, string hash)
        {
            using (var packet = new InterPacket(InterHeader.CLIENTTRANSFER))
            {
                packet.WriteByte(0);
                packet.WriteInt(accountID);
                packet.WriteStringLen(userName);
                packet.WriteStringLen(hash);
                packet.WriteByte(admin);
                packet.WriteStringLen(hostIP);
                this.SendPacket(packet);
            }
        }

        public void SendTransferClientFromZone(int accountID, string userName, string charName, ushort randid, byte admin, string hostIP)
        {
            using (var packet = new InterPacket(InterHeader.CLIENTTRANSFER))
            {
                packet.WriteByte(1);
                packet.WriteInt(accountID);
                packet.WriteStringLen(userName);
                packet.WriteStringLen(charName);
                packet.WriteUShort(randid);
                packet.WriteByte(admin);
                packet.WriteStringLen(hostIP);
                this.SendPacket(packet);
            }
        }
    }
}
