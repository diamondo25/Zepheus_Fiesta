using System.Collections.Generic;
using Zepheus.Util;
using Zepheus.Zone.Game;

namespace Zepheus.Zone.Security
{
    public sealed class CheatTracker
    {
        public const ushort MaxPoints = 500;
        public ushort CheatPoints { get; private set; }

        private Dictionary<CheatTypes, ushort> _cheats;
        private Dictionary<CheatTypes, ushort> Cheats { get { return _cheats ?? (_cheats = new Dictionary<CheatTypes,ushort>()); } }
        private ZoneCharacter _character;

        public CheatTracker(ZoneCharacter character)
        {
            _character = character;
        }

        public void AddCheat(CheatTypes type, ushort points)
        {
            Log.WriteLine(LogLevel.Debug, "Detecting cheat from {0}: {1}", _character.Name, type.ToString());
            if (!Cheats.ContainsKey(type))
            {
                Cheats.Add(type, 0);
            }
            Cheats[type]++;
            if (_character.Client.Admin == 0)
            {
                CheatPoints += points;
                CheckBan();
            }
        }

        private void CheckBan()
        {
            if (CheatPoints >= MaxPoints)
            {
                Log.WriteLine(LogLevel.Info, "CheatTracker auto banned {0}.", _character.Name);
                _character.Ban();
            }
        }
    }

    public enum CheatTypes : byte
    {
        SPEEDWALK,
        INVALID_MOVE,
        EMOTE,
        SPAM,
        WEAPON_HACK,
        DEAD_REST,
        DEAD_SALE,
    }
}
