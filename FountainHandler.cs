using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WarpMod
{
    /// <summary>
    /// Handles the special interaction with the Town Fountain that gives the Magic Atlas
    /// </summary>
    public class FountainHandler
    {
        private readonly IModHelper Helper;
        private readonly IMonitor Monitor;
        private readonly ModConfig Config;
        
        // Constants for the fountain location
        private const int ATLAS_TILE_X = 24;
        private const int ATLAS_TILE_Y = 25;
        private const string ATLAS_FLAG = "GotMagicAtlas";
        
        // Track if the player has already received the atlas
        private bool AtlasReceived => Game1.player.mailReceived.Contains(ATLAS_FLAG);
        
        public FountainHandler(IModHelper helper, IMonitor monitor, ModConfig config)
        {
            this.Helper = helper;
            this.Monitor = monitor;
            this.Config = config;
            
            // Subscribe to the events
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.CursorMoved += OnCursorMoved;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }
        
        /// <summary>
        /// Called each tick to handle tool uses
        /// </summary>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            // Only process once per second to save resources
            if (e.IsMultipleOf(60))
            {
                if (Context.IsWorldReady && Game1.currentLocation is Town && !AtlasReceived && !PlayerHasAtlasItem())
                {
                    // Check if it's winter, as that's when the fountain is empty/frozen
                    bool isWinter = Game1.currentSeason.Equals("winter");
                    
                    // Check for using tools at the fountain during winter
                    if ((Game1.player.CurrentTool is Hoe) && 
                        isWinter &&
                        IsPlayerFacingAtlasSpot())
                    {
                        // Check for tool use action rather than mouse clicks
                        if (Game1.player.UsingTool && !Game1.player.canMove)
                        {
                            DigForAtlas();
                            // Add debug log
                            Monitor.Log("Player is using tool at atlas spot. Attempting to dig for atlas.", LogLevel.Debug);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Called when the cursor moves to update cursor appearance
        /// </summary>
        private void OnCursorMoved(object sender, CursorMovedEventArgs e)
        {
            if (!Context.IsWorldReady || !(Game1.currentLocation is Town))
                return;
                
            // If atlas not yet found, show magnifying glass cursor over atlas spot
            if (!AtlasReceived && !PlayerHasAtlasItem())
            {
                Vector2 cursorTile = e.NewPosition.GrabTile;
                if (IsClickOnAtlasSpot(cursorTile.X, cursorTile.Y))
                {
                    Game1.mouseCursor = 5; // Set to magnifying glass cursor
                }
            }
        }
        
        /// <summary>
        /// Handle button press events to intercept clicks on the atlas spot
        /// </summary>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // Only proceed if it's an action button (use/interact) and world is ready
            if (!Context.IsWorldReady || !e.Button.IsActionButton())
                return;
                
            // Need to be in Town
            if (!(Game1.currentLocation is Town town))
                return;
                
            // Get cursor position
            Vector2 cursorTile = e.Cursor.GrabTile;
            
            // Check if cursor is on the atlas spot
            if (IsClickOnAtlasSpot(cursorTile.X, cursorTile.Y))
            {
                HandleAtlasSpotInteraction(town);
                Helper.Input.Suppress(e.Button); // Suppress the original click
            }
        }
        
        /// <summary>
        /// Check if player is facing the atlas spot
        /// </summary>
        private bool IsPlayerFacingAtlasSpot()
        {
            // Get tile in front of player based on facing direction
            Vector2 facingTile = Game1.player.TilePoint.ToVector2();
            switch (Game1.player.FacingDirection)
            {
                case 0: // Up
                    facingTile.Y--; 
                    break;
                case 1: // Right
                    facingTile.X++; 
                    break;
                case 2: // Down
                    facingTile.Y++; 
                    break;
                case 3: // Left
                    facingTile.X--; 
                    break;
            }
            
            // Check if facing tile is the atlas spot
            bool result = IsClickOnAtlasSpot(facingTile.X, facingTile.Y);
            
            // If the player is facing the atlas spot, log it for debugging
            if (result)
            {
                Monitor.Log($"Player is facing the atlas spot at ({facingTile.X}, {facingTile.Y})", LogLevel.Debug);
            }
            
            return result;
        }
        
        /// <summary>
        /// Check if player is within specified distance of atlas spot
        /// </summary>
        private bool IsPlayerNearAtlasSpot(int tileDistance)
        {
            Vector2 playerPos = Game1.player.TilePoint.ToVector2();
            double distance = Math.Sqrt(
                Math.Pow(playerPos.X - ATLAS_TILE_X, 2) + 
                Math.Pow(playerPos.Y - ATLAS_TILE_Y, 2)
            );
            
            return distance <= tileDistance;
        }
        
        /// <summary>
        /// Process digging with hoe to find the atlas
        /// </summary>
        private void DigForAtlas()
        {
            if (!Context.IsWorldReady || !(Game1.currentLocation is Town town) || AtlasReceived || PlayerHasAtlasItem())
                return;
            
            // Log that dig method was triggered    
            Monitor.Log("DigForAtlas method triggered", LogLevel.Debug);
                
            // Play dig animation and sound
            Game1.playSound("hoeHit");
            
            // Create animation for digging
            town.temporarySprites.Add(new TemporaryAnimatedSprite(
                12, // Index of dirt animation
                Game1.player.getStandingPosition(),
                Color.White,
                8, // Animation frames
                false,
                100f, // Animation interval
                0, // Loop count (0 = no loops)
                -1, // Layer depth
                -1, // Extra info
                -1,
                0
            ));
            
            // After a brief delay, create the "artifact found" animation (book emerging from ground)
            DelayedAction.functionAfterDelay(() => CreateBookDiscoveryEffect(town), 600);
        }
        
        /// <summary>
        /// Create a book discovery effect - similar to the vanilla reading animation
        /// </summary>
        private void CreateBookDiscoveryEffect(Town town)
        {
            // Position where the book will appear
            Vector2 spotPos = new Vector2(ATLAS_TILE_X * Game1.tileSize, ATLAS_TILE_Y * Game1.tileSize);
            
            // Play the discovery sound
            Game1.playSound("woodyStep");
            
            // Create dust puff animation
            town.temporarySprites.Add(new TemporaryAnimatedSprite(
                "TileSheets\\animations",
                new Rectangle(0, 128, 64, 64), // Dust animation
                spotPos,
                false,
                0.1f,
                Color.White
            )
            {
                scale = 0.75f,
                layerDepth = 1f
            });
            
            // Create a vanilla-like book reading animation
            town.temporarySprites.Add(new TemporaryAnimatedSprite(
                "LooseSprites\\letterBG", 
                new Rectangle(0, 0, 320, 180),
                2000f, // Duration
                1, // Frames
                0, // Loop count
                new Vector2(Game1.viewport.Width / 2 - 160, Game1.viewport.Height / 2 - 90),
                false,
                false,
                0.9f,
                0f, // Alpha
                Color.White,
                1f, // Scale
                0f, // Rotation
                0f, // Rotation change
                0f) // Motion
            {
                alpha = 0f,
                alphaFade = -0.002f, // Fade in
                local = true
            });
            
            // After a slight delay, show dialogue and give player the atlas
            DelayedAction.functionAfterDelay(() => {
                // Show message about finding the atlas
                string message = "You break through the ice in the fountain and find a magical atlas! The pages shimmer with energy.";
                Game1.drawObjectDialogue(message);
                
                // Give the player the atlas after dialogue ends
                DelayedAction.functionAfterDelay(() => GiveAtlasToPlayer(), 2000);
            }, 1500);
        }
        
        /// <summary>
        /// Check if the click is on the atlas spot
        /// </summary>
        private bool IsClickOnAtlasSpot(float x, float y)
        {
            // More focused area around the fountain
            Rectangle atlasArea = new Rectangle(
                ATLAS_TILE_X - 1, 
                ATLAS_TILE_Y - 1, 
                3, // Width of 3 tiles
                3  // Height of 3 tiles
            );
            
            bool result = atlasArea.Contains((int)x, (int)y);
            
            // Log if this was a hit for debugging
            if (result)
            {
                Monitor.Log($"Atlas spot clicked/targeted at position ({x}, {y})", LogLevel.Debug);
            }
            
            return result;
        }
        
        /// <summary>
        /// Handle the interaction with the atlas spot
        /// </summary>
        private void HandleAtlasSpotInteraction(Town town)
        {
            string message;
            bool isWinter = Game1.currentSeason.Equals("winter");
            
            // If player already has the atlas
            if (AtlasReceived || PlayerHasAtlasItem())
            {
                message = "The fountain looks normal.";
                Game1.drawObjectDialogue(message);
                return;
            }
            
            // Different messages based on season
            if (isWinter)
            {
                // Show appropriate message based on tool
                if (Game1.player.CurrentTool is Hoe)
                {
                    message = "Something glimmers beneath the ice in the fountain. Your hoe might help dig it out.";
                    DigForAtlas(); // Automatically dig if they're holding a hoe
                }
                else
                {
                    message = "Something glimmers beneath the ice in the fountain. You might need to dig here with a hoe.";
                }
            }
            else
            {
                // During other seasons
                message = "You notice something glimmering at the bottom of the fountain, but can't reach it through the water. Perhaps when the fountain is frozen or empty...";
            }
            
            Game1.drawObjectDialogue(message);
        }
        
        /// <summary>
        /// Give the Magic Atlas to the player
        /// </summary>
        private void GiveAtlasToPlayer()
        {
            // Create the atlas item
            var atlas = new MagicAtlasItem();
            
            // Try to add it to inventory
            if (Game1.player.addItemToInventory(atlas) != null)
            {
                // If inventory full, drop it at player's feet
                Game1.createItemDebris(atlas, Game1.player.Position, -1);
            }
            
            // Play reward sound
            Game1.playSound("reward");
            
            // Play the book reading animation
            Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(
                "LooseSprites\\letterBG",
                new Rectangle(0, 0, 320, 180),
                2000f, // Duration
                1, // Frames
                0, // Loop count
                new Vector2(Game1.viewport.Width / 2 - 160, Game1.viewport.Height / 2 - 90),
                false,
                false,
                0.9f,
                0f, // Alpha
                Color.White,
                1f, // Scale
                0f, // Rotation
                0f, // Rotation change
                0f) // Motion
            {
                alpha = 1f,
                alphaFade = 0.0025f, // Fade out
                local = true
            });
            
            // Mark as received
            Game1.player.mailReceived.Add(ATLAS_FLAG);
        }
        
        /// <summary>
        /// Check if the player already has the atlas in their inventory
        /// </summary>
        private bool PlayerHasAtlasItem()
        {
            return Game1.player.Items.Any(item => item is MagicAtlasItem);
        }
    }
}