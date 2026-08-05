#nullable disable
using System;
using System.Collections.Generic;

namespace OrdinalCultureComparerSubset
{
    // StringComparer.CurrentCulture is intercepted to an ordinal GenericComparer<string>,
    // which keeps the whole CompareInfo/GlobalizationMode/ICU subtree unreachable —
    // System.Linq's OrderBy-over-string substitutes this same getter for
    // Comparer<string>.Default. Test data is distinct lowercase ASCII, where ordinal and
    // culture order agree, and only the SIGN of Compare is observed since dn2cpp returns
    // the code-unit delta rather than a normalised -1/0/1.
    internal static class Program
    {
        private static void InsertionSort(string[] a, IComparer<string> c)
        {
            for (int i = 1; i < a.Length; i++)
            {
                string key = a[i];
                int j = i - 1;
                while (j >= 0 && c.Compare(a[j], key) > 0)
                {
                    a[j + 1] = a[j];
                    j--;
                }
                a[j + 1] = key;
            }
        }

        internal static void Run()
        {
            IComparer<string> cmp = StringComparer.CurrentCulture;

            string[] words = { "banana", "apple", "cherry", "date", "blueberry", "avocado" };
            InsertionSort(words, cmp);
            foreach (string w in words)
                Console.WriteLine(w);
            Console.WriteLine("--");

            // Sign of the comparer on representative pairs (ordinal == culture here).
            Console.WriteLine(cmp.Compare("apple", "banana") < 0);   // True
            Console.WriteLine(cmp.Compare("banana", "apple") > 0);   // True
            Console.WriteLine(cmp.Compare("apple", "apple") == 0);   // True
            Console.WriteLine(cmp.Compare("apple", "avocado") < 0);  // True
            Console.WriteLine(cmp.Compare("ab", "abc") < 0);         // True (prefix shorter first)
        }
    }
}
