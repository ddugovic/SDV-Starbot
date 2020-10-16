using StarbotLib.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarbotLib.World
{
    [Serializable]
    public class Location : MarshalByRefObject
    {
        public enum Type
        {
            Unknown,
            Open,
            Obstacle,
            Door,
            Warp
        }

        //Defining characteristics
        public Map map;
        public int x;
        public int y;
        public Type type;
        public Location warpTarget = null;
        public Location warpOrigin = null;

        //Object in location
        public WorldObject worldObject = null;

        //State
        private bool passable;
        private bool passableCached;

        public Location(Map map, int x, int y)
        {
            if (map == null)
            {
                throw new ArgumentNullException("map cannot be null");
            }
            this.map = map;
            this.x = x;
            this.y = y;
            type = Type.Unknown;
        }

        public bool IsReady()
        {
            return passableCached;
        }

        public WorldObject AddWorldObject(string name, string displayName, string description, string category, bool passable, bool actionable)
        {
            worldObject = new WorldObject()
            {
                location = this,
                name = name,
                displayName = displayName,
                description = description,
                category = category,
                passable = passable,
                actionable = actionable
            };
            return worldObject;
        }

        public string GetMapID()
        {
            return map.mapID;
        }

        public List<Location> Area(int distance)
        {
            if (distance <= 0)
            {
                var ret = new List<Location>();
                ret.Add(this);
                return ret;
            }
            return map.locations.Where(loc => loc.x >= x - distance &&
                                              loc.x <= x + distance &&
                                              loc.y >= y - distance &&
                                              loc.y <= y + distance).ToList();
        }

        public bool IsPassableCached()
        {
            CLogger.Info(this + " passable cached = " + passableCached);
            return passableCached;
        }

        public bool IsPassable()
        {
            if (type == Type.Warp)
            {
                return true;
            }
            return passable;
        }

        public void SetPassable(bool passable)
        {
            this.passable = passable;
            passableCached = true;
        }

        public void ClearPassableCache()
        {
            passableCached = false;
        }

        public List<Location> PassableAdjacent()
        {
            var adjacent = Adjacent();
            return adjacent.Where(loc => loc.IsPassable()).ToList();
        }

        public List<Location> Adjacent()
        {
            List<Location> locs = new List<Location>();
            var loc = Up();
            if (loc != null)
            {
                locs.Add(loc);
            }
            loc = Left();
            if (loc != null)
            {
                locs.Add(loc);
            }
            loc = Right();
            if (loc != null)
            {
                locs.Add(loc);
            }
            loc = Down();
            if (loc != null)
            {
                locs.Add(loc);
            }
            return locs;
        }

        public Location Up()
        {
            return map.GetLocation(x, y - 1);
        }

        public Location Left()
        {
            return map.GetLocation(x - 1, y);
        }

        public Location Down()
        {
            return map.GetLocation(x, y + 1);
        }

        public Location Right()
        {
            return map.GetLocation(x + 1, y);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Location))
            {
                return false;
            }
            var loc = (Location)obj;
            return loc.map == map &&
                   loc.x == x &&
                   loc.y == y &&
                   loc.type == type;
        }

        public override string ToString()
        {
            var t = (type == Type.Door || type == Type.Warp) ? ("-" + type) : "";
            return map + "-(" + x + ", " + y + ")" + t;
        }

        public SavableLocation GetSavable()
        {
            var savable = new SavableLocation()
            {
                mapID = map.mapID,
                x = x,
                y = y,
                type = type,
                passable = passable,
                passableCached = passableCached
            };
            if (warpTarget != null)
            {
                savable.warpTarget = warpTarget.GetSavable();
            }
            if (warpOrigin != null)
            {
                savable.warpOrigin = warpOrigin.GetSavable();
            }
            if (worldObject != null)
            {
                savable.worldObject = worldObject.GetSavable();
            }
            return savable;
        }
    }
}
