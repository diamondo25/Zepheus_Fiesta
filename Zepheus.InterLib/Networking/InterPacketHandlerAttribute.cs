using System;

namespace Zepheus.InterLib.Networking
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class InterPacketHandlerAttribute : Attribute
    {
        public InterHeader Header { get; private set; }

        public InterPacketHandlerAttribute(InterHeader ih)
        {
            Header = ih;
        }
    }
}
