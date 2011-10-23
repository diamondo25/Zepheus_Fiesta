using System;

namespace Zepheus.FiestaLib.Networking
{
    public sealed class PacketReceivedEventArgs : EventArgs
    {
        public Packet Packet { get; private set; }
        public PacketReceivedEventArgs(Packet packet)
        {
            this.Packet = packet;
        }
    }
}
