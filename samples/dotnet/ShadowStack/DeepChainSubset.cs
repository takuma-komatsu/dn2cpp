using System;

namespace DeepChainSubset
{
    // A five-level chain with NO [MethodImpl(NoInlining)]: -O2 inlines it whole,
    // and the trace survives anyway because each body's Dn2CppShadowFrame guard
    // is a side effect the optimizer must keep. The throw path is app-only, so no
    // CoreLib version detail can leak into the frozen text.
    //
    // Declared divergence: no parameter list and no "in file:line". Real .NET's
    // Release JIT may also inline frames away — the exact five-frame chain is the
    // shadow stack's guarantee, not .NET's.
    internal static class Program
    {
        private static void Depth5()
        {
            throw new InvalidOperationException("thrown in Depth5");
        }

        private static void Depth4() { Depth5(); }
        private static void Depth3() { Depth4(); }
        private static void Depth2() { Depth3(); }
        private static void Depth1() { Depth2(); }

        // "unhandled" mode: the chain escapes Main, so the stderr report's trace
        // must name Depth5 exactly.
        internal static void ThrowUncaught() { Depth1(); }

        internal static void __GateEntry()
        {
            Console.WriteLine("== DeepChainSubset ==");
            try
            {
                Depth1();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("caught: " + ex.Message);
                Console.WriteLine("trace is null: " + (ex.StackTrace is null));
                Console.WriteLine("deep-chain trace:");
                Console.WriteLine(ex.StackTrace);
            }
        }

        internal static void __GateSmoke()
        {
            try
            {
                Depth1();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("deep-chain caught: " + ex.Message);
            }
        }
    }
}
