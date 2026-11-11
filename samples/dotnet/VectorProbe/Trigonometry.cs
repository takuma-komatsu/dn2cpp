using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace VectorProbe;

internal static partial class Program
{
    private static int _trigChecks;
    private static int _trigEvaluations;
    private static bool _trigRecords;

    private static void Trigonometry(bool records = false)
    {
        Console.WriteLine("-- vector trigonometry --");
        _trigChecks = 0;
        _trigRecords = records;
        float[] singles =
        {
            0.0f, -0.0f, float.Epsilon, -float.Epsilon,
            BitConverter.Int32BitsToSingle(0x00800000), BitConverter.Int32BitsToSingle(unchecked((int)0x80800000)),
            BitConverter.Int32BitsToSingle(0x007fffff), BitConverter.Int32BitsToSingle(unchecked((int)0x807fffff)),
            1.0f, -1.0f, 0.5f, -0.5f,
            float.Pi / 4, -float.Pi / 4, float.Pi / 2, -float.Pi / 2,
            float.BitIncrement(float.Pi / 2), float.BitDecrement(float.Pi / 2), float.Pi, -float.Pi,
            1e6f, -1e6f, 1e20f, -1e20f,
            float.MaxValue, -float.MaxValue, float.PositiveInfinity, float.NegativeInfinity,
            float.NaN, BitConverter.Int32BitsToSingle(0x7fc01234), 2.3f, -2.3f,
        };
        double[] doubles =
        {
            0.0, -0.0, double.Epsilon, -double.Epsilon,
            BitConverter.Int64BitsToDouble(0x0010000000000000L), BitConverter.Int64BitsToDouble(unchecked((long)0x8010000000000000UL)),
            BitConverter.Int64BitsToDouble(0x000fffffffffffffL), BitConverter.Int64BitsToDouble(unchecked((long)0x800fffffffffffffUL)),
            1.0, -1.0, 0.5, -0.5,
            double.Pi / 4, -double.Pi / 4, double.Pi / 2, -double.Pi / 2,
            double.BitIncrement(double.Pi / 2), double.BitDecrement(double.Pi / 2), double.Pi, -double.Pi,
            1e6, -1e6, 1e300, -1e300,
            double.MaxValue, -double.MaxValue, double.PositiveInfinity, double.NegativeInfinity,
            double.NaN, BitConverter.Int64BitsToDouble(0x7ff8000000001234L), 2.3, -2.3,
        };
        for (int i = 0; i < singles.Length; i += 4)
        {
            var input = Vector128.Create(singles[i], singles[i + 1], singles[i + 2], singles[i + 3]);
            var sin = Vector128.Sin(input);
            var cos = Vector128.Cos(input);
            var pair = Vector128.SinCos(input);
            for (int lane = 0; lane < 4; ++lane)
                TrigLaneF("v128f." + (i + lane), singles[i + lane], sin.GetElement(lane), cos.GetElement(lane),
                    pair.Sin.GetElement(lane), pair.Cos.GetElement(lane));
        }
        for (int i = 0; i < doubles.Length; i += 2)
        {
            var input = Vector128.Create(doubles[i], doubles[i + 1]);
            var sin = Vector128.Sin(input);
            var cos = Vector128.Cos(input);
            var pair = Vector128.SinCos(input);
            for (int lane = 0; lane < 2; ++lane)
                TrigLaneD("v128d." + (i + lane), doubles[i + lane], sin.GetElement(lane), cos.GetElement(lane),
                    pair.Sin.GetElement(lane), pair.Cos.GetElement(lane));
        }
        TrigSiblingWidths();
        TrigConcreteVectors();
        _trigEvaluations = 0;
        var returned = TrigReturnedPair(TrigInput());
        var (onceSin, onceCos) = returned;
        if (_trigEvaluations != 1) throw new InvalidOperationException("SinCos argument evaluation");
        TrigF("once.sin.x", 1.0f, onceSin.X, true);
        TrigF("once.cos.x", 1.0f, onceCos.X, false);
        TrigF("once.sin.y", -0.0f, onceSin.Y, true);
        TrigF("once.cos.y", -0.0f, onceCos.Y, false);
        TrigF("once.sin.z", -0.5f, onceSin.Z, true);
        TrigF("once.cos.z", -0.5f, onceCos.Z, false);
        TrigF("once.sin.w", 2.3f, onceSin.W, true);
        TrigF("once.cos.w", 2.3f, onceCos.W, false);
        Console.WriteLine("trig-evaluations=" + _trigEvaluations);
        Console.WriteLine("trig-checks=" + _trigChecks);
        Console.WriteLine("-- vector trigonometry end --");
    }

    private static void TrigConcreteVectors()
    {
        // Observe only named fields; Vector2/3 have no defined upper lanes.
        var v2 = new Vector2(-0.0f, 1.0f);
        var s2 = Vector2.Sin(v2); var c2 = Vector2.Cos(v2); var p2 = Vector2.SinCos(v2);
        TrigLaneF("v2.x", v2.X, s2.X, c2.X, p2.Sin.X, p2.Cos.X);
        TrigLaneF("v2.y", v2.Y, s2.Y, c2.Y, p2.Sin.Y, p2.Cos.Y);
        var v3 = new Vector3(-1.0f, float.Epsilon, float.PositiveInfinity);
        var s3 = Vector3.Sin(v3); var c3 = Vector3.Cos(v3); var p3 = Vector3.SinCos(v3);
        TrigLaneF("v3.x", v3.X, s3.X, c3.X, p3.Sin.X, p3.Cos.X);
        TrigLaneF("v3.y", v3.Y, s3.Y, c3.Y, p3.Sin.Y, p3.Cos.Y);
        TrigLaneF("v3.z", v3.Z, s3.Z, c3.Z, p3.Sin.Z, p3.Cos.Z);
        var v4 = new Vector4(0.5f, -0.5f, float.NaN, 2.3f);
        var s4 = Vector4.Sin(v4); var c4 = Vector4.Cos(v4); var p4 = Vector4.SinCos(v4);
        TrigLaneF("v4.x", v4.X, s4.X, c4.X, p4.Sin.X, p4.Cos.X);
        TrigLaneF("v4.y", v4.Y, s4.Y, c4.Y, p4.Sin.Y, p4.Cos.Y);
        TrigLaneF("v4.z", v4.Z, s4.Z, c4.Z, p4.Sin.Z, p4.Cos.Z);
        TrigLaneF("v4.w", v4.W, s4.W, c4.W, p4.Sin.W, p4.Cos.W);
    }

    private static void TrigSiblingWidths()
    {
        var f64 = Vector64.Create(1.0f, -0.0f);
        var s64 = Vector64.Sin(f64); var c64 = Vector64.Cos(f64); var p64 = Vector64.SinCos(f64);
        for (int i = 0; i < 2; ++i)
            TrigLaneF("v64f." + i, f64.GetElement(i), s64.GetElement(i), c64.GetElement(i), p64.Sin.GetElement(i), p64.Cos.GetElement(i));
        var f128 = Vector128.Create(0.5f, -1.0f, -0.0f, float.NaN);
        var f256 = Vector256.Create(f128, Vector128.Create(2.3f, -0.5f, float.Epsilon, float.PositiveInfinity));
        var s256 = Vector256.Sin(f256); var c256 = Vector256.Cos(f256); var p256 = Vector256.SinCos(f256);
        for (int i = 0; i < 8; ++i)
            TrigLaneF("v256f." + i, f256.GetElement(i), s256.GetElement(i), c256.GetElement(i), p256.Sin.GetElement(i), p256.Cos.GetElement(i));
        var f512 = Vector512.Create(f256, f256);
        var s512 = Vector512.Sin(f512); var c512 = Vector512.Cos(f512); var p512 = Vector512.SinCos(f512);
        for (int i = 0; i < 16; ++i)
            TrigLaneF("v512f." + i, f512.GetElement(i), s512.GetElement(i), c512.GetElement(i), p512.Sin.GetElement(i), p512.Cos.GetElement(i));
        float[] nfValues = new float[Vector<float>.Count];
        for (int i = 0; i < nfValues.Length; ++i) nfValues[i] = f128.GetElement(i % 4);
        var nf = new Vector<float>(nfValues);
        var nsf = Vector.Sin(nf); var ncf = Vector.Cos(nf); var npf = Vector.SinCos(nf);
        // The CLR can have a wider Numerics Vector; print only its common prefix.
        for (int i = 0; i < 4; ++i) TrigLaneF("numericsf." + i, nf[i], nsf[i], ncf[i], npf.Sin[i], npf.Cos[i]);

        var d64 = Vector64.Create(-1.0);
        var ds64 = Vector64.Sin(d64); var dc64 = Vector64.Cos(d64); var dp64 = Vector64.SinCos(d64);
        TrigLaneD("v64d.0", d64.GetElement(0), ds64.GetElement(0), dc64.GetElement(0), dp64.Sin.GetElement(0), dp64.Cos.GetElement(0));
        var d256 = Vector256.Create(Vector128.Create(0.5, -0.0), Vector128.Create(-1.0, double.NaN));
        var ds256 = Vector256.Sin(d256); var dc256 = Vector256.Cos(d256); var dp256 = Vector256.SinCos(d256);
        for (int i = 0; i < 4; ++i)
            TrigLaneD("v256d." + i, d256.GetElement(i), ds256.GetElement(i), dc256.GetElement(i), dp256.Sin.GetElement(i), dp256.Cos.GetElement(i));
        var d512 = Vector512.Create(d256, Vector256.Create(Vector128.Create(2.3, -0.5), Vector128.Create(double.Epsilon, double.NegativeInfinity)));
        var ds512 = Vector512.Sin(d512); var dc512 = Vector512.Cos(d512); var dp512 = Vector512.SinCos(d512);
        for (int i = 0; i < 8; ++i)
            TrigLaneD("v512d." + i, d512.GetElement(i), ds512.GetElement(i), dc512.GetElement(i), dp512.Sin.GetElement(i), dp512.Cos.GetElement(i));
        double[] ndValues = new double[Vector<double>.Count];
        for (int i = 0; i < ndValues.Length; ++i) ndValues[i] = d256.GetElement(i % 4);
        var nd = new Vector<double>(ndValues);
        var nsd = Vector.Sin(nd); var ncd = Vector.Cos(nd); var npd = Vector.SinCos(nd);
        for (int i = 0; i < 2; ++i) TrigLaneD("numericsd." + i, nd[i], nsd[i], ncd[i], npd.Sin[i], npd.Cos[i]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector4 TrigInput()
    {
        ++_trigEvaluations;
        return new Vector4(1.0f, -0.0f, -0.5f, 2.3f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (Vector4 Sin, Vector4 Cos) TrigReturnedPair(Vector4 input) => Vector4.SinCos(input);

    private static void TrigLaneF(string label, float input, float sin, float cos, float pairSin, float pairCos)
    {
        TrigF(label + ".sin", input, sin, true); TrigF(label + ".cos", input, cos, false);
        TrigF(label + ".pair-sin", input, pairSin, true); TrigF(label + ".pair-cos", input, pairCos, false);
    }

    private static void TrigLaneD(string label, double input, double sin, double cos, double pairSin, double pairCos)
    {
        TrigD(label + ".sin", input, sin, true); TrigD(label + ".cos", input, cos, false);
        TrigD(label + ".pair-sin", input, pairSin, true); TrigD(label + ".pair-cos", input, pairCos, false);
    }

    private static void TrigF(string label, float input, float actual, bool sine)
    {
        bool same;
        if (!float.IsFinite(input)) same = float.IsNaN(actual);
        else if (MathF.Abs(input) <= BitConverter.Int32BitsToSingle(0x00800000))
            same = BitConverter.SingleToInt32Bits(actual) == BitConverter.SingleToInt32Bits(sine ? input : 1.0f);
        else
        {
            // These are corpus acceptance thresholds, not a universal libm bound.
            float expected = input == 1.0f ? (sine ? 0.8414709848078965f : 0.5403023058681397f)
                : sine ? MathF.Sin(input) : MathF.Cos(input);
            same = float.IsFinite(actual) && MathF.Abs(actual) <= 1.0f && MathF.Abs(actual - expected) <= 1e-6f;
        }
        if (!same) throw new InvalidOperationException(label);
        ++_trigChecks;
        Console.WriteLine(_trigRecords ? "f:" + label + ":" + BitConverter.SingleToInt32Bits(input).ToString("X8")
            + ":" + BitConverter.SingleToInt32Bits(actual).ToString("X8") : label + "=ok");
    }

    private static void TrigD(string label, double input, double actual, bool sine)
    {
        bool same;
        if (!double.IsFinite(input)) same = double.IsNaN(actual);
        else if (Math.Abs(input) <= BitConverter.Int64BitsToDouble(0x0010000000000000L))
            same = BitConverter.DoubleToInt64Bits(actual) == BitConverter.DoubleToInt64Bits(sine ? input : 1.0);
        else
        {
            double expected = input == 1.0 ? (sine ? 0.84147098480789650665 : 0.54030230586813971740)
                : sine ? Math.Sin(input) : Math.Cos(input);
            same = double.IsFinite(actual) && Math.Abs(actual) <= 1.0 && Math.Abs(actual - expected) <= 1e-14;
        }
        if (!same) throw new InvalidOperationException(label);
        ++_trigChecks;
        Console.WriteLine(_trigRecords ? "d:" + label + ":" + BitConverter.DoubleToInt64Bits(input).ToString("X16")
            + ":" + BitConverter.DoubleToInt64Bits(actual).ToString("X16") : label + "=ok");
    }
}
