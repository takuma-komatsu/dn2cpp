// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
// Licensed under the MIT License.

namespace Godot
{
    /// <summary>
    /// Stand-in for the official <c>Godot.Variant</c> value type: a tagged,
    /// field-carrying struct (bitwise-copy-safe — no owned engine storage, so the
    /// transpiler's value-type copies can never double-free) converted to/from a
    /// real engine Variant only at the runtime boundary (Decode/EncodeVariant in
    /// runtime/godot). Standard Godot C# code passes primitives, math value
    /// types, arrays, and engine objects where a Variant is expected, relying on
    /// the implicit conversions below. Unlike the rest of the shim these bodies
    /// ARE transpiled (the same field-carrying value-type pattern as
    /// <c>Callable</c>); only the terminal consumers (GD.Print, engine-call
    /// args/returns) are intrinsics.
    /// </summary>
    public readonly struct Variant
    {
        // Marshalling tag, mirrored by the runtime bridge (Decode/EncodeVariant
        // in dn2cpp_godot.cpp): 0=nil, 1=bool, 2=int, 3=float, 4=string,
        // 5=Vector3, 6=Vector2, 7=Vector4, 8=Color, 9=Object (engine handle in
        // _int), 10=Rid (raw id in _int), 11=StringName / 12=NodePath (text in
        // _str — the shim's string-family C# type is `string`, so only the tag
        // keeps the engine identity), 13..20 = the big builtin PODs (Rect2,
        // Plane, AABB, Quaternion, Transform2D, Basis, Transform3D, Projection —
        // a boxed shim struct in _obj), 21..30 = packed arrays (byte[], int[],
        // long[], float[], double[], string[], Vector2[], Vector3[], Vector4[],
        // Color[] — the managed array in _obj), 31 = Godot.Collections.Array /
        // 32 = Godot.Collections.Dictionary (the engine-backed wrapper class in
        // _obj — the codec shares the wrapper's engine container, so identity
        // survives the round trip), 33 = Callable / 34 = Signal (the boxed shim
        // struct in _obj — a delegate-backed Callable round-trips through the
        // engine via its custom-callable userdata, a standard one is rebuilt
        // from its target + method).
        internal readonly int _kind;
        internal readonly long _int;
        internal readonly double _float;
        internal readonly string? _str;
        // Float-vector payload — carries the small (≤4-float) builtin value types a
        // Variant can hold: Vector3 (kind 5, vx/vy/vz), Vector2 (kind 6, vx/vy),
        // Vector4 (kind 7, vx/vy/vz/vw) and Color (kind 8, r/g/b/a → vx/vy/vz/vw).
        // The runtime bridge (Decode/EncodeVariant) reads/writes these fields (C++
        // f__vx..f__vw) directly from the decoded engine Variant; the field order
        // must stay layout-identical to the runtime's Dn2CppGodotVariant struct.
        internal readonly float _vx;
        internal readonly float _vy;
        internal readonly float _vz;
        internal readonly float _vw;
        // Kinds 13..30: the boxed big-POD shim struct or the managed array
        // reference. The struct is readonly, so a boxed payload is immutable and
        // safe to share across bitwise Variant copies; an array payload is shared
        // by reference, matching the engine's copy-on-write Packed*Array
        // semantics closely enough for the call boundary (the runtime copies the
        // elements in/out per engine call).
        internal readonly object? _obj;
        // kind 9 (Object) only, and only for a RefCounted decoded from an engine
        // Variant/Array/Dictionary: a runtime-created guard object owning one engine
        // reference() that it returns from its finalizer, so the reference lives
        // exactly as long as this Variant is reachable — the engine-side source
        // container is destroyed right after decode, so this may be the only thing
        // keeping the object alive. Written by the runtime codec (DecodeVariant),
        // always null for managed-built Variants. Must stay the LAST field (layout
        // mirrored by Dn2CppGodotVariant).
        internal readonly object? _refGuard;

        private Variant(int kind, long i, double f, string? s)
        {
            _kind = kind;
            _int = i;
            _float = f;
            _str = s;
            _vx = 0;
            _vy = 0;
            _vz = 0;
            _vw = 0;
            _obj = null;
            _refGuard = null;
        }

        private Variant(int kind, float x, float y, float z, float w)
        {
            _kind = kind;
            _int = 0;
            _float = 0;
            _str = null;
            _vx = x;
            _vy = y;
            _vz = z;
            _vw = w;
            _obj = null;
            _refGuard = null;
        }

        // Boxed-struct / array payload (kinds 13..30). A null payload builds NIL,
        // so a null array converts like GodotSharp's (an empty engine value).
        private Variant(int kind, object? obj)
        {
            _kind = obj is null ? VariantKind.Nil : kind;
            _int = 0;
            _float = 0;
            _str = null;
            _vx = 0;
            _vy = 0;
            _vz = 0;
            _vw = 0;
            _obj = obj;
            _refGuard = null;
        }

        public static implicit operator Variant(bool value) => new Variant(VariantKind.Bool, value ? 1 : 0, 0, null);
        public static implicit operator Variant(long value) => new Variant(VariantKind.Int, value, 0, null);
        public static implicit operator Variant(int value) => new Variant(VariantKind.Int, value, 0, null);
        public static implicit operator Variant(double value) => new Variant(VariantKind.Float, 0, value, null);
        public static implicit operator Variant(float value) => new Variant(VariantKind.Float, 0, value, null);
        public static implicit operator Variant(string value) => new Variant(VariantKind.String, 0, 0, value);
        public static implicit operator Variant(Vector3 value) => new Variant(VariantKind.Vector3, value.X, value.Y, value.Z, 0);
        public static implicit operator Variant(Vector2 value) => new Variant(VariantKind.Vector2, value.X, value.Y, 0, 0);
        public static implicit operator Variant(Vector4 value) => new Variant(VariantKind.Vector4, value.X, value.Y, value.Z, value.W);
        public static implicit operator Variant(Color value) => new Variant(VariantKind.Color, value.R, value.G, value.B, value.A);
        public static implicit operator Variant(Rid value) => new Variant(VariantKind.Rid, unchecked((long)value.Id), 0, null);
        // The big builtin PODs are carried as a boxed shim struct: the box is
        // immutable (readonly struct, fresh box per conversion), so sharing it
        // across bitwise Variant copies is safe. The runtime codec reads the
        // box's flat floats (shim field order) and builds the engine value,
        // transposing Basis/Transform3D into Godot's row-major layout.
        public static implicit operator Variant(Rect2 value) => new Variant(VariantKind.Rect2, (object)value);
        public static implicit operator Variant(Plane value) => new Variant(VariantKind.Plane, (object)value);
        public static implicit operator Variant(AABB value) => new Variant(VariantKind.Aabb, (object)value);
        public static implicit operator Variant(Quaternion value) => new Variant(VariantKind.Quaternion, (object)value);
        public static implicit operator Variant(Transform2D value) => new Variant(VariantKind.Transform2D, (object)value);
        public static implicit operator Variant(Basis value) => new Variant(VariantKind.Basis, (object)value);
        public static implicit operator Variant(Transform3D value) => new Variant(VariantKind.Transform3D, (object)value);
        public static implicit operator Variant(Projection value) => new Variant(VariantKind.Projection, (object)value);
        // Packed arrays carry the managed array reference; the runtime builds /
        // copies out the engine Packed*Array at the call boundary (the same
        // element-wise codec the typed packed-array engine calls use).
        public static implicit operator Variant(byte[]? value) => new Variant(VariantKind.PackedByteArray, value);
        public static implicit operator Variant(int[]? value) => new Variant(VariantKind.PackedInt32Array, value);
        public static implicit operator Variant(long[]? value) => new Variant(VariantKind.PackedInt64Array, value);
        public static implicit operator Variant(float[]? value) => new Variant(VariantKind.PackedFloat32Array, value);
        public static implicit operator Variant(double[]? value) => new Variant(VariantKind.PackedFloat64Array, value);
        public static implicit operator Variant(string[]? value) => new Variant(VariantKind.PackedStringArray, value);
        public static implicit operator Variant(Vector2[]? value) => new Variant(VariantKind.PackedVector2Array, value);
        public static implicit operator Variant(Vector3[]? value) => new Variant(VariantKind.PackedVector3Array, value);
        public static implicit operator Variant(Vector4[]? value) => new Variant(VariantKind.PackedVector4Array, value);
        public static implicit operator Variant(Color[]? value) => new Variant(VariantKind.PackedColorArray, value);
        // The engine-backed container wrappers carry the wrapper reference; the
        // runtime codec builds the engine Variant from the wrapper's own engine
        // container value (reference-shared, CoW-free — mutations stay visible).
        public static implicit operator Variant(Collections.Array? value) => new Variant(VariantKind.GodotArray, (object?)value);
        public static implicit operator Variant(Collections.Dictionary? value) => new Variant(VariantKind.GodotDictionary, (object?)value);
        // Callable/Signal are carried as a boxed shim struct (like the big
        // PODs): the box shares the delegate/target references, and the codec
        // builds/decodes the engine value from the box's fields.
        public static implicit operator Variant(Callable value) => new Variant(VariantKind.Callable, (object)value);
        public static implicit operator Variant(Signal value) => new Variant(VariantKind.Signal, (object)value);

        // Reverse conversions so standard code can assign a Variant-returning API
        // (e.g. GetMeta / generated Variant-typed math like Mathf.Abs/Max) to a
        // typed local. The official binding exposes these too; implicit here is a
        // superset of its explicit form, so unmodified code still compiles.
        public static implicit operator bool(Variant value) => value._int != 0;
        public static implicit operator long(Variant value) => value._int;
        public static implicit operator int(Variant value) => (int)value._int;
        public static implicit operator double(Variant value) => value._float;
        public static implicit operator float(Variant value) => (float)value._float;
        // Covers String and the text-carried StringName/NodePath kinds alike.
        public static implicit operator string(Variant value) => value._str ?? "";
        // These `op_Implicit(Variant)` overloads share a parameter type and differ only
        // in return type; the transpiler disambiguates them by return type.
        public static implicit operator Vector3(Variant value) => new Vector3(value._vx, value._vy, value._vz);
        public static implicit operator Vector2(Variant value) => new Vector2(value._vx, value._vy);
        public static implicit operator Vector4(Variant value) => new Vector4(value._vx, value._vy, value._vz, value._vw);
        public static implicit operator Color(Variant value) => new Color(value._vx, value._vy, value._vz, value._vw);
        public static implicit operator Rid(Variant value) => new Rid(unchecked((ulong)value._int));
        // The boxed big-POD payloads unbox back to their struct; a kind mismatch
        // surfaces as the unbox's InvalidCastException (matching the official
        // binding's As* behavior of throwing on a wrong payload), while a NIL
        // Variant unboxes to the default value instead of throwing (mirroring
        // GodotSharp's As* behavior for an empty Variant, like Callable/Signal
        // below).
        public static implicit operator Rect2(Variant value) => value._obj is null ? default : (Rect2)value._obj;
        public static implicit operator Plane(Variant value) => value._obj is null ? default : (Plane)value._obj;
        public static implicit operator AABB(Variant value) => value._obj is null ? default : (AABB)value._obj;
        public static implicit operator Quaternion(Variant value) => value._obj is null ? default : (Quaternion)value._obj;
        public static implicit operator Transform2D(Variant value) => value._obj is null ? default : (Transform2D)value._obj;
        public static implicit operator Basis(Variant value) => value._obj is null ? default : (Basis)value._obj;
        public static implicit operator Transform3D(Variant value) => value._obj is null ? default : (Transform3D)value._obj;
        public static implicit operator Projection(Variant value) => value._obj is null ? default : (Projection)value._obj;
        public static implicit operator byte[]?(Variant value) => (byte[]?)value._obj;
        public static implicit operator int[]?(Variant value) => (int[]?)value._obj;
        public static implicit operator long[]?(Variant value) => (long[]?)value._obj;
        public static implicit operator float[]?(Variant value) => (float[]?)value._obj;
        public static implicit operator double[]?(Variant value) => (double[]?)value._obj;
        public static implicit operator string[]?(Variant value) => (string[]?)value._obj;
        public static implicit operator Vector2[]?(Variant value) => (Vector2[]?)value._obj;
        public static implicit operator Vector3[]?(Variant value) => (Vector3[]?)value._obj;
        public static implicit operator Vector4[]?(Variant value) => (Vector4[]?)value._obj;
        public static implicit operator Color[]?(Variant value) => (Color[]?)value._obj;
        public static implicit operator Collections.Array?(Variant value) => (Collections.Array?)value._obj;
        public static implicit operator Collections.Dictionary?(Variant value) => (Collections.Dictionary?)value._obj;
        // The boxed Callable/Signal payloads unbox back to their struct (a kind
        // mismatch throws, matching the big-POD reverse conversions above); a
        // NIL Variant unboxes to the default (null) value instead of throwing,
        // mirroring GodotSharp's As* behavior for an empty Variant.
        public static implicit operator Callable(Variant value) => value._obj is null ? default : (Callable)value._obj;
        public static implicit operator Signal(Variant value) => value._obj is null ? default : (Signal)value._obj;

        // Kind 9 carries an engine Object as its borrowed handle in _int. A
        // `(Object)variant` / `(Node)variant` cast wraps that handle back into a fresh
        // shim of the static target type (known at the cast site — no runtime type
        // lookup); the reverse builds a kind-9 Variant from a live shim's handle, and
        // every other engine type converts through the Object overload via an implicit
        // upcast. A NIL/handle-less Variant wraps as null, a null Object encodes as NIL.
        public static explicit operator Object?(Variant value)
        {
            if (value._kind != VariantKind.Object)
                return null;
            var o = new Object();
            o.__SetHandle((System.IntPtr)value._int);
            return o;
        }

        public static explicit operator Node?(Variant value)
        {
            if (value._kind != VariantKind.Object)
                return null;
            var n = new Node();
            n.__SetHandle((System.IntPtr)value._int);
            return n;
        }

        public static implicit operator Variant(Object? value) =>
            value is null ? new Variant(VariantKind.Nil, 0, 0, null) : new Variant(VariantKind.Object, (long)value.__GetHandle(), 0, null);

        // GodotSharp-style typed accessors, mirroring the official binding's As*
        // surface for the kinds this shim models (each is the matching reverse
        // conversion above, spelled as a method).
        public bool AsBool() => this;
        public int AsInt32() => this;
        public long AsInt64() => this;
        public float AsSingle() => this;
        public double AsDouble() => this;
        public string AsString() => this;
        public Vector2 AsVector2() => this;
        public Vector3 AsVector3() => this;
        public Vector4 AsVector4() => this;
        public Color AsColor() => this;
        public Rid AsRid() => this;
        public Rect2 AsRect2() => this;
        public Plane AsPlane() => this;
        public AABB AsAabb() => this;
        public Quaternion AsQuaternion() => this;
        public Transform2D AsTransform2D() => this;
        public Basis AsBasis() => this;
        public Transform3D AsTransform3D() => this;
        public Projection AsProjection() => this;
        public byte[]? AsByteArray() => this;
        public int[]? AsInt32Array() => this;
        public long[]? AsInt64Array() => this;
        public float[]? AsFloat32Array() => this;
        public double[]? AsFloat64Array() => this;
        public string[]? AsStringArray() => this;
        public Vector2[]? AsVector2Array() => this;
        public Vector3[]? AsVector3Array() => this;
        public Vector4[]? AsVector4Array() => this;
        public Color[]? AsColorArray() => this;
        public Collections.Array? AsGodotArray() => this;
        public Collections.Dictionary? AsGodotDictionary() => this;
        public Callable AsCallable() => this;
        public Signal AsSignal() => this;
        public Object? AsGodotObject() => (Object?)this;

        // The official binding spells this `VariantType == Variant.Type.Nil`; the
        // minimal shim exposes a direct predicate so a Variant-returning query (an
        // intersection that may miss) can be tested for a NIL result.
        public bool IsNil => _kind == VariantKind.Nil;
    }
}
