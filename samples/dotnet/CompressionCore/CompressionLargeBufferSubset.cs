#nullable disable
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CompressionLargeBufferSubset
{
    // A buffer large enough to span multiple of DeflateStream's internal
    // ~8192-byte buffer refills on both the compress and decompress paths.
    // Deterministic xorshift32 fill (not real randomness) behind a short
    // readable prefix: mostly incompressible, so the compressed size stays
    // close to the input size and comfortably exceeds one internal buffer's
    // worth of data on every CompressionLevel. Fixed seed -> byte-identical
    // input on every run, so the transpiled binary and real .NET compress the
    // exact same bytes.
    internal static class Program
    {
        internal static void Run()
        {
            byte[] large = BuildLargeBuffer(65536);

            byte[] deflated = CompressDeflate(large);
            byte[] inflated = DecompressDeflate(deflated);
            Console.WriteLine("large-deflate: origLen=" + large.Length
                + " restoredLen=" + inflated.Length + " ok=" + BytesEqual(large, inflated));

            byte[] gzipped = CompressGZip(large);
            byte[] gunzipped = DecompressGZip(gzipped);
            Console.WriteLine("large-gzip: origLen=" + large.Length
                + " restoredLen=" + gunzipped.Length + " ok=" + BytesEqual(large, gunzipped));
        }

        private static byte[] BuildLargeBuffer(int length)
        {
            byte[] buffer = new byte[length];
            byte[] prefix = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog. ");
            uint state = 0x9E3779B9u;
            for (int i = 0; i < length; i++)
            {
                if (i < prefix.Length)
                {
                    buffer[i] = prefix[i];
                    continue;
                }
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                buffer[i] = (byte)state;
            }
            return buffer;
        }

        private static byte[] CompressDeflate(byte[] data)
        {
            using MemoryStream output = new MemoryStream();
            using (DeflateStream deflate = new DeflateStream(output, CompressionMode.Compress, leaveOpen: true))
            {
                deflate.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private static byte[] DecompressDeflate(byte[] compressed)
        {
            using MemoryStream input = new MemoryStream(compressed);
            using DeflateStream deflate = new DeflateStream(input, CompressionMode.Decompress);
            using MemoryStream output = new MemoryStream();
            deflate.CopyTo(output);
            return output.ToArray();
        }

        private static byte[] CompressGZip(byte[] data)
        {
            using MemoryStream output = new MemoryStream();
            using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
            {
                gzip.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private static byte[] DecompressGZip(byte[] compressed)
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
