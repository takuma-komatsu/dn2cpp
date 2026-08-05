using System;
using System.IO;
using System.Text;

namespace FileCoreSubset
{
// System.IO.File through the REAL BCL bodies: everything here except the six
// intercepted methods (Exists/Delete/ReadAllText/WriteAllText/ReadAllBytes/
// WriteAllBytes, still lowered to dn2cpp_file_*) runs the transpiled real
// CoreLib implementation — File.Copy/Move/Append*/ReadAllLines/WriteAllLines/
// ReadLines, FileStream, StreamReader/StreamWriter, File.Open* — bottoming out
// in the Interop.Sys P/Invokes that the dn2cpp runtime provides as the
// SystemNative_* PAL shims (runtime/core/platform/posix/dn2cpp_system_native.cpp).
//
// The scratch directory comes from the driver (args[0]): the gate gives the
// native build and real .NET SEPARATE fresh directories and diffs their stdout
// exactly; the output prints only content/bytes/results, never absolute paths.
internal static class Program
{
    internal static void __GateEntry(string dir)
    {
        // AppendAllText: creates, then appends.
        string a = Path.Combine(dir, "a.txt");
        File.AppendAllText(a, "one\n");
        File.AppendAllText(a, "two\n");
        Console.WriteLine("append=[" + File.ReadAllText(a) + "]");

        // Copy (and the overwrite overload).
        string b = Path.Combine(dir, "b.txt");
        File.Copy(a, b);
        Console.WriteLine("copied=" + File.Exists(b));
        File.Copy(a, b, overwrite: true);
        Console.WriteLine("recopied=[" + File.ReadAllText(b) + "]");
        try { File.Copy(a, b); Console.WriteLine("copyClash=noexc"); }
        catch (IOException) { Console.WriteLine("copyClash=IOException"); }

        // Move: source disappears, destination appears.
        string c = Path.Combine(dir, "c.txt");
        File.Move(b, c);
        Console.WriteLine("moved=" + File.Exists(b) + "," + File.Exists(c));

        // ReadAllLines / WriteAllLines / ReadLines / AppendAllLines.
        string[] lines = File.ReadAllLines(a);
        Console.WriteLine("lines=" + lines.Length + ":" + lines[0] + "," + lines[1]);
        File.WriteAllLines(c, new[] { "x", "y", "z" });
        foreach (string ln in File.ReadLines(c))
            Console.WriteLine("rl:" + ln);
        File.AppendAllLines(c, new[] { "w" });
        Console.WriteLine("appended=" + File.ReadAllLines(c).Length);

        // FileStream via File.Create: write, flush, length/position, seek, patch.
        string s = Path.Combine(dir, "s.bin");
        using (FileStream w = File.Create(s))
        {
            w.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
            w.Flush();
            Console.WriteLine("len=" + w.Length + " pos=" + w.Position);
            w.Seek(1, SeekOrigin.Begin);
            w.WriteByte(9);
        }
        using (FileStream r = File.OpenRead(s))
        {
            byte[] buf = new byte[5];
            int n = r.Read(buf, 0, 5);
            Console.Write("read=" + n + ":");
            foreach (byte v in buf)
                Console.Write(v + ",");
            Console.WriteLine();
            r.Seek(-2, SeekOrigin.End);
            Console.WriteLine("tail=" + r.ReadByte() + "," + r.ReadByte() + "," + r.ReadByte());
        }

        // StreamWriter/StreamReader via File.CreateText/OpenText/AppendText.
        string t = Path.Combine(dir, "t.txt");
        using (StreamWriter sw = File.CreateText(t))
            sw.WriteLine("hello text");
        using (StreamReader sr = File.OpenText(t))
            Console.WriteLine("line=[" + sr.ReadLine() + "] eof=" + (sr.ReadLine() is null));
        using (StreamWriter sw = File.AppendText(t))
            sw.WriteLine("more");
        Console.WriteLine("tlines=" + File.ReadAllLines(t).Length);

        // File.Open with explicit FileMode/FileAccess; in-place patch.
        using (FileStream fs = File.Open(s, FileMode.Open, FileAccess.ReadWrite))
        {
            fs.Position = 0;
            fs.WriteByte(42);
        }
        Console.WriteLine("patched=" + File.ReadAllBytes(s)[0]);

        // Error paths: the real bodies throw the real exception types.
        try { File.Copy(Path.Combine(dir, "none.txt"), Path.Combine(dir, "x.txt")); Console.WriteLine("copyMissing=noexc"); }
        catch (FileNotFoundException) { Console.WriteLine("copyMissing=FileNotFoundException"); }
        try { File.Move(Path.Combine(dir, "none.txt"), Path.Combine(dir, "x.txt")); Console.WriteLine("moveMissing=noexc"); }
        catch (FileNotFoundException) { Console.WriteLine("moveMissing=FileNotFoundException"); }
        try { using (FileStream fs = File.OpenRead(Path.Combine(dir, "none.txt"))) { } Console.WriteLine("openMissing=noexc"); }
        catch (FileNotFoundException) { Console.WriteLine("openMissing=FileNotFoundException"); }

        // The Path/File OVERLOADS that are not lowered to a dn2cpp_path_*/dn2cpp_file_*
        // helper, and so transpile from their real BCL bodies — the shape that breaks when
        // an intercept cuts by method NAME while its lowering matches on the SIGNATURE.
        // Where an intercepted sibling exists, the two lanes must agree exactly.

        // GetFullPath(path, basePath) resolves against an explicit base instead of the cwd.
        // The base is a FIXED absolute literal, so the printed result is machine-independent
        // (a relative basePath throws in .NET). It reaches Interop.Sys.GetCwd, which is why
        // it is asserted here, in the net10-pinned bucket, and not in ConsoleIo.
        //
        // The base literal is OS-CONDITIONAL, and that is load-bearing. A rooted-but-not-
        // drive-qualified base ("/base/dir") is rejected by Windows' PathInternal
        // .IsPartiallyQualified, so both sides throw here and the process dies mid-section —
        // with the gate still green, because two truncated transcripts still MATCH. Keep the
        // base drive-qualified on Windows so the whole program executes.
        //
        // Only the BASE is conditional: "/already/abs" stays, and on Windows it exercises
        // the drive-splicing arm (rooted, not fully qualified -> the root of basePath is
        // spliced on) that POSIX has no equivalent of.
        string baseDir = Path.DirectorySeparatorChar == '/' ? "/base/dir" : "C:\\base\\dir";
        Console.WriteLine("fullBase=[" + Path.GetFullPath("b/../c/./d.txt", baseDir) + "]");
        Console.WriteLine("fullBaseAbs=[" + Path.GetFullPath("/already/abs", baseDir) + "]");

        // ReadAllText(path, Encoding) / WriteAllText(path, string, Encoding). Encoding.UTF8
        // only: Encoding.ASCII/Latin1/GetEncoding can reach the SIMD transcoders
        // (Ascii.WidenAsciiToUtf16), which are a separate carve-out — and one that fails
        // loudly, naming the real reach chain, rather than silently.
        string e = Path.Combine(dir, "e.txt");
        File.WriteAllText(e, "encé\n", Encoding.UTF8);
        Console.WriteLine("encRT=[" + File.ReadAllText(e, Encoding.UTF8) + "]");
        // The explicit-UTF8 lane and the intercepted default-encoding lane must agree.
        Console.WriteLine("encVsDefault=" + (File.ReadAllText(e) == File.ReadAllText(e, Encoding.UTF8)));

        // WriteAllText(path, ReadOnlySpan<char>): a char[] argument binds THIS overload, not
        // WriteAllText(string, string) — an easy trip, and one the name-routed intercept
        // used to turn into a transpile failure.
        string f = Path.Combine(dir, "f.txt");
        char[] chars = new[] { 'c', 'h', 'a', 'r', 's' };
        File.WriteAllText(f, chars);
        Console.WriteLine("spanText=[" + File.ReadAllText(f) + "]");
        File.WriteAllText(f, chars, Encoding.UTF8);
        Console.WriteLine("spanTextEnc=[" + File.ReadAllText(f) + "]");

        // WriteAllBytes(path, ReadOnlySpan<byte>) — the slice — read back through the
        // INTERCEPTED ReadAllBytes(string). The two lanes must see identical bytes.
        string h = Path.Combine(dir, "h.bin");
        byte[] raw = new byte[] { 10, 20, 30, 40, 50 };
        File.WriteAllBytes(h, raw.AsSpan(1, 3));
        byte[] back = File.ReadAllBytes(h);
        Console.Write("spanBytes=" + back.Length + ":");
        foreach (byte v in back)
            Console.Write(v + ",");
        Console.WriteLine();

        // SafeFileHandle through the SafeHandle base surface — the ordinary interop shape
        // on the handle File.OpenHandle returns. DangerousGetHandle() is declared on
        // System.Runtime.InteropServices.SafeHandle, so a call from app code is a
        // cross-assembly MemberRef against that BASE type, and it is emitted as a call to
        // the real transpiled body. The real body therefore has to be in the tree: a reach
        // cut keyed on the member name alone deletes it for every SafeHandle in the
        // program, and the result is not a transpile error but an undefined symbol at C++
        // LINK time. The one case that IS lowered inline is the memory-mapped view handle,
        // and the emit guard selecting it keys on the RECEIVER's static type — something
        // reachability cannot see, so the cut must not try to approximate it.
        using (Microsoft.Win32.SafeHandles.SafeFileHandle fh =
                   File.OpenHandle(a, FileMode.Open, FileAccess.Read))
        {
            Console.WriteLine("fhInvalid=" + fh.IsInvalid + " fhClosed=" + fh.IsClosed);
            IntPtr rawPtr = fh.DangerousGetHandle();
            Console.WriteLine("fhRawOk=" + (rawPtr != IntPtr.Zero && rawPtr != new IntPtr(-1)));
            Console.WriteLine("fhLen=" + RandomAccess.GetLength(fh));
            byte[] two = new byte[2];
            Console.WriteLine("fhRead=" + RandomAccess.Read(fh, two, 0) + ":" + two[0] + "," + two[1]);
        }

        // Reading into a buffer the COLLECTOR MAY HAVE WRITE-PROTECTED.
        //
        // Boehm's incremental collector mprotects the heap to get its dirty bits.
        // A user-space store into a protected page faults and is handled; a KERNEL
        // store is not — ::pread into a managed byte[] returns EFAULT, and
        // FileStream surfaces that as an IOException. The gate runs this whole
        // program a second time with DN2CPP_GC_INCREMENTAL=1, which is the Godot
        // lane's DEFAULT, so this section is what proves the runtime's bounce
        // (dn2cpp_gc_kernel_write_unsafe + the SystemNative_* read paths).
        //
        // The shape is load-bearing: bdwgc leaves a FRESHLY allocated block
        // unprotected, so only a buffer that has survived a collection can be hit.
        // Allocate, collect, THEN read. (Allocating it atomically does not save it:
        // where a page is bigger than a heap block — 16 KB-page arm64 — the
        // collector protects the pointer-free heap too.)
        string g = Path.Combine(dir, "g.bin");
        byte[] payload = new byte[1 << 20];
        for (int i = 0; i < payload.Length; i++)
            payload[i] = (byte)(i * 31 + 7);
        File.WriteAllBytes(g, payload);

        byte[] rbuf = new byte[64 * 1024];
        GC.Collect(); // rbuf now survives into a heap the collector has re-armed
        long sum = 0;
        long total = 0;
        using (FileStream fs = File.OpenRead(g))
        {
            int n;
            while ((n = fs.Read(rbuf, 0, rbuf.Length)) > 0)
            {
                total += n;
                for (int i = 0; i < n; i++)
                    sum += rbuf[i];
            }
        }
        Console.WriteLine("gcio=" + total + " sum=" + sum);
    }
}
}
