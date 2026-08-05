#nullable disable
using System;
using System.IO;
using System.IO.Compression;

namespace ZipErrorPathsSubset
{
    // Corrupted-archive error paths. Only the exception TYPE is printed: message
    // wording can legitimately differ without being a bug. Every input derives
    // from the byte-fixed blob or is literal garbage, so all four are
    // deterministic. The last one opens successfully — only the central directory
    // is read at open — and throws at entry.Open().
    internal static class Program
    {
        internal static void __GateEntry()
        {
            byte[] blob = Convert.FromBase64String(ZipFixedBlobReadSubset.Program.Blob);

            byte[] garbage = new byte[64];
            for (int i = 0; i < garbage.Length; i++)
            {
                garbage[i] = 0xAA;
            }
            ExpectOpenThrow("not-a-zip", garbage);
            ExpectOpenThrow("empty-stream", Array.Empty<byte>());

            // Cutting 30 bytes off the tail destroys the EOCD record itself.
            byte[] truncated = new byte[blob.Length - 30];
            Array.Copy(blob, truncated, truncated.Length);
            ExpectOpenThrow("truncated-tail", truncated);

            // The blob opens with the first local header's magic; destroy it and
            // leave the central directory intact.
            byte[] badLocal = new byte[blob.Length];
            Array.Copy(blob, badLocal, blob.Length);
            badLocal[0] = 0x00;
            badLocal[1] = 0x00;
            try
            {
                using (var archive = new ZipArchive(new MemoryStream(badLocal), ZipArchiveMode.Read))
                {
                    Console.WriteLine("bad-local-header: opened count=" + archive.Entries.Count);
                    using (Stream s = archive.GetEntry("hello.txt").Open())
                    {
                        s.ReadByte();
                    }
                    Console.WriteLine("bad-local-header: threw=False");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("bad-local-header: threw=True type=" + ex.GetType().Name);
            }
        }

        private static void ExpectOpenThrow(string label, byte[] bytes)
        {
            try
            {
                using (var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read))
                {
                    Console.WriteLine(label + ": threw=False count=" + archive.Entries.Count);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(label + ": threw=True type=" + ex.GetType().Name);
            }
        }
    }
}
