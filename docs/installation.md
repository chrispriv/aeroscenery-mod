# Installation Guide

This document describes the installation and initial configuration of
**AeroScenery Community Mod k** (portable TEST snapshot).

---

> ⚠️ **Work in progress**  
> Mod k is functionally stable. Screenshots and a later MSI installer
> (planned for 2.0.0) are not part of this snapshot.

---

## Portable installation (Mod k)

The original AeroScenery **1.0.1 MSI is not required**.

1. Download the Community Mod ZIP from GitHub Releases, or build the
   `AeroScenery` project in Visual Studio 2022 (.NET Framework 4.8).
2. Extract (or copy) the output folder anywhere you can write to.
3. Keep all files next to `AeroScenery.exe`. Several libraries live under
   `bin\Debug` (or your Release output) and are **not** fully restored by
   NuGet alone.
4. Start `AeroScenery.exe`.

Do not mix this tree with an old `GeoConvertWrapper.exe` install. Sequential
GeoConvert is built into the main application.

---

## GeoConvert (Aerofly FS SDK)

AeroScenery requires `GeoConvert.exe` from the Aerofly FS 2 SDK.

- Download the SDK from:  
  https://www.aerofly-sim.de/aerofly_fs_2_sdk
- Point Settings at the SDK root that contains `aerofly_fs_2_geoconvert`

---

## AeroScenery Settings

Open **Settings** and configure:

### Working Directory
- Temporary files and processing data

### Database Directory
- Internal AeroScenery database files

### Aerofly user / scenery folder
- Target for **Install Scenery** / **Install Tile**

### Aerofly FS SDK Path
- Directory that contains GeoConvert  
- This setting is **mandatory**

---

## Verify the Installation

- Start AeroScenery
- Confirm GeoConvert is found
- Run a small test tile, then **Install Tile** or enable
  **Install Scenery (waiting for GeoConvert)** under Actions

---

## Common Issues

- GeoConvert not found — check the SDK path
- App starts but libraries fail — copy the full `bin` output, not the EXE alone
- Permission issues if you still place files under Program Files
