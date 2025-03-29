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
    public class GridWarpMenu : IClickableMenu
    {
        // Core dependencies
        private readonly IMonitor Monitor;
        private readonly IModHelper Helper;
        
        // Utility services
        private readonly LocationManager locationManager;
        private readonly MapRenderer mapRenderer;
        private readonly MenuStyles menuStyles;
        
        // UI state
        private readonly List<ClickableComponent> locationButtons = new();
        private string selectedLocation = null;
        private GameLocation currentLocation = null;
        private Rectangle mapViewArea;
        private bool showingMap = false;
        private string currentTab = "Town";
        private readonly List<ClickableComponent> tabButtons = new();
        private readonly Dictionary<string, Vector2> buttonSizes = new();
        
        // Map interaction tracking
        private Point? lastClickedPosition = null;
        private float clickAnimationTimer = 0f;
        
        // Search functionality
        private const string SEARCH_PLACEHOLDER = "Search locations...";
        private string searchText = "";
        private Rectangle searchBoxRect;
        private bool searchFocused = false;
        private List<string> originalTabLocations = new List<string>();
        
        // User configuration
        private readonly ModConfig config;
        
        // UI layout constants
        private const int BUTTON_PADDING = 12;
        private const int BUTTONS_PER_ROW = 1; // Only one button per row for better text display
        private const string MOD_LOCATIONS_CATEGORY = "Mod Locations";
        private int MAX_BUTTONS_TO_SHOW => config?.MaxLocationsPerCategory ?? 8;

        // Left sidebar width
        private const int LEFT_SIDEBAR_WIDTH = 280;
        private const int TAB_WIDTH = 120;
        
        // Background texture
        private Texture2D backgroundTexture;
        
        public GridWarpMenu(IModHelper helper, IMonitor monitor, ModConfig config = null)
            : base(Game1.viewport.Width / 2 - (1032) / 2,
                  Game1.viewport.Height / 2 - (664) / 2,
                  1032, 
                  664,
                  true)
        {
            this.Monitor = monitor;
            this.Helper = helper;
            
            // Use provided config or default to a new one if null
            this.config = config ?? new ModConfig();
            
            // Initialize utility classes
            this.locationManager = new LocationManager(monitor, this.config.ShowSVELocations);
            this.mapRenderer = new MapRenderer(monitor, helper);
            this.menuStyles = new MenuStyles(new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));
            
            // Load background texture from the game's content
            try {
                // Use the game's content manager to load the texture
                backgroundTexture = Helper.GameContent.Load<Texture2D>("LooseSprites\\Cloudy_Ocean_BG_Night");
                Monitor.Log($"Loaded custom background texture: Cloudy_Ocean_BG_Night", LogLevel.Debug);
            }
            catch (Exception ex) {
                // Fallback to using a standard game texture that we know exists
                backgroundTexture = Game1.menuTexture;
                Monitor.Log($"Failed to load specified background texture. Using default instead. Error: {ex.Message}", LogLevel.Warn);
            }
            
            // Play open menu sound
            Game1.playSound("bigSelect");
            
            Initialize();
        }
        
        private void Initialize()
        {
            // Pre-calculate button sizes for all locations with wider buttons
            foreach (var category in locationManager.GetCategories())
            {
                foreach (var location in locationManager.GetLocationsInCategory(category))
                {
                    if (!buttonSizes.ContainsKey(location))
                    {
                        string displayName = locationManager.GetDisplayName(location);
                        Vector2 textSize = Game1.dialogueFont.MeasureString(displayName) * MenuStyles.BUTTON_TEXT_SCALE;

                        // Calculate button size based on text with appropriate width for sidebar
                        int buttonWidth = (int)Math.Min(LEFT_SIDEBAR_WIDTH - 40,
                                            Math.Max(MenuStyles.MIN_BUTTON_WIDTH, textSize.X + 40));
                        int buttonHeight = (int)Math.Max(MenuStyles.MIN_BUTTON_HEIGHT, textSize.Y + 20);

                        buttonSizes[location] = new Vector2(buttonWidth, buttonHeight);
                    }
                }
            }

            InitializeTabs();
            UpdateLocationButtons();
            
            // Cache commonly used maps for better performance
            PrecacheMapThumbnails();
        }
        
        private void InitializeTabs()
        {
            // Create tab buttons vertically on the left side
            int tabX = xPositionOnScreen + borderWidth;
            int tabY = yPositionOnScreen + MenuStyles.TITLE_HEIGHT + 10;
            int tabHeight = 42;
            
            foreach (var tab in locationManager.GetCategories())
            {
                var tabBounds = new Rectangle(tabX, tabY, TAB_WIDTH, tabHeight);
                tabButtons.Add(new ClickableComponent(tabBounds, tab));
                tabY += tabHeight + 2; // Small gap between tabs
            }
        }

        private void UpdateLocationButtons()
        {
            locationButtons.Clear();

            var locations = locationManager.GetLocationsInCategory(currentTab);
            if (locations == null || !locations.Any())
                return;
                
            // Get only the first MAX_BUTTONS_TO_SHOW locations to avoid cluttering
            locations = locations.Take(MAX_BUTTONS_TO_SHOW).ToList();
            
            // Calculate position for the location buttons
            // Start buttons to the right of tabs but still in the sidebar
            int buttonX = xPositionOnScreen + TAB_WIDTH + 20;
            int buttonWidth = LEFT_SIDEBAR_WIDTH - TAB_WIDTH - 30;
            int buttonHeight = MenuStyles.MIN_BUTTON_HEIGHT;
            
            // Position buttons at top area, below title
            int buttonY = yPositionOnScreen + MenuStyles.TITLE_HEIGHT + 10;

            int count = 0;
            foreach (var location in locations)
            {
                // Get the text size to ensure button is tall enough
                string displayName = locationManager.GetDisplayName(location);
                Vector2 textSize = Game1.dialogueFont.MeasureString(displayName) * MenuStyles.BUTTON_TEXT_SCALE;
                
                // Make sure button height accommodates the text
                int thisButtonHeight = Math.Max(buttonHeight, (int)(textSize.Y + 20));
                
                var bounds = new Rectangle(
                    buttonX,
                    buttonY + (count * (thisButtonHeight + BUTTON_PADDING)),
                    buttonWidth,
                    thisButtonHeight
                );

                locationButtons.Add(new ClickableComponent(bounds, location));
                count++;
            }

            // Position map to the right of the sidebar
            int mapX = xPositionOnScreen + LEFT_SIDEBAR_WIDTH + 20;
            int mapY = yPositionOnScreen + MenuStyles.TITLE_HEIGHT + 20;
            int mapWidth = width - LEFT_SIDEBAR_WIDTH - 40;
            int mapHeight = height - MenuStyles.TITLE_HEIGHT - 40;

            // Create the map view area on the right side of the menu
            mapViewArea = new Rectangle(mapX, mapY, mapWidth, mapHeight);

            // If we have a selected location, calculate map dimensions based on its actual size
            if (showingMap && currentLocation?.Map != null)
            {
                mapViewArea = mapRenderer.CalculateMapViewArea(
                    currentLocation.Map.DisplayWidth, 
                    currentLocation.Map.DisplayHeight,
                    new Rectangle(mapX, mapY, mapWidth, mapHeight)
                );
            }
            
            // Position search box if needed
            if (currentTab == MOD_LOCATIONS_CATEGORY)
            {
                searchBoxRect = new Rectangle(
                    xPositionOnScreen + TAB_WIDTH + 20,
                    yPositionOnScreen + MenuStyles.TITLE_HEIGHT + 10,
                    LEFT_SIDEBAR_WIDTH - TAB_WIDTH - 30,
                    40
                );
                
                // Move location buttons down to make room for search box
                foreach (var button in locationButtons)
                {
                    button.bounds.Y += searchBoxRect.Height + 10;
                }
            }
        }

        private void PrecacheMapThumbnails()
        {
            // This method can be called on game load to pre-generate map thumbnails
            // for common locations, improving the responsiveness of the UI
            mapRenderer.CacheThumbnailsForCommonLocations();
        }

        public override void update(GameTime time)
        {
            base.update(time);

            // Update animations
            menuStyles.Update(time);
            
            // Update click animation timer
            if (clickAnimationTimer > 0f)
            {
                clickAnimationTimer -= (float)time.ElapsedGameTime.TotalSeconds;
                if (clickAnimationTimer <= 0f)
                {
                    lastClickedPosition = null;
                }
            }
        }

        public override void draw(SpriteBatch b)
        {
            // Draw a semi-transparent dark overlay over the entire screen first
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);
            
            // Draw the load game menu background (similar to title menu)
            if (backgroundTexture != null)
            {
                Rectangle backgroundSource = new Rectangle(0, 0, 640, 360);
                Rectangle destinationRect = new Rectangle(
                    (int)(Game1.viewport.Width / 2 - backgroundSource.Width * 1.5f),
                    (int)(Game1.viewport.Height / 2 - backgroundSource.Height * 1.5f),
                    (int)(backgroundSource.Width * 3f),
                    (int)(backgroundSource.Height * 3f)
                );
                
                b.Draw(backgroundTexture, destinationRect, backgroundSource, Color.White * 0.7f);
            }
            
            // Draw menu background using the game's built-in menu background
            drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                xPositionOnScreen, yPositionOnScreen, width, height, Color.White);
                
            // Draw starry background from Game1.background
            if (Game1.background != null)
            {
                // Use Game1.background which contains a procedural star field
                Game1.background.draw(b);
            }
                
            // Draw title at the top
            string title = "Magic Atlas";
            Vector2 titleSize = Game1.dialogueFont.MeasureString(title) * MenuStyles.TITLE_SCALE;
            Vector2 titlePos = new Vector2(
                xPositionOnScreen + (width - titleSize.X) / 2,
                yPositionOnScreen + MenuStyles.TITLE_VERTICAL_OFFSET
            );
            
            // Draw title with shadow and glow effect
            b.DrawString(Game1.dialogueFont, title, titlePos + new Vector2(3, 3), Color.Black * 0.5f, 0f, Vector2.Zero, MenuStyles.TITLE_SCALE, SpriteEffects.None, 0f);
            b.DrawString(Game1.dialogueFont, title, titlePos + new Vector2(2, 2), Color.DarkBlue * 0.5f, 0f, Vector2.Zero, MenuStyles.TITLE_SCALE, SpriteEffects.None, 0f);
            b.DrawString(Game1.dialogueFont, title, titlePos, new Color(130, 180, 255), 0f, Vector2.Zero, MenuStyles.TITLE_SCALE, SpriteEffects.None, 0f);
            
            // Add sparkles to the title
            float time = (float)Game1.currentGameTime.TotalGameTime.TotalSeconds;
            menuStyles.DrawSparkle(b, titlePos.X - 10, titlePos.Y + 10, time);
            menuStyles.DrawSparkle(b, titlePos.X + titleSize.X + 10, titlePos.Y + 10, time + 1.5f);
            
            // Draw left sidebar background slightly darker for visual separation
            Rectangle sidebarRect = new Rectangle(
                xPositionOnScreen + borderWidth,
                yPositionOnScreen + MenuStyles.TITLE_HEIGHT,
                LEFT_SIDEBAR_WIDTH - borderWidth * 2,
                height - MenuStyles.TITLE_HEIGHT - borderWidth
            );
            b.Draw(Game1.staminaRect, sidebarRect, Color.Black * 0.1f);
            
            // Draw tabs
            foreach (var tab in tabButtons)
            {
                bool isSelected = tab.name == currentTab;
                menuStyles.DrawVerticalTab(b, tab.bounds, tab.name, isSelected);
            }
            
            // Draw search box if needed
            if (currentTab == MOD_LOCATIONS_CATEGORY && locationManager.GetLocationsInCategory(MOD_LOCATIONS_CATEGORY)?.Count > 10)
            {
                DrawSimpleSearchBox(b);
            }
            
            // Draw location buttons
            foreach (var button in locationButtons)
            {
                bool isSelected = button.name == selectedLocation;
                bool isHovering = button.containsPoint(Game1.getMouseX(), Game1.getMouseY());
                
                // Get the display name
                string displayName = locationManager.GetDisplayName(button.name);
                
                menuStyles.DrawLocationButton(b, button.bounds, displayName, isSelected, isHovering);
            }
            
            // Draw map preview for selected location
            if (showingMap && selectedLocation != null && currentLocation != null)
            {
                // Draw map area background with fancy border
                drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                    mapViewArea.X - 12, mapViewArea.Y - 12, mapViewArea.Width + 24, mapViewArea.Height + 24, Color.White);
                
                // Draw the location name above the map
                string locationName = locationManager.GetDisplayName(selectedLocation);
                Vector2 nameSize = Game1.smallFont.MeasureString(locationName);
                Vector2 namePos = new Vector2(
                    mapViewArea.X + (mapViewArea.Width - nameSize.X) / 2,
                    mapViewArea.Y - 30
                );
                
                // Draw location name with shadow and color based on location type
                Color mapColor = GetMapColor(selectedLocation);
                b.DrawString(Game1.smallFont, locationName, namePos + new Vector2(1, 1), Color.Black * 0.6f);
                b.DrawString(Game1.smallFont, locationName, namePos, mapColor);
                
                // Draw the map using our MapRenderer - no gray box, single sprite or instruction text
                mapRenderer.DrawMapThumbnail(b, currentLocation, mapViewArea);
                
                // Draw click position visualization if we have one
                if (lastClickedPosition.HasValue && clickAnimationTimer > 0)
                {
                    float normalizedX = lastClickedPosition.Value.X / (float)mapViewArea.Width;
                    float normalizedY = lastClickedPosition.Value.Y / (float)mapViewArea.Height;
                    
                    Vector2 clickPos = new Vector2(
                        mapViewArea.X + normalizedX * mapViewArea.Width, 
                        mapViewArea.Y + normalizedY * mapViewArea.Height
                    );
                    
                    // Create a pulsing circle effect
                    float pulse = (float)(0.5f + 0.5f * Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 10));
                    float alpha = clickAnimationTimer * (0.4f + 0.4f * pulse);
                    
                    // Draw click indicator
                    b.Draw(Game1.mouseCursors, 
                        clickPos,
                        new Rectangle(402, 194, 16, 16), 
                        Color.Yellow * alpha, 0f, new Vector2(8f, 8f), 
                        1f + 0.5f * pulse, SpriteEffects.None, 1f);
                }
            }
            // If no location is selected yet, show instructions
            else
            {
                Vector2 instructionPos = new Vector2(
                    mapViewArea.X + mapViewArea.Width / 2,
                    mapViewArea.Y + mapViewArea.Height / 2
                );
                
                string instruction = "Select a location to see its map";
                Vector2 textSize = Game1.smallFont.MeasureString(instruction);
                
                b.DrawString(Game1.smallFont, instruction, 
                    instructionPos - textSize / 2 + new Vector2(1, 1), 
                    Color.Black * 0.6f);
                b.DrawString(Game1.smallFont, instruction, 
                    instructionPos - textSize / 2, 
                    Color.White);
            }
            
            // Draw cursor
            drawMouse(b);
        }
        
        private Color GetMapColor(string locationName)
        {
            switch (locationName)
            {
                case "Farm": return new Color(146, 188, 137); // Green
                case "Town": return new Color(117, 165, 200); // Blue
                case "Beach": return new Color(242, 225, 149); // Sand
                case "Mountain": return new Color(181, 179, 174); // Gray
                case "Desert": return new Color(248, 218, 158); // Light yellow
                case "Forest": return new Color(94, 153, 107); // Dark green
                case "Woods": return new Color(82, 102, 50); // Dark forest green
                case "BusStop": return new Color(172, 198, 168); // Light green
                case "Railroad": return new Color(160, 142, 129); // Brown
                case "Mine": return new Color(108, 93, 125); // Purple
                case "SeedShop": 
                case "ScienceHouse":
                case "AnimalShop":
                case "Saloon":
                case "ManorHouse":
                case "Blacksmith":
                case "Hospital":
                    return new Color(158, 145, 126); // Building color
                default:
                    if (locationName.StartsWith("Custom_"))
                        return new Color(194, 178, 224); // Purple for SVE locations
                    return new Color(168, 178, 189); // Default blue-gray
            }
        }
        
        private void DrawSimpleSearchBox(SpriteBatch b)
        {
            // Draw search box background
            drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                searchBoxRect.X, searchBoxRect.Y, searchBoxRect.Width, searchBoxRect.Height, Color.White * 0.9f);
                
            // Draw search icon
            b.Draw(Game1.mouseCursors, 
                new Vector2(searchBoxRect.X + 10, searchBoxRect.Y + (searchBoxRect.Height - 24) / 2), 
                new Rectangle(80, 0, 13, 12), Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 1f);
                
            // Draw search text or placeholder
            string textToShow = string.IsNullOrEmpty(searchText) ? SEARCH_PLACEHOLDER : searchText;
            Color textColor = string.IsNullOrEmpty(searchText) ? Color.Gray : Color.Black;
            
            Vector2 textPos = new Vector2(
                searchBoxRect.X + 40, 
                searchBoxRect.Y + (searchBoxRect.Height - Game1.smallFont.MeasureString(textToShow).Y) / 2
            );
            
            b.DrawString(Game1.smallFont, textToShow, textPos, textColor);
            
            // Draw cursor if search is focused
            if (searchFocused && !string.IsNullOrEmpty(searchText))
            {
                float cursorX = textPos.X + Game1.smallFont.MeasureString(searchText).X;
                
                // Blinking cursor effect
                if ((int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 500) % 2 == 0)
                {
                    b.Draw(Game1.staminaRect, new Rectangle((int)cursorX, (int)textPos.Y, 2, 24), Color.Black);
                }
            }
        }
        
        public override void receiveKeyPress(Keys key)
        {
            base.receiveKeyPress(key);
            
            if (searchFocused)
            {
                // Handle backspace
                if (key == Microsoft.Xna.Framework.Input.Keys.Back && searchText.Length > 0)
                {
                    searchText = searchText.Substring(0, searchText.Length - 1);
                    UpdateSearchResults();
                }
                // Handle escape to unfocus search
                else if (key == Microsoft.Xna.Framework.Input.Keys.Escape)
                {
                    searchFocused = false;
                }
                // Handle text input for alphanumeric and space characters
                else if (KeyboardUtils.IsAlphanumericOrSpaceKey(key))
                {
                    char charInput = KeyboardUtils.KeyToChar(key, Game1.oldKBState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || 
                                                              Game1.oldKBState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift));
                    if (charInput != '\0')
                    {
                        searchText += charInput;
                        UpdateSearchResults();
                    }
                }
            }
        }
        
        public override void receiveGamePadButton(Buttons b)
        {
            base.receiveGamePadButton(b);
            
            // Handle controller input for navigation
            switch (b)
            {
                case Buttons.B:
                    exitThisMenu();
                    Game1.playSound("bigDeSelect");
                    break;
                    
                case Buttons.DPadLeft:
                case Buttons.LeftThumbstickLeft:
                    // If we have a map shown, go back to just showing location list
                    if (showingMap)
                    {
                        showingMap = false;
                        Game1.playSound("shwip");
                    }
                    break;
                    
                case Buttons.DPadRight:
                case Buttons.LeftThumbstickRight:
                    // If we have a location selected but no map shown, show the map
                    if (selectedLocation != null && !showingMap)
                    {
                        showingMap = true;
                        currentLocation = locationManager.LoadLocation(selectedLocation);
                        UpdateLocationButtons();
                        Game1.playSound("smallSelect");
                    }
                    break;
            }
        }
        
        public override void receiveScrollWheelAction(int direction)
        {
            base.receiveScrollWheelAction(direction);
            
            // Handle scrolling through the location list
            if (locationButtons.Count > MAX_BUTTONS_TO_SHOW)
            {
                int scrollAmount = 75 * Math.Sign(direction);
                
                // Scroll location buttons
                foreach (var button in locationButtons)
                {
                    button.bounds.Y += scrollAmount;
                }
            }
        }
        
        private void UpdateSearchResults()
        {
            // If this is the first search, save the original locations
            if (originalTabLocations.Count == 0 && !string.IsNullOrEmpty(searchText))
            {
                originalTabLocations = locationManager.GetLocationsInCategory(currentTab).ToList();
            }
            
            // If search is empty, restore original locations
            if (string.IsNullOrEmpty(searchText) && originalTabLocations.Count > 0)
            {
                // Pass the original locations list directly, not as a string
                locationManager.UpdateCategoryLocations(currentTab, originalTabLocations);
                originalTabLocations.Clear();
            }
            else if (!string.IsNullOrEmpty(searchText))
            {
                // Filter locations based on search text
                var filteredLocations = originalTabLocations
                    .Where(loc => locationManager.GetDisplayName(loc).ToLower().Contains(searchText.ToLower()))
                    .ToList();
                
                // Pass the filtered list directly, not as a string
                locationManager.UpdateCategoryLocations(currentTab, filteredLocations);
            }
            
            UpdateLocationButtons();
        }
        
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // Check if click is within menu bounds
            if (!isWithinBounds(x, y))
            {
                // Close menu when clicking outside bounds
                exitThisMenu();
                Game1.playSound("bigDeSelect");
                return;
            }
            
            // Check tab buttons first
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
                        Game1.playSound("smallSelect");
                        UpdateLocationButtons();
                    }
                    return;
                }
            }
            
            // Check search box clicks
            if (currentTab == MOD_LOCATIONS_CATEGORY && searchBoxRect.Contains(x, y))
            {
                searchFocused = true;
                Game1.playSound("drumkit6");
                return;
            }
            else
            {
                searchFocused = false;
            }
            
            // Check location button clicks
            foreach (var button in locationButtons)
            {
                if (button.containsPoint(x, y))
                {
                    selectedLocation = button.name;
                    showingMap = true;
                    currentLocation = locationManager.LoadLocation(selectedLocation);
                    UpdateLocationButtons(); // Recalculate map area
                    Game1.playSound("bigSelect");
                    return;
                }
            }
            
            // Check map area clicks for warping
            if (showingMap && mapViewArea.Contains(x, y) && selectedLocation != null)
            {
                // Calculate normalized position within the map (0-1 range)
                Point relativePos = new Point(x - mapViewArea.X, y - mapViewArea.Y);
                
                // Store click position and start animation timer
                lastClickedPosition = relativePos;
                clickAnimationTimer = 1.5f;
                
                // Calculate destination position as a percentage of the map size
                float normalizedX = relativePos.X / (float)mapViewArea.Width;
                float normalizedY = relativePos.Y / (float)mapViewArea.Height;
                
                // Convert to game coordinates (tiles)
                MapCoordinateSystem coordinates = new MapCoordinateSystem(currentLocation);
                Point gameTile = coordinates.MapPositionToGameTile(normalizedX, normalizedY);
                
                // Warp to location
                WarpToLocation(selectedLocation, gameTile.X, gameTile.Y);
                
                // Play teleport sound
                Game1.playSound("wand");
                return;
            }
            
            base.receiveLeftClick(x, y, playSound);
        }
        
        private void WarpToLocation(string locationName, int tileX, int tileY)
        {
            // Create warp particles before closing menu if effects are enabled
            if (config.UseWarpEffects)
            {
                CreateWarpParticles();
            }
            
            // Close menu 
            exitThisMenu();
            
            try
            {
                // Let the location manager handle the warp
                if (locationManager.WarpToLocation(locationName, tileX, tileY))
                {
                    // Add arrival effects after warping if effects are enabled
                    if (config.UseWarpEffects)
                    {
                        AddArrivalEffect();
                    }
                    
                    Monitor.Log($"Successfully warped to {locationName} at tile ({tileX}, {tileY})");
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
        
        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            // Right click exits the menu with sound
            if (playSound)
                Game1.playSound("bigDeSelect");
            exitThisMenu();
        }
        
        /// <summary>
        /// Create particle effects when warping
        /// </summary>
        private void CreateWarpParticles()
        {
            // Example particle effect: sparkles around the player
            for (int i = 0; i < 20; i++)
            {
                Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(
                    textureName: "LooseSprites\\Cursors",
                    sourceRect: new Rectangle(294, 185, 10, 10),
                    animationInterval: 50f,
                    animationLength: 10,
                    numberOfLoops: 1,
                    position: Game1.player.Position + new Vector2(Game1.random.Next(-32, 32), Game1.random.Next(-32, 32)),
                    flicker: false,
                    flipped: false
                )
                {
                    scale = 2f,
                    layerDepth = 1f,
                    motion = new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2)),
                    acceleration = new Vector2(0, 0.1f),
                    delayBeforeAnimationStart = i * 50
                });
            }
        }

        /// <summary>
        /// Add arrival effects at the destination
        /// </summary>
        private void AddArrivalEffect()
        {
            // Example arrival effect: a burst of light
            Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(
                textureName: "LooseSprites\\Cursors",
                sourceRect: new Rectangle(276, 198, 10, 10),
                animationInterval: 70f,
                animationLength: 8,
                numberOfLoops: 1,
                position: Game1.player.Position,
                flicker: false,
                flipped: false
            )
            {
                scale = 4f,
                layerDepth = 1f
            });
        }
    }
    
    public static class KeyboardUtils
    {
        public static bool IsAlphanumericOrSpaceKey(Microsoft.Xna.Framework.Input.Keys key)
        {
            return (key >= Microsoft.Xna.Framework.Input.Keys.A && key <= Microsoft.Xna.Framework.Input.Keys.Z) ||
                   (key >= Microsoft.Xna.Framework.Input.Keys.D0 && key <= Microsoft.Xna.Framework.Input.Keys.D9) ||
                   (key >= Microsoft.Xna.Framework.Input.Keys.NumPad0 && key <= Microsoft.Xna.Framework.Input.Keys.NumPad9) ||
                   key == Microsoft.Xna.Framework.Input.Keys.Space;
        }
        
        public static char KeyToChar(Microsoft.Xna.Framework.Input.Keys key, bool shift)
        {
            // Letters
            if (key >= Microsoft.Xna.Framework.Input.Keys.A && key <= Microsoft.Xna.Framework.Input.Keys.Z)
            {
                char baseChar = (char)('a' + (key - Microsoft.Xna.Framework.Input.Keys.A));
                return shift ? char.ToUpper(baseChar) : baseChar;
            }
            
            // Numbers on the top row
            if (key >= Microsoft.Xna.Framework.Input.Keys.D0 && key <= Microsoft.Xna.Framework.Input.Keys.D9)
            {
                if (!shift)
                    return (char)('0' + (key - Microsoft.Xna.Framework.Input.Keys.D0));
                
                // Shift + number for symbols
                switch (key)
                {
                    case Microsoft.Xna.Framework.Input.Keys.D0: return ')';
                    case Microsoft.Xna.Framework.Input.Keys.D1: return '!';
                    case Microsoft.Xna.Framework.Input.Keys.D2: return '@';
                    case Microsoft.Xna.Framework.Input.Keys.D3: return '#';
                    case Microsoft.Xna.Framework.Input.Keys.D4: return '$';
                    case Microsoft.Xna.Framework.Input.Keys.D5: return '%';
                    case Microsoft.Xna.Framework.Input.Keys.D6: return '^';
                    case Microsoft.Xna.Framework.Input.Keys.D7: return '&';
                    case Microsoft.Xna.Framework.Input.Keys.D8: return '*';
                    case Microsoft.Xna.Framework.Input.Keys.D9: return '(';
                }
            }
            
            // Numpad numbers
            if (key >= Microsoft.Xna.Framework.Input.Keys.NumPad0 && key <= Microsoft.Xna.Framework.Input.Keys.NumPad9)
                return (char)('0' + (key - Microsoft.Xna.Framework.Input.Keys.NumPad0));
            
            // Other common keys
            switch (key)
            {
                case Microsoft.Xna.Framework.Input.Keys.Space: return ' ';
                case Microsoft.Xna.Framework.Input.Keys.OemComma: return shift ? '<' : ',';
                case Microsoft.Xna.Framework.Input.Keys.OemPeriod: return shift ? '>' : '.';
                case Microsoft.Xna.Framework.Input.Keys.OemQuestion: return shift ? '?' : '/';
                case Microsoft.Xna.Framework.Input.Keys.OemSemicolon: return shift ? ':' : ';';
                case Microsoft.Xna.Framework.Input.Keys.OemQuotes: return shift ? '"' : '\'';
                case Microsoft.Xna.Framework.Input.Keys.OemOpenBrackets: return shift ? '{' : '[';
                case Microsoft.Xna.Framework.Input.Keys.OemCloseBrackets: return shift ? '}' : ']';
                case Microsoft.Xna.Framework.Input.Keys.OemPipe: return shift ? '|' : '\\';
                case Microsoft.Xna.Framework.Input.Keys.OemMinus: return shift ? '_' : '-';
                case Microsoft.Xna.Framework.Input.Keys.OemPlus: return shift ? '+' : '=';
                case Microsoft.Xna.Framework.Input.Keys.OemTilde: return shift ? '~' : '`';
            }
            
            return '\0'; // Not a character key
        }
    }
}