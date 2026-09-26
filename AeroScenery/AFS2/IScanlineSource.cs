using System;

namespace AeroScenery.AFS2
{
    /// <summary>
    /// One large source image opened for a single north-to-south pass.
    ///
    /// Stitched images reach ~800 MB and a gigapixel, and loading one whole costs about 2.8 GB -
    /// measured, against GDI+, on a real 16,384 square stitched PNG. Nothing above this interface
    /// may assume the image fits in memory.
    ///
    /// MEMORY GUARANTEE: an implementation holds O(width x band) bytes, never O(image). Measured at
    /// ~200 MB total for that same file through WicScanlineSource, against 2,853 MB for
    /// Image.FromFile.
    ///
    /// FORWARD ONLY. Row 0 is the top, which is the north edge, and rows already read are gone. PNG
    /// cannot seek: its filter chain makes every row depend on the one above it, so a decoder can
    /// only ever go forwards or start again. Reset() is that restart, and it is deliberately the
    /// only way back - an implementation must not quietly rewind, because a silent restart turns a
    /// cheap read into a full re-decode of everything skipped.
    ///
    /// Pixels come out as RGB24, one byte per channel, alpha dropped. That normalisation happens
    /// inside the implementation on purpose: the decoder's native order is BGRA, the rest of the
    /// converter and the Python reference both work in RGB, and a channel swap is invisible to
    /// every file-to-file comparison there is. Exactly one place decides channel order, and this
    /// interface is its boundary. See the row-order correction in docs/ttc-format.md for
    /// what this class of mistake costs when it is not contained.
    /// </summary>
    public interface IScanlineSource : IDisposable
    {
        int Width { get; }

        int Height { get; }

        /// <summary>The next row that will be read. 0 is the north edge.</summary>
        int NextRow { get; }

        /// <summary>
        /// Copies rowCount rows into destination at destOffset as RGB24, top to bottom, and
        /// advances NextRow. destination must hold rowCount * Width * 3 bytes from destOffset.
        ///
        /// The offset exists so a caller can keep the tail of the band it already has and read
        /// only what is new. Consecutive tile rows share their boundary row, so without it every
        /// row of tiles would ask for one row it had just passed, and each of those would cost a
        /// full restart.
        /// </summary>
        void ReadRows(byte[] destination, long destOffset, int rowCount);

        /// <summary>
        /// Advances NextRow without copying anything out. The decoder still has to work through the
        /// skipped rows when the next read happens - this saves the copy and the conversion, not
        /// the inflate.
        /// </summary>
        void SkipRows(int rowCount);

        /// <summary>
        /// Returns to row 0 by reopening the image. A restart, not a seek, and priced like one.
        /// </summary>
        void Reset();
    }
}
