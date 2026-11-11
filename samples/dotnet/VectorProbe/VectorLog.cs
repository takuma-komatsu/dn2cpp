using System;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace VectorProbe;

// Log and Log2 lanes follow the platform's scalar library, so finite results are
// held to a tolerance around scalar Math.Log/Math.Log2 instead of exact bits.
// Domain classification and gradual underflow are exact.
internal static partial class Program
{
    private static readonly float[] LogSingles =
    {
        0f, -0f, float.PositiveInfinity, float.NegativeInfinity,
        float.NaN, -1f, -float.Epsilon, 1f,
        2f, 0.5f, 10f, 0.75f,
        float.Epsilon, BitConverter.UInt32BitsToSingle(0x007FFFFF), float.MaxValue, 1e-30f,
    };
    private static readonly double[] LogDoubles =
    {
        0d, -0d, double.PositiveInfinity, double.NegativeInfinity,
        double.NaN, -1d, -double.Epsilon, 1d,
        2d, 0.5d, 10d, 0.75d,
        double.Epsilon, BitConverter.UInt64BitsToDouble(0x000FFFFFFFFFFFFF), double.MaxValue, 1e-300,
    };

    private static void VectorLogSection()
    {
        Console.WriteLine("-- vector log --");
        Func<Vector128<float>, Vector128<float>> log = Vector128.Log;
        for (int i = 0; i < LogSingles.Length; i += 4)
        {
            var input = Vector128.Create(LogSingles, i);
            var natural = Vector128.Log(input);
            var binary = Vector128.Log2(input);
            var delegated = log(input);
            for (int lane = 0; lane < 4; lane++)
            {
                float x = LogSingles[i + lane];
                LogCheck(BitConverter.SingleToUInt32Bits(delegated.GetElement(lane)) == BitConverter.SingleToUInt32Bits(natural.GetElement(lane))
                    || float.IsNaN(delegated.GetElement(lane)) && float.IsNaN(natural.GetElement(lane)), "delegate");
                LogPrint("v128f.log." + (i + lane), x, natural.GetElement(lane), Math.Log(x), 1e-6);
                LogPrint("v128f.log2." + (i + lane), x, binary.GetElement(lane), Math.Log2(x), 1e-6);
            }
        }
        for (int i = 0; i < LogDoubles.Length; i += Vector<double>.Count)
        {
            var input = new Vector<double>(LogDoubles, i);
            var natural = Vector.Log(input);
            var binary = Vector.Log2(input);
            for (int lane = 0; lane < Vector<double>.Count; lane++)
            {
                double x = LogDoubles[i + lane];
                LogPrint("vnd.log." + (i + lane), x, natural[lane], Math.Log(x), 1e-14);
                LogPrint("vnd.log2." + (i + lane), x, binary[lane], Math.Log2(x), 1e-14);
            }
        }
        var concrete = new Vector3(-0f, 8f, float.Epsilon);
        var concreteLog = Vector3.Log(concrete);
        var concreteLog2 = Vector3.Log2(concrete);
        LogPrint("v3.log.x", concrete.X, concreteLog.X, Math.Log(concrete.X), 1e-6);
        LogPrint("v3.log2.y", concrete.Y, concreteLog2.Y, Math.Log2(concrete.Y), 1e-6);
        LogPrint("v3.log2.z", concrete.Z, concreteLog2.Z, Math.Log2(concrete.Z), 1e-6);
        Console.WriteLine("-- vector log end --");
    }

    private static void LogCheck(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("vector log " + label);
    }

    private static void LogPrint(string label, double input, double actual, double expected, double tolerance)
    {
        string text;
        if (double.IsNaN(input) || input < 0) text = double.IsNaN(actual) ? "NaN" : "not-NaN";
        else if (input == 0) text = double.IsNegativeInfinity(actual) ? "-inf" : "not-inf";
        else if (double.IsPositiveInfinity(input)) text = double.IsPositiveInfinity(actual) ? "+inf" : "not-inf";
        else text = double.IsFinite(actual) && Math.Abs(actual - expected) <= tolerance * Math.Max(1, Math.Abs(expected)) ? "ok" : "off";
        Console.WriteLine(label + "=" + text);
    }
}
