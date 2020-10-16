using StarbotLib.Logging;
using StarbotLib.World;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StarbotLib.Pathfinding
{
    [Serializable]
    public class Path : MarshalByRefObject, ICloneable
    {
        public Route route;
        public Location start;
        public Location target;
        public bool pathUntilTarget;
        public int cutoff;
        public List<Step> steps;
        public enum Status
        {
            Created,
            Waiting,
            Processing,
            Cancelled,
            Failed,
            Successful,
            NeedsValidation
        }
        public Status status
        {
            get; private set;
        }

        public Path()
        {
            steps = new List<Step>();
            status = Status.Created;
            cutoff = -1;
        }

        public bool Validate()
        {
            return steps.All(step => step.loc.IsPassable());
        }

        public int GetCost()
        {
            if (steps == null || !steps.Any())
            {
                return Int32.MaxValue;
            }
            return steps.Count();
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
            if (IsReady() && route != null)
            {
                route.PathReady(this);
            }
        }

        public void Cancel()
        {
            SetStatus(Status.Cancelled);
        }

        public override string ToString()
        {
            if (start == null || target == null)
            {
                return "ErrorInvalidPath";
            }
            return start.ToString() + "-->" + target.ToString();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Path))
            {
                return false;
            }
            var other = (Path)obj;
            return status == other.status &&
                   start.Equals(other.start) &&
                   target.Equals(other.target) &&
                   GetCost().Equals(other.GetCost());
        }

        public object Clone()
        {
            return new Path()
            {
                start = this.start,
                target = this.target,
                pathUntilTarget = this.pathUntilTarget,
                cutoff = this.cutoff,
                steps = this.steps.ToList()
            };
        }

        public SavablePath GetSavable()
        {
            var savable = new SavablePath()
            {
                start = start.GetSavable(),
                target = target.GetSavable(),
                pathUntilTarget = pathUntilTarget
            };
            foreach (var step in steps)
            {
                savable.steps.Add(step.loc.GetSavable());
            }
            return savable;
        }
    }
}
