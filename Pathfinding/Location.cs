using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starbot.Pathfinding {
    public class Location {

        //Defining characteristics
        public GameLocation map;
        public int x;
        public int y;
        public string hardType;
        public string containsType;

        //Pathing specifics
        public int f;
        public int g;
        public int h;
        public Location parent;
        public bool preferable = false;

        public Location() {

        }

        public Location(int x, int y) {
            this.x = x;
            this.y = y;
        }
    }
}
