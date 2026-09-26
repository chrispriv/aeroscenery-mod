using System;

namespace AeroScenery.AFS2
{
    /// <summary>
    /// Builds the concatenated mip chain a .ttc carries as its payload. Ported from
    /// build_mip_chain / _halve in tools/ttc/ttc.py.
    ///
    /// Level 0 comes first and there are no per-level headers - the levels are simply written one
    /// after another, which is why MipChainSize can predict the total exactly. Each level is a box
    /// filter of the one above it, halved in both axes with round-to-nearest.
    /// </summary>
    public static class TtcMipChain
    {
        /// <summary>
        /// Encodes an image and its whole mip pyramid into one buffer.
        /// </summary>
        /// <param name="pixels">Row-major image, channels bytes per pixel.</param>
        /// <param name="channels">3 for RGB (encoded as BC1), 1 for L8 (stored raw).</param>
        /// <param name="format">TtcFile.FormatDxt1 or TtcFile.FormatL8.</param>
        /// <param name="numMips">Levels to emit, or 0 for the full chain down to 1x1.</param>
        public static byte[] Build(byte[] pixels, int width, int height, int channels, uint format,
            out int numMips, int requestedMips = 0, int maxThreads = 1)
        {
            numMips = requestedMips > 0 ? requestedMips : TtcFile.FullMipCount(width, height);

            var chain = new byte[TtcFile.MipChainSize(width, height, numMips, format)];
            int offset = 0;

            byte[] cur = pixels;
            int cw = width, ch = height;

            for (int i = 0; i < numMips; i++)
            {
                byte[] levelBytes = format == TtcFile.FormatL8
                    ? cur
                    : Bc1Encoder.Encode(cur, cw, ch, maxThreads);

                Buffer.BlockCopy(levelBytes, 0, chain, offset, levelBytes.Length);
                offset += levelBytes.Length;

                if (i + 1 < numMips)
                {
                    int nw, nh;
                    cur = Halve(cur, cw, ch, channels, out nw, out nh);
                    cw = nw;
                    ch = nh;
                }
            }

            if (offset != chain.Length)
            {
                // The predicted size and what we wrote must agree exactly, or the header's
                // size fields would lie about the payload and the engine would read garbage.
                throw new InvalidOperationException(String.Format(
                    "mip chain wrote {0} bytes, expected {1}", offset, chain.Length));
            }
            return chain;
        }

        /// <summary>
        /// Box filter to half size, round to nearest. Matches _halve in the reference, including
        /// the odd-size behaviour: the last row or column is dropped rather than weighted.
        /// </summary>
        public static byte[] Halve(byte[] src, int width, int height, int channels,
            out int newWidth, out int newHeight)
        {
            newWidth = Math.Max(1, width / 2);
            newHeight = Math.Max(1, height / 2);

            if (width < 2 || height < 2)
            {
                // Degenerate level: crop rather than average, as the reference does
                var crop = new byte[newWidth * newHeight * channels];
                for (int y = 0; y < newHeight; y++)
                {
                    Buffer.BlockCopy(src, y * width * channels, crop,
                        y * newWidth * channels, newWidth * channels);
                }
                return crop;
            }

            var dst = new byte[newWidth * newHeight * channels];
            for (int y = 0; y < newHeight; y++)
            {
                int r0 = (2 * y) * width * channels;
                int r1 = (2 * y + 1) * width * channels;
                int o = y * newWidth * channels;

                for (int x = 0; x < newWidth; x++)
                {
                    int c0 = (2 * x) * channels;
                    int c1 = (2 * x + 1) * channels;

                    for (int c = 0; c < channels; c++)
                    {
                        int sum = src[r0 + c0 + c] + src[r0 + c1 + c]
                                + src[r1 + c0 + c] + src[r1 + c1 + c];
                        dst[o + x * channels + c] = (byte)((sum + 2) / 4);
                    }
                }
            }
            return dst;
        }
    }
}
