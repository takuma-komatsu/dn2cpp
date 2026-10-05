#nullable disable
using System;
using System.Runtime.CompilerServices;

namespace ArrayRuntimeBoxSubset;

enum Wide : long { First = 1, Second = 2 }
ref struct RefCell { public int Value; }

internal static class Program
{
    private struct HandlePayload
    {
        public int Value;
        public override string ToString() => "handle:" + Value;
    }

    private struct NullablePayload
    {
        public int Value;
        public override string ToString() => "nullable:" + Value;
    }

    private struct IndirectPayload
    {
        public int Value;
        public override string ToString() => "indirect:" + Value;
    }

    private struct GenericPayload<T>
    {
        public int Value;
        public override string ToString() => typeof(T).Name + ":" + Value;
    }

    private struct OrdinaryPayload
    {
        public int Value;
        public override string ToString() => "ordinary:" + Value;
    }

    private struct UnselectedPayload
    {
        public override string ToString() => "unselected override";
    }

    private static RuntimeTypeHandle _formatHandle;

    private static RuntimeTypeHandle FormatHandle() => typeof(IndirectPayload).TypeHandle;
    private static object BoxHandle(ref byte value, RuntimeTypeHandle handle) => RuntimeHelpers.Box(ref value, handle);
    private static object BoxValue<T>(ref T value) => RuntimeHelpers.Box(ref Unsafe.As<T, byte>(ref value), typeof(T).TypeHandle);

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

    internal static void RunFormattingRegressions()
    {
        Console.WriteLine("== runtime-handle boxed formatting ==");
        HandlePayload value = new HandlePayload { Value = 17 };
        object boxed = RuntimeHelpers.Box(ref Unsafe.As<HandlePayload, byte>(ref value), typeof(HandlePayload).TypeHandle);
        value.Value = 29;
        Console.WriteLine("plain copied=" + boxed.ToString() + ":" + ((HandlePayload)boxed).Value);
        ref HandlePayload payload = ref Unsafe.Unbox<HandlePayload>(boxed);
        payload.Value = 23;
        object second = RuntimeHelpers.Box(ref Unsafe.As<HandlePayload, byte>(ref value), typeof(HandlePayload).TypeHandle);
        Console.WriteLine("plain mutated=" + boxed.ToString() + ":" + second.ToString() + ":distinct=" + !ReferenceEquals(boxed, second));
        NullablePayload? nullable = new NullablePayload { Value = 31 };
        object nullableBox = RuntimeHelpers.Box(ref Unsafe.As<NullablePayload?, byte>(ref nullable), typeof(NullablePayload?).TypeHandle);
        nullable = new NullablePayload { Value = 43 };
        Console.WriteLine("nullable copied=" + nullableBox.ToString() + ":underlying=" + (nullableBox.GetType() == typeof(NullablePayload)));
        nullable = null;
        Console.WriteLine("nullable empty=" + (RuntimeHelpers.Box(ref Unsafe.As<NullablePayload?, byte>(ref nullable), typeof(NullablePayload?).TypeHandle) is null));
        IndirectPayload indirect = new IndirectPayload { Value = 53 };
        _formatHandle = FormatHandle();
        RuntimeTypeHandle localHandle = _formatHandle;
        object indirectBox = BoxHandle(ref Unsafe.As<IndirectPayload, byte>(ref indirect), localHandle);
        indirect.Value = 67;
        Console.WriteLine("indirect copied=" + indirectBox.ToString() + ":type=" + (indirectBox.GetType() == typeof(IndirectPayload)));
        GenericPayload<int> number = new GenericPayload<int> { Value = 71 };
        GenericPayload<string> text = new GenericPayload<string> { Value = 79 };
        Console.WriteLine("generic payloads=" + BoxValue(ref number).ToString() + "/" + BoxValue(ref text).ToString());
        OrdinaryPayload ordinary = new OrdinaryPayload { Value = 83 };
        object ordinaryBox = ordinary;
        ordinary.Value = 89;
        Console.WriteLine("ordinary control=" + ordinaryBox.ToString() + "/" + ordinary.ToString());
        Console.WriteLine("unselected type=" + typeof(UnselectedPayload).Name);
        Console.WriteLine("runtime-handle boxed formatting end");
    }

    internal static void RunExcessRankMessages()
    {
        Console.WriteLine("== excessive array rank diagnostics ==");
        RankFailure("int lengths", typeof(int), 33, false);
        RankFailure("object lengths", typeof(object), 33, false);
        RankFailure("char lengths", typeof(char), 34, false);
        RankFailure("int bounds", typeof(int), 33, true);
        RankFailure("object bounds", typeof(object), 34, true);
        RankFailure("char bounds", typeof(char), 33, true);
        Array accepted = Array.CreateInstance(typeof(int), new int[32]);
        Console.WriteLine("rank32 accepted=" + accepted.Rank + ":" + accepted.Length);
        Probe("rank invalid length", () =>
        {
            var lengths = new int[33];
            lengths[0] = -1;
            return Array.CreateInstance(typeof(int), lengths).Length.ToString();
        });
        Probe("rank null lengths", () => Array.CreateInstance(typeof(int), (int[])null).Length.ToString());
        Probe("rank bounds mismatch", () => Array.CreateInstance(typeof(int), new int[33], new int[32]).Length.ToString());
        Console.WriteLine("excessive array rank diagnostics end");
    }

    private static void RankFailure(string label, Type element, int rank, bool bounds)
    {
        try
        {
            int[] lengths = new int[rank];
            Array value = bounds ? Array.CreateInstance(element, lengths, new int[rank])
                : Array.CreateInstance(element, lengths);
            Console.WriteLine(label + "=returned:" + value.Rank);
        }
        catch (Exception fault)
        {
            Console.WriteLine(label + "=" + fault.GetType().FullName + "/"
                + fault.HResult.ToString("X8") + ":" + Show(fault.Message));
        }
    }
}
