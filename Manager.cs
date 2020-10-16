using StardewModdingAPI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starbot
{
    public abstract class Manager
    {
        public abstract void UpdateTicked(object sender, UpdateTickedEventArgs e);

        public abstract void Rendered(object sender, RenderedEventArgs e);

        public abstract void SaveLoaded(object sender, SaveLoadedEventArgs e);
    }
}
