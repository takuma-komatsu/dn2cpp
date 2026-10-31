using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;

namespace VectorProbe;

internal static partial class Program
{
    private static Vector64<T> FixedUnsupportedOperand64<T>(string mark, bool fail = false) where T : struct
    {
        VectorUnsupportedEffects += mark;
        if (fail) throw new InvalidOperationException();
        return default;
    }
    private static void FixedUnsupportedTest64<T>(string name) where T : struct
    {
        Vector64<T> value = default;
        object box = value;
        VectorUnsupportedRecord(name + ".default-init", () => { Vector64<T> initialized = new Vector64<T>(); return initialized.Equals((object)null); });
        VectorUnsupportedRecord(name + ".IsSupported", () => Vector64<T>.IsSupported);
        VectorUnsupportedRecord(name + ".Count", () => Vector64<T>.Count);
        VectorUnsupportedRecord(name + ".typed", () => value.Equals(default(Vector64<T>)));
        VectorUnsupportedRecord(name + ".hash", () => value.GetHashCode());
        VectorUnsupportedRecord(name + ".object-null", () => value.Equals((object)null));
        VectorUnsupportedRecord(name + ".object-wrong", () => value.Equals((object)"wrong"));
        VectorUnsupportedRecord(name + ".object-other-vector", () => value.Equals((object)default(Vector64<int>)));
        VectorUnsupportedRecord(name + ".object-same-vector", () => value.Equals(box));
        VectorUnsupportedRecord(name + ".boxed-null", () => box.Equals(null));
        VectorUnsupportedRecord(name + ".boxed-wrong", () => box.Equals("wrong"));
        VectorUnsupportedRecord(name + ".boxed-same", () => box.Equals(box));
        VectorUnsupportedRecord(name + ".boxed-distinct-same-type", () => box.Equals((object)default(Vector64<T>)));
        VectorUnsupportedRecord(name + ".boxed-hash", () => box.GetHashCode());
        VectorUnsupportedRecord(name + ".interface", () => ((IEquatable<Vector64<T>>)box).Equals(value));
        VectorUnsupportedRecord(name + ".constrained-hash", () => FixedUnsupportedConstrainedHash(value));
        VectorUnsupportedRecord(name + ".constrained", () => VectorUnsupportedConstrainedEqual(value, value));
        VectorUnsupportedRecord(name + ".comparer-equal", () => EqualityComparer<Vector64<T>>.Default.Equals(value, value));
        VectorUnsupportedRecord(name + ".comparer-hash", () => EqualityComparer<Vector64<T>>.Default.GetHashCode(value));
        Func<Vector64<T>, bool> typedAddress = value.Equals;
        Func<object, bool> objectAddress = value.Equals;
        Func<int> hashAddress = value.GetHashCode;
        Func<Vector64<T>, bool> interfaceAddress = ((IEquatable<Vector64<T>>)box).Equals;
        VectorUnsupportedRecord(name + ".interface-address", () => interfaceAddress(value));
        VectorUnsupportedRecord(name + ".typed-address", () => typedAddress(value));
        VectorUnsupportedRecord(name + ".object-address-null", () => objectAddress(null));
        VectorUnsupportedRecord(name + ".object-address-same", () => objectAddress(box));
        VectorUnsupportedRecord(name + ".hash-address", () => hashAddress());
        VectorUnsupportedRecord(name + ".effects-typed", () => FixedUnsupportedOperand64<T>("R").Equals(FixedUnsupportedOperand64<T>("A")));
        VectorUnsupportedRecord(name + ".effects-typed-argument-throws", () => FixedUnsupportedOperand64<T>("R").Equals(FixedUnsupportedOperand64<T>("A", true)));
        VectorUnsupportedRecord(name + ".effects-typed-receiver-throws", () => FixedUnsupportedOperand64<T>("R", true).Equals(FixedUnsupportedOperand64<T>("A")));
        VectorUnsupportedRecord(name + ".effects-object-null", () => FixedUnsupportedOperand64<T>("R").Equals(VectorUnsupportedObjectOperand(null)));
        VectorUnsupportedRecord(name + ".effects-object-same", () => FixedUnsupportedOperand64<T>("R").Equals(VectorUnsupportedObjectOperand(box)));
        VectorUnsupportedRecord(name + ".effects-object-argument-throws", () => FixedUnsupportedOperand64<T>("R").Equals(VectorUnsupportedObjectOperand(null, true)));
        VectorUnsupportedRecord(name + ".effects-hash", () => FixedUnsupportedOperand64<T>("R").GetHashCode());
    }
    private static Vector128<T> FixedUnsupportedOperand128<T>(string mark, bool fail = false) where T : struct
    {
        VectorUnsupportedEffects += mark;
        if (fail) throw new InvalidOperationException();
        return default;
    }
    private static void FixedUnsupportedTest128<T>(string name) where T : struct
    {
        Vector128<T> value = default;
        object box = value;
        VectorUnsupportedRecord(name + ".default-init", () => { Vector128<T> initialized = new Vector128<T>(); return initialized.Equals((object)null); });
        VectorUnsupportedRecord(name + ".IsSupported", () => Vector128<T>.IsSupported);
        VectorUnsupportedRecord(name + ".Count", () => Vector128<T>.Count);
        VectorUnsupportedRecord(name + ".typed", () => value.Equals(default(Vector128<T>)));
        VectorUnsupportedRecord(name + ".hash", () => value.GetHashCode());
        VectorUnsupportedRecord(name + ".object-null", () => value.Equals((object)null));
        VectorUnsupportedRecord(name + ".object-wrong", () => value.Equals((object)"wrong"));
        VectorUnsupportedRecord(name + ".object-other-vector", () => value.Equals((object)default(Vector128<int>)));
        VectorUnsupportedRecord(name + ".object-same-vector", () => value.Equals(box));
        VectorUnsupportedRecord(name + ".boxed-null", () => box.Equals(null));
        VectorUnsupportedRecord(name + ".boxed-wrong", () => box.Equals("wrong"));
        VectorUnsupportedRecord(name + ".boxed-same", () => box.Equals(box));
        VectorUnsupportedRecord(name + ".boxed-distinct-same-type", () => box.Equals((object)default(Vector128<T>)));
        VectorUnsupportedRecord(name + ".boxed-hash", () => box.GetHashCode());
        VectorUnsupportedRecord(name + ".interface", () => ((IEquatable<Vector128<T>>)box).Equals(value));
        VectorUnsupportedRecord(name + ".constrained-hash", () => FixedUnsupportedConstrainedHash(value));
        VectorUnsupportedRecord(name + ".constrained", () => VectorUnsupportedConstrainedEqual(value, value));
        VectorUnsupportedRecord(name + ".comparer-equal", () => EqualityComparer<Vector128<T>>.Default.Equals(value, value));
        VectorUnsupportedRecord(name + ".comparer-hash", () => EqualityComparer<Vector128<T>>.Default.GetHashCode(value));
        Func<Vector128<T>, bool> typedAddress = value.Equals;
        Func<object, bool> objectAddress = value.Equals;
        Func<int> hashAddress = value.GetHashCode;
        Func<Vector128<T>, bool> interfaceAddress = ((IEquatable<Vector128<T>>)box).Equals;
        VectorUnsupportedRecord(name + ".interface-address", () => interfaceAddress(value));
        VectorUnsupportedRecord(name + ".typed-address", () => typedAddress(value));
        VectorUnsupportedRecord(name + ".object-address-null", () => objectAddress(null));
        VectorUnsupportedRecord(name + ".object-address-same", () => objectAddress(box));
        VectorUnsupportedRecord(name + ".hash-address", () => hashAddress());
        VectorUnsupportedRecord(name + ".effects-typed", () => FixedUnsupportedOperand128<T>("R").Equals(FixedUnsupportedOperand128<T>("A")));
        VectorUnsupportedRecord(name + ".effects-typed-argument-throws", () => FixedUnsupportedOperand128<T>("R").Equals(FixedUnsupportedOperand128<T>("A", true)));
        VectorUnsupportedRecord(name + ".effects-typed-receiver-throws", () => FixedUnsupportedOperand128<T>("R", true).Equals(FixedUnsupportedOperand128<T>("A")));
        VectorUnsupportedRecord(name + ".effects-object-null", () => FixedUnsupportedOperand128<T>("R").Equals(VectorUnsupportedObjectOperand(null)));
        VectorUnsupportedRecord(name + ".effects-object-same", () => FixedUnsupportedOperand128<T>("R").Equals(VectorUnsupportedObjectOperand(box)));
        VectorUnsupportedRecord(name + ".effects-object-argument-throws", () => FixedUnsupportedOperand128<T>("R").Equals(VectorUnsupportedObjectOperand(null, true)));
        VectorUnsupportedRecord(name + ".effects-hash", () => FixedUnsupportedOperand128<T>("R").GetHashCode());
    }
    private static Vector256<T> FixedUnsupportedOperand256<T>(string mark, bool fail = false) where T : struct
    {
        VectorUnsupportedEffects += mark;
        if (fail) throw new InvalidOperationException();
        return default;
    }
    private static void FixedUnsupportedTest256<T>(string name) where T : struct
    {
        Vector256<T> value = default;
        object box = value;
        VectorUnsupportedRecord(name + ".default-init", () => { Vector256<T> initialized = new Vector256<T>(); return initialized.Equals((object)null); });
        VectorUnsupportedRecord(name + ".IsSupported", () => Vector256<T>.IsSupported);
        VectorUnsupportedRecord(name + ".Count", () => Vector256<T>.Count);
        VectorUnsupportedRecord(name + ".typed", () => value.Equals(default(Vector256<T>)));
        VectorUnsupportedRecord(name + ".hash", () => value.GetHashCode());
        VectorUnsupportedRecord(name + ".object-null", () => value.Equals((object)null));
        VectorUnsupportedRecord(name + ".object-wrong", () => value.Equals((object)"wrong"));
        VectorUnsupportedRecord(name + ".object-other-vector", () => value.Equals((object)default(Vector256<int>)));
        VectorUnsupportedRecord(name + ".object-same-vector", () => value.Equals(box));
        VectorUnsupportedRecord(name + ".boxed-null", () => box.Equals(null));
        VectorUnsupportedRecord(name + ".boxed-wrong", () => box.Equals("wrong"));
        VectorUnsupportedRecord(name + ".boxed-same", () => box.Equals(box));
        VectorUnsupportedRecord(name + ".boxed-distinct-same-type", () => box.Equals((object)default(Vector256<T>)));
        VectorUnsupportedRecord(name + ".boxed-hash", () => box.GetHashCode());
        VectorUnsupportedRecord(name + ".interface", () => ((IEquatable<Vector256<T>>)box).Equals(value));
        VectorUnsupportedRecord(name + ".constrained-hash", () => FixedUnsupportedConstrainedHash(value));
        VectorUnsupportedRecord(name + ".constrained", () => VectorUnsupportedConstrainedEqual(value, value));
        VectorUnsupportedRecord(name + ".comparer-equal", () => EqualityComparer<Vector256<T>>.Default.Equals(value, value));
        VectorUnsupportedRecord(name + ".comparer-hash", () => EqualityComparer<Vector256<T>>.Default.GetHashCode(value));
        Func<Vector256<T>, bool> typedAddress = value.Equals;
        Func<object, bool> objectAddress = value.Equals;
        Func<int> hashAddress = value.GetHashCode;
        Func<Vector256<T>, bool> interfaceAddress = ((IEquatable<Vector256<T>>)box).Equals;
        VectorUnsupportedRecord(name + ".interface-address", () => interfaceAddress(value));
        VectorUnsupportedRecord(name + ".typed-address", () => typedAddress(value));
        VectorUnsupportedRecord(name + ".object-address-null", () => objectAddress(null));
        VectorUnsupportedRecord(name + ".object-address-same", () => objectAddress(box));
        VectorUnsupportedRecord(name + ".hash-address", () => hashAddress());
        VectorUnsupportedRecord(name + ".effects-typed", () => FixedUnsupportedOperand256<T>("R").Equals(FixedUnsupportedOperand256<T>("A")));
        VectorUnsupportedRecord(name + ".effects-typed-argument-throws", () => FixedUnsupportedOperand256<T>("R").Equals(FixedUnsupportedOperand256<T>("A", true)));
        VectorUnsupportedRecord(name + ".effects-typed-receiver-throws", () => FixedUnsupportedOperand256<T>("R", true).Equals(FixedUnsupportedOperand256<T>("A")));
        VectorUnsupportedRecord(name + ".effects-object-null", () => FixedUnsupportedOperand256<T>("R").Equals(VectorUnsupportedObjectOperand(null)));
        VectorUnsupportedRecord(name + ".effects-object-same", () => FixedUnsupportedOperand256<T>("R").Equals(VectorUnsupportedObjectOperand(box)));
        VectorUnsupportedRecord(name + ".effects-object-argument-throws", () => FixedUnsupportedOperand256<T>("R").Equals(VectorUnsupportedObjectOperand(null, true)));
        VectorUnsupportedRecord(name + ".effects-hash", () => FixedUnsupportedOperand256<T>("R").GetHashCode());
    }
    private static Vector512<T> FixedUnsupportedOperand512<T>(string mark, bool fail = false) where T : struct
    {
        VectorUnsupportedEffects += mark;
        if (fail) throw new InvalidOperationException();
        return default;
    }
    private static void FixedUnsupportedTest512<T>(string name) where T : struct
    {
        Vector512<T> value = default;
        object box = value;
        VectorUnsupportedRecord(name + ".default-init", () => { Vector512<T> initialized = new Vector512<T>(); return initialized.Equals((object)null); });
        VectorUnsupportedRecord(name + ".IsSupported", () => Vector512<T>.IsSupported);
        VectorUnsupportedRecord(name + ".Count", () => Vector512<T>.Count);
        VectorUnsupportedRecord(name + ".typed", () => value.Equals(default(Vector512<T>)));
        VectorUnsupportedRecord(name + ".hash", () => value.GetHashCode());
        VectorUnsupportedRecord(name + ".object-null", () => value.Equals((object)null));
        VectorUnsupportedRecord(name + ".object-wrong", () => value.Equals((object)"wrong"));
        VectorUnsupportedRecord(name + ".object-other-vector", () => value.Equals((object)default(Vector512<int>)));
        VectorUnsupportedRecord(name + ".object-same-vector", () => value.Equals(box));
        VectorUnsupportedRecord(name + ".boxed-null", () => box.Equals(null));
        VectorUnsupportedRecord(name + ".boxed-wrong", () => box.Equals("wrong"));
        VectorUnsupportedRecord(name + ".boxed-same", () => box.Equals(box));
        VectorUnsupportedRecord(name + ".boxed-distinct-same-type", () => box.Equals((object)default(Vector512<T>)));
        VectorUnsupportedRecord(name + ".boxed-hash", () => box.GetHashCode());
        VectorUnsupportedRecord(name + ".interface", () => ((IEquatable<Vector512<T>>)box).Equals(value));
        VectorUnsupportedRecord(name + ".constrained-hash", () => FixedUnsupportedConstrainedHash(value));
        VectorUnsupportedRecord(name + ".constrained", () => VectorUnsupportedConstrainedEqual(value, value));
        VectorUnsupportedRecord(name + ".comparer-equal", () => EqualityComparer<Vector512<T>>.Default.Equals(value, value));
        VectorUnsupportedRecord(name + ".comparer-hash", () => EqualityComparer<Vector512<T>>.Default.GetHashCode(value));
        Func<Vector512<T>, bool> typedAddress = value.Equals;
        Func<object, bool> objectAddress = value.Equals;
        Func<int> hashAddress = value.GetHashCode;
        Func<Vector512<T>, bool> interfaceAddress = ((IEquatable<Vector512<T>>)box).Equals;
        VectorUnsupportedRecord(name + ".interface-address", () => interfaceAddress(value));
        VectorUnsupportedRecord(name + ".typed-address", () => typedAddress(value));
        VectorUnsupportedRecord(name + ".object-address-null", () => objectAddress(null));
        VectorUnsupportedRecord(name + ".object-address-same", () => objectAddress(box));
        VectorUnsupportedRecord(name + ".hash-address", () => hashAddress());
        VectorUnsupportedRecord(name + ".effects-typed", () => FixedUnsupportedOperand512<T>("R").Equals(FixedUnsupportedOperand512<T>("A")));
        VectorUnsupportedRecord(name + ".effects-typed-argument-throws", () => FixedUnsupportedOperand512<T>("R").Equals(FixedUnsupportedOperand512<T>("A", true)));
        VectorUnsupportedRecord(name + ".effects-typed-receiver-throws", () => FixedUnsupportedOperand512<T>("R", true).Equals(FixedUnsupportedOperand512<T>("A")));
        VectorUnsupportedRecord(name + ".effects-object-null", () => FixedUnsupportedOperand512<T>("R").Equals(VectorUnsupportedObjectOperand(null)));
        VectorUnsupportedRecord(name + ".effects-object-same", () => FixedUnsupportedOperand512<T>("R").Equals(VectorUnsupportedObjectOperand(box)));
        VectorUnsupportedRecord(name + ".effects-object-argument-throws", () => FixedUnsupportedOperand512<T>("R").Equals(VectorUnsupportedObjectOperand(null, true)));
        VectorUnsupportedRecord(name + ".effects-hash", () => FixedUnsupportedOperand512<T>("R").GetHashCode());
    }
    private static int FixedUnsupportedConstrainedHash<T>(T value) => value.GetHashCode();

    private static void FixedUnsupportedValueSemantics()
    {
        Console.WriteLine("-- fixed vector unsupported semantics --");
        FixedUnsupportedTest64<decimal>("fixed64.decimal");
        FixedUnsupportedTest64<Int128>("fixed64.int128");
        FixedUnsupportedTest64<UInt128>("fixed64.uint128");
        FixedUnsupportedTest64<VectorUnsupportedEnum>("fixed64.enum");
        FixedUnsupportedTest64<VectorUnsupportedStruct>("fixed64.struct");
        FixedUnsupportedTest64<VectorUnsupportedRefStruct>("fixed64.ref-containing-struct");
        FixedUnsupportedTest128<decimal>("fixed128.decimal");
        FixedUnsupportedTest128<Int128>("fixed128.int128");
        FixedUnsupportedTest128<UInt128>("fixed128.uint128");
        FixedUnsupportedTest128<VectorUnsupportedEnum>("fixed128.enum");
        FixedUnsupportedTest128<VectorUnsupportedStruct>("fixed128.struct");
        FixedUnsupportedTest128<VectorUnsupportedRefStruct>("fixed128.ref-containing-struct");
        FixedUnsupportedTest256<decimal>("fixed256.decimal");
        FixedUnsupportedTest256<Int128>("fixed256.int128");
        FixedUnsupportedTest256<UInt128>("fixed256.uint128");
        FixedUnsupportedTest256<VectorUnsupportedEnum>("fixed256.enum");
        FixedUnsupportedTest256<VectorUnsupportedStruct>("fixed256.struct");
        FixedUnsupportedTest256<VectorUnsupportedRefStruct>("fixed256.ref-containing-struct");
        FixedUnsupportedTest512<decimal>("fixed512.decimal");
        FixedUnsupportedTest512<Int128>("fixed512.int128");
        FixedUnsupportedTest512<UInt128>("fixed512.uint128");
        FixedUnsupportedTest512<VectorUnsupportedEnum>("fixed512.enum");
        FixedUnsupportedTest512<VectorUnsupportedStruct>("fixed512.struct");
        FixedUnsupportedTest512<VectorUnsupportedRefStruct>("fixed512.ref-containing-struct");
        Console.WriteLine("fixed vector unsupported end");
    }
}
