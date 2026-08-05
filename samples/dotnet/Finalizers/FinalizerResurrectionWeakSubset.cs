using System;
using System.Runtime.CompilerServices;

namespace FinalizerResurrectionWeakSubset
{
    // A long (trackResurrection: true) WeakReference<T> keeps pointing
    // at a resurrected instance -- resurrection makes the referent genuinely
    // reachable again (via Survivor below), so this is a deterministic fact,
    // not a GC-timing-dependent one. A short WeakReference is registered
    // alongside it (must not crash or otherwise misbehave once its referent
    // resurrects) but its post-collection state is deliberately not
    // asserted here: per WeakReferenceBasicSubset, a single
    // referent's collection is not reliably observable from a managed test
    // under a conservative collector (incidental register/stack copies can
    // keep it "alive" for one run and not the next).
    internal sealed class WeaklyTrackedResurrector
    {
        internal static WeaklyTrackedResurrector Survivor;
        public readonly int Id;
        public WeaklyTrackedResurrector(int id) { Id = id; }
        ~WeaklyTrackedResurrector()
        {
            Survivor = this;
        }
    }

    internal static class Program
    {
        private static WeakReference<WeaklyTrackedResurrector> s_short;

        // NoInlining is load-bearing under the conservative (Boehm) GC: an
        // inlined body's strong `obj` slot joins the retry loop's live frame
        // and is scanned as a root every round, so the finalizer never fires.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference<WeaklyTrackedResurrector> CreateAndTrack(int id)
        {
            var obj = new WeaklyTrackedResurrector(id);
            s_short = new WeakReference<WeaklyTrackedResurrector>(obj, trackResurrection: false);
            return new WeakReference<WeaklyTrackedResurrector>(obj, trackResurrection: true);
        }

        internal static void __GateEntry()
        {
            var longWr = CreateAndTrack(22);
            // Collect until the finalizer resurrects the instance:
            // one round is not guaranteed to reclaim it under a conservative
            // collector, and the long weak reference's target is only
            // deterministically alive once Survivor roots it. Real .NET
            // always exits after the first round.
            for (int rounds = 0; WeaklyTrackedResurrector.Survivor is null && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            _ = s_short.TryGetTarget(out _); // must not crash; result not asserted (see note above)
            bool longAlive = longWr.TryGetTarget(out var t);
            Console.WriteLine("long alive after finalize=" + longAlive + " id=" + t.Id);
            WeaklyTrackedResurrector.Survivor = null;
            Console.WriteLine("done-resurrection-weak");
        }
    }
}
