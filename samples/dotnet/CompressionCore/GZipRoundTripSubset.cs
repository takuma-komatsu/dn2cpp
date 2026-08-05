#nullable disable
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace GZipRoundTripSubset
{
    // Same shape as DeflateRoundTripSubset, via GZipStream (adds the gzip
    // header/trailer framing around the same deflate payload format).
    internal static class Program
    {
        internal static void Run()
        {
            RoundTripAsciiText();
            RoundTripEmptyBuffer();
        }

        private static void RoundTripAsciiText()
        {
            byte[] original = Encoding.UTF8.GetBytes(
                "The quick brown fox jumps over the lazy dog. " +
                "The quick brown fox jumps over the lazy dog.");
            byte[] compressed = Compress(original);
            byte[] restored = Decompress(compressed);
            Console.WriteLine("gzip-ascii: origLen=" + original.Length
                + " restoredLen=" + restored.Length + " ok=" + BytesEqual(original, restored));
        }

        private static void RoundTripEmptyBuffer()
        {
            byte[] original = Array.Empty<byte>();
            byte[] compressed = Compress(original);
            byte[] restored = Decompress(compressed);
            Console.WriteLine("gzip-empty: restoredLen=" + restored.Length
                + " ok=" + BytesEqual(original, restored));
        }

        private static byte[] Compress(byte[] data)
        {
            using MemoryStream output = new MemoryStream();
            using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
            {
                gzip.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private static byte[] Decompress(byte[] compressed)
        {
            using MemoryStream input = new MemoryStream(compressed);
            using GZipStream gzip = new GZipStream(input, CompressionMode.Decompress);
            using MemoryStream output = new MemoryStream();
            gzip.CopyTo(output);
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
