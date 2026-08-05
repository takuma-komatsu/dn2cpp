using System;
using System.Runtime.CompilerServices;

namespace FinalizerKeepAliveSubset
{
    // GC.KeepAlive(obj) guarantees the reference stays live up to the call:
    // a Collect + WaitForPendingFinalizers issued BEFORE the KeepAlive point
    // must not have finalized the object, and after the KeepAlive point (once
    // the frame drops the reference) it becomes collectible as usual.
    internal sealed class Guarded
    {
        public static int Finalized;
        public int Ticket = 41;
        ~Guarded()
        {
            Finalized++;
        }
    }

    internal static class Program
    {
        // NoInlining is load-bearing under the conservative (Boehm) GC, not a
        // perf hint: Use must be a real call frame so that on return the last
        // strong reference to `g` is gone from the caller's live callee-saved
        // registers. If Use is inlined into __GateEntry (MSVC does this), the
        // object pointer stays parked in a nonvolatile register (r14) across the
        // whole retry loop below, the collector conservatively scans it as a
        // root, and the instance is never reclaimed inside the 64-round window.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Use()
        {
            var g = new Guarded();
            g.Ticket++;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            // Not finalized yet: KeepAlive below still holds g live here.
            Console.WriteLine("keepalive pre=" + Guarded.Finalized + " ticket=" + g.Ticket);
            GC.KeepAlive(g);
            return 1;
        }

        internal static void __GateEntry()
        {
            int r = Use();
            // Past the KeepAlive point the instance is collectible, but one
            // round is not guaranteed to reclaim it under a conservative
            // collector — collect until its finalizer reports in.
            // Real .NET always exits after the first round.
            for (int rounds = 0; Guarded.Finalized == 0 && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Console.WriteLine("keepalive post=" + Guarded.Finalized + " r=" + r);
        }
    }
}
