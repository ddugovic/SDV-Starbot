using Starbot.Logging;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starbot.Objectives
{
    public class ObjectiveForage : Objective
    {
        public override string announceMessage => "Forage the " + targetMap;

        public override string uniquePoolId => "forage." + targetMap;

        public override bool cooperative => false;

        string targetMap;
        public List<Tuple<int, int, bool>> ForageSpots = new List<Tuple<int, int, bool>>();
        bool hasScanned = false;
        bool WasRoutingToForage = false;
        bool RoutingComplete = false;

        public ObjectiveForage(string map)
        {
            IsComplete = false;
            this.targetMap = map;
        }

        public override void Reset()
        {
            base.Reset();
            hasScanned = false;
            WasRoutingToForage = false;
            RoutingComplete = false;
            ForageSpots.Clear();
            IsComplete = false;
        }

        public override void Step()
        {
            base.Step();

            //step one: are we on the beach? no? route to the beach.
            if(Game1.player.currentLocation.NameOrUniqueName != targetMap)
            {
                if (!Mod.i.core.IsRouting)
                {
                    int tx = -3, ty = -3;
                    Utility.getDefaultWarpLocation(targetMap, ref tx, ref ty);
                    Mod.i.core.RouteTo(targetMap, false, tx, ty, true);
                }
                return;
            }

            //step two: scan for forages on the map
            if (!hasScanned){
                Logger.Warn("Scanning for forage items...");
                foreach(var o in Game1.currentLocation.Objects.Values)
                {
                    if (o.isForage(Game1.currentLocation))
                    {
                        bool hs = false;
                        if (o.ParentSheetIndex == 590) hs = true;
                        ForageSpots.Add(new Tuple<int, int, bool>((int)o.TileLocation.X, (int)o.TileLocation.Y, hs));
                        Logger.Alert("Found forage item: " + (int)o.TileLocation.X + ", " + (int)o.TileLocation.Y);
                    }
                }
                hasScanned = true;
                return;
            }

            //step three: is there any forage? if not, we're complete
            if(ForageSpots.Count == 0)
            {
                IsComplete = true;
                return;
            }

            var spot = ForageSpots[0];

            //step four: route to the next piece of forage
            if (!WasRoutingToForage && !RoutingComplete)
            {
                WasRoutingToForage = true;

                //check hotbar for hoe
                if(spot.Item3)
                    Mod.i.core.EquipToolIfOnHotbar("Hoe"); //in case of worms

                int x = spot.Item1;
                int y = spot.Item2;
                //try the tile left of it
                if (!Mod.i.core.RouteTo(targetMap, true, x, y)) {
                    //we can't reach this one. remove it from the list
                    ForageSpots.RemoveAt(0);
                    Logger.Trace("Can't route to " + x + "," + y + " forage. " + ForageSpots.Count() + " remaining.");
                    WasRoutingToForage = false;
                }
                if (WasRoutingToForage)
                {
                    WasRoutingToForage = false;
                    RoutingComplete = true;
                }
                return;
            }

            if (RoutingComplete)
            {
                //step five: face the forage
                bool gotcha = (int)(Game1.player.GetToolLocation().X / 64f) == spot.Item1 && (int)(Game1.player.GetToolLocation().Y / 64f) == spot.Item2;
                if (!gotcha) //we're in the wrong spot. try to face
                {
                    int x = spot.Item1;
                    int y = spot.Item2;
                    int px = Game1.player.getTileX();
                    int py = Game1.player.getTileY();

                    Mod.i.core.FaceTile(x, y);

                    WasRoutingToForage = false;
                    return;
                }

                //pick
                bool hoeSpot = spot.Item3;
                Mod.i.core.FaceTile(spot.Item1, spot.Item2);
                
                if (hoeSpot)
                    Mod.i.core.SwingTool();
                else
                    Mod.i.core.DoActionButton();
                Logger.Warn("Pick!");
                ForageSpots.RemoveAt(0);
                RoutingComplete = false;
            }
        }

        public override void CantMoveUpdate()
        {
            base.CantMoveUpdate();
            if (Game1.dialogueUp)
            {

                if(Game1.activeClickableMenu is DialogueBox)
                {
                    Logger.Warn("stupid archeology box. shoo");
                    Game1.dialogueUp = false;
                    Game1.currentDialogueCharacterIndex = 0;
                    Game1.playSound("dialogueCharacterClose");
                    Game1.activeClickableMenu = null;
                    Game1.player.forceCanMove();
                }
            }
        }
    }
}
