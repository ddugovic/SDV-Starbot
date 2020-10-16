using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarbotLib.World
{
    [Serializable]
    public class SavableMap
    {
        public string mapID;
        public List<SavableLocation> locations;
        public int minX;
        public int minY;
        public int maxX;
        public int maxY;

        public SavableMap()
        {
            locations = new List<SavableLocation>();
        }
    }
}
