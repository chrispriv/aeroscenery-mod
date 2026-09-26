using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace AeroScenery.AFS2
{
    /// <summary>
    /// Turns one sampled tile into the files Aerofly reads: the colour .ttc, and a _mask.ttc when
    /// the tile is not fully covered.
    ///
    /// Port of write_tile in tools/ttc/convert_tmc.py.
    /// </summary>
    public static class TtcTileWriter
    {
        public const int TileSize = 2048;
        public const int MaskSize = 512;

        /// <summary>
        /// Writes a tile and returns the file names produced, which is empty when no source
        /// reached it.
        ///
        /// A tile nothing covers is not a black tile, it is no tile. GeoConvert omits them and so
        /// must we: each would cost 2.8 MB and a full encode, and would black out terrain Aerofly
        /// would otherwise draw from its own imagery.
        /// </summary>
        public static List<string> Write(string outputDirectory, int level, int tileX, int tileY,
            byte[] rgb, bool[] covered, bool wantMask, int maxThreads = 1, string rawDirectory = null)
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
            // The DXT1 payload is flipped south-up after this.
            if (!string.IsNullOrEmpty(rawDirectory))
            {
                WriteRawPng(rawDirectory, level, tileX, tileY, rgb);
            }

            // Aerofly stores tile rows bottom-up: row 0 of the texture is the SOUTH edge.
            // Everything upstream works north-up, like the source and like GeoConvert's
            // write_raw_files dump, so the flip happens here, once, on the way out.
            //
            // This cost a whole debugging session. Comparing against GeoConvert's raw PNGs never
            // caught it, because those really are north-up - the flip lives between the raw dump
            // and the DXT1 payload. Nor can a symmetric test pattern: a checkerboard mirrors to a
            // checkerboard. An asymmetric one in the simulator can, and an upright F came back
            // mirrored. See section 3 of docs/ttc-format.md.
            var flipped = FlipRows(rgb, TileSize, TileSize, 3);

            int mips;
            byte[] chain = TtcMipChain.Build(flipped, TileSize, TileSize, 3, TtcFile.FormatDxt1,
                out mips, 0, maxThreads);
            byte[] data = TtcFile.BuildCompressed(level, TileSize, TileSize, mips, TtcFile.FormatDxt1, chain);

            string name = TtcTileName.ForTile(level, tileX, tileY);
            File.WriteAllBytes(Path.Combine(outputDirectory, name), data);
            written.Add(name);

            if (wantMask && !allCovered)
            {
                byte[] mask = BuildMask(covered);
                int mmips;
                byte[] mchain = TtcMipChain.Build(mask, MaskSize, MaskSize, 1, TtcFile.FormatL8, out mmips);
                byte[] mdata = TtcFile.BuildCompressed(level, MaskSize, MaskSize, mmips, TtcFile.FormatL8,
                    mchain, TtcFile.MaskUnk24, TtcFile.MaskUnk28);

                string mname = TtcTileName.ForTile(level, tileX, tileY, true);
                File.WriteAllBytes(Path.Combine(outputDirectory, mname), mdata);
                written.Add(mname);
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
        /// The mask is a quarter resolution and max-pooled, not averaged: a 4x4 source block counts
        /// as covered if any of its 16 pixels was. Averaging would erode coverage by up to two
        /// pixels at every edge of the imagery.
        ///
        /// Flipped here too, since it accompanies a flipped tile.
        /// </summary>
        private static byte[] BuildMask(bool[] covered)
        {
            var mask = new byte[MaskSize * MaskSize];
            int step = TileSize / MaskSize;

            for (int my = 0; my < MaskSize; my++)
            {
                // south edge first, matching the colour tile
                int srcYBase = TileSize - 1 - my * step;
                for (int mx = 0; mx < MaskSize; mx++)
                {
                    byte v = 0;
                    for (int dy = 0; dy < step && v == 0; dy++)
                    {
                        int sy = srcYBase - dy;
                        int rowBase = sy * TileSize + mx * step;
                        for (int dx = 0; dx < step; dx++)
                        {
                            if (covered[rowBase + dx])
                            {
                                v = 255;
                                break;
                            }
                        }
                    }
                    mask[my * MaskSize + mx] = v;
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
