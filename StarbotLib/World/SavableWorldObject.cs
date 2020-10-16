using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarbotLib.World
{
    [Serializable]
    public class SavableWorldObject
    {
        public bool passable;
        public string name;
        public string displayName;
        public string description;
        public string category;
        public bool actionable;
    }
}
