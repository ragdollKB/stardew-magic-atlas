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
        private List<ClickableComponent> visibleTabButtons = new(); // Tabs currently visible

        // Scrolling state for tabs
        private int tabScrollOffset = 0;
        private int maxVisibleTabs = 0;
        private bool canScrollTabsUp = false;
        private bool canScrollTabsDown = false;
        private ClickableTextureComponent tabScrollUpButton;
        private ClickableTextureComponent tabScrollDownButton;
        private Rectangle tabsArea; // Area where tabs are drawn
        
        // Configuration
        private readonly ModConfig config;
        
        // UI constants
        private const int BUTTON_PADDING = 10;
        private const int SIDEBAR_WIDTH = 300;
        private const int TAB_WIDTH = 130;
        private const int TAB_HEIGHT = 46; // Define tab height constant
        private const int TAB_SPACING = 10; // Define tab spacing constant
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
            
            // Initialize utility classes - Pass the full config object
            this.locationManager = new LocationManager(monitor, this.config);
            this.mapRenderer = new MapRenderer(monitor, helper);
            
            // Play open menu sound and initialize the UI
            Game1.playSound("bigSelect");
            Initialize();
        }
        
        private void Initialize()
        {
            // Define the area available for tabs
            int tabsStartY = yPositionOnScreen + 80;
            int tabsEndY = yPositionOnScreen + height - 40; // Leave space at bottom
            tabsArea = new Rectangle(
                xPositionOnScreen + borderWidth,
                tabsStartY,
                TAB_WIDTH,
                tabsEndY - tabsStartY
            );

            // Calculate how many tabs can fit
            maxVisibleTabs = tabsArea.Height / (TAB_HEIGHT + TAB_SPACING);

            CreateTabs(); // Create all potential tab buttons
            CreateScrollButtons();
            UpdateVisibleTabs(); // Determine which tabs are initially visible
            UpdateLocationButtons(); // Update locations for the default/current tab
        }
        
        private void CreateTabs()
        {
            tabButtons.Clear(); // Clear the main list

            // Get location categories sorted correctly
            var categories = GetSortedCategories();

            // Create a clickable component for each category, even if not initially visible
            int currentY = tabsArea.Y; // Use tabsArea for positioning reference
            foreach (var category in categories)
            {
                // Store bounds relative to the potential full list, not screen position yet
                var bounds = new Rectangle(tabsArea.X, currentY, TAB_WIDTH, TAB_HEIGHT);
                tabButtons.Add(new ClickableComponent(bounds, category));
            }
        }

        /// <summary>Gets categories sorted with standard ones first.</summary>
        private List<string> GetSortedCategories()
        {
            var allCategories = locationManager.GetCategories();
            // Use LocationManager.MOD_LOCATIONS_CATEGORY constant
            string[] orderedTabs = { "Farm", "Town", "Beach", "Mountain", "Forest", "Desert", "Island", LocationManager.MOD_LOCATIONS_CATEGORY }; 

            var sortedList = new List<string>();

            // Add standard tabs in order if they exist
            foreach (var standardTab in orderedTabs)
            {
                // Handle split categories (e.g., "Town", "Town 2")
                var matchingTabs = allCategories
                    .Where(c => c == standardTab || c.StartsWith(standardTab + " "))
                    .OrderBy(c => c.Length).ThenBy(c => c) // Sort "Town", "Town 2", "Town 10"
                    .ToList();
                sortedList.AddRange(matchingTabs);
            }

            // Add any remaining categories not in the standard list
            sortedList.AddRange(allCategories.Where(c => !sortedList.Contains(c)).OrderBy(c => c));

            return sortedList.Distinct().ToList(); // Ensure no duplicates if logic overlaps
        }

        private void CreateScrollButtons()
        {
            tabScrollUpButton = new ClickableTextureComponent(
                new Rectangle(tabsArea.X + (tabsArea.Width - Game1.tileSize) / 2, tabsArea.Y - TAB_HEIGHT / 2 - 5, Game1.tileSize, Game1.tileSize), // Position above tabs
                Game1.mouseCursors,
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 12), // Up arrow
                1f
            );
            tabScrollDownButton = new ClickableTextureComponent(
                new Rectangle(tabsArea.X + (tabsArea.Width - Game1.tileSize) / 2, tabsArea.Y + tabsArea.Height + 5, Game1.tileSize, Game1.tileSize), // Position below tabs
                Game1.mouseCursors,
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 11), // Down arrow
                1f
            );
        }

        /// <summary>Updates which tabs are visible based on the scroll offset.</summary>
        private void UpdateVisibleTabs()
        {
            visibleTabButtons.Clear();
            canScrollTabsUp = tabScrollOffset > 0;
            canScrollTabsDown = tabScrollOffset + maxVisibleTabs < tabButtons.Count;

            int currentY = tabsArea.Y;
            for (int i = 0; i < maxVisibleTabs; i++)
            {
                int tabIndex = tabScrollOffset + i;
                if (tabIndex >= tabButtons.Count) break; // Stop if we run out of tabs

                ClickableComponent tab = tabButtons[tabIndex];
                // Update the bounds to the actual screen position for drawing and clicking
                tab.bounds = new Rectangle(tabsArea.X, currentY, TAB_WIDTH, TAB_HEIGHT);
                visibleTabButtons.Add(tab);
                currentY += TAB_HEIGHT + TAB_SPACING;
            }

            // Ensure the currentTab is still valid, select the first visible if not
            if (!visibleTabButtons.Any(t => t.name == currentTab) && visibleTabButtons.Any())
            {
                currentTab = visibleTabButtons.First().name;
                // Need to update location buttons if the tab changed automatically
                UpdateLocationButtons();
            }
        }

        private void UpdateLocationButtons()
        {
            locationButtons.Clear();

            // Calculate positions
            // Increase horizontal space between tabs and buttons
            int buttonX = xPositionOnScreen + TAB_WIDTH + 40; // Increased from 24 to 40
            int buttonWidth = SIDEBAR_WIDTH - TAB_WIDTH - 48; // Adjust width calculation if needed, maybe keep SIDEBAR_WIDTH relative?
            buttonWidth = xPositionOnScreen + SIDEBAR_WIDTH - buttonX - 16; // Calculate width based on new start X and right padding

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
            
            // Setup map area - Ensure mapX respects the new sidebar spacing
            int mapX = xPositionOnScreen + SIDEBAR_WIDTH + 24; // Keep map relative to the overall sidebar width for now
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
            
            // Draw title - Move slightly higher
            string title = "Magic Atlas";
            Vector2 titleSize = Game1.dialogueFont.MeasureString(title) * TITLE_SCALE;
            Vector2 titlePos = new Vector2(xPositionOnScreen + width / 2 - titleSize.X / 2, yPositionOnScreen + 20); // Changed from 30 to 20
            b.DrawString(Game1.dialogueFont, title, titlePos, Game1.textColor, 0f, Vector2.Zero, TITLE_SCALE, SpriteEffects.None, 1f);
            
            // Draw VISIBLE tabs
            foreach (var tab in visibleTabButtons) // Iterate through visible tabs only
            {
                bool isSelected = tab.name == currentTab;
                DrawTab(b, tab.bounds, tab.name, isSelected, tab.containsPoint(Game1.getMouseX(), Game1.getMouseY()));
            }

            // Draw scroll buttons if needed
            if (canScrollTabsUp)
            {
                tabScrollUpButton.draw(b);
            }
            if (canScrollTabsDown)
            {
                tabScrollDownButton.draw(b);
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
                Color.White * 0.9f); 
            
            // Draw location name above the map - Ensure it doesn't overlap main title
            string locationName = locationManager.GetDisplayName(selectedLocation);
            Vector2 nameSize = Game1.dialogueFont.MeasureString(locationName);
            Vector2 namePos = new Vector2(
                mapContainerX + (mapContainerWidth - nameSize.X) / 2,
                mapContainerY - nameSize.Y - 15 // Increased spacing slightly more (from 12 to 15)
            );
            // Ensure name doesn't go too high and overlap main title (adjust minimum Y if needed)
            namePos.Y = Math.Max(namePos.Y, yPositionOnScreen + 55); // Adjusted minimum Y slightly (from 50 to 55)

            b.DrawString(Game1.dialogueFont, locationName, namePos, Game1.textColor);
            
            // Draw instruction text below the map - Move slightly higher and ensure centering
            string instructionText = "Click on the map to warp to a specific spot";
            Vector2 instructionSize = Game1.smallFont.MeasureString(instructionText);
            Vector2 instructionPos = new Vector2(
                mapContainerX + (mapContainerWidth - instructionSize.X) / 2,
                mapContainerY + mapContainerHeight + 5 // Position below the container, slightly less padding (from 8 to 5)
            );
            // Ensure it doesn't go below the menu box bottom edge
            instructionPos.Y = Math.Min(instructionPos.Y, yPositionOnScreen + height - instructionSize.Y - 15); 

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
            
            // Handle tab clicks (check only visible tabs)
            foreach (var tab in visibleTabButtons)
            {
                if (tab.containsPoint(x, y))
                {
                    if (tab.name != currentTab)
                    {
                        currentTab = tab.name;
                        selectedLocation = null;
                        showingMap = false;
                        currentLocation = null;
                        Game1.playSound("smallSelect");
                        UpdateLocationButtons(); // Update locations for the new tab
                    }
                    return; // Click handled
                }
            }

            // Handle scroll button clicks
            if (canScrollTabsUp && tabScrollUpButton.containsPoint(x, y))
            {
                ScrollTabs(-1);
                Game1.playSound("shwip"); // Sound for scroll
                return;
            }
            if (canScrollTabsDown && tabScrollDownButton.containsPoint(x, y))
            {
                ScrollTabs(1);
                Game1.playSound("shwip");
                return;
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
                relativePos.X = Math.Max(0, relativePos.X);
                relativePos.Y = Math.Max(0, relativePos.Y);

                float normalizedX = mapViewArea.Width > 0 ? (float)relativePos.X / mapViewArea.Width : 0;
                float normalizedY = mapViewArea.Height > 0 ? (float)relativePos.Y / mapViewArea.Height : 0;

                // Convert to game coordinates (ensure map is not null)
                if (currentLocation.map?.Layers?.Count > 0)
                {
                    int tileX = (int)(normalizedX * currentLocation.map.Layers[0].LayerWidth);
                    int tileY = (int)(normalizedY * currentLocation.map.Layers[0].LayerHeight);

                    // Warp to location
                    WarpToLocation(selectedLocation, tileX, tileY);
                }
                else
                {
                    Monitor.Log($"Cannot warp via map click: Map data unavailable for {selectedLocation}.", LogLevel.Warn);
                    Game1.playSound("cancel");
                }
                return; // Click handled
            }
        }

        public override void receiveScrollWheelAction(int direction)
        {
            // Check if mouse is over the tabs area
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            if (tabsArea.Contains(mouseX, mouseY))
            {
                // direction is positive for scroll down, negative for scroll up
                if (direction > 0 && canScrollTabsDown)
                {
                    ScrollTabs(1);
                    Game1.playSound("shiny4"); // Different sound for wheel scroll
                }
                else if (direction < 0 && canScrollTabsUp)
                {
                    ScrollTabs(-1);
                    Game1.playSound("shiny4");
                }
            }
        }

        /// <summary>Scrolls the tab list up or down.</summary>
        /// <param name="amount">Positive to scroll down, negative to scroll up.</param>
        private void ScrollTabs(int amount)
        {
            int newOffset = tabScrollOffset + amount;

            // Clamp the offset
            newOffset = Math.Max(0, newOffset); // Don't go below 0
            // Ensure scrolling stops when the last item is fully visible
            if (tabButtons.Count > maxVisibleTabs) // Only clamp if scrolling is possible
            {
                newOffset = Math.Min(tabButtons.Count - maxVisibleTabs, newOffset); 
            }
            else
            {
                newOffset = 0; // No scrolling needed if all fit
            }

            if (newOffset != tabScrollOffset)
            {
                tabScrollOffset = newOffset;
                UpdateVisibleTabs(); // Update which tabs are shown
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