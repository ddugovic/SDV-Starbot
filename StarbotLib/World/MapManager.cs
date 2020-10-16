using System;
using System.Collections.Generic;
using System.Linq;

namespace StarbotLib.World
{
    [Serializable]
    public class MapManager : MarshalByRefObject
    {
        private StarbotServerCore core;

        public enum Status
        {
            Waiting,
            Setup,
            Ready
        };
        public Status status;
        public Dictionary<string, Map> mapCache;

        public MapManager(StarbotServerCore core)
        {
            this.core = core;
            mapCache = new Dictionary<string, Map>();
            Reset();
        }

        public bool MapExists(string mapID)
        {
            return mapCache.ContainsKey(mapID);
        }

        public Map AddMap(string mapID)
        {
            Map newMap = GetMap(mapID);
            if (newMap == null)
            {
                newMap = new Map(mapID);
                mapCache.Add(newMap.mapID, newMap);
            }
            return newMap;
        }

        public Map GetMap(string mapID)
        {
            Map map;
            if (mapCache.TryGetValue(mapID, out map))
            {
                return map;
            }
            return null;
        }

        public List<Map> GetMaps()
        {
            return mapCache.Values.ToList();
        }

        public int TotalLocations()
        {
            return mapCache.Values.Sum(m => m.GetLocations().Count());
        }

        public void ClearPassableCache(Map map = null)
        {
            foreach (var m in mapCache.Values.Where(m => map == null || m.Equals(map)))
            {
                m.ClearPassableCache();
            }
        }

        public void Reset()
        {
            status = Status.Waiting;
            //mapCache.Clear();
        }

        public List<SavableMap> GetSavableMaps()
        {
            var savables = new List<SavableMap>();
            foreach (var m in mapCache.Values)
            {
                savables.Add(m.GetSavable());
            }
            return savables;
        }
    }
}
