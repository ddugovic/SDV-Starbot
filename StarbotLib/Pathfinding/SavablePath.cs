using StarbotLib.World;
using System;
using System.Collections.Generic;

namespace StarbotLib.Pathfinding
{
    [Serializable]
    public class SavablePath
    {
        public SavableLocation start;
        public SavableLocation target;
        public bool pathUntilTarget;
        public List<SavableLocation> steps;

        public SavablePath()
        {
            steps = new List<SavableLocation>();
        }
    }
}
