using Starbot.Logging;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starbot
{

    public static class Routing
    {
        public static bool Ready = false;
        private static Dictionary<string, HashSet<string>> MapConnections = new Dictionary<string, HashSet<string>>();

        public static void Reset()
        {
            Ready = false;
            if (Game1.IsMultiplayer && !Game1.IsMasterGame)
            {
                //client mode
                MapConnections.Clear();
                Logger.Info("Starbot is now in multiplayer client mode.");
                Logger.Info("The server will need to have Starbot installed to proceed.");
                Logger.Info("Awaiting response from server...");
                Mod.i.Helper.Multiplayer.SendMessage<int>(0, "authRequest");
            } else
            {
                //host/singleplayer mode
                MapConnections = BuildRouteCache();
                Ready = true;
            }
        }

        public static Dictionary<string, HashSet<string>> BuildRouteCache()
        {
            var routeDictionary = new Dictionary<string, HashSet<string>>();
            foreach (var map in Game1.locations)
            {
                string key = map.NameOrUniqueName;
                if (!string.IsNullOrWhiteSpace(key))// && !gl.isTemp())
                {
                    if (map.warps != null && map.warps.Count > 0)
                    {
                        Logger.Alert("Learning about " + key);
                        routeDictionary[key] = new HashSet<string>();
                        foreach (var w in map.warps) routeDictionary[key].Add(w.);
                        foreach (var d in map.doors.Values) routeDictionary[key].Add(d);
                        foreach (var s in routeDictionary[key]) Logger.Warn("It connects to " + s);
                    }
                }
                if(map is StardewValley.Locations.BuildableGameLocation)
                {
                    StardewValley.Locations.BuildableGameLocation bl = map as StardewValley.Locations.BuildableGameLocation;
                    foreach(var b in bl.buildings)
                    {
                        if (b.indoors != null && b.indoors.Value != null) {
                            var indoorsKey = b.indoors.Value.NameOrUniqueName;
                            if (!routeDictionary.ContainsKey(key)) {
                                routeDictionary[key] = new HashSet<string>();
                            }
                            routeDictionary[key].Add(indoorsKey);
                            //add the way in
                            Logger.Alert("Learning about " + indoorsKey);
                            routeDictionary[indoorsKey] = new HashSet<string>();
                            //add the way out
                            foreach (var s in routeDictionary[indoorsKey]) {
                                Logger.Warn("It connects to " + s);
                            }
                            routeDictionary[indoorsKey].Add(key);
                        }
                    }
                }
            }
            return routeDictionary;
        }

        public static void Multiplayer_ModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (Game1.IsMasterGame && e.Type == "authRequest")
            {
                Logger.Trace("Starbot authorization requested by client. Approving...");
                //listen for authorization requests
                Dictionary<string, HashSet<string>> response = null;
                if (MapConnections.Count > 0)
                {
                    //host bot is active, use existing cache
                    response = MapConnections;
                } else
                {
                    response = BuildRouteCache();
                }
                Mod.i.Helper.Multiplayer.SendMessage<Dictionary<string, HashSet<string>>>(response, "authResponse");
            } 
            else if(!Game1.IsMasterGame && e.Type == "authResponse")
            {
                //listen for authorization responses
                MapConnections = e.ReadAs<Dictionary<string, HashSet<string>>>();
                Logger.Trace("Starbot authorization request was approved by server.");
                Logger.Trace("Server offered routing data for " + MapConnections.Count + " locations.");
                Ready = true;
            } 
            else if(e.Type == "taskAssigned")
            {
                string task = e.ReadAs<string>();
                Logger.Trace("Another player has taken task: " + task);
                Mod.i.core.ObjectivePool.RemoveAll(x => x.uniquePoolId == task);
            }
        }

        public static List<string> GetRoute(string destination)
        {
            return GetRoute(Game1.player.currentLocation.NameOrUniqueName, destination);
        }

        public static List<string> GetRoute(string start, string destination)
        {
            var result = SearchRoute(start, destination);
            if (result != null) result.Add(destination);
            return result;
        }

        private static List<string> SearchRoute(string step, string target, List<string> route = null, List<string> blacklist = null)
        {
            if (route == null) route = new List<string>();
            if (blacklist == null) blacklist = new List<string>();
            List<string> route2 = new List<string>(route);
            route2.Add(step);
            // Do a breadth-first search
            foreach (string s in MapConnections[step]) {
                if (route.Contains(s) || blacklist.Contains(s))
                    continue;
                if (s == target) {
                    return route2;
                }
            }
            foreach (string s in MapConnections[step]) {
                if (route.Contains(s) || blacklist.Contains(s))
                    continue;
                if (s == target) {
                    return route2;
                }
                List<string> result = SearchRoute(s, target, route2, blacklist);
                if (result != null)
                    return result;
            }
            blacklist.Add(step);
            return null;
        }
    }
}
