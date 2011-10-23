using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Zepheus.FiestaLib.SHN
{
    public sealed class SHNWriter : BinaryWriter
    {
        public SHNWriter(Stream input)
            : base (input)
        {

        }

        public void WritePaddedString(string value, int Length)
        {
            byte[] data = SHNFile.Encoding.GetBytes(value);
            if (data.Length > Length)
            {
                throw new ArgumentOutOfRangeException("Padded string is too long");
            }
            this.Write(data);
            Fill(0, Length - data.Length);
        }

        public void Fill(byte value, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                base.Write(value);
            }
        }
    }
}
