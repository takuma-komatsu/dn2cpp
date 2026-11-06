using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace ArraySortFactoryWitness;

internal static class Program
{
    private const BindingFlags HiddenStatic = BindingFlags.Static | BindingFlags.NonPublic;

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== ArraySortHelper genuine factories ==");
        object integer = FactoryInt();
        Shape(integer, true, typeof(int));
        int[] ints = { 5, -1, 8, 2, 2, 0 };
        SortInt(integer, ints);
        Check(ints[0] == -1 && ints[1] == 0 && ints[2] == 2 && ints[3] == 2 && ints[4] == 5 && ints[5] == 8, "int order");
        Pass("unary-int");

        object value = FactoryValue();
        Shape(value, true, typeof(RankedValue));
        RankedValue.Comparisons = 0;
        RankedValue[] values = { new(7), new(-2), new(4), new(0) };
        SortValue(value, values);
        Check(values[0].Rank == -2 && values[1].Rank == 0 && values[2].Rank == 4 && values[3].Rank == 7
            && RankedValue.Comparisons > 0, "value order");
        Pass("unary-novel-value");

        object reference = FactoryReference();
        Shape(reference, true, typeof(RankedReference));
        RankedReference.Comparisons = 0;
        RankedReference[] references = { new(6), new(3), new(-5) };
        SortReference(reference, references);
        Check(references[0].Rank == -5 && references[1].Rank == 3 && references[2].Rank == 6
            && RankedReference.Comparisons > 0, "reference order");
        Pass("unary-novel-reference");

        object variant = FactoryContravariant();
        Shape(variant, true, typeof(DerivedRank));
        BaseRank.Comparisons = 0;
        DerivedRank[] derived = { new(9), new(1), new(5) };
        SortContravariant(variant, derived);
        Check(derived[0].Rank == 1 && derived[1].Rank == 5 && derived[2].Rank == 9
            && BaseRank.Comparisons > 0, "contravariant order");
        Pass("unary-contravariant-reference");

        object fallback = FactoryOpaque();
        Shape(fallback, false, typeof(Opaque));
        OpaqueComparer.Comparisons = 0;
        Opaque[] opaque = { new(8), new(-4), new(3) };
        SortOpaque(fallback, opaque, new OpaqueComparer());
        Check(opaque[0].Rank == -4 && opaque[1].Rank == 3 && opaque[2].Rank == 8
            && OpaqueComparer.Comparisons > 0, "fallback order");
        Pass("unary-noncomparable-fallback");

        object pair = FactoryPair();
        Shape(pair, true, typeof(RankedValue), typeof(Opaque));
        RankedValue[] keys = { new(5), new(-1), new(2) };
        Opaque[] payloads = { new(50), new(-10), new(20) };
        SortPair(pair, keys, payloads);
        Check(keys[0].Rank == -1 && keys[1].Rank == 2 && keys[2].Rank == 5
            && payloads[0].Rank == -10 && payloads[1].Rank == 20 && payloads[2].Rank == 50, "pair permutation");
        Pass("pair-unconstrained-value");

        object pairFallback = FactoryOpaquePair();
        Shape(pairFallback, false, typeof(Opaque), typeof(int));
        Opaque[] opaqueKeys = { new(4), new(-3), new(6) };
        int[] opaqueValues = { 40, -30, 60 };
        SortOpaquePair(pairFallback, opaqueKeys, opaqueValues, new OpaqueComparer());
        Check(opaqueKeys[0].Rank == -3 && opaqueKeys[1].Rank == 4 && opaqueKeys[2].Rank == 6
            && opaqueValues[0] == -30 && opaqueValues[1] == 40 && opaqueValues[2] == 60, "fallback pair permutation");
        Pass("pair-noncomparable-key-fallback");

        object next = FactoryInt();
        object cached = CachedInt();
        Shape(cached, true, typeof(int));
        Check(!ReferenceEquals(integer, next) && !ReferenceEquals(integer, cached)
            && ReferenceEquals(cached, CachedInt()), "fresh and cached identities");
        Pass("factory-fresh-default-cached");
        object addressFactory = FactoryAddressInt();
        Shape(addressFactory, true, typeof(int));
        Check(!ReferenceEquals(integer, addressFactory), "factory address identity");
        Pass("factory-address");

        object directOne = RuntimeOneDirect(integer.GetType(), typeof(int));
        object addressOne = RuntimeOneCalli(integer.GetType(), typeof(int));
        object reflectedOne = RuntimeOneReflection(integer.GetType(), typeof(int));
        Shape(directOne, true, typeof(int)); Shape(addressOne, true, typeof(int)); Shape(reflectedOne, true, typeof(int));
        Check(!ReferenceEquals(directOne, addressOne) && !ReferenceEquals(addressOne, reflectedOne), "one factory allocation");
        Pass("runtime-wrapper-one-direct-address-reflection");
        object directTwo = RuntimeTwoDirect(pair.GetType(), typeof(RankedValue), typeof(Opaque));
        object addressTwo = RuntimeTwoCalli(pair.GetType(), typeof(RankedValue), typeof(Opaque));
        object reflectedTwo = RuntimeTwoReflection(pair.GetType(), typeof(RankedValue), typeof(Opaque));
        Shape(directTwo, true, typeof(RankedValue), typeof(Opaque));
        Shape(addressTwo, true, typeof(RankedValue), typeof(Opaque));
        Shape(reflectedTwo, true, typeof(RankedValue), typeof(Opaque));
        Check(!ReferenceEquals(directTwo, addressTwo) && !ReferenceEquals(addressTwo, reflectedTwo), "two factory allocation");
        Pass("runtime-wrapper-two-direct-address-reflection");
        Console.WriteLine("ArraySortHelper genuine factories end");
    }

    private static void Check(bool value, string label)
    {
        if (!value) throw new InvalidOperationException(label);
    }

    private static void Pass(string label) => Console.WriteLine("array-sort-factory:" + label + ":pass");

    private static void Shape(object helper, bool optimized, Type key, Type? value = null)
    {
        Type actual = helper.GetType();
        string family = optimized ? "GenericArraySortHelper`" : "ArraySortHelper`";
        Check(actual.IsGenericType && actual.GetGenericTypeDefinition().Name == family + (value is null ? "1" : "2"), "helper family");
        Type[] arguments = actual.GetGenericArguments();
        Check(arguments.Length == (value is null ? 1 : 2) && arguments[0] == key
            && (value is null || arguments[1] == value), "actual generic arguments");
    }

    private static object FactoryInt() => FactoryOracle(typeof(int));
    private static object FactoryValue() => FactoryOracle(typeof(RankedValue));
    private static object FactoryReference() => FactoryOracle(typeof(RankedReference));
    private static object FactoryContravariant() => FactoryOracle(typeof(DerivedRank));
    private static object FactoryOpaque() => FactoryOracle(typeof(Opaque));
    private static object FactoryPair() => FactoryOracle(typeof(RankedValue), typeof(Opaque));
    private static object FactoryOpaquePair() => FactoryOracle(typeof(Opaque), typeof(int));
    private static object FactoryAddressInt() => FactoryOracle(typeof(int));
    private static object CachedInt() => HelperTypeOracle(typeof(int)).GetProperty("Default", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
    private static void SortInt(object helper, int[] values) => SortOracle(helper, values, null);
    private static void SortValue(object helper, RankedValue[] values) => SortOracle(helper, values, null);
    private static void SortReference(object helper, RankedReference[] values) => SortOracle(helper, values, null);
    private static void SortContravariant(object helper, DerivedRank[] values) => SortOracle(helper, values, null);
    private static void SortOpaque(object helper, Opaque[] values, IComparer<Opaque> comparer) => SortOracle(helper, values, comparer);
    private static void SortPair(object helper, RankedValue[] keys, Opaque[] values) => SortPairOracle(helper, keys, values, null);
    private static void SortOpaquePair(object helper, Opaque[] keys, int[] values, IComparer<Opaque> comparer) => SortPairOracle(helper, keys, values, comparer);
    private static object RuntimeOneDirect(Type representative, Type argument) => RuntimeOneReflection(representative, argument);
    private static object RuntimeOneCalli(Type representative, Type argument) => RuntimeOneReflection(representative, argument);
    private static object RuntimeTwoDirect(Type representative, Type first, Type second) => RuntimeTwoReflection(representative, first, second);
    private static object RuntimeTwoCalli(Type representative, Type first, Type second) => RuntimeTwoReflection(representative, first, second);

    private static object RuntimeOneReflection(Type representative, Type argument)
    {
        Type runtimeType = typeof(object).Assembly.GetType("System.RuntimeType")!;
        return typeof(RuntimeTypeHandle).GetMethod("CreateInstanceForAnotherGenericParameter", HiddenStatic,
            null, new[] { runtimeType, runtimeType }, null)!.Invoke(null, new object[] { representative, argument })!;
    }

    private static object RuntimeTwoReflection(Type representative, Type first, Type second)
    {
        Type runtimeType = typeof(object).Assembly.GetType("System.RuntimeType")!;
        return typeof(RuntimeTypeHandle).GetMethod("CreateInstanceForAnotherGenericParameter", HiddenStatic,
            null, new[] { runtimeType, runtimeType, runtimeType }, null)!.Invoke(null, new object[] { representative, first, second })!;
    }

    private static Type HelperTypeOracle(Type key, Type? value = null) => typeof(object).Assembly
        .GetType("System.Collections.Generic.ArraySortHelper`" + (value is null ? "1" : "2"))!
        .MakeGenericType(value is null ? new[] { key } : new[] { key, value });
    private static object FactoryOracle(Type key, Type? value = null) => HelperTypeOracle(key, value)
        .GetMethod("CreateArraySortHelper", HiddenStatic)!.Invoke(null, null)!;

    private static void SortOracle<T>(object helper, T[] values, IComparer<T>? comparer)
    {
        Type contract = typeof(object).Assembly.GetType("System.Collections.Generic.IArraySortHelper`1")!.MakeGenericType(typeof(T));
        var method = new DynamicMethod("ActualHelperSort", typeof(void), new[] { typeof(object), typeof(T[]), typeof(IComparer<T>) }, typeof(Program).Module, true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Castclass, contract);
        il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Newobj, typeof(Span<T>).GetConstructor(new[] { typeof(T[]) })!);
        il.Emit(OpCodes.Ldarg_2); il.Emit(OpCodes.Callvirt, contract.GetMethod("Sort")!); il.Emit(OpCodes.Ret);
        ((Action<object, T[], IComparer<T>?>)method.CreateDelegate(typeof(Action<object, T[], IComparer<T>?>)))(helper, values, comparer);
    }

    private static void SortPairOracle<TKey, TValue>(object helper, TKey[] keys, TValue[] values, IComparer<TKey>? comparer)
    {
        Type contract = typeof(object).Assembly.GetType("System.Collections.Generic.IArraySortHelper`2")!.MakeGenericType(typeof(TKey), typeof(TValue));
        var method = new DynamicMethod("ActualHelperPairSort", typeof(void), new[] { typeof(object), typeof(TKey[]), typeof(TValue[]), typeof(IComparer<TKey>) }, typeof(Program).Module, true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Castclass, contract);
        il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Newobj, typeof(Span<TKey>).GetConstructor(new[] { typeof(TKey[]) })!);
        il.Emit(OpCodes.Ldarg_2); il.Emit(OpCodes.Newobj, typeof(Span<TValue>).GetConstructor(new[] { typeof(TValue[]) })!);
        il.Emit(OpCodes.Ldarg_3); il.Emit(OpCodes.Callvirt, contract.GetMethod("Sort")!); il.Emit(OpCodes.Ret);
        ((Action<object, TKey[], TValue[], IComparer<TKey>?>)method.CreateDelegate(typeof(Action<object, TKey[], TValue[], IComparer<TKey>?>)))(helper, keys, values, comparer);
    }

    private static void UnusedDietSentinel() { }
}

internal readonly struct RankedValue : IComparable<RankedValue>
{
    public static int Comparisons;
    public readonly int Rank;
    public RankedValue(int rank) { Rank = rank; }
    public int CompareTo(RankedValue other) { Comparisons++; return Rank.CompareTo(other.Rank); }
}

internal sealed class RankedReference : IComparable<RankedReference>
{
    public static int Comparisons;
    public readonly int Rank;
    public RankedReference(int rank) { Rank = rank; }
    public int CompareTo(RankedReference? other) { Comparisons++; return other is null ? 1 : Rank.CompareTo(other.Rank); }
}

internal class BaseRank : IComparable<BaseRank>
{
    public static int Comparisons;
    public readonly int Rank;
    public BaseRank(int rank) { Rank = rank; }
    public int CompareTo(BaseRank? other) { Comparisons++; return other is null ? 1 : Rank.CompareTo(other.Rank); }
}

internal sealed class DerivedRank : BaseRank
{
    public DerivedRank(int rank) : base(rank) { }
}

internal sealed class Opaque
{
    public readonly int Rank;
    public Opaque(int rank) { Rank = rank; }
}

internal sealed class OpaqueComparer : IComparer<Opaque>
{
    public static int Comparisons;
    public int Compare(Opaque? first, Opaque? second)
    {
        Comparisons++;
        return first is null ? second is null ? 0 : -1 : second is null ? 1 : first.Rank.CompareTo(second.Rank);
    }
}
