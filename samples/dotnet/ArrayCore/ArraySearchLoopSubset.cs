#nullable enable
using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace ArraySearchLoopSubset;

internal struct LoopFirstMatch
{
    internal static int Calls;
    public override bool Equals(object? other) { Calls++; return other is null; }
    public override int GetHashCode() => 0;
}

internal struct LoopSecondMatch
{
    internal static int Calls;
    public override bool Equals(object? other) { Calls++; return other is null; }
    public override int GetHashCode() => 0;
}

internal struct LoopTypeMatch
{
    internal static int Calls;
    public override bool Equals(object? other) { Calls++; return other is null; }
    public override int GetHashCode() => 0;
}

internal struct DeepTypeMatch
{
    internal static int Calls;
    public override bool Equals(object? other) { Calls++; return other is null; }
    public override int GetHashCode() => 0;
}

internal struct LateLookupMatch
{
    internal static int Calls;
    public override bool Equals(object? other) { Calls++; return other is null; }
    public override int GetHashCode() => 0;
}

internal struct SharedCallsiteFirstMatch
{
    internal static int Calls;
    public override bool Equals(object? other) { Calls++; return other is null; }
    public override int GetHashCode() => 0;
}

internal struct SharedCallsiteSecondMatch
{
    internal static int Calls;
    public override bool Equals(object? other) { Calls++; return other is null; }
    public override int GetHashCode() => 0;
}

internal struct InterfaceListMatch
{
    internal static int Calls;
    internal int Value;
    public override bool Equals(object? other)
    {
        Calls++;
        return other is InterfaceListMatch match && Value == match.Value;
    }
    public override int GetHashCode() => Value;
}

internal struct ContravariantSearchMatch
{
    internal static int Calls;
    public override bool Equals(object? other) { Calls++; return other is null; }
    public override int GetHashCode() => 0;
}

internal struct SharedReadWriteMatch
{
    internal static int Calls;
    public override bool Equals(object? other) { Calls++; return other is null; }
    public override int GetHashCode() => 0;
}

internal sealed class TypeArrayHolder
{
    internal Type[] Values;
    internal TypeArrayHolder(Type[] values) { Values = values; }
}

internal interface IArraySearcher<in T>
{
    int Search(T values);
}

internal sealed class SearchHolder
{
    internal Array Values;
    internal SearchHolder(Array values) { Values = values; }
}

internal sealed class ArraySearcher : IArraySearcher<object>
{
    public int Search(object value) => Array.IndexOf(((SearchHolder)value).Values, null);
}

internal struct LateLookupUnselectedMatch
{
    public override bool Equals(object? other) => other is null;
    public override int GetHashCode() => 0;
}

internal static class LookupOverloads
{
    public static LateLookupMatch[] Values(int value) => new LateLookupMatch[1];
    public static LateLookupUnselectedMatch[] Values(string value) => new LateLookupUnselectedMatch[1];
}

internal struct LoopUnsearchedMatch
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
    public int[] Values;
    public LoopUnsearchedMatch() { Values = Array.Empty<int>(); }
}

internal static class Program
{
    private static string LocalSearch()
    {
        Array values = new LoopFirstMatch[1];
        LoopFirstMatch.Calls = LoopSecondMatch.Calls = 0;
        int result = 0;
        int count = 0;
        while (count++ < 2)
        {
            result += Array.IndexOf(values, null);
            values = new LoopSecondMatch[1];
        }
        return result + ":" + LoopFirstMatch.Calls + ":" + LoopSecondMatch.Calls;
    }

    private static string ArgumentSearch(Array values)
    {
        LoopFirstMatch.Calls = LoopSecondMatch.Calls = 0;
        int result = 0;
        for (int count = 0; count < 2; count++)
        {
            result += Array.LastIndexOf(values, null);
            values = new LoopSecondMatch[1];
        }
        return result + ":" + LoopFirstMatch.Calls + ":" + LoopSecondMatch.Calls;
    }

    private static int ConstructTypeSearch()
    {
        // MakeArrayType needs its leaf array shape in the AOT image.
        _ = new int[0];
        Type type = typeof(int);
        int count = 2;
        do { type = type.MakeArrayType(); } while (--count > 0);
        return Array.IndexOf(Array.CreateInstance(type, 1), null);
    }

    private static string ConstructAndUnwrapSearch()
    {
        Type type = typeof(LoopTypeMatch);
        int count = 8;
        do
        {
            type = Array.CreateInstance(type, 1).GetType();
            type = Array.CreateInstance(type, 1).GetType();
        } while (--count > 0);
        type = type.GetElementType()!.GetElementType()!.GetElementType()!.GetElementType()!
            .GetElementType()!.GetElementType()!.GetElementType()!.GetElementType()!
            .GetElementType()!.GetElementType()!.GetElementType()!.GetElementType()!
            .GetElementType()!.GetElementType()!.GetElementType()!.GetElementType()!;
        LoopTypeMatch.Calls = 0;
        int result = Array.IndexOf(Array.CreateInstance(type, 1), null);
        return result + ":" + LoopTypeMatch.Calls;
    }

    private static int UnsearchedLeafSearch()
    {
        _ = new LoopUnsearchedMatch[0];
        Type type = typeof(LoopUnsearchedMatch);
        int count = 2;
        do { type = type.MakeArrayType(); } while (--count > 0);
        return Array.IndexOf(Array.CreateInstance(type, 1), null);
    }

    private static Type ConstructEightLayers(Type type)
    {
        type = Array.CreateInstance(type, 0).GetType();
        type = Array.CreateInstance(type, 0).GetType();
        type = Array.CreateInstance(type, 0).GetType();
        type = Array.CreateInstance(type, 0).GetType();
        type = Array.CreateInstance(type, 0).GetType();
        type = Array.CreateInstance(type, 0).GetType();
        type = Array.CreateInstance(type, 0).GetType();
        return Array.CreateInstance(type, 0).GetType();
    }

    private static Type UnwrapEightLayers(Type type) =>
        type.GetElementType()!.GetElementType()!.GetElementType()!.GetElementType()!
            .GetElementType()!.GetElementType()!.GetElementType()!.GetElementType()!;

    private static string DeepAcyclicTypeSearch()
    {
        Type type = typeof(DeepTypeMatch);
        type = ConstructEightLayers(type);
        type = ConstructEightLayers(type);
        type = ConstructEightLayers(type);
        type = ConstructEightLayers(type);
        type = ConstructEightLayers(type);
        type = UnwrapEightLayers(type);
        type = UnwrapEightLayers(type);
        type = UnwrapEightLayers(type);
        type = UnwrapEightLayers(type);
        type = UnwrapEightLayers(type);
        DeepTypeMatch.Calls = 0;
        int result = Array.LastIndexOf(Array.CreateInstance(type, 1), null);
        return result + ":" + DeepTypeMatch.Calls + ":" + (type == typeof(DeepTypeMatch));
    }

    private static Type[] LookupSignature()
    {
        Type[] signature = new Type[1];
        signature[0] = typeof(int);
        return signature;
    }

    private static Type[] ForwardSignature(Type[] signature) => signature;

    private static string LateSignatureLookupSearch()
    {
        Type[] signature = ForwardSignature(LookupSignature());
        Type arrayType = typeof(LookupOverloads)
            .GetMethod(nameof(LookupOverloads.Values), signature)!.ReturnType;
        Array values = Array.CreateInstanceFromArrayType(arrayType, 1);
        LateLookupMatch.Calls = 0;
        return Array.IndexOf(values, null) + ":" + LateLookupMatch.Calls;
    }

    private static Array AllocateFromSlot(Type[] holder) => Array.CreateInstance(holder[0], 1);

    private static Array AllocateThroughSlot(Type element)
    {
        Type[] holder = new Type[1];
        holder[0] = element;
        return AllocateFromSlot(holder);
    }

    private static int SharedCaller(Type element) => Array.IndexOf(AllocateThroughSlot(element), null);

    private static string SharedCallsiteSearch()
    {
        SharedCallsiteFirstMatch.Calls = SharedCallsiteSecondMatch.Calls = 0;
        int first = SharedCaller(typeof(SharedCallsiteFirstMatch));
        int second = SharedCaller(typeof(SharedCallsiteSecondMatch));
        return first + ":" + second + ":" + SharedCallsiteFirstMatch.Calls
            + ":" + SharedCallsiteSecondMatch.Calls;
    }

    private static int SearchList(IList values, object needle) => values.IndexOf(needle);

    private static string InterfaceListSearch()
    {
        IList values = new ArrayList();
        values.Add(new InterfaceListMatch { Value = 7 });
        InterfaceListMatch.Calls = 0;
        return SearchList(values, new InterfaceListMatch { Value = 7 })
            + ":" + InterfaceListMatch.Calls;
    }

    private static int SearchContravariant(IArraySearcher<SearchHolder> searcher,
        SearchHolder values) => searcher.Search(values);

    private static string ContravariantInterfaceSearch()
    {
        IArraySearcher<object> searcher = new ArraySearcher();
        ContravariantSearchMatch.Calls = 0;
        return SearchContravariant(searcher, new SearchHolder(new ContravariantSearchMatch[1]))
            + ":" + ContravariantSearchMatch.Calls;
    }

    private static string SharedReadWriteSearch(bool second)
    {
        Type[] first = new Type[] { typeof(int) };
        Type[] other = new Type[] { typeof(string) };
        var holder = new TypeArrayHolder(first);
        if (second)
            holder.Values = other;
        Type[] written = holder.Values;
        Type[] observed = holder.Values;
        string initial = written[0].Name;
        written[0] = typeof(SharedReadWriteMatch);
        Array values = Array.CreateInstance(observed[0], 1);
        SharedReadWriteMatch.Calls = 0;
        return initial + ":" + Array.IndexOf(values, null) + ":" + SharedReadWriteMatch.Calls
            + ":" + ReferenceEquals(written, observed);
    }

    internal static void Run(bool includeAdditionalCases = true)
    {
        Console.WriteLine("== array search loop provenance ==");
        Console.WriteLine("loop-local=" + LocalSearch());
        Console.WriteLine("loop-argument=" + ArgumentSearch(new LoopFirstMatch[1]));
#if !ARRAY_SEARCH_LOOP_BACKWARD_ONLY
        Console.WriteLine("loop-type=" + ConstructTypeSearch());
        Console.WriteLine("loop-type-unwrapped=" + ConstructAndUnwrapSearch());
        Console.WriteLine("loop-unsearched-leaf=" + UnsearchedLeafSearch());
#endif
        Console.WriteLine("array search loop provenance end");
        if (!includeAdditionalCases)
            return;
#if !ARRAY_SEARCH_LOOP_BACKWARD_ONLY
        Console.WriteLine("== array search deep types and late signatures ==");
        Console.WriteLine("deep-acyclic-type=" + DeepAcyclicTypeSearch());
        Console.WriteLine("late-signature-lookup=" + LateSignatureLookupSearch());
        Console.WriteLine("array search deep types and late signatures end");
        Console.WriteLine("== array search shared callsite provenance ==");
        Console.WriteLine("shared-callsite=" + SharedCallsiteSearch());
        Console.WriteLine("array search shared callsite provenance end");
#endif
    }

    internal static void RunInterfaces()
    {
        // IList.IndexOf can root unrelated collection bodies in the combined bucket.
        Console.WriteLine("== array search interface provenance ==");
        Console.WriteLine("interface-arraylist=" + InterfaceListSearch());
        Console.WriteLine("contravariant-array-search=" + ContravariantInterfaceSearch());
        Console.WriteLine("array search interface provenance end");
    }

    internal static void RunSharedReadWrites()
    {
        Console.WriteLine("== array search shared read writes ==");
        Console.WriteLine("shared-read-write-first=" + SharedReadWriteSearch(false));
        Console.WriteLine("shared-read-write-second=" + SharedReadWriteSearch(true));
        Console.WriteLine("array search shared read writes end");
    }
}
