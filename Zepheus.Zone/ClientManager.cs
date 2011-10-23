using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers;

using Zepheus.Services.DataContracts;
using Zepheus.Util;
using Zepheus.Zone.Networking;

namespace Zepheus.Zone
{
    [ServerModule(Util.InitializationStage.DataStore)]
    public sealed class ClientManager
    {
        public static ClientManager Instance { get; private set; }
        public int ZoneLoad { get { return clientCount(); } }

        //private List<ZoneClient> clients = new List<ZoneClient>();
        private ConcurrentDictionary<string, ZoneClient> clientsByName = new ConcurrentDictionary<string, ZoneClient>();
        private ConcurrentDictionary<string, ClientTransfer> transfers = new ConcurrentDictionary<string, ClientTransfer>();
        private Timer expirator;
        private int transferTimeout = 1;

        public ClientManager()
        {
            expirator = new Timer(1000);
            expirator.Elapsed += new ElapsedEventHandler(expirator_Elapsed);
            expirator.Start();
        }

        private int clientCount()
        {
            return clientsByName.Count;
        }

        public ZoneClient GetClientByName(string name)
        {
            ZoneClient client;
            if (clientsByName.TryGetValue(name, out client))
            {
                return client;
            }
            else return null;
        }

        public void DisconnectAll()
        {
            foreach (var c in clientsByName.Values)
            {
                c.Disconnect();
            }
        }

        List<string> pingTimeouts = new List<string>();
        public void PingCheck()
        {
            lock (clientsByName)
            {

                foreach (var kvp in clientsByName)
                {
                    var client = kvp.Value;
                    if (!client.Authenticated) continue; //they don't have ping shit, since they don't even send a response.
                    if (client.HasPong)
                    {
                        Handlers.Handler2.SendPing(client);
                        client.HasPong = false;
                    }
                    else
                    {
                        pingTimeouts.Add(kvp.Key);
                        Log.WriteLine(LogLevel.Debug, "Ping timeout from {0} ({1})", client.Username, client.Host);
                    }
                }

                foreach (var client in pingTimeouts)
                {
                    ZoneClient derp = null;
                    clientsByName.TryRemove(client, out derp);
                    derp.Disconnect();
                }
                pingTimeouts.Clear();
            }
        }


        public bool AddClient(ZoneClient client)
        {
            if (client.Character == null)
            {
                Log.WriteLine(LogLevel.Warn, "ClientManager trying to add character = null.", client.Username);
                return false;
            }
            else if (clientsByName.ContainsKey(client.Character.Name))
            {
                Log.WriteLine(LogLevel.Warn, "Character {0} is already registered to client manager!", client.Character.Name);
                return false;
            }
            else
            {
                if (!clientsByName.TryAdd(client.Character.Name, client))
                {
                    Log.WriteLine(LogLevel.Warn, "Could not add client to list!");
                    return false;
                }
            }
            return true;
        }

        public void RemoveClient(ZoneClient client)
        {
            if(client.Character == null) return;
            ZoneClient deleted;
            clientsByName.TryRemove(client.Character.Name, out deleted);
            if (deleted != client)
            {
                Log.WriteLine(LogLevel.Warn, "There was a duplicate client object registered for {0}.", client.Character.Name);
            }
        }

        public void AddTransfer(ClientTransfer transfer)
        {
            if (transfer.Type != TransferType.GAME)
            {
                Log.WriteLine(LogLevel.Warn, "Zone received a World transfer request. Trashing it.");
                return;
            }

            if (transfers.ContainsKey(transfer.CharacterName))
            {
                ClientTransfer trans;
                if (transfers.TryRemove(transfer.CharacterName, out trans))
                {
                    Log.WriteLine(LogLevel.Warn, "Duplicate client transfer (Char={0}) attempt from {1}.", transfer.CharacterName, trans.HostIP);
                }
            }

            if (!transfers.TryAdd(transfer.CharacterName, transfer))
            {
                Log.WriteLine(LogLevel.Warn, "Error registering client transfer for {0}.", transfer.CharacterName);
            }
            else Log.WriteLine(LogLevel.Debug, "Transfering {0}.", transfer.CharacterName);
        }

        public bool RemoveTransfer(string charname)
        {
            ClientTransfer trans;
            return transfers.TryRemove(charname, out trans);
        }

        public ClientTransfer GetTransfer(string charname)
        {
            ClientTransfer trans;
            if (transfers.TryGetValue(charname, out trans))
            {
                return trans;
            }
            else return null;
        }

        private List<string> toExpire = new List<string>();
        void expirator_Elapsed(object sender, ElapsedEventArgs e)
        {
            //this is actually executed in the main thread! (ctor is in STAThread)
            foreach (var transfer in transfers.Values)
            {
                if (Program.CurrentTime.Subtract(transfer.Time).TotalMilliseconds >= transferTimeout)
                {
                    toExpire.Add(transfer.CharacterName);
                    Log.WriteLine(LogLevel.Debug, "Transfer timeout for {0}", transfer.CharacterName);
                }
            }

            if (toExpire.Count > 0)
            {
                ClientTransfer trans;
                foreach (var expired in toExpire)
                {
                    transfers.TryRemove(expired, out trans);
                }
                toExpire.Clear();
            }
        }

        [InitializerMethod]
        public static bool Load()
        {
            Instance = new ClientManager()
            {
                transferTimeout = Settings.Instance.TransferTimeout
            };
            Log.WriteLine(LogLevel.Info, "ClientManager initialized.");
            return true;
        }
    }
}
