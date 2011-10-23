using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zepheus.InterLib.Encryption
{
    public sealed class InterCrypto
    {

        public static byte[] EncryptData(byte[] IV, byte[] data)
        {
            byte[] ret = new byte[data.Length];
            Buffer.BlockCopy(data, 0, ret, 0, data.Length);
            // Some simple encryption...
            for (int i = 0; i < ret.Length; i++)
            {
                ret[ret.Length - 1 - i] ^= IV[i % 16];
                ret[i] ^= IV[i % 16];
            }

            return ret;
        }

        public static byte[] DecryptData(byte[] IV, byte[] data)
        {
            byte[] ret = new byte[data.Length];
            Buffer.BlockCopy(data, 0, ret, 0, data.Length);
            // Some simple encryption...
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] ^= IV[i % 16];
                ret[ret.Length - 1 - i] ^= IV[i % 16];
            }

            return ret;
        }
    }
}
