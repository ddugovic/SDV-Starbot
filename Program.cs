﻿using StardewModdingAPI;
using System;
using StardewModdingAPI.Events;
using Starbot.Pathfinding;
using Starbot.Threading;
using System.Runtime.CompilerServices;

namespace Starbot
{
    public class Mod : StardewModdingAPI.Mod
    {
        internal static Mod i;

        internal static Random RNG = new Random(Guid.NewGuid().GetHashCode());
        internal static bool BotActive = false;
        internal static Input2 Input = new Input2();

        internal ThreadManager Threading;
        internal PathingManager Pathfinding;
        internal StarbotCore core;

        public override void Entry(IModHelper helper)
        {
            i = this;
            i.Threading = new ThreadManager();
            i.Pathfinding = new PathingManager();
            i.core = new StarbotCore();

            //Input.Setup();

            Helper.Events.Input.ButtonPressed += Input_ButtonPressed;
            Helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            Helper.Events.Display.Rendered += Display_Rendered;
            Helper.Events.Multiplayer.ModMessageReceived += Routing.Multiplayer_ModMessageReceived;
        }

        private void Display_Rendered(object sender, RenderedEventArgs e) {
            if (!BotActive)
                return;
            core.Display(e);
        }

        private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!BotActive) return;
            core.Update(e);
            if (core.WantsToStop)
            {
                Monitor.Log("Bot is going to stop itself to prevent further complications.", LogLevel.Warn);
                ToggleBot();
            }
        }

        private void Input_ButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            bool shifting = false;
            if (e.IsDown(SButton.LeftShift)) shifting = true;
            if (e.IsDown(SButton.RightShift)) shifting = true;

            //prevent vanilla from disabling the input simulator on esc key down
            if (BotActive && e.IsDown(SButton.Escape)) Helper.Input.Suppress(SButton.Escape);

            //bot toggle hotkey
            if (e.Button == SButton.B && shifting)
            {
                if (!Context.IsWorldReady && !BotActive)
                {
                    Monitor.Log("Cannot toggle bot in current game state.", LogLevel.Warn);
                    return;
                }
                Helper.Input.Suppress(SButton.B);
                ToggleBot();
            }

            else if(e.Button == SButton.F)
            {
                Monitor.Log("Player location: " + StardewValley.Game1.player.currentLocation.NameOrUniqueName + ", " + StardewValley.Game1.player.getTileX() + ", " + StardewValley.Game1.player.getTileY());
            }
        }

        private void ToggleBot()
        {
            BotActive = !BotActive;
            Monitor.Log("Toggled bot status. Bot is now " + (BotActive ? "ON." : "OFF."), LogLevel.Warn);
            if (!BotActive)
            {
                Input.UninstallSimulator();
                core.ReleaseKeys();
            }
            else
            {
                Input.InstallSimulator();
                core.Reset();
            }
        }
    }
}