# Magic Atlas - Stardew Valley Mod

A magical map interface that lets you instantly warp to any location in Stardew Valley. Features an elegant grid-based menu with location previews and smart pathfinding.

## Features

- Grid-based location warp menu
- Location previews with map thumbnails
- Player position indicator on current location's map
- Support for modded locations (including Stardew Valley Expanded)
- Customizable key binding

## Installation

1. Install the latest version of [SMAPI](https://smapi.io/)
2. Download the mod from [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/32591)
3. Extract the mod folder into your `Stardew Valley/Mods` directory
4. Launch the game with SMAPI

## Usage

Press the warp key (configurable, default is `M`) to open the Magic Atlas grid menu. Select any location to instantly warp there.

## Map Images

The mod uses map images to display location thumbnails in the warp menu. There are several ways these images are loaded:

1. **Pre-exported PNGs** - The mod will first check the `assets/maps` folder for PNG images matching location names.
2. **Direct Rendering** - If no pre-exported image is found, the mod will attempt to render location maps directly.
3. **Fallback Rendering** - If all else fails, the mod will render a simple colored map with the location name.

### Exporting Map Images with Tiled

You can use the included `export_maps.js` script with [Tiled](https://www.mapeditor.org/) to export Stardew Valley map files as PNG images:

1. Install [Tiled](https://www.mapeditor.org/)
2. Make sure you've unpacked the XNB files using XNBhack to `game_folder/Stardew Valley/Content (unpacked)`
3. Open Tiled
4. Run the export script with the following command:

```
tiled --script "C:/Users/kanto/OneDrive/Documents/stardew_plugin/export_maps.js"
```

This will export all the map images to the `assets/maps` folder for use by the mod.

You can customize which maps to export by editing the `mapFiles` array in the `export_maps.js` script.

## Configuration

You can configure the mod through the `config.json` file that's created after first launch:

```json
{
  "WarpKey": "M",
  "MapWarpEnabled": true,
  "EnableMapCaching": true,
  "ShowSVELocations": true,
  "MapImagesPath": ""
}
```

- `WarpKey` - The key to open the warp menu
- `MapWarpEnabled` - Enable/disable the warp functionality
- `EnableMapCaching` - Cache map images for better performance
- `ShowSVELocations` - Show Stardew Valley Expanded locations (if installed)
- `MapImagesPath` - Custom path for map images (optional)

## Compatibility

- Works with Stardew Valley 1.5.6 or later
- Compatible with Stardew Valley Expanded and other location mods
- Compatible with most other SMAPI mods

## For Developers

If you're working on this mod or using it as a reference:

1. You'll need the XNB files extracted using [XNBhack](https://stardewvalleywiki.com/Modding:Editing_XNB_files)
2. The `MapRenderer` class handles loading and rendering map thumbnails
3. Use the `export_maps.js` script with Tiled to pre-generate map PNGs

## Credits

- Created by Ragdoll
- SMAPI by Pathoschild
- Stardew Valley by ConcernedApe