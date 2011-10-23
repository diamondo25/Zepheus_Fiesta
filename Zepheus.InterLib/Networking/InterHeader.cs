using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zepheus.InterLib.Networking
{
    public enum InterHeader : ushort
    {
        PING = 0x0000,
        PONG = 0x0001,
        IVS = 0x0002,

        AUTH = 0x0010,

        ASSIGN = 0x0100,
        ASSIGNED = 0x0101,

        CLIENTTRANSFER = 0x1000,
        CLIENTTRANSFERZONE = 0x1001,

        ZONEOPENED = 0x2000,
        ZONECLOSED = 0x2001,
        ZONELIST = 0x2002,

        WORLDMSG = 0x3000,
    }
}
