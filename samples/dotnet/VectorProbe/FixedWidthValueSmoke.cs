using System;
using System.Runtime.Intrinsics;

namespace VectorProbe;

internal static partial class Program
{
    // The detailed 128/256 matrix is shared by the width lowerings. Exercise
    // the smallest and largest layouts with a final-lane mismatch as controls.
    private static void FixedWidthValuePair<T>(string name, T left, T equal, T different)
        where T : struct, IEquatable<T>
    {
        FixedValueAssert(left.Equals(equal) && !left.Equals(different), name + " typed");
        object box = left;
        FixedValueAssert(box.Equals(equal) && !box.Equals(different) && !box.Equals(null), name + " boxed");
        FixedValueAssert(left.GetHashCode() == equal.GetHashCode()
            && box.GetHashCode() == equal.GetHashCode(), name + " hash");
        Func<T, bool> equals = left.Equals;
        Func<int> hash = left.GetHashCode;
        FixedValueAssert(equals(equal) && !equals(different)
            && hash() == equal.GetHashCode(), name + " callable");
        Console.WriteLine(name + " value/hash/callable passed");
    }

    private static void FixedWidthValueSmoke()
    {
        Console.WriteLine("-- fixed width value smoke --");
        Vector64<int> small = Vector64.Create(7, 19);
        FixedWidthValuePair("fixed64.int", small, small, small.WithElement(1, 23));
        Vector512<int> large = Vector512.Create(7);
        FixedWidthValuePair("fixed512.int", large, large, large.WithElement(15, 23));

        Vector64<float> nan64 = Vector64.Create(float.NaN, -0.0f);
        Vector64<float> equal64 = Vector64.Create(BitConverter.Int32BitsToSingle(0x7fc12345), 0.0f);
        FixedWidthValuePair("fixed64.float", nan64, equal64, equal64.WithElement(1, 1.0f));
        Vector512<double> nan512 = Vector512.Create(double.NaN).WithElement(7, -0.0);
        Vector512<double> equal512 = Vector512.Create(BitConverter.Int64BitsToDouble(0x7ff8123456789abc)).WithElement(7, 0.0);
        FixedWidthValuePair("fixed512.double", nan512, equal512, equal512.WithElement(7, 1.0));
        FixedValueAssert(!((object)small).Equals(default(Vector64<uint>))
            && !((object)large).Equals(default(Vector512<uint>)), "fixed width exact element identity");

        Vector64<VectorUnsupportedEnum> unsupported64 = default;
        VectorUnsupportedRecord("fixed64.enum.IsSupported", () => Vector64<VectorUnsupportedEnum>.IsSupported);
        VectorUnsupportedRecord("fixed64.enum.Count", () => Vector64<VectorUnsupportedEnum>.Count);
        VectorUnsupportedRecord("fixed64.enum.typed", () => unsupported64.Equals(unsupported64));
        VectorUnsupportedRecord("fixed64.enum.hash", () => unsupported64.GetHashCode());
        VectorUnsupportedRecord("fixed64.enum.object-null", () => unsupported64.Equals((object)null));
        VectorUnsupportedRecord("fixed64.enum.boxed-same", () => ((object)unsupported64).Equals(unsupported64));
        Func<Vector64<VectorUnsupportedEnum>, bool> callable64 = unsupported64.Equals;
        VectorUnsupportedRecord("fixed64.enum.callable", () => callable64(unsupported64));

        Vector512<VectorUnsupportedEnum> unsupported512 = default;
        VectorUnsupportedRecord("fixed512.enum.IsSupported", () => Vector512<VectorUnsupportedEnum>.IsSupported);
        VectorUnsupportedRecord("fixed512.enum.Count", () => Vector512<VectorUnsupportedEnum>.Count);
        VectorUnsupportedRecord("fixed512.enum.typed", () => unsupported512.Equals(unsupported512));
        VectorUnsupportedRecord("fixed512.enum.hash", () => unsupported512.GetHashCode());
        VectorUnsupportedRecord("fixed512.enum.object-null", () => unsupported512.Equals((object)null));
        VectorUnsupportedRecord("fixed512.enum.boxed-same", () => ((object)unsupported512).Equals(unsupported512));
        Func<Vector512<VectorUnsupportedEnum>, bool> callable512 = unsupported512.Equals;
        VectorUnsupportedRecord("fixed512.enum.callable", () => callable512(unsupported512));
        Console.WriteLine("fixed width value smoke end");
    }
}
