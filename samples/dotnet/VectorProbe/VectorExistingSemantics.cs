using System;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace VectorProbe;

internal static partial class Program
{
    private static void VectorExistingSemantics()
    {
        Console.WriteLine("-- vector existing value semantics --");
        Console.WriteLine("typed NaN="
            + Vector64.Create(float.NaN).Equals(Vector64.Create(float.NaN))
            + ":" + Vector128.Create(float.NaN).Equals(Vector128.Create(float.NaN))
            + ":" + Vector256.Create(double.NaN).Equals(Vector256.Create(double.NaN))
            + ":" + Vector512.Create(double.NaN).Equals(Vector512.Create(double.NaN))
            + ":" + new Vector<float>(float.NaN).Equals(new Vector<float>(float.NaN)));
        Console.WriteLine("typed zero=" + Vector128.Create(0f).Equals(Vector128.Create(-0f)));
        Console.WriteLine("typed unequal=" + Vector128.Create(float.NaN, 1f, 2f, 3f)
            .Equals(Vector128.Create(float.NaN, 1f, 2f, 4f)));
        Console.WriteLine("comparison NaN="
            + Vector128.EqualsAll(Vector128.Create(float.NaN), Vector128.Create(float.NaN))
            + ":" + (Vector128.Create(float.NaN) == Vector128.Create(float.NaN)));

        // Multiplication overflows before division, even though a precomputed
        // pi/180 factor would leave these finite.
        Console.WriteLine("degrees overflow="
            + float.IsPositiveInfinity(Vector64.DegreesToRadians(Vector64.Create(float.MaxValue)).GetElement(0))
            + ":" + float.IsPositiveInfinity(Vector128.DegreesToRadians(Vector128.Create(float.MaxValue)).GetElement(0))
            + ":" + double.IsPositiveInfinity(Vector256.DegreesToRadians(Vector256.Create(double.MaxValue)).GetElement(0))
            + ":" + double.IsPositiveInfinity(Vector512.DegreesToRadians(Vector512.Create(double.MaxValue)).GetElement(0))
            + ":" + float.IsPositiveInfinity(Vector.DegreesToRadians(new Vector<float>(float.MaxValue))[0]));
        Console.WriteLine("degrees negative="
            + double.IsNegativeInfinity(Vector128.DegreesToRadians(Vector128.Create(-double.MaxValue)).GetElement(0)));
        Console.WriteLine("-- vector existing value semantics end --");
    }
}
