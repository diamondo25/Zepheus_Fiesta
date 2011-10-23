using System;
using System.Collections.Generic;
using System.IO;
using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Data;
using Zepheus.FiestaLib.ShineTable;
using Zepheus.FiestaLib.SHN;
using Zepheus.Util;

namespace Zepheus.Zone.Data
{
    [ServerModule(Util.InitializationStage.SpecialDataProvider)]
    public sealed class DataProvider
    {
        public Dictionary<ushort, MapInfo> MapsByID { get; private set; }
        public Dictionary<string, MapInfo> MapsByName { get; private set; }
        public Dictionary<string, LinkTable> NpcLinkTable { get; private set; }
        public Dictionary<string, MobInfoServer> MobData { get; private set; }
        public Dictionary<ushort, BlockInfo> Blocks { get; private set; }
        public Dictionary<Job, List<FiestaBaseStat>> JobInfos { get; private set; }
        public Dictionary<ushort, ItemInfo> ItemsByID { get; private set; }
        public Dictionary<string, ItemInfo> ItemsByName { get; private set; }
        public Dictionary<string, DropGroupInfo> DropGroups { get; private set; }
        public Dictionary<ushort, MobInfo> MobsByID { get; private set; }
        public Dictionary<string, MobInfo> MobsByName { get; private set; }
        public Dictionary<ushort, ItemUseEffectInfo> ItemUseEffects { get; private set; }
        public Dictionary<string, RecallCoordinate> RecallCoordinates { get; private set; }
        public Dictionary<byte, ulong> ExpTable { get; private set; }
        public Dictionary<ushort, MiniHouseInfo> MiniHouses { get; private set; }

        public Dictionary<ushort, ActiveSkillInfo> ActiveSkillsByID { get; private set; }
        public Dictionary<string, ActiveSkillInfo> ActiveSkillsByName { get; private set; }
        public static DataProvider Instance { get; private set; }
        private string folder;

        public DataProvider(string path)
        {
            folder = path;
            //LoadMaps(Program.serviceInfo.MapsToLoad);
            LoadMaps(null); //this loads all the maps, but we get issues with zone spread (fix later)
            LoadJobStats();
            LoadExpTable();
            LoadItemInfo();
            LoadRecallCoordinates();
            LoadMobs();
            LoadDrops();
            LoadItemInfoServer();
            LoadMiniHouseInfo();
            LoadActiveSkills();
        }

        [InitializerMethod]
        public static bool Load()
        {
            try
            {
                Instance = new DataProvider(Settings.Instance.DataFolder);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "Error loading dataprovider: {0}", ex.ToString());
                return false;
            }
        }

        private static readonly string[] DropGroupNames = new string[] { "DropGroupA", "DropGroupB", "RandomOptionDropGroup" };
        private void LoadItemInfoServer()
        {
            try
            {
                using (var reader = new DataTableReaderEx(new SHNFile(folder + @"\ItemInfoServer.shn")))
                {
                    while (reader.Read())
                    {
                        ushort itemid = (ushort)reader.GetUInt32("ID");
                        ItemInfo item;
                        if (ItemsByID.TryGetValue(itemid, out item))
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                string groupname = reader.GetString(DropGroupNames[i]);
                                if (groupname.Length > 2)
                                {
                                    DropGroupInfo group;
                                    if (DropGroups.TryGetValue(groupname, out group))
                                    {
                                        group.Items.Add(item);
                                    }
                                    else
                                    {
                                        //Log.WriteLine(LogLevel.Warn, "{0} was assigned to unknown DropGroup {1}.", item.InxName, groupname);
                                    }
                                }
                            }
                        }
                        else Log.WriteLine(LogLevel.Warn, "ItemInfoServer has obsolete item ID: {0}.", itemid);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "Error loading ItemInfoServer.shn: {0}", ex);
            }
        }

        private void LoadDrops()
        {
            DropGroups = new Dictionary<string, DropGroupInfo>();
            try
            {
                //first we load the dropgroups
                using (var groupfile = new ShineReader(folder + @"\ItemDropGroup.txt"))
                {
                    var table = groupfile["ItemDropGroup"];
                    using (var reader = new DataTableReaderEx(table))
                    {
                        while (reader.Read())
                        {
                            DropGroupInfo info = DropGroupInfo.Load(reader);
                            if (DropGroups.ContainsKey(info.GroupID))
                            {
                                //Log.WriteLine(LogLevel.Warn, "Duplicate DropGroup ID found: {0}.", info.GroupID);
                                continue;
                            }
                            DropGroups.Add(info.GroupID, info);
                        }
                    }
                }

                //now we load the actual drops
                int dropcount = 0;
                using (var tablefile = new ShineReader(folder + @"\ItemDropTable.txt"))
                {
                    var table = tablefile["ItemGroup"];
                    using (var reader = new DataTableReaderEx(table))
                    {
                        while (reader.Read())
                        {
                            string mobid = reader.GetString("MobId");
                            MobInfo mob;
                            if (MobsByName.TryGetValue(mobid, out mob))
                            {
                                mob.MinDropLevel = (byte)reader.GetInt16("MinLevel");
                                mob.MaxDropLevel = (byte)reader.GetInt16("MaxLevel");
                                for (int i = 1; i <= 45; ++i)
                                {
                                    string dropgroup = reader.GetString("DrItem" + i);
                                    if (dropgroup.Length <= 2) continue;
                                    DropGroupInfo group;
                                    if (DropGroups.TryGetValue(dropgroup, out group))
                                    {
                                        float rate = reader.GetInt32("DrItem" + i + "R") / 100000f;
                                        DropInfo info = new DropInfo(group, rate);
                                        mob.Drops.Add(info);
                                        ++dropcount;
                                    }
                                    else
                                    {
                                        //this seems to happen a lot so disable this for the heck of it.
                                        // Log.WriteLine(LogLevel.Warn, "Could not find DropGroup {0}.", dropgroup);
                                    }
                                }
                            }
                            else Log.WriteLine(LogLevel.Warn, "Could not find mobname: {0} for drop.", mobid);
                        }
                    }
                }
                Log.WriteLine(LogLevel.Info, "Loaded {0} DropGroups, with {1} drops in total.", DropGroups.Count, dropcount);
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "Error loading DropTable: {0}", ex);
            }
        }

        private void LoadActiveSkills()
        {
            ActiveSkillsByID = new Dictionary<ushort, ActiveSkillInfo>();
            ActiveSkillsByName = new Dictionary<string, ActiveSkillInfo>();
            using (var file = new SHNFile(folder + @"\ActiveSkill.shn"))
            {
                using (DataTableReaderEx reader = new DataTableReaderEx(file))
                {
                    while (reader.Read())
                    {
                        ActiveSkillInfo info = ActiveSkillInfo.Load(reader);
                        if (ActiveSkillsByID.ContainsKey(info.ID) || ActiveSkillsByName.ContainsKey(info.Name))
                        {
                            Log.WriteLine(LogLevel.Warn, "Duplicate ActiveSkill found: {0} ({1})", info.ID, info.Name);
                            continue;
                        }
                        ActiveSkillsByID.Add(info.ID, info);
                        ActiveSkillsByName.Add(info.Name, info);
                    }
                }
            }
            Log.WriteLine(LogLevel.Info, "Loaded {0} ActiveSkills.", ActiveSkillsByID.Count);
        }

        private void LoadRecallCoordinates()
        {
            RecallCoordinates = new Dictionary<string, RecallCoordinate>();
            if (!File.Exists(folder + @"\RecallCoordinates.txt"))
            {
                Log.WriteLine(LogLevel.Warn, "Could not find RecallCoordinates.txt, return scrolls won't work.");
                return;
            }

            using (var data = new ShineReader(folder + @"\RecallCoordinates.txt"))
            {

                var recallData = data["RecallPoint"];

                using (var reader = new DataTableReaderEx(recallData))
                {
                    while (reader.Read())
                    {
                        var rc = RecallCoordinate.Load(reader);
                        RecallCoordinates.Add(rc.ItemIndex, rc);
                    }
                }

                Log.WriteLine(LogLevel.Info, "Loaded {0} recall coordinates.", RecallCoordinates.Count);
            }
        }

        private void LoadMobs()
        {
            MobsByID = new Dictionary<ushort, MobInfo>();
            MobsByName = new Dictionary<string, MobInfo>();
            using (var file = new SHNFile(folder + @"\MobInfo.shn"))
            {
                using (DataTableReaderEx reader = new DataTableReaderEx(file))
                {
                    while (reader.Read())
                    {
                        MobInfo info = MobInfo.Load(reader);
                        if (MobsByID.ContainsKey(info.ID) || MobsByName.ContainsKey(info.Name))
                        {
                            Log.WriteLine(LogLevel.Warn, "Duplicate mob ID found in MobInfo.shn: {0}.", info.ID);
                            continue;
                        }
                        MobsByID.Add(info.ID, info);
                        MobsByName.Add(info.Name, info);
                    }
                }
            }
            Log.WriteLine(LogLevel.Info, "Loaded {0} mobs.", MobsByID.Count);

            MobData = new Dictionary<string, MobInfoServer>();
            using (var file = new SHNFile(folder + @"\MobInfoServer.shn"))
            {
                using (DataTableReaderEx reader = new DataTableReaderEx(file))
                {
                    while (reader.Read())
                    {
                        MobInfoServer info = MobInfoServer.Load(reader);
                        if (MobData.ContainsKey(info.InxName))
                        {
                            Log.WriteLine(LogLevel.Warn, "Duplicate mob ID found in MobInfoServer.shn: {0}.", info.InxName);
                            continue;
                        }
                        MobData.Add(info.InxName, info);
                    }
                }
            }
            Log.WriteLine(LogLevel.Info, "Loaded {0} mob infos.", MobsByID.Count);

        }

        public MobInfo GetMobInfo(ushort id)
        {
            MobInfo toret;
            if (MobsByID.TryGetValue(id, out toret))
            {
                return toret;
            }
            else return null;
        }

        public ushort GetMobIDFromName(string name)
        {
            MobInfoServer mis = null;
            if (MobData.TryGetValue(name, out mis))
            {
                return (ushort)mis.ID;
            }
            return 0;
        }

        public FiestaBaseStat GetBaseStats(Job job, byte level)
        {
            return JobInfos[job][level - 1];
        }

        public void LoadJobStats()
        {
            LoadJobStatsNEW();
        }

        public void LoadJobStatsNEW()
        {
            // Temp set a dict for every job/filename
            Dictionary<string, Job> sj = new Dictionary<string, Job>();
            sj.Add("Archer", Job.Archer);
            sj.Add("Assassin", Job.Reaper);
            sj.Add("Chaser", Job.Gambit);
            sj.Add("Cleric", Job.Cleric);
            sj.Add("CleverFighter", Job.CleverFighter);
            sj.Add("Closer", Job.Spectre);
            sj.Add("Cruel", Job.Renegade);
            sj.Add("Enchanter", Job.Enchanter);
            sj.Add("Fighter", Job.Fighter);
            sj.Add("Gladiator", Job.Gladiator);
            sj.Add("Guardian", Job.Guardian);
            sj.Add("HawkArcher", Job.HawkArcher);
            sj.Add("HighCleric", Job.HighCleric);
            sj.Add("HolyKnight", Job.HolyKnight);
            sj.Add("Joker", Job.Trickster); // hah
            sj.Add("Knight", Job.Knight);
            sj.Add("Mage", Job.Mage);
            sj.Add("Paladin", Job.Paladin);
            sj.Add("Ranger", Job.Ranger);
            sj.Add("Scout", Job.Scout);
            sj.Add("SharpShooter", Job.SharpShooter);
            sj.Add("Warrock", Job.Warlock); // ITS A GAME. AND YOU LOST IT
            sj.Add("Warrior", Job.Warrior);
            sj.Add("Wizard", Job.Wizard);
            sj.Add("WizMage", Job.WizMage);

            // DAMN THATS A LONG LIST

            Log.WriteLine(LogLevel.Debug, "Trying to load {0} jobs.", sj.Count);
            JobInfos = new Dictionary<Job, List<FiestaBaseStat>>();

            foreach (var kvp in sj)
            {
                // Make the filename and see if we can find it's stats
                string file = string.Format(folder + @"\Stats\Param{0}Server.txt", kvp.Key);
                if (!File.Exists(file))
                {
                    Log.WriteLine(LogLevel.Error, "Could not find file {0}!", file);
                    continue;
                }
                List<FiestaBaseStat> stats = new List<FiestaBaseStat>();
                using (var tables = new ShineReader(file))
                {
                    if (tables.FileContents.Count == 0)
                    {
                        Log.WriteLine(LogLevel.Warn, "Corrupt ShineTable file.");
                        continue;
                    }

                    using (var reader = new DataTableReaderEx(tables["Param"]))
                    {
                        while (reader.Read())
                        {
                            stats.Add(FiestaBaseStat.Load(reader, kvp.Value));
                        }
                    }
                }

                JobInfos.Add(kvp.Value, stats);
                //   Log.WriteLine(LogLevel.Debug, "Loaded {0} levels for job {1}", stats.Count, kvp.Value.ToString());

            }
        }

        public void LoadItemInfo()
        {
            Dictionary<string, ItemUseEffectInfo> effectcache = new Dictionary<string, ItemUseEffectInfo>();
            ItemUseEffects = new Dictionary<ushort, ItemUseEffectInfo>();
            using (var file = new SHNFile(folder + @"\ItemUseEffect.shn"))
            {
                using (DataTableReaderEx reader = new DataTableReaderEx(file))
                {
                    while (reader.Read())
                    {
                        string inxname;
                        ItemUseEffectInfo info = ItemUseEffectInfo.Load(reader, out inxname);
                        effectcache.Add(inxname, info);
                    }
                }
            }


            ItemsByID = new Dictionary<ushort, ItemInfo>();
            ItemsByName = new Dictionary<string, ItemInfo>();
            using (var file = new SHNFile(folder + @"\ItemInfo.shn"))
            {
                using (DataTableReaderEx reader = new DataTableReaderEx(file))
                {
                    while (reader.Read())
                    {
                        ItemInfo info = ItemInfo.Load(reader);
                        if (ItemsByID.ContainsKey(info.ItemID) || ItemsByName.ContainsKey(info.InxName))
                        {
                            Log.WriteLine(LogLevel.Warn, "Duplicate item found ID: {0} ({1}).", info.ItemID, info.InxName);
                            continue;
                        }
                        ItemsByID.Add(info.ItemID, info);
                        ItemsByName.Add(info.InxName, info);

                        if (effectcache.ContainsKey(info.InxName))
                        {
                            if (info.Type != ItemType.Useable)
                            {
                                Log.WriteLine(LogLevel.Warn, "Invalid useable item: {0} ({1})", info.ItemID, info.InxName);
                                continue;
                            }
                            ItemUseEffectInfo effectinfo = effectcache[info.InxName];
                            effectinfo.ID = info.ItemID;
                            ItemUseEffects.Add(effectinfo.ID, effectinfo);
                        }
                    }
                }
            }
            effectcache.Clear();
            Log.WriteLine(LogLevel.Info, "Loaded {0} items.", ItemsByID.Count);
        }

        public ItemInfo GetItemInfo(ushort ID)
        {
            ItemInfo info;
            if (ItemsByID.TryGetValue(ID, out info))
            {
                return info;
            }
            else return null;
        }

        public void LoadExpTable()
        {
            ExpTable = new Dictionary<byte, ulong>();
            try
            {
                using (var tables = new ShineReader(folder + @"\ChrCommon.txt"))
                {
                    using (DataTableReaderEx reader = new DataTableReaderEx(tables["StatTable"]))
                    {
                        while (reader.Read())
                        {
                            ExpTable.Add(reader.GetByte("level"), reader.GetUInt64("NextExp"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "Couldn't load EXP table: {0}", ex.ToString());
            }
        }

        public ulong GetMaxExpForLevel(byte pLevel)
        {
            ulong ret = 0;
            if (!ExpTable.TryGetValue(pLevel, out ret))
            {
                Log.WriteLine(LogLevel.Warn, "Something tried to get the amount of EXP for level {0} (which is higher than it's max, {1}). Please backtrace the calls to this function!", pLevel, ExpTable.Count);
                Log.WriteLine(LogLevel.Warn, Environment.StackTrace);
            }
            return ret;
        }

        public void LoadMaps(List<ushort> toload = null)
        {
            MapsByID = new Dictionary<ushort, MapInfo>();
            MapsByName = new Dictionary<string, MapInfo>();
            using (var file = new SHNFile(folder + @"\MapInfo.shn"))
            {
                using (DataTableReaderEx reader = new DataTableReaderEx(file))
                {
                    while (reader.Read())
                    {
                        MapInfo info = MapInfo.Load(reader);
                        info.NPCs = new List<ShineNPC>();
                        if (MapsByID.ContainsKey(info.ID))
                        {
                            Log.WriteLine(LogLevel.Debug, "Duplicate map ID {0} ({1})", info.ID, info.FullName);
                            MapsByID.Remove(info.ID);
                            MapsByName.Remove(info.ShortName);
                        }
                        if (toload == null || toload.Contains(info.ID))
                        {
                            MapsByID.Add(info.ID, info);
                            MapsByName.Add(info.ShortName, info);
                        }
                    }
                }
            }

            Blocks = new Dictionary<ushort, BlockInfo>();
            foreach (var map in MapsByID.Values)
            {
                string renderpath = folder + @"\BlockInfo\" + map.ShortName + ".shbd";
                if (File.Exists(renderpath))
                {
                    BlockInfo info = new BlockInfo(renderpath, map.ID);
                    Blocks.Add(map.ID, info);
                }
            }


            using (var tables = new ShineReader(folder + @"\NPC.txt"))
            {
                NpcLinkTable = new Dictionary<string, LinkTable>();
                using (DataTableReaderEx reader = new DataTableReaderEx(tables["LinkTable"]))
                {
                    while (reader.Read())
                    {
                        LinkTable link = LinkTable.Load(reader);
                        if (Program.IsLoaded(GetMapidFromMapShortName(link.MapClient)))
                        {
                            NpcLinkTable.Add(link.argument, link);
                        }
                    }
                }

                using (DataTableReaderEx reader = new DataTableReaderEx(tables["ShineNPC"]))
                {
                    while (reader.Read())
                    {
                        ShineNPC npc = ShineNPC.Load(reader);
                        MapInfo mi = null;
                        if (Program.IsLoaded(GetMapidFromMapShortName(npc.Map)) && MapsByName.TryGetValue(npc.Map, out mi))
                        {
                            mi.NPCs.Add(npc);
                        }
                    }
                }
            }

            Log.WriteLine(LogLevel.Info, "Loaded {0} maps.", MapsByID.Count);
        }

        public ushort GetMapidFromMapShortName(string name)
        {
            MapInfo mi = null;
            if (MapsByName.TryGetValue(name, out mi))
            {
                return mi.ID;
            }
            return 0;
        }

        public string GetMapShortNameFromMapid(ushort id)
        {
            MapInfo mi = null;
            if (MapsByID.TryGetValue(id, out mi))
            {
                return mi.ShortName;
            }
            return "";
        }

        public string GetMapFullNameFromMapid(ushort id)
        {
            MapInfo mi = null;
            if (MapsByID.TryGetValue(id, out mi))
            {
                return mi.FullName;
            }
            return "";
        }

        public void LoadMiniHouseInfo()
        {
            MiniHouses = new Dictionary<ushort, MiniHouseInfo>();
            using (var file = new SHNFile(folder + @"\MiniHouse.shn"))
            {
                using (DataTableReaderEx reader = new DataTableReaderEx(file))
                {
                    while (reader.Read())
                    {
                        MiniHouseInfo mhi = new MiniHouseInfo(reader);
                        MiniHouses.Add(mhi.ID, mhi);
                    }
                }
            }
            Log.WriteLine(LogLevel.Info, "Loaded {0} Mini Houses.", MiniHouses.Count);
        }
    }
}
