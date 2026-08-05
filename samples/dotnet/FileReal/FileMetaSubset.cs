using System;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace FileMetaSubset
{
    // The file-metadata face — FileInfo, the attribute get/set pair, File.Replace — and
    // the raw-handle face: File.OpenHandle's SafeFileHandle driven through the public
    // RandomAccess API (PRead / PWrite / FTruncate / FStat under it).
    //
    // FileInfo.FullName / .DirectoryName are absolute paths and are never printed; only
    // Name / Extension / Length / Exists are. Exception TYPE names only.
    internal static class Program
    {
        internal static void __GateEntry(string dir)
        {
            // FileInfo over a file that exists.
            string p = Path.Combine(dir, "meta.dat");
            File.WriteAllBytes(p, new byte[] { 1, 2, 3, 4, 5, 6, 7 });
            FileInfo fi = new FileInfo(p);
            Console.WriteLine("fi=" + fi.Exists + " len=" + fi.Length
                + " name=" + fi.Name + " ext=" + fi.Extension);

            // FileInfo is a SNAPSHOT: Length is cached until Refresh().
            File.WriteAllBytes(p, new byte[] { 1, 2, 3 });
            Console.WriteLine("fiStale=" + fi.Length);
            fi.Refresh();
            Console.WriteLine("fiFresh=" + fi.Length);

            // …over one that does not. Exists is false; Length throws.
            FileInfo missing = new FileInfo(Path.Combine(dir, "no-such.dat"));
            Console.WriteLine("fiMissing=" + missing.Exists + " ext=" + missing.Extension);
            try { Console.WriteLine("fiMissingLen=" + missing.Length); }
            catch (FileNotFoundException) { Console.WriteLine("fiMissingLen=FileNotFoundException"); }

            // FileInfo.Delete, and delete of an absent file (which is a no-op, not a throw).
            FileInfo del = new FileInfo(Path.Combine(dir, "todelete.dat"));
            File.WriteAllText(del.FullName, "x");
            del.Refresh();
            Console.WriteLine("delBefore=" + del.Exists);
            del.Delete();
            del.Refresh();
            Console.WriteLine("delAfter=" + del.Exists);
            missing.Delete();   // absent: no throw
            Console.WriteLine("delMissing=noexc");

            // Attributes: set ReadOnly, read it back, clear it. Only the bit that was
            // touched is asserted — the rest of the word is platform-dependent.
            File.SetAttributes(p, File.GetAttributes(p) | FileAttributes.ReadOnly);
            Console.WriteLine("ro=" + ((File.GetAttributes(p) & FileAttributes.ReadOnly) != 0));
            // …and a read-only file refuses to open for writing.
            try
            {
                using (FileStream fs = new FileStream(p, FileMode.Open, FileAccess.Write)) { }
                Console.WriteLine("roOpen=noexc");
            }
            catch (UnauthorizedAccessException) { Console.WriteLine("roOpen=UnauthorizedAccessException"); }
            File.SetAttributes(p, File.GetAttributes(p) & ~FileAttributes.ReadOnly);
            Console.WriteLine("roCleared=" + ((File.GetAttributes(p) & FileAttributes.ReadOnly) != 0));

            // File.Replace(source, destination, backup): destination takes the source's
            // contents, the old destination is kept as the backup, and the source is gone.
            string src = Path.Combine(dir, "repl-src.txt");
            string dst = Path.Combine(dir, "repl-dst.txt");
            string bak = Path.Combine(dir, "repl-bak.txt");
            File.WriteAllText(src, "NEW");
            File.WriteAllText(dst, "OLD");
            File.Replace(src, dst, bak);
            Console.WriteLine("replace=[" + File.ReadAllText(dst) + "] backup=["
                + File.ReadAllText(bak) + "] srcGone=" + !File.Exists(src));

            // File.OpenHandle -> SafeFileHandle, driven through the public RandomAccess
            // surface: GetLength (FStat), Read (PRead), Write (PWrite), SetLength
            // (FTruncate). Offsets are explicit — the handle carries no position.
            string h = Path.Combine(dir, "handle.bin");
            File.WriteAllBytes(h, new byte[] { 10, 20, 30, 40, 50, 60 });
            using (SafeFileHandle fh = File.OpenHandle(h, FileMode.Open, FileAccess.ReadWrite))
            {
                Console.WriteLine("hLen=" + RandomAccess.GetLength(fh)
                    + " invalid=" + fh.IsInvalid + " closed=" + fh.IsClosed);

                byte[] three = new byte[3];
                int n = RandomAccess.Read(fh, three, 2);      // from offset 2
                Console.WriteLine("hRead=" + n + ":" + three[0] + "," + three[1] + "," + three[2]);

                RandomAccess.Write(fh, new byte[] { 99, 98 }, 1);
                byte[] all = new byte[6];
                RandomAccess.Read(fh, all, 0);
                Console.WriteLine("hWrite=" + all[0] + "," + all[1] + "," + all[2] + "," + all[5]);

                RandomAccess.SetLength(fh, 3);
                Console.WriteLine("hSetLen=" + RandomAccess.GetLength(fh));

                // A read past the end returns 0, it does not throw.
                Console.WriteLine("hReadPastEnd=" + RandomAccess.Read(fh, three, 100));
            }
            Console.WriteLine("hOnDisk=" + new FileInfo(h).Length);

            // File.OpenHandle with FileMode.CreateNew over an existing file throws, the
            // same as the FileStream ctor does.
            try
            {
                using (SafeFileHandle fh = File.OpenHandle(h, FileMode.CreateNew, FileAccess.Write)) { }
                Console.WriteLine("hCreateNewClash=noexc");
            }
            catch (IOException) { Console.WriteLine("hCreateNewClash=IOException"); }
        }
    }
}
