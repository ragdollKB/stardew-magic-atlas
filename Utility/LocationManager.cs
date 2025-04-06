using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions; // Add for Regex

namespace WarpMod.Utility
{
    /// <summary>
    /// Manages location data and categorization for the warp menu
    /// </summary>
    public class LocationManager
    {
        private readonly IMonitor Monitor;
        private readonly ModConfig Config; // Add config field
        private readonly Dictionary<string, List<string>> locationTabs = new();
        private readonly HashSet<string> blacklistedLocations = new();
        private readonly Dictionary<string, Point> ValidWarpPositionCache = new Dictionary<string, Point>();
        
        // The mod locations category name - Make public so GridWarpMenu can access it for sorting
        public const string MOD_LOCATIONS_CATEGORY = "Mod Locations";
        private const int MaxLocationsPerTab = 9; // Define the fixed limit

        public LocationManager(IMonitor monitor, ModConfig config)
        {
            this.Monitor = monitor;
            this.Config = config; // Store config
            InitializeCategories();
            InitializeBlacklist();
            LoadLocations();
        }

        /// <summary>
        /// Initialize empty base category lists
        /// </summary>
        private void InitializeCategories()
        {
            // Only initialize base categories here, numbered tabs are created dynamically
            locationTabs["Farm"] = new List<string>();
            locationTabs["Town"] = new List<string>();
            locationTabs["Beach"] = new List<string>();
            locationTabs["Mountain"] = new List<string>();
            locationTabs["Forest"] = new List<string>();
            locationTabs["Desert"] = new List<string>();
            locationTabs["Island"] = new List<string>();
            
            // Mod category is handled dynamically if needed/enabled
        }
        
        /// <summary>
        /// Initialize blacklist of locations that shouldn't be included
        /// </summary>
        private void InitializeBlacklist()
        {
            // Add locations that should be excluded
            string[] excludedMaps = new[]
            {
                "Backwoods", "Temp", "Cutscene", "Credits", "Intro", "Mine", // Keep existing
                "UndergroundMine", "VolcanoDungeon", "Submarine", "BoatTunnel", 
                "Temp_", "_Ambient", "fishingGame", "festivalSpot",
                "DesertFestival" // Add DesertFestival
                // Add other temporary or unusable locations if needed
            };
            
            foreach (var map in excludedMaps)
            {
                blacklistedLocations.Add(map);
            }
        }

        /// <summary>
        /// Load all available locations from the game and mods, splitting categories if they exceed the limit.
        /// </summary>
        private void LoadLocations()
        {
            locationTabs.Clear(); // Clear existing tabs before loading
            InitializeCategories(); // Initialize base categories

            // Get all valid locations first, applying filters and initial sorting
            var allLocations = Game1.locations
                .Where(loc => loc != null && !string.IsNullOrEmpty(loc.Name) && loc.map != null)
                .Where(loc => !IsBlacklisted(loc.Name))
                .Where(loc => !Config.HideLockedLocations || IsLocationAccessible(loc.Name)) // Use full accessibility check
                // Apply the new filter based on config
                .Where(loc => Config.ShowIndoorAndSpecialLocations || IsMajorOutdoorLocation(loc.Name))
                .OrderBy(loc => GetDisplayName(loc.Name)) // Sort alphabetically by display name
                .ToList();

            // Temporary dictionary to hold locations before splitting
            var tempCategorizedLocations = new Dictionary<string, List<string>>();

            // Initial categorization pass
            foreach (var location in allLocations)
            {
                string baseCategory = DetermineMapCategory(location.Name);
                bool isModded = IsModdedLocation(location.Name);

                // Determine the final base category (considering mod grouping)
                if (isModded && Config.GroupModdedLocations && Config.ShowSVELocations)
                {
                    baseCategory = MOD_LOCATIONS_CATEGORY;
                }
                else if (isModded && !Config.ShowSVELocations)
                {
                    continue; // Skip modded locations if SVE locations are disabled
                }
                // If modded but not grouping, it stays in its determined category

                // Add to temporary category list
                if (!tempCategorizedLocations.ContainsKey(baseCategory))
                {
                    tempCategorizedLocations[baseCategory] = new List<string>();
                }
                if (!tempCategorizedLocations[baseCategory].Contains(location.Name))
                {
                     tempCategorizedLocations[baseCategory].Add(location.Name);
                }
            }

            // Now, distribute locations into final tabs, splitting as needed
            locationTabs.Clear(); // Clear again to populate with final, potentially split tabs
            foreach (var kvp in tempCategorizedLocations.OrderBy(kv => kv.Key)) // Process categories alphabetically
            {
                string baseCategory = kvp.Key;
                List<string> locationsInCategory = kvp.Value;
                int tabIndex = 1;
                int locationIndex = 0;

                while (locationIndex < locationsInCategory.Count)
                {
                    string currentTabName = (tabIndex == 1) ? baseCategory : $"{baseCategory} {tabIndex}";
                    
                    // Ensure the tab exists in the final dictionary
                    if (!locationTabs.ContainsKey(currentTabName))
                    {
                        locationTabs[currentTabName] = new List<string>();
                    }

                    // Add locations to the current tab up to the limit
                    int addedCount = 0;
                    while (addedCount < MaxLocationsPerTab && locationIndex < locationsInCategory.Count)
                    {
                        locationTabs[currentTabName].Add(locationsInCategory[locationIndex]);
                        locationIndex++;
                        addedCount++;
                    }
                    
                    tabIndex++; // Move to the next potential tab number
                }
            }

             // Apply specific sorting overrides (like Farm first) AFTER splitting
             ApplySortingOverrides();

            // Remove empty categories that might have been initialized but not used (shouldn't happen with new logic, but safe)
            foreach (var category in locationTabs.Keys.ToList())
            {
                if (locationTabs[category].Count == 0)
                {
                    locationTabs.Remove(category);
                }
            }
        }

        /// <summary>Checks if a location is one of the major outdoor areas.</summary>
        private bool IsMajorOutdoorLocation(string locationName)
        {
            // Define the core outdoor locations
            string[] majorOutdoor = { 
                "Farm", "Town", "Beach", "Mountain", "Forest", "Desert", "BusStop", "Railroad"
                // Add Island locations if desired as core outdoor areas
                // "IslandSouth", "IslandWest", "IslandNorth", "IslandEast"
            };
            
            // Check if the location name matches or starts with one of the major areas
            // (StartsWith helps catch variations like FarmHouse, FarmCave if needed, though categorization might handle this)
            // For simplicity, let's do an exact match for the main areas.
            return majorOutdoor.Contains(locationName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Applies specific sorting overrides like putting "Farm" first.</summary>
         private void ApplySortingOverrides()
         {
             // Example: Ensure "Farm" is first in the "Farm" category (the first tab)
             if (locationTabs.TryGetValue("Farm", out var farmList) && farmList.Contains("Farm"))
             {
                 farmList.Remove("Farm");
                 farmList.Insert(0, "Farm");
             }
              // Example: Ensure "Forest" is first in the "Forest" category (the first tab)
             if (locationTabs.TryGetValue("Forest", out var forestList) && forestList.Contains("Forest"))
             {
                 forestList.Remove("Forest");
                 forestList.Insert(0, "Forest");
             }
             // Add other overrides if needed (e.g., FarmHouse second in Farm)
         }

        /// <summary>Checks if the player has unlocked access to a given location.</summary>
        private bool IsLocationUnlocked(string locationName)
        {
            // Always allow Farm and Town basics if not hidden
            if (locationName == "Farm" || locationName == "FarmHouse" || locationName == "Town")
                return true;

            // Check specific unlock conditions
            switch (locationName)
            {
                case "Desert":
                    return Game1.isLocationAccessible("Desert"); // Bus repair
                case "SkullCave": // Requires Skull Key (in addition to Desert access)
                    return Game1.isLocationAccessible("Desert") && Game1.player.hasSkullKey;
                case "Club": // Casino - Requires Club Card (in addition to Desert access)
                     return Game1.isLocationAccessible("Desert") && Game1.player.hasClubCard;
                case "IslandSouth":
                    return Game1.isLocationAccessible("IslandSouth"); // Boat repair
                case "IslandWest":
                    return Game1.isLocationAccessible("IslandWest"); // Boat repair
                case "IslandNorth":
                case "IslandEast":
                case "IslandFarmhouse":
                case "Caldera":
                case "VolcanoDungeon0":
                    return Game1.isLocationAccessible("IslandSouth"); // Boat repair
                case "Woods": // Secret Woods
                    return Game1.player.toolBeingUpgraded.Value == null && Game1.player.getToolFromName("Axe")?.UpgradeLevel >= 2;
                case "Sewer":
                    return Game1.player.hasRustyKey;
                case "BugLand": // Mutant Bug Lair
                    return Game1.player.hasDarkTalisman;
                case "WitchSwamp": // Accessed via Railroad
                    return Game1.player.hasMagicInk;
                case "Railroad": // Event triggers Summer 3, Year 1
                    return Game1.Date.TotalDays >= (1 * 28 + 3 + 28); 
                case "CommunityCenter":
                    return true; 
                case "MovieTheater":
                    return (Game1.MasterPlayer.mailReceived.Contains("ccIsComplete") || Game1.MasterPlayer.mailReceived.Contains("JojaMember")) && Game1.isLocationAccessible("MovieTheater");
                
                // ADDED/UPDATED CHECKS:
                case "Summit": // Requires Perfection & event seen
                    return StardewValley.Utility.percentGameComplete() >= 1.0 && Game1.player.eventsSeen.Contains("79160001");
                case "Sunroom": // Caroline's Sunroom
                    return Game1.player.getFriendshipHeartLevelForNPC("Caroline") >= 2;
                case "WitchHut": // Requires Dark Talisman quest completion (Goblin Problem)
                    return Game1.player.mailReceived.Contains("witchStatueGone");
                case "Trailer_Big": // Pam's upgraded trailer
                    return Game1.player.mailReceived.Contains("pamHouseUpgrade");

                // Most standard houses/shops are accessible by default if shown (no specific unlock needed beyond game access)
                case "HaleyHouse":
                case "SamHouse":
                case "JoshHouse":
                case "ScienceHouse":
                case "ElliottHouse":
                case "LeahHouse":
                case "Trailer": // Pam's initial trailer
                case "Tent": // Linus' tent
                case "SeedShop":
                case "Saloon":
                case "Blacksmith":
                case "Hospital":
                case "AnimalShop":
                case "FishShop":
                case "ArchaeologyHouse": // Museum
                case "AdventureGuild":
                case "BathHouse_Entry":
                case "WizardHouse": // Base wizard house access
                    return true;

                // Add more checks for specific mod locations if needed
            }

            // Default to accessible ONLY if indoor locations are shown AND no specific condition failed above.
            // If HideLockedLocations is true, this default won't matter due to the filter in LoadLocations.
            // If HideLockedLocations is false, this allows unlisted indoor locations to show up.
            // Consider changing this default to 'false' if you want *only* explicitly checked locations to appear.
            return true; 
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
        /// Checks if a location is currently accessible for the player considering all factors
        /// </summary>
        /// <param name="locationName">The name of the location to check</param>
        /// <returns>True if the player can access the location, false otherwise</returns>
        public bool IsLocationAccessible(string locationName)
        {
            // Skip blacklisted locations
            if (IsBlacklisted(locationName))
                return false;
                
            // Check if the player has unlocked this location through progression
            if (!IsLocationUnlocked(locationName))
                return false;
                
            // Check for event-related restrictions
            if (IsLocationBlockedByEvent(locationName))
                return false;
                
            // Check time-of-day and weather restrictions
            if (!IsAccessibleInCurrentConditions(locationName))
                return false;
                
            // Check friendship-based access
            if (!HasSufficientFriendshipAccess(locationName))
                return false;
                
            // If all checks pass, the location is accessible
            return true;
        }
        
        /// <summary>
        /// Checks if a location is currently hosting a festival or other blocking event
        /// </summary>
        private bool IsLocationBlockedByEvent(string locationName)
        {
            try
            {
                // Check for festivals
                if (Game1.isFestival())
                {
                    // Can't warp to festival location unless already in the festival
                    if (Game1.currentLocation.Name != locationName && locationName == Game1.currentLocation.Name)
                    {
                        return true;
                    }
                }
                
                // Check for wedding event
                if (Game1.weddingToday && locationName == "Town" && Game1.timeOfDay < 1400)
                {
                    return true;
                }
                
                // Check for movies being played (if at the right time and day)
                if (locationName == "MovieTheater" && Game1.Date.DayOfWeek != 0 && Game1.timeOfDay >= 1200)
                {
                    if (Game1.player.team.movieMutex.IsLocked())
                    {
                        // Movie already in session
                        return true;
                    }
                }
                
                // Check for special cutscene locations
                if (Game1.eventUp || Game1.farmEvent != null)
                {
                    // During events, restrict warping to event locations
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error checking event restrictions for {locationName}: {ex.Message}", LogLevel.Error);
                return false; // Default to allowing access if there's an error
            }
        }
        
        /// <summary>
        /// Checks if a location is accessible based on time of day and weather
        /// </summary>
        private bool IsAccessibleInCurrentConditions(string locationName)
        {
            // Time of day restrictions
            bool isDaytime = Game1.timeOfDay >= 600 && Game1.timeOfDay < 2400;
            
            switch (locationName)
            {
                // Locations only open during the day
                case "SeedShop": // Pierre's
                case "JojaMart":
                    // Wednesday is day 3 in Stardew Valley's calendar (0 = Sunday, 3 = Wednesday)
                    return isDaytime && Game1.timeOfDay < 1700 && !Game1.isFestival() && (int)Game1.Date.DayOfWeek != 3; // Closed Wednesday
                
                case "Saloon": // Stardrop Saloon
                    return Game1.timeOfDay >= 1200 && !Game1.isFestival(); // Open from noon
                
                case "ScienceHouse": // Robin's
                case "Blacksmith": // Clint's
                case "AnimalShop": // Marnie's
                    return isDaytime && Game1.timeOfDay < 1600 && !Game1.isFestival();
                    
                case "Hospital": // Harvey's Clinic
                    // Wednesday is day 3 in Stardew Valley's calendar
                    return isDaytime && Game1.timeOfDay < 1500 && !Game1.isFestival() && (int)Game1.Date.DayOfWeek != 3; // Closed Wednesday
                
                case "Beach": // Check for storms
                    if (Game1.isLightning)
                    {
                        return Config.AllowDangerousLocations; // Optional: Limit beach access during storms
                    }
                    return true;
                    
                case "Mine": // Mines
                case "SkullCave": // Skull Cavern
                    // Optionally restrict dangerous locations when health is low
                    if (Game1.player.health < Game1.player.maxHealth * 0.25 && !Config.AllowDangerousLocations)
                    {
                        return false;
                    }
                    return true;
                
                // Desert access affected by bus schedule
                case "Desert":
                    // If configured to be strict about bus schedule
                    if (Config.StrictTransportation && Game1.timeOfDay < 1000)
                    {
                        return false; // Bus doesn't run before 10am
                    }
                    return true;
                
                default:
                    return true; // Most locations have no time restrictions
            }
        }
        
        /// <summary>
        /// Checks if player has sufficient friendship to access certain locations
        /// </summary>
        private bool HasSufficientFriendshipAccess(string locationName)
        {
            try
            {
                // For bedrooms and personal spaces, check friendship levels
                switch (locationName)
                {
                    case "HaleyHouse":
                    case "SamHouse":
                    case "JoshHouse":
                    case "ScienceHouse": // Robin's house
                    case "SebastianRoom": // Special case for Sebastian's room
                        // Basic access to houses generally allowed
                        return true;
                        
                    // Special rooms that might need friendship
                    case "MaruRoom":
                    case "ElliottHouse":
                    case "HarveyRoom":
                    case "LeahHouse":
                    case "ShantyRoom": // Willy's back room
                    case "WizardHouse":
                    case "WizardHouseBasement":
                        // For these personal spaces, can optionally require higher friendship
                        if (Config.RequireFriendshipForHomes)
                        {
                            string npcName = GetNpcForLocation(locationName);
                            if (!string.IsNullOrEmpty(npcName) && Game1.player.getFriendshipHeartLevelForNPC(npcName) < 2)
                            {
                                return false;
                            }
                        }
                        return true;
                        
                    default:
                        return true; // Most locations don't require friendship
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error checking friendship access for {locationName}: {ex.Message}", LogLevel.Error);
                return true; // Default to allowing access if there's an error
            }
        }
        
        /// <summary>
        /// Returns the NPC associated with a location for friendship checks
        /// </summary>
        private string GetNpcForLocation(string locationName)
        {
            // Map locations to their primary NPC residents
            var locationToNpc = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "ElliottHouse", "Elliott" },
                { "HarveyRoom", "Harvey" },
                { "SebastianRoom", "Sebastian" },
                { "MaruRoom", "Maru" },
                { "LeahHouse", "Leah" },
                { "WizardHouse", "Wizard" },
                { "WizardHouseBasement", "Wizard" }
                // Add more as needed
            };
            
            if (locationToNpc.TryGetValue(locationName, out string npcName))
            {
                return npcName;
            }
            
            return null;
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
            if (IsModdedLocation(mapName) && Config.ShowSVELocations)
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
                { "BathHouse_Mens" , "Spa Entrance" }, // More descriptive
                { "BathHouse_Womens", "Spa Entrance" }, // More descriptive
            };
            
            if (nameMap.TryGetValue(locationName, out string displayName))
            {
                return displayName;
            }
            
            return locationName; // Fallback to original name if not found
        }

        /// <summary>
        /// Get all available location categories
        /// </summary>
        public List<string> GetCategories()
        {
            return locationTabs.Keys.ToList();
        }
        
        /// <summary>
        /// Get all locations in a specific category
        /// </summary>
        public List<string> GetLocationsInCategory(string category)
        {
            if (locationTabs.ContainsKey(category))
            {
                return locationTabs[category];
            }
            return new List<string>();
        }
        
        /// <summary>
        /// Load a specific location and return the GameLocation object
        /// </summary>
        public GameLocation LoadLocation(string locationName)
        {
            // Refresh the locations list
            LoadLocations();
            
            // Return the location object
            return Game1.getLocationFromName(locationName);
        }
        
        /// <summary>
        /// Warp the player to a specific tile in a location
        /// </summary>
        public bool WarpToLocation(string locationName, int tileX, int tileY)
        {
            try
            {
                // Find the Game1.location object from the name
                GameLocation targetLocation = Game1.getLocationFromName(locationName);
                
                if (targetLocation == null)
                {
                    Monitor.Log($"Could not find location: {locationName}", LogLevel.Error);
                    return false;
                }
                
                // Check if the specified tile is valid
                if (!IsTileValid(targetLocation, tileX, tileY))
                {
                    // If not, try to find a nearby valid tile
                    Point validPoint = FindNearestValidTile(targetLocation, tileX, tileY);
                    if (validPoint.X >= 0 && validPoint.Y >= 0)
                    {
                        tileX = validPoint.X;
                        tileY = validPoint.Y;
                    }
                    else
                    {
                        Monitor.Log($"Could not find a valid warp point near ({tileX}, {tileY}) in {locationName}", LogLevel.Error);
                        return false;
                    }
                }
                
                // Apply warp effect if configured
                if (Config.UseWarpEffects)
                {
                    Game1.player.temporarilyInvincible = true;
                    Game1.player.temporaryInvincibilityTimer = 0;
                    Game1.player.jitterStrength = 1f;
                    Game1.fadeToBlackAlpha = 0.99f;
                    Game1.screenGlow = false;
                    
                    // Add the visual effects for warping
                    Game1.player.CanMove = false;
                    Game1.playSound("wand");
                }
                
                // Perform the actual warp
                Game1.warpFarmer(locationName, tileX, tileY, Game1.player.FacingDirection);
                Monitor.Log($"Warped to {locationName} at ({tileX}, {tileY})", LogLevel.Trace);
                return true;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error warping to {locationName}: {ex.Message}", LogLevel.Error);
                return false;
            }
        }
        
        /// <summary>
        /// Find the nearest valid tile to a given position
        /// </summary>
        private Point FindNearestValidTile(GameLocation location, int centerX, int centerY)
        {
            // Try the center position first
            if (IsTileValid(location, centerX, centerY))
            {
                return new Point(centerX, centerY);
            }
            
            // Spiral out from center to find a valid tile
            for (int radius = 1; radius < 10; radius++)
            {
                // Check in a square pattern around the center
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    for (int y = centerY - radius; y <= centerY + radius; y++)
                    {
                        // Only check the perimeter of the square
                        if (x == centerX - radius || x == centerX + radius || 
                            y == centerY - radius || y == centerY + radius)
                        {
                            if (IsTileValid(location, x, y))
                            {
                                return new Point(x, y);
                            }
                        }
                    }
                }
            }
            
            // No valid tile found nearby
            return new Point(-1, -1);
        }
        
        /// <summary>
        /// Checks if a tile is valid for warping (not blocked, etc.)
        /// </summary>
        private bool IsTileValid(GameLocation location, int x, int y)
        {
            // Check if the tile is within bounds
            if (x < 0 || x >= location.map.Layers[0].LayerWidth || y < 0 || y >= location.map.Layers[0].LayerHeight)
                return false;
            
            // Check if the tile is passable (not blocked by walls or terrain)
            if (!location.isTilePassable(new xTile.Dimensions.Location(x, y), Game1.viewport))
                return false;
                
            // Check for explicit "NoWarp" property on tile
            var tile = location.map.Layers[0].Tiles[x, y];
            if (tile == null || tile.Properties.ContainsKey("NoWarp"))
                return false;
            
            // Check for collision with objects or buildings
            Vector2 tileVector = new Vector2(x, y);
            if (location.isCollidingPosition(new Rectangle(x * Game1.tileSize, y * Game1.tileSize, Game1.tileSize, Game1.tileSize), Game1.viewport, true, 0, false, null, false, false))
                return false;
            
            // Check if the tile has terrain features or objects that block movement
            if (location.terrainFeatures.ContainsKey(tileVector))
            {
                // Check if the terrain feature is passable
                var feature = location.terrainFeatures[tileVector];
                if (!feature.isPassable())
                    return false;
            }
            
            if (location.objects.ContainsKey(tileVector))
            {
                // Check if the object is passable
                var obj = location.objects[tileVector];
                if (obj.isPassable() == false)
                    return false;
            }
            
            // Check for furniture items
            foreach (var furniture in location.furniture)
            {
                if (furniture.boundingBox.Value.Contains(x * Game1.tileSize, y * Game1.tileSize))
                    return false;
            }
            
            // Check for special areas in mines/skull cavern/volcano where players shouldn't go
            if ((location.Name.Contains("Mine") || location.Name.Contains("SkullCave") || location.Name.Contains("Volcano")))
            {
                // Check for edge cases like ladder positions, shafts, etc.
                foreach (var ladder in location.objects.Pairs)
                {
                    if ((ladder.Value.Name.Contains("Ladder") || ladder.Value.Name.Contains("Shaft")) && ladder.Key == tileVector)
                        return false; // Don't warp onto ladders or shafts
                }
                
                // Check for deep water or lava
                if (location.doesTileHaveProperty((int)tileVector.X, (int)tileVector.Y, "Water", "Back") != null ||
                    location.doesTileHaveProperty((int)tileVector.X, (int)tileVector.Y, "WaterSource", "Back") != null ||
                    location.doesTileHaveProperty((int)tileVector.X, (int)tileVector.Y, "Lava", "Back") != null)
                    return false;
            }
            
            // Check for edge tiles to avoid warping on map edges
            if (x <= 1 || y <= 1 || x >= location.map.Layers[0].LayerWidth - 2 || y >= location.map.Layers[0].LayerHeight - 2)
            {
                // Check if this edge tile has a warp
                if (location.isCollidingWithWarpOrDoor(new Rectangle(x * Game1.tileSize, y * Game1.tileSize, Game1.tileSize, Game1.tileSize)) != null)
                    return false; // Don't allow warping onto warps - it's confusing
            }
            
            // Check for water/unwalkable paths
            if (location.doesTileHaveProperty((int)tileVector.X, (int)tileVector.Y, "Water", "Back") != null ||
                location.doesTileHaveProperty((int)tileVector.X, (int)tileVector.Y, "Unwalkable", "Back") != null)
                return false;
                
            // Check for NPC pathing obstacles
            if (location.doesTileHavePropertyNoNull((int)tileVector.X, (int)tileVector.Y, "NoPath", "Buildings") == "T")
                return false;
                
            // Tile is valid for warping
            return true;
        }
    }
}