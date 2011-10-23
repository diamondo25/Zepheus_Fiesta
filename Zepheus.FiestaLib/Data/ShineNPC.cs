using System;
using Zepheus.Util;

namespace Zepheus.FiestaLib.Data
{
	public sealed class ShineNPC
	{
		public String MobName { get; private set; }
		public String Map { get; private set; }
		public Int32 Coord_X { get; private set; }
		public Int32 Coord_Y { get; private set; }
		public Int16 Direct { get; private set; }
		public Byte NPCMenu { get; private set; }
		public String Role { get; private set; }
        public String RoleArg0 { get; private set; }

		public static ShineNPC Load(DataTableReaderEx reader)
		{
			ShineNPC info = new ShineNPC
			{
				MobName = reader.GetString("MobName"),
				Map = reader.GetString("Map"),
				Coord_X = reader.GetInt32("Coord-X"),
				Coord_Y = reader.GetInt32("Coord-Y"),
				Direct = reader.GetInt16("Direct"),
				NPCMenu = reader.GetByte("NPCMenu"),
                Role = reader.GetString("Role"),
                RoleArg0 = reader.GetString("RoleArg0"),
			};
			return info;
		}
	}
}
