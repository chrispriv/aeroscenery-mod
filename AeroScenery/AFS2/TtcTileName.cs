using System;
using System.Globalization;

namespace AeroScenery.AFS2
{
    /// <summary>
    /// Converts between a .ttc filename and the tile index it names.
    ///
    /// A name carries a position in a fixed 2^16 x 2^16 world grid, not the tile index, so the
    /// multiplier shrinks as the level deepens: 128 at level 9, 16 at level 12, 4 at level 14.
    /// Verified against six levels of real GeoConvert output. Treating 128 as a constant produces
    /// correct names at level 9 only - that bug survived a level-9-only fixture set and named every
    /// deeper tile wrongly, so the levels in the validation set are not decoration.
    ///
    /// AFS2Grid.GetGridSquareAtLatLon builds the same name inline from a lat/lon; the multiplier
    /// there (65536 / 2^level) is this same value. This class exists so the converter can name a
    /// tile it already has indices for, without going back through a lat/lon round trip.
    /// </summary>
    public static class TtcTileName
    {
        private const int WorldGridBits = 16;

        public static int TileStep(int level)
        {
            return 1 << (WorldGridBits - level);
        }

        public static string ForTile(int level, int tileX, int tileY, bool mask = false)
        {
            int s = TileStep(level);
            return String.Format("map_{0:d2}_{1:x4}_{2:x4}{3}.ttc",
                level, tileX * s, tileY * s, mask ? "_mask" : "");
        }

        /// <summary>Inverse of ForTile. Returns false rather than throwing on an unrelated name.</summary>
        public static bool TryParse(string name, out int level, out int tileX, out int tileY, out bool mask)
        {
            level = tileX = tileY = 0;
            mask = false;

            int dot = name.IndexOf('.');
            string stem = dot >= 0 ? name.Substring(0, dot) : name;

            mask = stem.EndsWith("_mask", StringComparison.OrdinalIgnoreCase);
            if (mask)
            {
                stem = stem.Substring(0, stem.Length - 5);
            }

            var parts = stem.Split('_');
            if (parts.Length != 4 || !parts[0].Equals("map", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int hx, hy;
            if (!Int32.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out level)
                || !Int32.TryParse(parts[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hx)
                || !Int32.TryParse(parts[3], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hy)
                || level < 0 || level > WorldGridBits)
            {
                return false;
            }

            int s = TileStep(level);
            tileX = hx / s;
            tileY = hy / s;
            return true;
        }
    }
}
