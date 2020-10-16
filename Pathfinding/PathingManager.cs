using Starbot.Logging;
using StarbotLib.Pathfinding;
using StarbotLib.World;
using StardewModdingAPI.Events;
using System.Linq;

namespace Starbot.Pathfinding
{
    public class PathingManager : Manager
    {
        public enum Status
        {
            Setup,
            Idle,
            Pathing,
            Arrived,
            Stuck
        };
        public Status status;

        public Path currentPath;
        public Step currentStep;

        public PathingManager()
        {
            status = Status.Setup;
        }

        public void ExecutePath(Path path)
        {
            if (path == null ||
                path.start == null ||
                path.target == null ||
                !path.steps.Any())
            {
                SLogger.Error("Path was invalid when trying to execute.");
                return;
            }
            if (!path.start.Equals(path.steps.First()))
            {
                SLogger.Error("Path starting location didn't match starting step when trying to execute.");
                return;
            }
            if (Mod.i.maps.PlayerDistance(path.start) >= 2)
            {
                SLogger.Error("Path starting location didn't match player location when trying to execute.");
                return;
            }
            currentPath = path;
            status = Status.Pathing;
        }

        public void Stop(Status status)
        {
            currentStep = null;
            currentPath = null;
            Mod.i.movement.Stop(Actions.MovementManager.Status.Idle);
            this.status = status;
        }

        public override void UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (status == Status.Pathing)
            {
                // We're actively pathing, check if movement is ready for more inputs
                switch (Mod.i.movement.status)
                {
                    case Actions.MovementManager.Status.Moving:
                        // Good
                        break;
                    case Actions.MovementManager.Status.Idle:
                    case Actions.MovementManager.Status.Warped:
                    case Actions.MovementManager.Status.Arrived:
                        // We've arrived at the next point, advance the path if there are
                        // any steps left in it, otherwise complete.
                        if (!currentPath.steps.Any())
                        {
                            Mod.i.movement.Stop(Actions.MovementManager.Status.Idle);
                            if (currentPath.target.type == Location.Type.Door)
                            {
                                // We've ended pathing at a door. Open it.
                                Mod.i.interaction.DoOpenDoor();
                            }
                            else
                            {
                                // It's not a door, so just face the ending tile.
                                Mod.i.movement.FaceTile(currentPath.target.x, currentPath.target.y);
                            }
                            status = Status.Arrived;
                            SLogger.Info("Pathing complete.");
                            currentPath = null;
                            currentStep = null;
                            return;
                        }
                        var nextLocation = currentPath.steps.First();
                        if (currentStep != null)
                        {
                            //Logger.Alert("Pathing arrived at " + currentLocation.ToString() + ". Moving to: " + nextLocation.ToString());
                        }
                        currentPath.steps.RemoveAt(0);
                        Mod.i.movement.MoveTo(nextLocation.loc);
                        currentStep = nextLocation;
                        break;
                    case Actions.MovementManager.Status.Stuck:
                        // Movement is stuck, pass that up the chain.
                        SLogger.Alert("Pathing stuck.");
                        currentPath = null;
                        status = Status.Stuck;
                        break;
                }
            }
        }

        public override void Rendered(object sender, RenderedEventArgs e)
        {
            // Update the UI to show the bot's path
            Mod.i.rendering.RenderPath(currentPath);
        }

        public override void SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
        }
    }
}