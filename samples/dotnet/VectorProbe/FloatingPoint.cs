using System;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace VectorProbe;

internal static partial class Program
{
    private static void FloatingPoint()
    {
        Console.WriteLine("-- vector floating point --");
        FloatingPointWitnesses();
        float[] single = FloatingCasesSingle();
        double[] dbl = FloatingCasesDouble();
        Floating64Single(single);
        Floating64Double(dbl);
        Floating128Single(single);
        Floating128Double(dbl);
        Floating256Single(single);
        Floating256Double(dbl);
        Floating512Single(single);
        Floating512Double(dbl);
        FloatingNSingle(single);
        FloatingNDouble(dbl);
        Console.WriteLine("vector floating point end");
    }

    private static float[] FloatingCasesSingle()
    {
        float z = BitConverter.Int32BitsToSingle(int.MinValue);
        float n = float.NaN, p = float.PositiveInfinity, m = float.NegativeInfinity;
        float e = float.Epsilon, largest = float.MaxValue;
        float delta = 0.0001220703125f;
        // Triples serve as (left, right, addend) or (value, lower, upper).
        return new float[]
        {
            n, 1, 2,     1, n, 2,     1, 0, n,     n, n, n,
            z, 0, 1,     0, z, 1,     z, z, z,     0, 0, 0,
            p, 1, m,     m, 1, p,     1, p, m,     -1, m, p,
            p, p, 1,     m, m, -1,    e, -e, e,    -e, e, -e,
            1, 0, 1,     -1, 0, 1,    1, z, 1,     -1, z, 1,
            n, 0, 1,     n, z, 1,     p, 0, 1,     m, 0, 1,
            p, z, 1,     m, z, 1,     0, 2, z,     z, 2, 0,
            1 + delta, 1 - delta, -1,
            largest, 2, -largest,     -largest, 2, largest,
            e, 0.5f, e,  0.5f, 2, 1,  0, z, 1,    z, 0, z,
            0, 1, z,     z, 1, 0,     1, 1, 1,     -1, -1, -1,
        };
    }

    private static double[] FloatingCasesDouble()
    {
        double z = BitConverter.Int64BitsToDouble(long.MinValue);
        double n = double.NaN, p = double.PositiveInfinity, m = double.NegativeInfinity;
        double e = double.Epsilon, largest = double.MaxValue;
        double delta = 0.000000000931322574615478515625;
        return new double[]
        {
            n, 1, 2,     1, n, 2,     1, 0, n,     n, n, n,
            z, 0, 1,     0, z, 1,     z, z, z,     0, 0, 0,
            p, 1, m,     m, 1, p,     1, p, m,     -1, m, p,
            p, p, 1,     m, m, -1,    e, -e, e,    -e, e, -e,
            1, 0, 1,     -1, 0, 1,    1, z, 1,     -1, z, 1,
            n, 0, 1,     n, z, 1,     p, 0, 1,     m, 0, 1,
            p, z, 1,     m, z, 1,     0, 2, z,     z, 2, 0,
            1 + delta, 1 - delta, -1,
            largest, 2, -largest,     -largest, 2, largest,
            e, 0.5, e,   0.5, 2, 1,   0, z, 1,    z, 0, z,
            0, 1, z,     z, 1, 0,     1, 1, 1,     -1, -1, -1,
        };
    }

    private static void FloatingPointWitnesses()
    {
        var nan = Vector128.Create(double.NaN);
        var one = Vector128.Create(1.0);
        var zero = Vector128<double>.Zero;
        var negativeZero = Vector128.Create(BitConverter.Int64BitsToDouble(long.MinValue));
        double delta = 0.000000000931322574615478515625;
        float deltaF = 0.0001220703125f;
        Console.WriteLine("floating witness divide=" + FloatingBits(Vector128.Divide(one, zero).GetElement(1)));
        Console.WriteLine("floating witness min-nan=" + FloatingBits(Vector128.Min(nan, one).GetElement(1)));
        Console.WriteLine("floating witness max-nan=" + FloatingBits(Vector128.Max(nan, one).GetElement(1)));
        Console.WriteLine("floating witness clamp-nan=" + FloatingBits(Vector128.Clamp(nan, zero, one).GetElement(1)));
        Console.WriteLine("floating witness min-zero=" + FloatingBits(Vector128.Min(negativeZero, zero).GetElement(1)));
        Console.WriteLine("floating witness max-zero=" + FloatingBits(Vector128.Max(zero, negativeZero).GetElement(1)));
        Console.WriteLine("floating witness fma-f32=" + FloatingBits(Vector128.FusedMultiplyAdd(
            Vector128.Create(1 + deltaF), Vector128.Create(1 - deltaF), Vector128.Create(-1f)).GetElement(3)));
        Console.WriteLine("floating witness fma-f64=" + FloatingBits(Vector128.FusedMultiplyAdd(
            Vector128.Create(1 + delta), Vector128.Create(1 - delta), Vector128.Create(-1.0)).GetElement(1)));
    }

    private static string FloatingBits(float value) =>
        float.IsNaN(value) ? "NaN" : BitConverter.SingleToInt32Bits(value).ToString("X8");

    private static string FloatingBits(double value) =>
        double.IsNaN(value) ? "NaN" : BitConverter.DoubleToInt64Bits(value).ToString("X16");

    // NaN payloads and signs are target-dependent; every other result retains its bits.
    private static void FloatingAccumulate(ulong[] digest, int operation, float value)
    {
        ulong bits = float.IsNaN(value) ? 0x7FC00000U : unchecked((uint)BitConverter.SingleToInt32Bits(value));
        for (int shift = 0; shift < 32; shift += 8)
            digest[operation] = unchecked((digest[operation] ^ ((bits >> shift) & 255)) * 1099511628211UL);
    }

    private static void FloatingAccumulate(ulong[] digest, int operation, double value)
    {
        ulong bits = double.IsNaN(value) ? 0x7FF8000000000000UL : unchecked((ulong)BitConverter.DoubleToInt64Bits(value));
        for (int shift = 0; shift < 64; shift += 8)
            digest[operation] = unchecked((digest[operation] ^ ((bits >> shift) & 255)) * 1099511628211UL);
    }

    private static void PrintFloatingDigest(string label, ulong[] digest)
    {
        string[] operations = { "Min", "Max", "Clamp", "Fma", "Divide", "operator/", "MinNative", "MaxNative", "ClampNative" };
        for (int i = 0; i < operations.Length; i++)
            Console.WriteLine("floating " + label + " " + operations[i] + "=" + digest[i].ToString("X16"));
    }

    private static void Floating64Single(float[] inputs)
    {
        int lanes = Vector64<float>.Count;
        var left = new float[lanes];
        var right = new float[lanes];
        var addend = new float[lanes];
        var digest = new ulong[9];
        for (int i = 0; i < digest.Length; i++)
            digest[i] = 14695981039346656037UL;
        // Rotate each case through every lane, including all chunks of wide vectors.
        for (int start = 0; start < inputs.Length / 3; start++)
        {
            for (int lane = 0; lane < lanes; lane++)
            {
                int index = ((start + lane) % (inputs.Length / 3)) * 3;
                left[lane] = inputs[index];
                right[lane] = inputs[index + 1];
                addend[lane] = inputs[index + 2];
            }
            var a = Vector64.Create(left);
            var b = Vector64.Create(right);
            var c = Vector64.Create(addend);
            var min = Vector64.Min(a, b);
            var max = Vector64.Max(a, b);
            var clamp = Vector64.Clamp(a, b, c);
            var fma = Vector64.FusedMultiplyAdd(a, b, c);
            var divide = Vector64.Divide(a, b);
            var quotient = a / b;
            for (int lane = 0; lane < lanes; lane++)
            {
                FloatingAccumulate(digest, 0, min.GetElement(lane));
                FloatingAccumulate(digest, 1, max.GetElement(lane));
                FloatingAccumulate(digest, 2, clamp.GetElement(lane));
                FloatingAccumulate(digest, 3, fma.GetElement(lane));
                FloatingAccumulate(digest, 4, divide.GetElement(lane));
                FloatingAccumulate(digest, 5, quotient.GetElement(lane));
            }
        }
        // Native variants have target-specific NaN/zero semantics; finite lanes agree.
        for (int lane = 0; lane < lanes; lane++)
        {
            left[lane] = lane + 1;
            right[lane] = lanes - lane + 1;
            addend[lane] = lane + 2;
        }
        var nativeMin = Vector64.MinNative(Vector64.Create(left), Vector64.Create(right));
        var nativeMax = Vector64.MaxNative(Vector64.Create(left), Vector64.Create(right));
        var nativeClamp = Vector64.ClampNative(Vector64.Create(left), Vector64.Create(right), Vector64.Create(addend));
        for (int lane = 0; lane < lanes; lane++)
        {
            FloatingAccumulate(digest, 6, nativeMin.GetElement(lane));
            FloatingAccumulate(digest, 7, nativeMax.GetElement(lane));
            FloatingAccumulate(digest, 8, nativeClamp.GetElement(lane));
        }
        PrintFloatingDigest("v64 f32", digest);
    }

    private static void Floating64Double(double[] inputs)
    {
        int lanes = Vector64<double>.Count;
        var left = new double[lanes];
        var right = new double[lanes];
        var addend = new double[lanes];
        var digest = new ulong[9];
        for (int i = 0; i < digest.Length; i++)
            digest[i] = 14695981039346656037UL;
        // Rotate each case through every lane, including all chunks of wide vectors.
        for (int start = 0; start < inputs.Length / 3; start++)
        {
            for (int lane = 0; lane < lanes; lane++)
            {
                int index = ((start + lane) % (inputs.Length / 3)) * 3;
                left[lane] = inputs[index];
                right[lane] = inputs[index + 1];
                addend[lane] = inputs[index + 2];
            }
            var a = Vector64.Create(left);
            var b = Vector64.Create(right);
            var c = Vector64.Create(addend);
            var min = Vector64.Min(a, b);
            var max = Vector64.Max(a, b);
            var clamp = Vector64.Clamp(a, b, c);
            var fma = Vector64.FusedMultiplyAdd(a, b, c);
            var divide = Vector64.Divide(a, b);
            var quotient = a / b;
            for (int lane = 0; lane < lanes; lane++)
            {
                FloatingAccumulate(digest, 0, min.GetElement(lane));
                FloatingAccumulate(digest, 1, max.GetElement(lane));
                FloatingAccumulate(digest, 2, clamp.GetElement(lane));
                FloatingAccumulate(digest, 3, fma.GetElement(lane));
                FloatingAccumulate(digest, 4, divide.GetElement(lane));
                FloatingAccumulate(digest, 5, quotient.GetElement(lane));
            }
        }
        // Native variants have target-specific NaN/zero semantics; finite lanes agree.
        for (int lane = 0; lane < lanes; lane++)
        {
            left[lane] = lane + 1;
            right[lane] = lanes - lane + 1;
            addend[lane] = lane + 2;
        }
        var nativeMin = Vector64.MinNative(Vector64.Create(left), Vector64.Create(right));
        var nativeMax = Vector64.MaxNative(Vector64.Create(left), Vector64.Create(right));
        var nativeClamp = Vector64.ClampNative(Vector64.Create(left), Vector64.Create(right), Vector64.Create(addend));
        for (int lane = 0; lane < lanes; lane++)
        {
            FloatingAccumulate(digest, 6, nativeMin.GetElement(lane));
            FloatingAccumulate(digest, 7, nativeMax.GetElement(lane));
            FloatingAccumulate(digest, 8, nativeClamp.GetElement(lane));
        }
        PrintFloatingDigest("v64 f64", digest);
    }

    private static void Floating128Single(float[] inputs)
    {
        int lanes = Vector128<float>.Count;
        var left = new float[lanes];
        var right = new float[lanes];
        var addend = new float[lanes];
        var digest = new ulong[9];
        for (int i = 0; i < digest.Length; i++)
            digest[i] = 14695981039346656037UL;
        // Rotate each case through every lane, including all chunks of wide vectors.
        for (int start = 0; start < inputs.Length / 3; start++)
        {
            for (int lane = 0; lane < lanes; lane++)
            {
                int index = ((start + lane) % (inputs.Length / 3)) * 3;
                left[lane] = inputs[index];
                right[lane] = inputs[index + 1];
                addend[lane] = inputs[index + 2];
            }
            var a = Vector128.Create(left);
            var b = Vector128.Create(right);
            var c = Vector128.Create(addend);
            var min = Vector128.Min(a, b);
            var max = Vector128.Max(a, b);
            var clamp = Vector128.Clamp(a, b, c);
            var fma = Vector128.FusedMultiplyAdd(a, b, c);
            var divide = Vector128.Divide(a, b);
            var quotient = a / b;
            for (int lane = 0; lane < lanes; lane++)
            {
                FloatingAccumulate(digest, 0, min.GetElement(lane));
                FloatingAccumulate(digest, 1, max.GetElement(lane));
                FloatingAccumulate(digest, 2, clamp.GetElement(lane));
                FloatingAccumulate(digest, 3, fma.GetElement(lane));
                FloatingAccumulate(digest, 4, divide.GetElement(lane));
                FloatingAccumulate(digest, 5, quotient.GetElement(lane));
            }
        }
        // Native variants have target-specific NaN/zero semantics; finite lanes agree.
        for (int lane = 0; lane < lanes; lane++)
        {
            left[lane] = lane + 1;
            right[lane] = lanes - lane + 1;
            addend[lane] = lane + 2;
        }
        var nativeMin = Vector128.MinNative(Vector128.Create(left), Vector128.Create(right));
        var nativeMax = Vector128.MaxNative(Vector128.Create(left), Vector128.Create(right));
        var nativeClamp = Vector128.ClampNative(Vector128.Create(left), Vector128.Create(right), Vector128.Create(addend));
        for (int lane = 0; lane < lanes; lane++)
        {
            FloatingAccumulate(digest, 6, nativeMin.GetElement(lane));
            FloatingAccumulate(digest, 7, nativeMax.GetElement(lane));
            FloatingAccumulate(digest, 8, nativeClamp.GetElement(lane));
        }
        PrintFloatingDigest("v128 f32", digest);
    }

    private static void Floating128Double(double[] inputs)
    {
        int lanes = Vector128<double>.Count;
        var left = new double[lanes];
        var right = new double[lanes];
        var addend = new double[lanes];
        var digest = new ulong[9];
        for (int i = 0; i < digest.Length; i++)
            digest[i] = 14695981039346656037UL;
        // Rotate each case through every lane, including all chunks of wide vectors.
        for (int start = 0; start < inputs.Length / 3; start++)
        {
            for (int lane = 0; lane < lanes; lane++)
            {
                int index = ((start + lane) % (inputs.Length / 3)) * 3;
                left[lane] = inputs[index];
                right[lane] = inputs[index + 1];
                addend[lane] = inputs[index + 2];
            }
            var a = Vector128.Create(left);
            var b = Vector128.Create(right);
            var c = Vector128.Create(addend);
            var min = Vector128.Min(a, b);
            var max = Vector128.Max(a, b);
            var clamp = Vector128.Clamp(a, b, c);
            var fma = Vector128.FusedMultiplyAdd(a, b, c);
            var divide = Vector128.Divide(a, b);
            var quotient = a / b;
            for (int lane = 0; lane < lanes; lane++)
            {
                FloatingAccumulate(digest, 0, min.GetElement(lane));
                FloatingAccumulate(digest, 1, max.GetElement(lane));
                FloatingAccumulate(digest, 2, clamp.GetElement(lane));
                FloatingAccumulate(digest, 3, fma.GetElement(lane));
                FloatingAccumulate(digest, 4, divide.GetElement(lane));
                FloatingAccumulate(digest, 5, quotient.GetElement(lane));
            }
        }
        // Native variants have target-specific NaN/zero semantics; finite lanes agree.
        for (int lane = 0; lane < lanes; lane++)
        {
            left[lane] = lane + 1;
            right[lane] = lanes - lane + 1;
            addend[lane] = lane + 2;
        }
        var nativeMin = Vector128.MinNative(Vector128.Create(left), Vector128.Create(right));
        var nativeMax = Vector128.MaxNative(Vector128.Create(left), Vector128.Create(right));
        var nativeClamp = Vector128.ClampNative(Vector128.Create(left), Vector128.Create(right), Vector128.Create(addend));
        for (int lane = 0; lane < lanes; lane++)
        {
            FloatingAccumulate(digest, 6, nativeMin.GetElement(lane));
            FloatingAccumulate(digest, 7, nativeMax.GetElement(lane));
            FloatingAccumulate(digest, 8, nativeClamp.GetElement(lane));
        }
        PrintFloatingDigest("v128 f64", digest);
    }

    private static void Floating256Single(float[] inputs)
    {
        int lanes = Vector256<float>.Count;
        var left = new float[lanes];
        var right = new float[lanes];
        var addend = new float[lanes];
        var digest = new ulong[9];
        for (int i = 0; i < digest.Length; i++)
            digest[i] = 14695981039346656037UL;
        // Rotate each case through every lane, including all chunks of wide vectors.
        for (int start = 0; start < inputs.Length / 3; start++)
        {
            for (int lane = 0; lane < lanes; lane++)
            {
                int index = ((start + lane) % (inputs.Length / 3)) * 3;
                left[lane] = inputs[index];
                right[lane] = inputs[index + 1];
                addend[lane] = inputs[index + 2];
            }
            var a = Vector256.Create(left);
            var b = Vector256.Create(right);
            var c = Vector256.Create(addend);
            var min = Vector256.Min(a, b);
            var max = Vector256.Max(a, b);
            var clamp = Vector256.Clamp(a, b, c);
            var fma = Vector256.FusedMultiplyAdd(a, b, c);
            var divide = Vector256.Divide(a, b);
            var quotient = a / b;
            for (int lane = 0; lane < lanes; lane++)
            {
                FloatingAccumulate(digest, 0, min.GetElement(lane));
                FloatingAccumulate(digest, 1, max.GetElement(lane));
                FloatingAccumulate(digest, 2, clamp.GetElement(lane));
                FloatingAccumulate(digest, 3, fma.GetElement(lane));
                FloatingAccumulate(digest, 4, divide.GetElement(lane));
                FloatingAccumulate(digest, 5, quotient.GetElement(lane));
            }
        }
        // Native variants have target-specific NaN/zero semantics; finite lanes agree.
        for (int lane = 0; lane < lanes; lane++)
        {
            left[lane] = lane + 1;
            right[lane] = lanes - lane + 1;
            addend[lane] = lane + 2;
        }
        var nativeMin = Vector256.MinNative(Vector256.Create(left), Vector256.Create(right));
        var nativeMax = Vector256.MaxNative(Vector256.Create(left), Vector256.Create(right));
        var nativeClamp = Vector256.ClampNative(Vector256.Create(left), Vector256.Create(right), Vector256.Create(addend));
        for (int lane = 0; lane < lanes; lane++)
        {
            FloatingAccumulate(digest, 6, nativeMin.GetElement(lane));
            FloatingAccumulate(digest, 7, nativeMax.GetElement(lane));
            FloatingAccumulate(digest, 8, nativeClamp.GetElement(lane));
        }
        PrintFloatingDigest("v256 f32", digest);
    }

    private static void Floating256Double(double[] inputs)
    {
        int lanes = Vector256<double>.Count;
        var left = new double[lanes];
        var right = new double[lanes];
        var addend = new double[lanes];
        var digest = new ulong[9];
        for (int i = 0; i < digest.Length; i++)
            digest[i] = 14695981039346656037UL;
        // Rotate each case through every lane, including all chunks of wide vectors.
        for (int start = 0; start < inputs.Length / 3; start++)
        {
            for (int lane = 0; lane < lanes; lane++)
            {
                int index = ((start + lane) % (inputs.Length / 3)) * 3;
                left[lane] = inputs[index];
                right[lane] = inputs[index + 1];
                addend[lane] = inputs[index + 2];
            }
            var a = Vector256.Create(left);
            var b = Vector256.Create(right);
            var c = Vector256.Create(addend);
            var min = Vector256.Min(a, b);
            var max = Vector256.Max(a, b);
            var clamp = Vector256.Clamp(a, b, c);
            var fma = Vector256.FusedMultiplyAdd(a, b, c);
            var divide = Vector256.Divide(a, b);
            var quotient = a / b;
            for (int lane = 0; lane < lanes; lane++)
            {
                FloatingAccumulate(digest, 0, min.GetElement(lane));
                FloatingAccumulate(digest, 1, max.GetElement(lane));
                FloatingAccumulate(digest, 2, clamp.GetElement(lane));
                FloatingAccumulate(digest, 3, fma.GetElement(lane));
                FloatingAccumulate(digest, 4, divide.GetElement(lane));
                FloatingAccumulate(digest, 5, quotient.GetElement(lane));
            }
        }
        // Native variants have target-specific NaN/zero semantics; finite lanes agree.
        for (int lane = 0; lane < lanes; lane++)
        {
            left[lane] = lane + 1;
            right[lane] = lanes - lane + 1;
            addend[lane] = lane + 2;
        }
        var nativeMin = Vector256.MinNative(Vector256.Create(left), Vector256.Create(right));
        var nativeMax = Vector256.MaxNative(Vector256.Create(left), Vector256.Create(right));
        var nativeClamp = Vector256.ClampNative(Vector256.Create(left), Vector256.Create(right), Vector256.Create(addend));
        for (int lane = 0; lane < lanes; lane++)
        {
            FloatingAccumulate(digest, 6, nativeMin.GetElement(lane));
            FloatingAccumulate(digest, 7, nativeMax.GetElement(lane));
            FloatingAccumulate(digest, 8, nativeClamp.GetElement(lane));
        }
        PrintFloatingDigest("v256 f64", digest);
    }

    private static void Floating512Single(float[] inputs)
    {
        int lanes = Vector512<float>.Count;
        var left = new float[lanes];
        var right = new float[lanes];
        var addend = new float[lanes];
        var digest = new ulong[9];
        for (int i = 0; i < digest.Length; i++)
            digest[i] = 14695981039346656037UL;
        // Rotate each case through every lane, including all chunks of wide vectors.
        for (int start = 0; start < inputs.Length / 3; start++)
        {
            for (int lane = 0; lane < lanes; lane++)
            {
                int index = ((start + lane) % (inputs.Length / 3)) * 3;
                left[lane] = inputs[index];
                right[lane] = inputs[index + 1];
                addend[lane] = inputs[index + 2];
            }
            var a = Vector512.Create(left);
            var b = Vector512.Create(right);
            var c = Vector512.Create(addend);
            var min = Vector512.Min(a, b);
            var max = Vector512.Max(a, b);
            var clamp = Vector512.Clamp(a, b, c);
            var fma = Vector512.FusedMultiplyAdd(a, b, c);
            var divide = Vector512.Divide(a, b);
            var quotient = a / b;
            for (int lane = 0; lane < lanes; lane++)
            {
                FloatingAccumulate(digest, 0, min.GetElement(lane));
                FloatingAccumulate(digest, 1, max.GetElement(lane));
                FloatingAccumulate(digest, 2, clamp.GetElement(lane));
                FloatingAccumulate(digest, 3, fma.GetElement(lane));
                FloatingAccumulate(digest, 4, divide.GetElement(lane));
                FloatingAccumulate(digest, 5, quotient.GetElement(lane));
            }
        }
        // Native variants have target-specific NaN/zero semantics; finite lanes agree.
        for (int lane = 0; lane < lanes; lane++)
        {
            left[lane] = lane + 1;
            right[lane] = lanes - lane + 1;
            addend[lane] = lane + 2;
        }
        var nativeMin = Vector512.MinNative(Vector512.Create(left), Vector512.Create(right));
        var nativeMax = Vector512.MaxNative(Vector512.Create(left), Vector512.Create(right));
        var nativeClamp = Vector512.ClampNative(Vector512.Create(left), Vector512.Create(right), Vector512.Create(addend));
        for (int lane = 0; lane < lanes; lane++)
        {
            FloatingAccumulate(digest, 6, nativeMin.GetElement(lane));
            FloatingAccumulate(digest, 7, nativeMax.GetElement(lane));
            FloatingAccumulate(digest, 8, nativeClamp.GetElement(lane));
        }
        PrintFloatingDigest("v512 f32", digest);
    }

    private static void Floating512Double(double[] inputs)
    {
        int lanes = Vector512<double>.Count;
        var left = new double[lanes];
        var right = new double[lanes];
        var addend = new double[lanes];
        var digest = new ulong[9];
        for (int i = 0; i < digest.Length; i++)
            digest[i] = 14695981039346656037UL;
        // Rotate each case through every lane, including all chunks of wide vectors.
        for (int start = 0; start < inputs.Length / 3; start++)
        {
            for (int lane = 0; lane < lanes; lane++)
            {
                int index = ((start + lane) % (inputs.Length / 3)) * 3;
                left[lane] = inputs[index];
                right[lane] = inputs[index + 1];
                addend[lane] = inputs[index + 2];
            }
            var a = Vector512.Create(left);
            var b = Vector512.Create(right);
            var c = Vector512.Create(addend);
            var min = Vector512.Min(a, b);
            var max = Vector512.Max(a, b);
            var clamp = Vector512.Clamp(a, b, c);
            var fma = Vector512.FusedMultiplyAdd(a, b, c);
            var divide = Vector512.Divide(a, b);
            var quotient = a / b;
            for (int lane = 0; lane < lanes; lane++)
            {
                FloatingAccumulate(digest, 0, min.GetElement(lane));
                FloatingAccumulate(digest, 1, max.GetElement(lane));
                FloatingAccumulate(digest, 2, clamp.GetElement(lane));
                FloatingAccumulate(digest, 3, fma.GetElement(lane));
                FloatingAccumulate(digest, 4, divide.GetElement(lane));
                FloatingAccumulate(digest, 5, quotient.GetElement(lane));
            }
        }
        // Native variants have target-specific NaN/zero semantics; finite lanes agree.
        for (int lane = 0; lane < lanes; lane++)
        {
            left[lane] = lane + 1;
            right[lane] = lanes - lane + 1;
            addend[lane] = lane + 2;
        }
        var nativeMin = Vector512.MinNative(Vector512.Create(left), Vector512.Create(right));
        var nativeMax = Vector512.MaxNative(Vector512.Create(left), Vector512.Create(right));
        var nativeClamp = Vector512.ClampNative(Vector512.Create(left), Vector512.Create(right), Vector512.Create(addend));
        for (int lane = 0; lane < lanes; lane++)
        {
            FloatingAccumulate(digest, 6, nativeMin.GetElement(lane));
            FloatingAccumulate(digest, 7, nativeMax.GetElement(lane));
            FloatingAccumulate(digest, 8, nativeClamp.GetElement(lane));
        }
        PrintFloatingDigest("v512 f64", digest);
    }

    private static void FloatingNSingle(float[] inputs)
    {
        int lanes = Vector<float>.Count;
        var left = new float[lanes];
        var right = new float[lanes];
        var addend = new float[lanes];
        var digest = new ulong[9];
        for (int i = 0; i < digest.Length; i++)
            digest[i] = 14695981039346656037UL;
        // Rotate each case through every lane, including all chunks of wide vectors.
        for (int start = 0; start < inputs.Length / 3; start++)
        {
            for (int lane = 0; lane < lanes; lane++)
            {
                int index = ((start + lane) % (inputs.Length / 3)) * 3;
                left[lane] = inputs[index];
                right[lane] = inputs[index + 1];
                addend[lane] = inputs[index + 2];
            }
            var a = new Vector<float>(left);
            var b = new Vector<float>(right);
            var c = new Vector<float>(addend);
            var min = Vector.Min(a, b);
            var max = Vector.Max(a, b);
            var clamp = Vector.Clamp(a, b, c);
            var fma = Vector.FusedMultiplyAdd(a, b, c);
            var divide = Vector.Divide(a, b);
            var quotient = a / b;
            for (int lane = 0; lane < lanes; lane++)
            {
                FloatingAccumulate(digest, 0, min[lane]);
                FloatingAccumulate(digest, 1, max[lane]);
                FloatingAccumulate(digest, 2, clamp[lane]);
                FloatingAccumulate(digest, 3, fma[lane]);
                FloatingAccumulate(digest, 4, divide[lane]);
                FloatingAccumulate(digest, 5, quotient[lane]);
            }
        }
        // Native variants have target-specific NaN/zero semantics; finite lanes agree.
        for (int lane = 0; lane < lanes; lane++)
        {
            left[lane] = lane + 1;
            right[lane] = lanes - lane + 1;
            addend[lane] = lane + 2;
        }
        var nativeMin = Vector.MinNative(new Vector<float>(left), new Vector<float>(right));
        var nativeMax = Vector.MaxNative(new Vector<float>(left), new Vector<float>(right));
        var nativeClamp = Vector.ClampNative(new Vector<float>(left), new Vector<float>(right), new Vector<float>(addend));
        for (int lane = 0; lane < lanes; lane++)
        {
            FloatingAccumulate(digest, 6, nativeMin[lane]);
            FloatingAccumulate(digest, 7, nativeMax[lane]);
            FloatingAccumulate(digest, 8, nativeClamp[lane]);
        }
        PrintFloatingDigest("vn f32", digest);
    }

    private static void FloatingNDouble(double[] inputs)
    {
        int lanes = Vector<double>.Count;
        var left = new double[lanes];
        var right = new double[lanes];
        var addend = new double[lanes];
        var digest = new ulong[9];
        for (int i = 0; i < digest.Length; i++)
            digest[i] = 14695981039346656037UL;
        // Rotate each case through every lane, including all chunks of wide vectors.
        for (int start = 0; start < inputs.Length / 3; start++)
        {
            for (int lane = 0; lane < lanes; lane++)
            {
                int index = ((start + lane) % (inputs.Length / 3)) * 3;
                left[lane] = inputs[index];
                right[lane] = inputs[index + 1];
                addend[lane] = inputs[index + 2];
            }
            var a = new Vector<double>(left);
            var b = new Vector<double>(right);
            var c = new Vector<double>(addend);
            var min = Vector.Min(a, b);
            var max = Vector.Max(a, b);
            var clamp = Vector.Clamp(a, b, c);
            var fma = Vector.FusedMultiplyAdd(a, b, c);
            var divide = Vector.Divide(a, b);
            var quotient = a / b;
            for (int lane = 0; lane < lanes; lane++)
            {
                FloatingAccumulate(digest, 0, min[lane]);
                FloatingAccumulate(digest, 1, max[lane]);
                FloatingAccumulate(digest, 2, clamp[lane]);
                FloatingAccumulate(digest, 3, fma[lane]);
                FloatingAccumulate(digest, 4, divide[lane]);
                FloatingAccumulate(digest, 5, quotient[lane]);
            }
        }
        // Native variants have target-specific NaN/zero semantics; finite lanes agree.
        for (int lane = 0; lane < lanes; lane++)
        {
            left[lane] = lane + 1;
            right[lane] = lanes - lane + 1;
            addend[lane] = lane + 2;
        }
        var nativeMin = Vector.MinNative(new Vector<double>(left), new Vector<double>(right));
        var nativeMax = Vector.MaxNative(new Vector<double>(left), new Vector<double>(right));
        var nativeClamp = Vector.ClampNative(new Vector<double>(left), new Vector<double>(right), new Vector<double>(addend));
        for (int lane = 0; lane < lanes; lane++)
        {
            FloatingAccumulate(digest, 6, nativeMin[lane]);
            FloatingAccumulate(digest, 7, nativeMax[lane]);
            FloatingAccumulate(digest, 8, nativeClamp[lane]);
        }
        PrintFloatingDigest("vn f64", digest);
    }
}
