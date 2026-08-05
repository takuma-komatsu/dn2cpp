using System;
using System.Runtime.CompilerServices;

namespace FinalizerExceptionTypeSubset
{
    // A user exception type may declare a finalizer like any other class, and
    // it runs no matter how the instance was born — thrown and caught, or just
    // constructed and dropped (verified against real .NET, net10.0). dn2cpp
    // intercepts every exception construction into a uniform message-carrying
    // allocator, so this pins down that the intercepted path still registers
    // the instance with the finalizer queue.
    internal sealed class FinalizableException : Exception
    {
        public FinalizableException(string message) : base(message) { }

        ~FinalizableException()
        {
            Console.WriteLine("exception finalized");
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
        private static void ThrowAndCatch()
        {
            try
            {
                throw new FinalizableException("boom");
            }
            catch (FinalizableException e)
            {
                Console.WriteLine("caught: " + e.Message);
            }
        }

        internal static void __GateEntry()
        {
            ThrowAndCatch();
            // Collect until the finalizer reports in: one round is
            // not guaranteed to reclaim the instance under a conservative
            // collector (the throw/catch path leaves extra stale copies).
            // Real .NET always exits after the first round.
            for (int rounds = 0; Finalized == 0 && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Console.WriteLine("exception-finalizer done");
        }
    }
}
