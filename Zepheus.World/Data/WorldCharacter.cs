using System;
using System.Collections.Generic;
using System.Linq;
using Zepheus.Database;
using Zepheus.FiestaLib;
using Zepheus.Util;

namespace Zepheus.World.Data
{
    public class WorldCharacter
    {
        private Character _character;
        public Character Character { get { return _character ?? (_character = LazyLoadMe()); } set { _character = value; } }
        public int ID { get; private set; }
        public Dictionary<byte, ushort> Equips { get; private set; }
        public bool IsDeleted { get; private set; }

        public WorldCharacter(Character ch)
        {
            Character = ch;
            ID = Character.ID;
            Equips = new Dictionary<byte, ushort>();
            foreach (var eqp in ch.Equips.Where(eq => eq.Slot < 0))
            {
                byte realslot = (byte)(eqp.Slot * -1);
                if (Equips.ContainsKey(realslot))
                {
                    Log.WriteLine(LogLevel.Warn, "{0} has duplicate equip in slot {1}", ch.Name, realslot);
                    Equips.Remove(realslot);
                }
                Equips.Add(realslot, (ushort)eqp.EquipID);
            }
            Detach();
        }

        private Character LazyLoadMe()
        {
            return Program.Entity.Characters.First(c => c.ID == ID);
        }

        public void Detach()
        {
            try
            {
                Program.Entity.Detach(Character);
                Character = null;
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "Error detaching character from entity: {0}.", ex.ToString());
            }
        }

        public bool Delete()
        {
            if (IsDeleted) return false;
            try
            {
                Program.Entity.Refresh(System.Data.Objects.RefreshMode.StoreWins, Character);
                Program.Entity.DeleteObject(Character);
                Program.Entity.SaveChanges();
                IsDeleted = true;
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "Error deleting character: {0}", ex.ToString());
                return false;
            }
        }

        public WorldCharacter(Character ch, byte eqpslot, ushort eqpid)
        {
            Character = ch;
            ID = Character.ID;
            Equips = new Dictionary<byte, ushort>();
            Equips.Add(eqpslot, eqpid);
        }

        public ushort GetEquipBySlot(ItemSlot slot)
        {
            if (Equips.ContainsKey((byte)slot))
            {
                return Equips[(byte)slot];
            }
            else
            {
                return ushort.MaxValue;
            }
        }

        public void SetQuickBarData(byte[] pData)
        {
            _character = LazyLoadMe();
            _character.QuickBar = pData;
            Program.Entity.SaveChanges();
            Detach();
        }

        public void SetQuickBarStateData(byte[] pData)
        {
            _character = LazyLoadMe();
            _character.QuickBarState = pData;
            Program.Entity.SaveChanges();
            Detach();
        }

        public void SetGameSettingsData(byte[] pData)
        {
            _character = LazyLoadMe();
            _character.GameSettings = pData;
            Program.Entity.SaveChanges();
            Detach();
        }

        public void SetClientSettingsData(byte[] pData)
        {
            _character = LazyLoadMe();
            _character.ClientSettings = pData;
            Program.Entity.SaveChanges();
            Detach();
        }

        public void SetShortcutsData(byte[] pData)
        {
            _character = LazyLoadMe();
            _character.Shortcuts = pData;
            Program.Entity.SaveChanges();
            Detach();
        }
    }
}
