#nullable enable
using System;
using System.Runtime.InteropServices;

// Conservative-GC pinning integrity: pin a heap int[], hand only its raw address +
// length to "native-style" pointer code that writes into the backing store, churn
// the managed heap (so a moving/collecting GC would have a chance to relocate or
// reclaim the array), then free the handle and confirm managed indexing observes
// exactly what the native code wrote. dn2cpp's conservative non-moving GC keeps the
// object put, so the pin's writes survive the churn and the handle release. Real
// .NET (which truly pins) trivially produces the same output, so this is an exact
// diff that proves dn2cpp matches. CoreLib only.
namespace ConservativeGcPinSubset;

unsafe class Program
{
    // "Native-style" code: sees only a raw pointer + element count.
    private static void FillNative(IntPtr p, int count)
    {
        int* ip = (int*)p;
        for (int i = 0; i < count; i++)
            ip[i] = i * i;
    }

    private static long SumNative(IntPtr p, int count)
    {
        int* ip = (int*)p;
        long s = 0;
        for (int i = 0; i < count; i++)
            s += ip[i];
        return s;
    }

    internal static void __GateEntry()
    {
        const int N = 16;
        int[] data = new int[N];
        GCHandle h = GCHandle.Alloc(data, GCHandleType.Pinned);
        IntPtr addr = h.AddrOfPinnedObject();

        // native code writes squares into the pinned array's storage.
        FillNative(addr, N);

        // churn the heap: a moving/collecting GC would have a chance to relocate or
        // reclaim `data` here; the conservative non-moving GC must keep it put.
        long junk = 0;
        for (int i = 0; i < 5000; i++)
        {
            int[] garbage = new int[32];
            garbage[i % 32] = i;
            junk += garbage[i % 32];
        }

        // native code reads back through the same raw pointer after the churn.
        long viaPtr = SumNative(addr, N);

        // release the pin; `data` remains a live managed object.
        h.Free();

        // managed indexing must observe exactly what native code wrote.
        long viaManaged = 0;
        for (int i = 0; i < N; i++)
            viaManaged += data[i];

        Console.WriteLine(viaPtr);            // 0+1+4+...+225 = 1240
        Console.WriteLine(viaManaged);        // 1240 (same backing store, not moved/collected)
        Console.WriteLine(viaPtr == viaManaged); // True
        Console.WriteLine(data[7]);           // 49
        Console.WriteLine(junk != 0);         // True (the churn ran)
    }
}
