using StarbotLib.Logging;
using StarbotLib.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarbotLib.World
{
    [Serializable]
    public class Map : MarshalByRefObject, ICloneable
    {
        public string mapID;
        public List<Location> locations;
        public List<Path> warpPaths;
        public int minX;
        public int minY;
        public int maxX;
        public int maxY;

        public Map(string mapID)
        {
            CLogger.Info("MAP: Created new map.");
            this.mapID = mapID;
            locations = new List<Location>();
            warpPaths = new List<Path>();
        }

        public bool IsReady()
        {
            return locations.All(loc => loc.IsReady()) && warpPaths.All(path => path.IsReady());
        }

        public Location GetLocation(int x, int y)
        {
            return locations.FirstOrDefault(loc => loc.x == x && loc.y == y);
        }

        public Location AddLocation(int x, int y)
        {
            if (GetLocation(x, y) == null)
            {
                var loc = new Location(this, x, y);
                locations.Add(loc);
                return loc;
            }
            return null;
        }

        public List<Location> GetLocations()
        {
            return locations.ToList();
        }

        public List<Location> GetWarps()
        {
            return locations.Where(loc => loc.type == Location.Type.Warp).ToList();
        }

        public List<Location> GetDoors()
        {
            return locations.Where(loc => loc.type == Location.Type.Door).ToList();
        }

        public void UpdateBounds()
        {
            if (!locations.Any())
            {
                minX = 0;
                minY = 0;
                maxX = 0;
                maxY = 0;
                return;
            }
            minX = locations.Min(loc => loc.x);
            minY = locations.Min(loc => loc.y);
            maxX = locations.Max(loc => loc.x);
            maxY = locations.Max(loc => loc.y);
        }

        public void ClearPassableCache()
        {
            foreach (var loc in locations)
            {
                loc.ClearPassableCache();
            }
        }

        public override bool Equals(object obj)
        {
            return this.mapID.Equals(((Map)obj).mapID);
        }

        public override string ToString()
        {
            return mapID;
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        public SavableMap GetSavable()
        {
            var savable = new SavableMap()
            {
                mapID = mapID,
                minX = minX,
                minY = minY,
                maxX = maxX,
                maxY = maxY
            };
            foreach (var loc in locations)
            {
                savable.locations.Add(loc.GetSavable());
            }
            return savable;
        }
    }
}
