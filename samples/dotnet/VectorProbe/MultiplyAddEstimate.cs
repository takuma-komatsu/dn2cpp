using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace VectorProbe;

internal static partial class Program
{
    private static int EstimateOrder;
    private static int EstimateThrowAt;

    private static void EstimateCheck(bool condition)
    {
        if (!condition) throw new InvalidOperationException("vector estimate mismatch");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void EstimateEffect(int step)
    {
        EstimateOrder = EstimateOrder * 10 + step;
        if (EstimateThrowAt == step) throw new InvalidOperationException("estimate operand");
    }

    private static bool EstimateSame(float a, float b) =>
        BitConverter.SingleToInt32Bits(a) == BitConverter.SingleToInt32Bits(b) || float.IsNaN(a) && float.IsNaN(b);
    private static bool EstimateSame(double a, double b) =>
        BitConverter.DoubleToInt64Bits(a) == BitConverter.DoubleToInt64Bits(b) || double.IsNaN(a) && double.IsNaN(b);

    // Inputs are loaded by bits so negative zero and NaN payloads survive C# literals.
    private static float[] EstimateSingles(uint[] bits)
    {
        float[] values = new float[bits.Length];
        for (int i = 0; i < bits.Length; i++) values[i] = BitConverter.Int32BitsToSingle(unchecked((int)bits[i]));
        return values;
    }
    private static double[] EstimateDoubles(ulong[] bits)
    {
        double[] values = new double[bits.Length];
        for (int i = 0; i < bits.Length; i++) values[i] = BitConverter.Int64BitsToDouble(unchecked((long)bits[i]));
        return values;
    }

    private static void MultiplyAddEstimates(bool raw = false)
    {
        Console.WriteLine("-- vector multiply add estimate --");
        var fa = EstimateSingles(new uint[] { 0x40000000U, 0x3f800001U, 0x7f7fffffU, 0x00000001U, 0x00800000U, 0x80000000U, 0x80000000U, 0x00000000U, 0x00000000U, 0x7f800000U, 0x7f800000U, 0x7f800000U, 0x7fc12345U, 0x3f800000U, 0x3f800000U, 0x7f812345U, 0x3f800000U, 0x3f800001U, 0x00800000U, 0x80800000U });
        var fb = EstimateSingles(new uint[] { 0x40400000U, 0x3f7ffffeU, 0x40000000U, 0x3f000000U, 0x3f000000U, 0x3f800000U, 0x3f800000U, 0xbf800000U, 0x3f800000U, 0x00000000U, 0x3f800000U, 0xbf800000U, 0x3f800000U, 0xffc54321U, 0x3f800000U, 0x3f800000U, 0x3f800000U, 0x3f800000U, 0x3f7fffffU, 0x3f000000U });
        var fc = EstimateSingles(new uint[] { 0x40800000U, 0xbf800000U, 0xff7fffffU, 0x00000001U, 0x00000000U, 0x80000000U, 0x00000000U, 0x80000000U, 0x00000000U, 0x3f800000U, 0xff800000U, 0x00000000U, 0x00000000U, 0x00000000U, 0x7f812345U, 0xffc54321U, 0x33800000U, 0x33800000U, 0x80000001U, 0x80000001U });
        var da = EstimateDoubles(new ulong[] { 0x4000000000000000UL, 0x3ff0000000000001UL, 0x7fefffffffffffffUL, 0x0000000000000001UL, 0x0010000000000000UL, 0x8000000000000000UL, 0x8000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x7ff0000000000000UL, 0x7ff0000000000000UL, 0x7ff0000000000000UL, 0x7ff8123456789abcUL, 0x3ff0000000000000UL, 0x3ff0000000000000UL, 0x7ff0123456789abcUL, 0x3ff0000000000000UL, 0x3ff0000000000001UL, 0x0010000000000000UL, 0x8010000000000000UL });
        var db = EstimateDoubles(new ulong[] { 0x4008000000000000UL, 0x3feffffffffffffeUL, 0x4000000000000000UL, 0x3fe0000000000000UL, 0x3fe0000000000000UL, 0x3ff0000000000000UL, 0x3ff0000000000000UL, 0xbff0000000000000UL, 0x3ff0000000000000UL, 0x0000000000000000UL, 0x3ff0000000000000UL, 0xbff0000000000000UL, 0x3ff0000000000000UL, 0xfff8543212345678UL, 0x3ff0000000000000UL, 0x3ff0000000000000UL, 0x3ff0000000000000UL, 0x3ff0000000000000UL, 0x3fefffffffffffffUL, 0x3fe0000000000000UL });
        var dc = EstimateDoubles(new ulong[] { 0x4010000000000000UL, 0xbff0000000000000UL, 0xffefffffffffffffUL, 0x0000000000000001UL, 0x0000000000000000UL, 0x8000000000000000UL, 0x0000000000000000UL, 0x8000000000000000UL, 0x0000000000000000UL, 0x3ff0000000000000UL, 0xfff0000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x7ff0123456789abcUL, 0xfff8543212345678UL, 0x3ca0000000000000UL, 0x3ca0000000000000UL, 0x8000000000000001UL, 0x8000000000000001UL });
        Estimate64f(fa, fb, fc, raw);
        Estimate64d(da, db, dc, raw);
        Estimate128f(fa, fb, fc, raw);
        Estimate128d(da, db, dc, raw);
        Estimate256f(fa, fb, fc, raw);
        Estimate256d(da, db, dc, raw);
        Estimate512f(fa, fb, fc, raw);
        Estimate512d(da, db, dc, raw);
        EstimateNf(fa, fb, fc, raw);
        EstimateNd(da, db, dc, raw);
        Console.WriteLine("vector estimate ten direct delegate calli lane and order targets passed");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<float> Estimate64fOperand(int step, Vector64<float> value)
    { EstimateEffect(step); return value; }

    private static unsafe void Estimate64f(float[] a, float[] b, float[] c, bool raw)
    {
        Func<Vector64<float>, Vector64<float>, Vector64<float>, Vector64<float>> callback = Vector64.MultiplyAddEstimate;
        delegate* managed<Vector64<float>, Vector64<float>, Vector64<float>, Vector64<float>> pointer = &Vector64.MultiplyAddEstimate;
        float[] baseline = new float[a.Length];
        for (int test = 0; test < a.Length; test++)
        {
            var av = Vector64.Create(a[test]);
            var bv = Vector64.Create(b[test]);
            var cv = Vector64.Create(c[test]);
            var direct = Vector64.MultiplyAddEstimate(av, bv, cv);
            var delegated = callback(av, bv, cv);
            var indirect = pointer(av, bv, cv);
            baseline[test] = direct.GetElement(0);
            for (int i = 0; i < Vector64<float>.Count; i++)
            {
                EstimateCheck(EstimateSame(direct.GetElement(i), baseline[test]));
                EstimateCheck(EstimateSame(delegated.GetElement(i), baseline[test]));
                EstimateCheck(EstimateSame(indirect.GetElement(i), baseline[test]));
            }
            if (raw) Console.WriteLine("64f:" + test + ":" + BitConverter.SingleToInt32Bits(baseline[test]).ToString("X8"));
        }
        EstimateCheck(baseline[0] == 10);
        for (int rotation = 0; rotation < a.Length; rotation++)
        {
            float[] aa = new float[Vector64<float>.Count];
            float[] bb = new float[Vector64<float>.Count];
            float[] cc = new float[Vector64<float>.Count];
            for (int i = 0; i < Vector64<float>.Count; i++)
            {
                int test = (rotation + i) % a.Length;
                aa[i] = a[test]; bb[i] = b[test]; cc[i] = c[test];
            }
            var result = Vector64.MultiplyAddEstimate(Vector64.Create(aa), Vector64.Create(bb), Vector64.Create(cc));
            for (int i = 0; i < Vector64<float>.Count; i++)
                EstimateCheck(EstimateSame(result.GetElement(i), baseline[(rotation + i) % a.Length]));
        }
        for (int fail = 0; fail <= 3; fail++)
        {
            EstimateOrder = 0; EstimateThrowAt = fail;
            bool caught = false;
            try
            {
                var result = Vector64.MultiplyAddEstimate(
                    Estimate64fOperand(1, Vector64.Create(a[0])),
                    Estimate64fOperand(2, Vector64.Create(b[0])),
                    Estimate64fOperand(3, Vector64.Create(c[0])));
                EstimateCheck(result.GetElement(0) == 10);
            }
            catch (InvalidOperationException error) when (error.Message == "estimate operand") { caught = true; }
            EstimateCheck(caught == (fail != 0));
            EstimateCheck(EstimateOrder == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        EstimateThrowAt = 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<double> Estimate64dOperand(int step, Vector64<double> value)
    { EstimateEffect(step); return value; }

    private static unsafe void Estimate64d(double[] a, double[] b, double[] c, bool raw)
    {
        Func<Vector64<double>, Vector64<double>, Vector64<double>, Vector64<double>> callback = Vector64.MultiplyAddEstimate;
        delegate* managed<Vector64<double>, Vector64<double>, Vector64<double>, Vector64<double>> pointer = &Vector64.MultiplyAddEstimate;
        double[] baseline = new double[a.Length];
        for (int test = 0; test < a.Length; test++)
        {
            var av = Vector64.Create(a[test]);
            var bv = Vector64.Create(b[test]);
            var cv = Vector64.Create(c[test]);
            var direct = Vector64.MultiplyAddEstimate(av, bv, cv);
            var delegated = callback(av, bv, cv);
            var indirect = pointer(av, bv, cv);
            baseline[test] = direct.GetElement(0);
            for (int i = 0; i < Vector64<double>.Count; i++)
            {
                EstimateCheck(EstimateSame(direct.GetElement(i), baseline[test]));
                EstimateCheck(EstimateSame(delegated.GetElement(i), baseline[test]));
                EstimateCheck(EstimateSame(indirect.GetElement(i), baseline[test]));
            }
            if (raw) Console.WriteLine("64d:" + test + ":" + BitConverter.DoubleToInt64Bits(baseline[test]).ToString("X16"));
        }
        EstimateCheck(baseline[0] == 10);
        for (int rotation = 0; rotation < a.Length; rotation++)
        {
            double[] aa = new double[Vector64<double>.Count];
            double[] bb = new double[Vector64<double>.Count];
            double[] cc = new double[Vector64<double>.Count];
            for (int i = 0; i < Vector64<double>.Count; i++)
            {
                int test = (rotation + i) % a.Length;
                aa[i] = a[test]; bb[i] = b[test]; cc[i] = c[test];
            }
            var result = Vector64.MultiplyAddEstimate(Vector64.Create(aa), Vector64.Create(bb), Vector64.Create(cc));
            for (int i = 0; i < Vector64<double>.Count; i++)
                EstimateCheck(EstimateSame(result.GetElement(i), baseline[(rotation + i) % a.Length]));
        }
        for (int fail = 0; fail <= 3; fail++)
        {
            EstimateOrder = 0; EstimateThrowAt = fail;
            bool caught = false;
            try
            {
                var result = Vector64.MultiplyAddEstimate(
                    Estimate64dOperand(1, Vector64.Create(a[0])),
                    Estimate64dOperand(2, Vector64.Create(b[0])),
                    Estimate64dOperand(3, Vector64.Create(c[0])));
                EstimateCheck(result.GetElement(0) == 10);
            }
            catch (InvalidOperationException error) when (error.Message == "estimate operand") { caught = true; }
            EstimateCheck(caught == (fail != 0));
            EstimateCheck(EstimateOrder == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        EstimateThrowAt = 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> Estimate128fOperand(int step, Vector128<float> value)
    { EstimateEffect(step); return value; }

    private static unsafe void Estimate128f(float[] a, float[] b, float[] c, bool raw)
    {
        Func<Vector128<float>, Vector128<float>, Vector128<float>, Vector128<float>> callback = Vector128.MultiplyAddEstimate;
        delegate* managed<Vector128<float>, Vector128<float>, Vector128<float>, Vector128<float>> pointer = &Vector128.MultiplyAddEstimate;
        float[] baseline = new float[a.Length];
        for (int test = 0; test < a.Length; test++)
        {
            var av = Vector128.Create(a[test]);
            var bv = Vector128.Create(b[test]);
            var cv = Vector128.Create(c[test]);
            var direct = Vector128.MultiplyAddEstimate(av, bv, cv);
            var delegated = callback(av, bv, cv);
            var indirect = pointer(av, bv, cv);
            baseline[test] = direct.GetElement(0);
            for (int i = 0; i < Vector128<float>.Count; i++)
            {
                EstimateCheck(EstimateSame(direct.GetElement(i), baseline[test]));
                EstimateCheck(EstimateSame(delegated.GetElement(i), baseline[test]));
                EstimateCheck(EstimateSame(indirect.GetElement(i), baseline[test]));
            }
            if (raw) Console.WriteLine("128f:" + test + ":" + BitConverter.SingleToInt32Bits(baseline[test]).ToString("X8"));
        }
        EstimateCheck(baseline[0] == 10);
        for (int rotation = 0; rotation < a.Length; rotation++)
        {
            float[] aa = new float[Vector128<float>.Count];
            float[] bb = new float[Vector128<float>.Count];
            float[] cc = new float[Vector128<float>.Count];
            for (int i = 0; i < Vector128<float>.Count; i++)
            {
                int test = (rotation + i) % a.Length;
                aa[i] = a[test]; bb[i] = b[test]; cc[i] = c[test];
            }
            var result = Vector128.MultiplyAddEstimate(Vector128.Create(aa), Vector128.Create(bb), Vector128.Create(cc));
            for (int i = 0; i < Vector128<float>.Count; i++)
                EstimateCheck(EstimateSame(result.GetElement(i), baseline[(rotation + i) % a.Length]));
        }
        for (int fail = 0; fail <= 3; fail++)
        {
            EstimateOrder = 0; EstimateThrowAt = fail;
            bool caught = false;
            try
            {
                var result = Vector128.MultiplyAddEstimate(
                    Estimate128fOperand(1, Vector128.Create(a[0])),
                    Estimate128fOperand(2, Vector128.Create(b[0])),
                    Estimate128fOperand(3, Vector128.Create(c[0])));
                EstimateCheck(result.GetElement(0) == 10);
            }
            catch (InvalidOperationException error) when (error.Message == "estimate operand") { caught = true; }
            EstimateCheck(caught == (fail != 0));
            EstimateCheck(EstimateOrder == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        EstimateThrowAt = 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<double> Estimate128dOperand(int step, Vector128<double> value)
    { EstimateEffect(step); return value; }

    private static unsafe void Estimate128d(double[] a, double[] b, double[] c, bool raw)
    {
        Func<Vector128<double>, Vector128<double>, Vector128<double>, Vector128<double>> callback = Vector128.MultiplyAddEstimate;
        delegate* managed<Vector128<double>, Vector128<double>, Vector128<double>, Vector128<double>> pointer = &Vector128.MultiplyAddEstimate;
        double[] baseline = new double[a.Length];
        for (int test = 0; test < a.Length; test++)
        {
            var av = Vector128.Create(a[test]);
            var bv = Vector128.Create(b[test]);
            var cv = Vector128.Create(c[test]);
            var direct = Vector128.MultiplyAddEstimate(av, bv, cv);
            var delegated = callback(av, bv, cv);
            var indirect = pointer(av, bv, cv);
            baseline[test] = direct.GetElement(0);
            for (int i = 0; i < Vector128<double>.Count; i++)
            {
                EstimateCheck(EstimateSame(direct.GetElement(i), baseline[test]));
                EstimateCheck(EstimateSame(delegated.GetElement(i), baseline[test]));
                EstimateCheck(EstimateSame(indirect.GetElement(i), baseline[test]));
            }
            if (raw) Console.WriteLine("128d:" + test + ":" + BitConverter.DoubleToInt64Bits(baseline[test]).ToString("X16"));
        }
        EstimateCheck(baseline[0] == 10);
        for (int rotation = 0; rotation < a.Length; rotation++)
        {
            double[] aa = new double[Vector128<double>.Count];
            double[] bb = new double[Vector128<double>.Count];
            double[] cc = new double[Vector128<double>.Count];
            for (int i = 0; i < Vector128<double>.Count; i++)
            {
                int test = (rotation + i) % a.Length;
                aa[i] = a[test]; bb[i] = b[test]; cc[i] = c[test];
            }
            var result = Vector128.MultiplyAddEstimate(Vector128.Create(aa), Vector128.Create(bb), Vector128.Create(cc));
            for (int i = 0; i < Vector128<double>.Count; i++)
                EstimateCheck(EstimateSame(result.GetElement(i), baseline[(rotation + i) % a.Length]));
        }
        for (int fail = 0; fail <= 3; fail++)
        {
            EstimateOrder = 0; EstimateThrowAt = fail;
            bool caught = false;
            try
            {
                var result = Vector128.MultiplyAddEstimate(
                    Estimate128dOperand(1, Vector128.Create(a[0])),
                    Estimate128dOperand(2, Vector128.Create(b[0])),
                    Estimate128dOperand(3, Vector128.Create(c[0])));
                EstimateCheck(result.GetElement(0) == 10);
            }
            catch (InvalidOperationException error) when (error.Message == "estimate operand") { caught = true; }
            EstimateCheck(caught == (fail != 0));
            EstimateCheck(EstimateOrder == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        EstimateThrowAt = 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<float> Estimate256fOperand(int step, Vector256<float> value)
    { EstimateEffect(step); return value; }

    private static unsafe void Estimate256f(float[] a, float[] b, float[] c, bool raw)
    {
        Func<Vector256<float>, Vector256<float>, Vector256<float>, Vector256<float>> callback = Vector256.MultiplyAddEstimate;
        delegate* managed<Vector256<float>, Vector256<float>, Vector256<float>, Vector256<float>> pointer = &Vector256.MultiplyAddEstimate;
        float[] baseline = new float[a.Length];
        for (int test = 0; test < a.Length; test++)
        {
            var av = Vector256.Create(a[test]);
            var bv = Vector256.Create(b[test]);
            var cv = Vector256.Create(c[test]);
            var direct = Vector256.MultiplyAddEstimate(av, bv, cv);
            var delegated = callback(av, bv, cv);
            var indirect = pointer(av, bv, cv);
            baseline[test] = direct.GetElement(0);
            for (int i = 0; i < Vector256<float>.Count; i++)
            {
                EstimateCheck(EstimateSame(direct.GetElement(i), baseline[test]));
                EstimateCheck(EstimateSame(delegated.GetElement(i), baseline[test]));
                EstimateCheck(EstimateSame(indirect.GetElement(i), baseline[test]));
            }
            if (raw) Console.WriteLine("256f:" + test + ":" + BitConverter.SingleToInt32Bits(baseline[test]).ToString("X8"));
        }
        EstimateCheck(baseline[0] == 10);
        for (int rotation = 0; rotation < a.Length; rotation++)
        {
            float[] aa = new float[Vector256<float>.Count];
            float[] bb = new float[Vector256<float>.Count];
            float[] cc = new float[Vector256<float>.Count];
            for (int i = 0; i < Vector256<float>.Count; i++)
            {
                int test = (rotation + i) % a.Length;
                aa[i] = a[test]; bb[i] = b[test]; cc[i] = c[test];
            }
            var result = Vector256.MultiplyAddEstimate(Vector256.Create(aa), Vector256.Create(bb), Vector256.Create(cc));
            for (int i = 0; i < Vector256<float>.Count; i++)
                EstimateCheck(EstimateSame(result.GetElement(i), baseline[(rotation + i) % a.Length]));
        }
        for (int fail = 0; fail <= 3; fail++)
        {
            EstimateOrder = 0; EstimateThrowAt = fail;
            bool caught = false;
            try
            {
                var result = Vector256.MultiplyAddEstimate(
                    Estimate256fOperand(1, Vector256.Create(a[0])),
                    Estimate256fOperand(2, Vector256.Create(b[0])),
                    Estimate256fOperand(3, Vector256.Create(c[0])));
                EstimateCheck(result.GetElement(0) == 10);
            }
            catch (InvalidOperationException error) when (error.Message == "estimate operand") { caught = true; }
            EstimateCheck(caught == (fail != 0));
            EstimateCheck(EstimateOrder == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        EstimateThrowAt = 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector256<double> Estimate256dOperand(int step, Vector256<double> value)
    { EstimateEffect(step); return value; }

    private static unsafe void Estimate256d(double[] a, double[] b, double[] c, bool raw)
    {
        Func<Vector256<double>, Vector256<double>, Vector256<double>, Vector256<double>> callback = Vector256.MultiplyAddEstimate;
        delegate* managed<Vector256<double>, Vector256<double>, Vector256<double>, Vector256<double>> pointer = &Vector256.MultiplyAddEstimate;
        double[] baseline = new double[a.Length];
        for (int test = 0; test < a.Length; test++)
        {
            var av = Vector256.Create(a[test]);
            var bv = Vector256.Create(b[test]);
            var cv = Vector256.Create(c[test]);
            var direct = Vector256.MultiplyAddEstimate(av, bv, cv);
            var delegated = callback(av, bv, cv);
            var indirect = pointer(av, bv, cv);
            baseline[test] = direct.GetElement(0);
            for (int i = 0; i < Vector256<double>.Count; i++)
            {
                EstimateCheck(EstimateSame(direct.GetElement(i), baseline[test]));
                EstimateCheck(EstimateSame(delegated.GetElement(i), baseline[test]));
                EstimateCheck(EstimateSame(indirect.GetElement(i), baseline[test]));
            }
            if (raw) Console.WriteLine("256d:" + test + ":" + BitConverter.DoubleToInt64Bits(baseline[test]).ToString("X16"));
        }
        EstimateCheck(baseline[0] == 10);
        for (int rotation = 0; rotation < a.Length; rotation++)
        {
            double[] aa = new double[Vector256<double>.Count];
            double[] bb = new double[Vector256<double>.Count];
            double[] cc = new double[Vector256<double>.Count];
            for (int i = 0; i < Vector256<double>.Count; i++)
            {
                int test = (rotation + i) % a.Length;
                aa[i] = a[test]; bb[i] = b[test]; cc[i] = c[test];
            }
            var result = Vector256.MultiplyAddEstimate(Vector256.Create(aa), Vector256.Create(bb), Vector256.Create(cc));
            for (int i = 0; i < Vector256<double>.Count; i++)
                EstimateCheck(EstimateSame(result.GetElement(i), baseline[(rotation + i) % a.Length]));
        }
        for (int fail = 0; fail <= 3; fail++)
        {
            EstimateOrder = 0; EstimateThrowAt = fail;
            bool caught = false;
            try
            {
                var result = Vector256.MultiplyAddEstimate(
                    Estimate256dOperand(1, Vector256.Create(a[0])),
                    Estimate256dOperand(2, Vector256.Create(b[0])),
                    Estimate256dOperand(3, Vector256.Create(c[0])));
                EstimateCheck(result.GetElement(0) == 10);
            }
            catch (InvalidOperationException error) when (error.Message == "estimate operand") { caught = true; }
            EstimateCheck(caught == (fail != 0));
            EstimateCheck(EstimateOrder == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        EstimateThrowAt = 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<float> Estimate512fOperand(int step, Vector512<float> value)
    { EstimateEffect(step); return value; }

    private static unsafe void Estimate512f(float[] a, float[] b, float[] c, bool raw)
    {
        Func<Vector512<float>, Vector512<float>, Vector512<float>, Vector512<float>> callback = Vector512.MultiplyAddEstimate;
        delegate* managed<Vector512<float>, Vector512<float>, Vector512<float>, Vector512<float>> pointer = &Vector512.MultiplyAddEstimate;
        float[] baseline = new float[a.Length];
        for (int test = 0; test < a.Length; test++)
        {
            var av = Vector512.Create(a[test]);
            var bv = Vector512.Create(b[test]);
            var cv = Vector512.Create(c[test]);
            var direct = Vector512.MultiplyAddEstimate(av, bv, cv);
            var delegated = callback(av, bv, cv);
            var indirect = pointer(av, bv, cv);
            baseline[test] = direct.GetElement(0);
            for (int i = 0; i < Vector512<float>.Count; i++)
            {
                EstimateCheck(EstimateSame(direct.GetElement(i), baseline[test]));
                EstimateCheck(EstimateSame(delegated.GetElement(i), baseline[test]));
                EstimateCheck(EstimateSame(indirect.GetElement(i), baseline[test]));
            }
            if (raw) Console.WriteLine("512f:" + test + ":" + BitConverter.SingleToInt32Bits(baseline[test]).ToString("X8"));
        }
        EstimateCheck(baseline[0] == 10);
        for (int rotation = 0; rotation < a.Length; rotation++)
        {
            float[] aa = new float[Vector512<float>.Count];
            float[] bb = new float[Vector512<float>.Count];
            float[] cc = new float[Vector512<float>.Count];
            for (int i = 0; i < Vector512<float>.Count; i++)
            {
                int test = (rotation + i) % a.Length;
                aa[i] = a[test]; bb[i] = b[test]; cc[i] = c[test];
            }
            var result = Vector512.MultiplyAddEstimate(Vector512.Create(aa), Vector512.Create(bb), Vector512.Create(cc));
            for (int i = 0; i < Vector512<float>.Count; i++)
                EstimateCheck(EstimateSame(result.GetElement(i), baseline[(rotation + i) % a.Length]));
        }
        for (int fail = 0; fail <= 3; fail++)
        {
            EstimateOrder = 0; EstimateThrowAt = fail;
            bool caught = false;
            try
            {
                var result = Vector512.MultiplyAddEstimate(
                    Estimate512fOperand(1, Vector512.Create(a[0])),
                    Estimate512fOperand(2, Vector512.Create(b[0])),
                    Estimate512fOperand(3, Vector512.Create(c[0])));
                EstimateCheck(result.GetElement(0) == 10);
            }
            catch (InvalidOperationException error) when (error.Message == "estimate operand") { caught = true; }
            EstimateCheck(caught == (fail != 0));
            EstimateCheck(EstimateOrder == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        EstimateThrowAt = 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<double> Estimate512dOperand(int step, Vector512<double> value)
    { EstimateEffect(step); return value; }

    private static unsafe void Estimate512d(double[] a, double[] b, double[] c, bool raw)
    {
        Func<Vector512<double>, Vector512<double>, Vector512<double>, Vector512<double>> callback = Vector512.MultiplyAddEstimate;
        delegate* managed<Vector512<double>, Vector512<double>, Vector512<double>, Vector512<double>> pointer = &Vector512.MultiplyAddEstimate;
        double[] baseline = new double[a.Length];
        for (int test = 0; test < a.Length; test++)
        {
            var av = Vector512.Create(a[test]);
            var bv = Vector512.Create(b[test]);
            var cv = Vector512.Create(c[test]);
            var direct = Vector512.MultiplyAddEstimate(av, bv, cv);
            var delegated = callback(av, bv, cv);
            var indirect = pointer(av, bv, cv);
            baseline[test] = direct.GetElement(0);
            for (int i = 0; i < Vector512<double>.Count; i++)
            {
                EstimateCheck(EstimateSame(direct.GetElement(i), baseline[test]));
                EstimateCheck(EstimateSame(delegated.GetElement(i), baseline[test]));
                EstimateCheck(EstimateSame(indirect.GetElement(i), baseline[test]));
            }
            if (raw) Console.WriteLine("512d:" + test + ":" + BitConverter.DoubleToInt64Bits(baseline[test]).ToString("X16"));
        }
        EstimateCheck(baseline[0] == 10);
        for (int rotation = 0; rotation < a.Length; rotation++)
        {
            double[] aa = new double[Vector512<double>.Count];
            double[] bb = new double[Vector512<double>.Count];
            double[] cc = new double[Vector512<double>.Count];
            for (int i = 0; i < Vector512<double>.Count; i++)
            {
                int test = (rotation + i) % a.Length;
                aa[i] = a[test]; bb[i] = b[test]; cc[i] = c[test];
            }
            var result = Vector512.MultiplyAddEstimate(Vector512.Create(aa), Vector512.Create(bb), Vector512.Create(cc));
            for (int i = 0; i < Vector512<double>.Count; i++)
                EstimateCheck(EstimateSame(result.GetElement(i), baseline[(rotation + i) % a.Length]));
        }
        for (int fail = 0; fail <= 3; fail++)
        {
            EstimateOrder = 0; EstimateThrowAt = fail;
            bool caught = false;
            try
            {
                var result = Vector512.MultiplyAddEstimate(
                    Estimate512dOperand(1, Vector512.Create(a[0])),
                    Estimate512dOperand(2, Vector512.Create(b[0])),
                    Estimate512dOperand(3, Vector512.Create(c[0])));
                EstimateCheck(result.GetElement(0) == 10);
            }
            catch (InvalidOperationException error) when (error.Message == "estimate operand") { caught = true; }
            EstimateCheck(caught == (fail != 0));
            EstimateCheck(EstimateOrder == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        EstimateThrowAt = 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<float> EstimateNfOperand(int step, Vector<float> value)
    { EstimateEffect(step); return value; }

    private static unsafe void EstimateNf(float[] a, float[] b, float[] c, bool raw)
    {
        Func<Vector<float>, Vector<float>, Vector<float>, Vector<float>> callback = Vector.MultiplyAddEstimate;
        delegate* managed<Vector<float>, Vector<float>, Vector<float>, Vector<float>> pointer = &Vector.MultiplyAddEstimate;
        float[] baseline = new float[a.Length];
        for (int test = 0; test < a.Length; test++)
        {
            var av = new Vector<float>(a[test]);
            var bv = new Vector<float>(b[test]);
            var cv = new Vector<float>(c[test]);
            var direct = Vector.MultiplyAddEstimate(av, bv, cv);
            var delegated = callback(av, bv, cv);
            var indirect = pointer(av, bv, cv);
            baseline[test] = direct[0];
            for (int i = 0; i < Vector<float>.Count; i++)
            {
                EstimateCheck(EstimateSame(direct[i], baseline[test]));
                EstimateCheck(EstimateSame(delegated[i], baseline[test]));
                EstimateCheck(EstimateSame(indirect[i], baseline[test]));
            }
            if (raw) Console.WriteLine("Nf:" + test + ":" + BitConverter.SingleToInt32Bits(baseline[test]).ToString("X8"));
        }
        EstimateCheck(baseline[0] == 10);
        for (int rotation = 0; rotation < a.Length; rotation++)
        {
            float[] aa = new float[Vector<float>.Count];
            float[] bb = new float[Vector<float>.Count];
            float[] cc = new float[Vector<float>.Count];
            for (int i = 0; i < Vector<float>.Count; i++)
            {
                int test = (rotation + i) % a.Length;
                aa[i] = a[test]; bb[i] = b[test]; cc[i] = c[test];
            }
            var result = Vector.MultiplyAddEstimate(new Vector<float>(aa), new Vector<float>(bb), new Vector<float>(cc));
            for (int i = 0; i < Vector<float>.Count; i++)
                EstimateCheck(EstimateSame(result[i], baseline[(rotation + i) % a.Length]));
        }
        for (int fail = 0; fail <= 3; fail++)
        {
            EstimateOrder = 0; EstimateThrowAt = fail;
            bool caught = false;
            try
            {
                var result = Vector.MultiplyAddEstimate(
                    EstimateNfOperand(1, new Vector<float>(a[0])),
                    EstimateNfOperand(2, new Vector<float>(b[0])),
                    EstimateNfOperand(3, new Vector<float>(c[0])));
                EstimateCheck(result[0] == 10);
            }
            catch (InvalidOperationException error) when (error.Message == "estimate operand") { caught = true; }
            EstimateCheck(caught == (fail != 0));
            EstimateCheck(EstimateOrder == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        EstimateThrowAt = 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<double> EstimateNdOperand(int step, Vector<double> value)
    { EstimateEffect(step); return value; }

    private static unsafe void EstimateNd(double[] a, double[] b, double[] c, bool raw)
    {
        Func<Vector<double>, Vector<double>, Vector<double>, Vector<double>> callback = Vector.MultiplyAddEstimate;
        delegate* managed<Vector<double>, Vector<double>, Vector<double>, Vector<double>> pointer = &Vector.MultiplyAddEstimate;
        double[] baseline = new double[a.Length];
        for (int test = 0; test < a.Length; test++)
        {
            var av = new Vector<double>(a[test]);
            var bv = new Vector<double>(b[test]);
            var cv = new Vector<double>(c[test]);
            var direct = Vector.MultiplyAddEstimate(av, bv, cv);
            var delegated = callback(av, bv, cv);
            var indirect = pointer(av, bv, cv);
            baseline[test] = direct[0];
            for (int i = 0; i < Vector<double>.Count; i++)
            {
                EstimateCheck(EstimateSame(direct[i], baseline[test]));
                EstimateCheck(EstimateSame(delegated[i], baseline[test]));
                EstimateCheck(EstimateSame(indirect[i], baseline[test]));
            }
            if (raw) Console.WriteLine("Nd:" + test + ":" + BitConverter.DoubleToInt64Bits(baseline[test]).ToString("X16"));
        }
        EstimateCheck(baseline[0] == 10);
        for (int rotation = 0; rotation < a.Length; rotation++)
        {
            double[] aa = new double[Vector<double>.Count];
            double[] bb = new double[Vector<double>.Count];
            double[] cc = new double[Vector<double>.Count];
            for (int i = 0; i < Vector<double>.Count; i++)
            {
                int test = (rotation + i) % a.Length;
                aa[i] = a[test]; bb[i] = b[test]; cc[i] = c[test];
            }
            var result = Vector.MultiplyAddEstimate(new Vector<double>(aa), new Vector<double>(bb), new Vector<double>(cc));
            for (int i = 0; i < Vector<double>.Count; i++)
                EstimateCheck(EstimateSame(result[i], baseline[(rotation + i) % a.Length]));
        }
        for (int fail = 0; fail <= 3; fail++)
        {
            EstimateOrder = 0; EstimateThrowAt = fail;
            bool caught = false;
            try
            {
                var result = Vector.MultiplyAddEstimate(
                    EstimateNdOperand(1, new Vector<double>(a[0])),
                    EstimateNdOperand(2, new Vector<double>(b[0])),
                    EstimateNdOperand(3, new Vector<double>(c[0])));
                EstimateCheck(result[0] == 10);
            }
            catch (InvalidOperationException error) when (error.Message == "estimate operand") { caught = true; }
            EstimateCheck(caught == (fail != 0));
            EstimateCheck(EstimateOrder == (fail == 1 ? 1 : fail == 2 ? 12 : 123));
        }
        EstimateThrowAt = 0;
    }
}
