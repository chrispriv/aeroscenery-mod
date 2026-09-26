using System;
using System.Globalization;
using System.IO;

namespace AeroScenery.AFS2
{
    /// <summary>
    /// A per-square correction for the water, applied to source pixels as they are sampled.
    ///
    /// Bing builds its high-zoom imagery out of separate acquisitions. Over water the joins are
    /// very visible: one pass carries a layer of thin haze and the next does not, so a lake gets a
    /// straight-edged block tens of RGB units brighter than the water beside it. Measured on the
    /// lake Llanquihue, square map_09_4c00_5f80: clean water is (49, 52, 83) and the block beside
    /// it is (119, 111, 145) - the same standard deviation, so it is a layer on top of the same
    /// water rather than different water.
    ///
    /// The field says what to do about it, per point, as two numbers: a scale and an offset, so
    /// the whole correction is one multiply and one add per channel.
    ///
    ///     corrected = source * scale + offset
    ///
    /// That form is not a simplification, it is the exact algebra of the three things the
    /// correction does - subtract haze, fade out at the shore, and replace thick cloud with a
    /// clean low-zoom reference - folded together. tools/ttc/water_fix.py measures them and folds
    /// them; see its header for how, and for why the reference is only ever used for the water
    /// mask and for the few pixels under cloud.
    ///
    /// It is measured OUTSIDE the converter, in Python, because measuring it needs the whole
    /// square at once plus tiles fetched from Bing at a zoom this converter never asks for. What
    /// is left here is the cheap half: look up two numbers and apply them.
    ///
    /// The correction is applied to the SOURCE value, before the tone curve. The haze is additive
    /// in the units the stitched PNG carries, and the tone curve is not linear, so correcting
    /// after it would subtract the wrong amount - most of all in the shadows, where the curve is
    /// steepest.
    ///
    /// Off by default, for the same two reasons the coastline is: it changes output, and the
    /// Python reference in tools/ttc/convert_tmc.py knows nothing about it, so the byte-for-byte
    /// cross-check only holds without it.
    /// </summary>
    public sealed class WaterFixField
    {
        /// <summary>"AWFX", little endian.</summary>
        public const uint Magic = 0x58465741;

        public const uint Version = 1;

        /// <summary>Texels per side. Square, like the grid square it describes.</summary>
        public int Size { get; private set; }

        public double West { get; private set; }
        public double East { get; private set; }
        public double North { get; private set; }
        public double South { get; private set; }

        /// <summary>Size*Size, one per texel.</summary>
        private float[] scale;

        /// <summary>Size*Size*3, interleaved RGB.</summary>
        private float[] offset;

        /// <summary>
        /// Where a tile's columns land in the field. The columns of a tile depend only on
        /// longitude, so they are worked out once per tile rather than once per row.
        /// </summary>
        public sealed class Columns
        {
            public int[] Left;
            public int[] Right;
            public float[] Fraction;
        }

        private WaterFixField()
        {
        }

        public static WaterFixField Load(string path)
        {
            using (var f = File.OpenRead(path))
            using (var r = new BinaryReader(f))
            {
                var field = new WaterFixField();

                uint magic = r.ReadUInt32();
                if (magic != Magic)
                {
                    throw new InvalidDataException(String.Format(
                        CultureInfo.InvariantCulture,
                        "{0} is not a water fix field: magic {1:x8}, expected {2:x8}",
                        Path.GetFileName(path), magic, Magic));
                }

                uint version = r.ReadUInt32();
                if (version != Version)
                {
                    throw new InvalidDataException(String.Format(
                        CultureInfo.InvariantCulture,
                        "{0} is version {1}, and this build reads version {2}",
                        Path.GetFileName(path), version, Version));
                }

                field.Size = (int)r.ReadUInt32();
                if (field.Size < 2 || field.Size > 16384)
                {
                    throw new InvalidDataException(String.Format(
                        CultureInfo.InvariantCulture, "{0} declares a size of {1}",
                        Path.GetFileName(path), field.Size));
                }

                field.West = r.ReadDouble();
                field.East = r.ReadDouble();
                field.North = r.ReadDouble();
                field.South = r.ReadDouble();

                int n = field.Size * field.Size;
                field.scale = new float[n];
                field.offset = new float[n * 3];

                var bytes = r.ReadBytes(n * 4);
                if (bytes.Length != n * 4)
                {
                    throw new InvalidDataException(Path.GetFileName(path) + " is short of scale data");
                }
                Buffer.BlockCopy(bytes, 0, field.scale, 0, bytes.Length);

                bytes = r.ReadBytes(n * 3 * 4);
                if (bytes.Length != n * 3 * 4)
                {
                    throw new InvalidDataException(Path.GetFileName(path) + " is short of offset data");
                }
                Buffer.BlockCopy(bytes, 0, field.offset, 0, bytes.Length);

                return field;
            }
        }

        /// <summary>
        /// Fractional texel column for a longitude, and row for a latitude. The field is linear in
        /// both, which is what the stitched sources are and is not what the output grid is - but
        /// the correction is smoothed over about 1.8 km, so it is sampled rather than resampled.
        /// </summary>
        private double ColOf(double lon)
        {
            return (lon - West) / (East - West) * Size - 0.5;
        }

        private double RowOf(double lat)
        {
            return (lat - North) / (South - North) * Size - 0.5;
        }

        /// <summary>
        /// Whether any point in the box needs correcting at all.
        ///
        /// Almost every tile in a square needs nothing - the water is a small part of it, and a
        /// tile wholly over land is the common case. Asking this first costs one scan of a few
        /// hundred texels and lets those tiles run exactly the loop they have always run.
        ///
        /// Texels are taken whole rather than interpolated, and the box is grown by one texel on
        /// each side, so a tile that only just reaches a corrected texel still answers true.
        /// </summary>
        public bool TouchesBox(double lonWest, double lonEast, double latSouth, double latNorth)
        {
            int c0 = (int)Math.Floor(ColOf(lonWest)) - 1;
            int c1 = (int)Math.Ceiling(ColOf(lonEast)) + 1;
            int r0 = (int)Math.Floor(RowOf(latNorth)) - 1;
            int r1 = (int)Math.Ceiling(RowOf(latSouth)) + 1;

            if (c0 < 0) c0 = 0;
            if (r0 < 0) r0 = 0;
            if (c1 > Size - 1) c1 = Size - 1;
            if (r1 > Size - 1) r1 = Size - 1;

            for (int r = r0; r <= r1; r++)
            {
                int row = r * Size;
                for (int c = c0; c <= c1; c++)
                {
                    int i = row + c;
                    if (scale[i] != 1.0f
                        || offset[i * 3] != 0.0f
                        || offset[i * 3 + 1] != 0.0f
                        || offset[i * 3 + 2] != 0.0f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Where a tile's longitudes land in the field, worked out once per tile.</summary>
        public Columns PlanColumns(double[] lons)
        {
            var plan = new Columns
            {
                Left = new int[lons.Length],
                Right = new int[lons.Length],
                Fraction = new float[lons.Length]
            };

            for (int i = 0; i < lons.Length; i++)
            {
                double c = ColOf(lons[i]);
                if (c < 0.0) c = 0.0;
                if (c > Size - 1) c = Size - 1;

                int c0 = (int)Math.Floor(c);
                if (c0 > Size - 2) c0 = Math.Max(0, Size - 2);

                plan.Left[i] = c0;
                plan.Right[i] = Math.Min(c0 + 1, Size - 1);
                plan.Fraction[i] = (float)(c - c0);
            }

            return plan;
        }

        /// <summary>
        /// Fills one row of scale and offset, bilinearly, for the longitudes the plan describes.
        ///
        /// scaleOut is one per column, offsetOut three. Outside the field the edge value is held
        /// rather than the correction being switched off: the field covers its own square, the
        /// converter works over a slightly wider range so that every parent has four children, and
        /// switching off at the edge would put a step in the middle of the water there.
        /// </summary>
        public void RowFactors(double lat, Columns cols, float[] scaleOut, float[] offsetOut)
        {
            double rr = RowOf(lat);
            if (rr < 0.0) rr = 0.0;
            if (rr > Size - 1) rr = Size - 1;

            int r0 = (int)Math.Floor(rr);
            if (r0 > Size - 2) r0 = Math.Max(0, Size - 2);
            int r1 = Math.Min(r0 + 1, Size - 1);

            float fr = (float)(rr - r0);
            int top = r0 * Size;
            int bottom = r1 * Size;

            for (int i = 0; i < scaleOut.Length; i++)
            {
                int cl = cols.Left[i];
                int cr = cols.Right[i];
                float fc = cols.Fraction[i];

                int a = top + cl, b = top + cr, c = bottom + cl, d = bottom + cr;

                float sTop = scale[a] + (scale[b] - scale[a]) * fc;
                float sBottom = scale[c] + (scale[d] - scale[c]) * fc;
                scaleOut[i] = sTop + (sBottom - sTop) * fr;

                int a3 = a * 3, b3 = b * 3, c3 = c * 3, d3 = d * 3, o3 = i * 3;
                for (int k = 0; k < 3; k++)
                {
                    float oTop = offset[a3 + k] + (offset[b3 + k] - offset[a3 + k]) * fc;
                    float oBottom = offset[c3 + k] + (offset[d3 + k] - offset[c3 + k]) * fc;
                    offsetOut[o3 + k] = oTop + (oBottom - oTop) * fr;
                }
            }
        }

        /// <summary>One channel, clamped back into a byte. Kept here so the sampler reads plainly.</summary>
        public static byte Apply(byte source, float scale, float offset)
        {
            float v = source * scale + offset;
            if (v <= 0.0f) return 0;
            if (v >= 255.0f) return 255;
            return (byte)(v + 0.5f);
        }
    }
}
