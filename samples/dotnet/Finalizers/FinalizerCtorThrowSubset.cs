using System;
using System.Runtime.CompilerServices;

namespace FinalizerCtorThrowSubset
{
    // An object is finalizable from the moment it is allocated, not from the
    // end of its constructor: a ctor that throws still leaves the partially
    // constructed object to be finalized, with whatever fields it managed to
    // assign before throwing (verified against real .NET, net10.0).
    internal sealed class ThrowingCtor
    {
        private readonly int _progress;

        public ThrowingCtor()
        {
            _progress = 42;
            throw new InvalidOperationException("ctor boom");
        }

        ~ThrowingCtor()
        {
            Console.WriteLine("ctor-throw finalized, progress=" + _progress);
            Program.Finalized++;
        }
    }

    internal static class Program
    {
        internal static int Finalized;

        // Kept out of __GateEntry so the half-built object is provably
        // unreachable once this returns. NoInlining is load-bearing under the
        // conservative (Boehm) GC: an inlined body's reference slots join the
        // retry loop's live frame and are scanned as roots every round.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void MakeAndCatch()
        {
            try
            {
                _ = new ThrowingCtor();
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("caught: " + e.Message);
            }
        }

        internal static void __GateEntry()
        {
            MakeAndCatch();
            // Collect until the finalizer reports in: one round is
            // not guaranteed to reclaim the half-built instance under a
            // conservative collector (the throw path leaves extra stale
            // copies). Real .NET always exits after the first round.
            for (int rounds = 0; Finalized == 0 && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Console.WriteLine("ctor-throw done");
        }
    }
}
