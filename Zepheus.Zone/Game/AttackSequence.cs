using System;
using System.Collections.Generic;
using Zepheus.FiestaLib.Data;
using Zepheus.Util;
using Zepheus.Zone.Data;
using Zepheus.Zone.Handlers;

namespace Zepheus.Zone.Game
{
    public class AttackSequence
    {
        public enum AnimationState
        {
            Starting,
            Running,
            AoEShow,
            AoEDo,
            Ended
        }

        public const ushort NoSkill = 0xFFFF;

        private DateTime _nextAction;
        private ushort skillid;
        private ActiveSkillInfo skillInfo { get { return DataProvider.Instance.ActiveSkillsByID[skillid]; } }
        public bool IsSkill { get { return skillid != NoSkill; } }
        public bool IsAoE { get { return this.X != 0 && this.Y != 0; } }
        private uint MinDamage;
        private uint MaxDamage;
        private ushort attackspeed;
        private uint X;
        private uint Y;
        private ushort AnimationID;

        private MapObject attacker;
        private MapObject victim;
        public AnimationState State { get; private set; }

        public AttackSequence(MapObject att, MapObject vict, uint min, uint max, ushort attackspeed)
        {
            this.State = AnimationState.Starting;
            this.attacker = att;
            this.victim = vict;
            this.MinDamage = min;
            this.MaxDamage = max;
            this.attackspeed = attackspeed;
            this._nextAction = Program.CurrentTime;
            this.skillid = NoSkill;
            this.AnimationID = att.UpdateCounter;
            this.State = AnimationState.Running;
        }


        public AttackSequence(MapObject att, MapObject vict, uint min, uint max, ushort skillid, bool derp) :
            this(att, vict, min, max, 0)
        {
            this.skillid = skillid;
            this.MinDamage += skillInfo.MinDamage;
            this.MaxDamage += skillInfo.MaxDamage;
            this.X = 0;
            this.Y = 0;
        }

        public AttackSequence(MapObject att, uint min, uint max, ushort skillid, uint x = 0, uint y = 0) :
            this(att, null, min, max, skillid, true)
        {
            this.X = x;
            this.Y = y;
            this._nextAction = Program.CurrentTime.AddMilliseconds(skillInfo.CastTime);
            this.State = AnimationState.AoEShow;
        }


        public void Update(DateTime now)
        {
            if (this.State == AnimationState.Ended) return;
            if (this.attacker == null || this.attacker.IsDead)
            {
                this.State = AnimationState.Ended;
                return;
            }
            if (!IsAoE && (this.victim == null || this.victim.IsDead))
            {
                this.State = AnimationState.Ended;
                return;
            }
            if (this._nextAction > now) return;

            if (IsSkill)
            {
                if (IsAoE)
                {
                    if (this.State == AnimationState.AoEShow)
                    {
                        Handler9.SendSkillPosition(attacker, AnimationID, skillid, X, Y);
                        Handler9.SendSkillAnimationForPlayer(attacker, skillid, AnimationID);
                        this._nextAction = Program.CurrentTime.AddMilliseconds(skillInfo.SkillAniTime);
                        this.State = AnimationState.AoEDo;
                    }
                    else if (this.State == AnimationState.AoEDo)
                    {
                        // Lets create an AoE skill @ X Y
                        List<SkillVictim> victims = new List<SkillVictim>();
                        var pos = new Vector2((int)X, (int)Y);
                        // Find victims
                        foreach (var v in attacker.Map.GetObjectsBySectors(attacker.MapSector.SurroundingSectors))
                        {
                            if (attacker == v) continue;
                            if (v is ZoneCharacter) continue;
                            if (Vector2.Distance(v.Position, pos) > skillInfo.Range) continue;
                            // Calculate dmg

                            uint dmg = (uint)Program.Randomizer.Next((int)MinDamage, (int)MaxDamage);
                            if (dmg > v.HP)
                            {
                                v.HP = 0;
                            }

                            if (!v.IsDead)
                            {
                                v.Attack(attacker);
                            }
                            else
                            {
                                if (v is Mob && attacker is ZoneCharacter)
                                {
                                    uint exp = (v as Mob).InfoServer.MonEXP;
                                    (attacker as ZoneCharacter).GiveEXP(exp, v.MapObjectID);
                                }
                            }

                            victims.Add(new SkillVictim(v.MapObjectID, dmg, v.HP, 0x01, 0x01, v.UpdateCounter));
                            if (victims.Count == skillInfo.MaxTargets) break;
                        }

                        Handler9.SendSkill(attacker, AnimationID, victims);
                        foreach (var v in victims)
                        {
                            if (v.HPLeft == 0)
                            {
                                Handler9.SendDieAnimation(attacker, v.MapObjectID);
                            }
                        }

                        victims.Clear();
                        State = AnimationState.Ended;
                    }
                }
                else
                {
                    // Normal skill parsing
                    State = AnimationState.Ended;
                }
            }
            else
            {
                // Just attacking...
                if (victim == null || victim.IsDead)
                {
                    victim = null;
                    attacker = null;
                    State = AnimationState.Ended;
                }
                else
                {
                    // Calculate some damage to do
                    ushort dmg = (ushort)Program.Randomizer.Next((int)MinDamage, (int)MaxDamage);
                    if (dmg > victim.HP)
                    {
                        victim.HP = 0;
                    }
                    bool Crit = Program.Randomizer.Next() % 100 >= 80;
                    byte stance = (byte)(Program.Randomizer.Next(0, 3));
                    victim.Damage(attacker, dmg);
                    Handler9.SendAttackAnimation(attacker, victim.MapObjectID, attackspeed, stance);
                    Handler9.SendAttackDamage(attacker, victim.MapObjectID, dmg, Crit, victim.HP, victim.UpdateCounter);

                    if (victim.IsDead)
                    {
                        if (victim is Mob && attacker is ZoneCharacter)
                        {
                            uint exp = (victim as Mob).InfoServer.MonEXP;
                            (attacker as ZoneCharacter).GiveEXP(exp, victim.MapObjectID);
                        }
                        
                        Handler9.SendDieAnimation(attacker, victim.MapObjectID);
                        victim = null;
                        State = AnimationState.Ended;
                    }
                    else
                    {
                        _nextAction = now.AddMilliseconds(attackspeed);
                    }
                }
            }
        }
    }
}
