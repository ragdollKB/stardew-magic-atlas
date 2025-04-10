# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0] - 2025-04-07

### Changed
- Removed sparkle effect from the fountain, keeping only the magnifying glass cursor
- Reduced the size of the Magic Atlas item in the inventory for better visual balance
- Updated item descriptions to be clearer and more immersive

### Fixed
- Fixed visual issues with the Magic Atlas item appearing too large in inventory
- Improved clarity of item descriptions to better explain the mod's functionality

## [0.3.0] - 2025-04-06

### Added
- Magic Atlas is now an in-game item that can be found in the playground near the swings
- Players can see something shimmering in the soil near the swings
- The Magic Atlas can be retrieved by digging at the playground with a hoe
- Added configuration option to use the atlas without finding the item
- Added README.readme file for SMAPI website compatibility
- Support for additional map locations (including all indoor locations)
- Improved map thumbnail generation
- Enhanced category sorting for better organization
- Added debug command `give_atlas` for testing

### Changed
- Warping now requires finding the Magic Atlas item by default
- Optimized map rendering for faster load times
- Improved error handling for invalid warp locations
- Updated documentation for clarity

### Fixed
- Fixed issue with location names not displaying correctly in some languages
- Resolved conflicts with certain location mods
- Fixed rare crash when warping to farm buildings

## [0.2.0] - 2025-03-29

### Added
- Enhanced UX following "Don't Make Me Think" principles
- Clearer visual hierarchy and improved readability
- Larger, more accessible buttons with proper text sizing
- Improved instructions and visual cues for better usability
- Enhanced search functionality with visual feedback
- Better contrast and visual indicators for selected items
- Added map instruction text: "Click on map to warp to that exact location"

### Changed
- Increased overall menu size for better readability (1200x700)
- Made buttons larger to fit location names properly
- Improved visual feedback for interactive elements
- Enhanced sidebar with better tab navigation

### Fixed
- Fixed missing sounds for warping by letting the game handle sound effects
- Fixed issue where text wouldn't fit in location buttons
- Fixed search functionality in modded location tabs
- Improved contrast for better readability

## [0.1.0] - 2025-03-23

### Added
- Initial release of the Warp Mod
- Grid-based warp menu interface with location categories
- Visual map preview system
- Location categories: Town, Farm, Beach, Mountain, and Indoor
- Smart warp validation to prevent invalid warping
- Controller support with Back button
- Keyboard support with K key
- Generic Mod Config Menu integration
- Basic mod configuration (enable/disable warp functionality)
- Map preview for each location
- Invalid location detection and nearby valid tile finding

### Changed
- N/A (Initial Release)

### Fixed
- N/A (Initial Release)

[0.4.0]: https://github.com/yourusername/stardew-magic-atlas/releases/tag/v0.4.0
[0.3.0]: https://github.com/yourusername/stardew-magic-atlas/releases/tag/v0.3.0
[0.2.0]: https://github.com/yourusername/stardew-magic-atlas/releases/tag/v0.2.0
[0.1.0]: https://github.com/yourusername/stardew-magic-atlas/releases/tag/v0.1.0