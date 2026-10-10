# AeroScenery Community Mod – Documentation

Guides for the **AeroScenery Community Mod l** fork of Nick Hod’s AeroScenery
1.1.3-beta. Mod l is **portable** (unzip and run). 

<p><img src="aeroscenery_main_default_mod-l.jpg" alt="AeroScenery Community Mod l main window" width="800"></p>

---

## Getting Started

- 🚀 [Get Started](getstarted.md)  
  Install AeroScenery, first scenery, built-in FS4 / FSG conversion, moving map.

- 🛠️ [Installation Guide](installation.md)  
  Portable ZIP (default) or overlay on the official 1.0.1 MSI, converter
  settings, first Settings tabs.

---

## Features and help

- ⭐ [Feature Overview](featureoverview.md)
- ❓ [FAQ](faq.md)
- 📝 [Changelog](https://github.com/chrispriv/aeroscenery-mod/blob/mod-l/CHANGELOG.md)

---

## Project

Unofficial community fork of [AeroScenery](https://github.com/nickhod/aeroscenery)
by Nick Hod. The original author is inactive; this **Mod l** line keeps the
app usable on current PCs and Aerofly FS 4.

**Credits.** Parallel image downloads, the in-process converter that replaces
SDK GeoConvert, and the run timer follow work published by **Juan Luis
Gabriel** in [AeroScenery FS4](https://github.com/jlgabriel/aeroscenery-fs4)
(currently 2.2.1). That fork is a lean, FS4-PC-only photoscenery app. Community
**Mod l** keeps the wider toolset (FSG Android, moving map, OSM, elevation,
Carto water masking) and uses its own TTC writer: compressed tiles, `_mask.ttc`
from alpha-channel stitched images, optional RAW PNG for visual check, DXT1
for FS4 and ETC2 for FSG. Carto/OSM water masking is used instead of a
coastline cut; FSG Android needs `_mask` tiles, which a coastline cut does
not provide. The moving-map DLL is
[Aerofly FS4 Bridge](https://github.com/jlgabriel/Aerofly-FS4-Bridge)
(same author).

Both apps can run on the same PC: AeroScenery FS4 writes `settings2.xml` so it
does not overwrite this Mod’s `settings.xml` (API keys stay intact). Downloaded
grid squares in the SQLite database are **not** shared between the two.

⬅️ [GitHub repository](https://github.com/chrispriv/aeroscenery-mod)
