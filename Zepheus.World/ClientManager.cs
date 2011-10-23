using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers;

using Zepheus.FiestaLib.Networking;
using Zepheus.Services.DataContracts;
using Zepheus.Util;
using Zepheus.World.Networking;

namespace Zepheus.World
{
    [ServerModule(Util.InitializationStage.Clients)]
    public sealed class ClientManager
    {
        public static ClientManager Instance { get; private set; }
        public int WorldLoad { get { return clientCount(); } }

        private List<WorldClient> clients = new List<WorldClient>();
        private ConcurrentDictionary<string, WorldClient> clientsByName = new ConcurrentDictionary<string, WorldClient>();
        private ConcurrentDictionary<string, ClientTransfer> transfers = new ConcurrentDictionary<string, ClientTransfer>();
        private Timer expirator;
        private int transferTimeout = 1;

        public ClientManager()
        {
            expirator = new Timer(2000);
            expirator.Elapsed += new ElapsedEventHandler(expirator_Elapsed);
            expirator.Start();
        }

        private int clientCount()
        {
            lock (clients)
            {
                return clients.Count;
            }
        }

        public void AddClient(WorldClient client)
        {
            lock (clients)
            {
                clients.Add(client);
            }
        }

        public void AddClientByName(WorldClient client)
        {
            if (client.Character != null && !clientsByName.ContainsKey(client.CharacterName))
            {
                clientsByName.TryAdd(client.CharacterName, client);
            }
            else Log.WriteLine(LogLevel.Warn, "Trying to register client by name without having Character object.");
        }

        List<WorldClient> pingTimeouts = new List<WorldClient>();
        public void PingCheck(DateTime now)
        {
            lock (clients)
            {

                foreach (var client in clients)
                {
                    if (!client.Authenticated) continue; //they don't have ping shit, since they don't even send a response.
                    if (client.Pong)
                    {
                        Handlers.Handler2.SendPing(client);
                        client.Pong = false;
                        client.lastPing = now;
                    }
                    else
                    {
                        if (now.Subtract(client.lastPing).TotalSeconds >= 30)
                        {
                            pingTimeouts.Add(client);
                            Log.WriteLine(LogLevel.Debug, "Ping timeout from {0} ({1})", client.Username, client.Host);
                        }
                    }
                }

                foreach (var client in pingTimeouts)
                {
                    clients.Remove(client);
                    client.Disconnect();
                }
                pingTimeouts.Clear();
            }
        }

        public WorldClient GetClientByCharname(string name)
        {
            WorldClient client;
            if (clientsByName.TryGetValue(name, out client))
            {
                return client;
            }
            else return null;
        }

        public void RemoveClient(WorldClient client)
        {
            lock (clients)
            {
                clients.Remove(client);
            }

            if (client.Character != null)
            {
                WorldClient deleted;
                clientsByName.TryRemove(client.CharacterName, out deleted);
                if (deleted != client)
                {
                    Log.WriteLine(LogLevel.Warn, "There was a duplicate client in clientsByName: {0}", client.CharacterName);
                }
            }
        }

        public void AddTransfer(ClientTransfer transfer)
        {
            if (transfer.Type != TransferType.WORLD)
            {
                Log.WriteLine(LogLevel.Warn, "Received a GAME transfer request. Trashing it.");
                return;
            }
            if (transfers.ContainsKey(transfer.Hash))
            {
                ClientTransfer trans;
                if (transfers.TryRemove(transfer.Hash, out trans))
                {
                    Log.WriteLine(LogLevel.Warn, "Duplicate client transfer hash. {0} hacked into {1}", transfer.HostIP, trans.HostIP);
                }
            }

            if (!transfers.TryAdd(transfer.Hash, transfer))
            {
                Log.WriteLine(LogLevel.Warn, "Error registering client transfer.");
            }
        }

        public bool RemoveTransfer(string hash)
        {
            ClientTransfer trans;
            return transfers.TryRemove(hash, out trans);
        }

        public ClientTransfer GetTransfer(string hash)
        {
            ClientTransfer trans;
            if(transfers.TryGetValue(hash, out trans)){
                return trans;
            } else return null;
        }

        public void SendPacketToAll(Packet pPacket, WorldClient pExcept = null)
        {
            foreach (var client in clients.FindAll(c => c != pExcept))
            {
                client.SendPacket(pPacket);
            }
        }

        private List<string> toExpire = new List<string>();
        void expirator_Elapsed(object sender, ElapsedEventArgs e)
        {
            //this is actually executed in the main thread! (ctor is in STAThread)
            foreach (var transfer in transfers.Values)
            {
                if ((DateTime.Now - transfer.Time).TotalMilliseconds >= transferTimeout)
                {
                    toExpire.Add(transfer.Hash);
                    Log.WriteLine(LogLevel.Debug, "Transfer timeout for {0}", transfer.Username);
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
