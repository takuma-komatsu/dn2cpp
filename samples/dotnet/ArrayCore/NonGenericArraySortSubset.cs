#nullable disable
using System;
using System.Collections;

namespace NonGenericArraySortSubset
{
    // The NON-generic, boxed-element Array.Sort(Array[, index, length][, IComparer]) and
    // Array.BinarySearch(Array, object[, index, length][, IComparer]) surface of the old
    // non-generic BCL collections. Binding the non-generic overload needs a statically
    // System.Array-typed receiver (a plain int[] binds the generic Array.Sort<T>). Exercises a
    // boxed-primitive element, a user IComparable, an explicit IComparer, the range forms, and
    // ArrayList (which reaches Array.Sort(Array,int,int,IComparer)/BinarySearch internally).
    //
    // Keys are DISTINCT everywhere: Array.Sort is unstable, so ties could diverge from real .NET.
    // No strings are sorted/searched: dn2cpp orders strings ordinally while .NET's non-generic
    // Comparer.Default is culture-sensitive (a guaranteed, intended divergence).
    internal sealed class Item : IComparable
    {
        public int Key;
        public Item(int k) { Key = k; }
        public int CompareTo(object o) { return Key.CompareTo(((Item)o).Key); }
    }

    internal sealed class RevItemCmp : IComparer   // non-generic IComparer over Item, descending
    {
        public int Compare(object a, object b) { return ((Item)b).Key.CompareTo(((Item)a).Key); }
    }

    internal sealed class DescIntCmp : IComparer   // non-generic IComparer over boxed ints, descending
    {
        public int Compare(object a, object b) { return ((int)b).CompareTo((int)a); }
    }

    internal enum Color { Red = 3, Green = 1, Blue = 2, Yellow = 5 }        // int-backed
    internal enum Big : long { A = 10L, B = 3L, C = 8L, D = 1L }            // long-backed

    internal static class Program
    {
        private static void DumpInts(int[] a)
        {
            foreach (int x in a) Console.WriteLine(x);
        }

        private static void DumpItems(Item[] a)
        {
            foreach (Item x in a) Console.WriteLine(x.Key);
        }

        internal static void Run()
        {
            Console.WriteLine("-- a: boxed int default sort/search --");
            int[] ints = { 5, 2, 9, 1, 7, 3 };
            Array a1 = ints;
            Array.Sort(a1);
            DumpInts(ints);                                   // 1 2 3 5 7 9
            Console.WriteLine(Array.BinarySearch(a1, (object)7)); // 4
            Console.WriteLine(Array.BinarySearch(a1, (object)4)); // -4 (~insertion)

            Console.WriteLine("-- a': range sort/search --");
            int[] ints2 = { 90, 8, 7, 6, 5, 40 };
            Array a2 = ints2;
            Array.Sort(a2, 1, 4);
            DumpInts(ints2);                                  // 90 5 6 7 8 40
            Console.WriteLine(Array.BinarySearch(a2, 1, 4, (object)7)); // 3

            Console.WriteLine("-- b: user IComparable default sort/search --");
            Item[] items = { new Item(30), new Item(10), new Item(20), new Item(40), new Item(25) };
            Array ib = items;
            Array.Sort(ib);
            DumpItems(items);                                 // 10 20 25 30 40
            Console.WriteLine(Array.BinarySearch(ib, new Item(25)));  // 2
            Console.WriteLine(Array.BinarySearch(ib, new Item(15)));  // -2

            Console.WriteLine("-- c: explicit IComparer sort/search --");
            int[] ints3 = { 5, 2, 9, 1, 7, 3 };
            Array a3 = ints3;
            IComparer desc = new DescIntCmp();
            Array.Sort(a3, desc);
            DumpInts(ints3);                                  // 9 7 5 3 2 1
            Console.WriteLine(Array.BinarySearch(a3, (object)5, desc)); // 2

            Item[] items2 = { new Item(30), new Item(10), new Item(20), new Item(40) };
            Array ic = items2;
            IComparer rev = new RevItemCmp();
            Array.Sort(ic, rev);
            DumpItems(items2);                                // 40 30 20 10
            Console.WriteLine(Array.BinarySearch(ic, new Item(20), rev)); // 2

            // (d) ArrayList integration — it reaches Array.Sort(Array,int,int,IComparer) /
            // Array.BinarySearch(Array,int,int,object,IComparer) internally. The default (no-arg)
            // Sort routes through System.Collections.Comparer.Default, whose real body does
            // `element as IComparable`, so the elements must be reference types that implement it
            // (a boxed PRIMITIVE has no interface map, so it is NOT exercised here — see (f)).
            // An explicit custom comparer bypasses Comparer.Default entirely.
            Console.WriteLine("-- d: ArrayList user-IComparable + custom comparer --");
            ArrayList al = new ArrayList();
            al.Add(new Item(30)); al.Add(new Item(10)); al.Add(new Item(20)); al.Add(new Item(40));
            al.Sort();                                              // Comparer.Default -> Item.CompareTo
            foreach (object o in al) Console.WriteLine(((Item)o).Key); // 10 20 30 40
            Console.WriteLine(al.BinarySearch(new Item(20)));       // 1

            ArrayList al2 = new ArrayList();
            al2.Add(5); al2.Add(2); al2.Add(9); al2.Add(1); al2.Add(7);
            al2.Sort(new DescIntCmp());                             // custom comparer unboxes ints
            foreach (object o in al2) Console.WriteLine((int)o);    // 9 7 5 2 1

            // (e) boxed-enum default order — dn2cpp_object_compare reads the backing width and
            // signedness off enumUnderlying (int-backed here, and a long-backed enum), matching
            // TryCompareLValue. The inline enum arm serves because it is tried BEFORE the
            // IComparable probe — not because a boxed enum has no map: it carries the shared
            // System.Enum one, and only a cross-enum-type compare reaches the probe.
            Console.WriteLine("-- e: boxed enum default sort/search --");
            Color[] colors = { Color.Red, Color.Green, Color.Blue, Color.Yellow };
            Array ce = colors;
            Array.Sort(ce);
            foreach (Color c in colors) Console.WriteLine((int)c);  // 1 2 3 5
            Console.WriteLine(Array.BinarySearch(ce, Color.Blue));  // 1

            Big[] bigs = { Big.A, Big.B, Big.C, Big.D };
            Array be = bigs;
            Array.Sort(be);
            foreach (Big b in bigs) Console.WriteLine((long)b);     // 1 3 8 10
            Console.WriteLine(Array.BinarySearch(be, Big.C));       // 2

            // (f) no-arg ArrayList.Sort() over BOXED INT elements. Routes through
            // System.Collections.Comparer.Default, whose real body does `x as IComparable` and threw
            // "At least one object must implement IComparable" on a boxed primitive; the
            // Comparer.Compare body intercept lowers it to dn2cpp_object_compare, which orders boxed
            // ints inline. (The int-keyed non-generic SortedList
            // takes the same Comparer.Default-over-boxed-primitive path and is exercised by
            // DictCollections, which references System.Collections.NonGeneric where SortedList lives;
            // ArrayCore stays on CoreLib alone, as several other gates transpile it.)
            Console.WriteLine("-- f: ArrayList.Sort() over boxed ints --");
            ArrayList ai = new ArrayList();
            ai.Add(5); ai.Add(2); ai.Add(9); ai.Add(1); ai.Add(7); ai.Add(3);
            ai.Sort();
            foreach (object o in ai) Console.WriteLine((int)o);     // 1 2 3 5 7 9
            Console.WriteLine(ai.BinarySearch(7));                  // 4
        }
    }
}
