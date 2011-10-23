
using Zepheus.FiestaLib;
using Zepheus.FiestaLib.Networking;
using Zepheus.Zone.Game;
namespace Zepheus.Zone.Handlers
{
    public sealed class Handler18
    {
        public static void SendSkillLearnt(ZoneCharacter character, ushort skillid)
        {
            using (var packet = new Packet(SH18Type.LearnSkill))
            {
                packet.WriteUShort(skillid);
                packet.WriteByte(0); //unk
                character.Client.SendPacket(packet);
            }
        }
    }
}
