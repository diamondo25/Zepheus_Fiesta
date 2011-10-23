using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using Zepheus.FiestaLib.Data;
using Zepheus.InterLib.Networking;
using Zepheus.Util;
using Zepheus.World.Data;

namespace Zepheus.World.InterServer
{
    public sealed class ZoneConnection : InterClient
    {
        public bool IsAZone { get; set; }
        public int Load { get; private set; }

        public byte ID { get; set; }
        public ushort Port { get; set; }
        public string IP { get; set; }
        public List<MapInfo> Maps { get; set; }

        public ZoneConnection(Socket sock) : base(sock)
        {
            IsAZone = false;
            this.OnPacket += new EventHandler<InterPacketReceivedEventArgs>(WorldConnection_OnPacket);
            this.OnDisconnect += new EventHandler<InterLib.Networking.SessionCloseEventArgs>(WorldConnection_OnDisconnect);
        }

        void WorldConnection_OnDisconnect(object sender, InterLib.Networking.SessionCloseEventArgs e)
        {
            if (IsAZone)
            {
                this.OnPacket -= new EventHandler<InterPacketReceivedEventArgs>(WorldConnection_OnPacket);
                this.OnDisconnect -= new EventHandler<InterLib.Networking.SessionCloseEventArgs>(WorldConnection_OnDisconnect);

                ZoneConnection derp;
                if (Program.Zones.TryRemove(ID, out derp))
                {
                    Log.WriteLine(LogLevel.Info, "Zone {0} disconnected.", ID);
                    InterHandler.SendZoneStopped(ID);
                }
                else
                {
                    Log.WriteLine(LogLevel.Info, "Could not remove zone {0}!?", ID);
                }
            }
        }

        void WorldConnection_OnPacket(object sender, InterPacketReceivedEventArgs e)
        {
            if (e.Client.Assigned == false)
            {
                if (Program.Zones.Count >= 3)
                {
                    Log.WriteLine(LogLevel.Warn, "We can't load more than 3 zones atm.");
                    e.Client.Disconnect();
                    return;
                }

                if (e.Packet.OpCode == InterHeader.AUTH)
                {
                    string pass;
                    if (!e.Packet.TryReadString(out pass))
                    {
                        e.Client.Disconnect();
                        return;
                    }

                    if (!pass.Equals(Settings.Instance.InterPassword))
                    {
                        e.Client.Disconnect();
                        return;
                    }
                    else
                    {
                        try
                        {
                            e.Client.Assigned = true;

                            ID = Program.GetFreeZoneID();
                            this.Port = (ushort)(Settings.Instance.ZoneBasePort + ID);

                            var l = DataProvider.Instance.GetMapsForZone(ID);
                            Maps = new List<MapInfo>();
                            foreach (var mapid in l)
                            {
                                MapInfo map;
                                if (DataProvider.Instance.Maps.TryGetValue(mapid, out map))
                                {
                                    Maps.Add(map);
                                }
                                else
                                    Log.WriteLine(LogLevel.Warn, "Zone is loading map {0} which could not be found.", mapid);
                            }

                            if (Program.Zones.TryAdd(ID, this))
                            {
                                IsAZone = true;
                                SendData();
                                Log.WriteLine(LogLevel.Info, "Added zone {0} with {1} maps.", ID, Maps.Count);
                            }
                            else
                            {
                                Log.WriteLine(LogLevel.Error, "Failed to add zone. Terminating connection.");
                                Disconnect();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine(LogLevel.Exception, ex.ToString());
                            Disconnect();
                        }
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

        public void SendData()
        {
            using (var packet = new InterPacket(InterHeader.ASSIGNED))
            {
                packet.WriteStringLen(ConnectionStringbuilder.CreateEntityString(Settings.Instance.Entity));
                packet.WriteByte(ID);
                packet.WriteStringLen(String.Format("{0}-{1}", Settings.Instance.GameServiceURI, ID));
                packet.WriteUShort((ushort)(Settings.Instance.ZoneBasePort + ID));

                packet.WriteInt(Maps.Count);
                foreach (var m in Maps)
                {
                    packet.WriteUShort(m.ID);
                    packet.WriteStringLen(m.ShortName);
                    packet.WriteStringLen(m.FullName);
                    packet.WriteInt(m.RegenX);
                    packet.WriteInt(m.RegenY);
                    packet.WriteByte(m.Kingdom);
                    packet.WriteUShort(m.ViewRange);
                }
                this.SendPacket(packet);
            }

        }
    }
}
