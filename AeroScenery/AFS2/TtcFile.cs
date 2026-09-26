using System;
using System.IO;

namespace AeroScenery.AFS2
{
    /// <summary>
    /// Reader and writer for Aerofly .ttc colormap tiles.
    ///
    /// Ported from tools/ttc/ttc.py, which was derived by black-box analysis of GeoConvert 1.4.5
    /// output. Every constant here was read off real files, not guessed. The full spec and how each
    /// field was verified is in docs/ttc-format.md - read that before changing anything.
    ///
    /// Layout
    ///   0x000  u32  magic              0x0000303A
    ///   0x004  u32  version            0x00000100
    ///   0x008  u32  level
    ///   0x00C  u32  size_compressed    bytes after the header
    ///   0x010  u32  size_uncompressed
    ///   0x014  u32  width
    ///   0x018  u32  height
    ///   0x01C  u32  num_mips
    ///   0x020  u32  format             0 = L8, 10 = DXT1/BC1
    ///   0x024  u32  unknown            FFFFFFFF colour / 00FFFFFF mask
    ///   0x028  u32  unknown            FFFFFFFF colour / 00000000 mask
    ///   0x02C..0x0FF  zero padding
    ///   0x100  payload: RFC 1950 zlib (CMF/FLG 78 01), a tmcompress chunk, or the raw mip chain
    ///
    /// FS4 loads a zlib stream sitting straight at 0x100 (no tmcompress wrapper). That is what
    /// BuildCompressed writes. BuildStored writes the raw mip chain when zlib does not shrink the
    /// tile. There is no extra checksum outside the zlib Adler-32.
    /// </summary>
    public static class TtcFile
    {
        public const uint Magic = 0x0000303A;
        public const uint Version = 0x00000100;
        public const int HeaderLength = 0x100;

        public const uint FormatL8 = 0;
        public const uint FormatDxt1 = 10;

        // Sentinels, not texture types: the payload is a Basis Universal file rather than a raw mip
        // chain. ...71 is stored, ...72 is wrapped in a chunk. Over half of a real Aerofly install is
        // Basis, so a reader must expect them. We read them only to identify; we never write them.
        public const uint FormatBasisStored = 0x12345671;
        public const uint FormatBasisChunked = 0x12345672;

        public const int ChunkHeaderLength = 0x40;
        public const uint ChunkMagic1 = 0xA810BEF4;
        public const ulong ChunkMagic2 = 0x17F34DF32797945CUL;
        public const uint ChunkParamA = 21;
        public const uint ChunkParamB = 20;

        public const uint DefaultUnk24 = 0xFFFFFFFF;
        public const uint DefaultUnk28 = 0xFFFFFFFF;
        public const uint MaskUnk24 = 0x00FFFFFF;
        public const uint MaskUnk28 = 0x00000000;

        /// <summary>Bytes per BC1 block. L8 is a byte per pixel and has no blocks.</summary>
        private const int Dxt1BytesPerBlock = 8;

        /// <summary>Total bytes of a concatenated mip chain. No per-level headers.</summary>
        public static int MipChainSize(int width, int height, int numMips, uint format)
        {
            int total = 0;
            for (int i = 0; i < numMips; i++)
            {
                int w = Math.Max(1, width >> i);
                int h = Math.Max(1, height >> i);
                total += format == FormatL8
                    ? w * h
                    : Math.Max(1, (w + 3) / 4) * Math.Max(1, (h + 3) / 4) * Dxt1BytesPerBlock;
            }
            return total;
        }

        /// <summary>Number of mip levels down to 1x1, i.e. bit length of the larger side.</summary>
        public static int FullMipCount(int width, int height)
        {
            int v = Math.Max(width, height);
            int bits = 0;
            while (v > 0) { bits++; v >>= 1; }
            return bits;
        }

        /// <summary>
        /// Assembles a .ttc whose payload is stored, not compressed. The chain goes straight at
        /// 0x100 and both size fields carry its length, which is how the engine is told there is
        /// nothing to decompress. Used when zlib does not shrink the tile.
        /// </summary>
        public static byte[] BuildStored(int level, int width, int height, int numMips, uint format,
            byte[] chain, uint unk24 = DefaultUnk24, uint unk28 = DefaultUnk28)
        {
            var data = new byte[HeaderLength + chain.Length];
            WriteHeader(data, level, width, height, numMips, format,
                (uint)chain.Length, (uint)chain.Length, unk24, unk28);
            Buffer.BlockCopy(chain, 0, data, HeaderLength, chain.Length);
            return data;
        }

        /// <summary>
        /// Assembles a .ttc whose payload is a zlib stream at 0x100, matching FS4-readable tiles.
        /// Falls back to <see cref="BuildStored"/> if compression fails or does not shrink the tile.
        /// </summary>
        public static byte[] BuildCompressed(int level, int width, int height, int numMips, uint format,
            byte[] chain, uint unk24 = DefaultUnk24, uint unk28 = DefaultUnk28)
        {
            byte[] packed;
            if (!ZlibCodec.TryCompress(chain, out packed))
            {
                return BuildStored(level, width, height, numMips, format, chain, unk24, unk28);
            }

            var data = new byte[HeaderLength + packed.Length];
            WriteHeader(data, level, width, height, numMips, format,
                (uint)packed.Length, (uint)chain.Length, unk24, unk28);
            Buffer.BlockCopy(packed, 0, data, HeaderLength, packed.Length);
            return data;
        }

        /// <summary>
        /// Wraps an already-compressed payload in a tmcompress chunk.
        /// </summary>
        public static byte[] BuildChunk(byte[] payload, long sizeUncompressed)
        {
            var chunk = new byte[ChunkHeaderLength + payload.Length];
            WriteU32(chunk, 0x00, ChunkHeaderLength);
            WriteU32(chunk, 0x04, ChunkMagic1);
            WriteU64(chunk, 0x08, (ulong)sizeUncompressed);
            WriteU64(chunk, 0x10, (ulong)chunk.Length);
            WriteU64(chunk, 0x28, ChunkMagic2);
            WriteU32(chunk, 0x30, ChunkParamA);
            WriteU32(chunk, 0x34, ChunkParamB);
            Buffer.BlockCopy(payload, 0, chunk, ChunkHeaderLength, payload.Length);
            return chunk;
        }

        /// <summary>Assembles a complete .ttc from a finished tmcompress chunk.</summary>
        public static byte[] Build(int level, int width, int height, int numMips, uint format,
            byte[] chunk, long sizeUncompressed, uint unk24 = DefaultUnk24, uint unk28 = DefaultUnk28)
        {
            var data = new byte[HeaderLength + chunk.Length];
            WriteHeader(data, level, width, height, numMips, format,
                (uint)chunk.Length, (uint)sizeUncompressed, unk24, unk28);
            Buffer.BlockCopy(chunk, 0, data, HeaderLength, chunk.Length);
            return data;
        }

        private static void WriteHeader(byte[] data, int level, int width, int height, int numMips,
            uint format, uint sizeCompressed, uint sizeUncompressed, uint unk24, uint unk28)
        {
            WriteU32(data, 0x00, Magic);
            WriteU32(data, 0x04, Version);
            WriteU32(data, 0x08, (uint)level);
            WriteU32(data, 0x0C, sizeCompressed);
            WriteU32(data, 0x10, sizeUncompressed);
            WriteU32(data, 0x14, (uint)width);
            WriteU32(data, 0x18, (uint)height);
            WriteU32(data, 0x1C, (uint)numMips);
            WriteU32(data, 0x20, format);
            WriteU32(data, 0x24, unk24);
            WriteU32(data, 0x28, unk28);
            // 0x2C..0xFF stays zero
        }

        /// <summary>Parses a .ttc header and locates its payload.</summary>
        public static TtcHeader Read(byte[] data)
        {
            uint magic = ReadU32(data, 0x00);
            if (magic != Magic)
            {
                throw new InvalidDataException(String.Format("bad ttc magic 0x{0:x8}", magic));
            }

            var h = new TtcHeader
            {
                Magic = magic,
                Version = ReadU32(data, 0x04),
                Level = (int)ReadU32(data, 0x08),
                SizeCompressed = ReadU32(data, 0x0C),
                SizeUncompressed = ReadU32(data, 0x10),
                Width = (int)ReadU32(data, 0x14),
                Height = (int)ReadU32(data, 0x18),
                NumMips = (int)ReadU32(data, 0x1C),
                Format = ReadU32(data, 0x20),
                Unk24 = ReadU32(data, 0x24),
                Unk28 = ReadU32(data, 0x28),
                PayloadOffset = HeaderLength
            };

            // A payload is stored unless the chunk magic is there. Do not treat stored as an
            // error: IPACS ship 59,637 such files themselves.
            uint chunkMagic = h.SizeCompressed >= 8 ? ReadU32(data, HeaderLength + 4) : 0;
            if (chunkMagic != ChunkMagic1)
            {
                h.Stored = true;
                h.CodecOffset = HeaderLength;
                return h;
            }

            h.Stored = false;
            h.ChunkHeaderLength = (int)ReadU32(data, HeaderLength + 0x00);
            h.ChunkSizeUncompressed = (long)ReadU64(data, HeaderLength + 0x08);
            h.ChunkSizeTotal = (long)ReadU64(data, HeaderLength + 0x10);
            h.ChunkMagic2 = ReadU64(data, HeaderLength + 0x28);
            h.ChunkParamA = ReadU32(data, HeaderLength + 0x30);
            h.ChunkParamB = ReadU32(data, HeaderLength + 0x34);
            h.CodecOffset = HeaderLength + h.ChunkHeaderLength;
            return h;
        }

        private static void WriteU32(byte[] b, int o, uint v)
        {
            b[o] = (byte)v; b[o + 1] = (byte)(v >> 8); b[o + 2] = (byte)(v >> 16); b[o + 3] = (byte)(v >> 24);
        }

        private static void WriteU64(byte[] b, int o, ulong v)
        {
            for (int i = 0; i < 8; i++) b[o + i] = (byte)(v >> (8 * i));
        }

        private static uint ReadU32(byte[] b, int o)
        {
            return (uint)(b[o] | (b[o + 1] << 8) | (b[o + 2] << 16) | (b[o + 3] << 24));
        }

        private static ulong ReadU64(byte[] b, int o)
        {
            ulong v = 0;
            for (int i = 7; i >= 0; i--) v = (v << 8) | b[o + i];
            return v;
        }
    }

    /// <summary>Parsed .ttc header. Chunk fields are only meaningful when Stored is false.</summary>
    public class TtcHeader
    {
        public uint Magic { get; set; }
        public uint Version { get; set; }
        public int Level { get; set; }
        public uint SizeCompressed { get; set; }
        public uint SizeUncompressed { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int NumMips { get; set; }
        public uint Format { get; set; }
        public uint Unk24 { get; set; }
        public uint Unk28 { get; set; }

        /// <summary>True when the mip chain sits straight at 0x100 with no tmcompress chunk.</summary>
        public bool Stored { get; set; }

        /// <summary>Offset of the payload, i.e. the chunk if there is one. Always 0x100.</summary>
        public int PayloadOffset { get; set; }

        /// <summary>Offset of the codec data, past the chunk header if there is one.</summary>
        public int CodecOffset { get; set; }

        public int ChunkHeaderLength { get; set; }
        public long ChunkSizeUncompressed { get; set; }
        public long ChunkSizeTotal { get; set; }
        public ulong ChunkMagic2 { get; set; }
        public uint ChunkParamA { get; set; }
        public uint ChunkParamB { get; set; }
    }
}
