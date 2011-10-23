using System;

namespace Zepheus.FiestaLib
{
    public static class Extensions
    {
        public static uint ToFiestaTime(this DateTime pValue)
        {
            // Copyright Diamondo25 & CSharp
            uint val = 0;
            val |= (uint)(pValue.Minute << 25);
            val |= (uint)((pValue.Hour & 0x3F) << 19);
            val |= (uint)((pValue.Day & 0x3F) << 13);
            val |= (uint)((pValue.Month & 0x1F) << 8);
            val |= (byte)(pValue.Year - 2000);
            return val;
        }
    }
}
