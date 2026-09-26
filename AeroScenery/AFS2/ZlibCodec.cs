using System;
using System.IO;
using System.IO.Compression;

namespace AeroScenery.AFS2
{
    /// <summary>
    /// RFC 1950 zlib wrapper around a raw DEFLATE stream. Aerofly FS4 and GeoConvert-style
    /// zlib .ttc files put this stream at offset 0x100 with no tmcompress chunk.
    /// CMF/FLG 0x78 0x01 matches a known-good FS4 tile (32K window, FLEVEL fastest).
    /// </summary>
    internal static class ZlibCodec
    {
        public static bool TryCompress(byte[] source, out byte[] compressed)
        {
            compressed = null;
            if (source == null || source.Length == 0)
            {
                return false;
            }

            byte[] deflate;
            using (var deflateMs = new MemoryStream())
            {
                using (var ds = new DeflateStream(deflateMs, CompressionLevel.Fastest, true))
                {
                    ds.Write(source, 0, source.Length);
                }
                deflate = deflateMs.ToArray();
            }

            uint adler = Adler32(source);
            var wrapped = new byte[2 + deflate.Length + 4];
            wrapped[0] = 0x78;
            wrapped[1] = 0x01;
            Buffer.BlockCopy(deflate, 0, wrapped, 2, deflate.Length);
            int o = 2 + deflate.Length;
            wrapped[o] = (byte)(adler >> 24);
            wrapped[o + 1] = (byte)(adler >> 16);
            wrapped[o + 2] = (byte)(adler >> 8);
            wrapped[o + 3] = (byte)adler;

            if (wrapped.Length >= source.Length)
            {
                return false;
            }

            compressed = wrapped;
            return true;
        }

        private static uint Adler32(byte[] data)
        {
            const uint ModAdler = 65521;
            uint s1 = 1;
            uint s2 = 0;
            int i = 0;
            int n = data.Length;
            while (i < n)
            {
                int end = i + Math.Min(n - i, 5552);
                for (; i < end; i++)
                {
                    s1 += data[i];
                    s2 += s1;
                }
                s1 %= ModAdler;
                s2 %= ModAdler;
            }
            return (s2 << 16) | s1;
        }
    }
}
