using System;
using System.Collections.Generic;
using Zepheus.Login.Networking;
using Zepheus.Util;

namespace Zepheus.Login
{
    [ServerModule(Util.InitializationStage.Clients)]
    public sealed class ClientManager
    {
        public static ClientManager Instance { get; private set; }

        private List<LoginClient> clients = new List<LoginClient>();

        public bool IsConnected(string IP)
        {
            lock (clients)
            {
                LoginClient client = clients.Find(c => c.Host == IP);
                return (client != null);
            }
        }

        public bool IsLoggedIn(string username)
        {
            lock (clients)
            {
                LoginClient client = clients.Find(c => c.Username == username);
                return (client != null);
            }
        }

        public bool RemoveClient(LoginClient client)
        {
            lock (clients)
            {
                return clients.Remove(client);
            }
        }

       

        public void AddClient(LoginClient client)
        {
            lock (clients)
            {
                clients.Add(client);
            }
        }

        [InitializerMethod]
        public static bool Load()
        {
            try
            {
                Instance = new ClientManager();
                Log.WriteLine(LogLevel.Info, "ClientManager Initialized.");
                return true;
            }
            catch (Exception Exception) {
                Log.WriteLine(LogLevel.Exception, "ClientManager failed to initialize: {0}", Exception.ToString());
                return false;
            }
        }
    }
}
