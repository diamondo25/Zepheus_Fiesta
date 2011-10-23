using System;

using Zepheus.FiestaLib.Data;
using Zepheus.Zone.Data;

namespace Zepheus.Zone.Game
{
    public class DroppedItem
    {
        public short Amount { get; set; }
        public ushort ItemID { get; protected set; }
        public virtual DateTime? Expires { get; set; }
        public ItemInfo Info { get { return DataProvider.Instance.GetItemInfo(this.ItemID); } }

        public DroppedItem()
        {
        }

        public DroppedItem(Item pBase)
        {
            Amount = pBase.Amount;
            ItemID = pBase.ItemID;
            Expires = pBase.Expires;
        }
    }
}
