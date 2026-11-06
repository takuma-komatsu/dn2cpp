using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace VectorProbe;

internal static partial class Program
{
    private static int _predicateCopyChecks;
    private static string _predicateCopyOrder = "";

    private static void PC(string label, bool actual, bool expected = true)
    {
        if (actual != expected) throw new InvalidOperationException(label);
        ++_predicateCopyChecks;
        Console.WriteLine(label + "=ok");
    }

    private static float PCFloat(int bits) => BitConverter.Int32BitsToSingle(bits);
    private static bool PCBits(float left, float right) =>
        BitConverter.SingleToInt32Bits(left) == BitConverter.SingleToInt32Bits(right);

    private static bool PCFilled(float[] values, float expected)
    {
        for (int i = 0; i < values.Length; ++i)
            if (!PCBits(values[i], expected)) return false;
        return true;
    }

    private static bool PCFilled(int[] values, int expected)
    {
        for (int i = 0; i < values.Length; ++i)
            if (values[i] != expected) return false;
        return true;
    }

    private static bool PCFilled(byte[] values, byte expected)
    {
        for (int i = 0; i < values.Length; ++i)
            if (values[i] != expected) return false;
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector4 PCVector(bool fail)
    {
        _predicateCopyOrder += "V";
        if (fail) throw new InvalidOperationException("vector");
        return new Vector4(1, 2, 3, 4);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float PCValue(bool fail)
    {
        _predicateCopyOrder += "S";
        if (fail) throw new InvalidOperationException("scalar");
        return 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Span<float> PCSpan(float[] values, int length, bool fail)
    {
        _predicateCopyOrder += "D";
        if (fail) throw new InvalidOperationException("destination");
        return values.AsSpan(1, length);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Span<byte> PCMutateDestination(ref Vector<int> value, byte[] destination)
    {
        _predicateCopyOrder += "M";
        value = new Vector<int>(-1);
        return destination.AsSpan(1, Vector<byte>.Count);
    }

    private static void PredicateCopy()
    {
        _predicateCopyChecks = 0;
        Console.WriteLine("-- vector predicate/copy --");
        float allBits = PCFloat(unchecked((int)0xffffffff));
        float otherNaN = PCFloat(0x7fc01234);
        Vector4 values = new(1, 2, 3, 4);
        PC("pc.none.absent", Vector4.None(values, 5));
        PC("pc.none.first", Vector4.None(values, 1), false);
        PC("pc.none.middle", Vector4.None(values, 3), false);
        PC("pc.none.last", Vector4.None(values, 4), false);
        PC("pc.none.all", Vector4.None(new Vector4(2), 2), false);
        PC("pc.none.nan-target", Vector4.None(new Vector4(otherNaN), otherNaN));
        PC("pc.none.mixed-hit", Vector4.None(new Vector4(otherNaN, 1, allBits, 2), 2), false);
        PC("pc.none.mixed-miss", Vector4.None(new Vector4(otherNaN, 1, allBits, 2), 3));
        PC("pc.none.positive-zero", Vector4.None(new Vector4(-0.0f), 0.0f), false);
        PC("pc.none.negative-zero", Vector4.None(new Vector4(0.0f), -0.0f), false);
        PC("pc.none.infinity", Vector4.None(new Vector4(float.PositiveInfinity), float.PositiveInfinity), false);
        PC("pc.none.opposite-infinity", Vector4.None(new Vector4(float.PositiveInfinity), float.NegativeInfinity));
        PC("pc.bits.none", Vector4.NoneWhereAllBitsSet(values));
        PC("pc.bits.first", Vector4.NoneWhereAllBitsSet(new Vector4(allBits, 1, 2, 3)), false);
        PC("pc.bits.last", Vector4.NoneWhereAllBitsSet(new Vector4(1, 2, 3, allBits)), false);
        PC("pc.bits.all", Vector4.NoneWhereAllBitsSet(new Vector4(allBits)), false);
        PC("pc.bits.other-nans", Vector4.NoneWhereAllBitsSet(new Vector4(otherNaN, PCFloat(unchecked((int)0xffc01234)), float.NaN, -0.0f)));
        PC("pc.bits.partial", Vector4.NoneWhereAllBitsSet(new Vector4(PCFloat(0x00ffffff), PCFloat(unchecked((int)0xffffff00)), -0.0f, 0)));
        PC("pc.control.any", Vector4.Any(values, 3));
        PC("pc.control.any-nan", Vector4.Any(new Vector4(otherNaN), otherNaN), false);
        PC("pc.control.any-bits", Vector4.AnyWhereAllBitsSet(new Vector4(1, allBits, 2, 3)));
        PC("pc.control.vector2-padding", Vector2.None(new Vector2(1, 2), 0));
        PC("pc.control.vector3-bits", Vector3.NoneWhereAllBitsSet(new Vector3(1, 2, allBits)), false);

        PC("pc.v64.none", Vector64.None(Vector64.Create(-1, 2), 2), false);
        PC("pc.v64.bits", Vector64.NoneWhereAllBitsSet(Vector64.Create(-1, 2)), false);
        PC("pc.v128.double-none", Vector128.None(Vector128.Create(double.NaN, 1.0), double.NaN));
        PC("pc.v128.double-bits", Vector128.NoneWhereAllBitsSet(Vector128.Create(BitConverter.Int64BitsToDouble(-1L), 1.0)), false);
        PC("pc.v256.none", Vector256.None(Vector256.Create((byte)3).WithElement(31, (byte)255), (byte)255), false);
        PC("pc.v256.bits", Vector256.NoneWhereAllBitsSet(Vector256.Create((byte)3).WithElement(31, (byte)255)), false);
        PC("pc.v512.none", Vector512.None(Vector512.Create(1.0f).WithElement(15, -0.0f), 0.0f), false);
        PC("pc.v512.bits", Vector512.NoneWhereAllBitsSet(Vector512.Create(1.0f).WithElement(15, allBits)), false);
        PC("pc.numerics.none", Vector.None(new Vector<float>(otherNaN), otherNaN));
        PC("pc.numerics.bits-miss", Vector.NoneWhereAllBitsSet(new Vector<float>(otherNaN)));
        PC("pc.numerics.bits-hit", Vector.NoneWhereAllBitsSet(new Vector<float>(allBits)), false);

        Vector4 copy = new(-0.0f, otherNaN, allBits, 1.25f);
        float[] expected = { copy.X, copy.Y, copy.Z, copy.W };
        PC("pc.copy.default", copy.TryCopyTo(default(Span<float>)), false);
        for (int length = 0; length < 4; ++length)
        {
            float[] destination = new float[10];
            Array.Fill(destination, 13.25f);
            PC("pc.copy.short-result." + length, copy.TryCopyTo(destination.AsSpan(2, length)), false);
            PC("pc.copy.short-unchanged." + length, PCFilled(destination, 13.25f));
        }
        foreach (int length in new[] { 4, 6 })
        {
            float[] destination = new float[10];
            Array.Fill(destination, 13.25f);
            PC("pc.copy.success." + length, copy.TryCopyTo(destination.AsSpan(2, length)));
            for (int i = 0; i < destination.Length; ++i)
                PC("pc.copy.bits." + length + "." + i, PCBits(destination[i], i >= 2 && i < 6 ? expected[i - 2] : 13.25f));
        }
        float[] control = new float[4];
        copy.CopyTo(control.AsSpan());
        for (int i = 0; i < control.Length; ++i) PC("pc.control.copy-bits." + i, PCBits(control[i], expected[i]));

        ushort[] small = new ushort[6];
        Array.Fill(small, (ushort)9);
        PC("pc.copy.v64", Vector64.Create((ushort)0x0102, 0x0304, 0x0506, 0x0708).TryCopyTo(small.AsSpan(1, 4)));
        PC("pc.copy.v64-data", small[0] == 9 && small[1] == 0x0102 && small[2] == 0x0304
            && small[3] == 0x0506 && small[4] == 0x0708 && small[5] == 9);
        double[] wide = new double[6];
        Array.Fill(wide, 9.0);
        PC("pc.copy.v256", Vector256.Create(1.0, -0.0, double.PositiveInfinity, BitConverter.Int64BitsToDouble(-1L)).TryCopyTo(wide.AsSpan(1, 4)));
        PC("pc.copy.v256-data", wide[0] == 9 && wide[1] == 1 && BitConverter.DoubleToInt64Bits(wide[2]) == long.MinValue
            && double.IsPositiveInfinity(wide[3]) && BitConverter.DoubleToInt64Bits(wide[4]) == -1L && wide[5] == 9);
        byte[] largest = new byte[70];
        Array.Fill(largest, (byte)9);
        PC("pc.copy.v512", Vector512.Create((byte)0xa5).TryCopyTo(largest.AsSpan(3, 64)));
        bool largestData = true;
        for (int i = 0; i < largest.Length; ++i)
            largestData &= largest[i] == (i >= 3 && i < 67 ? 0xa5 : 9);
        PC("pc.copy.v512-data", largestData);

        int count = Vector<int>.Count;
        Vector<int> numerics = new(7);
        int[] typed = new int[count + 4];
        Array.Fill(typed, 9);
        PC("pc.copy.numerics-short", numerics.TryCopyTo(typed.AsSpan(1, count - 1)), false);
        PC("pc.copy.numerics-short-unchanged", PCFilled(typed, 9));
        PC("pc.copy.numerics", numerics.TryCopyTo(typed.AsSpan(1, count + 1)));
        bool typedData = true;
        for (int i = 0; i < typed.Length; ++i) typedData &= typed[i] == (i >= 1 && i <= count ? 7 : 9);
        PC("pc.copy.numerics-data", typedData);
        int bytes = Vector<byte>.Count;
        byte[] raw = new byte[bytes + 3];
        Array.Fill(raw, (byte)9);
        PC("pc.copy.numerics-byte-short", numerics.TryCopyTo(raw.AsSpan(1, bytes - 1)), false);
        PC("pc.copy.numerics-byte-short-unchanged", PCFilled(raw, 9));
        _predicateCopyOrder = "";
        PC("pc.copy.numerics-byte", numerics.TryCopyTo(PCMutateDestination(ref numerics, raw)));
        PC("pc.copy.numerics-post-argument", numerics[0] == -1 && _predicateCopyOrder == "M");
        bool rawData = true;
        for (int i = 0; i < raw.Length; ++i) rawData &= raw[i] == (i >= 1 && i <= bytes ? 255 : 9);
        PC("pc.copy.numerics-byte-data", rawData);

        Vector<int>[] overlap = { new Vector<int>(11), new Vector<int>(22) };
        Span<int> overlapLanes = MemoryMarshal.Cast<Vector<int>, int>(overlap.AsSpan());
        PC("pc.copy.overlap", overlap[0].TryCopyTo(overlapLanes.Slice(1, count)));
        bool overlapData = true;
        for (int i = 0; i < overlapLanes.Length; ++i) overlapData &= overlapLanes[i] == (i <= count ? 11 : 22);
        PC("pc.copy.overlap-data", overlapData);

        _predicateCopyOrder = "";
        PC("pc.order.predicate", Vector4.None(PCVector(false), PCValue(false)), false);
        PC("pc.order.predicate-once", _predicateCopyOrder == "VS");
        _predicateCopyOrder = "";
        try { Vector4.None(PCVector(true), PCValue(false)); PC("pc.order.predicate-left-missed", false); }
        catch (InvalidOperationException error) { PC("pc.order.predicate-left", error.Message == "vector" && _predicateCopyOrder == "V"); }
        _predicateCopyOrder = "";
        try { Vector4.None(PCVector(false), PCValue(true)); PC("pc.order.predicate-right-missed", false); }
        catch (InvalidOperationException error) { PC("pc.order.predicate-right", error.Message == "scalar" && _predicateCopyOrder == "VS"); }
        float[] ordered = new float[6];
        Array.Fill(ordered, 9.0f);
        _predicateCopyOrder = "";
        PC("pc.order.copy-short", PCVector(false).TryCopyTo(PCSpan(ordered, 3, false)), false);
        PC("pc.order.copy-short-once", _predicateCopyOrder == "VD" && PCFilled(ordered, 9.0f));
        _predicateCopyOrder = "";
        PC("pc.order.copy-success", PCVector(false).TryCopyTo(PCSpan(ordered, 4, false)));
        PC("pc.order.copy-success-once", _predicateCopyOrder == "VD" && ordered[0] == 9 && ordered[1] == 1
            && ordered[2] == 2 && ordered[3] == 3 && ordered[4] == 4 && ordered[5] == 9);
        Array.Fill(ordered, 9.0f);
        _predicateCopyOrder = "";
        try { PCVector(true).TryCopyTo(PCSpan(ordered, 4, false)); PC("pc.order.copy-left-missed", false); }
        catch (InvalidOperationException error) { PC("pc.order.copy-left", error.Message == "vector" && _predicateCopyOrder == "V" && PCFilled(ordered, 9.0f)); }
        _predicateCopyOrder = "";
        try { PCVector(false).TryCopyTo(PCSpan(ordered, 4, true)); PC("pc.order.copy-right-missed", false); }
        catch (InvalidOperationException error) { PC("pc.order.copy-right", error.Message == "destination" && _predicateCopyOrder == "VD" && PCFilled(ordered, 9.0f)); }
        Console.WriteLine("predicate-copy-checks=" + _predicateCopyChecks);
        Console.WriteLine("-- vector predicate/copy complete --");
    }
}
