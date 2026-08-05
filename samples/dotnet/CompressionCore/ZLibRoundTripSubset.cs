#nullable disable
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ZLibRoundTripSubset
{
    // ZLibStream (zlib format: 2-byte zlib header + deflate payload + Adler-32
    // trailer; public API since .NET 6) round-trips -- the third container
    // format over the same ZLibNative/windowBits machinery Deflate/GZip
    // already cover. Sections: basic round-trip, the CompressionLevel matrix,
    // format-header interop checks against DeflateStream/GZipStream, and
    // corrupted-header error paths.
    //
    // Determinism rules (same as the sibling subsets): never print compressed
    // bytes or CompressedLength -- real .NET (zlib-ng) and the vendored
    // classic zlib legitimately encode differently. The header checks below
    // are safe because they assert only format-spec-fixed facts: the zlib CMF
    // byte is 0x78 for any windowBits-15 encoder (CM=8, CINFO=7), the
    // (CMF<<8|FLG) % 31 == 0 FCHECK rule is part of RFC 1950, and gzip's
    // 0x1F 0x8B magic is fixed by RFC 1952. The FLG byte's FLEVEL bits are
    // informational and encoder-chosen, so it is never printed raw.
    internal static class Program
    {
        private static readonly CompressionLevel[] Levels =
        {
            CompressionLevel.Optimal,
            CompressionLevel.Fastest,
            CompressionLevel.NoCompression,
            CompressionLevel.SmallestSize,
        };

        internal static void Run()
        {
            RoundTripAsciiText();
            RoundTripEmptyBuffer();
            RoundTripLevels();
            HeaderInterop();
            ErrorPaths();
        }

        private static void RoundTripAsciiText()
        {
            byte[] original = Encoding.UTF8.GetBytes(
                "The quick brown fox jumps over the lazy dog. " +
                "The quick brown fox jumps over the lazy dog.");
            byte[] compressed = Compress(original);
            byte[] restored = Decompress(compressed);
            Console.WriteLine("zlib-ascii: origLen=" + original.Length
                + " restoredLen=" + restored.Length + " ok=" + BytesEqual(original, restored));
        }

        private static void RoundTripEmptyBuffer()
        {
            byte[] original = Array.Empty<byte>();
            byte[] compressed = Compress(original);
            byte[] restored = Decompress(compressed);
            Console.WriteLine("zlib-empty: restoredLen=" + restored.Length
                + " ok=" + BytesEqual(original, restored));
        }

        private static void RoundTripLevels()
        {
            byte[] data = BuildSampleBuffer();
            foreach (CompressionLevel level in Levels)
            {
                byte[] compressed = CompressLevel(data, level);
                byte[] restored = Decompress(compressed);
                Console.WriteLine("zlib-level/" + level + ": restoredLen=" + restored.Length
                    + " ok=" + BytesEqual(data, restored));
            }
        }

        // Format-header interop with the sibling stream types: a ZLibStream
        // payload starts with the zlib CMF byte 0x78 and satisfies the RFC 1950
        // FCHECK rule; it is NOT the gzip magic, and a GZipStream payload IS.
        private static void HeaderInterop()
        {
            byte[] data = Encoding.UTF8.GetBytes(
                "The quick brown fox jumps over the lazy dog.");

            byte[] zlibbed = Compress(data);
            bool zlibCmf = zlibbed.Length >= 2 && zlibbed[0] == 0x78;
            bool zlibFcheck = zlibbed.Length >= 2
                && ((zlibbed[0] << 8) | zlibbed[1]) % 31 == 0;
            bool zlibLooksGZip = zlibbed.Length >= 2
                && zlibbed[0] == 0x1F && zlibbed[1] == 0x8B;
            Console.WriteLine("zlib-header: cmf78=" + zlibCmf
                + " fcheck=" + zlibFcheck + " looksGZip=" + zlibLooksGZip);

            // Every CompressionLevel keeps the same format-fixed CMF byte.
            bool allLevelsCmf = true;
            foreach (CompressionLevel level in Levels)
            {
                byte[] compressed = CompressLevel(data, level);
                if (compressed.Length < 2 || compressed[0] != 0x78
                    || ((compressed[0] << 8) | compressed[1]) % 31 != 0)
                {
                    allLevelsCmf = false;
                }
            }
            Console.WriteLine("zlib-header-levels: allCmf78=" + allLevelsCmf);

            using MemoryStream gzOutput = new MemoryStream();
            using (GZipStream gzip = new GZipStream(gzOutput, CompressionMode.Compress, leaveOpen: true))
            {
                gzip.Write(data, 0, data.Length);
            }
            byte[] gzipped = gzOutput.ToArray();
            bool gzipMagic = gzipped.Length >= 2
                && gzipped[0] == 0x1F && gzipped[1] == 0x8B;
            bool gzipLooksZLib = gzipped.Length >= 1 && gzipped[0] == 0x78;
            Console.WriteLine("gzip-header: magic=" + gzipMagic
                + " looksZLib=" + gzipLooksZLib);

            // Cross-feeding gzip data to ZLibStream must fail the zlib header
            // check: gzip's fixed 0x1F first byte decodes as CM=15, which RFC
            // 1950 does not define -- spec-governed regardless of which zlib
            // produced the gzip bytes, so safe to assert despite being derived
            // from this program's own compressed output.
            try
            {
                using MemoryStream input = new MemoryStream(gzipped);
                using ZLibStream zlib = new ZLibStream(input, CompressionMode.Decompress);
                using MemoryStream output = new MemoryStream();
                zlib.CopyTo(output);
                Console.WriteLine("zlib-reads-gzip: threw=False");
            }
            catch (Exception ex)
            {
                Console.WriteLine("zlib-reads-gzip: threw=True type=" + ex.GetType().Name);
            }
        }

        // Literal constant corrupted buffers (never derived from this
        // program's own compressed output -- see CompressionErrorPathsSubset's
        // rationale): outcomes are governed purely by the RFC 1950 header
        // rules, not by any encoder's internal choices.
        private static void ErrorPaths()
        {
            // CM = 0xA (from 0xAA & 0x0F) is not a defined zlib compression
            // method; fails before any deflate decoding starts.
            ExpectThrow("zlib-invalid-cm", Fill(32, 0xAA));

            // CMF is valid (0x78) but FLG=0x00 breaks the FCHECK rule:
            // 0x7800 % 31 == 30, not 0.
            ExpectThrow("zlib-invalid-fcheck", new byte[]
                { 0x78, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x01 });
        }

        private static byte[] Fill(int length, byte value)
        {
            byte[] buffer = new byte[length];
            for (int i = 0; i < length; i++)
            {
                buffer[i] = value;
            }
            return buffer;
        }

        private static void ExpectThrow(string label, byte[] corrupted)
        {
            try
            {
                using MemoryStream input = new MemoryStream(corrupted);
                using ZLibStream zlib = new ZLibStream(input, CompressionMode.Decompress);
                using MemoryStream output = new MemoryStream();
                zlib.CopyTo(output);
                Console.WriteLine(label + ": threw=False");
            }
            catch (Exception ex)
            {
                Console.WriteLine(label + ": threw=True type=" + ex.GetType().Name);
            }
        }

        private static byte[] BuildSampleBuffer()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 50; i++)
            {
                sb.Append("The quick brown fox jumps over the lazy dog. ");
            }
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static byte[] Compress(byte[] data)
        {
            using MemoryStream output = new MemoryStream();
            using (ZLibStream zlib = new ZLibStream(output, CompressionMode.Compress, leaveOpen: true))
            {
                zlib.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private static byte[] CompressLevel(byte[] data, CompressionLevel level)
        {
            using MemoryStream output = new MemoryStream();
            using (ZLibStream zlib = new ZLibStream(output, level, leaveOpen: true))
            {
                zlib.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private static byte[] Decompress(byte[] compressed)
        {
            using MemoryStream input = new MemoryStream(compressed);
            using ZLibStream zlib = new ZLibStream(input, CompressionMode.Decompress);
            using MemoryStream output = new MemoryStream();
            zlib.CopyTo(output);
            return output.ToArray();
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }
    }
}
