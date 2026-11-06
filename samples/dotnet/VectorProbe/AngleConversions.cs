using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace VectorProbe;

internal static partial class Program
{
    private static int _angleChecks;
    private static int _angleEvaluations;

    private static void AngleConversions()
    {
        Console.WriteLine("-- vector angle conversions --");
        _angleChecks = 0;
        float[] singles =
        {
            0.0f, -0.0f, float.Epsilon, -float.Epsilon,
            BitConverter.Int32BitsToSingle(0x00800000), BitConverter.Int32BitsToSingle(unchecked((int)0x80800000)),
            BitConverter.Int32BitsToSingle(0x007fffff), BitConverter.Int32BitsToSingle(unchecked((int)0x807fffff)),
            1.0f, -1.0f, float.Pi, -float.Pi,
            180.0f, -180.0f, 2.3f, -2.3f,
            float.MaxValue / 2, -float.MaxValue / 2, float.MaxValue / 64, -float.MaxValue / 64,
            float.PositiveInfinity, float.NegativeInfinity, float.NaN, BitConverter.Int32BitsToSingle(0x7fc01234),
            90.0f, 0.5f, 12.34f, -12.34f,
        };
        double[] doubles =
        {
            0.0, -0.0, double.Epsilon, -double.Epsilon,
            BitConverter.Int64BitsToDouble(0x0010000000000000L), BitConverter.Int64BitsToDouble(unchecked((long)0x8010000000000000UL)),
            BitConverter.Int64BitsToDouble(0x000fffffffffffffL), BitConverter.Int64BitsToDouble(unchecked((long)0x800fffffffffffffUL)),
            1.0, -1.0, double.Pi, -double.Pi,
            180.0, -180.0, 2.3, -2.3,
            double.MaxValue / 2, -double.MaxValue / 2, double.MaxValue / 64, -double.MaxValue / 64,
            double.PositiveInfinity, double.NegativeInfinity, double.NaN, BitConverter.Int64BitsToDouble(0x7ff8000000001234L),
            90.0, 0.5, 12.34, -12.34,
        };

        for (int i = 0; i < singles.Length; i += 4)
        {
            var input = Vector128.Create(singles[i], singles[i + 1], singles[i + 2], singles[i + 3]);
            var degrees = Vector128.DegreesToRadians(input);
            var radians = Vector128.RadiansToDegrees(input);
            for (int lane = 0; lane < Vector128<float>.Count; ++lane)
            {
                AngleF("v128f.degrees." + (i + lane), singles[i + lane], degrees.GetElement(lane), true);
                AngleF("v128f.radians." + (i + lane), singles[i + lane], radians.GetElement(lane), false);
            }

            // Vector2/3 wrappers have undefined upper lanes; only named fields count.
            var v2 = new Vector2(singles[i], singles[i + 1]);
            var d2 = Vector2.DegreesToRadians(v2);
            var r2 = Vector2.RadiansToDegrees(v2);
            AngleF("v2.degrees.x." + i, v2.X, d2.X, true);
            AngleF("v2.degrees.y." + i, v2.Y, d2.Y, true);
            AngleF("v2.radians.x." + i, v2.X, r2.X, false);
            AngleF("v2.radians.y." + i, v2.Y, r2.Y, false);

            var v3 = new Vector3(singles[i], singles[i + 1], singles[i + 2]);
            var d3 = Vector3.DegreesToRadians(v3);
            var r3 = Vector3.RadiansToDegrees(v3);
            AngleF("v3.degrees.x." + i, v3.X, d3.X, true);
            AngleF("v3.degrees.y." + i, v3.Y, d3.Y, true);
            AngleF("v3.degrees.z." + i, v3.Z, d3.Z, true);
            AngleF("v3.radians.x." + i, v3.X, r3.X, false);
            AngleF("v3.radians.y." + i, v3.Y, r3.Y, false);
            AngleF("v3.radians.z." + i, v3.Z, r3.Z, false);

            var v4 = new Vector4(singles[i], singles[i + 1], singles[i + 2], singles[i + 3]);
            var d4 = Vector4.DegreesToRadians(v4);
            var r4 = Vector4.RadiansToDegrees(v4);
            AngleF("v4.degrees.x." + i, v4.X, d4.X, true);
            AngleF("v4.degrees.y." + i, v4.Y, d4.Y, true);
            AngleF("v4.degrees.z." + i, v4.Z, d4.Z, true);
            AngleF("v4.degrees.w." + i, v4.W, d4.W, true);
            AngleF("v4.radians.x." + i, v4.X, r4.X, false);
            AngleF("v4.radians.y." + i, v4.Y, r4.Y, false);
            AngleF("v4.radians.z." + i, v4.Z, r4.Z, false);
            AngleF("v4.radians.w." + i, v4.W, r4.W, false);
        }

        for (int i = 0; i < doubles.Length; i += 2)
        {
            var input = Vector128.Create(doubles[i], doubles[i + 1]);
            var degrees = Vector128.DegreesToRadians(input);
            var radians = Vector128.RadiansToDegrees(input);
            for (int lane = 0; lane < Vector128<double>.Count; ++lane)
            {
                AngleD("v128d.degrees." + (i + lane), doubles[i + lane], degrees.GetElement(lane), true);
                AngleD("v128d.radians." + (i + lane), doubles[i + lane], radians.GetElement(lane), false);
            }
        }

        AngleSiblingWidths();
        _angleEvaluations = 0;
        var onceDegrees = Vector4.DegreesToRadians(AngleInput());
        var onceRadians = Vector4.RadiansToDegrees(AngleInput());
        if (_angleEvaluations != 2)
            throw new InvalidOperationException("Angle argument evaluated more than once");
        AngleF("once.degrees", 2.3f, onceDegrees.X, true);
        AngleF("once.radians", 2.3f, onceRadians.X, false);
        Console.WriteLine("angle-evaluations=" + _angleEvaluations);
        Console.WriteLine("angle-checks=" + _angleChecks);
        Console.WriteLine("-- vector angle conversions end --");
    }

    private static void AngleSiblingWidths()
    {
        var f64 = Vector64.Create(2.3f, -0.0f);
        var d64 = Vector64.DegreesToRadians(f64);
        var r64 = Vector64.RadiansToDegrees(f64);
        for (int i = 0; i < Vector64<float>.Count; ++i)
        {
            AngleF("v64f.degrees." + i, f64.GetElement(i), d64.GetElement(i), true);
            AngleF("v64f.radians." + i, f64.GetElement(i), r64.GetElement(i), false);
        }
        var f128 = Vector128.Create(2.3f, -0.0f, float.MaxValue / 64, float.NaN);
        var f256 = Vector256.Create(f128, f128);
        var d256 = Vector256.DegreesToRadians(f256);
        var r256 = Vector256.RadiansToDegrees(f256);
        for (int i = 0; i < Vector256<float>.Count; ++i)
        {
            AngleF("v256f.degrees." + i, f256.GetElement(i), d256.GetElement(i), true);
            AngleF("v256f.radians." + i, f256.GetElement(i), r256.GetElement(i), false);
        }
        var f512 = Vector512.Create(f256, f256);
        var d512 = Vector512.DegreesToRadians(f512);
        var r512 = Vector512.RadiansToDegrees(f512);
        for (int i = 0; i < Vector512<float>.Count; ++i)
        {
            AngleF("v512f.degrees." + i, f512.GetElement(i), d512.GetElement(i), true);
            AngleF("v512f.radians." + i, f512.GetElement(i), r512.GetElement(i), false);
        }
        float[] nfValues = new float[Vector<float>.Count];
        for (int i = 0; i < nfValues.Length; ++i) nfValues[i] = f128.GetElement(i % 4);
        var nfloat = new Vector<float>(nfValues);
        var nfDegrees = Vector.DegreesToRadians(nfloat);
        var nfRadians = Vector.RadiansToDegrees(nfloat);
        // Observe the common 128-bit prefix; the CLR may have a wider Vector<T>.
        for (int i = 0; i < 4; ++i)
        {
            AngleF("numericsf.degrees." + i, nfloat[i], nfDegrees[i], true);
            AngleF("numericsf.radians." + i, nfloat[i], nfRadians[i], false);
        }

        var d64Input = Vector64.Create(double.MaxValue / 64);
        AngleD("v64d.degrees", d64Input.GetElement(0), Vector64.DegreesToRadians(d64Input).GetElement(0), true);
        AngleD("v64d.radians", d64Input.GetElement(0), Vector64.RadiansToDegrees(d64Input).GetElement(0), false);
        var d128Input = Vector128.Create(2.3, -0.0);
        var d256Input = Vector256.Create(d128Input, Vector128.Create(double.MaxValue / 64, double.NaN));
        var dd256 = Vector256.DegreesToRadians(d256Input);
        var rd256 = Vector256.RadiansToDegrees(d256Input);
        for (int i = 0; i < Vector256<double>.Count; ++i)
        {
            AngleD("v256d.degrees." + i, d256Input.GetElement(i), dd256.GetElement(i), true);
            AngleD("v256d.radians." + i, d256Input.GetElement(i), rd256.GetElement(i), false);
        }
        var d512Input = Vector512.Create(d256Input, d256Input);
        var dd512 = Vector512.DegreesToRadians(d512Input);
        var rd512 = Vector512.RadiansToDegrees(d512Input);
        for (int i = 0; i < Vector512<double>.Count; ++i)
        {
            AngleD("v512d.degrees." + i, d512Input.GetElement(i), dd512.GetElement(i), true);
            AngleD("v512d.radians." + i, d512Input.GetElement(i), rd512.GetElement(i), false);
        }
        double[] ndValues = new double[Vector<double>.Count];
        for (int i = 0; i < ndValues.Length; ++i) ndValues[i] = d256Input.GetElement(i % 4);
        var ndouble = new Vector<double>(ndValues);
        var ndDegrees = Vector.DegreesToRadians(ndouble);
        var ndRadians = Vector.RadiansToDegrees(ndouble);
        for (int i = 0; i < 2; ++i)
        {
            AngleD("numericsd.degrees." + i, ndouble[i], ndDegrees[i], true);
            AngleD("numericsd.radians." + i, ndouble[i], ndRadians[i], false);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector4 AngleInput()
    {
        ++_angleEvaluations;
        return new Vector4(2.3f, -0.0f, float.MaxValue / 64, float.NaN);
    }

    private static void AngleF(string label, float input, float actual, bool degrees)
    {
        float expected = degrees ? float.DegreesToRadians(input) : float.RadiansToDegrees(input);
        // NaN payloads are not contractual; every other result is bit-exact.
        bool same = float.IsNaN(expected) ? float.IsNaN(actual)
            : BitConverter.SingleToInt32Bits(expected) == BitConverter.SingleToInt32Bits(actual);
        if (!same) throw new InvalidOperationException(label);
        ++_angleChecks;
        Console.WriteLine(label + "=" + (float.IsNaN(actual) ? "NaN" : BitConverter.SingleToInt32Bits(actual).ToString("X8")));
    }

    private static void AngleD(string label, double input, double actual, bool degrees)
    {
        double expected = degrees ? double.DegreesToRadians(input) : double.RadiansToDegrees(input);
        bool same = double.IsNaN(expected) ? double.IsNaN(actual)
            : BitConverter.DoubleToInt64Bits(expected) == BitConverter.DoubleToInt64Bits(actual);
        if (!same) throw new InvalidOperationException(label);
        ++_angleChecks;
        Console.WriteLine(label + "=" + (double.IsNaN(actual) ? "NaN" : BitConverter.DoubleToInt64Bits(actual).ToString("X16")));
    }
}
