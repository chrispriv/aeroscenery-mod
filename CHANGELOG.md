# Changelog

All notable changes to this project are documented in this file.

This project is a community-maintained fork of
[AeroScenery](https://github.com/nickhod/aeroscenery).

---

## [1.1.3-mod.j] – Community Mod j
**Maintainer:** chrispriv  
**Based on:** AeroScenery 1.1.3-beta

### Added
- Moving map functionality for Aerofly FS2 / FS4 using UDP data streaming
- Modes: *Map fixed*, *Flight tracing*, *Hide working tiles*
- Automatic IP address detection
- Interpolation for smoother moving-map movement
- Enhanced *Search Tile / Location* function with geocoding via OpenStreetMap data

---

## [1.1.3-mod.i] – Community Mod i

### Added
- Tooltips and hints to improve usability for beginners
- Configurable working scenery name for easier installation
- Support for downloading elevation data with **10 m resolution** (USGS via OpenTopography)
- Trees Presets selection (works with TreesDetection App 1.0 Beta)
- Batch conversion of raw images to `.tcc` files for **Android (Mobile)** platform

---

## [1.1.3-mod.h] – Community Mod h

### Fixed
- Switzerland Geoportals
- LINZ (New Zealand)
- Here WeGo map source (now requires API key)

### Changed
- Google Satellite switched to secured HTTPS
- Manual removal of deprecated Google URL template required

### Added
- PowerShell script for downloading 30 m elevation GeoTIFF data from OpenTopography
- QGIS Processing Executor integration (coastline peak fixing, GeoTIFF decompression)
- Separate action selections for OSM and elevation data downloads
- Maximum altitude setting for TreesDetection
- Improved handling of missing **and empty** image tiles

---

## [1.1.3-mod.g] – Community Mod g

### Added
- Integrated *TreesDetection App* as optional processing step
- Configurable number of simultaneous downloads (1–8, recommended: 6)
- Tile search by FS2 grid coordinates (e.g. `8500_a500`)
- Toolbar buttons:
  - Open AFS2 User Folder
  - Open Scenery Editor (FS2 Cultivation Editor by Nabeel)

---

## [1.1.3-mod.f] – Community Mod f

### Added
- PowerShell script for manual OSM data download via Overpass API
- Boundary box copy to clipboard for AFS2 Editor
- Additional Google Earth (Web) map option
- Clipboard helpers for coordinates and grid square names

### Changed
- Optimized tile download logic to fetch only missing image tiles

---

## [1.1.3-mod.e] – Community Mod e

### Added
- Mapbox map source (requires access token)
- Enhanced PowerShell script to download missing image tiles only

### Changed
- Increased retry attempts for tile downloads
- Improved reliability for HTTPS ArcGIS sources

---

## [1.1.3-mod.d] – Community Mod d

### Fixed
- ArcGIS source working again  
  *(Known issue: some tiles may still be missing)*

---

## [1.1.3-mod.c] – Community Mod c

### Added
- PowerShell script generation for manual bulk image tile downloads

---

## [1.1.3-mod.b] – Community Mod b

### Added
- Carto DB Light map source for masking purposes

---

## [1.1.3-mod.a] – Community Mod a

### Fixed
- fscloudport airports due to URL and HTTPS changes

---

# Original AeroScenery Releases (Upstream)

## [1.1.3]
- Temporary fix for Here WeGo maps version change
- USGS source working again
- Added GuleSider orthophoto source
- Option to disable confirmation message for concurrent GeoConvert runs

## [1.1.2]
- Added support for Here WeGo maps orthophoto source

## [1.1.1]
- Added retry for USGS tiles on non-success HTTP responses

## [1.1.0]
- Added support for many additional orthophoto sources
- Fixed culture-specific decimal separator issue

## [1.0.2]
- Fixed issues with hex folder names
- GeoConvert wrapper enabled by default

## [1.0.1]
- Fixed null registry entry issues
- Fixed missing sample image installation

## [1.0.0]
- Switched from registry-based to XML-based configuration
- Added Install Scenery toolbar button
- Multiple fixes and enhancements to grid handling and GeoConvert integration

## [0.6]
- Improved GeoConvert handling and UI stability
- Multiple fixes related to culture handling and tile processing
