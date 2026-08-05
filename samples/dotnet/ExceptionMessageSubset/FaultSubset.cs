using System;
using System.Collections.Generic;

namespace FaultSubset
{
    // `fault` exception-handling regions. The C# compiler lowers a
    // `try/finally` inside a `yield` iterator into the state machine's MoveNext as
    // a `try { ... } fault { CallFinally(); }` region (so the finally also runs
    // when an exception unwinds through MoveNext), plus a Dispose path for the
    // normal/early-exit case. A fault handler runs only on the exceptional path
    // and then re-raises; the normal path leaves the try past the handler.
    internal static class Program
    {
        // try/finally inside an iterator. Normal iteration runs the finally via
        // Dispose at the end; an exception thrown mid-iteration unwinds through
        // the MoveNext fault handler, which runs the finally, then re-raises.
        private static IEnumerable<int> Counting(bool boom)
        {
            try
            {
                yield return 1;
                if (boom)
                {
                    throw new InvalidOperationException();
                }

                yield return 2;
            }
            finally
            {
                Console.WriteLine("cleanup");
            }
        }

        internal static void Run()
        {
            // Normal path: 1, 2, then cleanup (finally via Dispose).
            foreach (int x in Counting(boom: false))
            {
                Console.WriteLine(x);
            }

            // Exceptional path: 1, then the throw unwinds through the fault
            // handler (cleanup) and is caught here.
            int caught = 0;
            try
            {
                foreach (int x in Counting(boom: true))
                {
                    Console.WriteLine(x);
                }
            }
            catch (InvalidOperationException)
            {
                caught = 1;
            }
            Console.WriteLine(caught); // 1 — fault re-raised after running cleanup
        }
    }
}
