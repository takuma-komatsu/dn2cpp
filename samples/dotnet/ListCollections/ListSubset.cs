#nullable enable
using System;
using System.Collections.Generic;

namespace ListSubset
{
    // Drive the *real* System.Private.CoreLib's List<T> through the
    // transpiler. Unlike MultiAssembly (hand-written MiniCorlib), the IL bodies
    // for List<T>.ctor/Add/get_Item/get_Count/grow come from the real BCL,
    // passed to dn2cpp with -r and tree-shaken to just the reached methods.
    internal static class Program
    {
        internal static int __GateEntry()
        {
            List<int> xs = new List<int>();
            xs.Add(10);
            xs.Add(20);
            xs.Add(30);

            // Force a grow past the default capacity to exercise the
            // Array.Empty -> first-alloc -> double-capacity copy path.
            for (int i = 0; i < 10; i++)
            {
                xs.Add(i);
            }

            Console.WriteLine(xs.Count);   // 13

            int sum = 0;
            for (int i = 0; i < xs.Count; i++)
            {
                sum = sum + xs[i];
            }
            Console.WriteLine(sum);        // 60 + (0..9 = 45) = 105

            xs[0] = 100;                   // set_Item
            Console.WriteLine(xs[0]);      // 100

            // List<string> (ref-backed array), foreach over the struct
            // enumerator, and Insert/RemoveAt (element shifts via Array.Copy).
            List<string> names = new List<string>();
            names.Add("alpha");
            names.Add("gamma");
            names.Insert(1, "beta");       // {alpha, beta, gamma}
            names.RemoveAt(0);             // {beta, gamma}

            Console.WriteLine(names.Count); // 2

            // foreach lowers to List<T>.GetEnumerator() -> a struct enumerator
            // (MoveNext / get_Current) used by value, then Dispose.
            string joined = "";
            foreach (string n in names)
            {
                joined = joined + n + ".";
            }
            Console.WriteLine(joined);     // beta.gamma.

            // foreach over the List<int> too (value-typed elements).
            int total = 0;
            foreach (int v in xs)
            {
                total = total + v;
            }
            Console.WriteLine(total);      // 105 - 10 + 100 = 195

            // Contains / IndexOf / Remove route through Array.IndexOf<T>,
            // which compares elements by value (int) / by string content.
            Console.WriteLine(names.IndexOf("gamma"));   // 1
            Console.WriteLine(names.Contains("beta"));    // True
            Console.WriteLine(names.Contains("delta"));   // False
            bool removed = names.Remove("beta");          // {gamma}
            Console.WriteLine(removed);                    // True
            Console.WriteLine(names.Count);                // 1
            Console.WriteLine(names[0]);                    // gamma

            Console.WriteLine(xs.IndexOf(30));            // 2
            Console.WriteLine(xs.Contains(7));            // True (added in the 0..9 loop)

            return 0;
        }
    }
}
