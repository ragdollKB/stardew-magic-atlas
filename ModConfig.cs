using StardewModdingAPI;
using StardewModdingAPI.Utilities; // Add missing using for KeybindList

namespace WarpMod
{
    /// <summary>
    /// Configuration settings for the Magic Atlas mod
    /// </summary>
    public class ModConfig
    {
        /// <summary>
        /// Keybind to open the warp menu
        /// </summary>
        public KeybindList WarpKey { get; set; } = KeybindList.Parse("M");

        /// <summary>
        /// Whether the warping functionality is enabled
        /// </summary>
        public bool EnableWarping { get; set; } = true;

        /// <summary>
        /// Whether the Magic Atlas item is enabled
        /// </summary>
        public bool EnableMagicAtlas { get; set; } = true;

        /// <summary>
        /// Whether to allow using the warp key without having the Magic Atlas item
        /// </summary>
        public bool AllowWarpingWithoutItem { get; set; } = false;
        
        /// <summary>
        /// Whether to show locations from Stardew Valley Expanded (SVE)
        /// </summary>
        public bool ShowSVELocations { get; set; } = true;

        /// <summary>
        /// Whether to group modded locations into a separate category.
        /// </summary>
        public bool GroupModdedLocations { get; set; } = true; // Add missing property used in ModEntry

        /// <summary>
        /// Maximum number of locations to show per category tab. Fixed at 9.
        /// </summary>
        public int MaxLocationsPerCategory { get; set; } = 9; // Fixed value

        /// <summary>
        /// Whether to use visual effects when warping
        /// </summary>
        public bool UseWarpEffects { get; set; } = true;

        /// <summary>
        /// Whether to hide locations that the player hasn't unlocked yet.
        /// </summary>
        public bool HideLockedLocations { get; set; } = true; // Default to true
        
        /// <summary>
        /// Whether to allow warping to locations that might be dangerous (e.g., mines when low health).
        /// </summary>
        public bool AllowDangerousLocations { get; set; } = true;
        
        /// <summary>
        /// Whether to strictly enforce transportation schedules (bus times, etc.).
        /// </summary>
        public bool StrictTransportation { get; set; } = false;
        
        /// <summary>
        /// Whether to require friendship levels to enter personal spaces like bedrooms.
        /// </summary>
        public bool RequireFriendshipForHomes { get; set; } = false;

        /// <summary>
        /// Whether to show indoor locations (shops, houses) and special areas (mines, caves) in the warp menu.
        /// </summary>
        public bool ShowIndoorAndSpecialLocations { get; set; } = false; // Default to false for simpler map
    }
}