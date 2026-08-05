using System;

namespace NestedFinallySubset
{
    // A `leave` exiting several nested `finally` regions runs every enclosing
    // finally in turn, innermost first, reaching the branch target only once
    // none remain.
    internal static class Program
    {
        // Return from the innermost of three nested trys: all three finallys run.
        private static int Deep()
        {
            try
            {
                try
                {
                    try
                    {
                        return 1;
                    }
                    finally { Console.WriteLine("f3"); }
                }
                finally { Console.WriteLine("f2"); }
            }
            finally { Console.WriteLine("f1"); }
        }

        // Target inside the outer try, outside the inner one: only the inner
        // finally runs at the jump; the outer runs on the way out.
        private static int Partial(int n)
        {
            int r = 0;
            try
            {
                try
                {
                    if (n > 0)
                    {
                        goto done;
                    }

                    r += 5;
                }
                finally { Console.WriteLine("inner"); }

                r += 100;
            done:
                r += 1000;
            }
            finally { Console.WriteLine("outer"); }

            return r;
        }

        // An exception unwinds through both finallys (which run) before the catch.
        private static int Boom()
        {
            try
            {
                try
                {
                    try
                    {
                        throw new InvalidOperationException();
                    }
                    finally { Console.WriteLine("b-inner"); }
                }
                finally { Console.WriteLine("b-outer"); }
            }
            catch (InvalidOperationException)
            {
                return 9;
            }
        }

        private static void Main()
        {
            Console.WriteLine(Deep());      // f3, f2, f1, 1
            Console.WriteLine(Partial(1));  // inner, outer, 1000
            Console.WriteLine(Boom());      // b-inner, b-outer, 9

            InflightExcRoot.Program.__GateEntry();
        }
    }
}
