using System;
using System.Collections.Generic;
using System.Linq;
using Zepheus.Database;
using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Data;
using Zepheus.FiestaLib.Networking;
using Zepheus.Util;
using Zepheus.Zone.Data;
using Zepheus.Zone.Handlers;
using Zepheus.Zone.InterServer;
using Zepheus.Zone.Networking;
using Zepheus.Zone.Security;

namespace Zepheus.Zone.Game
{
    public sealed class ZoneCharacter : MapObject
    {
        public const byte ChatDelay = 0;
        public const byte ShoutDelay = 10;

        public Character character { get; private set; }
        public int ID { get { return character.ID; } }
        public int AccountID { get { return character.AccountID; } }
        public string Name { get { return character.Name; } set { character.Name = value; } }
        public byte Slot { get { return character.Slot; } }
        public byte MapID { get { return character.Map; } set { character.Map = value; } }
        public byte Level { get { return character.CharLevel; } set { character.CharLevel = value; } }
        public Job Job { get { return (Job)character.Job; } set { character.Job = (byte)value; } }
        public bool Male { get { return character.Male; } set { character.Male = value; } }

        // Next values we map locally & save at the end (less stress on entity)
        public long Exp { get; set; }
        public short StonesSP { get; set; }
        public short StonesHP { get; set; }
        // End of local variables

        public int Fame { get { return character.Fame; } set { character.Fame = value; } }
        public long Money { get { return character.Money; } set { character.Money = value; } }
        public byte Hair { get { return character.Hair; } set { character.Hair = value; } }
        public byte HairColor { get { return character.HairColor; } set { character.HairColor = value; } }
        public byte Face { get { return character.Face; } set { character.Face = value; } }
        public byte StatPoints { get { return character.StatPoints; } set { character.StatPoints = value; } }
        public byte Str { get { return character.StrStats; } set { character.StrStats = value; } }
        public byte Dex { get { return character.DexStats; } set { character.DexStats = value; } }
        public byte Int { get { return character.IntStats; } set { character.IntStats = value; } }
        public byte Spr { get { return character.SprStats; } set { character.SprStats = value; } }
        public byte End { get { return character.EndStats; } set { character.EndStats = value; } }
        public override uint MaxHP { get { return (uint)(BaseStats.MaxHP + GetMaxHPBuff()); } set { return; } }
        public override uint MaxSP { get { return (uint)(BaseStats.MaxSP + GetMaxSPBuff()); } set { return; } }

        //local shit
        public ZoneClient Client { get; set; }
        public Dictionary<ItemSlot, Equip> EquippedItems { get; private set; }
        public Dictionary<sbyte, Item> InventoryItems { get; private set; }
        public Dictionary<ushort, Skill> SkillsActive { get; private set; }
        public Dictionary<ushort, Skill> SkillsPassive { get; private set; }
        public PlayerState State { get; set; }
        public MapObject SelectedObject { get; set; }
        public FiestaBaseStat BaseStats { get { return DataProvider.Instance.GetBaseStats(Job, Level); } }
        private Buffs Buffs { get; set; }
        public House House { get; set; }
        public Question Question { get; set; }
        private AttackSequence AttackingSequence;
        public bool IsAttacking { get { return AttackingSequence != null && AttackingSequence.State != AttackSequence.AnimationState.Ended; } }

        public DateTime LastShout { get; set; }
        public DateTime LastChat { get; set; }
        public DateTime ChatBlocked { get; set; }

        public DateTime NextHPRest { get; set; }
        public DateTime NextSPRest { get; set; }

        //lazy loading cheattracker
        private CheatTracker _tracker;
        public CheatTracker CheatTracker { get { return _tracker ?? (_tracker = new CheatTracker(this)); } }

        public ZoneCharacter(string name, bool loadequips = true)
        {
            try
            {
                character = Program.Entity.Characters.First(c => c.Name == name);
                if (character == null) throw new Exception("Character not found.");
                Program.Entity.Refresh(System.Data.Objects.RefreshMode.StoreWins, character); //fixes changes when character is offline
                Buffs = new Buffs(this);
                LastShout = Program.CurrentTime;
                ChatBlocked = DateTime.MinValue;
                NextSPRest = DateTime.MaxValue;
                NextHPRest = DateTime.MaxValue;
                SelectedObject = null;
                House = null;
                HP = (uint)character.HP; //we copy these to make less stress on Entity
                SP = (uint)character.SP;
                Exp = character.Exp;
                StonesHP = character.HPStones;
                StonesSP = character.SPStones;
                State = PlayerState.Normal;
                LoadItems();
                if (IsDead)
                {
                    HP = MaxHP / 4;
                    Exp = Exp / 2;
                    MapInfo mi;
                    DataProvider.Instance.MapsByID.TryGetValue(MapID, out mi);
                    if (mi != null)
                    {
                        character.XPos = mi.RegenX;
                        character.YPos = mi.RegenY;
                    }
                }
                SetMap(MapID);
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "Error reading character from entity: {0}", ex.ToString());
            }
        }

        public bool Save()
        {
            character.HP = (int)this.HP;
            character.SP = (int)this.SP;
            character.Exp = this.Exp;
            character.HPStones = this.StonesHP;
            character.SPStones = this.StonesSP;
            character.XPos = this.Position.X;
            character.YPos = this.Position.Y;
            if (Map != null)
            {
                character.Map = (byte)Map.MapID;
            }
            return Program.Entity.SaveChanges() > 0;
        }

        public override string ToString()
        {
            return "ZoneCharacter(" + this.Name + " | " + this.ID + ")";
        }

        public void GiveEXP(uint amount, ushort mobid = 0xFFFF)
        {
            if (Level == DataProvider.Instance.ExpTable.Count) return; // No overleveling
            if (Exp + amount < 0)
            {
                Exp = long.MaxValue;
            }
            else
            {
                Exp += amount;
            }

            Handler9.SendGainEXP(this, amount, mobid);

            while (true)
            {
                if ((ulong)this.Exp >= DataProvider.Instance.GetMaxExpForLevel(Level))
                {
                    LevelUP(mobid); // Auto levels
                }
                else
                {
                    break;
                }
            }
        }

        public void SetMap(ushort MapID, short instance = -1)
        {
            MapInfo info;
            if (DataProvider.Instance.MapsByID.TryGetValue(MapID, out info))
            {
                Map = MapManager.Instance.GetMap(info);
                if (Map.Block != null)
                {
                    if (!Map.Block.CanWalk(character.XPos, character.YPos))
                    {
                        character.XPos = Map.MapInfo.RegenX;
                        character.YPos = Map.MapInfo.RegenY;
                    }
                }
                Position = new Vector2(character.XPos, character.YPos);
                Rotation = 0x55; //degrees / 2
                Map.AssignObjectID(this);
            }
            else
            {
                Log.WriteLine(LogLevel.Warn, "Character joined the wrong zone. Map {0} doesn't belong here.", MapID);
            }
        }

        public void EquipItem(Equip equip)
        {
            if (equip.IsEquipped || Level < equip.Info.Level) return;

            sbyte oldslot = equip.Slot;

            Equip oldequip;
            EquippedItems.TryGetValue(equip.Info.Slot, out oldequip);

            if (oldequip != null)
            {
                oldequip.Slot = oldslot;
                EquippedItems.Remove(equip.Info.Slot);
            }

            InventoryItems.Remove(oldslot);
            Handler12.ModifyInventorySlot(this, 0x20,  (byte)oldslot, (byte)equip.Info.Slot, oldequip);
            if (oldequip != null)
            {
                InventoryItems.Add(oldequip.Slot, oldequip);
            }

            equip.Slot = (sbyte)equip.Info.Slot;
            equip.IsEquipped = true;
            EquippedItems.Add(equip.Info.Slot, equip);
            Handler12.ModifyEquipSlot(this, (byte)equip.Info.Slot, (byte)oldslot, equip);
            Save();

            using (var broad = Handler7.Equip(this, equip))
            {
                Broadcast(broad);
            }
        }

        public void MoveItem(sbyte fromslot, sbyte toslot)
        {
            Item from;
            if (!InventoryItems.TryGetValue(fromslot, out from))
            {
                Log.WriteLine(LogLevel.Warn, "{0} tried to move non-existing item.", this.Name);
                return;
            }

            Item to;
            InventoryItems.TryGetValue(toslot, out to);

            InventoryItems.Remove(fromslot);
            if (to != null)
            {
                InventoryItems.Remove(toslot);
                to.Slot = fromslot;
                InventoryItems.Add(fromslot, to);
            }

            from.Slot = toslot;
            InventoryItems.Add(toslot, from);
            Save();

            Handler12.ModifyInventorySlot(this, 0x24, (byte)toslot, (byte)fromslot, from);
            Handler12.ModifyInventorySlot(this, 0x24, (byte)fromslot, (byte)toslot, to);
        }


        public void UnequipItem(ItemSlot source, sbyte destination)
        {
            Equip equip;
            if (!EquippedItems.TryGetValue(source, out equip))
            {
                Log.WriteLine(LogLevel.Warn, "{0} unequipped non-existing item.", character.Name);
                Handler12.FailedUnequip(this);
                return;
            }

            if (InventoryItems.ContainsKey(destination))
            {
                if (!GetFreeInventorySlot(out destination))
                {
                    Handler12.FailedUnequip(this);
                    return;
                }
            }

            if (EquippedItems.Remove(source))
            {
                equip.Slot = destination;
                InventoryItems.Add(destination, equip);

                Handler12.ModifyEquipSlot(this, (byte)source, (byte)destination, null); //unequip
                Handler12.ModifyInventorySlot(this, 0x20, (byte)destination, (byte)source, equip);
                Save();

                using (var broad = Handler7.Unequip(this, equip))
                {
                    Broadcast(broad);
                }
            }
            else return;
        }

        public InventoryStatus GiveItem(ushort id, byte amount = 1)
        {
            ItemInfo info;
            if (DataProvider.Instance.ItemsByID.TryGetValue(id, out info))
            {
                if (info.Slot == FiestaLib.ItemSlot.Normal)
                {
                    foreach (var i in InventoryItems.Values)
                    {
                        if (i.ItemID == id && i.Amount < i.Info.MaxLot)
                        {
                            // We found the same item and it can stack more!
                            byte left = (byte)(i.Info.MaxLot - i.Amount);
                            if (left > amount)
                            {
                                i.Amount += left;
                                amount -= left;
                            }
                            else
                            {
                                i.Amount += amount;
                                amount = 0;
                            }
                            Handler12.ModifyInventorySlot(this, 0x24, (byte)i.Slot, (byte)i.Slot, i);
                            if (amount == 0)
                            {
                                break;
                            }
                        }
                    }
                    // If we have still some stuff left, go ahead and create new stacks!
                    if (amount > 0)
                    {
                        while (amount > 0)
                        {
                            sbyte invslot;
                            if (GetFreeInventorySlot(out invslot))
                            {
                                DatabaseItem item = new DatabaseItem();
                                item.Amount = amount;
                                item.Character = character;
                                item.ObjectID = info.ItemID;
                                item.Slot = invslot;
                                Program.Entity.AddToDatabaseItems(item);
                                Item nitem = new Item(item);
                                InventoryItems.Add(invslot, nitem);
                                Handler12.ModifyInventorySlot(this, 0x24, (byte)invslot, (byte)invslot, nitem);
                                amount -= info.MaxLot;
                            }
                            else return InventoryStatus.FULL;
                        }
                    }
                    Save();
                    return InventoryStatus.ADDED;
                }
                else
                {
                    sbyte invslot;
                    if (GetFreeInventorySlot(out invslot))
                    {
                        DatabaseEquip equip = new DatabaseEquip();
                        equip.Character = character;
                        equip.EquipID = info.ItemID;
                        equip.Slot = invslot;
                        Program.Entity.AddToDatabaseEquips(equip);
                        Equip nequip = new Equip(equip);
                        InventoryItems.Add(invslot, nequip);
                        Save();
                        Handler12.ModifyInventorySlot(this, 0x24, (byte)invslot, (byte)invslot, nequip);
                        return InventoryStatus.ADDED;
                    }
                    else return InventoryStatus.FULL;
                }
            }
            else return InventoryStatus.NOT_FOUND;
        }

        public void UpgradeItem(sbyte eqpslot, sbyte stoneslot)
        {
            Item eqpitem, stone;
            if (!InventoryItems.TryGetValue(eqpslot, out eqpitem) ||
                !InventoryItems.TryGetValue(stoneslot, out stone))
            {
                Log.WriteLine(LogLevel.Warn, "Invalid item enhancement: item slot does not exist.");
                return;
            }

            Equip eqp;
            if ((eqp = eqpitem as Equip) == null)
            {
                Log.WriteLine(LogLevel.Warn, "Character tried to upgrade non-equip item.");
                return;
            }

            if (stone.Info.UpResource == 0)
            {
                Log.WriteLine(LogLevel.Warn, "Character tried to upgrade with non-upgrade item.");
                return;
            }

            byte required = 0;
            if (eqp.Upgrades <= 2) required = 2;
            else if (eqp.Upgrades <= 5) required = 5;
            else if (eqp.Upgrades <= 8) required = 8;
            else required = 10;

            if (stone.Info.UpResource != required)
            {
                Log.WriteLine(LogLevel.Warn, "Character is using a wrong upgrade stone.");
                return;
            }

            UseOneItemStack(stone);
            int rand = Program.Randomizer.Next(0, 200);
            bool success = rand <= stone.Info.UpSucRation;

            if (success)
            {
                eqp.Upgrades++;
                Handler12.SendUpgradeResult(this, true);
            }
            else
            {
                //TODO: destroy item rate?
                if (eqp.Upgrades > 0) --eqp.Upgrades;
                Handler12.SendUpgradeResult(this, false);
            }
            Handler12.ModifyInventorySlot(this, 0x24, (byte)eqpslot, (byte)eqpslot, eqp);
        }

        public void DropItemRequest(sbyte slot)
        {
            Item item;
            if (!InventoryItems.TryGetValue(slot, out item))
            {
                //TODO: send client 'item not found'
                Log.WriteLine(LogLevel.Warn, "Client tried to drop non-existing object.");
                return;
            }

            if (Question != null)
            {
                Log.WriteLine(LogLevel.Debug, "Client is answering another question. Cannot proceed drop.");
                return;
            }

            Question = new Question("Do you want to discard the item?", new QuestionCallback(OnDropResponse), item);
            Question.Add("Yes", "No");
            Question.Send(this, 500);
        }

        public void LootItem(ushort id)
        {
            sbyte freeslot;
            bool gotslot = GetFreeInventorySlot(out freeslot);

            Drop drop;
            if (Map.Drops.TryGetValue(id, out drop))
            {
                if (!drop.CanTake || Vector2.Distance(this.Position, drop.Position) >= 500)
                {
                    Handler12.ObtainedItem(this, drop.Item, ObtainedItemStatus.FAILED);
                    return;
                }
                else if (!gotslot)
                {
                    Handler12.ObtainedItem(this, drop.Item, ObtainedItemStatus.INV_FULL);
                    return;
                }

                drop.CanTake = false; //just to be sure
                Map.RemoveDrop(drop);
                Item item = null;
                if (drop.Item is DroppedEquip)
                {
                    item = new Equip(drop.Item as DroppedEquip, this, freeslot);
                }
                else
                {
                    item = new Item(drop.Item, this, freeslot);
                }
                Handler12.ObtainedItem(this, drop.Item, ObtainedItemStatus.OBTAINED);
                Handler12.ModifyInventorySlot(this, 0x24, (byte)freeslot, 0, item);
            }
        }

        public void DropItem(Item item)
        {
            Drop drop;
            if (item is Equip)
            {
                drop = new Drop(item as Equip, this, Position.X, Position.Y, 120);
            }
            else
            {
                drop = new Drop(item, this, Position.X, Position.Y, 120);
                
            }
            InventoryItems.Remove(item.Slot);
            item.Remove();
            Handler12.ModifyInventorySlot(this, 0x24, (byte)item.Slot, 0, null);
            Map.AddDrop(drop);
        }

        private void OnDropResponse(ZoneCharacter character, byte answer)
        {
            Item item = (Item)character.Question.Object;
            switch (answer)
            {
                case 0:
                    DropItem(item);
                    break;
                case 1:

                    break;

                default:
                    Log.WriteLine(LogLevel.Warn, "Invalid dropitem response.");
                    break;
            }
        }

        public bool GetFreeInventorySlot(out sbyte value)
        {
            value = -1;
            for (sbyte i = 0; i < 96; i++)
            {
                if (!InventoryItems.ContainsKey(i))
                {
                    value = i;
                    return true;
                }
            }
            return false;
        }

        public int ChatCheck()
        {
            int currentblock = CheckSpamBlock();
            if (currentblock > 0) return currentblock;
            if (Program.CurrentTime.Subtract(LastChat).TotalSeconds <= ChatDelay)
            {
                ChatBlocked = Program.CurrentTime.AddSeconds(10);
                return 10;
            }
            else
            {
                LastChat = Program.CurrentTime;
                return -1;
            }
        }

        public int ShoutCheck()
        {
            int currentblock = CheckSpamBlock();
            if (currentblock > 0) return currentblock;
            if (Program.CurrentTime.Subtract(LastShout).TotalSeconds <= ShoutDelay)
            {
                ChatBlocked = Program.CurrentTime.AddSeconds(10);
                return 10;
            }
            else
            {
                LastShout = Program.CurrentTime;
                return -1;
            }
        }

        public int CheckSpamBlock()
        {
            DateTime now = Program.CurrentTime;
            if (now < ChatBlocked)
            {
                return (int)ChatBlocked.Subtract(Program.CurrentTime).TotalSeconds;
            }
            else return -1;
        }

        public void ChangeMap(ushort ID, int x = -1, int y = -1, short instance = -1)
        {
            if (ID > 120)
            {
                Log.WriteLine(LogLevel.Warn, "Character trying to warp to unexisting map: {0}", ID);
                DropMessage("Unable to transfer to this map. Error code 10");
                return;
            }
            ZoneData zci = Program.GetZoneForMap(ID);

            if (zci != null)
            {
                var v = zci.MapsToLoad.Find(m => m.ID == ID);
                int tox = 0;
                int toy = 0;
                if (x < 0 || y < 0)
                {
                    tox = v.RegenX;
                    toy = v.RegenY;
                }
                else
                {
                    tox = x;
                    toy = y;
                }

                // Try setting up transfer.
                ushort RandomID = (ushort)Program.Randomizer.Next(0, ushort.MaxValue);

                InterHandler.TransferClient(zci.ID, this.Client.AccountID, this.Client.Username, this.Name, RandomID, this.Client.Admin, this.Client.Host);

                Map.RemoveObject(MapObjectID);
                Position.X = tox;
                Position.Y = toy;
                character.Map = (byte)ID;
                Save();
                Handler6.SendChangeZone(this, ID, tox, toy, zci.IP, zci.Port, RandomID);
            }
            else
            {
                DropMessage("Unable to transfer to this map. Error code 1");
            }
        }

        public void DropMessage(string text, params object[] param)
        {
            Handler8.SendAdminNotice(Client, String.Format(text, param));
        }

        public void Teleport(int newx, int newy)
        {
            Position.X = newx;
            Position.Y = newy;
            Sector movedin = Map.GetSectorByPos(Position);
            if (movedin != MapSector)
            {
                MapSector.Transfer(this, movedin);
            }
        }

        public void Move(int oldx, int oldy, int newx, int newy, bool walk, bool stop)
        {
            Teleport(newx, newy);

            if (stop)
            {
                using (var packet = Handler8.StopObject(this))
                {
                    Broadcast(packet);
                }
            }
            else
            {
                ushort speed = 0;
                if (walk) speed = 60;
                else speed = 115; 
                // else if (horse) speed = 165

                using (var packet = Handler8.MoveObject(this, oldx, oldy, walk, speed))
                {
                    Broadcast(packet);
                }
            }
        }

        public void Broadcast(Packet packet, bool toself = false)
        {
            Broadcast(packet, MapSector.SurroundingSectors, toself);
        }

        public void Broadcast(Packet packet, List<Sector> sectors, bool toself = false)
        {
            foreach (var character in Map.GetCharactersBySectors(sectors))
            {
                if ((!toself && character == this) || character.Client == null) continue;
                character.Client.SendPacket(packet);
            }
        }

        public override Packet Spawn()
        {
            return Handler7.SpawnSinglePlayer(this);
        }

        public void Ban()
        {
            Save();
            // Program.worldService.DisconnectClient(this.Name, true); // TODO: Inter server packet.
            Client.Disconnect();
        }

        private void LoadItems()
        {
            EquippedItems = new Dictionary<ItemSlot, Equip>();
            foreach (var eqp in character.Equips.Where(e => e.Slot < 0))
            {
                Equip equip = new Equip(eqp);
                if (EquippedItems.ContainsKey(equip.SlotType))
                {
                    Log.WriteLine(LogLevel.Warn, "{0} has duplicate equipped in slot {1}.", Name, equip.SlotType);
                    continue;
                }
                EquippedItems.Add(equip.SlotType, equip);
            }

            InventoryItems = new Dictionary<sbyte, Item>();
            foreach (var eqp in character.Equips.Where(e => e.Slot >= 0))
            {
                Equip equip = new Equip(eqp);
                if (InventoryItems.ContainsKey(equip.Slot))
                {
                    Log.WriteLine(LogLevel.Warn, "{0} has a duplicate item in slot {0}.", Name, equip.Slot);
                    continue;
                }
                InventoryItems.Add(equip.Slot, equip);
            }

            foreach (var ditem in character.Items)
            {
                Item item = new Item(ditem);
                InventoryItems.Add((sbyte)item.Slot, item);
            }

            SkillsActive = new Dictionary<ushort, Skill>();
            SkillsPassive = new Dictionary<ushort, Skill>();
            foreach (var skill in character.Skills)
            {
                Skill s = new Skill(skill);
                if (s.IsPassive)
                {
                    SkillsPassive.Add(s.ID, s);
                }
                else
                {
                    SkillsActive.Add(s.ID, s);
                }
            }
        }

        public void SendGetIngameChunk()
        {
            Handler4.SendCharacterInfo(this);
            Handler4.SendCharacterLook(this);
            Handler4.SendQuestListBusy(this);
            Handler4.SendQuestListDone(this);
            Handler4.SendActiveSkillList(this);
            Handler4.SendPassiveSkillList(this);
            Handler4.SendEquippedList(this);
            Handler4.SendInventoryList(this);
            Handler4.SendHouseList(this);
            Handler4.SendPremiumEmotions(this);
            Handler4.SendPremiumItemList(this);
            Handler4.SendTitleList(this);
            Handler4.SendCharacterChunkEnd(this);

            Handler6.SendDetailedCharacterInfo(this);
        }

        public void UseItem(sbyte slot)
        {
            Item item;
            if (!InventoryItems.TryGetValue(slot, out item)) //TODO: not sure about return scrolls
            {
                //TODO: send item not found / error occured packet
                return;
            }

            if (item.Info.Level > Level)
            {
                Handler12.SendItemUsed(this, item, 1800);
                return;
            }

            if (((uint)item.Info.Jobs & (uint)Job) == 0)
            {
                Handler12.SendItemUsed(this, item, 1801);
                return;
            }

            if (item.Info.Type == ItemType.Useable) //potion
            {
                if (item.Info.Class == ItemClass.ReturnScroll) //return scroll
                {
                    RecallCoordinate coord;
                    MapInfo map;
                    if (DataProvider.Instance.RecallCoordinates.TryGetValue(item.Info.InxName, out coord)
                        && (map = DataProvider.Instance.MapsByID.Values.First(m => m.ShortName == coord.MapName)) != null)
                    {
                        Handler12.SendItemUsed(this, item); //No idea what this does, but normally it's sent.
                        UseOneItemStack(item);
                        ChangeMap(map.ID, coord.LinkX, coord.LinkY); //TODO: do this properly via world later.

                    }
                    else
                    {
                        Handler12.SendItemUsed(this, item, 1811);
                    }
                }
                else if (item.Info.Class == ItemClass.Skillbook)
                {
                    //TODO: passive skills!
                    ActiveSkillInfo info;
                    if (DataProvider.Instance.ActiveSkillsByName.TryGetValue(item.Info.InxName, out info))
                    {
                        if (SkillsActive.ContainsKey(info.ID))
                        {
                            Handler12.SendItemUsed(this, item, 1811);
                            //character has this skill already
                        }
                        else
                        {
                            Handler12.SendItemUsed(this, item);
                            UseOneItemStack(item);
                            DatabaseSkill dskill = new DatabaseSkill();
                            dskill.Character = character;
                            dskill.SkillID = (short)info.ID;
                            dskill.IsPassive = false;
                            dskill.Upgrades = 0;
                            Program.Entity.AddToDatabaseSkills(dskill);
                            Save();
                            Skill skill = new Skill(dskill);
                            SkillsActive.Add(skill.ID, skill);
                            Handler18.SendSkillLearnt(this, skill.ID);
                            //TODO: broadcast the animation of learning to others
                        }
                    }
                    else
                    {
                        Log.WriteLine(LogLevel.Error, "Character tried to use skillbook but ActiveSkill does not exist.");
                        Handler12.SendItemUsed(this, item, 1811);
                    }
                }
                else
                {
                    ItemUseEffectInfo effects;
                    if (!DataProvider.Instance.ItemUseEffects.TryGetValue(item.ItemID, out effects))
                    {
                        Log.WriteLine(LogLevel.Warn, "Missing ItemUseEffect for ID {0}", item.ItemID);
                        Handler12.SendItemUsed(this, item, 1811);
                        return;
                    }

                    Handler12.SendItemUsed(this, item); //No idea what this does, but normally it's sent.
                    UseOneItemStack(item);
                    foreach (ItemEffect effect in effects.Effects)
                    {
                        switch (effect.Type)
                        {
                            case ItemUseEffectType.AbState: //TOOD: add buffs for itemuse
                                continue;

                            case ItemUseEffectType.HP:
                                HealHP(effect.Value);
                                break;

                            case ItemUseEffectType.SP:
                                HealSP(effect.Value);
                                break;
                            case ItemUseEffectType.ScrollTier:

                                break;

                            default:
                                Log.WriteLine(LogLevel.Warn, "Invalid item effect for ID {0}: {1}", item.ItemID, effect.Type.ToString());
                                break;
                        }
                    }
                }
            }
            else
            {
                Log.WriteLine(LogLevel.Warn, "Invalid item use.");
            }
        }

        private void UseOneItemStack(Item item)
        {
            byte sendslot = (byte)item.Slot;
            if (item.Amount > 1)
            {
                --item.Amount;
                Handler12.ModifyInventorySlot(this, 0x24, sendslot, sendslot, item);
            }
            else
            {
                if (InventoryItems.Remove(item.Slot))
                {
                    item.Remove();
                    Handler12.ModifyInventorySlot(this, 0x24, sendslot, sendslot, null);
                }
                else Log.WriteLine(LogLevel.Warn, "Error deleting item from slot {0}.", item.Slot);
            }
            Save();
        }

        public override void Update(DateTime date)
        {
            if (AttackingSequence != null)
            {
                AttackingSequence.Update(date);
                if (AttackingSequence.State == AttackSequence.AnimationState.Ended)
                {
                    AttackingSequence = null;
                }
            }

            if (SelectedObject != null)
            {
                if (SelectedObject is Mob)
                {
                    if ((SelectedObject as Mob).IsDead) SelectedObject = null; // Stop the reference ffs
                }
            }

            if (State == PlayerState.Resting)
            {
                if (date >= NextHPRest)
                {
                    HealHP((uint)(MaxHP / 1000 * House.Info.HPRecovery));
                    //TODO: also show this to people who have me selected.
                    NextHPRest = date.AddMilliseconds(House.Info.HPTick);
                }
                if (date >= NextSPRest)
                {
                    HealSP((uint)(MaxSP / 1000 * House.Info.SPRecovery));
                    //TODO: also show this to people who have me selected.
                    NextSPRest = date.AddMilliseconds(House.Info.SPTick);
                }
            }
        }

        public void Heal()
        {
            HP = MaxHP;
            SP = MaxSP;

            if (State == PlayerState.Dead)
            {
                State = PlayerState.Normal;
            }

            Handler9.SendUpdateHP(this);
            Handler9.SendUpdateSP(this);
        }

        public void Damage(uint value)
        {
            Damage(null, value);
        }

        public bool RemoveFromMap()
        {
            if (Map != null)
            {
                return Map.RemoveObject(this.MapObjectID);
            }
            else return false;
        }

        public void WriteCharacterDisplay(Packet packet)
        {
            packet.WriteUShort(MapObjectID);
            packet.WriteString(Name, 16);
            packet.WriteInt(Position.X);
            packet.WriteInt(Position.Y);
            packet.WriteByte(Rotation);                // Rotation
            packet.WriteByte((byte)State);          // Player State (1,2 - Player, 3 - Dead, 4 - Resting, 5 - Vendor, 6 - On Mount)
            packet.WriteByte((byte)Job);
            if (State != PlayerState.Resting && State != PlayerState.Vendor && this.House == null)
            {
                WriteLook(packet);
                WriteEquipment(packet);
            }
            else
            {
                this.House.WritePacket(packet);
            }
            WriteRefinement(packet);

            packet.WriteUShort(0xffff);  // Mount Handle
            packet.WriteUShort(0xffff);
            packet.WriteByte(0xff);          // Emote (0xff = nothing)
            packet.WriteUShort(0xffff);
            packet.WriteShort(0);
            packet.WriteUShort(0);             // Mob ID (title = 10)

            packet.Fill(53, 0);                // Buff Bits? Something like that
            packet.WriteInt(character.GuildID.HasValue ? character.GuildID.Value : 0);      // Guild ID
            packet.WriteByte(0x02);            // UNK (0x02)
            packet.WriteBool(false);            // In Guild Academy (0 - No, 1 - Yes)
            packet.WriteBool(true);            // Pet AutoPickup   (0 - Off, 1 - On)
            packet.WriteByte(this.Level);
        }

        public ushort GetEquippedBySlot(ItemSlot slot)
        {
            Equip eqp;
            if (EquippedItems.TryGetValue(slot, out eqp))
            {
                return eqp.ItemID;
            }
            else return ushort.MaxValue;
        }

        public void WriteRefinement(Packet packet)
        {
            packet.WriteByte(Convert.ToByte(GetUpgradesBySlot(ItemSlot.Weapon) << 4 | GetUpgradesBySlot(ItemSlot.Weapon2)));
            packet.WriteByte(0);    		// UNK
            packet.WriteByte(0);    		// UNK
        }

        public byte GetUpgradesBySlot(ItemSlot slot)
        {
            Equip eqp;
            if (EquippedItems.TryGetValue(slot, out eqp))
            {
                return eqp.Upgrades;
            }
            else return 0;
        }

        public void WriteEquipment(Packet packet)
        {
            packet.WriteUShort(GetEquippedBySlot(ItemSlot.Helm));
            packet.WriteUShort(GetEquippedBySlot(ItemSlot.Weapon));
            packet.WriteUShort(GetEquippedBySlot(ItemSlot.Armor));
            packet.WriteUShort(GetEquippedBySlot(ItemSlot.Weapon2));
            packet.WriteUShort(GetEquippedBySlot(ItemSlot.Pants));
            packet.WriteUShort(GetEquippedBySlot(ItemSlot.Boots));
            packet.WriteUShort(GetEquippedBySlot(ItemSlot.CostumeBoots));
            packet.WriteUShort(GetEquippedBySlot(ItemSlot.CostumePants));
            packet.WriteUShort(GetEquippedBySlot(ItemSlot.CostumeArmor));
            packet.Fill(6, 0xff);              // UNK
            packet.WriteUShort(GetEquippedBySlot(ItemSlot.Glasses));
            packet.WriteUShort(GetEquippedBySlot(ItemSlot.CostumeHelm));
            packet.Fill(2, 0xff);              // UNK
            packet.WriteUShort(GetEquippedBySlot(ItemSlot.CostumeWeapon));
            packet.WriteUShort(GetEquippedBySlot(ItemSlot.Wing));
            packet.Fill(2, 0xff);              // UNK
            packet.WriteUShort(GetEquippedBySlot(ItemSlot.Tail));
            packet.WriteUShort(GetEquippedBySlot(ItemSlot.Pet));
        }

        public void WriteDetailedInfo(Packet pPacket)
        {
            pPacket.WriteInt(ID);
            pPacket.WriteString(this.Name, 16);
            pPacket.WriteByte(this.Slot);
            pPacket.WriteByte(this.Level);
            pPacket.WriteLong(this.Exp);
            pPacket.WriteInt(12345678);                // UNK
            pPacket.WriteShort(this.StonesHP);
            pPacket.WriteShort(this.StonesSP);
            pPacket.WriteUInt(this.HP);
            pPacket.WriteUInt(this.SP);
            pPacket.WriteInt(this.Fame);                // Fame
            pPacket.WriteLong(this.Money); //TODO: inventory class
            pPacket.WriteString(this.Map.MapInfo.ShortName, 12);
            pPacket.WriteInt(this.Position.X);
            pPacket.WriteInt(this.Position.Y);
            pPacket.WriteByte(this.Rotation);
            pPacket.WriteByte(this.Str);   // Str bonus
            pPacket.WriteByte(this.End);   // End bonus
            pPacket.WriteByte(this.Dex);   // Dex bonus
            pPacket.WriteByte(this.Int);   // Int bonus 
            pPacket.WriteByte(this.Spr);   // Spr bonus
            pPacket.WriteShort(0);               // UNK
            pPacket.WriteUInt(0);               // Killpoints
            pPacket.Fill(7, 0);                 // UNK
        }

        public void WriteLook(Packet packet)
        {
            packet.WriteByte(Convert.ToByte(0x01 | ((byte)Job << 2) | (Male ? 1 : 0) << 7));
            packet.WriteByte(this.Hair);
            packet.WriteByte(this.HairColor);
            packet.WriteByte(this.Face);
        }

        //TODO: add equip stats & buffs too
        public int GetExtraStr()
        {
            return this.Str + Buffs.Str;
        }

        public int GetExtraEnd()
        {
            return this.End + Buffs.End;
        }

        public int GetExtraDex()
        {
            return this.Dex + Buffs.Dex;
        }

        public int GetExtraInt()
        {
            return this.Int + Buffs.Int;
        }

        public int GetExtraSpr()
        {
            return this.Spr + Buffs.Spr;
        }

        public int GetWeaponDamage(bool buffed = false)
        {
            return this.Str + (this.Str % 10) + (buffed ? Buffs.WeaponDamage : 0);
        }

        public int GetMagicDamage(bool buffed = false)
        {
            return 10 + (buffed ? Buffs.MagicDamage : 0);
        }

        public int GetWeaponDefense(bool buffed = false)
        {
            return 10 + (buffed ? Buffs.WeaponDefense : 0);
        }

        public int GetMagicDefense(bool buffed = false)
        {
            return 10 + (buffed ? Buffs.MagicDefense : 0);
        }

        public int GetEvasion(bool buffed = false)
        {
            return 6 + (buffed ? Buffs.Evasion : 0);
        }

        public int GetMaxHPBuff()
        {
            return Buffs.MaxHP;
        }

        public int GetMaxSPBuff()
        {
            return Buffs.MaxSP;
        }

        public int GetAim(bool buffed = false)
        {
            return 15;
            //TODO: basestats aim + dex?
            //aim inc (calculate later based on dex)
        }

        public void WriteDetailedInfoExtra(Packet packet, bool levelUP = false)
        {
            if (!levelUP)
            {
                packet.WriteUShort(this.MapObjectID);
            }

            packet.WriteLong(this.Exp);
            packet.WriteULong(DataProvider.Instance.GetMaxExpForLevel(this.Level));

            packet.WriteInt(BaseStats.Strength);
            packet.WriteInt(BaseStats.Strength + GetExtraStr());
            packet.WriteInt(BaseStats.Endurance);
            packet.WriteInt(BaseStats.Endurance + GetExtraEnd());
            packet.WriteInt(BaseStats.Dexterity);
            packet.WriteInt(BaseStats.Dexterity + GetExtraDex());
            packet.WriteInt(BaseStats.Intelligence);
            packet.WriteInt(BaseStats.Intelligence + GetExtraInt());
            packet.WriteInt(0); // Wizdom. It isn't set in the server so it can contain shit from old buffers... :D
            packet.WriteInt(0); // I once had a name here :P
            packet.WriteInt(BaseStats.Spirit);
            packet.WriteInt(BaseStats.Spirit + GetExtraSpr());

            packet.WriteInt(GetWeaponDamage()); //base damage
            packet.WriteInt(GetWeaponDamage(true)); //increased damage (e.g. buffs)
            packet.WriteInt(GetMagicDamage()); //magic dmg
            packet.WriteInt(GetMagicDamage(true)); //inc magic dmg

            packet.WriteInt(GetWeaponDefense()); //todo equip stats loading (weapondef)
            packet.WriteInt(GetWeaponDefense(true)); //weapondef inc

            packet.WriteInt(GetAim()); //TODO: basestats aim + dex?
            packet.WriteInt(GetAim(true)); //aim inc (calcuate later based on dex)

            packet.WriteInt(GetEvasion()); //evasion
            packet.WriteInt(GetEvasion(true)); //evasion inc

            packet.WriteInt(GetWeaponDamage()); //damage block again
            packet.WriteInt(GetWeaponDamage(true));

            packet.WriteInt(GetMagicDamage()); //magic damage
            packet.WriteInt(GetMagicDamage(true));

            packet.WriteInt(GetMagicDefense()); //magic def 
            packet.WriteInt(GetMagicDefense(true)); //magic def inc

            packet.WriteInt(1);
            packet.WriteInt(20);
            packet.WriteInt(2);
            packet.WriteInt(40); 

            packet.WriteUInt(BaseStats.MaxHP); //max HP
            packet.WriteUInt(BaseStats.MaxSP); //max SP

            packet.WriteInt(0);                   // UNK
            packet.WriteInt(BaseStats.MAXSoulHP);   // Max HP Stones
            packet.WriteInt(BaseStats.MAXSoulSP);   // Max SP Stones
            packet.Fill(64, 0);
            if (!levelUP)
            {
                packet.WriteInt(this.Position.X);
                packet.WriteInt(this.Position.Y);
            }
        }

        public void WriteUpdateStats(Packet packet)
        {
            packet.WriteUInt(this.HP);
            packet.WriteUInt(BaseStats.MaxHP);
            packet.WriteUInt(this.SP);
            packet.WriteUInt(BaseStats.MaxSP);
            packet.WriteByte(this.Level);
            packet.WriteUShort(this.UpdateCounter);
        }

        public override void Attack(MapObject victim)
        {
            if (victim == null)
            {
                victim = SelectedObject;
            }

            if (IsAttacking || victim == null || !victim.IsAttackable) return;
            ushort attackspeed = 1200;
            Equip weapon;
            EquippedItems.TryGetValue(ItemSlot.Weapon, out weapon);
            uint dmgmin = (uint)GetWeaponDamage(true);
            uint dmgmax = (uint)(GetWeaponDamage(true) + (GetWeaponDamage(true) % 3));
            if (weapon != null)
            {
                attackspeed = weapon.Info.AttackSpeed;
                dmgmin += weapon.Info.MinMelee;
                dmgmax += weapon.Info.MaxMelee;
            }

            base.Attack(victim);
            AttackingSequence = new AttackSequence(this, victim, dmgmin, dmgmax, attackspeed);
        }

        public override void AttackSkill(ushort skillid, MapObject victim)
        {
            if (victim == null)
            {
                victim = SelectedObject;
            }

            if (IsAttacking || victim == null || !victim.IsAttackable) return;

            ushort attackspeed = 1200;
            Equip weapon;
            EquippedItems.TryGetValue(ItemSlot.Weapon, out weapon);
            uint dmgmin = (uint)GetWeaponDamage(true);
            uint dmgmax = (uint)(GetWeaponDamage(true) + (GetWeaponDamage(true) % 3));
            if (weapon != null)
            {
                attackspeed = weapon.Info.AttackSpeed;
                dmgmin += weapon.Info.MinMelee;
                dmgmax += weapon.Info.MaxMelee;
            }

            AttackingSequence = new AttackSequence(this, victim, dmgmin, dmgmax, skillid, true);
        }

        public override void AttackSkillAoE(ushort skillid, uint x, uint y)
        {
            if (IsAttacking) return;

            ushort attackspeed = 1200;
            Equip weapon;
            EquippedItems.TryGetValue(ItemSlot.Weapon, out weapon);
            uint dmgmin = (uint)GetExtraStr();
            uint dmgmax = (uint)(GetExtraStr() + (GetExtraStr() % 3));
            if (weapon != null)
            {
                attackspeed = weapon.Info.AttackSpeed;
                dmgmin += weapon.Info.MinMelee;
                dmgmax += weapon.Info.MaxMelee;
            }

            AttackingSequence = new AttackSequence(this, dmgmin, dmgmax, skillid, x, y);
        }

        public void AttackStop()
        {
            if (IsAttacking)
            {
                AttackingSequence = null;
            }
        }

        public override void Damage(MapObject bully, uint amount, bool isSP = false)
        {
            base.Damage(bully, amount, isSP);
            if (IsDead)
            {
                State = PlayerState.Dead;
                Handler4.SendReviveWindow(this.Client, 3);
            }
        }

        public void HealHP(uint value)
        {
            if (HP == MaxHP) return;

            if (HP + value > MaxHP)
                HP = MaxHP;
            else
                HP += value;

            Handler9.SendUpdateHP(this);
        }

        public void SetHP(uint value)
        {
            if (value > MaxHP) value = MaxHP;
            if (value < 0) value = 0;
            HP = value;
            Handler9.SendUpdateHP(this);
        }

        public void SetSP(uint value)
        {
            if (value > MaxSP) value = MaxSP;
            if (value < 0) value = 0;
            SP = value;
            Handler9.SendUpdateSP(this);
        }

        public void HealSP(uint value)
        {
            if (SP == MaxSP) return;

            if (SP + value > MaxSP)
                SP = MaxSP;
            else
                SP += value;

            Handler9.SendUpdateSP(this);
        }

        public void LevelUP(ushort mobid = 0xFFFF, byte levels = 1)
        {
            byte maxlvl = (byte)DataProvider.Instance.ExpTable.Count;
            if (Level + levels > maxlvl)
            {
                levels = (byte)(maxlvl - Level);
            }
            Level += levels;
            Handler9.SendLevelUPAnim(this, mobid);
            Handler9.SendLevelUPData(this, mobid);

            // Heal.
            SetHP(MaxHP);
            SetSP(MaxSP);


            character.UsablePoints += levels;
            Handler4.SendUsablePoints(Client);
        }

        public void Rest(bool pStart)
        {
            if (IsDead)
            {
                Log.WriteLine(LogLevel.Warn, "Zombie tried to rest while being dead. {0}", this);
                CheatTracker.AddCheat(CheatTypes.DEAD_REST, 100);
                return;
            }
            if (pStart && (this.State == PlayerState.Resting || this.State == PlayerState.Vendor))
            {
                Log.WriteLine(LogLevel.Warn, "Tried to go in home twice {0}", this);
                return;
            }
            else if (!pStart && this.House == null)
            {
                Log.WriteLine(LogLevel.Warn, "Tried to exit house while not in one {0}", this);
            }

            if (pStart)
            {
                this.State = PlayerState.Resting;
                this.House = new House(this, House.HouseType.Resting);
                this.NextHPRest = Program.CurrentTime.AddMilliseconds((uint)this.House.Info.HPTick);
                this.NextSPRest = Program.CurrentTime.AddMilliseconds((uint)this.House.Info.SPTick);
                Handler8.SendBeginRestResponse(this.Client, 0x0a81);

                using (var broad = Handler8.BeginDisplayRest(this))
                {
                    this.Broadcast(broad);
                }
            }
            else
            {
                this.State = PlayerState.Normal;
                this.House = null;

                Handler8.SendEndRestResponse(this.Client);
                this.NextHPRest = DateTime.MaxValue;
                this.NextSPRest = DateTime.MaxValue;
                using (var broad = Handler8.EndDisplayRest(this))
                {
                    this.Broadcast(broad);
                }
            }
        }

        public void Store(bool pStart, bool pSells = true, ushort pItemID = 0, string pName = "")
        {
            if (pStart && (this.State == PlayerState.Resting || this.State == PlayerState.Vendor))
            {
                Log.WriteLine(LogLevel.Warn, "Tried to go in home twice {0}", this);
                return;
            }
            else if (!pStart && this.House == null)
            {
                Log.WriteLine(LogLevel.Warn, "Tried to exit house while not in one {0}", this);
            }

            if (pStart)
            {
                this.State = PlayerState.Vendor;
                this.House = new House(this, pSells ? House.HouseType.SellingVendor : Game.House.HouseType.BuyingVendor, pItemID, pName);

                /*
                Handler8.SendBeginRestResponse(this.Client, 0x0a81);

                using (var broad = Handler8.BeginDisplayRest(this))
                {
                    this.Broadcast(broad);
                }
                */
            }
            else
            {
                this.State = PlayerState.Normal;
                this.House = null;

                /*
                Handler8.SendEndRestResponse(this.Client);
                using (var broad = Handler8.EndDisplayRest(this))
                {
                    this.Broadcast(broad);
                }
                */
            }
        }
    }
}