using System;
using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Data;
using Zepheus.FiestaLib.Networking;
using Zepheus.Util;
using Zepheus.Zone.Data;
using Zepheus.Zone.Handlers;

namespace Zepheus.Zone.Game
{
    class Mob : MapObject
    {
        public ushort ID { get; set; }
        public byte Level { get; set; }
        public bool Moving { get; set; }
        public MapObject Target { get; set; }
        public const int MinMovement = 60;
        public const int MaxMovement = 180;
        private MobBreedLocation Spawnplace;
        private bool DeathTriggered;
        public MobInfo Info { get; private set; }
        public MobInfoServer InfoServer { get; private set; }

        public override uint MaxHP { get { return Info.MaxHP; } set { return; } }
        public override uint MaxSP { get { return 100; } set { return; } } //TODO: load from mobinfoserver

        private DateTime _nextUpdate;
        private Vector2 boundryLT;
        private Vector2 boundryRB;

        public AttackSequence AttackingSequence { get; private set; }

        public Mob(MobBreedLocation mbl)
        {
            ID = mbl.MobID;

            Init();

            // Make random location
            if (!mbl.Map.AssignObjectID(this))
            {
                Log.WriteLine(LogLevel.Warn, "Couldn't spawn mob, out of ID's");
                return;
            }
            Map = mbl.Map;
            Spawnplace = mbl;
            while (true)
            {
                Position = Vector2.GetRandomSpotAround(Program.Randomizer, mbl.Position, 30);
                if (Map.Block.CanWalk(Position.X, Position.Y))
                {
                    break;
                }
            }
            SetBoundriesFromPointAndRange(Position, 100);

            Spawnplace.CurrentMobs++;
        }

        public Mob(ushort pID, Vector2 Pos)
        {
            ID = pID;
            Position = Pos;
            Init();
            SetBoundriesFromPointAndRange(Pos, 700);
        }

        private void Init()
        {

            Info = DataProvider.Instance.GetMobInfo(ID);
            MobInfoServer temp;
            DataProvider.Instance.MobData.TryGetValue(Info.Name, out temp);
            InfoServer = temp;
            Moving = false;
            Target = null;
            Spawnplace = null;
            _nextUpdate = Program.CurrentTime;
            DeathTriggered = false;

            HP = MaxHP;
            SP = MaxSP;
            Level = Info.Level;

        }

        private bool PositionIsInBoundries(Vector2 pos)
        {
            return !(pos.X < boundryLT.X || pos.X > boundryRB.X || pos.Y < boundryLT.Y || pos.Y > boundryRB.Y);
        }

        private void SetBoundriesFromPointAndRange(Vector2 startpos, int range)
        {
            boundryLT = new Vector2(startpos.X - range, startpos.Y - range);
            boundryRB = new Vector2(startpos.X + range, startpos.Y + range);
        }

        public void Die()
        {
            HP = 0;
            Moving = false;
            boundryLT = null;
            boundryRB = null;
            AttackingSequence = null;
            Target = null;
            DeathTriggered = true;

            if (Spawnplace != null)
            {
                Spawnplace.CurrentMobs--;
            }
            _nextUpdate = Program.CurrentTime.AddSeconds(3);
        }

        public override void Attack(MapObject victim)
        {
            base.Attack(victim); // lol

            if (AttackingSequence != null) return;
            AttackingSequence = new AttackSequence(this, victim, 0, InfoServer.Str, 1400);
            Target = victim;
        }

        public override void AttackSkill(ushort skillid, MapObject victim)
        {
            base.AttackSkill(skillid, victim); // lol

            if (AttackingSequence != null) return;
            AttackingSequence = new AttackSequence(this, victim, 0, InfoServer.Str, skillid, true);
            Target = victim;
        }

        public override void AttackSkillAoE(ushort skillid, uint X, uint Y)
        {
            base.AttackSkillAoE(skillid, X, Y); // lol

            if (AttackingSequence != null) return;
            AttackingSequence = new AttackSequence(this, 0, InfoServer.Str, skillid, X, Y);
        }

        public override Packet Spawn()
        {
            Packet packet = new Packet(SH7Type.SpawnSingleObject);
            Write(packet);
            return packet;
        }

        public void Write(Packet packet)
        {
            packet.WriteUShort(this.MapObjectID);
            packet.WriteByte(2);
            packet.WriteUShort(ID);
            packet.WriteInt(this.Position.X);
            packet.WriteInt(this.Position.Y);
            packet.WriteByte(this.Rotation);
            packet.Fill(54, 0);
        }

        public void WriteUpdateStats(Packet packet)
        {
            packet.WriteUInt(HP);
            packet.WriteUInt(MaxHP); // Max HP
            packet.WriteUInt(SP);
            packet.WriteUInt(MaxSP); // Max SP
            packet.WriteByte(Level);
            packet.WriteUShort(this.UpdateCounter);
        }

        public override void Update(DateTime date)
        {
            if (Position == null)
            {
                return;
            }

            if (IsDead)
            {
                if (!DeathTriggered)
                {
                    Die();
                    return; // Wait till 3 seconds are over, then remove
                }
                else if (_nextUpdate <= date)
                {
                    Map.RemoveObject(this.MapObjectID);
                    Position = null;
                    return;
                }
                return;
            }

            if (AttackingSequence != null && Target != null)
            {
                if (Vector2.Distance(Target.Position, Position) < 50)
                {
                    AttackingSequence.Update(date);
                    if (AttackingSequence.State == AttackSequence.AnimationState.Ended)
                    {
                        AttackingSequence = null;
                        Target = null;

                    }
                }
                else
                {
                    _nextUpdate = _nextUpdate.AddDays(-1);
                }
            }

            if (_nextUpdate > date) return;


            if (Target != null)
            {
                _nextUpdate = Program.CurrentTime.AddSeconds(1);

                // Try to move to target's pos
                // Might glitch the fuck out. lol
                if (Target.Map != Map)
                {
                    Target = null; // Stop aggro-ing >:(
                }
                else
                {
                    if (Vector2.Distance(Target.Position, Position) < 800)
                    {
                        if (Map.Block.CanWalk(Target.Position.X, Target.Position.Y))
                        {
                            Move(Position.X, Position.Y, Target.Position.X, Target.Position.Y, false, false);
                        }
                    }
                    else
                    {
                        Target = null; // Stop aggro-ing >:(
                    }
                }
                return;
            }
            else
            {
                _nextUpdate = Program.CurrentTime.AddSeconds(Program.Randomizer.Next(10, 60)); // Around 10 seconds to 1 minute before new movement is made

                // Move to random spot.
                Vector2 newpos = new Vector2(Position);
                bool ok = false;
                for (int i = 1; i <= 20; i++)
                {
                    // Generate new position, and check if it's in valid bounds, else recheck
                    newpos = Vector2.GetRandomSpotAround(Program.Randomizer, newpos, 60);
                    if (newpos.X > 0 && newpos.Y > 0 && Map.Block.CanWalk(newpos.X, newpos.Y) && PositionIsInBoundries(newpos))
                    {
                        ok = true;
                        break;
                    }
                    /*
                    int t = Program.Randomizer.Next() % 11;

                    if (t <= 2)
                    {
                        // All +

                        newx += Program.Randomizer.Next(MinMovement, MaxMovement);
                        newy += Program.Randomizer.Next(MinMovement, MaxMovement);
                    }
                    else if (t <= 5)
                    {
                        newx -= Program.Randomizer.Next(MinMovement, MaxMovement);
                        newy += Program.Randomizer.Next(MinMovement, MaxMovement);
                    }
                    else if (t <= 8)
                    {
                        newx += Program.Randomizer.Next(MinMovement, MaxMovement);
                        newy -= Program.Randomizer.Next(MinMovement, MaxMovement);
                    }
                    else
                    {
                        newx -= Program.Randomizer.Next(MinMovement, MaxMovement);
                        newy -= Program.Randomizer.Next(MinMovement, MaxMovement);
                    }
                    Vector2 test = newpos + new Vector2(newx, newy);
                    if (Map.Block.CanWalk(test.X, test.Y) && PositionIsInBoundries(test))
                    {
                        newpos = test;
                        break;
                    }
                    */
                }

                if (ok)
                {
                    Move(Position.X, Position.Y, newpos.X, newpos.Y, false, false);
                }
            }
        }

        public void Move(int oldx, int oldy, int newx, int newy, bool walk, bool stop)
        {
            Position.X = newx;
            Position.Y = newy;
            Sector movedin = Map.GetSectorByPos(Position);
            if (movedin != MapSector)
            {
                MapSector.Transfer(this, movedin);
            }

            if (stop)
            {
                using (var packet = Handler8.StopObject(this))
                {
                    Map.Broadcast(packet);
                }
            }
            else
            {
                ushort speed = 0;
                if (walk) speed = 60;
                else speed = 115;

                using (var packet = Handler8.MoveObject(this, oldx, oldy, walk, speed))
                {
                    Map.Broadcast(packet);
                }
            }
        }
    }
}
