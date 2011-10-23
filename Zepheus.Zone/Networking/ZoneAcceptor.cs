using System;
using System.Net.Sockets;

using Zepheus.FiestaLib.Networking;
using Zepheus.Util;

namespace Zepheus.Zone.Networking
{
    public sealed class ZoneAcceptor : Listener
    {
        public static ZoneAcceptor Instance { get; private set; }

        public ZoneAcceptor(int port)
            : base(port)
        {
            Start();
            Log.WriteLine(LogLevel.Info, "Listening at port {0} for incoming clients.", port);
        }

        public override void OnClientConnect(Socket socket)
        {
            ZoneClient client = new ZoneClient(socket);
            //  ClientManager.Instance.AddClient(client); //They register once authenticated now
            Log.WriteLine(LogLevel.Debug, "Client connected from {0}", client.Host);
        }

        public static bool Load()
        {
            try
            {
                Instance = new ZoneAcceptor(Program.serviceInfo.Port);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "ZoneAcceptor exception: {0}", ex.ToString());
                return false;
            }
        }
    }
}
