# Frequently Asked Questions (FAQ)

> ⚠️ **Work in progress**  
> This FAQ is based on the current project state and will be expanded
> as new questions arise.

## Where can I download GeoConvert.exe?

GeoConvert is part of the Aerofly FS 2 Software Development Kit (SDK).

https://www.aerofly-sim.de/aerofly_fs_2_sdk

---

## Is this an official AeroScenery release?

No. This is an unofficial, community-maintained fork of the original
AeroScenery project.

---

## Does this work with Aerofly FS 4?

Yes, basic photo scenery generation works with Aerofly FS 4.
Some features may behave differently depending on simulator version.

---

## Why is there no installer?

The original MSI installer is deprecated.
The Community Mod is distributed as a ZIP package.

---

### GeoConvert does not start / hangs at startup
- Ensure the Aerofly FS SDK is installed correctly
- Verify the GeoConvert path in AeroScenery settings
- Check that required image tiles are available
- Look for "Running GeoConvert" messages in the log file

---

### Some map tiles are missing or appear grey
This can happen due to temporary server limitations.

Recommended solutions:
- Increase "Waiting between downloads" and "Randomize +/-" values
- Re-run the stitching process (only missing tiles will be downloaded)
- Use the provided PowerShell scripts for manual downloads

---

### ArcGIS map source is unreliable
ArcGIS servers may throttle requests.

Recommendations:
- Increase delay settings (e.g. 15–30 ms)
- Use PowerShell scripts for the most reliable results

---

### PowerShell scripts cannot be executed
On some systems, Windows security settings prevent PowerShell scripts from
running. This may affect AeroScenery helper scripts used during downloads.

If you see an error like *"Running scripts is disabled on this system"*:
1. Open **Windows PowerShell** **as Administrator**
2. Check the current execution policy:
   ```powershell
   Get-ExecutionPolicySet-ExecutionPolicy
3. Set the execution policy for the current user:
   ```powershell
   Set-ExecutionPolicyExecutionPolicy RemoteSigned -Scope CurrentUser
4. Confirm the change when prompted.

This setting allows locally created scripts to run while keeping downloaded
scripts restricted. Administrator rights may be required.

---

### Moving map does not work
- Enable *Data Streaming* in Aerofly FS settings
- Ensure AeroScenery is allowed through the firewall
- Verify correct IP address detection
- Aerofly FS must be running while using the moving map

---

### Is this an official AeroScenery release?
No.  
This is an **unofficial community-maintained fork** of AeroScenery.

---

### Why is there no installer?
The legacy MSI installer is deprecated.
The Community Mod is distributed as a ZIP package.
