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
#endif
    }

#if !MMAP_UNINITIALIZED_ONLY
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
