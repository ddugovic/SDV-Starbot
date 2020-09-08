using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starbot.Pathfinding {
    public class Path {
        public Location start;
        public Location target;
        public bool pathUntilTarget;
        public int cutoff;
        public List<Location> steps;
        public enum Status {
            Created,
            Waiting,
            Processing,
            Failed,
            Successful
        }
        public Status status;

        public Path() {
            steps = new List<Location>();
            status = Status.Created;
            cutoff = -1;
        }

        public int GetCost() {
            if (steps == null || !steps.Any()) {
                return Int32.MaxValue;
            }
            return steps.Count();
        }

        public override string ToString() {
            var result = start.map.NameOrUniqueName + start.x + "-" + start.y + "-";
            if (target.map != null && !string.IsNullOrEmpty(target.map.NameOrUniqueName)) {
                result += target.map.NameOrUniqueName + "-";
            }
            if (!string.IsNullOrEmpty(target.hardType)) {
                result += target.hardType;
            }
            else if (!string.IsNullOrEmpty(target.containsType)) {
                result += target.containsType;
            }
            else {
                result += target.x + "-" + target.y;
            }
            return result;
        }

        public Path GenerateInstance() {
            return new Path() {
                start = this.start,
                target = this.target,
                pathUntilTarget = this.pathUntilTarget,
                cutoff = this.cutoff,
                steps = this.steps.ToList()
            };
        }
    }
}
