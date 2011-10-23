using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Zepheus.FiestaLib.SHN
{
    public sealed class SHNReader : BinaryReader
    {
        public SHNReader(Stream input)
            :base (input)
        {

        }

        public string ReadPaddedString(int length)
        {
            string value = string.Empty;
            int offset = 0;
            byte[] buffer = base.ReadBytes(length);
            while( offset < length && buffer[offset] != 0x00 ) offset++;
            if (length > 0) value = SHNFile.Encoding.GetString(buffer, 0, offset);
            return value;
        }

        public long Seek(long offset, SeekOrigin origin)
        {
            return base.BaseStream.Seek(offset, origin);
        }

        public long Skip(long offset)
        {
            return this.Seek(offset, SeekOrigin.Current);
        }
    }
}
