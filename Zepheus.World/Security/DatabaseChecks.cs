using System.Linq;

namespace Zepheus.World.Security
{
    public sealed class DatabaseChecks
    {
        public static bool IsCharNameUsed(string name)
        {
            return Program.Entity.Characters.Count(ch => ch.Name.ToLower() == name.ToLower()) > 0;
        }
    }
}
