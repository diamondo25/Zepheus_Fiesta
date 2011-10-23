using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

using Zepheus.FiestaLib.Networking;
using Zepheus.Util;

namespace Zepheus.Login.Networking
{
    [ServerModule(Util.InitializationStage.Networking)]
    public sealed class LoginAcceptor : Listener
    {
        public static LoginAcceptor Instance { get; private set; }
        public LoginAcceptor(int port)
            : base(port)
        {
            Start();
            Log.WriteLine(LogLevel.Info, "Accepting clients on port {0}", port);
        }

        public override void OnClientConnect(Socket socket)
        {
            LoginClient client = new LoginClient(socket);
            ClientManager.Instance.AddClient(client);
            Log.WriteLine(LogLevel.Debug, "Client connected from {0}", client.Host);
        }

        [InitializerMethod]
        public static bool Load()
        {
            try
            {
                Instance = new LoginAcceptor(Settings.Instance.Port);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "LoginAcceptor exception: {0}", ex.ToString());
                return false;
            }
        }
    }
}
