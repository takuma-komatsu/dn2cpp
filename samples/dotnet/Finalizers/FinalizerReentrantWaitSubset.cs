using System;
using System.Runtime.CompilerServices;

namespace FinalizerReentrantWaitSubset
{
    // GC.WaitForPendingFinalizers called from inside a finalizer body returns
    // immediately instead of deadlocking (verified against real .NET,
    // net10.0). A naive wait would sleep on the finalizer's own completion:
    // the in-flight item is only accounted as drained after its body returns.
    internal sealed class WaitsInFinalizer
    {
        ~WaitsInFinalizer()
        {
            Console.WriteLine("finalizer: before wait");
            GC.WaitForPendingFinalizers();
            Console.WriteLine("finalizer: after wait");
            Program.Finalized++;
        }
    }

    internal static class Program
    {
        internal static int Finalized;

        // NoInlining is load-bearing under the conservative (Boehm) GC: an
        // inlined body's reference slots join the retry loop's live frame and
        // are scanned as roots every round.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateAndDrop()
        {
            _ = new WaitsInFinalizer();
        }

        internal static void __GateEntry()
        {
            CreateAndDrop();
            // Collect until the finalizer reports in: one round is
            // not guaranteed to reclaim the instance under a conservative
            // collector. Real .NET always exits after the first round.
            for (int rounds = 0; Finalized == 0 && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Console.WriteLine("reentrant-wait done");
        }
    }
}
