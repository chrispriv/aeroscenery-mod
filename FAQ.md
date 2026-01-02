## FAQ

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
