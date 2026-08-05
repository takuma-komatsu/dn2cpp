using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dn2Cpp.Godot;

/// <summary>Marshaling tag for an engine value, shared by the binding generator
/// (C# shim emission) and the call intrinsics (C++ ptrcall emission).
/// <c>Enum</c> covers both <c>enum::</c> and <c>bitfield::</c> engine types:
/// GDExtension ptrcall passes either as a 64-bit int, and every generated C#
/// enum is int32-backed (see <see cref="GodotApi.TryResolveEnum"/>).
/// <c>Pod</c> covers every remaining builtin POD value type: ptrcall passes each
/// by pointer to its native-layout storage, which equals the C# shim struct's
/// layout except for the column-major Basis/Transform3D (transposed on the emit
/// side, keyed by the shim type). Vector2 keeps its own dedicated bridge tag.
/// <c>Packed</c> covers the ten <c>Packed*Array</c> types, which marshal to/from
/// managed C# arrays at the boundary (GodotSharp-compatible — no wrapper type),
/// with the concrete packed kind derived from the signature's element type.
/// <c>Variant</c> is a bare engine <c>Variant</c>, marshalled to/from the shim's
/// field-carrying <c>Godot.Variant</c> struct by the runtime codec.
/// <c>GdArray</c>/<c>GdDictionary</c> are the engine container types, surfaced as
/// the engine-backed <c>Godot.Collections.Array</c>/<c>Dictionary</c> wrappers
/// (identity semantics): an argument passes a BORROWED pointer to the wrapper's
/// own engine value, a return wraps the ptrcall's value in a fresh wrapper that
/// OWNS it. <c>typedarray::T</c> also maps to GdArray, keyed off the signature's
/// <c>Array&lt;T&gt;</c> instantiation. <c>Callable</c>/<c>Signal</c> are the
/// 16-byte engine value types, surfaced as field-carrying shim structs: an
/// argument builds a pinned engine value destroyed after the call, a return
/// ptrcalls into a caller-owned value decoded back into the struct.
///
/// <para><b>The direction × kind matrix.</b> Nine switch families lower these
/// kinds, and the split is structural: a family is fixed by the <i>ABI</i> it
/// writes (there are three), the <i>direction</i>, and what it <i>keys on</i>
/// (<see cref="GdKind"/> where the engine API dump names the type,
/// <see cref="GdTag"/> where a runtime table carries it). Unifying them is a
/// non-goal — the same C# type legitimately encodes differently in different
/// cells, so knowing which cell one is in is not optional.</para>
/// <code>
///  #  family                     site                             ABI      dir   key
///  1  export-wrapper arg         GodotBackend.EmitEpilogue        slot     e-&gt;m  GdTag
///  2  export-wrapper return      GodotBackend.EmitEpilogue        slot     m-&gt;e  GdTag
///  3  virtual-trampoline arg     GodotBackend.DecodeVirtualArg    ptrcall  e-&gt;m  GdKind
///  4  virtual-trampoline return  GodotBackend.EmitVirtualRet      ptrcall  m-&gt;e  GdKind
///  5  generic-ptrcall arg        GodotCallIntrinsics.EmitGeneric- ptrcall  m-&gt;e  GdKind
///                                Ptrcall + CastArg/GenericArgCppType
///  6  generic-ptrcall return     EmitGenericPtrcall's ret switch  ptrcall  e-&gt;m  GdKind
///  7  builtin-method bridge      EmitBuiltinCall (args + return)  ptrcall  both  GdTag
///  8  vararg return              EmitVarargCall's ret switch      variant  e-&gt;m  GdKind
///  9  Variant-POD push/read      PushPodAsValue/EmitValueToPod    variant  both  TypeDesc
/// </code>
/// <para>Feeding them are three classifiers, each total over its own domain and
/// none over another's: <c>GodotBackend.GdTypeOf</c> (managed TypeDesc → GdTag,
/// the export/signal/property boundary), <see cref="MapTypeToGd"/> (engine type
/// name → GdKind, the call boundary), and <see cref="BridgeGdTag"/> (engine type
/// name → GdTag, the builtin bridge). Their domains differ; the gaps are listed
/// at the end of this comment.</para>
///
/// <para><b>A. The export slot</b> (families 1-2). <c>Dn2CppValue</c> is a
/// 64-byte tagged union — <c>i</c> / <c>d</c> / <c>s</c> / <c>arr</c> /
/// <c>v2[2]</c> / <c>v3[3]</c> / <c>v4[4]</c> / <c>big[16]</c> — so every kind
/// is rebuilt member-wise, never passed by address:</para>
/// <code>
///   Bool, Int              .i (int64)      no 1-byte bool here: the slot is int64
///   Float                  .d (double)
///   String                 .s             one tag; StringName/NodePath collapse into it
///   Vector2/3/4, Color     .v2/.v3/.v4    positional brace-init in, field stores out
///   Transform2D, Basis,    .big[]         float-by-float through BigTypeFloatMap — the
///     Transform3D, Projection,            IDENTITY-layout types too, because the slot is
///     Quaternion, Plane, AABB             a float union and not the shim struct
///   Packed*, Array,        .arr           the managed pointer; the contiguous tag range
///     Dictionary, GodotArray              PackedByte..GodotArray (GdTag.RidesArraySlot)
///   Variant                (bit copy)     the slot IS the decoded POD's storage, so the
///                                         shim struct is reinterpret_cast onto it
/// </code>
///
/// <para><b>B. The native ptrcall</b> (families 3-7). GDExtension's
/// <c>void* const*</c> args / <c>void* r_ret</c>: each value sits in its own
/// native layout and is passed by address, so most kinds copy nothing:</para>
/// <code>
///   Bool                   uint8_t — 1 byte, NOT the slot's int64
///   Int, Enum              int64_t on the wire; an enum narrows to int32 managed-side
///   Float                  double
///   String/StringName/     three distinct kinds, three helper pairs: {str,strname,
///     NodePath             nodepath}_arg_new/_free, _ret_*, decode_*/encode_*
///   Vector2                its own kind — a Dn2CppVector2 local, floats read back out
///   Vector3/4, Color, and  Pod: the shim struct passes by address, nothing copied
///     every identity POD
///   Basis, Transform3D     Pod, but column-major: a float[] built by EmitTranspose in,
///                          read back element-wise out (GdTag.TransposeMap/TransposeMapOf)
///   Packed*                packed_arg_new/_free, _ret_packed, decode_/encode_packed —
///                          tag from GdTag.PackedOfElement
///   GdArray/GdDictionary   arg borrows the wrapper's engine value (null → a fresh empty
///                          one destroyed after); return owns a fresh one
///   Callable/Signal        the six shim⇄POD copy helpers + _arg_new/_ret_*
///   GdObject               arg passes f__handle; a RETURN wraps a fresh OWNING shim
///                          (the ptrcall already transferred one reference), a virtual
///                          ARG wraps a BORROWED one (nothing to release)
///   Variant                CopyShimToPod/CopyPodToShim + variant_arg_new/_ret_variant
/// </code>
///
/// <para><b>C. The Variant POD</b> (families 8-9). <c>Dn2CppGodotVariant</c> is
/// the shim <c>Godot.Variant</c>'s layout twin, so a kind here is a
/// <c>VariantKind</c> payload rather than a wire encoding: scalars ride
/// <c>i</c>/<c>f</c>, strings <c>s</c>, arrays and container wrappers
/// <c>obj</c>, packed arrays <c>obj</c> under
/// <c>GdTag.Packed* + PackedTagOffset</c>, an object its handle in <c>i</c>.
/// This is the only ABI where <i>null</i> is expressible (it encodes as
/// <c>Nil</c>), which is why the write direction tests for it and the two
/// pointer ABIs never do.</para>
///
/// <para><b>Differences that are context, not drift.</b> A bool is 8 bytes in A
/// and 1 in B (union slot vs. ptrcall encoding); the identity-layout matrices are
/// permuted in A and untouched in B (A's destination is a float array, B's is the
/// struct itself); Vector2 has its own kind while Vector3/4/Color are Pod (its
/// dedicated tag is baked into the C++ enum, which may never be renumbered);
/// families 3-4 support fewer kinds than 5-6 <i>by declaration</i>
/// (<c>ReverseArgSupported</c>/<c>ReverseRetSupported</c> — an unbridged virtual
/// is reported, not silently dropped); and family 9's read direction lacks the
/// GdObject arm its write direction has, because the one caller that can reach an
/// object element takes the dedicated <c>array_get_object</c> path above it.</para>
///
/// <para><b>Gaps.</b> An <b>enum</b> crosses the export boundary as
/// <see cref="GdTag.Int"/> (Variant has no enum kind), and Quaternion, Plane and
/// AABB cross as their own tags riding the <c>big[]</c> slot via identity
/// <c>BigTypeFloatMap</c> rows; RID, Rect2/Rect2i and the integer vectors cannot
/// cross. The <b>POD→shim Variant conversion is spelled two ways</b>: field-wise
/// through <c>CopyPodToShim</c> (families 3, 6, 8, 9), so a layout drift becomes a
/// compile error, and as a <c>reinterpret_cast</c> in families 1-2 only, where it
/// is forced — the union slot IS the decoded POD's storage, so there is no second
/// object to copy into. Do not spread the cast: it is asserted by the
/// <c>_PostProcessKeyValue</c> section of
/// <c>gates/build-and-run-godot-sample.sh</c>.</para></summary>
internal enum GdKind { Void, Bool, Int, Float, String, StringName, NodePath, Vector2, GdObject, Enum, Pod, Packed, Variant, GdArray, GdDictionary, Callable, Signal }

/// <summary>Loads and models <c>extension_api.json</c> (the Godot engine API
/// dump) and provides the type/name mapping shared by <see cref="BindingGenerator"/>
/// and <see cref="GodotCallIntrinsics"/>. Keeping both consumers on one model
/// guarantees the generated C# shims and the transpiler's engine-call map stay
/// in lockstep.</summary>
internal static class GodotApi
{
    internal sealed class ClassApi
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("inherits")] public string Inherits { get; set; } = "";
        [JsonPropertyName("methods")] public List<MethodApi>? Methods { get; set; }
        [JsonPropertyName("properties")] public List<PropertyApi>? Properties { get; set; }
        [JsonPropertyName("enums")] public List<EnumApi>? Enums { get; set; }

        /// <summary>Whether the engine exposes a global instance of this class (the
        /// dump's top-level <c>singletons</c> array). Not a JSON member — projected
        /// onto the class in <see cref="LoadUncached"/>, so the naming seam
        /// (<see cref="InstanceCSharpName"/>) can be answered from the class alone
        /// instead of threading a singleton set through every type-mapping call
        /// site.</summary>
        [JsonIgnore] public bool IsSingleton { get; set; }
    }

    /// <summary>An entry of the dump's top-level <c>singletons</c> array: the global
    /// name the engine exposes and the ClassDB class behind it.</summary>
    internal sealed class SingletonApi
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("type")] public string Type { get; set; } = "";
    }

    internal sealed class EnumApi
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("is_bitfield")] public bool IsBitfield { get; set; }
        [JsonPropertyName("values")] public List<EnumValueApi>? Values { get; set; }
    }

    internal sealed class EnumValueApi
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("value")] public long Value { get; set; }
    }

    internal sealed class MethodApi
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("hash")] public long Hash { get; set; }
        [JsonPropertyName("is_static")] public bool IsStatic { get; set; }
        [JsonPropertyName("is_virtual")] public bool IsVirtual { get; set; }
        [JsonPropertyName("is_vararg")] public bool IsVararg { get; set; }
        [JsonPropertyName("return_value")] public ReturnValueApi? ReturnValue { get; set; }
        // builtin_classes methods describe their return as a bare `return_type`
        // string (object-class methods use the `return_value` object above).
        [JsonPropertyName("return_type")] public string? ReturnType { get; set; }
        [JsonPropertyName("arguments")] public List<ArgumentApi>? Arguments { get; set; }
    }

    internal sealed class ReturnValueApi
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("meta")] public string? Meta { get; set; }
    }

    internal sealed class ArgumentApi
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("meta")] public string? Meta { get; set; }
        [JsonPropertyName("default_value")] public string? DefaultValue { get; set; }
    }

    internal sealed class PropertyApi
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("getter")] public string Getter { get; set; } = "";
        [JsonPropertyName("setter")] public string Setter { get; set; } = "";
    }

    internal sealed class UtilityFunctionApi
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("category")] public string Category { get; set; } = "";
        [JsonPropertyName("is_vararg")] public bool IsVararg { get; set; }
        [JsonPropertyName("return_type")] public string? ReturnType { get; set; }
        [JsonPropertyName("arguments")] public List<ArgumentApi>? Arguments { get; set; }
    }

    internal sealed class BuiltinClassApi
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("members")] public List<MemberApi>? Members { get; set; }
        [JsonPropertyName("constructors")] public List<ConstructorApi>? Constructors { get; set; }
        [JsonPropertyName("constants")] public List<ConstantApi>? Constants { get; set; }
        [JsonPropertyName("operators")] public List<OperatorApi>? Operators { get; set; }
        [JsonPropertyName("methods")] public List<MethodApi>? Methods { get; set; }
        [JsonPropertyName("has_destructor")] public bool HasDestructor { get; set; }
    }

    internal sealed class OperatorApi
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("right_type")] public string? RightType { get; set; }
        [JsonPropertyName("return_type")] public string ReturnType { get; set; } = "";
    }

    internal sealed class MemberApi
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("type")] public string Type { get; set; } = "";
    }

    internal sealed class ConstructorApi
    {
        [JsonPropertyName("index")] public int Index { get; set; }
        [JsonPropertyName("arguments")] public List<ArgumentApi>? Arguments { get; set; }
    }

    internal sealed class ConstantApi
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("value")] public string Value { get; set; } = "";
    }

    internal sealed class ExtensionApi
    {
        [JsonPropertyName("classes")] public List<ClassApi>? Classes { get; set; }
        [JsonPropertyName("utility_functions")] public List<UtilityFunctionApi>? UtilityFunctions { get; set; }
        [JsonPropertyName("builtin_classes")] public List<BuiltinClassApi>? BuiltinClasses { get; set; }
        [JsonPropertyName("global_enums")] public List<EnumApi>? GlobalEnums { get; set; }
        [JsonPropertyName("singletons")] public List<SingletonApi>? Singletons { get; set; }
    }

    /// <summary>Engine methods handled by a dedicated intrinsic rather than the
    /// generated shim/ptrcall surface: only <c>emit_signal</c>, a vararg helper the
    /// <c>Object</c> shim hand-writes. <c>connect</c> is NOT excluded (Callable is a
    /// first-class argument type, so it ptrcalls like any other method), and every
    /// <c>is_virtual</c> method is emitted as an overridable <c>public virtual</c>
    /// bridged generically through the node-class virtual table.</summary>
    internal static readonly HashSet<(string Class, string Method)> ExcludedMethods = new()
    {
        ("Object", "emit_signal"),
    };

    /// <summary>Builtin value types generated from <c>builtin_classes</c>.
    /// Excludes the hand-written/special types (<c>Variant</c>, <c>Callable</c>),
    /// the primitives, and the <c>string</c>-aliased / <c>has_destructor</c> heap
    /// types.</summary>
    internal static readonly HashSet<string> GeneratedBuiltins = new(StringComparer.Ordinal)
    {
        "Vector2",
        "Vector3",
        "Vector2i",
        "Vector3i",
        "Vector4",
        "Vector4i",
        "Quaternion",
        "Color",
        "Rect2",
        "Rect2i",
        "Plane",
        "AABB",
        "Transform2D",
        "Basis",
        "Transform3D",
        "Projection",
    };

    /// <summary>The packed-array engine types and their GodotSharp C# surface
    /// types. Packed arrays have no wrapper shim type: an engine value is built
    /// from / copied out to a plain managed array at the call boundary (the same
    /// build-call-destroy ownership pattern the String args/returns use).</summary>
    internal static readonly Dictionary<string, string> PackedArrayTypes = new(StringComparer.Ordinal)
    {
        ["PackedByteArray"] = "byte[]",
        ["PackedInt32Array"] = "int[]",
        ["PackedInt64Array"] = "long[]",
        ["PackedFloat32Array"] = "float[]",
        ["PackedFloat64Array"] = "double[]",
        ["PackedStringArray"] = "string[]",
        ["PackedVector2Array"] = "Vector2[]",
        ["PackedVector3Array"] = "Vector3[]",
        ["PackedVector4Array"] = "Vector4[]",
        ["PackedColorArray"] = "Color[]",
    };

    /// <summary>Builtin value-type instance methods exposed via the engine bridge:
    /// generated as placeholder-body shim signatures and lowered by
    /// <see cref="GodotCallIntrinsics"/> to a runtime <c>variant_get_ptr_builtin_method</c>
    /// call, so the math comes from the engine instead of being reimplemented in C#.
    /// Curated (not the full method surface) so it never collides with the
    /// hand-written shim partials and stays within the supported marshalling shapes
    /// (receiver/args/return ∈ primitives + value types). <b>Key:</b> (builtin type,
    /// engine snake_case method name).</summary>
    internal static readonly HashSet<(string Type, string Method)> BridgedBuiltinMethods = new()
    {
        // Color colour-space / blend (value-type return)
        ("Color", "srgb_to_linear"),
        ("Color", "linear_to_srgb"),
        ("Color", "blend"),
        // bool / float returns across Color/Vector2/Vector3. (The packed
        // `to_rgba32`/`to_argb32` int returns are deliberately omitted: Godot's `int`
        // is 64-bit but the shim maps it to C# `int` (32-bit), so a packed colour
        // > 2^31 truncates to a negative value — a shim int-width limitation orthogonal
        // to the bridge, not a marshalling bug.)
        ("Color", "is_equal_approx"),
        ("Vector2", "aspect"),
        ("Vector2", "is_normalized"),
        ("Vector3", "is_equal_approx"),
        // Vector4 method surface. Vector4 has no hand-written methods, so the
        // whole surface routes through the bridge. (The `int`-returning
        // `*_axis_index` are deferred — the Godot-int=64-bit vs shim-int=32-bit
        // width caveat.)
        ("Vector4", "abs"),
        ("Vector4", "normalized"),
        ("Vector4", "inverse"),
        ("Vector4", "min"),
        ("Vector4", "max"),
        ("Vector4", "direction_to"),
        ("Vector4", "snappedf"),
        ("Vector4", "lerp"),
        ("Vector4", "clamp"),
        ("Vector4", "clampf"),
        ("Vector4", "length"),
        ("Vector4", "dot"),
        ("Vector4", "distance_to"),
        ("Vector4", "is_normalized"),
        ("Vector4", "is_equal_approx"),
        ("Vector4", "is_finite"),
        // Quaternion method surface (tag 13 — 4 floats x/y/z/w, non-transpose like
        // Vector4). The hand-written shim partials
        // (Dot/Length/LengthSquared/Inverse/Normalized/Slerp/FromEuler) are skipped;
        // the rest ride the bridge. The receiver value type can differ from the
        // return (`get_euler`/`get_axis` return Vector3 from a Quaternion), and
        // `get_euler` takes an int EulerOrder arg.
        ("Quaternion", "is_normalized"),
        ("Quaternion", "is_equal_approx"),
        ("Quaternion", "is_finite"),
        ("Quaternion", "log"),
        ("Quaternion", "exp"),
        ("Quaternion", "angle_to"),
        ("Quaternion", "slerpni"),
        ("Quaternion", "spherical_cubic_interpolate"),
        ("Quaternion", "get_euler"),
        ("Quaternion", "get_axis"),
        ("Quaternion", "get_angle"),
        // Transform2D + Projection: the *large* (>16-byte) bridge value types. Both
        // have an identity shim↔native layout (Transform2D = cols X/Y/Origin,
        // Projection = cols X/Y/Z/W), so they ride the generic value-type path by
        // direct struct passing just like Vector4 — no transpose, no runtime change
        // (the enum/ToVariantType already carry tags 9/12).
        // Neither type has any hand-written method, so the whole supported surface
        // rides the bridge. (`Projection.get_pixels_per_meter` int-return deferred —
        // the int-width caveat.) Covers large-value-type returns AND args
        // (interpolate_with / is_equal_approx take a Transform2D).
        ("Transform2D", "affine_inverse"),
        ("Transform2D", "orthonormalized"),
        ("Transform2D", "rotated"),
        ("Transform2D", "scaled"),
        ("Transform2D", "interpolate_with"),
        ("Transform2D", "determinant"),
        ("Transform2D", "get_rotation"),
        ("Transform2D", "get_origin"),
        ("Transform2D", "basis_xform"),
        ("Transform2D", "is_conformal"),
        ("Transform2D", "is_equal_approx"),
        ("Transform2D", "is_finite"),
        ("Projection", "inverse"),
        ("Projection", "perspective_znear_adjusted"),
        ("Projection", "flipped_y"),
        ("Projection", "determinant"),
        ("Projection", "get_aspect"),
        ("Projection", "get_z_far"),
        ("Projection", "is_orthogonal"),
        ("Projection", "get_viewport_half_extents"),
        // Basis + Transform3D: the *transpose* matrix types (the shim stores columns,
        // Godot rows). The bridge transposes the receiver/value-type-args/return via
        // GdTag.BigTypeFloatMap (an involution), so these ride the same path as
        // the identity types. The hand-written Basis partials
        // (Inverse/Transposed/Determinant) are skipped; Transform3D has none. A
        // transpose value type can be passed *as an argument* (Basis.slerp / Transform3D.
        // interpolate_with / is_equal_approx), and a transpose receiver can return a
        // different type (Basis.get_rotation_quaternion → Quaternion, get_euler/get_scale
        // → Vector3). (`Basis.get_euler` takes an int EulerOrder arg.)
        ("Basis", "orthonormalized"),
        ("Basis", "rotated"),
        ("Basis", "scaled"),
        ("Basis", "get_scale"),
        ("Basis", "get_euler"),
        ("Basis", "get_rotation_quaternion"),
        ("Basis", "slerp"),
        ("Basis", "tdotx"),
        ("Basis", "is_conformal"),
        ("Basis", "is_equal_approx"),
        ("Basis", "is_finite"),
        ("Transform3D", "inverse"),
        ("Transform3D", "affine_inverse"),
        ("Transform3D", "orthonormalized"),
        ("Transform3D", "rotated"),
        ("Transform3D", "scaled"),
        ("Transform3D", "translated"),
        ("Transform3D", "interpolate_with"),
        ("Transform3D", "is_equal_approx"),
        ("Transform3D", "is_finite"),
        // string return. Color.to_html is the only builtin value-type method
        // returning a String; the engine writes a heap Godot String into the
        // ptrcall return, which the bridge converts to a managed Dn2CppString*
        // (dn2cpp_godot_builtin_call_str) and frees. A bool argument
        // (with_alpha) is ptrcall-encoded as a 1-byte uint8_t.
        ("Color", "to_html"),
        // Variant return. The Plane/AABB intersection queries return a
        // Variant that is either NIL (no hit) or a Vector3 (the hit point). Plane
        // (tag 14) and AABB (tag 15) join as identity-layout bridge receiver types;
        // the Variant result is marshalled into the shim Variant (nil / Vector3
        // payload) by dn2cpp_godot_builtin_call_variant. Args are Vector3 (tag 6).
        ("Plane", "intersects_ray"),
        ("Plane", "intersects_segment"),
        ("AABB", "intersects_ray"),
        ("AABB", "intersects_segment"),
    };

    /// <summary>For types whose <c>members</c> list mixes real storage with derived
    /// values (e.g. <c>Color</c> exposes <c>r8</c>/<c>h</c>/<c>s</c>/<c>v</c>/
    /// <c>ok_hsl_*</c> alongside the real <c>r</c>/<c>g</c>/<c>b</c>/<c>a</c>), this
    /// pins the real-storage members (and their order) so the generator emits the
    /// correct layout instead of materializing the derived members as fields.
    /// Types absent here use their full <c>members</c> list as-is.</summary>
    internal static readonly Dictionary<string, string[]> BuiltinRealMembers = new(StringComparer.Ordinal)
    {
        ["Color"] = new[] { "r", "g", "b", "a" },
        ["Rect2"] = new[] { "position", "size" },  // `end` is derived (position + size)
        ["Rect2i"] = new[] { "position", "size" }, // `end` is derived
        ["AABB"] = new[] { "position", "size" },   // `end` is derived
        ["Plane"] = new[] { "normal", "d" },       // x/y/z are accessors into `normal`
    };

    private static readonly object LoadLock = new();
    private static readonly Dictionary<string, (List<ClassApi>, Dictionary<string, ClassApi>, List<UtilityFunctionApi>, List<BuiltinClassApi>, Dictionary<string, BuiltinClassApi>, List<EnumApi>)> LoadCache = new(StringComparer.Ordinal);

    /// <summary>Parses <paramref name="jsonPath"/> and returns the Object-derived
    /// classes (the GDExtension hierarchy) plus a name lookup over every class,
    /// and the global enums (a plain ordered list — 22 entries, so the enum
    /// lookups scan it linearly, which keeps generation order deterministic).
    /// Memoized per path — one transpile loads the dump from several places
    /// (the intrinsics maps, the epilogue) and the model is never mutated.</summary>
    internal static (List<ClassApi> ObjectClasses, Dictionary<string, ClassApi> All, List<UtilityFunctionApi> Utilities, List<BuiltinClassApi> Builtins, Dictionary<string, BuiltinClassApi> AllBuiltins, List<EnumApi> GlobalEnums) Load(string jsonPath)
    {
        string key = Path.GetFullPath(jsonPath);
        lock (LoadLock)
        {
            if (LoadCache.TryGetValue(key, out var cached))
                return cached;
            var loaded = LoadUncached(jsonPath);
            LoadCache[key] = loaded;
            return loaded;
        }
    }

    private static (List<ClassApi> ObjectClasses, Dictionary<string, ClassApi> All, List<UtilityFunctionApi> Utilities, List<BuiltinClassApi> Builtins, Dictionary<string, BuiltinClassApi> AllBuiltins, List<EnumApi> GlobalEnums) LoadUncached(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException("extension_api.json not found", jsonPath);
        }

        string jsonContent = File.ReadAllText(jsonPath);
        // Source-gen path (not the reflection-based Deserialize<T>): the generated
        // JsonTypeInfo<ExtensionApi> + converters are produced at compile time, so
        // there is no runtime generic instantiation (the MakeGenericType AOT carve-out
        // the reflection overload would hit), while the generated converters are still
        // the real System.Text.Json IL. This is what lets dn2cpp transpile its own
        // engine-binding load path (Godot-lane self-host).
        var api = JsonSerializer.Deserialize(jsonContent, GodotApiJsonContext.Default.ExtensionApi);
        if (api?.Classes == null)
        {
            throw new InvalidDataException("Failed to deserialize extension_api.json classes");
        }

        var all = api.Classes.ToDictionary(c => c.Name);

        // Project the top-level `singletons` array onto the classes it names, so a
        // singleton is a property of the class rather than a set every type-mapping
        // call site would have to be handed.
        foreach (var s in api.Singletons ?? new List<SingletonApi>())
        {
            // The static facade is generated under the singleton's *global* name and
            // ptrcalls the binds of its *type*. Every entry in every dump so far has
            // the two equal; if one ever diverges, the facade would call the right
            // method on the wrong class — so stop rather than guess which name wins.
            if (s.Name != s.Type)
            {
                throw new InvalidDataException(
                    $"engine singleton '{s.Name}' is typed '{s.Type}': the singleton's global name and its "
                    + "class name must agree — a facade generated under one and bound against the other "
                    + "would call the wrong engine class");
            }
            if (!all.TryGetValue(s.Type, out var scls))
            {
                throw new InvalidDataException(
                    $"engine singleton '{s.Name}' names class '{s.Type}', which is not in the dump's classes");
            }
            scls.IsSingleton = true;
        }

        var objectClasses = api.Classes.Where(c => IsObjectDerived(c, all)).ToList();
        var utilities = api.UtilityFunctions ?? new List<UtilityFunctionApi>();
        var allBuiltinsList = api.BuiltinClasses ?? new List<BuiltinClassApi>();
        var builtins = allBuiltinsList.Where(b => GeneratedBuiltins.Contains(b.Name)).ToList();
        var allBuiltins = allBuiltinsList.ToDictionary(b => b.Name);
        var globalEnums = api.GlobalEnums ?? new List<EnumApi>();
        return (objectClasses, all, utilities, builtins, allBuiltins, globalEnums);
    }

    /// <summary>The C# name of the type carrying an engine class's INSTANCE members.
    /// For an ordinary class that is the class's own name; for a singleton it is
    /// <c>&lt;Name&gt;Instance</c>, because the engine name goes to a <b>static
    /// facade</b> instead.
    ///
    /// <para>A singleton is surfaced GodotSharp-style as two C# types over one engine
    /// class: <c>static class ProjectSettings</c> (what game code writes —
    /// <c>ProjectSettings.GlobalizePath(path)</c>) plus <c>class
    /// ProjectSettingsInstance</c> for the ClassDB class itself. The split is forced,
    /// not stylistic: C# cannot host a static class and an instance class of the same
    /// name in one namespace. Matching GodotSharp's spelling is what lets one game
    /// source build on both the GDExtension and the mono-module lane.</para>
    ///
    /// <para>This is the one naming seam, shared by the shim generator and the
    /// transpiler's call map, so the two cannot drift into disagreeing about which
    /// C# type a bind belongs to.</para></summary>
    internal static string InstanceCSharpName(ClassApi cls) => cls.IsSingleton ? cls.Name + "Instance" : cls.Name;

    /// <summary>True when every value of <paramref name="e"/> fits an int32, i.e.
    /// the enum can be generated with the default C# <c>int</c> backing. The one
    /// current exception (RenderingServer.ArrayFormat carries 1&lt;&lt;35 flags) is
    /// skipped entirely: the transpiler models every enum as an int32 stack value
    /// (<c>CppTypes.Of</c>/<c>KindOf</c>), so a <c>long</c>-backed enum would
    /// silently truncate rather than work.</summary>
    internal static bool IsInt32Enum(EnumApi e)
    {
        foreach (var v in e.Values ?? new List<EnumValueApi>())
        {
            if (v.Value < int.MinValue || v.Value > int.MaxValue) return false;
        }
        return true;
    }

    /// <summary>The C# name for an object class's nested enum: the engine name,
    /// suffixed with <c>Enum</c> while it collides with the class's own name or any
    /// method/property PascalCase name (matching GodotSharp's convention — e.g.
    /// Node's <c>ProcessMode</c> property forces the enum to <c>ProcessModeEnum</c>).
    /// Computed from the raw API surface, so the generator and the type mapping
    /// can never disagree.</summary>
    internal static string ClassEnumName(ClassApi cls, string enumName)
    {
        var taken = new HashSet<string>(StringComparer.Ordinal) { cls.Name };
        foreach (var m in cls.Methods ?? new List<MethodApi>())
            taken.Add(ToPascalCase(m.Name));
        foreach (var p in cls.Properties ?? new List<PropertyApi>())
            taken.Add(ToPascalCase(p.Name));

        string name = enumName;
        while (taken.Contains(name))
            name += "Enum";
        return name;
    }

    /// <summary>Resolves an <c>enum::X</c> / <c>bitfield::X</c> engine type to its
    /// generated declaration and C# type name (<c>Owner.Name</c> for an
    /// object-class enum, the bare name for a global one). Returns false for
    /// anything not generated: non-enum types, builtin-class-owned enums
    /// (<c>enum::Variant.Type</c>, <c>enum::Vector3.Axis</c> — their owners have no
    /// generated class to nest in), and enums with values beyond int32 range
    /// (see <see cref="IsInt32Enum"/>).</summary>
    internal static bool TryResolveEnum(string godotType, Dictionary<string, ClassApi> all,
        List<EnumApi> globalEnums, out EnumApi enumApi, out string csharpType)
    {
        enumApi = null!;
        csharpType = "";

        string rest;
        if (godotType.StartsWith("enum::")) rest = godotType.Substring(6);
        else if (godotType.StartsWith("bitfield::")) rest = godotType.Substring(10);
        else return false;

        int dot = rest.IndexOf('.');
        if (dot < 0)
        {
            var ge = globalEnums.FirstOrDefault(e => e.Name == rest);
            if (ge is null || !IsInt32Enum(ge)) return false;
            enumApi = ge;
            csharpType = rest;
            return true;
        }

        string owner = rest.Substring(0, dot);
        string name = rest.Substring(dot + 1);
        if (!all.TryGetValue(owner, out var cls)) return false;
        var ce = cls.Enums?.FirstOrDefault(e => e.Name == name);
        if (ce is null || !IsInt32Enum(ce)) return false;
        enumApi = ce;
        csharpType = $"{owner}.{ClassEnumName(cls, name)}";
        return true;
    }

    /// <summary>The C# member name for an enum value: the shared
    /// <c>ENUM_NAME_</c> prefix is stripped when present (Godot's convention —
    /// <c>PROCESS_MODE_INHERIT</c> on <c>ProcessMode</c> becomes <c>Inherit</c>),
    /// keeping the full PascalCase name when the value doesn't carry the prefix
    /// (<c>Error</c>'s <c>OK</c>/<c>ERR_*</c>), when stripping would leave a
    /// digit-leading identifier (<c>KEY_1</c> → <c>Key1</c>), or when the result
    /// would collide with the enclosing enum's own name (a C# error).</summary>
    internal static string EnumValueName(string enumCSharpName, string enumEngineName, string valueName)
    {
        string prefix = PascalToUpperSnake(enumEngineName) + "_";
        string result;
        if (valueName.StartsWith(prefix)
            && valueName.Length > prefix.Length
            && !char.IsDigit(valueName[prefix.Length]))
        {
            result = ConstNameToPascal(valueName.Substring(prefix.Length));
        }
        else
        {
            result = ConstNameToPascal(valueName);
        }
        if (result == enumCSharpName)
            result = ConstNameToPascal(valueName);
        return result;
    }

    /// <summary>Converts a PascalCase enum name to the ALL_CAPS prefix its values
    /// use (<c>ProcessMode</c> → <c>PROCESS_MODE</c>, <c>MIDIMessage</c> →
    /// <c>MIDI_MESSAGE</c>): an underscore goes before an upper/digit following a
    /// lower/digit, and before the last upper of an acronym run.</summary>
    private static string PascalToUpperSnake(string name)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (i > 0 && (char.IsUpper(c) || char.IsDigit(c)))
            {
                char prev = name[i - 1];
                bool afterLower = char.IsLower(prev) || (char.IsDigit(prev) && char.IsUpper(c));
                bool acronymEnd = char.IsUpper(prev) && char.IsUpper(c)
                    && i + 1 < name.Length && char.IsLower(name[i + 1]);
                if (afterLower || acronymEnd)
                    sb.Append('_');
            }
            sb.Append(char.ToUpperInvariant(c));
        }
        return sb.ToString();
    }

    /// <summary>Maps a builtin value type's <c>member</c> type to the C# field
    /// type. Unlike the engine-signature map, this keeps the natural 32-bit width
    /// (Godot's value structs are <c>float</c>/<c>int32</c>, like the hand-written
    /// <c>Vector2</c>'s <c>{float X, float Y}</c> layout). Returns null for a
    /// member whose type isn't a primitive (e.g. a nested builtin), which makes
    /// the whole type ungeneratable for now.</summary>
    internal static string? MapMemberTypeToCSharp(string godotType)
    {
        return godotType switch
        {
            "float" => "float",
            "int" => "int",
            "bool" => "bool",
            // String/Variant aren't builtin value-struct field types but appear as
            // bridged builtin-method return types (Color.to_html → String, the
            // Plane/AABB intersection queries → Variant).
            "String" => "string",
            "Variant" => "Variant",
            // A member whose type is another known value type (e.g. Rect2's
            // Vector2 position/size) maps to that shim type.
            "Vector2" => "Vector2",
            _ when GeneratedBuiltins.Contains(godotType) => godotType,
            _ => null
        };
    }

    /// <summary>The <c>Dn2CppGdType</c> tag (matching the runtime's <c>DN2CPP_GD_*</c>
    /// enum) for a builtin-bridge method's return/argument type, or null when the type
    /// isn't a supported bridge shape. Restricted to the value types whose shim layout
    /// equals Godot's native ptrcall layout plus the primitives. Most pass the shim struct
    /// straight through as the native base/arg/return pointer (the bridge's direct struct
    /// passing is size-agnostic, so the large identity-layout Transform2D/Projection ride
    /// it too); the column-major Basis/Transform3D need a transpose, which the bridge
    /// applies via <see cref="GdTag.BigTypeFloatMap"/>. A null
    /// <paramref name="godotType"/> is the void return (0).</summary>
    internal static int? BridgeGdTag(string? godotType) => godotType switch
    {
        null => GdTag.Void,
        "bool" => GdTag.Bool,
        "int" => GdTag.Int,
        "float" => GdTag.Float,
        "String" => GdTag.String,
        "Vector2" => GdTag.Vector2,
        "Vector3" => GdTag.Vector3,
        "Vector4" => GdTag.Vector4,
        "Color" => GdTag.Color,
        "Transform2D" => GdTag.Transform2D,
        "Basis" => GdTag.Basis,
        "Transform3D" => GdTag.Transform3D,
        "Projection" => GdTag.Projection,
        "Quaternion" => GdTag.Quaternion,
        "Plane" => GdTag.Plane,
        "AABB" => GdTag.Aabb,
        "Variant" => GdTag.Variant, // engine Variant return → shim Variant
        _ => null,
    };

    /// <summary>Converts an ALL_CAPS engine constant name to Godot's C# PascalCase
    /// (<c>ZERO</c> → <c>Zero</c>, <c>MODEL_FRONT</c> → <c>ModelFront</c>).</summary>
    internal static string ConstNameToPascal(string name)
    {
        var sb = new StringBuilder();
        foreach (var part in name.Split('_'))
        {
            if (part.Length == 0) continue;
            sb.Append(char.ToUpper(part[0]));
            if (part.Length > 1)
                sb.Append(part.Substring(1).ToLowerInvariant());
        }
        return sb.ToString();
    }

    /// <summary>Maps a Godot utility-function type to the shim C# type, or null
    /// when the type isn't modeled yet (e.g. <c>PackedByteArray</c>/<c>RID</c>);
    /// the whole function is then skipped. <c>null</c>/empty input is <c>void</c>
    /// (an absent <c>return_type</c> means a void function).</summary>
    internal static string? MapUtilTypeToCSharp(string? godotType, Dictionary<string, ClassApi> all)
    {
        if (string.IsNullOrEmpty(godotType) || godotType == "void") return "void";
        return godotType switch
        {
            "bool" => "bool",
            "int" => "long",
            "float" => "double",
            "String" or "StringName" or "NodePath" => "string",
            "Variant" => "Variant",
            "Object" => "Object",
            "Vector2" => "Vector2",
            // A signature naming a singleton's class means the engine OBJECT, which
            // lives on the instance class — the engine name belongs to the static
            // facade (see InstanceCSharpName).
            _ when all.TryGetValue(godotType, out var cls) && IsObjectDerived(cls, all) => InstanceCSharpName(cls),
            _ => null
        };
    }

    internal static bool IsObjectDerived(ClassApi cls, Dictionary<string, ClassApi> all)
    {
        if (cls.Name == "Object") return true;
        var current = cls;
        while (!string.IsNullOrEmpty(current.Inherits))
        {
            if (current.Inherits == "Object") return true;
            if (all.TryGetValue(current.Inherits, out var parent))
                current = parent;
            else
                break;
        }
        return false;
    }

    // ── The supported-shape predicates ─────────────────────────────────────────
    // MapTypeToGd below is the SINGLE TRUTH of which engine types the generated
    // surface supports: it maps an engine type to its marshalling kind, and the
    // other three predicates (MapTypeToCSharp / IsSupportedArgType /
    // IsSupportedRetType) are thin projections of its verdict, so the four cannot
    // drift apart per-shape again. One deliberate divergence remains, stated at
    // IsSupportedRetType: object-typed engine PROPERTIES are dropped from the shim
    // while object-returning methods are kept.

    /// <summary>The generated C# spelling for a supported engine type
    /// (<c>int</c> → <c>long</c>, the string family → <c>string</c>, …), or null
    /// when <see cref="MapTypeToGd"/> does not support the shape. Keyed off the
    /// marshalling kind so the supported domain is exactly MapTypeToGd's.</summary>
    internal static string? MapTypeToCSharp(string godotType, Dictionary<string, ClassApi> all, List<EnumApi> globalEnums)
    {
        return MapTypeToGd(godotType, all, globalEnums) switch
        {
            GdKind.Bool => "bool",
            GdKind.Int => "long",
            GdKind.Float => "double",
            // The three string-family engine types share the C# shim type.
            GdKind.String or GdKind.StringName or GdKind.NodePath => "string",
            // A bare Variant arg/return maps to the shim's tagged Variant struct.
            GdKind.Variant => "Variant",
            GdKind.Vector2 => "Vector2",
            // The engine dump calls the resource-id type `RID`; the C# shim
            // follows GodotSharp's `Rid` (a hand-written 8-byte struct). Every
            // other Pod kind is a generated builtin keeping its engine name.
            GdKind.Pod => godotType == "RID" ? "Rid" : godotType,
            // The engine containers map to the engine-backed wrapper classes
            // (the generated file lives inside `namespace Godot`, so the
            // Collections-qualified name resolves to Godot.Collections.*);
            // typedarray::T surfaces as the GodotSharp-style generic wrapper
            // over the same engine value, for the supported element types.
            GdKind.GdArray => TypedArrayElement(godotType) is { } elem
                ? MapTypeToCSharp(elem, all, globalEnums) is { } elemCs
                    ? $"Collections.Array<{elemCs}>"
                    : null
                : "Collections.Array",
            GdKind.GdDictionary => "Collections.Dictionary",
            // The 16-byte engine value types map to the hand-written
            // field-carrying shim structs (Callable.cs).
            GdKind.Callable => "Callable",
            GdKind.Signal => "Signal",
            GdKind.Packed => PackedArrayTypes[godotType],
            // A signature naming a singleton's class (EditorPlugin.get_editor_interface
            // -> EditorInterface, PhysicsServer2DExtension's base) means the engine
            // OBJECT, which lives on the instance class.
            GdKind.GdObject => InstanceCSharpName(all[godotType]),
            // An enum stays on the FACADE — GodotSharp spells them Input.MouseModeEnum,
            // and TryResolveEnum already names them with the engine owner, which IS the
            // facade's name. Nothing to translate here.
            GdKind.Enum => TryResolveEnum(godotType, all, globalEnums, out _, out var enumCs) ? enumCs : null,
            _ => null
        };
    }

    /// <summary>Maps an engine type to its marshalling kind, or null when the shape
    /// is unsupported. The single truth of the supported-type domain — the three
    /// predicates above/below project this verdict (see the note above
    /// <see cref="MapTypeToCSharp"/>).</summary>
    internal static GdKind? MapTypeToGd(string godotType, Dictionary<string, ClassApi> all, List<EnumApi> globalEnums)
    {
        return godotType switch
        {
            "bool" => GdKind.Bool,
            "int" => GdKind.Int,
            "float" => GdKind.Float,
            // The three string-family types share the C# shim type (`string`) but
            // have distinct engine ptrcall encodings, so they map to distinct
            // marshalling tags. Only plain `String` is bridged today;
            // StringName/NodePath are recognised so they stay in the call map but
            // are rejected at emit time (see GodotCallIntrinsics).
            "String" => GdKind.String,
            "StringName" => GdKind.StringName,
            "NodePath" => GdKind.NodePath,
            "Vector2" => GdKind.Vector2,
            // Every other generated builtin POD (plus the opaque 8-byte RID)
            // rides the generic ptrcall by pointer to native-layout storage.
            "RID" => GdKind.Pod,
            // A bare Variant travels as a real engine Variant on the wire,
            // converted to/from the shim Variant struct by the runtime codec.
            "Variant" => GdKind.Variant,
            // The engine containers travel as pointers to an engine
            // Array/Dictionary value — the wrapper's own value for arguments, a
            // fresh wrapper-owned value for returns. typedarray::T rides the
            // same GdArray path (the element conversion is keyed off the
            // signature's Array<T> instantiation at emit time).
            "Array" => GdKind.GdArray,
            "Dictionary" => GdKind.GdDictionary,
            // Callable/Signal travel as pointers to a pinned engine value built
            // from / decoded into the field-carrying shim struct.
            "Callable" => GdKind.Callable,
            "Signal" => GdKind.Signal,
            _ when TypedArrayElement(godotType) is { } elem
                && IsSupportedTypedArrayElement(elem, all) => GdKind.GdArray,
            // Packed arrays marshal to/from managed arrays at the boundary; the
            // emit side derives the concrete kind from the C# element type.
            _ when PackedArrayTypes.ContainsKey(godotType) => GdKind.Packed,
            _ when GeneratedBuiltins.Contains(godotType) => GdKind.Pod,
            _ when all.TryGetValue(godotType, out var cls) && IsObjectDerived(cls, all) => GdKind.GdObject,
            _ when TryResolveEnum(godotType, all, globalEnums, out _, out _) => GdKind.Enum,
            _ => null
        };
    }

    /// <summary>Whether an engine method argument type is supported: exactly
    /// <see cref="MapTypeToGd"/>'s domain.</summary>
    internal static bool IsSupportedArgType(string type, Dictionary<string, ClassApi> all, List<EnumApi> globalEnums)
        => MapTypeToGd(type, all, globalEnums) is not null;

    /// <summary>Whether an engine RETURN type is supported where this predicate
    /// gates: <see cref="MapTypeToGd"/>'s domain plus <c>void</c>, MINUS the
    /// engine-object kind. Its only consumer is the property surface
    /// (<c>BindingGenerator.EmitClassProperties</c>): object-typed engine PROPERTIES
    /// are dropped from the shim while object-returning METHODS are kept (they gate on
    /// <see cref="MapTypeToCSharp"/> directly) — so <c>GetOwner()</c> exists and
    /// <c>Owner</c> does not. Widening the property surface to objects would change the
    /// generated shim; do not fold this into <see cref="IsSupportedArgType"/>.</summary>
    internal static bool IsSupportedRetType(string type, Dictionary<string, ClassApi> all, List<EnumApi> globalEnums)
        => type == "void"
            || MapTypeToGd(type, all, globalEnums) is { } kind && kind != GdKind.GdObject;

    /// <summary>The element type of a <c>typedarray::T</c> engine type, or null
    /// for any other type string.</summary>
    internal static string? TypedArrayElement(string godotType) =>
        godotType.StartsWith("typedarray::") ? godotType.Substring("typedarray::".Length) : null;

    /// <summary>Whether a <c>typedarray::T</c> element type is supported by the
    /// typed wrapper surface (<c>Godot.Collections.Array&lt;T&gt;</c>): the
    /// engine-object classes plus the element kinds whose Variant payload the
    /// codec models (scalars, the string family, RID, packed arrays, and nested
    /// containers). The big builtin PODs (Plane, Transform3D, the int vectors)
    /// stay out until they are Variant payload kinds themselves.</summary>
    internal static bool IsSupportedTypedArrayElement(string elem, Dictionary<string, ClassApi> all)
    {
        return elem switch
        {
            "bool" or "int" or "float" or "String" or "StringName" or "NodePath" or "RID"
                or "Array" or "Dictionary" => true,
            _ when PackedArrayTypes.ContainsKey(elem) => true,
            _ when all.TryGetValue(elem, out var cls) && IsObjectDerived(cls, all) => true,
            _ => false
        };
    }

    internal static string ToPascalCase(string snake)
    {
        if (string.IsNullOrEmpty(snake)) return snake;
        bool startsWithUnderscore = snake.StartsWith("_");
        var parts = snake.Split('_');
        var sb = new StringBuilder();
        if (startsWithUnderscore)
            sb.Append("_");
        foreach (var p in parts)
        {
            if (p.Length > 0)
            {
                sb.Append(char.ToUpper(p[0]));
                if (p.Length > 1)
                    sb.Append(p.Substring(1));
            }
        }
        return sb.ToString();
    }

    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
        "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected",
        "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while"
    };

    internal static string ToCamelCase(string snake)
    {
        if (string.IsNullOrEmpty(snake)) return snake;
        bool startsWithUnderscore = snake.StartsWith("_");
        string pascal = ToPascalCase(snake);
        if (pascal.Length > 0)
        {
            int firstCharIdx = startsWithUnderscore ? 1 : 0;
            if (firstCharIdx < pascal.Length)
            {
                string camel = pascal.Substring(0, firstCharIdx) + char.ToLower(pascal[firstCharIdx]) + pascal.Substring(firstCharIdx + 1);
                string check = startsWithUnderscore ? camel.Substring(1) : camel;
                if (CSharpKeywords.Contains(check))
                    return "@" + camel;
                return camel;
            }
            return pascal;
        }
        return snake;
    }
}

/// <summary>Source-generated <see cref="JsonSerializerContext"/> for the
/// <c>extension_api.json</c> model. The System.Text.Json source generator emits a
/// <c>JsonTypeInfo&lt;ExtensionApi&gt;</c> (and the converters for its nested
/// <c>List&lt;ClassApi&gt;</c>/… graph) at compile time, so <see cref="GodotApi.Load"/>
/// deserializes with no runtime generic instantiation — keeping the engine-binding
/// load path inside the AOT boundary dn2cpp self-hosts within.</summary>
[JsonSerializable(typeof(GodotApi.ExtensionApi))]
internal partial class GodotApiJsonContext : JsonSerializerContext
{
}
