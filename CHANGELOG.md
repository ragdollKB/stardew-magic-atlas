# Changelog

## [1.0.0] - 2025-04-20

### Fixed
- Magic Atlas is now spawned and checked using the new ItemRegistry system and Data/Objects, ensuring correct sprite, name, and description. No more Topaz or error items. Right-click and menu logic will now work as intended.

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

## [0.4.2] - 2025-04-20
### Fixed
- Resolved game save error where the `MagicAtlasItem` type was not recognized by the XML serializer by adding `XmlInclude` and `XmlType` attributes.

## [0.4.1] - 2025-04-20
### Fixed
- Resolved game save error caused by the Magic Atlas item missing a parameterless constructor required for serialization.

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