#nullable disable
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ZipFixedBlobReadSubset
{
    // Read-mode over a byte-fixed blob generated once on real .NET. Because the
    // bytes are frozen in source, EVERY metadata property is deterministic and so
    // printed in full — including CompressedLength and Crc32, which a self-created
    // archive must never print (zlib-ng and classic zlib disagree). LastWriteTime
    // is printed component-wise: DOS time, 2-second resolution, no zone.
    //
    // The second pass patches the version-made-by platform byte to Windows so
    // ParseFileName takes its GetFileName_Windows branch at RUN time.
    internal static class Program
    {
        internal const string Blob =
            "UEsDBBQAAAAIAIMYIlBAu6IjEQAAAC8AAAAJAAAAaGVsbG8udHh080jNycnXUYjKLFBU8CDMBgBQ" +
            "SwMEFAAAAAAAXb+fU4zODhBAAAAAQAAAAAwAAABzdWIvZGF0YS5iaW4AAQIDBAUGBwgJCgsMDQ4P" +
            "EBESExQVFhcYGRobHB0eHyAhIiMkJSYnKCkqKywtLi8wMTIzNDU2Nzg5Ojs8PT4/UEsBAhQDFAAA" +
            "AAgAgxgiUEC7oiMRAAAALwAAAAkAAAAOAAAAAAAAAKSBAAAAAGhlbGxvLnR4dGdyZWV0aW5nIGVu" +
            "dHJ5UEsBAhQDFAAAAAAAXb+fU4zODhBAAAAAQAAAAAwAAAAAAAAAAAAAAKSBOAAAAHN1Yi9kYXRh" +
            "LmJpblBLBQYAAAAAAgACAH8AAACiAAAAEwBmaXhlZCBwcm9iZSBhcmNoaXZl";

        internal static void __GateEntry()
        {
            byte[] blob = Convert.FromBase64String(Blob);
            byte[] expectedText = Encoding.UTF8.GetBytes(
                "Hello, Zip! Hello, Zip! Hello, Zip! Hello, Zip!");
            byte[] expectedBin = new byte[64];
            for (int i = 0; i < expectedBin.Length; i++)
            {
                expectedBin[i] = (byte)i;
            }

            using (var archive = new ZipArchive(new MemoryStream(blob), ZipArchiveMode.Read))
            {
                Console.WriteLine("fixed-blob: comment=[" + archive.Comment
                    + "] count=" + archive.Entries.Count);
                PrintEntry(archive.Entries[0], expectedText);
                PrintEntry(archive.Entries[1], expectedBin);
            }

            // The platform byte sits at +5 from each "PK\x01\x02" signature; 3 is
            // Unix, 0 is MS-DOS/Windows.
            byte[] patched = Convert.FromBase64String(Blob);
            int patchedCount = 0;
            for (int i = 0; i + 5 < patched.Length; i++)
            {
                if (patched[i] == 0x50 && patched[i + 1] == 0x4B
                    && patched[i + 2] == 0x01 && patched[i + 3] == 0x02)
                {
                    patched[i + 5] = 0;
                    patchedCount++;
                }
            }
            Console.WriteLine("fixed-blob-winnames: patched=" + patchedCount);
            using (var archive = new ZipArchive(new MemoryStream(patched), ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    Console.WriteLine("  " + entry.FullName + " name=" + entry.Name);
                }
            }
        }

        private static void PrintEntry(ZipArchiveEntry entry, byte[] expected)
        {
            DateTime lwt = entry.LastWriteTime.DateTime;
            Console.WriteLine("  " + entry.FullName
                + " name=" + entry.Name
                + " len=" + entry.Length
                + " clen=" + entry.CompressedLength
                + " crc=" + entry.Crc32
                + " comment=[" + entry.Comment + "]"
                + " lwt=" + lwt.Year + "-" + lwt.Month + "-" + lwt.Day
                + " " + lwt.Hour + ":" + lwt.Minute + ":" + lwt.Second
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
