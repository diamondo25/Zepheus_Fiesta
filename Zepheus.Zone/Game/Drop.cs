using System;
using Zepheus.FiestaLib.Networking;
using Zepheus.Util;

namespace Zepheus.Zone.Game
{
    public class Drop
    {
        public ushort ID { get; set; }
        public Sector MapSector { get; set; }
        public DroppedItem Item { get; private set; }
        public MapObject DroppedBy { get; private set; }
        public DateTime Expire { get; private set; }
        public Vector2 Position { get; private set; }
        public bool CanTake { get; set; }

        public Drop(Item item, MapObject dropper, int x, int y, int secondsToLive)
        {
            if (item is Equip)
            {
                Item = new DroppedEquip(item as Equip);
            }
            else
            {
                Item = new DroppedItem(item);
            }
            DroppedBy = dropper;
            Position = new Vector2(x, y);
            Expire = Program.CurrentTime.AddSeconds(secondsToLive);
            CanTake = true;
        }

        public bool IsExpired(DateTime now)
        {
            return now >= Expire;
        }

        public void Write(Packet packet)
        {
            packet.WriteUShort(ID);
            packet.WriteUShort(Item.ItemID);
            packet.WriteInt(Position.X);
            packet.WriteInt(Position.Y);
            packet.WriteUShort((DroppedBy != null) ? DroppedBy.MapObjectID : (ushort)0xffff);
            packet.WriteByte(CanTake ? (byte)0x08 : (byte)0x00); 
        }
    }
}
