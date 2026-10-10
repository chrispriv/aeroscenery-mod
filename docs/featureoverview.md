# Feature Overview

Enhancements in **AeroScenery Community Mod l** compared with original 1.1.3-beta.

| Feature | Original 1.1.3-beta | Community Mod l |
|------|---------------------|-----------------|
| Photo scenery creation | ✅ | ✅ |
| Multiple map sources | ✅ | ✅ (extended & fixed) |
| Community maintenance | ❌ | ✅ |
| Simultaneous downloads | Up to 4 | Up to 16 (=> faster)|
| Tile & location search | Basic | Enhanced (OSM geocoding) |
| Improved workflows & UI | ❌ | ✅ |
| Portable (unzip and run) | ❌ | ✅ (default) |
| Overlay on 1.0.1 MSI | — | Optional |
| Built-in TTC converter (FS4 / FSG / both) | ❌ (SDK needed)| ✅ (default => highspeed) |
| Sequential SDK GeoConvert | Wrapper EXE | Built into AeroScenery (optional SDK) |
| Install Scenery for all selected tiles | ❌ | ✅ (waits for conversion) |
| Copy scenery to FSG working folder | ❌ | ✅ |
| Carto / OSM water masking | ❌ | ✅ |
| GeoConvert N/S shift | ❌ | ✅ (optional)|
| Elevation / OSM download | ❌ | ✅ |
| Tile Info / Generate AID/TMC metadata | ❌ | ✅ |
| TreesDetection | ❌ | Removed (use Mod j) |
| Tooltips / in-app help `(?)` | ❌ | ✅ |
| Moving map | ❌ | UDP + FS4 Bridge |
| Show Airports | fscloudport.com | OurAirports CSV import |

---

## Portable install

Unzip the release and start `AeroScenery.exe`. Optionally copy the ZIP over
Nick Hod’s **1.0.1 MSI** install. See [Installation](installation.md).

---

## Built-in converter

Default on the main window and in Settings: **Run Built-in converter**.

You do **not** need the Aerofly FS2 SDK or GeoConvert for this path. Conversion
runs in-process and is **much faster** than the old GeoConvert EXE. There is
no GeoConvert console window. The idea of an in-process converter comes from
Juan Luis Gabriel’s lean [AeroScenery FS4](https://github.com/jlgabriel/aeroscenery-fs4)
(FS4 PC only). This fork’s TTC output is our own implementation: compressed
tiles, `_mask.ttc` from alpha-channel stitched images, optional RAW PNG for
visual check, DXT1 for FS4 and ETC2 for FSG Android.

Target:

- **Aerofly FS4** — DXT1 `.ttc` (including 4×4 mask tiles when Write Images
  With Mask is on and alpha channel)
- **Aerofly FSG (Android)** — ETC2 `.ttc` written directly (no extra Content
  Converter step). You only pack the scenery as a `.tme` file
- **FS4 and FSG (Android)** — both in one run

Squares are converted one after another. DXT1 and ETC2 `.ttc` files are
compressed.

**Aerofly FS2 SDK GeoConvert** remains available in Settings if you still want
the original EXE. Sequential SDK jobs and **Install Scenery for FS4** then wait
for that process.

---

## Install FS4 scenery / pack FSG as .tme

Do not copy `.ttc` files into Aerofly folders by hand. Set **AFS Working
Scenery Folder** (FS4) and **FSG Scenery Working Folder** (Android) in Settings
first. Details: [Get Started](getstarted.md).

**Aerofly FS4 (PC)** — after conversion, AeroScenery copies DXT1 tiles into the
FS4 user scenery folder:

- Map toolbar **Install FS4 Tile** — the currently selected square only
- Actions **Install Scenery for FS4** — every selected square, after conversion
  finishes (Default Actions does this when the convert target includes FS4)

**FSG (Android)** — the built-in converter already produces ETC2 tiles. There is
no second conversion with the SDK Content Converter.

- Actions **Copy Scenery to FSG Scenery Working Folder** (Default Actions when
  the convert target includes FSG) copies those tiles into  
  `fsg_scenery_<name>\fsg_scenery_<name>_images\scenery\images\`
- Zip the `fsg_scenery_<name>_images` folder, rename `.zip` to `.tme`, and copy that file to 
  FSG data path on your Android device. That pack step is the only remaining work for mobile image scenery

---

## Map sources and downloads

Regional and global sources (Google, Bing, map portals, and others) with
fixes over the original 1.1.3-beta. Up to sixteen simultaneous downloads
(true parallel workers, following AeroScenery FS4).
**Fix Missing Tiles** re-gets empty or missing tiles only (optional).

---

## Tile and location search

Search by Aerofly grid name (for example `8500_a500`) or by place name
(OpenStreetMap geocoding).

---

## Water masking and image processing

Optional Carto Basemaps / OSM water mask (API key under Image Sources).
That masking, plus `_mask.ttc` tiles, is the water/coast approach in this Mod
(it also works for FSG Android). A map-drawn coastline cut is not used.
Brightness, contrast and related sliders apply when you run **Stitch Image
Tiles** again.

---

## Elevation and OSM data

Optional OpenTopography elevation (API key) and OSM Overpass download from
Actions, after the matching Settings switches. Elevation-only squares draw
with a **green** map border. **Generate AID / TMC** stores image-processing
and convert settings per tile; **Tile Info** shows them.

---

## Moving map (additional feature)

**Aerofly FS4 on PC only.** UDP broadcast (port **49002**, IP `…255`) or
shared memory via `AeroflyBridge.dll`
([jlgabriel/Aerofly-FS4-Bridge](https://github.com/jlgabriel/Aerofly-FS4-Bridge)).
Map-fixed, flight trace, hide working tiles, HUD. Setup: [Get Started](getstarted.md).

---

## Show Airports

Import current **OurAirports** `airports.csv` (and optionally `runways.csv`)
from Settings. **Show Airports** then appears on the map toolbar.

---

## TreesDetection

Not supported in Mod l (Aerofly FS4 global data; the extra app is no longer
public). Use **Mod j** if you still need it.
