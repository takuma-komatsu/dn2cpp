using System;

namespace FinalizerNullArgSubset
{
    // GC.SuppressFinalize(null) / GC.ReRegisterForFinalize(null) throw a
    // catchable ArgumentNullException (verified against real .NET, net10.0) —
    // not a hard fault. Only fixed markers are printed (messages differ
    // between runtimes).
    internal static class Program
    {
        internal static void __GateEntry()
        {
            try
            {
                GC.SuppressFinalize(null!);
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("SuppressFinalize(null): ArgumentNullException");
            }
            try
            {
                GC.ReRegisterForFinalize(null!);
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("ReRegisterForFinalize(null): ArgumentNullException");
            }
            Console.WriteLine("null-arg done");
        }
    }
}
