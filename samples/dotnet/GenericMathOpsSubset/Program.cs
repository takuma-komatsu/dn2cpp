using System;
using System.Globalization;
using System.Numerics;

// A user-defined static-abstract interface + value-type impl, folded through a
// `constrained. call` to the static-virtual member (the same shape real System.Linq's
// IMinMaxCalc<T> Min/Max comparers take). Validates end-to-end — compile AND link AND
// run — that a static-virtual interface call devirtualizes to the struct's impl.
internal interface IBinOp<T>
{
    static abstract T Apply(T a, T b);
}

internal readonly struct AddOp : IBinOp<int>
{
    public static int Apply(int a, int b) => a + b;
}

internal readonly struct MaxOp : IBinOp<int>
{
    public static int Apply(int a, int b) => a > b ? a : b;
}

// Generic-math static-abstract interface members lowered to scalar primitive ops:
// the constants (T.Zero / T.One), the arithmetic operators (op_Addition /
// op_Subtraction / op_Multiply / op_Division / op_UnaryNegation), the comparison
// operators (op_LessThan / op_GreaterThan) and the interface-routed conversion
// (T.CreateChecked -> INumberBase<T>.TryConvertFromChecked). These are exactly the
// shapes the real System.Linq Sum / Average / Min / Max / Range reach once T is
// closed to a primitive (IL2CPP lowers them the same way). The app drives the
// patterns through generic methods so each call site monomorphizes to a concrete
// primitive; output diffs exact vs real .NET. CoreLib only (no Linq shim).
internal static class Program
{
    // T.Zero seed + op_Addition accumulate — the Enumerable.Sum shape.
    static T SumAll<T>(T[] xs) where T : INumberBase<T>
    {
        T s = T.Zero;
        foreach (var x in xs)
            s += x;
        return s;
    }

    // T.One seed + op_Multiply accumulate.
    static T Product<T>(T[] xs) where T : INumberBase<T>
    {
        T p = T.One;
        foreach (var x in xs)
            p *= x;
        return p;
    }

    static T Diff<T>(T a, T b) where T : INumberBase<T> => a - b;       // op_Subtraction
    static T Negate<T>(T a) where T : INumberBase<T> => -a;            // op_UnaryNegation

    // op_LessThan / op_GreaterThan — the Enumerable.Min / Max shape.
    static T Min<T>(T[] xs) where T : INumber<T>
    {
        T m = xs[0];
        foreach (var x in xs)
            if (x < m) m = x;
        return m;
    }

    static T Max<T>(T[] xs) where T : INumber<T>
    {
        T m = xs[0];
        foreach (var x in xs)
            if (x > m) m = x;
        return m;
    }

    // op_Division + interface-routed conversion — the Enumerable.Average shape.
    static double Average<T>(T[] xs) where T : INumberBase<T>
    {
        T s = T.Zero;
        foreach (var x in xs)
            s += x;
        return WidenToDouble(s) / xs.Length;
    }

    // T.CreateChecked through the INumberBase<T> default impl reaches the static
    // abstract TryConvertFromChecked leaf — the conversion gap real Average hit.
    static double WidenToDouble<T>(T v) where T : INumberBase<T> => double.CreateChecked(v);
    static R WidenFrom<R, U>(U v) where R : INumberBase<R> where U : INumberBase<U> => R.CreateChecked(v);

    // Fold via a static-virtual interface member: `TOp.Apply` lowers to a
    // `constrained. !!TOp; call IBinOp<T>::Apply` the transpiler must devirtualize to
    // the value-type TOp's static impl (the IMinMaxCalc resolution path).
    static T Fold<T, TOp>(T[] xs) where TOp : IBinOp<T>
    {
        T acc = xs[0];
        for (int i = 1; i < xs.Length; i++)
            acc = TOp.Apply(acc, xs[i]);
        return acc;
    }

    static int Main()
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        int[] ints = { 3, 1, 4, 1, 5, 9, 2, 6 };
        long[] longs = { 10L, -20L, 30L, -40L };
        double[] dbls = { 1.5, 2.25, -0.75, 4.0 };
        float[] flts = { 2.5f, -1.5f, 3.0f };

        Console.WriteLine("== Sum (T.Zero + op_Addition) ==");
        Console.WriteLine($"int    {SumAll(ints)}");
        Console.WriteLine($"long   {SumAll(longs)}");
        Console.WriteLine($"double {SumAll(dbls)}");
        Console.WriteLine($"float  {SumAll(flts)}");

        Console.WriteLine("== Product (T.One + op_Multiply) ==");
        Console.WriteLine($"int    {Product(ints)}");
        Console.WriteLine($"double {Product(dbls)}");

        Console.WriteLine("== Diff / Negate (op_Subtraction / op_UnaryNegation) ==");
        Console.WriteLine($"int    {Diff(10, 3)} {Negate(7)}");
        Console.WriteLine($"long   {Diff(5L, 9L)} {Negate(-4L)}");
        Console.WriteLine($"double {Diff(1.5, 0.25)} {Negate(2.5)}");

        Console.WriteLine("== Min / Max (op_LessThan / op_GreaterThan) ==");
        Console.WriteLine($"int    {Min(ints)} {Max(ints)}");
        Console.WriteLine($"long   {Min(longs)} {Max(longs)}");
        Console.WriteLine($"double {Min(dbls)} {Max(dbls)}");

        Console.WriteLine("== Average (op_Division + interface CreateChecked) ==");
        Console.WriteLine($"int    {Average(ints)}");
        Console.WriteLine($"long   {Average(longs)}");
        Console.WriteLine($"double {Average(dbls)}");

        Console.WriteLine("== CreateChecked via INumberBase (TryConvertFromChecked) ==");
        Console.WriteLine($"long<-int(123)   {WidenFrom<long, int>(123)}");
        Console.WriteLine($"double<-int(7)    {WidenFrom<double, int>(7)}");
        Console.WriteLine($"double<-long(50)  {WidenFrom<double, long>(50L)}");
        Console.WriteLine($"int<-long(99)     {WidenFrom<int, long>(99L)}");

        Console.WriteLine("== Fold via static-virtual interface (constrained call) ==");
        Console.WriteLine($"AddOp  {Fold<int, AddOp>(ints)}");
        Console.WriteLine($"MaxOp  {Fold<int, MaxOp>(ints)}");

        GenericMathChecked.CheckedGenericMath.__GateEntry();
        GenericMathStaticVirtuals.StaticVirtualDefaults.__GateEntry();
        GenericMathFloatingPoint.FloatingPointMath.__GateEntry();
        GenericMathShiftModulus.ShiftModulusOps.__GateEntry();
        GenericMathClassOperators.ClassOperators.__GateEntry();
        GenericMathConformance.ConformanceSweep.__GateEntry();
        GenericMathDecimal.DecimalGenericMath.__GateEntry();
        GenericMathBinaryBits.BinaryNumberBits.__GateEntry();
        GenericMathResolverAdmission.ResolverAdmission.__GateEntry();
        GenericMathConvert.CreateConversions.__GateEntry();
        GenericMathInt128Convert.Int128Conversions.__GateEntry();
        GenericMathInt128Sign.Int128SignedCompare.__GateEntry();
        GenericMathInt128Parse.Int128ParseConstrained.__GateEntry();
        GenericMathEnumName.EnumUnderlyingName.__GateEntry();
        GenericMathReadBE.ReadBigEndianOps.__GateEntry();
        GenericMathOutWidth.OutSlotWidth.__GateEntry();
        GenericMathStaticAbstractGvm.StaticAbstractGenericMethod.__GateEntry();
        GenericMathInterfaceStaticImpl.InterfaceStaticImpl.__GateEntry();

        return 0;
    }
}
