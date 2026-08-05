using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FinalizerClonedSubset
{
    // The THIRD allocation mouth for a finalizable type, and the sibling of
    // FinalizerActivatorSubset above: Object.MemberwiseClone. A clone is a fresh
    // instance of the same runtime type, so it must be queued for finalization
    // exactly as the original is — dropping both reports TWO finalizations. A clone
    // path that failed to register would run no finalizer and say nothing.
    //
    // MemberwiseClone is `protected`, so the reflective Invoke is the only mouth a
    // section outside the declaring type can use — the same route
    // MemberwiseCloneSubset (ReflectInvoke bucket) drives. The finalizers only bump
    // a counter, printed once at the end, so the assertion is independent of
    // finalization-queue ordering.
    internal sealed class Cloned
    {
        public int Id;

        ~Cloned()
        {
            Interlocked.Increment(ref Program.Finalized);
        }
    }

    internal static class Program
    {
        internal static int Finalized;

        // Both the original and the clone are created and dropped inside this
        // helper so neither lingers in the entry frame's slots across the
        // collections below. NoInlining is load-bearing under the conservative
        // (Boehm) GC, not a perf hint: inlined into __GateEntry (MSVC does this),
        // both pointers stay parked in the retry loop's own live frame slots,
        // the collector scans them as roots every round, and neither instance
        // is ever reclaimed.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateCloneAndDrop()
        {
            MethodInfo mwc = typeof(object).GetMethod(
                "MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            var a = new Cloned { Id = 1 };
            var b = (Cloned)mwc.Invoke(a, null);
            // Read the clone so the copy is observably a real instance rather than a
            // header the optimizer could have elided.
            Console.WriteLine("clone id=" + b.Id + " same=" + ReferenceEquals(a, b));
        }

        internal static void __GateEntry()
        {
            CreateCloneAndDrop();
            // Collect until BOTH finalizers report in: one round is not guaranteed to
            // reclaim either instance under a conservative collector. Real .NET always
            // exits after the first round.
            for (int rounds = 0; Finalized < 2 && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Console.WriteLine("cloned finalized count=" + Finalized);
            Console.WriteLine("cloned done");
        }
    }
}
