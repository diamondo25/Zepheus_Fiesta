using System;
using System.Collections.Generic;
using System.Data;

namespace Zepheus.Util
{
    /// <summary>
    /// Class to read a DataTable by ColumnName, as .NET one doesn't support this.
    /// </summary>
    public sealed class DataTableReaderEx : IDisposable
    {
        private bool isDisposed;
        private DataTableReader reader;
        private Dictionary<string, int> columns = new Dictionary<string, int>();

        public DataTableReaderEx(DataTable pTable)
        {
            this.isDisposed = false;
            //Thank you MicroSoft, for making it a sealed class...
            this.reader = new DataTableReader(pTable);
            for (int i = 0; i < pTable.Columns.Count; ++i)
            {
                this.columns.Add(pTable.Columns[i].Caption.ToLower(), i);
            }
        }

        public bool Read()
        {
            return this.reader.Read();
        }

        public string GetString(string pColumnName)
        {
            return this.reader.GetString(GetIndex(pColumnName));
        }

        public int GetInt32(string pColumnName)
        {
            return this.reader.GetInt32(GetIndex(pColumnName));
        }

        public uint GetUInt32(string pColumnName)
        {
            return Convert.ToUInt32(this.reader.GetValue(GetIndex(pColumnName)));
        }

        public ulong GetUInt64(string pColumnName)
        {
            return Convert.ToUInt64(this.reader.GetValue(GetIndex(pColumnName)));
        }

        public ushort GetUInt16(string pColumnName)
        {
            //weird that this wasn't implemented at all
            return Convert.ToUInt16(this.reader.GetValue(GetIndex(pColumnName)));
        }

        public short GetInt16(string pColumnName)
        {
            return Convert.ToInt16(this.reader.GetValue(GetIndex(pColumnName)));
        }

        public byte GetByte(string pColumnName)
        {
            return Convert.ToByte(this.reader.GetValue(GetIndex(pColumnName)));
        }

        public float GetFloat(string pColumnName)
        {
            return Convert.ToSingle(this.reader.GetValue(GetIndex(pColumnName)));
        }

        public bool GetBoolean(string pColumnName)
        {
            return Convert.ToBoolean(this.reader.GetValue(GetIndex(pColumnName)));
        }

        public int GetIndex(string pName)
        {
            int offset;
            if (this.columns.TryGetValue(pName.ToLower(), out offset))
            {
                return offset;
            }
            else
            {
                throw new Exception("Column name not found: " + pName);
            }
        }

        public Type GetType(string pName)
        {
            return this.reader.GetValue(GetIndex(pName)).GetType();
        }

        ~DataTableReaderEx()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!this.isDisposed)
            {
                this.reader.Dispose();
                this.columns.Clear();
                this.columns = null;
                this.isDisposed = true;
            }
        }

    }
}
