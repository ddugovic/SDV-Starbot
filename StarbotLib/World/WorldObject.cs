using StarbotLib.Logging;
using System;

namespace StarbotLib.World
{
    [Serializable]
    public class WorldObject : MarshalByRefObject
    {
        public Location location;
        public bool passable;
        public string name;
        public string displayName;
        public string description;
        public string category;
        public bool actionable;

        public WorldObject()
        {

        }

        public SavableWorldObject GetSavable()
        {
            return new SavableWorldObject()
            {
                passable = passable,
                name = name,
                displayName = displayName,
                description = description,
                category = category,
                actionable = actionable
            };
        }
    }
}
