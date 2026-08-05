using System;
using System.Runtime.CompilerServices;

namespace FinalizerReRegisterSubset
{
    // GC.ReRegisterForFinalize opts an
    // instance back into finalization after GC.SuppressFinalize opted it out.
    internal sealed class ReRegisterable
    {
        private readonly string _tag;
        public ReRegisterable(string tag) { _tag = tag; }
        ~ReRegisterable()
        {
            Console.WriteLine("finalized " + _tag);
            Program.Finalized++;
        }
    }

    internal static class Program
    {
        internal static int Finalized;

        // NoInlining is load-bearing under the conservative (Boehm) GC, not a
        // perf hint: this must be a real call frame so that on return the last
        // strong reference to `obj` is gone from the caller's live callee-saved
        // registers. If it is inlined into __GateEntry (MSVC does this), the
        // object pointer stays parked in a nonvolatile register (rdi) across the
        // whole retry loop below, the collector conservatively scans it as a
        // root, and the instance is never reclaimed inside the 64-round window.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateSuppressReRegisterAndDrop(string tag)
        {
            var obj = new ReRegisterable(tag);
            GC.SuppressFinalize(obj);
            GC.ReRegisterForFinalize(obj);
        }

        internal static void __GateEntry()
        {
            CreateSuppressReRegisterAndDrop("reregistered");
            // Collect until the finalizer reports in: one round is
            // not guaranteed to reclaim the instance under a conservative
            // collector. Real .NET always exits after the first round.
            for (int rounds = 0; Finalized == 0 && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Console.WriteLine("done-reregister");
        }
    }
}
