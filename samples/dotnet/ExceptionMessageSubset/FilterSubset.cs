using System;

namespace FilterSubset
{
    // Exception filters (`catch (E) when (cond)`). The compiler lowers each
    // filter into a separate IL region whose code ends in `endfilter`, yielding 1
    // (run this handler) or 0 (continue the search). Handlers on a try are tried
    // in order; a false filter falls through to the next clause, and if none match
    // the exception propagates to an outer handler.
    internal static class Program
    {
        // Three clauses on one try: filter, filter, typed catch-all. Exercises the
        // ordered dispatch chain (filter -> filter -> typed).
        private static string Pick(int n)
        {
            try
            {
                throw new InvalidOperationException();
            }
            catch (InvalidOperationException) when (n == 1)
            {
                return "one";
            }
            catch (InvalidOperationException) when (n == 2)
            {
                return "two";
            }
            catch (InvalidOperationException)
            {
                return "other";
            }
        }

        // A false filter on the inner try lets the exception bubble to the outer
        // catch (continue-search semantics: endfilter 0 re-raises).
        private static int Bubble(int n)
        {
            try
            {
                try
                {
                    throw new InvalidOperationException();
                }
                catch (InvalidOperationException) when (n > 100)
                {
                    return -1;
                }
            }
            catch (InvalidOperationException)
            {
                return 7;
            }

            return 0;
        }

        internal static void Run()
        {
            Console.WriteLine(Pick(1));   // one
            Console.WriteLine(Pick(2));   // two
            Console.WriteLine(Pick(3));   // other (both filters false)
            Console.WriteLine(Bubble(5)); // 7 (inner filter false -> outer catch)
        }
    }
}
