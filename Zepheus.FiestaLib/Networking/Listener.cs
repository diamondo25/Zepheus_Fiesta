using System;
using System.Net;
using System.Net.Sockets;

namespace Zepheus.FiestaLib.Networking
{
    public abstract class Listener
    {
        public bool IsRunning { get; private set; }
        public Socket Socket { get; private set; }

        public Listener(int port)
        {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket.Bind(new IPEndPoint(IPAddress.Any, port));
        }

        private void EndAccept(IAsyncResult ar)
        {
            if (!IsRunning) return;
            try
            {
              Socket newclient = Socket.EndAccept(ar);
              OnClientConnect(newclient);
            }
            finally
            {
                Socket.BeginAccept(new AsyncCallback(EndAccept), null);
            }
        }

        public void Stop()
        {
            Socket.Close();
            IsRunning = false;
        }

        public void Start()
        {
            Socket.Listen(10);
            IsRunning = true;
            Socket.BeginAccept(new AsyncCallback(EndAccept), null);
        }

        public abstract void OnClientConnect(Socket socket);
    }
}
