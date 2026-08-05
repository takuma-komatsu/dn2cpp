#nullable disable
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ZipCreateRoundTripSubset
{
    // Create-mode round-trip: the default and every explicit CompressionLevel, a
    // subdirectory path, and an entry created but never written.
    //
    // DETERMINISM DISCIPLINE, inherited by every Zip section that creates bytes:
    // real .NET links zlib-ng and the native binary classic zlib, both valid and
    // differing — so never print CompressedLength, the archive size, or any
    // compressed data, and pin LastWriteTime rather than leave it at DateTime.Now.
    internal static class Program
    {
        private static readonly DateTimeOffset FixedStamp =
            new DateTimeOffset(2020, 1, 2, 3, 4, 6, TimeSpan.Zero);

        internal static void __GateEntry()
        {
            byte[] textPayload = Encoding.UTF8.GetBytes(
                "Zip create round-trip payload. Zip create round-trip payload.");
            byte[] binPayload = new byte[512];
            for (int i = 0; i < binPayload.Length; i++)
            {
                binPayload[i] = (byte)(i * 11);
            }

            var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                WriteEntry(archive.CreateEntry("root.txt"), textPayload);
                WriteEntry(archive.CreateEntry("dir/sub/nested.bin", CompressionLevel.NoCompression), binPayload);
                WriteEntry(archive.CreateEntry("fastest.txt", CompressionLevel.Fastest), textPayload);
                WriteEntry(archive.CreateEntry("optimal.txt", CompressionLevel.Optimal), textPayload);
                WriteEntry(archive.CreateEntry("smallest.txt", CompressionLevel.SmallestSize), textPayload);
                ZipArchiveEntry empty = archive.CreateEntry("dir/empty.marker");
                empty.LastWriteTime = FixedStamp;
            }

            ms.Position = 0;
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Read))
            {
                Console.WriteLine("create-roundtrip: count=" + archive.Entries.Count);
                PrintEntry(archive, "root.txt", textPayload);
                PrintEntry(archive, "dir/sub/nested.bin", binPayload);
                PrintEntry(archive, "fastest.txt", textPayload);
                PrintEntry(archive, "optimal.txt", textPayload);
                PrintEntry(archive, "smallest.txt", textPayload);
                PrintEntry(archive, "dir/empty.marker", Array.Empty<byte>());
            }
        }

        private static void WriteEntry(ZipArchiveEntry entry, byte[] payload)
        {
            entry.LastWriteTime = FixedStamp;
            using (Stream s = entry.Open())
            {
                s.Write(payload, 0, payload.Length);
            }
        }

        private static void PrintEntry(ZipArchive archive, string fullName, byte[] expected)
        {
            ZipArchiveEntry entry = archive.GetEntry(fullName);
            if (entry is null)
            {
                Console.WriteLine("  " + fullName + ": MISSING");
                return;
            }
            Console.WriteLine("  " + entry.FullName
                + " name=" + entry.Name
                + " len=" + entry.Length
                + " lwtOk=" + (entry.LastWriteTime.DateTime == FixedStamp.DateTime)
                + " ok=" + BytesEqual(ReadAll(entry), expected));
        }

        private static byte[] ReadAll(ZipArchiveEntry entry)
        {
            var sink = new MemoryStream();
            using (Stream s = entry.Open())
            {
                s.CopyTo(sink);
            }
            return sink.ToArray();
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
