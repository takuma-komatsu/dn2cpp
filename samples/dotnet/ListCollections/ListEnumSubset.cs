#nullable enable
using System;
using System.Collections.Generic;

namespace ListEnumSubset
{
    // a generic method enumerating a List<T> through the IEnumerable<T>
    // interface (foreach). This boxes List<T>.Enumerator (a struct) as
    // IEnumerator<T> and dispatches MoveNext/Current/Dispose through the
    // interface — previously unsupported (the value type had no interface
    // dispatch table, and the slot would have called the impl with the box
    // header instead of the unboxed payload). The empty-list fast path returns
    // the static SZGenericArrayEnumerator<T>.Empty, a type reached only through
    // a static field load. Together these were the "List<T> enumeration"
    // segfault gap that gates Concat/Join over List<T> and LINQ.
    internal static class Program
    {
        // Enumerate via the interface, not the concrete struct enumerator.
        private static int SumVia<T>(IEnumerable<T> items, Func<T, int> sel)
        {
            int sum = 0;
            int n = 0;
            foreach (T x in items)
            {
                sum += sel(x);
                n++;
            }
            return sum * 100 + n;
        }

        internal static int __GateEntry()
        {
            List<int> xs = new List<int>();
            xs.Add(1);
            xs.Add(2);
            xs.Add(3);
            xs.Add(4);
            Console.WriteLine(SumVia<int>(xs, v => v));         // 10*100+4 = 1004

            List<int> empty = new List<int>();
            Console.WriteLine(SumVia<int>(empty, v => v));      // 0 (Empty enumerator path)

            List<string> ws = new List<string>();
            ws.Add("ab");
            ws.Add("cde");
            ws.Add("f");
            Console.WriteLine(SumVia<string>(ws, s => s.Length)); // 6*100+3 = 603

            return 0;
        }
    }
}
