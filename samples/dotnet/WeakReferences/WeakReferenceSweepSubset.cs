using System;

namespace WeakReferenceSweepSubset
{
    // The only section that READS a weak reference whose target was collected —
    // Basic/Long stop before collection and Memory only samples heap size. That
    // matters because Boehm clears a disappearing link by writing a raw 0, which
    // GC_REVEAL_POINTER turns back into ~0 (non-null garbage) unless the runtime
    // special-cases it: a collected target must report false, or the field read
    // below faults. The output is one line, so this stays a clean diff gate.
    internal static class Program
    {
        private const int Count = 20_000;
        private const int PayloadBytes = 256;

        // Must not be inlined into __GateEntry: no strong reference to a target may
        // linger on the entry frame's native stack across the GC.Collect.
        private static WeakReference<byte[]>[] MakeWeaks()
        {
            var arr = new WeakReference<byte[]>[Count];
            for (int i = 0; i < Count; i++)
            {
                arr[i] = new WeakReference<byte[]>(new byte[PayloadBytes]);
                GC.SuppressFinalize(arr[i]);
            }
            return arr;
        }

        internal static void __GateEntry()
        {
            var weaks = MakeWeaks(); // targets reachable only through the weak links
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
            Console.WriteLine("weak sweep over " + Count + " targets: no dangling dereference");
        }
    }
}
