using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Reflection;
using xTile.Layers;

namespace WarpMod.Utility
{
    /// <summary>
    /// Helper methods for handling map interactions and warping functionality.
    /// </summary>
    public static class MapHelpers
    {
        /// <summary>Represents a map click result with location name and tile coordinates.</summary>
        public struct MapClickResult
        {
            public string LocationName;
            public int TileX;
            public int TileY;
        }
        
        // Cache for valid warp positions to avoid recalculating 
        private static readonly Dictionary<string, Point> ValidWarpPositionCache = new Dictionary<string, Point>();
        
        /// <summary>Gets the map area rectangle using robust reflection with fallbacks.</summary>
        public static Rectangle GetMapAreaSafely(GameMenu gameMenu, IMonitor monitor)
        {
            // Define a standard fallback map area based on the game menu dimensions
            Rectangle fallbackArea = new Rectangle(
                gameMenu.xPositionOnScreen + 30,
                gameMenu.yPositionOnScreen + 30,
                gameMenu.width - 60,
                gameMenu.height - 100
            );
            
            try
            {
                // Try multiple approaches to get the map page
                
                // Approach 1: Try to access via reflection on pages
                var pagesField = gameMenu.GetType().GetField("pages", BindingFlags.Instance | BindingFlags.NonPublic);
                if (pagesField != null)
                {
                    var pagesValue = pagesField.GetValue(gameMenu);
                    if (pagesValue != null)
                    {
                        IClickableMenu mapPage = null;
                        
                        // Try to handle various types the pages collection might be
                        if (pagesValue is List<IClickableMenu> pagesList && GameMenu.mapTab < pagesList.Count)
                        {
                            mapPage = pagesList[GameMenu.mapTab];
                        }
                        else if (pagesValue is IClickableMenu[] pagesArray && GameMenu.mapTab < pagesArray.Length)
                        {
                            mapPage = pagesArray[GameMenu.mapTab];
                        }
                        
                        if (mapPage != null)
                        {
                            // Try to get the map area using reflection
                            foreach (var fieldName in new[] { "mapArea", "area", "_mapArea", "mapDisplayArea" })
                            {
                                var field = mapPage.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                if (field != null)
                                {
                                    var value = field.GetValue(mapPage);
                                    if (value is Rectangle rect)
                                    {
                                        return rect;
                                    }
                                }
                            }
                        }
                    }
                }
                
                // If we get here, we couldn't find the map area - use the fallback
                return fallbackArea;
            }
            catch (Exception ex)
            {
                monitor.Log($"Error getting map area: {ex.Message}", LogLevel.Debug);
                return fallbackArea;
            }
        }
        
        /// <summary>Get the map page from the game menu using reflection.</summary>
        private static IClickableMenu GetMapPage(GameMenu gameMenu, IMonitor monitor)
        {
            try
            {
                var pagesField = gameMenu.GetType().GetField("pages", BindingFlags.Instance | BindingFlags.NonPublic);
                if (pagesField != null)
                {
                    var pagesValue = pagesField.GetValue(gameMenu);
                    if (pagesValue != null)
                    {
                        if (pagesValue is List<IClickableMenu> pagesList && GameMenu.mapTab < pagesList.Count)
                        {
                            return pagesList[GameMenu.mapTab];
                        }
                        else if (pagesValue is IClickableMenu[] pagesArray && GameMenu.mapTab < pagesArray.Length)
                        {
                            return pagesArray[GameMenu.mapTab];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                monitor.Log($"Error getting map page: {ex.Message}", LogLevel.Debug);
            }
            
            return null;
        }
        
        /// <summary>Converts a map point to a game location name and position.</summary>
        private static (string locationName, Point position)? ConvertMapPointToLocation(Point point, IClickableMenu mapPage, IMonitor monitor)
        {
            try
            {
                // Access the map's point to location conversion method using reflection
                var method = mapPage.GetType().GetMethod("GetLocationFromClickPosition", 
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
                    null, new[] { typeof(int), typeof(int) }, null);
                
                // If we found the method, invoke it to get the location
                if (method != null)
                {
                    var result = method.Invoke(mapPage, new object[] { point.X, point.Y });
                    if (result != null)
                    {
                        // The result should be a GameLocation
                        if (result is GameLocation location)
                        {
                            monitor.Log($"Found location directly: {location.Name}", LogLevel.Debug);
                            
                            // Get a reasonable position within this location (center or entrance)
                            Point position = GetDefaultPositionForLocation(location.Name);
                            return (location.Name, position);
                        }
                    }
                }
                
                // Try a different approach: see if there's a different method that maps points to names
                method = mapPage.GetType().GetMethod("GetLocationNameAtPoint", 
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
                    null, new[] { typeof(int), typeof(int) }, null);
                
                if (method != null)
                {
                    var result = method.Invoke(mapPage, new object[] { point.X, point.Y });
                    if (result is string locationName && !string.IsNullOrEmpty(locationName))
                    {
                        monitor.Log($"Found location by name: {locationName}", LogLevel.Debug);
                        
                        // Get a reasonable position within this location
                        Point position = GetDefaultPositionForLocation(locationName);
                        return (locationName, position);
                    }
                }
                
                // If the specific methods aren't found, try to get the point translation directly
                var pointToLocationField = mapPage.GetType().GetField("pointToLocationLookup", 
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                
                if (pointToLocationField != null)
                {
                    var lookup = pointToLocationField.GetValue(mapPage);
                    if (lookup is Dictionary<Rectangle, string> rectToNameLookup)
                    {
                        foreach (var pair in rectToNameLookup)
                        {
                            if (pair.Key.Contains(point.X, point.Y))
                            {
                                monitor.Log($"Found location from rect lookup: {pair.Value}", LogLevel.Debug);
                                
                                // Get a reasonable position
                                Point position = GetDefaultPositionForLocation(pair.Value);
                                return (pair.Value, position);
                            }
                        }
                    }
                }
                
                // If we can't find it directly, fall back to our region-based approach
                return DetermineLocationFromPoint(point, mapPage, monitor);
            }
            catch (Exception ex)
            {
                monitor.Log($"Error converting map point: {ex.Message}", LogLevel.Debug);
                
                // Fall back to our default region mapping
                return DetermineLocationFromPoint(point, mapPage, monitor);
            }
        }
        
        /// <summary>Determines a location from a point using manual mapping.</summary>
        private static (string locationName, Point position)? DetermineLocationFromPoint(Point point, IClickableMenu mapPage, IMonitor monitor)
        {
            // Get the map area for calculating relative position
            Rectangle mapArea = new Rectangle(0, 0, 0, 0);
            
            try
            {
                var mapAreaField = mapPage.GetType().GetField("mapArea", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (mapAreaField != null)
                {
                    var value = mapAreaField.GetValue(mapPage);
                    if (value is Rectangle rect)
                    {
                        mapArea = rect;
                    }
                }
            }
            catch
            {
                // Fallback for map area
                mapArea = new Rectangle(mapPage.xPositionOnScreen + 30, mapPage.yPositionOnScreen + 30, 
                                       mapPage.width - 60, mapPage.height - 100);
            }
            
            if (mapArea.Width == 0 || mapArea.Height == 0)
            {
                // Another fallback
                mapArea = new Rectangle(mapPage.xPositionOnScreen + 30, mapPage.yPositionOnScreen + 30, 
                                       mapPage.width - 60, mapPage.height - 100);
            }
            
            // Calculate relative position (0-1 range)
            float relativeX = (float)(point.X - mapArea.X) / mapArea.Width;
            float relativeY = (float)(point.Y - mapArea.Y) / mapArea.Height;
            
            monitor.Log($"Relative map position: ({relativeX}, {relativeY})", LogLevel.Debug);
            
            // Manual map region to location mapping
            string locationName = DetermineLocationFromMapPosition(relativeX, relativeY);
            
            // Get the game location to check dimensions
            GameLocation gameLocation = Game1.getLocationFromName(locationName);
            if (gameLocation == null)
            {
                monitor.Log($"Location not found: {locationName}", LogLevel.Debug);
                return null;
            }
            
            // Get map dimensions
            int mapWidth = gameLocation.map.Layers[0].LayerWidth;
            int mapHeight = gameLocation.map.Layers[0].LayerHeight;
            
            // Calculate position based on relative click within the location
            Point position = ProjectRelativePositionToLocation(locationName, relativeX, relativeY, mapWidth, mapHeight);
            
            return (locationName, position);
        }
        
        /// <summary>Maps a relative position to coordinates in a specific location.</summary>
        private static Point ProjectRelativePositionToLocation(string locationName, float relativeX, float relativeY, int mapWidth, int mapHeight)
        {
            int x, y;
            
            // Project the click coordinates differently for each location based on its layout
            switch (locationName)
            {
                case "Town":
                    // Town center is roughly at (40, 64)
                    float townOffsetX = relativeX - 0.5f; // -0.5 to 0.5
                    float townOffsetY = relativeY - 0.5f; // -0.5 to 0.5
                    
                    x = 40 + (int)(townOffsetX * 60); // Scale offset
                    y = 64 + (int)(townOffsetY * 60);
                    break;
                
                case "Farm":
                    // Farm's playable area is roughly centered
                    x = (int)(relativeX * mapWidth * 0.6f) + (int)(mapWidth * 0.2f);
                    y = (int)(relativeY * mapHeight * 0.6f) + (int)(mapHeight * 0.2f);
                    break;
                
                case "Mountain":
                    // Mountain area (scale based on layout)
                    x = (int)(relativeX * 0.7f * mapWidth) + (int)(mapWidth * 0.15f);
                    y = (int)(relativeY * 0.7f * mapHeight) + (int)(mapHeight * 0.15f);
                    break;
                
                case "Forest":
                    // Cindersap Forest has water at bottom and right edges
                    x = (int)(relativeX * mapWidth * 0.7f) + (int)(mapWidth * 0.1f);
                    y = (int)(relativeY * mapHeight * 0.7f) + (int)(mapHeight * 0.1f);
                    break;
                
                case "Beach":
                    // Beach has water on right and bottom
                    x = (int)(relativeX * mapWidth * 0.6f) + (int)(mapWidth * 0.1f);
                    y = (int)(relativeY * mapHeight * 0.5f) + (int)(mapHeight * 0.1f);
                    break;
                
                case "BusStop":
                    // Bus Stop is a narrow area
                    x = (int)(relativeX * mapWidth * 0.7f) + (int)(mapWidth * 0.1f);
                    y = (int)(relativeY * mapHeight * 0.4f) + (int)(mapHeight * 0.3f);
                    break;
                
                case "Desert":
                    // Desert is mostly open
                    x = (int)(relativeX * mapWidth * 0.7f) + (int)(mapWidth * 0.15f);
                    y = (int)(relativeY * mapHeight * 0.7f) + (int)(mapHeight * 0.15f);
                    break;
                
                case "Mine":
                    // Default to mine entrance
                    x = 18;
                    y = 12;
                    break;
                
                case "Railroad":
                    // Railroad area
                    x = (int)(relativeX * mapWidth * 0.7f) + (int)(mapWidth * 0.15f);
                    y = (int)(relativeY * mapHeight * 0.7f) + (int)(mapHeight * 0.15f);
                    break;
                
                case "WizardHouse":
                    // Default to entrance
                    x = 4;
                    y = 20;
                    break;
                
                case "Woods":
                    // Secret Woods
                    x = 44;
                    y = 13;
                    break;
                
                default:
                    // General case - aim for center area of map
                    x = (int)(relativeX * mapWidth * 0.6f) + (int)(mapWidth * 0.2f);
                    y = (int)(relativeY * mapHeight * 0.6f) + (int)(mapHeight * 0.2f);
                    break;
            }
            
            // Ensure coordinates are within valid range
            x = Math.Clamp(x, 1, mapWidth - 2);
            y = Math.Clamp(y, 1, mapHeight - 2);
            
            return new Point(x, y);
        }
        
        /// <summary>Get a default position for a location (typically entrances or center).</summary>
        public static Point GetDefaultPositionForLocation(string locationName)
        {
            // Check cache first to avoid recalculating 
            if (ValidWarpPositionCache.TryGetValue(locationName, out Point cachedPosition))
            {
                return cachedPosition;
            }
            
            // Standard positions for vanilla locations
            Point position;
            switch (locationName)
            {
                case "Town": position = new Point(29, 67); break;
                case "Farm": position = new Point(64, 15); break;
                case "FarmHouse": position = new Point(9, 9); break;
                case "Mountain": position = new Point(42, 25); break;
                case "Forest": position = new Point(59, 15); break;
                case "Beach": position = new Point(29, 36); break;
                case "BusStop": position = new Point(15, 23); break;
                case "Desert": position = new Point(35, 43); break;
                case "SkullCave": position = new Point(3, 4); break;
                case "Mine": position = new Point(18, 12); break;
                case "Sewer": position = new Point(31, 15); break;
                case "WizardHouse": position = new Point(4, 20); break;
                case "Woods": position = new Point(44, 13); break;
                case "Railroad": position = new Point(35, 53); break;
                case "CommunityCenter": position = new Point(32, 13); break;
                case "ScienceHouse": position = new Point(7, 22); break;
                case "SeedShop": position = new Point(8, 23); break;
                case "JojaMart": position = new Point(13, 28); break;
                case "Saloon": position = new Point(17, 18); break;
                case "HaleyHouse": position = new Point(7, 9); break;
                case "SamHouse": position = new Point(7, 9); break;
                case "Blacksmith": position = new Point(7, 14); break;
                case "Hospital": position = new Point(9, 17); break;
                case "ManorHouse": position = new Point(5, 6); break;
                case "ElliottHouse": position = new Point(7, 9); break;
                case "LeahHouse": position = new Point(7, 9); break;
                case "AnimalShop": position = new Point(12, 16); break;
                case "SebastianRoom": position = new Point(6, 4); break;
                case "JoshHouse": position = new Point(7, 9); break;
                case "Trailer": position = new Point(14, 6); break;
                case "Trailer_Big": position = new Point(14, 6); break;
                
                // SVE locations
                case "Custom_Backwoods": position = new Point(30, 30); break;
                case "Custom_AdventureGuild": position = new Point(6, 9); break;
                case "Summit": position = new Point(15, 32); break;
                case "Custom_DeepWoods": position = new Point(15, 15); break;
                case "Custom_ForestWest": position = new Point(22, 18); break;
                case "Custom_GramplesHouse": position = new Point(7, 9); break;
                case "Custom_FairhavenFarm": position = new Point(35, 27); break;
                case "Custom_SophiaFarm": position = new Point(20, 14); break;
                case "Custom_BlueMoonVineyard": position = new Point(19, 42); break;
                case "Custom_AuroraVineyard": position = new Point(19, 31); break;
                case "Custom_MorrisJoja": position = new Point(12, 14); break;
                case "Custom_JenkinsHouse": position = new Point(8, 11); break;
                case "Custom_SusanHouse": position = new Point(7, 15); break;
                case "Custom_AndyHouse": position = new Point(6, 9); break;
                case "Custom_MartinHouse": position = new Point(5, 9); break;
                case "Custom_VictorHouse": position = new Point(8, 10); break;
                case "Custom_OliviaBedroom": position = new Point(7, 8); break;
                case "Custom_ClaireRoom": position = new Point(5, 5); break;
                case "Custom_MorrisApartment": position = new Point(9, 5); break;
                case "Custom_IridiumQuarry": position = new Point(18, 32); break;
                case "Custom_AdventurerCabin": position = new Point(10, 7); break;
                case "Custom_TreasureCave": position = new Point(16, 30); break;
                
                // Default for unknown/modded locations
                default:
                    position = FindValidPositionForLocation(locationName);
                    break;
            }
            
            // Add to cache
            ValidWarpPositionCache[locationName] = position;
            return position;
        }
        
        /// <summary>
        /// For unknown/modded locations, try to find a valid position
        /// </summary>
        private static Point FindValidPositionForLocation(string locationName)
        {
            GameLocation location = Game1.getLocationFromName(locationName);
            if (location == null)
            {
                return new Point(20, 20);  // Fallback default
            }
            
            // Try to find entrances first
            foreach (var warp in location.warps)
            {
                // Look for a position just inside the warp entrance
                int searchX = warp.TargetX;
                int searchY = warp.TargetY;
                
                if (IsTileValid(location, searchX, searchY))
                {
                    return new Point(searchX, searchY);
                }
            }
            
            // If no valid entrance found, try to find center of the map
            int midX = location.map.Layers[0].LayerWidth / 2;
            int midY = location.map.Layers[0].LayerHeight / 2;
            
            // Try center and search outward in a spiral
            for (int radius = 0; radius < 20; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        // Skip tiles we've already checked
                        if (Math.Abs(dx) < radius && Math.Abs(dy) < radius) continue;
                        
                        int testX = midX + dx;
                        int testY = midY + dy;
                        
                        if (IsTileValid(location, testX, testY))
                        {
                            return new Point(testX, testY);
                        }
                    }
                }
            }
            
            // Last resort - try standard positions
            int[] standardCoords = { 10, 15, 20, 25, 30 };
            
            foreach (int x in standardCoords)
            {
                foreach (int y in standardCoords)
                {
                    if (IsTileValid(location, x, y))
                    {
                        return new Point(x, y);
                    }
                }
            }
            
            // Ultimate fallback - this might not be valid but we've tried everything else
            return new Point(20, 20);
        }
        
        /// <summary>Checks if a tile is valid to warp to.</summary>
        public static bool IsTileValid(GameLocation location, int tileX, int tileY)
        {
            try
            {
                // Check if tile is within bounds
                if (location == null || location.map == null || location.map.Layers.Count == 0)
                    return false;
                    
                if (tileX < 0 || tileY < 0 || tileX >= location.map.Layers[0].LayerWidth || tileY >= location.map.Layers[0].LayerHeight)
                    return false;
                
                // Try a faster check first
                if (FastTileCheck(location, tileX, tileY))
                {
                    // If it passes fast check, do a more thorough check
                    // Check if tile is passable (not colliding)
                    // Use a slightly modified rectangle to avoid edge case issues
                    if (location.isCollidingPosition(new Rectangle(tileX * 64 + 1, tileY * 64 + 1, 62, 62), Game1.viewport, true, 0, false, Game1.player))
                        return false;
                    
                    // Check for water or deep water
                    if (location.isWaterTile(tileX, tileY))
                        return false;
                        
                    if (location.doesTileHaveProperty(tileX, tileY, "Water", "Back") != null)
                        return false;
                        
                    // Some special case checks for modded locations
                    // For SVE/modded locations, be more lenient with collision detection
                    if (location.Name.StartsWith("Custom_") || location.Name.StartsWith("SVE_"))
                    {
                        // Special case: location is modded, check only critical issues
                        Vector2 tileVec = new Vector2(tileX, tileY);
                        
                        // The method doesn't return a bool, it returns a Farmer or null
                        bool noFarmer = location.isTileOccupiedByFarmer(tileVec) == null;
                        bool noObjects = !location.Objects.ContainsKey(tileVec);
                        return noFarmer && noObjects;
                    }
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception)
            {
                // If any error, consider tile invalid
                return false;
            }
        }
        
        /// <summary>Quick check to determine if a tile is potentially valid</summary>
        private static bool FastTileCheck(GameLocation location, int x, int y) 
        {
            try
            {
                // Check for special "NoSpawn" property that could be added by mods
                if (location.doesTileHaveProperty(x, y, "NoSpawn", "Back") != null)
                    return false;
                    
                // Check tile properties for common Passable tag
                if (location.doesTileHaveProperty(x, y, "Passable", "Buildings") != null) 
                    return false;
                    
                // Check for cliff edges
                string terrain = location.doesTileHaveProperty(x, y, "Type", "Back");
                if (terrain != null && (terrain == "Wood" || terrain == "Stone"))
                    return false;
                    
                // For farm, check buildings
                if (location is StardewValley.Farm farm)
                {
                    foreach (var building in farm.buildings)
                    {
                        if (building.occupiesTile(x, y))
                            return false;
                    }
                }
                
                // Check for NPC/Character collision
                foreach (var character in location.characters)
                {
                    Vector2 charTile = new Vector2((int)(character.Position.X / 64), (int)(character.Position.Y / 64));
                    if (charTile.X == x && charTile.Y == y)
                        return false;
                }
                
                // Check for solid objects
                if (location.objects.ContainsKey(new Vector2(x, y)))
                {
                    var obj = location.objects[new Vector2(x, y)];
                    if (obj != null && !obj.isPassable())
                        return false;
                }
                
                return true;
            }
            catch
            {
                return true; // Continue to full check on error
            }
        }
        
        /// <summary>Finds a nearby valid tile if the target is invalid.</summary>
        public static Point? FindNearbyValidTile(GameLocation location, int centerX, int centerY)
        {
            // Try tiles in expanding radius around the center point
            for (int radius = 0; radius <= 10; radius++)
            {
                // Check tiles in a spiral pattern, starting from the center
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        // Skip tiles we've already checked (inner radius)
                        if (Math.Abs(dx) < radius && Math.Abs(dy) < radius)
                            continue;
                        
                        int x = centerX + dx;
                        int y = centerY + dy;
                        
                        if (IsTileValid(location, x, y))
                            return new Point(x, y);
                    }
                }
            }
            
            // If we still haven't found a valid tile, try some fixed positions
            int[] backupXCoordinates = { 10, 15, 20, 25, 30 };
            int[] backupYCoordinates = { 10, 15, 20, 25, 30 };
            
            foreach (int x in backupXCoordinates)
            {
                foreach (int y in backupYCoordinates)
                {
                    if (IsTileValid(location, x, y))
                        return new Point(x, y);
                }
            }
            
            // Try potential warps
            foreach (var warp in location.warps)
            {
                if (IsTileValid(location, warp.TargetX, warp.TargetY))
                {
                    return new Point(warp.TargetX, warp.TargetY);
                }
            }
            
            return null;
        }
        
        /// <summary>Converts a click on the map to a game location and tile coordinates.</summary>
        public static MapClickResult? ConvertMapClickToGameLocation(int x, int y, Rectangle mapArea, IMonitor monitor)
        {
            try
            {
                // Get the current GameMenu and its MapPage
                var gameMenu = Game1.activeClickableMenu as GameMenu;
                if (gameMenu == null)
                    return null;

                var mapPage = GetMapPage(gameMenu, monitor);
                if (mapPage == null)
                    return null;

                // Get the actual click point relative to the map
                Point clickPoint = new Point(x, y);

                // Try to get the location directly from the game's map page
                var locationResult = ConvertMapPointToLocation(clickPoint, mapPage, monitor);
                if (!locationResult.HasValue)
                {
                    monitor.Log("Could not determine location from click point", LogLevel.Debug);
                    return null;
                }

                string locationName = locationResult.Value.locationName;
                Point position = locationResult.Value.position;

                // Get the location to verify it exists
                var gameLocation = Game1.getLocationFromName(locationName);
                if (gameLocation == null)
                {
                    monitor.Log($"Location '{locationName}' does not exist", LogLevel.Debug);
                    return null;
                }

                // Check if the tile is walkable/valid
                if (!IsTileValid(gameLocation, position.X, position.Y))
                {
                    // Try to find a nearby valid tile
                    var validTile = FindNearbyValidTile(gameLocation, position.X, position.Y);
                    if (!validTile.HasValue)
                    {
                        monitor.Log($"No valid tile found near ({position.X}, {position.Y}) in {locationName}", LogLevel.Debug);
                        return null;
                    }
                    
                    position = validTile.Value;
                }

                monitor.Log($"Converted to location: {locationName}, tile: ({position.X}, {position.Y})", LogLevel.Debug);
                
                return new MapClickResult { 
                    LocationName = locationName, 
                    TileX = position.X, 
                    TileY = position.Y 
                };
            }
            catch (Exception ex)
            {
                monitor.Log($"Error converting map click: {ex.Message}", LogLevel.Error);
                return null;
            }
        }
        
        /// <summary>Determines the location name based on map position.</summary>
        private static string DetermineLocationFromMapPosition(float relativeX, float relativeY)
        {
            // Improved map regions that better match the actual Stardew Valley map layout
            
            // Town area (center of map)
            if (relativeX > 0.4f && relativeX < 0.6f && relativeY > 0.4f && relativeY < 0.6f)
                return "Town";
            
            // Farm area (upper left quadrant)
            if (relativeX > 0.2f && relativeX < 0.4f && relativeY > 0.15f && relativeY < 0.35f)
                return "Farm";
            
            // Forest area (lower left quadrant)
            if (relativeX > 0.05f && relativeX < 0.3f && relativeY > 0.6f && relativeY < 0.95f)
                return "Forest";
            
            // Mountain area (upper right quadrant)
            if (relativeX > 0.6f && relativeX < 0.85f && relativeY > 0.15f && relativeY < 0.35f)
                return "Mountain";
            
            // Beach area (bottom center/right)
            if (relativeX > 0.5f && relativeX < 0.8f && relativeY > 0.7f && relativeY < 0.95f)
                return "Beach";
            
            // Backwoods/Bus Stop (upper center)
            if (relativeX > 0.35f && relativeX < 0.5f && relativeY > 0.1f && relativeY < 0.3f)
                return "BusStop";
            
            // Desert (far top left, if unlocked)
            if (relativeX < 0.15f && relativeY < 0.15f && Game1.player.mailReceived.Contains("ccVault"))
                return "Desert";
            
            // Mine (top right corner)
            if (relativeX > 0.8f && relativeY < 0.2f)
                return "Mine";
            
            // Wizard's Tower (bottom left)
            if (relativeX < 0.15f && relativeY > 0.8f)
                return "WizardHouse";
            
            // Cindersap Forest (bottom left - more specific part)
            if (relativeX > 0.05f && relativeX < 0.25f && relativeY > 0.7f && relativeY < 0.9f)
                return "Forest";
                
            // Railroad (top of map)
            if (relativeX > 0.5f && relativeX < 0.8f && relativeY < 0.1f)
                return "Railroad";
                
            // Sewer (if unlocked)
            if (relativeX > 0.45f && relativeX < 0.55f && relativeY > 0.6f && relativeY < 0.7f && Game1.player.hasRustyKey)
                return "Sewer";
                
            // Secret Woods (lower left, specific location)
            if (relativeX > 0.05f && relativeX < 0.15f && relativeY > 0.5f && relativeY < 0.65f)
                return "Woods";
            
            // If no specific match but in general map area, try to determine best location
            if (relativeX > 0 && relativeX < 1 && relativeY > 0 && relativeY < 1)
            {
                // Left side of map
                if (relativeX < 0.3f)
                {
                    if (relativeY < 0.5f)
                        return "Farm";
                    else
                        return "Forest";
                }
                // Right side of map
                else if (relativeX > 0.7f)
                {
                    if (relativeY < 0.4f)
                        return "Mountain";
                    else if (relativeY > 0.7f)
                        return "Beach";
                    else
                        return "Town";
                }
                // Center area
                else
                {
                    if (relativeY < 0.3f)
                        return "BusStop";
                    else if (relativeY > 0.7f)
                        return "Beach";
                    else
                        return "Town";
                }
            }
            
            // Default to Town if no match at all
            return "Town";
        }
        
        /// <summary>Access Stardew Valley's internal map click handling logic</summary>
        private static (GameLocation location, Vector2 position)? GetMapClickLocation(IClickableMenu mapPage, int x, int y, IMonitor monitor)
        {
            try
            {
                // Get the map area to help with coordinate translation
                Rectangle mapArea = GetMapAreaSafely(Game1.activeClickableMenu as GameMenu, monitor);
                
                // Calculate relative position
                float relativeX = (float)(x - mapArea.X) / mapArea.Width;
                float relativeY = (float)(y - mapArea.Y) / mapArea.Height;

                // First try to use the local map images for precise coordinate mapping
                var mapImageCoords = GetCoordinatesFromMapImage(x, y, mapArea, monitor);
                if (mapImageCoords.HasValue)
                {
                    var gameLocation = Game1.getLocationFromName(mapImageCoords.Value.locationName);
                    if (gameLocation != null)
                    {
                        return (gameLocation, new Vector2(mapImageCoords.Value.x, mapImageCoords.Value.y));
                    }
                }

                // Fall back to using game's internal mapping if map image approach fails
                var getWorldPointMethod = mapPage.GetType().GetMethod("transformWorldPoint",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(int), typeof(int) },
                    null);

                if (getWorldPointMethod != null)
                {
                    // If we have a method to transform the world point, log and continue
                    monitor.Log($"Relative map position: ({relativeX}, {relativeY})", LogLevel.Debug);
                }
                
                monitor.Log($"Relative map position: ({relativeX}, {relativeY})", LogLevel.Debug);

                return null;
            }
            catch (Exception ex)
            {
                monitor.Log($"Error in GetMapClickLocation: {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        /// <summary>Get coordinates from map images for more precise location mapping</summary>
        private static (string locationName, int x, int y)? GetCoordinatesFromMapImage(int clickX, int clickY, Rectangle mapArea, IMonitor monitor)
        {
            try
            {
                // Calculate relative position within the map area
                float relativeX = (float)(clickX - mapArea.X) / mapArea.Width;
                float relativeY = (float)(clickY - mapArea.Y) / mapArea.Height;

                // Map known locations to their image files and coordinate ranges
                var locationMaps = new Dictionary<string, (string imageFile, Rectangle bounds)>
                {
                    { "Town", ("Town.png", new Rectangle(0, 0, 128, 112)) },
                    { "Beach", ("Beach.png", new Rectangle(0, 0, 100, 42)) },
                    { "Mountain", ("Mountain.png", new Rectangle(0, 0, 135, 41)) },
                    { "BusStop", ("BusStop.png", new Rectangle(0, 0, 35, 30)) },
                    { "Forest", ("Forest.png", new Rectangle(0, 0, 137, 88)) },
                    { "Railroad", ("Railroad.png", new Rectangle(0, 0, 30, 25)) }
                    // Add more locations as needed
                };

                // Determine which location was clicked based on map position
                string locationName = DetermineLocationFromMapPosition(relativeX, relativeY);
                if (string.IsNullOrEmpty(locationName) || !locationMaps.ContainsKey(locationName))
                {
                    return null;
                }

                var (imageFile, bounds) = locationMaps[locationName];

                // Calculate tile coordinates based on the map image dimensions
                int tileX = (int)(relativeX * bounds.Width);
                int tileY = (int)(relativeY * bounds.Height);

                // Adjust coordinates based on the specific location's layout
                switch (locationName)
                {
                    case "Town":
                        // Town center adjustment
                        tileX = (int)(bounds.Width * (relativeX - 0.5f) + 64);
                        tileY = (int)(bounds.Height * (relativeY - 0.5f) + 64);
                        break;

                    case "Beach":
                        // Beach has water on the sides, adjust to keep player on land
                        tileX = Math.Clamp(tileX, 10, bounds.Width - 10);
                        tileY = Math.Clamp(tileY, 5, bounds.Height - 5);
                        break;

                    case "Mountain":
                        // Mountain area has lakes and cliffs
                        tileX = Math.Clamp(tileX, 5, bounds.Width - 5);
                        tileY = Math.Clamp(tileY, 5, bounds.Height - 5);
                        break;

                    case "Forest":
                        // Forest has water and obstacles
                        tileX = Math.Clamp(tileX, 10, bounds.Width - 15);
                        tileY = Math.Clamp(tileY, 10, bounds.Height - 15);
                        break;
                }

                monitor.Log($"Mapped to {locationName} coordinates: ({tileX}, {tileY})", LogLevel.Debug);
                return (locationName, tileX, tileY);
            }
            catch (Exception ex)
            {
                monitor.Log($"Error mapping coordinates from image: {ex.Message}", LogLevel.Error);
                return null;
            }
        }
    }
}