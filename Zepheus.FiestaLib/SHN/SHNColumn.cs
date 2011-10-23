using System;
using System.Data;
using System.IO;

namespace Zepheus.FiestaLib.SHN
{
    public class SHNColumn : DataColumn
    {
        public int Length { get; private set; }
        public byte TypeByte { get; private set; }

        public void Load(SHNReader reader, ref int unkcount)
        {
            string caption = reader.ReadPaddedString(48);
            if (caption.Trim().Length < 2)
            {
                base.ColumnName = "Undefined " + unkcount.ToString();
                ++unkcount;
            }
            else
            {
                base.ColumnName = caption;
            }
            this.TypeByte = (byte)reader.ReadUInt32();
            this.DataType = GetType(this.TypeByte);
            this.Length = reader.ReadInt32();
        }

        public void Write(SHNWriter writer)
        {
            if (this.ColumnName.StartsWith("Undefined"))
            {
                writer.WritePaddedString(" ", 48);
            }
            else
            {
                writer.WritePaddedString(this.ColumnName, 48);
            }
            writer.Write((uint)this.TypeByte);
            writer.Write((uint)this.Length);
        }

        public static Type GetType(uint pCode)
        {
            switch (pCode)
            {
                case 1:
                case 12:
                    return typeof(byte);
                case 2:
                    return typeof(UInt16);
                case 3:
                case 11:
                    return typeof(UInt32);
                case 5:
                    return typeof(Single);
                case 0x15:
                case 13:
                    return typeof(Int16);
                case 0x10:
                    return typeof(byte);
                case 0x12:
                case 0x1b:
                    return typeof(UInt32);
                case 20:
                    return typeof(SByte);
                case 0x16:
                    return typeof(Int32);
                case 0x18:
                case 0x1a:
                case 9:
                    return typeof(string);
                default:
                    return typeof(object);
            }
        }
    }
}
