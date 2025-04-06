// Tiled script to export Stardew Valley map files as PNG images
// Usage: tiled --script export_maps.js

var FileInfo = {
    join: function(path1, path2) {
        if (path1.endsWith('/') || path1.endsWith('\\'))
            return path1 + path2;
        return path1 + '/' + path2;
    }
};

// Configuration
var config = {
    // Source directory with unpacked TBIN files - Corrected based on user feedback
    sourceDir: "C:/Users/kanto/OneDrive/Documents/stardew-magic-atlas/game_folder/Stardew Valley/Content (unpacked)/Maps",
    
    // Target directory for exported PNG files
    targetDir: "C:/Users/kanto/OneDrive/Documents/stardew-magic-atlas/assets/maps",
    
    // List of maps to export (add or remove as needed)
    mapFiles: [
        "Farm.tbin",
        "Town.tbin",
        "Beach.tbin",
        "Mountain.tbin",
        "Forest.tbin",
        "BusStop.tbin",
        "Desert.tbin",
        "Woods.tbin",
        "Railroad.tbin",
        "Mine.tbin", // Note: This is just the entrance map
        "Backwoods.tbin",
        "SeedShop.tbin",
        "ScienceHouse.tbin",
        "AnimalShop.tbin",
        "FishShop.tbin",
        "Saloon.tbin",
        "ManorHouse.tbin",
        "Blacksmith.tbin",
        "Hospital.tbin",
        "IslandSouth.tbin",
        "IslandWest.tbin",
        "IslandNorth.tbin",
        "IslandEast.tbin",
        // Added missing indoor/special locations:
        "SkullCave.tbin",
        "Club.tbin", // Casino
        "Sewer.tbin",
        "BugLand.tbin",
        "WitchSwamp.tbin",
        "WitchHut.tbin",
        "CommunityCenter.tbin", // Base Community Center
        "MovieTheater.tbin",
        "Summit.tbin",
        "Sunroom.tbin",
        "Trailer.tbin", // Pam's initial trailer
        "Trailer_Big.tbin", // Pam's upgraded trailer
        "Tent.tbin", // Linus' tent
        "ArchaeologyHouse.tbin", // Museum
        "AdventureGuild.tbin",
        "BathHouse_Entry.tbin",
        "WizardHouse.tbin",
        "WizardHouseBasement.tbin",
        "HaleyHouse.tbin",
        "SamHouse.tbin",
        "JoshHouse.tbin",
        "ElliottHouse.tbin",
        "LeahHouse.tbin",
        "HarveyRoom.tbin",
        "SebastianRoom.tbin"
        // Add others if needed, e.g., specific mine levels aren't practical here
    ]
};

// Log function
function log(message) {
    if (tiled.isMainThread) {
        tiled.log(message);
    } else {
        console.log(message);
    }
}

// Export a map to PNG
function exportMapToPNG(mapFile) {
    try {
        var mapFilePath = FileInfo.join(config.sourceDir, mapFile);
        var targetFilePath = FileInfo.join(config.targetDir, mapFile.replace(".tbin", ".png"));
        
        log("Processing: " + mapFilePath);
        
        // Load the map
        var map = tiled.open(mapFilePath);
        if (!map) {
            log("Failed to open map: " + mapFilePath);
            return false;
        }
        
        // Export to PNG
        var exportResult = tiled.mapRenderer(map).exportToImage(targetFilePath);
        if (exportResult) {
            log("Exported successfully to: " + targetFilePath);
            return true;
        } else {
            log("Failed to export: " + targetFilePath);
            return false;
        }
    } catch (e) {
        log("Error processing " + mapFile + ": " + e);
        return false;
    }
}

// Main function to process all maps
function main() {
    log("Starting map export process...");
    log("Source directory: " + config.sourceDir);
    log("Target directory: " + config.targetDir);
    
    var successCount = 0;
    var failCount = 0;
    
    for (var i = 0; i < config.mapFiles.length; i++) {
        var mapFile = config.mapFiles[i];
        if (exportMapToPNG(mapFile)) {
            successCount++;
        } else {
            failCount++;
        }
    }
    
    log("Export complete: " + successCount + " succeeded, " + failCount + " failed");
}

// Run the script
if (tiled.isMainThread) {
    main();
}