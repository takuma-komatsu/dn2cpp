#nullable disable
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace DeflateRoundTripSubset
{
    // Baseline smoke test: DeflateStream compress/decompress round-trip via the
    // CompressionMode constructor overload. Prints only implementation-invariant
    // facts (lengths, round-trip equality) -- never the compressed bytes or their
    // exact length, since the compressed encoding is implementation-defined (our
    // vendored zlib can legitimately shape it differently than real .NET's while
    // both still round-trip correctly).
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
            Console.WriteLine("deflate-ascii: origLen=" + original.Length
                + " restoredLen=" + restored.Length + " ok=" + BytesEqual(original, restored));
        }

        private static void RoundTripEmptyBuffer()
        {
            byte[] original = Array.Empty<byte>();
            byte[] compressed = Compress(original);
            byte[] restored = Decompress(compressed);
            Console.WriteLine("deflate-empty: restoredLen=" + restored.Length
                + " ok=" + BytesEqual(original, restored));
        }

        private static byte[] Compress(byte[] data)
        {
            using MemoryStream output = new MemoryStream();
            using (DeflateStream deflate = new DeflateStream(output, CompressionMode.Compress, leaveOpen: true))
            {
                deflate.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private static byte[] Decompress(byte[] compressed)
        {
            using MemoryStream input = new MemoryStream(compressed);
            using DeflateStream deflate = new DeflateStream(input, CompressionMode.Decompress);
            using MemoryStream output = new MemoryStream();
            deflate.CopyTo(output);
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
