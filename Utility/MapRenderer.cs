using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.IO;
using xTile;
using xTile.Dimensions;

// Use XNA Rectangle by default
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace WarpMod.Utility
{
    /// <summary>
    /// Handles rendering of location maps in the warp menu
    /// </summary>
    public class MapRenderer
    {
        private readonly IMonitor Monitor;
        private readonly IModHelper Helper;
        
        // Dictionary to cache map textures we've loaded
        private readonly Dictionary<string, Texture2D> mapCache = new Dictionary<string, Texture2D>();
        
        // Dictionary to store location colors for fallback rendering
        private readonly Dictionary<string, Color> locationColors = new Dictionary<string, Color>();
        
        // Dictionary to track which maps we've already tried to load
        private readonly HashSet<string> attemptedMaps = new HashSet<string>();
        
        public MapRenderer(IMonitor monitor, IModHelper helper)
        {
            this.Monitor = monitor;
            this.Helper = helper;
            
            InitializeLocationColors(); // For fallback only
        }
        
        /// <summary>
        /// Initialize colors for different location types (used as fallbacks)
        /// </summary>
        private void InitializeLocationColors()
        {
            // Standard locations
            locationColors["Farm"] = new Color(146, 188, 137);
            locationColors["Town"] = new Color(117, 165, 200);
            locationColors["Beach"] = new Color(242, 225, 149);
            locationColors["Mountain"] = new Color(181, 179, 174);
            locationColors["Desert"] = new Color(248, 218, 158);
            locationColors["Forest"] = new Color(94, 153, 107);
            locationColors["Woods"] = new Color(82, 102, 50);
            locationColors["BusStop"] = new Color(172, 198, 168);
            locationColors["Railroad"] = new Color(160, 142, 129);
            locationColors["Mine"] = new Color(108, 93, 125);
            
            // Buildings
            Color buildingColor = new Color(158, 145, 126);
            locationColors["SeedShop"] = buildingColor;
            locationColors["ScienceHouse"] = buildingColor;
            locationColors["AnimalShop"] = buildingColor;
            locationColors["Saloon"] = buildingColor;
            locationColors["ManorHouse"] = buildingColor;
            locationColors["Blacksmith"] = buildingColor;
            locationColors["Hospital"] = buildingColor;
            
            // Islands
            Color islandColor = new Color(132, 201, 202);
            locationColors["IslandSouth"] = islandColor;
            locationColors["IslandWest"] = islandColor;
            locationColors["IslandNorth"] = islandColor;
            locationColors["IslandEast"] = islandColor;
            locationColors["IslandFarmHouse"] = islandColor;
        }
        
        /// <summary>
        /// Cache thumbnails for common locations
        /// </summary>
        public void CacheThumbnailsForCommonLocations()
        {
            // We'll disable pre-caching since it was causing index out of bounds errors
            // Just log that pre-caching is disabled
            Monitor.Log("Map pre-caching is disabled to avoid errors. Maps will be loaded on demand.", LogLevel.Debug);
            
            // Clear the attempted maps set in case we want to retry loading previously failed maps
            attemptedMaps.Clear();
        }
        
        /// <summary>
        /// Render the map image to a texture
        /// </summary>
        private Texture2D RenderMapToTexture(GameLocation location)
        {
            if (location?.Map == null)
                return null;
                
            try
            {
                // Get map dimensions
                int mapWidth = location.map.DisplayWidth * Game1.tileSize;
                int mapHeight = location.map.DisplayHeight * Game1.tileSize;
                
                // For very large maps, we'll scale down to avoid memory issues
                float scale = 1.0f;
                const int MAX_SIZE = 1024; // Reduced from 2048 to be safer with memory
                
                if (mapWidth > MAX_SIZE || mapHeight > MAX_SIZE)
                {
                    float scaleX = (float)MAX_SIZE / mapWidth;
                    float scaleY = (float)MAX_SIZE / mapHeight;
                    scale = Math.Min(scaleX, scaleY);
                    mapWidth = (int)(mapWidth * scale);
                    mapHeight = (int)(mapHeight * scale);
                }
                
                // Create a render target to draw the map onto
                RenderTarget2D renderTarget = new RenderTarget2D(
                    Game1.graphics.GraphicsDevice,
                    mapWidth, 
                    mapHeight
                );
                
                // Remember the current render target
                RenderTarget2D originalRenderTarget = Game1.graphics.GraphicsDevice.GetRenderTargets()[0].RenderTarget as RenderTarget2D;
                
                // Set our render target
                Game1.graphics.GraphicsDevice.SetRenderTarget(renderTarget);
                Game1.graphics.GraphicsDevice.Clear(Color.Transparent);
                
                // Use a SpriteBatch to draw the map texture
                SpriteBatch sb = new SpriteBatch(Game1.graphics.GraphicsDevice);
                sb.Begin(samplerState: SamplerState.PointClamp, transformMatrix: Matrix.CreateScale(scale));
                
                try 
                {
                    // Instead of using the Layer.Draw method directly (which requires IDisplayDevice), 
                    // we'll iterate through the tiles and draw them manually - with better error handling
                    foreach (xTile.Layers.Layer layer in location.map.Layers)
                    {
                        if (layer.Id == "Paths" || layer.Id == "AlwaysFront")
                            continue; // Skip these special layers
                            
                        for (int y = 0; y < layer.LayerHeight; y++)
                        {
                            for (int x = 0; x < layer.LayerWidth; x++)
                            {
                                try
                                {
                                    xTile.Tiles.Tile tile = layer.Tiles[x, y];
                                    if (tile == null)
                                        continue;
                                        
                                    // Draw the tile using its tileset texture
                                    xTile.Tiles.StaticTile staticTile = tile as xTile.Tiles.StaticTile;
                                    if (staticTile != null && staticTile.TileSheet != null && 
                                        !string.IsNullOrEmpty(staticTile.TileSheet.ImageSource))
                                    {
                                        // More defensive checks
                                        if (staticTile.TileSheet.TileSize.Width <= 0 || 
                                            staticTile.TileSheet.TileSize.Height <= 0)
                                            continue;
                                            
                                        try 
                                        {
                                            Texture2D texture = Game1.content.Load<Texture2D>(staticTile.TileSheet.ImageSource);
                                            
                                            if (texture.Width <= 0 || texture.Height <= 0)
                                                continue;
                                                
                                            int tileSheetWidth = texture.Width / staticTile.TileSheet.TileSize.Width;
                                            if (tileSheetWidth <= 0)
                                                continue;
                                                
                                            int tileIndex = staticTile.TileIndex;
                                            
                                            // Make sure the index is valid
                                            if (tileIndex < 0 || (tileIndex / tileSheetWidth >= texture.Height / staticTile.TileSheet.TileSize.Height))
                                                continue;
                                                
                                            int tileX = tileIndex % tileSheetWidth;
                                            int tileY = tileIndex / tileSheetWidth;
                                            
                                            Rectangle sourceRect = new Rectangle(
                                                tileX * staticTile.TileSheet.TileSize.Width,
                                                tileY * staticTile.TileSheet.TileSize.Height,
                                                staticTile.TileSheet.TileSize.Width,
                                                staticTile.TileSheet.TileSize.Height);
                                            
                                            Vector2 position = new Vector2(
                                                x * Game1.tileSize, 
                                                y * Game1.tileSize);
                                                
                                            sb.Draw(texture, position, sourceRect, Color.White);
                                        }
                                        catch (Exception)
                                        {
                                            // Just ignore this specific tile
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                    // Skip any problematic tiles
                                }
                            }
                        }
                    }
                }
                catch (Exception layerEx)
                {
                    // Log layer rendering error but continue with what we have
                    Monitor.Log($"Error rendering map layers for {location.Name}: {layerEx.Message}", LogLevel.Trace);
                }
                
                sb.End();
                
                // Restore the original render target
                Game1.graphics.GraphicsDevice.SetRenderTarget(originalRenderTarget);
                
                // Create a texture from the render target
                Texture2D mapTexture = new Texture2D(Game1.graphics.GraphicsDevice, renderTarget.Width, renderTarget.Height);
                Color[] data = new Color[renderTarget.Width * renderTarget.Height];
                renderTarget.GetData(data);
                mapTexture.SetData(data);
                
                // Dispose of the render target
                renderTarget.Dispose();
                
                return mapTexture;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error rendering map for {location.Name}: {ex.Message}", LogLevel.Trace); // Downgraded from Error to Trace
                return null;
            }
        }
        
        /// <summary>
        /// Try to load a map image directly from the game's content
        /// </summary>
        private Texture2D TryLoadMapDirectly(string locationName)
        {
            try
            {
                // Maps are stored in the "Maps" folder in the Content directory
                string mapPath = $"Maps/{locationName}";
                
                // Use SMAPI's content pipeline to load the map
                var mapData = Helper.GameContent.Load<Map>(mapPath);
                
                if (mapData != null)
                {
                    // Create a temporary location to render the map
                    var tempLocation = new GameLocation(mapPath, locationName);
                    
                    // Return a rendered texture of the map
                    return RenderMapToTexture(tempLocation);
                }
            }
            catch (Exception ex)
            {
                // This is expected for maps that don't exist, so only log at trace level
                Monitor.Log($"Failed to load map content for {locationName}: {ex.Message}", LogLevel.Trace);
            }
            
            return null;
        }
        
        /// <summary>
        /// Try to load a map image from the unpacked Content folder
        /// </summary>
        private Texture2D TryLoadMapFromUnpacked(string locationName)
        {
            try
            {
                // Path to the unpacked Content folder
                string contentPath = Path.Combine(
                    Path.GetDirectoryName(Helper.DirectoryPath), // Get the parent folder of our mod
                    "..", "..", // Navigate to the top level folder
                    "game_folder", "Stardew Valley", "Content (unpacked)"
                );
                
                // The map file path relative to the Content folder
                string mapFilePath = Path.Combine(contentPath, "Maps", $"{locationName}.tbin");
                
                if (!File.Exists(mapFilePath))
                {
                    Monitor.Log($"Could not find unpacked map file at {mapFilePath}", LogLevel.Trace);
                    return null;
                }
                
                // Load the map from the unpacked file
                Map mapData = null;
                try
                {
                    mapData = Game1.content.Load<Map>($"Maps/{locationName}");
                    Monitor.Log($"Successfully loaded map data for {locationName}", LogLevel.Debug);
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Could not load map data for {locationName}: {ex.Message}", LogLevel.Trace);
                    return null;
                }
                
                if (mapData != null)
                {
                    // Create a temporary location to render the map
                    var tempLocation = new GameLocation($"Maps/{locationName}", locationName);
                    
                    // Return a rendered texture of the map
                    return RenderMapToTexture(tempLocation);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to load unpacked map for {locationName}: {ex.Message}", LogLevel.Trace);
            }
            
            return null;
        }
        
        /// <summary>
        /// Try to load a map thumbnail image directly from the unpacked Content folder
        /// </summary>
        private Texture2D TryLoadMapThumbnail(string locationName)
        {
            try
            {
                // Path to the unpacked Content folder
                string contentPath = Path.Combine(
                    Path.GetDirectoryName(Helper.DirectoryPath), // Get the parent folder of our mod
                    "..", "..", // Navigate to the top level folder
                    "game_folder", "Stardew Valley", "Content (unpacked)"
                );
                
                // Looking for menu backgrounds which are often simplified maps
                string[] possiblePaths = new string[]
                {
                    // Try a few different paths where thumbnail-like images might exist
                    Path.Combine(contentPath, "LooseSprites", "Locations", $"{locationName}.png"),
                    Path.Combine(contentPath, "LooseSprites", "Locations", $"{locationName}_background.png"),
                    Path.Combine(contentPath, "LooseSprites", $"{locationName}_background.png"),
                };
                
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        using (FileStream stream = new FileStream(path, FileMode.Open))
                        {
                            Texture2D texture = Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream);
                            Monitor.Log($"Loaded map thumbnail for {locationName} from {path}", LogLevel.Debug);
                            return texture;
                        }
                    }
                }
                
                Monitor.Log($"No thumbnail found for {locationName}", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error loading map thumbnail for {locationName}: {ex.Message}", LogLevel.Trace);
            }
            
            return null;
        }
        
        /// <summary>
        /// Try to load a map from our exported PNG files
        /// </summary>
        private Texture2D TryLoadExportedMap(string locationName)
        {
            try
            {
                // Path to our exported map images
                string exportedMapsPath = Path.Combine(
                    Helper.DirectoryPath, // Our mod directory
                    "assets", "maps"
                );
                
                // Log that we're looking for the map
                Monitor.Log($"Looking for map image for '{locationName}' in {exportedMapsPath}", LogLevel.Debug);
                
                // The map file path
                string mapFilePath = Path.Combine(exportedMapsPath, $"{locationName}.png");
                
                // Try several possible filenames for the map
                string[] possibleFilenames = new string[]
                {
                    // Original name
                    $"{locationName}.png",
                    
                    // Lowercase
                    $"{locationName.ToLower()}.png",
                    
                    // Without spaces
                    $"{locationName.Replace(" ", "")}.png",
                    
                    // Lowercase without spaces
                    $"{locationName.ToLower().Replace(" ", "")}.png",
                    
                    // With any underscores as spaces
                    $"{locationName.Replace("_", " ")}.png",
                    
                    // With any spaces as underscores
                    $"{locationName.Replace(" ", "_")}.png"
                };
                
                foreach (string filename in possibleFilenames)
                {
                    mapFilePath = Path.Combine(exportedMapsPath, filename);
                    if (File.Exists(mapFilePath))
                    {
                        // Found a match!
                        Monitor.Log($"Found map file at {mapFilePath}", LogLevel.Debug);
                        
                        // Load the image directly from file
                        using (FileStream stream = new FileStream(mapFilePath, FileMode.Open))
                        {
                            try
                            {
                                Texture2D texture = Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream);
                                Monitor.Log($"Successfully loaded map for {locationName} from {mapFilePath}", LogLevel.Debug);
                                return texture;
                            }
                            catch (Exception ex)
                            {
                                Monitor.Log($"Error loading texture from {mapFilePath}: {ex.Message}", LogLevel.Trace);
                            }
                        }
                    }
                }
                
                // If we get here, no suitable file was found
                Monitor.Log($"No suitable map file found for {locationName}", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error looking for exported map for {locationName}: {ex.Message}", LogLevel.Trace);
            }
            
            return null;
        }
        
        /// <summary>
        /// Log information about available map files to help with debugging
        /// </summary>
        public void LogAvailableMaps()
        {
            try
            {
                string exportedMapsPath = Path.Combine(Helper.DirectoryPath, "assets", "maps");
                
                if (!Directory.Exists(exportedMapsPath))
                {
                    Monitor.Log($"Maps directory does not exist: {exportedMapsPath}", LogLevel.Debug);
                    return;
                }
                
                string[] mapFiles = Directory.GetFiles(exportedMapsPath, "*.png");
                Monitor.Log($"Found {mapFiles.Length} map PNG files in {exportedMapsPath}:", LogLevel.Debug);
                
                // Log the first 10 maps to avoid spamming the log
                int count = Math.Min(mapFiles.Length, 10);
                for (int i = 0; i < count; i++)
                {
                    Monitor.Log($"  - {Path.GetFileName(mapFiles[i])}", LogLevel.Debug);
                }
                
                if (mapFiles.Length > 10)
                {
                    Monitor.Log($"  - ... and {mapFiles.Length - 10} more", LogLevel.Debug);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error listing available maps: {ex.Message}", LogLevel.Warn);
            }
        }

        /// <summary>
        /// Get a map texture for the location using SMAPI's content system
        /// </summary>
        private Texture2D GetMapTexture(GameLocation location)
        {
            if (location == null)
                return null;
                
            // Check cache first
            if (mapCache.TryGetValue(location.Name, out Texture2D cachedTexture) && 
                cachedTexture != null && !cachedTexture.IsDisposed)
            {
                return cachedTexture;
            }
            
            // If we've already tried to load this map and failed, don't try again
            if (attemptedMaps.Contains(location.Name))
                return null;
                
            Texture2D mapTexture = null;
            
            // First try: check our exported map images
            mapTexture = TryLoadExportedMap(location.Name);
            
            // For Custom_ prefixed locations, also try with the prefix removed for exported maps
            if (mapTexture == null && location.Name.StartsWith("Custom_"))
            {
                string baseName = location.Name.Substring(7); // Remove "Custom_" prefix
                mapTexture = TryLoadExportedMap(baseName);
            }
            
            // If exported map not found, try other methods
            if (mapTexture == null)
            {
                // Second try: render the location's map directly if it's already loaded
                if (location.Map != null)
                {
                    mapTexture = RenderMapToTexture(location);
                }
                
                // Third try: try to load a thumbnail directly from the unpacked Content folder
                if (mapTexture == null)
                {
                    mapTexture = TryLoadMapThumbnail(location.Name);
                }
                
                // Fourth try: try to load from the unpacked Content folder directly
                if (mapTexture == null) 
                {
                    mapTexture = TryLoadMapFromUnpacked(location.Name);
                }
                
                // Fifth try: use SMAPI's content loader
                if (mapTexture == null)
                {
                    mapTexture = TryLoadMapDirectly(location.Name);
                }
                
                // For Custom_ prefixed locations, also try with the prefix removed
                if (mapTexture == null && location.Name.StartsWith("Custom_"))
                {
                    string baseName = location.Name.Substring(7); // Remove "Custom_" prefix
                    
                    // Try thumbnail from unpacked content for the base name
                    mapTexture = TryLoadMapThumbnail(baseName);
                    
                    if (mapTexture == null)
                    {
                        // Try map from unpacked content for the base name
                        mapTexture = TryLoadMapFromUnpacked(baseName);
                    }
                    
                    if (mapTexture == null)
                    {
                        // Try SMAPI content loader for the base name
                        mapTexture = TryLoadMapDirectly(baseName);
                    }
                }
            }
            
            // If successful, cache the result
            if (mapTexture != null)
            {
                mapCache[location.Name] = mapTexture;
                return mapTexture;
            }
            
            // Remember that we tried this map
            attemptedMaps.Add(location.Name);
            return null;
        }
        
        /// <summary>
        /// Clean up texture resources
        /// </summary>
        public void CleanupCache()
        {
            foreach (var texture in mapCache.Values)
            {
                if (texture != null && !texture.IsDisposed)
                {
                    try { texture.Dispose(); } 
                    catch { /* Ignore disposal errors */ }
                }
            }
            
            mapCache.Clear();
        }

        /// <summary>
        /// Calculate the appropriate map view area based on map dimensions
        /// </summary>
        public Rectangle CalculateMapViewArea(int mapWidth, int mapHeight, Rectangle menuBounds)
        {
            // Maximum size we want to allow the map to be
            int maxWidth = menuBounds.Width - 100;  // Leave some padding
            int maxHeight = menuBounds.Height - 250; // Increased space for buttons

            // Calculate scale while maintaining aspect ratio
            float scale = 1.0f;
            if (mapWidth > maxWidth || mapHeight > maxHeight)
            {
                float scaleX = (float)maxWidth / mapWidth;
                float scaleY = (float)maxHeight / mapHeight;
                scale = Math.Min(scaleX, scaleY);
            }

            // Calculate new dimensions
            int scaledWidth = (int)(mapWidth * scale);
            int scaledHeight = (int)(mapHeight * scale);

            // Center the map in the available space
            return new Rectangle(
                menuBounds.X + (menuBounds.Width - scaledWidth) / 2,
                menuBounds.Y + menuBounds.Height - scaledHeight - 50, // Position at bottom with padding
                scaledWidth,
                scaledHeight
            );
        }

        /// <summary>
        /// Draw a thumbnail of the location map
        /// </summary>
        public void DrawMapThumbnail(SpriteBatch b, GameLocation location, Rectangle bounds)
        {
            if (location == null)
                return;
            
            // Try to get a map texture for this location
            Texture2D mapTexture = GetMapTexture(location);
            
            if (mapTexture != null)
            {
                // Draw a border around the map for better visibility
                int borderSize = 2;
                b.Draw(Game1.staminaRect, 
                    new Rectangle(bounds.X - borderSize, bounds.Y - borderSize, 
                                 bounds.Width + borderSize * 2, bounds.Height + borderSize * 2), 
                    Color.DarkGray);
                
                // Draw the actual map texture
                b.Draw(mapTexture, bounds, Color.White);
                
                // Draw player position if this is the current location
                if (location.Name == Game1.currentLocation.Name)
                {
                    DrawPlayerPosition(b, location, bounds);
                }
            }
            else
            {
                // Fall back to drawing a colored rectangle with texture
                DrawFallbackMap(b, location, bounds);
            }
        }
        
        /// <summary>
        /// Draw player position on the map
        /// </summary>
        private void DrawPlayerPosition(SpriteBatch b, GameLocation location, Rectangle bounds)
        {
            if (location.Map == null || Game1.player == null)
                return;
                
            try
            {
                // Calculate position relative to the map
                float relativeX = Game1.player.Position.X / (location.map.DisplayWidth * Game1.tileSize);
                float relativeY = Game1.player.Position.Y / (location.map.DisplayHeight * Game1.tileSize);
                
                // Clamp to ensure we're within bounds
                relativeX = Math.Clamp(relativeX, 0.05f, 0.95f);
                relativeY = Math.Clamp(relativeY, 0.05f, 0.95f);
                
                // Calculate position on the displayed map
                Vector2 playerPos = new Vector2(
                    bounds.X + bounds.Width * relativeX,
                    bounds.Y + bounds.Height * relativeY
                );
                
                // Draw a pulsing player marker
                float pulse = (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 4) * 0.2f + 0.8f;
                b.Draw(
                    Game1.mouseCursors,
                    playerPos,
                    new Rectangle(341, 400, 6, 6),
                    Color.Red * pulse,
                    0f,
                    new Vector2(3, 3),
                    2f,
                    SpriteEffects.None,
                    1f
                );
            }
            catch
            {
                // Ignore errors in position calculation
            }
        }
        
        /// <summary>
        /// Draw a fallback map when no texture is available
        /// </summary>
        private void DrawFallbackMap(SpriteBatch b, GameLocation location, Rectangle bounds)
        {
            // Draw a colored rectangle based on location type
            Color mapColor = GetLocationColor(location.Name);
            b.Draw(Game1.staminaRect, bounds, mapColor);
            
            // Add some terrain features to make it look more map-like
            if (location.terrainFeatures != null && location.terrainFeatures.Count() > 0)
            {
                Random random = new Random(location.Name.GetHashCode());
                for (int i = 0; i < 10; i++)
                {
                    int x = random.Next(bounds.X + 5, bounds.X + bounds.Width - 5);
                    int y = random.Next(bounds.Y + 5, bounds.Y + bounds.Height - 5);
                    int size = random.Next(3, 8);
                    
                    Color featureColor = Color.DarkGreen;
                    if (location.IsOutdoors)
                    {
                        featureColor = new Color(67, 103, 56);
                    }
                    else
                    {
                        featureColor = new Color(115, 95, 76);
                    }
                    
                    b.Draw(Game1.staminaRect, 
                        new Rectangle(x, y, size, size), 
                        featureColor * 0.7f);
                }
            }
            
            // Draw location name in the center
            string displayName = location.Name;
            if (location.Name.StartsWith("Custom_"))
            {
                displayName = location.Name.Substring(7); // Remove "Custom_" prefix for better display
            }
            
            Vector2 textSize = Game1.smallFont.MeasureString(displayName);
            Vector2 textPos = new Vector2(
                bounds.X + (bounds.Width - textSize.X) / 2,
                bounds.Y + (bounds.Height - textSize.Y) / 2
            );
            
            b.DrawString(Game1.smallFont, displayName,
                textPos + new Vector2(1, 1), Color.Black * 0.6f);
            b.DrawString(Game1.smallFont, displayName,
                textPos, Color.White);
        }
        
        /// <summary>
        /// Get the color for a location based on its type
        /// </summary>
        private Color GetLocationColor(string locationName)
        {
            if (locationColors.TryGetValue(locationName, out Color color))
            {
                return color;
            }
            
            // Special cases
            if (locationName.Contains("Island"))
            {
                return new Color(132, 201, 202); // Island blue-green
            }
            else if (locationName.Contains("Mine") || locationName.Contains("Cave"))
            {
                return new Color(108, 93, 125); // Mine purple
            }
            else if (locationName.StartsWith("Custom_"))
            {
                return new Color(194, 178, 224); // Modded location purple
            }
            
            // Default color
            return new Color(168, 178, 189); // Default blue-gray
        }
    }
}