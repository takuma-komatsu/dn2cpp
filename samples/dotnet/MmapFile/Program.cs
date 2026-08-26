using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Globalization;

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
    private struct Rec
    {
        public int Id;
        public double Val;
        public long Extra;
    } // 24 bytes (Id, pad, Val, Extra) — word-or-larger fields, so C++/.NET layouts agree

    private static unsafe void Main(string[] args)
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

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
    }
}
