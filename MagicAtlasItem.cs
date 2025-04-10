using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using System;

namespace WarpMod
{
    /// <summary>
    /// The Magic Atlas item that opens the warp menu when used.
    /// </summary>
    public class MagicAtlasItem : StardewValley.Object
    {
        // Static reference to mod for helper and config access
        private static IModHelper Helper;
        private static IMonitor Monitor;
        private static ModConfig Config;
        
        // Item ID for save/load functionality
        public const string ITEM_ID = "MagicAtlas";
        public const int CATEGORY_ID = -999; // Custom category ID
        
        // Flag for whether the atlas has been read
        public const string READ_FLAG = "ReadMagicAtlas";
        
        // Item description
        private string itemDescription = "A magical atlas that warps you to locations you've visited. Found in the town fountain.";
        private string readDescription = "A magical atlas that grants permanent warping ability to any location. Found in the town fountain.";
        
        // Constructor
        public MagicAtlasItem() : base()
        {
            // Set basic properties
            this.Name = ITEM_ID;
            this.displayName = "Magic Atlas";
            this.Category = CATEGORY_ID;
            this.Stack = 1; // Only one can be owned
            this.CanBeSetDown = false; // Can't be placed
            this.CanBeGrabbed = true; // Can be grabbed into inventory
            this.HasBeenInInventory = true; // Start as known
            this.Price = 5000; // High sale value, but shouldn't be sold!
            this.Type = "Crafting"; // Crafting item type
        }
        
        // Static initializer method
        public static void Initialize(IModHelper helper, IMonitor monitor, ModConfig config)
        {
            Helper = helper;
            Monitor = monitor;
            Config = config;
        }

        // Method to open the warp menu
        public void OpenWarpMenu()
        {
            if (Context.IsWorldReady && Game1.activeClickableMenu == null)
            {
                Game1.activeClickableMenu = new GridWarpMenu(Helper, Monitor, Config);
                Game1.playSound("openBook"); // Changed from "openChest" to "openBook"
            }
        }
        
        // Check if the atlas has been read
        public static bool HasBeenRead()
        {
            return Game1.player.mailReceived.Contains(READ_FLAG);
        }
        
        // Mark the atlas as read
        private void MarkAsRead()
        {
            if (!HasBeenRead())
            {
                Game1.player.mailReceived.Add(READ_FLAG);
                // Show a visual effect to indicate the player gained a permanent ability
                ShowPermanentAbilityEffect();
                // Show message about gaining a permanent ability
                Game1.addHUDMessage(new HUDMessage("You've gained the permanent ability to warp!", HUDMessage.achievement_type));
            }
        }
        
        // Show visual effect for gaining a permanent ability
        private void ShowPermanentAbilityEffect()
        {
            // Play magical sound
            Game1.playSound("discoverMineral");
            
            // Show particles around player
            for (int i = 0; i < 16; i++)
            {
                Vector2 position = Game1.player.Position + new Vector2(
                    Game1.random.Next(-48, 48),
                    Game1.random.Next(-48, 48));
                    
                Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(
                    "TileSheets\\animations",
                    new Rectangle(0, 256, 64, 64), // Magic dust animation
                    position,
                    false,
                    (float)Game1.random.Next(100, 150) / 1000f,
                    new Color(120, 180, 255) // Blue magical color
                )
                {
                    scale = (float)Game1.random.Next(100, 150) / 100f,
                    layerDepth = 1f,
                    delayBeforeAnimationStart = i * 50,
                    motion = new Vector2(
                        (float)Game1.random.Next(-10, 11) / 10f,
                        (float)Game1.random.Next(-10, 11) / 10f
                    )
                });
            }
        }
        
        public override string getDescription()
        {
            return HasBeenRead() ? readDescription : itemDescription;
        }
        
        public override bool canBeDropped()
        {
            return false;
        }
        
        public override bool canBeTrashed()
        {
            return false;
        }
        
        public override string getCategoryName()
        {
            return "Magic";
        }
        
        public override Color getCategoryColor()
        {
            return new Color(120, 100, 255);
        }
        
        public override int maximumStackSize()
        {
            return 1;
        }
        
        public override bool isPlaceable()
        {
            return false;
        }
        
        public override bool canBeGivenAsGift()
        {
            return false;
        }
        
        // Override to draw the item
        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            // Get the item's texture
            Texture2D texture = Helper.ModContent.Load<Texture2D>("assets/items/MagicAtlas.png");
            
            // Draw the item
            spriteBatch.Draw(
                texture,
                location + new Vector2(32f, 32f) * scaleSize,
                new Rectangle(0, 0, 16, 16),
                color * transparency,
                0f,
                new Vector2(8f, 8f),
                2.0f * scaleSize, // Reduced from 2.5f to 2.0f to make it even smaller
                SpriteEffects.None,
                layerDepth
            );
        }
        
        // Method to handle item use - use new implementation instead of overriding
        public new Item getOne()
        {
            return new MagicAtlasItem();
        }
        
        // Use the atlas
        public override bool performUseAction(GameLocation location)
        {
            // First time using the atlas, mark it as read and grant permanent ability
            MarkAsRead();
            
            // Open the warp menu
            OpenWarpMenu();
            
            // Play page turning sound
            Game1.playSound("turnPage");
            
            // Return true to indicate we handled the action
            return true;
        }
    }
}