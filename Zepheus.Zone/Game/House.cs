
using Zepheus.FiestaLib.Data;
using Zepheus.FiestaLib.Networking;
using Zepheus.Zone.Data;

namespace Zepheus.Zone.Game
{
    public class House
    {
        public enum HouseType
        {
            Resting,
            SellingVendor,
            BuyingVendor,
        }

        public ushort ItemID { get; private set; }
        public string Name { get; private set; }
        public HouseType Type { get; private set; }
        public ZoneCharacter Owner { get; private set; }
        public MiniHouseInfo Info { get { return (DataProvider.Instance.MiniHouses.ContainsKey(ItemID) ? DataProvider.Instance.MiniHouses[ItemID] : null); } }

        public House(ZoneCharacter pOwner, HouseType pType, ushort pItemID = 0, string pName = "")
        {
            this.Owner = pOwner;
            this.Type = pType;
            this.ItemID = pItemID;
            this.Name = pName;
        }

        public void WritePacket(Packet pPacket)
        {
            pPacket.WriteUShort(ItemID);
            if (this.Type != HouseType.Resting)
            {
                pPacket.Fill(10, 0xFF); // Unknown

                pPacket.WriteString(this.Name, 30);
            }
            else
            {
                pPacket.WriteHexAsBytes("BE 02 FA 01 F8 01");
                pPacket.Fill(34, 0xFF); // No idea!?
            }
            pPacket.WriteByte(0xFF);
        }
    }
}
