using System;

namespace Zepheus.Util
{
    public sealed class ByteArraySegment
    {
        public byte[] Buffer { get; private set; }
        public int Start { get; private set; }
        public int Length { get; private set; }

        public ByteArraySegment(byte[] pBuffer)
        {
            this.Start = 0;
            this.Buffer = pBuffer;
            this.Length = this.Buffer.Length;
        }

        public ByteArraySegment(byte[] pBuffer, int pStart, int pLength)
        {
            if (pStart + pLength > pBuffer.Length)
            {
                throw new ArgumentOutOfRangeException("pLength", "The segment doesn't fit the array bounds.");
            }
            this.Buffer = pBuffer;
            this.Start = pStart;
            this.Length = pLength;
        }

        public bool Advance(int pLength)
        {
            this.Start += pLength;
            this.Length -= pLength;
            return this.Length <= 0;
        }
    }
}
