using Starbot.Logging;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Starbot.Actions
{
    class InteractionManager : Manager
    {

        private readonly int SwingToolTime = 30;
        private readonly int SwingToolDelay = 10;
        private int SwingToolTimer = 0;

        public InteractionManager()
        {

        }

        public bool IsWaiting()
        {
            return !Context.CanPlayerMove || SwingToolTimer > 0 || ActionButtonTimer > 0;
        }

        public bool EquipToolIfOnHotbar(string name)
        {
            return EquipToolIfOnHotbar(name, true);
        }

        public bool EquipToolIfOnHotbar(string name, bool equip)
        {
            var t = Game1.player.getToolFromName(name);
            if (t == null)
            {
                SLogger.Info("Could not equip tool: " + name + " (not found in inventory)");
                return false;
            }
            for (int index = 0; index < 12; ++index)
            {
                if (Game1.player.items.Count > index && Game1.player.items.ElementAt<Item>(index) != null)
                {
                    if (Game1.player.items[index] == t)
                    {
                        //found it
                        if (Game1.player.CurrentToolIndex != index)
                        {
                            if (!equip)
                            {
                                return true;
                            }
                            SLogger.Info("Equipping tool: " + name);
                            Game1.player.CurrentToolIndex = index;
                        }
                        return true;
                    }
                }
            }
            SLogger.Info("Could not equip tool: " + name + " (not found on hotbar)");
            return false;
        }

        public bool OpeningDoor = false;
        public void DoOpenDoor()
        {
            OpeningDoor = true;
            Mod.i.movement.FaceUp();
            StartActionButton();
        }

        private readonly int ActionButtonTime = 30;
        private int ActionButtonTimer = 0;
        public void DoActionButton(bool stop = true)
        {
            if (stop)
            {
                Mod.i.movement.Stop(MovementManager.Status.Idle);
            }
            StartActionButton();
            ActionButtonTimer = ActionButtonTime;
        }

        public void DoFace(bool stop = true)
        {
            if (stop)
            {
                Mod.i.movement.Stop(MovementManager.Status.Idle);
            }
            StartActionButton();
            ActionButtonTimer = ActionButtonTime;
        }

        private void StartActionButton()
        {
            Mod.Input.StartActionButton();
        }

        private void StopActionButton()
        {
            Mod.Input.StopActionButton();
        }

        public void SwingTool()
        {
            SwingToolTimer = SwingToolTime + SwingToolDelay;
            Mod.i.movement.Stop(MovementManager.Status.Idle);
        }

        private void StartUseTool()
        {
            Mod.Input.StartUseTool();
        }

        private void StopUseTool()
        {
            Mod.Input.StopUseTool();
        }

        public void AnswerGameLocationDialogue(int selection)
        {
            if (Game1.activeClickableMenu is StardewValley.Menus.DialogueBox)
            {
                var db = Game1.activeClickableMenu as StardewValley.Menus.DialogueBox;
                var responses = (List<Response>)typeof(StardewValley.Menus.DialogueBox).GetField("responses", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(db);
                SLogger.Trace("Responding to dialogue with selection: " + responses[selection].responseKey);
                Game1.currentLocation.answerDialogue(responses[selection]);
                Game1.dialogueUp = false;
                if (!Game1.IsMultiplayer)
                {
                    Game1.activeClickableMenu = null;
                    Game1.playSound("dialogueCharacterClose");
                    Game1.currentDialogueCharacterIndex = 0;
                }
            }
        }

        public override void UpdateTicked(object sender, UpdateTickedEventArgs e)
        {

            //are we waiting on action button
            if (ActionButtonTimer > 0)
            {
                ActionButtonTimer--;
                Mod.i.movement.Stop(MovementManager.Status.Idle);
                if (ActionButtonTimer == 0)
                {
                    StopActionButton();
                }
                return;
            }

            //are we waiting on a tool swing
            if (SwingToolTimer > 0)
            {
                SwingToolTimer--;
                Mod.i.movement.Stop(MovementManager.Status.Idle);
                if (SwingToolTimer == 0)
                {
                    StopUseTool();
                }
                else if (SwingToolTimer == 1)
                {
                    // We're finishing a tool swing. Tiles could have changed passability. Refresh the area around the player.
                    Mod.i.maps.RefreshArea(Mod.i.player.location, 1);
                }
                else if (SwingToolTimer <= SwingToolTime)
                {
                    StartUseTool();
                }
                return;
            }

        }

        public override void Rendered(object sender, RenderedEventArgs e)
        {
        }

        public override void SaveLoaded(object sender, SaveLoadedEventArgs e)
        {

        }
    }
}
