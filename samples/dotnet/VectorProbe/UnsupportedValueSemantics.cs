using System;
using System.Collections.Generic;
using System.Numerics;

namespace VectorProbe;

internal enum VectorUnsupportedEnum : int { Zero, One }
internal struct VectorUnsupportedStruct { public int Value; }
internal struct VectorUnsupportedRefStruct { public string Value; }
// Inline value storage and inherited storage must preserve the vector representation.
internal class VectorUnsupportedDirect<T> where T : struct { public Vector<T> Value; }
internal struct VectorUnsupportedInline<T> where T : struct { public Vector<T> Value; }
internal class VectorUnsupportedNested<T> where T : struct { public VectorUnsupportedInline<T> Inner; }
internal class VectorUnsupportedBase<T> where T : struct { public Vector<T> Value; }
internal sealed class VectorUnsupportedDerived<T> : VectorUnsupportedBase<T> where T : struct { }
internal sealed class VectorUnsupportedScalar<T> { public T Value; }
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
    private static void VectorUnsupportedStored<T>(string name, Vector<T> value) where T : struct
    {
        VectorUnsupportedRecord(name + ".supported", () => Vector<T>.IsSupported);
        VectorUnsupportedRecord(name + ".count-positive", () => Vector<T>.Count > 0);
        VectorUnsupportedRecord(name + ".typed", () => value.Equals(default(Vector<T>)));
        VectorUnsupportedRecord(name + ".hash-match", () => value.GetHashCode() == default(Vector<T>).GetHashCode());
        VectorUnsupportedRecord(name + ".object-null", () => value.Equals((object)null));
        VectorUnsupportedRecord(name + ".object-same", () => value.Equals((object)default(Vector<T>)));
    }

    private static void VectorUnsupportedStorage<T>(string name) where T : struct
    {
        var direct = new VectorUnsupportedDirect<T>();
        direct.Value = default;
        VectorUnsupportedStored(name + ".direct", direct.Value);
        var nested = new VectorUnsupportedNested<T>();
        nested.Inner.Value = default;
        VectorUnsupportedStored(name + ".nested", nested.Inner.Value);
        var derived = new VectorUnsupportedDerived<T>();
        derived.Value = default;
        VectorUnsupportedBase<T> baseView = derived;
        VectorUnsupportedStored(name + ".base", baseView.Value);
    }

    private static void VectorUnsupportedScalarStorage<T>(string name, T value)
    {
        var holder = new VectorUnsupportedScalar<T>();
        holder.Value = value;
        VectorUnsupportedRecord(name, () => EqualityComparer<T>.Default.Equals(holder.Value, value));
    }

    private static void VectorUnsupportedSemantics()
    {
        Console.WriteLine("-- vector unsupported semantics --");
        VectorUnsupportedTest<decimal>("decimal");
        VectorUnsupportedTest<VectorUnsupportedEnum>("enum");
        VectorUnsupportedTest<VectorUnsupportedStruct>("struct");
        VectorUnsupportedTest<VectorUnsupportedRefStruct>("ref-containing-struct");
        VectorUnsupportedStorage<VectorUnsupportedEnum>("storage.enum");
        VectorUnsupportedStorage<int>("storage.int");
        VectorUnsupportedStorage<uint>("storage.uint");
        VectorUnsupportedScalarStorage("storage.scalar-enum", VectorUnsupportedEnum.One);
        VectorUnsupportedScalarStorage("storage.scalar-int", 1);
        VectorUnsupportedAbiSemantics();
        Console.WriteLine("vector unsupported end");
    }
}
