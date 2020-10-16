using StarbotLib.Pathfinding;
using StarbotLib.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarbotLib
{
    [Serializable]
    class StarbotServerSave
    {
        public string saveID;
        public List<SavableMap> maps;
        public List<SavablePath> paths;

        public StarbotServerSave(string s, List<SavableMap> m, List<SavablePath> p)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new ArgumentNullException("Save ID required to create save.");
            }
            saveID = s;
            maps = m;
            paths = p;
        }
    }
}
