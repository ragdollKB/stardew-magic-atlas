using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WarpMod.Utility
{
    /// <summary>
    /// Manages location data and categorization for the warp menu
    /// </summary>
    public class LocationManager
    {
        private readonly IMonitor Monitor;
        private readonly Dictionary<string, List<string>> locationTabs = new();
        private readonly HashSet<string> blacklistedLocations = new();
        private readonly Dictionary<string, Point> ValidWarpPositionCache = new Dictionary<string, Point>(); // Cache for default positions
        
        // The mod locations category name
        private const string MOD_LOCATIONS_CATEGORY = "Mod Locations";
        
        // Flag to control whether to show SVE locations
        private readonly bool showSVELocations;

        public LocationManager(IMonitor monitor, bool showSVELocations = true)
        {
            this.Monitor = monitor;
            this.showSVELocations = showSVELocations;
            InitializeCategories();
            InitializeBlacklist();
            LoadLocations();
        }

        /// <summary>
        /// Initialize empty category lists
        /// </summary>
        private void InitializeCategories()
        {
            // Main categories focused on major areas
            locationTabs["Farm"] = new List<string>();
            locationTabs["Town"] = new List<string>();
            locationTabs["Beach"] = new List<string>();
            locationTabs["Mountain"] = new List<string>();
            locationTabs["Forest"] = new List<string>();
            locationTabs["Desert"] = new List<string>();
            locationTabs["Island"] = new List<string>();
            
            // Add mod locations category if enabled
            if (showSVELocations)
            {
                locationTabs[MOD_LOCATIONS_CATEGORY] = new List<string>();
            }
        }
        
        /// <summary>
        /// Initialize blacklist of locations that shouldn't be included
        /// </summary>
        private void InitializeBlacklist()
        {
            // Add locations that should be excluded
            string[] excludedMaps = new[]
            {
                "Backwoods", "Temp", "Cutscene", "Credits", "Intro", "Mine",
                "UndergroundMine", "VolcanoDungeon", "Submarine", "BoatTunnel", 
                "Temp_", "_Ambient", "fishingGame", "festivalSpot"
            };
            
            foreach (var map in excludedMaps)
            {
                blacklistedLocations.Add(map);
            }
        }

        /// <summary>
        /// Load all available locations from the game and mods
        /// </summary>
        private void LoadLocations()
        {
            // Load locations directly from the game's location list
            foreach (var location in Game1.locations)
            {
                // Skip null locations and those without a name or map
                if (location == null || string.IsNullOrEmpty(location.Name) || location.map == null)
                    continue;
                    
                // Skip blacklisted locations
                if (IsBlacklisted(location.Name))
                    continue;
                
                // Categorize the location
                string category = DetermineMapCategory(location.Name);
                
                // Add location to appropriate category
                if (IsModdedLocation(location.Name) && showSVELocations)
                {
                    if (!locationTabs[MOD_LOCATIONS_CATEGORY].Contains(location.Name))
                    {
                        locationTabs[MOD_LOCATIONS_CATEGORY].Add(location.Name);
                    }
                }
                else
                {
                    // Handle specific location reassignments
                    if (location.Name == "BusStop" && !locationTabs["Farm"].Contains(location.Name))
                    {
                        locationTabs["Farm"].Add(location.Name);
                    }
                    else if (!locationTabs[category].Contains(location.Name))
                    {
                        locationTabs[category].Add(location.Name);
                    }
                }
            }
            
            // Sort locations within each category
            foreach (var category in locationTabs.Keys.ToList())
            {
                locationTabs[category].Sort();
            }

            // Make sure Farm is first in Farm category
            if (locationTabs["Farm"].Contains("Farm"))
            {
                locationTabs["Farm"].Remove("Farm");
                locationTabs["Farm"].Insert(0, "Farm");
            }

            // Make sure Forest is first in Forest category
            if (locationTabs["Forest"].Contains("Forest"))
            {
                locationTabs["Forest"].Remove("Forest");
                locationTabs["Forest"].Insert(0, "Forest");
            }
        }
        
        /// <summary>
        /// Check if a location is blacklisted
        /// </summary>
        private bool IsBlacklisted(string locationName)
        {
            foreach (var blacklisted in blacklistedLocations)
            {
                if (locationName.Contains(blacklisted))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Check if a location is from a mod
        /// </summary>
        private bool IsModdedLocation(string locationName)
        {
            // Check for common mod location prefixes
            string[] modLocationPatterns = { "Custom_", "SVE_", "DIGUS." };
            
            foreach (var pattern in modLocationPatterns)
            {
                if (locationName.Contains(pattern))
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Determine which category a map belongs to
        /// </summary>
        private string DetermineMapCategory(string mapName)
        {
            // Town locations
            if (mapName.Contains("Town") || mapName.Contains("Shop") || 
                mapName.Contains("Saloon") || mapName == "ManorHouse" || 
                mapName == "Blacksmith" || mapName == "Hospital" ||
                mapName.Contains("JojaMart"))
                return "Town";

            // Farm locations
            if (mapName.Contains("Farm") || mapName == "Greenhouse" || 
                mapName == "Cellar")
                return "Farm";

            // Beach locations
            if (mapName.Contains("Beach") || mapName == "ElliottHouse" || 
                mapName == "FishShop")
                return "Beach";

            // Desert locations
            if (mapName.Contains("Desert") || mapName == "Club")
                return "Desert";

            // Forest locations
            if (mapName.Contains("Forest") || mapName == "Woods" || 
                mapName == "WizardHouse" || mapName == "LeahHouse" || 
                mapName == "AnimalShop")
                return "Forest";

            // Mountain locations
            if (mapName.Contains("Mountain") || mapName == "Mine" || 
                mapName.Contains("Guild") || mapName.Contains("BathHouse"))
                return "Mountain";

            // Island locations
            if (mapName.Contains("Island") || mapName == "Caldera")
                return "Island";

            // Check if this is a modded location
            if (IsModdedLocation(mapName) && showSVELocations)
                return MOD_LOCATIONS_CATEGORY;

            // Default to Town if no match found
            return "Town";
        }

        /// <summary>
        /// Get a display-friendly name for a location
        /// </summary>
        public string GetDisplayName(string locationName)
        {
            // Use a dictionary for cleaner mapping and easier updates
            var nameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Town Locations (Referencing https://stardewvalleywiki.com/Category:Town_Locations)
                { "Town", "Pelican Town" },
                { "SeedShop", "Pierre's General Store" }, // Wiki name
                { "Saloon", "Stardrop Saloon" },
                { "ManorHouse", "Mayor's Manor" }, // Wiki name
                { "Blacksmith", "Blacksmith" },
                { "Hospital", "Harvey's Clinic" }, // Wiki name
                { "JojaMart", "JojaMart" },
                { "AbandonedJojaMart", "Abandoned JojaMart" },
                { "MovieTheater", "Movie Theater" },
                { "ArchaeologyHouse", "Museum" }, // Wiki name
                { "HarveyRoom", "Harvey's Room" },
                { "HaleyHouse", "2 Willow Lane" }, // Wiki name (Haley/Emily's House)
                { "SamHouse", "1 Willow Lane" }, // Wiki name (Jodi/Kent/Sam/Vincent's House)
                { "JoshHouse", "1 River Road" }, // Wiki name (George/Evelyn/Alex's House)
                { "CommunityCenter", "Community Center" },
                { "Sewer", "The Sewers" }, // Wiki name

                // Farm Locations
                { "Farm", "Farm" },
                { "FarmHouse", "Farmhouse" },
                { "Greenhouse", "Greenhouse" },
                { "Cellar", "Cellar" }, // Often referred to as Farmhouse Cellar
                { "FarmCave", "Farm Cave" },

                // Beach Locations
                { "Beach", "The Beach" }, // Wiki name
                { "ElliottHouse", "Elliott's Cabin" }, // Wiki name
                { "FishShop", "Willy's Fish Shop" }, // Wiki name

                // Desert Locations
                { "Desert", "Calico Desert" }, // Wiki name
                { "Club", "Casino" }, // Wiki name (located in Oasis)
                { "SkullCave", "Skull Cavern" }, // Wiki name

                // Forest Locations
                { "Forest", "Cindersap Forest" }, // Wiki name
                { "Woods", "Secret Woods" }, // Wiki name
                { "WizardHouse", "Wizard's Tower" }, // Wiki name
                { "LeahHouse", "Leah's Cottage" }, // Wiki name
                { "AnimalShop", "Marnie's Ranch" }, // Wiki name
                { "HatMouseHouse", "Abandoned House" }, // Wiki name (Hat Mouse)
                { "BugLand", "Mutant Bug Lair" }, // Wiki name

                // Mountain Locations
                { "Mountain", "The Mountain" }, // Wiki name
                { "Mine", "The Mines" }, // Wiki name (Entrance)
                { "AdventureGuild", "Adventurer's Guild" }, // Wiki name
                { "BathHouse_Entry", "Spa Entrance" }, // More descriptive
                { "BathHouse_MensLocker", "Spa Men's Locker" },
                { "BathHouse_WomensLocker", "Spa Women's Locker" },
                { "BathHouse_Pool", "Spa Pool" },
                { "Railroad", "Railroad" },
                { "Tent", "Linus' Tent" }, // Wiki name
                { "ScienceHouse", "Robin's Carpenter Shop" }, // Wiki name (Robin/Demetrius/Maru/Sebastian's House)
                { "SebastianRoom", "Sebastian's Room" },

                // Island Locations
                { "IslandSouth", "Ginger Island South" },
                { "IslandWest", "Ginger Island West" },
                { "IslandNorth", "Ginger Island North" },
                { "IslandEast", "Ginger Island East" },
                { "IslandFarmHouse", "Island Farmhouse" },
                { "Caldera", "Volcano Caldera" },
                { "Island_Hut", "Leo's Hut" }, // Wiki name
                { "Island_Shrine", "Island Shrine" }, // Generic, could be improved if specific shrine known
                { "Island_Resort", "Island Resort" },
                { "Island_FieldOffice", "Island Field Office" }, // Wiki name
                { "VolcanoDungeon0", "Volcano Entrance" }, // Level 0

                // Modded Locations (Example Handling)
                { "Custom_AdventureGuild", "SVE Guild" },
                { "Custom_Backwoods", "SVE Backwoods" },
                { "Custom_ForestWest", "SVE West Forest" }
                // Add more specific SVE or other mod mappings here if needed
            };

            // Try to find a specific mapping first
            if (nameMap.TryGetValue(locationName, out string displayName))
            {
                return displayName;
            }

            // Handle common prefixes for modded locations
            if (locationName.StartsWith("Custom_", StringComparison.OrdinalIgnoreCase))
            {
                // Clean up "Custom_" prefix and replace underscores
                return locationName.Substring("Custom_".Length).Replace("_", " ");
            }
            if (locationName.StartsWith("SVE_", StringComparison.OrdinalIgnoreCase))
            {
                return locationName.Substring("SVE_".Length).Replace("_", " ");
            }

            // Default case: split PascalCase/camelCase and replace underscores
            string defaultName = string.Join(" ", System.Text.RegularExpressions.Regex.Split(locationName, @"(?<!^)(?=[A-Z]|\d)"))
                .Replace("_", " ");
            // Capitalize first letter of each word for better readability
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(defaultName.ToLower());
        }

        /// <summary>Get all available location categories</summary>
        public IEnumerable<string> GetCategories()
        {
            // Only return categories that have locations
            return locationTabs.Keys.Where(k => locationTabs[k].Count > 0);
        }

        /// <summary>
        /// Get all locations in a specific category
        /// </summary>
        public List<string> GetLocationsInCategory(string category)
        {
            if (locationTabs.TryGetValue(category, out var locations))
            {
                return locations;
            }
            return new List<string>();
        }

        /// <summary>
        /// Load a specific location by name
        /// </summary>
        public GameLocation LoadLocation(string locationName)
        {
            if (string.IsNullOrEmpty(locationName))
                return null;
                
            return Game1.getLocationFromName(locationName);
        }

        /// <summary>
        /// Warps the player to the specified location, finding a valid tile if necessary.
        /// </summary>
        public bool WarpToLocation(string locationName, int? tileX = null, int? tileY = null)
        {
            // Validate location name
            if (string.IsNullOrEmpty(locationName))
            {
                Monitor.Log("Cannot warp to a location with no name", LogLevel.Warn);
                return false;
            }
            
            // Try to get the location
            GameLocation targetLocation = LoadLocation(locationName);
            if (targetLocation == null)
            {
                Monitor.Log($"Failed to warp: Location '{locationName}' not found", LogLevel.Error);
                return false;
            }
            
            try
            {
                Point targetTile;

                // If specific coordinates are provided, validate them
                if (tileX.HasValue && tileY.HasValue)
                {
                    if (IsTileValid(targetLocation, tileX.Value, tileY.Value))
                    {
                        targetTile = new Point(tileX.Value, tileY.Value);
                    }
                    else
                    {
                        // If the target tile is invalid, find a nearby valid one
                        Point? nearbyValidTile = FindNearbyValidTile(targetLocation, tileX.Value, tileY.Value);
                        if (nearbyValidTile.HasValue)
                        {
                            targetTile = nearbyValidTile.Value;
                            Monitor.Log($"Original warp tile ({tileX.Value}, {tileY.Value}) invalid. Warping to nearby valid tile ({targetTile.X}, {targetTile.Y}) instead.", LogLevel.Debug);
                        }
                        else
                        {
                            // If no nearby valid tile, use the default
                            targetTile = GetDefaultPositionForLocation(locationName); 
                            Monitor.Log($"Original warp tile ({tileX.Value}, {tileY.Value}) and nearby tiles invalid. Warping to default position ({targetTile.X}, {targetTile.Y}).", LogLevel.Warn);
                        }
                    }
                }
                else
                {
                    // If no coordinates provided, use the default warp point for the location
                    targetTile = GetDefaultPositionForLocation(locationName); 
                }
                
                // Perform the warp
                Game1.warpFarmer(locationName, targetTile.X, targetTile.Y, Game1.player.FacingDirection, false); // Set isStructure to false for outdoor locations
                return true;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error warping to {locationName}: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>Get a default position for a location (typically entrances or center).</summary>
        public Point GetDefaultPositionForLocation(string locationName)
        {
            // Check cache first to avoid recalculating 
            if (ValidWarpPositionCache.TryGetValue(locationName, out Point cachedPosition))
            {
                return cachedPosition;
            }
            
            GameLocation location = LoadLocation(locationName);
            Point position;

            if (location != null)
            {
                // Prioritize warp points within the location
                if (location.warps.Any())
                {
                    // Try to find a valid warp target point
                    foreach (var warp in location.warps.OrderBy(w => w.TargetX)) // Prioritize lower X warps (often main entrances)
                    {
                        if (IsTileValid(location, warp.TargetX, warp.TargetY))
                        {
                            position = new Point(warp.TargetX, warp.TargetY);
                            ValidWarpPositionCache[locationName] = position;
                            return position;
                        }
                    }
                }

                // If no valid warp found, try finding a valid position near the center
                position = FindValidPositionForLocation(locationName);
            }
            else
            {
                // Fallback if location couldn't be loaded (should not happen often)
                position = new Point(20, 20); 
                Monitor.Log($"Could not load location {locationName} to find default warp point. Using fallback (20, 20).", LogLevel.Warn);
            }
            
            ValidWarpPositionCache[locationName] = position;
            return position;
        }

        /// <summary>For unknown/modded locations, try to find a valid position near the center.</summary>
        private Point FindValidPositionForLocation(string locationName)
        {
            GameLocation location = LoadLocation(locationName);
            if (location == null || location.map == null || location.map.Layers.Count == 0)
            {
                return new Point(20, 20);  // Fallback default
            }
            
            int midX = location.map.Layers[0].LayerWidth / 2;
            int midY = location.map.Layers[0].LayerHeight / 2;
            
            // Try center first
            if (IsTileValid(location, midX, midY))
            {
                return new Point(midX, midY);
            }
            // Search outward in a spiral from the center
            Point? nearby = FindNearbyValidTile(location, midX, midY, 25); // Increased search radius
            if (nearby.HasValue)
            {
                return nearby.Value;
            }

            // Last resort - check common entry points if spiral search fails
            int[] standardCoords = { 10, 15, 5, 20, 25 };
            foreach (int y in standardCoords)
            {
                foreach (int x in standardCoords)
                {
                    if (IsTileValid(location, x, y))
                    {
                        return new Point(x, y);
                    }
                }
            }
            
            // Ultimate fallback - this might not be valid but we've tried everything else
            Monitor.Log($"Could not find any valid warp tile for {locationName}. Using fallback (20, 20).", LogLevel.Warn);
            return new Point(20, 20);
        }
        
        /// <summary>Checks if a tile is valid to warp to.</summary>
        public bool IsTileValid(GameLocation location, int tileX, int tileY)
        {
            try
            {
                // Check if tile is within bounds
                if (location == null || location.map == null || location.map.Layers.Count == 0)
                    return false;

                if (tileX < 0 || tileY < 0 || tileX >= location.map.Layers[0].LayerWidth || tileY >= location.map.Layers[0].LayerHeight)
                    return false;

                // Check basic passability (using the tile location)
                if (!location.isTilePassable(new xTile.Dimensions.Location(tileX, tileY), Game1.viewport))
                    return false;

                // Check for specific obstructions
                Vector2 tileVector = new Vector2(tileX, tileY);

                // Check for objects
                if (location.objects.ContainsKey(tileVector))
                {
                    // Optional: Check if the object is actually impassable
                    if (location.objects[tileVector] != null && !location.objects[tileVector].isPassable())
                        return false;
                }

                // Check for farmer
                if (location.isTileOccupiedByFarmer(tileVector) != null)
                    return false;

                // Check for terrain features (like trees, grass, paths)
                if (location.terrainFeatures.ContainsKey(tileVector))
                {
                    // Optional: Check if the terrain feature is impassable
                    if (location.terrainFeatures[tileVector] != null && !location.terrainFeatures[tileVector].isPassable())
                        return false;
                }

                // Check for large terrain features (like bushes)
                // Note: This check might be less precise as it uses bounding boxes
                foreach (var feature in location.largeTerrainFeatures)
                {
                    if (feature.getBoundingBox().Contains(tileX, tileY))
                        return false;
                }

                // Check for water
                if (location.isWaterTile(tileX, tileY))
                    return false;

                if (location.doesTileHaveProperty(tileX, tileY, "Water", "Back") != null)
                    return false;

                // Check for NoSpawn property
                if (location.doesTileHaveProperty(tileX, tileY, "NoSpawn", "Back") != null)
                    return false;

                // Check for explicitly impassable tiles on specific layers
                string buildingProperty = location.doesTileHaveProperty(tileX, tileY, "Passable", "Buildings");
                if (buildingProperty != null && buildingProperty.Equals("F", StringComparison.OrdinalIgnoreCase))
                    return false;

                string backProperty = location.doesTileHaveProperty(tileX, tileY, "Passable", "Back");
                if (backProperty != null && backProperty.Equals("F", StringComparison.OrdinalIgnoreCase))
                    return false;

                return true; // If all checks pass
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error checking tile validity for ({tileX}, {tileY}) in {location?.Name ?? "Unknown"}: {ex.Message}", LogLevel.Trace);
                return false;
            }
        }
        
        /// <summary>Finds a nearby valid tile if the target is invalid.</summary>
        public Point? FindNearbyValidTile(GameLocation location, int centerX, int centerY, int maxRadius = 10)
        {
            if (location == null) return null;
            // Check the starting point itself first
            if (IsTileValid(location, centerX, centerY)) return new Point(centerX, centerY);

            // Try tiles in expanding radius around the center point
            for (int radius = 1; radius <= maxRadius; radius++)
            {
                // Check tiles in a spiral pattern
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        // Only check the outer layer of the current radius
                        if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                            continue;
                        
                        int x = centerX + dx;
                        int y = centerY + dy;
                        
                        if (IsTileValid(location, x, y))
                            return new Point(x, y);
                    }
                }
            }
            
            return null; // No valid tile found within the radius
        }
    }
}