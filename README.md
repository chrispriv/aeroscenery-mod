# AeroScenery Community Mod (Mod j)

This repository is an **unofficial, community-maintained fork** of
[AeroScenery](https://github.com/nickhod/aeroscenery), originally developed by Nick Hod.

It is based on **AeroScenery 1.1.3-beta** and provides ongoing maintenance,
bug fixes, and a wide range of functional extensions for the community.

---

## Overview

- Base version: **AeroScenery 1.1.3-beta**
- .NET Framework **4.8**
- Active community maintenance starting with **Mod j**
- Original author currently inactive

This fork aims to keep AeroScenery usable on modern systems and extend its
functionality where reasonable, while staying compatible with the original
application.

---

## Feature Overview

| Feature | Original 1.1.3-beta | Community Mod j |
|------|---------------------|-----------------|
| Photo scenery creation | ✅ | ✅ |
| Multiple map sources | ✅ | ✅ (extended & fixed) |
| Community maintenance | ❌ | ✅ |
| Simultaneous downloads | Up to 4 | Up to 8 |
| Tile & location search | Basic | Enhanced (OSM geocoding) |
| Improved workflows & UI tweaks | ❌ | ✅ |
| Bug fixes & stability improvements | ❌ | ✅ |
| Extended configuration options | ❌ | ✅ |
| Consolidated binaries & dependencies | ❌ | ✅ |
| Elevation data download | ❌ | ✅ |
| OSM data download | ❌ | ✅ |
| TreesDetection integration | ❌ | ✅ |
| PowerShell scripts support| ❌ | ✅ |
| Moving map (UDP) with flight tracing | ❌ | ✅ |

---

## Projects in this Repository

- **AeroScenery**  
  Main application (extended and actively maintained)

- **GeoConvertWrapper**  
  Legacy wrapper code (unchanged from the original project)

- **AeroSceneryInstaller**  
  Deprecated MSI installer project (no longer functional, kept for reference)

---

## Installation (Short Version)

1. Install the original **AeroScenery 1.0.1** using the official [MSI installer Release 1.0.1](https://github.com/nickhod/aeroscenery/releases/tag/1.0.1)
   (base installation required).
2. Download the desired **Community Mod** release (ZIP) from GitHub Releases (AeroScenery 1.1.3-beta is already included and therefore does not need to be installed first).
3. Extract the ZIP archive and copy all files into your existing User ..\Program Files (x86)\AeroScenery\
   installation directory.
4. Overwrite existing files when prompted (admin rights needed).

➡️ For a detailed, step-by-step [installation](docs/installation.md) and [get started](docs/getting_started.md) guide (including screenshots),
see the documentation links below.

---

## GeoConvert (Aerofly FS SDK)

AeroScenery requires `GeoConvert.exe`, which is part of the
Aerofly FS 2 Software Development Kit (SDK).

GeoConvert is **not included** in this project and must be obtained separately.
It is no longer officially supported by IPACS, but still available via the
[Aerofly FS 2 Software Development Kit (SDK)](https://www.aerofly-sim.de/aerofly_fs_2_sdk).

➡️ See: [Installation](docs/installation.md) 

---

## Releases

Binary releases (ZIP packages) are provided via **GitHub Releases**.

The first public community-maintained release is:
- **v1.1.3-mod.j**

Each release includes the full application package (EXE and required DLLs).
No installer is provided.

---

## Documentation

- 📘 **Full documentation website**  
  https://<username>.github.io/<repository-name>/

- ▶️ [Detailed Installation Guide](docs/installation.md)
- 🚀 [Get Started Guide](docs/getstarted.md)
- ⭐ [Feature Overview (detailed)](docs/featureoverview.md)
- ❓ [FAQ](docs/faq.md)
- 📝 [Changelog](CHANGELOG.md)

---

## Notes

- The MSI installer project is deprecated and kept for reference only.
- Some legacy wrapper and helper code originates from the original AeroScenery project.
- This is a community-maintained project provided "as is".

---

## License

This project is licensed under the **GNU General Public License v3.0 (GPL-3.0)**,
in accordance with the original AeroScenery project.

---

## Credits

- Original AeroScenery by **Nick Hod**
- Community maintenance and extensions by **@chrispriv**
