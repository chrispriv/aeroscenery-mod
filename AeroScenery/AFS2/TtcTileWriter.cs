using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace AeroScenery.AFS2
{
    /// <summary>
    /// Turns one sampled tile into the files Aerofly reads: the colour .ttc, and an R8 (L8)
    /// 512x512 _mask.ttc in the FS4 folder when write_images_with_mask is on. That is the same
    /// layout GeoConvert writes (decoder: r8 512x512 mips=10). FSG never gets mask tiles.
    ///
    /// Port of write_tile in tools/ttc/convert_tmc.py.
    /// </summary>
    public static class TtcTileWriter
    {
        public const int TileSize = 2048;
        public const int MaskSize = 512;

        /// <summary>
        /// Alpha at or below this is black on the mask. Colour tiles use a lower cutoff so a
        /// little extra source imagery remains in the fade and FS4 does not show a black rim.
        /// </summary>
        public const int MaskWhiteMinAlpha = 192;

        /// <summary>
        /// When a mask is built, colour pixels at or below this are omitted. Must stay below
        /// MaskWhiteMinAlpha so the DXT1 fade is slightly wider than the white mask.
        /// </summary>
        public const int MaskColorMinAlpha = 128;

        /// <summary>
        /// Average this many texels on a side in each 4x4 source cell (mask is 512 from 2048).
        /// 4 is a full box; 2 was a centred subset.
        /// </summary>
        public const int MaskAverageBlock = 4;

        /// <summary>
        /// Writes a tile and returns the file names produced, which is empty when no source
        /// reached it.
        ///
        /// A tile nothing covers is not a black tile, it is no tile. GeoConvert omits them and so
        /// must we: each would cost 2.8 MB and a full encode, and would black out terrain Aerofly
        /// would otherwise draw from its own imagery.
        /// </summary>
        public static List<string> Write(string outputDirectory, int level, int tileX, int tileY,
            byte[] rgb, bool[] covered, bool wantMask, int maxThreads = 1, string rawDirectory = null,
            string mobileDirectory = null, bool writeDxt1 = true, bool writeEtc2 = false,
            byte[] maskAlpha = null)
        {
            var written = new List<string>();

            bool anyCovered = false;
            bool allCovered = true;
            for (int i = 0; i < covered.Length; i++)
            {
                if (covered[i]) anyCovered = true;
                else allCovered = false;
            }
            if (!anyCovered)
            {
                return written;
            }

            // Same north-up PNG dump GeoConvert writes when write_raw_files is on.
            // The GPU payload is flipped south-up after this.
            if (!string.IsNullOrEmpty(rawDirectory))
            {
                WriteRawPng(rawDirectory, level, tileX, tileY, rgb);
            }

            var flipped = FlipRows(rgb, TileSize, TileSize, 3);
            string name = TtcTileName.ForTile(level, tileX, tileY);

            if (writeDxt1)
            {
                int mips;
                byte[] chain = TtcMipChain.Build(flipped, TileSize, TileSize, 3, TtcFile.FormatDxt1,
                    out mips, 0, maxThreads);
                byte[] data = TtcFile.BuildCompressed(level, TileSize, TileSize, mips, TtcFile.FormatDxt1, chain);
                Directory.CreateDirectory(outputDirectory);
                string dxtPath = Path.Combine(outputDirectory, name);
                File.WriteAllBytes(dxtPath, data);
                written.Add(dxtPath);
            }

            if (writeEtc2 && !string.IsNullOrEmpty(mobileDirectory))
            {
                int emips;
                byte[] echain = TtcMipChain.Build(flipped, TileSize, TileSize, 3, TtcFile.FormatEtc2,
                    out emips, 0, maxThreads);
                byte[] edata = TtcFile.BuildCompressed(level, TileSize, TileSize, emips, TtcFile.FormatEtc2,
                    echain, 0, 0);
                Directory.CreateDirectory(mobileDirectory);
                string etcPath = Path.Combine(mobileDirectory, name);
                File.WriteAllBytes(etcPath, edata);
                written.Add(etcPath);
            }

            if (wantMask && writeDxt1 && NeedsFs4Mask(covered, maskAlpha, allCovered))
            {
                byte[] mask = BuildMask(covered, maskAlpha);
                int mmips;
                byte[] mchain = TtcMipChain.Build(mask, MaskSize, MaskSize, 1, TtcFile.FormatL8, out mmips);
                byte[] mdata = TtcFile.BuildCompressed(level, MaskSize, MaskSize, mmips, TtcFile.FormatL8,
                    mchain, TtcFile.MaskUnk24, TtcFile.MaskUnk28);

                string mname = TtcTileName.ForTile(level, tileX, tileY, true);
                Directory.CreateDirectory(outputDirectory);
                string maskPath = Path.Combine(outputDirectory, mname);
                File.WriteAllBytes(maskPath, mdata);
                written.Add(maskPath);
            }

            return written;
        }

        private static void WriteRawPng(string rawDirectory, int level, int tileX, int tileY, byte[] rgb)
        {
            Directory.CreateDirectory(rawDirectory);
            string fileName = TtcTileName.ForTile(level, tileX, tileY).Replace(".ttc", ".png");
            string path = Path.Combine(rawDirectory, fileName);

            using (var bitmap = new Bitmap(TileSize, TileSize, PixelFormat.Format24bppRgb))
            {
                var data = bitmap.LockBits(
                    new Rectangle(0, 0, TileSize, TileSize),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format24bppRgb);

                int stride = data.Stride;
                var row = new byte[stride];
                for (int y = 0; y < TileSize; y++)
                {
                    int src = y * TileSize * 3;
                    for (int x = 0; x < TileSize; x++)
                    {
                        int d = x * 3;
                        int s = src + x * 3;
                        row[d] = rgb[s + 2];
                        row[d + 1] = rgb[s + 1];
                        row[d + 2] = rgb[s];
                    }

                    Marshal.Copy(row, 0, IntPtr.Add(data.Scan0, y * stride), stride);
                }

                bitmap.UnlockBits(data);
                bitmap.Save(path, ImageFormat.Png);
            }
        }

        /// <summary>
        /// GeoConvert writes a _mask.ttc only when something is missing: a PNG alpha hole, a
        /// coastline cut, or any other uncovered pixel. FSG never gets these files.
        /// </summary>
        private static bool NeedsFs4Mask(bool[] covered, byte[] maskAlpha, bool allCovered)
        {
            if (maskAlpha != null)
            {
                for (int i = 0; i < maskAlpha.Length; i++)
                {
                    if (maskAlpha[i] <= TtcTileWriter.MaskWhiteMinAlpha)
                    {
                        return true;
                    }
                }
                return false;
            }

            return !allCovered;
        }

        /// <summary>
        /// 512 R8 mask: average MaskAverageBlock x MaskAverageBlock inside each 4x4 cell,
        /// then threshold at MaskWhiteMinAlpha.
        /// </summary>
        private static byte[] BuildMask(bool[] covered, byte[] maskAlpha)
        {
            var mask = new byte[MaskSize * MaskSize];
            int step = TileSize / MaskSize;
            int block = MaskAverageBlock;
            if (block < 1)
            {
                block = 1;
            }
            if (block > step)
            {
                block = step;
            }
            int inset = (step - block) / 2;
            int samples = block * block;

            for (int my = 0; my < MaskSize; my++)
            {
                int srcYBase = TileSize - 1 - my * step;
                for (int mx = 0; mx < MaskSize; mx++)
                {
                    int sum = 0;
                    for (int dy = 0; dy < block; dy++)
                    {
                        int sy = srcYBase - (inset + dy);
                        int rowBase = sy * TileSize + mx * step + inset;
                        for (int dx = 0; dx < block; dx++)
                        {
                            int i = rowBase + dx;
                            byte p = maskAlpha != null
                                ? maskAlpha[i]
                                : (covered[i] ? (byte)255 : (byte)0);
                            sum += p;
                        }
                    }
                    mask[my * MaskSize + mx] = (sum / samples) > MaskWhiteMinAlpha ? (byte)255 : (byte)0;
                }
            }
            return mask;
        }

        public static byte[] FlipRows(byte[] src, int width, int height, int channels)
        {
            int stride = width * channels;
            var dst = new byte[(long)stride * height];
            for (int y = 0; y < height; y++)
            {
                Buffer.BlockCopy(src, y * stride, dst, (height - 1 - y) * stride, stride);
            }
            return dst;
        }
    }
}
