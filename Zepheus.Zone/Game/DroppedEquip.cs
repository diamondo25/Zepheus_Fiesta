
namespace Zepheus.Zone.Game
{
    public class DroppedEquip : DroppedItem
    {
        public byte Dex { get; set; }
        public byte Str { get; set; }
        public byte End { get; set; }
        public byte Int { get; set; }
        public byte Spr { get; set; }
        public byte Upgrades { get; set; }

        public DroppedEquip(Equip pBase)
        {
            this.Amount = 1;
            this.Expires = pBase.Expires;
            this.Dex = pBase.Dex;
            this.Str = pBase.Str;
            this.End = pBase.End;
            this.Int = pBase.Int;
            this.Upgrades = pBase.Upgrades;
            this.ItemID = pBase.ItemID;
        }
    }
}
