# AeroScenery Community Mod (Mod l)

This repository is an **unofficial, community-maintained fork** of
[AeroScenery](https://github.com/nickhod/aeroscenery), originally developed by Nick Hod.

It is based on **AeroScenery 1.1.3-beta** and continues the community line
through Mods a–k. The current public line is **Mod l**: portable by default,
no community MSI, with a **built-in TTC converter** for Aerofly FS4 and FSG
Android. SDK GeoConvert remains optional.

---

## Overview

- Base version: **AeroScenery 1.1.3-beta**
- .NET Framework **4.8**
- Current line: **1.1.3 Mod l**
- Original author currently inactive

Mod l runs as a **portable** app (extract and run). The original
**AeroScenery 1.0.1 MSI** is optional (overlay).

---

## Feature Overview

| Feature | Original 1.1.3-beta | Community Mod l |
|------|---------------------|-----------------|
| Photo scenery creation | ✅ | ✅ |
| Multiple map sources | ✅ | ✅ (extended & fixed) |
| Community maintenance | ❌ | ✅ |
| Simultaneous downloads | Up to 4 | Up to 16 |
| Tile & location search | Basic | Enhanced (OSM geocoding) |
| Portable (no MSI required) | ❌ | ✅ |
| Built-in TTC converter (FS4 / FSG / both) | ❌ | ✅ |
| Sequential SDK GeoConvert | ❌ | ✅ (optional SDK) |
| Install Scenery (selected tiles) | Toolbar only | Toolbar + Actions (waits for conversion) |
| Copy to FSG working folder | ❌ | ✅ |
| Carto / OSM water masking | ❌ | ✅ |
| GeoConvert N/S shift correction | ❌ | ✅ |
| Aerofly FS4 Bridge moving map | ❌ | ✅ (UDP + shared memory) |
| Elevation / OSM download scripts | ❌ | ✅ |
| OurAirports on the map | ❌ | ✅ (CSV import) |
| TreesDetection integration | ❌ | Removed in Mod k (still in Mod j) |

---

## Projects in this Repository

- **AeroScenery**  
  Main application (only remaining project)

`GeoConvertWrapper` and the old WiX **AeroSceneryInstaller** were removed
in Mod k. Sequential SDK GeoConvert and the built-in converter run inside
AeroScenery.

---

## Installation

**Default (portable):** download the Mod l ZIP from GitHub Releases, extract
the whole folder anywhere, start `AeroScenery.exe`. Then fill in Settings tabs
**AeroScenery** and **GeoConvert**. The built-in converter needs no SDK. If you
use SDK GeoConvert, set the path to **`aerofly_fs_2_geoconvert.exe`**.

**Alternative:** install Nick Hod’s official [AeroScenery 1.0.1 MSI](https://github.com/nickhod/aeroscenery/releases/tag/1.0.1)
(there is no official 1.1.3 installer), then copy the Mod l ZIP over
`Program Files (x86)\AeroScenery\` and overwrite.

Keep every file next to the EXE. NuGet does not restore all runtime libraries.

➡️ [Installation](docs/installation.md) · [Get started](docs/getstarted.md)

---

## GeoConvert (Aerofly FS SDK)

Optional. AeroScenery’s default is the **built-in converter**.

SDK GeoConvert is **not included**. It is no longer officially supported by
IPACS, but is still available on Aerofly-Sim.de:
[Aerofly FS 2 Software Development Kit (SDK)](https://www.aerofly-sim.de/aerofly_fs_2_sdk).

➡️ See: [Installation](docs/installation.md)

---

## Releases

Binary releases (portable ZIP, no installer) are provided via **GitHub Releases**.

Published community line so far:

- **v1.1.3-mod.j**
- **v1.1.3-mod.k** (portable ZIP, no installer)
- **v1.1.3-mod.l** (portable ZIP, built-in converter) — *when published*

---

## Documentation

- 📘 **Documentation site:** https://chrispriv.github.io/aeroscenery-mod/
- ▶️ [Installation Guide](docs/installation.md)
- 🚀 [Get Started Guide](docs/getstarted.md)
- ⭐ [Feature Overview](docs/featureoverview.md)
- ❓ [FAQ](docs/faq.md)
- 📝 [Changelog](CHANGELOG.md)
- In-app notes: `AeroScenery/changelog.txt`

---

## Notes

- Mod l is portable. Runtime libraries must sit next to `AeroScenery.exe`.
- Community project, provided "as is".

---

## License

This project is licensed under the **GNU General Public License v3.0 (GPL-3.0)**,
in accordance with the original AeroScenery project.

---

## Credits

- Original AeroScenery by **Nick Hod**
- Community maintenance and extensions by **@chrispriv**
- Parallel image downloads, the in-process converter that replaces SDK
  GeoConvert, and the run timer follow **Juan Luis Gabriel**’s
  [AeroScenery FS4](https://github.com/jlgabriel/aeroscenery-fs4) (2.2.1), a
  lean FS4-PC-only photoscenery app. Mod l keeps FSG Android, moving map, OSM,
  elevation and Carto water masking. Its TTC writer is a separate path:
  compressed tiles, `_mask.ttc` from alpha-channel images, optional RAW PNG,
  DXT1 and ETC2. Water masking is used instead of a coastline cut (FSG needs
  `_mask` tiles). Settings files differ (`settings.xml` vs `settings2.xml`)
  so API keys are safe; the SQLite grid-square database is **not** shared.
- Aerofly FS4 Bridge (`AeroflyBridge.dll`) by **[Juan Luis Gabriel](https://github.com/jlgabriel/Aerofly-FS4-Bridge)**
