using System;
using System.Numerics;

// The System.Half static generic-math surface: the whole INumber /
// IFloatingPointIeee754 / ITrigonometricFunctions / IExponentialFunctions /
// ILogarithmicFunctions / IRootFunctions / IHyperbolicFunctions /
// IPowerFunctions static member set. In the BCL almost every member is the
// managed composition (Half)MathF.Xxx((float)x) — one float-precision libm
// call rounded to 11 mantissa bits — so results round-trip bit-exactly
// through the same route on both sides and are asserted through
// BitConverter.HalfToUInt16Bits like HalfBasics. The estimate members
// (ReciprocalEstimate / ReciprocalSqrtEstimate / MultiplyAddEstimate) are
// platform-defined in .NET, so only their pinned IEEE special cases and
// tolerance relations are asserted; the *Native min/max/clamp variants
// likewise only assert unambiguous finite results. Constrained-generic
// dispatch (T.Abs / T.Sin / ... with T = Half through the static-abstract
// interfaces) exercises the static-virtual explicit-interface resolution on
// a struct with a full explicit-impl surface.
namespace HalfStatics;

internal static class Program
{
    private static string Bits(Half h)
    {
        return BitConverter.HalfToUInt16Bits(h).ToString("X4");
    }

    // Constrained-generic helpers: each dispatches through a different
    // static-abstract interface, instantiated below with T = Half only.
    private static T GAbs<T>(T x) where T : INumber<T>
    {
        return T.Abs(x);
    }

    private static T GClamp<T>(T v, T min, T max) where T : INumber<T>
    {
        return T.Clamp(v, min, max);
    }

    private static T GSin<T>(T x) where T : ITrigonometricFunctions<T>
    {
        return T.Sin(x);
    }

    private static T GSqrt<T>(T x) where T : IRootFunctions<T>
    {
        return T.Sqrt(x);
    }

    private static T GExp<T>(T x) where T : IExponentialFunctions<T>
    {
        return T.Exp(x);
    }

    private static T GLog2<T>(T x) where T : IBinaryNumber<T>
    {
        return T.Log2(x);
    }

    private static T GFma<T>(T a, T b, T c) where T : IFloatingPointIeee754<T>
    {
        return T.FusedMultiplyAdd(a, b, c);
    }

    private static bool GIsNaN<T>(T x) where T : INumberBase<T>
    {
        return T.IsNaN(x);
    }

    private static T GMaxNumber<T>(T a, T b) where T : INumber<T>
    {
        return T.MaxNumber(a, b);
    }

    internal static void __GateEntry()
    {
        Half h3 = (Half)3.0f;
        Half hPi = (Half)3.140625f;      // exactly representable near pi
        Half hHalf = (Half)0.5f;
        Half nnan = Half.CopySign(Half.NaN, Half.NegativeOne);  // sign-pinned NaN

        Console.WriteLine("[halfstatics] abs sign copysign");
        Console.WriteLine(Bits(Half.Abs((Half)(-3.25f))));
        Console.WriteLine(Bits(Half.Abs(Half.NegativeZero)));
        Console.WriteLine(Bits(Half.Abs(Half.NegativeInfinity)));
        Console.WriteLine(Half.Sign((Half)(-5.5f)));
        Console.WriteLine(Half.Sign(Half.Zero));
        Console.WriteLine(Half.Sign(Half.NegativeZero));
        Console.WriteLine(Half.Sign(Half.PositiveInfinity));
        try
        {
            Console.WriteLine(Half.Sign(Half.NaN));
        }
        catch (ArithmeticException)
        {
            Console.WriteLine("Sign(NaN) throws");
        }
        Console.WriteLine(Bits(Half.CopySign(h3, Half.NegativeOne)));
        Console.WriteLine(Bits(Half.CopySign((Half)(-2.5f), Half.One)));
        Console.WriteLine(Bits(Half.CopySign(Half.Zero, Half.NegativeOne)));

        Console.WriteLine("[halfstatics] min max clamp");
        // IEEE 754:2019 minimum/maximum: NaN propagates, -0 < +0.
        Console.WriteLine(Bits(Half.Min((Half)2.0f, h3)));
        Console.WriteLine(Bits(Half.Max((Half)2.0f, h3)));
        Console.WriteLine(Half.IsNaN(Half.Min(Half.NaN, Half.One)));
        Console.WriteLine(Half.IsNaN(Half.Max(Half.One, Half.NaN)));
        Console.WriteLine(Bits(Half.Min(Half.NegativeZero, Half.Zero)));
        Console.WriteLine(Bits(Half.Max(Half.Zero, Half.NegativeZero)));
        // The *Number variants DROP a NaN operand.
        Console.WriteLine(Bits(Half.MaxNumber(Half.NaN, Half.One)));
        Console.WriteLine(Bits(Half.MinNumber((Half)2.5f, Half.NaN)));
        Console.WriteLine(Half.IsNaN(Half.MaxNumber(Half.NaN, Half.NaN)));
        Console.WriteLine(Bits(Half.MaxNumber(Half.NegativeZero, Half.Zero)));
        Console.WriteLine(Bits(Half.MinNumber(Half.NegativeZero, Half.Zero)));
        // Magnitude variants.
        Console.WriteLine(Bits(Half.MaxMagnitude((Half)2.0f, (Half)(-3.0f))));
        Console.WriteLine(Bits(Half.MinMagnitude((Half)(-4.0f), h3)));
        Console.WriteLine(Half.IsNaN(Half.MaxMagnitude(Half.NaN, Half.One)));
        Console.WriteLine(Bits(Half.MaxMagnitudeNumber(Half.NaN, Half.One)));
        Console.WriteLine(Bits(Half.MaxMagnitudeNumber((Half)2.0f, (Half)(-3.0f))));
        Console.WriteLine(Bits(Half.MinMagnitudeNumber((Half)(-2.0f), (Half)2.0f)));
        Console.WriteLine(Bits(Half.MinMagnitudeNumber(Half.One, Half.NaN)));
        Console.WriteLine(Bits(Half.Clamp((Half)5.0f, Half.Zero, (Half)2.0f)));
        Console.WriteLine(Bits(Half.Clamp((Half)(-1.0f), Half.Zero, (Half)2.0f)));
        Console.WriteLine(Bits(Half.Clamp(Half.One, Half.Zero, (Half)2.0f)));
        try
        {
            Console.WriteLine(Bits(Half.Clamp(Half.One, h3, (Half)2.0f)));
        }
        catch (ArgumentException)
        {
            Console.WriteLine("Clamp min>max throws");
        }
        // *Native variants: NaN and ±0 are platform-defined in .NET, so only
        // the unambiguous finite results are asserted.
        Console.WriteLine(Bits(Half.MaxNative((Half)2.0f, h3)));
        Console.WriteLine(Bits(Half.MinNative((Half)(-5.0f), (Half)(-7.0f))));
        Console.WriteLine(Bits(Half.ClampNative((Half)5.0f, Half.Zero, (Half)2.0f)));
        Console.WriteLine(Bits(Half.ClampNative((Half)(-1.0f), Half.Zero, (Half)2.0f)));

        Console.WriteLine("[halfstatics] roots powers");
        Console.WriteLine(Bits(Half.Sqrt((Half)2.25f)));
        Console.WriteLine(Bits(Half.Sqrt((Half)2.0f)));
        Console.WriteLine(Half.IsNaN(Half.Sqrt(Half.NegativeOne)));
        Console.WriteLine(Bits(Half.Cbrt((Half)27.0f)));
        Console.WriteLine(Bits(Half.Cbrt((Half)(-8.0f))));
        Console.WriteLine(Bits(Half.Pow((Half)2.0f, (Half)10.0f)));
        Console.WriteLine(Bits(Half.Pow(hPi, hHalf)));
        Console.WriteLine(Bits(Half.RootN((Half)27.0f, 3)));
        Console.WriteLine(Bits(Half.RootN((Half)16.0f, 4)));
        Console.WriteLine(Bits(Half.RootN((Half)(-27.0f), 3)));
        Console.WriteLine(Bits(Half.RootN((Half)5.0f, -2)));
        Console.WriteLine(Half.IsNaN(Half.RootN((Half)(-16.0f), 4)));
        Console.WriteLine(Bits(Half.Hypot(h3, (Half)4.0f)));
        Console.WriteLine(Bits(Half.Hypot((Half)5.0f, (Half)12.0f)));
        Console.WriteLine(Bits(Half.Hypot(Half.PositiveInfinity, Half.NaN)));

        Console.WriteLine("[halfstatics] trig");
        Console.WriteLine(Bits(Half.Sin(hHalf)));
        Console.WriteLine(Bits(Half.Cos(hHalf)));
        Console.WriteLine(Bits(Half.Tan(hHalf)));
        Console.WriteLine(Bits(Half.Sin(hPi)));
        Console.WriteLine(Bits(Half.Asin(hHalf)));
        Console.WriteLine(Bits(Half.Acos(hHalf)));
        Console.WriteLine(Bits(Half.Atan(Half.One)));
        Console.WriteLine(Bits(Half.Atan2(Half.One, (Half)2.0f)));
        Console.WriteLine(Bits(Half.Atan2((Half)(-3.0f), (Half)0.7f)));
        var (s, c) = Half.SinCos(hHalf);
        Console.WriteLine(Bits(s));
        Console.WriteLine(Bits(c));

        Console.WriteLine("[halfstatics] pi trig");
        Console.WriteLine(Bits(Half.SinPi(hHalf)));
        Console.WriteLine(Bits(Half.SinPi((Half)1.0f)));
        Console.WriteLine(Bits(Half.SinPi((Half)0.25f)));
        Console.WriteLine(Bits(Half.CosPi(hHalf)));
        Console.WriteLine(Bits(Half.CosPi((Half)1.0f)));
        Console.WriteLine(Bits(Half.CosPi((Half)0.25f)));
        Console.WriteLine(Bits(Half.TanPi((Half)0.25f)));
        Console.WriteLine(Bits(Half.TanPi((Half)1.0f)));
        Console.WriteLine(Half.IsPositiveInfinity(Half.TanPi(hHalf)));
        var (sp, cp) = Half.SinCosPi((Half)0.25f);
        Console.WriteLine(Bits(sp));
        Console.WriteLine(Bits(cp));
        Console.WriteLine(Bits(Half.AsinPi(hHalf)));
        Console.WriteLine(Bits(Half.AcosPi((Half)(-0.5f))));
        Console.WriteLine(Bits(Half.AtanPi(Half.One)));
        Console.WriteLine(Bits(Half.Atan2Pi(Half.One, (Half)2.0f)));

        Console.WriteLine("[halfstatics] hyperbolic");
        Console.WriteLine(Bits(Half.Sinh(Half.One)));
        Console.WriteLine(Bits(Half.Cosh(Half.One)));
        Console.WriteLine(Bits(Half.Tanh(Half.One)));
        Console.WriteLine(Bits(Half.Asinh(Half.One)));
        Console.WriteLine(Bits(Half.Acosh((Half)2.0f)));
        Console.WriteLine(Bits(Half.Atanh(hHalf)));

        Console.WriteLine("[halfstatics] exp log");
        Console.WriteLine(Bits(Half.Exp(Half.One)));
        Console.WriteLine(Bits(Half.Exp((Half)(-0.5f))));
        Console.WriteLine(Bits(Half.ExpM1(hHalf)));
        Console.WriteLine(Bits(Half.Exp2((Half)10.0f)));
        Console.WriteLine(Bits(Half.Exp2(hHalf)));
        Console.WriteLine(Bits(Half.Exp2M1(h3)));
        Console.WriteLine(Bits(Half.Exp10((Half)2.0f)));
        Console.WriteLine(Bits(Half.Exp10M1((Half)2.0f)));
        Console.WriteLine(Bits(Half.Log((Half)8.0f)));
        Console.WriteLine(Bits(Half.Log((Half)8.0f, (Half)2.0f)));
        Console.WriteLine(Bits(Half.Log2((Half)1024.0f)));
        Console.WriteLine(Bits(Half.Log2((Half)10.0f)));
        Console.WriteLine(Bits(Half.Log10((Half)100.0f)));
        Console.WriteLine(Bits(Half.LogP1(Half.Zero)));
        Console.WriteLine(Bits(Half.LogP1((Half)1.3f)));
        Console.WriteLine(Bits(Half.Log2P1(h3)));
        Console.WriteLine(Bits(Half.Log10P1((Half)99.0f)));
        Console.WriteLine(Half.IsNegativeInfinity(Half.LogP1(Half.NegativeOne)));
        Console.WriteLine(Half.IsNaN(Half.Log(Half.NegativeOne)));

        Console.WriteLine("[halfstatics] rounding");
        Console.WriteLine(Bits(Half.Round((Half)2.5f)));
        Console.WriteLine(Bits(Half.Round((Half)3.5f)));
        Console.WriteLine(Bits(Half.Round((Half)(-2.5f))));
        Console.WriteLine(Bits(Half.Round(hPi, 2)));
        Console.WriteLine(Bits(Half.Round((Half)2.5f, MidpointRounding.AwayFromZero)));
        Console.WriteLine(Bits(Half.Round((Half)(-2.5f), 0, MidpointRounding.AwayFromZero)));
        Console.WriteLine(Bits(Half.Round(hPi, 1, MidpointRounding.ToZero)));
        try
        {
            Console.WriteLine(Bits(Half.Round(hPi, 2, (MidpointRounding)42)));
        }
        catch (ArgumentException)
        {
            Console.WriteLine("Round bad mode throws");
        }
        Console.WriteLine(Bits(Half.Floor((Half)(-1.5f))));
        Console.WriteLine(Bits(Half.Floor((Half)1.5f)));
        Console.WriteLine(Bits(Half.Ceiling((Half)1.5f)));
        Console.WriteLine(Bits(Half.Ceiling((Half)(-1.5f))));
        Console.WriteLine(Bits(Half.Truncate((Half)(-7.9f))));
        Console.WriteLine(Bits(Half.Truncate((Half)7.9f)));

        Console.WriteLine("[halfstatics] ieee members");
        Console.WriteLine(Bits(Half.FusedMultiplyAdd((Half)2.0f, h3, Half.One)));
        // fma cancellation visible at half precision: (1+2^-10)^2 - 1 - 2^-9
        // is 2^-20, which survives only through a fused multiply-add.
        Half onePlus = BitConverter.UInt16BitsToHalf(0x3C01);  // 1 + 2^-10
        Console.WriteLine(Bits(Half.FusedMultiplyAdd(onePlus, onePlus, (Half)(-1.001953125f))));
        Console.WriteLine(Bits(Half.Ieee754Remainder((Half)10.0f, h3)));
        Console.WriteLine(Bits(Half.Ieee754Remainder(h3, (Half)2.0f)));
        Console.WriteLine(Half.IsNaN(Half.Ieee754Remainder(Half.One, Half.Zero)));
        Console.WriteLine(Half.ILogB((Half)1024.0f));
        Console.WriteLine(Half.ILogB((Half)0.09375f));
        Console.WriteLine(Half.ILogB(Half.Zero));
        Console.WriteLine(Half.ILogB(Half.NaN));
        Console.WriteLine(Half.ILogB(Half.PositiveInfinity));
        Console.WriteLine(Half.ILogB(Half.Epsilon));
        Console.WriteLine(Bits(Half.ScaleB(Half.One, 10)));
        Console.WriteLine(Bits(Half.ScaleB((Half)1.5f, -3)));
        Console.WriteLine(Bits(Half.ScaleB(Half.One, 20)));
        Console.WriteLine(Bits(Half.ScaleB(Half.One, -30)));
        Console.WriteLine(Bits(Half.ScaleB(Half.NegativeOne, -15)));
        Console.WriteLine(Bits(Half.BitIncrement(Half.One)));
        Console.WriteLine(Bits(Half.BitDecrement(Half.One)));
        Console.WriteLine(Bits(Half.BitIncrement(Half.NegativeZero)));
        Console.WriteLine(Bits(Half.BitDecrement(Half.Zero)));
        Console.WriteLine(Bits(Half.BitIncrement(Half.MaxValue)));
        Console.WriteLine(Bits(Half.BitDecrement(Half.PositiveInfinity)));

        Console.WriteLine("[halfstatics] lerp degrees");
        Console.WriteLine(Bits(Half.Lerp((Half)2.0f, (Half)10.0f, hHalf)));
        Console.WriteLine(Bits(Half.Lerp((Half)2.0f, (Half)10.0f, Half.Zero)));
        Console.WriteLine(Bits(Half.Lerp((Half)2.0f, (Half)10.0f, Half.One)));
        Console.WriteLine(Bits(Half.Lerp((Half)1.25f, (Half)7.75f, (Half)0.3f)));
        Console.WriteLine(Bits(Half.DegreesToRadians((Half)180.0f)));
        Console.WriteLine(Bits(Half.DegreesToRadians((Half)90.0f)));
        Console.WriteLine(Bits(Half.RadiansToDegrees(hPi)));
        Console.WriteLine(Bits(Half.RadiansToDegrees((Half)2.3f)));

        Console.WriteLine("[halfstatics] estimates");
        // Platform-defined precision in .NET: assert the pinned IEEE special
        // cases exactly and the finite results only by tolerance.
        Console.WriteLine(Bits(Half.ReciprocalEstimate(Half.Zero)));
        Console.WriteLine(Bits(Half.ReciprocalEstimate(Half.PositiveInfinity)));
        Console.WriteLine(Half.IsNaN(Half.ReciprocalEstimate(Half.NaN)));
        Console.WriteLine(Half.Abs(Half.ReciprocalEstimate((Half)2.0f) - hHalf) <= (Half)0.004f);
        Console.WriteLine(Bits(Half.ReciprocalSqrtEstimate(Half.PositiveInfinity)));
        Console.WriteLine(Half.IsPositiveInfinity(Half.ReciprocalSqrtEstimate(Half.Zero)));
        Console.WriteLine(Half.Abs(Half.ReciprocalSqrtEstimate((Half)4.0f) - hHalf) <= (Half)0.004f);
        Half mae = Half.MultiplyAddEstimate((Half)1.1f, (Half)2.3f, (Half)4.5f);
        Console.WriteLine(Half.Abs(mae - (Half)7.03f) <= (Half)0.01f);

        Console.WriteLine("[halfstatics] predicates");
        Console.WriteLine(Half.IsInteger((Half)2.0f));
        Console.WriteLine(Half.IsInteger((Half)(-3.0f)));
        Console.WriteLine(Half.IsInteger(hHalf));
        Console.WriteLine(Half.IsInteger((Half)2048.0f));
        Console.WriteLine(Half.IsInteger(Half.PositiveInfinity));
        Console.WriteLine(Half.IsInteger(Half.NaN));
        Console.WriteLine(Half.IsEvenInteger((Half)2.0f));
        Console.WriteLine(Half.IsEvenInteger(h3));
        Console.WriteLine(Half.IsEvenInteger((Half)(-4.0f)));
        Console.WriteLine(Half.IsEvenInteger(Half.Zero));
        Console.WriteLine(Half.IsEvenInteger(hHalf));
        // 2047 is the largest odd half integer; 2048 has ulp 2.
        Console.WriteLine(Half.IsEvenInteger((Half)2047.0f));
        Console.WriteLine(Half.IsEvenInteger((Half)2048.0f));
        Console.WriteLine(Half.IsOddInteger(h3));
        Console.WriteLine(Half.IsOddInteger((Half)(-3.0f)));
        Console.WriteLine(Half.IsOddInteger((Half)2.0f));
        Console.WriteLine(Half.IsOddInteger((Half)2047.0f));
        Console.WriteLine(Half.IsOddInteger(Half.PositiveInfinity));
        Console.WriteLine(Half.IsRealNumber(Half.One));
        Console.WriteLine(Half.IsRealNumber(Half.PositiveInfinity));
        Console.WriteLine(Half.IsRealNumber(Half.NaN));
        Console.WriteLine(Half.IsPow2((Half)4.0f));
        Console.WriteLine(Half.IsPow2(Half.One));
        Console.WriteLine(Half.IsPow2(hHalf));
        Console.WriteLine(Half.IsPow2(h3));
        Console.WriteLine(Half.IsPow2((Half)(-4.0f)));
        Console.WriteLine(Half.IsPow2(Half.Zero));
        Console.WriteLine(Half.IsPow2(Half.Epsilon));
        Console.WriteLine(Half.IsPow2(Half.PositiveInfinity));
        Console.WriteLine(Half.IsPositive(Half.Zero));
        Console.WriteLine(Half.IsPositive(Half.NegativeZero));
        Console.WriteLine(Half.IsPositive(Half.CopySign(Half.NaN, Half.One)));
        Console.WriteLine(Half.IsPositive(nnan));
        Console.WriteLine(Half.IsNegative(nnan));

        Console.WriteLine("[halfstatics] constants create convert");
        Console.WriteLine(Bits(Half.MultiplicativeIdentity));
        Console.WriteLine(Bits(Half.CreateChecked(3)));
        Console.WriteLine(Bits(Half.CreateChecked(0.25)));
        Console.WriteLine(Bits(Half.CreateSaturating(70000)));
        Console.WriteLine(Bits(Half.CreateSaturating(-70000.0)));
        Console.WriteLine(Bits(Half.CreateTruncating(2049)));
        Console.WriteLine(Bits(Half.CreateChecked(0.1f)));
        // Checked narrowing operators (op_CheckedExplicit) and the saturating
        // unchecked route.
        Console.WriteLine((int)Half.MaxValue);
        Console.WriteLine(checked((int)h3));
        try
        {
            Console.WriteLine(checked((short)Half.MaxValue));
        }
        catch (OverflowException)
        {
            Console.WriteLine("checked (short)65504 throws");
        }
        try
        {
            Console.WriteLine(checked((int)Half.PositiveInfinity));
        }
        catch (OverflowException)
        {
            Console.WriteLine("checked (int)Inf throws");
        }
        Console.WriteLine((int)Half.PositiveInfinity);
        Console.WriteLine((short)Half.MaxValue);

        Console.WriteLine("[halfstatics] constrained generics");
        Console.WriteLine(Bits(GAbs((Half)(-2.75f))));
        Console.WriteLine(Bits(GClamp((Half)5.0f, Half.Zero, (Half)2.0f)));
        Console.WriteLine(Bits(GSin(hHalf)));
        Console.WriteLine(Bits(GSqrt((Half)2.25f)));
        Console.WriteLine(Bits(GExp(Half.One)));
        Console.WriteLine(Bits(GLog2((Half)1024.0f)));
        Console.WriteLine(Bits(GFma((Half)2.0f, h3, Half.One)));
        Console.WriteLine(GIsNaN(Half.NaN));
        Console.WriteLine(GIsNaN(Half.One));
        Console.WriteLine(Bits(GMaxNumber(Half.NaN, Half.One)));
    }
}
