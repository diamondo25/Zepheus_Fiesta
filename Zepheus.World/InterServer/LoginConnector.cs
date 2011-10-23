using System;
using System.Reflection;
using Zepheus.InterLib.Networking;
using Zepheus.InterLib.NetworkObjects;
using Zepheus.Util;

namespace Zepheus.World.InterServer
{
    [ServerModule(Util.InitializationStage.Services)]
    public sealed class LoginConnector : AbstractConnector
    {
        public static LoginConnector Instance { get; private set; }

        public LoginConnector(string ip, int port)
        {
            try
            {
                Connect(ip, port);
                Log.WriteLine(LogLevel.Info, "Connected to server @ {0}:{1}", ip, port);
                this.client.OnPacket += new EventHandler<InterPacketReceivedEventArgs>(client_OnPacket);
                this.client.OnDisconnect += new EventHandler<SessionCloseEventArgs>(client_OnDisconnect);
                this.client.SendInterPass(Settings.Instance.InterPassword);
                InterHandler.TryAssiging(this);
            }
            catch
            {
                Log.WriteLine(LogLevel.Error, "Couldn't connect to server @ {0}:{1}", ip, port);
                Console.ReadLine();
                Environment.Exit(7);
            }
        }

        void client_OnDisconnect(object sender, SessionCloseEventArgs e)
        {
            Log.WriteLine(LogLevel.Error, "Disconnected from server.");
            this.client.OnPacket -= new EventHandler<InterPacketReceivedEventArgs>(client_OnPacket);
            this.client.OnDisconnect -= new EventHandler<SessionCloseEventArgs>(client_OnDisconnect);
        }

        void client_OnPacket(object sender, InterPacketReceivedEventArgs e)
        {
            try
            {
                MethodInfo method = InterHandlerStore.GetHandler(e.Packet.OpCode);
                if (method != null)
                {
                    Action action = InterHandlerStore.GetCallback(method, this, e.Packet);
                    if (Worker.Instance == null)
                    {
                        action();
                    }
                    else
                    {
                        Worker.Instance.AddCallback(action);
                    }
                }
                else
                {
                    Log.WriteLine(LogLevel.Debug, "Unhandled packet: {0}", e.Packet);
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, ex.ToString());
            }
        }

        [InitializerMethod]
        public static bool Load()
        {
            return Load(Settings.Instance.LoginServerIP, Settings.Instance.LoginServerPort);
        }

        public static bool Load(string ip, int port)
        {
            try
            {
                Instance = new LoginConnector(ip, port);
                return true;
            }
            catch { return false; }
        }

        public void SendPacket(InterPacket packet)
        {
            if (this.client == null) return;
            this.client.SendPacket(packet);
        }
    }
}
