using Starbot.Logging;
using StarbotLib.Pathfinding;
using StarbotLib.World;
using StardewModdingAPI.Events;
using System.Linq;
using System.Threading;

namespace Starbot.Pathfinding
{

    public class RoutingManager : Manager
    {
        public enum Status
        {
            Setup,
            Idle,
            Routing,
            Arrived,
            Stuck
        };
        public Status status;

        public Route currentRoute;

        public RoutingManager()
        {

        }

        public void ExecuteRoute(Route route)
        {
            if (route == null ||
                route.start == null ||
                route.target == null ||
                !route.paths.Any())
            {
                SLogger.Error("Route was invalid when trying to execute.");
                return;
            }
            if (!route.start.Equals(route.paths.First().steps.First()))
            {
                SLogger.Error("Route starting location didn't match starting step when trying to execute.");
                return;
            }
            if (Mod.i.maps.PlayerDistance(route.start) >= 2)
            {
                SLogger.Error("Route starting location didn't match player location when trying to execute.");
                return;
            }
            currentRoute = route;
            status = Status.Routing;
        }

        public void Stop(Status status)
        {
            currentRoute = null;
            Mod.i.pathing.Stop(PathingManager.Status.Idle);
            this.status = status;
        }

        public Route GetRoute(Location target, bool pathUntilTarget = false, int cutoff = -1)
        {
            return GetRoute(Mod.i.player.location, target, pathUntilTarget, cutoff);
        }

        public Route GetRoute(Location start, Location target, bool pathUntilTarget = false, int cutoff = -1)
        {
            return Mod.i.server.routes.GetRoute(start, target, pathUntilTarget, cutoff);
        }

        public override void UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (status == Status.Routing)
            {
                // We're actively routing, check if pathing is ready for more inputs
                switch (Mod.i.pathing.status)
                {
                    case PathingManager.Status.Setup:
                        SLogger.Alert("Routing tried to operate while pathing was still in setup mode.");
                        Thread.Sleep(1000);
                        break;
                    case PathingManager.Status.Pathing:
                        // Good
                        break;
                    case PathingManager.Status.Idle:
                    case PathingManager.Status.Arrived:
                        // We've finished the latest path, advance the route if there are
                        // any paths left in it, otherwise mark completed.
                        if (!currentRoute.paths.Any())
                        {
                            currentRoute = null;
                            status = Status.Arrived;
                            Mod.i.pathing.Stop(PathingManager.Status.Idle);
                            SLogger.Info("Routing complete.");
                            return;
                        }
                        var nextPath = currentRoute.paths.First();
                        currentRoute.paths.RemoveAt(0);
                        Mod.i.pathing.ExecutePath(nextPath);
                        break;
                    case PathingManager.Status.Stuck:
                        // Pathing is stuck, pass that up the chain.
                        SLogger.Alert("Routing stuck.");
                        currentRoute = null;
                        status = Status.Stuck;
                        break;
                }
            }
        }

        public override void Rendered(object sender, RenderedEventArgs e)
        {
        }

        public override void SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
        }

        /*
        if (Game1.IsMultiplayer && !Game1.IsMasterGame) {
            //client mode
            maps.Clear();
            Logger.Info("Starbot is now in multiplayer client mode.");
            Logger.Info("The server will need to have Starbot installed to proceed.");
            Logger.Info("Awaiting response from server...");
            Mod.i.Helper.Multiplayer.SendMessage<int>(0, "authRequest");
        }
        else {
        }
        */
        /*
        public void Multiplayer_ModMessageReceived(object sender, ModMessageReceivedEventArgs e) {
            if (Game1.IsMasterGame && e.Type == "authRequest") {
                Logger.Trace("Starbot authorization requested by client. Approving...");
                //listen for authorization requests
                Dictionary<string, Map> response = null;
                if (maps.Count > 0) {
                    //host bot is active, use existing cache
                    response = maps;
                }
                else {
                    response = BuildRouteCache();
                }
                Mod.i.Helper.Multiplayer.SendMessage<Dictionary<string, Map>>(response, "authResponse");
            }
            else if (!Game1.IsMasterGame && e.Type == "authResponse") {
                //listen for authorization responses
                maps = e.ReadAs<Dictionary<string, Map>>();
                Logger.Trace("Starbot authorization request was approved by server.");
                Logger.Trace("Server offered routing data for " + maps.Count + " locations.");
                status = Status.Ready;
            }
            else if (e.Type == "taskAssigned") {
                string task = e.ReadAs<string>();
                Logger.Trace("Another player has taken task: " + task);
                Mod.i.core.ObjectivePool.RemoveAll(x => x.uniquePoolId == task);
            }
        }
        */
    }
}
