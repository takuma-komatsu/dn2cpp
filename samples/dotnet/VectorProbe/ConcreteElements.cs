using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace VectorProbe;

internal static partial class Program
{
    private static int ConcreteChecks;
    private static int ConcreteOrder;
    private static int ConcreteThrowAt;

    private static void ConcreteCheck(bool condition)
    {
        if (!condition) throw new InvalidOperationException("concrete vector element mismatch");
        ConcreteChecks++;
    }

    // CoreCLR's Vector4 JIT intrinsic omits the parameter; its managed fallback
    // names index. Gates retain both raw oracles and diff native output strictly
    // against the hardware-intrinsics-disabled CLR profile.
    private static void ConcreteVector4Exception(ArgumentOutOfRangeException error)
    {
        ConcreteCheck(error.ActualValue == null && (error.ParamName == null || error.ParamName == "index"));
        ConcreteCheck(error.Message == (error.ParamName == null
            ? "Specified argument was out of the range of valid values."
            : "Specified argument was out of the range of valid values. (Parameter 'index')"));
    }

    private static int ConcreteBits(float value) => BitConverter.SingleToInt32Bits(value);
    private static float ConcreteFloat(int value) => BitConverter.Int32BitsToSingle(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ConcreteEffect(int step)
    {
        ConcreteOrder = ConcreteOrder * 10 + step;
        if (ConcreteThrowAt == step) throw new InvalidOperationException("operand");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ConcreteIndex(int value) { ConcreteEffect(2); return value; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float ConcreteValue() { ConcreteEffect(3); return ConcreteFloat(unchecked((int)0x80000000)); }

    private static void ConcreteElements()
    {
        Console.WriteLine("-- concrete vector elements --");
        ConcreteChecks = 0;
        ConcreteCases2();
        ConcreteCases3();
        ConcreteCases4();
        ConcreteCallables();
        Matrix3x2 matrix = new Matrix3x2(1, 2, 3, 4, 5, 6);
        for (int row = 0; row < 3; row++)
            for (int column = 0; column < 2; column++)
            {
                ConcreteCheck(matrix[row, column] == row * 2 + column + 1);
                Matrix3x2 copy = matrix;
                copy[row, column] = ConcreteFloat(unchecked((int)0xffc12345));
                for (int r = 0; r < 3; r++)
                    for (int c = 0; c < 2; c++)
                        ConcreteCheck(ConcreteBits(copy[r, c]) == (r == row && c == column
                            ? unchecked((int)0xffc12345) : ConcreteBits(matrix[r, c])));
            }
        Console.WriteLine("concrete vector element bit/copy/bounds/order checks=" + ConcreteChecks);
    }

    private static float ConcreteLane(Vector2 value, int index) =>
        index == 0 ? value.X : value.Y;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector2 ConcreteSource2() { ConcreteEffect(1); return new Vector2(1, 2); }

    private static void ConcreteCases2()
    {
        int[] bits = { 0, unchecked((int)0x80000000), 1, unchecked((int)0x80000001),
            0x7fc12345, unchecked((int)0xffc54321), 0x7f812345, 0x7f800000 };
        for (int rotation = 0; rotation < bits.Length; rotation++)
        {
            Vector2 original = new Vector2(ConcreteFloat(bits[(rotation + 0) % bits.Length]), ConcreteFloat(bits[(rotation + 1) % bits.Length]));
            for (int index = 0; index < 2; index++)
            {
                ConcreteCheck(ConcreteBits(Vector.GetElement(original, index)) == bits[(rotation + index) % bits.Length]);
                ConcreteCheck(ConcreteBits(original[index]) == bits[(rotation + index) % bits.Length]);
                foreach (int replacement in bits)
                {
                    Vector2 result = Vector.WithElement(original, index, ConcreteFloat(replacement));
                    Vector2 indexed = original;
                    indexed[index] = ConcreteFloat(replacement);
                    for (int lane = 0; lane < 2; lane++)
                    {
                        int expected = lane == index ? replacement : bits[(rotation + lane) % bits.Length];
                        ConcreteCheck(ConcreteBits(ConcreteLane(result, lane)) == expected);
                        ConcreteCheck(ConcreteBits(ConcreteLane(indexed, lane)) == expected);
                        ConcreteCheck(ConcreteBits(ConcreteLane(original, lane)) == bits[(rotation + lane) % bits.Length]);
                    }
                }
            }
        }
        foreach (int index in new[] { -1, int.MinValue, 2, int.MaxValue })
        {
            Vector2 source = new Vector2(1, 2);
            try { Vector.GetElement(source, index); ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteCheck(error.ParamName == "index" && error.ActualValue == null); Console.Error.WriteLine("v2 get " + index + ":" + error.ParamName + ":" + error.Message); }
            try { Vector.WithElement(source, index, 0); ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteCheck(error.ParamName == "index" && error.ActualValue == null); Console.Error.WriteLine("v2 with " + index + ":" + error.ParamName + ":" + error.Message); }
            try { float unused = source[index]; ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteCheck(error.ParamName == "index" && error.ActualValue == null); }
            try { source[index] = 0; ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteCheck(error.ParamName == "index" && error.ActualValue == null); }
            for (int lane = 0; lane < 2; lane++) ConcreteCheck(ConcreteLane(source, lane) == lane + 1);
        }
        foreach (int index in new[] { 0, -1, 2, int.MaxValue })
        {
            ConcreteOrder = 0;
            ConcreteThrowAt = 0;
            try
            {
                float value = Vector.GetElement(ConcreteSource2(), ConcreteIndex(index));
                ConcreteCheck(index == 0 && value == 1);
            }
            catch (ArgumentOutOfRangeException) { ConcreteCheck(index != 0); }
            ConcreteCheck(ConcreteOrder == 12);
            ConcreteOrder = 0;
            try
            {
                Vector2 value = Vector.WithElement(ConcreteSource2(), ConcreteIndex(index), ConcreteValue());
                ConcreteCheck(index == 0 && ConcreteBits(value.X) == unchecked((int)0x80000000));
            }
            catch (ArgumentOutOfRangeException) { ConcreteCheck(index != 0); }
            ConcreteCheck(ConcreteOrder == 123);
        }
        for (int step = 1; step <= 3; step++)
        {
            ConcreteOrder = 0;
            ConcreteThrowAt = step;
            bool caught = false;
            try { Vector.WithElement(ConcreteSource2(), ConcreteIndex(-1), ConcreteValue()); }
            catch (InvalidOperationException error) { caught = error.Message == "operand"; }
            ConcreteCheck(caught && ConcreteOrder == (step == 1 ? 1 : step == 2 ? 12 : 123));
        }
        for (int step = 1; step <= 2; step++)
        {
            ConcreteOrder = 0;
            ConcreteThrowAt = step;
            bool caught = false;
            try { Vector.GetElement(ConcreteSource2(), ConcreteIndex(-1)); }
            catch (InvalidOperationException error) { caught = error.Message == "operand"; }
            ConcreteCheck(caught && ConcreteOrder == (step == 1 ? 1 : 12));
        }
        ConcreteThrowAt = 0;
    }

    private static float ConcreteLane(Vector3 value, int index) =>
        index == 0 ? value.X : index == 1 ? value.Y : value.Z;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector3 ConcreteSource3() { ConcreteEffect(1); return new Vector3(1, 2, 3); }

    private static void ConcreteCases3()
    {
        int[] bits = { 0, unchecked((int)0x80000000), 1, unchecked((int)0x80000001),
            0x7fc12345, unchecked((int)0xffc54321), 0x7f812345, 0x7f800000 };
        for (int rotation = 0; rotation < bits.Length; rotation++)
        {
            Vector3 original = new Vector3(ConcreteFloat(bits[(rotation + 0) % bits.Length]), ConcreteFloat(bits[(rotation + 1) % bits.Length]), ConcreteFloat(bits[(rotation + 2) % bits.Length]));
            for (int index = 0; index < 3; index++)
            {
                ConcreteCheck(ConcreteBits(Vector.GetElement(original, index)) == bits[(rotation + index) % bits.Length]);
                ConcreteCheck(ConcreteBits(original[index]) == bits[(rotation + index) % bits.Length]);
                foreach (int replacement in bits)
                {
                    Vector3 result = Vector.WithElement(original, index, ConcreteFloat(replacement));
                    Vector3 indexed = original;
                    indexed[index] = ConcreteFloat(replacement);
                    for (int lane = 0; lane < 3; lane++)
                    {
                        int expected = lane == index ? replacement : bits[(rotation + lane) % bits.Length];
                        ConcreteCheck(ConcreteBits(ConcreteLane(result, lane)) == expected);
                        ConcreteCheck(ConcreteBits(ConcreteLane(indexed, lane)) == expected);
                        ConcreteCheck(ConcreteBits(ConcreteLane(original, lane)) == bits[(rotation + lane) % bits.Length]);
                    }
                }
            }
        }
        foreach (int index in new[] { -1, int.MinValue, 3, int.MaxValue })
        {
            Vector3 source = new Vector3(1, 2, 3);
            try { Vector.GetElement(source, index); ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteCheck(error.ParamName == "index" && error.ActualValue == null); Console.Error.WriteLine("v3 get " + index + ":" + error.ParamName + ":" + error.Message); }
            try { Vector.WithElement(source, index, 0); ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteCheck(error.ParamName == "index" && error.ActualValue == null); Console.Error.WriteLine("v3 with " + index + ":" + error.ParamName + ":" + error.Message); }
            try { float unused = source[index]; ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteCheck(error.ParamName == "index" && error.ActualValue == null); }
            try { source[index] = 0; ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteCheck(error.ParamName == "index" && error.ActualValue == null); }
            for (int lane = 0; lane < 3; lane++) ConcreteCheck(ConcreteLane(source, lane) == lane + 1);
        }
        foreach (int index in new[] { 0, -1, 3, int.MaxValue })
        {
            ConcreteOrder = 0;
            ConcreteThrowAt = 0;
            try
            {
                float value = Vector.GetElement(ConcreteSource3(), ConcreteIndex(index));
                ConcreteCheck(index == 0 && value == 1);
            }
            catch (ArgumentOutOfRangeException) { ConcreteCheck(index != 0); }
            ConcreteCheck(ConcreteOrder == 12);
            ConcreteOrder = 0;
            try
            {
                Vector3 value = Vector.WithElement(ConcreteSource3(), ConcreteIndex(index), ConcreteValue());
                ConcreteCheck(index == 0 && ConcreteBits(value.X) == unchecked((int)0x80000000));
            }
            catch (ArgumentOutOfRangeException) { ConcreteCheck(index != 0); }
            ConcreteCheck(ConcreteOrder == 123);
        }
        for (int step = 1; step <= 3; step++)
        {
            ConcreteOrder = 0;
            ConcreteThrowAt = step;
            bool caught = false;
            try { Vector.WithElement(ConcreteSource3(), ConcreteIndex(-1), ConcreteValue()); }
            catch (InvalidOperationException error) { caught = error.Message == "operand"; }
            ConcreteCheck(caught && ConcreteOrder == (step == 1 ? 1 : step == 2 ? 12 : 123));
        }
        for (int step = 1; step <= 2; step++)
        {
            ConcreteOrder = 0;
            ConcreteThrowAt = step;
            bool caught = false;
            try { Vector.GetElement(ConcreteSource3(), ConcreteIndex(-1)); }
            catch (InvalidOperationException error) { caught = error.Message == "operand"; }
            ConcreteCheck(caught && ConcreteOrder == (step == 1 ? 1 : 12));
        }
        ConcreteThrowAt = 0;
    }

    private static float ConcreteLane(Vector4 value, int index) =>
        index == 0 ? value.X : index == 1 ? value.Y : index == 2 ? value.Z : value.W;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector4 ConcreteSource4() { ConcreteEffect(1); return new Vector4(1, 2, 3, 4); }

    private static void ConcreteCases4()
    {
        int[] bits = { 0, unchecked((int)0x80000000), 1, unchecked((int)0x80000001),
            0x7fc12345, unchecked((int)0xffc54321), 0x7f812345, 0x7f800000 };
        for (int rotation = 0; rotation < bits.Length; rotation++)
        {
            Vector4 original = new Vector4(ConcreteFloat(bits[(rotation + 0) % bits.Length]), ConcreteFloat(bits[(rotation + 1) % bits.Length]), ConcreteFloat(bits[(rotation + 2) % bits.Length]), ConcreteFloat(bits[(rotation + 3) % bits.Length]));
            for (int index = 0; index < 4; index++)
            {
                ConcreteCheck(ConcreteBits(Vector.GetElement(original, index)) == bits[(rotation + index) % bits.Length]);
                ConcreteCheck(ConcreteBits(original[index]) == bits[(rotation + index) % bits.Length]);
                foreach (int replacement in bits)
                {
                    Vector4 result = Vector.WithElement(original, index, ConcreteFloat(replacement));
                    Vector4 indexed = original;
                    indexed[index] = ConcreteFloat(replacement);
                    for (int lane = 0; lane < 4; lane++)
                    {
                        int expected = lane == index ? replacement : bits[(rotation + lane) % bits.Length];
                        ConcreteCheck(ConcreteBits(ConcreteLane(result, lane)) == expected);
                        ConcreteCheck(ConcreteBits(ConcreteLane(indexed, lane)) == expected);
                        ConcreteCheck(ConcreteBits(ConcreteLane(original, lane)) == bits[(rotation + lane) % bits.Length]);
                    }
                }
            }
        }
        foreach (int index in new[] { -1, int.MinValue, 4, int.MaxValue })
        {
            Vector4 source = new Vector4(1, 2, 3, 4);
            try { Vector.GetElement(source, index); ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteVector4Exception(error); Console.Error.WriteLine("v4 get " + index + ":" + error.ParamName + ":" + error.Message); }
            try { Vector.WithElement(source, index, 0); ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteVector4Exception(error); Console.Error.WriteLine("v4 with " + index + ":" + error.ParamName + ":" + error.Message); }
            try { float unused = source[index]; ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteVector4Exception(error); }
            try { source[index] = 0; ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteVector4Exception(error); }
            for (int lane = 0; lane < 4; lane++) ConcreteCheck(ConcreteLane(source, lane) == lane + 1);
        }
        foreach (int index in new[] { 0, -1, 4, int.MaxValue })
        {
            ConcreteOrder = 0;
            ConcreteThrowAt = 0;
            try
            {
                float value = Vector.GetElement(ConcreteSource4(), ConcreteIndex(index));
                ConcreteCheck(index == 0 && value == 1);
            }
            catch (ArgumentOutOfRangeException) { ConcreteCheck(index != 0); }
            ConcreteCheck(ConcreteOrder == 12);
            ConcreteOrder = 0;
            try
            {
                Vector4 value = Vector.WithElement(ConcreteSource4(), ConcreteIndex(index), ConcreteValue());
                ConcreteCheck(index == 0 && ConcreteBits(value.X) == unchecked((int)0x80000000));
            }
            catch (ArgumentOutOfRangeException) { ConcreteCheck(index != 0); }
            ConcreteCheck(ConcreteOrder == 123);
        }
        for (int step = 1; step <= 3; step++)
        {
            ConcreteOrder = 0;
            ConcreteThrowAt = step;
            bool caught = false;
            try { Vector.WithElement(ConcreteSource4(), ConcreteIndex(-1), ConcreteValue()); }
            catch (InvalidOperationException error) { caught = error.Message == "operand"; }
            ConcreteCheck(caught && ConcreteOrder == (step == 1 ? 1 : step == 2 ? 12 : 123));
        }
        for (int step = 1; step <= 2; step++)
        {
            ConcreteOrder = 0;
            ConcreteThrowAt = step;
            bool caught = false;
            try { Vector.GetElement(ConcreteSource4(), ConcreteIndex(-1)); }
            catch (InvalidOperationException error) { caught = error.Message == "operand"; }
            ConcreteCheck(caught && ConcreteOrder == (step == 1 ? 1 : 12));
        }
        ConcreteThrowAt = 0;
    }

    private static unsafe void ConcreteCallables()
    {
        Func<Vector2, int, float> get2 = Vector.GetElement;
        Func<Vector2, int, float, Vector2> with2 = Vector.WithElement;
        delegate* managed<Vector2, int, float> getPointer2 = &Vector.GetElement;
        delegate* managed<Vector2, int, float, Vector2> withPointer2 = &Vector.WithElement;
        Vector2 source2 = new Vector2(ConcreteFloat(unchecked((int)0x80000000)), ConcreteFloat(0x7fc12345));
        for (int lane = 0; lane < 2; lane++)
        {
            int expected = ConcreteBits(ConcreteLane(source2, lane));
            ConcreteCheck(ConcreteBits(get2(source2, lane)) == expected);
            ConcreteCheck(ConcreteBits(getPointer2(source2, lane)) == expected);
            Vector2 result = with2(source2, lane, ConcreteFloat(unchecked((int)0xffc12345)));
            Vector2 pointerResult = withPointer2(source2, lane, ConcreteFloat(unchecked((int)0xffc12345)));
            for (int component = 0; component < 2; component++)
            {
                int bits = component == lane ? unchecked((int)0xffc12345) : ConcreteBits(ConcreteLane(source2, component));
                ConcreteCheck(ConcreteBits(ConcreteLane(result, component)) == bits);
                ConcreteCheck(ConcreteBits(ConcreteLane(pointerResult, component)) == bits);
            }
        }
        foreach (int index in new[] { -1, int.MinValue, 2, int.MaxValue })
        {
            try { get2(source2, index); ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteCheck(error.ParamName == "index" && error.ActualValue == null); }
            try { getPointer2(source2, index); ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteCheck(error.ParamName == "index" && error.ActualValue == null); }
            try { with2(source2, index, 0); ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteCheck(error.ParamName == "index" && error.ActualValue == null); }
            try { withPointer2(source2, index, 0); ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteCheck(error.ParamName == "index" && error.ActualValue == null); }
        }
        Func<Vector3, int, float> get3 = Vector.GetElement;
        Func<Vector3, int, float, Vector3> with3 = Vector.WithElement;
        delegate* managed<Vector3, int, float> getPointer3 = &Vector.GetElement;
        delegate* managed<Vector3, int, float, Vector3> withPointer3 = &Vector.WithElement;
        Vector3 source3 = new Vector3(ConcreteFloat(unchecked((int)0x80000000)), ConcreteFloat(0x7fc12345), ConcreteFloat(1));
        for (int lane = 0; lane < 3; lane++)
        {
            int expected = ConcreteBits(ConcreteLane(source3, lane));
            ConcreteCheck(ConcreteBits(get3(source3, lane)) == expected);
            ConcreteCheck(ConcreteBits(getPointer3(source3, lane)) == expected);
            Vector3 result = with3(source3, lane, ConcreteFloat(unchecked((int)0xffc12345)));
            Vector3 pointerResult = withPointer3(source3, lane, ConcreteFloat(unchecked((int)0xffc12345)));
            for (int component = 0; component < 3; component++)
            {
                int bits = component == lane ? unchecked((int)0xffc12345) : ConcreteBits(ConcreteLane(source3, component));
                ConcreteCheck(ConcreteBits(ConcreteLane(result, component)) == bits);
                ConcreteCheck(ConcreteBits(ConcreteLane(pointerResult, component)) == bits);
            }
        }
        foreach (int index in new[] { -1, int.MinValue, 3, int.MaxValue })
        {
            try { get3(source3, index); ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteCheck(error.ParamName == "index" && error.ActualValue == null); }
            try { getPointer3(source3, index); ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteCheck(error.ParamName == "index" && error.ActualValue == null); }
            try { with3(source3, index, 0); ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteCheck(error.ParamName == "index" && error.ActualValue == null); }
            try { withPointer3(source3, index, 0); ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteCheck(error.ParamName == "index" && error.ActualValue == null); }
        }
        Func<Vector4, int, float> get4 = Vector.GetElement;
        Func<Vector4, int, float, Vector4> with4 = Vector.WithElement;
        delegate* managed<Vector4, int, float> getPointer4 = &Vector.GetElement;
        delegate* managed<Vector4, int, float, Vector4> withPointer4 = &Vector.WithElement;
        Vector4 source4 = new Vector4(ConcreteFloat(unchecked((int)0x80000000)), ConcreteFloat(0x7fc12345), ConcreteFloat(1), ConcreteFloat(unchecked((int)0xffc54321)));
        for (int lane = 0; lane < 4; lane++)
        {
            int expected = ConcreteBits(ConcreteLane(source4, lane));
            ConcreteCheck(ConcreteBits(get4(source4, lane)) == expected);
            ConcreteCheck(ConcreteBits(getPointer4(source4, lane)) == expected);
            Vector4 result = with4(source4, lane, ConcreteFloat(unchecked((int)0xffc12345)));
            Vector4 pointerResult = withPointer4(source4, lane, ConcreteFloat(unchecked((int)0xffc12345)));
            for (int component = 0; component < 4; component++)
            {
                int bits = component == lane ? unchecked((int)0xffc12345) : ConcreteBits(ConcreteLane(source4, component));
                ConcreteCheck(ConcreteBits(ConcreteLane(result, component)) == bits);
                ConcreteCheck(ConcreteBits(ConcreteLane(pointerResult, component)) == bits);
            }
        }
        foreach (int index in new[] { -1, int.MinValue, 4, int.MaxValue })
        {
            try { get4(source4, index); ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteVector4Exception(error); }
            try { getPointer4(source4, index); ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteVector4Exception(error); }
            try { with4(source4, index, 0); ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteVector4Exception(error); }
            try { withPointer4(source4, index, 0); ConcreteCheck(false); }
            catch (ArgumentOutOfRangeException error) { ConcreteVector4Exception(error); }
        }
        Console.WriteLine("concrete vector six delegate and calli targets passed");
    }
}
