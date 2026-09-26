using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace AeroScenery.AFS2
{
    /// <summary>Which side of the drawn line the land is on.</summary>
    public enum LandSide
    {
        East,
        West,
        North,
        South
    }

    public struct GeoPoint
    {
        public double Lat;
        public double Lon;

        public GeoPoint(double lat, double lon)
        {
            Lat = lat;
            Lon = lon;
        }
    }

    /// <summary>
    /// A hand-drawn coastline, and the rule that turns it into where photoscenery stops.
    ///
    /// The line is the WATERLINE, not the cut. The cut sits a margin out to sea, and the user
    /// never draws it: they trace something they can actually see, and the margin is applied
    /// afterwards. Two things fall out of that split, and they are the whole reason for it.
    ///
    /// The margin becomes a parameter rather than a redraw - re-cut at 2, 3 or 5 NM without
    /// touching the line. And precision stops mattering: with the cut 5.5 km offshore, being 500 m
    /// wrong about the waterline moves the cut 500 m over open water, which is invisible. Drawing
    /// the cut directly would need tens of metres, because there Aerofly's coast and the imagery's
    /// coast disagree and any error shows as a fringe of the wrong texture along every beach.
    ///
    /// Coverage is:
    ///
    ///     landward of the line, OR within MarginKm of the line
    ///
    /// which is just "every point within MarginKm of the land". Stating it as a distance rather
    /// than as an offset polygon is what makes headlands come out ROUNDED, radius = the margin -
    /// the set of points within d of a point is a disc. IPACS' own sea cut has exactly that
    /// rounding, which is the signature of the same operation, and it means the radius of one of
    /// their arcs reads off their margin directly. Offsetting each segment and joining the results
    /// in a point would instead grow a spike at every cape.
    ///
    /// Distances are in kilometres through a local equirectangular scale rather than a great
    /// circle: over the few kilometres a margin spans, the difference is far below the metre and
    /// the saving is that everything stays plain arithmetic.
    /// </summary>
    public class Coastline
    {
        public const double KmPerDegLat = 110.9;

        private readonly List<List<GeoPoint>> strokes = new List<List<GeoPoint>>();
        private List<GeoPoint> current;
        private List<GeoPoint> pointsCache;
        private double longestJoinKm;

        public Coastline()
        {
            Land = LandSide.East;
            MarginKm = 3.0 * 1.852;
        }

        /// <summary>Which side of the line is land. East for a west-facing coast like Chile's.</summary>
        public LandSide Land { get; set; }

        /// <summary>How far out to sea the cut sits. 3 NM by default; see docs/user-guide.md, section 5.</summary>
        public double MarginKm { get; set; }

        public int StrokeCount { get { return strokes.Count; } }

        public bool IsEmpty { get { return Points.Count < 2; } }

        /// <summary>
        /// Every stroke's points end to end, in order along the coast, which is the polyline the
        /// margin is measured from.
        ///
        /// Strokes are kept separate only so an undo can drop the last one whole. They are joined
        /// rather than treated as separate lines because the coast is one line: the user releases
        /// the button to pan and carries on, and a gap between where they let go and where they
        /// picked up is a gap in the coast, not a new one.
        ///
        /// Treat the list as read only. It is cached, so the same list comes back until the line
        /// changes, and a caller that edits it edits the coastline.
        /// </summary>
        public List<GeoPoint> Points
        {
            get
            {
                if (pointsCache == null)
                {
                    Rebuild();
                }
                return pointsCache;
            }
        }

        /// <summary>
        /// The widest gap between one stroke and the next, in km, once they are in order.
        ///
        /// A few hundred metres is the ordinary business of letting go of the button to pan.
        /// Kilometres mean a piece of coast was never drawn, and the straight line laid across the
        /// gap counts as coast to the converter. Nothing can invent the missing piece, so the only
        /// useful thing to do with this number is show it.
        /// </summary>
        public double LongestJoinKm
        {
            get
            {
                if (pointsCache == null)
                {
                    Rebuild();
                }
                return longestJoinKm;
            }
        }

        private void Rebuild()
        {
            var all = new List<GeoPoint>();
            longestJoinKm = 0.0;

            foreach (var s in Chain())
            {
                if (all.Count > 0)
                {
                    double gap = PointDistanceKm(all[all.Count - 1], s[0]);
                    if (gap > longestJoinKm)
                    {
                        longestJoinKm = gap;
                    }
                }
                all.AddRange(s);
            }
            pointsCache = all;
        }

        /// <summary>
        /// The strokes put in order along the coast, each one turned to face the same way.
        ///
        /// They used to be joined in the order they were DRAWN, which quietly made the drawing
        /// order part of the data. Draw the south end after the north end and the line gained a
        /// straight segment between the two, tens of kilometres long, which the converter reads as
        /// coast. That is a silent way to lose an afternoon, and it also meant a coast could only
        /// ever be extended at one end - carrying on southward tomorrow meant starting again.
        ///
        /// Each stroke is now attached to whichever end of the chain it is nearest, reversed if it
        /// faces the wrong way. A coast is one line, so its pieces have exactly one sensible
        /// arrangement and the nearest ends find it. Draw in any order, in either direction, and
        /// start anywhere.
        ///
        /// What this cannot do is invent a piece that was never drawn. A gap is still a gap and
        /// still gets a straight line across it; LongestJoinKm is what makes that visible rather
        /// than silent.
        /// </summary>
        private List<List<GeoPoint>> Chain()
        {
            var ordered = new List<List<GeoPoint>>();
            if (strokes.Count == 0)
            {
                return ordered;
            }

            var used = new bool[strokes.Count];
            ordered.Add(new List<GeoPoint>(strokes[0]));
            used[0] = true;

            for (int placed = 1; placed < strokes.Count; placed++)
            {
                List<GeoPoint> lastStroke = ordered[ordered.Count - 1];
                GeoPoint head = ordered[0][0];
                GeoPoint tail = lastStroke[lastStroke.Count - 1];

                double best = Double.MaxValue;
                int bestStroke = -1;
                bool atTail = true;
                bool flip = false;

                for (int i = 0; i < strokes.Count; i++)
                {
                    if (used[i])
                    {
                        continue;
                    }

                    List<GeoPoint> s = strokes[i];
                    GeoPoint a = s[0];
                    GeoPoint b = s[s.Count - 1];

                    // Four ways to attach it: either end of the chain, either way round.
                    double d = PointDistanceKm(tail, a);
                    if (d < best) { best = d; bestStroke = i; atTail = true; flip = false; }

                    d = PointDistanceKm(tail, b);
                    if (d < best) { best = d; bestStroke = i; atTail = true; flip = true; }

                    d = PointDistanceKm(head, b);
                    if (d < best) { best = d; bestStroke = i; atTail = false; flip = false; }

                    d = PointDistanceKm(head, a);
                    if (d < best) { best = d; bestStroke = i; atTail = false; flip = true; }
                }

                if (bestStroke < 0)
                {
                    break;
                }

                var add = new List<GeoPoint>(strokes[bestStroke]);
                if (flip)
                {
                    add.Reverse();
                }
                if (atTail)
                {
                    ordered.Add(add);
                }
                else
                {
                    ordered.Insert(0, add);
                }
                used[bestStroke] = true;
            }

            return ordered;
        }

        public static double KmPerDegLon(double lat)
        {
            return 111.320 * Math.Cos(lat * Math.PI / 180.0);
        }

        public void BeginStroke()
        {
            current = new List<GeoPoint>();
            strokes.Add(current);
            pointsCache = null;
        }

        public void AddPoint(double lat, double lon)
        {
            if (current == null)
            {
                BeginStroke();
            }
            current.Add(new GeoPoint(lat, lon));
            pointsCache = null;
        }

        /// <summary>
        /// Ends a stroke and thins it, because a drag produces a point every few pixels and the
        /// margin cannot resolve them. Tolerance is in km and belongs well under the margin: at a
        /// fifth of it, the thinning is invisible in the cut and the vertex count drops by orders
        /// of magnitude.
        /// </summary>
        public void EndStroke(double toleranceKm)
        {
            if (current == null)
            {
                return;
            }
            if (current.Count < 2)
            {
                strokes.Remove(current);
            }
            else
            {
                int i = strokes.IndexOf(current);
                strokes[i] = Simplify(current, toleranceKm);
            }
            current = null;
            pointsCache = null;
        }

        /// <summary>
        /// Drops the stroke drawn most recently, which is not necessarily the one at either end of
        /// the coast now that the strokes are ordered geographically. Undo means "take back what I
        /// just did", so it stays the last one DRAWN.
        /// </summary>
        public bool UndoStroke()
        {
            if (strokes.Count == 0)
            {
                return false;
            }
            strokes.RemoveAt(strokes.Count - 1);
            current = null;
            pointsCache = null;
            return true;
        }

        public void Clear()
        {
            strokes.Clear();
            current = null;
            pointsCache = null;
        }

        // -----------------------------------------------------------------------------------
        // geometry
        // -----------------------------------------------------------------------------------

        /// <summary>Distance in km between two points.</summary>
        private static double PointDistanceKm(GeoPoint a, GeoPoint b)
        {
            double x, y;
            Delta(a, b, out x, out y);
            return Math.Sqrt(x * x + y * y);
        }

        /// <summary>Local km offsets of b from a, x east and y north.</summary>
        private static void Delta(GeoPoint a, GeoPoint b, out double x, out double y)
        {
            double midLat = 0.5 * (a.Lat + b.Lat);
            x = (b.Lon - a.Lon) * KmPerDegLon(midLat);
            y = (b.Lat - a.Lat) * KmPerDegLat;
        }

        /// <summary>Shortest distance in km from a point to the polyline.</summary>
        public double DistanceKm(double lat, double lon)
        {
            return DistanceKm(Points, lat, lon);
        }

        /// <summary>
        /// The same, against a list the caller already holds. Points rebuilds itself on every
        /// read, so a loop that measures thousands of points wants to hoist it out.
        /// </summary>
        private static double DistanceKm(List<GeoPoint> pts, double lat, double lon)
        {
            if (pts.Count == 0)
            {
                return Double.MaxValue;
            }

            var p = new GeoPoint(lat, lon);
            double best = Double.MaxValue;

            for (int i = 0; i + 1 < pts.Count; i++)
            {
                double d = SegmentDistanceKm(p, pts[i], pts[i + 1]);
                if (d < best)
                {
                    best = d;
                }
            }
            return best;
        }

        /// <summary>
        /// Shortest distance in km from a point to one segment. Public because CoastlineField
        /// measures the same thing a few million times and has to get the same answer this class
        /// would: one definition of the distance, or the rasterised cut and the drawn one drift.
        /// </summary>
        public static double SegmentDistanceKm(GeoPoint p, GeoPoint a, GeoPoint b)
        {
            double abx, aby, apx, apy;
            Delta(a, b, out abx, out aby);
            Delta(a, p, out apx, out apy);

            double len2 = abx * abx + aby * aby;
            double t = (len2 <= 0.0) ? 0.0 : (apx * abx + apy * aby) / len2;
            if (t < 0.0) t = 0.0;
            else if (t > 1.0) t = 1.0;

            double dx = apx - t * abx;
            double dy = apy - t * aby;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Which hand the sea is on, as +1 or -1, decided once for the whole line.
        ///
        /// Per segment it cannot be decided at all: a stretch of coast running exactly east-west
        /// has one normal north and one south, and "land is east" says nothing about either. So
        /// the handedness is settled from the line's overall run and then applied to every
        /// segment, which is also what keeps a wiggly coast from flipping its cut inside out at
        /// each wiggle.
        /// </summary>
        private int SeaHand(List<GeoPoint> pts)
        {
            double x, y;
            Delta(pts[0], pts[pts.Count - 1], out x, out y);
            if (Math.Abs(x) < 1e-9 && Math.Abs(y) < 1e-9)
            {
                return 1;
            }

            // Right of travel, i.e. the direction rotated -90 degrees.
            double rx = y, ry = -x;

            switch (Land)
            {
                case LandSide.East: return rx < 0 ? 1 : -1;
                case LandSide.West: return rx > 0 ? 1 : -1;
                case LandSide.North: return ry < 0 ? 1 : -1;
                default: return ry > 0 ? 1 : -1;
            }
        }

        /// <summary>
        /// The seaward cut line: where photoscenery stops, at the current margin.
        ///
        /// Built by offsetting each segment and rounding every convex corner into an arc, then
        /// dropping any point that came out closer to the coast than the margin. That last pass is
        /// what makes it agree with the distance rule rather than merely resemble it: where the
        /// coast bends tighter than the margin the raw offset crosses itself, and the points
        /// inside those loops are exactly the ones that are too close. Cull them and what is left
        /// is the true contour, with no polygon clipping anywhere.
        /// </summary>
        public List<GeoPoint> CutLine()
        {
            var pts = Points;
            if (pts.Count < 2 || MarginKm <= 0.0)
            {
                return pts;
            }

            int hand = SeaHand(pts);
            double m = MarginKm;
            var raw = new List<GeoPoint>();

            for (int i = 0; i + 1 < pts.Count; i++)
            {
                double dx, dy;
                Delta(pts[i], pts[i + 1], out dx, out dy);
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len < 1e-9)
                {
                    continue;
                }

                double nx = hand * dy / len;
                double ny = -hand * dx / len;

                raw.Add(Offset(pts[i], m * nx, m * ny));
                raw.Add(Offset(pts[i + 1], m * nx, m * ny));

                // Round the corner into the next segment. Only the seaward turn needs arc points;
                // the landward one is where the offset self-crosses and gets culled below anyway.
                if (i + 2 < pts.Count)
                {
                    double ex, ey;
                    Delta(pts[i + 1], pts[i + 2], out ex, out ey);
                    double elen = Math.Sqrt(ex * ex + ey * ey);
                    if (elen < 1e-9)
                    {
                        continue;
                    }

                    double a0 = Math.Atan2(ny, nx);
                    double a1 = Math.Atan2(-hand * ex / elen, hand * ey / elen);
                    double sweep = NormaliseAngle(a1 - a0);

                    int steps = (int)Math.Ceiling(Math.Abs(sweep) / (Math.PI / 12.0));
                    for (int k = 1; k < steps; k++)
                    {
                        double a = a0 + sweep * k / steps;
                        raw.Add(Offset(pts[i + 1], m * Math.Cos(a), m * Math.Sin(a)));
                    }
                }
            }

            // Anything nearer the coast than the margin is inside a fold, not on the contour.
            // The slack keeps the offset's own points, which sit at exactly the margin.
            //
            // Dropping them is not enough on its own, and believing it was is what put the drawn
            // cut 2.25 km OUTSIDE a 1.852 km margin on the pilot's own line. The two points either
            // side of a dropped run are both at the margin, but the chord between them is not:
            // where the offset folds, the contour turns a corner that no vertex was ever generated
            // for, and the chord sails straight past it out to sea. A vertex test cannot see it -
            // every vertex is exactly where it should be - and on the map it reads as the cut
            // wandering off and losing whole stretches of coast.
            //
            // So each crossing gets a point of its own, found on the raw segment that straddles
            // it. That is the missing corner, and it costs one bisection per fold.
            double keep = m * 0.999;
            var near = new NearIndex(pts, keep);
            var cut = new List<GeoPoint>();
            bool wasOut = false;

            for (int i = 0; i < raw.Count; i++)
            {
                bool isOut = !near.AnyNearerThan(raw[i].Lat, raw[i].Lon, keep);

                if (i > 0 && isOut != wasOut)
                {
                    cut.Add(isOut ? Crossing(near, raw[i], raw[i - 1], keep)
                                  : Crossing(near, raw[i - 1], raw[i], keep));
                }
                if (isOut)
                {
                    cut.Add(raw[i]);
                }
                wasOut = isOut;
            }

            return Smooth(cut, near, m);
        }

        /// <summary>
        /// Fills in the long steps the culling leaves behind, and pulls each new point onto the
        /// contour.
        ///
        /// The crossing points above stop the cut leaving the margin by kilometres, but they only
        /// bound the ERROR - the shape between them is still a straight chord where the contour
        /// curves, and around a headland with a bay on each side that reads as a rectangular notch
        /// in what should be an arc. Measured at 1.68 km outside a 5.556 km margin at Hualpen, on
        /// a 900 km line whose worst point anywhere else was under 500 m.
        ///
        /// Every step longer than a quarter of the margin is subdivided, and each new point is
        /// walked onto the contour along the gradient of the distance - which is just "straight
        /// away from the nearest coast", found by differences rather than by hunting for the
        /// nearest segment. A point already at the margin does not move, so the long straight
        /// stretches of a cut cost only the points.
        /// </summary>
        private static List<GeoPoint> Smooth(List<GeoPoint> cut, NearIndex near, double m)
        {
            double step = m * 0.25;
            var outPts = new List<GeoPoint>(cut.Count * 2);

            for (int i = 0; i < cut.Count; i++)
            {
                outPts.Add(cut[i]);
                if (i + 1 >= cut.Count)
                {
                    break;
                }

                double gap = PointDistanceKm(cut[i], cut[i + 1]);
                if (gap <= step)
                {
                    continue;
                }

                int pieces = (int)Math.Ceiling(gap / step);
                for (int k = 1; k < pieces; k++)
                {
                    double t = (double)k / pieces;
                    var mid = new GeoPoint(cut[i].Lat + (cut[i + 1].Lat - cut[i].Lat) * t,
                                           cut[i].Lon + (cut[i + 1].Lon - cut[i].Lon) * t);
                    outPts.Add(OntoContour(near, mid, m));
                }
            }
            return outPts;
        }

        /// <summary>Walks a point onto the margin contour, along the gradient of the distance.</summary>
        private static GeoPoint OntoContour(NearIndex near, GeoPoint p, double m)
        {
            double limit = 2.0 * m;
            const double Eps = 0.02;

            for (int i = 0; i < 4; i++)
            {
                double d = near.NearestKm(p.Lat, p.Lon, limit);
                if (Math.Abs(d - m) < 0.01)
                {
                    break;
                }

                double dLat = Eps / KmPerDegLat;
                double dLon = Eps / KmPerDegLon(p.Lat);
                double gy = (near.NearestKm(p.Lat + dLat, p.Lon, limit)
                           - near.NearestKm(p.Lat - dLat, p.Lon, limit)) / (2.0 * Eps);
                double gx = (near.NearestKm(p.Lat, p.Lon + dLon, limit)
                           - near.NearestKm(p.Lat, p.Lon - dLon, limit)) / (2.0 * Eps);

                double g = Math.Sqrt(gx * gx + gy * gy);
                if (g < 1e-6)
                {
                    break;
                }

                double move = m - d;
                p = Offset(p, move * gx / g, move * gy / g);
            }
            return p;
        }

        /// <summary>
        /// Where the straight line from a point at the margin to one inside it crosses the margin.
        ///
        /// Bisection rather than algebra, because the margin is a distance to a whole polyline and
        /// the crossing has no closed form. Twenty halvings take any raw segment to under a metre,
        /// which is four orders of magnitude below the thing being decided.
        /// </summary>
        private static GeoPoint Crossing(NearIndex near, GeoPoint outside, GeoPoint inside,
            double keep)
        {
            for (int i = 0; i < 20; i++)
            {
                var mid = new GeoPoint(0.5 * (outside.Lat + inside.Lat),
                                       0.5 * (outside.Lon + inside.Lon));
                if (near.AnyNearerThan(mid.Lat, mid.Lon, keep))
                {
                    inside = mid;
                }
                else
                {
                    outside = mid;
                }
            }
            return outside;
        }

        /// <summary>
        /// The segments bucketed by latitude, so asking "does the coast come nearer than the
        /// margin" walks a handful of them instead of all of them.
        ///
        /// CutLine asks that once per raw offset point plus twenty per fold, and answering it the
        /// plain way made a 2000-vertex line take nearly two seconds - on every release of the
        /// mouse button, which is what a coast worth drawing costs. That cost, not any argument
        /// about the cut, is what used to keep the thinning tolerance coarse.
        ///
        /// A segment further away in latitude than the limit is further away outright, so
        /// bucketing on latitude alone can drop a segment but never the nearest one. A coast
        /// running exactly east-west lands in a single bucket and gets no faster, which is exactly
        /// where it started.
        /// </summary>
        private sealed class NearIndex
        {
            private const int MaxBands = 4096;

            private readonly List<GeoPoint> pts;
            private readonly List<int>[] bands;
            private readonly double south;
            private readonly double step;

            public NearIndex(List<GeoPoint> pts, double limitKm)
            {
                this.pts = pts;

                double lo = Double.MaxValue, hi = Double.MinValue;
                for (int i = 0; i < pts.Count; i++)
                {
                    if (pts[i].Lat < lo) lo = pts[i].Lat;
                    if (pts[i].Lat > hi) hi = pts[i].Lat;
                }

                south = lo;
                step = Math.Max(limitKm, 0.01) / KmPerDegLat;

                int count = (int)((hi - lo) / step) + 1;
                if (count > MaxBands)
                {
                    step = (hi - lo) / MaxBands + 1e-12;
                    count = MaxBands + 1;
                }

                bands = new List<int>[count];
                for (int i = 0; i < count; i++)
                {
                    bands[i] = new List<int>();
                }

                for (int i = 0; i + 1 < pts.Count; i++)
                {
                    int a = Band(Math.Min(pts[i].Lat, pts[i + 1].Lat));
                    int b = Band(Math.Max(pts[i].Lat, pts[i + 1].Lat));
                    for (int k = a; k <= b; k++)
                    {
                        bands[k].Add(i);
                    }
                }
            }

            private int Band(double lat)
            {
                int b = (int)((lat - south) / step);
                if (b < 0) return 0;
                if (b > bands.Length - 1) return bands.Length - 1;
                return b;
            }

            public bool AnyNearerThan(double lat, double lon, double limitKm)
            {
                return NearestKm(lat, lon, limitKm) < limitKm;
            }

            /// <summary>
            /// Distance to the nearest segment, or limitKm if nothing comes that close. The cap is
            /// what lets the bands do their work; past it the answer is only ever compared.
            /// </summary>
            public double NearestKm(double lat, double lon, double limitKm)
            {
                double slack = limitKm / KmPerDegLat;
                int first = Band(lat - slack);
                int last = Band(lat + slack);
                var p = new GeoPoint(lat, lon);
                double best = limitKm;

                for (int b = first; b <= last; b++)
                {
                    List<int> band = bands[b];
                    for (int j = 0; j < band.Count; j++)
                    {
                        int s = band[j];
                        double d = SegmentDistanceKm(p, pts[s], pts[s + 1]);
                        if (d < best)
                        {
                            best = d;
                        }
                    }
                }
                return best;
            }
        }

        private static double NormaliseAngle(double a)
        {
            while (a > Math.PI) a -= 2.0 * Math.PI;
            while (a < -Math.PI) a += 2.0 * Math.PI;
            return a;
        }

        private static GeoPoint Offset(GeoPoint p, double eastKm, double northKm)
        {
            return new GeoPoint(p.Lat + northKm / KmPerDegLat,
                                p.Lon + eastKm / KmPerDegLon(p.Lat));
        }

        /// <summary>Douglas-Peucker, iterative so a long drag cannot blow the stack.</summary>
        public static List<GeoPoint> Simplify(List<GeoPoint> pts, double toleranceKm)
        {
            if (pts.Count < 3)
            {
                return new List<GeoPoint>(pts);
            }

            var keep = new bool[pts.Count];
            keep[0] = true;
            keep[pts.Count - 1] = true;

            var work = new Stack<int[]>();
            work.Push(new int[] { 0, pts.Count - 1 });

            while (work.Count > 0)
            {
                int[] range = work.Pop();
                int first = range[0], last = range[1];

                double worst = -1.0;
                int worstAt = -1;
                for (int i = first + 1; i < last; i++)
                {
                    double d = SegmentDistanceKm(pts[i], pts[first], pts[last]);
                    if (d > worst)
                    {
                        worst = d;
                        worstAt = i;
                    }
                }

                if (worst > toleranceKm && worstAt > 0)
                {
                    keep[worstAt] = true;
                    work.Push(new int[] { first, worstAt });
                    work.Push(new int[] { worstAt, last });
                }
            }

            var outPts = new List<GeoPoint>();
            for (int i = 0; i < pts.Count; i++)
            {
                if (keep[i])
                {
                    outPts.Add(pts[i]);
                }
            }
            return outPts;
        }

        // -----------------------------------------------------------------------------------
        // storage - plain text, one point per line, so it can be read and hand-edited
        // -----------------------------------------------------------------------------------

        public void Save(string path)
        {
            using (var w = new StreamWriter(path, false))
            {
                w.WriteLine("# aeroscenery coastline - lat lon, one point per line, blank line ends a stroke");
                w.WriteLine("# land {0}", Land);
                w.WriteLine("# margin_km {0}", MarginKm.ToString("R", CultureInfo.InvariantCulture));
                foreach (var s in strokes)
                {
                    foreach (var p in s)
                    {
                        w.WriteLine("{0} {1}",
                            p.Lat.ToString("R", CultureInfo.InvariantCulture),
                            p.Lon.ToString("R", CultureInfo.InvariantCulture));
                    }
                    w.WriteLine();
                }
            }
        }

        public static Coastline Load(string path)
        {
            var c = new Coastline();
            if (!File.Exists(path))
            {
                return c;
            }

            List<GeoPoint> stroke = null;
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0)
                {
                    stroke = null;
                    continue;
                }

                if (line[0] == '#')
                {
                    string[] meta = line.Substring(1).Trim().Split(' ');
                    if (meta.Length == 2 && meta[0] == "land")
                    {
                        try { c.Land = (LandSide)Enum.Parse(typeof(LandSide), meta[1], true); }
                        catch (ArgumentException) { }
                    }
                    else if (meta.Length == 2 && meta[0] == "margin_km")
                    {
                        double km;
                        if (Double.TryParse(meta[1], NumberStyles.Float,
                                            CultureInfo.InvariantCulture, out km))
                        {
                            c.MarginKm = km;
                        }
                    }
                    continue;
                }

                string[] parts = line.Split(' ');
                double lat, lon;
                if (parts.Length == 2
                    && Double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out lat)
                    && Double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out lon))
                {
                    if (stroke == null)
                    {
                        stroke = new List<GeoPoint>();
                        c.strokes.Add(stroke);
                    }
                    stroke.Add(new GeoPoint(lat, lon));
                }
            }

            c.strokes.RemoveAll(delegate (List<GeoPoint> s) { return s.Count < 2; });
            return c;
        }
    }
}
