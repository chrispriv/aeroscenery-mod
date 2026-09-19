# Get Started – First Photo Scenery

Create a small test scenery for **Aerofly FS4** with **Community Mod k**.

Install first: [Installation Guide](installation.md) (portable ZIP by default).
Complete Settings tabs **AeroScenery** and **GeoConvert** before you continue.

---

## Grid size and zoom (read this first)

Users often pick tiles that are too small, or Size 9 at much too high zoom **20**
(~0.149 m/pixel). That is far too heavy for a first try.

| Use | Grid Square Size (toolbar) | Image Detail (zoom) | Approx. resolution |
|---|---|---|---|
| First test | Size **11** | 15 or 16 | ~4.8 m or ~2.4 m |
| Normal base scenery | Size **9** or **10** (default 9) | 15 or 16 | ~4.8 m or ~2.4 m |
| Airports, towns (smaller area) | Smaller than the base, or a subset | 17 | ~1.2 m |
| Airport grounds, FS4 **PC** only | Small area | 18 | ~0.6 m |

Zoom **18** is not useful for the **mobile** (FSG Android) path.

On the main window: set **Grid Square Selection Size** in the toolbar first,
then **Image Source**. The `(?)` next to Image Source explains the same order.

Do not start **nine Size 9** squares on a first run. One test square is enough.

---

## Step 1 – Location and size

- Pan the map, pick land
- Toolbar: Size 11 for a test, later Size 9 or 10
- Click the map to select the square(s), one may be enough for a first try

---

## Step 2 – Image source and zoom

- Choose an image source (Google and similar sources have their own limits)
- Set **Image Detail (Zoom Level)** — 15 or 16 for the first scenery
- **Generate AFS Levels** → **Choose For Me** (see the `(?)` there)

---

## Step 3 – Actions

- **Run Default Actions** runs the usual chain
- Or **Choose Actions To Run** for single steps (for example **Run GeoConvert**
  again after you edited stitched images)

Set **AFS Working Scenery Folder** in Settings first. Enable **Install Scenery (waiting for GeoConvert)** so every
selected square is installed for FS4 PC when GeoConvert finishes. 

**Install Tile** on the map toolbar installs **only** the square that is
selected. Prefer these functions over copying files by hand.

---

## Step 4 – Run and wait

Click **Start**. Downloads can take time. GeoConvert is heavier than the
image download, takes much longer and is CPU and memory intensive. 
You can continue to work in AeroScenery while GeoConvert runs on powerful hardware.

On a normal PC, turn on sequential GeoConvert in Settings if you process
more than one square. Details: [Installation](installation.md).

---

## Step 5 – Check in Aerofly

- Start Aerofly FS4
- Confirm the scenery name/folder you set in Settings
- If nothing shows: you probably copied files manually — use **Install Tile**
  or **Install Scenery** and check the AFS user folder path

---

## Step 6 – Optional: convert for FSG Android (mobile)

Always build the scenery **for Aerofly FS4 on the PC first** (the steps above).
Mobile is an **extra** conversion of that desktop result, not a separate
workflow.

### If you have no Aerofly FS4 on this PC

You can still run the full AeroScenery chain. Create an empty **dummy AFS user
folder** anywhere on disk (for example `D:\AFS4UserDummy\`) and enter it under
Settings → **AeroScenery** as the AFS user folder. Set **AFS Working Scenery
Folder** as usual, then use **Install Tile** or **Install Scenery** so the
desktop `.ttc` files land in that dummy folder. You do not need a licensed
Aerofly install for that.

### Extra conversion for Android

1. Install **Aerofly FS2 Content Converter** from the FS2 SDK tools on the PC
   (same SDK family as GeoConvert; it is a separate app).
2. In Settings → **GeoConvert**, enable conversion for mobile so **Generate
   AID / TMC Files** also creates a mobile working folder from the GeoConvert
   raw images.
3. After GeoConvert has finished, open that folder, right-click the generated
   **TMC** file and choose **Run with Aerofly FS2 Content Converter**.
4. Use a sensible zoom for mobile (15–17). Zoom **18** is for FS4 PC hotspots
   only and is not useful on FSG Android.

The `(?)` next to **Conversion for mobile** in Settings repeats this sequence.

---

## Next

- Raise zoom only on airports and cities (17, or up to 18 on FS4 PC)
- Optional: OSM / elevation downloads, water masking (Settings + Actions)
- [Feature Overview](featureoverview.md)
- [FAQ](faq.md)
