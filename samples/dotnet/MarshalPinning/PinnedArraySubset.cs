#nullable enable
using System;
using System.Runtime.InteropServices;

// GCHandle pinning over arbitrary blittable types: pin a byte[], an int[], a
// double[], and a blittable-struct array; round-trip values both ways (write
// through the raw AddrOfPinnedObject pointer -> observe via managed indexing, and
// vice-versa). Also exercises the non-pinned Alloc(obj) form, get_Target, and
// get_IsAllocated BEFORE Free, plus the new GCHandle(obj) ctor-equivalent path.
// dn2cpp's heap is non-moving (Boehm/calloc), so pinning is identity and the raw
// pointer aliases the managed array's backing store exactly. The struct Pt uses
// only word-or-larger fields, so its C++ and .NET layouts are byte-identical.
// IsAllocated is read only before Free (dn2cpp's identity-pinning Free is a no-op,
// so it does not invalidate the handle — that after-Free state is carved out).
// No raw addresses are printed. CoreLib only; diffed exact vs real .NET.
namespace PinnedArraySubset;

struct Pt { public int X; public long Y; public double Z; }   // 24 bytes, word+ fields only

unsafe class Program
{
    internal static void __GateEntry()
    {
        // --- byte[] : write through the pin, observe via managed indexing ---
        byte[] bytes = new byte[8];
        for (int i = 0; i < 8; i++) bytes[i] = (byte)i;
        GCHandle hb = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        Console.WriteLine(hb.IsAllocated);                            // True (before Free)
        byte* pb = (byte*)hb.AddrOfPinnedObject();
        for (int i = 0; i < 8; i++) pb[i] = (byte)(pb[i] * 2 + 1);    // 1,3,5,...,15
        long bsum = 0;
        for (int i = 0; i < 8; i++) bsum += bytes[i];
        Console.WriteLine(bsum);                                      // 64
        bytes[3] = 200;                                              // managed write
        Console.WriteLine(pb[3]);                                     // 200 (seen through the pin)
        hb.Free();

        // --- int[] (Dn2CppArrayI4 rep) ---
        int[] ints = { 10, 20, 30, 40 };
        GCHandle hi = GCHandle.Alloc(ints, GCHandleType.Pinned);
        int* pi = (int*)hi.AddrOfPinnedObject();
        for (int i = 0; i < 4; i++) pi[i] += 5;
        Console.WriteLine($"{ints[0]},{ints[1]},{ints[2]},{ints[3]}"); // 15,25,35,45
        ints[1] = 999;
        Console.WriteLine(pi[1]);                                      // 999
        hi.Free();

        // --- double[] (Dn2CppArrayN rep, 8-byte elements) ---
        double[] dbls = { 1.5, 2.5, 3.5 };
        GCHandle hd = GCHandle.Alloc(dbls, GCHandleType.Pinned);
        double* pd = (double*)hd.AddrOfPinnedObject();
        pd[0] += 0.25;
        pd[2] = pd[1] * 2;
        Console.WriteLine($"{dbls[0]},{dbls[1]},{dbls[2]}");           // 1.75,2.5,5
        hd.Free();

        // --- blittable-struct array : write fields through the pin, read managed ---
        Pt[] pts = new Pt[3];
        for (int i = 0; i < 3; i++) pts[i] = new Pt { X = i, Y = i * 100L, Z = i * 0.5 };
        GCHandle hp = GCHandle.Alloc(pts, GCHandleType.Pinned);
        Pt* pp = (Pt*)hp.AddrOfPinnedObject();
        pp[2].X = 42;
        pp[1].Y = 7777;
        pp[0].Z = 9.5;
        Console.WriteLine($"{pts[2].X},{pts[1].Y},{pts[0].Z}");        // 42,7777,9.5
        // and a managed field write observed through the pin
        pts[1].X = 314;
        Console.WriteLine(pp[1].X);                                    // 314
        hp.Free();

        // --- non-pinned Alloc(obj) (Normal) : Target / IsAllocated before Free ---
        object boxed = "hello";
        GCHandle hn = GCHandle.Alloc(boxed);
        Console.WriteLine(hn.IsAllocated);                            // True
        Console.WriteLine(ReferenceEquals(hn.Target, boxed));         // True
        Console.WriteLine((string)hn.Target);                         // hello
        hn.Free();

        // --- the GCHandle(obj, Pinned) ctor-equivalent (Alloc is inlined to a value
        //     newobj by Roslyn): Target round-trips the same array. ---
        int[] arr2 = { 7, 8, 9 };
        GCHandle hc = GCHandle.Alloc(arr2, GCHandleType.Pinned);
        Console.WriteLine(hc.IsAllocated);                           // True
        int[] back = (int[])hc.Target;
        Console.WriteLine(ReferenceEquals(back, arr2));               // True
        Console.WriteLine($"{back[0]},{back[1]},{back[2]}");          // 7,8,9
        hc.Free();
    }
}
