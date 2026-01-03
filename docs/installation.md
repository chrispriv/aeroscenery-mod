# Installation Guide

This document describes the installation and initial configuration of
**AeroScenery Community Mod**.

---

> ⚠️ **Work in progress**  
> This guide will be extended with screenshots and additional troubleshooting
> information.

---

## Base Installation

1. Install the original AeroScenery base version using the MSI installer.
2. Download the AeroScenery Community Mod ZIP from GitHub Releases.
3. Extract and copy all files into the AeroScenery installation directory.
4. Overwrite existing files if prompted.

---

## GeoConvert (Aerofly FS SDK)

AeroScenery requires `GeoConvert.exe` from the Aerofly FS 2 SDK.

- Download the SDK from:  
  https://www.aerofly-sim.de/aerofly_fs_2_sdk
- Extract `GeoConvert.exe` to a suitable location

---

## AeroScenery Settings

Open **Settings** in AeroScenery and configure the following paths:

### Working Directory
- Temporary files and processing data

### Database Directory
- Internal AeroScenery database files

### Installation Directory
- Output directory for generated scenery

### Aerofly FS SDK Path
- Path to the directory containing `GeoConvert.exe`
- This setting is **mandatory**

*(Screenshot placeholder)*

---

## Verify the Installation

- Start AeroScenery
- Check that GeoConvert is detected
- Run a small test scenery

---

## Common Issues

- GeoConvert not found
- Permission issues
- Invalid directory paths
