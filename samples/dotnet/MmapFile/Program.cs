using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;
using System.Globalization;
using System.Threading;

// System.IO.MemoryMappedFiles file-backed map subset lowered to the dn2cpp_mmap_*
// helpers (POSIX mmap/munmap). CreateFromFile maps an existing file; a view accessor
// reads/writes the mapped bytes (typed accessors + generic Read/Write/ReadArray/
// WriteArray<T>); the SafeMemoryMappedViewHandle exposes the raw byte* (AcquirePointer)
// the System.Reflection.Metadata PEReader path scans. args[0] is a caller-supplied
// scratch directory (the gate gives native and real .NET separate fresh dirs); the
// output prints only computed/observed values — never addresses or paths — so it diffs
// exact vs real .NET. arm64 macOS is little-endian; no cross-endian assertions.
internal static class Program
{
#if !MMAP_UNINITIALIZED_ONLY
    private sealed class MapSlot
    {
        public MemoryMappedFile Value;
    }

    private struct Rec
    {
        public int Id;
        public double Val;
        public long Extra;
    } // 24 bytes (Id, pad, Val, Extra) — word-or-larger fields, so C++/.NET layouts agree

    private sealed class RefusingFlushStream : FileStream
    {
        internal RefusingFlushStream(string path) : base(path, FileMode.Open, FileAccess.ReadWrite) { }
        public override void Flush() => throw new IOException("flush marker");
    }

    private sealed class GrowingFlushStream : FileStream
    {
        internal GrowingFlushStream(string path) : base(path, FileMode.Open, FileAccess.ReadWrite) { }
        public override void Flush()
        {
            base.SetLength(64);
            base.Flush();
        }
    }

    private sealed class LongerLengthStream : FileStream
    {
        internal LongerLengthStream(string path) : base(path, FileMode.Open, FileAccess.Read) { }
        public override long Length => base.Length + 1;
    }

    private sealed class NegativeLengthStream : FileStream
    {
        internal NegativeLengthStream(string path) : base(path, FileMode.Open, FileAccess.ReadWrite) { }
        public override long Length => -1;
    }

    private sealed class InvalidHandleStream : FileStream
    {
        internal InvalidHandleStream(string path) : base(path, FileMode.Open, FileAccess.Read) { }
        public override SafeFileHandle SafeFileHandle => new SafeFileHandle(new IntPtr(-1), false);
    }
#endif

    private static unsafe void Main(string[] args)
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

#if !MMAP_UNINITIALIZED_ONLY
        string dir = args[0];

        // ── Read-only: author a file with known bytes via normal I/O, map it read-only,
        //    read primitives + a struct + an array, and scan it via a raw byte*. ──
        string roPath = Path.Combine(dir, "ro.bin");
        byte[] buf = new byte[64];
        Span<byte> sp = buf;
        int i32 = 287454020;                 // 0x11223344
        short i16 = 4660;                    // 0x1234
        long i64 = 1234567890123456789L;
        double dval = 3.5;
        Rec rec = new Rec { Id = 7654321, Val = 6.25, Extra = 42L };
        int arr0 = 1000;
        int arr1 = 2000;
        MemoryMarshal.Write(sp.Slice(0), in i32);
        MemoryMarshal.Write(sp.Slice(4), in i16);
        MemoryMarshal.Write(sp.Slice(8), in i64);
        buf[16] = 171;                       // 0xAB
        MemoryMarshal.Write(sp.Slice(24), in dval);
        MemoryMarshal.Write(sp.Slice(32), in rec);
        MemoryMarshal.Write(sp.Slice(56), in arr0);
        MemoryMarshal.Write(sp.Slice(60), in arr1);
        File.WriteAllBytes(roPath, buf);

        Console.WriteLine($"recSize={sizeof(Rec)}");

        MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(
            roPath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        MemoryMappedViewAccessor acc = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

        Console.WriteLine($"capacity={acc.Capacity}");
        Console.WriteLine($"readInt32={acc.ReadInt32(0)}");
        Console.WriteLine($"readInt16={acc.ReadInt16(4)}");
        Console.WriteLine($"readInt64={acc.ReadInt64(8)}");
        Console.WriteLine($"readByte={acc.ReadByte(16)}");
        Console.WriteLine($"readDouble={acc.ReadDouble(24)}");
        acc.Read(24, out double gd);
        Console.WriteLine($"genericDouble={gd}");

        acc.Read(32, out Rec r);
        Console.WriteLine($"recId={r.Id} recVal={r.Val} recExtra={r.Extra}");

        int[] back = new int[2];
        int got = acc.ReadArray(56, back, 0, 2);
        Console.WriteLine($"readArrayCount={got} a0={back[0]} a1={back[1]}");

        // Raw-pointer scan (the SRM PEReader surface): AcquirePointer hands back the
        // mapped region base; sum the bytes and reinterpret a couple of values.
        SafeMemoryMappedViewHandle h = acc.SafeMemoryMappedViewHandle;
        byte* p = null;
        h.AcquirePointer(ref p);
        long cap = acc.Capacity;
        long sum = 0;
        for (long i = 0; i < cap; i++)
        {
            sum += p[i];
        }
        int ptrFirstInt = *(int*)p;
        byte ptrByte16 = p[16];
        bool handleMatches = (byte*)h.DangerousGetHandle() == p;
        ulong byteLen = h.ByteLength;
        h.ReleasePointer();
        Console.WriteLine($"scanSum={sum}");
        Console.WriteLine($"ptrFirstInt={ptrFirstInt}");
        Console.WriteLine($"ptrByte16={ptrByte16}");
        Console.WriteLine($"handleMatches={handleMatches}");
        Console.WriteLine($"byteLength={byteLen}");

        acc.Dispose();
        mmf.Dispose();

        // ── Read-write: map a zeroed file, modify it through the view accessor, flush
        //    + dispose, then reopen via normal I/O and verify the bytes changed. ──
        string rwPath = Path.Combine(dir, "rw.bin");
        File.WriteAllBytes(rwPath, new byte[64]);

        MemoryMappedFile mmf2 = MemoryMappedFile.CreateFromFile(rwPath, FileMode.Open);
        MemoryMappedViewAccessor acc2 = mmf2.CreateViewAccessor();
        acc2.Write(0, 195948557);            // 0x0BADF00D (int overload)
        acc2.Write(8, 9876543210L);          // long overload
        acc2.Write(16, (byte)127);           // byte overload
        Rec rec2 = new Rec { Id = 55, Val = 2.25, Extra = 999L };
        acc2.Write(24, ref rec2);
        int[] src = { 100, 200, 300 };
        acc2.WriteArray(48, src, 0, 3);
        acc2.Flush();
        acc2.Dispose();
        mmf2.Dispose();

        byte[] after = File.ReadAllBytes(rwPath);
        Span<byte> ap = after;
        Console.WriteLine($"diskLen={after.Length}");
        Console.WriteLine($"diskInt32={MemoryMarshal.Read<int>(ap.Slice(0))}");
        Console.WriteLine($"diskInt64={MemoryMarshal.Read<long>(ap.Slice(8))}");
        Console.WriteLine($"diskByte16={after[16]}");
        Rec dr = MemoryMarshal.Read<Rec>(ap.Slice(24));
        Console.WriteLine($"diskRecId={dr.Id} diskRecVal={dr.Val} diskRecExtra={dr.Extra}");
        Console.WriteLine($"diskA0={MemoryMarshal.Read<int>(ap.Slice(48))}");
        Console.WriteLine($"diskA1={MemoryMarshal.Read<int>(ap.Slice(52))}");
        Console.WriteLine($"diskA2={MemoryMarshal.Read<int>(ap.Slice(56))}");
        Console.WriteLine($"mmap tostring={mmf2.ToString()}|{acc2.ToString()}|{mmf2}|{acc2}");
        Console.WriteLine($"mmap handle tostring={h.ToString()}|{h}");

        TestReferenceExchange(roPath);
#endif
        TestUninitializedMap();
#if !MMAP_UNINITIALIZED_ONLY
        if (args.Length > 1 && args[1] == "legacy") return;
        TestStreamMaps(dir);
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_IO_VALIDATION") == "1") return;
        Console.WriteLine("== mmap validation ==");
        TestArgumentNames(dir);
        TestArgumentMessages(dir);
        TestViewRanges(dir);
        TestMissingPathAndNamedMap(dir);
        TestOffsetAndWriteMessages(dir);
        Console.WriteLine("mmap validation complete");
        if (args.Length > 1 && args[1] == "before-mmap-full-path") return;
        TestFullPathMap(dir);
        if (args.Length > 1 && args[1] == "before-mmap-disposal-fields") return;
        TestClosedAccessorFields(dir);
#endif
    }

#if !MMAP_UNINITIALIZED_ONLY
    private static void TestClosedAccessorFields(string dir)
    {
        Console.WriteLine("-- mapped accessor disposal fields --");
        string path = Path.Combine(dir, "closed-accessor.bin");
        File.WriteAllBytes(path, new byte[64]);
        using var map = MemoryMappedFile.CreateFromFile(path, FileMode.Open);
        var closed = map.CreateViewAccessor();
        closed.Dispose();
        int[] values = new int[2];
        Rec value = new Rec { Id = 7 };
        ProbeClosed("typed read", () => closed.ReadInt32(0));
        ProbeClosed("typed write", () => closed.Write(0, 1));
        ProbeClosed("generic read", () => closed.Read<Rec>(0, out _));
        ProbeClosed("generic write", () => closed.Write(0, ref value));
        ProbeClosed("array read", () => closed.ReadArray(0, values, 0, 2));
        ProbeClosed("array write", () => closed.WriteArray(0, values, 0, 2));
        ProbeClosed("typed negative", () => closed.ReadInt32(-1));
        ProbeClosed("generic negative", () => closed.Read<Rec>(-1, out _));
        ProbeClosed("array null", () => closed.ReadArray<int>(0, null, 0, 1));
        ProbeClosed("array range", () => closed.ReadArray(0, values, 1, 2));
        ProbeClosed("array negative", () => closed.WriteArray(-1, values, 0, 1));
        ProbeClosed("flush", () => closed.Flush());
        using var handleClosed = map.CreateViewAccessor();
        handleClosed.SafeMemoryMappedViewHandle.Dispose();
        ProbeClosed("handle read", () => handleClosed.ReadInt32(0));
        ProbeClosed("handle generic", () => handleClosed.Read<Rec>(0, out _));
        ProbeClosed("handle array", () => handleClosed.ReadArray(0, values, 0, 1));
        ProbeClosed("handle flush", () => handleClosed.Flush());
        Console.WriteLine("mapped accessor disposal fields end");
    }

    private static void ProbeClosed(string label, Action action)
    {
        try
        {
            action();
            Console.WriteLine("closed " + label + ": returned");
        }
        catch (Exception e)
        {
            Console.WriteLine("closed " + label + ": " + e.GetType().FullName + "|" + e.HResult.ToString("X8")
                + "|" + e.Message.Replace("\r", "\\r").Replace("\n", "\\n"));
            if (e is ObjectDisposedException disposed)
                Console.WriteLine("closed " + label + " object=" + disposed.ObjectName);
        }
    }

    private static void TestFullPathMap(string dir)
    {
        Console.WriteLine("-- lexical mapped file paths --");
        File.WriteAllBytes(Path.Combine(dir, "dotmap.bin"), new byte[] { 51 });
        try
        {
            using var map = MemoryMappedFile.CreateFromFile(Path.Combine(dir, "absent", "..", "dotmap.bin"),
                FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            using var view = map.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            Console.WriteLine("mapped lexical byte=" + view.ReadByte(0));
        }
        catch (Exception error)
        {
            Console.WriteLine("mapped lexical fault=" + error.GetType().Name);
        }
        Console.WriteLine("-- lexical mapped file paths end --");
    }

    private static void TestMissingPathAndNamedMap(string dir)
    {
        Console.WriteLine("== mmap missing paths ==");
        string root = Path.GetFullPath(dir);
        string leaf = Path.Combine(dir, "absent.bin");
        string parent = Path.Combine(dir, "missing", "..", "missing", "child.bin");
        string existing = Path.Combine(dir, "named.bin");
        File.WriteAllBytes(existing, new byte[64]);
        void Probe(string label, Action action, bool message = true)
        {
            try
            {
                action();
                Console.WriteLine(label + ": no exception");
            }
            catch (Exception e)
            {
                Console.WriteLine(label + ": " + e.GetType().Name
                    + (message ? " | " + e.Message.Replace(root, "<scratch>") : ""));
            }
        }
        Probe("leaf", () => MemoryMappedFile.CreateFromFile(leaf, FileMode.Open).Dispose());
        Probe("parent", () => MemoryMappedFile.CreateFromFile(parent, FileMode.Open).Dispose());
        Probe("create parent", () => MemoryMappedFile.CreateFromFile(parent, FileMode.OpenOrCreate).Dispose());
        if (!OperatingSystem.IsWindows())
        {
            Probe("named", () => MemoryMappedFile.CreateFromFile(existing, FileMode.Open,
                "named-map", 0, MemoryMappedFileAccess.ReadWrite).Dispose());
            Probe("named missing", () => MemoryMappedFile.CreateFromFile(leaf, FileMode.Open,
                "named-map", 0, MemoryMappedFileAccess.ReadWrite).Dispose());
            Probe("named device", () => MemoryMappedFile.CreateFromFile("/dev/null", FileMode.Open,
                "named-map", 16, MemoryMappedFileAccess.Read).Dispose());
            using FileStream stream = File.OpenRead(existing);
            Probe("stream named", () => MemoryMappedFile.CreateFromFile(stream, "named-map", 0,
                MemoryMappedFileAccess.Read, HandleInheritability.None, true).Dispose());
            string empty = Path.Combine(dir, "named-empty.bin");
            File.WriteAllBytes(empty, Array.Empty<byte>());
            using FileStream emptyStream = File.OpenRead(empty);
            Probe("stream named empty", () => MemoryMappedFile.CreateFromFile(emptyStream, "named-map", 0,
                MemoryMappedFileAccess.Read, HandleInheritability.None, true).Dispose());
            FileStream closed = File.OpenRead(existing);
            closed.Dispose();
            Probe("stream named closed", () => MemoryMappedFile.CreateFromFile(closed, "named-map", 0,
                MemoryMappedFileAccess.Read, HandleInheritability.None, true).Dispose(), false);
            Probe("stream named inheritability", () => MemoryMappedFile.CreateFromFile(stream,
                "named-map", 0, MemoryMappedFileAccess.Read, (HandleInheritability)99, true).Dispose());
            using RefusingFlushStream refusing = new RefusingFlushStream(existing);
            Probe("stream flush before capacity", () => MemoryMappedFile.CreateFromFile(refusing,
                "named-map", 32, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true).Dispose());
            string growingPath = Path.Combine(dir, "named-growing.bin");
            File.WriteAllBytes(growingPath, new byte[16]);
            using GrowingFlushStream growing = new GrowingFlushStream(growingPath);
            Probe("stream captured length", () => MemoryMappedFile.CreateFromFile(growing,
                "named-map", 32, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true).Dispose());
            Console.WriteLine("stream grew=" + growing.Length);
            using LongerLengthStream longer = new LongerLengthStream(existing);
            Probe("stream overridden length", () => MemoryMappedFile.CreateFromFile(longer,
                "named-map", 64, MemoryMappedFileAccess.Read, HandleInheritability.None, true).Dispose());
            using NegativeLengthStream negative = new NegativeLengthStream(existing);
            Probe("stream negative length", () => MemoryMappedFile.CreateFromFile(negative,
                "named-map", 32, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true).Dispose());
            using InvalidHandleStream invalidHandle = new InvalidHandleStream(existing);
            Probe("stream named invalid handle", () => MemoryMappedFile.CreateFromFile(invalidHandle,
                "named-map", 64, MemoryMappedFileAccess.Read, HandleInheritability.None, true).Dispose());
            string shorterPath = Path.Combine(dir, "unnamed-shorter.bin");
            File.WriteAllBytes(shorterPath, new byte[64]);
            using LongerLengthStream longerUnnamed = new LongerLengthStream(shorterPath);
            using (MemoryMappedFile unnamed = MemoryMappedFile.CreateFromFile(longerUnnamed,
                null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true))
                Console.WriteLine("stream unnamed created=" + (unnamed is not null)
                    + ":" + new FileInfo(shorterPath).Length);
            string growUnnamedPath = Path.Combine(dir, "unnamed-growing.bin");
            File.WriteAllBytes(growUnnamedPath, new byte[16]);
            using GrowingFlushStream growingUnnamed = new GrowingFlushStream(growUnnamedPath);
            using (MemoryMappedFile unnamed = MemoryMappedFile.CreateFromFile(growingUnnamed,
                null, 32, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true))
                Console.WriteLine("stream unnamed grew=" + new FileInfo(growUnnamedPath).Length);
            using SafeFileHandle emptyHandle = File.OpenHandle(empty, FileMode.Open, FileAccess.Read);
            Probe("handle named empty", () => MemoryMappedFile.CreateFromFile(emptyHandle,
                "named-map", 0, MemoryMappedFileAccess.Read, HandleInheritability.None, true).Dispose());
        }
        Console.WriteLine("mmap missing paths complete");
    }

    // `message` prints the text too, for the accessor checks whose sentences CoreLib owns.
    private static void ProbeArgument(string label, Action action, bool message = false)
    {
        try
        {
            action();
            Console.WriteLine($"mmap args {label}: no exception");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"mmap args {label}: {ex.GetType().Name} param={ex.ParamName ?? "<null>"}");
            if (message) Console.WriteLine($"  message={ex.Message.Replace(Environment.NewLine, "|")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"mmap args {label}: {ex.GetType().Name}");
        }
    }

    // The rejections of the map factory, views and accessors: .NET's exception type and
    // the parameter it names, checked in .NET's order.
    private static void TestArgumentNames(string dir)
    {
        string path = Path.Combine(dir, "args.bin");
        File.WriteAllBytes(path, new byte[64]);
        ProbeArgument("from file null path", () => MemoryMappedFile.CreateFromFile((string)null, FileMode.Open));
        ProbeArgument("from file negative capacity",
            () => MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, -1, MemoryMappedFileAccess.ReadWrite));
        using MemoryMappedFile map = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 64,
            MemoryMappedFileAccess.ReadWrite);
        ProbeArgument("view negative offset", () => map.CreateViewAccessor(-1, 8).Dispose());
        ProbeArgument("view negative size", () => map.CreateViewAccessor(0, -1).Dispose());
        ProbeArgument("view past end", () => map.CreateViewAccessor(60, 8).Dispose());
        using MemoryMappedViewAccessor view = map.CreateViewAccessor(0, 64);
        int[] data = { 1, 2, 3, 4 };
        ProbeArgument("write array negative position", () => view.WriteArray(-1, data, 0, 4));
        ProbeArgument("write array past end", () => view.WriteArray(56, data, 0, 4));
        ProbeArgument("write array at capacity", () => view.WriteArray(64, data, 0, 4));
        ProbeArgument("read negative position", () => view.ReadInt32(-1), true);
        ProbeArgument("read straddling end", () => view.ReadInt32(62), true);
        ProbeArgument("read at capacity", () => view.ReadInt64(64), true);
        ProbeArgument("write straddling end", () => view.Write(61, 7), true);
        ProbeArgument("write negative position", () => view.Write(-1, (byte)7), true);
        ProbeArgument("read struct straddling end", () => view.Read(60, out long _), true);
        ProbeArgument("write struct negative position", () =>
        {
            long value = 1;
            view.Write(-1, ref value);
        }, true);
        ProbeArgument("read array null", () => view.ReadArray(0, (int[])null, 0, 1), true);
        ProbeArgument("read array negative offset", () => view.ReadArray(0, data, -1, 1), true);
        ProbeArgument("read array negative count", () => view.ReadArray(0, data, 0, -1), true);
        ProbeArgument("read array short", () => view.ReadArray(0, data, 2, 4), true);
        ProbeArgument("read array at capacity", () => view.ReadArray(64, data, 0, 1), true);
        ProbeArgument("write array null", () => view.WriteArray(0, (int[])null, 0, 1), true);
        using (MemoryMappedViewAccessor readOnly = map.CreateViewAccessor(0, 8, MemoryMappedFileAccess.Read))
            ProbeArgument("write read-only view", () => readOnly.Write(0, 1), true);
        MemoryMappedViewAccessor closed = map.CreateViewAccessor(0, 8);
        closed.Dispose();
        ProbeArgument("read closed view", () => closed.ReadInt32(0), true);
        ProbeArgument("read closed view negative position", () => closed.ReadInt32(-1), true);
        ProbeArgument("read struct closed view negative position", () => closed.Read(-1, out int _), true);
        ProbeArgument("write array closed view at capacity", () => closed.WriteArray(8, data, 0, 1), true);
        Console.WriteLine($"mmap args untouched: {view.ReadInt32(0)} {view.ReadInt32(56)} {view.ReadInt32(60)}"
            + $" {string.Join(",", data)}");
        Console.WriteLine("mmap args complete");
    }

    // The sentence of each factory, view and accessor rejection, and the checks the path
    // factory runs against the size of the file it opens; a file the refused call created
    // is gone again. A directory's refusal names the path, so only its type prints.
    private static void TestArgumentMessages(string dir)
    {
        string path = Path.Combine(dir, "messages.bin");
        File.WriteAllBytes(path, new byte[64]);
        string emptyPath = Path.Combine(dir, "messages-empty.bin");
        File.WriteAllBytes(emptyPath, new byte[0]);
        const MemoryMappedFileAccess rw = MemoryMappedFileAccess.ReadWrite;
        const MemoryMappedFileAccess read = MemoryMappedFileAccess.Read;
        const MemoryMappedFileAccess write = MemoryMappedFileAccess.Write;
        const MemoryMappedFileAccess badAccess = (MemoryMappedFileAccess)99;
        const HandleInheritability none = HandleInheritability.None;
        const HandleInheritability badInheritability = (HandleInheritability)99;
        void FromPath(string label, string file, FileMode mode, string mapName, long capacity,
            MemoryMappedFileAccess access)
            => ProbeArgument("message from file " + label,
                () => MemoryMappedFile.CreateFromFile(file, mode, mapName, capacity, access).Dispose(), true);
        FromPath("map name", path, FileMode.Open, "", 0, rw);
        FromPath("negative capacity", path, FileMode.Open, null, -1, rw);
        FromPath("access", path, FileMode.Open, null, 0, badAccess);
        FromPath("write access", path, FileMode.Open, null, 0, write);
        FromPath("append", path, FileMode.Append, null, 0, rw);
        FromPath("truncate", path, FileMode.Truncate, null, 0, rw);
        FromPath("write access before mode", path, FileMode.Truncate, null, 0, write);
        FromPath("empty path", "", FileMode.Open, null, 0, rw);
        FromPath("append before empty path", "", FileMode.Append, null, 0, rw);
        FromPath("mode", path, (FileMode)99, null, 0, rw);
        FromPath("empty path before mode", "", (FileMode)99, null, 0, rw);
        FromPath("nul path", path + "\0x", FileMode.Open, null, 0, rw);
        FromPath("mode before nul path", path + "\0x", (FileMode)99, null, 0, rw);
        FromPath("read grows", path, FileMode.Open, null, 65, read);
        FromPath("smaller", path, FileMode.Open, null, 1, rw);
        FromPath("empty file", emptyPath, FileMode.Open, null, 0, rw);
        string created = Path.Combine(dir, "messages-created.bin");
        FromPath("created empty", created, FileMode.OpenOrCreate, null, 0, rw);
        Console.WriteLine($"mmap message created file kept={File.Exists(created)}");
        FromPath("directory", dir, FileMode.Open, null, 0, rw);
        FromPath("directory read", dir, FileMode.Open, null, 0, read);
        Console.WriteLine($"mmap message file untouched={new FileInfo(path).Length}");

        using (FileStream input = File.OpenRead(path))
        using (FileStream empty = File.OpenRead(emptyPath))
        {
            void FromStream(string label, FileStream stream, string mapName, long capacity,
                MemoryMappedFileAccess access, HandleInheritability inheritability)
                => ProbeArgument("message from stream " + label,
                    () => MemoryMappedFile.CreateFromFile(stream, mapName, capacity, access, inheritability, true).Dispose(),
                    true);
            FromStream("map name", input, "", 0, read, none);
            FromStream("negative capacity", input, null, -1, read, none);
            FromStream("access", input, null, 0, badAccess, none);
            FromStream("write access", input, null, 0, write, none);
            FromStream("empty", empty, null, 0, read, none);
            FromStream("empty before inheritability", empty, null, 0, read, badInheritability);
            FromStream("inheritability before read grows", input, null, 65, read, badInheritability);
            FromStream("read grows", input, null, 65, read, none);
            FromStream("smaller", input, null, 1, read, none);
        }
        using (SafeFileHandle handle =
            File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (SafeFileHandle emptyHandle =
            File.OpenHandle(emptyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            void FromHandle(string label, SafeFileHandle source, long capacity,
                HandleInheritability inheritability)
                => ProbeArgument("message from handle " + label,
                    () => MemoryMappedFile.CreateFromFile(source, null, capacity, read, inheritability, true).Dispose(),
                    true);
            FromHandle("empty before inheritability", emptyHandle, 0, badInheritability);
            FromHandle("inheritability", handle, 0, badInheritability);
            FromHandle("read grows", handle, 65, none);
            FromHandle("smaller", handle, 1, none);
        }

        using MemoryMappedFile map = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 64, rw);
        ProbeArgument("message view negative size", () => map.CreateViewAccessor(0, -1).Dispose(), true);
        ProbeArgument("message view negative size before access",
            () => map.CreateViewAccessor(0, -1, badAccess).Dispose(), true);
        ProbeArgument("message view access", () => map.CreateViewAccessor(0, 8, badAccess).Dispose(), true);
        ProbeArgument("message view access before offset", () => map.CreateViewAccessor(100, 8, badAccess).Dispose(), true);
        ProbeArgument("message view offset past end", () => map.CreateViewAccessor(100, 8).Dispose(), true);
        ProbeArgument("message view past end", () => map.CreateViewAccessor(60, 8).Dispose(), true);
        using MemoryMappedViewAccessor view = map.CreateViewAccessor(0, 64);
        int[] data = { 1, 2, 3, 4 };
        ProbeArgument("message write array negative position", () => view.WriteArray(-1, data, 0, 4), true);
        ProbeArgument("message write array past end", () => view.WriteArray(56, data, 0, 4), true);
        ProbeArgument("message write array at capacity", () => view.WriteArray(64, data, 0, 4), true);
        ProbeArgument("message read array negative position", () => view.ReadArray(-1, data, 0, 4), true);
        Console.WriteLine("mmap argument messages complete");
    }

    private static void TestOffsetAndWriteMessages(string dir)
    {
        Console.WriteLine("== mmap position messages ==");
        string path = Path.Combine(dir, "positions.bin");
        File.WriteAllBytes(path, new byte[64]);
        using MemoryMappedFile map = MemoryMappedFile.CreateFromFile(path, FileMode.Open);
        ProbeArgument("position view negative offset", () => map.CreateViewAccessor(-1, 8).Dispose(), true);
        using MemoryMappedViewAccessor view = map.CreateViewAccessor(0, 64);
        ProbeArgument("position write at end byte", () => view.Write(64, (byte)7), true);
        Console.WriteLine("mmap position messages end");
    }

    // A view past the map: Unix .NET refuses the range against the map's capacity, which
    // outlives Dispose, before the closed handle; Windows .NET widens the view down to the
    // allocation granularity and lets MapViewOfFile refuse it, so a view of the rest past
    // the end maps the file's zero-filled last page. The path factory's all-space name
    // passes its empty check and only Windows's Path.GetFullPath refuses it.
    private static void TestViewRanges(string dir)
    {
        string path = Path.Combine(dir, "ranges.bin");
        File.WriteAllBytes(path, new byte[64]);
        const MemoryMappedFileAccess rw = MemoryMappedFileAccess.ReadWrite;
        using (MemoryMappedFile map = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 64, rw))
        {
            void View(string label, long offset, long size)
                => ProbeArgument("view range " + label, () =>
                {
                    using MemoryMappedViewAccessor view = map.CreateViewAccessor(offset, size);
                    Console.WriteLine($"mmap view range {label} capacity={view.Capacity}");
                }, true);
            View("rest at end", 64, 0);
            View("rest past end", 100, 0);
            View("rest past page", 8192, 0);
            View("rest past granularity", 70000, 0);
            View("past granularity", 70000, 8);
            View("size past address space", 0, 8192000000001);
            View("size past virtual memory", 0, 1L << 62);
        }
        // Windows .NET leaves the view of the rest past the page mapped when its accessor
        // refuses the negative capacity, and that view holds the file open until it is
        // finalized, so the later maps open a fresh file.
        path = Path.Combine(dir, "ranges-reopened.bin");
        File.WriteAllBytes(path, new byte[64]);
        MemoryMappedFile disposed = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 64, rw);
        disposed.Dispose();
        ProbeArgument("view range disposed offset past end", () => disposed.CreateViewAccessor(100, 8).Dispose(), true);
        ProbeArgument("view range disposed", () => disposed.CreateViewAccessor(0, 8).Dispose(), true);
        using (MemoryMappedFile read = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0,
            MemoryMappedFileAccess.Read))
        {
            ProbeArgument("view range write view of read map", () => read.CreateViewAccessor(0, 8).Dispose(), true);
        }
        ProbeArgument("view range blank path",
            () => MemoryMappedFile.CreateFromFile("   ", FileMode.Open, null, 0, rw).Dispose(), true);
        ProbeArgument("view range mode before blank path",
            () => MemoryMappedFile.CreateFromFile("   ", (FileMode)99, null, 0, rw).Dispose(), true);
        Console.WriteLine("mmap view ranges complete");
    }

    private sealed class ObservedStream : FileStream
    {
        public bool LengthRead;
        public bool Flushed;
        public bool HandleRead;
        public bool Disposed;

        public ObservedStream(string path) : base(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)
        {
        }

        public override long Length { get { LengthRead = true; return base.Length; } }
        public override SafeFileHandle SafeFileHandle { get { HandleRead = true; return base.SafeFileHandle; } }
        public override void Flush() { Flushed = true; base.Flush(); }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }

    private static unsafe void TestStreamMaps(string dir)
    {
        string path = Path.Combine(dir, "stream.bin");
        File.WriteAllBytes(path, new byte[64]);
        var stream = new ObservedStream(path);
        stream.Position = 5;
        stream.WriteByte(173);
        MemoryMappedFile map = MemoryMappedFile.CreateFromFile(stream, null, 128,
            MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);
        Console.WriteLine($"mmap stream virtual length={stream.LengthRead} flush={stream.Flushed} handle={stream.HandleRead} position={stream.Position} length={stream.Length}");
        MemoryMappedViewAccessor view = map.CreateViewAccessor(5, 16, MemoryMappedFileAccess.ReadWrite);
        byte* pointer = null;
        SafeMemoryMappedViewHandle viewHandle = view.SafeMemoryMappedViewHandle;
        viewHandle.AcquirePointer(ref pointer);
        Console.WriteLine($"mmap stream pointer offset={view.PointerOffset} byte={pointer[view.PointerOffset]} bytes={viewHandle.ByteLength}");
        viewHandle.ReleasePointer();
        view.Write(1, (byte)91);
        view.Flush();
        map.Dispose();
        map.Dispose();
        Console.WriteLine($"mmap stream leaveOpen={stream.CanRead} closed={stream.SafeFileHandle.IsClosed} view={view.ReadByte(1)}");
        view.Dispose();
        stream.Position = 5;
        Console.WriteLine($"mmap stream persisted={stream.ReadByte()},{stream.ReadByte()}");
        stream.Dispose();

        var ownedStream = new ObservedStream(path);
        SafeFileHandle ownedHandle = ownedStream.SafeFileHandle;
        MemoryMappedFile ownedMap = MemoryMappedFile.CreateFromFile(ownedStream, null, 0,
            MemoryMappedFileAccess.Read, HandleInheritability.None, false);
        MemoryMappedViewAccessor ownedView = ownedMap.CreateViewAccessor(5, 2, MemoryMappedFileAccess.Read);
        ownedMap.Dispose();
        Console.WriteLine($"mmap stream owned closed={ownedHandle.IsClosed} canRead={ownedStream.CanRead} disposed={ownedStream.Disposed} view={ownedView.ReadByte(0)}");
        ownedView.Dispose();
        ownedStream.Dispose();

        foreach (bool leaveOpen in new[] { true, false })
        {
            SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            MemoryMappedFile handleMap = MemoryMappedFile.CreateFromFile(handle, null, 0,
                MemoryMappedFileAccess.Read, HandleInheritability.Inheritable, leaveOpen);
            MemoryMappedViewAccessor handleView = handleMap.CreateViewAccessor(5, 2, MemoryMappedFileAccess.Read);
            handleMap.Dispose();
            Console.WriteLine($"mmap handle leaveOpen={leaveOpen} closed={handle.IsClosed} view={handleView.ReadByte(1)}");
            handleView.Dispose();
            handle.Dispose();
        }

        using (FileStream input = File.OpenRead(path))
        {
            ProbeStreamMap("negative", input, -1, MemoryMappedFileAccess.Read, HandleInheritability.None);
            ProbeStreamMap("smaller", input, 1, MemoryMappedFileAccess.Read, HandleInheritability.None);
            ProbeStreamMap("read-grow", input, 129, MemoryMappedFileAccess.Read, HandleInheritability.None);
            ProbeStreamMap("access", input, 0, (MemoryMappedFileAccess)99, HandleInheritability.None);
            ProbeStreamMap("write", input, 0, MemoryMappedFileAccess.Write, HandleInheritability.None);
            ProbeStreamMap("inherit", input, 0, MemoryMappedFileAccess.Read, (HandleInheritability)99);
            Console.WriteLine($"mmap stream failures leaveOpen={input.CanRead} closed={input.SafeFileHandle.IsClosed}");
        }
        ProbeStreamMap("null", null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None);
        using (FileStream empty = File.Create(Path.Combine(dir, "empty.bin")))
            ProbeStreamMap("empty", empty, 0, MemoryMappedFileAccess.Read, HandleInheritability.None);
        FileStream closed = File.OpenRead(path);
        closed.Dispose();
        ProbeStreamMap("closed", closed, 0, MemoryMappedFileAccess.Read, HandleInheritability.None);
        TestViewReferences(path);
        TestReadMapAccess(path);
        TestCollectedMapSource(path);
        Console.WriteLine("mmap stream factories complete");
        return;
    }

    private static void TestCollectedMapSource(string path)
    {
        using FileStream stream = File.OpenRead(path);
        WeakReference mapReference = null;
        // Removing the creator's stack prevents stale words from retaining the map in Boehm.
        var creator = new Thread(() =>
        {
            var map = MemoryMappedFile.CreateFromFile(stream, null, 0,
                MemoryMappedFileAccess.Read, HandleInheritability.None, false);
            mapReference = new WeakReference(map);
        });
        creator.Start();
        creator.Join();
        for (int rounds = 0; mapReference.IsAlive && rounds < 64; rounds++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        if (mapReference.IsAlive)
        {
            throw new InvalidOperationException("The unrooted map was not collected.");
        }
        Console.WriteLine($"mmap collected source open={!stream.SafeFileHandle.IsClosed} readable={stream.ReadByte() >= 0}");
        GC.KeepAlive(stream);
        return;
    }

    private static unsafe void TestViewReferences(string path)
    {
        using MemoryMappedFile map = MemoryMappedFile.CreateFromFile(path, FileMode.Open);
        MemoryMappedViewAccessor view = map.CreateViewAccessor(5, 16, MemoryMappedFileAccess.Read);
        object boxed = view;
        IDisposable disposable = view;
        UnmanagedMemoryAccessor accessor = (UnmanagedMemoryAccessor)boxed;
        Console.WriteLine($"mmap view identity={ReferenceEquals(view, disposable)} base={ReferenceEquals(view, accessor)} type={view.GetType() == typeof(MemoryMappedViewAccessor)} baseType={view.GetType().BaseType == typeof(UnmanagedMemoryAccessor)}");
        SafeMemoryMappedViewHandle handle = view.SafeMemoryMappedViewHandle;
        SafeBuffer buffer = handle;
        byte* pointer = null;
        buffer.AcquirePointer(ref pointer);
        disposable.Dispose();
        Console.WriteLine($"mmap view handle identity={ReferenceEquals(handle, view.SafeMemoryMappedViewHandle)} capacity={accessor.Capacity} offset={view.PointerOffset} leased={pointer[view.PointerOffset]}");
        buffer.ReleasePointer();
        Console.WriteLine($"mmap view released closed={handle.IsClosed}");
        try
        {
            Console.WriteLine($"mmap view disposed read={accessor.ReadByte(0)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"mmap view disposed read={ex.GetType().Name}");
        }
        view.Dispose();
        MemoryMappedViewAccessor handleOwnedView = map.CreateViewAccessor(5, 16, MemoryMappedFileAccess.Read);
        handleOwnedView.SafeMemoryMappedViewHandle.Dispose();
        try
        {
            Console.WriteLine($"mmap view closed handle read={handleOwnedView.ReadByte(0)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"mmap view closed handle read={ex.GetType().Name}");
        }
        handleOwnedView.Dispose();
        return;
    }

    private static void TestReadMapAccess(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        using MemoryMappedFile map = MemoryMappedFile.CreateFromFile(stream, null, 0,
            MemoryMappedFileAccess.Read, HandleInheritability.None, true);
        for (int overload = 0; overload < 3; overload++)
        {
            try
            {
                MemoryMappedViewAccessor view = overload == 0 ? map.CreateViewAccessor()
                    : overload == 1 ? map.CreateViewAccessor(0, 16)
                    : map.CreateViewAccessor(0, 16, MemoryMappedFileAccess.ReadWrite);
                view.Dispose();
                Console.WriteLine($"mmap read map overload={overload} write=allowed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"mmap read map overload={overload} write={ex.GetType().Name}");
            }
        }
        using MemoryMappedViewAccessor readable = map.CreateViewAccessor(5, 16, MemoryMappedFileAccess.Read);
        Console.WriteLine($"mmap read map readable={readable.ReadByte(0)}");
        return;
    }

    private static void ProbeStreamMap(string label, FileStream stream, long capacity,
        MemoryMappedFileAccess access, HandleInheritability inheritability)
    {
        try
        {
            MemoryMappedFile map = MemoryMappedFile.CreateFromFile(stream, null, capacity, access, inheritability, false);
            map.Dispose();
            Console.WriteLine($"mmap stream {label}=none");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"mmap stream {label}={ex.GetType().Name}");
        }
        return;
    }

    private static void TestReferenceExchange(string path)
    {
        MemoryMappedFile first = MemoryMappedFile.CreateFromFile(
            path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        MemoryMappedFile second = MemoryMappedFile.CreateFromFile(
            path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        MemoryMappedFile alias = first;
        object boxed = first;
        MemoryMappedFile[] maps = { first, second };
        MapSlot slot = new MapSlot { Value = first };

        MemoryMappedFile previous = Interlocked.Exchange(ref slot.Value, second);
        Console.WriteLine($"mmap exchange identity={ReferenceEquals(previous, first)} replacement={ReferenceEquals(slot.Value, second)} alias={ReferenceEquals(previous, alias)}");
        Console.WriteLine($"mmap object identity={ReferenceEquals(boxed, first)} array={ReferenceEquals(maps[0], first)} distinct={!ReferenceEquals(first, second)}");
        Console.WriteLine($"mmap runtime type={first.GetType() == typeof(MemoryMappedFile)} objectType={boxed.GetType() == typeof(MemoryMappedFile)}");
        MemoryMappedFile castMap = (MemoryMappedFile)boxed;
        IDisposable castDisposable = (IDisposable)boxed;
        Console.WriteLine($"mmap object cast={ReferenceEquals(castMap, first)} isMap={boxed is MemoryMappedFile} disposable={ReferenceEquals(castDisposable, first)} isDisposable={boxed is IDisposable}");

        previous = Interlocked.CompareExchange(ref slot.Value, first, first);
        Console.WriteLine($"mmap compare mismatch old={ReferenceEquals(previous, second)} unchanged={ReferenceEquals(slot.Value, second)}");
        previous = Interlocked.CompareExchange(ref slot.Value, first, second);
        Console.WriteLine($"mmap compare match old={ReferenceEquals(previous, second)} replacement={ReferenceEquals(slot.Value, first)}");

        previous = Interlocked.Exchange(ref slot.Value, null);
        Console.WriteLine($"mmap exchange null old={ReferenceEquals(previous, first)} cleared={slot.Value is null}");
        previous = Interlocked.Exchange(ref slot.Value, null);
        Console.WriteLine($"mmap exchange empty old={previous is null} cleared={slot.Value is null}");
        previous = Interlocked.CompareExchange(ref slot.Value, second, null);
        Console.WriteLine($"mmap compare null old={previous is null} replacement={ReferenceEquals(slot.Value, second)}");
        previous = Interlocked.CompareExchange(ref slot.Value, null, first);
        Console.WriteLine($"mmap compare clear mismatch old={ReferenceEquals(previous, second)} unchanged={ReferenceEquals(slot.Value, second)}");
        previous = Interlocked.CompareExchange(ref slot.Value, null, second);
        Console.WriteLine($"mmap compare clear match old={ReferenceEquals(previous, second)} cleared={slot.Value is null}");
        Interlocked.Exchange(ref slot.Value, second);
        previous = Interlocked.Exchange(ref maps[0], null);
        Console.WriteLine($"mmap exchange array old={ReferenceEquals(previous, first)} cleared={maps[0] is null} alias={ReferenceEquals(alias, first)}");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        MemoryMappedViewAccessor liveView = alias.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
        Console.WriteLine($"mmap alias read={liveView.ReadInt32(0)}");
        GC.KeepAlive(alias);
        IDisposable disposable = first;
        disposable.Dispose();
        alias.Dispose();
        bool disposed = false;
        try
        {
            MemoryMappedViewAccessor unexpected = first.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            unexpected.Dispose();
        }
        catch (ObjectDisposedException)
        {
            disposed = true;
        }
        Console.WriteLine($"mmap disposed alias={disposed} liveView={liveView.ReadInt32(0)}");
        liveView.Dispose();

        MemoryMappedViewAccessor secondView = slot.Value.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
        Console.WriteLine($"mmap replacement read={secondView.ReadInt32(0)}");
        secondView.Dispose();
        int ready = 0;
        int start = 0;
        int claims = 0;
        int identities = 0;
        MemoryMappedFile winner = null;
        Thread[] threads = new Thread[8];
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() =>
            {
                Interlocked.Increment(ref ready);
                while (Volatile.Read(ref start) == 0)
                {
                    Thread.Yield();
                }
                MemoryMappedFile claimed = Interlocked.Exchange(ref slot.Value, null);
                if (claimed is not null)
                {
                    Interlocked.Increment(ref claims);
                    if (ReferenceEquals(claimed, second))
                    {
                        Interlocked.Increment(ref identities);
                    }
                    winner = claimed;
                }
            });
            threads[i].Start();
        }
        while (Volatile.Read(ref ready) != threads.Length)
        {
            Thread.Yield();
        }
        Volatile.Write(ref start, 1);
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i].Join();
        }
        Console.WriteLine($"mmap concurrent claims={claims} identities={identities} cleared={slot.Value is null} winner={ReferenceEquals(winner, second)}");
        winner.Dispose();
        maps[1].Dispose();
        Console.WriteLine("mmap reference exchange complete");
        return;
    }
#endif

    private static void TestUninitializedMap()
    {
        MemoryMappedFile map = (MemoryMappedFile)RuntimeHelpers.GetUninitializedObject(typeof(MemoryMappedFile));
        MemoryMappedFile alias = map;
        object boxed = map;
        Console.WriteLine($"mmap uninitialized disposable={boxed is IDisposable}");
        IDisposable disposable = (IDisposable)boxed;
        Console.WriteLine($"mmap uninitialized type={map.GetType() == typeof(MemoryMappedFile)} alias={ReferenceEquals(alias, disposable)}");
        try
        {
            disposable.Dispose();
            Console.WriteLine("mmap uninitialized dispose=none");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"mmap uninitialized dispose={ex.GetType().Name}");
        }
        try
        {
            alias.Dispose();
            Console.WriteLine("mmap uninitialized alias dispose=none");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"mmap uninitialized alias dispose={ex.GetType().Name}");
        }
        try
        {
            MemoryMappedViewAccessor view = map.CreateViewAccessor();
            view.Dispose();
            Console.WriteLine("mmap uninitialized view=none");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"mmap uninitialized view={ex.GetType().Name}");
        }
        Console.WriteLine("mmap uninitialized complete");
        return;
    }
}
