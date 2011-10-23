using System;
using System.Net.Sockets;

using Zepheus.FiestaLib.Networking;
using Zepheus.Util;

namespace Zepheus.World.Networking
{
    [ServerModule(Util.InitializationStage.Networking)]
    public sealed class WorldAcceptor : Listener
    {
        public static WorldAcceptor Instance { get; private set; }

        public WorldAcceptor(int port)
            : base(port)
        {
            Start();
            Log.WriteLine(LogLevel.Info, "WorldAcceptor ready at {0}", port);
        }

        public override void OnClientConnect(Socket socket)
        {
            WorldClient client = new WorldClient(socket);
            ClientManager.Instance.AddClient(client);
            Log.WriteLine(LogLevel.Debug, "Client connected from {0}", client.Host);
        }

        [InitializerMethod]
        public static bool Load()
        {
            try
            {
                Instance = new WorldAcceptor(Settings.Instance.Port);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "WorldAcceptor exception: {0}", ex.ToString());
                return false;
            }
        }
    }
}
