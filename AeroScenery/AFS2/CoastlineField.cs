using System;
using System.Collections.Generic;

namespace AeroScenery.AFS2
{
    /// <summary>
    /// A hand-drawn coastline rasterised into a signed distance field, so the sampler can ask "is
    /// this point still inside the photoscenery" once per pixel and get the answer in nanoseconds.
    ///
    /// Asking Coastline.DistanceKm per pixel does not close. One grid square is 1024 tiles of
    /// 2048x2048 at level 14, and each of those 4.3 billion points would walk a few hundred
    /// segments. The field is built once per square instead and read back with bilinear
    /// interpolation, which also removes the staircase a nearest-texel lookup would leave along
    /// the cut.
    ///
    /// The value is the signed distance in km to the drawn line, POSITIVE out to sea, and coverage
    /// is simply value &lt;= MarginKm. That single comparison is the whole rule:
    /// landward of the line, or within the margin of it.
    ///
    /// Two things make the build cheap as well as the lookup.
    ///
    /// The SIGN comes from an even-odd ray cast, not from the nearest segment. Cast a ray from the
    /// point in the direction the land lies, count the crossings of the line, and an even count
    /// means land. That is exact for bays, capes and river mouths alike, where a nearest-segment
    /// normal is only usually right, and it costs one walk along the line per scan line rather
    /// than one per texel. The line is extended from each end, across the ray, so a ray that
    /// passes beyond where the user stopped drawing still answers.
    ///
    /// The DISTANCE is clamped, so a scan line only has to consider the segments that come within
    /// the clamp of it and skips the rest outright. Clamping cannot change any answer, because the
    /// clamp sits well beyond the margin and past the margin the only question left is which side.
    ///
    /// WinForms-free, like Coastline, and for the same reason: this runs inside the converter with
    /// no UI anywhere in sight.
    /// </summary>
    public sealed class CoastlineField
    {
        /// <summary>
        /// Default texel size in km. 30 m is about 170 times finer than a 3 NM margin.
        ///
        /// Far finer than it has to be, and deliberately so, because the cost is small: one level-9
        /// square comes out 2213 x 2178 texels, 18.4 MB, built in 1.20 s. What sets the real
        /// requirement is that the cut is a smooth contour whose curvature is at most 1/margin, so
        /// bilinear interpolation puts it wrong by roughly h*h/(8*margin) - centimetres at this
        /// texel, and still under a metre at ten times it. Raise it if a square ever has to be
        /// built in a hurry.
        /// </summary>
        public const double DefaultTexelKm = 0.03;

        /// <summary>
        /// The most texels a field may hold, which is 64 MB of float. One grid square needs about
        /// 7 million at the default texel. The cap only bites if a .tmc ever covers far more
        /// ground than one square, and it coarsens instead of failing, because the accuracy it
        /// gives up is accuracy nothing downstream could use anyway.
        /// </summary>
        private const long MaxTexels = 16L * 1024 * 1024;

        private readonly float[] field;
        private readonly int width;
        private readonly int height;
        private readonly double lonWest;
        private readonly double latSouth;
        private readonly double lonStep;
        private readonly double latStep;
        private readonly double marginKm;
        private readonly double texelKm;

        private CoastlineField(float[] field, int width, int height,
            double lonWest, double latSouth, double lonStep, double latStep,
            double marginKm, double texelKm)
        {
            this.field = field;
            this.width = width;
            this.height = height;
            this.lonWest = lonWest;
            this.latSouth = latSouth;
            this.lonStep = lonStep;
            this.latStep = latStep;
            this.marginKm = marginKm;
            this.texelKm = texelKm;
        }

        /// <summary>How far out to sea the cut sits, copied from the line it was built from.</summary>
        public double MarginKm { get { return marginKm; } }

        /// <summary>What the texels ended up being, which is not the requested size if it was capped.</summary>
        public double TexelKm { get { return texelKm; } }

        public int Width { get { return width; } }

        public int Height { get { return height; } }

        /// <summary>
        /// Rasterises a line over a lon/lat box.
        ///
        /// Pass the box that will actually be SAMPLED rather than the one the .tmc asked for. A
        /// lookup outside the box falls back on the edge texel, which is a sensible thing to do
        /// with a stray pixel and a bad thing to be doing along a whole tile row.
        /// </summary>
        public static CoastlineField Build(Coastline line, double west, double east,
            double south, double north, double texelKm = DefaultTexelKm)
        {
            if (line == null)
            {
                throw new ArgumentNullException("line");
            }
            if (texelKm <= 0.0)
            {
                throw new ArgumentOutOfRangeException("texelKm", "the texel size must be positive");
            }

            List<GeoPoint> pts = line.Points;
            if (pts.Count < 2)
            {
                throw new InvalidOperationException(
                    "a coastline needs at least two points before it can be rasterised");
            }

            if (east < west) { double t = west; west = east; east = t; }
            if (north < south) { double t = south; south = north; north = t; }

            double midLat = 0.5 * (south + north);
            double kmPerDegLon = Coastline.KmPerDegLon(midLat);

            int width, height;
            double lonStep, latStep;
            for (;;)
            {
                lonStep = texelKm / kmPerDegLon;
                latStep = texelKm / Coastline.KmPerDegLat;

                // A texel of slack all round, so a lookup anywhere inside the box has four
                // neighbours and never has to fall back on the edge.
                width = (int)Math.Ceiling((east - west) / lonStep) + 3;
                height = (int)Math.Ceiling((north - south) / latStep) + 3;
                if (width < 2) width = 2;
                if (height < 2) height = 2;

                long want = (long)width * height;
                if (want <= MaxTexels)
                {
                    break;
                }
                texelKm *= Math.Sqrt((double)want / MaxTexels) * 1.01;
            }

            double originLon = west - lonStep;
            double originLat = south - latStep;

            var field = new float[width * height];
            Fill(field, width, height, originLon, originLat, lonStep, latStep, line, pts);

            return new CoastlineField(field, width, height, originLon, originLat,
                lonStep, latStep, line.MarginKm, texelKm);
        }

        // -----------------------------------------------------------------------------------
        // lookup
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// Signed distance in km at one point, positive out to sea. Outside the box it reads the
        /// nearest edge texel rather than throwing.
        /// </summary>
        public double SignedDistanceKm(double lat, double lon)
        {
            double x = (lon - lonWest) / lonStep;
            double y = (lat - latSouth) / latStep;

            if (x < 0.0) x = 0.0; else if (x > width - 1) x = width - 1;
            if (y < 0.0) y = 0.0; else if (y > height - 1) y = height - 1;

            int i = (int)x;
            int j = (int)y;
            if (i > width - 2) i = width - 2;
            if (j > height - 2) j = height - 2;

            double fx = x - i;
            double fy = y - j;

            int row0 = j * width + i;
            int row1 = row0 + width;

            double lower = field[row0] + (field[row0 + 1] - field[row0]) * fx;
            double upper = field[row1] + (field[row1 + 1] - field[row1]) * fx;
            return lower + (upper - lower) * fy;
        }

        /// <summary>Whether photoscenery should still be drawn at this point.</summary>
        public bool IsCovered(double lat, double lon)
        {
            return SignedDistanceKm(lat, lon) <= marginKm;
        }

        /// <summary>
        /// Bounds on what SignedDistanceKm can return anywhere in a lon/lat box.
        ///
        /// This is what makes the per-pixel test affordable: a tile whose maximum is inside the
        /// margin is wholly landward and needs no test at all, and a tile whose minimum is outside
        /// it is wholly at sea and needs no source opened. Only the thin band of tiles the cut
        /// actually crosses pays for itself.
        ///
        /// The bound is exact rather than approximate, because bilinear interpolation never leaves
        /// the range of the four texels it reads, and the range scanned here covers every texel
        /// any lookup inside the box could touch.
        /// </summary>
        public void RangeOver(double west, double east, double south, double north,
            out double minKm, out double maxKm)
        {
            int i0 = Clamp((int)Math.Floor((west - lonWest) / lonStep), 0, width - 1);
            int i1 = Clamp((int)Math.Floor((east - lonWest) / lonStep) + 1, 0, width - 1);
            int j0 = Clamp((int)Math.Floor((south - latSouth) / latStep), 0, height - 1);
            int j1 = Clamp((int)Math.Floor((north - latSouth) / latStep) + 1, 0, height - 1);

            float lo = Single.MaxValue;
            float hi = Single.MinValue;

            for (int j = j0; j <= j1; j++)
            {
                int row = j * width;
                for (int i = i0; i <= i1; i++)
                {
                    float v = field[row + i];
                    if (v < lo) lo = v;
                    if (v > hi) hi = v;
                }
            }

            minKm = lo;
            maxKm = hi;
        }

        private static int Clamp(int v, int lo, int hi)
        {
            return v < lo ? lo : (v > hi ? hi : v);
        }

        // -----------------------------------------------------------------------------------
        // build
        // -----------------------------------------------------------------------------------

        /// <summary>
        /// Walks the field one scan line at a time, ACROSS the ray rather than along the grid.
        ///
        /// Which axis that is follows from where the land is, and it matters for speed as well as
        /// for the ray cast: a coast with land to the east runs roughly north-south, so a scan
        /// line of constant latitude only ever has a handful of segments near it, and the same
        /// holds for an east-west coast scanned by longitude. Scanning the other way round would
        /// put the whole line within reach of every scan line near the coast.
        /// </summary>
        private static void Fill(float[] field, int width, int height,
            double originLon, double originLat, double lonStep, double latStep,
            Coastline line, List<GeoPoint> pts)
        {
            bool alongLon = (line.Land == LandSide.East || line.Land == LandSide.West);
            bool towardsPositive = (line.Land == LandSide.East || line.Land == LandSide.North);

            // Well beyond the margin, so no interpolation anywhere near the cut can see the clamp.
            double clampKm = 2.0 * line.MarginKm + 1.0;
            double clampAcross = alongLon
                ? clampKm / Coastline.KmPerDegLat
                : clampKm / Coastline.KmPerDegLon(originLat + 0.5 * height * latStep);

            int acrossCount = alongLon ? height : width;
            int alongCount = alongLon ? width : height;
            double acrossOrigin = alongLon ? originLat : originLon;
            double acrossStep = alongLon ? latStep : lonStep;
            double alongOrigin = alongLon ? originLon : originLat;
            double alongStep = alongLon ? lonStep : latStep;

            List<GeoPoint> ext = Extend(pts, alongLon,
                acrossOrigin - acrossStep, acrossOrigin + acrossCount * acrossStep);
            var crossings = new double[ext.Count];
            var near = new int[ext.Count];

            for (int u = 0; u < acrossCount; u++)
            {
                double across = acrossOrigin + u * acrossStep;

                int crossCount = Crossings(ext, alongLon, across, crossings);
                int nearCount = Candidates(ext, alongLon, across, clampAcross, near);

                // The scan runs one way along the ray axis, so the count of crossings behind it
                // only ever grows - no search, just a pointer that keeps up.
                int behind = 0;

                for (int v = 0; v < alongCount; v++)
                {
                    double along = alongOrigin + v * alongStep;
                    while (behind < crossCount && crossings[behind] <= along)
                    {
                        behind++;
                    }

                    int ahead = towardsPositive ? crossCount - behind : behind;
                    bool land = (ahead & 1) == 0;

                    var p = alongLon
                        ? new GeoPoint(across, along)
                        : new GeoPoint(along, across);

                    double d = clampKm;
                    for (int k = 0; k < nearCount; k++)
                    {
                        int s = near[k];
                        double dd = Coastline.SegmentDistanceKm(p, ext[s], ext[s + 1]);
                        if (dd < d)
                        {
                            d = dd;
                        }
                    }

                    field[alongLon ? u * width + v : v * width + u] = (float)(land ? -d : d);
                }
            }
        }

        /// <summary>
        /// The line carried straight on from either end until it spans the field, and no further.
        ///
        /// A ray that passes beyond where the user stopped drawing has to cross something, or it
        /// counts zero crossings and calls open ocean land. Carrying the coast straight on is the
        /// obvious reading of a line that simply ran out, and it is also the forgiving one: a line
        /// drawn short then costs a straight cut through the rest of the square, where treating
        /// the ends as final would cut every last kilometre of it away.
        ///
        /// The extension counts for the DISTANCE as well as for the side, and it has to. A side
        /// that flips where the distance does not is a step of twice the clamp between two
        /// neighbouring texels, and interpolating across that step puts a hairline of scenery down
        /// the middle of the sea. Found by a test that compared 20,000 points against the rule,
        /// and it failed 2 of them.
        ///
        /// Stopping at the edge of the field rather than at the pole is what keeps the extension
        /// honest: Coastline measures in local kilometres through the latitude at a segment's
        /// midpoint, and a segment reaching to the pole has no meaningful midpoint. It also means
        /// a line that already spans the field is returned untouched, so in the ordinary case -
        /// one line drawn for the whole package - this changes nothing at all.
        /// </summary>
        private static List<GeoPoint> Extend(List<GeoPoint> pts, bool alongLon,
            double acrossLo, double acrossHi)
        {
            GeoPoint first = pts[0];
            GeoPoint last = pts[pts.Count - 1];

            double firstAcross = alongLon ? first.Lat : first.Lon;
            double lastAcross = alongLon ? last.Lat : last.Lon;

            // Each end carries on away from the other one, so which limit an end runs to depends
            // on which way round the line was drawn.
            bool upwards = firstAcross <= lastAcross;
            double firstTo = upwards ? acrossLo : acrossHi;
            double lastTo = upwards ? acrossHi : acrossLo;

            var ext = new List<GeoPoint>(pts.Count + 2);

            if (upwards ? firstAcross > firstTo : firstAcross < firstTo)
            {
                ext.Add(At(first, alongLon, firstTo));
            }
            ext.AddRange(pts);
            if (upwards ? lastAcross < lastTo : lastAcross > lastTo)
            {
                ext.Add(At(last, alongLon, lastTo));
            }
            return ext;
        }

        /// <summary>The same point moved to a different across coordinate.</summary>
        private static GeoPoint At(GeoPoint p, bool alongLon, double across)
        {
            return alongLon ? new GeoPoint(across, p.Lon) : new GeoPoint(p.Lat, across);
        }

        /// <summary>
        /// Where one scan line crosses the extended coast, sorted along the ray.
        ///
        /// The half-open test is what keeps a vertex that sits exactly on the scan line from being
        /// counted twice, which would flip the parity of everything past it.
        /// </summary>
        private static int Crossings(List<GeoPoint> ext, bool alongLon, double across, double[] into)
        {
            int n = 0;
            for (int i = 0; i + 1 < ext.Count; i++)
            {
                GeoPoint a = ext[i];
                GeoPoint b = ext[i + 1];

                double aAcross = alongLon ? a.Lat : a.Lon;
                double bAcross = alongLon ? b.Lat : b.Lon;
                if ((aAcross <= across) == (bAcross <= across))
                {
                    continue;
                }

                double aAlong = alongLon ? a.Lon : a.Lat;
                double bAlong = alongLon ? b.Lon : b.Lat;
                into[n++] = aAlong + (bAlong - aAlong) * (across - aAcross) / (bAcross - aAcross);
            }

            Array.Sort(into, 0, n);
            return n;
        }

        /// <summary>
        /// The segments that could be the nearest one to any point on this scan line.
        ///
        /// Conservative, and cheaply so: a segment nearer than the clamp is also nearer than the
        /// clamp measured across the scan alone, so testing only that one coordinate can drop a
        /// segment but never the right one.
        /// </summary>
        private static int Candidates(List<GeoPoint> ext, bool alongLon, double across,
            double clampAcross, int[] into)
        {
            int n = 0;
            for (int i = 0; i + 1 < ext.Count; i++)
            {
                double a = alongLon ? ext[i].Lat : ext[i].Lon;
                double b = alongLon ? ext[i + 1].Lat : ext[i + 1].Lon;
                double lo = (a < b ? a : b) - clampAcross;
                double hi = (a < b ? b : a) + clampAcross;

                if (across >= lo && across <= hi)
                {
                    into[n++] = i;
                }
            }
            return n;
        }
    }
}
