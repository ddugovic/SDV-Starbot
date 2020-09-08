using Starbot.Logging;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starbot.Objectives
{
    class ObjectiveSleep : Objective
    {
        public override string announceMessage => "Going to sleep";
        public override string uniquePoolId => "sleep";
        public override bool cooperative => true; //not exclusive to a single player

        public ObjectiveSleep()
        {
            IsComplete = false;
        }

        public override void Reset()
        {
            base.Reset();
            IsComplete = false;
        }

        public override void Step()
        {
            base.Step();

            //step one: route to the homelocation
            if (Game1.player.homeLocation != Game1.player.currentLocation.NameOrUniqueName)
            {
                Mod.i.core.RouteTo(Game1.player.homeLocation, false, critical: true);
                return;
            }

            //step two: to bed!
            if(!(Game1.player.currentLocation is StardewValley.Locations.FarmHouse))
            {
                Logger.Error("This is home but not a FarmHouse?!");
                Fail();
                return;
            }

            var fh = Game1.player.currentLocation as StardewValley.Locations.FarmHouse;
            var bed = fh.getBedSpot();

            Mod.i.core.RouteTo(Game1.player.currentLocation.NameOrUniqueName, false, bed.X, bed.Y, true);
        }

        public override void CantMoveUpdate()
        {
            base.CantMoveUpdate();
            if (Game1.dialogueUp)
            {
                Logger.Info("Bed prompt activated. Choosing yes...");
                Mod.i.core.AnswerGameLocationDialogue(0);
                Mod.i.core.IsSleeping = true;
                IsComplete = true;
            }
        }
    }
}
