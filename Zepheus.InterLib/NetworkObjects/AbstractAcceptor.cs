using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Zepheus.InterLib.NetworkObjects
{
    public delegate void dOnIncommingConnection(Socket session);
    public class AbstractAcceptor
    {
        public event dOnIncommingConnection OnIncommingConnection;
        private TcpListener _listener;
        public ulong AcceptedClients { get; private set; }

        public AbstractAcceptor(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start(5);
            StartReceive();
        }

        private void StartReceive()
        {
            _listener.BeginAcceptSocket(new AsyncCallback(EndReceive), null);
        }

        private void EndReceive(IAsyncResult iar)
        {
            Socket socket = _listener.EndAcceptSocket(iar);
            if (socket != null && OnIncommingConnection != null)
            {
                OnIncommingConnection(socket);
            }
            AcceptedClients++;
            StartReceive();
        }
    }
}
