using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Starbot.Pathfinding;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starbot.UI {
    class PathRenderer {
        public static void RenderPath(Path path) {
            if (path != null && path.steps != null && path.steps.Count() > 0) {
                // Code credit 'UI Info Suite'
                foreach (var step in path.steps) {
                    Game1.spriteBatch.Draw(
                        Game1.mouseCursors,
                        Game1.GlobalToLocal(new Vector2(step.x * Game1.tileSize, step.y * Game1.tileSize)),
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
    }
}
