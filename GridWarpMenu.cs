using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using WarpMod.Utility;

namespace WarpMod
{
    /// <summary>
    /// Menu that displays available warp locations organized by category
    /// </summary>
    public class GridWarpMenu : IClickableMenu
    {
        // Core dependencies
        private readonly IMonitor Monitor;
        private readonly IModHelper Helper;
        private readonly LocationManager locationManager;
        private readonly MapRenderer mapRenderer;
        
        // UI state
        private readonly List<ClickableComponent> locationButtons = new();
        private string selectedLocation = null;
        private GameLocation currentLocation = null;
        private Rectangle mapViewArea;
        private bool showingMap = false;
        private string currentTab = "Town";
        private readonly List<ClickableComponent> tabButtons = new();
        
        // Configuration
        private readonly ModConfig config;
        
        // UI constants
        private const int BUTTON_PADDING = 10;
        private const int SIDEBAR_WIDTH = 300;
        private const int TAB_WIDTH = 130;
        private const float TITLE_SCALE = 1.2f;
        private const float BUTTON_TEXT_SCALE = 0.9f;
        
        public GridWarpMenu(IModHelper helper, IMonitor monitor, ModConfig config = null)
            : base(Game1.viewport.Width / 2 - 600,
                  Game1.viewport.Height / 2 - 350,
                  1200, 
                  700,
                  true)
        {
            this.Monitor = monitor;
            this.Helper = helper;
            this.config = config ?? new ModConfig();
            
            // Initialize utility classes
            this.locationManager = new LocationManager(monitor, this.config.ShowSVELocations);
            this.mapRenderer = new MapRenderer(monitor, helper);
            
            // Play open menu sound and initialize the UI
            Game1.playSound("bigSelect");
            Initialize();
        }
        
        private void Initialize()
        {
            CreateTabs();
            UpdateLocationButtons();
        }
        
        private void CreateTabs()
        {
            // Clear any existing tabs
            tabButtons.Clear();
            
            // Set position variables
            int tabX = xPositionOnScreen + borderWidth;
            int tabY = yPositionOnScreen + 80;
            int tabHeight = 46;
            int tabSpacing = 10;
            
            // Get location categories
            var categories = locationManager.GetCategories().ToList();
            
            // Standard tab order for common locations
            string[] orderedTabs = { "Town", "Farm", "Beach", "Mountain", "Forest", "Desert", "Island" };
            
            // Add standard tabs first
            foreach (var category in orderedTabs)
            {
                if (categories.Contains(category))
                {
                    var bounds = new Rectangle(tabX, tabY, TAB_WIDTH, tabHeight);
                    tabButtons.Add(new ClickableComponent(bounds, category));
                    tabY += tabHeight + tabSpacing;
                }
            }
            
            // Add any remaining tabs
            foreach (var category in categories)
            {
                if (!orderedTabs.Contains(category))
                {
                    var bounds = new Rectangle(tabX, tabY, TAB_WIDTH, tabHeight);
                    tabButtons.Add(new ClickableComponent(bounds, category));
                    tabY += tabHeight + tabSpacing;
                }
            }
        }

        private void UpdateLocationButtons()
        {
            locationButtons.Clear();

            // Calculate positions
            int buttonX = xPositionOnScreen + TAB_WIDTH + 24;
            int buttonWidth = SIDEBAR_WIDTH - TAB_WIDTH - 32;
            int buttonHeight = 55;
            int buttonY = yPositionOnScreen + 75;
            
            // Show locations for current tab
            var locations = locationManager.GetLocationsInCategory(currentTab);
            if (locations != null && locations.Any())
            {
                int count = 0;
                int maxLocationsToShow = config.MaxLocationsPerCategory;
                
                foreach (var location in locations.Take(maxLocationsToShow))
                {
                    var bounds = new Rectangle(
                        buttonX,
                        buttonY + (count * (buttonHeight + BUTTON_PADDING)),
                        buttonWidth,
                        buttonHeight
                    );
                    
                    locationButtons.Add(new ClickableComponent(bounds, location));
                    count++;
                }
            }
            
            // Setup map area
            int mapX = xPositionOnScreen + SIDEBAR_WIDTH + 24;
            int mapY = yPositionOnScreen + 75;
            int mapWidth = width - SIDEBAR_WIDTH - 48;
            int mapHeight = height - 110;
            
            mapViewArea = new Rectangle(mapX, mapY, mapWidth, mapHeight);

            // Calculate map dimensions for selected location
            if (showingMap && currentLocation?.Map != null)
            {
                // Use a slightly smaller area for the map itself to allow for padding/border
                Rectangle innerMapBounds = new Rectangle(mapX + 10, mapY + 10, mapWidth - 20, mapHeight - 20);
                mapViewArea = mapRenderer.CalculateMapViewArea(
                    currentLocation.Map.DisplayWidth, 
                    currentLocation.Map.DisplayHeight,
                    innerMapBounds
                );
            }
        }

        public override void draw(SpriteBatch b)
        {
            // Draw semi-transparent background
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.7f);
            DrawMenuBox(b);
            
            // Draw title - Larger and centered
            string title = "Magic Atlas";
            Vector2 titleSize = Game1.dialogueFont.MeasureString(title) * TITLE_SCALE;
            Vector2 titlePos = new Vector2(xPositionOnScreen + width / 2 - titleSize.X / 2, yPositionOnScreen + 30);
            b.DrawString(Game1.dialogueFont, title, titlePos, Game1.textColor, 0f, Vector2.Zero, TITLE_SCALE, SpriteEffects.None, 1f);
            
            // Draw tabs
            foreach (var tab in tabButtons)
            {
                bool isSelected = tab.name == currentTab;
                DrawTab(b, tab.bounds, tab.name, isSelected, tab.containsPoint(Game1.getMouseX(), Game1.getMouseY()));
            }
            
            // Draw location buttons
            foreach (var button in locationButtons)
            {
                bool isSelected = button.name == selectedLocation;
                DrawButton(b, button.bounds, locationManager.GetDisplayName(button.name), 
                    isSelected, button.containsPoint(Game1.getMouseX(), Game1.getMouseY()));
            }
            
            // Draw map or instructions
            if (showingMap && selectedLocation != null && currentLocation != null)
            {
                DrawMap(b);
            }
            else
            {
                DrawInstructions(b);
            }
            
            // Draw cursor
            drawMouse(b);
        }
        
        private void DrawMenuBox(SpriteBatch b)
        {
            drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                xPositionOnScreen, yPositionOnScreen, width, height, Color.White);
        }
        
        private void DrawTab(SpriteBatch b, Rectangle bounds, string text, bool selected, bool hovered)
        {
            // Use standard Stardew Valley tab textures/colors
            Color baseColor = selected ? Color.White : Color.Gray;
            Color textColor = selected ? Game1.textColor : Color.DarkGray;
            float transparency = hovered ? 1f : 0.85f;

            // Draw tab background
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(16, 368, 16, 16), 
                bounds.X, bounds.Y, bounds.Width, bounds.Height, 
                baseColor * transparency, 4f, false);

            // Draw tab text - centered
            Vector2 textSize = Game1.smallFont.MeasureString(text);
            Vector2 textPos = new Vector2(
                bounds.X + (bounds.Width - textSize.X) / 2,
                bounds.Y + (bounds.Height - textSize.Y) / 2
            );
            
            b.DrawString(Game1.smallFont, text, textPos, textColor * transparency);
        }
        
        private void DrawButton(SpriteBatch b, Rectangle bounds, string text, bool selected, bool hovered)
        {
            // Use standard button textures/colors
            Color baseColor = Color.White;
            Color textColor = Game1.textColor;
            float transparency = hovered ? 1f : 0.9f;
            float scale = BUTTON_TEXT_SCALE;

            // Draw button background
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(432, 439, 9, 9), 
                bounds.X, bounds.Y, bounds.Width, bounds.Height, 
                baseColor * transparency, 4f, false);

            // Draw selection indicator if selected
            if (selected)
            {
                IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(375, 357, 3, 3), 
                    bounds.X + 4, bounds.Y + 4, bounds.Width - 8, bounds.Height - 8, 
                    Color.White * 0.7f, 4f, false);
            }

            // Draw button text - vertically centered, left-aligned with padding
            Vector2 textSize = Game1.smallFont.MeasureString(text) * scale;
            // Adjust scale if text is too wide
            if (textSize.X > bounds.Width - 24) // 12px padding on each side
            {
                scale *= (bounds.Width - 24) / textSize.X;
                textSize = Game1.smallFont.MeasureString(text) * scale;
            }
            Vector2 textPos = new Vector2(
                bounds.X + 12, // Left padding
                bounds.Y + (bounds.Height - textSize.Y) / 2
            );
            
            b.DrawString(Game1.smallFont, text, textPos, textColor * transparency, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
        }
        
        private void DrawMap(SpriteBatch b)
        {   
            // Draw map container box
            int mapContainerX = xPositionOnScreen + SIDEBAR_WIDTH + 24;
            int mapContainerY = yPositionOnScreen + 75;
            int mapContainerWidth = width - SIDEBAR_WIDTH - 48;
            int mapContainerHeight = height - 110;
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                mapContainerX, mapContainerY, mapContainerWidth, mapContainerHeight, 
                Color.White * 0.9f); // Slightly transparent background
            
            // Draw location name above the map
            string locationName = locationManager.GetDisplayName(selectedLocation);
            Vector2 nameSize = Game1.dialogueFont.MeasureString(locationName);
            Vector2 namePos = new Vector2(
                mapContainerX + (mapContainerWidth - nameSize.X) / 2,
                mapContainerY - nameSize.Y - 8 // Position above the container
            );
            b.DrawString(Game1.dialogueFont, locationName, namePos, Game1.textColor);
            
            // Draw instruction text below the map - More specific
            string instructionText = "Click on the map to warp to a specific spot";
            Vector2 instructionSize = Game1.smallFont.MeasureString(instructionText);
            Vector2 instructionPos = new Vector2(
                mapContainerX + (mapContainerWidth - instructionSize.X) / 2,
                mapContainerY + mapContainerHeight + 8 // Position below the container
            );
            b.DrawString(Game1.smallFont, instructionText, instructionPos, Game1.textColor * 0.8f);
            
            // Draw the map itself within the calculated mapViewArea
            mapRenderer.DrawMapThumbnail(b, currentLocation, mapViewArea);
        }
        
        private void DrawInstructions(SpriteBatch b)
        {
            // Draw instruction text centered in the map area
            string instruction = "Select a location";
            Vector2 textSize = Game1.dialogueFont.MeasureString(instruction);
            // Use the map container bounds for centering
            int mapContainerX = xPositionOnScreen + SIDEBAR_WIDTH + 24;
            int mapContainerY = yPositionOnScreen + 75;
            int mapContainerWidth = width - SIDEBAR_WIDTH - 48;
            int mapContainerHeight = height - 110;
            Vector2 position = new Vector2(
                mapContainerX + (mapContainerWidth - textSize.X) / 2,
                mapContainerY + (mapContainerHeight - textSize.Y) / 2
            );
            
            b.DrawString(Game1.dialogueFont, instruction, position, Game1.textColor * 0.8f);
        }
        
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // Handle outside click to close menu
            if (!isWithinBounds(x, y))
            {
                exitThisMenu();
                Game1.playSound("bigDeSelect");
                return;
            }
            
            // Handle tab clicks
            foreach (var tab in tabButtons)
            {
                if (tab.containsPoint(x, y))
                {
                    if (tab.name != currentTab)
                    {
                        currentTab = tab.name;
                        selectedLocation = null;
                        showingMap = false;
                        currentLocation = null;
                        Game1.playSound("smallSelect"); // Standard UI sound
                        UpdateLocationButtons();
                    }
                    return;
                }
            }
            
            // Handle location button clicks
            foreach (var button in locationButtons)
            {
                if (button.containsPoint(x, y))
                {
                    selectedLocation = button.name;
                    showingMap = true;
                    currentLocation = locationManager.LoadLocation(selectedLocation);
                    
                    if (currentLocation != null)
                    {
                        UpdateLocationButtons(); // Recalculate map area
                        Game1.playSound("dwop"); // Changed sound for selecting an item (was drumkit6)
                    }
                    else
                    {
                        selectedLocation = null;
                        showingMap = false;
                        Game1.playSound("cancel");
                        Game1.addHUDMessage(new HUDMessage("Unable to load location", HUDMessage.error_type));
                    }
                    return;
                }
            }
            
            // Handle map click for warping
            if (showingMap && mapViewArea.Contains(x, y) && selectedLocation != null && currentLocation != null)
            {
                // Calculate normalized position within map
                Point relativePos = new Point(x - mapViewArea.X, y - mapViewArea.Y);
                // Ensure relativePos is not negative if click is on the border
                relativePos.X = Math.Max(0, relativePos.X);
                relativePos.Y = Math.Max(0, relativePos.Y);

                float normalizedX = mapViewArea.Width > 0 ? relativePos.X / (float)mapViewArea.Width : 0;
                float normalizedY = mapViewArea.Height > 0 ? relativePos.Y / (float)mapViewArea.Height : 0;
                
                // Convert to game coordinates
                int tileX = (int)(normalizedX * currentLocation.map.Layers[0].LayerWidth);
                int tileY = (int)(normalizedY * currentLocation.map.Layers[0].LayerHeight);
                
                // Warp to location
                WarpToLocation(selectedLocation, tileX, tileY);
                return;
            }
        }
        
        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            // Right click exits the menu
            if (playSound)
                Game1.playSound("bigDeSelect");
            exitThisMenu();
        }
        
        private void WarpToLocation(string locationName, int tileX, int tileY)
        {
            try
            {
                // Add warp effect if configured
                if (config.UseWarpEffects)
                {
                    CreateWarpEffect();
                }
                
                // Close menu
                exitThisMenu(playSound: false);
                
                // Warp to location
                if (locationManager.WarpToLocation(locationName, tileX, tileY))
                {
                    // Play sound
                    Game1.playSound("wand");
                    Monitor.Log($"Warped to {locationName} at ({tileX}, {tileY})", LogLevel.Debug);
                }
                else
                {
                    Monitor.Log($"Failed to warp to {locationName}", LogLevel.Warn);
                    Game1.addHUDMessage(new HUDMessage("Failed to warp to location", HUDMessage.error_type));
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error warping to location: {ex.Message}", LogLevel.Error);
                Game1.addHUDMessage(new HUDMessage("Error warping to location", HUDMessage.error_type));
            }
        }
        
        /// <summary>
        /// Create visual effect when warping
        /// </summary>
        private void CreateWarpEffect()
        {
            for (int i = 0; i < 12; i++)
            {
                Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(
                    textureName: "LooseSprites\\Cursors",
                    sourceRect: new Rectangle(294, 185, 10, 10),
                    animationInterval: 50f,
                    animationLength: 8,
                    numberOfLoops: 1,
                    position: Game1.player.Position + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-32, 32)),
                    flicker: false,
                    flipped: false
                )
                {
                    scale = 2f,
                    layerDepth = 1f,
                    motion = new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2))
                });
            }
        }
    }
}