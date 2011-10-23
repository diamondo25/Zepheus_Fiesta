using System;
using System.Collections.Generic;

using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Data;
using Zepheus.InterLib.Networking;
using Zepheus.Services.DataContracts;
using Zepheus.Util;
using Zepheus.Zone.Networking;

namespace Zepheus.Zone.InterServer
{
    public sealed class InterHandler
    {
        [InterPacketHandler(InterHeader.ASSIGNED)]
        public static void HandleAssigned(WorldConnector lc, InterPacket packet)
        {
            string entity, name;
            byte id;
            ushort port;
            int mapidcout;
            if (!packet.TryReadString(out entity) || !packet.TryReadByte(out id) || !packet.TryReadString(out name) ||
                !packet.TryReadUShort(out port) || !packet.TryReadInt(out mapidcout))
            {
                return;
            }

            Program.serviceInfo = new ZoneData
            {
                EntityString = entity,
                ID = id,
                Port = port,
                MapsToLoad = new List<FiestaLib.Data.MapInfo>()
            };

            for (int i = 0; i < mapidcout; i++)
            {
                ushort mapid, viewrange;
                string shortname, fullname;
                int regenx, regeny;
                byte kingdom;
                if (!packet.TryReadUShort(out mapid) || !packet.TryReadString(out shortname) || !packet.TryReadString(out fullname) || !packet.TryReadInt(out regenx) || !packet.TryReadInt(out regeny) || !packet.TryReadByte(out kingdom) || !packet.TryReadUShort(out viewrange))
                {
                    break;
                }
                Program.serviceInfo.MapsToLoad.Add(new MapInfo(mapid, shortname, fullname, regenx, regeny, kingdom, viewrange)); ;
            }

            Console.Title = "Zepheus.Zone[" + id + "]";
            Log.WriteLine(LogLevel.Info, "Successfully linked with worldserver. [Zone: {0} | Port: {1}]", id, port);
            ZoneAcceptor.Load();
        }

        [InterPacketHandler(InterHeader.ZONECLOSED)]
        public static void HandleZoneClosed(WorldConnector lc, InterPacket packet)
        {
            byte id;
            if (!packet.TryReadByte(out id))
            {
                return;
            }
            ZoneData zd;
            if (Program.Zones.TryRemove(id, out zd))
            {
                Log.WriteLine(LogLevel.Info, "Removed zone {0} from zones (disconnected)", id);
            }
        }

        [InterPacketHandler(InterHeader.ZONEOPENED)]
        public static void HandleZoneOpened(WorldConnector lc, InterPacket packet)
        {
            byte id;
            string ip;
            ushort port;
            int mapcount;
            if (!packet.TryReadByte(out id) || !packet.TryReadString(out ip) || !packet.TryReadUShort(out port) || !packet.TryReadInt(out mapcount))
            {
                return;
            }

            List<MapInfo> maps = new List<MapInfo>();
            for (int j = 0; j < mapcount; j++)
            {
                ushort mapid, viewrange;
                string shortname, fullname;
                int regenx, regeny;
                byte kingdom;
                if (!packet.TryReadUShort(out mapid) || !packet.TryReadString(out shortname) || !packet.TryReadString(out fullname) || !packet.TryReadInt(out regenx) || !packet.TryReadInt(out regeny) || !packet.TryReadByte(out kingdom) || !packet.TryReadUShort(out viewrange))
                {
                    break;
                }
                maps.Add(new MapInfo(mapid, shortname, fullname, regenx, regeny, kingdom, viewrange)); ;
            }

            ZoneData zd;
            if (!Program.Zones.TryGetValue(id, out zd))
            {
                zd = new ZoneData();
            }
            zd.ID = id;
            zd.IP = ip;
            zd.Port = port;
            zd.MapsToLoad = maps;
            Program.Zones[id] = zd;
            Log.WriteLine(LogLevel.Info, "Added zone {0} to zonelist. {1}:{2}", zd.ID, zd.IP, zd.Port);
        }

        [InterPacketHandler(InterHeader.ZONELIST)]
        public static void HandleZoneList(WorldConnector lc, InterPacket packet)
        {
            int amount;
            if (!packet.TryReadInt(out amount))
            {
                return;
            }

            for (int i = 0; i < amount; i++)
            {
                byte id;
                string ip;
                ushort port;
                int mapcount;
                if (!packet.TryReadByte(out id) || !packet.TryReadString(out ip) || !packet.TryReadUShort(out port) || !packet.TryReadInt(out mapcount))
                {
                    return;
                }
                var maps = new List<MapInfo>();
                for (int j = 0; j < mapcount; j++)
                {
                    ushort mapid, viewrange;
                    string shortname, fullname;
                    int regenx, regeny;
                    byte kingdom;
                    if (!packet.TryReadUShort(out mapid) || !packet.TryReadString(out shortname) || !packet.TryReadString(out fullname) || !packet.TryReadInt(out regenx) || !packet.TryReadInt(out regeny) || !packet.TryReadByte(out kingdom) || !packet.TryReadUShort(out viewrange))
                    {
                        break;
                    }
                    maps.Add(new MapInfo(mapid, shortname, fullname, regenx, regeny, kingdom, viewrange)); ;
                }

                ZoneData zd;
                if (!Program.Zones.TryGetValue(id, out zd))
                {
                    zd = new ZoneData();
                }
                zd.ID = id;
                zd.IP = ip;
                zd.Port = port;
                zd.MapsToLoad = maps;
                Program.Zones[id] = zd;
                Log.WriteLine(LogLevel.Info, "Added zone {0} to zonelist. {1}:{2}", zd.ID, zd.IP, zd.Port);
            }
        }


        [InterPacketHandler(InterHeader.CLIENTTRANSFER)]
        public static void HandleTransfer(WorldConnector lc, InterPacket packet)
        {
            byte v;
            if (!packet.TryReadByte(out v))
            {
                return;
            }

            if (v == 0)
            {
                byte admin;
                int accountid;
                string username, hash, hostip;
                if (!packet.TryReadInt(out accountid) || !packet.TryReadString(out username) || !packet.TryReadString(out hash) || !packet.TryReadByte(out admin) || !packet.TryReadString(out hostip))
                {
                    return;
                }
                ClientTransfer ct = new ClientTransfer(accountid, username, admin, hostip, hash);
                ClientManager.Instance.AddTransfer(ct);
            }
            else if (v == 1)
            {
                byte admin;
                int accountid;
                string username, charname, hostip;
                ushort randid;
                if (!packet.TryReadInt(out accountid) || !packet.TryReadString(out username) || !packet.TryReadString(out charname) ||
                    !packet.TryReadUShort(out randid) || !packet.TryReadByte(out admin) || !packet.TryReadString(out hostip))
                {
                    return;
                }
                ClientTransfer ct = new ClientTransfer(accountid, username, charname, randid, admin, hostip);
                ClientManager.Instance.AddTransfer(ct);
            }
        }

        public static void TryAssiging(WorldConnector lc)
        {
            using (var p = new InterPacket(InterHeader.ASSIGN))
            {
                p.WriteStringLen(Settings.Instance.IP);
                lc.SendPacket(p);
            }
        }

        public static void TransferClient(byte zoneID, int accountID, string userName, string charName, ushort randid, byte admin, string hostIP)
        {
            using (var packet = new InterPacket(InterHeader.CLIENTTRANSFERZONE))
            {
                packet.WriteByte(zoneID);
                packet.WriteInt(accountID);
                packet.WriteStringLen(userName);
                packet.WriteStringLen(charName);
                packet.WriteUShort(randid);
                packet.WriteByte(admin);
                packet.WriteStringLen(hostIP);
                WorldConnector.Instance.SendPacket(packet);
            }
        }

        public static void SendWorldMessage(WorldMessageTypes type, string message, string to = "")
        {
            using (var packet = new InterPacket(InterHeader.WORLDMSG))
            {
                packet.WriteStringLen(message);
                packet.WriteByte((byte)type);
                packet.WriteBool(to != "");
                if (to != "")
                {
                    packet.WriteStringLen(to);
                }
                WorldConnector.Instance.SendPacket(packet);
            }
        }
    }
}
