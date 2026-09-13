# AeroScenery Community Mod (Mod k TEST)

This repository is an **unofficial, community-maintained fork** of
[AeroScenery](https://github.com/nickhod/aeroscenery), originally developed by Nick Hod.

It is based on **AeroScenery 1.1.3-beta** and continues the community line
through Mods a–j. The current working line is **Mod k** (TEST): portable,
no MSI installer, and sequential GeoConvert built into the main app.

---

## Overview

- Base version: **AeroScenery 1.1.3-beta**
- .NET Framework **4.8**
- Current line: **1.1.3 Mod k (TEST)**
- Original author currently inactive

Mod k can run as a **portable** application (extract and run). The old
AeroScenery 1.0.1 MSI is no longer required. A new community MSI is planned
for a later **2.0.0** release and is **not** part of this TEST snapshot.

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

## Installation (Portable ZIP)

1. Download the **Community Mod k** ZIP from GitHub Releases (when published)
   or build `AeroScenery` from this repository in Visual Studio.
2. Extract the folder anywhere (no Program Files install required).
3. Keep the libraries next to `AeroScenery.exe` (NuGet does not restore every
   dependency used at runtime).
4. Start `AeroScenery.exe` and set the Aerofly SDK / GeoConvert path under
   **Settings**.

The original **AeroScenery 1.0.1 MSI** is optional history, not a requirement
for Mod k.

➡️ Step-by-step notes: [installation](docs/installation.md) and
[get started](docs/getstarted.md).

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
- **v1.1.3-mod.k** (TEST — this branch; release ZIP after remaining UI polish)

A later **2.0.0** line is planned with a new MSI. That work is out of scope
until Mod k is finished.

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

- This is a TEST snapshot of Mod k: functionally stable, with remaining
  tooltip and Install Scenery polish still planned.
- `bin\Debug` is gitignored. Local builds still need those libraries beside
  the EXE because NuGet does not restore them all.
- Community project, provided "as is".

---

## License

This project is licensed under the **GNU General Public License v3.0 (GPL-3.0)**,
in accordance with the original AeroScenery project.

---

## Credits

- Original AeroScenery by **Nick Hod**
- Community maintenance and extensions by **@chrispriv**
