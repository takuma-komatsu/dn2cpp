using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace VectorProbe;

// Magnitude and Number selection pick one operand per lane: NaN propagation versus
// numeric selection and signed-zero ties are exact. Unsupported managed elements
// evaluate both operands and then throw.
internal static partial class Program
{
    private static readonly float[] SelectionLeft =
    {
        0f, -0f, 1f, -1f, 2f, float.NaN, 1f, float.PositiveInfinity,
        float.NegativeInfinity, float.Epsilon, -2f, float.NaN,
    };
    private static readonly float[] SelectionRight =
    {
        -0f, 0f, -1f, 1f, -3f, 1f, float.NaN, float.NegativeInfinity,
        5f, -float.Epsilon, 2f, float.NaN,
    };
    private static string SelectionEffects = "";

    private static string SelectionText(float value) =>
        float.IsNaN(value) ? "NaN" : BitConverter.SingleToUInt32Bits(value).ToString("X8");
    private static string SelectionText(double value) =>
        double.IsNaN(value) ? "NaN" : BitConverter.DoubleToUInt64Bits(value).ToString("X16");

    private static void SelectionSingles(string name, Func<Vector128<float>, Vector128<float>, Vector128<float>> select)
    {
        var text = new string[SelectionLeft.Length];
        for (int i = 0; i < SelectionLeft.Length; i += 4)
        {
            var result = select(Vector128.Create(SelectionLeft, i), Vector128.Create(SelectionRight, i));
            for (int lane = 0; lane < 4; lane++) text[i + lane] = SelectionText(result.GetElement(lane));
        }
        Console.WriteLine("select 128f." + name + "=" + string.Join(",", text));
    }

    private static void SelectionDoubles(string name, Func<Vector<double>, Vector<double>, Vector<double>> select)
    {
        var text = new string[SelectionLeft.Length];
        int count = Vector<double>.Count;
        for (int i = 0; i < SelectionLeft.Length; i += count)
        {
            var left = new double[count];
            var right = new double[count];
            for (int lane = 0; lane < count; lane++) { left[lane] = SelectionLeft[i + lane]; right[lane] = SelectionRight[i + lane]; }
            var result = select(new Vector<double>(left), new Vector<double>(right));
            for (int lane = 0; lane < count; lane++) text[i + lane] = SelectionText(result[lane]);
        }
        Console.WriteLine("select Nd." + name + "=" + string.Join(",", text));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T SelectionOperand<T>(string mark) where T : struct
    {
        SelectionEffects += mark;
        return default;
    }

    private static void SelectionRecord(string name, Func<object> action)
    {
        SelectionEffects = "";
        try { Console.WriteLine("select " + name + "=" + action() + " effects=" + SelectionEffects); }
        catch (Exception error) { Console.WriteLine("select " + name + "=" + error.GetType().FullName + ":" + error.Message + " effects=" + SelectionEffects); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<T> SelectionGeneric<T>(Vector<T> left, Vector<T> right) where T : struct =>
        Vector.MaxMagnitude(left, right);

    private static void MagnitudeSelectionSection()
    {
        Console.WriteLine("-- vector magnitude selection --");
        SelectionSingles("MaxMagnitude", (a, b) => Vector128.MaxMagnitude(a, b));
        SelectionSingles("MaxMagnitudeNumber", (a, b) => Vector128.MaxMagnitudeNumber(a, b));
        SelectionSingles("MinMagnitude", (a, b) => Vector128.MinMagnitude(a, b));
        SelectionSingles("MinMagnitudeNumber", (a, b) => Vector128.MinMagnitudeNumber(a, b));
        SelectionSingles("MaxNumber", (a, b) => Vector128.MaxNumber(a, b));
        SelectionSingles("MinNumber", (a, b) => Vector128.MinNumber(a, b));
        SelectionSingles("delegate.MinMagnitude", Vector128.MinMagnitude);
        SelectionDoubles("MaxMagnitude", (a, b) => Vector.MaxMagnitude(a, b));
        SelectionDoubles("MaxMagnitudeNumber", (a, b) => Vector.MaxMagnitudeNumber(a, b));
        SelectionDoubles("MinMagnitude", (a, b) => Vector.MinMagnitude(a, b));
        SelectionDoubles("MinMagnitudeNumber", (a, b) => Vector.MinMagnitudeNumber(a, b));
        SelectionDoubles("MaxNumber", (a, b) => Vector.MaxNumber(a, b));
        SelectionDoubles("MinNumber", (a, b) => Vector.MinNumber(a, b));
        SelectionDoubles("generic.MaxMagnitude", SelectionGeneric);
        var v4 = Vector4.MaxMagnitude(new Vector4(-0f, 3f, float.NaN, -2f), new Vector4(0f, -4f, 1f, 2f));
        Console.WriteLine("select v4.MaxMagnitude=" + SelectionText(v4.X) + "," + SelectionText(v4.Y) + "," + SelectionText(v4.Z) + "," + SelectionText(v4.W));
        SelectionRecord("128m.MaxMagnitude", () => Vector128.MaxMagnitude(SelectionOperand<Vector128<decimal>>("L"), SelectionOperand<Vector128<decimal>>("R")));
        SelectionRecord("Ni128.MinNumber", () => Vector.MinNumber(SelectionOperand<Vector<Int128>>("L"), SelectionOperand<Vector<Int128>>("R")));
        SelectionRecord("generic.i128", () => SelectionGeneric(SelectionOperand<Vector<Int128>>("L"), SelectionOperand<Vector<Int128>>("R")));
        Console.WriteLine("-- vector magnitude selection end --");
    }
}
