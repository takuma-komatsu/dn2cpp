using System;

namespace WeakReferenceHeapHeldLongSubset
{
    // A long WeakReference keeps its handle TAGGED (`cell | 1`) in the heap
    // _taggedHandle field. Boehm rejects heap-scanned interior pointers unless the
    // tag displacements are registered, so on an unregistered build the cell is
    // reclaimed and this sweep reads freed memory. The tagged word is the only
    // route to the cell here — no untagged stack copy survives.
    internal static class Program
    {
        private const int Count = 20_000;
        private const int PayloadBytes = 256;

        // Must not be inlined into __GateEntry: no strong reference to a target,
        // nor any untagged cell handle, may linger on the entry frame's stack.
        private static WeakReference<byte[]>[] MakeLongWeaks()
        {
            var arr = new WeakReference<byte[]>[Count];
            for (int i = 0; i < Count; i++)
            {
                arr[i] = new WeakReference<byte[]>(new byte[PayloadBytes], trackResurrection: true);
                GC.SuppressFinalize(arr[i]);
            }
            return arr;
        }

        internal static void __GateEntry()
        {
            var weaks = MakeLongWeaks(); // cells reachable only via tagged heap words
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            for (int i = 0; i < weaks.Length; i++)
            {
                // The impossible condition keeps the output deterministic while
                // still dereferencing every target reported alive.
                if (weaks[i].TryGetTarget(out var payload) && payload.Length < 0)
                    Console.WriteLine("unreachable");
            }
            Console.WriteLine("long weak sweep over " + Count + " heap-held handles: no dangling dereference");
        }
    }
}
