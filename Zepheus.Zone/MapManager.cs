using System.Collections.Generic;
using Zepheus.FiestaLib.Data;
using Zepheus.Util;
using Zepheus.Zone.Data;
using Zepheus.Zone.Game;

namespace Zepheus.Zone
{
    [ServerModule(Util.InitializationStage.DataStore)]
    public sealed class MapManager
    {
        public static MapManager Instance { get; private set; }
        public Dictionary<MapInfo, List<Map>> Maps { get; private set; }

        public MapManager()
        {
            Maps = new Dictionary<MapInfo, List<Map>>();
        }

        public Map GetMap(MapInfo info, short instance = 0)
        {
            //lazy loading
            if (!Maps.ContainsKey(info))
            {
                Maps.Add(info, new List<Map>());
            }
            BlockInfo block;
            DataProvider.Instance.Blocks.TryGetValue(info.ID, out block);
            Map toret;
            List<Map> maps = Maps[info];
            if (maps.Count == 0)
            {
                //we load the first map instance
                maps.Add(toret = new Map(info, block, (short)maps.Count));
            }
            else
            {
                if (maps.Count < instance) // Check if instance exists, else, add another
                {
                    if (maps.Count - 1 < instance)
                    {
                        // ohnoes
                        Log.WriteLine(LogLevel.Info, "Couldn't find instance for map {0}", info.ID);
                        instance = 0;
                    }
                    else
                    {
                        // Add another instance of map
                        maps.Add(toret = new Map(info, block, (short)maps.Count));
                    }
                }
                toret = Maps[info][instance];
            }
            return toret;
        }

        [InitializerMethod]
        public static bool Load()
        {
            Instance = new MapManager();
            return true;
        }
    }
}
