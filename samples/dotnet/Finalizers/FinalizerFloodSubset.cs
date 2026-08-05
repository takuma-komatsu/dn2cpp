using System;
using System.Threading;

namespace FinalizerFloodSubset
{
    // A single collection wave can make far more objects f-reachable than the
    // runtime's fixed pending-finalizer ring holds: keeping every instance
    // alive through one array until the array itself is dropped guarantees all
    // of them are queued in one burst by the first Collect(), while the
    // consumer (finalizer thread or manual drain) is still catching up. Real
    // .NET's f-reachable queue is unbounded, so this must complete instead of
    // aborting. The finalizer does a little deterministic work per object (a
    // small hash folded into a global sum), which both models a realistic
    // non-trivial Finalize body and makes the assertion order-independent.
    // What gets printed must also be collector-independent: a conservative
    // collector may pin a handful of instances forever through a stale stack
    // word, so neither the raw count nor the raw checksum can be diffed
    // against real .NET. Print booleans instead — "enough ran" (a small pin
    // allowance off the full count) and "the checksum is self-consistent"
    // (recomputed over exactly the ids whose finalizers ran, so every one
    // that did run did its work exactly once).
    internal sealed class Small
    {
        private readonly int _id;

        internal Small(int id)
        {
            _id = id;
        }

        ~Small()
        {
            Interlocked.Add(ref Program.Hash, Program.HashOf(_id));
            Program.Ran[_id] = true;
            Interlocked.Increment(ref Program.Finalized);
        }
    }

    internal static class Program
    {
        internal static int Finalized;
        internal static long Hash;
        internal static bool[] Ran;
        private static Small[] s_arr;
        private const int Count = 150_000;
        // How many instances a conservative stack scan may pin forever through
        // stale words; real .NET (precise) always finalizes all of them.
        private const int PinAllowance = 8;

        internal static long HashOf(int id)
        {
            long h = id;
            for (int k = 0; k < 16; k++)
                h = h * 31 + k;
            return h;
        }

        // Holds every instance through a static array so no organic mid-loop
        // collection can finalize a prefix early: the whole population becomes
        // f-reachable together only when DropAll nulls the slots. The array
        // itself deliberately stays rooted (a static) — releasing the objects
        // must not hinge on a single local array reference vanishing from
        // every stale stack word and register, which a conservative collector
        // does not guarantee.
        private static void AllocateAll()
        {
            s_arr = new Small[Count];
            for (int i = 0; i < Count; i++)
                s_arr[i] = new Small(i);
        }

        private static void DropAll()
        {
            for (int i = 0; i < Count; i++)
                s_arr[i] = null;
        }

        internal static void __GateEntry()
        {
            Ran = new bool[Count];
            AllocateAll();
            DropAll();
            int rounds = 0;
            while (Finalized < Count && rounds < 64)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                rounds++;
            }
            long expected = 0;
            int ranCount = 0;
            for (int i = 0; i < Count; i++)
            {
                if (Ran[i])
                {
                    expected += HashOf(i);
                    ranCount++;
                }
            }
            Console.WriteLine("flood finalized enough=" + (Finalized >= Count - PinAllowance));
            Console.WriteLine("flood hash ok=" + (Hash == expected && ranCount == Finalized));
            Console.WriteLine("flood done");
        }
    }
}
