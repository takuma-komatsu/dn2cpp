#nullable disable
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CompressionLevelsSubset
{
    // Exercises all four CompressionLevel values via the CompressionLevel-taking
    // constructor overload, on both DeflateStream and GZipStream.
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
            byte[] data = BuildSampleBuffer();
            foreach (CompressionLevel level in Levels)
            {
                byte[] deflated = CompressDeflate(data, level);
                byte[] inflated = DecompressDeflate(deflated);
                Console.WriteLine("level-deflate/" + level + ": restoredLen=" + inflated.Length
                    + " ok=" + BytesEqual(data, inflated));

                byte[] gzipped = CompressGZip(data, level);
                byte[] gunzipped = DecompressGZip(gzipped);
                Console.WriteLine("level-gzip/" + level + ": restoredLen=" + gunzipped.Length
                    + " ok=" + BytesEqual(data, gunzipped));
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

        private static byte[] CompressDeflate(byte[] data, CompressionLevel level)
        {
            using MemoryStream output = new MemoryStream();
            using (DeflateStream deflate = new DeflateStream(output, level, leaveOpen: true))
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

        private static byte[] CompressGZip(byte[] data, CompressionLevel level)
        {
            using MemoryStream output = new MemoryStream();
            using (GZipStream gzip = new GZipStream(output, level, leaveOpen: true))
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
