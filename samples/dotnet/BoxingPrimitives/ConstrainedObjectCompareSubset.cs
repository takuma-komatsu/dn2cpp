using System;
using System.Collections;
using System.Collections.Generic;

// CompareTo(object) on a primitive, string, decimal or date/time type through the
// mouths that do not name the type's own method — a constrained call to
// IComparable.CompareTo(object) under a generic, and a boxed receiver — plus the
// direct object overload. Each answers the type's own CompareTo: null sorts first,
// and a box of another type throws .NET's ArgumentException naming the receiver's
// type. The typed rows then ask IComparable<T>.CompareTo under a generic, the three
// default comparers and a boxed IComparable<T> receiver, each answering the raw
// difference for the sub-word integers and Char and the unsigned order of nuint.
namespace ConstrainedObjectCompareSubset
{
    internal static class Program
    {
        private static string Try(Func<int> compare)
        {
            try
            {
                return compare().ToString();
            }
            catch (ArgumentException ex)
            {
                return ex.GetType().Name + ": " + ex.Message;
            }
        }

        private static int Untyped<T>(T value, object other) where T : IComparable => value.CompareTo(other);

        private static void Row<T>(string name, T high, T low, object foreign) where T : IComparable
        {
            Console.WriteLine("ccmp " + name + ": " + Untyped(high, low) + " " + Untyped(low, high) + " "
                + Untyped(high, null) + " | " + Try(() => Untyped(high, foreign))
                + " | " + Try(() => ((IComparable)high).CompareTo(foreign)));
        }

        private static int Typed<T>(T value, T other) where T : IComparable<T> => value.CompareTo(other);

        private static void TypedRow<T>(string name, T high, T low) where T : IComparable, IComparable<T>
        {
            Console.WriteLine("tcmp " + name + ": " + Typed(high, low) + " " + Typed(low, high) + " "
                + Comparer<T>.Default.Compare(high, low) + " " + Comparer.Default.Compare(high, low) + " "
                + Comparer<object>.Default.Compare(high, low) + " " + ((IComparable<T>)high).CompareTo(low));
        }

        internal static void Run()
        {
            Console.WriteLine("== constrained CompareTo(object) ==");
            Row("bool", true, false, 1);
            Row("char", 'c', 'a', 99);
            Row("sbyte", (sbyte)100, (sbyte)-100, 1);
            Row("byte", (byte)200, (byte)3, 1);
            Row("short", (short)30000, (short)-30000, 1);
            Row("ushort", (ushort)65000, (ushort)1, 1);
            Row("int", 7, -7, 7L);
            Row("uint", 4000000000U, 1U, 1);
            Row("long", long.MaxValue, long.MinValue, 1);
            Row("ulong", ulong.MaxValue, 1UL, 1);
            Row("float", 2.5f, float.NaN, 2.5);
            Row("double", 2.5, double.NaN, 2.5f);
            Row("nint", (nint)5, (nint)(-5), 5);
            Row("nuint", (nuint)5, (nuint)1, 5);
            Row("string", "b", "a", 1);
            Row("decimal", 2.5m, -1m, 1);
            Row("DateTime", new DateTime(2020, 1, 2), new DateTime(2020, 1, 1), 1);
            Row("TimeSpan", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(-1), 1);
            Row("DateTimeOffset", new DateTimeOffset(2020, 1, 2, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), 1);
            Row("DateOnly", new DateOnly(2020, 1, 2), new DateOnly(2020, 1, 1), 1);
            Row("TimeOnly", new TimeOnly(2, 0), new TimeOnly(1, 0), 1);
            Console.WriteLine("direct: " + Try(() => 5.CompareTo((object)"x")) + " | " + Try(() => 'a'.CompareTo((object)97))
                + " | " + Try(() => 1.5.CompareTo((object)1)));
            TypedRow("bool", true, false);
            TypedRow("char", 'c', 'a');
            TypedRow("sbyte", (sbyte)100, (sbyte)-100);
            TypedRow("byte", (byte)200, (byte)3);
            TypedRow("short", (short)30000, (short)-30000);
            TypedRow("ushort", (ushort)65000, (ushort)1);
            TypedRow("int", 7, -7);
            TypedRow("uint", 4000000000U, 1U);
            TypedRow("long", long.MaxValue, long.MinValue);
            TypedRow("ulong", ulong.MaxValue, 1UL);
            TypedRow("float", 2.5f, float.NaN);
            TypedRow("double", 2.5, double.NaN);
            TypedRow("nint", (nint)5, (nint)(-5));
            TypedRow("nuint", nuint.MaxValue, (nuint)1);
            TypedRow("string", "b", "a");
            TypedRow("decimal", 2.5m, -1m);
            TypedRow("DateTime", new DateTime(2020, 1, 2), new DateTime(2020, 1, 1));
            TypedRow("TimeSpan", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(-1));
        }
    }
}
