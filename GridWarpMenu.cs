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

        // UI constants - Adjusted for new design
        private const int CATEGORY_BUTTON_WIDTH = 150; // Wider category buttons
        private const int CATEGORY_BUTTON_HEIGHT = 50;
        private const int CATEGORY_SPACING = 8;
        private const int LOCATION_BUTTON_HEIGHT = 50; // Slightly smaller location buttons
        private const int LOCATION_BUTTON_PADDING = 8;
        private const int SIDEBAR_AREA_WIDTH = 180; // Area reserved for category buttons and padding
        private const int CONTENT_PADDING = 20; // Padding around content areas
        private const float TITLE_SCALE = 1.3f; // Slightly larger title
        private const float CATEGORY_TEXT_SCALE = 1.0f;
        private const float LOCATION_TEXT_SCALE = 0.9f;

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
            // Define the area available for category buttons
            int categoriesStartY = yPositionOnScreen + 70; // Position below title
            int categoriesEndY = yPositionOnScreen + height - 30; // Leave space at bottom for scroll arrow
            tabsArea = new Rectangle( // Renaming 'tabsArea' internally but keeping variable name for simplicity
                xPositionOnScreen + CONTENT_PADDING,
                categoriesStartY,
                CATEGORY_BUTTON_WIDTH,
                categoriesEndY - categoriesStartY
            );

            // Calculate how many category buttons can fit
            maxVisibleTabs = tabsArea.Height / (CATEGORY_BUTTON_HEIGHT + CATEGORY_SPACING); // Use new constants

            CreateTabs(); // Create all potential category buttons
            CreateScrollButtons();
            UpdateVisibleTabs(); // Determine which are initially visible
            UpdateLocationButtons(); // Update locations for the default/current category
        }

        private void CreateTabs() // Renamed internally to CreateCategoryButtons
        {
            tabButtons.Clear(); // Clear the main list

            var categories = GetSortedCategories();

            // Create a clickable component for each category
            // Bounds are initially relative to the potential full list
            foreach (var category in categories)
            {
                // Placeholder bounds, will be updated in UpdateVisibleTabs
                var bounds = new Rectangle(tabsArea.X, 0, CATEGORY_BUTTON_WIDTH, CATEGORY_BUTTON_HEIGHT);
                tabButtons.Add(new ClickableComponent(bounds, category));
            }
        }

        /// <summary>Gets categories sorted with standard ones first.</summary>
        private List<string> GetSortedCategories()
        {
            var allCategories = locationManager.GetCategories();
            string[] orderedTabs = { "Farm", "Town", "Beach", "Mountain", "Forest", "Desert", "Island", LocationManager.MOD_LOCATIONS_CATEGORY };

            var sortedList = new List<string>();

            // Add standard tabs in order if they exist
            foreach (var standardTab in orderedTabs)
            {
                var matchingTabs = allCategories
                    .Where(c => c == standardTab || c.StartsWith(standardTab + " "))
                    .OrderBy(c => c.Length).ThenBy(c => c)
                    .ToList();
                sortedList.AddRange(matchingTabs);
            }

            // Add any remaining categories not in the standard list
            sortedList.AddRange(allCategories.Where(c => !sortedList.Contains(c)).OrderBy(c => c));

            return sortedList.Distinct().ToList(); // Ensure no duplicates if logic overlaps
        }

        private void CreateScrollButtons()
        {
            // Adjust scroll button positions based on the new category button layout
            tabScrollUpButton = new ClickableTextureComponent(
                new Rectangle(tabsArea.X + (tabsArea.Width - Game1.tileSize) / 2, tabsArea.Y - CATEGORY_BUTTON_HEIGHT / 2 - 10, Game1.tileSize, Game1.tileSize), // Position above category area
                Game1.mouseCursors,
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 12), // Up arrow
                1f
            );
            tabScrollDownButton = new ClickableTextureComponent(
                new Rectangle(tabsArea.X + (tabsArea.Width - Game1.tileSize) / 2, tabsArea.Y + tabsArea.Height + 10, Game1.tileSize, Game1.tileSize), // Position below category area
                Game1.mouseCursors,
                Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 11), // Down arrow
                1f
            );
        }

        /// <summary>Updates which category buttons are visible based on the scroll offset.</summary>
        private void UpdateVisibleTabs() // Renamed internally to UpdateVisibleCategoryButtons
        {
            visibleTabButtons.Clear();
            canScrollTabsUp = tabScrollOffset > 0;
            canScrollTabsDown = tabScrollOffset + maxVisibleTabs < tabButtons.Count;

            int currentY = tabsArea.Y;
            for (int i = 0; i < maxVisibleTabs; i++)
            {
                int tabIndex = tabScrollOffset + i;
                if (tabIndex >= tabButtons.Count) break;

                ClickableComponent categoryButton = tabButtons[tabIndex];
                // Update the bounds to the actual screen position
                categoryButton.bounds = new Rectangle(tabsArea.X, currentY, CATEGORY_BUTTON_WIDTH, CATEGORY_BUTTON_HEIGHT);
                visibleTabButtons.Add(categoryButton);
                currentY += CATEGORY_BUTTON_HEIGHT + CATEGORY_SPACING;
            }

            // Ensure the currentTab (category) is still valid
            if (!visibleTabButtons.Any(t => t.name == currentTab) && visibleTabButtons.Any())
            {
                currentTab = visibleTabButtons.First().name;
                UpdateLocationButtons(); // Update locations if category changed automatically
            }
        }

        private void UpdateLocationButtons()
        {
            locationButtons.Clear();

            // --- Layout Calculation ---
            int sidebarWidth = SIDEBAR_AREA_WIDTH + CONTENT_PADDING; // Width of the category sidebar area
            int availableContentWidth = width - sidebarWidth - CONTENT_PADDING * 2; // Total width for buttons + map

            // Prioritize map width: Allocate a larger portion (e.g., 60-65%) of the available width to the map
            int mapAreaMinWidth = 400; // Minimum width for the map area
            int mapAreaWidth = Math.Max(mapAreaMinWidth, (int)(availableContentWidth * 0.65)); // Give map ~65% of space

            // Calculate button width based on remaining space
            int buttonAreaWidth = availableContentWidth - mapAreaWidth - CONTENT_PADDING; // Subtract map width and padding between buttons/map
            int buttonWidth = Math.Max(150, buttonAreaWidth); // Ensure a minimum button width

            // Define starting positions
            int buttonStartX = xPositionOnScreen + sidebarWidth;
            int mapX = buttonStartX + buttonWidth + CONTENT_PADDING; // Map starts after buttons and padding

            int buttonY = yPositionOnScreen + 70; // Align with top of category buttons
            int mapY = buttonY; // Align map top with button top
            int mapHeight = height - 100; // Map height (leave space for title/instructions)

            // --- Location Buttons ---
            var locations = locationManager.GetLocationsInCategory(currentTab);
            if (locations != null && locations.Any())
            {
                int count = 0;
                int maxLocationsToShow = config.MaxLocationsPerCategory;

                foreach (var location in locations.Take(maxLocationsToShow))
                {
                    var bounds = new Rectangle(
                        buttonStartX, // Use calculated start X
                        buttonY + (count * (LOCATION_BUTTON_HEIGHT + LOCATION_BUTTON_PADDING)),
                        buttonWidth,  // Use calculated button width
                        LOCATION_BUTTON_HEIGHT
                    );

                    locationButtons.Add(new ClickableComponent(bounds, location));
                    count++;
                }
            }

            // --- Map Area Calculation ---
            // Define the container for the map rendering (slightly smaller than the allocated map area)
            Rectangle innerMapBounds = new Rectangle(mapX + 10, mapY + 10, mapAreaWidth - 20, mapHeight - 20);

            if (showingMap && currentLocation?.Map != null)
            {
                // Calculate the final scaled map view area using the renderer
                mapViewArea = mapRenderer.CalculateMapViewArea(
                    currentLocation.Map.DisplayWidth,
                    currentLocation.Map.DisplayHeight,
                    innerMapBounds // Use the calculated inner bounds
                );
            }
            else
            {
                // Default map area when no map is shown (for instructions)
                mapViewArea = innerMapBounds; // Use the inner bounds for the placeholder too
            }
        }

        public override void draw(SpriteBatch b)
        {
            // Draw a darker, slightly textured background for the "atlas" feel
            DrawMenuBackground(b); // New method for background

            // Title is now drawn conditionally within DrawMapAreaContent or DrawInstructions

            // Draw VISIBLE category buttons
            foreach (var categoryButton in visibleTabButtons)
            {
                bool isSelected = categoryButton.name == currentTab;
                DrawCategoryButton(b, categoryButton.bounds, categoryButton.name, isSelected, categoryButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()));
            }

            // Draw scroll buttons if needed
            if (canScrollTabsUp) tabScrollUpButton.draw(b, Color.White * (tabScrollUpButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ? 1f : 0.7f), 0.9f);
            if (canScrollTabsDown) tabScrollDownButton.draw(b, Color.White * (tabScrollDownButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ? 1f : 0.7f), 0.9f);

            // Draw location buttons
            foreach (var button in locationButtons)
            {
                bool isSelected = button.name == selectedLocation;
                DrawLocationButton(b, button.bounds, locationManager.GetDisplayName(button.name),
                    isSelected, button.containsPoint(Game1.getMouseX(), Game1.getMouseY()));
            }

            // Draw map or instructions in the map area (this will handle title drawing)
            DrawMapAreaContent(b);

            // Draw cursor
            drawMouse(b);
        }

        // New method to draw the menu background
        private void DrawMenuBackground(SpriteBatch b)
        {
            // Draw a simple dark semi-transparent background first
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);

            // Draw the main menu box using a slightly darker/textured look if possible
            // Using the standard texture box for now, but with a darker tint
            Color menuBackgroundColor = new Color(50, 40, 30) * 0.95f; // Dark brown tint
            drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                xPositionOnScreen, yPositionOnScreen, width, height, menuBackgroundColor, 1f, true); // Use drawShadow = true
        }

        // Replaces DrawTab
        private void DrawCategoryButton(SpriteBatch b, Rectangle bounds, string text, bool selected, bool hovered)
        {
            // Use a simpler, darker style for category buttons
            Color baseColor = selected ? new Color(180, 150, 100) : (hovered ? new Color(100, 80, 60) : new Color(80, 60, 40)); // Earthy tones
            Color textColor = selected ? Color.Wheat : (hovered ? Color.White * 0.9f : Color.Tan * 0.8f);
            float transparency = 1f; // No extra transparency needed

            // Draw button background (simple box)
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(384, 373, 18, 18), // A simple square texture piece
                bounds.X, bounds.Y, bounds.Width, bounds.Height,
                baseColor * transparency, 4f, false);

            // Draw text - centered
            float scale = CATEGORY_TEXT_SCALE;
            Vector2 textSize = Game1.dialogueFont.MeasureString(text) * scale; // Use dialogueFont for categories

            // Adjust scale if text doesn't fit
            if (textSize.X > bounds.Width - 16) // 8px padding
            {
                scale *= (bounds.Width - 16) / textSize.X;
                textSize = Game1.dialogueFont.MeasureString(text) * scale;
            }

            Vector2 textPos = new Vector2(
                bounds.X + (bounds.Width - textSize.X) / 2,
                bounds.Y + (bounds.Height - textSize.Y) / 2
            );

            b.DrawString(Game1.dialogueFont, text, textPos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);

            // Add a subtle selection indicator (e.g., underline or side bar)
            if (selected)
            {
                Rectangle indicatorRect = new Rectangle(bounds.X - 5, bounds.Y + 4, 4, bounds.Height - 8); // Left side bar
                b.Draw(Game1.staminaRect, indicatorRect, Color.Wheat * 0.8f);
            }
        }

        // Replaces DrawButton
        private void DrawLocationButton(SpriteBatch b, Rectangle bounds, string text, bool selected, bool hovered)
        {
            // Use a very subtle background for location buttons
            Color baseColor = hovered ? new Color(90, 80, 70) * 0.7f : Color.Transparent; // Slightly more visible hover
            // Use brighter text colors overall - Ensure selected is bright (Yellow)
            Color textColor = selected ? Color.Yellow : (hovered ? Color.White : Color.Wheat * 0.9f); // Bright yellow for selected, White for hover, Wheat for default
            float scale = LOCATION_TEXT_SCALE;

            // Draw subtle background on hover
            if (hovered)
            {
                b.Draw(Game1.staminaRect, bounds, baseColor);
            }

            // Draw selection indicator (more prominent background)
            if (selected)
            {
                // Use a slightly lighter brown for selected background
                b.Draw(Game1.staminaRect, bounds, new Color(110, 100, 80) * 0.8f);
            }

            // Draw button text - vertically centered, left-aligned with padding
            Vector2 textSize = Game1.smallFont.MeasureString(text) * scale;
            // Adjust scale if text is too wide
            if (textSize.X > bounds.Width - 16) // 8px padding each side
            {
                scale *= (bounds.Width - 16) / textSize.X;
                textSize = Game1.smallFont.MeasureString(text) * scale;
            }
            Vector2 textPos = new Vector2(
                bounds.X + 8, // Left padding
                bounds.Y + (bounds.Height - textSize.Y) / 2
            );

            // Draw text with shadow for better contrast if needed (optional)
            // Utility.drawTextWithShadow(b, text, Game1.smallFont, textPos, textColor, scale); // Example using shadow
            b.DrawString(Game1.smallFont, text, textPos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f); // Drawing without shadow for now
        }

        // Renamed DrawMap to DrawMapAreaContent for clarity
        private void DrawMapAreaContent(SpriteBatch b)
        {
            // Draw a subtle container for the map/instructions area
            Rectangle mapContainerBounds = new Rectangle(
                mapViewArea.X - 10, // Adjust based on innerMapBounds padding used in UpdateLocationButtons
                mapViewArea.Y - 10,
                mapViewArea.Width + 20,
                mapViewArea.Height + 20
            );
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                mapContainerBounds.X, mapContainerBounds.Y, mapContainerBounds.Width, mapContainerBounds.Height,
                new Color(40, 30, 20) * 0.8f, 1f, false); // Darker, slightly transparent box

            if (showingMap && selectedLocation != null && currentLocation != null)
            {
                // Draw location name above the map (centered within the main menu width)
                string locationName = locationManager.GetDisplayName(selectedLocation);
                Vector2 nameSize = Game1.dialogueFont.MeasureString(locationName) * TITLE_SCALE; // Use title scale
                // Center based on the overall menu width for consistency
                Vector2 namePos = new Vector2(
                    xPositionOnScreen + width / 2 - nameSize.X / 2,
                    yPositionOnScreen + 25 // Same Y position as the original title
                );
                b.DrawString(Game1.dialogueFont, locationName, namePos, Color.Wheat, 0f, Vector2.Zero, TITLE_SCALE, SpriteEffects.None, 1f); // Use a thematic color and scale

                // Draw the map itself within the calculated mapViewArea
                mapRenderer.DrawMapThumbnail(b, currentLocation, mapViewArea);

                // Draw instruction text below the map
                string instructionText = "Click map to warp to a specific spot";
                Vector2 instructionSize = Game1.smallFont.MeasureString(instructionText);
                Vector2 instructionPos = new Vector2(
                    mapContainerBounds.X + (mapContainerBounds.Width - instructionSize.X) / 2,
                    mapContainerBounds.Y + mapContainerBounds.Height + 5 // Position below the container
                );
                b.DrawString(Game1.smallFont, instructionText, instructionPos, Game1.textColor * 0.7f);
            }
            else
            {
                // Draw placeholder instructions and the open atlas image when no map is shown
                DrawInstructionsAndTitle(b, mapContainerBounds);
            }
        }

        // Updated DrawInstructions to accept bounds and draw title conditionally
        private void DrawInstructionsAndTitle(SpriteBatch b, Rectangle containerBounds)
        {
            try
            {
                // Load the open atlas texture
                Texture2D openAtlasTexture = Helper.ModContent.Load<Texture2D>("assets/items/atlas_sprite_open.png");
                
                // Calculate a scaling factor to fit the image within the container, preserving aspect ratio
                // Leave some margins (20 pixels on each side)
                float availableWidth = containerBounds.Width - 40; 
                float availableHeight = containerBounds.Height - 80; // Extra space at bottom for text
                
                float scaleWidth = availableWidth / openAtlasTexture.Width;
                float scaleHeight = availableHeight / openAtlasTexture.Height;
                
                // Use the smaller scale to ensure the image fits in both dimensions
                float scale = Math.Min(scaleWidth, scaleHeight);
                
                // Calculate centered position
                Vector2 imagePosition = new Vector2(
                    containerBounds.X + (containerBounds.Width - (openAtlasTexture.Width * scale)) / 2,
                    containerBounds.Y + (containerBounds.Height - (openAtlasTexture.Height * scale) - 40) / 2 // Offset up a bit for text below
                );
                
                // Draw the open atlas image scaled to fit the container
                b.Draw(
                    openAtlasTexture,
                    imagePosition,
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    1f
                );
                
                // Draw instructions at the bottom with more space
                // No "select a button" instruction - just the image speaks for itself
                string instruction = "Choose a destination";
                Vector2 textSize = Game1.dialogueFont.MeasureString(instruction);
                Vector2 position = new Vector2(
                    containerBounds.X + (containerBounds.Width - textSize.X) / 2,
                    containerBounds.Y + containerBounds.Height - textSize.Y - 30  // More padding at bottom
                );
                
                b.DrawString(Game1.dialogueFont, instruction, position, Game1.textColor * 0.8f);
            }
            catch (Exception ex)
            {
                // Fallback to text-only if image fails to load
                Monitor?.Log($"Error loading open atlas image: {ex.Message}", LogLevel.Error);
                
                // Draw instructions centered in the container
                string instruction = "Choose a destination";
                Vector2 textSize = Game1.dialogueFont.MeasureString(instruction);
                Vector2 position = new Vector2(
                    containerBounds.X + (containerBounds.Width - textSize.X) / 2,
                    containerBounds.Y + (containerBounds.Height - textSize.Y) / 2
                );
                
                b.DrawString(Game1.dialogueFont, instruction, position, Game1.textColor * 0.6f); // Dimmer text
            }
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

            // Handle category button clicks (check only visible buttons)
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
                        UpdateLocationButtons(); // Update locations for the new category
                    }
                    return; // Make sure to return here regardless if same or different tab
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
                        Game1.playSound("crystal"); // Changed sound for selecting an item (was dwop)
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

                // Warp to location
                if (locationManager.WarpToLocation(locationName, tileX, tileY))
                {
                    // Play sound
                    Game1.playSound("wand");
                    Monitor.Log($"Warped to {locationName} at ({tileX}, {tileY})", LogLevel.Debug);

                    // Exit menu after warping - no more continue dialog
                    exitThisMenu(playSound: false);
                }
                else
                {
                    Monitor.Log($"Failed to warp to {locationName}", LogLevel.Warn);
                    Game1.addHUDMessage(new HUDMessage("Failed to warp to location", HUDMessage.error_type));
                    exitThisMenu(playSound: false);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error warping to location: {ex.Message}", LogLevel.Error);
                Game1.addHUDMessage(new HUDMessage("Error warping to location", HUDMessage.error_type));
                exitThisMenu(playSound: false);
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