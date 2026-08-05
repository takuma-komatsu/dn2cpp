using System;
using System.Numerics;

namespace GenericMathFloatingPoint;

// The floating-point generic-math interfaces dispatched through constrained
// static-virtual calls — T.Sqrt(x) under `where T : IRootFunctions<T>`, and
// likewise ITrigonometricFunctions / IExponentialFunctions / ILogarithmicFunctions /
// IPowerFunctions / IHyperbolicFunctions / IFloatingPointIeee754 / IFloatingPoint.
// Every helper is generic over one interface constraint and monomorphizes at BOTH
// double and float (TSelf closed to the intrinsic-mapped primitives, resolved via
// the CoreLib Double/Single structs), plus Half spot-checks (a non-intrinsic
// struct exercising the plain value-type resolution path). Output determinism
// (the gate diffs against real .NET): exactly-representable results print
// directly; irrational results assert equality against the same-type non-generic
// static (both routes reach the identical implementation on each side of the
// diff) and print the bool; NaN/Infinity results and the IEEE constants go
// through predicates or equality with the non-generic constant.
// ReciprocalEstimate/MultiplyAddEstimate are platform-defined and excluded.
internal static class FloatingPointMath
{
    // IRootFunctions
    static T GSqrt<T>(T x) where T : IRootFunctions<T> => T.Sqrt(x);
    static T GCbrt<T>(T x) where T : IRootFunctions<T> => T.Cbrt(x);
    static T GRootN<T>(T x, int n) where T : IRootFunctions<T> => T.RootN(x, n);
    static T GHypot<T>(T x, T y) where T : IRootFunctions<T> => T.Hypot(x, y);

    // ITrigonometricFunctions
    static T GSin<T>(T x) where T : ITrigonometricFunctions<T> => T.Sin(x);
    static T GCos<T>(T x) where T : ITrigonometricFunctions<T> => T.Cos(x);
    static T GTan<T>(T x) where T : ITrigonometricFunctions<T> => T.Tan(x);
    static T GAsin<T>(T x) where T : ITrigonometricFunctions<T> => T.Asin(x);
    static T GAcos<T>(T x) where T : ITrigonometricFunctions<T> => T.Acos(x);
    static T GAtan<T>(T x) where T : ITrigonometricFunctions<T> => T.Atan(x);
    static (T Sin, T Cos) GSinCos<T>(T x) where T : ITrigonometricFunctions<T> => T.SinCos(x);
    static T GSinPi<T>(T x) where T : ITrigonometricFunctions<T> => T.SinPi(x);
    static T GCosPi<T>(T x) where T : ITrigonometricFunctions<T> => T.CosPi(x);
    static T GDegToRad<T>(T x) where T : ITrigonometricFunctions<T> => T.DegreesToRadians(x);

    // IExponentialFunctions
    static T GExp<T>(T x) where T : IExponentialFunctions<T> => T.Exp(x);
    static T GExp2<T>(T x) where T : IExponentialFunctions<T> => T.Exp2(x);
    static T GExp10<T>(T x) where T : IExponentialFunctions<T> => T.Exp10(x);
    static T GExpM1<T>(T x) where T : IExponentialFunctions<T> => T.ExpM1(x);

    // ILogarithmicFunctions
    static T GLog<T>(T x) where T : ILogarithmicFunctions<T> => T.Log(x);
    static T GLog2<T>(T x) where T : ILogarithmicFunctions<T> => T.Log2(x);
    static T GLog10<T>(T x) where T : ILogarithmicFunctions<T> => T.Log10(x);
    static T GLogP1<T>(T x) where T : ILogarithmicFunctions<T> => T.LogP1(x);
    static T GLogBase<T>(T x, T b) where T : ILogarithmicFunctions<T> => T.Log(x, b);

    // IPowerFunctions
    static T GPow<T>(T x, T y) where T : IPowerFunctions<T> => T.Pow(x, y);

    // IHyperbolicFunctions
    static T GSinh<T>(T x) where T : IHyperbolicFunctions<T> => T.Sinh(x);
    static T GCosh<T>(T x) where T : IHyperbolicFunctions<T> => T.Cosh(x);
    static T GTanh<T>(T x) where T : IHyperbolicFunctions<T> => T.Tanh(x);
    static T GAsinh<T>(T x) where T : IHyperbolicFunctions<T> => T.Asinh(x);
    static T GAcosh<T>(T x) where T : IHyperbolicFunctions<T> => T.Acosh(x);
    static T GAtanh<T>(T x) where T : IHyperbolicFunctions<T> => T.Atanh(x);

    // IFloatingPointIeee754 members
    static T GAtan2<T>(T y, T x) where T : IFloatingPointIeee754<T> => T.Atan2(y, x);
    static T GFma<T>(T a, T b, T c) where T : IFloatingPointIeee754<T> => T.FusedMultiplyAdd(a, b, c);
    static T GRem<T>(T a, T b) where T : IFloatingPointIeee754<T> => T.Ieee754Remainder(a, b);
    static T GScaleB<T>(T x, int n) where T : IFloatingPointIeee754<T> => T.ScaleB(x, n);
    static int GILogB<T>(T x) where T : IFloatingPointIeee754<T> => T.ILogB(x);
    static T GBitInc<T>(T x) where T : IFloatingPointIeee754<T> => T.BitIncrement(x);
    static T GBitDec<T>(T x) where T : IFloatingPointIeee754<T> => T.BitDecrement(x);
    static T GLerp<T>(T a, T b, T t) where T : IFloatingPointIeee754<T> => T.Lerp(a, b, t);

    // IFloatingPointIeee754 constants (their Double/Single impls are explicit
    // property impls — the interface-side constant lowering)
    static T GEpsilon<T>() where T : IFloatingPointIeee754<T> => T.Epsilon;
    static T GNaN<T>() where T : IFloatingPointIeee754<T> => T.NaN;
    static T GPosInf<T>() where T : IFloatingPointIeee754<T> => T.PositiveInfinity;
    static T GNegInf<T>() where T : IFloatingPointIeee754<T> => T.NegativeInfinity;
    static T GNegZero<T>() where T : IFloatingPointIeee754<T> => T.NegativeZero;
    static T GPi<T>() where T : IFloatingPointIeee754<T> => T.Pi;
    static T GE<T>() where T : IFloatingPointIeee754<T> => T.E;
    static T GTau<T>() where T : IFloatingPointIeee754<T> => T.Tau;

    // INumberBase members free-riding on the same constraint
    static bool GIsInteger<T>(T x) where T : IFloatingPointIeee754<T> => T.IsInteger(x);
    static bool GIsNaN<T>(T x) where T : IFloatingPointIeee754<T> => T.IsNaN(x);
    static bool GIsNegative<T>(T x) where T : IFloatingPointIeee754<T> => T.IsNegative(x);
    static bool GIsPositive<T>(T x) where T : IFloatingPointIeee754<T> => T.IsPositive(x);
    static bool GIsFinite<T>(T x) where T : IFloatingPointIeee754<T> => T.IsFinite(x);
    static bool GIsSubnormal<T>(T x) where T : IFloatingPointIeee754<T> => T.IsSubnormal(x);
    static T GMaxMagnitude<T>(T x, T y) where T : IFloatingPointIeee754<T> => T.MaxMagnitude(x, y);
    static T GMinMagnitude<T>(T x, T y) where T : IFloatingPointIeee754<T> => T.MinMagnitude(x, y);

    // IFloatingPoint
    static T GFloor<T>(T x) where T : IFloatingPoint<T> => T.Floor(x);
    static T GCeiling<T>(T x) where T : IFloatingPoint<T> => T.Ceiling(x);
    static T GRound<T>(T x) where T : IFloatingPoint<T> => T.Round(x);
    static T GTruncate<T>(T x) where T : IFloatingPoint<T> => T.Truncate(x);

    internal static void __GateEntry()
    {
        Console.WriteLine("== FP generic math: double ==");
        Console.WriteLine(GSqrt(2.25));                             // 1.5
        Console.WriteLine(GSqrt(2.0) == double.Sqrt(2.0));          // True
        Console.WriteLine(GCbrt(27.0));                             // 3
        Console.WriteLine(GRootN(32.0, 5) == double.RootN(32.0, 5)); // True
        Console.WriteLine(GHypot(3.0, 4.0));                        // 5
        Console.WriteLine(GSin(0.5) == double.Sin(0.5));            // True
        Console.WriteLine(GCos(0.5) == double.Cos(0.5));            // True
        Console.WriteLine(GTan(0.5) == double.Tan(0.5));            // True
        Console.WriteLine(GAsin(0.5) == double.Asin(0.5));          // True
        Console.WriteLine(GAcos(0.5) == double.Acos(0.5));          // True
        Console.WriteLine(GAtan(0.5) == double.Atan(0.5));          // True
        var (s, c) = GSinCos(0.5);
        Console.WriteLine(s == double.Sin(0.5) && c == double.Cos(0.5)); // True
        Console.WriteLine(GSinPi(0.25) == double.SinPi(0.25));      // True
        Console.WriteLine(GCosPi(0.25) == double.CosPi(0.25));      // True
        Console.WriteLine(GDegToRad(90.0) == double.DegreesToRadians(90.0)); // True
        Console.WriteLine(GExp(1.0) == double.Exp(1.0));            // True
        Console.WriteLine(GExp2(10.0));                             // 1024
        Console.WriteLine(GExp10(2.0));                             // 100
        Console.WriteLine(GExpM1(0.5) == double.ExpM1(0.5));        // True
        Console.WriteLine(GLog(8.0) == double.Log(8.0));            // True
        Console.WriteLine(GLog2(8.0));                              // 3
        Console.WriteLine(GLog10(0.5) == double.Log10(0.5));        // True
        Console.WriteLine(GLogP1(0.5) == double.LogP1(0.5));        // True
        Console.WriteLine(GLogBase(8.0, 2.0));                      // 3
        Console.WriteLine(GPow(2.0, 10.0));                         // 1024
        Console.WriteLine(GSinh(0.5) == double.Sinh(0.5));          // True
        Console.WriteLine(GCosh(0.5) == double.Cosh(0.5));          // True
        Console.WriteLine(GTanh(0.5) == double.Tanh(0.5));          // True
        Console.WriteLine(GAsinh(0.5) == double.Asinh(0.5));        // True
        Console.WriteLine(GAcosh(1.5) == double.Acosh(1.5));        // True
        Console.WriteLine(GAtanh(0.5) == double.Atanh(0.5));        // True
        Console.WriteLine(GAtan2(1.0, 2.0) == double.Atan2(1.0, 2.0)); // True
        Console.WriteLine(GFma(2.0, 3.0, 1.0));                     // 7
        Console.WriteLine(GRem(5.0, 3.0));                          // -1
        Console.WriteLine(GScaleB(1.5, 3));                         // 12
        Console.WriteLine(GILogB(1024.0));                          // 10
        Console.WriteLine(GBitInc(1.0) == double.BitIncrement(1.0)); // True
        Console.WriteLine(GBitDec(1.0) == double.BitDecrement(1.0)); // True
        Console.WriteLine(GLerp(2.0, 6.0, 0.25));                   // 3

        Console.WriteLine("== FP generic math: float ==");
        Console.WriteLine(GSqrt(2.25f));                            // 1.5
        Console.WriteLine(GCbrt(27.0f));                            // 3
        Console.WriteLine(GRootN(32.0f, 5) == float.RootN(32.0f, 5)); // True
        Console.WriteLine(GHypot(3.0f, 4.0f));                      // 5
        Console.WriteLine(GSin(0.5f) == float.Sin(0.5f));           // True
        Console.WriteLine(GAsin(0.5f) == float.Asin(0.5f));         // True
        var (fs, fc) = GSinCos(0.5f);
        Console.WriteLine(fs == float.Sin(0.5f) && fc == float.Cos(0.5f)); // True
        Console.WriteLine(GSinPi(0.25f) == float.SinPi(0.25f));     // True
        Console.WriteLine(GCosPi(0.25f) == float.CosPi(0.25f));     // True
        Console.WriteLine(GDegToRad(90.0f) == float.DegreesToRadians(90.0f)); // True
        Console.WriteLine(GExp(1.0f) == float.Exp(1.0f));           // True
        Console.WriteLine(GExp2(10.0f));                            // 1024
        Console.WriteLine(GExp10(2.0f));                            // 100
        Console.WriteLine(GExpM1(0.5f) == float.ExpM1(0.5f));       // True
        Console.WriteLine(GLog(8.0f) == float.Log(8.0f));           // True
        Console.WriteLine(GLog2(8.0f));                             // 3
        Console.WriteLine(GLog10(0.5f) == float.Log10(0.5f));       // True
        Console.WriteLine(GLogP1(0.5f) == float.LogP1(0.5f));       // True
        Console.WriteLine(GLogBase(8.0f, 2.0f));                    // 3
        Console.WriteLine(GPow(2.0f, 10.0f));                       // 1024
        Console.WriteLine(GTanh(0.5f) == float.Tanh(0.5f));         // True
        Console.WriteLine(GAcosh(1.5f) == float.Acosh(1.5f));       // True
        Console.WriteLine(GAtan2(1.0f, 1.0f) == float.Atan2(1.0f, 1.0f)); // True
        Console.WriteLine(GFma(2.0f, 3.0f, 1.0f));                  // 7
        Console.WriteLine(GRem(5.0f, 3.0f));                        // -1
        Console.WriteLine(GScaleB(1.5f, 3));                        // 12
        Console.WriteLine(GILogB(1024.0f));                         // 10
        Console.WriteLine(GBitInc(1.0f) == float.BitIncrement(1.0f)); // True
        Console.WriteLine(GBitDec(1.0f) == float.BitDecrement(1.0f)); // True
        Console.WriteLine(GLerp(2.0f, 6.0f, 0.25f));                // 3

        Console.WriteLine("== FP generic math: IEEE constants ==");
        Console.WriteLine(GPi<double>() == double.Pi);              // True
        Console.WriteLine(GE<double>() == double.E);                // True
        Console.WriteLine(GTau<double>() == double.Tau);            // True
        Console.WriteLine(GEpsilon<double>() == double.Epsilon);    // True
        Console.WriteLine(double.IsNaN(GNaN<double>()));            // True
        Console.WriteLine(double.IsPositiveInfinity(GPosInf<double>())); // True
        Console.WriteLine(double.IsNegativeInfinity(GNegInf<double>())); // True
        Console.WriteLine(double.IsNegativeInfinity(1.0 / GNegZero<double>())); // True
        Console.WriteLine(GPi<float>() == float.Pi);                // True
        Console.WriteLine(GE<float>() == float.E);                  // True
        Console.WriteLine(GTau<float>() == float.Tau);              // True
        Console.WriteLine(GEpsilon<float>() == float.Epsilon);      // True
        Console.WriteLine(float.IsNaN(GNaN<float>()));              // True
        Console.WriteLine(float.IsPositiveInfinity(GPosInf<float>())); // True
        Console.WriteLine(float.IsNegativeInfinity(GNegInf<float>())); // True
        Console.WriteLine(float.IsNegativeInfinity(1.0f / GNegZero<float>())); // True

        Console.WriteLine("== FP generic math: predicates / magnitude ==");
        Console.WriteLine(GIsInteger(3.0));                         // True
        Console.WriteLine(GIsInteger(2.5));                         // False
        Console.WriteLine(GIsNaN(GNaN<double>()));                  // True
        Console.WriteLine(GIsNegative(GNegZero<double>()));         // True (signbit)
        Console.WriteLine(GIsPositive(GNegZero<double>()));         // False (signbit)
        Console.WriteLine(GIsPositive(0.0));                        // True
        Console.WriteLine(GIsFinite(1.5));                          // True
        Console.WriteLine(GIsFinite(GPosInf<double>()));            // False
        Console.WriteLine(GIsSubnormal(GEpsilon<double>()));        // True
        Console.WriteLine(GMaxMagnitude(2.0, -3.0));                // -3
        Console.WriteLine(GMinMagnitude(-4.0, 3.0));                // 3
        Console.WriteLine(GIsInteger(2.5f));                        // False
        Console.WriteLine(GIsNegative(GNegZero<float>()));          // True (signbit)
        Console.WriteLine(GIsSubnormal(GEpsilon<float>()));         // True
        Console.WriteLine(GMaxMagnitude(2.0f, -3.0f));              // -3
        Console.WriteLine(GMinMagnitude(-4.0f, 3.0f));              // 3

        Console.WriteLine("== FP generic math: rounding ==");
        Console.WriteLine(GFloor(-1.5));                            // -2
        Console.WriteLine(GCeiling(1.25));                          // 2
        Console.WriteLine(GRound(2.5));                             // 2
        Console.WriteLine(GTruncate(-7.9));                         // -7
        Console.WriteLine(GFloor(-1.5f));                           // -2
        Console.WriteLine(GCeiling(1.25f));                         // 2
        Console.WriteLine(GRound(2.5f));                            // 2
        Console.WriteLine(GTruncate(-7.9f));                        // -7

        Console.WriteLine("== FP generic math: Half spot-checks ==");
        Console.WriteLine(GSqrt((Half)2.25f));                      // 1.5
        Console.WriteLine(GPow((Half)2.0f, (Half)3.0f));            // 8
        Console.WriteLine(GFloor((Half)(-1.5f)));                   // -2
        Console.WriteLine(GIsInteger((Half)3.0f));                  // True
        Console.WriteLine(GMaxMagnitude((Half)2.0f, (Half)(-3.0f))); // -3
    }
}
