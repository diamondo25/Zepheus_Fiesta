using System;
using Zepheus.Database;
using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Data;
using Zepheus.FiestaLib.Networking;
using Zepheus.Zone.Data;

namespace Zepheus.Zone.Game
{
    public class Item
    {
        private DatabaseItem _item;
        public virtual short Amount { get { return _item.Amount; } set { _item.Amount = value; } }
        public ushort ItemID { get; protected set; } 
        public virtual Character Owner { get { return _item.Character; } set { _item.Character = value; } }
        public virtual DateTime? Expires { get; set; }
        public virtual sbyte Slot { get { return (sbyte)_item.Slot; } set { _item.Slot = value; } }
        public ItemInfo Info { get { return DataProvider.Instance.GetItemInfo(this.ItemID); } }

        public Item(DatabaseItem item)
        {
            _item = item;
            ItemID = (ushort)item.ObjectID;
        }

        public Item(DroppedItem item, ZoneCharacter pNewOwner, sbyte pSlot)
        {
            DatabaseItem dbi = new DatabaseItem();
            dbi.Amount = item.Amount;
            dbi.Character = pNewOwner.character;
            dbi.ObjectID = item.ItemID;
            dbi.Slot = pSlot;
            Program.Entity.AddToDatabaseItems(dbi);
            Program.Entity.SaveChanges();
            _item = dbi;
            ItemID = item.ItemID;
            pNewOwner.InventoryItems.Add(pSlot, this);
        }

        public Item()
        {
        }

        public virtual void Remove()
        {
            if (_item != null)
            {
                Program.Entity.DeleteObject(_item);
                Program.Entity.SaveChanges();
            }
        }

        public uint GetExpirationTime()
        {
            return this.Expires.HasValue ? this.Expires.Value.ToFiestaTime() : 0;
        }

        public virtual void WriteInfo(Packet packet)
        {
            packet.WriteByte(5); //entry length
            packet.WriteSByte(this.Slot);
            packet.WriteByte(0x24); //status?
            WriteItemStats(packet);
        }

        public void WriteItemStats(Packet packet)
        {
            packet.WriteUShort(ItemID);
            packet.WriteByte((byte)Amount);
        }
    }
}
