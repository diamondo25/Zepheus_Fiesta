using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Zepheus.Util;

namespace Zepheus.FiestaLib.Data
{
    public sealed class DropGroupInfo
    {
        public string GroupID { get; private set; }
        public byte MinCount { get; private set; }
        public byte MaxCount { get; private set; }
        public List<ItemInfo> Items { get; private set; }

        public static DropGroupInfo Load(DataTableReaderEx reader)
        {
            DropGroupInfo info = new DropGroupInfo()
            {
                GroupID = reader.GetString("ItemID"),
                MinCount = reader.GetByte("MinQtty"),
                MaxCount = reader.GetByte("MaxQtty"),
                Items = new List<ItemInfo>()
            };
            return info;
        }
    }
}
