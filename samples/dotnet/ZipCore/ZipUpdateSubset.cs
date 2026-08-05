#nullable disable
using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ZipUpdateSubset
{
    // Update mode over the fixed blob: delete, append to an existing entry's
    // stream, add a new entry, then dispose — which rewrites the archive, sorting
    // the survivors by local-header offset through Int64.CompareTo.
    //
    // The rewrite RE-COMPRESSES, so nothing compression-shaped may be printed
    // (ZipCreateRoundTripSubset states why).
    internal static class Program
    {
        private static readonly DateTimeOffset FixedStamp =
            new DateTimeOffset(2020, 1, 2, 3, 4, 6, TimeSpan.Zero);

        internal static void __GateEntry()
        {
            byte[] blob = Convert.FromBase64String(ZipFixedBlobReadSubset.Program.Blob);
            var ms = new MemoryStream();
            ms.Write(blob, 0, blob.Length);

            byte[] appended = Encoding.UTF8.GetBytes(" [appended in update mode]");
            byte[] fresh = Encoding.UTF8.GetBytes("fresh entry payload written in update mode");

            using (var archive = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
            {
                archive.GetEntry("sub/data.bin").Delete();

                ZipArchiveEntry hello = archive.GetEntry("hello.txt");
                using (Stream s = hello.Open())
                {
                    s.Seek(0, SeekOrigin.End);
                    s.Write(appended, 0, appended.Length);
                }

                ZipArchiveEntry added = archive.CreateEntry("added/fresh.txt", CompressionLevel.Fastest);
                added.LastWriteTime = FixedStamp;
                using (Stream s = added.Open())
                {
                    s.Write(fresh, 0, fresh.Length);
                }
            }

            ms.Position = 0;
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Read))
            {
                Console.WriteLine("update: count=" + archive.Entries.Count
                    + " deletedGone=" + (archive.GetEntry("sub/data.bin") is null));

                byte[] expectedHello = Encoding.UTF8.GetBytes(
                    "Hello, Zip! Hello, Zip! Hello, Zip! Hello, Zip! [appended in update mode]");
                ZipArchiveEntry hello = archive.GetEntry("hello.txt");
                Console.WriteLine("  hello.txt len=" + hello.Length
                    + " ok=" + BytesEqual(ReadAll(hello), expectedHello));

                ZipArchiveEntry added = archive.GetEntry("added/fresh.txt");
                Console.WriteLine("  added/fresh.txt name=" + added.Name
                    + " len=" + added.Length
                    + " lwtOk=" + (added.LastWriteTime.DateTime == FixedStamp.DateTime)
                    + " ok=" + BytesEqual(ReadAll(added), fresh));
            }
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
