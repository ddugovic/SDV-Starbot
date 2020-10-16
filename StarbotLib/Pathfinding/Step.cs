using StarbotLib.World;
using System;

namespace StarbotLib.Pathfinding
{
    [Serializable]
    public class Step : MarshalByRefObject
    {
        public Location loc;
        public int f;
        public int g;
        public int h;
        public Step parent;
        public bool preferable = false;

        public Step(Location loc)
        {
            this.loc = loc;
        }
    }
}
