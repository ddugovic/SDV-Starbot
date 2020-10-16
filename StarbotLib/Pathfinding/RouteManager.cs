using StarbotLib.Logging;
using StarbotLib.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace StarbotLib.Pathfinding
{
    [Serializable]
    public class RouteManager : MarshalByRefObject
    {
        private StarbotServerCore core;

        public enum Status
        {
            Waiting,
            Setup,
            Ready
        };
        public Status status;

        public RouteManager(StarbotServerCore core)
        {
            this.core = core;
            core.threads.StartWatchdog(new Thread(new ThreadStart(delegate
            {
                Thread.CurrentThread.Name = "WarpPathBuilder";
                while (core.maps.status != MapManager.Status.Ready)
                {
                    CLogger.Info("ROUTEMANAGER: Maps not ready. Waiting.");
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
                status = Status.Setup;
                BuildRouteCache();
                var totalWarps = warpPathCache.Count();
                var warpsCalculating = warpPathCache.Count(path => !path.IsReady());
                while (warpsCalculating > 0)
                {
                    warpsCalculating = warpPathCache.Count(path => !path.IsReady());
                    var percent = Math.Round((double)(totalWarps - warpsCalculating) / (double)totalWarps * 100.0);
                    CLogger.Info("Warp Paths Calculating: " + percent + "% (" + (totalWarps - warpsCalculating) + "/" + totalWarps + ")");
                    // Remove failed paths.
                    foreach (var path in warpPathCache.ToList())
                    {
                        if (path.status == Path.Status.Failed)
                        {
                            warpPathCache.Remove(path);
                        }
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
                // Remove failed paths.
                foreach (var path in warpPathCache.ToList())
                {
                    if (path.status != Path.Status.Successful)
                    {
                        warpPathCache.Remove(path);
                    }
                }
                status = Status.Ready;
                CLogger.Alert("Warp route calculation complete.");
                core.threads.StopWatchdog();
            })));
        }

        public void BuildRouteCache()
        {
            // Calculate the paths between all warp locations inside each map
            foreach (var map in core.maps.GetMaps())
            {
                var locs = map.GetLocations();
                // Locations with a warp origin are places the player enters the map
                foreach (var entranceLoc in locs.Where(loc => loc.warpOrigin != null))
                {
                    // Locations with a warp target are places the player leaves the map
                    foreach (var exitLoc in locs.Where(loc => loc.warpTarget != null))
                    {
                        var path = core.paths.GeneratePathObject(entranceLoc, exitLoc, false);
                        map.warpPaths.Add(path);
                        core.paths.CalculatePathAsync(path);
                    }
                }
            }
        }

        public Route GetRoute(Location start, Location target, bool pathUntilTarget = false, int cutoff = -1)
        {
            if (start == null || target == null)
            {
                throw new ArgumentNullException("locations were null when getting route.");
            }
            if (status == Status.Setup)
            {
                throw new Exception("Cannot calculate routes without setup complete.");
            }
            Route route = new Route()
            {
                start = start,
                target = target,
                pathUntilTarget = pathUntilTarget,
                cutoff = cutoff
            };
            route.SetStatus(Route.Status.Processing);
            if (start.map.Equals(target.map))
            {
                // Same map, just do a simple path
                var path = core.paths.GeneratePathObject(start, target, pathUntilTarget, cutoff);
                route.paths.Add(path);
                core.paths.CalculatePathAsync(path);
                return route;
            }
            // Not the same map, find valid routes
            route.childRoutes = SearchRoutes(route);
            return route;
        }

        private List<Route> SearchRoutes(Route route, List<Route> successfulRoutes = null)
        {
            if (successfulRoutes == null)
            {
                successfulRoutes = new List<Route>();
            }
            // If our route has no paths, add paths to all the exit points of this map and search those routes
            if (!route.paths.Any())
            {
                // Locations with a warp target are places the player leaves the map
                foreach (var exitLoc in route.start.map.GetLocations().Where(loc => loc.warpTarget != null))
                {
                    var path = core.paths.GeneratePathObject(route.start, exitLoc, false);
                    core.paths.CalculatePathAsync(path);
                    Route newRoute = (Route)route.Clone();
                    newRoute.parentRoute = route;
                    newRoute.paths.Add(path);
                    SearchRoutes(newRoute, successfulRoutes);
                }
            }
            // If our route has a path that warps to the target map, add a path to the target tile and finish
            else if (route.paths.Last().target.warpTarget.map.Equals(route.target.map))
            {
                var entrance = route.paths.Last().target.warpTarget;
                var path = core.paths.GeneratePathObject(entrance, route.target, false);
                core.paths.CalculatePathAsync(path);
                route.paths.Add(path);
                successfulRoutes.Add(route);
            }
            // Otherwise, we're not there yet, keep searching
            else
            {
                var searchedMaps = route.paths.Select(path => path.start.map).Distinct();
                var lastLocation = route.paths.Last().target.warpTarget;
                foreach (var path in warpPathCache.Where(dPath => dPath.start.Equals(lastLocation) && !searchedMaps.Contains(dPath.start.map)))
                {
                    Route newRoute = (Route)route.Clone();
                    newRoute.parentRoute = route;
                    newRoute.paths.Add(path);
                    SearchRoutes(newRoute, successfulRoutes);
                }
            }
            return successfulRoutes;
        }

        public List<SavablePath> GetSavablePaths()
        {
            var savables = new List<SavablePath>();
            foreach (var p in warpPathCache.Where(path => path.status == Path.Status.Successful || path.status == Path.Status.NeedsValidation))
            {
                savables.Add(p.GetSavable());
            }
            return savables;
        }
    }
}
