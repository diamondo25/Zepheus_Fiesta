using System;
using Zepheus.Util;

namespace Zepheus.FiestaLib.Data
{
    public sealed class RecallCoordinate
    {
        public String ItemIndex { get; private set; }
        public String MapName { get; private set; }
        public Int16 LinkX { get; private set; }
        public Int16 LinkY { get; private set; }

        public static RecallCoordinate Load(DataTableReaderEx reader)
        {
            RecallCoordinate info = new RecallCoordinate
            {
                ItemIndex = reader.GetString("ItemIndex"),
                MapName = reader.GetString("MapName"),
                LinkX = reader.GetInt16("LinkX"),
                LinkY = reader.GetInt16("LinkY"),
            };
            return info;
        }
    }
}