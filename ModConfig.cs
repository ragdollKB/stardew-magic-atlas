using StardewModdingAPI.Utilities;

namespace WarpMod
{
    public class ModConfig
    {
        /// <summary>Whether to enable the grid-based map warping functionality.</summary>
        public bool MapWarpEnabled { get; set; } = true;
        
        /// <summary>Whether to enable caching of map thumbnails for better performance.</summary>
        public bool EnableMapCaching { get; set; } = true;
        
        /// <summary>Whether to show modded locations in a separate category.</summary>
        public bool GroupModdedLocations { get; set; } = true;
        
        /// <summary>Whether to enable search functionality for modded locations.</summary>
        public bool EnableLocationSearch { get; set; } = true;
        
        /// <summary>Key binding to open the warp menu.</summary>
        public KeybindList WarpKey { get; set; } = KeybindList.Parse("K, ControllerBack");
        
        /// <summary>Whether to use a special animation effect when warping.</summary>
        public bool UseWarpEffects { get; set; } = true;
        
        /// <summary>Whether to prioritize SVE locations in the warp menu.</summary>
        public bool ShowSVELocations { get; set; } = true;
        
        /// <summary>Maximum number of locations to show in each category.</summary>
        public int MaxLocationsPerCategory { get; set; } = 8;
        
        /// <summary>The path to the folder containing extracted map images (.png files).</summary>
        public string MapImagesPath { get; set; } = "";
    }
}