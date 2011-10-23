using System;
using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Data;
using Zepheus.FiestaLib.Networking;
using Zepheus.Util;
using Zepheus.Zone.Data;
using Zepheus.Zone.Game;
using Zepheus.Zone.Networking;
using Zepheus.Zone.Security;

namespace Zepheus.Zone.Handlers
{
    public sealed class Handler8
    {
        [PacketHandler(CH8Type.ChatNormal)]
        public static void NormalChatHandler(ZoneClient client, Packet packet)
        {
            byte len;
            string text;
            if (!packet.TryReadByte(out len) || !packet.TryReadString(out text, len))
            {
                Log.WriteLine(LogLevel.Warn, "Could not parse normal chat from {0}.", client.Character.Name);
                return;
            }
            if (client.Admin > 0 && (text.StartsWith("&") || text.StartsWith("/")))
            {
                CommandLog.Instance.LogCommand(client.Character.Name, text);
                CommandStatus status = CommandHandler.Instance.ExecuteCommand(client.Character, text.Split(' '));
                switch (status)
                {
                    case CommandStatus.ERROR:
                        client.Character.DropMessage("Error executing command.");
                        break;
                    case CommandStatus.GM_LEVEL_TOO_LOW:
                        client.Character.DropMessage("You do not have the privileges for this command.");
                        break;
                    case CommandStatus.NOT_FOUND:
                        client.Character.DropMessage("Command not found.");
                        break;
                }
            }
            else
            {
                int chatblock = client.Character.ChatCheck();
                if (chatblock == -1)
                {
                    ChatLog.Instance.LogChat(client.Character.Name, text, false);
                    SendNormalChat(client.Character, text, client.Admin > 0 ? (byte)0x03 : (byte)0x2a);
                }
                else
                {
                    Handler2.SendChatBlock(client.Character, chatblock);
                }
            }
        }

        [PacketHandler(CH8Type.Shout)]
        public static void ShoutHandler(ZoneClient client, Packet packet)
        {
            ZoneCharacter character = client.Character;
            byte len;
            string message;
            if (!packet.TryReadByte(out len) ||
                !packet.TryReadString(out message, len))
            {
                Log.WriteLine(LogLevel.Warn, "Could not read shout from {0}.", character.Name);
                return;
            }

            int shoutcheck = character.ShoutCheck();
            if (shoutcheck > 0)
            {
                Handler2.SendChatBlock(character, shoutcheck);
            }
            else
            {
                ChatLog.Instance.LogChat(client.Character.Name, message, true);
                using (var broad = Shout(character.Name, message))
                {
                    character.Map.Broadcast(broad);
                }
            }
        }

        [PacketHandler(CH8Type.BeginInteraction)]
        public static void BeginInteractionHandler(ZoneClient client, Packet packet)
        {
            ushort entityid;
            if (!packet.TryReadUShort(out entityid))
            {
                Log.WriteLine(LogLevel.Warn, "Error reading interaction attempt.");
                return;
            }
            ZoneCharacter character = client.Character;
            MapObject obj;
            if (character.Map.Objects.TryGetValue(entityid, out obj))
            {
                NPC npc = obj as NPC;
                if (npc != null)
                {
                    if (npc.Gate != null)
                    {
                        
                        MapInfo mi = null;
                        if (DataProvider.Instance.MapsByName.TryGetValue(npc.Gate.MapServer, out mi))
                        {
                            Question q = new Question(string.Format("Do you want to move to {0} field?", mi.FullName), new QuestionCallback(AnswerOnGateQuestion), npc);
                            q.Add("Yes", "No");
                            q.Send(character, 500);
                            character.Question = q;
                        }
                        else
                        {
                            character.DropMessage("You can't travel to this place.");
                        }
                    }
                    else
                    {
                        SendNPCInteraction(client, npc.ID);
                    }
                }
            }
            else Log.WriteLine(LogLevel.Warn, "{0} selected invalid object.", character.Name);
        }


        private static void AnswerOnGateQuestion(ZoneCharacter character, byte answer)
        {
            NPC npc = (character.Question.Object as NPC);
            MapInfo mi = null;
            if (DataProvider.Instance.MapsByName.TryGetValue(npc.Gate.MapServer, out mi))
            {

                switch (answer)
                {
                    case 0:
                        character.ChangeMap(mi.ID, npc.Gate.Coord_X, npc.Gate.Coord_Y);
                        break;

                    case 1: break;
                    default:
                        Log.WriteLine(LogLevel.Warn, "Invalid gate question response.");
                        break;
                }
            }
        }


        public static void SendNPCInteraction(ZoneClient client, ushort ID)
        {
            using (var packet = new Packet(SH8Type.Interaction))
            {
                packet.WriteUShort(ID);
                client.SendPacket(packet);
            }
        }

        [PacketHandler(CH8Type.BeginRest)]
        public static void BeginRestHandler(ZoneClient client, Packet packet)
        {
            client.Character.Rest(true);
        }

        [PacketHandler(CH8Type.EndRest)]
        public static void EndRestHandler(ZoneClient client, Packet packet)
        {
            client.Character.Rest(false);
        }

        public static void SendEndRestResponse(ZoneClient client)
        {
            using (var packet = new Packet(SH8Type.EndRest))
            {
                packet.WriteUShort(0x0a81);
                client.SendPacket(packet);
            }
        }

        public static void SendBeginRestResponse(ZoneClient client, ushort value)
        {
            /*  0x0A81 - OK to rest
                   0x0A82 - Can't rest on mount
                   0x0A83 - Too close to NPC*/
            using (var packet = new Packet(SH8Type.BeginRest))
            {
                packet.WriteUShort(value);
                client.SendPacket(packet);
            }
        }

        public static Packet BeginDisplayRest(ZoneCharacter character)
        {
            Packet packet = new Packet(SH8Type.BeginDisplayRest);
            packet.WriteUShort(character.MapObjectID);
            packet.WriteUShort(character.House.ItemID);
            packet.Fill(10, 0xff);
            return packet;
        }

        public static Packet EndDisplayRest(ZoneCharacter character)
        {
            Packet packet = new Packet(SH8Type.EndDisplayRest);
            packet.WriteUShort(character.MapObjectID);
            character.WriteLook(packet);
            character.WriteEquipment(packet);
            character.WriteRefinement(packet);
            return packet;
        }

        [PacketHandler(CH8Type.Emote)]
        public static void EmoteHandler(ZoneClient client, Packet packet)
        {
            ZoneCharacter character = client.Character;
            byte action;
            if (!packet.TryReadByte(out action))
            {
                Log.WriteLine(LogLevel.Warn, "{0} did empty emote.", character.Name);
                return;
            }

            if (action > 74)
            {
                character.CheatTracker.AddCheat(CheatTypes.EMOTE, 500);
                return;
            }

            using (var broad = Animation(character, action))
            {
                character.Broadcast(broad, true);
            }
        }

        public static Packet Animation(ZoneCharacter character, byte id)
        {
            Packet packet = new Packet(SH8Type.Emote);
            packet.WriteUShort(character.MapObjectID);
            packet.WriteByte(id);
            return packet;
        }

        public static Packet Shout(string charname, string text)
        {
            Packet packet = new Packet(SH8Type.Shout);
            packet.WriteString(charname, 16);
            packet.WriteByte(0); //color
            packet.WriteByte((byte)text.Length);
            packet.WriteString(text, text.Length);
            return packet;
        }

        [PacketHandler(CH8Type.Jump)]
        public static void JumpHandler(ZoneClient client, Packet packet)
        {
            ZoneCharacter character = client.Character;
            if (character.State == PlayerState.Normal || character.State == PlayerState.Mount)
            {
                using (var broad = Jump(character))
                {
                    character.Broadcast(broad);
                }
            }
            else character.CheatTracker.AddCheat(CheatTypes.INVALID_MOVE, 50);
        }

        public static Packet Jump(ZoneCharacter character)
        {
            Packet packet = new Packet(SH8Type.Jump);
            packet.WriteUShort(character.MapObjectID);
            return packet;
        }

        [PacketHandler(CH8Type.Run)]
        public static void RunHandler(ZoneClient client, Packet packet)
        {
            HandleMovement(client.Character, packet, true);
        }

        [PacketHandler(CH8Type.Stop)]
        public static void StopHandler(ZoneClient client, Packet packet)
        {
            HandleMovement(client.Character, packet, true, true);
        }

        [PacketHandler(CH8Type.Walk)]
        public static void WalkHandler(ZoneClient client, Packet packet)
        {
            HandleMovement(client.Character, packet, false);
        }

        private static void HandleMovement(ZoneCharacter character, Packet packet, bool run, bool stop = false)
        {
            if (character.State == PlayerState.Dead || character.State == PlayerState.Resting || character.State == PlayerState.Vendor)
            {
                character.CheatTracker.AddCheat(CheatTypes.INVALID_MOVE, 50);
                return;
            }

            int newX, oldX, newY, oldY;
            if (!stop)
            {
                if (!packet.TryReadInt(out oldX) || !packet.TryReadInt(out oldY) ||
                    !packet.TryReadInt(out newX) || !packet.TryReadInt(out newY))
                {
                    Log.WriteLine(LogLevel.Warn, "Invalid movement packet detected.");
                    return;
                }
            }
            else
            {
                if (!packet.TryReadInt(out newX) || !packet.TryReadInt(out newY))
                {
                    Log.WriteLine(LogLevel.Warn, "Invalid stop packet detected.");
                    return;
                }
                oldX = character.Position.X;
                oldY = character.Position.Y;
            }

            if (character.Map.Block != null)
            {
                if (Settings.Instance.UseSHBD)
                {
                    if (!character.Map.Block.CanWalk(newX, newY))
                    {
                        Log.WriteLine(LogLevel.Debug, "Blocking walk at {0}:{1}.", newX, newY);
                        SendPositionBlock(character, newX, newY);
                        SendTeleportCharacter(character, oldX, oldY);
                        return;
                    }
                }
            }

            double distance = Vector2.Distance(newX, oldX, newY, oldY);
            if ((run && distance > 500d) || (!run && distance > 400d)) //TODO: mounts don't check with these speeds
            {
                character.CheatTracker.AddCheat(Security.CheatTypes.SPEEDWALK, 50);
                return;
            }

            if (!stop)
            {
                int deltaY = newY - character.Position.Y;
                int deltaX = newX - character.Position.X;
                double radians = Math.Atan((double)deltaY / deltaX);
                double angle = radians * (180 / Math.PI);
                character.Rotation = (byte)(angle / 2);
            }

            character.Move(oldX, oldY, newX, newY, !run, stop); // hehe
        }

        public static Packet MoveObject(MapObject obj, int oldx, int oldy, bool walk, ushort speed = 115)
        {
            Packet packet = new Packet(walk ? SH8Type.Walk : SH8Type.Move);
            packet.WriteUShort(obj.MapObjectID);
            packet.WriteInt(oldx);
            packet.WriteInt(oldy);
            packet.WriteInt(obj.Position.X);
            packet.WriteInt(obj.Position.Y);
            packet.WriteUShort(speed);
            return packet;
        }

        public static Packet StopObject(MapObject obj)
        {
            Packet packet = new Packet(SH8Type.StopTele);
            packet.WriteUShort(obj.MapObjectID);
            packet.WriteInt(obj.Position.X);
            packet.WriteInt(obj.Position.Y);
            return packet;
        }

        public static void SendAdminNotice(ZoneClient client, string text)
        {
            using (var packet = new Packet(SH8Type.GmNotice))
            {
                packet.WriteByte((byte)text.Length);
                packet.WriteString(text, text.Length);
                client.SendPacket(packet);
            }
        }

        public static void SendPositionBlock(ZoneCharacter character, int x, int y)
        {
            using (var packet = new Packet(SH8Type.BlockWalk))
            {
                packet.WriteInt(x);
                packet.WriteInt(y);
                character.Client.SendPacket(packet);
            }
        }

        public static void SendTeleportCharacter(ZoneCharacter character, int x, int y)
        {
            using (var packet = new Packet(SH8Type.Teleport))
            {
                packet.WriteInt(x);
                packet.WriteInt(y);
                character.Client.SendPacket(packet);
            }
        }

        public static void SendNormalChat(ZoneCharacter character, string chat, byte color = 0x2a)
        {
            using (var packet = new Packet(SH8Type.ChatNormal))
            {
                packet.WriteUShort(character.MapObjectID);
                packet.WriteByte((byte)chat.Length);
                packet.WriteByte(color);
                packet.WriteString(chat, chat.Length);
                character.Broadcast(packet, true);
            }
        }
    }
}
