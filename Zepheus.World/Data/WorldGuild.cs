using System.Linq;

using Zepheus.Database;

namespace Zepheus.World.Data
{
    public sealed class WorldGuild
    {
        private Guild _guild;

        public string Name { get { return _guild.Name; } private set { _guild.Name = value; } }

        public WorldGuild(Guild guild)
        {
            _guild = guild;
        }

        public static WorldGuild GetGuild(int ID)
        {
            WorldGuild g;
            DataProvider.Instance.Guilds.TryGetValue(ID, out g);
            if (g == null)
            {
                var ng = Program.Entity.Guilds.First(v => v.ID == ID);
                if (ng == null)
                {
                    return null;
                }
                else
                {
                    g = new WorldGuild(ng);
                    DataProvider.Instance.Guilds.Add(ID, g);
                }
            }
            return g;
        }
    }
}
