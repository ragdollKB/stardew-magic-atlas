using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Menus;
using System;
using System.Linq;
using WarpMod.Utility;

namespace WarpMod
{
    /// <summary>
    /// Magic Atlas item that allows warping to discovered locations
    /// </summary>
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
        
        // Add description field
        private string _description = "A magical atlas that lets you warp to discovered locations.";
        
        /// <summary>
        /// Initialize the static properties of the class
        /// </summary>
        public static void Initialize(IModHelper helper, IMonitor monitor, ModConfig config)
        {
            Helper = helper;
            Monitor = monitor;
            Config = config;
            
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
        /// Default constructor - required for item serialization
        /// </summary>
        public MagicAtlasItem(ModConfig config = null)
            : base("102", 1) // Fix: Use string item ID "102" and stack size 1
        {
            this.config = config ?? new ModConfig();
            
            // Configure item properties
            this.Name = "Magic Atlas";
            this.bigCraftable.Value = false;
            this.Type = "Basic";
            this.Category = -22; // Misc category value in Stardew Valley
            this.Price = 5000;
            this.Edibility = -300; // Inedible
            this.Stack = 1;
            
            // Set description using the field
            this._description = "A magical atlas that lets you warp to discovered locations.";
        }

        /// <summary>
        /// Creates a new instance of the item
        /// </summary>
        public new StardewValley.Object getOne()
        {
            MagicAtlasItem newItem = new MagicAtlasItem();
            // Copy state if needed (nothing needed for now)
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
        /// Override to draw the item
        /// </summary>
        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            try
            {
                // Get the item's texture
                Texture2D texture = Helper?.ModContent.Load<Texture2D>("assets/items/MagicAtlas.png");
                
                if (texture != null)
                {
                    // Draw the item with proper scaling
                    spriteBatch.Draw(
                        texture,
                        location + new Vector2(32f, 32f) * scaleSize,
                        new Rectangle(0, 0, 16, 16),
                        color * transparency,
                        0f,
                        new Vector2(8f, 8f),
                        4.0f * scaleSize, // Scale properly to fill the inventory slot
                        SpriteEffects.None,
                        layerDepth
                    );
                    
                    // Draw stack number if needed
                    if (this.Stack > 1 && drawStackNumber != StackDrawType.Hide)
                        StardewValley.Utility.drawTinyDigits(this.Stack, spriteBatch, location + new Vector2((float)(64 - StardewValley.Utility.getWidthOfTinyDigitString(this.Stack, 3f)) + 3f, 64f - 18f + 2f), 3f, 1f, color);
                }
                else
                {
                    // Fallback if texture fails to load
                    Monitor?.Log("Failed to load Magic Atlas texture, using fallback drawing method", LogLevel.Warn);
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
        
        // Override the description property getter
        public override string getDescription()
        {
            return _description;
        }
    }
}