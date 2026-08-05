using System;
using System.Runtime.CompilerServices;

namespace FinalizerResurrectionSubset
{
    // A finalizer that re-attaches `this` to a
    // reachable root ("resurrects" the instance) keeps it alive past the
    // collection that queued it. Verified against real .NET (net10.0): once
    // resurrected, the instance's finalizer does not fire a second time when
    // it later becomes unreachable again -- an explicit
    // GC.ReRegisterForFinalize is required for that, matching Boehm's
    // finalization queue (a finalized entry is removed from the registry,
    // not re-armed).
    internal sealed class Resurrector
    {
        internal static Resurrector Survivor;
        private readonly int _id;
        public int Id => _id;
        public Resurrector(int id) { _id = id; }
        ~Resurrector()
        {
            Console.WriteLine("resurrection finalizer id=" + _id);
            Survivor = this;
        }
    }

    internal static class Program
    {
        // NoInlining is load-bearing under the conservative (Boehm) GC: an
        // inlined body's reference slots join the retry loop's live frame and
        // are scanned as roots every round.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateAndDrop(int id)
        {
            _ = new Resurrector(id);
        }

        internal static void __GateEntry()
        {
            CreateAndDrop(21);
            // Collect until the finalizer resurrects the instance:
            // one round is not guaranteed to reclaim it under a conservative
            // collector, and reading Survivor before the finalizer ran would
            // dereference null. Real .NET always exits after the first round.
            for (int rounds = 0; Resurrector.Survivor is null && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Console.WriteLine("survivor id=" + Resurrector.Survivor.Id);

            // Drop the resurrected reference and collect again: the
            // finalizer must not re-run without an explicit
            // GC.ReRegisterForFinalize.
            Resurrector.Survivor = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Console.WriteLine("done-resurrection");
        }
    }
}
