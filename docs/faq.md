# Frequently Asked Questions (FAQ)

## Is this an official AeroScenery release?

No. This is an unofficial, community-maintained fork of Nick Hod’s AeroScenery.

---

## Does this work with Aerofly FS 4?

Yes. Photo scenery, **Install Scenery**, and the moving map (UDP and FS4
shared memory) are used with FS4. Zoom 18 is for FS4 **PC** hotspots only,
not for FSG Android.

---

## How do I install Mod k? Is there an MSI?

**Default:** unzip the GitHub Release, put the folder anywhere, start
`AeroScenery.exe`.

**Alternative:** install Nick Hod’s official **AeroScenery 1.0.1 MSI**, then
copy the Mod k ZIP over that folder. There is no official 1.1.3 installer.

A new community MSI is planned for a later **2.0.0** line, not for Mod k.

Full steps: [Installation Guide](installation.md).

---

## Where do I download GeoConvert?

It is part of the Aerofly FS 2 SDK tools (sold/offered as **separate apps**,
not one big SDK installer):

https://www.aerofly-sim.de/aerofly_fs_2_sdk

Unzip GeoConvert onto a disk. In Settings → GeoConvert, set the **SDK root**
that contains the `aerofly_fs_2_geoconvert\` folder — **not** the path to the
`.exe`.

---

## GeoConvert is not found / I “installed” it and Settings still fails

Typical mistakes:

- Searching for a Windows installer instead of unzipping the tool
- Forgetting Settings → GeoConvert after unpacking
- Pasting the path to `aerofly_fs_2_geoconvert.exe` instead of the parent SDK
  folder that contains `aerofly_fs_2_geoconvert\`

---

## I filled some Settings later and nothing works

Complete the first two tabs (**AeroScenery** and **GeoConvert**) right after
installation, including **AFS Working Scenery Folder** if you use
**Install Scenery**.

---

## Which tile size and zoom should I use?

- First try: Size **11**, zoom **15** or **16**
- Normal scenery: Size **9** or **10**, zoom **15** (~4.8 m) or **16** (~2.4 m)
- Airports/towns: zoom **17** (~1.2 m) on a smaller area
- Airport grounds on FS4 PC: zoom **18** (~0.6 m)
- Do not start with Size 9 at zoom **20** (~0.149 m)
- Zoom 18 is not for mobile/Android

---

## How do I install scenery into Aerofly? I copied files and they do not show

Use **Install Tile** (one square) or **Install Scenery (waiting for GeoConvert)**
(all selected squares). Set the working scenery folder in Settings.
Do not copy `.ttc` files by hand.

---

## The PC locks up when I run GeoConvert on many squares

Mod j started several GeoConvert processes in parallel. That can fill CPU and
RAM.

In Mod k enable **Run GeoConvert sequentially for multiple squares**
(Settings → GeoConvert). The same sequential behaviour applies when
**Install Scenery (waiting for GeoConvert)** is selected.

Do not begin with nine Size 9 squares. A powerful PC may still run GeoConvert
in parallel and download or stitch more tiles at the same time.

---

## GeoConvert hangs or never seems to finish

- SDK path is the folder containing `aerofly_fs_2_geoconvert\`, not the EXE
- Stitch and AID/TMC steps have already run
- Watch the log for “Running GeoConvert”
- One square first; sequential mode if you run several

---

## Some map tiles are missing or grey

- Increase “Waiting between downloads” and “Randomize +/-”
- Use **Fix Missing Tiles** / re-run download (only missing or empty tiles)
- PowerShell helper scripts in the working folder, if you use them

---

## PowerShell scripts will not run

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

## The moving map does not move

- Aerofly: **Settings → Miscellaneous → Broadcast flight info to IP address = on**
- Port **49002**. Click the moving-map `(?)` to see the detected IP
- Allow AeroScenery in the firewall / antivirus
- FS4 shared memory: copy `aeroflybridge.dll` from this package into the
  Aerofly FS4 `external_dll` folder
- Aerofly must be running

---

## Where did Show Airports go?

**Show Airports** is hidden in Mod k because **fscloudport.com** is no longer
online. The old download and map-marker code is still in the project for a
later source (for example OurAirports or a local ICAO scan).

---

## ArcGIS (and some other sources) miss tiles

Servers throttle. Increase delays (for example 15–30 ms) and use the missing-tile
repair options.
