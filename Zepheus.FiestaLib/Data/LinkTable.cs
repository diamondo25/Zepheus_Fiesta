using System;
using Zepheus.Util;

namespace Zepheus.FiestaLib.Data
{
 	public sealed class LinkTable
	{
		public String argument { get; private set; }
		public String MapServer { get; private set; }
		public String MapClient { get; private set; }
		public Int32 Coord_X { get; private set; }
		public Int32 Coord_Y { get; private set; }
		public Int16 Direct { get; private set; }
		public Int16 LevelFrom { get; private set; }
		public Int16 LevelTo { get; private set; }
		public Byte Party { get; private set; }

		public static LinkTable Load(DataTableReaderEx reader)
		{
			LinkTable info = new LinkTable
			{
				argument = reader.GetString("argument"),
				MapServer = reader.GetString("MapServer"),
				MapClient = reader.GetString("MapClient"),
				Coord_X = reader.GetInt32("Coord-X"),
				Coord_Y = reader.GetInt32("Coord-Y"),
				Direct = reader.GetInt16("Direct"),
				LevelFrom = reader.GetInt16("LevelFrom"),
				LevelTo = reader.GetInt16("LevelTo"),
				Party = reader.GetByte("Party"),
			};
			return info;
		}
	}
}
