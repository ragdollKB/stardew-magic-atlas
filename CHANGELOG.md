# Changelog

## Unreleased
- Fixed property name mismatch in ModEntry.cs (updated references from `AllowHotkeyWithoutItem` to `AllowWarpingWithoutItem`)

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.4] - 2025-04-12

### Fixed
- Implemented comprehensive fix for the persistent bug where the Magic Atlas could disappear from inventory and spawn a yellow stone object
- Added automatic inventory restoration mechanism that detects and fixes Atlas item issues
- Added protection to prevent the Atlas from being dropped accidentally
- Improved warp handling to preserve Atlas items during location changes
- Cleanup system that removes any yellow stones that appear near the player

## [0.4.3] - 2025-04-12

### Fixed
- Fixed open atlas image not resizing correctly with window size
- Fixed overlapping text on the open atlas image
- Fixed bug where the Magic Atlas would disappear from inventory and spawn a yellow stone object after use
- Improved atlas instruction text to be clearer

## [0.4.2] - 2025-04-12

### Changed
- Improved Atlas item by using properly sized 64x64 sprite
- Removed "Magic Atlas" title from the Atlas interface when a location is selected
- Added visual improvement showing an open atlas image when no location is selected

## [0.4.1] - 2025-04-12

### Fixed
- Fixed error in FountainHandler where `isTileLocationTotallyClearAndPlaceable` method didn't exist by using the correct collision detection method
- Improved item spawning code to better handle collisions and find valid placement locations
- Fixed Magic Atlas item appearing as "Large Egg" in inventory by using correct item ID and custom sprite
- Implemented proper custom sprite rendering for the Magic Atlas when held, in inventory, and in world
- Fixed drawWhenHeld method to properly position the item when player is holding it
- Improved the Atlas spawning animation from the fountain to make it more visible

## [0.4.0] - 2025-04-20
### Fixed
- Resolved build errors related to duplicate assembly attributes and incorrect inclusion of test projects.

## [0.3.0] - 2025-04-19
### Added
- Magic Atlas is now an in-game item found in the Town Fountain during winter.
- Configuration option `UseAtlasWithoutItem` to allow opening the menu with a keybind even without the item.

### Changed
- Default warp key changed from `K` to `None` if `UseAtlasWithoutItem` is false.

## [0.2.0] - 2025-04-18
### Added
- Player position indicator on the current location's map thumbnail.
- Support for Stardew Valley Expanded (SVE) locations.

### Changed
- Improved map thumbnail loading and rendering.

## [0.1.0] - 2025-04-17
- Initial release.