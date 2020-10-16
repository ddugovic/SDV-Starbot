using StardewModdingAPI;
using System;
using StardewModdingAPI.Events;
using Starbot.Pathfinding;
using Starbot.Threading;
using Starbot.Actions;
using Starbot.UI;
using Starbot.Logging;
using Starbot.World;
using System.Runtime.Remoting.Channels;
using StarbotLib.World;
using StarbotLib;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting;
using StarbotLib.Pathfinding;

namespace Starbot
{
    public class Mod : StardewModdingAPI.Mod
    {
        internal static Mod i;

        internal static Random RNG = new Random(Guid.NewGuid().GetHashCode());
        internal static bool BotActive = false;
        internal static bool debugLocations = false;
        internal static Location lastLocation = null;
        internal static InputSim Input = new InputSim();

        // Core
        internal StarbotServerCore server;
        internal StarbotCore core;
        internal Player player;
        // Managers
        internal ThreadManager threads;
        internal PathingManager pathing;
        internal RoutingManager routing;
        internal InteractionManager interaction;
        internal MovementManager movement;
        internal RenderingManager rendering;
        // Utils
        internal StardewMapManager maps;

        public override void Entry(IModHelper helper)
        {
            i = this;

            // Server
            RegisterClientObjects();
            // Core
            i.core = new StarbotCore();
            i.player = new Player();
            // Managers
            i.threads = new ThreadManager();
            i.routing = new RoutingManager();
            i.pathing = new PathingManager();
            i.interaction = new InteractionManager();
            i.rendering = new RenderingManager();
            i.movement = new MovementManager();
            i.maps = new StardewMapManager();

            Helper.Events.Input.ButtonPressed += Input_ButtonPressed;
            Helper.Events.GameLoop.UpdateTicked += GameLoop_UpdateTicked;
            Helper.Events.Display.Rendered += Display_Rendered;
            Helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;
            //Helper.Events.Multiplayer.ModMessageReceived += Routing.Multiplayer_ModMessageReceived;
        }

        private void RegisterClientObjects()
        {
            var serverAddress = "ipc://starbot/StarbotServer";
            IpcClientChannel clientChannel = new IpcClientChannel();
            ChannelServices.RegisterChannel(clientChannel, true);

            i.server = (StarbotServerCore)Activator.GetObject(typeof(StarbotServerCore), serverAddress);
            //RemotingConfiguration.RegisterActivatedClientType(typeof(StarbotServerCore), serverAddress);
            //i.server = new StarbotServerCore();
            if (i.server == null)
            {
                throw new Exception("Unable to connect to Starbot Server. Shutting down Starbot.");
            }
            i.server.Initialize();
            //RemotingConfiguration.RegisterActivatedClientType(typeof(Map), serverAddress);
            //RemotingConfiguration.RegisterActivatedClientType(typeof(Location), serverAddress);
            //RemotingConfiguration.RegisterActivatedClientType(typeof(WorldObject), serverAddress);
            //RemotingConfiguration.RegisterActivatedClientType(typeof(Path), serverAddress);
            //RemotingConfiguration.RegisterActivatedClientType(typeof(Route), serverAddress);
        }

        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            i.server.Load(Constants.SaveFolderName);
            i.threads.SaveLoaded(sender, e);
            i.routing.SaveLoaded(sender, e);
            i.pathing.SaveLoaded(sender, e);
            i.interaction.SaveLoaded(sender, e);
            i.rendering.SaveLoaded(sender, e);
            i.movement.SaveLoaded(sender, e);
            i.maps.SaveLoaded(sender, e);
        }

        private void Display_Rendered(object sender, RenderedEventArgs e)
        {
            if (!BotActive || !Context.IsWorldReady)
                return;
            i.threads.Rendered(sender, e);
            i.routing.Rendered(sender, e);
            i.pathing.Rendered(sender, e);
            i.interaction.Rendered(sender, e);
            i.rendering.Rendered(sender, e);
            i.movement.Rendered(sender, e);
            i.maps.Rendered(sender, e);
        }

        private void GameLoop_UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            i.maps.RefreshPlayerLocation();
            if (debugLocations)
            {
                if (i.player.location != null && !i.player.location.Equals(lastLocation))
                {
                    SLogger.Info("Player location: " + i.player.location + " (passable=" + i.player.location.IsPassable() + ")");
                    lastLocation = i.player.location;
                }
            }
            if (!BotActive)
                return;
            i.core.UpdateTicked(sender, e);
            if (core.WantsToStop)
            {
                Monitor.Log("Bot is going to stop itself to prevent further complications.", LogLevel.Warn);
                ToggleBot();
            }
            else
            {
                i.threads.UpdateTicked(sender, e);
                i.routing.UpdateTicked(sender, e);
                i.pathing.UpdateTicked(sender, e);
                i.interaction.UpdateTicked(sender, e);
                i.rendering.UpdateTicked(sender, e);
                i.movement.UpdateTicked(sender, e);
                i.maps.UpdateTicked(sender, e);
            }
        }

        private void Input_ButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            bool shifting = false;
            if (e.IsDown(SButton.LeftShift))
                shifting = true;
            if (e.IsDown(SButton.RightShift))
                shifting = true;

            //prevent vanilla from disabling the input simulator on esc key down
            if (BotActive && e.IsDown(SButton.Escape))
                Helper.Input.Suppress(SButton.Escape);

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
            else if (e.Button == SButton.G)
            {
                debugLocations = !debugLocations;
            }
        }

        private void ToggleBot()
        {
            BotActive = !BotActive;
            Monitor.Log("Toggled bot status. Bot is now " + (BotActive ? "ON." : "OFF."), LogLevel.Warn);
            if (!BotActive)
            {
                Input.UninstallSimulator();
                i.pathing.Stop(PathingManager.Status.Idle);
            }
            else
            {
                Input.InstallSimulator();
                core.Reset();
            }
        }

        public bool IsWaiting()
        {
            return i.interaction.IsWaiting() || i.movement.IsWaiting() || i.server.maps.status != MapManager.Status.Ready || i.routing.status == RoutingManager.Status.Setup;
        }
    }
}