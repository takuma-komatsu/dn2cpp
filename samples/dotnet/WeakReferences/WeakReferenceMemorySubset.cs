using System;

namespace WeakReferenceMemorySubset
{
    // The AGGREGATE check the per-object sections deliberately cannot make: no
    // single object's collection is reliably observable under a conservative GC,
    // but if every weak target survived the weak link would be doing nothing. A
    // few stragglers retained by a stray register or stack copy are tolerated.
    internal static class Program
    {
        private const int Count = 50_000;
        private const int PayloadBytes = 4096;

        // Must not be inlined into __GateEntry: a target held there would sit on
        // the entry frame's native stack across GC.GetTotalMemory and be
        // conservatively retained.
        private static void AllocateMany()
        {
            for (int i = 0; i < Count; i++)
            {
                var wr = new WeakReference<byte[]>(new byte[PayloadBytes]);
                GC.SuppressFinalize(wr); // 50,000 wrapper finalizations otherwise
            }
        }

        internal static void __GateEntry()
        {
            AllocateMany();
            long used = GC.GetTotalMemory(true);
            long everythingSurvivedThreshold = (long)Count * PayloadBytes / 4; // 25% margin
            bool bounded = used < everythingSurvivedThreshold;
            Console.WriteLine("heap bounded after " + Count + " weakly-referenced allocations: " + bounded);
        }
    }
}
