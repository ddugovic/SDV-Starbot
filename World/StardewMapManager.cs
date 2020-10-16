using Microsoft.Xna.Framework;
using Starbot.Logging;
using StarbotLib.World;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using xTile.ObjectModel;
using xTile.Tiles;

namespace Starbot.World
{
    class StardewMapManager : Manager
    {
        public void BuildMapCache()
        {
            var maps = Mod.i.server.maps;

            SLogger.Alert("BUILDING MAP CACHE");
            var gameMaps = Game1.locations.ToList();
            var player = Game1.player;

            // Firstly add any additional indoor maps to the map list so they can be processed
            foreach (var gameMap in gameMaps.ToList())
            {
                if (gameMap is StardewValley.Locations.BuildableGameLocation)
                {
                    StardewValley.Locations.BuildableGameLocation bl = gameMap as StardewValley.Locations.BuildableGameLocation;
                    foreach (var building in bl.buildings.Where(iBuilding => iBuilding.indoors != null && iBuilding.indoors.Value != null))
                    {
                        gameMaps.Add(building.indoors.Value);
                    }
                }
            }
            // For testing purposes, only take the first 2 maps in the list, the farmhouse and farm
            //gameMaps = gameMaps.Take(2).ToList();

            // Go through once and add the maps to our cache along with their locations/dimensions
            foreach (var gameMap in gameMaps)
            {
                var mapID = gameMap.NameOrUniqueName;

                // Only add maps that have valid names
                if (!string.IsNullOrWhiteSpace(mapID))
                {
                    Map map = maps.GetMap(mapID);
                    if (map == null)
                    {
                        map = maps.AddMap(mapID);
                        SLogger.Alert(map + " scanning...");
                        // Loop over locations until we search all coordinates from -2,-2 through maxX+2,maxY+2
                        int maxX = 1;
                        int maxY = 1;
                        for (int x = -2; x <= maxX; x++)
                        {
                            for (int y = -2; y <= maxY; y++)
                            {
                                if (gameMap.isTileOnMap(new Vector2(x, y)))
                                {
                                    maxX = x + 2;
                                    maxY = y + 2;
                                    var loc = map.GetLocation(x, y);
                                    if (loc == null)
                                    {
                                        loc = map.AddLocation(x, y);
                                    }
                                    // Refresh, does object/passable calculations
                                    //var locNow = DateTime.Now;
                                    RefreshArea(loc, 0);
                                    //SLogger.Alert("location took " + ((int)(DateTime.Now - locNow).TotalMilliseconds) + "ms.");

                                }
                            }
                        }
                        map.UpdateBounds();
                        SLogger.Alert(map + " bounds: (" + map.minX + ", " + map.minY + ") -> (" + map.maxX + ", " + map.maxY + ").");
                        SLogger.Info("Total Locations: " + maps.TotalLocations());
                        // Call for a save.
                        Mod.i.server.Save();
                    }
                    else
                    {
                        SLogger.Alert(map + " already loaded. Refreshing...");
                        foreach (var loc in map.GetLocations())
                        {
                            RefreshArea(loc, 0);
                        }
                    }
                    
                }
                else
                {
                    // Not valid, remove it from the list
                    gameMaps.Remove(gameMap);
                }
            }

            // Then populate their locations with the appropriate info
            foreach (var gameMap in gameMaps)
            {
                Map map = maps.GetMap(gameMap.NameOrUniqueName);

                GatherWarps(map, gameMap);

                GatherDoors(map, gameMap);
            }
        }

        private void GatherWarps(Map map, GameLocation gameMap)
        {
            var maps = Mod.i.server.maps;

            // Gather all warps on the map
            foreach (var w in gameMap.warps)
            {
                Map warpMap = maps.GetMap(w.TargetName);
                if (warpMap == null)
                {
                    SLogger.Alert("Tried to learn about warp " + map + "->" + w.TargetName + " but couldn't find a valid target map for it.");
                    continue;
                }

                var startingLocation = map.GetLocation(w.X, w.Y);
                // If the outbound warp isn't present on the map (usually because it's outside the bounds), add it.
                if (startingLocation == null)
                {
                    startingLocation = map.AddLocation(w.X, w.Y);
                }

                var targetLocation = warpMap.GetLocation(w.TargetX, w.TargetY);
                // If the inbound warp isn't present on the target map (this shouldn't really happen, it should be walkable), add it.
                if (targetLocation == null)
                {
                    targetLocation = warpMap.AddLocation(w.TargetX, w.TargetY);
                }

                startingLocation.type = Location.Type.Warp;
                startingLocation.warpTarget = targetLocation;
                targetLocation.warpOrigin = startingLocation;

                SLogger.Alert("Learned WARP " + startingLocation + " --> " + targetLocation);
            }
        }

        private void GatherDoors(Map map, GameLocation gameMap)
        {
            // Gather all doors on the map. All this does is change the location type to a door so we know it must be opened.
            foreach (var doorKey in gameMap.doors.Keys.ToList())
            {
                var doorLocation = map.GetLocation(doorKey.X, doorKey.Y);
                if (doorLocation == null)
                {
                    SLogger.Alert("Door on map " + map + " was at location (" + doorKey.X + ", " + doorKey.Y + ") but that location isn't on the map.");
                    continue;
                }

                // We should already have a warp loaded for this door, confirm it's the same
                if (doorLocation.warpTarget == null)
                {
                    SLogger.Alert("Expected door on map " + map + " at location (" + doorKey.X + ", " + doorKey.Y + ") to have a warp, but it didn't.");
                    continue;
                }

                if (doorLocation.warpTarget.map.mapID != gameMap.doors[doorKey])
                {
                    SLogger.Alert("Door on map " + map + " at location (" + doorKey.X + ", " + doorKey.Y + ") had mismatched target map with its associated warp.");
                    continue;
                }

                doorLocation.type = Location.Type.Door;
                SLogger.Alert("Learned DOOR on map " + map + " at location (" + doorKey.X + ", " + doorKey.Y + ").");
            }
        }

        public void RefreshMap(Map map)
        {
            RefreshLocations(GetGameMap(map), map.GetLocations());
        }

        public void RefreshArea(Location location, int distance)
        {
            List<Location> locs;
            if (distance <= 0)
            {
                locs = new List<Location>();
                locs.Add(location);
            }
            else
            {
                locs = location.Area(distance);
            }
            RefreshLocations(GetGameMap(location.map), locs);
        }

        private void RefreshLocations(GameLocation gameMap, List<Location> locations)
        {
            foreach (var loc in locations)
            {
                loc.worldObject = null;
                var locX = loc.x;
                var locY = loc.y;
                try
                {
                    foreach (var objectVector in gameMap.objects.Keys.Where(v => v.X == locX && v.Y == locY))
                    {
                        SetWorldObject(loc, gameMap.objects[objectVector]);
                    }
                }
                catch (NullReferenceException e) { return; }
                Mod.i.maps.CalculatePassable(loc, locX, locY);
            }
        }

        private void SetWorldObject(Location location, StardewValley.Object obj)
        {
            location.AddWorldObject(obj.Name, obj.DisplayName, obj.getDescription(), obj.getCategoryName(), obj.isPassable(), obj.isActionable(Game1.player));
        }

        private GameLocation GetGameMap(Map map)
        {
            if (map == null || string.IsNullOrEmpty(map.mapID))
            {
                throw new ArgumentException("Map was invalid when trying to get game map.");
            }
            return Game1.locations.FirstOrDefault(gMap => gMap.NameOrUniqueName.Equals(map.mapID));
        }

        public void RefreshPlayerLocation()
        {
            try
            {
                if (Game1.player == null || Game1.player.currentLocation == null || Game1.player.Position == null)
                {
                    return;
                }
                var map = Mod.i.server.maps.GetMap(Game1.player.currentLocation.NameOrUniqueName);
                if (map == null)
                {
                    throw new Exception("Player map " + Game1.player.currentLocation.NameOrUniqueName + " not found in map cache.");
                }
                var x = Game1.player.getTileX();
                var y = Game1.player.getTileY();
                Mod.i.player.location = map.GetLocation(x, y);
                if (Mod.i.player.location == null)
                {
                    throw new Exception("Player location (" + x + ", " + y + ") not found in map " + map.ToString() + ".");
                }
                Mod.i.player.tileX = Game1.player.Position.X / (float)Game1.tileSize;
                Mod.i.player.tileY = Game1.player.Position.Y / (float)Game1.tileSize;
            }
            catch (Exception) { }
        }

        public bool CalculatePassable(Location loc, int x, int y)
        {
            var mapID = loc.GetMapID();
            var map = Game1.locations.FirstOrDefault(aMap => aMap.NameOrUniqueName == mapID);

            // Warps are always passable
            foreach (var w in map.warps)
            {
                if (w.X == x && w.Y == y)
                {
                    return true;
                }
            }

            var v = new Vector2(x, y);
            //TODO: Maybe use this for passable?
            //map.getObjectAtTile(0, 0).isPassable();
            bool isOnMap = map.isTileOnMap(v);
            bool isOccupied = map.isTileOccupiedIgnoreFloors(v, "");
            var passableLocation = new xTile.Dimensions.Location(x, y);
            bool isPassable = isTilePassable(map, passableLocation, Game1.viewport);
            if (!isPassable)
            {
                if (map.NameOrUniqueName == "Farm" &&
                    // Manual fix for SVE Immersive Farm 2 - Ladder below cave not marked as passable when it should be
                    (x == 141 && (y == 101 || y == 102 || y == 103 || y == 104)) &&
                    // Manual fix for SVE Immersive Farm 2 - Dock in lower left of map not marked as passable when it should be
                    ((x == 12 || x == 13 || x == 14) && y == 48) &&
                    // Manual fix for SVE Immersive Farm 2 - Dock after warp in lower left of map not marked as passable when it should be
                    ((x == 9 || x == 10 || x == 11) && y == 145))
                {
                    isPassable = true;
                }
            }
            //check for bigresourceclumps on the farm
            if (map is Farm)
            {
                foreach (var brc in map.largeTerrainFeatures)
                {
                    var r = brc.getBoundingBox();
                    var xx = x;
                    var yy = y;
                    if (xx > r.X && xx < r.X + r.Width && yy > r.Y && yy < r.Y + r.Height)
                        return false;
                }
            }
            if (map is StardewValley.Locations.BuildableGameLocation)
            {
                var bgl = map as StardewValley.Locations.BuildableGameLocation;
                foreach (var b in bgl.buildings)
                {
                    if (!b.isTilePassable(v))
                        return false;
                }
            }
            if (map is StardewValley.Locations.BuildableGameLocation || map is Farm)
            {
                //more aggressive test. doesn't like floors
                if (map.isCollidingPosition(new Rectangle((x * 64) + 2, (y * 64) + 2, 60, 60), Game1.viewport, true, 0, false, null, false, false, true))
                    return false;
            }
            var retPassable = isOnMap && !isOccupied && isPassable;
            loc.SetPassable(retPassable);
            return retPassable;
        }

        private bool isTilePassable(GameLocation map, xTile.Dimensions.Location tileLocation, xTile.Dimensions.Rectangle viewport)
        {
            PropertyValue passable = null;
            Tile tmp = map.map.GetLayer("Back").PickTile(new xTile.Dimensions.Location(tileLocation.X * 64, tileLocation.Y * 64), viewport.Size);
            tmp?.TileIndexProperties.TryGetValue("Passable", out passable);
            Tile tile = map.map.GetLayer("Buildings").PickTile(new xTile.Dimensions.Location(tileLocation.X * 64, tileLocation.Y * 64), viewport.Size);
            if (passable == null && tile == null)
            {
                return tmp != null;
            }
            return false;
        }

        private bool isPointPassable(GameLocation map, xTile.Dimensions.Location location, xTile.Dimensions.Rectangle viewport)
        {
            PropertyValue passable = null;
            PropertyValue shadow = null;
            map.map.GetLayer("Back").PickTile(new xTile.Dimensions.Location(location.X, location.Y), viewport.Size)?.TileIndexProperties.TryGetValue("Passable", out passable);
            Tile tile = map.map.GetLayer("Buildings").PickTile(new xTile.Dimensions.Location(location.X, location.Y), viewport.Size);
            tile?.TileIndexProperties.TryGetValue("Shadow", out shadow);
            if (passable == null)
            {
                if (tile != null)
                {
                    return shadow != null;
                }
                return true;
            }
            return false;
        }

        public override void UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
        }

        public override void Rendered(object sender, RenderedEventArgs e)
        {
        }

        public override void SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            var maps = Mod.i.server.maps;
            //Constants.SaveFolderName
            maps.Reset();

            Mod.i.threads.StartWatchdog(new Thread(new ThreadStart(delegate
            {
                Thread.CurrentThread.Name = "MapCacheBuilder";
                while (!Context.IsWorldReady)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(0.1));
                }
                maps.status = MapManager.Status.Setup;
                BuildMapCache();
                maps.status = MapManager.Status.Ready;
                Mod.i.threads.StopWatchdog();
            })));
        }

        public double PlayerDistance(Location loc2)
        {
            return Distance(Mod.i.player.location, loc2);
        }

        public double Distance(Location loc1, Location loc2)
        {
            return Math.Sqrt(Math.Pow(loc2.x - loc1.x, 2) + Math.Pow(loc2.y - loc1.y, 2));
        }
    }
}
