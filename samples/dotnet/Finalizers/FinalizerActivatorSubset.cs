using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FinalizerActivatorSubset
{
    // A finalizable instance is finalizable no matter which factory built it:
    // the generic Activator.CreateInstance<T>() (what a constrained `new T()`
    // compiles to) and the reflective Activator.CreateInstance(Type) must both
    // register the instance with the finalizer queue, exactly like a direct
    // `new T()` (verified against real .NET, net10.0). The finalizers only
    // bump a counter — printed once at the end — so the assertion is
    // independent of finalization-queue ordering.
    internal sealed class ViaGeneric
    {
        ~ViaGeneric()
        {
            Interlocked.Increment(ref Program.Finalized);
        }
    }

    internal sealed class ViaReflection
    {
        ~ViaReflection()
        {
            Interlocked.Increment(ref Program.Finalized);
        }
    }

    internal static class Program
    {
        internal static int Finalized;

        private static T Make<T>() where T : new()
        {
            return new T(); // compiles to Activator.CreateInstance<T>()
        }

        // Each factory call sits in its own helper so no reference lingers on
        // the entry frame's slots across the collections below. NoInlining is
        // load-bearing under the conservative (Boehm) GC, not a perf hint: an
        // inlined body's reference slots join the retry loop's live frame and
        // are conservatively scanned as roots every round.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateAndDrop()
        {
            _ = Make<ViaGeneric>();
            _ = Activator.CreateInstance(typeof(ViaReflection));
        }

        internal static void __GateEntry()
        {
            CreateAndDrop();
            // Collect until both finalizers report in: one round is
            // not guaranteed to reclaim either instance under a conservative
            // collector. Real .NET always exits after the first round.
            for (int rounds = 0; Finalized < 2 && rounds < 64; rounds++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Console.WriteLine("activator finalized count=" + Finalized);
            Console.WriteLine("activator done");
        }
    }
}
