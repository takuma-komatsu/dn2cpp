using System;

namespace WeakReferenceBasicSubset
{
    // The before-collection path only — construct, then read while the referent is
    // still reachable — for the generic and non-generic forms. A single referent's
    // COLLECTION is deliberately not asserted: neither a conservative collector nor
    // real .NET's JIT-driven liveness reliably drops any one specific object, so
    // WeakReferenceMemorySubset makes that check in aggregate instead.
    internal sealed class Target
    {
        public int Id;
        public Target(int id) { Id = id; }
    }

    internal static class Program
    {
        private static WeakReference<Target> CreateGenericWeak(int id)
        {
            return new WeakReference<Target>(new Target(id));
        }

        private static WeakReference CreateWeak(int id)
        {
            return new WeakReference(new Target(id));
        }

        internal static void __GateEntry()
        {
            var gwr = CreateGenericWeak(1);
            bool genericAlive = gwr.TryGetTarget(out var t1);
            Console.WriteLine("generic before: alive=" + genericAlive + " id=" + t1.Id);

            var wr = CreateWeak(2);
            Console.WriteLine("nongeneric before: alive=" + (wr.Target != null));
        }
    }
}
