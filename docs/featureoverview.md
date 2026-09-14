# Feature Overview

Enhancements in **AeroScenery Community Mod k** compared with original 1.1.3-beta.

| Feature | Original 1.1.3-beta | Community Mod k |
|------|---------------------|-----------------|
| Photo scenery creation | ✅ | ✅ |
| Multiple map sources | ✅ | ✅ (extended & fixed) |
| Community maintenance | ❌ | ✅ |
| Simultaneous downloads | Up to 4 | Up to 8 |
| Tile & location search | Basic | Enhanced (OSM geocoding) |
| Improved workflows & UI | ❌ | ✅ |
| Portable (unzip and run) | ❌ | ✅ (default) |
| Overlay on 1.0.1 MSI | — | Optional |
| Sequential GeoConvert | Wrapper EXE | Built into AeroScenery |
| Install Scenery for all selected tiles | ❌ | ✅ (waits for GeoConvert) |
| Carto / OSM water masking | ❌ | ✅ |
| GeoConvert N/S shift | ❌ | ✅ |
| Elevation / OSM download | ❌ | ✅ |
| TreesDetection | ❌ | Removed (use Mod j) |
| PowerShell helpers | ❌ | ✅ |
| Moving map | ❌ | UDP + FS4 Bridge |
| Show Airports (fscloudport) | ✅ | Hidden (server offline; code kept) |

---

## Portable install

Unzip the release and start `AeroScenery.exe`. Optionally copy the ZIP over
Nick Hod’s **1.0.1 MSI** install. See [Installation](installation.md).

---

## Sequential GeoConvert

Mod j could start several GeoConvert processes at once (high CPU and RAM).
Mod k can run them **one after another** (Settings → GeoConvert). The same
applies when **Install Scenery (waiting for GeoConvert)** is on. Fast PCs may
still run GeoConvert in parallel and download or stitch more tiles at the same
time.

---

## Install Tile / Install Scenery

Use the map **Install Tile** button or Actions **Install Scenery (waiting for
GeoConvert)** instead of copying `.ttc` files by hand. Set **AFS Working
Scenery Folder** in Settings. Details: [Get Started](getstarted.md).

---

## Map sources and downloads

Regional and global sources (Google, Bing, map portals, and others) with
fixes over the original 1.1.3-beta. Up to eight simultaneous image downloads.
**Fix Missing Tiles** re-gets empty or missing tiles only.

---

## Tile and location search

Search by Aerofly grid name (for example `8500_a500`) or by place name
(OpenStreetMap geocoding).

---

## Water masking and image processing

Optional Carto Basemaps / OSM water mask (API key under Image Sources).
Brightness, contrast and related sliders apply when you run **Stitch Image
Tiles** again.

---

## Elevation and OSM data

Optional OpenTopography elevation (API key) and OSM Overpass download from
Actions, after the matching Settings switches.

---

## Moving map

UDP broadcast (port 49002) for FS2/FS4, plus Aerofly FS4 **shared memory**
via `aeroflybridge.dll`. Map-fixed, flight trace, hide working tiles, HUD.

---

## TreesDetection

Not supported in Mod k (Aerofly FS4 global data; the extra app is no longer
public). Use **Mod j** if you still need it.
