using System;

namespace AeroScenery.AFS2
{
    /// <summary>
    /// The Aerofly world grid as plain functions, and their inverses.
    ///
    /// Same mapping AFS2Grid.GetGridSquareAtLatLon implements, same constant, credited to the same
    /// Aerofly forum thread. It lives here as well because AFS2Grid works in GMap's PointLatLng and
    /// answers "which square is this coordinate in, and what are its corners"; the converter needs
    /// the other direction - "what longitude is the west edge of tile x" - for every row and column
    /// of every tile, with no allocation.
    ///
    /// Do NOT re-derive this from the outside. It is not Mercator, and hours went into fitting
    /// projections to it before anyone noticed the repository already had it. See section 5 of
    /// docs/ttc-format.md.
    ///
    /// y counts NORTHWARD from the south pole, so a southern latitude gives y below half of
    /// 2^level. Latitude must be WGS84 geodetic, not geocentric.
    /// </summary>
    public static class AFS2World
    {
        /// <summary>The constant from AFS2Grid.cs. Keep the two identical.</summary>
        public const double WorldGridConstant = 2.3311223704144;

        public static double GridX(double lonDegrees, int level)
        {
            double x0 = (lonDegrees * Math.PI / 180.0) / Math.PI;
            return Math.Pow(2, level) * (0.5 + 0.5 * x0);
        }

        public static double GridY(double latDegrees, int level)
        {
            double y0 = (latDegrees * Math.PI / 180.0) / Math.PI;
            double y1 = Math.Tan(WorldGridConstant * y0) / WorldGridConstant;
            return Math.Pow(2, level) * (0.5 + 0.5 * y1);
        }

        public static double LonOfGridX(double x, int level)
        {
            return (Math.PI * (2.0 * (x / Math.Pow(2, level)) - 1.0)) * 180.0 / Math.PI;
        }

        public static double LatOfGridY(double y, int level)
        {
            double y1 = 2.0 * (y / Math.Pow(2, level)) - 1.0;
            return (Math.PI * Math.Atan(WorldGridConstant * y1) / WorldGridConstant) * 180.0 / Math.PI;
        }

        /// <summary>
        /// The tiles a lon/lat box needs at one level. x1 and y1 are exclusive.
        /// </summary>
        public static void TileRange(double west, double east, double south, double north, int level,
            out int x0, out int x1, out int y0, out int y1)
        {
            x0 = (int)Math.Floor(GridX(west, level));
            x1 = (int)Math.Ceiling(GridX(east, level));
            y0 = (int)Math.Floor(GridY(south, level));
            y1 = (int)Math.Ceiling(GridY(north, level));

            // A box that lands exactly on a tile edge would otherwise ask for zero tiles
            if (x1 <= x0) x1 = x0 + 1;
            if (y1 <= y0) y1 = y0 + 1;
        }
    }
}
