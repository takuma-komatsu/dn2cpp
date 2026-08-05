using System;
using System.IO;

// System.IO.File lowered to the dn2cpp_file_* libc helpers (the
// biggest self-host gap cut — the real bodies pull in the ArrayPool/EventSource/
// Tracing/Sys.* P/Invoke cascade). `dir` is the bucket's scratch directory (the
// gate hands each side a fresh mktemp dir, so native and real .NET write to
// separate roots); the output prints only content/bytes/results, never absolute
// paths, so it diffs exact. WriteAllText is UTF-8 with no BOM; ReadAllText strips
// a UTF-8 BOM. Every file name used here (t.txt, n.txt, b.bin, bom.txt, none.txt)
// is this section's own, so the fixture the enumeration sections walk is
// untouched — and this section runs after them regardless.
namespace FileIoSubset;

internal static class Program
{
    internal static void __GateEntry(string dir)
    {
        Console.WriteLine("-- System.IO.File --");

        // WriteAllText round trip + raw bytes (proves UTF-8, no BOM).
        string f = Path.Combine(dir, "t.txt");
        File.WriteAllText(f, "héllo\nわ\n");
        byte[] raw = File.ReadAllBytes(f);
        Console.Write("writeBytes=");
        foreach (byte b in raw) Console.Write($"{b:X2} ");
        Console.WriteLine();
        Console.WriteLine($"readText=[{File.ReadAllText(f)}]");
        Console.WriteLine($"bytesType={raw.GetType().Name}");
        Console.WriteLine($"bytesLen={raw.Length}");

        // Exists: true for a file, false for a directory or a missing path.
        Console.WriteLine($"existsFile={File.Exists(f)}");
        Console.WriteLine($"existsDir={File.Exists(dir)}");
        Console.WriteLine($"existsNo={File.Exists(Path.Combine(dir, "none.txt"))}");

        // WriteAllText(null) writes an empty file.
        string fn = Path.Combine(dir, "n.txt");
        File.WriteAllText(fn, null);
        Console.WriteLine($"nullLen={File.ReadAllBytes(fn).Length}");

        // WriteAllBytes / ReadAllBytes round trip (full byte range).
        byte[] data = { 0, 1, 2, 255, 128, 7 };
        string fb = Path.Combine(dir, "b.bin");
        File.WriteAllBytes(fb, data);
        byte[] back = File.ReadAllBytes(fb);
        Console.Write("bytesBack=");
        foreach (byte b in back) Console.Write($"{b} ");
        Console.WriteLine($"len={back.Length}");

        // Delete: missing path is a no-op, existing file is removed.
        File.Delete(Path.Combine(dir, "none.txt"));
        Console.WriteLine("deleteMissing=ok");
        File.Delete(fb);
        Console.WriteLine($"deletedExists={File.Exists(fb)}");

        // ReadAllText strips a leading UTF-8 BOM.
        string fbom = Path.Combine(dir, "bom.txt");
        File.WriteAllBytes(fbom, new byte[] { 0xEF, 0xBB, 0xBF, (byte)'h', (byte)'i' });
        string bt = File.ReadAllText(fbom);
        Console.WriteLine($"bomText=[{bt}] bomLen={bt.Length}");

        // Error paths throw catchable managed exceptions (the runtime uses the real
        // .NET exception types — FileNotFoundException / UnauthorizedAccessException —
        // but Exception.GetType()/specific-type catch are a separate gap, so
        // here we only assert the throw is catchable, not a hard abort).
        try { File.ReadAllText(Path.Combine(dir, "none.txt")); Console.WriteLine("readMissing=noexc"); }
        catch (Exception) { Console.WriteLine("readMissing=caught"); }
        try { File.Delete(dir); Console.WriteLine("delDir=noexc"); }
        catch (Exception) { Console.WriteLine("delDir=caught"); }
    }
}
