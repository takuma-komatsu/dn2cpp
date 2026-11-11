using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace VectorProbe;

// Vector.As reinterprets numeric lanes bit for bit. An unsupported managed element
// on either side throws NotSupportedException and produces no vector.
internal static partial class Program
{
    private static string AsRoundTrip<T>(Vector<byte> source) where T : struct
    {
        Vector<T> middle = Vector.As<byte, T>(source);
        return Vector.As<T, byte>(middle) == source ? "ok" : "changed";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<TTo> AsGeneric<TTo>(Vector<int> value) where TTo : struct => Vector.As<int, TTo>(value);

    private static void VectorAsSection()
    {
        Console.WriteLine("-- vector as --");
        var bytes = new byte[Vector<byte>.Count];
        for (int i = 0; i < bytes.Length; i++) bytes[i] = (byte)(i * 37 + 5);
        var source = new Vector<byte>(bytes);
        Console.WriteLine("as roundtrip=" + AsRoundTrip<sbyte>(source) + "," + AsRoundTrip<short>(source) + ","
            + AsRoundTrip<ushort>(source) + "," + AsRoundTrip<int>(source) + "," + AsRoundTrip<uint>(source) + ","
            + AsRoundTrip<long>(source) + "," + AsRoundTrip<ulong>(source) + "," + AsRoundTrip<nint>(source) + ","
            + AsRoundTrip<nuint>(source) + "," + AsRoundTrip<float>(source) + "," + AsRoundTrip<double>(source));
        Vector<uint> words = Vector.As<byte, uint>(source);
        Console.WriteLine("as uint=" + words[0].ToString("X8") + "," + words[3].ToString("X8"));
        Console.WriteLine("as generic int=" + AsGeneric<int>(Vector.As<byte, int>(source))[2].ToString("X8"));
        VectorUnsupportedRecord("as.int-to-enum", () => Vector.As<int, VectorUnsupportedEnum>(default));
        VectorUnsupportedRecord("as.decimal-to-int", () => Vector.As<decimal, int>(default));
        VectorUnsupportedRecord("as.generic-enum", () => AsGeneric<VectorUnsupportedEnum>(VectorUnsupportedOperand<int>("A")));
        Func<Vector<int>, Vector<VectorUnsupportedEnum>> guarded = Vector.As<int, VectorUnsupportedEnum>;
        VectorUnsupportedRecord("as.callable-enum", () => guarded(VectorUnsupportedOperand<int>("A")));
        VectorUnsupportedRecord("as.explicit-decimal", () => (Vector<int>)VectorUnsupportedOperand<decimal>("A"));
        VectorUnsupportedRecord("as.forwarded-decimal", () => Vector.AsVectorInt32(VectorUnsupportedOperand<decimal>("A")));
        Console.WriteLine("-- vector as end --");
    }
}
