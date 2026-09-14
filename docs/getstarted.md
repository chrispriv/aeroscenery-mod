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

## Next

- Raise zoom only on airports and cities (17, or up to 18 on FS4 PC)
- Optional: OSM / elevation downloads, water masking (Settings + Actions)
- [Feature Overview](featureoverview.md)
- [FAQ](faq.md)
