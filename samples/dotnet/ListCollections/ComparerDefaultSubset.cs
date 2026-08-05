#nullable enable
using System;
using System.Collections.Generic;

namespace ComparerDefaultSubset
{
    // Comparer<T>.Default, an intrinsic sentinel (get_Default -> nullptr)
    // whose Compare lowers to a type-specialized three-way compare — the real one
    // routes through reflection + CreateInstanceForAnotherGenericParameter, which
    // is unsupported. Tested directly and through a generic helper (the real use
    // case: T closed by monomorphization to int/string/double/enum).
    internal static class Program
    {
        private enum Color { Red = 2, Green = 5, Blue = 9 }

        // The canonical pattern that needs Comparer<T>.Default in generic code.
        private static T MinOf<T>(IList<T> items)
        {
            Comparer<T> cmp = Comparer<T>.Default;
            T best = items[0];
            for (int i = 1; i < items.Count; i++)
                if (cmp.Compare(items[i], best) < 0)
                    best = items[i];
            return best;
        }

        internal static int __GateEntry()
        {
            Comparer<int> ci = Comparer<int>.Default;
            Console.WriteLine(ci.Compare(3, 5));            // -1
            Console.WriteLine(ci.Compare(9, 2));            // 1
            Console.WriteLine(ci.Compare(4, 4));            // 0

            Comparer<string> cs = Comparer<string>.Default;
            Console.WriteLine(cs.Compare("a", "b") < 0);    // True
            Console.WriteLine(cs.Compare("b", "a") > 0);    // True

            Comparer<double> cd = Comparer<double>.Default;
            Console.WriteLine(cd.Compare(2.5, 1.5) > 0);    // True

            Console.WriteLine(MinOf(new List<int> { 5, 3, 8, 1, 9 }));            // 1
            Console.WriteLine(MinOf(new List<string> { "pear", "fig", "apple" })); // apple
            Console.WriteLine(MinOf(new List<long> { 40L, 10L, 30L }));           // 10
            // Print the underlying value (enum-name ToString is a separate gap);
            // the point here is that Comparer<Color>.Default picks the min (Red=2).
            Console.WriteLine((int)MinOf(new List<Color> { Color.Blue, Color.Red, Color.Green })); // 2

            return 0;
        }
    }
}
