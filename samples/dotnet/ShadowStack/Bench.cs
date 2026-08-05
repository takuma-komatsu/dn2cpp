using System;

namespace Bench
{
    // "bench" mode: a call-dense workload for timing the same source transpiled
    // with and without --shadow-stack. A measurement aid, asserted by no gate arm;
    // the printed sum only keeps the calls from being optimized away.
    internal static class Program
    {
        private static long Fib(int n)
        {
            return n < 2 ? n : Fib(n - 1) + Fib(n - 2);
        }

        internal static void __GateEntry()
        {
            long sum = 0;
            for (int i = 0; i < 3; i++)
                sum += Fib(32);
            Console.WriteLine("bench fib sum: " + sum);
        }
    }
}
