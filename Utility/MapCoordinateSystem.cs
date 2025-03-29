using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;

namespace WarpMod.Utility
{
    public class MapCoordinateSystem
    {
        // Known coordinate mappings for each location
        private static readonly Dictionary<string, LocationData> LocationMappings = new Dictionary<string, LocationData>
        {
            {
                "Town", new LocationData(
                    worldX: 29, worldY: 67,  // Center town coordinates
                    mapBounds: new Rectangle(0, 0, 128, 112),
                    validRegions: new List<Rectangle> {
                        new Rectangle(15, 15, 100, 80)  // Main town area
                    }
                )
            },
            {
                "Farm", new LocationData(
                    worldX: 64, worldY: 15,  // Farm entrance
                    mapBounds: new Rectangle(0, 0, 80, 65),
                    validRegions: new List<Rectangle> {
                        new Rectangle(10, 10, 60, 45)  // Main farm area
                    }
                )
            },
            {
                "Beach", new LocationData(
                    worldX: 29, worldY: 36,  // Beach entrance
                    mapBounds: new Rectangle(0, 0, 100, 42),
                    validRegions: new List<Rectangle> {
                        new Rectangle(5, 5, 90, 32)  // Main beach area
                    }
                )
            },
            {
                "Mountain", new LocationData(
                    worldX: 42, worldY: 25,  // Mountain lake area
                    mapBounds: new Rectangle(0, 0, 135, 41),
                    validRegions: new List<Rectangle> {
                        new Rectangle(5, 5, 125, 31)  // Main mountain area
                    }
                )
            },
            {
                "Forest", new LocationData(
                    worldX: 59, worldY: 15,  // Forest entrance
                    mapBounds: new Rectangle(0, 0, 137, 88),
                    validRegions: new List<Rectangle> {
                        new Rectangle(5, 5, 127, 78)  // Main forest area
                    }
                )
            },
            {
                "BusStop", new LocationData(
                    worldX: 15, worldY: 23,  // Bus stop
                    mapBounds: new Rectangle(0, 0, 35, 30),
                    validRegions: new List<Rectangle> {
                        new Rectangle(5, 5, 25, 20)  // Main bus stop area
                    }
                )
            }
        };

        /// <summary>
        /// Map coordinate system for a specific location
        /// </summary>
        private GameLocation currentLocation;

        /// <summary>
        /// Constructor that initializes the coordinate system for a specified location
        /// </summary>
        /// <param name="location">The game location to create a coordinate system for</param>
        public MapCoordinateSystem(GameLocation location)
        {
            this.currentLocation = location;
        }

        /// <summary>
        /// Constructor that takes just a map name
        /// </summary>
        /// <param name="mapName">The name of the map to load</param>
        public MapCoordinateSystem(string mapName)
        {
            if (string.IsNullOrEmpty(mapName))
            {
                throw new ArgumentException("Map name cannot be null or empty", nameof(mapName));
            }
            
            // Try to get the location from the game
            GameLocation location = Game1.getLocationFromName(mapName);
            if (location == null)
            {
                throw new ArgumentException($"Could not find location: {mapName}", nameof(mapName));
            }
            
            // Initialize with the found location
            this.currentLocation = location;
        }

        /// <summary>
        /// Location data containing mapping information
        /// </summary>
        public class LocationData
        {
            public int WorldX { get; }
            public int WorldY { get; }
            public Rectangle MapBounds { get; }
            public List<Rectangle> ValidRegions { get; }

            public LocationData(int worldX, int worldY, Rectangle mapBounds, List<Rectangle> validRegions)
            {
                WorldX = worldX;
                WorldY = worldY;
                MapBounds = mapBounds;
                ValidRegions = validRegions;
            }
        }

        /// <summary>
        /// Convert map coordinates to game world coordinates for a specific location
        /// </summary>
        public static Point? ConvertToGameCoordinates(string locationName, float relativeX, float relativeY)
        {
            if (!LocationMappings.TryGetValue(locationName, out LocationData locationData))
                return null;

            // Convert relative coordinates (0-1) to absolute map coordinates
            int mapX = (int)(relativeX * locationData.MapBounds.Width);
            int mapY = (int)(relativeY * locationData.MapBounds.Height);

            // Check if the point is in any valid region
            bool inValidRegion = false;
            foreach (var region in locationData.ValidRegions)
            {
                if (region.Contains(mapX, mapY))
                {
                    inValidRegion = true;
                    break;
                }
            }

            if (!inValidRegion)
                return null;

            // Convert map coordinates to world coordinates
            float percentX = (float)mapX / locationData.MapBounds.Width;
            float percentY = (float)mapY / locationData.MapBounds.Height;

            // Get the GameLocation to check dimensions
            GameLocation gameLocation = Game1.getLocationFromName(locationName);
            if (gameLocation == null)
                return null;

            int worldWidth = gameLocation.map.Layers[0].LayerWidth;
            int worldHeight = gameLocation.map.Layers[0].LayerHeight;

            // Calculate final coordinates
            int finalX = (int)(percentX * worldWidth);
            int finalY = (int)(percentY * worldHeight);

            return new Point(finalX, finalY);
        }

        /// <summary>
        /// Converts a position on the map to a game tile position
        /// </summary>
        /// <param name="position">The position on the map (in pixels)</param>
        /// <returns>The corresponding game tile position</returns>
        public Vector2 MapPositionToGameTile(Point position)
        {
            if (currentLocation == null || currentLocation.map == null)
                return Vector2.Zero;
                
            // Calculate relative position within map view
            float relativeX = position.X / (float)currentLocation.map.DisplayWidth;
            float relativeY = position.Y / (float)currentLocation.map.DisplayHeight;
            
            // Convert to tile coordinates
            int tileX = (int)(relativeX * currentLocation.map.Layers[0].LayerWidth);
            int tileY = (int)(relativeY * currentLocation.map.Layers[0].LayerHeight);
            
            // Ensure the coordinates are within the map bounds
            tileX = Math.Max(0, Math.Min(tileX, currentLocation.map.Layers[0].LayerWidth - 1));
            tileY = Math.Max(0, Math.Min(tileY, currentLocation.map.Layers[0].LayerHeight - 1));
            
            return new Vector2(tileX, tileY);
        }

        /// <summary>
        /// Converts a position on the map image to a game tile coordinate
        /// </summary>
        /// <param name="position">The position on the map image (in pixels)</param>
        /// <returns>The corresponding game tile coordinates</returns>
        public Vector2 MapPositionToGameTile(Vector2 position)
        {
            if (currentLocation == null || currentLocation.map == null)
                return Vector2.Zero;
                
            // Apply scaling and offset transformations
            float tileX = position.X / currentLocation.map.DisplayWidth;
            float tileY = position.Y / currentLocation.map.DisplayHeight;
            
            // Convert to actual tile coordinates
            int pixelX = (int)(tileX * currentLocation.map.DisplayWidth);
            int pixelY = (int)(tileY * currentLocation.map.DisplayHeight);
            
            // Convert pixels to tiles
            int gameTileX = pixelX / Game1.tileSize;
            int gameTileY = pixelY / Game1.tileSize;
            
            return new Vector2(gameTileX, gameTileY);
        }

        /// <summary>
        /// Converts a normalized position (0-1) on the map to game tile coordinates
        /// </summary>
        public Point MapPositionToGameTile(float normalizedX, float normalizedY)
        {
            // Scale normalized position to map size
            int pixelX = (int)(normalizedX * currentLocation.map.DisplayWidth);
            int pixelY = (int)(normalizedY * currentLocation.map.DisplayHeight);
            
            // Convert to game tile coordinates
            int tileX = pixelX / Game1.tileSize;
            int tileY = pixelY / Game1.tileSize;
            
            return new Point(tileX, tileY);
        }

        /// <summary>
        /// Gets the size of a location's map
        /// </summary>
        public static Rectangle? GetLocationBounds(string locationName)
        {
            if (LocationMappings.TryGetValue(locationName, out LocationData locationData))
                return locationData.MapBounds;
            return null;
        }

        /// <summary>
        /// Checks if a point is within valid areas of a location
        /// </summary>
        public static bool IsValidMapPoint(string locationName, int x, int y)
        {
            if (!LocationMappings.TryGetValue(locationName, out LocationData locationData))
                return false;

            foreach (var region in locationData.ValidRegions)
            {
                if (region.Contains(x, y))
                    return true;
            }

            return false;
        }
    }
}