using System;
using System.Globalization;

namespace HalfBasics;

// System.Half basic surface: creation, conversion, arithmetic (all of it
// round-trips through float in the BCL), comparisons, predicates,
// formatting, parsing, ordering and the IEEE binary16 edge cases
// (subnormals, overflow-to-infinity, round-to-nearest-even at the half
// precision boundary). Bit patterns are asserted through
// BitConverter.HalfToUInt16Bits so the checks are exact, not
// formatting-dependent.
internal static class Program
{
    private struct HalfPair
    {
        public Half X;
        public Half Y;
    }

    private static string Bits(Half h)
    {
        return BitConverter.HalfToUInt16Bits(h).ToString("X4");
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("[half] creation and conversion");
        Half a = (Half)3.14f;
        Half b = (Half)2.0;
        Half i3 = (Half)3;
        Console.WriteLine((float)a);
        Console.WriteLine((double)a);
        Console.WriteLine((float)b);
        Console.WriteLine((float)i3);
        Console.WriteLine(Bits(a));
        Console.WriteLine(Bits(b));
        Console.WriteLine(Bits(i3));
        // float -> Half -> float round-trip on an exactly representable value.
        Console.WriteLine((float)(Half)1.5f);
        Console.WriteLine((double)(Half)(-0.125));
        // double -> Half takes the SoftFloat-style RoundPackToHalf path.
        Console.WriteLine(Bits((Half)3.14159265358979));
        Console.WriteLine(Bits((Half)1e-8));
        Console.WriteLine(Bits((Half)(-3.14159265358979)));

        Console.WriteLine("[half] constants");
        Console.WriteLine(Bits(Half.Pi));
        Console.WriteLine(Bits(Half.E));
        Console.WriteLine(Bits(Half.Tau));
        Console.WriteLine(Bits(Half.Epsilon));
        Console.WriteLine(Bits(Half.MinValue));
        Console.WriteLine(Bits(Half.MaxValue));
        Console.WriteLine(Bits(Half.NaN));
        Console.WriteLine(Bits(Half.PositiveInfinity));
        Console.WriteLine(Bits(Half.NegativeInfinity));
        Console.WriteLine(Bits(Half.NegativeZero));
        Console.WriteLine(Bits(Half.Zero));
        Console.WriteLine(Bits(Half.One));
        Console.WriteLine(Bits(Half.NegativeOne));
        Console.WriteLine((float)Half.MaxValue);
        Console.WriteLine((float)Half.Epsilon);

        Console.WriteLine("[half] arithmetic");
        Console.WriteLine(Bits(a + b));
        Console.WriteLine(Bits(a - b));
        Console.WriteLine(Bits(a * b));
        Console.WriteLine(Bits(a / b));
        Console.WriteLine(Bits(a % b));
        Console.WriteLine(Bits(-a));
        Console.WriteLine(Bits(+a));
        Console.WriteLine((float)(a + b));
        Console.WriteLine((float)(a % b));
        // Accumulation happens at half precision each step, not float.
        Half acc = Half.Zero;
        for (int i = 0; i < 10; i++)
        {
            acc += (Half)0.1f;
        }
        Console.WriteLine(Bits(acc));
        // Infinity and NaN propagation through the operators.
        Console.WriteLine(Bits(Half.MaxValue + Half.MaxValue));
        // inf-inf and 0/0 must be computed at run time, not from the
        // Half.PositiveInfinity/Half.Zero constants directly: a constant
        // operand lets the C++ optimizer fold the subtraction/division at
        // compile time, which follows LLVM's default-NaN convention (sign
        // bit 0) instead of the x86 hardware SUBSS/DIVSD "real indefinite"
        // result (sign bit 1) that the real .NET JIT produces at run time.
        // Routing the operands through double.Parse keeps them opaque to the
        // optimizer so both dn2cpp and real .NET hit the same hardware path.
        Half runtimeInf = (Half)double.Parse("Infinity", CultureInfo.InvariantCulture);
        Half runtimeZero = (Half)double.Parse("0", CultureInfo.InvariantCulture);
        Console.WriteLine(Bits(runtimeInf - runtimeInf));
        Console.WriteLine(Bits(runtimeZero / runtimeZero));
        Console.WriteLine(Bits(Half.One / Half.Zero));
        Console.WriteLine(Bits(Half.NegativeOne / Half.Zero));

        Console.WriteLine("[half] comparisons");
        Console.WriteLine(a < b);
        Console.WriteLine(a <= b);
        Console.WriteLine(a > b);
        Console.WriteLine(a >= b);
        Console.WriteLine(a == b);
        Console.WriteLine(a != b);
        Console.WriteLine(a == (Half)3.14f);
        Console.WriteLine(Half.NaN == Half.NaN);
        Console.WriteLine(Half.NaN != Half.NaN);
        Console.WriteLine(Half.NaN < Half.One);
        Console.WriteLine(Half.NaN >= Half.One);
        Console.WriteLine(Half.Zero == Half.NegativeZero);
        Console.WriteLine(Half.PositiveInfinity > Half.MaxValue);
        Console.WriteLine(a.CompareTo(b));
        Console.WriteLine(b.CompareTo(a));
        Console.WriteLine(a.CompareTo(a));
        Console.WriteLine(Half.NaN.CompareTo(Half.One));
        Console.WriteLine(Half.One.CompareTo(Half.NaN));
        Console.WriteLine(Half.NaN.CompareTo(Half.NaN));
        Console.WriteLine(Half.NegativeZero.CompareTo(Half.Zero));
        Console.WriteLine(a.Equals(a));
        Console.WriteLine(a.Equals(b));
        Console.WriteLine(Half.NaN.Equals(Half.NaN));
        Console.WriteLine(a.GetHashCode() == ((Half)3.14f).GetHashCode());
        Console.WriteLine(Half.Zero.GetHashCode() == Half.NegativeZero.GetHashCode());
        Console.WriteLine(Half.NaN.GetHashCode() == Half.NaN.GetHashCode());

        Console.WriteLine("[half] predicates");
        Console.WriteLine(Half.IsNaN(Half.NaN));
        Console.WriteLine(Half.IsNaN(a));
        Console.WriteLine(Half.IsInfinity(Half.PositiveInfinity));
        Console.WriteLine(Half.IsInfinity(Half.NegativeInfinity));
        Console.WriteLine(Half.IsInfinity(Half.MaxValue));
        Console.WriteLine(Half.IsPositiveInfinity(Half.PositiveInfinity));
        Console.WriteLine(Half.IsNegativeInfinity(Half.NegativeInfinity));
        Console.WriteLine(Half.IsFinite(a));
        Console.WriteLine(Half.IsFinite(Half.PositiveInfinity));
        Console.WriteLine(Half.IsFinite(Half.NaN));
        Console.WriteLine(Half.IsSubnormal(Half.Epsilon));
        Console.WriteLine(Half.IsSubnormal(a));
        Console.WriteLine(Half.IsSubnormal(Half.Zero));
        Console.WriteLine(Half.IsNegative(Half.NegativeOne));
        Console.WriteLine(Half.IsNegative(Half.NegativeZero));
        Console.WriteLine(Half.IsNegative(Half.Zero));
        Console.WriteLine(Half.IsNegative(Half.NaN));
        Console.WriteLine(Half.IsNormal(a));
        Console.WriteLine(Half.IsNormal(Half.Epsilon));

        Console.WriteLine("[half] tostring");
        Console.WriteLine(a.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine(b.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine(Half.MaxValue.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine(Half.Epsilon.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine(Half.NaN.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine(Half.PositiveInfinity.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine(Half.NegativeInfinity.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine(((Half)0.1f).ToString(CultureInfo.InvariantCulture));
        Console.WriteLine(a.ToString("F2", CultureInfo.InvariantCulture));
        Console.WriteLine(a.ToString("E3", CultureInfo.InvariantCulture));
        Console.WriteLine(Half.MaxValue.ToString("G", CultureInfo.InvariantCulture));
        Console.WriteLine(((Half)0.5f).ToString("R", CultureInfo.InvariantCulture));

        Console.WriteLine("[half] parse");
        Console.WriteLine(Bits(Half.Parse("3.14", CultureInfo.InvariantCulture)));
        Console.WriteLine(Bits(Half.Parse("-0.5", CultureInfo.InvariantCulture)));
        Console.WriteLine(Bits(Half.Parse("65504", CultureInfo.InvariantCulture)));
        Console.WriteLine(Bits(Half.Parse("1e5", CultureInfo.InvariantCulture)));
        Console.WriteLine(Bits(Half.Parse("NaN", CultureInfo.InvariantCulture)));
        Console.WriteLine(Bits(Half.Parse("Infinity", CultureInfo.InvariantCulture)));
        Half parsed;
        Console.WriteLine(Half.TryParse("2.5", CultureInfo.InvariantCulture, out parsed));
        Console.WriteLine(Bits(parsed));
        Console.WriteLine(Half.TryParse("bogus", CultureInfo.InvariantCulture, out parsed));
        Console.WriteLine(Bits(parsed));

        Console.WriteLine("[half] ordering");
        Half[] values = new Half[]
        {
            Half.One, Half.NegativeInfinity, (Half)0.5f, Half.NaN,
            Half.MaxValue, Half.NegativeZero, (Half)(-2.75f), Half.Zero,
            Half.Epsilon, Half.PositiveInfinity,
        };
        // NOTE: Array.Sort(values) is a transpiler generic intrinsic limited to
        // int/long/double/string elements — probed separately. Insertion sort
        // via CompareTo exercises the same ordering semantics (NaN first).
        for (int i = 1; i < values.Length; i++)
        {
            Half key = values[i];
            int j = i - 1;
            while (j >= 0 && values[j].CompareTo(key) > 0)
            {
                values[j + 1] = values[j];
                j--;
            }
            values[j + 1] = key;
        }
        for (int i = 0; i < values.Length; i++)
        {
            Console.WriteLine(Bits(values[i]));
        }

        Console.WriteLine("[half] boxing and struct fields");
        object boxed = a;
        Console.WriteLine(boxed is Half);
        Half unboxed = (Half)boxed;
        Console.WriteLine(Bits(unboxed));
        // NOTE: boxed.Equals(a) (Object::Equals dispatch on an object-typed
        // receiver) is a pre-existing general boxed-struct gap in dn2cpp:
        // value-type Equals(object) overrides are use-site gated and the plain
        // object-receiver dispatch site does not mark them, so the runtime
        // falls back to reference equality. Unbox first instead.
        Console.WriteLine(((Half)boxed).Equals(a));
        Console.WriteLine(((Half)boxed).Equals(b));
        Console.WriteLine(((IComparable)boxed).CompareTo(b));
        HalfPair pair;
        pair.X = (Half)1.25f;
        pair.Y = (Half)(-7f);
        Console.WriteLine(Bits(pair.X));
        Console.WriteLine(Bits(pair.Y));
        Console.WriteLine((float)(pair.X + pair.Y));

        Console.WriteLine("[half] binary16 edges");
        // Overflow to infinity: 70000 > 65504 (Half.MaxValue).
        Console.WriteLine(Bits((Half)70000f));
        Console.WriteLine(Bits((Half)(-70000f)));
        // 65520 is the rounding boundary: rounds to infinity.
        Console.WriteLine(Bits((Half)65520f));
        Console.WriteLine(Bits((Half)65519.9));
        // Largest finite: 65504.
        Console.WriteLine(Bits((Half)65504f));
        // Round-to-nearest-even at ulp=2 (values in [2048, 4096)).
        Console.WriteLine(Bits((Half)2048f));
        Console.WriteLine(Bits((Half)2049f));
        Console.WriteLine(Bits((Half)2050f));
        Console.WriteLine(Bits((Half)2051f));
        Console.WriteLine(Bits((Half)2052f));
        Console.WriteLine((float)(Half)2049f);
        Console.WriteLine((float)(Half)2051f);
        // Subnormals: smallest normal is 2^-14 = 6.103515625e-05.
        Half smallestNormal = BitConverter.UInt16BitsToHalf(0x0400);
        Console.WriteLine(Half.IsSubnormal(smallestNormal));
        Console.WriteLine((float)smallestNormal);
        Half subnormal = (Half)5e-5f;
        Console.WriteLine(Half.IsSubnormal(subnormal));
        Console.WriteLine(Bits(subnormal));
        Console.WriteLine((float)subnormal);
        // Subnormal arithmetic (runs at float precision, repacks to half).
        Console.WriteLine(Bits(Half.Epsilon + Half.Epsilon));
        Console.WriteLine(Bits((Half)((float)Half.Epsilon * 0.5f)));
        // Underflow to zero.
        Console.WriteLine(Bits((Half)1e-9f));
        Console.WriteLine(Bits((Half)(-1e-9f)));
        // NaN payload quieting through float round-trips.
        Console.WriteLine(Bits((Half)(float)Half.NaN));
        Console.WriteLine(Half.IsNaN((Half)float.NaN));
    }
}
