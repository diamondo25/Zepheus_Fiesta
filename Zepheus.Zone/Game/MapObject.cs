using System;
using System.Collections.Generic;
using Zepheus.FiestaLib.Networking;
using Zepheus.Util;
using Zepheus.Zone.Handlers;

namespace Zepheus.Zone.Game
{
    public abstract class MapObject
    {
        public byte Rotation { get; set; }
        public Vector2 Position { get; set; }
        public ushort MapObjectID { get; set; }
        bool IsAdded { get; set; }
        public bool IsAttackable { get; set; }
        public Map Map { get; set; }
        public Sector MapSector { get; set; }

        public virtual uint HP { get; set; }
        public virtual uint MaxHP { get; set; }
        public virtual uint SP { get; set; }
        public virtual uint MaxSP { get; set; }
        public bool IsDead { get { return HP == 0; } }

        // HP/SP update counter thing
        private ushort _statUpdateCounter = 0;
        public ushort UpdateCounter { get { return ++_statUpdateCounter; } }
        public List<ZoneCharacter> SelectedBy { get; private set; }

        public virtual void Attack(MapObject victim)
        {
            if (victim != null && !victim.IsAttackable) return;
        }

        public virtual void AttackSkill(ushort skillid, MapObject victim)
        {
            if (victim != null && !victim.IsAttackable) return;
        }

        public virtual void AttackSkillAoE(ushort skillid, uint X, uint Y)
        {
        }

        public abstract void Update(DateTime date);
        public abstract Packet Spawn();


        public virtual void Revive(bool totally = false)
        {
            if (totally)
            {
                HP = MaxHP;
                SP = MaxSP;
            }
            else
            {
                HP = 50;
            }
        }

        public virtual void Damage(MapObject bully, uint amount, bool isSP = false)
        {
            if (isSP)
            {
                if (SP < amount) SP = 0;
                else SP -= amount;
            }
            else
            {
                if (HP < amount) HP = 0;
                else HP -= amount;
            }

            if (bully == null)
            {
                if (this is ZoneCharacter)
                {
                    ZoneCharacter character = this as ZoneCharacter;
                    if (isSP)
                        Handler9.SendUpdateSP(character);
                    else
                        Handler9.SendUpdateHP(character);
                }
            }
            else
            {
                if (this is Mob && ((Mob)this).AttackingSequence == null)
                {
                    ((Mob)this).Attack(bully);
                }
                else if (this is ZoneCharacter && !((ZoneCharacter)this).IsAttacking)
                {
                    ((ZoneCharacter)this).Attack(bully);
                }
            }
        }

        public MapObject()
        {
            IsAttackable = true;
            SelectedBy = new List<ZoneCharacter>();
        }

        ~MapObject()
        {
            SelectedBy.Clear();
        }
    }
}
