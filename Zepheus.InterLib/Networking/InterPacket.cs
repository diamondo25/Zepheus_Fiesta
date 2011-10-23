using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Zepheus.Util;

namespace Zepheus.InterLib.Networking
{
    public sealed class InterPacket : IDisposable
    {

        private MemoryStream memoryStream;
        private BinaryReader reader;
        private BinaryWriter writer;

        public InterHeader OpCode { get; private set; }

        public int Length { get { return (int)this.memoryStream.Length; } }
        public int Cursor { get { return (int)this.memoryStream.Position; } }
        public int Remaining { get { return (int)(this.memoryStream.Length - this.memoryStream.Position); } }

        public InterPacket()
        {
            this.memoryStream = new MemoryStream();
            this.writer = new BinaryWriter(this.memoryStream);
        }

        public InterPacket(ushort pOpCode)
        {
            this.memoryStream = new MemoryStream();
            this.writer = new BinaryWriter(this.memoryStream);
            this.OpCode = (InterHeader)pOpCode;
            WriteUShort(pOpCode);
        }

        public InterPacket(byte[] pData)
        {
            this.memoryStream = new MemoryStream(pData);
            this.reader = new BinaryReader(this.memoryStream);

            ushort opCode;
            this.TryReadUShort(out opCode);
            this.OpCode = (InterHeader)opCode;
        }

        public InterPacket(InterHeader type) : this((ushort)type) { }

        public void Dispose()
        {
            if (this.writer != null) this.writer.Close();
            if (this.reader != null) this.reader.Close();
            this.memoryStream = null;
            this.writer = null;
            this.reader = null;
        }

        ~InterPacket()
        {
            Dispose();
        }

        public void Seek(int offset)
        {
            if (offset > this.Length) throw new IndexOutOfRangeException("Cannot go to packet offset.");
            this.memoryStream.Seek(offset, SeekOrigin.Begin);
        }

        #region Write methods

        public void WriteHexAsBytes(string hexString)
        {
            byte[] bytes = ByteUtils.HexToBytes(hexString);
            WriteBytes(bytes);
        }

        public void SetByte(long pOffset, byte pValue)
        {
            long oldoffset = this.memoryStream.Position;
            this.memoryStream.Seek(pOffset, SeekOrigin.Begin);
            this.writer.Write(pValue);
            this.memoryStream.Seek(oldoffset, SeekOrigin.Begin);
        }

        public void Fill(int pLength, byte pValue)
        {
            for (int i = 0; i < pLength; ++i)
            {
                WriteByte(pValue);
            }
        }

        public void WriteDouble(double pValue)
        {
            this.writer.Write(pValue);
        }

        public void WriteBool(bool pValue)
        {
            this.writer.Write(pValue);
        }

        public void WriteByte(byte pValue)
        {
            this.writer.Write(pValue);
        }

        public void WriteSByte(sbyte pValue)
        {
            this.writer.Write(pValue);
        }

        public void WriteBytes(byte[] pBytes)
        {
            this.writer.Write(pBytes);
        }

        public void WriteUShort(ushort pValue)
        {
            this.writer.Write(pValue);
        }

        public void WriteShort(short pValue)
        {
            this.writer.Write(pValue);
        }

        public void WriteUInt(uint pValue)
        {
            this.writer.Write(pValue);
        }

        public void WriteInt(int pValue)
        {
            this.writer.Write(pValue);
        }

        public void WriteFloat(float pValue)
        {
            this.writer.Write(pValue);
        }

        public void WriteULong(ulong pValue)
        {
            this.writer.Write(pValue);
        }

        public void WriteLong(long pValue)
        {
            this.writer.Write(pValue);
        }

        public void WriteStringLen(string pValue, bool addNullTerminator = false)
        {
            if (addNullTerminator) pValue += char.MinValue;
            if (pValue.Length > 0xFF)
            {
                throw new Exception("Too long!");
            }
            WriteByte((byte)pValue.Length);
            WriteBytes(Encoding.ASCII.GetBytes(pValue));
            // NOTE: Some messages might be NULL terminated!
        }

        public void WriteString(string pValue)
        {
            WriteBytes(Encoding.ASCII.GetBytes(pValue));
            // NOTE: Some messages might be NULL terminated!
        }

        public void WriteString(string pValue, int pLen)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(pValue);
            if (buffer.Length > pLen)
            {
                throw new ArgumentException("pValue is bigger than pLen", "pLen");
            }
            else
            {
                WriteBytes(buffer);
                for (int i = 0; i < pLen - buffer.Length; i++)
                {
                    WriteByte(0);
                }
            }
        }



        public void Write(object val)
        {
            if (val is byte) WriteByte((byte)val);
            else if (val is sbyte) WriteSByte((sbyte)val);
            else if (val is byte[]) WriteBytes((byte[])val);
            else if (val is short) WriteShort((short)val);
            else if (val is ushort) WriteUShort((ushort)val);
            else if (val is int) WriteInt((int)val);
            else if (val is uint) WriteUInt((uint)val);
            else if (val is long) WriteLong((long)val);
            else if (val is ulong) WriteULong((ulong)val);
            else if (val is double) WriteDouble((double)val);
            else if (val is float) WriteFloat((float)val);
            else if (val is string) WriteStringLen((string)val);
            else if (val is List<ushort>)
            {
                var list = val as List<ushort>;
                WriteInt(list.Count);
                foreach (var v in list) WriteUShort(v);
            }
            else if (val is List<int>)
            {
                var list = val as List<int>;
                WriteInt(list.Count);
                foreach (var v in list) WriteInt(v);
            }
            else
            {
                throw new Exception("Unknown type: " + val.ToString());
            }
        }

        #endregion

        #region Read methods

        public bool ReadSkip(int pLength)
        {
            if (Remaining < pLength) return false;

            this.memoryStream.Seek(pLength, SeekOrigin.Current);
            return true;
        }

        public bool TryReadBool(out bool pValue)
        {
            pValue = false;
            if (Remaining < 1) return false;
            pValue = this.reader.ReadBoolean();
            return true;
        }

        public bool TryReadByte(out byte pValue)
        {
            pValue = 0;
            if (Remaining < 1) return false;
            pValue = this.reader.ReadByte();
            return true;
        }

        public bool TryReadBytes(int pLength, out byte[] pValue)
        {
            pValue = new byte[] {};
            if (Remaining < pLength) return false;
            pValue = this.reader.ReadBytes(pLength);
            return true;
        }

        public bool TryReadSByte(out sbyte pValue)
        {
            pValue = 0;
            if (Remaining < 1) return false;
            pValue = this.reader.ReadSByte();
            return true;
        }

        // UInt16 is more conventional
        public bool TryReadUShort(out ushort pValue)
        {
            pValue = 0;
            if (Remaining < 2) return false;
            pValue = this.reader.ReadUInt16();
            return true;
        }

        // Int16 is more conventional
        public bool TryReadShort(out short pValue)
        {
            pValue = 0;
            if (Remaining < 2) return false;
            pValue = this.reader.ReadInt16();
            return true;
        }

        public bool TryReadFloat(out float pValue)
        {
            pValue = 0;
            if (Remaining < 2) return false;
            pValue = this.reader.ReadSingle();
            return true;
        }

        // UInt32 is better
        public bool TryReadUInt(out uint pValue)
        {
            pValue = 0;
            if (Remaining < 4) return false;
            pValue = this.reader.ReadUInt32();
            return true;
        }

        // Int32
        public bool TryReadInt(out int pValue)
        {
            pValue = 0;
            if (Remaining < 4) return false;
            pValue = this.reader.ReadInt32();
            return true;
        }

        // UInt64
        public bool TryReadULong(out ulong pValue)
        {
            pValue = 0;
            if (Remaining < 8) return false;
            pValue = this.reader.ReadUInt64();
            return true;
        }

        // Int64
        public bool TryReadLong(out long pValue)
        {
            pValue = 0;
            if (Remaining < 8) return false;
            pValue = this.reader.ReadInt64();
            return true;
        }

        public bool TryReadString(out string pValue)
        {
            pValue = "";
            if (this.Remaining < 1) return false;
            byte len;
            this.TryReadByte(out len);
            if (this.Remaining < len) return false;
            return TryReadString(out pValue, len);
        }

        public bool TryReadString(out string pValue, int pLen)
        {
            pValue = "";
            if (Remaining < pLen) return false;

            byte[] buffer = new byte[pLen];
            ReadBytes(buffer);
            int length = 0;
            if (buffer[pLen - 1] != 0)
            {
                length = pLen;
            }
            else
            {
                while (buffer[length] != 0x00 && length < pLen)
                {
                    length++;
                }
            }
            if (length > 0)
            {
                pValue = Encoding.ASCII.GetString(buffer, 0, length);
            }

            return true;
        }

        public bool ReadBytes(byte[] pBuffer)
        {
            if (Remaining < pBuffer.Length) return false;
            this.memoryStream.Read(pBuffer, 0, pBuffer.Length);
            return true;
        }

        #endregion

        public byte[] ToArray()
        {
            return memoryStream.ToArray();
        }

        public string Dump()
        {
            return ByteUtils.BytesToHex(this.memoryStream.ToArray(), string.Format("Packet (0x{0} - {1}): ", this.OpCode.ToString("X4"), this.Length));
        }

        public override string ToString()
        {
            byte[] buf = new byte[this.Length - 2];
            Buffer.BlockCopy(this.memoryStream.ToArray(), 2, buf, 0, buf.Length);
            return string.Format("Opcode: 0x{0:X4} Length: {1} Data: {2}", (ushort)this.OpCode, buf.Length, ByteUtils.BytesToHex(buf));
        }
    }
}
