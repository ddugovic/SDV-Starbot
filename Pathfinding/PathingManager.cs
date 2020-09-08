using Microsoft.Xna.Framework;
using Starbot.Logging;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Starbot.Pathfinding
{
    public class PathingManager
    {
        private Dictionary<string, Path> successfulPaths = new Dictionary<string, Path>();
        private Thread masterPathingThread;
        private List<Thread> pathThreads;
        private Queue<Path> pathRequests; 

        public PathingManager() {
            pathThreads = new List<Thread>();
            pathRequests = new Queue<Path>();
            RunPathfinder();
        }
        
        private void RunPathfinder() {

            // Create a master pathing thread that posts our location every 10 seconds
            masterPathingThread = new Thread(new ThreadStart(delegate {
                Thread.CurrentThread.Name = "MasterPathingThread";
                Thread.Sleep(TimeSpan.FromSeconds(1));
                DateTime starting = DateTime.Now;
                Logger.Alert("Started new " + Thread.CurrentThread.Name);
                while (true) {
                    try {
                        if (Game1.player != null && Game1.player.currentLocation != null && !String.IsNullOrEmpty(Game1.player.currentLocation.NameOrUniqueName) && Game1.player.Position != null) {
                            string pl = Game1.player.currentLocation.NameOrUniqueName;
                            float px = Game1.player.Position.X;
                            float py = Game1.player.Position.Y;
                            Logger.Info("Player location: " + pl + " (" + Math.Round(px / (float)Game1.tileSize, 3) + ", " + Math.Round(py / (float)Game1.tileSize, 3) + ")");
                        }
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                    }
                    catch (Exception e) {
                        Logger.Error("Error in " + Thread.CurrentThread.Name, e);
                    }
                }
                //Mod.instance.Threading.StopWatchdog();
            }));
            Mod.i.Threading.StartWatchdog(masterPathingThread);
            // Create 4 path calculation threads
            for (int threadID = 1; threadID <= 4; threadID++) {
                var pathThread = new Thread(new ThreadStart(delegate {
                    Thread.CurrentThread.Name = "PathCalculationThread-" + threadID;
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    DateTime starting = DateTime.Now;
                    Logger.Alert("Started new " + Thread.CurrentThread.Name);
                    while (true) {
                        try {
                            Path inputPath = null;
                            lock (pathRequests) {
                                if (pathRequests.Any()) {
                                    inputPath = pathRequests.Dequeue();
                                }
                            }
                            if (inputPath != null) {
                                CalculatePath(inputPath);
                                if (inputPath.status == Path.Status.Successful) {
                                    successfulPaths.Add(inputPath.ToString(), inputPath);
                                }
                            }
                            else {
                                // Could use a mutex, but for now i'll just have a 250ms wait when the request queue is empty
                                Thread.Sleep(TimeSpan.FromSeconds(0.25));
                            }
                        }
                        catch (Exception e) {
                            Logger.Error("Error in " + Thread.CurrentThread.Name, e);
                        }
                    }
                    //Mod.instance.Threading.StopWatchdog();
                }));
                Mod.i.Threading.StartWatchdog(pathThread);
            }
        }

        public Path GeneratePathObject(GameLocation map, int startX, int startY, int targetX, int targetY, bool pathUntilTarget, int cutoff = -1) {
            var start = new Location() {
                map = map,
                x = startX,
                y = startY,
            };
            var target = new Location() {
                map = map,
                x = targetX,
                y = targetY,
            };
            return GeneratePathObject(start, target, pathUntilTarget, cutoff);
        }

        public Path GeneratePathObject(Location start, Location target, bool pathUntilTarget, int cutoff = -1) {
            return new Path() {
                start = start,
                target = target,
                pathUntilTarget = pathUntilTarget,
                cutoff = cutoff
            };
        }

        public void CalculatePathAsync(Path path) {
            if (path == null) {
                Logger.Alert("Path was null in CalculatePathAsync.");
                return;
            }
            lock (pathRequests) {
                path.status = Path.Status.Waiting;
                pathRequests.Enqueue(path);
            }
        }
        
        public Path CalculatePath(Path path) {
            if (path == null) {
                Logger.Alert("Path was null in CalculatePath.");
                return null;
            }
            if (path.start == null) {
                Logger.Alert("Path START was null in CalculatePath.");
                return null;
            }
            if (path.start.map == null) {
                Logger.Alert("Path START MAP was null in CalculatePath.");
                return null;
            }
            if (path.target == null) {
                Logger.Alert("Path TARGET was null in CalculatePath.");
                return null;
            }
            if (path.target.map == null) {
                Logger.Alert("Path TARGET MAP was null in CalculatePath.");
                return null;
            }
            path.status = Path.Status.Processing;
            Location current = null;
            var openList = new List<Location>();
            var closedList = new List<Location>();
            int g = 0;

            // start by adding the original position to the open list  
            openList.Add(path.start);

            while (openList.Count > 0)
            {
                // get the square with the lowest F score  
                var lowest = openList.Min(l => l.f);
                current = openList.First(l => l.f == lowest);

                // add to closed, remove from open
                closedList.Add(current);
                openList.Remove(current);

                // Do adjacent pathing
                if (path.pathUntilTarget && CheckAdjacentTarget(current.x, current.y, path.target)) {
                    // Target is adjacent to us so we're finished.
                    break;
                }
                // if closed contains destination, we're done
                if (closedList.FirstOrDefault(l => l.x == path.target.x && l.y == path.target.y) != null) {
                    break;
                }

                // if closed has exceed cutoff, break out and fail
                if (path.cutoff > 0 && closedList.Count > path.cutoff)
                {
                    //Logger.Log("Breaking out of pathfinding, cutoff exceeded");
                    return null;
                }

                var adjacentSquares = GetWalkableAdjacentSquares(current.x, current.y, path.start.map, openList);
                g = current.g + 1;

                foreach (var adjacentSquare in adjacentSquares)
                {
                    // if closed, ignore 
                    if (closedList.FirstOrDefault(l => l.x == adjacentSquare.x
                        && l.y == adjacentSquare.y) != null)
                        continue;

                    // if it's not in open
                    if (openList.FirstOrDefault(l => l.x == adjacentSquare.x
                        && l.y == adjacentSquare.y) == null)
                    {
                        // compute score, set parent  
                        adjacentSquare.g = g;
                        adjacentSquare.h = ComputeHScore(adjacentSquare.preferable, adjacentSquare.x, adjacentSquare.y, path.target.x, path.target.y);
                        adjacentSquare.f = adjacentSquare.g + adjacentSquare.h;
                        adjacentSquare.parent = current;

                        // and add it to open
                        openList.Insert(0, adjacentSquare);
                    }
                    else
                    {
                        // test if using the current G score makes the adjacent square's F score lower
                        // if yes update the parent because it means it's a better path  
                        if (g + adjacentSquare.h < adjacentSquare.f)
                        {
                            adjacentSquare.g = g;
                            adjacentSquare.f = adjacentSquare.g + adjacentSquare.h;
                            adjacentSquare.parent = current;
                        }
                    }
                }
            }

            //make sure path is complete
            if (current == null) return null;
            if ((!path.pathUntilTarget || !CheckAdjacentTarget(current.x, current.y, path.target)) &&
                (current.x != path.target.x || current.y != path.target.y)) 
            {
                Logger.Warn("No path available.");
                return null;
            }

            // if path exists, let's pack it up for return
            while (current != null)
            {
                path.steps.Add(new Location(current.x, current.y));
                current = current.parent;
            }
            path.steps.Reverse();
            return path;
        }

        bool CheckAdjacentTarget(int x, int y, Location target) {
            if (target != null &&
                ((x == target.x && y - 1 == target.y) ||  // Top
                 (x == target.x && y + 1 == target.y) ||  // Bottom
                 (x - 1 == target.x && y == target.y) ||  // Left
                 (x + 1 == target.x && y == target.y))) { // Right
                return true;
            }
            return false;
        }

        List<Location> GetWalkableAdjacentSquares(int x, int y, GameLocation map, List<Location> openList)
        {
            List<Location> list = new List<Location>();

            if (IsPassable(map, x, y - 1))
            {
                Location node = openList.Find(l => l.x == x && l.y == y - 1);
                if (node == null) list.Add(new Location() { preferable = IsPreferableWalkingSurface(map, x, y), x = x, y = y - 1 });
                else list.Add(node);
            }

            if (IsPassable(map, x, y + 1))
            {
                Location node = openList.Find(l => l.x == x && l.y == y + 1);
                if (node == null) list.Add(new Location() { preferable = IsPreferableWalkingSurface(map, x, y), x = x, y = y + 1 });
                else list.Add(node);
            }

            if (IsPassable(map, x - 1, y))
            {
                Location node = openList.Find(l => l.x == x - 1 && l.y == y);
                if (node == null) list.Add(new Location() { preferable = IsPreferableWalkingSurface(map, x, y), x = x - 1, y = y });
                else list.Add(node);
            }

            if (IsPassable(map, x + 1, y))
            {
                Location node = openList.Find(l => l.x == x + 1 && l.y == y);
                if (node == null) list.Add(new Location() { preferable = IsPreferableWalkingSurface(map, x, y), x = x + 1, y = y });
                else list.Add(node);
            }

            return list;
        }

        bool IsPreferableWalkingSurface(GameLocation location, int x, int y)
        {
            //todo, make roads more desireable
            return false;
        }

        bool IsPassable(GameLocation map, int x, int y)
        {
            var v = new Vector2(x, y);
            bool isWarp = false;
            foreach(var w in map.warps)
            {
                if (w.X == x && w.Y == y) isWarp = true;
            }
            bool isOnMap = map.isTileOnMap(v);
            bool isOccupied = map.isTileOccupiedIgnoreFloors(v, "");
            bool isPassable = map.isTilePassable(new xTile.Dimensions.Location((int)x, (int)y), Game1.viewport);
            //check for bigresourceclumps on the farm
            if(map is Farm)
            {
                var fff = map as Farm;
                foreach(var brc in fff.largeTerrainFeatures)
                {
                    var r = brc.getBoundingBox();
                    var xx = x;
                    var yy = y;
                    if (xx > r.X && xx < r.X + r.Width && yy > r.Y && yy < r.Y + r.Height) return false;
                }
            }
            if (map is StardewValley.Locations.BuildableGameLocation)
            {
                var bgl = map as StardewValley.Locations.BuildableGameLocation;
                foreach (var b in bgl.buildings)
                {
                    if (!b.isTilePassable(v)) return false;
                }
            }
            if(map is StardewValley.Locations.BuildableGameLocation || map is Farm)
            {
                //more aggressive test. doesn't like floors
                if (map.isCollidingPosition(new Rectangle((x * 64) + 2, (y * 64) + 2, 60, 60), Game1.viewport, true, 0, false, null, false, false, true)) return false;
            }
            return (isWarp || (isOnMap && !isOccupied && isPassable)); //warps must be passable even off-map
        }

        int ComputeHScore(bool preferable, int x, int y, int targetX, int targetY)
        {
            return (Math.Abs(targetX - x) + Math.Abs(targetY - y)) - (preferable ? 1 : 0);
        }
    }
}