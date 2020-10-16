using StarbotLib.Logging;
using StarbotLib.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace StarbotLib.Pathfinding
{
    [Serializable]
    public class PathManager : MarshalByRefObject
    {
        private StarbotServerCore core;

        public enum Status
        {
            Setup,
            Ready
        };
        public Status status;

        private EventWaitHandle pathWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        private Queue<Path> pathRequests;

        public PathManager(StarbotServerCore core)
        {
            this.core = core;
            status = Status.Setup;
            pathRequests = new Queue<Path>();
            RunPathfinder();
        }

        private void RunPathfinder()
        {
            // Create 10 path calculation threads
            for (int threadID = 1; threadID <= 10; threadID++)
            {
                core.threads.StartWatchdog(new Thread(new ThreadStart(delegate
                {
                    Thread.CurrentThread.Name = "PathCalculationThread-" + threadID;
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    DateTime starting = DateTime.Now;
                    CLogger.Alert("Started new " + Thread.CurrentThread.Name);
                    while (true)
                    {
                        try
                        {
                            Path inputPath = null;
                            lock (pathRequests)
                            {
                                if (pathRequests.Any())
                                {
                                    inputPath = pathRequests.Dequeue();
                                }
                            }
                            if (inputPath != null && inputPath.status != Path.Status.Cancelled)
                            {
                                DateTime calcStart = DateTime.Now;
                                CalculatePath(inputPath);
                                if (inputPath.status == Path.Status.Successful)
                                {
                                    CLogger.Alert("Path (" + inputPath.start.x + ", " + inputPath.start.y + ") -> (" + inputPath.target.x + ", " + inputPath.target.y + ") processed by " + Thread.CurrentThread.Name + " in " + Math.Round((DateTime.Now - calcStart).Duration().TotalMilliseconds) + "ms.");
                                    if (core.routes.warpPathCache.Contains(inputPath))
                                    {
                                        core.Save();
                                    }
                                }
                            }
                            else
                            {
                                pathWaitHandle.Reset();
                                pathWaitHandle.WaitOne(TimeSpan.FromSeconds(0.5));
                            }
                        }
                        catch (Exception e)
                        {
                            CLogger.Error("Error in " + Thread.CurrentThread.Name, e);
                        }
                    }
                    //core.threads.StopWatchdog();
                })));
                status = Status.Ready;
            }
        }

        public Path GeneratePathObject(Map map, int startX, int startY, int targetX, int targetY, bool pathUntilTarget, int cutoff = -1)
        {
            return GeneratePathObject(map.GetLocation(startX, startY), map.GetLocation(targetX, targetY), pathUntilTarget, cutoff);
        }

        public Path GeneratePathObject(Location start, Location target, bool pathUntilTarget, int cutoff = -1)
        {
            return new Path()
            {
                start = start,
                target = target,
                pathUntilTarget = pathUntilTarget,
                cutoff = cutoff
            };
        }

        public void CalculatePathAsync(Path path)
        {
            if (path == null)
            {
                CLogger.Alert("Path was null in CalculatePathAsync.");
                return;
            }
            lock (pathRequests)
            {
                path.SetStatus(Path.Status.Waiting);
                pathRequests.Enqueue(path);
                pathWaitHandle.Set();
            }
        }

        public Path CalculatePath(Path path)
        {
            if (path == null)
            {
                CLogger.Alert("Path was null in CalculatePath.");
                return path;
            }
            if (path.start == null)
            {
                CLogger.Alert("Path START was null in CalculatePath.");
                return path;
            }
            if (path.start.map == null)
            {
                CLogger.Alert("Path START MAP was null in CalculatePath.");
                return path;
            }
            if (path.target == null)
            {
                CLogger.Alert("Path TARGET was null in CalculatePath.");
                return path;
            }
            if (path.target.map == null)
            {
                CLogger.Alert("Path TARGET MAP was null in CalculatePath.");
                return path;
            }
            path.SetStatus(Path.Status.Processing);
            Step current = null;
            var openList = new List<Step>();
            var closedList = new List<Step>();
            int g = 0;

            // start by adding the original position to the open list  
            openList.Add(new Step(path.start));

            while (openList.Count > 0)
            {
                // get the square with the lowest F score  
                var lowest = openList.Min(l => l.f);
                current = openList.First(l => l.f == lowest);

                // add to closed, remove from open
                closedList.Add(current);
                openList.Remove(current);

                // Do adjacent pathing
                if (path.pathUntilTarget && CheckAdjacentTarget(current.loc.x, current.loc.y, path.target))
                {
                    // Target is adjacent to us so we're finished.
                    break;
                }
                // if closed contains destination, we're done
                if (closedList.FirstOrDefault(l => l.loc.x == path.target.x && l.loc.y == path.target.y) != null)
                {
                    break;
                }

                if (path.status == Path.Status.Cancelled)
                {
                    CLogger.Info("Cancelled path!");
                    return path;
                }

                // if closed has exceed cutoff, break out and fail
                if (path.cutoff > 0 && closedList.Count > path.cutoff)
                {
                    //Logger.Log("Breaking out of pathfinding, cutoff exceeded");
                    path.SetStatus(Path.Status.Failed);
                    return path;
                }

                var adjacentSteps = GetWalkableAdjacentSteps(current);
                g = current.g + 1;

                foreach (var adjacentStep in adjacentSteps)
                {
                    // if closed, ignore 
                    if (closedList.FirstOrDefault(l => l.loc.x == adjacentStep.loc.x && l.loc.y == adjacentStep.loc.y) != null)
                        continue;

                    // if it's not in open
                    if (openList.FirstOrDefault(l => l.loc.x == adjacentStep.loc.x && l.loc.y == adjacentStep.loc.y) == null)
                    {
                        // compute score, set parent  
                        adjacentStep.g = g;
                        adjacentStep.h = ComputeHScore(adjacentStep.preferable, adjacentStep.loc.x, adjacentStep.loc.y, path.target.x, path.target.y);
                        adjacentStep.f = adjacentStep.g + adjacentStep.h;
                        adjacentStep.parent = current;

                        // and add it to open
                        openList.Insert(0, adjacentStep);
                    }
                    else
                    {
                        // test if using the current G score makes the adjacent square's F score lower
                        // if yes update the parent because it means it's a better path  
                        if (g + adjacentStep.h < adjacentStep.f)
                        {
                            adjacentStep.g = g;
                            adjacentStep.f = adjacentStep.g + adjacentStep.h;
                            adjacentStep.parent = current;
                        }
                    }
                }
            }

            //make sure path is complete
            if (current == null)
            {
                path.SetStatus(Path.Status.Failed);
                return path;
            }
            if ((!path.pathUntilTarget || !CheckAdjacentTarget(current.loc.x, current.loc.y, path.target)) && (current.loc.x != path.target.x || current.loc.y != path.target.y))
            {
                CLogger.Warn("No path available " + path);
                path.SetStatus(Path.Status.Failed);
                return path;
            }

            // if path exists, let's pack it up for return
            while (current != null)
            {
                path.steps.Add(current);
                current = current.parent;
            }
            path.steps.Reverse();
            path.SetStatus(Path.Status.Successful);
            return path;
        }

        private bool CheckAdjacentTarget(int x, int y, Location target)
        {
            if (target != null &&
                ((x == target.x && y - 1 == target.y) ||  // Top
                 (x == target.x && y + 1 == target.y) ||  // Bottom
                 (x - 1 == target.x && y == target.y) ||  // Left
                 (x + 1 == target.x && y == target.y)))   // Right
            { 
                return true;
            }
            return false;
        }

        private List<Step> GetWalkableAdjacentSteps(Step step)
        {
            if (step.loc.warpTarget != null &&
                step.loc.map.Equals(step.loc.warpTarget.map))
            {
                // If the current location is a warp location, and it targets the current map,
                // modify the location as if we've just walked through the warp
                step.loc = step.loc.warpTarget;
            }
            return step.loc.PassableAdjacent().Select(loc => new Step(loc)).ToList();
        }

        private int ComputeHScore(bool preferable, int x, int y, int targetX, int targetY)
        {
            return (Math.Abs(targetX - x) + Math.Abs(targetY - y)) - (preferable ? 1 : 0);
        }
    }
}