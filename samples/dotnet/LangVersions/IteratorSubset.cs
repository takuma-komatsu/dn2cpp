using System;
using System.Collections.Generic;

namespace IteratorSubset
{
    // C# `yield` iterators lower to a compiler-generated state-machine class
    // that implements IEnumerable<T>/IEnumerator<T> against the *real* CoreLib
    // (passed to dn2cpp with -r). The transpiler models that class like any
    // other — its MoveNext switch, state fields and interface dispatch are
    // ordinary IL — so iterators "just work" once the BCL plumbing they touch
    // (the generic collection interfaces, an IEnumerator.Reset that throws
    // NotSupportedException, Environment.CurrentManagedThreadId) is handled.
    internal static class Program
    {
        // Stateful loop with an early `yield break`.
        private static IEnumerable<int> Squares(int n)
        {
            for (int i = 0; i < n; i++)
            {
                if (i == 100)
                {
                    yield break;
                }

                yield return i * i;
            }
        }

        // Local state (`total`) carried across successive yields.
        private static IEnumerable<int> RunningTotal(int[] xs)
        {
            int total = 0;
            foreach (int x in xs)
            {
                total += x;
                yield return total;
            }
        }

        // An iterator whose element type is a reference type (string).
        private static IEnumerable<string> Names()
        {
            yield return "a";
            yield return "bb";
            yield return "ccc";
        }

        internal static void __GateEntry()
        {
            // foreach binds to IEnumerable<int>.GetEnumerator()/MoveNext()/Current
            // and disposes the enumerator in a finally — all via interface dispatch.
            int sum = 0;
            foreach (int v in Squares(4))
            {
                sum += v;
            }

            Console.WriteLine(sum);            // 0+1+4+9 = 14

            int[] xs = new int[4];
            for (int i = 0; i < 4; i++)
            {
                xs[i] = i + 1;
            }

            int last = 0;
            foreach (int t in RunningTotal(xs))
            {
                last = t;
            }

            Console.WriteLine(last);           // running total ends at 10

            int chars = 0;
            foreach (string s in Names())
            {
                chars += s.Length;
            }

            Console.WriteLine(chars);          // 1+2+3 = 6

            // Driving the enumerator by hand: GetEnumerator/MoveNext/Dispose.
            IEnumerator<int> e = Squares(3).GetEnumerator();
            int count = 0;
            while (e.MoveNext())
            {
                count++;
            }

            e.Dispose();
            Console.WriteLine(count);          // 3
        }
    }
}
