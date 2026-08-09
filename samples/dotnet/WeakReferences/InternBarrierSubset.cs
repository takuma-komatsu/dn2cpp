using System;

namespace InternBarrierSubset
{
    // A run-time-built string handed to string.Intern is rooted by the pool cell
    // alone — the pool keys on contents through unscanned storage — so a missed
    // barrier on the cell's store drops it across an incremental cycle and a
    // later IsInterned answers null. Literals cannot show this: the compiler
    // interns them and dn2cpp roots every ldstr for the process lifetime.
    internal static class Program
    {
        private const int AllocationLimit = 100_000;

        // Not a literal, and not inlined: the returned instance must be fresh.
        private static string Build()
        {
            return new string(new char[] { 'd', 'n', '2', 'c', 'p', 'p', ' ', 'i', 'n', 't', 'e', 'r', 'n' });
        }

        internal static void __GateEntry()
        {
            string s1 = Build();
            _ = string.Intern(s1);
            s1 = null;

            var pressure = new byte[64][];
            int target = GC.CollectionCount(0) + 1;
            int allocations = 0;
            while (GC.CollectionCount(0) < target && allocations < AllocationLimit)
            {
                pressure[allocations & (pressure.Length - 1)] = new byte[4096];
                allocations++;
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();

            string s2 = Build();
            string found = string.IsInterned(s2);
            bool survived = found is not null && ReferenceEquals(string.Intern(s2), found);
            GC.KeepAlive(pressure);
            Console.WriteLine("interned string survives collection: " + survived);
        }
    }
}
