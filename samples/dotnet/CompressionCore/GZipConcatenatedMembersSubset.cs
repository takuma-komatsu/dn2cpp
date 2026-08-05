#nullable disable
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace GZipConcatenatedMembersSubset
{
    // Two independently-gzip-compressed members concatenated into one byte
    // buffer, decompressed through a single GZipStream/CopyTo read. Confirms
    // the full original content of BOTH members comes back. This is the one
    // path that reaches Inflater.ResetStreamForLeftoverInput (it only fires
    // when there is leftover input after a member ends), which none of the
    // other sections in this bucket reach.
    internal static class Program
    {
        internal static void Run()
        {
            byte[] member1 = Encoding.UTF8.GetBytes("Hello, ");
            byte[] member2 = Encoding.UTF8.GetBytes("World! This is the second gzip member.");
            string expected = "Hello, World! This is the second gzip member.";

            byte[] gz1 = Compress(member1);
            byte[] gz2 = Compress(member2);
            byte[] combined = new byte[gz1.Length + gz2.Length];
            Array.Copy(gz1, 0, combined, 0, gz1.Length);
            Array.Copy(gz2, 0, combined, gz1.Length, gz2.Length);

            string actual = DecompressToString(combined);
            Console.WriteLine("gzip-concat-members: restoredLen=" + actual.Length
                + " ok=" + (actual == expected));
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

        private static string DecompressToString(byte[] compressed)
        {
            using MemoryStream input = new MemoryStream(compressed);
            using GZipStream gzip = new GZipStream(input, CompressionMode.Decompress);
            using MemoryStream output = new MemoryStream();
            gzip.CopyTo(output);
            return Encoding.UTF8.GetString(output.ToArray());
        }
    }
}
