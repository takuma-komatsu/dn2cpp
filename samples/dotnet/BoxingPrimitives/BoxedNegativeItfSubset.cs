using System;
using System.Collections;
using System.Collections.Generic;

// The NEGATIVE half of the boxed-built-in interface test: an interface a boxed
// primitive does NOT implement must answer False. The positive matrix next door
// only proves that an admitted pair dispatches; a test that over-admits is
// invisible there, and the arm it lets run reads the box payload as a type
// pointer.
namespace BoxedNegativeItfSubset;

internal static class Program
{
    internal static void Run()
    {
        object of = 0.003f;
        Console.WriteLine("float is IEnumerable: " + (of is IEnumerable));
        Console.WriteLine("float is IComparable: " + (of is IComparable));
        Console.WriteLine("float CompareTo: " + ((IComparable)of).CompareTo(0.004f));

        object od = 0.003;
        Console.WriteLine("double is IEnumerable: " + (od is IEnumerable));
        object oi = 42;
        Console.WriteLine("int is IEnumerable: " + (oi is IEnumerable));
        Console.WriteLine("int is IEnumerable<char>: " + (oi is IEnumerable<char>));
        Console.WriteLine("int is IList: " + (oi is IList));
        Console.WriteLine("int is IDictionary: " + (oi is IDictionary));
        Console.WriteLine("int is ICollection: " + (oi is ICollection));

        // Boxed enum and the date family, whose type-infos carry a real map.
        object oe = DayOfWeek.Monday;
        Console.WriteLine("enum is IEnumerable: " + (oe is IEnumerable));
        object odt = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        Console.WriteLine("DateTime is IEnumerable: " + (odt is IEnumerable));
        object om = 3.5m;
        Console.WriteLine("decimal is IEnumerable: " + (om is IEnumerable));

        // Controls: the two boxed shapes that DO answer True.
        object os = "x";
        Console.WriteLine("string is IEnumerable: " + (os is IEnumerable));
        Console.WriteLine("string is IEnumerable<char>: " + (os is IEnumerable<char>));
        object oa = new int[] { 1, 2 };
        Console.WriteLine("int[] is IEnumerable: " + (oa is IEnumerable));

        // The shape the crash took: a True answer runs the foreach arm.
        Console.WriteLine("float enumerated: " + Enumerate(of));
        Console.WriteLine("string enumerated: " + Enumerate(os));
    }

    private static string Enumerate(object v)
    {
        if (v is not IEnumerable e)
        {
            return "not-enumerable";
        }

        int n = 0;
        foreach (object _ in e)
        {
            n++;
        }

        return "count=" + n;
    }
}
