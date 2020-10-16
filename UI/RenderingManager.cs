using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarbotLib.Pathfinding;
using StardewModdingAPI.Events;
using StardewValley;
using System.Linq;

namespace Starbot.UI
{
    class RenderingManager : Manager
    {
        public override void Rendered(object sender, RenderedEventArgs e)
        {
        }

        public override void UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
        }

        public void RenderPath(Path path)
        {
            if (path != null && path.steps != null && path.steps.Count() > 0)
            {
                // Code credit 'UI Info Suite'
                foreach (var step in path.steps)
                {
                    Game1.spriteBatch.Draw(
                        Game1.mouseCursors,
                        Game1.GlobalToLocal(new Vector2(step.loc.x * Game1.tileSize, step.loc.y * Game1.tileSize)),
                        new Rectangle(194, 388, 16, 16),
                        Color.White * 0.7f,
                        0.0f,
                        Vector2.Zero,
                        Game1.pixelZoom,
                        SpriteEffects.None,
                        0.01f);
                }
            }
        }

        public override void SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
        }
    }
}
