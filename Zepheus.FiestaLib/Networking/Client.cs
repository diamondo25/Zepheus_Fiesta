using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Zepheus.FiestaLib.Encryption;
using Zepheus.Util;

namespace Zepheus.FiestaLib.Networking
{
    public class Client
    {
        private const int MaxReceiveBuffer = 16384; //16kb

        private int mDisconnected;
        private byte[] mReceiveBuffer;
        private int mReceiveStart;
        private int mReceiveLength;
        private ConcurrentQueue<ByteArraySegment> mSendSegments;
        private int mSending;
        private NetCrypto crypto;
        private ushort mReceivingPacketLength;
        private byte HeaderLength = 0;

        public Socket Socket { get; private set; }
        public string Host { get; private set; }
        public event EventHandler<PacketReceivedEventArgs> OnPacket;
        public event EventHandler<SessionCloseEventArgs> OnDisconnect;

        public Client(Socket socket)
        {
            mSendSegments = new ConcurrentQueue<ByteArraySegment>();
            this.Socket = socket;
            Host =  ((IPEndPoint)Socket.RemoteEndPoint).Address.ToString();
            mReceiveBuffer = new byte[MaxReceiveBuffer];
            Start();
        }

        public void Start()
        {
            crypto = new NetCrypto(MathUtils.RandomizeShort(498));
            SendHandshake(crypto.XorPos);
            BeginReceive();
        }

        public void Disconnect()
        {
            if (Interlocked.CompareExchange(ref mDisconnected, 1, 0) == 0)
            {
                try
                {
                    Socket.Shutdown(SocketShutdown.Both);
                }
                catch { }
                if (OnDisconnect != null)
                {
                    this.OnDisconnect(this, SessionCloseEventArgs.ConnectionTerminated); //TODO: split
                }
            }
        }

        private void BeginReceive()
        {
            if (mDisconnected != 0) return;
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += EndReceive;
            args.SetBuffer(mReceiveBuffer, mReceiveStart, mReceiveBuffer.Length - (mReceiveStart + mReceiveLength));
            try
            {
                if (!this.Socket.ReceiveAsync(args))
                {
                    EndReceive(this, args);
                }
            }
            catch (ObjectDisposedException ex)
            {
                Log.WriteLine(LogLevel.Exception,"Error at BeginReceive: {0}",  ex.ToString());
                Disconnect();
            }
        }

        private void EndReceive(object sender, SocketAsyncEventArgs pArguments)
        {
            if (mDisconnected != 0) return;
            if (pArguments.BytesTransferred <= 0)
            {
                Disconnect();
                return;
            }
            mReceiveLength += pArguments.BytesTransferred;

            while (mReceiveLength > 1)
            {
                //parse headers
                //TODO: proper rewrite!
                if (mReceivingPacketLength == 0)
                {
                    mReceivingPacketLength = mReceiveBuffer[mReceiveStart];
                    if (mReceivingPacketLength == 0)
                    {
                        if (mReceiveLength >= 3)
                        {
                            mReceivingPacketLength = BitConverter.ToUInt16(mReceiveBuffer, mReceiveStart + 1);
                            HeaderLength = 3;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        HeaderLength = 1;
                    }
                }

                //parse packets
                if (mReceivingPacketLength > 0 && mReceiveLength >= mReceivingPacketLength + HeaderLength)
                {
                    byte[] packetData = new byte[mReceivingPacketLength];
                    Buffer.BlockCopy(mReceiveBuffer, mReceiveStart + HeaderLength, packetData, 0, mReceivingPacketLength);
                    crypto.Crypt(packetData, 0, mReceivingPacketLength);
                    if (OnPacket != null)
                    {
                        Packet packet = new Packet(packetData);
                        if (packet.Header > 45)
                        {
                            Log.WriteLine(LogLevel.Warn, "Header out of range from {0} ({1}|{2})", Host, packet.Header, packet.Type);
                            Disconnect();
                        }
                        else
                        {
                            this.OnPacket(this, new PacketReceivedEventArgs(packet));
                        }
                    }

                    //we reset this packet
                    mReceiveStart += mReceivingPacketLength + HeaderLength;
                    mReceiveLength -= mReceivingPacketLength + HeaderLength;
                    mReceivingPacketLength = 0;
                }
                else break;
            }

            if (mReceiveLength == 0) mReceiveStart = 0;
            else if (mReceiveStart > 0 && (mReceiveStart + mReceiveLength) >= mReceiveBuffer.Length)
            {
                Buffer.BlockCopy(mReceiveBuffer, mReceiveStart, mReceiveBuffer, 0, mReceiveLength);
                mReceiveStart = 0;
            }
            if (mReceiveLength == mReceiveBuffer.Length)
            {
                Disconnect();
            }
            else BeginReceive();
            pArguments.Dispose();
        }

        private void SendHandshake(short pXorPos)
        {
            Packet packet = new Packet(SH2Type.SetXorKeyPosition);
            packet.WriteShort(pXorPos);
            SendPacket(packet);
        }

        public void Send(byte[] pBuffer)
        {
            if (mDisconnected != 0) return;
            // Everything we send is from the main thread, so no async sending.
         /*   int len = pBuffer.Length, offset = 0;
            while (true)
            {
                int send = Socket.Send(pBuffer, offset, len, SocketFlags.None);
                if (send == 0)
                {
                    Disconnect();
                    return;
                }
                offset += send;
                if (offset == len) break;
            } */
            
            mSendSegments.Enqueue(new ByteArraySegment(pBuffer));
            if (Interlocked.CompareExchange(ref mSending, 1, 0) == 0)
            {
                BeginSend();
            }
        }

        public void SendPacket(Packet pPacket)
        {
           Send(pPacket.ToArray());
        }

        private void BeginSend()
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            ByteArraySegment segment;
            if (mSendSegments.TryPeek(out segment))
            {
                args.Completed += EndSend;
                args.SetBuffer(segment.Buffer, segment.Start, segment.Length);
                // args.SetBuffer(segment.Buffer, segment.Start, Math.Min(segment.Length, 1360));
                try
                {
                    if (!this.Socket.SendAsync(args))
                    {
                            EndSend(this, args);
                    }
                }
                catch (ObjectDisposedException ex)
                {
                    Log.WriteLine(LogLevel.Exception, "Error at BeginSend: {0}", ex.ToString());
                    Disconnect();
                }
            }
        }

        private void EndSend(object sender, SocketAsyncEventArgs pArguments)
        {
            if (mDisconnected != 0) return;

            if (pArguments.BytesTransferred <= 0)
            {
                Disconnect();
                return;
            }

            ByteArraySegment segment;
            if (mSendSegments.TryPeek(out segment))
            {
                if (segment.Advance(pArguments.BytesTransferred))
                {
                    ByteArraySegment seg;
                    mSendSegments.TryDequeue(out seg); //we try to get it out
                }

                if (mSendSegments.Count > 0)
                {
                    this.BeginSend();
                }
                else
                {
                    mSending = 0;
                }
            }
            pArguments.Dispose(); //clears out the whole async buffer
        }
    }
}
