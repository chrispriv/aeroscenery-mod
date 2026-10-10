# Frequently Asked Questions (FAQ)

## Is this an official AeroScenery release?

No. This is an unofficial, community-maintained fork of Nick Hod’s AeroScenery.

---

## Is this for Aerofly FS 4?

Yes. The original AeroScenery targeted Aerofly FS 2. Community **Mod l** is
built for **Aerofly FS 4** (folder labels, install path, masks). FS 2 is no
longer a supported workflow, but still works: FS4 tiles remain backward
compatible with former FS 2.

The other direction is the problem. Older FS 2 photosceneries often omit a
compile level (typically **Generate AFS Levels** 10). With global data
streaming in FS 4 and FSG, that gap shows as texture switches when you fly
closer. Those old sceneries work well if every compile level is present with
no gaps.

---

## Why must Generate AFS Levels have no gaps?

The **built-in converter** needs a **continuous** set of compile levels
(unlike some earlier mods, where a skipped level was more easily tolerated).
Skipping a level also makes poor scenery: FS 4 / FSG streaming then pops
textures as you approach.

Use **Choose For Me**, or tick every level from the coarsest through the
finest you need (for example 9–12) with no holes.

You can still **compile a single level** later if that level belongs to a
complete set: re-run from **Generate AID / TMC** with only that level
checked. Do not ship scenery that jumps 9 → 11 and skips 10.

---

## Does it also work for FSG on mobile?

**Android:** yes. The built-in converter writes ETC2 tiles; copy them to the
FSG working folder and pack as `.tme`. See [Get Started](getstarted.md)
Step 6. Use zoom 15–17; zoom 18 is for FS4 PC hotspots only.

**iOS:** not supported. FSG on iOS does not give access to the scenery data
folder, so a `.tme` cannot be installed in a supported way.

---

## How do I install Mod l? Is there an MSI?

**Default:** unzip the GitHub Release, put the folder anywhere, start
`AeroScenery.exe`.

**Alternative:** install Nick Hod’s official **AeroScenery 1.0.1 MSI**, then
copy the Mod l ZIP over that folder. There is no official 1.1.3 installer.

Full steps: [Installation Guide](installation.md).

---

## Can I use this together with Juan’s AeroScenery FS4?

Yes, as two separate programs. His [AeroScenery FS4](https://github.com/jlgabriel/aeroscenery-fs4)
is a lean FS4-PC-only photoscenery app. It writes `settings2.xml`, so it does
not overwrite this Mod’s `settings.xml` (API keys stay intact). Grid squares
in the AeroScenery SQLite database are **not** kept in sync between the two.

Mod l keeps FSG Android (ETC2), Carto/OSM water masking and `_mask.ttc` tiles,
optional RAW PNG, compressed TTC, moving map, OSM and elevation. His fork
uses a coastline cut instead of water masking; that cut is not used here
because masking works better and FSG Android needs `_mask` tiles.

---

## Do I still need GeoConvert from the Aerofly SDK?

Not for the default workflow. **Run Built-in converter** writes FS4 and/or
FSG tiles inside AeroScenery.

Use **Aerofly FS2 SDK GeoConvert** in Settings only if you prefer the original
EXE. Set the path to **`aerofly_fs_2_geoconvert.exe`** (a folder is accepted
and stored as the EXE). Download:

https://www.aerofly-sim.de/aerofly_fs_2_sdk

Elevation mesh conversion (`mesh_conv.bat`) still uses that EXE when you
download elevation data.

---

## GeoConvert is not found / I “installed” it and Settings still fails

Typical mistakes:

- Searching for a Windows installer instead of unzipping the tool
- Forgetting Settings → GeoConvert after unpacking
- Selecting **SDK GeoConvert** without a valid EXE path

---

## I filled some Settings later and nothing works

Complete the first two tabs (**AeroScenery** and **GeoConvert**) right after
installation, including **FS4 Install Folder**, **Working Scenery Name** (and
**FSG Scenery Working Folder** if you convert for Android).

---

## Which tile size and zoom should I use?

- First try: Size **11**, zoom **15** or **16**
- Normal scenery: Size **9** or **10**, zoom **15** (~4.8 m) or **16** (~2.4 m)
- Airports/towns: zoom **17** (~1.2 m) on a smaller area
- Airport grounds on FS4 PC: zoom **18** (~0.6 m)
- Do not start with Size 9 at zoom **20** (~0.149 m)
- Zoom 18 is not for mobile/Android

---

## Photoscenery sits too far south (N/S shift)

This comes from geo-coordinate conversion and earth curvature. **Grid Size 13
and 14** tiles stay in the right place. The offset shows mainly on **Size 9
and 10** (and other large squares).

Lowering **Max tiles per stitched image** from the default **66** may reduce
the error, but you then have many more stitched images to edit, and **water
masking** no longer works (distance to the coast cannot be measured).

Better: in Settings, enable **Allows for manual correction of the north-south
offset**. On the main window, **Shift Correction** then applies an N/S
offset. The value is scaled to the selected grid size and is referenced to
**Size 9** scenery, so Size 13/14 get no shift. Even on large tiles the
**northern edge stays correct**; the southward error grows toward the south.

---

## How do I install scenery into Aerofly? I copied files and they do not show

Use **Install FS4 Tile** (one square) or **Install Scenery for FS4** (all
selected squares). For Android, use **Copy Scenery to FSG Scenery Working
Folder**, then zip the `_images` folder to `.tme`.
Do not copy `.ttc` files by hand.

---

## The PC locks up when I convert many squares

The **built-in converter** always runs squares sequentially.

SDK GeoConvert can start several processes in parallel and fill CPU and RAM.
Enable **Run GeoConvert sequentially for multiple squares** (Settings →
GeoConvert) when using the SDK. The same sequential behaviour applies when
**Install Scenery for FS4** is selected with the SDK.

Do not begin with nine Size 9 squares.

---

## How do I convert existing tiles for FSG Android later?

If the **stitched images** are already there, you do not need to download
again. Select the same squares, set the dropdown to **Aerofly FSG (Android)**
(or **FS4 and FSG**). Under **Choose Actions To Run**, start from
**Generate AID / TMC Files** through **Run Built-in converter** and
**Copy Scenery to FSG Scenery Working Folder**. Then pack as `.tme`
(see [Get Started](getstarted.md) Step 6).

---

## What does Tile Info / right-click on the map do?

- **Tile Info** (map toolbar) — stored Generate AID/TMC settings, OSM and
  elevation flags for the **selected** square
- **Right-click** a tile — opens that square’s working folder (name is copied
  to the clipboard)

---

## The moving map does not move / which mode should I use?

The moving map works **only with Aerofly FS4 on the PC**. FSG Android has no
*Broadcast flight info to IP address* setting. Shared memory works only on
the **same computer** that is running FS4.

**UDP**

- FS4: **Settings → Miscellaneous → Broadcast flight info to IP address = on**
- Broadcast IP: your LAN broadcast (last octet **255**), for example
  `192.168.1.255`
- **Broadcast IP port:** **49002**
- Click the moving-map `(?)` in AeroScenery for the detected address
- Allow AeroScenery in the firewall / antivirus
- FS4 must be in a flight

**Shared memory (DLL)**

- Copy `AeroflyBridge.dll` to  
  `%USERPROFILE%\Documents\Aerofly FS 4\external_dll\`  
  (from Mod l `Resources\external_dll\` or from
  [Aerofly-FS4-Bridge](https://github.com/jlgabriel/Aerofly-FS4-Bridge))
- Start FS4, load a flight, then choose **DLL (Shared Memory)** in AeroScenery
- Credits: [jlgabriel/Aerofly-FS4-Bridge](https://github.com/jlgabriel/Aerofly-FS4-Bridge)
  (Quick install in that README)

Full steps: [Get Started — Moving map](getstarted.md).

---

## Map tiles are missing, white or empty (ArcGIS, Google, …)

Servers throttle. This shows most often with **ArcGIS**, and sometimes with
**Google**. A missing downloaded tile usually appears **white** (empty), not
grey.

- Increase **Wait Between Downloads** and **randomize by + or −** (for example
  15–30 ms wait)
- Enable **Fix Missing Tiles** / re-run download (only missing or empty tiles)

---

## PowerShell scripts will not run (optional fix)

If Windows reports that running scripts is disabled:

1. Open **Windows PowerShell** as Administrator.
2. Check the policy:

   ```powershell
   Get-ExecutionPolicy
   ```

3. Allow local scripts for your user:

   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```

4. Confirm when prompted.

---

## How do I show airports on the map?

In Settings, download the current **OurAirports** `airports.csv` (optional:
`runways.csv`) and import them. **Show Airports** then appears on the map
toolbar.
