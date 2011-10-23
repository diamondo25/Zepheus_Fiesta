using System.Net.Sockets;

using Zepheus.InterLib.NetworkObjects;
using Zepheus.Util;

namespace Zepheus.World.InterServer
{
    [ServerModule(Util.InitializationStage.Services)]
    public sealed class ZoneAcceptor : AbstractAcceptor
    {
        public static ZoneAcceptor Instance { get; private set; }

        public ZoneAcceptor(int port) : base(port)
        {
            this.OnIncommingConnection += new dOnIncommingConnection(WorldAcceptor_OnIncommingConnection);
            Log.WriteLine(LogLevel.Info, "Listening on port {0} for zones.", port);
        }

        private void WorldAcceptor_OnIncommingConnection(Socket session)
        {
            // So something with it X:
            Log.WriteLine(LogLevel.Info, "Incoming connection from {0}", session.RemoteEndPoint);
            ZoneConnection wc = new ZoneConnection(session);
        }

        [InitializerMethod]
        public static bool Load()
        {
            return Load(Settings.Instance.InterServerPort);
        }

        public static bool Load(int port)
        {
            try
            {
                Instance = new ZoneAcceptor(port);
                return true;
            }
            catch { return false; }
        }

    }
}
