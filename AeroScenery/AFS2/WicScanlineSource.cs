using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AeroScenery.AFS2
{
    /// <summary>
    /// An IScanlineSource backed by WIC (Windows Imaging Component), which is the PNG decoder
    /// already sitting in Windows behind WPF.
    ///
    /// Why WIC rather than GDI+ or a managed decoder, measured on a real 876 MB / 16,384 square
    /// stitched PNG, reading north to south in bands of 2,048 rows:
    ///
    ///     GDI+ Image.FromFile      3.6 s   2,853 MB peak   loads the whole image
    ///     WIC banded CopyPixels    3.4 s     201 MB peak   incremental, no frame cache
    ///     managed scanline reader  6.3 s      75 MB peak
    ///
    /// So WIC is both the fastest and within a rounding error of the cheapest, and it costs no new
    /// dependency: PresentationCore and WindowsBase are framework assemblies, and the app already
    /// pulls WPF in through GMap.NET.WindowsPresentation.
    ///
    /// The one rule is that reads must move forwards. BitmapCacheOption.None is what keeps the
    /// frame from being materialised, and it also means the decoder holds live zlib state that only
    /// runs one way; asking for a rectangle above the cursor makes WIC silently restart and
    /// re-inflate from the beginning. That is why this class tracks the cursor itself and refuses
    /// to go backwards except through Reset.
    ///
    /// THREAD AFFINITY: BitmapDecoder and BitmapFrame are DispatcherObjects and a frame created
    /// with CacheOption.None cannot be frozen, so one instance belongs to one thread for its whole
    /// life. Parallelise across source files, never within one - PNG inflate is serial per file
    /// anyway, and one decode thread already runs at roughly 300 MB/s.
    /// </summary>
    public class WicScanlineSource : IScanlineSource
    {
        /// <summary>
        /// Ceiling on how much the internal native-format buffer may take, so that a caller asking
        /// for a huge band does not make us allocate a huge scratch. Reads are chunked to fit.
        /// </summary>
        private const int MaxScratchBytes = 32 * 1024 * 1024;

        private readonly string path;

        private FileStream stream;
        private BitmapSource frame;
        private int bytesPerPixel;
        private PixelFormat format;

        /// <summary>Reused across reads. Large-object-heap sized, and the LOH never compacts.</summary>
        private byte[] scratch;
        private int scratchRows;

        public int Width { get; private set; }

        public int Height { get; private set; }

        public int NextRow { get; private set; }

        public WicScanlineSource(string path)
        {
            this.path = path;
            Open();
        }

        private void Open()
        {
            // CacheOption.None makes WIC read lazily, which is the whole point - and means the
            // stream has to stay open for as long as the decoder lives.
            stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            var decoder = BitmapDecoder.Create(stream,
                BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile,
                BitmapCacheOption.None);

            BitmapSource f = decoder.Frames[0];

            // Branch on the pixel format exactly once, here. Everything above this class is RGB24.
            // GDI+ writes these files as 32bpp ARGB, which WIC reports as Bgra32, but a source that
            // came from somewhere else can be anything - so convert rather than assume. A
            // FormatConvertedBitmap is itself lazy, so this does not defeat the streaming.
            if (f.Format != PixelFormats.Bgra32 && f.Format != PixelFormats.Bgr32
                && f.Format != PixelFormats.Bgr24 && f.Format != PixelFormats.Rgb24)
            {
                f = new FormatConvertedBitmap(f, PixelFormats.Bgra32, null, 0);
            }

            frame = f;
            format = f.Format;
            bytesPerPixel = (f.Format.BitsPerPixel + 7) / 8;

            Width = f.PixelWidth;
            Height = f.PixelHeight;
            NextRow = 0;

            if (Width <= 0 || Height <= 0)
            {
                throw new InvalidDataException("source image has no pixels: " + path);
            }
        }

        public void ReadRows(byte[] destination, long destOffset, int rowCount)
        {
            if (rowCount <= 0)
            {
                return;
            }
            if (NextRow + rowCount > Height)
            {
                throw new ArgumentOutOfRangeException("rowCount", String.Format(
                    "asked for rows {0}..{1} of an image {2} rows tall",
                    NextRow, NextRow + rowCount - 1, Height));
            }
            if (destOffset < 0 || destOffset + (long)rowCount * Width * 3 > destination.Length)
            {
                throw new ArgumentException("destination is too small for " + rowCount
                    + " rows at offset " + destOffset, "destination");
            }

            int srcStride = Width * bytesPerPixel;
            int chunkRows = Math.Max(1, MaxScratchBytes / Math.Max(1, srcStride));
            EnsureScratch(Math.Min(chunkRows, rowCount), srcStride);

            int done = 0;
            while (done < rowCount)
            {
                int rows = Math.Min(chunkRows, rowCount - done);

                frame.CopyPixels(new Int32Rect(0, NextRow, Width, rows),
                    scratch, srcStride, 0);

                Convert(scratch, srcStride, destination, destOffset + (long)done * Width * 3, rows);

                NextRow += rows;
                done += rows;
            }
        }

        public void SkipRows(int rowCount)
        {
            if (rowCount <= 0)
            {
                return;
            }
            if (NextRow + rowCount > Height)
            {
                throw new ArgumentOutOfRangeException("rowCount");
            }

            // Only the cursor moves. WIC still inflates what was skipped when the next read comes,
            // because PNG has no way to jump - this saves the copy and the conversion, nothing more.
            NextRow += rowCount;
        }

        public void Reset()
        {
            if (NextRow == 0)
            {
                return;
            }
            Close();
            Open();
        }

        private void EnsureScratch(int rows, int srcStride)
        {
            long need = (long)rows * srcStride;
            if (scratch == null || scratch.Length < need)
            {
                scratch = new byte[need];
                scratchRows = rows;
            }
        }

        /// <summary>
        /// The single place channel order is decided. Everything above is RGB24 with no alpha.
        ///
        /// Dropping alpha rather than compositing matches the Python reference, whose Source.load
        /// does im.convert('RGB'). It matters because the stitcher leaves untouched areas
        /// transparent, and GDI+ writes those as zeroes, so a dropped alpha and a composite over
        /// black give the same pixels. Which parts of a tile are really covered is decided from the
        /// grid, not from alpha - see the mask in the converter.
        /// </summary>
        private void Convert(byte[] src, int srcStride, byte[] dst, long dstOffset, int rows)
        {
            bool bgr = format != PixelFormats.Rgb24;
            int bpp = bytesPerPixel;

            for (int y = 0; y < rows; y++)
            {
                int s = y * srcStride;
                long d = dstOffset + (long)y * Width * 3;

                if (bgr)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        dst[d] = src[s + 2];
                        dst[d + 1] = src[s + 1];
                        dst[d + 2] = src[s];
                        s += bpp;
                        d += 3;
                    }
                }
                else
                {
                    for (int x = 0; x < Width; x++)
                    {
                        dst[d] = src[s];
                        dst[d + 1] = src[s + 1];
                        dst[d + 2] = src[s + 2];
                        s += bpp;
                        d += 3;
                    }
                }
            }
        }

        private void Close()
        {
            frame = null;
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }

        public void Dispose()
        {
            Close();
            scratch = null;
        }
    }
}
