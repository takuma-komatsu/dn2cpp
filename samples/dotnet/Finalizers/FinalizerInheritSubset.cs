using System;
using System.Runtime.CompilerServices;

namespace FinalizerInheritSubset
{
    // A subclass that does not itself declare a destructor still dispatches
    // its base's Finalize override — exercises the base-chain walk in
    // Compilation.EffectiveFinalize (same shape as EffectiveToString).
    internal class BaseFinalizable
    {
        protected readonly string Tag;
        public BaseFinalizable(string tag) { Tag = tag; }
        ~BaseFinalizable()
        {
            Console.WriteLine("base finalized " + Tag);
            Program.Finalized++;
        }
    }

    internal sealed class DerivedNoOverride : BaseFinalizable
    {
        public DerivedNoOverride(string tag) : base(tag) { }
    }

    internal static class Program
    {
        internal static int Finalized;

        // NoInlining is load-bearing under the conservative (Boehm) GC: an
        // inlined body's reference slots join the retry loop's live frame and
        // are scanned as roots every round.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateAndDrop()
        {
            _ = new DerivedNoOverride("derived");
        }

        internal static void __GateEntry()
        {
            CreateAndDrop();
            // Collect until the finalizer reports in: one round is
            // not guaranteed to reclaim the instance under a conservative
            // collector. Real .NET always exits after the first round.
            for (int rounds = 0; Finalized == 0 && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Console.WriteLine("done-inherit");
        }
    }
}
