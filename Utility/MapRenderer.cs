using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using xTile;

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
        
        // Cache map textures
        private readonly Dictionary<string, Texture2D> mapCache = new Dictionary<string, Texture2D>();
        
        public MapRenderer(IMonitor monitor, IModHelper helper)
        {
            this.Monitor = monitor;
            this.Helper = helper;
        }
        
        /// <summary>
        /// Calculate appropriate map view area based on map dimensions
        /// </summary>
        public Rectangle CalculateMapViewArea(int mapWidth, int mapHeight, Rectangle menuBounds)
        {
            // Maximum size for the map
            int maxWidth = menuBounds.Width;
            int maxHeight = menuBounds.Height;
            
            // Calculate scale while maintaining aspect ratio
            float scale = 1.0f;
            if (mapWidth > maxWidth || mapHeight > maxHeight)
            {
                float scaleX = (float)maxWidth / mapWidth;
                float scaleY = (float)maxHeight / mapHeight;
                scale = Math.Min(scaleX, scaleY);
            }
            else
            {
                // Try to scale up small maps
                float scaleX = (float)maxWidth / mapWidth;
                float scaleY = (float)maxHeight / mapHeight;
                float possibleScale = Math.Min(scaleX, scaleY);
                scale = Math.Min(possibleScale, 2.5f); // Don't scale up more than 250%
            }
            
            // Calculate new dimensions
            int scaledWidth = (int)(mapWidth * scale);
            int scaledHeight = (int)(mapHeight * scale);
            
            // Center the map in the available space
            return new Rectangle(
                menuBounds.X + (menuBounds.Width - scaledWidth) / 2,
                menuBounds.Y + (menuBounds.Height - scaledHeight) / 2,
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
                // Draw the map
                b.Draw(mapTexture, bounds, Color.White);
                
                // Draw player position if this is the current location
                if (location.Name == Game1.currentLocation.Name)
                {
                    DrawPlayerPosition(b, location, bounds);
                }
            }
            else
            {
                // Draw placeholder if map not available
                DrawPlaceholder(b, location.Name, bounds);
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
                // Calculate position relative to the map (using tile coordinates for accuracy)
                float relativeX = Game1.player.TilePoint.X / (float)location.map.Layers[0].LayerWidth;
                float relativeY = Game1.player.TilePoint.Y / (float)location.map.Layers[0].LayerHeight;
                
                // Clamp to valid range [0, 1]
                relativeX = Math.Clamp(relativeX, 0f, 1f);
                relativeY = Math.Clamp(relativeY, 0f, 1f);
                
                // Position on displayed map bounds
                Vector2 playerMapPos = new Vector2(
                    bounds.X + bounds.Width * relativeX,
                    bounds.Y + bounds.Height * relativeY
                );
                
                // Draw a more distinct player marker (e.g., a small pulsing circle)
                float pulseScale = 1f + (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * 5) * 0.15f; // Faster pulse
                float markerSize = 8f * pulseScale; // Base size 8px
                Vector2 origin = new Vector2(4, 4); // Center of the marker texture

                // Draw outer circle (slightly transparent white)
                b.Draw(
                    Game1.mouseCursors, 
                    playerMapPos, 
                    new Rectangle(375, 357, 3, 3), // Small white square texture
                    Color.White * 0.7f * pulseScale, 
                    0f, 
                    new Vector2(1.5f, 1.5f), // Origin for scaling
                    markerSize * 1.5f, // Slightly larger outer circle
                    SpriteEffects.None, 
                    1f
                );

                // Draw inner circle (solid red)
                b.Draw(
                    Game1.mouseCursors, 
                    playerMapPos, 
                    new Rectangle(375, 357, 3, 3), // Small white square texture, tinted red
                    Color.Red, 
                    0f, 
                    new Vector2(1.5f, 1.5f), // Origin for scaling
                    markerSize, // Inner circle size
                    SpriteEffects.None, 
                    1f
                );
            }
            catch (Exception ex) // Catch specific exceptions if possible
            {
                Monitor.Log($"Error drawing player position: {ex.Message}", LogLevel.Debug);
                // Ignore errors in position calculation to prevent crashes
            }
        }
        
        /// <summary>
        /// Draw a placeholder when a map isn't available
        /// </summary>
        private void DrawPlaceholder(SpriteBatch b, string locationName, Rectangle bounds)
        {
            // Draw gray background
            b.Draw(Game1.staminaRect, bounds, Color.DarkGray * 0.5f);
            
            // Draw location name
            string name = locationName;
            Vector2 textSize = Game1.smallFont.MeasureString(name);
            Vector2 textPos = new Vector2(
                bounds.X + (bounds.Width - textSize.X) / 2,
                bounds.Y + (bounds.Height - textSize.Y) / 2
            );
            
            b.DrawString(Game1.smallFont, name, textPos, Color.White);
        }

        /// <summary>
        /// Get a map texture for the location
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
            
            // Try to load from our exported maps folder
            Texture2D mapTexture = TryLoadExportedMap(location.Name);
            
            // If not found and location has a map, render it directly
            if (mapTexture == null && location.Map != null)
            {
                mapTexture = RenderMapToTexture(location);
            }
            
            // Cache the result if successful
            if (mapTexture != null)
            {
                mapCache[location.Name] = mapTexture;
            }
            
            return mapTexture;
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
                    Helper.DirectoryPath, 
                    "assets", "maps"
                );
                
                // The map file path
                string mapFilePath = Path.Combine(exportedMapsPath, $"{locationName}.png");
                
                // Check if file exists
                if (File.Exists(mapFilePath))
                {
                    // Load the image
                    using (FileStream stream = new FileStream(mapFilePath, FileMode.Open))
                    {
                        try
                        {
                            return Texture2D.FromStream(Game1.graphics.GraphicsDevice, stream);
                        }
                        catch (Exception ex)
                        {
                            Monitor.Log($"Error loading map texture: {ex.Message}", LogLevel.Debug);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error looking for exported map: {ex.Message}", LogLevel.Debug);
            }
            
            return null;
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
                
                // For very large maps, scale down
                float scale = 1.0f;
                const int MAX_SIZE = 1024;
                
                if (mapWidth > MAX_SIZE || mapHeight > MAX_SIZE)
                {
                    float scaleX = (float)MAX_SIZE / mapWidth;
                    float scaleY = (float)MAX_SIZE / mapHeight;
                    scale = Math.Min(scaleX, scaleY);
                    mapWidth = (int)(mapWidth * scale);
                    mapHeight = (int)(mapHeight * scale);
                }
                
                // Create a render target
                RenderTarget2D renderTarget = new RenderTarget2D(
                    Game1.graphics.GraphicsDevice,
                    mapWidth, 
                    mapHeight
                );
                
                // Store current render target
                RenderTarget2D originalRenderTarget = Game1.graphics.GraphicsDevice.GetRenderTargets()[0].RenderTarget as RenderTarget2D;
                
                // Set our render target
                Game1.graphics.GraphicsDevice.SetRenderTarget(renderTarget);
                Game1.graphics.GraphicsDevice.Clear(Color.Transparent);
                
                // Draw the map
                SpriteBatch sb = new SpriteBatch(Game1.graphics.GraphicsDevice);
                sb.Begin(samplerState: SamplerState.PointClamp, transformMatrix: Matrix.CreateScale(scale));
                
                // Simple rendering approach
                try 
                {
                    location.drawBackground(sb);
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Error rendering map: {ex.Message}", LogLevel.Debug);
                }
                
                sb.End();
                
                // Restore original render target
                Game1.graphics.GraphicsDevice.SetRenderTarget(originalRenderTarget);
                
                // Create texture from render target
                Texture2D mapTexture = new Texture2D(Game1.graphics.GraphicsDevice, renderTarget.Width, renderTarget.Height);
                Color[] data = new Color[renderTarget.Width * renderTarget.Height];
                renderTarget.GetData(data);
                mapTexture.SetData(data);
                
                // Clean up
                renderTarget.Dispose();
                
                return mapTexture;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error rendering map: {ex.Message}", LogLevel.Debug);
                return null;
            }
        }
    }
}