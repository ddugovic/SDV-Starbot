using StarbotLib.Logging;
using StarbotLib.World;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarbotLib.Pathfinding
{
    [Serializable]
    public class Route : MarshalByRefObject, ICloneable
    {
        public Location start;
        public Location target;
        public bool pathUntilTarget;
        public int cutoff;
        public List<Path> paths;

        //Parent-child relationship
        public Route parentRoute;
        public List<Route> childRoutes;

        public enum Status
        {
            Created,
            Waiting,
            Processing,
            Cancelled,
            Failed,
            Successful
        }
        public Status status
        {
            get; private set;
        }

        public Route()
        {
            CLogger.Info("ROUTE: Created new route.");
            paths = new List<Path>();
            childRoutes = new List<Route>();
            status = Status.Created;
            cutoff = -1;
        }

        public bool IsReady()
        {
            return status == Status.Failed ||
                   status == Status.Cancelled ||
                   status == Status.Successful;
        }

        public void SetStatus(Status status)
        {
            this.status = status;
            // Notify the parent route if one exists
            if (IsReady() && parentRoute != null)
            {
                parentRoute.RouteReady(this);
            }
        }

        public void PathReady(Path path)
        {
            if (paths.Contains(path) && paths.All(iPath => iPath.IsReady()))
            {
                var stat = Status.Failed;
                if (paths.All(iPath => iPath.status == Path.Status.Successful))
                {
                    stat = Status.Successful;
                }
                SetStatus(stat);
            }
        }

        public void RouteReady(Route route)
        {
            if (childRoutes.Contains(route) && childRoutes.All(iRoute => iRoute.IsReady()))
            {
                var stat = Status.Failed;
                var successfulRoutes = childRoutes.Where(iRoute => iRoute.status == Status.Successful);
                if (successfulRoutes.Any())
                {
                    stat = Status.Successful;
                    Route chosenRoute = successfulRoutes.First();
                    foreach (var candidateRoute in successfulRoutes)
                    {
                        if (candidateRoute.GetCost() < chosenRoute.GetCost())
                        {
                            chosenRoute = candidateRoute;
                        }
                    }
                    // Overwrite this parent route's paths with the chosen child
                    paths = chosenRoute.paths;
                    childRoutes.Clear();
                }
                SetStatus(stat);
            }
        }

        public int GetCost()
        {
            var cost = Int32.MaxValue;
            if (paths == null || !paths.Any())
            {
                return cost;
            }
            try
            {
                cost = paths.Sum(path => path.GetCost());
            }
            catch (OverflowException e)
            {
                // Ignore overflow, use maxint
            }
            return cost;
        }

        public void Cancel()
        {
            foreach (var path in paths)
            {
                if (path != null)
                {
                    path.Cancel();
                }
            }
            SetStatus(Status.Cancelled);
        }

        public override string ToString()
        {
            if (start == null || target == null || !paths.Any())
            {
                return "ErrorInvalidRoute";
            }
            return string.Join("==>", paths.Select(path => path.ToString()));
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Route))
            {
                return false;
            }
            var other = (Route)obj;
            if (!start.Equals(other.start) || !target.Equals(other.target) || paths.Count() != other.paths.Count())
            {
                return false;
            }
            return start.Equals(other.start) &&
                   target.Equals(other.target) &&
                   paths.Count() == other.paths.Count() &&
                   GetCost() == other.GetCost();
        }

        public object Clone()
        {
            return new Route()
            {
                start = start,
                target = target,
                pathUntilTarget = pathUntilTarget,
                cutoff = cutoff,
                paths = paths.Where(path => path != null).Select(path => (Path)path.Clone()).ToList()
            };
        }
    }
}
