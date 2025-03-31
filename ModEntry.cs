using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
using System;
using System.IO;
using System.Reflection;
using WarpMod.Utility;

namespace WarpMod
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private ModConfig config;
        private bool mapWarpEnabled = true; // Enable map warping by default
        private MapRenderer mapRenderer; // Store reference for resource cleanup

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            // Read config and initialize
            this.config = helper.ReadConfig<ModConfig>();
            this.mapWarpEnabled = this.config.MapWarpEnabled;
            
            // Initialize the map renderer
            this.mapRenderer = new MapRenderer(this.Monitor, helper);

            // Hook into events
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
            helper.Events.Display.WindowResized += this.OnWindowResized;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.DayEnding += this.OnDayEnding;

            this.Monitor.Log("Magic Atlas mod initialized", LogLevel.Info);
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // Ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            if (!mapWarpEnabled)
                return;
                
            // Check if warp key is pressed (using config binding)
            if (this.config.WarpKey.JustPressed())
            {
                // Open our custom menu
                Game1.activeClickableMenu = new GridWarpMenu(this.Helper, this.Monitor, this.config);
                Helper.Input.Suppress(e.Button);
            }
        }

        /// <summary>Called when a new day starts</summary>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            // Nothing needed here
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // Refresh settings when save is loaded
            this.mapWarpEnabled = this.config.MapWarpEnabled;
            
            // Check if map files exist in assets folder
            CheckMapAssets();
            
            // Check if SVE is installed and log
            CheckForSVE();
        }
        
        /// <summary>Check if map assets exist in the assets folder</summary>
        private void CheckMapAssets()
        {
            string mapFolder = Path.Combine(this.Helper.DirectoryPath, "assets", "maps");
            if (Directory.Exists(mapFolder))
            {
                string[] mapFiles = Directory.GetFiles(mapFolder, "*.png");
                this.Monitor.Log($"Found {mapFiles.Length} map files in assets/maps folder", LogLevel.Info);
            }
            else
            {
                this.Monitor.Log("Map assets folder not found. Maps may not display correctly.", LogLevel.Warn);
            }
        }
        
        /// <summary>Check if SVE is installed and log appropriate message</summary>
        private void CheckForSVE()
        {
            bool sveDetected = Game1.getLocationFromName("Custom_Backwoods") != null ||
                             Game1.getLocationFromName("Custom_GramplesHouse") != null;
                             
            if (sveDetected)
            {
                this.Monitor.Log("Stardew Valley Expanded detected - enabling expanded location support", LogLevel.Info);
                
                // Make sure SVE locations are enabled if detected
                if (!this.config.ShowSVELocations)
                {
                    this.Monitor.Log("SVE support was disabled in config but SVE is installed - enabling SVE location support", LogLevel.Debug);
                    this.config.ShowSVELocations = true;
                    this.Helper.WriteConfig(this.config);
                }
            }
        }

        /// <summary>Called when the day is ending</summary>
        private void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            // Nothing needed here in simplified version
        }

        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            // Cleanup not needed in simplified version
        }
        
        private void OnWindowResized(object sender, WindowResizedEventArgs e)
        {
            // Cleanup not needed in simplified version
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Set up Generic Mod Config Menu integration
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu == null)
            {
                this.Monitor.Log("Generic Mod Config Menu not available", LogLevel.Info);
                return;
            }

            // Register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.config)
            );

            // Add bool option for enabling/disabling map warping
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Enable Grid Warp",
                tooltip: () => "Whether to enable warping using the grid menu (K key).",
                getValue: () => this.config.MapWarpEnabled,
                setValue: value => {
                    this.config.MapWarpEnabled = value;
                    this.mapWarpEnabled = value;
                }
            );
            
            // Add keybind option
            configMenu.AddKeybindList(
                mod: this.ModManifest,
                name: () => "Warp Key",
                tooltip: () => "The key to press to open the warp menu.",
                getValue: () => this.config.WarpKey,
                setValue: value => this.config.WarpKey = value
            );
            
            // Add option for grouping modded locations
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Group Modded Locations",
                tooltip: () => "Whether to show modded locations in a separate category.",
                getValue: () => this.config.GroupModdedLocations,
                setValue: value => this.config.GroupModdedLocations = value
            );
            
            // Add option for warp effects
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Use Warp Effects",
                tooltip: () => "Whether to use a special animation effect when warping.",
                getValue: () => this.config.UseWarpEffects,
                setValue: value => this.config.UseWarpEffects = value
            );
            
            // Add option for SVE locations
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Show SVE Locations",
                tooltip: () => "Whether to prioritize SVE locations in the warp menu.",
                getValue: () => this.config.ShowSVELocations,
                setValue: value => this.config.ShowSVELocations = value
            );
            
            // Add integer option for max locations per category
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Show More Locations",
                tooltip: () => "Whether to show many locations per category.",
                getValue: () => this.config.MaxLocationsPerCategory > 8,
                setValue: value => this.config.MaxLocationsPerCategory = value ? 12 : 8
            );

            this.Monitor.Log("GMCM integration completed", LogLevel.Info);
        }
    }
}
