using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FinalizerBasicSubset
{
    // A plain C# destructor compiles to a
    // Finalize() override run by the dedicated finalizer thread once the
    // instance becomes unreachable and a collection runs.
    internal sealed class Finalizable
    {
        private readonly int _id;
        public Finalizable(int id) { _id = id; }
        ~Finalizable()
        {
            Console.WriteLine("finalized " + _id);
            Program.Finalized++;
        }
    }

    internal static class Program
    {
        internal static int Finalized;

        // Kept out of __GateEntry so the object is provably unreachable once
        // this returns. NoInlining is load-bearing under the conservative
        // (Boehm) GC: an inlined body's reference slots join the retry loop's
        // live frame and are scanned as roots every round.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateAndDrop(int id)
        {
            _ = new Finalizable(id);
        }

        private static void CreateFirst()
        {
            CreateAndDrop(1);
        }

        internal static void __GateEntry(bool requireFinalizerWindows)
        {
            if (requireFinalizerWindows)
            {
                // A finished thread removes its whole stack from Boehm's root set.
                var creator = new Thread(CreateFirst);
                creator.Start();
                creator.Join();
            }
            else
            {
                // The threadless arm preserves the WASM collection oracle.
                CreateFirst();
            }
            // A single collection cannot be relied on to reclaim the instance
            // under a conservative collector (a stale stack word or a
            // suspended thread's spilled register can pin it for a round), so
            // collect until the finalizer reports in. Real .NET
            // always exits after the first round — the output is identical.
            for (int rounds = 0; Finalized == 0 && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Console.WriteLine("done");
        }
    }
}
