
using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Networking;
using Zepheus.Util;
using Zepheus.World.Networking;

namespace Zepheus.World.Handlers
{
    public sealed class Handler28
    {
        [PacketHandler(CH28Type.GetQuickBar)]
        public static void QuickBarRequest(WorldClient client, Packet packet)
        {
            SendQuickbar(client);
        }

        [PacketHandler(CH28Type.GetQuickBarState)]
        public static void QuickBarStateRequest(WorldClient client, Packet packet)
        {
            SendQuickbarState(client);
        }

        [PacketHandler(CH28Type.GetClientSettings)]
        public static void ClientSettingsRequest(WorldClient client, Packet packet)
        {
            SendClientSettings(client);
        }


        [PacketHandler(CH28Type.GetGameSettings)]
        public static void GameSettingsRequest(WorldClient client, Packet packet)
        {
            SendGameSettings(client);
        }

        [PacketHandler(CH28Type.GetShortCuts)]
        public static void ShortcutsRequest(WorldClient client, Packet packet)
        {
            SendShortcuts(client);
        }

        [PacketHandler(CH28Type.SaveQuickBar)]
        public static void SaveQuickBarRequest(WorldClient client, Packet packet)
        {
            // Load up 1 KB of data (well, try to)
            byte[] data;
            if (!packet.TryReadBytes(1024, out data))
            {
                Log.WriteLine(LogLevel.Warn, "Unable to read 1024 bytes from stream for save");
                return;
            }

            // Save it.
            client.Character.SetQuickBarData(data);
        }

        [PacketHandler(CH28Type.SaveGameSettings)]
        public static void SaveGameSettingsRequest(WorldClient client, Packet packet)
        {
            // Load up 64 B of data (well, try to)
            byte[] data;
            if (!packet.TryReadBytes(64, out data))
            {
                Log.WriteLine(LogLevel.Warn, "Unable to read 64 bytes from stream for save");
                return;
            }

            // Save it.
            client.Character.SetGameSettingsData(data);
        }

        [PacketHandler(CH28Type.SaveClientSettings)]
        public static void SaveClientSettingsRequest(WorldClient client, Packet packet)
        {
            byte[] data;
            if (!packet.TryReadBytes(392, out data))
            {
                Log.WriteLine(LogLevel.Warn, "Unable to read 392 bytes from stream for save");
                return;
            }

            // Save it.
            client.Character.SetClientSettingsData(data);
        }

        [PacketHandler(CH28Type.SaveQuickBarState)]
        public static void SaveQuickBarStateRequest(WorldClient client, Packet packet)
        {
            byte[] data;
            if (!packet.TryReadBytes(24, out data))
            {
                Log.WriteLine(LogLevel.Warn, "Unable to read 24 bytes from stream for save");
                return;
            }

            // Save it.
            client.Character.SetQuickBarStateData(data);
        }

        [PacketHandler(CH28Type.SaveShortCuts)]
        public static void SaveShortCutsRequest(WorldClient client, Packet packet)
        {
            byte[] data;
            if (!packet.TryReadBytes(308, out data))
            {
                Log.WriteLine(LogLevel.Warn, "Unable to read 308 bytes from stream for save");
                return;
            }

            // Save it.
            client.Character.SetShortcutsData(data);
        }

        public static void SendShortcuts(WorldClient client)
        {
            using (var packet = new Packet(SH28Type.LoadShortCuts))
            {
                byte[] data = client.Character.Character.Shortcuts;
                bool hasData = data != null;
                packet.WriteBool(hasData);
                packet.WriteBytes(hasData ? data : new byte[] { 0 });
                client.SendPacket(packet);
            }
        }

        public static void SendGameSettings(WorldClient client)
        {
            using (var packet = new Packet(SH28Type.LoadGameSettings))
            {
                byte[] data = client.Character.Character.GameSettings;
                bool hasData = data != null;
                packet.WriteBool(hasData);
                packet.WriteBytes(hasData ? data : new byte[] { 0 });
                client.SendPacket(packet);
            }
        }

        public static void SendClientSettings(WorldClient client)
        {
            using (var packet = new Packet(SH28Type.LoadClientSettings))
            {
                byte[] data = client.Character.Character.ClientSettings;
                bool hasData = data != null;
                packet.WriteBool(hasData);
                packet.WriteBytes(hasData ? data : new byte[] { 0 });
                client.SendPacket(packet);
            }
        }

        public static void SendQuickbar(WorldClient client)
        {
            using (var packet = new Packet(SH28Type.LoadQuickBar))
            {
                byte[] data = client.Character.Character.QuickBar;
                bool hasData = data != null;
                packet.WriteBool(hasData);
                packet.WriteBytes(hasData ? data : new byte[] { 0 });
                client.SendPacket(packet);
            }
        }

        public static void SendQuickbarState(WorldClient client)
        {
            using (var packet = new Packet(SH28Type.LoadQuickBarState))
            {
                byte[] data = client.Character.Character.QuickBarState;
                bool hasData = data != null;
                packet.WriteBool(hasData);
                packet.WriteBytes(hasData ? data : new byte[] { 0 });
                client.SendPacket(packet);
            }
        }
    }
}
