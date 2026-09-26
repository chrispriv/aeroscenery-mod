using System;
using System.Threading.Tasks;

namespace AeroScenery.AFS2
{
    /// <summary>
    /// BC1 / DXT1 encoder and decoder. A faithful port of encode_dxt1 / decode_dxt1 in
    /// tools/ttc/ttc.py: a bounding-box range-fit encoder that always emits the 4-colour
    /// (opaque) mode. It is meant to be byte-for-byte identical to the Python reference, which
    /// is what the validation suite checks. Do not "improve" the encoder here without updating
    /// the reference in lock-step, or the two stop matching.
    /// </summary>
    public static class Bc1Encoder
    {
        /// <summary>
        /// Encodes a top-left-origin RGB image (3 bytes/pixel, row-major) to a BC1 block stream.
        /// The image is padded up to a multiple of 4 by edge replication, matching the reference.
        /// </summary>
        /// <param name="maxThreads">
        /// Block rows are independent - each writes its own disjoint slice of the output - so this
        /// splits them across threads. The result is identical whatever the value, which the
        /// cross-language hashes check. 1 keeps it serial; 0 or less means "as many as the machine
        /// has". This is where a conversion's time actually goes, and parallelising here rather
        /// than across tiles speeds up every level, including the shallow ones that are derived
        /// one row at a time and could not have been spread out any other way.
        /// </param>
        public static byte[] Encode(byte[] rgb, int width, int height, int maxThreads = 1)
        {
            int pw = (width + 3) & ~3;
            int ph = (height + 3) & ~3;
            int bw = pw / 4;
            int bh = ph / 4;
            var outBytes = new byte[bw * bh * 8];

            if (maxThreads == 1 || bh < 8)
            {
                // Small images are not worth the scheduling, and the mip chain ends in a lot of them
                var block = new int[16 * 3];
                for (int by = 0; by < bh; by++)
                {
                    EncodeBlockRow(rgb, width, height, bw, by, outBytes, block);
                }
                return outBytes;
            }

            var options = new ParallelOptions();
            if (maxThreads > 1)
            {
                options.MaxDegreeOfParallelism = maxThreads;
            }

            Parallel.For(0, bh, options,
                () => new int[16 * 3],                       // one scratch block per worker
                (by, state, block) =>
                {
                    EncodeBlockRow(rgb, width, height, bw, by, outBytes, block);
                    return block;
                },
                block => { });

            return outBytes;
        }

        private static void EncodeBlockRow(byte[] rgb, int width, int height, int bw, int by,
            byte[] outBytes, int[] block)
        {
            // Each block row owns its own slice of the output, which is what makes rows safe to
            // encode on separate threads.
            int o = by * bw * 8;

            for (int bx = 0; bx < bw; bx++)
            {
                int loR = 255, loG = 255, loB = 255;
                int hiR = 0, hiG = 0, hiB = 0;

                for (int py = 0; py < 4; py++)
                {
                    int sy = by * 4 + py;
                    if (sy >= height) sy = height - 1;   // edge replication for padding
                    for (int px = 0; px < 4; px++)
                    {
                        int sx = bx * 4 + px;
                        if (sx >= width) sx = width - 1;

                        int si = (sy * width + sx) * 3;
                        int r = rgb[si], g = rgb[si + 1], b = rgb[si + 2];

                        int bi = (py * 4 + px) * 3;
                        block[bi] = r; block[bi + 1] = g; block[bi + 2] = b;

                        if (r < loR) loR = r; if (r > hiR) hiR = r;
                        if (g < loG) loG = g; if (g > hiG) hiG = g;
                        if (b < loB) loB = b; if (b > hiB) hiB = b;
                    }
                }

                uint c0 = To565(hiR, hiG, hiB);
                uint c1 = To565(loR, loG, loB);
                // 4-colour mode requires c0 > c1; if they collapse the block is flat anyway
                if (c0 < c1) { uint t = c0; c0 = c1; c1 = t; }

                int e0R, e0G, e0B, e1R, e1G, e1B;
                From565(c0, out e0R, out e0G, out e0B);
                From565(c1, out e1R, out e1G, out e1B);

                int p2R = (2 * e0R + e1R) / 3, p2G = (2 * e0G + e1G) / 3, p2B = (2 * e0B + e1B) / 3;
                int p3R = (e0R + 2 * e1R) / 3, p3G = (e0G + 2 * e1G) / 3, p3B = (e0B + 2 * e1B) / 3;

                uint bits = 0;
                for (int i = 0; i < 16; i++)
                {
                    int bi = i * 3;
                    int r = block[bi], g = block[bi + 1], b = block[bi + 2];

                    // nearest of the 4 palette entries; ties resolve to the first, which is
                    // what numpy argmin does in the reference
                    int best = 0;
                    long bestD = Dist(r, g, b, e0R, e0G, e0B);
                    long d1 = Dist(r, g, b, e1R, e1G, e1B); if (d1 < bestD) { bestD = d1; best = 1; }
                    long d2 = Dist(r, g, b, p2R, p2G, p2B); if (d2 < bestD) { bestD = d2; best = 2; }
                    long d3 = Dist(r, g, b, p3R, p3G, p3B); if (d3 < bestD) { best = 3; }

                    bits |= (uint)(best & 3) << (2 * i);
                }

                outBytes[o++] = (byte)(c0 & 0xFF);
                outBytes[o++] = (byte)((c0 >> 8) & 0xFF);
                outBytes[o++] = (byte)(c1 & 0xFF);
                outBytes[o++] = (byte)((c1 >> 8) & 0xFF);
                outBytes[o++] = (byte)(bits & 0xFF);
                outBytes[o++] = (byte)((bits >> 8) & 0xFF);
                outBytes[o++] = (byte)((bits >> 16) & 0xFF);
                outBytes[o++] = (byte)((bits >> 24) & 0xFF);
            }
        }

        /// <summary>
        /// Inverse of Encode, for quality checking and round-trip tests. Always uses the 4-colour
        /// palette, matching decode_dxt1 in the reference.
        /// </summary>
        public static byte[] Decode(byte[] data, int width, int height)
        {
            int pw = (width + 3) & ~3;
            int ph = (height + 3) & ~3;
            int bw = pw / 4;
            int bh = ph / 4;
            var img = new byte[width * height * 3];
            int o = 0;

            for (int by = 0; by < bh; by++)
            {
                for (int bx = 0; bx < bw; bx++)
                {
                    uint c0 = (uint)(data[o] | (data[o + 1] << 8));
                    uint c1 = (uint)(data[o + 2] | (data[o + 3] << 8));
                    uint bits = (uint)(data[o + 4] | (data[o + 5] << 8) | (data[o + 6] << 16) | (data[o + 7] << 24));
                    o += 8;

                    int e0R, e0G, e0B, e1R, e1G, e1B;
                    From565(c0, out e0R, out e0G, out e0B);
                    From565(c1, out e1R, out e1G, out e1B);

                    int[] pr = { e0R, e1R, (2 * e0R + e1R) / 3, (e0R + 2 * e1R) / 3 };
                    int[] pg = { e0G, e1G, (2 * e0G + e1G) / 3, (e0G + 2 * e1G) / 3 };
                    int[] pb = { e0B, e1B, (2 * e0B + e1B) / 3, (e0B + 2 * e1B) / 3 };

                    for (int i = 0; i < 16; i++)
                    {
                        int idx = (int)((bits >> (2 * i)) & 3);
                        int x = bx * 4 + (i % 4);
                        int y = by * 4 + (i / 4);
                        if (x < width && y < height)
                        {
                            int di = (y * width + x) * 3;
                            img[di] = (byte)pr[idx];
                            img[di + 1] = (byte)pg[idx];
                            img[di + 2] = (byte)pb[idx];
                        }
                    }
                }
            }
            return img;
        }

        private static uint To565(int r, int g, int b)
        {
            return (uint)((((r >> 3) & 0x1F) << 11) | (((g >> 2) & 0x3F) << 5) | ((b >> 3) & 0x1F));
        }

        private static void From565(uint v, out int r, out int g, out int b)
        {
            int r5 = (int)((v >> 11) & 0x1F);
            int g6 = (int)((v >> 5) & 0x3F);
            int b5 = (int)(v & 0x1F);
            r = (r5 << 3) | (r5 >> 2);
            g = (g6 << 2) | (g6 >> 4);
            b = (b5 << 3) | (b5 >> 2);
        }

        private static long Dist(int r, int g, int b, int R, int G, int B)
        {
            long dr = r - R, dg = g - G, db = b - B;
            return dr * dr + dg * dg + db * db;
        }
    }
}
