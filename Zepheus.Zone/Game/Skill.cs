
using Zepheus.Database;
using Zepheus.FiestaLib.Data;
using Zepheus.FiestaLib.Networking;
using Zepheus.Zone.Data;

namespace Zepheus.Zone.Game
{
    public class Skill
    {
        private DatabaseSkill _skill;
        public ushort ID { get { return (ushort)_skill.SkillID; } set { _skill.SkillID = (short)value; } }
        public short Upgrades { get { return _skill.Upgrades; } set { _skill.Upgrades = value; } }
        public bool IsPassive { get { return _skill.IsPassive; } private set { _skill.IsPassive = value; } }
        public ActiveSkillInfo Info { get { return DataProvider.Instance.ActiveSkillsByID[ID]; } }

        public Skill(DatabaseSkill skill)
        {
            _skill = skill;
        }

        public Skill(ZoneCharacter c, ushort ID)
        {
            DatabaseSkill db = new DatabaseSkill();
            db.Owner = c.ID;
            db.SkillID = (short)ID;
            db.Upgrades = 0;
            db.IsPassive = false;
            db.Character = c.character;
            Program.Entity.AddToDatabaseSkills(db);
            Program.Entity.SaveChanges();
            _skill = db;
        }

        public void Write(Packet pPacket)
        {
            pPacket.WriteUShort(ID);
            pPacket.WriteInt(60000); // Cooldown
            //pPacket.WriteShort(Upgrades);
            pPacket.WriteUShort(GetUpgrades(4, 3, 2, 1));

            pPacket.WriteInt(9000);         // Skill exp???
        }

        public static ushort GetUpgrades(byte val1, byte val2, byte val3, byte val4)
        {
            int ret = 0;
            ret |= (val1 & 0xF);
            ret |= ((val2 & 0xF) << 4);
            ret |= ((val3 & 0xF) << 8);
            ret |= (val4 << 12);
            return (ushort)ret;
        }
    }
}
