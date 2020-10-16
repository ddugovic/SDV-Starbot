using Starbot.Logging;
using Starbot.Pathfinding;
using StarbotLib.Pathfinding;
using StarbotLib.World;
using StardewValley;
using System.Collections.Generic;
using System.Linq;

namespace Starbot.Objectives
{
    public class ObjectiveClearDebris : Objective
    {
        public override string announceMessage => "Clear debris from the " + targetMap;
        public override string uniquePoolId => "cleardebris." + targetMap;
        public override bool cooperative => true;
        string targetMap;

        public class DebrisSpot
        {
            public WorldObject worldObject;
            public string tool;
            public Route route;
        }
        public List<DebrisSpot> DebrisSpots = new List<DebrisSpot>();

        bool hasScanned = false;
        HashSet<string> availableTools;

        DebrisSpot currentSpot = null;

        public ObjectiveClearDebris(string map)
        {
            IsComplete = false;
            this.targetMap = map;
            availableTools = new HashSet<string>();
        }

        public override void Reset()
        {
            base.Reset();
            hasScanned = false;
            DebrisSpots.Clear();
            IsComplete = false;
        }

        List<DebrisSpot> SortByDistance(List<DebrisSpot> debrisList)
        {

            debrisList.Sort((debrisA, debrisB) =>
                Mod.i.maps.PlayerDistance(debrisA.worldObject.location)
                .CompareTo(
                Mod.i.maps.PlayerDistance(debrisB.worldObject.location))
            );
            return debrisList;
        }

        public override void Step()
        {
            base.Step();

            // Don't do anything if we're waiting
            if (Mod.i.IsWaiting())
            {
                return;
            }

            //short circuit here if energy is low
            if (Game1.player.Stamina <= 5)
            {
                Mod.i.core.FailObjective();
                SLogger.Trace("Cancelling task, not enough stamina!");
                return;
            }

            //step two: scan for forages on the map
            if (!hasScanned)
            {
                SLogger.Warn("Scanning for debris...");
                var farmMap = Mod.i.server.maps.GetMap("Farm");
                foreach (var location in farmMap.GetLocations().Where(loc => loc.worldObject != null))
                {
                    DebrisSpot spot = null;
                    if (location.worldObject.name.Contains("Weed"))
                    {
                        spot = new DebrisSpot()
                        {
                            worldObject = location.worldObject,
                            tool = "Axe"
                        };
                    }
                    else if (location.worldObject.name == "Stone")
                    {
                        spot = new DebrisSpot()
                        {
                            worldObject = location.worldObject,
                            tool = "Pickaxe"
                        };
                    }
                    else if (location.worldObject.name.Contains("Twig"))
                    {
                        spot = new DebrisSpot()
                        {
                            worldObject = location.worldObject,
                            tool = "Axe"
                        };
                    }
                    // Confirm we can perform this task before adding it
                    if (spot != null && (availableTools.Contains(spot.tool) || Mod.i.interaction.EquipToolIfOnHotbar(spot.tool, false)))
                    {
                        availableTools.Add(spot.tool);
                        DebrisSpots.Add(spot);
                    }
                }
                SLogger.Trace("found " + DebrisSpots.Count(spot => spot.worldObject.name.Contains("Weed")) + " weeds");
                SLogger.Trace("found " + DebrisSpots.Count(spot => spot.worldObject.name == "Stone") + " rocks");
                SLogger.Trace("found " + DebrisSpots.Count(spot => spot.worldObject.name.Contains("Twig")) + " twigs");
                // Sort the list so it's generally in order
                DebrisSpots = SortByDistance(DebrisSpots);
                hasScanned = true;
                return;
            }

            //step three: is there any debris? if not, we're complete
            if (DebrisSpots.Count == 0 && currentSpot == null)
            {
                IsComplete = true;
                return;
            }

            //are we already pathing?
            if (currentSpot != null)
            {
                switch (Mod.i.pathing.status)
                {
                    case PathingManager.Status.Idle:
                        SLogger.Alert("Pathing was still waiting when spot was assigned, this is wrong!");
                        break;
                    case PathingManager.Status.Pathing:
                        return;
                    case PathingManager.Status.Arrived:
                        //pick
                        Mod.i.interaction.SwingTool();
                        SLogger.Warn(currentSpot.tool.ToUpper() + "!");
                        DebrisSpots.Remove(currentSpot);
                        // Sort the list again
                        DebrisSpots = SortByDistance(DebrisSpots);
                        currentSpot = null;
                        return;
                    case PathingManager.Status.Stuck:
                        SLogger.Alert("Pathing got stuck when clearing debris!");
                        Mod.i.pathing.Stop(PathingManager.Status.Idle);
                        DebrisSpots.Remove(currentSpot);
                        currentSpot = null;
                        break;
                }
            }

            //check weeds
            //discard nonexistant ones that someone else dealt with
            //while (DebrisSpots.Count > 0 && !Game1.currentLocation.objects.ContainsKey(new Vector2(DebrisSpots[0].worldObject.location.x, DebrisSpots[0].worldObject.location.y)))
            //    DebrisSpots.RemoveAt(0);

            if (DebrisSpots.Count > 0)
            {
                //TODO: Add back
                int pathingCutoff = 9999; //(int)((150f / DebrisSpots.Count) * 150f);

                // If we have spots that don't have paths yet, create those and request pathing for them. Post 1 new spot for pathing each frame.
                foreach (var spot in DebrisSpots.Where(aSpot => aSpot.route == null).Take(1))
                {
                    SLogger.Trace("Submitting new spot: " + targetMap + "," + spot.worldObject.location.x + "," + spot.worldObject.location.y + " " + spot.worldObject.name + ". For async routing.");
                    spot.route = Mod.i.routing.GetRoute(spot.worldObject.location);
                }

                // See if there are any successful spots yet
                var successfulSpot = DebrisSpots.FirstOrDefault(aSpot => aSpot.route != null && aSpot.route.status == Route.Status.Successful);
                if (successfulSpot != null)
                {
                    // We have a successful spot. Use this one and cancel all others.
                    Mod.i.interaction.EquipToolIfOnHotbar(successfulSpot.tool);
                    currentSpot = successfulSpot;
                    Mod.i.routing.ExecuteRoute(successfulSpot.route);
                    foreach (var spot in DebrisSpots.Where(aSpot => aSpot != currentSpot).ToList())
                    {
                        if (spot.route != null)
                        {
                            // Setting this to cancelled will make async processing on them halt on the next loop.
                            spot.route.Cancel();
                        }
                        // Remove the route from the spot so it can be calculated from the new player location.
                        spot.route = null;
                    }
                }
                else
                {
                    // No successful ones yet. See if there are any failures and remove them.
                    foreach (var spot in DebrisSpots.Where(aSpot => aSpot.route != null && aSpot.route.status == Route.Status.Failed).ToList())
                    {
                        // We can't reach this one, remove it from the list.
                        DebrisSpots.Remove(spot);
                        SLogger.Trace("Can't route to " + targetMap + "," + spot.worldObject.location.x + "," + spot.worldObject.location.y + " " + spot.worldObject.name + ". " + DebrisSpots.Count() + " remaining.");
                    }
                }
            }
        }

        public override void CantMoveUpdate()
        {
            base.CantMoveUpdate();
        }
    }

}
