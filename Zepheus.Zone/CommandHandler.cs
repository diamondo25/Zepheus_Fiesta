using System;
using System.Collections.Generic;
using System.Linq;
using Zepheus.FiestaLib.Networking;
using Zepheus.Util;
using Zepheus.Zone.Data;
using Zepheus.Zone.Game;
using Zepheus.Zone.Handlers;
using Zepheus.Zone.InterServer;
using Zepheus.Zone.Networking;

namespace Zepheus.Zone
{
    [ServerModule(Util.InitializationStage.Metadata)]
    public sealed class CommandHandler
    {
        public static CommandHandler Instance { get; private set; }
        public delegate void Command(ZoneCharacter character, params string[] param);
        private Dictionary<string, CommandInfo> commands = new Dictionary<string, CommandInfo>();

        public CommandHandler()
        {
            LoadCommands();
            Log.WriteLine(LogLevel.Info, "{0} command(s) registered.", commands.Count);
        }

        public void LoadCommands()
        {
            RegisterCommand("&adminlevel", AdminLevel, 1);
            RegisterCommand("&map", ChangeMap, 1, "mapid", "x", "y");
            RegisterCommand("&pos", Pos, 1);
            RegisterCommand("&p", WritePacket, 1, "header", "type");
            RegisterCommand("&command", CommandParam, 1, "command");
            RegisterCommand("&item", ItemCommand, 1, "ID", "amount");
            RegisterCommand("&resetinv", ResetInv, 1, "slot");
            RegisterCommand("&isg@y", IsGay, 1, "charname");
            RegisterCommand("&spawnmob", SpawnMob, 1, "id");
            RegisterCommand("&spawnmobt", SpawnMobT, 1, "id", "amount");
            RegisterCommand("&createmobspawn", CreateMobSpawn, 1, "id");
            RegisterCommand("&savemobspawns", SaveMobSpawns, 1, "mapname/all");
            RegisterCommand("&heal", Heal, 1);
            RegisterCommand("&damageme", DamageMe, 1, "HP");
            RegisterCommand("&test", Test, 1, "SkillID");
            RegisterCommand("&list", List, 1);
            RegisterCommand("&killallmobs", KillAllMobs, 1);
            RegisterCommand("&levelup", LevelUP, 1, "{levels}");
            RegisterCommand("&expget", ExpGet, 1, "amount");
            RegisterCommand("&anim", Anim, 1, "animid");
            RegisterCommand("&animall", AnimAll, 1, "animid");
            RegisterCommand("&perf", Performance, 1);
        }

        private void Test(ZoneCharacter character, params string[] param)
        {
            ushort skillid = ushort.Parse(param[1]);
            if (!DataProvider.Instance.ActiveSkillsByID.ContainsKey(skillid))
            {
                character.DropMessage("Unknown skill.");
            }
            else if (character.SkillsActive.ContainsKey(skillid))
            {
                character.DropMessage("You already have that skill");
            }
            else
            {
                character.SkillsActive.Add(skillid, new Skill(character, skillid));
                Handler18.SendSkillLearnt(character, skillid);
            }
        }

        private void Anim(ZoneCharacter character, params string[] param)
        {
            byte animid = param.Length >= 2 ? byte.Parse(param[1]) : (byte)50;
            using (var broad = Handler8.Animation(character, animid))
            {
                character.Broadcast(broad, true);
            }
        }

        private void Performance(ZoneCharacter character, params string[] param)
        {
            character.DropMessage("Ticks per second on this zone: {0}", Worker.Instance.TicksPerSecond);
        }

        private void AnimAll(ZoneCharacter character, params string[] param)
        {
            byte animid = param.Length >= 2 ? byte.Parse(param[1]) : (byte)50;
            foreach (var ch in character.Map.Objects.Values.Where(o => o is ZoneCharacter))
            {
                var obj = ch as ZoneCharacter;
                using (var broad = Handler8.Animation(obj, animid))
                {
                    obj.Broadcast(broad, true);
                }
            }
        }

        private void LevelUP(ZoneCharacter character, params string[] param)
        {
            byte lvls = param.Length >= 2 ? byte.Parse(param[1]) : (byte)1;
            for (byte i = 0; i < lvls; i++)
            {
                character.LevelUP();
            }
        }

        private void ExpGet(ZoneCharacter character, params string[] param)
        {
            uint exp = param.Length >= 2 ? uint.Parse(param[1]) : 1234567;
            character.GiveEXP(exp);
        }

        private void KillAllMobs(ZoneCharacter character, params string[] param)
        {
            ushort dmg = (ushort)(param.Length >= 2 ? ushort.Parse(param[1]) : 900001);
            foreach (MapObject obj in character.Map.Objects.Values.Where(o => o is Mob))
            {
                var mob = obj as Mob;
                Handler9.SendAttackDamage(character, mob.MapObjectID, dmg, true, 0, obj.UpdateCounter);
                mob.Damage(null, dmg);
            }
        }

        private void List(ZoneCharacter character, params string[] param)
        {
            character.DropMessage("Your l33t commands:");
            foreach (var kvp in commands)
            {
                if (kvp.Value.GmLevel <= character.Client.Admin)
                {
                    character.DropMessage("{0} {1}", kvp.Key, string.Join(" ", kvp.Value.Parameters));
                }
            }
        }

        private void DamageMe(ZoneCharacter character, params string[] param)
        {
            uint value = uint.Parse(param[1]);
            character.Damage(value);
        }

        private void Heal(ZoneCharacter character, params string[] param)
        {
            character.Heal();
        }

        private void IsGay(ZoneCharacter character, params string[] param)
        {
            ZoneClient otherclient = ClientManager.Instance.GetClientByName(param[1]);
            if (otherclient == null || otherclient.Character == null)
            {
                character.DropMessage("Character not found.");
            }
            ZoneCharacter other = otherclient.Character;

            Question question = new Question("Are you gay?", new QuestionCallback(AnswerGay), character);
            question.Add("Yes", "No", "Boobs!");
            other.Question = question;
            question.Send(other);
        }

        private void AnswerGay(ZoneCharacter character, byte answer)
        {
            ZoneCharacter other = (ZoneCharacter)character.Question.Object;
            switch (answer)
            {
                case 0:
                    other.DropMessage("{0} admitted he's gay.", character.Name);
                    break;

                case 1:
                    other.DropMessage("{0} says he isn't gay, wouldn't trust him though...", character.Name);
                    break;

                case 2:
                    other.DropMessage("{0} watches too much porn, better ban him...", character.Name);
                    break;
                default:
                    Log.WriteLine(LogLevel.Warn, "Invalid gay question response.");
                    break;
            }
        }

        private void ResetInv(ZoneCharacter character, params string[] param)
        {
            byte slot = byte.Parse(param[1]);
            Handler12.ResetInventorySlot(character, slot);
        }

        private void ItemCommand(ZoneCharacter character, params string[] param)
        {
            ushort id = ushort.Parse(param[1]);
            byte amount = 1;
            if (param.Length > 2)
            {
                amount = byte.Parse(param[2]);
            }

            switch (character.GiveItem(id, amount))
            {
                case FiestaLib.InventoryStatus.FULL:
                    Handler12.InventoryFull(character);
                    return;
                case FiestaLib.InventoryStatus.NOT_FOUND:
                    character.DropMessage("Item not found.");
                    return;
            }
        }

        private void Pos(ZoneCharacter character, params string[] param)
        {
            character.DropMessage("Map= {0} ({1}); Pos= {2}:{3};",
                character.Map.MapID,
                character.Map.MapInfo.FullName,
                character.Position.X,
                character.Position.Y);
        }

        private void CreateMobSpawn(ZoneCharacter character, params string[] param)
        {
            if (param.Length == 2)
            {
                character.Map.MobBreeds.Add(MobBreedLocation.CreateLocationFromPlayer(character, ushort.Parse(param[1])));
            }
        }

        private void SaveMobSpawns(ZoneCharacter character, params string[] param)
        {
            if (param.Length == 2)
            {
                if (param[1] == "current")
                {
                    character.Map.SaveMobBreeds();
                }
                else if (param[1] == "all")
                {
                    foreach (var val in MapManager.Instance.Maps)
                    {
                        if (val.Value.Count > 0)
                        {
                            val.Value[0].SaveMobBreeds();
                        }
                    }
                }
                else
                {
                    param[1] = param[1].ToLower();
                    foreach (var val in MapManager.Instance.Maps)
                    {
                        if (val.Key.ShortName.ToLower() == param[1] && val.Value.Count > 0)
                        {
                            val.Value[0].SaveMobBreeds();
                        }
                    }
                }
            }
        }

        private void SpawnMob(ZoneCharacter character, params string[] param)
        {
            ushort id = ushort.Parse(param[1]);
            if (DataProvider.Instance.MobsByID.ContainsKey(id))
            {
                Mob mob = new Mob((ushort)(param.Length == 2 ? id : 1045), new Vector2(character.Position));
                character.Map.FullAddObject(mob);
            }
            else character.DropMessage("Monster ID not found.");
        }

        private void SpawnMobT(ZoneCharacter character, params string[] param)
        {
            ushort id = ushort.Parse(param[1]);
            int count = 1;
            if (param.Length >= 3)
            {
                count = int.Parse(param[2]);
            }
            if (DataProvider.Instance.MobsByID.ContainsKey(id))
            {
                for (int i = 0; i < count; i++)
                {
                    Mob mob = new Mob(id, new Vector2(character.Position));
                    character.Map.FullAddObject(mob);
                }
            }
            else character.DropMessage("Monster ID not found.");
        }

        private void CommandParam(ZoneCharacter character, params string[] param)
        {
            string input = param[1];
            string request;
            if (!input.StartsWith("&"))
            {
                request = "&" + input;
            }
            else request = input;
            CommandInfo info;
            if (commands.TryGetValue(request, out info))
            {
                string output = request + ": ";
                if (info.Parameters.Length > 0)
                {
                    foreach (var par in info.Parameters)
                    {
                        output += "[" + par + "] ";
                    }
                }
                else output += "None";
                character.DropMessage(output);
            }
            else character.DropMessage("Command not found.");
        }

        private void ChangeMap(ZoneCharacter character, params string[] param)
        {
            ushort mapid = 0;
            if (!ushort.TryParse(param[1], out mapid))
            {
                param[1] = param[1].ToLower();
                var map = DataProvider.Instance.MapsByID.Values.ToList().Find(m => m.ShortName.ToLower() == param[1]);
                if (map != null)
                {
                    mapid = map.ID;
                }
                else
                {
                    character.DropMessage("Map not found");
                    return;
                }
            }
            else
            {
                if (!DataProvider.Instance.MapsByID.ContainsKey(mapid))
                {
                    character.DropMessage("Map not found");
                    return;
                }
            }

            if (param.Length > 2)
            {
                int x = int.Parse(param[2]);
                int y = int.Parse(param[3]);
                character.ChangeMap(mapid, x, y);
            }
            character.ChangeMap(mapid);
        }

        private void WritePacket(ZoneCharacter character, params string[] param)
        {
            if (param.Length >= 3)
            {
                byte header = byte.Parse(param[1]);
                byte type = byte.Parse(param[2]);

                using (var packet = new Packet(header, type))
                {
                    if (param.Length > 3)
                    {
                        packet.WriteHexAsBytes(string.Join("", param, 3, param.Length - 3));
                    }
                    character.Client.SendPacket(packet);
                }
            }
        }

        private void AdminLevel(ZoneCharacter character, params string[] param)
        {
            Log.WriteLine(LogLevel.Debug, "GM {0} authenticated ingame.", character.Name);
            character.DropMessage("Admin level is {0}", character.Client.Admin);
            InterHandler.SendWorldMessage(FiestaLib.WorldMessageTypes.Level20, "Fun!");
        }

        public void RegisterCommand(string command, Command function, byte gmlevel, params string[] param)
        {
            if (commands.ContainsKey(command))
            {
                Log.WriteLine(LogLevel.Warn, "{0} already registered as a command.", command);
                return;
            }
            CommandInfo info = new CommandInfo(command.ToLower(), function, gmlevel, param);
            commands.Add(command.ToLower(), info);
        }

        public string[] GetCommandParams(string command)
        {
            CommandInfo info;
            if (commands.TryGetValue(command, out info))
            {
                return info.Parameters;
            }
            else return null;
        }

        public CommandStatus ExecuteCommand(ZoneCharacter character, string[] command)
        {
            if (character == null) return CommandStatus.ERROR;
            CommandInfo info;
            if (commands.TryGetValue(command[0].ToLower(), out info))
            {
                if (info.GmLevel > character.Client.Admin)
                {
                    return CommandStatus.GM_LEVEL_TOO_LOW;
                }
                else
                {
                    try
                    {
                        info.Function(character, command);
                        return CommandStatus.DONE;
                    }
                    catch (Exception ex)
                    {
                        string wholeCommand = string.Join(" ", command);
                        Log.WriteLine(LogLevel.Exception, "Exception while handling command '{0}': {1}", wholeCommand, ex.ToString());
                        return CommandStatus.ERROR;
                    }
                }
            }
            else return CommandStatus.NOT_FOUND;
        }

        [InitializerMethod]
        public static bool Load()
        {
            Instance = new CommandHandler();
            return true;
        }
    }

    public class CommandInfo
    {
        public string Command { get; private set; }
        public CommandHandler.Command Function { get; private set; }
        public string[] Parameters { get; private set; }
        public byte GmLevel { get; private set; }

        public CommandInfo(string command, CommandHandler.Command function, byte gmlevel, string[] param)
        {
            Command = command;
            Function = function;
            GmLevel = gmlevel;
            Parameters = param;
        }
    }

    public enum CommandStatus : byte
    {
        DONE,
        GM_LEVEL_TOO_LOW,
        NOT_FOUND,
        ERROR
    }
}
