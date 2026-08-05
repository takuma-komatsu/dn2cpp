using System.Reflection.Metadata;

namespace Dn2Cpp.Godot;

/// <summary>The runtime's <c>Dn2CppGdType</c> marshalling tags (the
/// <c>DN2CPP_GD_*</c> enum in <c>runtime/godot/dn2cpp_godot.h</c>) — the single
/// C# source for every emitter that stamps a tag into a generated table or
/// switches on one (<see cref="GodotBackend"/>, <see cref="GodotApi"/>,
/// <see cref="GodotCallIntrinsics"/>). The C++ side owns the enum; a
/// <c>static_assert</c> block beside it anchors every value against this mirror,
/// so the two sides must stay in lockstep — <b>never renumber, only append</b>
/// (the same contract as the shim Variant's <c>VariantKind</c>).</summary>
internal static class GdTag
{
    public const int Void = 0;
    public const int Bool = 1;
    public const int Int = 2;
    public const int Float = 3;
    // Heap String — engine String on the wire, managed Dn2CppString* in C++.
    public const int String = 4;
    public const int Vector2 = 5;
    public const int Vector3 = 6;
    public const int Vector4 = 7;
    public const int Color = 8;
    // The large (>16-byte) by-value types, flat floats in the ptrcall slot.
    // Basis (and Transform3D's basis) is column-major in the shim but row-major
    // in Godot, so those two carry a transpose (BigTypeFloatMap below);
    // Transform2D/Projection are identity-layout.
    public const int Transform2D = 9;  // 6 floats
    public const int Basis = 10;       // 9 floats, column<->row transpose
    public const int Transform3D = 11; // 12 floats, basis transpose + origin
    public const int Projection = 12;  // 16 floats
    public const int Quaternion = 13;  // 4 floats (x,y,z,w) — non-transpose, like Vector4
    public const int Plane = 14;       // 4 floats (normal.x/y/z, d) — identity layout
    public const int Aabb = 15;        // 6 floats (position + size) — identity layout
    // Engine Variant, marshalled to/from the shim Variant struct.
    public const int Variant = 16;
    // Packed arrays <-> plain managed arrays, element-wise at the boundary.
    // The shim Variant's packed payload kinds are these plus
    // VariantKind.PackedTagOffset.
    public const int PackedByte = 17;    // byte[]   <-> PackedByteArray
    public const int PackedInt32 = 18;   // int[]    <-> PackedInt32Array
    public const int PackedInt64 = 19;   // long[]   <-> PackedInt64Array
    public const int PackedFloat32 = 20; // float[]  <-> PackedFloat32Array
    public const int PackedFloat64 = 21; // double[] <-> PackedFloat64Array
    public const int PackedString = 22;  // string[] <-> PackedStringArray
    public const int PackedVector2 = 23; // Godot.Vector2[] <-> PackedVector2Array
    public const int PackedVector3 = 24; // Godot.Vector3[] <-> PackedVector3Array
    public const int PackedVector4 = 25; // Godot.Vector4[] <-> PackedVector4Array
    public const int PackedColor = 26;   // Godot.Color[]   <-> PackedColorArray
    // Heterogeneous engine Array <-> C# Godot.Variant[] (the element-copying
    // legacy mapping; the engine-backed wrapper mapping is GodotArray below).
    public const int Array = 27;
    // The engine-backed container wrapper CLASSES (identity semantics — the
    // engine value is shared across the boundary, not copied).
    public const int Dictionary = 28;
    public const int GodotArray = 29;
    // The 16-byte Callable/Signal engine values <-> the field-carrying shim
    // structs (runtime-internal tags; not export-boundary types).
    public const int Callable = 30;
    public const int Signal = 31;

    /// <summary>The <c>DN2CPP_GD_PACKED_*</c> tag selected by a managed array's
    /// ELEMENT type, or null when an array of that element is not a packed-array
    /// marshalling shape. The one element→tag map behind every packed-array site:
    /// GodotBackend's export boundary (<c>GdTypeOf</c>), its Variant payload
    /// registration (<c>VariantPackedKindOf</c>) and engine-type matching
    /// (<c>PackedElementMatches</c>), and GodotCallIntrinsics' container element
    /// codec (<c>PackedGdTag</c>).</summary>
    internal static int? PackedOfElement(TypeDesc e)
    {
        if (e.Kind == TypeKind.Primitive)
        {
            int? prim = e.Primitive switch
            {
                PrimitiveTypeCode.Byte => PackedByte,
                PrimitiveTypeCode.Int32 => PackedInt32,
                PrimitiveTypeCode.Int64 => PackedInt64,
                PrimitiveTypeCode.Single => PackedFloat32,
                PrimitiveTypeCode.Double => PackedFloat64,
                PrimitiveTypeCode.String => PackedString,
                _ => null,
            };
            if (prim is not null) return prim;
        }
        if (e.IsString) return PackedString;
        return e.Class?.FullName switch
        {
            "Godot.Vector2" => PackedVector2,
            "Godot.Vector3" => PackedVector3,
            "Godot.Vector4" => PackedVector4,
            "Godot.Color" => PackedColor,
            _ => null,
        };
    }

    /// <summary>Whether a tag's export-slot payload is a managed pointer living in
    /// <c>Dn2CppValue.arr</c> — the packed arrays, the heterogeneous
    /// <see cref="Array"/>, and the two container wrappers. The test is a range
    /// over the tag numbering, which is why it belongs beside the numbering: the
    /// four groups are contiguous only because they were appended in that order,
    /// and the append-only contract above is what keeps them so.</summary>
    internal static bool RidesArraySlot(int gd) => gd is >= PackedByte and <= GodotArray;

    /// <summary>For a flat-float by-value GD type riding the export slot's
    /// <c>big[]</c> (Transform2D/Basis/Transform3D/Projection, and the
    /// identity-layout Quaternion/Plane/AABB), the permutation between Godot's
    /// native flat-float order and the
    /// C# shim struct's in-memory float order: <c>shim[k] = native[map[k]]</c> one
    /// way, <c>native[j] = shim[map[j]]</c> the other. The shim stores Basis (and
    /// Transform3D's basis) <b>column-major</b> where Godot is <b>row-major</b>, so
    /// those two carry a 3×3 transpose — an involution, hence one map for both
    /// directions; Transform2D (cols X/Y/Origin) and Projection (cols X/Y/Z/W) match
    /// Godot exactly and get the identity.
    ///
    /// <para>The identity rows are not filler: the export slot
    /// (<c>Dn2CppValue.big</c>) is a float array rather than the shim struct, so it
    /// must copy float-by-float even where nothing is permuted, whereas the ptrcall
    /// sites pass the struct's own storage by address and need the map only where it
    /// reorders — which is why the two accessors below are accessors rather than
    /// separate tables.</para></summary>
    internal static int[]? BigTypeFloatMap(int gd) => gd switch
    {
        Transform2D => new[] { 0, 1, 2, 3, 4, 5 },
        Basis => new[] { 0, 3, 6, 1, 4, 7, 2, 5, 8 },                            // transpose
        Transform3D => new[] { 0, 3, 6, 1, 4, 7, 2, 5, 8, 9, 10, 11 },           // basis transpose + origin
        Projection => new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 },
        Quaternion => new[] { 0, 1, 2, 3 },      // x,y,z,w — identity, like Vector4
        Plane => new[] { 0, 1, 2, 3 },           // normal.x/y/z, d — identity
        Aabb => new[] { 0, 1, 2, 3, 4, 5 },      // position + size — identity
        _ => null,
    };

    /// <summary>The rows of <see cref="BigTypeFloatMap"/> that actually reorder —
    /// Basis and Transform3D — and null for everything else, the identity-layout
    /// large types included. Every ptrcall-ABI site asks this rather than the map
    /// itself: a null answer means "pass the shim storage straight through", which
    /// is precisely what an identity permutation would do the slow way.</summary>
    internal static int[]? TransposeMap(int gd) => gd is Basis or Transform3D ? BigTypeFloatMap(gd) : null;

    /// <summary>The same permutation keyed by the C# shim value type instead of the
    /// tag, for the sites that hold a signature type and no tag: the generic ptrcall
    /// carries <see cref="GdKind.Pod"/> for every builtin POD, and the virtual
    /// trampolines decode straight off the engine dump's type name.</summary>
    internal static int[]? TransposeMapOf(TypeDesc t) => t.Class?.FullName switch
    {
        "Godot.Basis" => BigTypeFloatMap(Basis),
        "Godot.Transform3D" => BigTypeFloatMap(Transform3D),
        _ => null,
    };

    /// <summary>The <c>DN2CPP_GD_PACKED_*</c> tag of a <c>Packed*Array</c> ENGINE
    /// type name (the keys of <see cref="GodotApi.PackedArrayTypes"/>), or null
    /// for any other name. With <see cref="PackedOfElement"/> this is what makes
    /// "does this managed element match that engine packed type" a tag-equality
    /// test instead of a third hand-kept pairing.</summary>
    internal static int? PackedOfEngineType(string engineType) => engineType switch
    {
        "PackedByteArray" => PackedByte,
        "PackedInt32Array" => PackedInt32,
        "PackedInt64Array" => PackedInt64,
        "PackedFloat32Array" => PackedFloat32,
        "PackedFloat64Array" => PackedFloat64,
        "PackedStringArray" => PackedString,
        "PackedVector2Array" => PackedVector2,
        "PackedVector3Array" => PackedVector3,
        "PackedVector4Array" => PackedVector4,
        "PackedColorArray" => PackedColor,
        _ => null,
    };
}
