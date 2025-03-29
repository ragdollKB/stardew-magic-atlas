using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WarpMod.Utility
{
    /// <summary>
    /// Manages location data, categorization, and filtering for the warp menu
    /// </summary>
    public class LocationManager
    {
        private readonly IMonitor Monitor;
        private readonly Dictionary<string, List<string>> locationTabs = new();
        private readonly HashSet<string> blacklistedLocations = new();
        
        // Add a new mod locations category
        private const string MOD_LOCATIONS_CATEGORY = "Mod Locations";
        
        // Flag to control whether to show SVE locations
        private readonly bool showSVELocations;

        public LocationManager(IMonitor monitor, bool showSVELocations = true)
        {
            this.Monitor = monitor;
            this.showSVELocations = showSVELocations;
            InitializeCategories();
            InitializeBlacklist(); // Add blacklist initialization
            LoadLocations();
        }

        /// <summary>
        /// Initialize empty category lists
        /// </summary>
        private void InitializeCategories()
        {
            // Initialize categories based on wiki structure
            locationTabs["Town"] = new List<string>();
            locationTabs["Farm"] = new List<string>();
            locationTabs["Beach"] = new List<string>();
            locationTabs["Mountain"] = new List<string>();
            locationTabs["Forest"] = new List<string>();
            locationTabs["Desert"] = new List<string>();
            locationTabs["Island"] = new List<string>();
            locationTabs["Indoor"] = new List<string>(); // For miscellaneous indoor locations
            locationTabs[MOD_LOCATIONS_CATEGORY] = new List<string>(); // For modded locations
        }
        
        /// <summary>
        /// Initialize blacklist of locations that shouldn't be included
        /// </summary>
        private void InitializeBlacklist()
        {
            // Add locations that should be excluded (temporary maps, unaccessible areas, etc.)
            string[] excludedMaps = new[]
            {
                "Backwoods", // Not a valid warp destination
                "Temp", // Temporary maps
                "Cutscene", // Cutscene maps
                "Credits", // Credits scene
                "Intro", // Intro scene
                "Mine", // Mines are handled specially (except Mine entrance)
                "UndergroundMine", // Mining levels (except level 120)
                "VolcanoDungeon", // Volcano levels (except entrance)
                "Submarine", // Special event location
                "Trailer_Big_Temp", // Temporary trailer map
                "BoatTunnel", // Special boat tunnel area
                "FishShopCutscene", // Cutscene location
                "Sewer_Temp", // Temporary sewer map
                "Temp_", // Any temp location
                "_Ambient", // Ambient maps
                "ElevatorShaft", // Elevator shaft
                "fishingGame", // Fishing mini-game
                "festivalSpot", // Festival spot
                "Pool", // Generic pool map
                "MermaidHouse", // Mermaid house (special access)
                "AbandonedBuilding", // Abandoned building (not meant for warping)
                "QiCave", // Qi's cave (special access)
                "CaptainRoom", // Captain's room
                "RailroadTemporary", // Temporary railroad map
                "event", // Event maps
                "_Savage", // Savage maps
                "Tutorial", // Tutorial maps
                "BeachNightMarket", // Beach Night Market - requested to remove
                "Cellar2", // Cellar 2 - requested to remove
                "Cellar3", // Cellar 3 - requested to remove 
                "Cellar4", // Cellar 4 - requested to remove
                "FarmHouse", // Farmhouse - requested to remove
                "QiNutRoom", // Qi Nut Room - requested to remove
                "Summit" // Summit - requested to remove
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
            // Track how many vanilla and modded locations we load
            int vanillaLocationsCount = 0;
            int moddedLocationsCount = 0;

            // Load locations directly from the game's location list
            foreach (var location in Game1.locations)
            {
                // Skip null locations and those without a name or map
                if (location == null || string.IsNullOrEmpty(location.Name) || location.map == null)
                    continue;
                    
                // Skip blacklisted locations
                if (IsBlacklisted(location.Name))
                    continue;
                    
                // Skip inaccessible locations
                if (!IsLocationAccessible(location))
                    continue;
                    
                // Categorize the location
                string category = DetermineMapCategory(location.Name);
                
                // Track if this is a modded location
                bool isModded = IsModdedLocation(location.Name);
                if (isModded)
                {
                    moddedLocationsCount++;
                    // For modded locations, add to the mod locations category
                    if (!locationTabs[MOD_LOCATIONS_CATEGORY].Contains(location.Name))
                    {
                        locationTabs[MOD_LOCATIONS_CATEGORY].Add(location.Name);
                    }
                }
                else
                {
                    vanillaLocationsCount++;
                }
                
                // Also add to the standard category if it's not already present
                if (!locationTabs[category].Contains(location.Name) && !isModded)
                {
                    locationTabs[category].Add(location.Name);
                }
            }

            // Add special handling for SVE locations if installed
            AddSVELocationsIfAvailable();

            // Sort locations within each category
            foreach (var category in locationTabs.Keys.ToList())
            {
                locationTabs[category].Sort();
            }

            // Log the count of locations in each category
            foreach (var category in locationTabs.Keys)
            {
                if (locationTabs[category].Count > 0)
                {
                    Monitor.Log($"Loaded {locationTabs[category].Count} locations in category: {category}", LogLevel.Debug);
                }
            }
            
            Monitor.Log($"Loaded a total of {vanillaLocationsCount} vanilla locations and {moddedLocationsCount} modded locations", LogLevel.Info);
        }
        
        /// <summary>
        /// Add Stardew Valley Expanded locations if the mod is installed
        /// </summary>
        private void AddSVELocationsIfAvailable()
        {
            // Skip if SVE locations are disabled in config
            if (!showSVELocations)
            {
                Monitor.Log("SVE location support is disabled in config", LogLevel.Debug);
                return;
            }
            
            // Check if SVE locations exist by looking for common SVE locations
            bool sveInstalled = Game1.getLocationFromName("Custom_Backwoods") != null || 
                               Game1.getLocationFromName("Custom_GramplesHouse") != null ||
                               Game1.getLocationFromName("Custom_ForestWest") != null;
            
            if (!sveInstalled)
                return;
                
            Monitor.Log("Stardew Valley Expanded detected, adding special handling for SVE locations", LogLevel.Info);
            
            // Add some key SVE locations that might not be in the loaded locations list
            string[] sveLocations = new[] {
                "Custom_Backwoods", 
                "Custom_ForestWest",
                "Custom_GramplesHouse",
                "Custom_AdventureGuild",
                "Custom_FairhavenFarm",
                "Custom_SophiaFarm",
                "Custom_BlueMoonVineyard",
                "Custom_MorrisJoja",
                "Custom_JenkinsHouse",
                "Custom_SusanHouse",
                "Custom_AndyHouse",
                "Custom_MartinHouse",
                "Custom_VictorHouse",
                "Custom_OliviaBedroom",
                "Custom_ClaireRoom",
                "Custom_MorrisApartment",
                "Custom_AuroraVineyard",
                "Custom_IridiumQuarry",
                "Custom_AdventurerCabin",
                "Custom_TreasureCave"
            };
            
            foreach (var locationName in sveLocations)
            {
                var location = Game1.getLocationFromName(locationName);
                if (location != null && location.map != null && !IsBlacklisted(locationName))
                {
                    // Add to mod locations category if not already present
                    if (!locationTabs[MOD_LOCATIONS_CATEGORY].Contains(locationName))
                    {
                        locationTabs[MOD_LOCATIONS_CATEGORY].Add(locationName);
                        Monitor.Log($"Added SVE location: {locationName}", LogLevel.Debug);
                    }
                }
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
            // Check for common mod location prefixes/patterns
            string[] modLocationPatterns = {
                "Custom_", "SVE_", "DIGUS.", "JA_", "CP_", 
                "Summit", "GingerIsland", "Ridgeside", "East Scarp", 
                "Zuzu", "BackwoodsMod", "BusStopExtension"
            };
            
            foreach (var pattern in modLocationPatterns)
            {
                if (locationName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            
            // Check if this is a vanilla location (this approach is more future-proof)
            string[] vanillaLocationPrefixes = {
                "Beach", "BusStop", "Desert", "Farm", "Forest", "Mountain", "Town", 
                "Mine", "Island", "Sewer", "WizardHouse", "Woods"
            };
            
            // If it matches a vanilla pattern, it's probably not modded
            foreach (var pattern in vanillaLocationPrefixes) 
            {
                if (locationName.Equals(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            
            // If the name is very long or complex, it's likely a modded location
            if (locationName.Length > 20 || locationName.Count(c => c == '_') > 1)
            {
                return true;
            }
            
            // Default to false (assume vanilla)
            return false;
        }
        
        /// <summary>
        /// Check if a location is accessible for warping
        /// </summary>
        private bool IsLocationAccessible(GameLocation location)
        {
            // Skip null locations and those without maps
            if (location == null || location.map == null)
                return false;
                
            // Skip temporary or special locations
            if (location.Name.Contains("Temp") || 
                location.Name.Contains("Trailer_") || // Skip trailer variations
                (location.Name.StartsWith("UndergroundMine") && !location.Name.EndsWith("120")) || // Only allow mine level 120
                location.Name.StartsWith("VolcanoDungeon"))
            {
                return false;
            }
            
            // Skip maps that require special conditions or are not meant to be warped to
            if (location.IsOutdoors && location.map.Layers[0].LayerWidth < 5)
                return false;
                
            return true;
        }

        /// <summary>
        /// Determine which category a map belongs to
        /// </summary>
        private string DetermineMapCategory(string mapName)
        {
            // Town locations - only include maps that exist in assets/maps
            if (mapName == "Town" || mapName == "SeedShop" || mapName == "Saloon" ||
                mapName == "ManorHouse" || mapName == "Blacksmith" || mapName == "Hospital" ||
                mapName == "JojaMart" || mapName == "AbandonedJojaMart" || mapName == "ArchaeologyHouse" ||
                mapName == "HarveyRoom" || mapName == "MovieTheater")
                return "Town";

            // Farm locations - only include maps that exist in assets/maps
            if (mapName == "Farm" || mapName == "FarmHouse" ||
                mapName == "Greenhouse" || mapName == "Cellar" || mapName == "FarmCave")
                return "Farm";

            // Beach locations - only include maps that exist in assets/maps
            if (mapName == "Beach" || mapName == "ElliottHouse" || mapName == "FishShop")
                return "Beach";

            // Desert locations - only include maps that exist in assets/maps
            if (mapName == "Desert")
                return "Desert";

            // Forest locations - only include maps that exist in assets/maps
            if (mapName == "Forest" || mapName == "Woods" || mapName == "BugLand" ||
                mapName == "WizardHouse" || mapName == "LeahHouse" || mapName == "BusStop" || 
                mapName == "AnimalShop")
                return "Forest";

            // Mountain locations - only include maps that exist in assets/maps
            if (mapName == "Mountain" || mapName == "Mine" || mapName == "AdventureGuild" ||
                mapName == "BathHouse_Entry" || mapName == "BathHouse_MensLocker" || 
                mapName == "BathHouse_WomensLocker" || mapName == "BathHouse_Pool")
                return "Mountain";

            // Island locations - only include maps that exist in assets/maps
            if (mapName == "IslandSouth" || mapName == "IslandWest" || mapName == "IslandNorth" ||
                mapName == "IslandEast" || mapName == "Island_Hut" || mapName == "Island_FarmCave" ||
                mapName == "Island_CaptainRoom" || mapName == "Island_Resort" || mapName == "Island_Secret" ||
                mapName == "Island_Shrine" || mapName == "IslandFarmHouse" || mapName == "Caldera")
                return "Island";

            // Indoor locations (catch-all for rooms and houses that don't fit elsewhere)
            if ((mapName.Contains("Room") || mapName.Contains("House") || mapName.Contains("Cave") ||
                mapName.Contains("Shop")) &&
                !mapName.Contains("Island") && !mapName.Contains("Beach") && 
                !mapName.Contains("Desert") && !mapName.Contains("Mountain"))
                return "Indoor";

            // Check if this appears to be a modded location
            if (IsModdedLocation(mapName))
                return MOD_LOCATIONS_CATEGORY;

            // Default to Town if no match found
            return "Town";
        }

        /// <summary>
        /// Get a display-friendly name for a location
        /// </summary>
        public string GetDisplayName(string locationName)
        {
            // Special cases for location names based on wiki categories
            switch (locationName)
            {
                // Town locations
                case "Town":
                    return "Pelican Town";
                case "SeedShop":
                    return "Pierre's Shop";
                case "Saloon":
                    return "Stardrop Saloon";
                case "ManorHouse":
                    return "Mayor's House";
                case "Blacksmith":
                    return "Blacksmith";
                case "Hospital":
                    return "Harvey's Clinic";
                case "JojaMart":
                    return "JojaMart";
                case "AbandonedJojaMart":
                    return "Old JojaMart";
                case "MovieTheater":
                    return "Movie Theater";
                case "ArchaeologyHouse":
                    return "Museum";
                case "HarveyRoom":
                    return "Harvey's Room";
                    
                // Farm locations
                case "Farm":
                    return "Farm";
                case "FarmHouse":
                    return "Farmhouse";
                case "Greenhouse":
                    return "Greenhouse";
                case "Cellar":
                    return "Farmhouse Cellar";
                case "FarmCave":
                    return "Farm Cave";
                    
                // Beach locations
                case "Beach":
                    return "Beach";
                case "ElliottHouse":
                    return "Elliott's Cabin";
                case "FishShop":
                    return "Fish Shop";
                    
                // Desert locations
                case "Desert":
                    return "Calico Desert";
                    
                // Forest locations
                case "Forest":
                    return "Cindersap Forest";
                case "Woods":
                    return "Secret Woods";
                case "BugLand":
                    return "Mutant Bug Lair";
                case "WizardHouse":
                    return "Wizard's Tower";
                case "LeahHouse":
                    return "Leah's Cottage";
                case "BusStop":
                    return "Bus Stop";
                case "AnimalShop":
                    return "Marnie's Ranch";
                    
                // Mountain locations
                case "Mountain":
                    return "Mountain";
                case "AdventureGuild":
                    return "Adventurer's Guild";
                case "Mine":
                    return "The Mines";
                case "BathHouse_Entry":
                    return "Bathhouse Entrance";
                case "BathHouse_MensLocker":
                    return "Men's Locker Room";
                case "BathHouse_WomensLocker":
                    return "Women's Locker Room";
                case "BathHouse_Pool":
                    return "Bath Pool";
                    
                // Island locations
                case "IslandSouth":
                    return "Ginger Island South";
                case "IslandWest":
                    return "Ginger Island West";
                case "IslandNorth":
                    return "Ginger Island North";
                case "IslandEast":
                    return "Ginger Island East";
                case "IslandFarmHouse":
                    return "Island Farmhouse";
                case "Caldera":
                    return "Volcano Caldera";
                case "Island_Hut":
                    return "Island Hut";
                case "Island_Shrine":
                    return "Island Shrine";
                case "Island_Secret":
                    return "Secret Island Area";
                case "Island_Resort":
                    return "Island Resort";
                case "Island_FarmCave":
                    return "Island Farm Cave";
                case "Island_CaptainRoom":
                    return "Captain's Room";
                    
                // SVE/mod locations
                case "Custom_AdventureGuild":
                    return "SVE Guild";
                case "Custom_Backwoods":
                    return "SVE Backwoods";
                case var s when s.StartsWith("Custom_"):
                    return s.Replace("Custom_", "").Replace("_", " ");
                case var s when s.StartsWith("SVE_"):
                    return s.Replace("SVE_", "").Replace("_", " ");
            }
            
            // Handle modded locations by cleaning up their names
            if (IsModdedLocation(locationName))
            {
                // Remove common modded location prefixes
                string cleanName = locationName
                    .Replace("Custom_", "")
                    .Replace("SVE_", "")
                    .Replace("DIGUS.", "")
                    .Replace("JA_", "")
                    .Replace("CP_", "");
                
                // Replace underscores with spaces
                cleanName = cleanName.Replace("_", " ");
                
                // If there's a period, take only what's after it (often used in modded locations)
                if (cleanName.Contains("."))
                {
                    cleanName = cleanName.Substring(cleanName.LastIndexOf('.') + 1);
                }
                
                return cleanName;
            }
            
            // Default split on capital letters and clean up
            return string.Join(" ", System.Text.RegularExpressions.Regex.Split(locationName, @"(?<!^)(?=[A-Z]|\d)"))
                .Replace("_", " ");
        }

        /// <summary>
        /// Get all available location categories
        /// </summary>
        public IEnumerable<string> GetCategories()
        {
            // Only return categories that have locations in them
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
        /// <param name="locationName">The name of the location to load</param>
        /// <returns>The GameLocation object if found, null otherwise</returns>
        public GameLocation LoadLocation(string locationName)
        {
            if (string.IsNullOrEmpty(locationName))
                return null;
                
            // Try to get the location from the game
            GameLocation location = Game1.getLocationFromName(locationName);
            
            // Log if location couldn't be found
            if (location == null)
            {
                Monitor.Log($"Could not find location: {locationName}", LogLevel.Debug);
            }
            
            return location;
        }

        /// <summary>
        /// Update the locations in a category with a new list of locations
        /// </summary>
        public void UpdateCategoryLocations(string category, List<string> locations)
        {
            if (locationTabs.ContainsKey(category))
            {
                locationTabs[category] = new List<string>(locations);
                Monitor.Log($"Updated {locations.Count} locations in category: {category}", LogLevel.Debug);
            }
            else
            {
                Monitor.Log($"Cannot update unknown category: {category}", LogLevel.Warn);
            }
        }

        /// <summary>
        /// Warps the player to the specified location
        /// </summary>
        /// <param name="locationName">The name of the location to warp to</param>
        /// <param name="tileX">Optional X tile coordinate within the location</param>
        /// <param name="tileY">Optional Y tile coordinate within the location</param>
        /// <returns>True if warp was successful, false otherwise</returns>
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
                // If no specific coordinates are provided, use default warp point
                if (!tileX.HasValue || !tileY.HasValue)
                {
                    // Use default warp point or center of map
                    tileX = targetLocation.warps.Count > 0 
                        ? targetLocation.warps[0].X  // Use first warp point
                        : targetLocation.map.Layers[0].LayerWidth / 2;  // Use center of map
                        
                    tileY = targetLocation.warps.Count > 0 
                        ? targetLocation.warps[0].Y 
                        : targetLocation.map.Layers[0].LayerHeight / 2;
                }
                
                // Ensure the tile is valid (not outside the map or in a wall)
                tileX = Math.Max(0, Math.Min(tileX.Value, targetLocation.map.Layers[0].LayerWidth - 1));
                tileY = Math.Max(0, Math.Min(tileY.Value, targetLocation.map.Layers[0].LayerHeight - 1));
                
                // Perform the warp - let the game handle its own sound effects
                Game1.warpFarmer(locationName, tileX.Value, tileY.Value, Game1.player.FacingDirection, true);
                
                // Log success
                Monitor.Log($"Warped to {locationName} at tile ({tileX}, {tileY})", LogLevel.Debug);
                return true;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error warping to {locationName}: {ex.Message}", LogLevel.Error);
                return false;
            }
        }
    }
}