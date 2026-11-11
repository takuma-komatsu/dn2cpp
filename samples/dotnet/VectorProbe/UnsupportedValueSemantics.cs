using System;
using System.Collections.Generic;
using System.Numerics;

namespace VectorProbe;

internal enum VectorUnsupportedEnum : int { Zero, One }
internal struct VectorUnsupportedStruct { public int Value; }
internal struct VectorUnsupportedRefStruct { public string Value; }
// Inline value storage and inherited storage must preserve the vector representation.
internal class VectorUnsupportedDirect<T> where T : struct { public Vector<T> Value; public int Marker; }
internal struct VectorUnsupportedInline<T> where T : struct { public Vector<T> Value; public int Marker; }
internal class VectorUnsupportedNested<T> where T : struct { public VectorUnsupportedInline<T> Inner; }
internal class VectorUnsupportedBase<T> where T : struct { public Vector<T> Value; public int Marker; }
internal sealed class VectorUnsupportedDerived<T> : VectorUnsupportedBase<T> where T : struct { }
internal static partial class Program
{
    private static string VectorUnsupportedEffects = "";
    private static void VectorUnsupportedRecord(string name, Func<object> action)
    {
        VectorUnsupportedEffects = "";
        try { Console.WriteLine($"{name}|result={action()}|effects={VectorUnsupportedEffects}"); }
        catch (Exception e) { Console.WriteLine($"{name}|exception={e.GetType().FullName}|effects={VectorUnsupportedEffects}"); }
    }
    private static Vector<T> VectorUnsupportedOperand<T>(string mark, bool fail = false) where T : struct
    {
        VectorUnsupportedEffects += mark;
        if (fail) throw new InvalidOperationException();
        return default;
    }
    private static object VectorUnsupportedObjectOperand(object value, bool fail = false)
    {
        VectorUnsupportedEffects += "A";
        if (fail) throw new InvalidOperationException();
        return value;
    }
    private static bool VectorUnsupportedConstrainedEqual<T>(T left, T right) where T : IEquatable<T> => left.Equals(right);
    private static void VectorUnsupportedTest<T>(string name) where T : struct
    {
        Vector<T> value = default;
        object box = value;
        VectorUnsupportedRecord(name + ".default-init", () => { Vector<T> initialized = new Vector<T>(); return initialized.Equals((object)null); });
        VectorUnsupportedRecord(name + ".IsSupported", () => Vector<T>.IsSupported);
        VectorUnsupportedRecord(name + ".Count", () => Vector<T>.Count);
        VectorUnsupportedRecord(name + ".typed", () => value.Equals(default(Vector<T>)));
        VectorUnsupportedRecord(name + ".hash", () => value.GetHashCode());
        VectorUnsupportedRecord(name + ".object-null", () => value.Equals((object)null));
        VectorUnsupportedRecord(name + ".object-wrong", () => value.Equals((object)"wrong"));
        VectorUnsupportedRecord(name + ".object-other-vector", () => value.Equals((object)default(Vector<int>)));
        VectorUnsupportedRecord(name + ".object-same-vector", () => value.Equals(box));
        VectorUnsupportedRecord(name + ".boxed-null", () => box.Equals(null));
        VectorUnsupportedRecord(name + ".boxed-wrong", () => box.Equals("wrong"));
        VectorUnsupportedRecord(name + ".boxed-same", () => box.Equals(box));
        VectorUnsupportedRecord(name + ".boxed-distinct-same-type", () => box.Equals((object)default(Vector<T>)));
        VectorUnsupportedRecord(name + ".boxed-hash", () => box.GetHashCode());
        VectorUnsupportedRecord(name + ".interface", () => ((IEquatable<Vector<T>>)box).Equals(value));
        VectorUnsupportedRecord(name + ".constrained", () => VectorUnsupportedConstrainedEqual(value, value));
        VectorUnsupportedRecord(name + ".comparer-equal", () => EqualityComparer<Vector<T>>.Default.Equals(value, value));
        VectorUnsupportedRecord(name + ".comparer-hash", () => EqualityComparer<Vector<T>>.Default.GetHashCode(value));
        Func<Vector<T>, bool> typedAddress = value.Equals;
        Func<object, bool> objectAddress = value.Equals;
        Func<int> hashAddress = value.GetHashCode;
        VectorUnsupportedRecord(name + ".typed-address", () => typedAddress(value));
        VectorUnsupportedRecord(name + ".object-address-null", () => objectAddress(null));
        VectorUnsupportedRecord(name + ".object-address-same", () => objectAddress(box));
        VectorUnsupportedRecord(name + ".hash-address", () => hashAddress());
        VectorUnsupportedRecord(name + ".effects-typed", () => VectorUnsupportedOperand<T>("R").Equals(VectorUnsupportedOperand<T>("A")));
        VectorUnsupportedRecord(name + ".effects-typed-argument-throws", () => VectorUnsupportedOperand<T>("R").Equals(VectorUnsupportedOperand<T>("A", true)));
        VectorUnsupportedRecord(name + ".effects-typed-receiver-throws", () => VectorUnsupportedOperand<T>("R", true).Equals(VectorUnsupportedOperand<T>("A")));
        VectorUnsupportedRecord(name + ".effects-object-null", () => VectorUnsupportedOperand<T>("R").Equals(VectorUnsupportedObjectOperand(null)));
        VectorUnsupportedRecord(name + ".effects-object-same", () => VectorUnsupportedOperand<T>("R").Equals(VectorUnsupportedObjectOperand(box)));
        VectorUnsupportedRecord(name + ".effects-object-argument-throws", () => VectorUnsupportedOperand<T>("R").Equals(VectorUnsupportedObjectOperand(null, true)));
        VectorUnsupportedRecord(name + ".effects-hash", () => VectorUnsupportedOperand<T>("R").GetHashCode());
        // Existing constructors are ordering controls, not newly implemented surfaces.
        VectorUnsupportedRecord(name + ".array-null", () => { _ = new Vector<T>((T[])null); return true; });
        VectorUnsupportedRecord(name + ".array-empty", () => { _ = new Vector<T>(Array.Empty<T>()); return true; });
        VectorUnsupportedRecord(name + ".array-null-negative", () => { _ = new Vector<T>((T[])null, -1); return true; });
        VectorUnsupportedRecord(name + ".array-null-zero", () => { _ = new Vector<T>((T[])null, 0); return true; });
        VectorUnsupportedRecord(name + ".array-empty-negative", () => { _ = new Vector<T>(Array.Empty<T>(), -1); return true; });
        VectorUnsupportedRecord(name + ".array-empty-zero", () => { _ = new Vector<T>(Array.Empty<T>(), 0); return true; });
        VectorUnsupportedRecord(name + ".readonly-span-empty", () => { _ = new Vector<T>(ReadOnlySpan<T>.Empty); return true; });
        VectorUnsupportedRecord(name + ".span-empty", () => { _ = new Vector<T>(Span<T>.Empty); return true; });
        VectorUnsupportedRecord(name + ".readonly-span-nonempty", () => { _ = new Vector<T>(new ReadOnlySpan<T>(new T[64])); return true; });
    }
    // The enum exercises dispatch and argument ordering. Other unsupported
    // layouts need admission controls, not the same callable matrix again.
    private static void VectorUnsupportedCompact<T>(string name) where T : struct
    {
        Vector<T> value = default;
        VectorUnsupportedRecord(name + ".IsSupported", () => Vector<T>.IsSupported);
        VectorUnsupportedRecord(name + ".Count", () => Vector<T>.Count);
        VectorUnsupportedRecord(name + ".typed", () => value.Equals(default(Vector<T>)));
        VectorUnsupportedRecord(name + ".hash", () => value.GetHashCode());
        VectorUnsupportedRecord(name + ".object-null", () => value.Equals((object)null));
        VectorUnsupportedRecord(name + ".object-same", () => value.Equals((object)default(Vector<T>)));
    }

    private static void VectorUnsupportedStored<T>(string name, Vector<T> value,
        Vector<T> expected, int marker, int expectedMarker) where T : struct
    {
        VectorUnsupportedRecord(name + ".value-and-marker", () => marker == expectedMarker && value.Equals(expected));
    }

    private static void VectorUnsupportedStorage<T>(string name, Vector<T> value) where T : struct
    {
        var direct = new VectorUnsupportedDirect<T> { Value = value, Marker = 43 };
        VectorUnsupportedStored(name + ".direct", direct.Value, value, direct.Marker, 43);
        var nested = new VectorUnsupportedNested<T>();
        nested.Inner.Value = value;
        nested.Inner.Marker = 47;
        VectorUnsupportedStored(name + ".nested", nested.Inner.Value, value, nested.Inner.Marker, 47);
        var derived = new VectorUnsupportedDerived<T> { Value = value, Marker = 53 };
        VectorUnsupportedBase<T> baseView = derived;
        VectorUnsupportedStored(name + ".base", baseView.Value, value, baseView.Marker, 53);
    }

    private static void VectorUnsupportedSemantics()
    {
        Console.WriteLine("-- vector unsupported semantics --");
        VectorUnsupportedCompact<decimal>("decimal");
        VectorUnsupportedTest<VectorUnsupportedEnum>("enum");
        VectorUnsupportedCompact<VectorUnsupportedStruct>("struct");
        VectorUnsupportedCompact<VectorUnsupportedRefStruct>("ref-containing-struct");
        VectorUnsupportedCompact<Int128>("int128");
        VectorUnsupportedCompact<UInt128>("uint128");
        VectorUnsupportedStorage<VectorUnsupportedEnum>("storage.enum", default);
        VectorUnsupportedStorage<int>("storage.int", new Vector<int>(37));
        VectorUnsupportedAbiSemantics();
        Console.WriteLine("vector unsupported end");
    }
}
