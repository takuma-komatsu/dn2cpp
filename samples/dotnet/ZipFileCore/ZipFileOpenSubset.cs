#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ZipFileCore;

namespace ZipFileOpenSubset
{
    // ZipFile.Open in all three modes plus the file-backed entry helpers. Every
    // ExtractToFile overwrite shape is covered, including overwrite:false onto an
    // existing file, which must throw AND leave the file untouched.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("-- ZipFile.Open --");
            string work = ZipScratch.Fresh("open");
            try
            {
                string one = Path.Combine(work, "one.txt");
                string two = Path.Combine(work, "two.txt");
                string three = Path.Combine(work, "three.txt");
                File.WriteAllText(one, "first file body");
                File.WriteAllText(two, "second file body");
                File.WriteAllText(three, "third file body");

                string zip = Path.Combine(work, "arch.zip");
                using (ZipArchive archive = ZipFile.Open(zip, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(one, "one.txt");
                    archive.CreateEntryFromFile(two, "sub/two.txt", CompressionLevel.NoCompression);
                }
                using (ZipArchive archive = ZipFile.Open(zip, ZipArchiveMode.Update))
                {
                    archive.CreateEntryFromFile(three, "three.txt", CompressionLevel.Fastest);
                }

                string dst = Path.Combine(work, "extracted.txt");
                using (ZipArchive archive = ZipFile.Open(zip, ZipArchiveMode.Read))
                {
                    var names = new List<string>();
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        names.Add(entry.FullName + " len=" + entry.Length);
                    }
                    ZipScratch.SortOrdinal(names);
                    ZipScratch.Print("entries (" + names.Count + ")", names);

                    archive.GetEntry("one.txt").ExtractToFile(dst);
                    Console.WriteLine("extract one ok: "
                        + (File.ReadAllText(dst) == "first file body"));

                    archive.GetEntry("sub/two.txt").ExtractToFile(dst, overwrite: true);
                    Console.WriteLine("extract two overwrite ok: "
                        + (File.ReadAllText(dst) == "second file body"));

                    try
                    {
                        archive.GetEntry("three.txt").ExtractToFile(dst, overwrite: false);
                        Console.WriteLine("extract three no-overwrite: threw=False");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("extract three no-overwrite: threw=True type=" + ex.GetType().Name);
                    }
                    Console.WriteLine("dst untouched ok: "
                        + (File.ReadAllText(dst) == "second file body"));
                }
            }
            finally
            {
                ZipScratch.Cleanup(work);
            }
        }
    }
}
