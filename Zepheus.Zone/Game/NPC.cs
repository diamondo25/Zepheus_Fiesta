using System;

using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Data;
using Zepheus.FiestaLib.Networking;
using Zepheus.Util;
using Zepheus.Zone.Data;

namespace Zepheus.Zone.Game
{
    public sealed class NPC : MapObject
    {
        //public byte Type { get; private set; } //TODO: load from?
        private ShineNPC point;
        public ushort ID { get; private set; }
        public LinkTable Gate { get; private set; }

        public NPC(ShineNPC spoint)
        {
            IsAttackable = false;
            point = spoint;
            LinkTable lt = null;
            if (point.Role == "Gate" && !DataProvider.Instance.NpcLinkTable.TryGetValue(spoint.RoleArg0, out lt))
            {
                Log.WriteLine(LogLevel.Warn, "Could not load LinkTable for NPC {0} LT {1}", point.MobName, point.RoleArg0);
            }
            Gate = lt;

            this.ID = DataProvider.Instance.GetMobIDFromName(point.MobName);
            this.Position = new Vector2(spoint.Coord_X, spoint.Coord_Y);
            if (spoint.Direct < 0)
            {
                this.Rotation = (byte)((360 + spoint.Direct) / 2);
            }
            else
            {
                this.Rotation = (byte)(spoint.Direct / 2);
            }
           
        }

        public override void Update(DateTime date)
        {
            //just for the fun of it?
        }

        public override Packet Spawn()
        {
            Packet packet = new Packet(SH7Type.SpawnSingleObject);
            Write(packet);
            return packet;
        }

        public void Write(Packet packet)
        {
            packet.WriteUShort(this.MapObjectID);
            packet.WriteByte(2); //always 2 (type i bet shown / transparent?)
            packet.WriteUShort(ID);
            packet.WriteInt(this.Position.X);
            packet.WriteInt(this.Position.Y);
            packet.WriteByte(this.Rotation); //TODO: rotation for NPC (from txt official files?)
            if (Gate != null)
            {
                packet.WriteByte(1);
                packet.WriteString(Gate.MapClient, 12);
                packet.Fill(41, 0);
            }
            else
            {
                packet.Fill(54, 0); //find out later
            }
        }
    }
}
