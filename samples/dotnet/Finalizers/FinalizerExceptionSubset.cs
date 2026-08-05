using System;
using System.Runtime.CompilerServices;

namespace FinalizerExceptionSubset
{
    // An uncaught finalizer exception crashes the process in real
    // .NET (verified against net10.0: an unhandled exception thrown from a
    // finalizer body aborts the process, and no further queued finalizer
    // runs) rather than being silently swallowed. This section is
    // deliberately last in Program.cs's __GateEntry sequence -- the process
    // aborts here, so nothing after it would run.
    internal sealed class Thrower
    {
        ~Thrower()
        {
            Console.WriteLine("about to throw from finalizer");
            throw new InvalidOperationException("boom");
        }
    }

    internal static class Program
    {
        // NoInlining is load-bearing under the conservative (Boehm) GC: an
        // inlined body's reference slots join the retry loop's live frame and
        // are scanned as roots every round.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateAndDrop()
        {
            _ = new Thrower();
        }

        internal static void __GateEntry()
        {
            CreateAndDrop();
            // No flag can witness this finalizer (running it aborts the
            // process), so the retry loop is unconditioned: each round either
            // aborts mid-wait or leaves the instance pinned for another try.
            // One round is not guaranteed to reclaim it under a conservative
            // collector; real .NET always aborts in the first round.
            for (int rounds = 0; rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Console.WriteLine("UNREACHABLE: process should have aborted");
        }
    }
}
