using Microsoft.Xna.Framework;
using Starbot.Logging;
using StarbotLib.World;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;

namespace Starbot.Actions
{
    class MovementManager : Manager
    {
        public enum Status
        {
            Idle,
            Moving,
            Arrived,
            Warped,
            Stuck
        };
        public Status status;

        private bool MovingDown = false;
        private bool MovingLeft = false;
        private bool MovingRight = false;
        private bool MovingUp = false;
        public bool ShouldBeMoving
        {
            get
            {
                return MovingDown || MovingLeft || MovingRight || MovingUp;
            }
        }
        private Location lastLocation;
        private Location currentLocation;
        private Location targetLocation;
        private int TicksSinceTileChange = 0;
        private int StopX = 0; //a delay on releasing keys so the animation isnt janky from spamming the button on and off
        private int StopY = 0;
        private readonly int StopDelay = 5;

        private readonly int FaceTime = 5;
        private int FaceTimer = 0;
        private Tuple<int, int> FaceTuple;

        public MovementManager()
        {

        }

        public bool IsWaiting()
        {
            return !Context.CanPlayerMove || this.FaceTimer > 0;
        }

        private void DetectWarp()
        {
            if (lastLocation != null && currentLocation != null)
            {
                /* Check to see if we've just warped.
                 * We've warped if the current location's name has changed. Alternatively if our player location 
                 * is more than 5 tiles away from the previous check, assume we've interally warped.
                 */
                var distance = Mod.i.maps.Distance(lastLocation, currentLocation);
                if (!lastLocation.map.Equals(currentLocation.map) || distance >= 3)
                {
                    SLogger.Info("Movement: Bot warped.");
                    Stop(Status.Warped);
                }
            }
        }
        
        private void Face()
        {
            if (FaceTimer > 0 && FaceTuple != null)
            {
                FaceTimer--;
                if (FaceTimer == 0)
                {
                    var v = new Vector2(((float)FaceTuple.Item1 * Game1.tileSize) + (Game1.tileSize / 2f), ((float)FaceTuple.Item2 * Game1.tileSize) + (Game1.tileSize / 2f));
                    Game1.player.faceGeneralDirection(v);
                    //Game1.setMousePosition(0, 0);//Utility.Vector2ToPoint(Game1.GlobalToLocal(v))
                    FaceTuple = null;
                }
                return;
            }
        }

        private void Move()
        {
            if (status == Status.Moving && targetLocation != null)
            {
                // We should be centered on tiles horizontally
                float tx = ((float)targetLocation.x);
                // We should be about 1/3 up a tile vertically
                float ty = ((float)targetLocation.y) + (1f / 3f);
                // Move the player until they are about 10% of a tile away from the target point
                float epsilon = 0.1f;

                if (tx - Mod.i.player.tileX > epsilon)
                    StartMovingRight();
                else if (Mod.i.player.tileX - tx > epsilon)
                    StartMovingLeft();
                else
                {
                    if (StopX > StopDelay)
                    {
                        StopX = 0;
                        StopMovingRight();
                        StopMovingLeft();
                    }
                    else
                        StopX++;
                }

                if (ty - Mod.i.player.tileY> epsilon)
                    StartMovingDown();
                else if (Mod.i.player.tileY - ty > epsilon)
                    StartMovingUp();
                else
                {
                    if (StopY > StopDelay)
                    {
                        StopY = 0;
                        StopMovingDown();
                        StopMovingUp();
                    }
                    else
                        StopY++;
                }

                if (Math.Abs(Mod.i.player.tileX - tx) < epsilon && 
                    Math.Abs(Mod.i.player.tileY - ty) < epsilon)
                {
                    //Logger.Info("Movement: Bot arrived.");
                    targetLocation = null;
                    status = Status.Arrived;
                }
            }
        }

        private void CheckStuck()
        {
            //stuck detection
            if (status == Status.Moving &&
                ShouldBeMoving &&
                lastLocation != null &&
                currentLocation != null)
            {
                //on logical tile change
                if (currentLocation.x != lastLocation.x ||
                    currentLocation.y != lastLocation.y)
                {
                    TicksSinceTileChange = 0;
                }
                TicksSinceTileChange++;
                // Assume the bot is stuck after 5 seconds worth of ticks
                if (TicksSinceTileChange > 60 * 5)
                {
                    SLogger.Alert("Bot is stuck.");
                    Stop(Status.Stuck);
                }
            }
            else
            {
                // We shouldn't be moving so reset tile change
                TicksSinceTileChange = 0;
            }
        }

        public void FaceUp()
        {
            var playerLocation = Mod.i.player.location;
            FaceTile(playerLocation.x, playerLocation.y - 1);
        }

        public void FaceDown()
        {
            var playerLocation = Mod.i.player.location;
            FaceTile(playerLocation.x, playerLocation.y + 1);
        }

        public void FaceLeft()
        {
            var playerLocation = Mod.i.player.location;
            FaceTile(playerLocation.x - 1, playerLocation.y);
        }

        public void FaceRight()
        {
            var playerLocation = Mod.i.player.location;
            FaceTile(playerLocation.x + 1, playerLocation.y);
        }

        public void FaceTile(int x, int y)
        {
            FaceTimer = FaceTime;
            FaceTuple = new Tuple<int, int>(x, y);
        }

        public void MoveTo(Location location)
        {
            targetLocation = location;
            status = Status.Moving;
        }

        private void StartMovingDown()
        {
            if (MovingUp)
                StopMovingUp();
            Mod.Input.StartMoveDown();
            MovingDown = true;
        }

        private void StopMovingDown()
        {
            Mod.Input.StopMoveDown();
            MovingDown = false;
        }

        private void StartMovingLeft()
        {
            if (MovingRight)
                StopMovingRight();
            Mod.Input.StartMoveLeft();
            MovingLeft = true;
        }

        private void StopMovingLeft()
        {
            Mod.Input.StopMoveLeft();
            MovingLeft = false;
        }

        private void StartMovingUp()
        {
            if (MovingDown)
                StopMovingDown();
            Mod.Input.StartMoveUp();
            MovingUp = true;
        }

        private void StopMovingUp()
        {
            Mod.Input.StopMoveUp();
            MovingUp = false;
        }

        private void StartMovingRight()
        {
            if (MovingLeft)
                StopMovingLeft();
            Mod.Input.StartMoveRight();
            MovingRight = true;
        }

        private void StopMovingRight()
        {
            Mod.Input.StopMoveRight();
            MovingRight = false;
        }

        public void Stop(Status targetStatus)
        {
            lastLocation = null;
            currentLocation = null;
            targetLocation = null;
            status = targetStatus;
            StopMovingDown();
            StopMovingLeft();
            StopMovingRight();
            StopMovingUp();
        }

        public override void UpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            lastLocation = currentLocation;
            currentLocation = Mod.i.player.location;
            DetectWarp();
            if (currentLocation != null &&
                Context.CanPlayerMove)
            {
                Face();
                Move();
                CheckStuck();
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
