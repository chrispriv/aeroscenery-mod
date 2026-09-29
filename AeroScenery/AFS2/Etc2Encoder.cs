using System;
using System.Threading.Tasks;

namespace AeroScenery.AFS2
{
    /// <summary>
    /// ETC2 RGB encoder (format id 21). Blocks are ETC1-compatible, which ETC2 RGB accepts.
    /// Layout matches Khronos ETC1 and the Python ttc_converter.py / etcpak path: 4x4 tiles,
    /// 8 bytes each, concatenated mips, zlib-wrapped by <see cref="TtcFile"/>.
    /// </summary>
    public static class Etc2Encoder
    {
        private static readonly int[,] Modifiers =
        {
            { 2, 8, -2, -8 },
            { 5, 17, -5, -17 },
            { 9, 29, -9, -29 },
            { 13, 42, -13, -42 },
            { 18, 60, -18, -60 },
            { 24, 80, -24, -80 },
            { 33, 106, -33, -106 },
            { 47, 183, -47, -183 }
        };

        private sealed class RowScratch
        {
            public readonly int[] Block = new int[16 * 3];
            public readonly byte[] Packed = new byte[8];
        }

        public static byte[] Encode(byte[] rgb, int width, int height, int maxThreads = 1)
        {
            int pw = (width + 3) & ~3;
            int ph = (height + 3) & ~3;
            int bw = pw / 4;
            int bh = ph / 4;
            var outBytes = new byte[bw * bh * 8];

            if (maxThreads == 1 || bh < 8)
            {
                var scratch = new RowScratch();
                for (int by = 0; by < bh; by++)
                {
                    EncodeBlockRow(rgb, width, height, bw, by, outBytes, scratch.Block, scratch.Packed);
                }
                return outBytes;
            }

            var options = new ParallelOptions();
            if (maxThreads > 1)
            {
                options.MaxDegreeOfParallelism = maxThreads;
            }

            Parallel.For(0, bh, options,
                () => new RowScratch(),
                (by, state, local) =>
                {
                    EncodeBlockRow(rgb, width, height, bw, by, outBytes, local.Block, local.Packed);
                    return local;
                },
                local => { });

            return outBytes;
        }

        private static void EncodeBlockRow(byte[] rgb, int width, int height, int bw, int by,
            byte[] outBytes, int[] block, byte[] packed)
        {
            int o = by * bw * 8;
            for (int bx = 0; bx < bw; bx++)
            {
                LoadBlock(rgb, width, height, bx, by, block);
                EncodeBlock(block, packed);
                Buffer.BlockCopy(packed, 0, outBytes, o, 8);
                o += 8;
            }
        }

        private static void LoadBlock(byte[] rgb, int width, int height, int bx, int by, int[] block)
        {
            for (int py = 0; py < 4; py++)
            {
                int sy = by * 4 + py;
                if (sy >= height) sy = height - 1;
                for (int px = 0; px < 4; px++)
                {
                    int sx = bx * 4 + px;
                    if (sx >= width) sx = width - 1;
                    int si = (sy * width + sx) * 3;
                    int bi = (py * 4 + px) * 3;
                    block[bi] = rgb[si];
                    block[bi + 1] = rgb[si + 1];
                    block[bi + 2] = rgb[si + 2];
                }
            }
        }

        private static void EncodeBlock(int[] block, byte[] packed)
        {
            long bestErr = long.MaxValue;
            var best = new byte[8];

            for (int flip = 0; flip < 2; flip++)
            {
                TryMode(block, flip, true, ref bestErr, best);
                TryMode(block, flip, false, ref bestErr, best);
            }

            Buffer.BlockCopy(best, 0, packed, 0, 8);
        }

        private static void TryMode(int[] block, int flip, bool differential, ref long bestErr, byte[] best)
        {
            int r1, g1, b1, r2, g2, b2;
            AverageSubblock(block, flip, 0, out r1, out g1, out b1);
            AverageSubblock(block, flip, 1, out r2, out g2, out b2);

            int qr1, qg1, qb1, qr2, qg2, qb2;
            if (differential)
            {
                int r5 = Quant5(r1), g5 = Quant5(g1), b5 = Quant5(b1);
                int r5b = Quant5(r2), g5b = Quant5(g2), b5b = Quant5(b2);
                int dr = r5b - r5, dg = g5b - g5, db = b5b - b5;
                if (dr < -4 || dr > 3 || dg < -4 || dg > 3 || db < -4 || db > 3)
                {
                    return;
                }
                qr1 = Expand5(r5);
                qg1 = Expand5(g5);
                qb1 = Expand5(b5);
                qr2 = Expand5(r5 + dr);
                qg2 = Expand5(g5 + dg);
                qb2 = Expand5(b5 + db);
                r1 = r5;
                g1 = g5;
                b1 = b5;
                r2 = dr & 7;
                g2 = dg & 7;
                b2 = db & 7;
            }
            else
            {
                r1 = Quant4(r1);
                g1 = Quant4(g1);
                b1 = Quant4(b1);
                r2 = Quant4(r2);
                g2 = Quant4(g2);
                b2 = Quant4(b2);
                qr1 = Expand4(r1);
                qg1 = Expand4(g1);
                qb1 = Expand4(b1);
                qr2 = Expand4(r2);
                qg2 = Expand4(g2);
                qb2 = Expand4(b2);
            }

            var idx = new int[16];
            int t1, t2;
            long e1 = BestTable(block, flip, 0, qr1, qg1, qb1, idx, out t1);
            long e2 = BestTable(block, flip, 1, qr2, qg2, qb2, idx, out t2);
            long err = e1 + e2;
            if (err >= bestErr)
            {
                return;
            }

            bestErr = err;
            if (differential)
            {
                best[0] = (byte)((r1 << 3) | r2);
                best[1] = (byte)((g1 << 3) | g2);
                best[2] = (byte)((b1 << 3) | b2);
            }
            else
            {
                best[0] = (byte)((r1 << 4) | r2);
                best[1] = (byte)((g1 << 4) | g2);
                best[2] = (byte)((b1 << 4) | b2);
            }
            best[3] = (byte)((t1 << 5) | (t2 << 2) | ((differential ? 1 : 0) << 1) | flip);

            uint msbs = 0, lsbs = 0;
            int bit = 0;
            for (int x = 0; x < 4; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    int p = idx[y * 4 + x];
                    msbs |= (uint)((p >> 1) & 1) << bit;
                    lsbs |= (uint)(p & 1) << bit;
                    bit++;
                }
            }
            uint packed = (msbs << 16) | lsbs;
            best[4] = (byte)(packed >> 24);
            best[5] = (byte)(packed >> 16);
            best[6] = (byte)(packed >> 8);
            best[7] = (byte)packed;
        }

        private static void AverageSubblock(int[] block, int flip, int which,
            out int r, out int g, out int b)
        {
            int sr = 0, sg = 0, sb = 0, n = 0;
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    if (SubblockOf(x, y, flip) != which)
                    {
                        continue;
                    }
                    int i = (y * 4 + x) * 3;
                    sr += block[i];
                    sg += block[i + 1];
                    sb += block[i + 2];
                    n++;
                }
            }
            r = (sr + n / 2) / n;
            g = (sg + n / 2) / n;
            b = (sb + n / 2) / n;
        }

        private static int SubblockOf(int x, int y, int flip)
        {
            return flip == 0 ? (x < 2 ? 0 : 1) : (y < 2 ? 0 : 1);
        }

        private static long BestTable(int[] block, int flip, int which, int br, int bg, int bb,
            int[] idx, out int table)
        {
            long best = long.MaxValue;
            table = 0;
            var bestIdx = new int[8];
            int slot = 0;

            for (int t = 0; t < 8; t++)
            {
                long err = 0;
                slot = 0;
                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        if (SubblockOf(x, y, flip) != which)
                        {
                            continue;
                        }
                        int i = (y * 4 + x) * 3;
                        int pr = block[i], pg = block[i + 1], pb = block[i + 2];
                        int bestP = 0;
                        long bestD = long.MaxValue;
                        for (int p = 0; p < 4; p++)
                        {
                            int mr = Clamp(br + Modifiers[t, p]);
                            int mg = Clamp(bg + Modifiers[t, p]);
                            int mb = Clamp(bb + Modifiers[t, p]);
                            long dr = pr - mr, dg = pg - mg, db = pb - mb;
                            long d = dr * dr + dg * dg + db * db;
                            if (d < bestD)
                            {
                                bestD = d;
                                bestP = p;
                            }
                        }
                        err += bestD;
                        bestIdx[slot++] = (y * 4 + x) | (bestP << 8);
                    }
                }
                if (err < best)
                {
                    best = err;
                    table = t;
                    for (int s = 0; s < 8; s++)
                    {
                        int packed = bestIdx[s];
                        idx[packed & 255] = packed >> 8;
                    }
                }
            }
            return best;
        }

        private static int Quant4(int c)
        {
            return (c * 15 + 127) / 255;
        }

        private static int Quant5(int c)
        {
            return (c * 31 + 127) / 255;
        }

        private static int Expand4(int v)
        {
            return (v << 4) | v;
        }

        private static int Expand5(int v)
        {
            return (v << 3) | (v >> 2);
        }

        private static int Clamp(int v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return v;
        }
    }
}
