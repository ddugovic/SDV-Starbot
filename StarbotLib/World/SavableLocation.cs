using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarbotLib.World
{
    [Serializable]
    public class SavableLocation
    {
        public string mapID;
        public int x;
        public int y;
        public Location.Type type;
        public SavableLocation warpTarget;
        public SavableLocation warpOrigin;
        public SavableWorldObject worldObject;
        public bool passable;
        public bool passableCached;
    }
}
