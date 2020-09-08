using Microsoft.Xna.Framework;
using Starbot.Logging;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starbot.Objectives
{
    public class ObjectiveClearDebris : Objective {
        public override string announceMessage => "Clear debris from the " + targetMap;
        public override string uniquePoolId => "cleardebris." + targetMap;
        public override bool cooperative => true;
        string targetMap;

        public struct DebrisSpot {
            public int x;
            public int y;
            public string type;
        }
        public List<DebrisSpot> DebrisSpots = new List<DebrisSpot>();

        bool hasScanned = false;

        bool WasRoutingToDebris = false;

        bool DebrisRoutingComplete = false;

        public ObjectiveClearDebris(string map)
        {
            IsComplete = false;
            this.targetMap = map;
        }

        public override void Reset()
        {
            base.Reset();
            hasScanned = false;
            WasRoutingToDebris = false;
            DebrisRoutingComplete = false;
            DebrisSpots.Clear();
            IsComplete = false;
        }

        List<DebrisSpot> SortByDistance(List<DebrisSpot> debrisList)
        {
            var playerX = Game1.player.getTileX();
            var playerY = Game1.player.getTileY();
            debrisList.Sort((debrisA, debrisB) => 
                (Math.Sqrt(Math.Pow(playerX - debrisA.x, 2) + Math.Pow(playerY - debrisA.y, 2)))
                .CompareTo((Math.Sqrt(Math.Pow(playerX - debrisB.x, 2) + Math.Pow(playerY - debrisB.y, 2))))
            );
            return debrisList;
        }

        public override void Step()
        {
            base.Step();
            //short circuit here if energy is low
            if (Game1.player.Stamina <= 5)
            {
                Mod.i.core.FailObjective();
                Logger.Trace("Cancelling task, not enough stamina!");
                return;
            }

            //step one: are we on the target map?
            if (Game1.player.currentLocation.NameOrUniqueName != targetMap)
            {
                if (!Mod.i.core.IsRouting)
                {
                    int tx = -3, ty = -3;
                    Utility.getDefaultWarpLocation(targetMap, ref tx, ref ty);
                    Mod.i.core.RouteTo(targetMap, false, tx, ty, true);
                }
                return;
            }

            //step two: scan for forages on the map
            if (!hasScanned)
            {
                Logger.Warn("Scanning for debris...");
                var ojs = Game1.currentLocation.objects;
                List<Vector2> vkeys = ojs.Keys.ToList();
                vkeys.Shuffle();
                foreach (var o in vkeys)
                {
                    var debrisName = ojs[o].Name;
                    if (debrisName.Contains("Weed")) {
                        DebrisSpots.Add(new DebrisSpot() { 
                            x = (int)o.X, 
                            y = (int)o.Y,
                            type = "Weed" 
                        });
                    } 
                    else if(debrisName == "Stone") {
                        DebrisSpots.Add(new DebrisSpot() {
                            x = (int)o.X,
                            y = (int)o.Y,
                            type = "Stone"
                        });
                    }
                    else if (debrisName.Contains("Twig")) {
                        DebrisSpots.Add(new DebrisSpot() {
                            x = (int)o.X,
                            y = (int)o.Y,
                            type = "Twig"
                        });
                    }
                }
                Logger.Trace("found " + DebrisSpots.Count(spot => spot.type == "Weed") + " weeds");
                Logger.Trace("found " + DebrisSpots.Count(spot => spot.type == "Stone") + " rocks");
                Logger.Trace("found " + DebrisSpots.Count(spot => spot.type == "Twig") + " twigs");
                DebrisSpots = SortByDistance(DebrisSpots);
                hasScanned = true;
                return;
            }

            //step three: is there any debris? if not, we're complete
            if (DebrisSpots.Count == 0)
            {
                IsComplete = true;
                return;
            }

            //check weeds
            //discard nonexistant ones that someone else dealt with
            while (DebrisSpots.Count > 0 && !Game1.currentLocation.objects.ContainsKey(new Vector2(DebrisSpots[0].x, DebrisSpots[0].y)))
                DebrisSpots.RemoveAt(0);
            if (DebrisSpots.Count > 0)
            {
                //TODO: Add back
                int pathingCutoff = (int)((150f / DebrisSpots.Count) * 150f);
                var spot = DebrisSpots[0];
                var tool = "Axe";
                if (spot.type == "Stone") {
                    tool = "Pickaxe";
                }
                if (Mod.i.core.EquipToolIfOnHotbar(tool, false)) {

                    if (!WasRoutingToDebris && !DebrisRoutingComplete) {
                        WasRoutingToDebris = true;
                        if (!Mod.i.core.RouteTo(targetMap, true, spot.x, spot.y, false, pathingCutoff)) {
                            //we can't reach this one. remove it from the list
                            DebrisSpots.RemoveAt(0);
                            Logger.Trace("Can't route to " + targetMap + ", " + spot.x + "," + spot.y + " " + spot.type + ". " + DebrisSpots.Count() + " remaining.");
                            WasRoutingToDebris = false;
                        }
                        if (WasRoutingToDebris) {
                            WasRoutingToDebris = false;
                            DebrisRoutingComplete = true;
                        }
                        return;
                    }
                    if (DebrisRoutingComplete) {
                        //step five: face the forage
                        bool gotcha = (int)(Game1.player.GetToolLocation().X / 64f) == spot.x && (int)(Game1.player.GetToolLocation().Y / 64f) == spot.y;
                        if (!gotcha) //we're in the wrong spot. try to face
                        {
                            int px = Game1.player.getTileX();
                            int py = Game1.player.getTileY();

                            Mod.i.core.FaceTile(spot.x, spot.y);

                            WasRoutingToDebris = false;
                            return;
                        }

                        //pick
                        Mod.i.core.EquipToolIfOnHotbar(tool);
                        Mod.i.core.FaceTile(spot.x, spot.y);
                        Mod.i.core.SwingTool();
                        Logger.Warn("HIT!");
                        DebrisSpots.RemoveAt(0);
                        DebrisRoutingComplete = false;
                        // Sort the list again
                        DebrisSpots = SortByDistance(DebrisSpots);
                    }
                    return;
                }
                else {
                    DebrisSpots.RemoveAt(0);
                    Logger.Trace("Don't have tool for " + spot.type + ", removing this debris. " + DebrisSpots.Count() + " remaining.");
                }
            }
        }

        public override void CantMoveUpdate()
        {
            base.CantMoveUpdate();
        }
    }

}
