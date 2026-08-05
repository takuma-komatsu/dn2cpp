#nullable disable
using System;
using System.IO;
using System.IO.Compression;
using ZipFileCore;

namespace ZipFileErrorPathsSubset
{
    // ZipFile error paths over the real filesystem. Only the exception TYPE is
    // printed, since the messages embed absolute paths.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("-- ZipFile error paths --");
            string work = ZipScratch.Fresh("errors");
            try
            {
                Expect("openread-missing",
                    () => ZipFile.OpenRead(Path.Combine(work, "missing.zip")));
                Expect("extract-missing-zip",
                    () => ZipFile.ExtractToDirectory(Path.Combine(work, "missing.zip"), Path.Combine(work, "out2")));
                Expect("create-missing-src",
                    () => ZipFile.CreateFromDirectory(Path.Combine(work, "no-such-dir"), Path.Combine(work, "x.zip")));

                string src = Path.Combine(work, "src");
                Directory.CreateDirectory(src);
                File.WriteAllText(Path.Combine(src, "a.txt"), "payload");
                string zip = Path.Combine(work, "arch.zip");
                ZipFile.CreateFromDirectory(src, zip);

                Expect("create-dest-exists",
                    () => ZipFile.CreateFromDirectory(src, zip));

                string outDir = Path.Combine(work, "out");
                ZipFile.ExtractToDirectory(zip, outDir);
                Expect("extract-duplicate",
                    () => ZipFile.ExtractToDirectory(zip, outDir));
                ZipFile.ExtractToDirectory(zip, outDir, overwriteFiles: true);
                Console.WriteLine("extract-overwrite ok: "
                    + (File.ReadAllText(Path.Combine(outDir, "a.txt")) == "payload"));

                using (ZipArchive archive = ZipFile.OpenRead(zip))
                {
                    Console.WriteLine("getentry-missing null: " + (archive.GetEntry("nope.txt") == null));
                }
            }
            finally
            {
                ZipScratch.Cleanup(work);
            }
        }

        private static void Expect(string label, Action action)
        {
            try
            {
                action();
                Console.WriteLine(label + ": threw=False");
            }
            catch (Exception ex)
            {
                Console.WriteLine(label + ": threw=True type=" + ex.GetType().Name);
            }
        }
    }
}
