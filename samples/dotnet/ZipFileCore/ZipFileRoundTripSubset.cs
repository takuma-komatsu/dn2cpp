#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ZipFileCore;

namespace ZipFileRoundTripSubset
{
    // CreateFromDirectory -> ExtractToDirectory over a real on-disk tree, under
    // both includeBaseDirectory settings. The empty directory is the interesting
    // one: it must survive as its own trailing-'/' archive entry.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("-- ZipFile round trip --");
            string work = ZipScratch.Fresh("roundtrip");
            try
            {
                string src = Path.Combine(work, "src");
                Directory.CreateDirectory(Path.Combine(src, "nested", "deeper"));
                Directory.CreateDirectory(Path.Combine(src, "emptydir"));
                File.WriteAllText(Path.Combine(src, "a.txt"), "alpha file contents");
                File.WriteAllBytes(Path.Combine(src, "b.bin"), new byte[] { 1, 2, 3, 4, 5, 250, 251, 252 });
                File.WriteAllText(Path.Combine(src, "nested", "c.txt"), "nested file contents");
                File.WriteAllText(Path.Combine(src, "nested", "deeper", "d.txt"), "deep file contents");

                // Source-relative entry names, no base prefix.
                string flatZip = Path.Combine(work, "flat.zip");
                ZipFile.CreateFromDirectory(src, flatZip, CompressionLevel.Optimal, includeBaseDirectory: false);
                PrintEntries("flat entries", flatZip);
                string outFlat = Path.Combine(work, "out-flat");
                ZipFile.ExtractToDirectory(flatZip, outFlat);
                ZipScratch.Print("flat extracted", ZipScratch.ListTree(outFlat));
                Console.WriteLine("flat contents ok: " + VerifyContents(outFlat));

                // Every entry prefixed with the source directory's name.
                string basedZip = Path.Combine(work, "based.zip");
                ZipFile.CreateFromDirectory(src, basedZip, CompressionLevel.Fastest, includeBaseDirectory: true);
                PrintEntries("based entries", basedZip);
                string outBased = Path.Combine(work, "out-based");
                ZipFile.ExtractToDirectory(basedZip, outBased);
                ZipScratch.Print("based extracted", ZipScratch.ListTree(outBased));
                Console.WriteLine("based contents ok: " + VerifyContents(Path.Combine(outBased, "src")));
            }
            finally
            {
                ZipScratch.Cleanup(work);
            }
        }

        private static void PrintEntries(string label, string zipPath)
        {
            var names = new List<string>();
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    names.Add(entry.FullName);
                }
            }
            ZipScratch.SortOrdinal(names);
            ZipScratch.Print(label + " (" + names.Count + ")", names);
        }

        private static bool VerifyContents(string root)
        {
            bool ok = true;
            ok &= File.ReadAllText(Path.Combine(root, "a.txt")) == "alpha file contents";
            byte[] bin = File.ReadAllBytes(Path.Combine(root, "b.bin"));
            byte[] expected = new byte[] { 1, 2, 3, 4, 5, 250, 251, 252 };
            ok &= bin.Length == expected.Length;
            for (int i = 0; ok && i < bin.Length; i++)
            {
                ok &= bin[i] == expected[i];
            }
            ok &= File.ReadAllText(Path.Combine(root, "nested", "c.txt")) == "nested file contents";
            ok &= File.ReadAllText(Path.Combine(root, "nested", "deeper", "d.txt")) == "deep file contents";
            ok &= Directory.Exists(Path.Combine(root, "emptydir"));
            return ok;
        }
    }
}
