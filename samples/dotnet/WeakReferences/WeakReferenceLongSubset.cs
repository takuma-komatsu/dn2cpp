using System;

namespace WeakReferenceLongSubset
{
    // trackResurrection routes through Boehm's GC_register_long_link rather than
    // the short-lived disappearing link the default form uses. Only the
    // before-collection path is checked: per-object post-collection state is not
    // reliably observable, and WeakReferenceMemorySubset makes that check.
    internal sealed class Target
    {
        public int Id;
        public Target(int id) { Id = id; }
    }

    internal static class Program
    {
        private static WeakReference<Target> CreateLongWeak(int id)
        {
            return new WeakReference<Target>(new Target(id), trackResurrection: true);
        }

        internal static void __GateEntry()
        {
            var wr = CreateLongWeak(1);
            bool alive = wr.TryGetTarget(out var t);
            Console.WriteLine("long before: alive=" + alive + " id=" + t.Id);
        }
    }
}
