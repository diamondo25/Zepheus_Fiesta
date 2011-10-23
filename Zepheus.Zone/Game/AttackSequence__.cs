using System;

using Zepheus.Zone.Handlers;

namespace Zepheus.Zone.Game
{
    public class AttackSequence__
    {
        public enum AnimationState
        {
            Starting,
            Running,
            Ended
        }
        public struct Attack
        {
            public ushort Damage;
            public bool Crit;
            public ushort MoveTime;
            public ushort Skill;

            public Attack(ushort dmg, bool crit, ushort movetime, ushort skill = 0)
            {
                Damage = dmg;
                Crit = crit;
                MoveTime = movetime;
                Skill = skill;
            }
        }

        private static byte _counter = 0;
        public static byte Counter { get { return _counter++; } }

        public AnimationState State { get; set; }
        private MapObject From;
        private MapObject To; // Could be a player too (PvP or EvP)
        private ushort _ToID;
        private DateTime _nextSequence;

        private ushort attackSpeed;
        private byte stance = 0;
        private ushort skillid = 0xFFFF;
        private bool IsSkill { get { return skillid == 0xFFFF; } }

        public AttackSequence__(MapObject from, MapObject to, ushort skill = 0xFFFF, ushort attackspeed = 1400)
        {
            From = from;
            To = to;
            _ToID = to.MapObjectID;
            State = AnimationState.Running;
            _nextSequence = Program.CurrentTime;
            attackSpeed = attackspeed;
            skillid = skill;
        }

        private void SetNewSequenceTime(ushort msecs)
        {
            _nextSequence = Program.CurrentTime.AddMilliseconds(msecs);
        }

        private uint GetHPLeft()
        {
            if (To == null || To.IsDead)
            {
                return 0;
            }
            else
            {
                return To.HP;
            }
        }

        private void Handle()
        {
            if (To != null)
            {
                ushort seed = (ushort)Program.Randomizer.Next(0, 100); //we use one seed & base damage on it

                ushort Damage = (ushort)Program.Randomizer.Next(0, seed);
                bool Crit = seed >= 80;
                stance = (byte)(Program.Randomizer.Next(0, 3));
                To.Damage(From, Damage);
                Handler9.SendAttackAnimation(From, _ToID, attackSpeed, stance);
                Handler9.SendAttackDamage(From, _ToID, Damage, Crit, GetHPLeft(), To.UpdateCounter);

                if (To.IsDead)
                {
                    if (To is Mob && From is ZoneCharacter)
                    {
                        uint exp = (To as Mob).InfoServer.MonEXP;
                        (From as ZoneCharacter).GiveEXP(exp, _ToID);
                    }
                    Handler9.SendDieAnimation(From, _ToID);
                    State = AnimationState.Ended;
                    To = null;
                }
                else
                {
                    SetNewSequenceTime(attackSpeed);
                }
            }
        }

        public void Update(DateTime time)
        {
            if ((To != null && To.IsDead) || (From != null && From.IsDead))
            {
                State = AnimationState.Ended;
            }

            if (State == AnimationState.Ended || _nextSequence > time) return;

            if (State == AnimationState.Running)
            {
                Handle();
            }
        }
        
    }
}
