using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace VectorProbe;

internal static partial class Program
{
    private static int VectorValueChecks;
    private static int VectorValueProductions;

    private static void VectorValueSemantics()
    {
        Console.WriteLine("-- vector value semantics --");
        VectorValueIntegers<byte>(1);
        VectorValueIntegers<sbyte>(1);
        VectorValueIntegers<short>(2);
        VectorValueIntegers<ushort>(2);
        VectorValueIntegers<int>(4);
        VectorValueIntegers<uint>(4);
        VectorValueIntegers<long>(8);
        VectorValueIntegers<ulong>(8);
        // Count is not the oracle: the target's native storage width determines lanes.
        VectorValueIntegers<nint>(IntPtr.Size);
        VectorValueIntegers<nuint>(IntPtr.Size);
        VectorValueFloating<float>(4, new ulong[] {
            0, 0x80000000, 1, 0x80000001, 0x3f800000, 0xbf800000,
            0x7f800000, 0xff800000, 0x7fc00001, 0x7fc12345,
            0xffc54321, 0x7f800001, 0xff800123 });
        VectorValueFloating<double>(8, new ulong[] {
            0, 0x8000000000000000, 1, 0x8000000000000001,
            0x3ff0000000000000, 0xbff0000000000000,
            0x7ff0000000000000, 0xfff0000000000000,
            0x7ff8000000000001, 0x7ff8123456789abc,
            0xfff876543210abcd, 0x7ff0000000000001, 0xfff0000000000123 });
        VectorValueBoxIdentity();
        VectorValueBoxRoundtrips();
        VectorValueTupleWitness();
        Console.WriteLine("vector value twelve closures and IEEE byte oracle passed");
        // Target width and counts are diagnostic, not cross-process parity data.
        Console.Error.WriteLine("vector value target pointer=" + IntPtr.Size
            + " nint-count=" + Vector<nint>.Count + " independent-lanes=" + (16 / IntPtr.Size));
        Console.WriteLine("vector value end");
    }

    private static void VectorValueAssert(bool value, string label)
    {
        if (!value) throw new Exception("vector value: " + label);
        VectorValueChecks++;
    }

    private static unsafe Vector<T> VectorValueSeed<T>(byte[] bytes) where T : unmanaged
    {
        Vector<T> result = default;
        byte* p = (byte*)&result;
        for (int i = 0; i < 16; i++) p[i] = bytes[i];
        return result;
    }

    private static unsafe void VectorValueBytes<T>(Vector<T> value, byte[] expected) where T : unmanaged
    {
        byte* p = (byte*)&value;
        for (int i = 0; i < 16; i++) VectorValueAssert(p[i] == expected[i], "input byte " + i);
    }

    private static byte[] VectorValuePattern(int width, ulong bits)
    {
        byte[] result = new byte[16];
        for (int i = 0; i < 16; i++) result[i] = (byte)(bits >> ((i % width) * 8));
        return result;
    }

    private static ulong VectorValueBits(byte[] bytes, int offset, int width)
    {
        ulong bits = 0;
        for (int i = 0; i < width; i++) bits |= (ulong)bytes[offset + i] << (8 * i);
        return bits;
    }

    // Integer encodings are an independent oracle; no floating operations or vector API.
    private static bool VectorValueLane(ulong a, ulong b, int width, bool floating, bool typed)
    {
        if (!floating) return a == b;
        ulong sign = width == 4 ? 0x80000000UL : 0x8000000000000000UL;
        ulong exponent = width == 4 ? 0x7f800000UL : 0x7ff0000000000000UL;
        ulong fraction = width == 4 ? 0x007fffffUL : 0x000fffffffffffffUL;
        bool na = (a & exponent) == exponent && (a & fraction) != 0;
        bool nb = (b & exponent) == exponent && (b & fraction) != 0;
        if (na || nb) return typed && na && nb;
        return a == b || ((a & ~sign) == 0 && (b & ~sign) == 0);
    }

    private static void VectorValueIntegers<T>(int width) where T : unmanaged
    {
        ulong highBit = 1UL << (width * 8 - 1);
        ulong[] patterns = { 0, ulong.MaxValue, highBit,
            highBit - 1, 0x80ff7f008001ffff, 0x123456789abcdef0 };
        foreach (ulong bits in patterns)
        {
            byte[] a = VectorValuePattern(width, bits);
            VectorValuePair<T>(a, a, width, false);
            for (int lane = 0; lane < 16 / width; lane++)
            {
                byte[] b = (byte[])a.Clone();
                b[lane * width + width - 1] ^= 0x80;
                VectorValuePair<T>(a, b, width, false);
            }
        }
        byte[] distinct = new byte[16];
        for (int i = 0; i < 16; i++) distinct[i] = (byte)(i * 17 + 3);
        VectorValuePair<T>(distinct, distinct, width, false);
    }

    private static void VectorValueFloating<T>(int width, ulong[] encodings) where T : unmanaged
    {
        foreach (ulong a in encodings)
            foreach (ulong b in encodings)
            {
                VectorValuePair<T>(VectorValuePattern(width, a), VectorValuePattern(width, b), width, true);
                // Mix NaN-equal and unequal finite lanes, including the final lane.
                byte[] left = VectorValuePattern(width, a);
                byte[] right = VectorValuePattern(width, b);
                byte[] one = VectorValuePattern(width, width == 4 ? 0x3f800000UL : 0x3ff0000000000000UL);
                for (int i = 16 - width; i < 16; i++) { left[i] = one[i]; right[i] = 0; }
                VectorValuePair<T>(left, right, width, true);
            }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool VectorValueConstrainedEquals<T>(T left, T right) where T : IEquatable<T> => left.Equals(right);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int VectorValueConstrainedHash<T>(T value) where T : struct => value.GetHashCode();

    private static Vector<T> VectorValueProduce<T>(byte[] bytes) where T : unmanaged
    {
        VectorValueProductions++;
        return VectorValueSeed<T>(bytes);
    }

    private static unsafe void VectorValuePair<T>(byte[] a, byte[] b, int width, bool floating) where T : unmanaged
    {
        Vector<T> left = VectorValueSeed<T>(a), right = VectorValueSeed<T>(b);
        VectorValueBytes(left, a); VectorValueBytes(right, b);
        bool typed = true, numeric = true;
        byte[] expectedMask = new byte[16];
        for (int i = 0; i < 16; i += width)
        {
            ulong x = VectorValueBits(a, i, width), y = VectorValueBits(b, i, width);
            typed &= VectorValueLane(x, y, width, floating, true);
            bool equal = VectorValueLane(x, y, width, floating, false);
            numeric &= equal;
            for (int j = 0; j < width; j++) expectedMask[i + j] = equal ? (byte)255 : (byte)0;
        }
        VectorValueAssert(left.Equals(right) == typed, "typed");
        VectorValueAssert(VectorValueConstrainedEquals(left, right) == typed, "constrained");
        VectorValueAssert((left == right) == numeric && (left != right) != numeric, "numeric operators");
        VectorValueAssert(Vector.EqualsAll(left, right) == numeric, "numeric all");
        VectorValueBytes(Vector.Equals(left, right), expectedMask);
        var comparer = EqualityComparer<Vector<T>>.Default;
        VectorValueAssert(comparer.Equals(left, right) == typed, "default comparer");
        var tupleA = (left, left); var tupleB = (right, right);
        VectorValueAssert(tupleA.Equals(tupleB) == typed && tupleA.Equals((object)tupleB) == typed, "tuple");
        VectorValueAssert((Array.IndexOf(new[] { left }, right) == 0) == typed, "array search");
        var list = new List<Vector<T>> { left };
        VectorValueAssert(list.Contains(right) == typed && (list.IndexOf(right) == 0) == typed, "list search");
        object boxA = left, boxB = right;
        VectorValueAssert(left.Equals(boxB) == typed && boxA.Equals(boxB) == typed
            && object.Equals(boxA, boxB) == typed, "direct and boxed object");
        VectorValueAssert(((IEquatable<Vector<T>>)boxA).Equals(right) == typed, "boxed interface");
        VectorValueAssert(EqualityComparer<object>.Default.Equals(boxA, boxB) == typed, "object comparer");
        VectorValueAssert(boxA.Equals(boxA) && !boxA.Equals(null) && !boxA.Equals("vector"), "boxed controls");
        int hash = left.GetHashCode();
        VectorValueAssert(hash == VectorValueConstrainedHash(left) && hash == comparer.GetHashCode(left)
            && hash == boxA.GetHashCode(), "hash routes");
        if (typed)
        {
            VectorValueAssert(hash == right.GetHashCode() && hash == boxB.GetHashCode(), "equal hash");
            VectorValueAssert(tupleA.GetHashCode() == tupleB.GetHashCode(), "tuple hash");
        }
        var dictionary = new Dictionary<Vector<T>, int> { [left] = 1 };
        VectorValueAssert(dictionary.ContainsKey(right) == typed, "dictionary lookup");
        dictionary[right] = 2;
        VectorValueAssert(dictionary.Count == (typed ? 1 : 2) && dictionary[right] == 2, "dictionary update");
        VectorValueAssert(dictionary.Remove(right) && dictionary.ContainsKey(left) != typed, "dictionary remove");
        var set = new HashSet<Vector<T>> { left };
        VectorValueAssert(set.Add(right) != typed && set.Count == (typed ? 1 : 2), "set dedup");
        var objects = new Dictionary<object, int> { [boxA] = 7 };
        VectorValueAssert(objects.ContainsKey(boxB) == typed, "object dictionary");
        var custom = new VectorValueAlwaysComparer<T>();
        var customSet = new HashSet<Vector<T>>(custom) { left };
        VectorValueAssert(!customSet.Add(right) && custom.EqualsCalls > 0 && custom.HashCalls >= 2, "custom comparer honored");
        VectorValueProductions = 0;
        VectorValueAssert(VectorValueProduce<T>(a).Equals(VectorValueProduce<T>(b)) == typed
            && VectorValueProductions == 2, "evaluate operands once");
        VectorValueProductions = 0;
        VectorValueAssert(VectorValueProduce<T>(a).GetHashCode() == hash
            && VectorValueProductions == 1, "evaluate hash receiver once");
        VectorValueBytes(left, a); VectorValueBytes(right, b);
    }

    private sealed class VectorValueAlwaysComparer<T> : IEqualityComparer<Vector<T>> where T : unmanaged
    {
        public int EqualsCalls;
        public int HashCalls;
        public bool Equals(Vector<T> x, Vector<T> y) { EqualsCalls++; return true; }
        public int GetHashCode(Vector<T> value) { HashCalls++; return 0; }
    }

    private static void VectorValueBoxIdentity()
    {
        byte[] bits = VectorValuePattern(8, 0);
        object a = VectorValueSeed<int>(bits), b = VectorValueSeed<uint>(bits);
        object c = default(Vector128<int>);
        VectorValueAssert(!a.Equals(b) && !b.Equals(a) && !a.Equals(c), "exact boxed type");
        VectorValueAssert(!VectorValueSeed<int>(bits).Equals(b)
            && !VectorValueSeed<int>(bits).Equals(null), "direct object overload");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T VectorValueUnbox<T>(object value) => (T)value;

    private static void VectorValueInvalidUnbox<T>(object value)
    {
        bool rejected = false;
        try { VectorValueUnbox<T>(value); }
        catch (InvalidCastException) { rejected = true; }
        VectorValueAssert(rejected, "incorrect unbox type");
    }

    private static void VectorValueBoxRoundtrips()
    {
        byte[] bytes = new byte[16];
        for (int i = 0; i < 16; i++) bytes[i] = (byte)(i * 17 + 3);
        byte[] nanBytes = VectorValuePattern(8, 0xfff0000000000123);
        Vector<byte> small = VectorValueSeed<byte>(bytes);
        Vector<double> floating = VectorValueSeed<double>(nanBytes);
        object smallBox = small, floatingBox = floating;
        VectorValueBytes(VectorValueUnbox<Vector<byte>>(smallBox), bytes);
        VectorValueBytes(VectorValueUnbox<Vector<double>>(floatingBox), nanBytes);
        var tuple = (floating, floating);
        object tupleBox = tuple;
        var roundtrip = VectorValueUnbox<ValueTuple<Vector<double>, Vector<double>>>(tupleBox);
        VectorValueBytes(roundtrip.Item1, nanBytes);
        VectorValueBytes(roundtrip.Item2, nanBytes);
        Vector<double>? nullable = floating;
        object nullableBox = nullable;
        var nullableRoundtrip = VectorValueUnbox<Vector<double>?>(nullableBox);
        VectorValueAssert(nullableRoundtrip.HasValue, "nullable vector has value");
        VectorValueBytes(nullableRoundtrip.Value, nanBytes);
        ValueTuple<Vector<double>, Vector<double>>? nullableTuple = tuple;
        object nullableTupleBox = nullableTuple;
        var nullableTupleRoundtrip = VectorValueUnbox<ValueTuple<Vector<double>, Vector<double>>?>(nullableTupleBox);
        VectorValueAssert(nullableTupleRoundtrip.HasValue, "nullable tuple has value");
        VectorValueBytes(nullableTupleRoundtrip.Value.Item1, nanBytes);
        VectorValueBytes(nullableTupleRoundtrip.Value.Item2, nanBytes);
        VectorValueAssert(!VectorValueUnbox<Vector<double>?>(null).HasValue
            && !VectorValueUnbox<ValueTuple<Vector<double>, Vector<double>>?>(null).HasValue,
            "null nullable unbox");
        VectorValueInvalidUnbox<Vector<double>>(smallBox);
        VectorValueInvalidUnbox<Vector<double>?>(smallBox);
        VectorValueInvalidUnbox<ValueTuple<Vector<double>, Vector<double>>>(floatingBox);
        VectorValueInvalidUnbox<ValueTuple<Vector<double>, Vector<double>>?>(floatingBox);
        bool rejectedNull = false;
        try { VectorValueUnbox<Vector<double>>(null); }
        catch (NullReferenceException) { rejectedNull = true; }
        VectorValueAssert(rejectedNull, "null nonnullable unbox");
        byte? promotedByte = 254;
        sbyte? promotedSByte = -117;
        short? promotedShort = -30123;
        ushort? promotedUShort = 60123;
        VectorValueAssert(VectorValueUnbox<byte?>((object)promotedByte).Value == 254
            && VectorValueUnbox<sbyte?>((object)promotedSByte).Value == -117
            && VectorValueUnbox<short?>((object)promotedShort).Value == -30123
            && VectorValueUnbox<ushort?>((object)promotedUShort).Value == 60123,
            "nullable promoted small integer unbox");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void VectorValueTupleWitness()
    {
        Vector<double> a = VectorValueSeed<double>(VectorValuePattern(8, 0x7ff0000000000001));
        Vector<double> b = VectorValueSeed<double>(VectorValuePattern(8, 0xfff8123456789abc));
        var left = (a, a); var right = (b, b);
        VectorValueAssert(left.Equals(right) && left.Equals((object)right), "retained double tuple equality");
        VectorValueAssert(left.GetHashCode() == right.GetHashCode(), "retained double tuple hash");
        // A boxed receiver is distinct from the value receiver's Equals(object) overload.
        object vectorValueTupleBoxLeft = left, vectorValueTupleBoxRight = right;
        VectorValueAssert(vectorValueTupleBoxLeft.Equals(vectorValueTupleBoxRight)
            && !vectorValueTupleBoxLeft.Equals(null) && !vectorValueTupleBoxLeft.Equals(a),
            "boxed tuple virtual equality");
        VectorValueAssert(vectorValueTupleBoxLeft.GetHashCode() == left.GetHashCode()
            && vectorValueTupleBoxLeft.GetHashCode() == vectorValueTupleBoxRight.GetHashCode(),
            "boxed tuple virtual hash");
        delegate*<Vector<double>, Vector<double>, bool> wrapper = &VectorValueAddressWrapper;
        VectorValueAssert(wrapper(a, b), "application wrapper calli");
        Func<Vector<double>, bool> vectorValueEqualsAddress = a.Equals;
        Func<object, bool> vectorValueObjectAddress = a.Equals;
        Func<int> vectorValueHashAddress = a.GetHashCode;
        VectorValueAssert(vectorValueEqualsAddress(b), "intrinsic typed method group");
        VectorValueAssert(vectorValueObjectAddress(b) && !vectorValueObjectAddress(null)
            && !vectorValueObjectAddress(default(Vector<long>)), "intrinsic object method group");
        VectorValueAssert(vectorValueHashAddress() == b.GetHashCode(), "intrinsic hash method group");
        Console.WriteLine("vector value tuple equality/hash and application wrapper passed");
    }

    private static bool VectorValueAddressWrapper(Vector<double> left, Vector<double> right) => left.Equals(right);
}
