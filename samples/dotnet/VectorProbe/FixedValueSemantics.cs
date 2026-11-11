using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace VectorProbe;

internal static partial class Program
{
    private static int FixedValueChecks;
    private static int FixedValueProductions;
    private static int FixedValueReceiverEffects;
    private static int FixedValueRightEffects;
    private static int FixedValueEffectOrder;

    private static void FixedValueSemantics()
    {
        Console.WriteLine("-- fixed vector value semantics --");
        FixedValueCases128<byte>(1, false);
        FixedValueCases128<sbyte>(1, false);
        FixedValueCases128<short>(2, false);
        FixedValueCases128<ushort>(2, false);
        FixedValueCases128<int>(4, false);
        FixedValueCases128<uint>(4, false);
        FixedValueCases128<long>(8, false);
        FixedValueCases128<ulong>(8, false);
        FixedValueCases128<nint>(IntPtr.Size, false);
        FixedValueCases128<nuint>(IntPtr.Size, false);
        FixedValueCases128<float>(4, true);
        FixedValueCases128<double>(8, true);
        FixedValueIntegrationCases<Vector128<int>>(16, 4, false);
        FixedValueIntegrationCases<Vector128<nint>>(16, IntPtr.Size, false);
        FixedValueIntegrationCases<Vector128<float>>(16, 4, true);
        FixedValueIntegrationCases<Vector128<double>>(16, 8, true);
        Console.WriteLine("fixed vector 128 collection integration passed");
        FixedValueTupleWitness128();
        Console.WriteLine("fixed vector 128 twelve closures and all-lane IEEE oracle passed");
        FixedValueCases256<byte>(1, false);
        FixedValueCases256<sbyte>(1, false);
        FixedValueCases256<short>(2, false);
        FixedValueCases256<ushort>(2, false);
        FixedValueCases256<int>(4, false);
        FixedValueCases256<uint>(4, false);
        FixedValueCases256<long>(8, false);
        FixedValueCases256<ulong>(8, false);
        FixedValueCases256<nint>(IntPtr.Size, false);
        FixedValueCases256<nuint>(IntPtr.Size, false);
        FixedValueCases256<float>(4, true);
        FixedValueCases256<double>(8, true);
        FixedValueIntegrationCases<Vector256<int>>(32, 4, false);
        FixedValueIntegrationCases<Vector256<nint>>(32, IntPtr.Size, false);
        FixedValueIntegrationCases<Vector256<float>>(32, 4, true);
        FixedValueIntegrationCases<Vector256<double>>(32, 8, true);
        Console.WriteLine("fixed vector 256 collection integration passed");
        FixedValueTupleWitness256();
        Console.WriteLine("fixed vector 256 twelve closures and all-lane IEEE oracle passed");
        FixedValueNullReceiver();
        Console.Error.WriteLine("fixed vector target pointer=" + IntPtr.Size
            + " independent-native-lanes=" + (16 / IntPtr.Size) + "," + (32 / IntPtr.Size));
        FixedValueAssert(FixedValueChecks > 0, "executed assertions");
        Console.WriteLine("fixed vector value end");
    }

    // Equals(object) tests the argument's type before it reads the receiver.
    private static void FixedValueNullReceiver()
    {
        object foreign = Vector128.Create(1L);
        FixedValueAssert(!Unsafe.NullRef<Vector128<int>>().Equals((object)null)
            && !Unsafe.NullRef<Vector128<int>>().Equals(foreign), "null 128 receiver");
        FixedValueAssert(!Unsafe.NullRef<Vector256<int>>().Equals((object)null)
            && !Unsafe.NullRef<Vector256<int>>().Equals(foreign), "null 256 receiver");
        Console.WriteLine("fixed vector null receiver object equality passed");
    }

    private static void FixedValueAssert(bool value, string label)
    {
        if (!value) throw new Exception("fixed vector value: " + label);
        FixedValueChecks++;
    }

    private static unsafe V FixedValueSeed<V>(byte[] bytes) where V : unmanaged
    {
        V value = default;
        FixedValueAssert(sizeof(V) == bytes.Length, "storage width");
        byte* pointer = (byte*)&value;
        for (int i = 0; i < bytes.Length; i++) pointer[i] = bytes[i];
        return value;
    }

    private static unsafe void FixedValueBytes<V>(V value, byte[] bytes) where V : unmanaged
    {
        FixedValueAssert(sizeof(V) == bytes.Length, "byte width");
        byte* pointer = (byte*)&value;
        for (int i = 0; i < bytes.Length; i++) FixedValueAssert(pointer[i] == bytes[i], "payload byte " + i);
    }

    private static byte[] FixedValuePattern(int length, int width, ulong bits)
    {
        byte[] bytes = new byte[length];
        for (int i = 0; i < length; i++) bytes[i] = (byte)(bits >> ((i % width) * 8));
        return bytes;
    }

    private static ulong FixedValueBits(byte[] bytes, int offset, int width)
    {
        ulong bits = 0;
        for (int i = 0; i < width; i++) bits |= (ulong)bytes[offset + i] << (8 * i);
        return bits;
    }

    // IEEE integer encodings distinguish scalar value equality from numeric equality.
    private static bool FixedValueLane(ulong x, ulong y, int width, bool floating, bool value)
    {
        if (!floating) return x == y;
        ulong sign = width == 4 ? 0x80000000UL : 0x8000000000000000UL;
        ulong exponent = width == 4 ? 0x7f800000UL : 0x7ff0000000000000UL;
        ulong fraction = width == 4 ? 0x007fffffUL : 0x000fffffffffffffUL;
        bool xn = (x & exponent) == exponent && (x & fraction) != 0;
        bool yn = (y & exponent) == exponent && (y & fraction) != 0;
        if (xn || yn) return value && xn && yn;
        return x == y || ((x & ~sign) == 0 && (y & ~sign) == 0);
    }

    private static bool FixedValueExpected(byte[] a, byte[] b, int width, bool floating, bool value)
    {
        for (int i = 0; i < a.Length; i += width)
            if (!FixedValueLane(FixedValueBits(a, i, width), FixedValueBits(b, i, width), width, floating, value))
                return false;
        return true;
    }

    private static byte[] FixedValueMask(byte[] a, byte[] b, int width, bool floating)
    {
        byte[] mask = new byte[a.Length];
        for (int i = 0; i < a.Length; i += width)
        {
            bool equal = FixedValueLane(FixedValueBits(a, i, width), FixedValueBits(b, i, width), width, floating, false);
            for (int j = 0; j < width; j++) mask[i + j] = equal ? (byte)255 : (byte)0;
        }
        return mask;
    }

    private static ulong[] FixedValueFloatingPatterns(int width) => width == 4
        ? new ulong[] { 0, 0x80000000, 1, 0x80000001, 0x3f800000, 0xbf800000,
            0x7f800000, 0xff800000, 0x7fc00001, 0x7fc12345, 0xffc54321, 0x7f800001, 0xff800123 }
        : new ulong[] { 0, 0x8000000000000000, 1, 0x8000000000000001,
            0x3ff0000000000000, 0xbff0000000000000, 0x7ff0000000000000, 0xfff0000000000000,
            0x7ff8000000000001, 0x7ff8123456789abc, 0xfff876543210abcd,
            0x7ff0000000000001, 0xfff0000000000123 };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool FixedValueConstrainedEquals<V>(V left, V right) where V : IEquatable<V> => left.Equals(right);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int FixedValueConstrainedHash<V>(V value) where V : struct => value.GetHashCode();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static V FixedValueUnbox<V>(object value) => (V)value;

    private static void FixedValueWrongUnbox<V>(object value)
    {
        bool rejected = false;
        try { FixedValueUnbox<V>(value); }
        catch (InvalidCastException) { rejected = true; }
        FixedValueAssert(rejected, "wrong unbox");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static V FixedValueProduce<V>(byte[] bytes) where V : unmanaged
    {
        FixedValueProductions++;
        return FixedValueSeed<V>(bytes);
    }

    private struct FixedValueNestedBox<V> where V : unmanaged
    {
        public ValueTuple<V, V> Pair;
        public byte Tag;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FixedValueNestedRoundtrip<V>(byte[] a, byte[] b) where V : unmanaged
    {
        V left = FixedValueSeed<V>(a), right = FixedValueSeed<V>(b);
        object first = new FixedValueNestedBox<V> { Pair = (left, left), Tag = 0x5a };
        object second = new FixedValueNestedBox<V> { Pair = (right, right), Tag = 0x5a };
        object different = new FixedValueNestedBox<V> { Pair = (right, right), Tag = 0xa5 };
        FixedValueAssert(first.Equals(second) && second.Equals(first), "nested default boxed equality");
        FixedValueAssert(first.GetHashCode() == second.GetHashCode(), "nested default boxed equal hash");
        FixedValueAssert(!first.Equals(different) && !different.Equals(first), "nested default boxed tag mismatch");
        var copy = FixedValueUnbox<FixedValueNestedBox<V>>(first);
        FixedValueBytes(copy.Pair.Item1, a); FixedValueBytes(copy.Pair.Item2, a);
        FixedValueAssert(copy.Tag == 0x5a, "nested default box retained tag");
    }

    private sealed class FixedValueHolder<V> where V : unmanaged
    {
        public byte Prefix;
        public V Current;
        public byte Suffix;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static FixedValueHolder<V> FixedValueHolderReceiver<V>(FixedValueHolder<V> value) where V : unmanaged
    {
        FixedValueReceiverEffects++;
        FixedValueEffectOrder = FixedValueEffectOrder * 10 + 1;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static V FixedValueHolderRight<V>(byte[] bytes) where V : unmanaged
    {
        FixedValueRightEffects++;
        FixedValueEffectOrder = FixedValueEffectOrder * 10 + 2;
        return FixedValueSeed<V>(bytes);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FixedValueHolderEffects<V>(byte[] a, byte[] b) where V : unmanaged
    {
        var holder = new FixedValueHolder<V> { Prefix = 0xa5, Current = FixedValueSeed<V>(a), Suffix = 0x5a };
        FixedValueReceiverEffects = FixedValueRightEffects = FixedValueEffectOrder = 0;
        FixedValueHolderReceiver(holder).Current = FixedValueHolderRight<V>(b);
        FixedValueAssert(FixedValueReceiverEffects == 1 && FixedValueRightEffects == 1
            && FixedValueEffectOrder == 12, "field store operands once and ordered");
        FixedValueBytes(holder.Current, b);
        FixedValueAssert(holder.Prefix == 0xa5 && holder.Suffix == 0x5a, "field store preserves companions");
        FixedValueReceiverEffects = FixedValueRightEffects = FixedValueEffectOrder = 0;
        FixedValueBytes(FixedValueHolderReceiver(holder).Current, b);
        FixedValueAssert(FixedValueReceiverEffects == 1 && FixedValueRightEffects == 0
            && FixedValueEffectOrder == 1, "field load receiver once");
        FixedValueReceiverEffects = FixedValueRightEffects = FixedValueEffectOrder = 0;
        bool storeNull = false;
        try
        {
            ref V destination = ref FixedValueHolderReceiver<V>(null).Current;
            destination = FixedValueHolderRight<V>(a);
        }
        catch (NullReferenceException) { storeNull = true; }
        FixedValueAssert(storeNull && FixedValueReceiverEffects == 1 && FixedValueRightEffects == 0
            && FixedValueEffectOrder == 1, "null field address throws before RHS");
        FixedValueBytes(holder.Current, b);
        FixedValueAssert(holder.Prefix == 0xa5 && holder.Suffix == 0x5a, "null store leaves live holder unchanged");
        FixedValueReceiverEffects = FixedValueRightEffects = FixedValueEffectOrder = 0;
        bool loadNull = false;
        try { FixedValueBytes(FixedValueHolderReceiver<V>(null).Current, a); }
        catch (NullReferenceException) { loadNull = true; }
        FixedValueAssert(loadNull && FixedValueReceiverEffects == 1 && FixedValueRightEffects == 0
            && FixedValueEffectOrder == 1, "null field load receiver once then throw");
    }

    private struct FixedValueMutableBox<V> where V : unmanaged, IEquatable<V>
    {
        public byte Prefix;
        public V Current;
        public V Replacement;
        public int Writes;
        public object Marker;

        public override bool Equals(object value)
        {
            if (value is not FixedValueMutableBox<V> other) return false;
            bool equal = Current.Equals(other.Current);
            ref V destination = ref Current;
            destination = other.Replacement;
            ref int counter = ref Writes;
            counter++;
            Prefix++;
            Marker = other.Marker;
            return equal;
        }

        public override int GetHashCode()
        {
            Current = Replacement;
            Writes++;
            Prefix++;
            Marker = "fixed-hash";
            return Writes;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FixedValueMutableRoundtrip<V>(byte[] a, byte[] b) where V : unmanaged, IEquatable<V>
    {
        V left = FixedValueSeed<V>(a), right = FixedValueSeed<V>(b);
        object receiver = new FixedValueMutableBox<V> { Prefix = 0x5a, Current = left, Replacement = right, Marker = "fixed-first" };
        object argument = new FixedValueMutableBox<V> { Current = left, Replacement = right, Marker = "fixed-second" };
        FixedValueAssert(receiver.Equals(argument), "mutable boxed equality first");
        var first = FixedValueUnbox<FixedValueMutableBox<V>>(receiver);
        FixedValueAssert(first.Writes == 1 && first.Prefix == 0x5b && object.ReferenceEquals(first.Marker, "fixed-second"), "mutable boxed receiver retained first write");
        FixedValueBytes(first.Current, b);
        argument = new FixedValueMutableBox<V> { Current = right, Replacement = left, Marker = "fixed-third" };
        FixedValueAssert(receiver.Equals(argument), "mutable boxed equality observes prior write");
        var second = FixedValueUnbox<FixedValueMutableBox<V>>(receiver);
        FixedValueAssert(second.Writes == 2 && second.Prefix == 0x5c && object.ReferenceEquals(second.Marker, "fixed-third"), "mutable boxed receiver retained second write");
        FixedValueBytes(second.Current, a);
        FixedValueAssert(receiver.GetHashCode() == 3, "mutable boxed hash observes counter");
        var third = FixedValueUnbox<FixedValueMutableBox<V>>(receiver);
        FixedValueAssert(third.Writes == 3 && third.Prefix == 0x5d && object.ReferenceEquals(third.Marker, "fixed-hash"), "mutable boxed hash retained write");
        FixedValueBytes(third.Current, b);
        FixedValueBytes(third.Replacement, b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void FixedValueUnalignedRoundtrip<V>(byte[] a, byte[] b) where V : unmanaged
    {
        V left = FixedValueSeed<V>(a), right = FixedValueSeed<V>(b);
        byte[] backing = new byte[a.Length + 2];
        backing[0] = 0xa5;
        backing[backing.Length - 1] = 0x5a;
        fixed (byte* start = backing)
        {
            V* location = (V*)(start + 1);
            *location = left;
            V read = *location;
            FixedValueBytes(read, a);
            for (int i = 0; i < a.Length; i++) FixedValueAssert(start[i + 1] == a[i], "unaligned store bytes first");
            *location = right;
            read = *location;
            FixedValueBytes(read, b);
            for (int i = 0; i < b.Length; i++) FixedValueAssert(start[i + 1] == b[i], "unaligned store bytes second");
        }
        FixedValueAssert(backing[0] == 0xa5 && backing[backing.Length - 1] == 0x5a, "unaligned storage bounds");
        FixedValueBytes(left, a); FixedValueBytes(right, b);
    }

    private sealed class FixedValueAlwaysComparer<V> : IEqualityComparer<V>
    {
        public int EqualsCalls;
        public int HashCalls;
        public bool Equals(V left, V right) { EqualsCalls++; return true; }
        public int GetHashCode(V value) { HashCalls++; return 0; }
    }

    private static void FixedValueRoutes<V>(V left, V right, bool equal)
        where V : unmanaged, IEquatable<V>
    {
        FixedValueAssert(FixedValueConstrainedEquals(left, right) == equal, "constrained equality");
        var comparer = EqualityComparer<V>.Default;
        FixedValueAssert(comparer.Equals(left, right) == equal, "default comparer");
        object boxA = left, boxB = right;
        FixedValueAssert(boxA.Equals(boxB) == equal && object.Equals(boxA, boxB) == equal, "boxed object");
        FixedValueAssert(((IEquatable<V>)boxA).Equals(right) == equal, "actual boxed interface");
        FixedValueAssert(EqualityComparer<object>.Default.Equals(boxA, boxB) == equal, "object comparer");
        FixedValueAssert(boxA.Equals(boxA) && !boxA.Equals(null) && !boxA.Equals("fixed vector"), "box controls");
        int hash = FixedValueConstrainedHash(left);
        FixedValueAssert(hash == comparer.GetHashCode(left) && hash == boxA.GetHashCode(), "hash routes");
        if (equal)
            FixedValueAssert(hash == FixedValueConstrainedHash(right) && hash == boxB.GetHashCode(), "equal hash");
    }

    // Exercise collection and nullable ABI routes separately from lane truth tables.
    private static void FixedValueIntegrationCases<V>(int length, int width, bool floating)
        where V : unmanaged, IEquatable<V>
    {
        byte[] zero = FixedValuePattern(length, width, 0);
        byte[] different = (byte[])zero.Clone();
        different[length - width] = 1;
        FixedValueIntegrationPair<V>(zero, zero, true);
        FixedValueIntegrationPair<V>(zero, different, false);
        if (!floating) return;
        FixedValueIntegrationPair<V>(zero, FixedValuePattern(length, width,
            width == 4 ? 0x80000000UL : 0x8000000000000000UL), true);
        byte[] nan = FixedValuePattern(length, width, width == 4 ? 0x7f800001UL : 0x7ff0000000000001UL);
        FixedValueIntegrationPair<V>(nan, FixedValuePattern(length, width,
            width == 4 ? 0xffc54321UL : 0xfff876543210abcdUL), true);
        FixedValueIntegrationPair<V>(nan, zero, false);
    }

    private static void FixedValueIntegrationPair<V>(byte[] a, byte[] b, bool equal)
        where V : unmanaged, IEquatable<V> =>
        FixedValueIntegration(FixedValueSeed<V>(a), FixedValueSeed<V>(b), a, b, equal);

    private static unsafe void FixedValueIntegration<V>(V left, V right, byte[] a, byte[] b, bool equal)
        where V : unmanaged, IEquatable<V>
    {
        FixedValueAssert(FixedValueConstrainedEquals(left, right) == equal, "constrained equality");
        var comparer = EqualityComparer<V>.Default;
        FixedValueAssert(comparer.Equals(left, right) == equal, "default comparer");
        object boxA = left, boxB = right;
        FixedValueAssert(boxA.Equals(boxB) == equal && object.Equals(boxA, boxB) == equal, "boxed object");
        FixedValueAssert(((IEquatable<V>)boxA).Equals(right) == equal, "actual boxed interface");
        FixedValueAssert(EqualityComparer<object>.Default.Equals(boxA, boxB) == equal, "object comparer");
        FixedValueAssert(boxA.Equals(boxA) && !boxA.Equals(null) && !boxA.Equals("fixed vector"), "box controls");
        int hash = FixedValueConstrainedHash(left);
        FixedValueAssert(hash == comparer.GetHashCode(left) && hash == boxA.GetHashCode(), "hash routes");
        var tupleA = (left, left); var tupleB = (right, right);
        FixedValueAssert(tupleA.Equals(tupleB) == equal && tupleA.Equals((object)tupleB) == equal, "tuple equality");
        if (equal)
        {
            FixedValueAssert(hash == FixedValueConstrainedHash(right) && hash == boxB.GetHashCode(), "equal hash");
            FixedValueAssert(tupleA.GetHashCode() == tupleB.GetHashCode(), "tuple constrained hash");
        }
        V[] array = { left, right };
        FixedValueAssert(Array.IndexOf(array, right) == (equal ? 0 : 1), "array search");
        FixedValueBytes(array[0], a); FixedValueBytes(array[1], b);
        fixed (V* pointer = array)
        {
            FixedValueAssert((byte*)(pointer + 1) - (byte*)pointer == a.Length, "array stride");
            byte* bytes = (byte*)pointer;
            for (int i = 0; i < a.Length; i++)
                FixedValueAssert(bytes[i] == a[i] && bytes[a.Length + i] == b[i], "array payload");
        }
        var list = new List<V> { left };
        FixedValueAssert(list.Contains(right) == equal && (list.IndexOf(right) == 0) == equal, "list search");
        var dictionary = new Dictionary<V, int> { [left] = 1 };
        FixedValueAssert(dictionary.ContainsKey(right) == equal, "dictionary lookup");
        dictionary[right] = 2;
        FixedValueAssert(dictionary.Count == (equal ? 1 : 2) && dictionary[right] == 2, "dictionary update");
        FixedValueAssert(dictionary.Remove(right) && dictionary.ContainsKey(left) != equal, "dictionary remove");
        var set = new HashSet<V> { left };
        FixedValueAssert(set.Add(right) != equal && set.Count == (equal ? 1 : 2), "set dedup");
        var objects = new Dictionary<object, int> { [boxA] = 7 };
        FixedValueAssert(objects.ContainsKey(boxB) == equal, "object dictionary");
        var custom = new FixedValueAlwaysComparer<V>();
        var customSet = new HashSet<V>(custom) { left };
        FixedValueAssert(!customSet.Add(right) && custom.EqualsCalls > 0 && custom.HashCalls >= 2, "custom comparer");
        FixedValueBytes(FixedValueUnbox<V>(boxA), a);
        V? nullable = left;
        var nullableCopy = FixedValueUnbox<V?>((object)nullable);
        FixedValueAssert(nullableCopy.HasValue, "nullable value");
        FixedValueBytes(nullableCopy.Value, a);
        ValueTuple<V, V>? nullableTuple = tupleA;
        var tupleCopy = FixedValueUnbox<ValueTuple<V, V>?>((object)nullableTuple);
        FixedValueAssert(tupleCopy.HasValue, "nullable tuple value");
        FixedValueBytes(tupleCopy.Value.Item1, a); FixedValueBytes(tupleCopy.Value.Item2, a);
        FixedValueAssert(!FixedValueUnbox<V?>(null).HasValue
            && !FixedValueUnbox<ValueTuple<V, V>?>(null).HasValue, "nullable null");
        FixedValueBytes(left, a); FixedValueBytes(right, b);
    }

    private static void FixedValueCases128<T>(int width, bool floating) where T : unmanaged
    {
        if (floating)
        {
            ulong[] patterns = FixedValueFloatingPatterns(width);
            foreach (ulong x in patterns)
                foreach (ulong y in patterns)
                    FixedValuePair128<T>(FixedValuePattern(16, width, x), FixedValuePattern(16, width, y), width, true);
            for (int lane = 0; lane < 16 / width; lane++)
            {
                byte[] a = FixedValuePattern(16, width, patterns[8]);
                byte[] b = FixedValuePattern(16, width, patterns[11]);
                byte[] one = FixedValuePattern(16, width, patterns[4]);
                for (int i = lane * width; i < (lane + 1) * width; i++) { a[i] = one[i]; b[i] = 0; }
                FixedValuePair128<T>(a, b, width, true);
            }
        }
        else
        {
            ulong high = 1UL << (width * 8 - 1);
            foreach (ulong bits in new ulong[] { 0, ulong.MaxValue, high, high - 1, 0x80ff7f008001ffff, 0x123456789abcdef0 })
            {
                byte[] a = FixedValuePattern(16, width, bits);
                FixedValuePair128<T>(a, a, width, false);
                for (int lane = 0; lane < 16 / width; lane++)
                {
                    byte[] b = (byte[])a.Clone();
                    b[lane * width + width - 1] ^= 0x80;
                    FixedValuePair128<T>(a, b, width, false);
                }
            }
            byte[] distinct = new byte[16];
            for (int i = 0; i < 16; i++) distinct[i] = (byte)(i * 17 + 3);
            FixedValuePair128<T>(distinct, distinct, width, false);
        }
    }

    private static void FixedValuePair128<T>(byte[] a, byte[] b, int width, bool floating) where T : unmanaged
    {
        Vector128<T> left = FixedValueSeed<Vector128<T>>(a), right = FixedValueSeed<Vector128<T>>(b);
        bool equal = FixedValueExpected(a, b, width, floating, true);
        bool numeric = FixedValueExpected(a, b, width, floating, false);
        FixedValueAssert(left.Equals(right) == equal, "typed 128");
        FixedValueAssert((left == right) == numeric && (left != right) != numeric, "numeric operators 128");
        FixedValueAssert(Vector128.EqualsAll(left, right) == numeric, "numeric reduction 128");
        FixedValueBytes(Vector128.Equals(left, right), FixedValueMask(a, b, width, floating));
        object box = right;
        FixedValueAssert(left.Equals(box) == equal, "direct object 128");
        FixedValueAssert(left.GetHashCode() == FixedValueConstrainedHash(left), "direct hash 128");
        FixedValueRoutes(left, right, equal);
        FixedValueProductions = 0;
        FixedValueAssert(FixedValueProduce<Vector128<T>>(a).Equals(FixedValueProduce<Vector128<T>>(b)) == equal
            && FixedValueProductions == 2, "once-only operands 128");
        FixedValueProductions = 0;
        FixedValueAssert(FixedValueProduce<Vector128<T>>(a).GetHashCode() == left.GetHashCode()
            && FixedValueProductions == 1, "once-only hash 128");
        FixedValueBytes(left, a); FixedValueBytes(right, b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Func<Vector128<double>, bool> FixedValueNullBind128(IEquatable<Vector128<double>> value) => value.Equals;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void FixedValueTupleWitness128()
    {
        byte[] bytesA = FixedValuePattern(16, 8, 0x7ff0000000000001);
        byte[] bytesB = FixedValuePattern(16, 8, 0xfff8123456789abc);
        Vector128<double> left = FixedValueSeed<Vector128<double>>(bytesA);
        Vector128<double> right = FixedValueSeed<Vector128<double>>(bytesB);
        FixedValueNestedRoundtrip<Vector128<double>>(bytesA, bytesB);
        FixedValueHolderEffects<Vector128<double>>(bytesA, bytesB);
        FixedValueMutableRoundtrip<Vector128<double>>(bytesA, bytesB);
        FixedValueUnalignedRoundtrip<Vector128<double>>(bytesA, bytesB);
        FixedValueArrayWitness128(new[] { left, right });
        var tupleA = (left, left); var tupleB = (right, right);
        FixedValueAssert(tupleA.Equals(tupleB) && tupleA.GetHashCode() == tupleB.GetHashCode(), "retained tuple 128");
        object boxedTupleA = tupleA, boxedTupleB = tupleB;
        FixedValueAssert(boxedTupleA.Equals(boxedTupleB)
            && boxedTupleA.GetHashCode() == boxedTupleB.GetHashCode(), "boxed tuple 128");
        Func<Vector128<double>, bool> fixedTyped128 = left.Equals;
        Func<object, bool> fixedObject128 = left.Equals;
        Func<int> fixedHash128 = left.GetHashCode;
        FixedValueAssert(fixedTyped128(right) && fixedObject128(right)
            && fixedHash128() == right.GetHashCode(), "actual bound methods 128");
        object wrongElement = default(Vector128<long>);
        object wrongWidth = default(Vector64<double>);
        object wrongFamily = default(Vector<double>);
        object boxedLeft = left;
        FixedValueAssert(!boxedLeft.Equals(wrongElement) && !boxedLeft.Equals(wrongWidth)
            && !boxedLeft.Equals(wrongFamily) && !wrongFamily.Equals(boxedLeft)
            && !fixedObject128(wrongElement) && !fixedObject128(null), "exact boxed identity 128");
        FixedValueWrongUnbox<Vector128<double>>(wrongElement);
        FixedValueWrongUnbox<Vector128<double>?>(wrongElement);
        bool nullBind = false;
        try { var observed = FixedValueNullBind128(null); GC.KeepAlive(observed); }
        catch (NullReferenceException) { nullBind = true; }
        FixedValueAssert(nullBind, "observable null binding 128");
        FixedValueBytes(left, bytesA); FixedValueBytes(right, bytesB);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void FixedValueArrayWitness128(Vector128<double>[] values)
    {
        FixedValueAssert(values.Length == 2, "array ABI length 128");
        fixed (Vector128<double>* pointer = values)
            FixedValueAssert((byte*)(pointer + 1) - (byte*)pointer == 16, "array ABI stride 128");
        FixedValueBytes(values[0], FixedValuePattern(16, 8, 0x7ff0000000000001));
        FixedValueBytes(values[1], FixedValuePattern(16, 8, 0xfff8123456789abc));
    }

    private static void FixedValueCases256<T>(int width, bool floating) where T : unmanaged
    {
        if (floating)
        {
            ulong[] patterns = FixedValueFloatingPatterns(width);
            foreach (ulong x in patterns)
                foreach (ulong y in patterns)
                    FixedValuePair256<T>(FixedValuePattern(32, width, x), FixedValuePattern(32, width, y), width, true);
            for (int lane = 0; lane < 32 / width; lane++)
            {
                byte[] a = FixedValuePattern(32, width, patterns[8]);
                byte[] b = FixedValuePattern(32, width, patterns[11]);
                byte[] one = FixedValuePattern(32, width, patterns[4]);
                for (int i = lane * width; i < (lane + 1) * width; i++) { a[i] = one[i]; b[i] = 0; }
                FixedValuePair256<T>(a, b, width, true);
            }
        }
        else
        {
            ulong high = 1UL << (width * 8 - 1);
            foreach (ulong bits in new ulong[] { 0, ulong.MaxValue, high, high - 1, 0x80ff7f008001ffff, 0x123456789abcdef0 })
            {
                byte[] a = FixedValuePattern(32, width, bits);
                FixedValuePair256<T>(a, a, width, false);
                for (int lane = 0; lane < 32 / width; lane++)
                {
                    byte[] b = (byte[])a.Clone();
                    b[lane * width + width - 1] ^= 0x80;
                    FixedValuePair256<T>(a, b, width, false);
                }
            }
            byte[] distinct = new byte[32];
            for (int i = 0; i < 32; i++) distinct[i] = (byte)(i * 17 + 3);
            FixedValuePair256<T>(distinct, distinct, width, false);
        }
    }

    private static void FixedValuePair256<T>(byte[] a, byte[] b, int width, bool floating) where T : unmanaged
    {
        Vector256<T> left = FixedValueSeed<Vector256<T>>(a), right = FixedValueSeed<Vector256<T>>(b);
        bool equal = FixedValueExpected(a, b, width, floating, true);
        bool numeric = FixedValueExpected(a, b, width, floating, false);
        FixedValueAssert(left.Equals(right) == equal, "typed 256");
        FixedValueAssert((left == right) == numeric && (left != right) != numeric, "numeric operators 256");
        FixedValueAssert(Vector256.EqualsAll(left, right) == numeric, "numeric reduction 256");
        FixedValueBytes(Vector256.Equals(left, right), FixedValueMask(a, b, width, floating));
        object box = right;
        FixedValueAssert(left.Equals(box) == equal, "direct object 256");
        FixedValueAssert(left.GetHashCode() == FixedValueConstrainedHash(left), "direct hash 256");
        FixedValueRoutes(left, right, equal);
        FixedValueProductions = 0;
        FixedValueAssert(FixedValueProduce<Vector256<T>>(a).Equals(FixedValueProduce<Vector256<T>>(b)) == equal
            && FixedValueProductions == 2, "once-only operands 256");
        FixedValueProductions = 0;
        FixedValueAssert(FixedValueProduce<Vector256<T>>(a).GetHashCode() == left.GetHashCode()
            && FixedValueProductions == 1, "once-only hash 256");
        FixedValueBytes(left, a); FixedValueBytes(right, b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Func<Vector256<double>, bool> FixedValueNullBind256(IEquatable<Vector256<double>> value) => value.Equals;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void FixedValueTupleWitness256()
    {
        byte[] bytesA = FixedValuePattern(32, 8, 0x7ff0000000000001);
        byte[] bytesB = FixedValuePattern(32, 8, 0xfff8123456789abc);
        Vector256<double> left = FixedValueSeed<Vector256<double>>(bytesA);
        Vector256<double> right = FixedValueSeed<Vector256<double>>(bytesB);
        FixedValueNestedRoundtrip<Vector256<double>>(bytesA, bytesB);
        FixedValueHolderEffects<Vector256<double>>(bytesA, bytesB);
        FixedValueMutableRoundtrip<Vector256<double>>(bytesA, bytesB);
        FixedValueUnalignedRoundtrip<Vector256<double>>(bytesA, bytesB);
        FixedValueArrayWitness256(new[] { left, right });
        var tupleA = (left, left); var tupleB = (right, right);
        FixedValueAssert(tupleA.Equals(tupleB) && tupleA.GetHashCode() == tupleB.GetHashCode(), "retained tuple 256");
        object boxedTupleA = tupleA, boxedTupleB = tupleB;
        FixedValueAssert(boxedTupleA.Equals(boxedTupleB)
            && boxedTupleA.GetHashCode() == boxedTupleB.GetHashCode(), "boxed tuple 256");
        Func<Vector256<double>, bool> fixedTyped256 = left.Equals;
        Func<object, bool> fixedObject256 = left.Equals;
        Func<int> fixedHash256 = left.GetHashCode;
        FixedValueAssert(fixedTyped256(right) && fixedObject256(right)
            && fixedHash256() == right.GetHashCode(), "actual bound methods 256");
        object wrongElement = default(Vector256<long>);
        object wrongWidth = default(Vector128<double>);
        object wrongFamily = default(Vector<double>);
        object boxedLeft = left;
        FixedValueAssert(!boxedLeft.Equals(wrongElement) && !boxedLeft.Equals(wrongWidth)
            && !boxedLeft.Equals(wrongFamily) && !wrongFamily.Equals(boxedLeft)
            && !fixedObject256(wrongElement) && !fixedObject256(null), "exact boxed identity 256");
        FixedValueWrongUnbox<Vector256<double>>(wrongElement);
        FixedValueWrongUnbox<Vector256<double>?>(wrongElement);
        bool nullBind = false;
        try { var observed = FixedValueNullBind256(null); GC.KeepAlive(observed); }
        catch (NullReferenceException) { nullBind = true; }
        FixedValueAssert(nullBind, "observable null binding 256");
        FixedValueBytes(left, bytesA); FixedValueBytes(right, bytesB);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void FixedValueArrayWitness256(Vector256<double>[] values)
    {
        FixedValueAssert(values.Length == 2, "array ABI length 256");
        fixed (Vector256<double>* pointer = values)
            FixedValueAssert((byte*)(pointer + 1) - (byte*)pointer == 32, "array ABI stride 256");
        FixedValueBytes(values[0], FixedValuePattern(32, 8, 0x7ff0000000000001));
        FixedValueBytes(values[1], FixedValuePattern(32, 8, 0xfff8123456789abc));
    }
}
