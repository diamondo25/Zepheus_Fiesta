using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Zepheus.FiestaLib.Data;
using Zepheus.FiestaLib.Networking;
using Zepheus.Util;
using Zepheus.Zone.Data;
using Zepheus.Zone.Handlers;

namespace Zepheus.Zone.Game
{
    public sealed class Map
    {
        public const int SectorCount = 16;
        public MapInfo MapInfo { get; private set; }
        public ushort MapID { get { return MapInfo.ID; } }
        public short InstanceID { get; private set; }
        public ConcurrentDictionary<ushort, MapObject> Objects { get; private set; }
        public ConcurrentDictionary<ushort, Drop> Drops { get; private set; }
        public BlockInfo Block { get; private set; }

        private Queue<ushort> availableLifeKeys = new Queue<ushort>();
        private ushort lifeIndexer = 1;
        private Queue<ushort> availableDropKeys = new Queue<ushort>();
        private ushort dropIndexer = 1;

        private Sector[,] sectors = new Sector[SectorCount, SectorCount];
        private int sectorWidth = 800; //default
        private int sectorHeight = 800;
        public List<MobBreedLocation> MobBreeds { get; private set; }

        public Map(MapInfo info, BlockInfo block, short instanceID)
        {
            this.MapInfo = info;
            this.Block = block;
            this.InstanceID = instanceID;
            this.Objects = new ConcurrentDictionary<ushort, MapObject>();
            this.Drops = new ConcurrentDictionary<ushort, Drop>();
            Load();
        }

        private void Load()
        {
            LoadSectors();
            LoadNPC();
            LoadMobBreeds();
        }

        private void LoadNPC()
        {
            //NPC's are shown always, they don't dissapear from sectors
            foreach (var spawn in MapInfo.NPCs)
            {
                NPC npc = new NPC(spawn);
                FullAddObject(npc);
            }
        }

        private void LoadMobBreeds()
        {
            string location = Settings.Instance.DataFolder + @"\MobSpawns\" + this.MapInfo.ShortName + ".xml";
            if (File.Exists(location))
            {
                XmlSerializer x = new XmlSerializer(typeof(List<MobBreedLocation>));
                using (var stream = File.Open(location, FileMode.Open))
                {
                    MobBreeds = (List<MobBreedLocation>)x.Deserialize(stream);
                }
            }
            else
            {
                MobBreeds = new List<MobBreedLocation>();
            }
        }

        public void SaveMobBreeds()
        {
            string location = Settings.Instance.DataFolder + @"\MobSpawns\" + this.MapInfo.ShortName + ".xml";

            XmlSerializer x = new XmlSerializer(typeof(List<MobBreedLocation>));
            using (var stream = File.Open(location, FileMode.Create))
            {
                x.Serialize(stream, MobBreeds);
            }
        }

        public void AddDrop(Drop drop)
        {
            ushort key = 0;
            if (availableDropKeys.Count > 0)
            {
                key = availableDropKeys.Dequeue();
            }
            else
            {
                if (dropIndexer == ushort.MaxValue)
                {
                    Log.WriteLine(LogLevel.Warn, "Drop buffer overflow at map {0}.", this.MapInfo.ShortName);
                    return;
                }
                else
                    key = dropIndexer++;
            }

            if (Drops.TryAdd(key, drop))
            {
                drop.ID = key;
                drop.MapSector = GetSectorByPos(drop.Position);
                drop.MapSector.AddDrop(drop);
            }
            else Log.WriteLine(LogLevel.Warn, "Failed to add drop at map {0}.", this.MapInfo.ShortName);
        }

        public void RemoveDrop(Drop drop)
        {
            if (drop.MapSector == null)
            {
                Log.WriteLine(LogLevel.Warn, "Tried to remove drop where sectors wasn't assigned.");
                return;
            }
            Drop test;
            if (Drops.TryRemove(drop.ID, out test) && test == drop)
            {
                availableDropKeys.Enqueue(drop.ID);
                drop.MapSector.RemoveDrop(drop);
                drop.MapSector = null;
            }
        }

        private void LoadSectors()
        {
            try
            {
                int sectorcount = SectorCount;
                if (Block != null)
                {
                    while (Block.Width / sectorcount < MapInfo.ViewRange && sectorcount != 1)
                    {
                        sectorcount--;
                    }
                    sectorWidth = Block.Width / sectorcount;
                    if (sectorWidth < MapInfo.ViewRange)
                    {
                        sectorWidth = MapInfo.ViewRange;
                    }
                    sectorHeight = Block.Height / sectorcount;
                    if (sectorHeight < MapInfo.ViewRange)
                    {
                        sectorHeight = MapInfo.ViewRange;
                    }
                }

                for (int y = 0; y < sectorcount; y++)
                {
                    for (int x = 0; x < sectorcount; x++)
                    {
                        sectors[y, x] = new Sector(x, y, this);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, ex.ToString());
            }
        }

        public Sector GetSectorByPos(int x, int y)
        {
            int ymatrix = y / sectorHeight;
            int xmatrix = x / sectorWidth;
            return sectors[ymatrix, xmatrix];
        }

        public List<Sector> GetSurroundingSectors(Sector sector)
        {
            int x = sector.X;
            int y = sector.Y;
            List<Sector> toret = new List<Sector>();
            for (int column = x - 1; column <= x + 1; ++column)
            {
                for (int row = y - 1; row <= y + 1; ++row)
                {
                    if (column < 0 || row < 0 || column >= SectorCount || row >= SectorCount)
                    {
                        continue;
                    }

                    toret.Add(sectors[row, column]);
                }
            }
            return toret;
        }

        public List<Sector> GetSectorsNewInRange(Sector oldsector, Sector newsector)
        {
            return new List<Sector>(newsector.SurroundingSectors.Except(oldsector.SurroundingSectors));
        }

        public List<Sector> GetSectorsOutOfRange(Sector oldsector, Sector newsector)
        {
            return new List<Sector>(oldsector.SurroundingSectors.Except(newsector.SurroundingSectors));
        }

        public List<ZoneCharacter> GetCharactersInRegion(Sector sector)
        {
            return GetCharactersBySectors(GetSurroundingSectors(sector));
        }

        public List<ZoneCharacter> GetCharactersBySectors(List<Sector> input)
        {
            List<ZoneCharacter> toret = new List<ZoneCharacter>();
            foreach (Sector sursector in input)
            {
                foreach (MapObject obj in sursector.Objects.Values.Where(o => o is ZoneCharacter))
                {
                    toret.Add((ZoneCharacter)obj);
                }
            }
            return toret;
        }

        public List<Drop> GetDropsBySectors(List<Sector> input)
        {
            List<Drop> toret = new List<Drop>();
            foreach (Sector sect in input)
            {
                toret.AddRange(sect.Drops.Values);
            }
            return toret;
        }

        public List<MapObject> GetObjectsBySectors(List<Sector> input)
        {
            List<MapObject> toret = new List<MapObject>();
            foreach (Sector sursector in input)
            {
                foreach (MapObject obj in sursector.Objects.Values.Where(o => !(o is NPC)))
                {
                    toret.Add(obj);
                }
            }
            return toret;
        }

        public void SendCharacterLeftMap(ZoneCharacter character, bool toplayer = true)
        {
            using (var removeObjPacket = Handler7.RemoveObject(character)) // Make new packet to remove object from map for others
            {
                foreach (var victimObject in character.MapSector.Objects.Values)
                {
                    if (victimObject == character) continue; // ...
                    if (victimObject is NPC) continue; // NPC's are for noobs. Can't despawn

                    if (victimObject is ZoneCharacter)
                    {
                        // Remove obj for player
                        ZoneCharacter victim = victimObject as ZoneCharacter;
                        victim.Client.SendPacket(removeObjPacket);
                    }

                    if (character != null && toplayer)
                    {
                        // Despawn victimObject for obj
                        using (var removeVictimPacket = Handler7.RemoveObject(victimObject))
                        {
                            character.Client.SendPacket(removeVictimPacket);
                        }
                    }
                }
            }
        }

        public void SendCharacterEnteredMap(ZoneCharacter character)
        {
            //we send all players in region to character
            List<ZoneCharacter> characters = GetCharactersInRegion(character.MapSector);
            using (var packet = Handler7.SpawnMultiPlayer(characters, character))
            {
                character.Client.SendPacket(packet);
            }

            //we send character to all players in region
            using (var packet = Handler7.SpawnSinglePlayer(character))
            {
                foreach (ZoneCharacter charinmap in characters)
                {
                    if (charinmap == character) continue;
                    charinmap.Client.SendPacket(packet);
                }
            }

            //we send moblist and NPC to local character
            IEnumerable<MapObject> npcs = Objects.Values.Where(o => o is NPC);
            IEnumerable<MapObject> monsters = GetObjectsBySectors(character.MapSector.SurroundingSectors).Where(o => o is Mob);

            List<MapObject> obj = new List<MapObject>(npcs);
            obj.AddRange(monsters);
            if (obj.Count > 0)
            {
                for (int i = 0; i < obj.Count; i += 255)
                {

                    using (var packet = Handler7.MultiObjectList(obj, i, i + Math.Min(255, obj.Count - i)))
                    {
                        character.Client.SendPacket(packet);
                    }
                }
            }

            //we send all drops to the character
            using (var spawndrops = Handler7.ShowDrops(GetDropsBySectors(character.MapSector.SurroundingSectors)))
            {
                character.Client.SendPacket(spawndrops);
            }
        }

        public Sector GetSectorByPos(Vector2 position)
        {
            return GetSectorByPos(position.X, position.Y);
        }

        public bool FinalizeAdd(MapObject obj)
        {
            Sector sector = GetSectorByPos(obj.Position);
            sector.AddObject(obj, true);
            return Objects.TryAdd(obj.MapObjectID, obj);
        }

        public bool AssignObjectID(MapObject obj)
        {
            bool result = false;
            lock (availableLifeKeys)
            {
                if (availableLifeKeys.Count == 0)
                {
                    if (lifeIndexer == ushort.MaxValue)
                    {
                        Log.WriteLine(LogLevel.Warn, "Map is having map object id overflow (cannot handler more than {0})", ushort.MaxValue);
                        result = false;
                    }
                    else
                    {
                        ushort key = lifeIndexer;
                        ++lifeIndexer;
                        obj.MapObjectID = key;
                        result = true;
                    }
                }
                else
                {
                    ushort key = availableLifeKeys.Dequeue();
                    obj.MapObjectID = key;
                    result = true;
                }
                if (result)
                    obj.Map = this;
                return result;
            }
        }


        public bool FullAddObject(MapObject obj)
        {
            Log.WriteLine(LogLevel.Debug, "Added {0} to the map.", obj.GetType().ToString());
            if (AssignObjectID(obj))
            {
                return FinalizeAdd(obj);
            }
            else return false;
        }

        public bool RemoveObject(ushort mapobjid)
        {
            MapObject obj;
            if (Objects.TryRemove(mapobjid, out obj))
            {
                Log.WriteLine(LogLevel.Debug, "Removed {0} (type: {1}) from the map.", mapobjid, obj.GetType().ToString());
                obj.MapSector.RemoveObject(obj);
                obj.MapObjectID = 0;
                obj.Map = null;
                lock (availableLifeKeys)
                {
                    availableLifeKeys.Enqueue(mapobjid);
                }
                return true;
            }
            else return false;
        }

        public void Broadcast(Packet packet)
        {
            foreach (KeyValuePair<ushort, MapObject> kvp in Objects.Where(o => o.Value is ZoneCharacter))
            {
                ZoneCharacter victim = (ZoneCharacter)kvp.Value;
                if (victim.Client != null)
                    victim.Client.SendPacket(packet);
            }
        }

        public void Update(DateTime date)
        {
            foreach (KeyValuePair<ushort, MapObject> kvp in Objects)
            {
                kvp.Value.Update(date);
            }

            foreach (var mb in MobBreeds)
            {
                mb.Update(date);
            }

            foreach (var drop in Drops.Values)
            {
                if (drop.IsExpired(date))
                {
                    drop.CanTake = false;
                    RemoveDrop(drop);
                }
            }
        }
    }
}
