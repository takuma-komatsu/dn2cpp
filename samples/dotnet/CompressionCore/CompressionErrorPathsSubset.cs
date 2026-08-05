#nullable disable
using System;
using System.IO;
using System.IO.Compression;

namespace CompressionErrorPathsSubset
{
    // Deliberately corrupted compressed bytes fed to DeflateStream/GZipStream
    // decompression, confirming an exception IS thrown. Prints only the
    // exception's type name -- never its message text, since exact wording can
    // legitimately differ from real .NET's own phrasing without that being a
    // real bug. The point is confirming the transpiled code detects the
    // corruption and throws, not matching an exact string.
    //
    // Every corrupted buffer below is a literal byte-array constant -- NOT
    // derived by truncating/flipping a byte of this program's own compressed
    // output. That distinction matters: the compressed *encoding* DEFLATE
    // produces is implementation-defined (our vendored zlib can legitimately
    // shape it differently than real .NET's), so mutating a real compressed
    // buffer risks corrupting a different logical position in each
    // implementation's own byte layout -- an implementation difference, not a
    // transpiler bug, and confirmed (empirically, against real .NET) to
    // sometimes silently decode instead of throwing when done that way. A
    // literal constant buffer's bytes are identical regardless of which zlib
    // produced (or is about to consume) them, so its throw/no-throw outcome is
    // governed purely by the DEFLATE/gzip format spec (a reserved block-type
    // code, a bad gzip magic number) rather than by an encoder's internal
    // choices.
    internal static class Program
    {
        internal static void Run()
        {
            // Deflate: BTYPE=11 (the 2 bits right after BFINAL) is a reserved,
            // always-invalid block type per the DEFLATE format -- an all-0xFF
            // buffer hits it in the very first byte, regardless of encoder.
            ExpectThrowDeflate("deflate-invalid-blocktype", Fill(32, 0xFF));

            // GZip: a buffer that never spells out the 2-byte 0x1F 0x8B magic
            // number fails the format check before any deflate decoding starts.
            ExpectThrowGZip("gzip-invalid-magic-garbage", Fill(32, 0xAA));
            ExpectThrowGZip("gzip-invalid-magic-explicit", new byte[]
                { 0x00, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03 });
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

        private static void ExpectThrowDeflate(string label, byte[] corrupted)
        {
            try
            {
                using MemoryStream input = new MemoryStream(corrupted);
                using DeflateStream deflate = new DeflateStream(input, CompressionMode.Decompress);
                using MemoryStream output = new MemoryStream();
                deflate.CopyTo(output);
                Console.WriteLine(label + ": threw=False");
            }
            catch (Exception ex)
            {
                Console.WriteLine(label + ": threw=True type=" + ex.GetType().Name);
            }
        }

        private static void ExpectThrowGZip(string label, byte[] corrupted)
        {
            try
            {
                using MemoryStream input = new MemoryStream(corrupted);
                using GZipStream gzip = new GZipStream(input, CompressionMode.Decompress);
                using MemoryStream output = new MemoryStream();
                gzip.CopyTo(output);
                Console.WriteLine(label + ": threw=False");
            }
            catch (Exception ex)
            {
                Console.WriteLine(label + ": threw=True type=" + ex.GetType().Name);
            }
        }
    }
}
