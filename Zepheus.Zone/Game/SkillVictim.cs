
namespace Zepheus.Zone.Game
{
    public class SkillVictim
    {
        public ushort MapObjectID { get; private set; }
        public uint Damage { get; private set; }
        public uint HPLeft { get; private set; }
        public byte Stance1 { get; private set; }
        public byte Stance2 { get; private set; }
        public ushort HPCounter { get; private set; }

        public SkillVictim(ushort id, uint dmg, uint hpleft, byte s1, byte s2, ushort count)
        {
            MapObjectID = id;
            Damage = dmg;
            HPLeft = hpleft;
            Stance1 = s1;
            Stance2 = s2;
            HPCounter = count;
        }
    }
}
