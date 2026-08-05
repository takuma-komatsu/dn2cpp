#nullable disable
using System;
using System.Collections.Generic;

namespace ArraySortGeneralSubset
{
    // Array.Sort over ANY element type (the devirtualized Comparer<T>.Default —
    // struct, reference, sub-word, unsigned, float, 64-bit enum, intrinsic value type),
    // the Sort<TKey,TValue> key+value overloads, Array.BinarySearch, and the span
    // mirrors of both. Also the float TOTAL order (a NaN sorts below every number and
    // equals itself), which is what Double.CompareTo says and what `<` alone does not.
    //
    // Every key here is DISTINCT on purpose: dn2cpp's comparer-driven sort is stable and
    // .NET's Array.Sort is an unstable introsort, so a tie would let the two disagree
    // about the order of equal keys without either being wrong.
    internal struct Point : IComparable<Point>
    {
        public int X;
        public int Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int CompareTo(Point other)
        {
            int c = X.CompareTo(other.X);
            return c != 0 ? c : Y.CompareTo(other.Y);
        }

        public override string ToString()
        {
            return "(" + X + "," + Y + ")";
        }
    }

    internal sealed class Person : IComparable<Person>
    {
        public string Name;
        public int Age;

        public Person(string name, int age)
        {
            Name = name;
            Age = age;
        }

        public int CompareTo(Person other)
        {
            return Age.CompareTo(other.Age);
        }

        public override string ToString()
        {
            return Name + ":" + Age;
        }
    }

    internal sealed class DescendingInt : IComparer<int>
    {
        public int Compare(int x, int y)
        {
            return y.CompareTo(x);
        }
    }

    internal sealed class DescendingDouble : IComparer<double>
    {
        public int Compare(double x, double y)
        {
            return y.CompareTo(x);
        }
    }

    internal sealed class ByLength : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return x.Length.CompareTo(y.Length);
        }
    }

    internal enum Level : long
    {
        Low = -5L,
        Mid = 0L,
        High = 1L << 40,
    }

    internal static class Program
    {
        private static void DumpInts(int[] a)
        {
            for (int i = 0; i < a.Length; i++)
            {
                Console.WriteLine(a[i]);
            }
        }

        private static void DumpStrings(string[] a)
        {
            for (int i = 0; i < a.Length; i++)
            {
                Console.WriteLine(a[i]);
            }
        }

        private static void DumpPoints(Point[] a)
        {
            for (int i = 0; i < a.Length; i++)
            {
                Console.WriteLine(a[i].ToString());
            }
        }

        internal static void Run()
        {
            // ---- comparerless Sort over element types the old intrinsic refused ----
            short[] sh = { 30, -10, 20, 5 };
            Array.Sort(sh);
            for (int i = 0; i < sh.Length; i++)
            {
                Console.WriteLine(sh[i]);
            }

            byte[] by = { 200, 3, 47, 128 };
            Array.Sort(by);
            for (int i = 0; i < by.Length; i++)
            {
                Console.WriteLine(by[i]);
            }

            char[] ch = { 'z', 'a', 'm' };
            Array.Sort(ch);
            for (int i = 0; i < ch.Length; i++)
            {
                Console.WriteLine(ch[i]);
            }

            // Unsigned: the array rep is the same int32 slot a signed int uses, so an
            // order that read it signed would put 3000000000 first.
            uint[] ui = { 3000000000u, 5u, 42u };
            Array.Sort(ui);
            for (int i = 0; i < ui.Length; i++)
            {
                Console.WriteLine(ui[i]);
            }

            float[] fl = { 2.5f, -1.5f, 0.5f };
            Array.Sort(fl);
            for (int i = 0; i < fl.Length; i++)
            {
                Console.WriteLine(fl[i]);
            }

            // A 64-bit-backed enum orders on all 64 bits (Low and High differ only above
            // the low word once High is 1 << 40).
            Level[] lv = { Level.High, Level.Low, Level.Mid };
            Array.Sort(lv);
            for (int i = 0; i < lv.Length; i++)
            {
                Console.WriteLine((long)lv[i]);
            }

            // Intrinsic value types order by their runtime three-way.
            decimal[] dec = { 3.5m, -2m, 0.25m };
            Array.Sort(dec);
            for (int i = 0; i < dec.Length; i++)
            {
                Console.WriteLine(dec[i]);
            }

            DateTime[] dt =
            {
                new DateTime(2020, 5, 1),
                new DateTime(1999, 1, 2),
                new DateTime(2024, 12, 31),
            };
            Array.Sort(dt);
            for (int i = 0; i < dt.Length; i++)
            {
                Console.WriteLine(dt[i].Ticks);
            }

            TimeSpan[] ts = { TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(-5), TimeSpan.FromSeconds(1) };
            Array.Sort(ts);
            for (int i = 0; i < ts.Length; i++)
            {
                Console.WriteLine(ts[i].Ticks);
            }

            // A struct element with IComparable<T> — the arm that dispatches through a
            // synthesized Comparer<T>.Default.
            Point[] pts = { new Point(2, 1), new Point(1, 9), new Point(2, 0) };
            Array.Sort(pts);
            DumpPoints(pts);

            // A reference element with IComparable<T>. The old code's null-comparer
            // fallback read these objects' bytes as a STRING (the ordinal compare).
            Person[] ppl = { new Person("c", 30), new Person("a", 10), new Person("b", 20) };
            Array.Sort(ppl);
            for (int i = 0; i < ppl.Length; i++)
            {
                Console.WriteLine(ppl[i].ToString());
            }

            // ---- float ordering is a TOTAL order: NaN below every number, equal to itself ----
            double[] nan = { 1.0, double.NaN, -1.0, 2.0 };
            Array.Sort(nan);
            for (int i = 0; i < nan.Length; i++)
            {
                Console.WriteLine(nan[i]);
            }
            Console.WriteLine(double.NaN.CompareTo(double.NaN));
            Console.WriteLine(double.NaN.CompareTo(-1.0));
            Console.WriteLine((-1.0).CompareTo(double.NaN));

            float[] fnan = { 1.0f, float.NaN, -1.0f };
            Array.Sort(fnan);
            for (int i = 0; i < fnan.Length; i++)
            {
                Console.WriteLine(fnan[i]);
            }

            // Ordering and equality agree about NaN (both say a NaN is its own equal).
            double[] nanKeys = { double.NaN, 1.0 };
            Array.Sort(nanKeys);
            Console.WriteLine(Array.IndexOf(nanKeys, double.NaN));

            // ---- comparer / comparison over elements the old intrinsic refused ----
            Point[] pts2 = { new Point(1, 1), new Point(3, 3), new Point(2, 2) };
            Array.Sort(pts2, (a, b) => b.X.CompareTo(a.X)); // Comparison<Point>
            DumpPoints(pts2);

            int[] desc = { 4, 1, 9, 2 };
            Array.Sort(desc, new DescendingInt()); // IComparer<int>
            DumpInts(desc);

            // List<T>.Sort() reaches Array.Sort(items, 0, size, (IComparer<T>)null) — the
            // null-comparer arm IS the default-order path, not a fallback nobody takes.
            List<Point> lp = new List<Point> { new Point(5, 0), new Point(1, 2), new Point(3, 7) };
            lp.Sort();
            for (int i = 0; i < lp.Count; i++)
            {
                Console.WriteLine(lp[i].ToString());
            }

            List<Person> lpe = new List<Person> { new Person("x", 9), new Person("y", 1) };
            lpe.Sort();
            for (int i = 0; i < lpe.Count; i++)
            {
                Console.WriteLine(lpe[i].ToString());
            }

            // ---- range overloads ----
            int[] rng = { 9, 8, 7, 6, 5 };
            Array.Sort(rng, 1, 3);
            DumpInts(rng);

            string[] srng = { "eeeee", "dddd", "ccc", "bb", "a" };
            Array.Sort(srng, 1, 3, new ByLength());
            DumpStrings(srng);

            // ---- key + value (pair) sort: two type arguments, all four overloads ----
            int[] k1 = { 3, 1, 2 };
            string[] v1 = { "three", "one", "two" };
            Array.Sort(k1, v1);
            DumpInts(k1);
            DumpStrings(v1);

            // A null items array is legal — it degrades to a bare key sort.
            int[] k1b = { 7, 5, 6 };
            Array.Sort(k1b, (string[])null);
            DumpInts(k1b);

            double[] k2 = { 3.5, 1.5, 2.5 };
            string[] v2 = { "c", "a", "b" };
            Array.Sort(k2, v2, new DescendingDouble());
            for (int i = 0; i < k2.Length; i++)
            {
                Console.WriteLine(k2[i]);
            }
            DumpStrings(v2);

            // Struct keys with a struct-sized value: both permute in lockstep.
            Point[] k3 = { new Point(3, 0), new Point(1, 0), new Point(2, 0) };
            int[] v3 = { 30, 10, 20 };
            Array.Sort(k3, v3);
            DumpPoints(k3);
            DumpInts(v3);

            int[] k4 = { 5, 4, 3, 2, 1 };
            string[] v4 = { "e", "d", "c", "b", "a" };
            Array.Sort(k4, v4, 1, 3);
            DumpInts(k4);
            DumpStrings(v4);

            long[] k5 = { 50L, 40L, 30L, 20L, 10L };
            char[] v5 = { 'e', 'd', 'c', 'b', 'a' };
            Array.Sort(k5, v5, 1, 3, Comparer<long>.Default);
            for (int i = 0; i < k5.Length; i++)
            {
                Console.WriteLine(k5[i]);
            }
            for (int i = 0; i < v5.Length; i++)
            {
                Console.WriteLine(v5[i]);
            }

            // ---- BinarySearch: every generic overload; a miss reports ~insertionPoint ----
            int[] bs = { 1, 3, 5, 7, 9 };
            Console.WriteLine(Array.BinarySearch(bs, 5));
            Console.WriteLine(Array.BinarySearch(bs, 4));
            Console.WriteLine(Array.BinarySearch(bs, 0));
            Console.WriteLine(Array.BinarySearch(bs, 10));
            Console.WriteLine(Array.BinarySearch(bs, 1, 3, 7));
            Console.WriteLine(Array.BinarySearch(bs, 1, 3, 2));

            string[] sbs = { "apple", "banana", "cherry" };
            Console.WriteLine(Array.BinarySearch(sbs, "banana"));
            Console.WriteLine(Array.BinarySearch(sbs, "blue"));

            Point[] pbs = { new Point(1, 0), new Point(2, 0), new Point(3, 0) };
            Console.WriteLine(Array.BinarySearch(pbs, new Point(2, 0)));
            Console.WriteLine(Array.BinarySearch(pbs, new Point(2, 5)));

            Person[] rbs = { new Person("a", 10), new Person("b", 20), new Person("c", 30) };
            Console.WriteLine(Array.BinarySearch(rbs, new Person("?", 20)));
            Console.WriteLine(Array.BinarySearch(rbs, new Person("?", 25)));

            int[] dbs = { 9, 7, 5, 3, 1 };
            Console.WriteLine(Array.BinarySearch(dbs, 5, new DescendingInt()));
            Console.WriteLine(Array.BinarySearch(dbs, 4, new DescendingInt()));
            Console.WriteLine(Array.BinarySearch(dbs, 1, 3, 3, new DescendingInt()));

            // List<T>.BinarySearch routes straight through Array.BinarySearch(…, comparer).
            List<int> lbs = new List<int> { 2, 4, 6, 8 };
            Console.WriteLine(lbs.BinarySearch(6));
            Console.WriteLine(lbs.BinarySearch(5));

            List<Point> lpbs = new List<Point> { new Point(1, 0), new Point(3, 0), new Point(5, 0) };
            Console.WriteLine(lpbs.BinarySearch(new Point(3, 0)));
            Console.WriteLine(lpbs.BinarySearch(new Point(4, 0)));

            // ---- span mirrors ----
            Point[] sp = { new Point(4, 0), new Point(2, 0), new Point(6, 0) };
            sp.AsSpan().Sort();
            DumpPoints(sp);

            float[] spf = { 3.5f, 1.5f, 2.5f };
            spf.AsSpan().Sort();
            for (int i = 0; i < spf.Length; i++)
            {
                Console.WriteLine(spf[i]);
            }

            Person[] spp = { new Person("p", 8), new Person("q", 2), new Person("r", 5) };
            spp.AsSpan().Sort();
            for (int i = 0; i < spp.Length; i++)
            {
                Console.WriteLine(spp[i].ToString());
            }

            int[] spk = { 3, 1, 2 };
            string[] spv = { "c", "a", "b" };
            spk.AsSpan().Sort(spv.AsSpan());
            DumpInts(spk);
            DumpStrings(spv);

            Point[] spk2 = { new Point(9, 0), new Point(7, 0), new Point(8, 0) };
            int[] spv2 = { 90, 70, 80 };
            spk2.AsSpan().Sort(spv2.AsSpan());
            DumpPoints(spk2);
            DumpInts(spv2);
        }
    }
}
