# AeroScenery Community Mod (Mod k)

This repository is an **unofficial, community-maintained fork** of
[AeroScenery](https://github.com/nickhod/aeroscenery), originally developed by Nick Hod.

It is based on **AeroScenery 1.1.3-beta** and continues the community line
through Mods a–j. The current public line is **Mod k**: portable by default,
no community MSI, sequential GeoConvert built into the main app.

---

## Overview

- Base version: **AeroScenery 1.1.3-beta**
- .NET Framework **4.8**
- Current line: **1.1.3 Mod k**
- Original author currently inactive

Mod k runs as a **portable** app (extract and run). The original
**AeroScenery 1.0.1 MSI** is optional (overlay). A new community MSI is planned
for a later **2.0.0** release, not for Mod k.

---

## Feature Overview

| Feature | Original 1.1.3-beta | Community Mod k |
|------|---------------------|-----------------|
| Photo scenery creation | ✅ | ✅ |
| Multiple map sources | ✅ | ✅ (extended & fixed) |
| Community maintenance | ❌ | ✅ |
| Simultaneous downloads | Up to 4 | Up to 8 |
| Tile & location search | Basic | Enhanced (OSM geocoding) |
| Portable (no MSI required) | ❌ | ✅ |
| Sequential GeoConvert (built-in) | ❌ | ✅ |
| Install Scenery (selected tiles) | Toolbar only | Toolbar + Actions (waits for GeoConvert) |
| Carto / OSM water masking | ❌ | ✅ |
| GeoConvert N/S shift correction | ❌ | ✅ |
| Aerofly FS4 Bridge moving map | ❌ | ✅ (UDP + shared memory) |
| Elevation / OSM download scripts | ❌ | ✅ |
| TreesDetection integration | ❌ | Removed in Mod k (still in Mod j) |

---

## Projects in this Repository

- **AeroScenery**  
  Main application (only remaining project)

`GeoConvertWrapper` and the old WiX **AeroSceneryInstaller** were removed
in Mod k. Sequential GeoConvert runs inside AeroScenery.

---

## Installation

**Default (portable):** download the Mod k ZIP from GitHub Releases, extract
the whole folder anywhere, start `AeroScenery.exe`. Then fill in Settings tabs
**AeroScenery** and **GeoConvert** (SDK **folder**, not the GeoConvert EXE).

**Alternative:** install Nick Hod’s official [AeroScenery 1.0.1 MSI](https://github.com/nickhod/aeroscenery/releases/tag/1.0.1)
(there is no official 1.1.3 installer), then copy the Mod k ZIP over
`Program Files (x86)\AeroScenery\` and overwrite.

Keep every file next to the EXE. NuGet does not restore all runtime libraries.

➡️ [Installation](docs/installation.md) · [Get started](docs/getstarted.md)

---

## GeoConvert (Aerofly FS SDK)

AeroScenery requires `GeoConvert.exe` from the Aerofly FS 2 SDK.

GeoConvert is **not included**. It is no longer officially supported by IPACS,
but is still available on Aerofly-Sim.de:
[Aerofly FS 2 Software Development Kit (SDK)](https://www.aerofly-sim.de/aerofly_fs_2_sdk).

➡️ See: [Installation](docs/installation.md)

---

## Releases

Binary releases (portable ZIP, no installer) are provided via **GitHub Releases**.

Published community line so far:

- **v1.1.3-mod.j**
- **v1.1.3-mod.k** (portable ZIP, no installer)

A later **2.0.0** line is planned with a new MSI.

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

- Mod k is portable. Runtime libraries must sit next to `AeroScenery.exe`.
- Community project, provided "as is".

---

## License

This project is licensed under the **GNU General Public License v3.0 (GPL-3.0)**,
in accordance with the original AeroScenery project.

---

## Credits

- Original AeroScenery by **Nick Hod**
- Community maintenance and extensions by **@chrispriv**
- Aerofly FS4 Bridge (`AeroflyBridge.dll`) by **[Juan Luis Gabriel](https://github.com/jlgabriel/Aerofly-FS4-Bridge)**
