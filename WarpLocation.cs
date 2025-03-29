using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace WarpMod
{
    /// <summary>Represents a warp destination with map name and coordinates.</summary>
    public class WarpLocation
    {
        public string MapName { get; }
        public int X { get; }
        public int Y { get; }
        public string Category { get; }
        public Vector2 GamePosition { get; } // Property for game world position
        public Func<bool> IsAvailable { get; } // Function to check if this location is available

        public WarpLocation(string mapName, int x, int y, string category = "Other", Vector2? gamePosition = null, Func<bool> isAvailable = null)
        {
            MapName = mapName;
            X = x;
            Y = y;
            Category = category;
            GamePosition = gamePosition ?? new Vector2(x * 100, y * 100); // Default approximation
            IsAvailable = isAvailable ?? (() => true); // Default to always available
        }
    }
}