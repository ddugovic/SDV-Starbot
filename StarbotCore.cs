using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;
using Starbot.Pathfinding;
using Starbot.Logging;
using Starbot.Actions;
using StarbotLib.World;
using StarbotLib.Pathfinding;

namespace Starbot
{
    public class StarbotCore
    {
        public bool WantsToStop = false;
        private int LastGameDay = -3;
        public bool IsSleeping = false;


        //Objectives
        private bool IsBored = true;
        private Objective Objective = null;
        private List<string> ObjectivesCompletedToday = new List<string>();
        public List<Objective> ObjectivePool = new List<Objective>();

        private void FindNewObjective()
        {
            if (ObjectivePool.Count == 0)
            {
                //sleep time
                SLogger.Alert("Bot has no remaining objectives for today. Time for bed!");
                Objective = null;
            }
            else
            {
                int randomObjective = Mod.RNG.Next(ObjectivePool.Count);
                Objective = ObjectivePool[randomObjective];
                ObjectivePool.RemoveAt(randomObjective);
                SLogger.Info("New objective: " + Objective.announceMessage);
                if (Game1.IsMultiplayer && !Objective.cooperative)
                {
                    Mod.i.Helper.Multiplayer.SendMessage<string>(Objective.uniquePoolId, "taskAssigned");
                }
            }
        }

        private void ResetObjectivePool()
        {
            ObjectivePool.Clear();
            //ObjectivePool.Add(new Objectives.ObjectiveForage("BusStop"));
            //ObjectivePool.Add(new Objectives.ObjectiveForage("Beach"));
            //ObjectivePool.Add(new Objectives.ObjectiveForage("Forest"));
            //ObjectivePool.Add(new Objectives.ObjectiveForage("Backwoods"));
            //ObjectivePool.Add(new Objectives.ObjectiveForage("Mountain"));
            //ObjectivePool.Add(new Objectives.ObjectiveForage("Town"));
            ObjectivePool.Add(new Objectives.ObjectiveClearDebris("Farm"));
        }

        public void FailObjective()
        {
            if (Objective != null)
            {
                SLogger.Info("Objective failed: " + Objective.announceMessage);
                Objective.Fail();
                if (Objective.FailureCount < 3)
                    ObjectivePool.Add(Objective);
                else
                {
                    SLogger.Info("Skipping objective for today (too many failures): " + Objective.announceMessage);
                }
                Objective = null;

                Mod.i.movement.Stop(MovementManager.Status.Idle);
            }
        }
        /*

        //Routing
        public bool IsRouting = false;
        private bool IsCriticalRoute = false;
        private int RoutingDestinationX = -3, RoutingDestinationY = -3;
        private bool HasRoutingDestination
        {
            get
            {
                return RoutingDestinationX != -3 && RoutingDestinationY != -3;
            }
        }
        private List<string> Route = null;

        public bool RoutePath(Path path)
        {
            RoutingDestinationX = path.target.x;
            RoutingDestinationY = path.target.y;
            Logger.Info("Routing existing path " + (path.pathUntilTarget ? "until" : "to") + ": " + path.target.x + ", " + path.target.y);
            if (path.pathUntilTarget)
            {
                // If we're pathing until the target, not onto it, modify x/y to the last tuple in the path.
                // The last tuple should be directly next to the original target.
                var x = path.steps.Last().x;
                var y = path.steps.Last().y;
                Logger.Info("Last path location is now: " + x + ", " + y);
                if (RoutingDestinationX >= 0 && RoutingDestinationY >= 0)
                {
                    RoutingDestinationX = x;
                    RoutingDestinationY = y;
                }
            }
            //set the bot's path
            IsPathfinding = true;
            PathfindingDestinationX = path.target.x;
            PathfindingDestinationY = path.target.y;
            //TODO: add back
            PathfindingOpenDoor = false;
            currentPath = (Path)path.Clone();
            return true;
        }

        public bool RouteTo(string targetMap, bool pathUntilTarget, Path path, int targetX = -3, int targetY = -3, bool critical = false, int localCutoff = -1)
        {
            if (Game1.player.currentLocation.NameOrUniqueName != targetMap)
            {
                Logger.Info("Routing to: " + targetMap + (targetY == -1 ? targetX + ", " + targetY : ""));
                //calculate a route to the destination
                var route = Routing.GetRoute(targetMap);
                if (route == null || route.Count < 2)
                {
                    if (critical)
                    {
                        Logger.Warn("Routing failed: no route!");
                        FailObjective();
                    }
                    return false;
                }
                else
                {
                    //debug, print route:
                    //string routeInfo = "Route: ";
                    //foreach (string s in route) routeInfo += s + ", ";
                    //Logger.Log(routeInfo.Substring(0, routeInfo.Length - 2), LogLevel.Trace);
                }

                //set the bot's route
                IsRouting = true;
                IsCriticalRoute = critical;
                RoutingDestinationX = targetX;
                RoutingDestinationY = targetY;
                Route = route;
                AdvanceRoute(pathUntilTarget);
                return true;
            }
            else if (targetX != -3 && targetY != -3)
            {
                RoutingDestinationX = targetX;
                RoutingDestinationY = targetY;
                return PathfindTo(targetX, targetY, pathUntilTarget, path, critical, false, localCutoff);
            }
            return false;
        }

        private void ClearRoutingDestination()
        {
            RoutingDestinationX = -3;
            RoutingDestinationY = -3;
        }

        //call on location change
        private void AdvanceRoute(bool pathUntilTarget)
        {
            if (!IsRouting)
                return;
            //Logger.Log("Advancing route...", LogLevel.Trace);
            Route.RemoveAt(0); //remove the current map from the list
            if (Route.Count == 0)
            {
                //route complete
                IsRouting = false;
                Route = null;
                if (HasRoutingDestination)
                {
                    //pathfind to final destination coordinates
                    PathfindTo(RoutingDestinationX, RoutingDestinationY, pathUntilTarget, null, IsCriticalRoute);
                }
            }
            else
            {
                //pathfind to the next map
                foreach (var w in Game1.player.currentLocation.warps)
                {
                    if (w.TargetName == Route[0])
                    {
                        PathfindTo(w.X, w.Y, pathUntilTarget, null, IsCriticalRoute);
                        return;
                    }
                }
                foreach (var w in Game1.player.currentLocation.doors.Keys)
                {
                    if (Game1.player.currentLocation.doors[w] == Route[0])
                    {
                        PathfindTo(w.X, w.Y + 1, pathUntilTarget, null, IsCriticalRoute, true);
                        return;
                    }
                }
                if (Game1.player.currentLocation is StardewValley.Locations.BuildableGameLocation)
                {
                    StardewValley.Locations.BuildableGameLocation bl = Game1.player.currentLocation as StardewValley.Locations.BuildableGameLocation;
                    foreach (var b in bl.buildings)
                    {
                        if (b.indoors.Value.NameOrUniqueName == Route[0])
                        {
                            PathfindTo(b.getPointForHumanDoor().X, b.getPointForHumanDoor().Y + 1, pathUntilTarget, null, IsCriticalRoute, true);
                            return;
                        }
                    }
                }

            }
        }

        //Pathfinding
        private bool IsPathfinding = false;
        private int PathfindingDestinationX = -3, PathfindingDestinationY = -3;
        private bool PathfindingOpenDoor = false;
        private Path currentPath = null;

        private bool PathfindTo(int x, int y, bool pathUntilTarget, Path path, bool critical = false, bool openDoor = false, int cutoff = -1)
        {
            Logger.Info("Pathfinding " + (pathUntilTarget ? "until" : "to") + ": " + x + ", " + y);
            if (path == null)
            {
                path = Mod.i.pathing.CalculatePath(Mod.i.pathing.GeneratePathObject(
                    Game1.player.currentLocation, Game1.player.getTileX(), Game1.player.getTileY(), x, y, pathUntilTarget, cutoff));
            }
            if (path == null)
            {
                if (critical)
                {
                    Logger.Alert("Pathfinding failed: no path!");
                    FailObjective();
                }
                return false;
            }
            if (pathUntilTarget)
            {
                // If we're pathing until the target, not onto it, modify x/y to the last tuple in the path.
                // The last tuple should be directly next to the original target.
                x = path.steps.Last().x;
                y = path.steps.Last().y;
                Logger.Info("Last path location is now: " + x + ", " + y);
                if (RoutingDestinationX >= 0 && RoutingDestinationY >= 0)
                {
                    RoutingDestinationX = x;
                    RoutingDestinationY = y;
                }
            }

            //set the bot's path
            IsPathfinding = true;
            PathfindingDestinationX = x;
            PathfindingDestinationY = y;
            PathfindingOpenDoor = openDoor;
            currentPath = (Path)path.Clone();
            return true;
        }



        private void ClearPathfindingDestination()
        {
            PathfindingDestinationX = -3;
            PathfindingDestinationY = -3;
        }
        */

        public void Reset()
        {
            Mod.i.pathing.Stop(PathingManager.Status.Idle);

            WantsToStop = false;

            Objective = null;
            ObjectivesCompletedToday.Clear();
            ObjectivePool.Clear();
            IsBored = false;
            LastGameDay = -3;
        }


        public void UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (Mod.i.server.routes.status != RouteManager.Status.Ready ||
                Mod.i.server.maps.status != MapManager.Status.Ready)
                return;

            Mod.Input.Tick();

            //cutscenes break it anyway
            if (Game1.eventUp)
            {
                WantsToStop = true;
            }

            //only update navigation while navigation is possible
            if (Context.CanPlayerMove)
            {
                //new day
                if (LastGameDay != Game1.dayOfMonth)
                {
                    //new day
                    IsSleeping = false;
                    ObjectivesCompletedToday.Clear();
                    Objective = null;
                    ResetObjectivePool();
                    LastGameDay = Game1.dayOfMonth;
                }

                //shh don't wake the bot
                if (IsSleeping)
                    return;

                //cache player position
                int px = Game1.player.getTileX();
                int py = Game1.player.getTileY();

                //for now, if stuck let's just shut it down
                if (Mod.i.pathing.status == PathingManager.Status.Stuck)
                {
                    WantsToStop = true;
                    return;
                }

                /*

                if (OpeningDoor)
                {
                    return; //let's not interfere
                }



                if (HasMoveTarget)
                {
                    AdvanceMove();
                }
                */

                //bored?
                if (Mod.i.pathing.status == PathingManager.Status.Idle || 
                    Mod.i.pathing.status == PathingManager.Status.Arrived)
                {
                    IsBored = true;
                }

                if (IsBored)
                {
                    if (Objective != null)
                    {
                        if (Objective.IsComplete)
                        {
                            string objName = Objective.GetType().Name;
                            SLogger.Info("Objective completed: " + Objective.announceMessage);
                            ObjectivesCompletedToday.Add(objName);
                            Objective = null;
                        }
                        else
                        {
                            IsBored = false;
                            Objective.Step();
                        }
                    }
                    else
                    {
                        FindNewObjective();
                    }
                }
            }
            else
            {
                if (Objective != null && !Objective.IsComplete)
                {
                    Objective.CantMoveUpdate();
                }
            }
        }
    }
}
