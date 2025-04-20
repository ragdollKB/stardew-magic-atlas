using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Menus;
using System;
using System.Linq;
using System.Xml.Serialization; // Add this using statement
using WarpMod.Utility;

namespace WarpMod
{
    /// <summary>
    /// Magic Atlas item that allows warping to discovered locations
    /// </summary>
    [XmlInclude(typeof(MagicAtlasItem))] // Include this type for serialization
    [XmlType("Mods_WarpMod_MagicAtlasItem")] // Define a unique XML type name
    public class MagicAtlasItem : StardewValley.Object
    {
        // Static references to mod helper and monitor for logging
        public static IModHelper Helper { get; private set; }
        public static IMonitor Monitor { get; private set; }
        public static ModConfig Config { get; private set; }

        // Properties
        private bool _HasBeenInInventory { get; set; } = false;
        // Explicitly use new keyword to resolve the warning
        public new bool HasBeenInInventory 
        { 
            get { return _HasBeenInInventory; } 
            set { _HasBeenInInventory = value; } 
        }
        
        // Static flag to track if any atlas has been read
        private static bool _anyAtlasRead = false;

        // Store the config reference
        private readonly ModConfig config;
        
        // Custom name and description for the item
        private readonly string _customName = "Magic Atlas";
        private readonly string _customDescription = "A magical atlas that lets you warp to discovered locations.";
        
        // Track if we've got a custom texture loaded successfully
        private static Texture2D _customTexture = null;
        
        /// <summary>
        /// Initialize the static properties of the class
        /// </summary>
        public static void Initialize(IModHelper helper, IMonitor monitor, ModConfig config)
        {
            Helper = helper;
            Monitor = monitor;
            Config = config;
            
            // Preload the texture
            if (helper != null)
            {
                try
                {
                    _customTexture = helper.ModContent.Load<Texture2D>("assets/items/atlas_sprite_64x64.png");
                    Monitor?.Log("Magic Atlas texture loaded successfully", LogLevel.Debug);
                }
                catch (Exception ex)
                {
                    Monitor?.Log($"Failed to load Magic Atlas texture: {ex.Message}", LogLevel.Error);
                    _customTexture = null;
                }
            }
            
            Monitor?.Log("Magic Atlas item initialized", LogLevel.Debug);
        }
        
        /// <summary>
        /// Check if any Magic Atlas has been read/used by the player
        /// </summary>
        public static bool HasBeenRead()
        {
            return _anyAtlasRead;
        }

        /// <summary>
        /// Parameterless constructor required for XML serialization.
        /// </summary>
        public MagicAtlasItem() : this(null) 
        {
            // Initialization logic handled by the main constructor called via : this(null)
        }

        /// <summary>
        /// Main constructor for the Magic Atlas item.
        /// </summary>
        public MagicAtlasItem(ModConfig config = null)
            : base("68", 1) // Using Lost Book ID (68) as base which is more thematically appropriate
        {
            this.config = config ?? new ModConfig();
            
            // Configure item properties
            this.Name = _customName;
            this.bigCraftable.Value = false;
            this.Type = "Basic";
            this.Category = -8; // Special Item category which is more visible
            this.Price = 5000;
            this.Edibility = -300; // Inedible
            this.Stack = 1;
            this.ParentSheetIndex = 68; // Explicitly setting to match constructor (Lost Book)
        }

        /// <summary>
        /// Creates a new instance of the item
        /// </summary>
        public new StardewValley.Object getOne()
        {
            MagicAtlasItem newItem = new MagicAtlasItem();
            return newItem;
        }

        /// <summary>
        /// Override to handle when the item is used from the inventory
        /// </summary>
        /// <returns>Always returns false to prevent consumption</returns>
        public override bool performUseAction(GameLocation location)
        {
            try
            {
                // Check if config allows using the item
                if (Config == null || !Config.EnableMagicAtlas)
                {
                    Game1.addHUDMessage(new HUDMessage("This item seems to be inactive.", HUDMessage.error_type));
                    return false;
                }

                // Mark as having been in inventory and read
                HasBeenInInventory = true;
                _anyAtlasRead = true;

                // Save reference to the currently held item and its position in inventory
                // This ensures we keep the proper item reference when the menu closes
                Item heldItem = Game1.player.CurrentItem;
                int heldItemIndex = Game1.player.CurrentToolIndex;

                // Register for events to monitor and restore the Atlas if needed
                if (Helper != null)
                {
                    Helper.Events.GameLoop.UpdateTicked += (sender, e) => RestoreAtlasIfNeeded(heldItem, heldItemIndex);
                }

                // Open warp menu when used
                Game1.activeClickableMenu = new GridWarpMenu(Helper, Monitor, Config);
            }
            catch (Exception ex)
            {
                Monitor?.Log($"Error using Magic Atlas item: {ex.Message}", LogLevel.Error);
                Game1.addHUDMessage(new HUDMessage("Something went wrong with the Magic Atlas.", HUDMessage.error_type));
            }
            return false; // Always return false to prevent consumption
        }

        /// <summary>
        /// Restore the atlas item if it disappeared from inventory
        /// This is called once per update tick after the atlas is used
        /// </summary>
        private void RestoreAtlasIfNeeded(Item originalItem, int originalIndex)
        {
            try
            {
                // Only run this once per tick after the atlas is used
                Helper.Events.GameLoop.UpdateTicked -= (sender, e) => RestoreAtlasIfNeeded(originalItem, originalIndex);

                // Check if the atlas should be restored
                if (originalItem is MagicAtlasItem && 
                    (Game1.player.Items[originalIndex] == null || 
                     !(Game1.player.Items[originalIndex] is MagicAtlasItem)))
                {
                    // If the atlas is missing or has been replaced with something else, restore it
                    Monitor?.Log("Atlas disappeared from inventory - restoring it", LogLevel.Debug);
                    
                    // If the slot is empty, just put the atlas back
                    if (Game1.player.Items[originalIndex] == null)
                    {
                        Game1.player.Items[originalIndex] = originalItem;
                    }
                    // If something else is in the slot (like a yellow stone), remove it and add the atlas
                    else
                    {
                        // Create a replacement atlas since the original one might have been transformed
                        Game1.player.Items[originalIndex] = new MagicAtlasItem(Config);
                    }
                }
                
                // Look for any objects on the ground that might be the transformed atlas
                // (like the yellow stone) and remove them
                if (Game1.currentLocation != null)
                {
                    Vector2 playerTileLocation = new Vector2(
                        (int)(Game1.player.Position.X / Game1.tileSize),
                        (int)(Game1.player.Position.Y / Game1.tileSize)
                    );
                    
                    var objectsToRemove = Game1.currentLocation.Objects.Values
                        .Where(o => o.ParentSheetIndex == 68 && 
                               Vector2.Distance(o.TileLocation, playerTileLocation) < 3)
                        .ToList();
                        
                    foreach (var obj in objectsToRemove)
                    {
                        Game1.currentLocation.Objects.Remove(obj.TileLocation);
                        Monitor?.Log("Removed dropped item that may have been a transformed atlas", LogLevel.Debug);
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor?.Log($"Error in RestoreAtlasIfNeeded: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>
        /// Override canBeDropped to prevent the atlas from being dropped
        /// </summary>
        public override bool canBeDropped()
        {
            return false; // Prevent the atlas from being dropped
        }

        /// <summary>
        /// Override the display name
        /// </summary>
        public override string DisplayName
        {
            get { return _customName; }
        }

        /// <summary>
        /// Override to always return our custom description
        /// </summary>
        public override string getDescription()
        {
            return _customDescription;
        }

        /// <summary>
        /// Override to draw the item in world
        /// </summary>
        public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1)
        {
            if (_customTexture != null)
            {
                spriteBatch.Draw(
                    _customTexture,
                    new Vector2(x, y),
                    new Rectangle(0, 0, 64, 64),
                    Color.White * alpha,
                    0f,
                    Vector2.Zero,
                    1f, 
                    SpriteEffects.None,
                    (float)(y + 32) / 10000f
                );
            }
            else
            {
                base.draw(spriteBatch, x, y, alpha);
            }
        }

        /// <summary>
        /// Override to draw the item when held
        /// </summary>
        public override void drawWhenHeld(SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
        {
            if (_customTexture != null)
            {
                spriteBatch.Draw(
                    _customTexture,
                    objectPosition,
                    new Rectangle(0, 0, 64, 64),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    1f,
                    SpriteEffects.None,
                    Math.Max(0f, (float)(f.GetBoundingBox().Bottom) / 10000f)
                );
            }
            else
            {
                base.drawWhenHeld(spriteBatch, objectPosition, f);
            }
        }

        /// <summary>
        /// Override to draw the item in menu
        /// </summary>
        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            try
            {
                if (_customTexture == null && Helper != null)
                {
                    // Try to load the texture if it failed earlier
                    _customTexture = Helper.ModContent.Load<Texture2D>("assets/items/atlas_sprite_64x64.png");
                }
                
                if (_customTexture != null)
                {
                    // Draw shadow if needed
                    if (drawShadow)
                    {
                        spriteBatch.Draw(
                            Game1.shadowTexture, 
                            location + new Vector2(32f, 48f) * scaleSize,
                            Game1.shadowTexture.Bounds,
                            color * 0.5f,
                            0f,
                            new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y),
                            3f * scaleSize,
                            SpriteEffects.None,
                            layerDepth - 0.0001f
                        );
                    }
                    
                    // Draw the item without scaling since we're using a proper 64x64 image
                    spriteBatch.Draw(
                        _customTexture,
                        location + new Vector2(32f, 32f) * scaleSize,
                        new Rectangle(0, 0, 64, 64),
                        color * transparency,
                        0f,
                        new Vector2(32f, 32f),
                        scaleSize, // Apply only the menu's scale size
                        SpriteEffects.None,
                        layerDepth
                    );
                    
                    // Draw stack number if needed
                    if (this.Stack > 1 && drawStackNumber != StackDrawType.Hide)
                        StardewValley.Utility.drawTinyDigits(this.Stack, spriteBatch, location + new Vector2((float)(64 - StardewValley.Utility.getWidthOfTinyDigitString(this.Stack, 3f)) + 3f, 64f - 18f + 2f), 3f, 1f, color);
                }
                else
                {
                    // Fall back to base class if our texture is unavailable
                    base.drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color, drawShadow);
                }
            }
            catch (Exception ex)
            {
                // Log error and use fallback drawing
                Monitor?.Log($"Error drawing Magic Atlas: {ex.Message}", LogLevel.Error);
                base.drawInMenu(spriteBatch, location, scaleSize, transparency, layerDepth, drawStackNumber, color, drawShadow);
            }
        }
    }
}