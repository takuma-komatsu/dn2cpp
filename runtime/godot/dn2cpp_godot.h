//
// Godot GDExtension bridge. The transpiler (in --gdextension mode) generates
// a dn2cpp_godot_methods table; this bridge registers every entry with the
// Godot ClassDB as a static method so GDScript can call transpiled C# code.

#pragma once

#include "dn2cpp.h"

enum Dn2CppGdType : int32_t
{
    DN2CPP_GD_VOID = 0,
    DN2CPP_GD_BOOL = 1,
    DN2CPP_GD_INT = 2,
    DN2CPP_GD_FLOAT = 3,
    DN2CPP_GD_STRING = 4,
    DN2CPP_GD_VECTOR2 = 5,
    DN2CPP_GD_VECTOR3 = 6,
    DN2CPP_GD_VECTOR4 = 7,
    DN2CPP_GD_COLOR = 8,
    DN2CPP_GD_TRANSFORM2D = 9,  // 6 floats
    DN2CPP_GD_BASIS = 10,       // 9 floats
    DN2CPP_GD_TRANSFORM3D = 11, // 12 floats
    DN2CPP_GD_PROJECTION = 12,  // 16 floats
    DN2CPP_GD_QUATERNION = 13,  // 4 floats (x,y,z,w) — non-transpose, like Vector4
    DN2CPP_GD_PLANE = 14,       // 4 floats (normal.x/y/z, d) — identity layout
    DN2CPP_GD_AABB = 15,        // 6 floats (position.x/y/z, size.x/y/z) — identity layout
    DN2CPP_GD_VARIANT = 16,     // engine Variant return — marshalled into Dn2CppGodotVariant
    // Packed scalar arrays <-> managed C# arrays. Each is a heap
    // `has_destructor` value marshalled element-wise to/from a Dn2CppArray*.
    DN2CPP_GD_PACKED_BYTE = 17,    // byte[]   <-> PackedByteArray
    DN2CPP_GD_PACKED_INT32 = 18,   // int[]    <-> PackedInt32Array
    DN2CPP_GD_PACKED_INT64 = 19,   // long[]   <-> PackedInt64Array
    DN2CPP_GD_PACKED_FLOAT32 = 20, // float[]  <-> PackedFloat32Array
    DN2CPP_GD_PACKED_FLOAT64 = 21, // double[] <-> PackedFloat64Array
    DN2CPP_GD_PACKED_STRING = 22,  // string[] <-> PackedStringArray
    // Struct-element packed arrays: contiguous shim structs, so the
    // same element-wise memcpy as the scalar packed types (single-precision layout).
    DN2CPP_GD_PACKED_VECTOR2 = 23, // Godot.Vector2[] <-> PackedVector2Array (8B elem)
    DN2CPP_GD_PACKED_VECTOR3 = 24, // Godot.Vector3[] <-> PackedVector3Array (12B elem)
    DN2CPP_GD_PACKED_VECTOR4 = 25, // Godot.Vector4[] <-> PackedVector4Array (16B elem)
    DN2CPP_GD_PACKED_COLOR = 26,   // Godot.Color[]   <-> PackedColorArray   (16B elem)
    // Heterogeneous Array <-> C# Godot.Variant[]. Elements are
    // engine Variants, decoded/encoded to the shim Variant struct (layout-identical
    // to the runtime's Dn2CppGodotVariant). The element-copying legacy mapping;
    // the engine-backed wrapper mapping is DN2CPP_GD_GODOT_ARRAY below.
    DN2CPP_GD_ARRAY = 27,
    // Engine Dictionary <-> Godot.Collections.Dictionary, an engine-backed
    // wrapper CLASS holding a pinned engine Dictionary value in its leading
    // handle field (identity semantics — the container is shared across the
    // boundary, not copied; the wrapper's finalizer destroys its value).
    DN2CPP_GD_DICTIONARY = 28,
    // Engine Array <-> Godot.Collections.Array (and its Array<T> typed
    // specializations), the same engine-backed wrapper shape as Dictionary.
    DN2CPP_GD_GODOT_ARRAY = 29,
    // The 16-byte engine value types Callable/Signal <-> the field-carrying
    // Godot.Callable/Godot.Signal shim structs (Dn2CppGodotCallableShim /
    // Dn2CppGodotSignalShim below). Runtime-internal tags (builtin-method
    // resolution on the Callable/Signal variant types); not export-boundary
    // types.
    DN2CPP_GD_CALLABLE = 30,
    DN2CPP_GD_SIGNAL = 31,
};

// The transpiler mirrors these values as bare int32 constants (GdTag.cs in
// src/Dn2Cpp.Godot) and stamps them into the generated method/property/signal
// tables — so the pairing is a wire contract, not a C++ implementation detail.
// These anchors are the renumber tripwire: never renumber, only append. (Same
// contract as the shim Variant's VariantKind.cs, whose packed payload kinds are
// these packed tags + DN2CPP_GDV_PACKED_TAG_OFFSET.)
static_assert(DN2CPP_GD_VOID == 0 && DN2CPP_GD_BOOL == 1 && DN2CPP_GD_INT == 2
        && DN2CPP_GD_FLOAT == 3 && DN2CPP_GD_STRING == 4,
    "Dn2CppGdType scalar tags are mirrored by the transpiler's GdTag.cs — never renumber");
static_assert(DN2CPP_GD_VECTOR2 == 5 && DN2CPP_GD_VECTOR3 == 6 && DN2CPP_GD_VECTOR4 == 7
        && DN2CPP_GD_COLOR == 8 && DN2CPP_GD_TRANSFORM2D == 9 && DN2CPP_GD_BASIS == 10
        && DN2CPP_GD_TRANSFORM3D == 11 && DN2CPP_GD_PROJECTION == 12
        && DN2CPP_GD_QUATERNION == 13 && DN2CPP_GD_PLANE == 14 && DN2CPP_GD_AABB == 15
        && DN2CPP_GD_VARIANT == 16,
    "Dn2CppGdType value-type tags are mirrored by the transpiler's GdTag.cs — never renumber");
static_assert(DN2CPP_GD_PACKED_BYTE == 17 && DN2CPP_GD_PACKED_INT32 == 18
        && DN2CPP_GD_PACKED_INT64 == 19 && DN2CPP_GD_PACKED_FLOAT32 == 20
        && DN2CPP_GD_PACKED_FLOAT64 == 21 && DN2CPP_GD_PACKED_STRING == 22
        && DN2CPP_GD_PACKED_VECTOR2 == 23 && DN2CPP_GD_PACKED_VECTOR3 == 24
        && DN2CPP_GD_PACKED_VECTOR4 == 25 && DN2CPP_GD_PACKED_COLOR == 26,
    "Dn2CppGdType packed tags are mirrored by the transpiler's GdTag.cs — never renumber");
static_assert(DN2CPP_GD_ARRAY == 27 && DN2CPP_GD_DICTIONARY == 28
        && DN2CPP_GD_GODOT_ARRAY == 29 && DN2CPP_GD_CALLABLE == 30 && DN2CPP_GD_SIGNAL == 31,
    "Dn2CppGdType container tags are mirrored by the transpiler's GdTag.cs — never renumber");

// NOTE: single-precision (real_t == float) Godot builds only. A double-precision
// engine build lays Vector2 out as two doubles (16 bytes), which this struct and
// the ptrcall memcpy paths in dn2cpp_godot.cpp do not handle.
struct Dn2CppVector2
{
    float x;
    float y;
};

struct Dn2CppVector3
{
    float x;
    float y;
    float z;
};

// By-value value types are inlined here. The widest slot is `big[16]` (64 bytes,
// up to a Projection's 16 floats), so the per-call args[8] frame is 512 bytes —
// acceptable for these cold marshalling paths. `big` holds Godot's NATIVE
// ptrcall/Variant byte order (e.g. row-major for Basis); the generated `gw_`
// wrappers map it to/from the C# shim's flat-float order. Larger-than-16-byte types
// are memcpy'd generically by their byte size (Dn2CppGdTypeByteSize); the small
// types keep their explicit v2/v3/v4 aliases.
union Dn2CppValue
{
    int64_t i;
    double d;
    Dn2CppString* s;
    void* arr;     // managed Dn2CppArray* payload (Packed* <-> C# array marshalling)
    float v2[2];
    float v3[3];
    float v4[4];  // Vector4 (XYZW) / Color (RGBA)
    float big[16]; // Transform2D(6) / Basis(9) / Transform3D(12) / Projection(16)
                   // / Quaternion(4) / Plane(4) / AABB(6)
};

typedef void (*Dn2CppInvoke)(void* instance, const Dn2CppValue* args, Dn2CppValue* ret);

struct Dn2CppGodotMethod
{
    const char* className;   // extension class registered in ClassDB
    const char* methodName;
    Dn2CppInvoke invoke;
    int32_t returnType;      // Dn2CppGdType
    int32_t argCount;
    int32_t argTypes[8];     // Dn2CppGdType
    int32_t isStatic;        // 1 if static, 0 if instance
};

struct Dn2CppGodotSignal
{
    const char* name;
    int32_t argCount;
    int32_t argTypes[8];
};

struct Dn2CppGodotProperty
{
    const char* propertyName;
    int32_t type;            // Dn2CppGdType
    const char* getterName;
    const char* setterName;  // nullptr if read-only
};

// One engine `_*` virtual a C# class overrides, bridged by reverse dispatch.
// `name` is the engine snake_case virtual name the engine asks for through
// get_virtual_call_data (e.g. "_gui_input"); `trampoline` (emitted per class by
// GodotBackend) decodes the ptrcall args into their managed form, calls the
// override, and encodes the return into r_ret.
struct Dn2CppGodotVirtual
{
    const char* name;
    void (*trampoline)(Dn2CppObject* self, const void* const* p_args, void* r_ret);
};

// One row per C# class deriving from the Godot.Node shim. Registered as an
// instantiable extension class. Every overridden engine `_*` virtual is bridged
// generically through the `virtuals` table (name-matched by the engine's
// get_virtual_call_data query). `notification` stays a dedicated slot: the
// engine delivers notifications through a separate GDExtension callback, not the
// virtual-call path.
struct Dn2CppGodotNodeClass
{
    const char* className;     // extension class name in ClassDB
    const char* parentClass;   // engine base class name (e.g. "Node", "Node2D")
    const Dn2CppTypeInfo* type;
    int32_t instanceSize;
    void (*ctor)(Dn2CppObject* self);            // parameterless ctor (may be null)
    void (*notification)(Dn2CppObject* self, int32_t what); // null = _Notification not overridden
    const Dn2CppGodotVirtual* virtuals;
    int32_t virtualCount;
    const Dn2CppGodotSignal* signals;
    int32_t signalCount;
    int32_t propertyCount;
    const Dn2CppGodotProperty* properties;
    int32_t isRefCounted;  // 1 = derives from RefCounted (always pinned; freed via
                            // free_instance_func). NodeCreateInstance does NOT call init_ref() —
                            // the caller's ClassDB::instantiate()-equivalent engine-side handling
                            // establishes the first reference (see NodeCreateInstance in
                            // dn2cpp_godot.cpp).
};

// Shim Variant payload kind tags (Dn2CppGodotVariant::kind below). MUST stay
// in lockstep with the C# side's single source, Godot.VariantKind
// (src/GodotSharpShim/VariantKind.cs) — the codec reads/writes the transpiled
// t_Godot_Variant's fields directly under the layout contract below, so the
// values must never be renumbered. The packed-array kinds map to the
// DN2CPP_GD_PACKED_* marshalling tags by subtracting
// DN2CPP_GDV_PACKED_TAG_OFFSET.
enum Dn2CppGodotVariantKind : int32_t
{
    DN2CPP_GDV_NIL = 0,
    DN2CPP_GDV_BOOL = 1,
    DN2CPP_GDV_INT = 2,
    DN2CPP_GDV_FLOAT = 3,
    DN2CPP_GDV_STRING = 4,
    DN2CPP_GDV_VECTOR3 = 5,
    DN2CPP_GDV_VECTOR2 = 6,
    DN2CPP_GDV_VECTOR4 = 7,
    DN2CPP_GDV_COLOR = 8,
    DN2CPP_GDV_OBJECT = 9,       // engine object handle in `i`
    DN2CPP_GDV_RID = 10,         // raw 8-byte id in `i`
    DN2CPP_GDV_STRING_NAME = 11, // text in `s` (the tag keeps the engine identity)
    DN2CPP_GDV_NODE_PATH = 12,
    // The big builtin PODs: a boxed shim struct in `obj`.
    DN2CPP_GDV_RECT2 = 13,
    DN2CPP_GDV_PLANE = 14,
    DN2CPP_GDV_AABB = 15,
    DN2CPP_GDV_QUATERNION = 16,
    DN2CPP_GDV_TRANSFORM2D = 17,
    DN2CPP_GDV_BASIS = 18,
    DN2CPP_GDV_TRANSFORM3D = 19,
    DN2CPP_GDV_PROJECTION = 20,
    // Packed arrays: the managed array in `obj`.
    DN2CPP_GDV_PACKED_BYTE = 21,
    DN2CPP_GDV_PACKED_INT32 = 22,
    DN2CPP_GDV_PACKED_INT64 = 23,
    DN2CPP_GDV_PACKED_FLOAT32 = 24,
    DN2CPP_GDV_PACKED_FLOAT64 = 25,
    DN2CPP_GDV_PACKED_STRING = 26,
    DN2CPP_GDV_PACKED_VECTOR2 = 27,
    DN2CPP_GDV_PACKED_VECTOR3 = 28,
    DN2CPP_GDV_PACKED_VECTOR4 = 29,
    DN2CPP_GDV_PACKED_COLOR = 30,
    // The engine-backed container wrappers in `obj`.
    DN2CPP_GDV_ARRAY = 31,
    DN2CPP_GDV_DICTIONARY = 32,
    // Boxed Callable/Signal shim structs in `obj`.
    DN2CPP_GDV_CALLABLE = 33,
    DN2CPP_GDV_SIGNAL = 34,
    // Not a payload kind of its own: the registration slot for the
    // Godot.Object shim used to wrap a decoded standard Callable's target /
    // Signal's owner.
    DN2CPP_GDV_OBJECT_SHIM_SLOT = 35,
};

constexpr int32_t DN2CPP_GDV_PACKED_TAG_OFFSET = 4; // DN2CPP_GDV_PACKED_* - DN2CPP_GD_PACKED_*

// Decoded form of a managed Godot.Variant (the GodotSharp shim's value type),
// filled by generated.cpp from t_Godot_Variant and consumed by the variadic
// print bridge / Variant codec (Decode/EncodeVariant). `kind`: 0=nil, 1=bool,
// 2=int, 3=float, 4=string, 5=Vector3, 6=Vector2, 7=Vector4, 8=Color, 9=Object
// (engine object handle in `i`), 10=RID (the raw 8-byte id in `i`),
// 11=StringName / 12=NodePath (both carried as their text in `s` — the shim has
// no dedicated string-family types, so identity is kept only as the kind tag),
// 13..20 = the large builtin PODs (Rect2, Plane, AABB, Quaternion, Transform2D,
// Basis, Transform3D, Projection — a boxed shim struct in `obj`, in the shim's
// flat-float field order, i.e. column-major for Basis/Transform3D), 21..30 =
// packed arrays (byte[], int[], long[], float[], double[], string[],
// Vector2[], Vector3[], Vector4[], Color[] — the managed array in `obj`; the
// matching DN2CPP_GD_PACKED_* tag is kind - 4), 31 = Godot.Collections.Array /
// 32 = Godot.Collections.Dictionary (the engine-backed wrapper class in `obj`
// — encode shares the wrapper's engine container value, decode wraps the
// carried container in a fresh finalizer-owned wrapper), 33 = Callable /
// 34 = Signal (a boxed shim struct in `obj`, laid out as
// Dn2CppGodotCallableShim / Dn2CppGodotSignalShim — one of our own
// delegate-backed callables decodes by recovering its custom-callable
// userdata, so the delegates survive the round trip). The field order
// mirrors the shim Variant's declaration order (C++ `f__kind..f__refGuard`)
// and MUST stay layout-identical to the transpiled t_Godot_Variant.
struct Dn2CppGodotVariant
{
    int32_t kind;
    int64_t i;
    double f;
    Dn2CppString* s;
    float vx;
    float vy;
    float vz;
    float vw;
    // kinds 13..30: the boxed big-POD shim struct or the managed array payload.
    Dn2CppObject* obj;
    // kind 9 only: a GC-visible guard object that owns one engine reference()
    // to the carried RefCounted, released by the guard's finalizer once the
    // managed Variant is unreachable. The source engine container (Variant /
    // Array / Dictionary) is destroyed right after decode, so without this the
    // handle in `i` would dangle whenever the container held the last
    // reference. Null for every other kind and for non-RefCounted Objects
    // (plain Node etc. stay borrowed — they are not reference-counted).
    // Must stay the LAST field (mirrored by the shim Variant).
    Dn2CppObject* refGuard;
};

// Decoded form of the managed Godot.Callable shim struct (all pointer-sized
// fields, declaration order) — MUST stay layout-identical to the transpiled
// t_Godot_Callable. `target`/`method` describe a standard engine callable
// (target is the Godot.Object-family shim whose leading field holds the engine
// handle); exactly one of the delegate fields declared below —
// `action`/`actionVariant`/`actionVariants`, one per supported delegate shape —
// is non-null for a delegate-backed callable, which is established on the
// managed side by `Godot.Callable.From`'s overloads each setting exactly one
// (built via callable_custom_create2 — the pinned
// userdata is a copy of this POD, which both keeps the delegates GC-reachable
// and lets a decode recover them intact).
struct Dn2CppGodotCallableShim
{
    Dn2CppObject* target;
    Dn2CppString* method;
    Dn2CppObject* action;         // Godot.CallableAction (zero-arg)
    Dn2CppObject* actionVariant;  // Godot.CallableVariantAction (one Variant)
    Dn2CppObject* actionVariants; // Godot.CallableVariantArgsAction (Variant[])
};

// Decoded form of the managed Godot.Signal shim struct — layout-identical to
// the transpiled t_Godot_Signal (owner + name, both pointers).
struct Dn2CppGodotSignalShim
{
    Dn2CppObject* owner;
    Dn2CppString* name;
};

// Registers the transpiled type-info a Variant payload kind decodes into: the
// boxed shim struct's ti for the big-POD kinds (13..20), the precise
// ti_arr_<elem> for the packed-array kinds (21..30), the container wrapper
// classes for kinds 31/32, the boxed Callable/Signal shim structs for kinds
// 33/34, and (slot 35, not a payload kind itself) the Godot.Object shim used
// to wrap a decoded standard Callable's target / Signal's owner. Called by
// generated.cpp's dn2cpp_godot_init_managed for each payload type the
// compilation actually emitted; DecodeVariant decodes an unregistered payload
// kind as nil (the program has no type to receive it anyway).
void dn2cpp_godot_variant_register_type(int32_t kind, const Dn2CppTypeInfo* ti);

// Registers the generated custom-callable dispatch bridge: the runtime's
// engine-side trampoline calls it to re-enter managed code when a
// delegate-backed Callable is invoked. `callableShim` is the pinned
// Dn2CppGodotCallableShim copy the callable carries; `variantArgs` is a
// managed Dn2CppArrayN of decoded shim Variants.
void dn2cpp_godot_set_callable_dispatch(void (*fn)(void* callableShim, void* variantArgs));

// Godot.GD.Print(params Variant[]): builds one engine Variant per argument and
// routes them through Godot's `print` utility (concatenated, no separator —
// matching Godot semantics); falls back to stdout before the engine is up.
void dn2cpp_godot_print_variants(const Dn2CppGodotVariant* args, int32_t n);

// General engine method-bind calls from transpiled C# (reverse direction).
// `cls`/`method`/`hash` identify the engine method; the bridge resolves the
// bind (cached) and ptrcalls it.
void    dn2cpp_godot_emit_signal(void* obj, Dn2CppString* signal_name, Dn2CppArrayRef* args);
void    dn2cpp_godot_call_ptrcall(void* obj, const char* cls, const char* method, int64_t hash, const void** args, void* r_ret);

// Reverse (engine -> managed) marshalling for the generic engine-virtual
// trampolines: the mirror of the forward ptrcall arg encoding / return
// readback, for the string-family / Variant / packed shapes that need runtime
// help (scalars, Vector2, PODs and borrowed-object args are decoded inline by
// the emitted trampoline). Each `decode` reads a value the engine still owns
// (borrowed — never destroyed here); each `encode` constructs into the
// engine-provided return slot (the engine owns and destroys it afterwards). A
// null managed string encodes as the empty engine value.
Dn2CppString* dn2cpp_godot_decode_str(const void* p);
// Wraps a borrowed engine object handle in a fresh shim of `ti` (null handle
// or type-info wraps as null) — the trampolines' input-object arg decode.
Dn2CppObject* dn2cpp_godot_wrap_borrowed_object(const Dn2CppTypeInfo* ti, void* handle);
Dn2CppString* dn2cpp_godot_decode_strname(const void* p);
Dn2CppString* dn2cpp_godot_decode_nodepath(const void* p);
void          dn2cpp_godot_encode_str(void* r_ret, Dn2CppString* s);
void          dn2cpp_godot_encode_strname(void* r_ret, Dn2CppString* s);
void          dn2cpp_godot_encode_nodepath(void* r_ret, Dn2CppString* s);
void          dn2cpp_godot_decode_variant(const void* p, Dn2CppGodotVariant* out);
void          dn2cpp_godot_encode_variant(void* r_ret, const Dn2CppGodotVariant* v);
void*         dn2cpp_godot_decode_packed(int32_t tag, const void* p);
void          dn2cpp_godot_encode_packed(int32_t tag, void* r_ret, void* arr);

// Sentinel instance pointer marking a static engine method-bind call. A static
// method has no receiver — the engine wants a null instance — but a null `obj`
// already means "instance call on a null managed receiver" (which the bridges
// no-op as a safety net). The generated static-call path passes this sentinel
// instead so the bridges can tell the two apart and fire the call with a null
// instance; a real engine handle can never collide with it.
#define DN2CPP_GODOT_STATIC_INSTANCE ((void*)(intptr_t)-1)

// Vararg engine method-bind call (the variant-call interface,
// object_method_bind_call — a vararg bind cannot be ptrcalled). The engine
// method's fixed arguments arrive pre-encoded as `fixedCount` runtime Variant
// PODs; `variantArgs` is the managed Variant[] tail (a Dn2CppArrayN of shim
// Variants, layout-identical to Dn2CppGodotVariant). The bridge builds one
// engine Variant array (fixed args then tail, each via the Dn2CppGodotVariant
// codec), calls the bind, and decodes the result Variant into `out` (nil on a
// call error or a void-returning method). `obj` may be the static sentinel.
void    dn2cpp_godot_call_vararg(void* obj, const char* cls, const char* method, int64_t hash,
    const Dn2CppGodotVariant* fixedArgs, int32_t fixedCount, void* variantArgs, Dn2CppGodotVariant* out);

// Shared construction helper: builds the engine_parent_class instance via
// gd_construct_object and plants it in the leading field (f__handle) of the
// already-allocated self (Dn2CppObject header + shim-type layout). When
// extension_class_name is non-null, binds self itself into the engine side via
// object_set_instance (the NodeCreateInstance case: a ClassDB-registered user
// class instantiated by the engine). When is_ref_counted is non-zero, calls
// RefCounted::init_ref() right after construction (via the existing generic
// ptrcall bridge). Shared by both ClassDB-driven construction
// (NodeCreateInstance) and C#-driven construction (GodotCallIntrinsics.TryEmitNewobj).
// Returns the constructed engine object pointer.
void* dn2cpp_godot_construct_engine_object(Dn2CppObject* self, const char* engine_parent_class,
    const char* extension_class_name, int32_t is_ref_counted);
// Thin, null-guarded wrapper over the GDExtension object_destroy interface function.
void  dn2cpp_godot_object_destroy(void* obj);
// The engine's own singleton Object for `name` (ProjectSettings, OS, Time, …),
// fetched through the GDExtension global_get_singleton interface function.
//
// The handle is BORROWED and the engine owns it for its whole lifetime: not one
// of the singletons is RefCounted, so there is nothing to reference() or
// unreference() and nothing to destroy — the managed wrapper the caller builds
// around it registers no finalizer.
//
// Cached on the pointer identity of the `name` string literal the generated code
// passes (the ResolveBind convention). A null result is cached too: a singleton
// can be absent from a build (an editor-only one in an exported game,
// JavaScriptBridge off-web, JavaClassWrapper off-Android), and that must degrade
// to a null receiver — reported once, not once per frame — rather than crash.
void* dn2cpp_godot_get_singleton(const char* name);
// Clears the engine→shim extension binding (object_set_instance(obj, cls, null))
// for a collectible C#-`new` RefCounted, using the shim's own type-info to name
// the concrete class. No-op if self or its engine handle is null.
void  dn2cpp_godot_object_clear_instance(Dn2CppObject* self);
// Keeps a C#-new registered RefCounted shim reachable (a GC root) across a
// collection while the engine still holds an independent reference beyond
// the shim's own baseline hold, so the object_set_instance binding is never
// detached just because C# dropped its own references. Called from
// RefCounted's finalizer, before it would otherwise release its own hold;
// returns 0 (caller falls through to the ordinary release/detach path) only
// when the table is momentarily full. A later sweep releases the anchor once
// the engine's own reference count drops back to the shim's baseline, at
// which point the next organic collection finalizes the object for real.
uint8_t dn2cpp_godot_try_anchor_refcounted(Dn2CppObject* self);
// Periodic sweep (piggybacked on the per-frame finalizer-drain call sites):
// releases anchors whose engine-side get_reference_count() has dropped back
// to 1 (the shim's own baseline hold, no other engine-side holder left).
void    dn2cpp_godot_sweep_anchored_refcounted();

// String-valued engine method-bind calls. A Godot `String` is a
// heap `has_destructor` value, so it cannot ride the plain ptrcall path that
// passes shim structs straight through. `_ret_str` ptrcalls the bind into an
// engine String, converts it to a managed Dn2CppString* and frees the engine
// String. `str_arg_new` builds an engine String from a managed string and
// returns a pointer usable as the native ptrcall arg; `str_arg_free` destroys it
// after the call. Only the plain `String` type — StringName/NodePath have
// different encodings and stay rejected at emit time.
Dn2CppString* dn2cpp_godot_call_ptrcall_ret_str(void* obj, const char* cls, const char* method, int64_t hash, const void** args);
void*   dn2cpp_godot_str_arg_new(Dn2CppString* s);
void    dn2cpp_godot_str_arg_free(void* gstr);

// StringName / NodePath: the other two string-family heap types.
// Both share the C# shim type (`string`) but have distinct engine encodings, so
// they are marshalled via a Godot String (constructed from / converted to the
// engine value). The `_new`/`_free` pair builds a pinned engine value for use as
// a native ptrcall arg and destroys it after the call.
Dn2CppString* dn2cpp_godot_call_ptrcall_ret_strname(void* obj, const char* cls, const char* method, int64_t hash, const void** args);
Dn2CppString* dn2cpp_godot_call_ptrcall_ret_nodepath(void* obj, const char* cls, const char* method, int64_t hash, const void** args);
void*   dn2cpp_godot_strname_arg_new(Dn2CppString* s);
void    dn2cpp_godot_strname_arg_free(void* p);
void*   dn2cpp_godot_nodepath_arg_new(Dn2CppString* s);
void    dn2cpp_godot_nodepath_arg_free(void* p);

// Packed-array engine method-bind calls. A Packed*Array is a heap
// `has_destructor` value marshalled to/from a managed C# array at the call
// boundary (the string family's ownership pattern; `tag` is a
// DN2CPP_GD_PACKED_* selecting the concrete packed kind). `_ret_packed`
// ptrcalls the bind into an engine packed value, copies its elements out into
// a fresh managed array (Dn2CppArrayI4/Ref/N by element kind — the caller
// stamps the precise array type-info) and destroys the engine value.
// `packed_arg_new` builds a pinned engine packed value from a managed array
// (null marshals as empty) and returns a pointer usable as the native ptrcall
// arg; `packed_arg_free` destroys it after the call.
void*   dn2cpp_godot_call_ptrcall_ret_packed(int32_t tag, void* obj, const char* cls, const char* method, int64_t hash, const void** args);
void*   dn2cpp_godot_packed_arg_new(int32_t tag, void* arr);
void    dn2cpp_godot_packed_arg_free(int32_t tag, void* p);

// Bare-Variant engine method-bind calls. A Variant argument is encoded from
// the decoded shim struct (`v`) into a pinned engine Variant whose address is
// the native ptrcall arg (`variant_arg_free` destroys it after the call — the
// string family's ownership pattern; constructing an engine Variant from an
// Object payload takes the engine's own reference for RefCounted, released on
// destroy). `_ret_variant` ptrcalls the bind into a caller-owned engine
// Variant, decodes it into `out` (DecodeVariant — a RefCounted Object payload
// gets its lifetime guard, see Dn2CppGodotVariant::refGuard) and destroys the
// engine value.
void*   dn2cpp_godot_variant_arg_new(const Dn2CppGodotVariant* v);
void    dn2cpp_godot_variant_arg_free(void* p);
void    dn2cpp_godot_call_ptrcall_ret_variant(void* obj, const char* cls, const char* method, int64_t hash, const void** args, Dn2CppGodotVariant* out);

// Callable/Signal engine method-bind calls. Both are 16-byte engine value
// types converted to/from the field-carrying shim structs at the boundary.
// The `_arg_new`/`_arg_free` pairs build a pinned engine value usable as a
// native ptrcall arg and destroy it after the call (the String ownership
// pattern): a standard Callable (target + method) and a Signal go through the
// builtin (Object, StringName) constructor; a delegate-backed Callable goes
// through callable_custom_create2, its userdata a pinned copy of the POD (a
// GC-scanned root keeping the delegates alive; freed by the callable's
// free_func when the engine drops the last reference). The `_ret_*` variants
// ptrcall into a caller-owned engine value, decode it into the POD (a custom
// callable of ours recovers the original userdata POD — delegates intact; a
// standard one decodes target/method via the get_object/get_method builtins,
// the target wrapped as a borrowed Godot.Object shim) and destroy it.
// `callable_call` invokes a callable built from the POD through the engine's
// own vararg `call` (via variant_call), decoding the result Variant into
// `out` — the Godot.Callable.Call surface.
void*   dn2cpp_godot_callable_arg_new(const Dn2CppGodotCallableShim* shim);
void    dn2cpp_godot_callable_arg_free(void* p);
void*   dn2cpp_godot_signal_arg_new(const Dn2CppGodotSignalShim* shim);
void    dn2cpp_godot_signal_arg_free(void* p);
void    dn2cpp_godot_call_ptrcall_ret_callable(void* obj, const char* cls, const char* method, int64_t hash, const void** args, Dn2CppGodotCallableShim* out);
void    dn2cpp_godot_call_ptrcall_ret_signal(void* obj, const char* cls, const char* method, int64_t hash, const void** args, Dn2CppGodotSignalShim* out);
void    dn2cpp_godot_callable_call(const Dn2CppGodotCallableShim* shim, void* variantArgs, Dn2CppGodotVariant* out);

// Engine container (Array/Dictionary) bridge, backing the engine-backed
// Godot.Collections.Array/Array<T>/Dictionary wrapper classes. A wrapper holds
// a pinned runtime-allocated slot containing the 8-byte engine container value
// in its leading handle field; `tag` is DN2CPP_GD_GODOT_ARRAY or
// DN2CPP_GD_DICTIONARY. Ownership: the wrapper owns its value (its finalizer
// calls `container_destroy`); an engine-call argument borrows the value
// (`_ret_container` transfers a fresh ptrcall-returned value to the caller).
// Element access rides the GDExtension array_operator_index /
// dictionary_operator_index plus the containers' own builtin methods
// (size/push_back/clear/has/keys); element values convert through the
// Dn2CppGodotVariant codec (Decode/EncodeVariant — a RefCounted Object element
// read through `array_get` gets the codec's lifetime guard).
void*   dn2cpp_godot_container_new(int32_t tag);           // fresh empty value (pinned slot)
void    dn2cpp_godot_container_destroy(int32_t tag, void* p);
int32_t dn2cpp_godot_container_size(int32_t tag, void* p);
void    dn2cpp_godot_container_clear(int32_t tag, void* p);
void*   dn2cpp_godot_call_ptrcall_ret_container(int32_t tag, void* obj, const char* cls, const char* method, int64_t hash, const void** args);
void    dn2cpp_godot_array_get(void* p, int64_t idx, Dn2CppGodotVariant* out);
void    dn2cpp_godot_array_set(void* p, int64_t idx, const Dn2CppGodotVariant* v);
void    dn2cpp_godot_array_add(void* p, const Dn2CppGodotVariant* v);
// Typed-array engine-object element read: extracts the element's object
// handle; a RefCounted element gets one reference() taken on the caller's
// behalf (the engine-object return convention — the managed wrapper's
// finalizer returns it). Non-RefCounted handles stay borrowed.
void*   dn2cpp_godot_array_get_object(void* p, int64_t idx);
void    dn2cpp_godot_dict_get(void* p, const Dn2CppGodotVariant* key, Dn2CppGodotVariant* out);
void    dn2cpp_godot_dict_set(void* p, const Dn2CppGodotVariant* key, const Dn2CppGodotVariant* v);
int32_t dn2cpp_godot_dict_has(void* p, const Dn2CppGodotVariant* key);
void*   dn2cpp_godot_dict_keys(void* p);   // keys() -> a fresh engine Array value the caller owns
void*   dn2cpp_godot_dict_values(void* p); // values(), same convention

// Builtin value-type method bridge: invoke a Godot builtin type's own method
// implementation (e.g. Color.srgb_to_linear) via variant_get_ptr_builtin_method.
// `gdType` is a Dn2CppGdType tag (mapped to the engine variant type); `base`,
// `args` and `r_ret` are pointers in Godot's native ptrcall layout (the generated
// caller passes the shim structs directly for the non-transpose value types).
void    dn2cpp_godot_builtin_call(int32_t gdType, const char* method, int64_t hash, const void* base, const void** args, void* r_ret, int32_t argc);

// String-returning variant: the engine method writes a Godot String (a heap
// `has_destructor` value) into r_ret, which the bridge converts to a managed
// Dn2CppString* and frees the engine String. Returns null when unresolved.
Dn2CppString* dn2cpp_godot_builtin_call_str(int32_t gdType, const char* method, int64_t hash, const void* base, const void** args, int32_t argc);

// Variant-returning variant: the engine method writes a Godot Variant into
// r_ret; the bridge decodes it into `out` (every payload kind the shim Variant
// models — see Dn2CppGodotVariant), destroying the engine Variant. Unmodelled
// payloads decode as nil.
void    dn2cpp_godot_builtin_call_variant(int32_t gdType, const char* method, int64_t hash, const void* base, const void** args, Dn2CppGodotVariant* out, int32_t argc);

// Provided by generated.cpp (transpiler --gdextension mode):
extern const Dn2CppGodotMethod dn2cpp_godot_methods[];
extern const int dn2cpp_godot_method_count;
extern const Dn2CppGodotNodeClass dn2cpp_godot_node_classes[];
extern const int dn2cpp_godot_node_class_count;
void dn2cpp_godot_init_managed();
