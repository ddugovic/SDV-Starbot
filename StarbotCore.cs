using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Starbot.Pathfinding;
using Starbot.Logging;

namespace Starbot
{
    public class StarbotCore
    {
        public bool WantsToStop = false;

        private bool MovingDown = false;
        private bool MovingLeft = false;
        private bool MovingRight = false;
        private bool MovingUp = false;

        public bool ShouldBeMoving { get { return MovingDown || MovingLeft || MovingRight || MovingUp; } }
        private bool IsStuck = false;
        private int LastTileX = -3, LastTileY = -3;
        private string LastLocationName = null;
        private int UpdatesSinceCoordinateChange = 0;
        private int LastGameDay = -3;

        public bool IsSleeping = false;

        //Libraries


        //Objectives
        private bool IsBored = true;
        private Objective Objective = null;
        private List<string> ObjectivesCompletedToday = new List<string>();
        public List<Objective> ObjectivePool = new List<Objective>();

        private void FindNewObjective()
        {
            if(ObjectivePool.Count == 0)
            {
                //sleep time
                Logger.Alert("Bot has no remaining objectives for today. Time for bed!");
                Objective = new Objectives.ObjectiveSleep();
                Logger.Info("New objective: " + Objective.announceMessage);
            } else
            {
                int randomObjective = Mod.RNG.Next(ObjectivePool.Count);
                Objective = ObjectivePool[randomObjective];
                ObjectivePool.RemoveAt(randomObjective);
                Logger.Info("New objective: " + Objective.announceMessage);
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
            if(Objective != null)
            {
                Logger.Info("Objective failed: " + Objective.announceMessage);
                Objective.Fail();
                if (Objective.FailureCount < 3) ObjectivePool.Add(Objective);
                else
                {
                    Logger.Info("Skipping objective for today (too many failures): " + Objective.announceMessage);
                }
                Objective = null;
                IsRouting = false;
                IsPathfinding = false;
                StopMovingDown();
                StopMovingLeft();
                StopMovingRight();
                StopMovingUp();
            }
        }

        //Routing
        public bool IsRouting = false;
        private bool IsCriticalRoute = false;
        private int RoutingDestinationX = -3, RoutingDestinationY = -3;
        private bool HasRoutingDestination { get { return RoutingDestinationX != -3 && RoutingDestinationY != -3; } }
        private List<string> Route = null;

        public bool RouteTo(string targetMap, bool pathUntilTarget, int targetX = -3, int targetY = -3, bool critical = false, int localCutoff = -1)
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
            else if(targetX != -3 && targetY != -3)
            {
                RoutingDestinationX = targetX;
                RoutingDestinationY = targetY;
                return PathfindTo(targetX, targetY, pathUntilTarget, critical, false, localCutoff);
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
            if (!IsRouting) return;
            //Logger.Log("Advancing route...", LogLevel.Trace);
            Route.RemoveAt(0); //remove the current map from the list
            if (Route.Count == 0)
            {
                //route complete
                IsRouting = false;
                Route = null;
                if(HasRoutingDestination)
                {
                    //pathfind to final destination coordinates
                    PathfindTo(RoutingDestinationX, RoutingDestinationY, pathUntilTarget, IsCriticalRoute);
                }
            } else
            {
                //pathfind to the next map
                foreach (var w in Game1.player.currentLocation.warps)
                {
                    if (w.TargetName == Route[0])
                    {
                        PathfindTo(w.X, w.Y, pathUntilTarget, IsCriticalRoute);
                        return;
                    }
                }
                foreach (var w in Game1.player.currentLocation.doors.Keys)
                {
                    if (Game1.player.currentLocation.doors[w] == Route[0])
                    {
                        PathfindTo(w.X, w.Y + 1, pathUntilTarget, IsCriticalRoute, true);
                        return;
                    }
                }
                if (Game1.player.currentLocation is StardewValley.Locations.BuildableGameLocation)
                {
                    StardewValley.Locations.BuildableGameLocation bl = Game1.player.currentLocation as StardewValley.Locations.BuildableGameLocation;
                    foreach (var b in bl.buildings)
                    {
                        if(b.indoors.Value.NameOrUniqueName == Route[0])
                        {
                            PathfindTo(b.getPointForHumanDoor().X, b.getPointForHumanDoor().Y + 1, pathUntilTarget, IsCriticalRoute, true);
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

        private bool PathfindTo(int x, int y, bool pathUntilTarget, bool critical = false, bool openDoor = false, int cutoff = -1)
        {
            Logger.Info("Pathfinding " + (pathUntilTarget?"until":"to") + ": " + x + ", " + y);
            var path = Mod.i.Pathfinding.CalculatePath(Mod.i.Pathfinding.GeneratePathObject(
                Game1.player.currentLocation, Game1.player.getTileX(), Game1.player.getTileY(), x, y, pathUntilTarget, cutoff));
            if (path == null)
            {
                if (critical)
                {
                    Logger.Alert("Pathfinding failed: no path!");
                    FailObjective();
                }
                return false;
            }
            if (pathUntilTarget) {
                // If we're pathing until the target, not onto it, modify x/y to the last tuple in the path.
                // The last tuple should be directly next to the original target.
                x = path.steps.Last().x;
                y = path.steps.Last().y;
                Logger.Info("Last path location is now: " + x + ", " + y);
                if (RoutingDestinationX >= 0 && RoutingDestinationY >= 0) {
                    RoutingDestinationX = x;
                    RoutingDestinationY = y;
                }
            }

            //set the bot's path
            IsPathfinding = true;
            PathfindingDestinationX = x;
            PathfindingDestinationY = y;
            PathfindingOpenDoor = openDoor;
            currentPath = path.GenerateInstance();
            return true;
        }

        private void ClearPathfindingDestination()
        {
            PathfindingDestinationX = -3;
            PathfindingDestinationY = -3;
        }

        private void AdvancePath()
        {
            if (!IsPathfinding) return;
            if (currentPath.steps.Count == 0)
            {
                currentPath = null;
                IsPathfinding = false;
                //ClearMoveTarget();
                ClearPathfindingDestination();
                Logger.Alert("Pathfinding complete.");
                if (PathfindingOpenDoor)
                {
                    DoOpenDoor();
                }
                return;
            }
            var next = currentPath.steps[0];
            currentPath.steps.RemoveAt(0);
            MoveTo(next);
        }

        //Movement
        private bool HasMoveTarget { get { return MoveTargetX != -3 && MoveTargetY != -3; } }
        private int MoveTargetX = -3;
        private int MoveTargetY = -3;

        public void MoveTo(Location location)
        {
            MoveTargetX = location.x;
            MoveTargetY = location.y;
        }

        private void ClearMoveTarget()
        {
            MoveTargetX = -3;
            MoveTargetY = -3;
        }

        private int StopX = 0; //a delay on releasing keys so the animation isnt janky from spamming the button on and off
        private int StopY = 0;
        private readonly int StopDelay = 10;
        private void AdvanceMove()
        {
            float px = Game1.player.Position.X;
            float py = Game1.player.Position.Y;

            float tx = ((float)MoveTargetX * Game1.tileSize);// + (Game1.tileSize / 2);
            float ty = ((float)MoveTargetY * Game1.tileSize) + (Game1.tileSize / 3);

            float epsilon = Game1.tileSize * 0.1f;

            if (tx - px > epsilon) StartMovingRight();
            else if (px - tx > epsilon) StartMovingLeft();
            else
            {
                if (StopX > StopDelay)
                {
                    StopX = 0;
                    StopMovingRight();
                    StopMovingLeft();
                }
                else StopX++;
            }

            if (ty - py > epsilon) StartMovingDown();
            else if (py - ty > epsilon) StartMovingUp();
            else
            {
                if (StopY > StopDelay)
                {
                    StopY = 0;
                    StopMovingDown();
                    StopMovingUp();
                }
                else StopY++;
            }

            if(Math.Abs(px - tx) < epsilon && Math.Abs(py - ty) < epsilon)
            {
                ClearMoveTarget();
            }
        }

        public void Reset()
        {
            ReleaseKeys();
            Routing.Reset();
            ClearMoveTarget();
            ClearPathfindingDestination();
            IsPathfinding = false;
            PathfindingOpenDoor = false;
            currentPath = null;
            IsRouting = false;
            ClearRoutingDestination();
            Route = null;
            LastTileX = -3;
            LastTileY = -3;
            LastLocationName = null;

            IsStuck = false;
            UpdatesSinceCoordinateChange = 0;
            WantsToStop = false;

            MovingDown = false;
            MovingLeft = false;
            MovingRight = false;
            MovingUp = false;

            Objective = null;
            ObjectivesCompletedToday.Clear();
            ObjectivePool.Clear();
            IsBored = false;
            LastGameDay = -3;
        }

        public void Display(RenderedEventArgs e) {
            if (!Routing.Ready)
                return;
            // Update the UI to show the bot's path
            UI.PathRenderer.RenderPath(currentPath);
        }

        public void Update(UpdateTickedEventArgs e)
        {
            if (!Routing.Ready) return;

            Mod.Input.Update();

            //cutscenes break it anyway
            if (Game1.eventUp)
            {
                WantsToStop = true;
            }

            //are we waiting on action button
            if (ActionButtonTimer > 0)
            {
                ActionButtonTimer--;
                StopMovingDown();
                StopMovingLeft();
                StopMovingRight();
                StopMovingUp();
                if (ActionButtonTimer == 0)
                {
                    StopActionButton();
                }
                return;
            }

            //are we waiting on a tool swing
            if (SwingToolTimer > 0)
            {
                SwingToolTimer--;
                StopMovingDown();
                StopMovingLeft();
                StopMovingRight();
                StopMovingUp();
                if (SwingToolTimer == 0) {
                    StopUseTool();
                }
                else if (SwingToolTimer < SwingToolTime) {
                    StartUseTool();
                }
                return;
            }

            //are we waiting on facing a tile
            if (FaceTimer > 0) {
                FaceTimer--;
                if (FaceTimer == 0) {
                    var v = new Vector2(((float)FaceTuple.Item1 * Game1.tileSize) + (Game1.tileSize / 2f), ((float)FaceTuple.Item2 * Game1.tileSize) + (Game1.tileSize / 2f));
                    Game1.player.faceGeneralDirection(v);
                    Game1.setMousePosition(0, 0);//Utility.Vector2ToPoint(Game1.GlobalToLocal(v))
                    FaceTuple = null;
                }
                return;
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
                if (IsSleeping) return;

                //cache player position
                int px = Game1.player.getTileX();
                int py = Game1.player.getTileY();

                //for now, if stuck let's just shut it down
                if (IsStuck)
                {
                    WantsToStop = true;
                    return;
                }

                //on logical location change
                if(Game1.currentLocation.NameOrUniqueName != LastLocationName)
                {
                    if (OpeningDoor)
                    {
                        StopActionButton();
                        OpeningDoor = false;
                    }
                    LastLocationName = Game1.currentLocation.NameOrUniqueName;
                    ClearMoveTarget();
                    ReleaseKeys();
                    if(IsRouting && Route[0] == Game1.currentLocation.NameOrUniqueName) AdvanceRoute(false);
                }

                if (OpeningDoor)
                {
                    return; //let's not interfere
                }

                //if we don't have a move target, check the path for one
                if (!HasMoveTarget && IsPathfinding)
                {
                    //is pathfinding complete?
                    if(px == PathfindingDestinationX && py == PathfindingDestinationY)
                    {
                        //move to the next node in the path
                        AdvancePath();

                        //check for route destination and announce
                        if (HasRoutingDestination && px == RoutingDestinationX && py == RoutingDestinationY)
                        {
                            Logger.Info("Routing complete.");
                            ClearMoveTarget();
                            ClearPathfindingDestination();
                            ClearRoutingDestination();
                            ReleaseKeys();
                        }
                    } else
                    {
                        //move to the next node in the path
                        AdvancePath();
                    }
                }

                if (HasMoveTarget)
                {
                    AdvanceMove();
                }

                //stuck detection
                if (ShouldBeMoving)
                {
                    //on logical tile change
                    if (px != LastTileX || py != LastTileY)
                    {
                        LastTileX = px;
                        LastTileY = py;
                        UpdatesSinceCoordinateChange = 0;
                    }
                    UpdatesSinceCoordinateChange++;
                    if (!IsStuck && UpdatesSinceCoordinateChange > 60 * 5) OnStuck();
                }

                //bored?
                if(!HasMoveTarget && !IsPathfinding && !IsRouting)
                {
                    IsBored = true;
                }

                if (IsBored)
                {
                    if(Objective != null)
                    {
                        if (Objective.IsComplete)
                        {
                            string objName = Objective.GetType().Name;
                            Logger.Info("Objective completed: " + Objective.announceMessage);
                            ObjectivesCompletedToday.Add(objName);
                            Objective = null;
                        } else
                        {
                            IsBored = false;
                            Objective.Step();
                        }
                    } else
                    {
                        FindNewObjective();
                    }
                }
            } else
            {
                if(Objective != null && !Objective.IsComplete)
                {
                    Objective.CantMoveUpdate();
                }
            }
        }

        public bool EquipToolIfOnHotbar(string name) {
            return EquipToolIfOnHotbar(name, true);
        }

        public bool EquipToolIfOnHotbar(string name, bool equip)
        {
            var t = Game1.player.getToolFromName(name);
            if (t == null)
            {
                Logger.Info("Could not equip tool: " + name + " (not found in inventory)");
                return false;
            }
            for (int index = 0; index < 12; ++index)
            {
                if (Game1.player.items.Count > index && Game1.player.items.ElementAt<Item>(index) != null)
                {
                    if(Game1.player.items[index] == t)
                    {
                        //found it
                        if(Game1.player.CurrentToolIndex != index) {
                            if (!equip) {
                                return true;
                            }
                            Logger.Info("Equipping tool: " + name);
                            Game1.player.CurrentToolIndex = index;
                        }
                        return true;
                    }
                }
            }
            Logger.Info("Could not equip tool: " + name + " (not found on hotbar)");
            return false;
        }

        public bool OpeningDoor = false;
        private void DoOpenDoor()
        {
            OpeningDoor = true;
            //StartMovingUp();
            FaceTile(Game1.player.getTileX(), Game1.player.getTileY() - 1);
            StartActionButton();
        }

        private void OnStuck()
        {
            IsStuck = true;
            Logger.Info("Bot is stuck.");
            UpdatesSinceCoordinateChange = 0;
            ReleaseKeys();
            if(Objective != null)
            {
                FailObjective();
                IsStuck = false;
            }
        }

        private readonly int ActionButtonTime = 30;
        private int ActionButtonTimer = 0;
        public void DoActionButton(bool stop = true)
        {
            if (stop) { 
                StopMovingDown();
                StopMovingLeft();
                StopMovingRight();
                StopMovingUp();
            }
            StartActionButton();
            ActionButtonTimer = ActionButtonTime;
        }

        public void DoFace(bool stop = true) {
            if (stop) {
                StopMovingDown();
                StopMovingLeft();
                StopMovingRight();
                StopMovingUp();
            }
            StartActionButton();
            ActionButtonTimer = ActionButtonTime;
        }

        private void StartActionButton()
        {
            Mod.Input.StartActionButton();
        }

        private void StopActionButton()
        {
            Mod.Input.StopActionButton();
        }

        private readonly int SwingToolTime = 30;
        private readonly int SwingToolDelay = 10;
        private int SwingToolTimer = 0;
        public void SwingTool()
        {
            StopMovingDown();
            StopMovingLeft();
            StopMovingRight();
            StopMovingUp();
            SwingToolTimer = SwingToolTime + SwingToolDelay;
        }

        private void StartUseTool()
        {
            Mod.Input.StartUseTool();
        }

        private void StopUseTool()
        {
            Mod.Input.StopUseTool();
        }

        private readonly int FaceTime = 5;
        private int FaceTimer = 0;
        private Tuple<int, int> FaceTuple;
        public void FaceTile(int x, int y)
        {
            FaceTimer = FaceTime;
            FaceTuple = new Tuple<int, int>(x, y);
        }

        private void StartMovingDown()
        {
            if (MovingUp) StopMovingUp();
            Mod.Input.StartMoveDown();
            MovingDown = true;
        }

        private void StopMovingDown()
        {
            Mod.Input.StopMoveDown();
            MovingDown = false;
        }

        private void StartMovingLeft()
        {
            if (MovingRight) StopMovingRight();
            Mod.Input.StartMoveLeft();
            MovingLeft = true;
        }

        private void StopMovingLeft()
        {
            Mod.Input.StopMoveLeft();
            MovingLeft = false;
        }

        private void StartMovingUp()
        {
            if (MovingDown) StopMovingDown();
            Mod.Input.StartMoveUp();
            MovingUp = true;
        }

        private void StopMovingUp()
        {
            Mod.Input.StopMoveUp();
            MovingUp = false;
        }

        private void StartMovingRight()
        {
            if (MovingLeft) StopMovingLeft();
            Mod.Input.StartMoveRight();
            MovingRight = true;
        }

        private void StopMovingRight()
        {
            Mod.Input.StopMoveRight();
            MovingRight = false;
        }

        public void ReleaseKeys()
        {
            StopMovingDown();
            StopMovingLeft();
            StopMovingRight();
            StopMovingUp();
        }

        public void AnswerGameLocationDialogue(int selection)
        {
            if(Game1.activeClickableMenu is StardewValley.Menus.DialogueBox)
            {
                var db = Game1.activeClickableMenu as StardewValley.Menus.DialogueBox;
                var responses = (List < Response > )typeof(StardewValley.Menus.DialogueBox).GetField("responses", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(db);
                Logger.Trace("Responding to dialogue with selection: " + responses[selection].responseKey);
                Game1.currentLocation.answerDialogue(responses[selection]);
                Game1.dialogueUp = false;
                if (!Game1.IsMultiplayer)
                {
                    Game1.activeClickableMenu = null;
                    Game1.playSound("dialogueCharacterClose");
                    Game1.currentDialogueCharacterIndex = 0;
                }
            }
        }
    }
}
