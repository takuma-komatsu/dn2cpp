using System;
using System.Runtime.InteropServices;

// Raw native (GC-unmanaged) heap allocation.
// NativeMemory.{Alloc,AllocZeroed,Free,Realloc} (void* handles) and
// Marshal.{AllocHGlobal,FreeHGlobal} (IntPtr handles) + Marshal.Copy
// (memcpy between a managed blittable array and a native pointer, both
// directions). Each lowers to the dn2cpp_native_* C-heap wrappers / memcpy;
// the block lives outside the GC (plain std::malloc/calloc/realloc/free even
// under Boehm GC), so only blittable data is stored. Covers both Alloc
// overloads (byteCount and the uninitialized elementCount x elementSize
// product), both AllocZeroed overloads, Realloc-grows-and-preserves, both
// AllocHGlobal overloads (int / IntPtr), and Marshal.Copy round trips for
// byte[] / int[] (I4 rep) / double[] / long[] (wide N rep) including
// startIndex offsets on both ends. Output is fully deterministic (we fill
// before we read, so the uninitialized Alloc forms never expose
// nondeterministic bytes). Diffed exact vs real .NET.
namespace NativeMemorySubset;

unsafe class Program
{
    internal static void __GateEntry()
    {
        nuint isz = (nuint)sizeof(int);
        nuint lsz = (nuint)sizeof(long);

        // NativeMemory.Alloc(nuint byteCount): uninitialized — fill, then read.
        byte* a = (byte*)NativeMemory.Alloc(8);
        for (int i = 0; i < 8; i++) a[i] = (byte)(i * 3);
        int sum = 0;
        for (int i = 0; i < 8; i++) sum += a[i];
        Console.WriteLine(sum);                          // 84
        NativeMemory.Free(a);

        // NativeMemory.Alloc(elementCount, elementSize): uninitialized product.
        int* p = (int*)NativeMemory.Alloc((nuint)4, isz);
        for (int i = 0; i < 4; i++) p[i] = (i + 1) * 100;
        Console.WriteLine(p[0] + p[1] + p[2] + p[3]);    // 1000

        // NativeMemory.Realloc: grow; the original 4 ints are preserved.
        p = (int*)NativeMemory.Realloc(p, (nuint)8 * isz);
        for (int i = 4; i < 8; i++) p[i] = (i + 1) * 100;
        long total = 0;
        for (int i = 0; i < 8; i++) total += p[i];
        Console.WriteLine(p[0]);                         // 100 (preserved)
        Console.WriteLine(total);                         // 3600
        NativeMemory.Free(p);

        // NativeMemory.AllocZeroed(byteCount): every byte is zero.
        byte* z = (byte*)NativeMemory.AllocZeroed(16);
        int nz = 0;
        for (int i = 0; i < 16; i++) if (z[i] != 0) nz++;
        Console.WriteLine(nz);                           // 0
        NativeMemory.Free(z);

        // NativeMemory.AllocZeroed(elementCount, elementSize): every slot zero.
        long* zl = (long*)NativeMemory.AllocZeroed((nuint)4, lsz);
        long zsum = 0;
        for (int i = 0; i < 4; i++) zsum += zl[i];
        Console.WriteLine(zsum);                         // 0
        NativeMemory.Free(zl);

        // Marshal.AllocHGlobal(int) -> IntPtr.
        IntPtr h = Marshal.AllocHGlobal(4 * sizeof(int));
        int* hp = (int*)h;
        for (int i = 0; i < 4; i++) hp[i] = i * i;
        Console.WriteLine(hp[0] + hp[1] + hp[2] + hp[3]); // 14
        Marshal.FreeHGlobal(h);

        // Marshal.AllocHGlobal(IntPtr) overload.
        IntPtr h2 = Marshal.AllocHGlobal((IntPtr)8);
        byte* h2p = (byte*)h2;
        h2p[0] = 200; h2p[7] = 55;
        Console.WriteLine(h2p[0] + h2p[7]);              // 255
        Marshal.FreeHGlobal(h2);

        // Marshal.Copy(byte[]) round trip: managed -> native -> managed.
        byte[] bsrc = { 1, 2, 3, 4, 5, 6 };
        IntPtr buf = Marshal.AllocHGlobal(6);
        Marshal.Copy(bsrc, 0, buf, 6);
        byte[] bdst = new byte[6];
        Marshal.Copy(buf, bdst, 0, 6);
        Console.WriteLine(bdst[0]);                      // 1
        Console.WriteLine(bdst[5]);                      // 6
        // startIndex offsets on both ends: src[2..6) -> buf[0..4) -> dst2[1..5).
        byte[] bdst2 = new byte[6];
        Marshal.Copy(bsrc, 2, buf, 4);
        Marshal.Copy(buf, bdst2, 1, 4);
        Console.WriteLine(bdst2[0]);                     // 0 (untouched)
        Console.WriteLine(bdst2[1]);                     // 3
        Console.WriteLine(bdst2[4]);                     // 6
        Console.WriteLine(bdst2[5]);                     // 0 (untouched)
        Marshal.FreeHGlobal(buf);

        // Marshal.Copy(int[]) round trip.
        int[] isrc = { 10, 20, 30, 40 };
        IntPtr ibuf = Marshal.AllocHGlobal(4 * sizeof(int));
        Marshal.Copy(isrc, 0, ibuf, 4);
        int[] idst = new int[4];
        Marshal.Copy(ibuf, idst, 0, 4);
        Console.WriteLine(idst[0]);                      // 10
        Console.WriteLine(idst[3]);                      // 40
        Marshal.FreeHGlobal(ibuf);

        // Marshal.Copy(double[]) round trip.
        double[] dsrc = { 1.5, 2.5, 3.5 };
        IntPtr dbuf = Marshal.AllocHGlobal(3 * sizeof(double));
        Marshal.Copy(dsrc, 0, dbuf, 3);
        double[] ddst = new double[3];
        Marshal.Copy(dbuf, ddst, 0, 3);
        Console.WriteLine(ddst[1]);                      // 2.5
        Console.WriteLine(ddst[0] + ddst[1] + ddst[2]);  // 7.5
        Marshal.FreeHGlobal(dbuf);

        // Marshal.Copy(long[]) round trip (wide N-rep elements).
        long[] lsrc = { 1L, 1000000000000L, -5L };
        IntPtr lbuf = Marshal.AllocHGlobal(3 * sizeof(long));
        Marshal.Copy(lsrc, 0, lbuf, 3);
        long[] ldst = new long[3];
        Marshal.Copy(lbuf, ldst, 0, 3);
        Console.WriteLine(ldst[1]);                      // 1000000000000
        Console.WriteLine(ldst[2]);                      // -5
        Marshal.FreeHGlobal(lbuf);
    }
}
