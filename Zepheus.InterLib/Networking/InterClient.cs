using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Zepheus.InterLib.Encryption;
using Zepheus.Util;

namespace Zepheus.InterLib.Networking
{
    public class InterClient
    {
        private const int MaxReceiveBuffer = 0x4000; //16kb

        private int mDisconnected;
        private byte[] mReceiveBuffer;
        private int mReceiveStart;
        private int mReceiveLength;
        private ConcurrentQueue<ByteArraySegment> mSendSegments;
        private int mSending;
        private byte[] mIVSend;
        private byte[] mIVRecv;
        private int mReceivingPacketLength;
        private bool mIVs = false;
        private bool mHeader = true;

        public bool Assigned { get; set; }

        public Socket Socket { get; private set; }
        public string Host { get; private set; }
        public event EventHandler<InterPacketReceivedEventArgs> OnPacket;
        public event EventHandler<SessionCloseEventArgs> OnDisconnect;

        public InterClient(Socket socket)
        {
            mSendSegments = new ConcurrentQueue<ByteArraySegment>();
            this.Socket = socket;
            Host = ((IPEndPoint)Socket.RemoteEndPoint).Address.ToString();
            // mReceiveBuffer = new byte[4];
            mReceiveBuffer = new byte[MaxReceiveBuffer];
            mReceivingPacketLength = 4; // Header length
            Assigned = false;
            Start();
            SendIVs();
        }

        public void Start()
        {
            Random rnd = new Random();
            mIVRecv = new byte[16];
            mIVSend = new byte[16];
            rnd.NextBytes(mIVSend);

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
            args.SetBuffer(mReceiveBuffer, mReceiveStart, mReceivingPacketLength - mReceiveStart);
            try
            {
                if (!this.Socket.ReceiveAsync(args))
                {
                    EndReceive(this, args);
                }
            }
            catch (ObjectDisposedException ex)
            {
                Log.WriteLine(LogLevel.Exception, "Error at BeginReceive: {0}", ex.ToString());
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

            if (mReceivingPacketLength == mReceiveLength)
            {
                if (mHeader) //parse headers
                {
                    mReceivingPacketLength = BitConverter.ToInt32(mReceiveBuffer, 0);
                    mReceiveLength = 0;
                    mReceiveStart = 0;
                    mHeader = false;
                    // mReceiveBuffer = new byte[mReceivingPacketLength];
                }
                else
                { //parse packets
                    byte[] packetData = new byte[mReceivingPacketLength];
                    Buffer.BlockCopy(mReceiveBuffer, 0, packetData, 0, mReceivingPacketLength);
                    if (!mIVs)
                    {
                        InterPacket packet = new InterPacket(packetData);
                        if (packet.OpCode == InterHeader.IVS)
                        {
                            Log.WriteLine(LogLevel.Info, "IV data received");
                            packet.ReadBytes(mIVRecv);
                            mIVs = true;
                        }
                        else
                        {
                            Log.WriteLine(LogLevel.Info, "Got wrong packet.");
                            Disconnect();
                        }
                    }
                    else
                    {
                        packetData = InterCrypto.DecryptData(mIVRecv, packetData);
                        if (OnPacket != null)
                        {
                            InterPacket packet = new InterPacket(packetData);
                            this.OnPacket(this, new InterPacketReceivedEventArgs(packet, this));
                        }
                    }
                    //we reset this packet
                    mReceivingPacketLength = 4;
                    mReceiveLength = 0;
                    mReceiveStart = 0;
                    mHeader = true;
                    // mReceiveBuffer = new byte[4];
                }
            }
            else
            {
                mReceiveStart += mReceivingPacketLength;
            }

            BeginReceive();
            pArguments.Dispose();
        }

        private void SendIVs()
        {
            InterPacket packet = new InterPacket(InterHeader.IVS);
            packet.WriteBytes(mIVSend);
            SendPacket(packet, false);
        }

        public void SendInterPass(string what)
        {
            InterPacket packet = new InterPacket(InterHeader.AUTH);
            packet.WriteStringLen(what);
            SendPacket(packet);
        }

        public void Send(byte[] pBuffer)
        {
            if (mDisconnected != 0) return;
            mSendSegments.Enqueue(new ByteArraySegment(pBuffer));
            if (Interlocked.CompareExchange(ref mSending, 1, 0) == 0)
            {
                BeginSend();
            }
        }

        public void SendPacket(InterPacket pPacket, bool crypto = true)
        {
            byte[] data = new byte[pPacket.Length + 4];
            Buffer.BlockCopy(crypto ? InterCrypto.EncryptData(mIVSend, pPacket.ToArray()) : pPacket.ToArray(), 0, data, 4, pPacket.Length);
            Buffer.BlockCopy(BitConverter.GetBytes((int)pPacket.Length), 0, data, 0, 4);
            Send(data);
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
            pArguments.Dispose();
        }
    }
}
