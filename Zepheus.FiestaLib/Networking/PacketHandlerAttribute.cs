using System;

namespace Zepheus.FiestaLib.Networking
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PacketHandlerAttribute : Attribute
    {
        public byte Header { get; private set; }
        public byte Type { get; private set; }

        private PacketHandlerAttribute(byte header, byte type)
        {
            Header = header;
            Type = type;
        }

        public PacketHandlerAttribute(CH2Type type) : this(2, (byte)type) { }
        public PacketHandlerAttribute(CH3Type type) : this(3, (byte)type) { }
        public PacketHandlerAttribute(CH4Type type) : this(4, (byte)type) { }
        public PacketHandlerAttribute(CH5Type type) : this(5, (byte)type) { }
        public PacketHandlerAttribute(CH6Type type) : this(6, (byte)type) { }
        public PacketHandlerAttribute(CH7Type type) : this(7, (byte)type) { }
        public PacketHandlerAttribute(CH8Type type) : this(8, (byte)type) { }
        public PacketHandlerAttribute(CH9Type type) : this(9, (byte)type) { }
        public PacketHandlerAttribute(CH12Type type) : this(12, (byte)type) { }
        public PacketHandlerAttribute(CH15Type type) : this(15, (byte)type) { }
        public PacketHandlerAttribute(CH20Type type) : this(20, (byte)type) { }
        public PacketHandlerAttribute(CH28Type type) : this(28, (byte)type) { }
        public PacketHandlerAttribute(CH29Type type) : this(29, (byte)type) { }
        public PacketHandlerAttribute(CH31Type type) : this(31, (byte)type) { }
    }
}
