using System;

namespace Zepheus.InterLib.Networking
{
    public sealed class InterPacketReceivedEventArgs : EventArgs
    {
        public InterPacket Packet { get; private set; }
        public InterClient Client { get; private set; }
        public InterPacketReceivedEventArgs(InterPacket packet, InterClient client)
        {
            this.Packet = packet;
            this.Client = client;
        }
    }
}
