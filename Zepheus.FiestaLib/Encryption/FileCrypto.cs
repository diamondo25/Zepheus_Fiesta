using System;

namespace Zepheus.FiestaLib.Encryption
{
    class FileCrypto
    {
        public static void Crypt(byte[] data, int index, int length)
        {
            if (((index < 0) | (length < 1)) | ((index + length) > data.Length))
            {
                throw new IndexOutOfRangeException();
            }
            byte num = (byte)length;
            for (int i = length - 1; i >= 0; i--)
            {
                data[i] = (byte)(data[i] ^ num);
                byte num3 = (byte)i;
                num3 = (byte)(num3 & 15);
                num3 = (byte)(num3 + 0x55);
                num3 = (byte)(num3 ^ ((byte)(((byte)i) * 11)));
                num3 = (byte)(num3 ^ num);
                num3 = (byte)(num3 ^ 170);
                num = num3;
            }
        }
    }
}
