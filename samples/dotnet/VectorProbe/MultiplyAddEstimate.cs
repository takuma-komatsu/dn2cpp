using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace VectorProbe;

// Portable MultiplyAddEstimate and Lerp round each product before the addition.
// Stdout carries only results a fusing CLR shares with that profile; the cases
// that tell the two apart are checked against nonfused bits on stderr.
internal static partial class Program
{
    private static int EstimateOrder;
    private static int EstimateThrowAt;
    // Cancellation, overflow, underflow and the subnormal boundary.
    private static readonly int[] EstimateFusionCases = { 1, 2, 3, 18 };
    private static readonly uint[] EstimateUnfusedSingles = { 0, 0x7F800000, 1, 0x007FFFFF };
    private static readonly ulong[] EstimateUnfusedDoubles = { 0, 0x7FF0000000000000, 1, 0x000FFFFFFFFFFFFF };

    private static void EstimateCheck(bool condition)
    {
        if (!condition) throw new InvalidOperationException("vector estimate mismatch");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T EstimateOperand<T>(int step, T value)
    {
        EstimateOrder = EstimateOrder * 10 + step;
        if (EstimateThrowAt == step) throw new InvalidOperationException("estimate operand");
        return value;
    }

    private static string EstimateOrderText(Func<float> call)
    {
        string text = "";
        for (int fail = 0; fail <= 3; fail++)
        {
            EstimateOrder = 0;
            EstimateThrowAt = fail;
            try { text += " " + call(); }
            catch (InvalidOperationException error) when (error.Message == "estimate operand") { text += " throw"; }
            text += "@" + EstimateOrder;
        }
        EstimateThrowAt = 0;
        return text;
    }

    // Inputs are loaded by bits so negative zero and NaN payloads survive C# literals.
    private static float[] EstimateSingles(uint[] bits)
    {
        float[] values = new float[bits.Length];
        for (int i = 0; i < bits.Length; i++) values[i] = BitConverter.UInt32BitsToSingle(bits[i]);
        return values;
    }
    private static double[] EstimateDoubles(ulong[] bits)
    {
        double[] values = new double[bits.Length];
        for (int i = 0; i < bits.Length; i++) values[i] = BitConverter.UInt64BitsToDouble(bits[i]);
        return values;
    }

    // Each case runs in every lane position; lanes must not depend on neighbours.
    private static T[] EstimateLanes<T>(int count, T[] a, T[] b, T[] c, Func<T[], T[], T[], T[]> op, Func<T, T, bool> same)
    {
        var results = new T[a.Length];
        var seen = new bool[a.Length];
        for (int start = 0; start < a.Length; start++)
        {
            T[] aa = new T[count], bb = new T[count], cc = new T[count];
            for (int lane = 0; lane < count; lane++)
            {
                int k = (start + lane) % a.Length;
                aa[lane] = a[k]; bb[lane] = b[k]; cc[lane] = c[k];
            }
            T[] lanes = op(aa, bb, cc);
            for (int lane = 0; lane < count; lane++)
            {
                int k = (start + lane) % a.Length;
                if (seen[k]) EstimateCheck(same(results[k], lanes[lane]));
                results[k] = lanes[lane];
                seen[k] = true;
            }
        }
        return results;
    }

    private static bool EstimateSame(float a, float b) =>
        BitConverter.SingleToUInt32Bits(a) == BitConverter.SingleToUInt32Bits(b) || float.IsNaN(a) && float.IsNaN(b);
    private static bool EstimateSame(double a, double b) =>
        BitConverter.DoubleToUInt64Bits(a) == BitConverter.DoubleToUInt64Bits(b) || double.IsNaN(a) && double.IsNaN(b);

    private static bool EstimatePrint(string name, float[] results)
    {
        var text = new string[results.Length];
        bool unfused = true;
        for (int k = 0; k < results.Length; k++)
        {
            int fusion = Array.IndexOf(EstimateFusionCases, k);
            text[k] = float.IsNaN(results[k]) ? "NaN" : fusion >= 0 ? "*" : BitConverter.SingleToUInt32Bits(results[k]).ToString("X8");
            if (fusion >= 0) unfused &= BitConverter.SingleToUInt32Bits(results[k]) == EstimateUnfusedSingles[fusion];
        }
        Console.WriteLine("estimate " + name + "=" + string.Join(",", text));
        return unfused;
    }
    private static bool EstimatePrint(string name, double[] results)
    {
        var text = new string[results.Length];
        bool unfused = true;
        for (int k = 0; k < results.Length; k++)
        {
            int fusion = Array.IndexOf(EstimateFusionCases, k);
            text[k] = double.IsNaN(results[k]) ? "NaN" : fusion >= 0 ? "*" : BitConverter.DoubleToUInt64Bits(results[k]).ToString("X16");
            if (fusion >= 0) unfused &= BitConverter.DoubleToUInt64Bits(results[k]) == EstimateUnfusedDoubles[fusion];
        }
        Console.WriteLine("estimate " + name + "=" + string.Join(",", text));
        return unfused;
    }

    private static float[] Estimate128f(float[] a, float[] b, float[] c)
    {
        var result = new float[Vector128<float>.Count];
        Vector128.MultiplyAddEstimate(Vector128.Create(a), Vector128.Create(b), Vector128.Create(c)).CopyTo(result);
        return result;
    }
    private static double[] Estimate256d(double[] a, double[] b, double[] c)
    {
        var result = new double[Vector256<double>.Count];
        Vector256.MultiplyAddEstimate(Vector256.Create(a), Vector256.Create(b), Vector256.Create(c)).CopyTo(result);
        return result;
    }
    private static double[] EstimateNd(double[] a, double[] b, double[] c)
    {
        var result = new double[Vector<double>.Count];
        Vector.MultiplyAddEstimate(new Vector<double>(a), new Vector<double>(b), new Vector<double>(c)).CopyTo(result);
        return result;
    }

    private static void MultiplyAddEstimates()
    {
        Console.WriteLine("-- vector multiply add estimate --");
        var fa = EstimateSingles(new uint[] { 0x40000000U, 0x3f800001U, 0x7f7fffffU, 0x00000001U, 0x00800000U, 0x80000000U, 0x80000000U, 0x00000000U, 0x00000000U, 0x7f800000U, 0x7f800000U, 0x7f800000U, 0x7fc12345U, 0x3f800000U, 0x3f800000U, 0x7f812345U, 0x3f800000U, 0x3f800001U, 0x00800000U, 0x80800000U });
        var fb = EstimateSingles(new uint[] { 0x40400000U, 0x3f7ffffeU, 0x40000000U, 0x3f000000U, 0x3f000000U, 0x3f800000U, 0x3f800000U, 0xbf800000U, 0x3f800000U, 0x00000000U, 0x3f800000U, 0xbf800000U, 0x3f800000U, 0xffc54321U, 0x3f800000U, 0x3f800000U, 0x3f800000U, 0x3f800000U, 0x3f7fffffU, 0x3f000000U });
        var fc = EstimateSingles(new uint[] { 0x40800000U, 0xbf800000U, 0xff7fffffU, 0x00000001U, 0x00000000U, 0x80000000U, 0x00000000U, 0x80000000U, 0x00000000U, 0x3f800000U, 0xff800000U, 0x00000000U, 0x00000000U, 0x00000000U, 0x7f812345U, 0xffc54321U, 0x33800000U, 0x33800000U, 0x80000001U, 0x80000001U });
        var da = EstimateDoubles(new ulong[] { 0x4000000000000000UL, 0x3ff0000000000001UL, 0x7fefffffffffffffUL, 0x0000000000000001UL, 0x0010000000000000UL, 0x8000000000000000UL, 0x8000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x7ff0000000000000UL, 0x7ff0000000000000UL, 0x7ff0000000000000UL, 0x7ff8123456789abcUL, 0x3ff0000000000000UL, 0x3ff0000000000000UL, 0x7ff0123456789abcUL, 0x3ff0000000000000UL, 0x3ff0000000000001UL, 0x0010000000000000UL, 0x8010000000000000UL });
        var db = EstimateDoubles(new ulong[] { 0x4008000000000000UL, 0x3feffffffffffffeUL, 0x4000000000000000UL, 0x3fe0000000000000UL, 0x3fe0000000000000UL, 0x3ff0000000000000UL, 0x3ff0000000000000UL, 0xbff0000000000000UL, 0x3ff0000000000000UL, 0x0000000000000000UL, 0x3ff0000000000000UL, 0xbff0000000000000UL, 0x3ff0000000000000UL, 0xfff8543212345678UL, 0x3ff0000000000000UL, 0x3ff0000000000000UL, 0x3ff0000000000000UL, 0x3ff0000000000000UL, 0x3fefffffffffffffUL, 0x3fe0000000000000UL });
        var dc = EstimateDoubles(new ulong[] { 0x4010000000000000UL, 0xbff0000000000000UL, 0xffefffffffffffffUL, 0x0000000000000001UL, 0x0000000000000000UL, 0x8000000000000000UL, 0x0000000000000000UL, 0x8000000000000000UL, 0x0000000000000000UL, 0x3ff0000000000000UL, 0xfff0000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL, 0x7ff0123456789abcUL, 0xfff8543212345678UL, 0x3ca0000000000000UL, 0x3ca0000000000000UL, 0x8000000000000001UL, 0x8000000000000001UL });
        bool unfused = EstimatePrint("128f", EstimateLanes(Vector128<float>.Count, fa, fb, fc, Estimate128f, EstimateSame));
        unfused &= EstimatePrint("256d", EstimateLanes(Vector256<double>.Count, da, db, dc, Estimate256d, EstimateSame));
        unfused &= EstimatePrint("Nd", EstimateLanes(Vector<double>.Count, da, db, dc, EstimateNd, EstimateSame));

        Func<Vector128<float>, Vector128<float>, Vector128<float>, Vector128<float>> callback = Vector128.MultiplyAddEstimate;
        for (int k = 0; k < fa.Length; k++)
        {
            var av = Vector128.Create(fa[k]);
            var bv = Vector128.Create(fb[k]);
            var cv = Vector128.Create(fc[k]);
            EstimateCheck(EstimateSame(callback(av, bv, cv).GetElement(3), Vector128.MultiplyAddEstimate(av, bv, cv).GetElement(3)));
        }
        Console.WriteLine("estimate order=" + EstimateOrderText(() => Vector128.MultiplyAddEstimate(
            EstimateOperand(1, Vector128.Create(2f)), EstimateOperand(2, Vector128.Create(3f)),
            EstimateOperand(3, Vector128.Create(4f))).GetElement(0)));
        unfused &= VectorLerps();
        Console.Error.WriteLine(unfused ? "vector estimate nonfused profile passed" : "vector estimate fused profile observed");
        Console.WriteLine("vector multiply add estimate end");
    }

    // Lerp neither clamps the amount nor shortcuts an endpoint. Every product here
    // is exact, so a fused estimate gives the same bits.
    private static bool VectorLerps()
    {
        float[] a = { 2, 2, 2, 1, float.PositiveInfinity, 1, -0f, -0f, 0, float.NaN, 1 };
        float[] b = { 4, 4, 4, 3, 1, float.PositiveInfinity, -0f, -0f, -0f, 1, float.PositiveInfinity };
        float[] t = { 0.5f, -1, 2, 0.25f, 1, 0.5f, 0, 1, 0, 0, 0 };
        var single = new string[a.Length];
        var dual = new string[a.Length];
        Func<Vector128<float>, Vector128<float>, Vector128<float>, Vector128<float>> callback = Vector128.Lerp;
        for (int k = 0; k < a.Length; k++)
        {
            var direct = Vector128.Lerp(Vector128.Create(a[k]), Vector128.Create(b[k]), Vector128.Create(t[k]));
            var delegated = callback(Vector128.Create(a[k]), Vector128.Create(b[k]), Vector128.Create(t[k]));
            for (int lane = 0; lane < Vector128<float>.Count; lane++)
                EstimateCheck(EstimateSame(direct.GetElement(lane), direct.GetElement(0)) && EstimateSame(delegated.GetElement(lane), direct.GetElement(0)));
            single[k] = float.IsNaN(direct.GetElement(0)) ? "NaN" : BitConverter.SingleToUInt32Bits(direct.GetElement(0)).ToString("X8");
            var wide = Vector.Lerp(new Vector<double>(a[k]), new Vector<double>(b[k]), new Vector<double>(t[k]));
            dual[k] = double.IsNaN(wide[0]) ? "NaN" : BitConverter.DoubleToUInt64Bits(wide[0]).ToString("X16");
        }
        Console.WriteLine("lerp 128f=" + string.Join(",", single));
        Console.WriteLine("lerp Nd=" + string.Join(",", dual));
        Console.WriteLine("lerp vector2=" + Vector2.Lerp(new Vector2(2, 1), new Vector2(4, 3), 0.25f));
        Console.WriteLine("lerp order=" + EstimateOrderText(() => Vector128.Lerp(
            EstimateOperand(1, Vector128.Create(2f)), EstimateOperand(2, Vector128.Create(4f)),
            EstimateOperand(3, Vector128.Create(0.5f))).GetElement(0)));
        // (1 + 2^-23)(1 - 2^-24) rounds to one before 2^-24 is subtracted.
        var nonfused = Vector128.Lerp(Vector128.Create(BitConverter.UInt32BitsToSingle(0x3F800001)),
            Vector128.Create(-1f), Vector128.Create(BitConverter.UInt32BitsToSingle(0x33800000)));
        return BitConverter.SingleToUInt32Bits(nonfused.GetElement(2)) == 0x3F7FFFFF;
    }
}
