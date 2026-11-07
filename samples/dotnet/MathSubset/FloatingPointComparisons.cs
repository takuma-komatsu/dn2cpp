using System;
using System.Runtime.CompilerServices;

namespace FloatingPointComparisons;

internal static class Program
{
    private static string Bit(bool value) => value ? "1" : "0";

    // Bit order: <, <=, >, >=, ==, !=.
    private static string Bits(bool lt, bool le, bool gt, bool ge, bool eq, bool ne) =>
        Bit(lt) + Bit(le) + Bit(gt) + Bit(ge) + Bit(eq) + Bit(ne);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Less(double a, double b) => a < b;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LessEqual(double a, double b) => a <= b;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Greater(double a, double b) => a > b;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GreaterEqual(double a, double b) => a >= b;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Equal(double a, double b) => a == b;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool NotEqual(double a, double b) => a != b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Less(float a, float b) => a < b;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool LessEqual(float a, float b) => a <= b;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Greater(float a, float b) => a > b;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool GreaterEqual(float a, float b) => a >= b;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Equal(float a, float b) => a == b;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool NotEqual(float a, float b) => a != b;

    // Non-boolean return arms keep Roslyn's relational branch instructions.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchLess(double a, double b)
    {
        if (a < b) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchLessEqual(double a, double b)
    {
        if (a <= b) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchGreater(double a, double b)
    {
        if (a > b) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchGreaterEqual(double a, double b)
    {
        if (a >= b) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchEqual(double a, double b)
    {
        if (a == b) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchNotEqual(double a, double b)
    {
        if (a != b) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchNotLess(double a, double b)
    {
        if (!(a < b)) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchNotLessEqual(double a, double b)
    {
        if (!(a <= b)) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchNotGreater(double a, double b)
    {
        if (!(a > b)) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchNotGreaterEqual(double a, double b)
    {
        if (!(a >= b)) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchLess(float a, float b)
    {
        if (a < b) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchLessEqual(float a, float b)
    {
        if (a <= b) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchGreater(float a, float b)
    {
        if (a > b) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchGreaterEqual(float a, float b)
    {
        if (a >= b) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchEqual(float a, float b)
    {
        if (a == b) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchNotEqual(float a, float b)
    {
        if (a != b) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchNotLess(float a, float b)
    {
        if (!(a < b)) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchNotLessEqual(float a, float b)
    {
        if (!(a <= b)) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchNotGreater(float a, float b)
    {
        if (!(a > b)) return 17;
        return 29;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int BranchNotGreaterEqual(float a, float b)
    {
        if (!(a >= b)) return 17;
        return 29;
    }

    private static void Check(string label, double a, double b)
    {
        Console.WriteLine("double " + label
            + " compare=" + Bits(Less(a, b), LessEqual(a, b), Greater(a, b), GreaterEqual(a, b), Equal(a, b), NotEqual(a, b))
            + " branch=" + Bits(BranchLess(a, b) == 17, BranchLessEqual(a, b) == 17,
                BranchGreater(a, b) == 17, BranchGreaterEqual(a, b) == 17,
                BranchEqual(a, b) == 17, BranchNotEqual(a, b) == 17)
            + " negated=" + Bit(BranchNotLess(a, b) == 17) + Bit(BranchNotLessEqual(a, b) == 17)
                + Bit(BranchNotGreater(a, b) == 17) + Bit(BranchNotGreaterEqual(a, b) == 17));
    }

    private static void Check(string label, float a, float b)
    {
        Console.WriteLine("single " + label
            + " compare=" + Bits(Less(a, b), LessEqual(a, b), Greater(a, b), GreaterEqual(a, b), Equal(a, b), NotEqual(a, b))
            + " branch=" + Bits(BranchLess(a, b) == 17, BranchLessEqual(a, b) == 17,
                BranchGreater(a, b) == 17, BranchGreaterEqual(a, b) == 17,
                BranchEqual(a, b) == 17, BranchNotEqual(a, b) == 17)
            + " negated=" + Bit(BranchNotLess(a, b) == 17) + Bit(BranchNotLessEqual(a, b) == 17)
                + Bit(BranchNotGreater(a, b) == 17) + Bit(BranchNotGreaterEqual(a, b) == 17));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Compare(int a, int b) => Bits(a < b, a <= b, a > b, a >= b, a == b, a != b);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Compare(uint a, uint b) => Bits(a < b, a <= b, a > b, a >= b, a == b, a != b);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Compare(long a, long b) => Bits(a < b, a <= b, a > b, a >= b, a == b, a != b);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Compare(ulong a, ulong b) => Bits(a < b, a <= b, a > b, a >= b, a == b, a != b);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Compare(nint a, nint b) => Bits(a < b, a <= b, a > b, a >= b, a == b, a != b);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Compare(nuint a, nuint b) => Bits(a < b, a <= b, a > b, a >= b, a == b, a != b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Compare(object a, object b) => Bit(a == b) + Bit(a != b) + Bit(a != null) + Bit(b != null);

    internal static void __GateEntry()
    {
        Console.WriteLine("== floating-point comparisons ==");
        Check("nan-left", double.NaN, 0.5);
        Check("nan-right", 0.5, double.NaN);
        Check("nan-both", double.NaN, double.NaN);
        Check("fraction-less", 0.25, 0.75);
        Check("fraction-greater", 0.75, 0.25);
        Check("fraction-equal", 0.5, 0.5);
        Check("negative-fractions", -0.75, -0.25);
        Check("negative-infinity", double.NegativeInfinity, -0.5);
        Check("positive-infinity", double.PositiveInfinity, 0.5);
        Check("infinities-less", double.NegativeInfinity, double.PositiveInfinity);
        Check("infinities-greater", double.PositiveInfinity, double.NegativeInfinity);
        Check("infinities-equal", double.PositiveInfinity, double.PositiveInfinity);
        Check("negative-infinities-equal", double.NegativeInfinity, double.NegativeInfinity);
        Check("negative-zero-left", -0.0, 0.0);
        Check("negative-zero-right", 0.0, -0.0);

        Check("nan-left", float.NaN, 0.5f);
        Check("nan-right", 0.5f, float.NaN);
        Check("nan-both", float.NaN, float.NaN);
        Check("fraction-less", 0.25f, 0.75f);
        Check("fraction-greater", 0.75f, 0.25f);
        Check("fraction-equal", 0.5f, 0.5f);
        Check("negative-fractions", -0.75f, -0.25f);
        Check("negative-infinity", float.NegativeInfinity, -0.5f);
        Check("positive-infinity", float.PositiveInfinity, 0.5f);
        Check("infinities-less", float.NegativeInfinity, float.PositiveInfinity);
        Check("infinities-greater", float.PositiveInfinity, float.NegativeInfinity);
        Check("infinities-equal", float.PositiveInfinity, float.PositiveInfinity);
        Check("negative-infinities-equal", float.NegativeInfinity, float.NegativeInfinity);
        Check("negative-zero-left", -0.0f, 0.0f);
        Check("negative-zero-right", 0.0f, -0.0f);

        Console.WriteLine("comparison i4=" + Compare(-1, 1));
        Console.WriteLine("comparison u4=" + Compare(uint.MaxValue, 1u));
        Console.WriteLine("comparison i8=" + Compare(long.MinValue, 1L));
        Console.WriteLine("comparison u8=" + Compare(ulong.MaxValue, 1UL));
        Console.WriteLine("comparison native=" + Compare(nint.MinValue, (nint)1));
        Console.WriteLine("comparison unative=" + Compare(nuint.MaxValue, (nuint)1));
        object instance = new object();
        Console.WriteLine("comparison same-reference=" + Compare(instance, instance));
        Console.WriteLine("comparison distinct-references=" + Compare(instance, new object()));
        Console.WriteLine("comparison null-left=" + Compare(null, instance));
        Console.WriteLine("comparison null-right=" + Compare(instance, null));
        Console.WriteLine("comparison null-both=" + Compare((object)null, null));
        Console.WriteLine("floating-point comparisons end");
    }
}
