#nullable disable
using System;
using System.Runtime.CompilerServices;

namespace ArrayRuntimeBoxSubset;

enum Wide : long { First = 1, Second = 2 }
ref struct RefCell { public int Value; }

internal static class Program
{
    private struct SearchBox
    {
        public int Value;
        public override bool Equals(object value) => value is SearchBox other && Value % 10 == other.Value % 10;
        public override int GetHashCode() => Value % 10;
    }

    private struct ReferenceSlotBox
    {
        public int Value;
        public override bool Equals(object value) => value is ReferenceSlotBox other && Value % 10 == other.Value % 10;
        public override int GetHashCode() => Value % 10;
    }

    private static string Show(string text) => text is null ? "<null>"
        : text.Replace("\r", "").Replace("\n", "|");

    private static void Probe(string label, Func<string> operation)
    {
        try
        {
            Console.WriteLine(label + "=" + operation());
        }
        catch (Exception ex)
        {
            string parameter = ex is ArgumentException argument ? Show(argument.ParamName) : "<none>";
            string actual = ex is ArgumentOutOfRangeException range
                ? Show(range.ActualValue?.ToString()) : "<none>";
            Console.WriteLine(label + "=" + ex.GetType().Name + ":" + parameter + ":" + actual
                + ":" + Show(ex.Message));
        }
    }

    private static string BoxAs<T>(T value, Type type)
    {
        object boxed = RuntimeHelpers.Box(ref Unsafe.As<T, byte>(ref value), type.TypeHandle);
        if (boxed is null)
            return "null";
        return boxed.GetType().FullName + ":" + boxed + ":equal=" + boxed.Equals(value)
            + "/" + (boxed.GetHashCode() == value.GetHashCode());
    }

    internal static void Run()
    {
        Console.WriteLine("== runtime type-handle boxes and array refusals ==");
        Probe("int", () => BoxAs(7, typeof(int)));
        Probe("short", () => BoxAs((short)-3, typeof(short)));
        Probe("bool", () => BoxAs(true, typeof(bool)));
        Probe("enum", () => BoxAs(2L, typeof(Wide)));
        Probe("nullable value", () => BoxAs((int?)5, typeof(int?)));
        Probe("nullable empty", () => BoxAs((int?)null, typeof(int?)));
        object reference = new object();
        Probe("reference identity", () => ReferenceEquals(RuntimeHelpers.Box(
            ref Unsafe.As<object, byte>(ref reference), typeof(object).TypeHandle), reference).ToString());
        Probe("void box", () => BoxAs((byte)0, typeof(void)));
        Probe("by-ref-like box", () => BoxAs((byte)0, typeof(RefCell)));
        Probe("generic definition box", () => BoxAs((byte)0, typeof(System.Collections.Generic.List<>)));
        Probe("array of void", () => Array.CreateInstance(typeof(void), 1).Length.ToString());
        Probe("uninitialized by-ref-like", () => RuntimeHelpers.GetUninitializedObject(typeof(RefCell)).ToString());
        Probe("array by-ref-like", () => Array.CreateInstance(typeof(RefCell), 1).Length.ToString());
        Probe("array by-ref-like two dimensions", () => Array.CreateInstance(typeof(RefCell), 1, 2).Length.ToString());
        Console.WriteLine("runtime type-handle boxes and array refusals end");
    }

    internal static void RunReviewRegressions()
    {
        Console.WriteLine("== array producer and validation order ==");
        SearchBox value = new SearchBox { Value = 1 };
        object boxed = RuntimeHelpers.Box(ref Unsafe.As<SearchBox, byte>(ref value), typeof(SearchBox).TypeHandle);
        Array values = new object[] { boxed };
        Probe("runtime box search", () => Array.IndexOf(values, new SearchBox { Value = 11 })
            + ":" + Array.LastIndexOf(values, new SearchBox { Value = 11 }));
        SearchBox? nullable = value;
        object nullableBox = RuntimeHelpers.Box(ref Unsafe.As<SearchBox?, byte>(ref nullable), typeof(SearchBox?).TypeHandle);
        Array nullableValues = new object[] { nullableBox };
        Probe("nullable runtime box search", () => Array.IndexOf(nullableValues, new SearchBox { Value = 11 })
            + ":" + Array.LastIndexOf(nullableValues, new SearchBox { Value = 11 }));
        object uninitialized = RuntimeHelpers.GetUninitializedObject(typeof(SearchBox));
        Array uninitializedValues = new object[] { uninitialized };
        Probe("uninitialized runtime box search", () => Array.IndexOf(uninitializedValues, new SearchBox { Value = 10 })
            + ":" + Array.LastIndexOf(uninitializedValues, new SearchBox { Value = 10 }));
        object referenceSlot = new ReferenceSlotBox { Value = 1 };
        object referenceBox = RuntimeHelpers.Box(ref Unsafe.As<object, byte>(ref referenceSlot), typeof(object).TypeHandle);
        Array referenceValues = new object[] { referenceBox };
        Probe("reference-slot runtime box search", () => Array.IndexOf(referenceValues, new ReferenceSlotBox { Value = 11 })
            + ":" + Array.LastIndexOf(referenceValues, new ReferenceSlotBox { Value = 11 }));
        Probe("reference-slot runtime box identity", () => ReferenceEquals(referenceSlot, referenceBox).ToString());
        Array fromReferenceType = Array.CreateInstance(referenceBox.GetType(), 2);
        Probe("reference-slot runtime box type", () => Array.IndexOf(fromReferenceType, new ReferenceSlotBox { Value = 10 })
            + ":" + Array.LastIndexOf(fromReferenceType, new ReferenceSlotBox { Value = 10 }));
        Probe("void rank33 lengths", () => Array.CreateInstance(typeof(void), new int[33]).Length.ToString());
        Probe("by-ref-like rank33 lengths", () => Array.CreateInstance(typeof(RefCell), new int[33]).Length.ToString());
        Probe("void rank33 bounds", () => Array.CreateInstance(typeof(void), new int[33], new int[33]).Length.ToString());
        Probe("by-ref-like rank33 bounds", () => Array.CreateInstance(typeof(RefCell), new int[33], new int[33]).Length.ToString());
        Console.WriteLine("array producer and validation order end");
    }
}
