#nullable enable
using System;

namespace ArrayResizeSubset
{
    // BCL growable-collection / argument-guard primitives.
    //  * Array.Resize<T> (ref T[], int) — alloc + copy overlap + write back; the
    //    pattern Stack/Queue/etc. Grow use. Grow, shrink, and null source;
    //    RunSameLength: the array's own length keeps the instance.
    //  * Array.MaxLength — the 0x7FFFFFC7 grow ceiling (exercised indirectly).
    //  * ArgumentOutOfRangeException.ThrowIf* guards, whose generic-math leaves
    //    (INumberBase<T>.IsNegative, IComparisonOperators op_*) are [Intrinsic]
    //    bodyless and lower to the primitive op.
    internal static class Program
    {
        internal static int Run()
        {
            int[] a = { 1, 2, 3 };
            Array.Resize(ref a, 5);
            a[3] = 4;
            a[4] = 5;
            Console.WriteLine(string.Join(",", a));               // 1,2,3,4,5

            Array.Resize(ref a, 2);
            Console.WriteLine(string.Join(",", a));               // 1,2

            string[]? s = null;
            Array.Resize(ref s, 2);                               // null source -> length 0
            s[0] = "x";
            Console.WriteLine(s.Length + ":" + (s[1] ?? "null")); // 2:null

            ArgumentOutOfRangeException.ThrowIfNegative(5);        // ok
            ArgumentOutOfRangeException.ThrowIfGreaterThan(3, 10); // ok
            ArgumentOutOfRangeException.ThrowIfLessThan(7, 2);     // ok
            Console.WriteLine("guards-ok");                       // guards-ok

            try
            {
                ArgumentOutOfRangeException.ThrowIfNegative(-1);
                Console.WriteLine("no throw");
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("caught neg");                  // caught neg
            }

            try
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(9, 4);
                Console.WriteLine("no throw");
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("caught gt");                   // caught gt
            }

            return 0;
        }

        private sealed class Holder
        {
            public Entry[] Items = { new Entry { Name = "a" }, new Entry { Name = "b" } };
        }

        private struct Entry
        {
            public string Name;
        }

        // Resizing to the array's own length keeps the instance, whatever slot holds
        // it; any other length, and a null slot, gets a new T[] of the static T.
        internal static void RunSameLength()
        {
            int[] a = { 1, 2, 3 };
            int[] before = a;
            Array.Resize(ref a, 3);
            Console.WriteLine("resize same int: " + ReferenceEquals(a, before) + " " + string.Join(",", a));
            Array.Resize(ref a, 4);
            Console.WriteLine("resize grow int: " + ReferenceEquals(a, before) + " " + string.Join(",", a));

            int[] empty = new int[0];
            int[] emptyBefore = empty;
            Array.Resize(ref empty, 0);
            Console.WriteLine("resize same empty: " + ReferenceEquals(empty, emptyBefore));

            string[]? none = null;
            Array.Resize(ref none, 0);
            Console.WriteLine("resize null to 0: " + (none is null ? "null" : none.Length + " " + none.GetType().Name));

            object[] covariant = new string[] { "x", "y" };
            object[] covariantBefore = covariant;
            Array.Resize(ref covariant, 2);
            Console.WriteLine("resize same covariant: " + ReferenceEquals(covariant, covariantBefore)
                + " " + covariant.GetType().Name);
            Array.Resize(ref covariant, 3);
            Console.WriteLine("resize grow covariant: " + ReferenceEquals(covariant, covariantBefore)
                + " " + covariant.GetType().Name + " " + covariant[1] + " " + (covariant[2] ?? "null"));

            var holder = new Holder();
            Entry[] itemsBefore = holder.Items;
            Array.Resize(ref holder.Items, 2);
            Console.WriteLine("resize same field: " + ReferenceEquals(holder.Items, itemsBefore));
            Array.Resize(ref holder.Items, 1);
            Console.WriteLine("resize shrink field: " + ReferenceEquals(holder.Items, itemsBefore)
                + " " + holder.Items.Length + " " + holder.Items[0].Name);

            DayOfWeek[] days = { DayOfWeek.Monday };
            DayOfWeek[] daysBefore = days;
            Array.Resize(ref days, 1);
            Console.WriteLine("resize same enum: " + ReferenceEquals(days, daysBefore));

            Console.WriteLine("resize same generic: " + KeepsInstance(new long[] { 1, 2 })
                + " " + KeepsInstance(new[] { "p" }) + " " + KeepsInstance(new Entry[3]));
        }

        private static bool KeepsInstance<T>(T[] array)
        {
            T[] before = array;
            Array.Resize(ref array, array.Length);
            return ReferenceEquals(array, before);
        }
    }
}
