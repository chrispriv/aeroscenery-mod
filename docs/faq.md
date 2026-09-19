# Frequently Asked Questions (FAQ)

## Is this an official AeroScenery release?

No. This is an unofficial, community-maintained fork of Nick Hod’s AeroScenery.

---

## Does this work with Aerofly FS 4?

Yes. Photo scenery, **Install Scenery**, and the moving map (UDP and FS4
shared memory) are used with FS4. Zoom 18 should be used for FS4 **PC** hotspots only,
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

With that option, the GeoConvert window is closed automatically after about
**60 seconds** of idle work (stable memory and a CPU drop), unless you close
it earlier. The delay avoids stopping GeoConvert too soon. If automatic
completion detection fails in a particular case, close the GeoConvert console
manually.

Do not begin with nine Size 9 squares. A powerful PC may still run GeoConvert
in parallel and download or stitch more tiles at the same time.

---

## GeoConvert hangs or never seems to finish

- SDK path is the folder containing `aerofly_fs_2_geoconvert\`, not the EXE
- Stitch and AID/TMC steps have already run
- Watch the log for “Running GeoConvert”
- One square first; sequential mode if you run several
- If sequential install never continues: wait ~60 seconds, or close the
  GeoConvert console yourself

---

## How do I convert existing tiles for FSG Android later?

Turn on **Conversion for mobile** in Settings → **GeoConvert**. Select the
tiles with the **same Image Source** and **Image Detail (Zoom Level)** as
the original job. Under **Choose Actions To Run**, run **only**
**Generate AID / TMC Files**. Then right-click `content_converter_config_mobile.tmc`
and choose **Run with Aerofly FS 2 Content Converter**. Replace `.ttc` files
in a **copy** of the scenery folder and pack as `.tme`
(`dlc_<scenery-name>\scenery\images\map_09_...`). See [Get Started](getstarted.md)
Step 6.

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
  (from Mod k `Resources\external_dll\` or from
  [Aerofly-FS4-Bridge](https://github.com/jlgabriel/Aerofly-FS4-Bridge))
- Start FS4, load a flight, then choose **DLL (Shared Memory)** in AeroScenery
- Credits: [jlgabriel/Aerofly-FS4-Bridge](https://github.com/jlgabriel/Aerofly-FS4-Bridge)
  (Quick install in that README)

Full steps: [Get Started — Moving map](getstarted.md).

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

## Where did Show Airports go?

**Show Airports** is hidden in Mod k because **fscloudport.com** is no longer
online. The old download and map-marker code is still in the project for a
later source (for example OurAirports or a local ICAO scan).

---

## ArcGIS (and some other sources) miss tiles

Servers throttle. Increase delays (for example 15–30 ms) and use the missing-tile
repair options.
