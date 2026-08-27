//
// dn2cpp minimal runtime — object model and intrinsics for transpiled IL.
//
// Runtime-INTERNAL core header: the whole object model + intrinsic surface,
// without the portable-SIMD vector surface. The vector ops live in
// dn2cpp_vectors.h, which under DN2CPP_USE_HIGHWAY=ON pulls ~100k preprocessed
// lines of hwy/highway.h — kept out of here so no runtime TU pays for it.

#pragma once

#include <cstdint>
#include <cstddef> // offsetof — the decimal layout assertions below
#include <cstdlib>
#include <cstring>
#include <cstdio>
#include <cmath>
#include <atomic> // std::atomic — the static-constructor guard cell (dn2cpp_cctor_run_once)
#include <exception>
#include <type_traits>
#include <limits>

// Marks a host-facing ABI hook resolved by name from outside the process image
// (dn2cpp_gdext_init as the .gdextension entry symbol; dn2cpp_runtime_quiesce
// and dn2cpp_set_native_callback_gc_registration via dlsym/GetProcAddress).
// The build is -fvisibility=hidden, so only this keeps a symbol in the export
// trie. On Windows the opposite applies: these hooks live in the
// dn2cpp_runtime static library, which WINDOWS_EXPORT_ALL_SYMBOLS on the DLL
// target does not reach, so they must be dllexport.
#if defined(_WIN32) && !defined(__CYGWIN__)
#define DN2CPP_RT_EXPORT __declspec(dllexport)
#else
#define DN2CPP_RT_EXPORT __attribute__((visibility("default")))
#endif

// Placement qualifier for a data-segment global that can hold a live pointer to
// a GC object. On the Emscripten/wasm GC build it corrals the global into the
// `dn2cpp_roots` section, which the runtime registers via GC_add_roots at init:
// bdwgc's EMSCRIPTEN arm reports an EMPTY static-data root range, so data-segment
// globals are otherwise invisible to the collector. Empty everywhere else —
// native Boehm scans the whole data segment — so it must stay behaviour-neutral.
#if defined(__EMSCRIPTEN__) && defined(DN2CPP_USE_BOEHM_GC)
#define DN2CPP_GC_STATIC_ROOT __attribute__((section("dn2cpp_roots")))
#else
#define DN2CPP_GC_STATIC_ROOT
#endif

// No-inline qualifier stamped on a [MethodImpl(MethodImplOptions.NoInlining)]
// method. Not merely a size hint: under the conservative (Boehm) GC a helper
// holding the last live reference to a soon-to-be-finalized object must be a
// real call frame, so that on return the pointer is gone from the caller's
// callee-saved registers instead of pinning the object. cl.exe silently ignores
// [[gnu::noinline]], hence the __declspec spelling (clang-cl defines __clang__
// and takes the attribute arm).
#if defined(_MSC_VER) && !defined(__clang__)
#define DN2CPP_NOINLINE __declspec(noinline)
#else
#define DN2CPP_NOINLINE [[gnu::noinline]]
#endif

// ---- MSVC-compat overflow-checked arithmetic ----
// The *.ovf lowering emits bare __builtin_{add,sub,mul}_overflow(T, T, T*) into
// generated code. cl.exe has no such builtins, so these same-named,
// same-signature stand-ins let every already-generated call site resolve by
// ordinary overload resolution with no codegen changes.
#if defined(_MSC_VER) && !defined(__clang__)
inline bool __builtin_add_overflow(uint32_t a, uint32_t b, uint32_t* res)
{ *res = a + b; return *res < a; }
inline bool __builtin_add_overflow(uint64_t a, uint64_t b, uint64_t* res)
{ *res = a + b; return *res < a; }
inline bool __builtin_sub_overflow(uint32_t a, uint32_t b, uint32_t* res)
{ *res = a - b; return a < b; }
inline bool __builtin_sub_overflow(uint64_t a, uint64_t b, uint64_t* res)
{ *res = a - b; return a < b; }
inline bool __builtin_mul_overflow(uint32_t a, uint32_t b, uint32_t* res)
{ uint64_t r = (uint64_t)a * (uint64_t)b; *res = (uint32_t)r; return r > 0xFFFFFFFFull; }
inline bool __builtin_mul_overflow(uint64_t a, uint64_t b, uint64_t* res)
{ *res = a * b; return a != 0 && *res / a != b; }

// Signed variants wrap in the unsigned domain and then test sign bits, avoiding
// signed-overflow UB: the sign bits of a, b and the result alone decide whether
// the infinite-precision result was out of range.
inline bool __builtin_add_overflow(int32_t a, int32_t b, int32_t* res)
{
    uint32_t ur = (uint32_t)a + (uint32_t)b;
    *res = (int32_t)ur;
    return ((a ^ (int32_t)ur) & (b ^ (int32_t)ur)) < 0;
}
inline bool __builtin_add_overflow(int64_t a, int64_t b, int64_t* res)
{
    uint64_t ur = (uint64_t)a + (uint64_t)b;
    *res = (int64_t)ur;
    return ((a ^ (int64_t)ur) & (b ^ (int64_t)ur)) < 0;
}
inline bool __builtin_sub_overflow(int32_t a, int32_t b, int32_t* res)
{
    uint32_t ur = (uint32_t)a - (uint32_t)b;
    *res = (int32_t)ur;
    return ((a ^ b) & (a ^ (int32_t)ur)) < 0;
}
inline bool __builtin_sub_overflow(int64_t a, int64_t b, int64_t* res)
{
    uint64_t ur = (uint64_t)a - (uint64_t)b;
    *res = (int64_t)ur;
    return ((a ^ b) & (a ^ (int64_t)ur)) < 0;
}
inline bool __builtin_mul_overflow(int32_t a, int32_t b, int32_t* res)
{
    int64_t r = (int64_t)a * (int64_t)b; // exact: int32 x int32 always fits in int64
    *res = (int32_t)r;
    return r < INT32_MIN || r > INT32_MAX;
}
inline bool __builtin_mul_overflow(int64_t a, int64_t b, int64_t* res)
{
    if (a == 0 || b == 0) { *res = 0; return false; }
    if ((a == -1 && b == INT64_MIN) || (b == -1 && a == INT64_MIN))
        return true; // the one product magnitude that itself overflows int64_t
    int64_t r = (int64_t)((uint64_t)a * (uint64_t)b); // wraps mod 2^64, well-defined
    *res = r;
    return r / a != b; // round-trip check; a==-1 already excluded above (no INT64_MIN/-1 divide)
}
#endif

// ---- MSVC-compat GCC/Clang builtins used directly by generated codegen ----
// Generated code also emits __builtin_alloca (localloc/stackalloc),
// __builtin_{clz,ctz,popcount}[ll] (BitOperations), __builtin_trap (unreachable
// sites) and __atomic_{load_n,store_n,thread_fence} (Volatile.Read/Write).
// cl.exe has none of them; these stand-ins keep every call site resolving
// unchanged, and the whole block compiles out on POSIX so generated source stays
// byte-identical. The *ZeroCount call sites already guard a zero operand.
#if defined(_MSC_VER) && !defined(__clang__)
#include <intrin.h> // _BitScanReverse/_BitScanForward (bsr/bsf, universal on x86-64)
#include <malloc.h> // _alloca

#define __builtin_alloca(n) _alloca(n)

[[noreturn]] inline void __builtin_trap() { std::abort(); }

inline int __builtin_clz(uint32_t x) { unsigned long i; _BitScanReverse(&i, x); return 31 - (int)i; }
inline int __builtin_clzll(uint64_t x) { unsigned long i; _BitScanReverse64(&i, x); return 63 - (int)i; }
inline int __builtin_ctz(uint32_t x) { unsigned long i; _BitScanForward(&i, x); return (int)i; }
inline int __builtin_ctzll(uint64_t x) { unsigned long i; _BitScanForward64(&i, x); return (int)i; }
// Software popcount (SWAR) — no hardware POPCNT dependency, unlike MSVC's __popcnt.
inline int __builtin_popcount(uint32_t x)
{
    x = x - ((x >> 1) & 0x55555555u);
    x = (x & 0x33333333u) + ((x >> 2) & 0x33333333u);
    x = (x + (x >> 4)) & 0x0F0F0F0Fu;
    return (int)((x * 0x01010101u) >> 24);
}
inline int __builtin_popcountll(uint64_t x)
{
    x = x - ((x >> 1) & 0x5555555555555555ull);
    x = (x & 0x3333333333333333ull) + ((x >> 2) & 0x3333333333333333ull);
    x = (x + (x >> 4)) & 0x0F0F0F0F0F0F0F0Full;
    return (int)((x * 0x0101010101010101ull) >> 56);
}

// An aligned 1/2/4/8-byte scalar or pointer load/store is atomic on x86-64;
// seq_cst ordering comes from full fences around the access. The memorder
// argument (always __ATOMIC_SEQ_CST from codegen) is accepted and ignored.
#ifndef __ATOMIC_SEQ_CST
#define __ATOMIC_SEQ_CST 5
#endif
template <typename T> inline T __atomic_load_n(T* p, int)
{
    std::atomic_thread_fence(std::memory_order_seq_cst);
    T v = *static_cast<volatile T*>(p);
    std::atomic_thread_fence(std::memory_order_seq_cst);
    return v;
}
template <typename T> inline void __atomic_store_n(T* p, T val, int)
{
    std::atomic_thread_fence(std::memory_order_seq_cst);
    *static_cast<volatile T*>(p) = val;
    std::atomic_thread_fence(std::memory_order_seq_cst);
}
inline void __atomic_thread_fence(int) { std::atomic_thread_fence(std::memory_order_seq_cst); }
#endif

// P/Invoke: bind a generated C++-side alias to a native C symbol's assembler
// name. The alias name (dn2cpp_pinvoke_<entry>) differs from the C symbol so the
// extern "C" declaration never collides with a system header's prototype for the
// same entry point. __USER_LABEL_PREFIX__ is `_` on Mach-O and empty on ELF.
// cl.exe has no GNU asm-label extension, so CppEmitter emits a different
// declaration shape there and never invokes this macro; it sits inert.
#define DN2CPP_PINVOKE_STR2(x) #x
#define DN2CPP_PINVOKE_STR(x) DN2CPP_PINVOKE_STR2(x)
#define DN2CPP_PINVOKE_ASM(sym) asm(DN2CPP_PINVOKE_STR(__USER_LABEL_PREFIX__) sym)

struct Dn2CppTypeInfo;
struct Dn2CppObject;
struct Dn2CppType;
struct Dn2CppString;
struct Dn2CppNumberFormatInfo;
struct Dn2CppFieldInfo;
struct Dn2CppMethodInfo;
struct Dn2CppParamInfo;
struct Dn2CppPropInfo;
struct Dn2CppAttrInfo;
struct Dn2CppThreadLocal;
struct Dn2CppBlockingCollection;

// One enum member: its name + the constant's value, widened to int64 so any
// underlying width fits. The per-enum table is pre-sorted by unsigned underlying
// magnitude at emit time (matching .NET's Enum.GetNames/GetValues ordering).
struct Dn2CppEnumMember { const char* name; int64_t value; };

// One row per implemented interface: pointer to the interface's type
// metadata plus the method slots implementing it (declaration order).
struct Dn2CppInterfaceEntry
{
    const Dn2CppTypeInfo* itf;
    const void** slots;
};

// Runtime type metadata (IL2CPP "klass" equivalent). One instance per
// managed type; boxed values and objects point at it from their header.
struct Dn2CppTypeInfo
{
    const char* name;             // namespace-qualified CLR name
    const Dn2CppTypeInfo* base;    // inheritance chain (nullptr at root)
    // A value type's UNBOXED payload size; a reference type's whole-object extent
    // (sizeof of the C++ struct instances are allocated as). A hand-written
    // type-info must state it too — the shallow clone and
    // RuntimeHelpers.GetUninitializedObject have no other source. 0 on a reference
    // type is the claim "no instance of this type-info exists" (System.Object,
    // whose instance IS the header; the abstract shells MemberInfo/MethodBase/
    // Enum/Void). Readers that size an ALLOCATION from it floor at the type's
    // prefix, since the opaque exception shells legitimately state 0.
    int32_t instanceSize;
    const void** vtable;          // virtual slots (nullptr if none)
    const Dn2CppInterfaceEntry* interfaces; // nullptr if none
    int32_t interfaceCount;
    // The reference type's overridden ToString, or null to format by default
    // (boxed primitive / type name). Lets Object.ToString dispatch the override
    // without depending on a vtable slot index. Existing initializers omit
    // this trailing member, so it value-initializes to null.
    Dn2CppString* (*tostring)(Dn2CppObject*);
    // The type's overridden GetHashCode / Equals(object), or null for the default
    // (identity hash / reference equality). Lets dn2cpp_object_gethashcode /
    // dn2cpp_object_equals dispatch the override so a record/class with value
    // equality works as a HashSet/Dictionary key. Same trailing-member
    // convention as `tostring`: older initializers omit them → null.
    int32_t (*gethashcode)(Dn2CppObject*);
    int32_t (*equals)(Dn2CppObject*, Dn2CppObject*);
    // Boolean Type properties that can't be derived from the other fields, packed
    // into bits so the dn2cpp_type_is_* helpers answer them for any type (typeof or
    // GetType()), not just static folds. Same trailing-member convention as
    // tostring/gethashcode/equals: initializers that omit it value-initialize to 0.
    int32_t flags;
    // Reflection field metadata: the type's declared fields, emitted by
    // CppEmitter and read by Type.GetFields/GetField. Same trailing-member 0-fill
    // convention — hand-written/enum type-infos that omit it report no fields.
    const Dn2CppFieldInfo* fields;
    int32_t fieldCount;
    // Reflection method metadata: the type's declared methods, emitted by
    // CppEmitter and read by Type.GetMethods/GetMethod. Same trailing-member 0-fill
    // convention — type-infos that omit it report no methods.
    const Dn2CppMethodInfo* methods;
    int32_t methodCount;
    // Reflection constructor metadata: the type's declared instance constructors
    // emitted by CppEmitter and read by Type.GetConstructors/GetConstructor.
    // Same trailing-member 0-fill convention. Constructors are never inherited, so this
    // table is not base-chain-walked.
    const Dn2CppMethodInfo* ctors;
    int32_t ctorCount;
    // Reflection property metadata: the type's declared properties, emitted by
    // CppEmitter and read by Type.GetProperties/GetProperty. Same trailing-member
    // 0-fill convention; base-chain-walked like fields/methods.
    const Dn2CppPropInfo* props;
    int32_t propCount;
    // Reflection custom-attribute metadata: the attributes applied to this type,
    // emitted by CppEmitter and read by Type.GetCustomAttributes/IsDefined. Named
    // customAttrs (not attrs) to avoid colliding with the accessibility `attrs` bitfield
    // on the member-info structs. Same trailing-member 0-fill convention.
    const Dn2CppAttrInfo* customAttrs;
    int32_t customAttrCount;
    // Generic reflection metadata. Same trailing-member 0-fill convention.
    // For a closed generic instantiation (e.g. List<int>): genericDef points at the
    // synthetic open-definition type-info (List`1), and genericArgs/genericArgCount
    // list the closed type arguments. For the open definition itself: genericDef
    // points at itself and it carries DN2CPP_TF_GENERICDEF (genericArgCount 0 — the
    // type parameters are not modeled as Types; genericParamNames below is display text
    // only). Non-generic types leave all three 0.
    const Dn2CppTypeInfo* genericDef;
    const Dn2CppTypeInfo* const* genericArgs;
    int32_t genericArgCount;
    // Enum reflection metadata. Same trailing-member 0-fill convention — only
    // the per-enum type-infos set these. enumUnderlying is the underlying primitive's
    // type-info (Type.GetEnumUnderlyingType); enumMembers is the (name, value) table
    // pre-sorted by unsigned underlying magnitude (matching Enum.GetNames ordering),
    // backing the non-generic Enum.GetNames/GetName/IsDefined/Parse(Type, …) bridge.
    const Dn2CppTypeInfo* enumUnderlying;
    const Dn2CppEnumMember* enumMembers;
    int32_t enumMemberCount;
    // Nested-type metadata. Same trailing-member 0-fill convention. The type's
    // *public* nested types that are emitted and non-generic (Type.GetNestedTypes() /
    // GetNestedType(name) — the default BindingFlags.Public set).
    const Dn2CppTypeInfo* const* nestedTypes;
    int32_t nestedCount;
    // Per-element array metadata. Same trailing 0-fill convention. Only the
    // per-element array type-infos CppEmitter emits (ti_arr_<T>, DN2CPP_TF_ARRAY) set
    // these: elementType is the SZArray element's type-info (backs Type.GetElementType),
    // arrayRank the rank (Type.GetArrayRank — 1 for an SZArray). The shared
    // dn2cpp_array_{ref,i4}_type handles and every non-array type-info leave them 0
    // (elementType null, arrayRank 0 — GetArrayRank then reports 1 for the shared array
    // handles, GetElementType null).
    const Dn2CppTypeInfo* elementType;
    int32_t arrayRank;
    // The simple name of the type's defining assembly (Type.Assembly identity),
    // emitted by CppEmitter from the type's module. Same trailing-member 0-fill
    // convention: hand-written type-infos omit it (null) — they are all CoreLib, so
    // dn2cpp_type_assembly_name treats null as "System.Private.CoreLib".
    const char* assemblyName;
    // The Finalize() override (a 0-arg, void-returning instance method with a
    // body) an instance of this type dispatches, or null when the type does not
    // override Object.Finalize. Wired at newobj time: dn2cpp_register_finalizer
    // is called on allocation only when this is non-null, so a program with no
    // finalizers never starts the finalizer thread. Same trailing-member 0-fill
    // convention as the fields above.
    void (*finalize)(Dn2CppObject*);
    // Runtime generic context (shared canonical generics): the per-instantiation
    // lookup table a shared generic body reads its instantiation-dependent
    // entries (type-infos, static-field addresses, cctor-ensure functions, …)
    // from. Emitted only for generic instantiations grouped under a canonical
    // owner; every other type-info leaves it null via the same trailing-member
    // 0-fill convention as the fields above.
    const void* const* rgctx;
    // The interned System.Type object for this type — typeof(X)/GetType() are a
    // lock-free load of this field. Statically-emitted type-infos bake a
    // data-segment companion in (const, .rodata); runtime-constructed ones
    // (hot-update patch/array types) stamp it at creation. Null falls back to
    // the mutex-interned slow path (same trailing-member 0-fill convention).
    const Dn2CppType* typeObject;
    // Formats a boxed instance of this type against an explicit format spec
    // (string.Format("{0:F2}", x), $"{dt:HH:mm}"), or null when the type has no
    // spec-aware formatter and a specified hole falls back to ToString(). Wired
    // where the type-info is defined, so the interpolation core never names a value
    // type's formatter and never pins its translation unit into a program that has
    // no dates in it. Same trailing-member 0-fill convention as the fields above.
    Dn2CppString* (*formatspec)(Dn2CppObject*, Dn2CppString*, const Dn2CppNumberFormatInfo*);
    // Raw ECMA TypeAttributes word (Type.Attributes and the IsPublic/IsVisible/
    // NotPublic family) + the type's metadata token (MemberInfo.MetadataToken).
    // CppEmitter stamps both on emitted user types/enums; hand-written and
    // synthetic (array/generic-def) type-infos leave them 0 via the trailing
    // 0-fill convention, and dn2cpp_type_il_attrs synthesizes a best-effort
    // word from the flags bits for those.
    uint32_t ilAttrs;
    int32_t metadataToken;
    // The member name the type's [DefaultMember] attribute declares ("Item" for a
    // type with an indexer), backing Type.GetDefaultMembers. Stamped by CppEmitter
    // from the metadata blob: DefaultMemberAttribute is a framework attribute and
    // therefore outside the reflected attribute tables (the IL2CPP-managed-
    // stripping bound), so the name rides the type-info instead. Null (0-fill
    // trailing convention) when the type carries no [DefaultMember].
    const char* defaultMemberName;
    // Generic variance, per type parameter, 2 bits each (parameter i at bits 2i):
    // DN2CPP_VAR_NONE / _OUT / _IN. Set on an open-definition type-info whose IL
    // declares an `in`/`out` parameter, and read by dn2cpp_itf_variant_match, which
    // decides each argument in the direction its parameter declares. 0 elsewhere,
    // which is exactly the "exact match only" the fast paths assume; the mask == 0
    // arm of the match still honours DN2CPP_TF_COVARIANT for older type-infos.
    int32_t varianceMask;
    // The type's MARSHALLED size — what Marshal.SizeOf answers — or 0 when the
    // marshalled-layout model has none. A different quantity from instanceSize, the
    // REPRESENTATION size: a one-bool struct marshals as 4 and represents as 1, a
    // one-char struct marshals as 1 and represents as 2, a [StructLayout(Sequential)]
    // CLASS marshals as its unmanaged extent while instanceSize counts the header.
    // Read ONLY by the size query in dn2cpp_marshal.cpp.
    //
    // 0 == "no answer" unambiguously: an empty struct marshals as 1 byte, so no type
    // has a marshalled size of 0 — which is what lets it ride the 0-fill convention.
    //
    // DO NOT read this to size a COPY: the bytes at a boxed value are instanceSize
    // bytes in the REPRESENTATION's layout, and this number describes a layout the
    // runtime cannot produce for a non-blittable type. The copy paths go through
    // dn2cpp_marshal_require_copyable, which never reads it.
    //
    // No type carries both this and the eventSource pair: the CLR loader refuses a
    // sequential/explicit-layout class over an auto-layout base, and EventSource is
    // auto-layout, so every provider is auto-layout and refused by the marshalled-
    // layout model's top-level auto gate.
    int32_t marshalSize;
    // System.Diagnostics.Tracing.EventSource provider identity, stamped on every
    // emitted class whose base chain reaches EventSource, null elsewhere.
    //
    // It rides the type-info for the same reason defaultMemberName does:
    // EventSourceAttribute is a FRAMEWORK attribute and so outside the reflected
    // attribute tables. Reading it off the receiver's DYNAMIC type is what makes a
    // two-level hierarchy (B : A : EventSource) answer with B's name; the base-ctor
    // call site that would be the alternative sees only A.
    //
    // eventSourceGuid is the canonical 36-character form of an explicit
    // [EventSource(Guid=…)], or null — then the guid is derived from the name by
    // dn2cpp_eventsource_guid. Neither field is base-chain-walked: .NET reads the
    // attribute with inherit:false.
    const char* eventSourceName;
    const char* eventSourceGuid;
    // A generic DEFINITION handle's declared type-parameter names, comma-joined
    // ("T", "TKey,TValue") — the bracket group Type.ToString() appends and FullName
    // does not. A display list, not a model of the parameters: dn2cpp materializes
    // no Type for a type parameter, so GetGenericArguments() on a definition stays
    // empty. Null on every other type-info, and on a definition minted from a name
    // alone (a cross-assembly typeof(Def<>) with no ClassInfo to read), which then
    // prints the bare FullName.
    const char* genericParamNames;
};

// Dn2CppTypeInfo::varianceMask, 2 bits per type parameter.
#define DN2CPP_VAR_NONE 0
#define DN2CPP_VAR_OUT  1   // covariant   (`out T`)
#define DN2CPP_VAR_IN   2   // contravariant (`in T`)
// Type parameters the mask can carry (int32 / 2 bits per parameter). A wider
// definition is simply not matched variantly.
#define DN2CPP_VARIANCE_MAX_PARAMS 16

// Wraps an existing positional Dn2CppTypeInfo initializer — which may stop at
// any trailing member — and sets typeObject without spelling the intervening
// zero-filled fields. Constant initialization: usable for const (.rodata) tis.
constexpr Dn2CppTypeInfo dn2cpp_ti_with_typeobject(Dn2CppTypeInfo ti, const Dn2CppType* ty)
{
    ti.typeObject = ty;
    return ti;
}

// The same, for the formatspec slot. Composes with the above:
//   dn2cpp_ti_with_formatspec(dn2cpp_ti_with_typeobject({…}, &obj), &wrapper)
constexpr Dn2CppTypeInfo dn2cpp_ti_with_formatspec(
    Dn2CppTypeInfo ti, Dn2CppString* (*fs)(Dn2CppObject*, Dn2CppString*, const Dn2CppNumberFormatInfo*))
{
    ti.formatspec = fs;
    return ti;
}

// The same, for a generic definition's parameter-name list. Wrapped rather than
// spelled positionally because the member is the struct's last and a gendef row
// stops at typeObject or varianceMask.
constexpr Dn2CppTypeInfo dn2cpp_ti_with_generic_params(Dn2CppTypeInfo ti, const char* names)
{
    ti.genericParamNames = names;
    return ti;
}

// Dn2CppTypeInfo::flags bits. The Godot/value-type intrinsics and most
// hand-written type-infos leave flags 0 (all false); CppEmitter sets them for the
// emitted user types, and the array type-infos carry DN2CPP_TF_ARRAY.
#define DN2CPP_TF_VALUETYPE 0x1
#define DN2CPP_TF_ENUM      0x2
#define DN2CPP_TF_INTERFACE 0x4
#define DN2CPP_TF_ABSTRACT  0x8
#define DN2CPP_TF_ARRAY     0x10
// System.Type.IsPrimitive: the 12 CLR primitives (Boolean/Char, the signed and
// unsigned 8/16/32/64-bit integers, Single/Double, IntPtr/UIntPtr). Decimal and
// enums are value types but NOT primitive, so they carry VALUETYPE without this
// bit. Set only on the hand-written primitive type-infos.
#define DN2CPP_TF_PRIMITIVE 0x20
// System.Type.IsGenericTypeDefinition: set only on the synthetic open-generic
// definition type-infos CppEmitter emits for GetGenericTypeDefinition()/
// MakeGenericType. Closed instantiations carry genericArgCount > 0 instead.
#define DN2CPP_TF_GENERICDEF 0x40
// Generic variance: set on a generic open-definition type-info whose single type
// parameter is covariant (`out` — IEnumerable<out T>, IReadOnlyList<out T>, …).
// Lets dn2cpp_isinst / dn2cpp_resolve_interface match a closed I<Derived> where
// I<Base> is requested. The COMPATIBILITY bit — a type-info built against the
// older layout (a hot-update base image) still matches covariantly through it;
// DN2CPP_TF_VARIANT + varianceMask are what the general paths gate on.
#define DN2CPP_TF_COVARIANT 0x80
// System.Type.IsSealed: cannot be derived from. In .NET every value type, enum,
// delegate, array, sealed class and static (abstract+sealed) class carries the
// metadata Sealed bit; object, interfaces and open/abstract classes do not.
#define DN2CPP_TF_SEALED 0x100
// System.Type.IsByRefLike: a ref struct (IsByRefLikeAttribute — Span<T>, ref structs).
// Set from ClassInfo.IsByRefLike on the emitted struct type-infos.
#define DN2CPP_TF_BYREFLIKE 0x200
// System.Type.IsNested: declared inside another type (metadata DeclaringType
// present). Set by CppEmitter from the type definition; the hand-written
// type-infos are all top-level and leave it clear.
#define DN2CPP_TF_NESTED 0x400
// Delegate types (MulticastDelegate-derived). Lets dn2cpp_object_gethashcode /
// dn2cpp_object_equals recognize a delegate instance and dispatch the
// chain-aware delegate hash/equality (delegates never wire the gethashcode/
// equals type-info slots — their BCL overrides are not transpiled).
#define DN2CPP_TF_DELEGATE 0x800
// Generic variance, generalized: set on an open-definition type-info whose IL declares
// ANY `in`/`out` type parameter, at any arity and in either direction — the pre-filter
// for reading `varianceMask`. A covariant single-parameter def carries both this and
// DN2CPP_TF_COVARIANT.
#define DN2CPP_TF_VARIANT 0x1000
// --trim-reflection removed this type's field, method and property tables. The bit
// is what makes the strip honest: from the tables alone a stripped type and a
// genuinely member-less one are indistinguishable, so a reflection read would
// otherwise answer an EMPTY member list. Every member-metadata entry point tests it
// and throws a catchable PlatformNotSupportedException naming the type and the
// remedy, and the base-chain walkers must test it AT EVERY LEVEL — a stripped base
// is how inherited members go missing unnoticed. Constructors are NOT stripped, so
// GetConstructor(s) / Activator.CreateInstance must not consult this bit; nor may
// anything outside reflection (name, base, interfaces, layout, vtable, assembly,
// Type object, enum members and the ToString/Equals/GetHashCode/Finalize slots).
#define DN2CPP_TF_METADATA_STRIPPED 0x2000
// Set on an enum type-info whose CLR definition carries [FlagsAttribute]. The
// emitter stamps it (the attribute is a custom attribute, invisible to the CLR
// TypeAttributes bits), and dn2cpp_enum_format reads it to decide whether the "G"
// format specifier decomposes into flag names ([Flags]) or reports a single member
// name / decimal value (a plain enum) — exactly .NET's Enum.Format branch.
#define DN2CPP_TF_FLAGS 0x4000
// Set on the abstract System.Array type-info alone. Every array derives from
// System.Array, but array type-infos carry base=nullptr, so a base-chain walk from
// an array never reaches it; dn2cpp_isinst recognizes the cast target through this
// bit instead, as it recognizes System.Object through dn2cpp_object_type.
#define DN2CPP_TF_SYSTEM_ARRAY 0x8000
// Set on the six NON-generic interface type-infos every array implements: the
// collection trio System.Collections.{IEnumerable,ICollection,IList} and the
// structural/clone trio System.{ICloneable,IStructuralComparable,IStructuralEquatable}.
// An array's interface-dispatch map is wired only when an array of its element is
// actually USED as a collection, so a bare `(array) is IList` on an un-enumerated
// array would meet an empty interface table and answer False. dn2cpp_isinst matches
// an array source against a target carrying this bit directly, independent of the
// map: every array implements all six. The flag governs the TYPE TEST only; the map
// still governs dispatch.
#define DN2CPP_TF_ARRAY_ITF 0x10000
// Set on the GENERIC-definition type-info of the five collection interfaces an
// SZArray implements over its element: System.Collections.Generic.{IEnumerable`1,
// ICollection`1,IList`1,IReadOnlyList`1,IReadOnlyCollection`1}. An SZArray of E
// implements I<T> when E is array-element-compatible with T (dn2cpp_array_elem_covariant:
// identity, reference covariance, or the CLR primitive/enum equivalence — int[] is
// IList<uint> True, is IList<long> False; string[] is IEnumerable<object> True, int[]
// is not). A MULTIDIM array implements NONE of these (int[2,2] is IList<int> False), so
// the isinst arm gates on rank == 1. Read off the closed instantiation's genericDef, so
// one stamp per definition covers every close. Like DN2CPP_TF_ARRAY_ITF, this governs
// the type test only; dispatch stays with the lazy map.
#define DN2CPP_TF_ARRAY_GEN_ITF 0x20000
// Set on the SZArrayEnumerable<object> wrapper's type-info alone — the class the
// shared reference-element array dispatch table forwards into. Reference elements
// share one C++ layout, so this wrapper's closed-generic interface rows are
// element-erased: dn2cpp_resolve_interface_walk lets them service a request for ANY
// all-reference-argument instantiation of the same definition. DISPATCH only; the
// isinst/castclass walks never read it, so type tests keep the exact/variant rules.
// Never stamp it on a type whose members read a type argument's layout — a
// value-typed argument is exactly what the erased rows cannot serve, and the walk
// arm rejects one for that reason.
#define DN2CPP_TF_REF_ERASED_ITF 0x40000
// The emitter could not model this value type's CLR layout extent, so its
// instanceSize is meaningless. Set only on an OPAQUE value-type shell — a type
// reached by a type token alone, with no emitted field layout — whose extent model
// returned "unknown". A shell WITH a model is padded to it so sizeof() tells the
// truth; this bit covers the residue where there is nothing to pad to. Same
// argument as DN2CPP_TF_METADATA_STRIPPED: from the number alone an unpadded shell
// (sizeof 1) is indistinguishable from a genuinely field-less struct, so the
// alternative to the bit is Unsafe.SizeOf<T>() answering 1 where .NET lays out 16.
// Every reader that turns instanceSize into a number or a stride MANAGED code
// observes calls dn2cpp_require_layout, which throws a catchable
// PlatformNotSupportedException. The pure allocation readers (exception/box/clone
// sizing) must NOT consult it: an opaque shell has no fields for emitted code to
// touch, so allocating exactly the representation is correct.
#define DN2CPP_TF_LAYOUT_UNKNOWN 0x80000
// The two marshalling verdicts. Two bits and not one because they carry different
// exceptions, and merging them would make one of the two a lie:
//   - NOT_MARSHALABLE is what .NET ITSELF refuses — an auto-layout value type
//     (DateTime, DateTimeOffset, every enum) or one holding a field with no
//     unmanaged form. Raises .NET's own ArgumentException with .NET's message.
//   - MARSHAL_INEXACT is a type .NET ANSWERS for and dn2cpp cannot: a bool field is
//     4 bytes unmanaged and 1 here, a char 1 unmanaged and 2 here, a [MarshalAs]
//     descriptor rewrites the form wholesale. Raises the catchable
//     PlatformNotSupportedException — a DECLARED divergence, not a claim about .NET.
// Stamped on value types only: a reference type is refused by the absence of
// DN2CPP_TF_VALUETYPE, a closed generic by its genericArgCount, and a primitive is
// answered from a fixed table — all three asked ahead of these bits.
// dn2cpp_marshal_require_size is the single point every reader goes through.
#define DN2CPP_TF_NOT_MARSHALABLE 0x100000
#define DN2CPP_TF_MARSHAL_INEXACT 0x200000
// A shared-generics CANONICAL type-info: an instantiation over the emitter's
// placeholder arguments ($CnRef, $CnInt32, …), minted so one shared body can
// dispatch for every member of its sharing group. Not a CLR type — no managed
// program can name it, no instance carries it — and the only place it is reachable
// from managed code is an interface table, where each real closed-generic row is
// followed by an ALIAS row carrying the canonical handle over the same slots.
//
// Those alias rows are load-bearing for DISPATCH and for the type tests a shared
// body performs against a canonical handle, so no walk in dn2cpp_casts.cpp may skip
// them. What must skip them is the opposite reader: the ones handing the row's type
// OUTWARD as a managed Type — Type.GetInterfaces() / FindInterfaces() — where the
// alias is a row .NET does not have and a consumer would see types that do not exist.
#define DN2CPP_TF_SHARED_CANON 0x400000
// This reference type's C++ representation may not be copied BITWISE, so
// Object.MemberwiseClone refuses it. Stamped on the hand-written runtime structs
// that live on the NATIVE heap, or that embed rather than point at state whose
// ownership is singular: a std::mutex / std::condition_variable (copying one is UB,
// and two independent locks are not the one shared lock a shallow copy gives you),
// a std::thread the copy would become a second joiner of, or a process-wide
// registry slot the copy would decrement twice.
//
// A BIT rather than the absence of an instanceSize, because those are different
// facts: extent 0 means "nobody stated this type's extent", this means "the extent
// is known and copying it anyway is wrong". Both throw, with different messages.
//
// Real .NET clones all of these, so this is a DECLARED dn2cpp divergence. Closing
// it needs a per-type clone hook that rebuilds the native half — not an extent
// stamp, and not a freshly constructed primitive: .NET's shallow copy SHARES the
// original's internal lock object.
#define DN2CPP_TF_NO_SHALLOW_CLONE 0x800000
// A runtime-instantiation TEMPLATE: a canonical instantiation (SHARED_CANON is
// also set) whose metadata IS emitted — vtable, member tables, interface rows —
// because dn2cpp_type_make_generic clones it for an instantiation the AOT image
// lacks (see Dn2CppRuntimeTemplate). Still not a CLR type: the clone strips both
// bits, stamps the real argument vector and fills the rgctx table the shared
// bodies read through the receiver's type-info.
#define DN2CPP_TF_RUNTIME_TEMPLATE 0x1000000
// A runtime-SYNTHESIZED instantiation: a clone dn2cpp_type_make_generic minted
// from a template row. Its base chain interns onto the AOT type-info wherever
// the image carries that instantiation (pointer-comparing walks demand the one
// type-info), so the chain cannot anchor the clone's shared bodies: an AOT base
// is monomorphic (rgctx null) or a shared world whose slot order is its own.
// dn2cpp_rgctx routes a flagged receiver to the clone's per-level tables.
#define DN2CPP_TF_RUNTIME_SYNTH 0x2000000
// The runtime-minted synthetic type of an EqualityComparer<T>.Default singleton
// (dn2cpp_default_equality_comparer). A BIT rather than a registry lookup because
// the non-generic comparer dispatch asks "is this the default comparer?" once per
// element; nothing but that mint stamps it.
#define DN2CPP_TF_DEFAULT_EQ_COMPARER 0x4000000

// The clone-owned rgctx anchor lookup behind DN2CPP_TF_RUNTIME_SYNTH
// (dn2cpp_system_reflection.cpp); falls back to the base-chain walk for levels
// below the placeholder chain.
const void* const* dn2cpp_rgctx_synth(const Dn2CppTypeInfo* t, const Dn2CppTypeInfo* genericDef);

// The runtime-generic-context table of the base-chain level whose generic
// definition matches `genericDef` — a shared canonical body derives its context
// from the receiver's dynamic type at the DECLARING class's level (the receiver
// may be a derived type whose own rgctx belongs to a different definition).
// Small and inlinable: shared-body prologues run it once per call.
static inline const void* const* dn2cpp_rgctx(const Dn2CppTypeInfo* t, const Dn2CppTypeInfo* genericDef)
{
    if (t == nullptr)
        return nullptr;
    // Receiver's own level: one compare — a synthesized clone's own table sits
    // in its type-info too, so only BASE levels take the side lookup.
    if (t->genericDef == genericDef)
        return t->rgctx;
    if ((t->flags & DN2CPP_TF_RUNTIME_SYNTH) != 0)
        return dn2cpp_rgctx_synth(t, genericDef);
    for (t = t->base; t != nullptr; t = t->base)
        if (t->genericDef == genericDef)
            return t->rgctx;
    return nullptr;
}

// Every managed object starts with a type pointer (dispatch goes
// header -> type -> vtable; no C++ virtual functions in managed layouts).
struct Dn2CppObject
{
    const Dn2CppTypeInfo* type;
};

// A boxed value type's payload sits immediately after the object header. Used by
// the tostring/gethashcode/equals/formatspec slot wrappers a value type wires on
// its own type-info.
template <typename T>
inline const T* dn2cpp_boxed(const Dn2CppObject* o)
{
    return reinterpret_cast<const T*>(o + 1);
}

// .NET string: UTF-16 code units. `length` is the code-unit count (matching
// `System.String.Length`). `chars` is NUL-terminated for convenience but the
// terminator is not counted in `length`.
struct Dn2CppString : Dn2CppObject
{
    int32_t length;
    const char16_t* chars;
};

// The trace captured when an exception was thrown: a tag plus the entries,
// innermost first, allocated at the exact needed size in pointer-free
// (atomic) GC memory — the entries are code addresses or rodata string
// literals, neither of which the collector must scan — and kept alive by the
// exception object's `trace` reference. Two kinds:
//   kind 0 — per-frame function ENTRY addresses (void*) from the PAL
//     backtrace (its contract; a frame whose entry the platform could not
//     derive is null). Resolution to names — an exact match against the
//     method table, never a nearest-below guess — happens lazily at render
//     time (Exception.StackTrace / the unhandled report), never at throw.
//   kind 1 — shadow-stack frame names (const char*, the emitter-stamped
//     rodata literals recorded by Dn2CppShadowFrame guards). Already names;
//     nothing to resolve at render. `dropped` counts the innermost frames
//     the shadow stack's capacity could not store at capture — recorded so
//     the render can say the trace is truncated instead of silently starting
//     mid-stack.
struct Dn2CppExcTrace
{
    int32_t kind;     // 0 = function entries (void*), 1 = shadow-stack frame names (const char*)
    int32_t dropped;  // kind 1: innermost frames lost to shadow-stack capacity at capture
    int32_t count;
    void* entries[1]; // trailing array: dn2cpp_alloc_atomic'd at count entries, innermost first
};

// A message-carrying managed exception object: the object header plus message,
// inner, HResult (seeded to COR_E_EXCEPTION, overwritten by each derived ctor's
// set_HResult) and the trace captured at throw (null until then, so an un-thrown
// exception's StackTrace is null as in real .NET). Every user-defined derived
// exception struct inherits this prefix, so its own fields sit past `trace`.
// INVARIANT: growing this prefix is an ABI change — bump
// AbiContract.LayoutPolicyVersion so a hot-update BPI built against the old layout
// is rejected at load.
struct Dn2CppExceptionObject : Dn2CppObject
{
    Dn2CppString* message;
    Dn2CppObject* inner;
    int32_t hresult;
    const Dn2CppExcTrace* trace;
};

// The capacity of the modeled NumberGroupSizes array (see the field below).
// Four, because every culture a host can be set to is [3] or [3,2]; a longer
// array is refused loudly rather than truncated, since truncating one would
// punctuate a large number in the wrong places.
#define DN2CPP_MAX_GROUP_SIZES 4

// Culture-aware numeric formatting data. Models the subset of
// System.Globalization.NumberFormatInfo that drives number/currency/percent
// rendering: separators, signs, the NaN/Infinity symbols, and the .NET
// currency/percent positive/negative *pattern indices* (laid out by
// dn2cpp_lay_pattern). Both the IFormatProvider-bearing CultureInfo and
// NumberFormatInfo managed types lower to a `const Dn2CppNumberFormatInfo*`.
struct Dn2CppNumberFormatInfo
{
    Dn2CppString* numberDecimal;     // NumberDecimalSeparator
    Dn2CppString* numberGroup;       // NumberGroupSeparator
    // NumberGroupSizes — and CurrencyGroupSizes and PercentGroupSizes, which share
    // this one array. INVARIANT: a culture's three are equal (the generator refuses
    // a candidate whose are not), so the property setters validate-and-trap rather
    // than write; a write to one would silently move the other two.
    //
    // Semantics: the sizes are consumed RIGHT to LEFT — groupSizes[0] digits, then
    // [1], and the LAST element repeats for everything further left. A trailing 0
    // means STOP, not "repeat nothing": [3,0] renders 123456789012345 as
    // 123456789012,345 where [3] renders 123,456,789,012,345. 0 is legal only as the
    // last element, and groupSizeCount == 0 means no grouping at all.
    int8_t groupSizes[DN2CPP_MAX_GROUP_SIZES];
    int8_t groupSizeCount;
    Dn2CppString* negativeSign;      // NegativeSign
    Dn2CppString* nan;               // NaNSymbol
    Dn2CppString* posInf;            // PositiveInfinitySymbol
    Dn2CppString* negInf;            // NegativeInfinitySymbol
    Dn2CppString* percentSymbol;     // PercentSymbol
    Dn2CppString* percentDecimal;    // PercentDecimalSeparator
    Dn2CppString* percentGroup;      // PercentGroupSeparator
    int32_t percentPosPattern;       // PercentPositivePattern (0..3)
    int32_t percentNegPattern;       // PercentNegativePattern (0..11)
    Dn2CppString* currencySymbol;    // CurrencySymbol
    Dn2CppString* currencyDecimal;   // CurrencyDecimalSeparator
    Dn2CppString* currencyGroup;     // CurrencyGroupSeparator
    int32_t currencyDigits;          // CurrencyDecimalDigits
    int32_t currencyPosPattern;      // CurrencyPositivePattern (0..3)
    int32_t currencyNegPattern;      // CurrencyNegativePattern (0..15)
    Dn2CppString* cultureName;       // CultureInfo.Name ("" for invariant / plain NumberFormatInfo)
    // CultureInfo.LCID. 127 (0x7F) for the invariant culture; the real Windows LCID
    // for a culture the table carries; 4096 (LOCALE_CUSTOM_UNSPECIFIED) for one it
    // does not — which is also what real .NET answers for a culture it can
    // materialize but has no LCID for, so the two cases agree honestly.
    int32_t lcid;
    // CultureInfo.IsNeutralCulture — 1 for a language-only culture ("en", "az-Latn"),
    // 0 for a specific one and for the invariant culture (.NET reports the invariant
    // culture as SPECIFIC, which is why the zero default is also the right one).
    int32_t isNeutralCulture;
    // 1 when this instance was minted through the NumberFormatInfo surface
    // (`new NumberFormatInfo()` / NumberFormatInfo.InvariantInfo), 0 for a
    // culture. Read by dn2cpp_nfi_wrap to recover the managed identity of a
    // value whose static type was erased to IFormatProvider before it escaped
    // into an `object` context. (Trailing member: zero-init covers cultures.)
    int32_t isNfi;
};

// A CultureInfo / NumberFormatInfo / TextInfo escaped into an `object` context.
// The intrinsic representation of those managed types is the HEADERLESS
// `const Dn2CppNumberFormatInfo*` above, so the escape must allocate a real
// managed object around the pointer — an object-generic consumer (ToString
// dispatch, GetType, equality, object[]) would otherwise misread the struct's
// first field as the type header and jump through literal-pool data.
// Wrappers are interned per (pointer, type identity), so reference equality
// across two escapes of the same culture behaves like .NET's.
struct Dn2CppNfiBox : Dn2CppObject
{
    const Dn2CppNumberFormatInfo* nfi;
};

// Shared runtime type headers for the wrapped forms (the same pattern as
// dn2cpp_stacktrace_type: typeof/isinst/castclass on these classes name the
// SAME symbols the wrapper carries, so GetType()==typeof(CultureInfo) holds).
extern const Dn2CppTypeInfo dn2cpp_cultureinfo_type;
extern const Dn2CppTypeInfo dn2cpp_numberformatinfo_type;
extern const Dn2CppTypeInfo dn2cpp_textinfo_type;

// DN2CPP_NFI_KIND_*: the ESCAPE SITE's static managed type, deciding the
// wrapper's identity. PROVIDER (IFormatProvider, or an unknown static type)
// resolves at run time through Dn2CppNumberFormatInfo::isNfi.
#define DN2CPP_NFI_KIND_CULTURE  0
#define DN2CPP_NFI_KIND_NFI      1
#define DN2CPP_NFI_KIND_TEXTINFO 2
#define DN2CPP_NFI_KIND_PROVIDER 3

// object <- culture escape: interned wrapper allocation (null passes through;
// a value that is already a wrapper passes through unchanged).
Dn2CppObject* dn2cpp_nfi_wrap(const Dn2CppNumberFormatInfo* n, int32_t kind);
// culture <- object: total and tolerant — null stays null, a wrapper yields its
// inner pointer, anything else is returned as-is (a raw pointer that flowed
// through an erased context; the probe is a value compare, never a dereference
// through the misread header).
const Dn2CppNumberFormatInfo* dn2cpp_nfi_unwrap(Dn2CppObject* o);
// castclass/isinst with an NFI-mapped target type: a wrapper is matched by
// kind (PROVIDER accepts CultureInfo and NumberFormatInfo, as the interface
// does) and unwrapped; a non-wrapper object falls back to the generic
// dn2cpp_castclass/dn2cpp_isinst against `fallbackTi` (the target's emitted
// type-info), preserving the pre-wrapper behavior for real objects.
const Dn2CppNumberFormatInfo* dn2cpp_nfi_castclass(Dn2CppObject* o, int32_t kind,
                                                   const Dn2CppTypeInfo* fallbackTi);
const Dn2CppNumberFormatInfo* dn2cpp_nfi_isinst(Dn2CppObject* o, int32_t kind,
                                                const Dn2CppTypeInfo* fallbackTi);

// The remaining headerless representation — SearchValues<T>, the raw membership
// set — escaping into an `object` context. It has no managed identity to wrap
// into (the identity .NET reports is a private per-shape subclass such as
// RangeCharSearchValues<T>), so this throws a catchable
// PlatformNotSupportedException naming the type instead of punning the handle
// (dn2cpp_casts.cpp). Declared to return Dn2CppObject* so an escape site can use
// it as an expression; it never returns.
Dn2CppObject* dn2cpp_headerless_escape(const char* managedTypeName);

// ---- Assembly/Module escaped into `object` ----------------------------------
// System.Reflection.Assembly and Module are modeled as the defining assembly's
// simple-name `const char*`, headerless like the NFI trio, so an escape into an
// `object` context allocates a real managed object around the handle. Unlike the
// NFI wrappers the box's header type-info is the PRIVATE implementation identity
// real .NET reports (RuntimeAssembly/RuntimeModule over the public abstract
// shell), so GetType().Name, GetType() == typeof(Assembly) (false) and
// `o is Assembly` (true, via the base chain) all answer like .NET. Wrappers intern
// per (assembly NAME, kind) by STRING compare, not pointer — two intrinsics can
// hand out two different `const char*`s for one assembly — so reference equality
// across escapes matches .NET's singleton Assembly. The box struct is private to
// dn2cpp_system_reflection.cpp; unwrap/box detection is an intern-chain membership
// test, never a header read (a raw `const char*` must not be dereferenced as one).

// The public abstract shells: never an instance's header word (instanceSize 0),
// carried so typeof(Assembly)/typeof(Module) and isinst/castclass name a real
// identity the box's base chain reaches.
extern const Dn2CppTypeInfo dn2cpp_assembly_type;
extern const Dn2CppTypeInfo dn2cpp_module_type;
// The box header identities (base = the shell above).
extern const Dn2CppTypeInfo dn2cpp_runtime_assembly_type;
extern const Dn2CppTypeInfo dn2cpp_runtime_module_type;

// DN2CPP_ASM_KIND_*: the escape site's static managed type, deciding the
// wrapper's identity. Assembly is the default when the static type was lost —
// the same convention as the Object::ToString arm on a `const char*` receiver
// (Assembly handles are the overwhelmingly common case).
#define DN2CPP_ASM_KIND_ASSEMBLY 0
#define DN2CPP_ASM_KIND_MODULE   1

// object <- Assembly/Module escape: interned wrapper allocation (null passes
// through; a value that is already a wrapper passes through unchanged).
Dn2CppObject* dn2cpp_asm_wrap(const char* h, int32_t kind);
// Assembly/Module <- object: total and tolerant — null stays null, a wrapper
// yields its inner handle, anything else is returned as-is (a raw handle that
// flowed through an erased context; the probe is intern-chain membership, never
// a dereference).
const char* dn2cpp_asm_unwrap(Dn2CppObject* o);
// castclass/isinst with an Assembly/Module target type: a wrapper is matched by
// kind and unwrapped; anything else falls back to the generic
// dn2cpp_castclass/dn2cpp_isinst against `fallbackTi` (the target's type-info),
// preserving the proper InvalidCastException/null for real objects.
const char* dn2cpp_asm_castclass(Dn2CppObject* o, int32_t kind,
                                 const Dn2CppTypeInfo* fallbackTi);
const char* dn2cpp_asm_isinst(Dn2CppObject* o, int32_t kind,
                              const Dn2CppTypeInfo* fallbackTi);

struct Dn2CppType : Dn2CppObject
{
    const Dn2CppTypeInfo* typeInfo;
};

// Reflection field metadata. One entry per declared field in a type's
// Dn2CppTypeInfo::fields table. getter/setter are CppEmitter-generated thunks that
// do the typed access + box/unbox; null when the field has no reflectable
// storage (a literal/const, or an opaque declaring type), in which case
// GetValue/SetValue raise InvalidOperationException.
struct Dn2CppFieldInfo
{
    const char* name;
    const Dn2CppTypeInfo* declaringType;
    const Dn2CppTypeInfo* fieldType;
    int32_t attrs;                       // DN2CPP_FLDA_* bits
    Dn2CppObject* (*getter)(Dn2CppObject* obj);
    void (*setter)(Dn2CppObject* obj, Dn2CppObject* value);
    // Custom attributes applied to this field; 0-fill trailing convention.
    const Dn2CppAttrInfo* customAttrs;
    int32_t customAttrCount;
    // Raw ECMA FieldAttributes word (FieldInfo.Attributes/IsSpecialName) + the
    // field's metadata token. 0-fill trailing convention (0 when unrecorded).
    int32_t ilAttrs;
    int32_t metadataToken;
    // The constant of a LITERAL row that has no storage to thunk-read, widened
    // to int64 (an enum member's value — the rows CppEmitter emits for enum
    // type-infos). getter/setter stay null; dn2cpp_fieldref_get_value boxes
    // this as the declaring enum instead of throwing, so GetField(name)
    // .GetValue(null) answers like real .NET (the Newtonsoft EnumUtils path).
    // Same trailing 0-fill convention — non-enum rows leave it 0 and keep the
    // null-thunk InvalidOperationException behavior.
    int64_t literalValue;
};

// Dn2CppFieldInfo::attrs bits. PUBLIC/PRIVATE mirror the CLR field
// accessibility (internal/protected set neither bit); STATIC marks a static
// field; INITONLY a C# readonly field (FieldAttributes.InitOnly); LITERAL a
// const (FieldAttributes.Literal — no storage, GetValue/SetValue thunks null).
#define DN2CPP_FLDA_STATIC   0x1
#define DN2CPP_FLDA_PUBLIC   0x2
#define DN2CPP_FLDA_PRIVATE  0x4
#define DN2CPP_FLDA_INITONLY 0x8
#define DN2CPP_FLDA_LITERAL  0x10

// A reflected field handle: a managed object wrapping a Dn2CppFieldInfo* table
// entry. Backs System.Reflection.FieldInfo (its header type is
// dn2cpp_fieldinfo_type, distinct from a Type's dn2cpp_type_type).
//
// reflectedType (here and on Dn2CppMethodRef/Dn2CppPropRef) models
// MemberInfo.ReflectedType: the type the member was OBTAINED THROUGH, which for
// an inherited member differs from declaringType — .NET's
// `typeof(D).GetMethod(m) == typeof(Base).GetMethod(m)` is false for that
// reason, and handles intern per (row, reflectedType) to match. Normalized at
// mint time: a Type-query mint stamps the queried type, every other mint passes
// null and the minter substitutes the row's declaringType — never null after
// minting, so equality is a plain pointer compare. That normalization is .NET's
// own observable behaviour: its member cache is keyed (declaring, reflected), and
// a non-query mint (delegate.Method, GetBaseDefinition,
// GetGenericMethodDefinition) hands back the same instance as the declaring-type
// query.
struct Dn2CppFieldRef : Dn2CppObject
{
    const Dn2CppFieldInfo* field;
    const Dn2CppTypeInfo* reflectedType;
};

// Reflection parameter metadata. One entry per parameter in a method's
// Dn2CppMethodInfo::parameters table. Position is the array index; name is the
// source parameter name (or null when the metadata carries none).
struct Dn2CppParamInfo
{
    const Dn2CppTypeInfo* paramType;
    const char* name;
    // Custom attributes applied to this parameter; 0-fill trailing convention.
    const Dn2CppAttrInfo* customAttrs;
    int32_t customAttrCount;
    // Raw ECMA ParameterAttributes word (ParameterInfo.Attributes/IsOptional).
    // 0-fill trailing convention (0 == ParameterAttributes.None when unrecorded,
    // which is also the correct answer for a parameter with no Param row).
    int32_t ilAttrs;
    // Signature custom modifiers. A null vector with a zero count is a real empty
    // answer only when customModifiersKnown is set; hand-written/legacy rows
    // zero-fill it and the reflection API fails loud instead of fabricating one.
    const Dn2CppTypeInfo* const* requiredCustomModifiers;
    int32_t requiredCustomModifierCount;
    const Dn2CppTypeInfo* const* optionalCustomModifiers;
    int32_t optionalCustomModifierCount;
    int32_t customModifiersKnown;
};

// Reflection method metadata. One entry per declared method in a type's
// Dn2CppTypeInfo::methods table. attrs uses the same STATIC/PUBLIC/PRIVATE bit
// layout as Dn2CppFieldInfo (DN2CPP_MTHA_* alias DN2CPP_FLDA_*). vtableSlot is the
// method's virtual slot (>= 0) or -1 for non-virtual, used by GetMethods to collapse
// an override onto its base virtual (a `new`/overload keeps both). fnPtr is the
// emitted method's function pointer (for Invoke), or null when the method body
// was not reached/emitted.
struct Dn2CppMethodInfo
{
    const char* name;
    const Dn2CppTypeInfo* declaringType;
    const Dn2CppTypeInfo* returnType;
    const Dn2CppParamInfo* parameters;
    int32_t paramCount;
    int32_t attrs;                       // DN2CPP_MTHA_* (== DN2CPP_FLDA_*) bits
    int32_t vtableSlot;                  // virtual slot, or -1
    void* fnPtr;
    // Signature-deduplicated invoker thunk: unboxes/casts the boxed args,
    // calls fnPtr with the right C++ signature, and boxes the result. null when the
    // method body was not reached/emitted (Invoke then throws). Shape:
    //   Dn2CppObject* (*)(void* fn, Dn2CppObject* self, Dn2CppObject** args,
    //                     const Dn2CppTypeInfo* retType)
    void* invoker;
    // Custom attributes applied to this method/constructor; 0-fill trailing.
    const Dn2CppAttrInfo* customAttrs;
    int32_t customAttrCount;
    // The method's v1 sigShape ("(paramTypes):retType" in TypeDesc rendering) —
    // the overload discriminator the hot-update loader matches an import against
    // by string equality when a type carries several same-(name, arity, static)
    // methods (chiefly the instantiations a generic method emits under one name).
    // Non-null only in a --hotupdate-base build; null otherwise (normal builds
    // never read it). Hand-written method rows that omit it (and the trailing
    // members below) value-initialize the rest to 0.
    const char* sigShape;
    // Raw ECMA MethodAttributes / MethodImplAttributes words (MethodBase.Attributes,
    // IsVirtual/IsAbstract/IsFinal, MethodImplementationFlags) + the method's
    // metadata token (MemberInfo.MetadataToken). 0-fill trailing convention.
    int32_t ilAttrs;
    int32_t ilImplAttrs;
    int32_t metadataToken;
    // The generic arity of the method this row instantiates (0 for a non-generic
    // method; rows are per-closed-instantiation, so a generic row's arity is its
    // instantiation's argument count). Backs GetMethod(name, genericParameterCount,
    // …) arity matching. 0-fill trailing convention.
    int32_t genericParamCount;
    // The closed instantiation's type arguments (genericParamCount entries), or
    // null for a non-generic method. Backs MakeGenericMethod's in-image resolution
    // (rows sharing the definition's metadata token are matched argument-wise) and
    // MethodInfo.GetGenericArguments. 0-fill trailing convention.
    const Dn2CppTypeInfo* const* genericArgs;
    // Return-parameter custom modifiers; ordinary parameters carry the same
    // representation in Dn2CppParamInfo.
    const Dn2CppTypeInfo* const* returnRequiredCustomModifiers;
    int32_t returnRequiredCustomModifierCount;
    const Dn2CppTypeInfo* const* returnOptionalCustomModifiers;
    int32_t returnOptionalCustomModifierCount;
    int32_t returnCustomModifiersKnown;
};

// Dn2CppMethodInfo::attrs bits; identical layout to DN2CPP_FLDA_* so the
// binding-flag matcher is shared between fields and methods.
#define DN2CPP_MTHA_STATIC      0x1
#define DN2CPP_MTHA_PUBLIC      0x2
#define DN2CPP_MTHA_PRIVATE     0x4
// MethodAttributes.SpecialName (operator methods, property/event accessors).
#define DN2CPP_MTHA_SPECIALNAME 0x20
// A generic method's closed instantiation (MethodBase.IsGenericMethod). ECMA
// MethodAttributes carries no genericness bit, so it rides the dn2cpp attrs word.
#define DN2CPP_MTHA_GENERIC     0x40
// A metadata-ANSWERED row: a member of an INTRINSIC type (lowered inline, so its
// ti_ carries no method table) whose result is a function of the runtime type
// metadata, synthesized on demand so a named lookup can answer it. Such a row has
// no fnPtr and no invoker — no compiled body exists anywhere in the image — so
// dn2cpp_invoke_mi must test this bit BEFORE reading them. Never emitted; see
// "metadata-answerable members" in dn2cpp_system_reflection.cpp.
#define DN2CPP_MTHA_METAANSWER  0x80

// A reflected method handle wrapping a Dn2CppMethodInfo* entry; backs
// System.Reflection.MethodInfo (header type dn2cpp_methodinfo_type).
struct Dn2CppMethodRef : Dn2CppObject
{
    const Dn2CppMethodInfo* method;
    // MemberInfo.ReflectedType; see the Dn2CppFieldRef note for the model and
    // the mint-time normalization (never null on a minted handle).
    const Dn2CppTypeInfo* reflectedType;
    // Definition-view flag (GetGenericMethodDefinition). The image carries no
    // open generic method rows — a "definition" handle wraps a representative
    // closed row, retagged: IsGenericMethodDefinition answers true, equality
    // compares (declaringType, metadata token) so the definitions obtained from
    // two different instantiations of one method agree, and MakeGenericMethod
    // re-resolves in-image. GetGenericArguments still reports the wrapped row's
    // CLOSED arguments (no open T handles exist), a documented divergence.
    int32_t isGenericDefView;
};

// A reflected parameter handle wrapping a Dn2CppParamInfo* entry; backs
// System.Reflection.ParameterInfo (header type dn2cpp_parameterinfo_type). Position
// is carried on the handle since a ParamInfo entry has no back-pointer; owner is
// the declaring member's row (ParameterInfo.Member), null when the handle was
// created without one.
struct Dn2CppParamRef : Dn2CppObject
{
    const Dn2CppParamInfo* param;
    int32_t position;
    const Dn2CppMethodInfo* owner;
    // The reflectedType of the member handle GetParameters was called on.
    // ParameterInfo carries no ReflectedType of its own, but .NET's
    // ParameterInfo.Member IS the originating member instance with its
    // ReflectedType intact, so paramref_member mints through this rather than
    // through the declaring-normalized null.
    const Dn2CppTypeInfo* ownerReflected;
};

// Reflection property metadata. getter/setter point at the property's accessor
// entries in the declaring type's method table (null when the property has no get/set
// accessor); GetValue/SetValue dispatch through their invoker thunks. attrs uses the
// DN2CPP_MTHA_* bits derived from the accessors (public if either accessor is public;
// static if the accessors are static) for BindingFlags matching.
struct Dn2CppPropInfo
{
    const char* name;
    const Dn2CppTypeInfo* declaringType;
    const Dn2CppTypeInfo* propType;
    const Dn2CppMethodInfo* getter;
    const Dn2CppMethodInfo* setter;
    int32_t attrs;
    // Custom attributes applied to this property; 0-fill trailing convention.
    const Dn2CppAttrInfo* customAttrs;
    int32_t customAttrCount;
    // The property's metadata token (MemberInfo.MetadataToken); 0-fill trailing.
    int32_t metadataToken;
};

// A reflected property handle wrapping a Dn2CppPropInfo* entry; backs
// System.Reflection.PropertyInfo (header type dn2cpp_propertyinfo_type).
struct Dn2CppPropRef : Dn2CppObject
{
    const Dn2CppPropInfo* prop;
    // MemberInfo.ReflectedType; see the Dn2CppFieldRef note for the model and
    // the mint-time normalization (never null on a minted handle).
    const Dn2CppTypeInfo* reflectedType;
};

struct Dn2CppArray : Dn2CppObject
{
    int32_t length;
};

struct Dn2CppArrayI4 : Dn2CppArray
{
    int32_t data[1]; // trailing elements allocated inline
};

struct Dn2CppArrayRef : Dn2CppArray
{
    Dn2CppObject* data[1]; // trailing elements allocated inline
};

// General element-sized array (for long/float/double/struct elements).
struct Dn2CppArrayN : Dn2CppArray
{
    int32_t elemSize;
    alignas(16) char data[1]; // element-aligned inline storage
};

struct Dn2CppMDArray : Dn2CppObject
{
    int32_t rank;
    int32_t* lengths;
    int32_t* lowerBounds;
    int32_t elemSize;
    char* data;
};

// Uniform layout of all generated delegate types ({target, method, prev}).
// `prev` chains earlier entries of the invocation list (null = single).
struct Dn2CppDelegate : Dn2CppObject
{
    Dn2CppObject* target;
    void* method;
    Dn2CppObject* prev;
};

// A reflection-bound delegate's context node (MethodInfo.CreateDelegate /
// Delegate.CreateDelegate): parked in the delegate's `target` slot and unpacked
// by the per-Invoke-signature dgrefl_* trampoline the emitter generates as the
// delegate's `method`. `target` is the user-visible bound receiver / first
// argument (null for open bindings); `mode` selects how the trampoline maps its
// own arguments onto the methtab row's boxed invoker.
#define DN2CPP_DGBIND_OPEN_STATIC     0
#define DN2CPP_DGBIND_CLOSED_INSTANCE 1
#define DN2CPP_DGBIND_OPEN_INSTANCE   2
#define DN2CPP_DGBIND_CLOSED_STATIC   3
struct Dn2CppReflBind : Dn2CppObject
{
    const Dn2CppMethodInfo* method;
    Dn2CppObject* target;
    int32_t mode;
};
// The node's header type-info (identity tag only; never a managed Type).
extern const Dn2CppTypeInfo dn2cpp_reflbind_type;

// One row per emitted delegate class: its type-info + the reflection-bind
// trampoline for its Invoke signature. Emitted by CppEmitter — real rows only
// when a CreateDelegate intrinsic was reached, a single null row otherwise so
// the symbols always link.
struct Dn2CppDelegateReflEntry
{
    const Dn2CppTypeInfo* ti;
    void* tramp;
};
extern const Dn2CppDelegateReflEntry dn2cpp_delegate_refl_registry[];
extern const int32_t dn2cpp_delegate_refl_registry_count;

// MethodInfo.CreateDelegate / Delegate.CreateDelegate: bind a methtab row into
// a fresh delegate of `dt` through the registry trampoline (boxed-invoker
// dispatch — the IL2CPP-style degradation for reflection-created delegates).
// `closedForm` marks the overloads passing an explicit target/firstArgument
// (which admit a null-bound closed-instance delegate, like real .NET); a bind
// failure throws ArgumentException, or returns null when `throwOnFailure` == 0
// (the throwOnBindFailure: false forms). The AOT boundaries (delegate type not
// in the registry, method body not compiled, open-instance value-type
// receiver) throw a catchable PlatformNotSupportedException instead.
Dn2CppObject* dn2cpp_delegate_create(Dn2CppType* dt, Dn2CppObject* target,
                                     Dn2CppMethodRef* m, int32_t closedForm,
                                     int32_t throwOnFailure);
// The boxed-invoker dispatch behind a dgrefl_* trampoline.
Dn2CppObject* dn2cpp_reflbind_invoke(Dn2CppReflBind* ctx, Dn2CppObject* self, Dn2CppObject** argv);
// Delegate.Target / Delegate.Method: the bound receiver / reflected MethodInfo,
// unwrapping a reflection-bind node. An IL-constructed delegate reports a null
// Method (its `method` is a bare code address with no metadata back-reference).
Dn2CppObject* dn2cpp_delegate_get_target(Dn2CppObject* d);
Dn2CppObject* dn2cpp_delegate_get_method(Dn2CppObject* d);
Dn2CppObject* dn2cpp_delegate_combine(Dn2CppObject* a, Dn2CppObject* b);
Dn2CppObject* dn2cpp_delegate_remove(Dn2CppObject* source, Dn2CppObject* value);
// Delegate value equality/hash over the uniform {target, method, prev} layout:
// two delegates are equal iff they are the same delegate type and their
// invocation chains match pairwise (matching .NET Delegate/MulticastDelegate
// semantics); the hash folds the chain's (target, method) pairs so equal
// delegates always agree (stable per process — the exact .NET number, which is
// type-identity based, is not modeled). Backs Delegate.op_Equality/Equals/
// GetHashCode and the DN2CPP_TF_DELEGATE branches of the object hash/equality
// helpers (e.g. GodotSharp's DelegateUtils callbacks, where the engine keys
// Callable dedup on managed delegate identity).
int32_t dn2cpp_delegate_equal(Dn2CppObject* a, Dn2CppObject* b);
int32_t dn2cpp_delegate_hash(Dn2CppObject* d);
// Mutex-interned fallback for a type-info whose typeObject was never wired
// (defensive: statically-emitted and runtime tis all bake/stamp one).
Dn2CppType* dn2cpp_get_type_from_handle_slow(const Dn2CppTypeInfo* handle);
// typeof(X)/GetType(): the interned Type for a handle — a lock-free field load.
// The const_cast is safe: a Dn2CppType is never written after creation (the
// monitor and identity hash key on its address via side tables), so the const
// (.rodata) companions are only ever read through the returned pointer.
inline Dn2CppType* dn2cpp_get_type_from_handle(const Dn2CppTypeInfo* handle)
{
    if (handle == nullptr)
        return nullptr;
    if (handle->typeObject != nullptr)
        return const_cast<Dn2CppType*>(handle->typeObject);
    return dn2cpp_get_type_from_handle_slow(handle);
}
int32_t dn2cpp_type_equals(Dn2CppType* a, Dn2CppType* b);
// The simple-name tail of a CLR reflection name: the pointer past the last '.'
// OR '+' ("Ns.Outer+Inner" -> "Inner", "Ns.Outer+Inner[]" -> "Inner[]"). The one
// splitter every name-splitting surface shares — a nested type-info's name
// carries the CLR '+' syntax, so a dot-only split would leak "Outer+Inner".
const char* dn2cpp_simple_type_name(const char* full);
// Type.Name — the simple name (after the last '.' or '+'), unlike the qualified
// Type.FullName / Type.ToString.
Dn2CppString* dn2cpp_type_name(const Dn2CppTypeInfo* ti);
// Type.FullName and Type.ToString. Both are ti->name verbatim ('+'-qualified for a
// nested type) EXCEPT for a closed generic, which composes Def`N[...] from its
// genericDef + genericArgs: the emitted ti->name is the dn2cpp-mangled
// instantiation and is a contract elsewhere (--trim-reflection's --reflection-root
// remedy), so it is never changed. FullName qualifies each argument with its
// assembly display name; ToString does not — that is the whole difference. A generic
// DEFINITION is the mirror case: ToString appends its parameter names (List`1[T]),
// FullName does not.
Dn2CppString* dn2cpp_type_fullname(const Dn2CppTypeInfo* ti);
Dn2CppString* dn2cpp_type_tostring(const Dn2CppTypeInfo* ti);
// Type.Namespace: the declaring chain's namespace — the part before the last '.'
// of the outermost name (i.e. within the prefix up to the first '+'), empty when
// the type has none.
Dn2CppString* dn2cpp_type_namespace(const Dn2CppTypeInfo* ti);
// Type.IsEnum / IsArray / IsInterface / IsAbstract — read the flag bits, so they
// answer for any type (typeof or a runtime GetType()), not only static folds.
int32_t dn2cpp_type_is_enum(const Dn2CppTypeInfo* ti);
int32_t dn2cpp_type_is_array(const Dn2CppTypeInfo* ti);
int32_t dn2cpp_type_is_szarray(const Dn2CppTypeInfo* ti);
int32_t dn2cpp_type_is_nested(const Dn2CppTypeInfo* ti);
int32_t dn2cpp_type_is_interface(const Dn2CppTypeInfo* ti);
int32_t dn2cpp_type_is_abstract(const Dn2CppTypeInfo* ti);
int32_t dn2cpp_type_is_sealed(const Dn2CppTypeInfo* ti);
// Type.IsByRefLike reads the flag bit. Type.IsPointer / IsByRef are always 0 at
// runtime — dn2cpp never materializes a pointer/byref Type value (only typeof of a
// pointer/byref type folds to true statically); the helpers take the type so the
// receiver is still evaluated, matching the other Type-property getters.
int32_t dn2cpp_type_is_by_ref_like(const Dn2CppTypeInfo* ti);
// Type.IsNestedPublic: the ECMA visibility nibble of the raw TypeAttributes
// word equals NestedPublic (the synthesized word for hand-written type-infos
// never carries a nested visibility, so they answer false).
int32_t dn2cpp_type_is_nested_public(const Dn2CppTypeInfo* ti);
int32_t dn2cpp_type_is_pointer(const Dn2CppTypeInfo* ti);
int32_t dn2cpp_type_is_by_ref(const Dn2CppTypeInfo* ti);
int32_t dn2cpp_type_is_value_type(const Dn2CppTypeInfo* ti);
int32_t dn2cpp_type_is_class(const Dn2CppTypeInfo* ti);
int32_t dn2cpp_type_is_primitive(const Dn2CppTypeInfo* ti);
// Type.GetTypeCode: the type's System.TypeCode numeric value (an enum unwraps to its
// underlying integer's code, like .NET). The static typeof(T) fold serves the real
// path; this covers a runtime Type value.
int32_t dn2cpp_type_get_type_code(const Dn2CppTypeInfo* ti);
// Type.BaseType — the base in the inheritance chain wrapped as a Dn2CppType, or null
// at the root. A reference type whose chain ends (base==nullptr) reports
// System.Object, matching .NET (our user-type chains stop instead of pointing at the
// object handle); object/interfaces report null. An array reports System.Array when
// the program carries its type-info (registry lookup), else degrades to System.Object.
Dn2CppType* dn2cpp_type_base_type(Dn2CppType* a);
// Type.GetElementType() — the SZArray element's Type for a per-element array type-info,
// or null for a non-array type (and for the shared array handles, whose element typing
// is not yet precise). Type.GetArrayRank() — the array rank (1 for SZArray), throwing
// ArgumentException for a non-array.
Dn2CppType* dn2cpp_type_get_element_type(Dn2CppType* a);
int32_t dn2cpp_type_get_array_rank(Dn2CppType* a);
// Type.IsAssignableFrom(b): an instance of b is assignable to an a-typed location.
// Delegates to dn2cpp_typeinfo_assignable — the ONE rule isinst runs — so arrays
// (System.Array, the array collection interfaces, element covariance) and generic
// variance answer here exactly as a cast would.
int32_t dn2cpp_type_is_assignable_from(Dn2CppType* a, Dn2CppType* b);
int32_t dn2cpp_type_is_assignable_to(Dn2CppType* t, Dn2CppType* target);
// Type.IsInstanceOfType(obj): obj is non-null and an instance of type a.
int32_t dn2cpp_type_is_instance_of_type(Dn2CppType* a, Dn2CppObject* obj);
// Type.IsSubclassOf(c): a strictly derives from c (class inheritance only).
// An array answers true for System.Array and System.Object (its real .NET base
// chain, which array type-infos do not carry).
int32_t dn2cpp_type_is_subclass_of(Dn2CppType* a, Dn2CppType* c);
// RuntimeHelpers.ObjectHasComponentSize(o): true for variable-length objects — arrays
// and strings (the JIT's HasComponentSize MethodTable bit). ReadOnlyMemory<T>.Span uses
// it to tell an array-backed memory from a MemoryManager-backed one.
int32_t dn2cpp_object_has_component_size(Dn2CppObject* o);
// Type.Assembly identity: the defining assembly's simple name (null type-info name
// falls back to "System.Private.CoreLib", since only hand-written CoreLib type-infos
// omit it). Assembly is modeled as this opaque name pointer; op_Equality compares the
// names. Reached from JsonConverter..ctor (IsInternalConverter = GetType().Assembly ==
// typeof(JsonConverter).Assembly).
const char* dn2cpp_type_assembly_name(Dn2CppType* a);
// The .NET assembly display name ("Name, Version=…, Culture=…, PublicKeyToken=…")
// as UTF-8. Exposed because the assembly registry lives in another TU than
// dn2cpp_type_fullname, which qualifies each generic argument with it.
const char* dn2cpp_assembly_display_name_utf8(const char* name);
int32_t dn2cpp_assembly_equals(const char* a, const char* b);
// Assembly.GetTypes(): the type-registry entries defined in the named assembly
// as a Type[] (the AOT-preserved subset, like IL2CPP metadata).
Dn2CppArrayRef* dn2cpp_assembly_get_types(const char* asmName);
// RuntimeHelpers.GetUninitializedObject(Type): a zeroed, finalizer-registered
// instance with the type header planted and no constructor run.
Dn2CppObject* dn2cpp_get_uninitialized_object(Dn2CppType* t);

// Generic reflection. IsGenericType / IsGenericTypeDefinition /
// ContainsGenericParameters read the type-info; GetGenericTypeDefinition returns the
// open definition (or throws InvalidOperationException for a non-generic type);
// GetGenericArguments returns the closed type arguments as a Type[]. MakeGenericType
// looks the requested (definition, args) instantiation up among the program's
// reachability-generated closed types — like IL2CPP, an instantiation that AOT didn't
// generate throws NotSupportedException rather than building it at runtime.
int32_t dn2cpp_type_is_generic_type(const Dn2CppTypeInfo* ti);
int32_t dn2cpp_type_is_constructed_generic(const Dn2CppTypeInfo* ti);
int32_t dn2cpp_type_is_generic_type_definition(const Dn2CppTypeInfo* ti);
int32_t dn2cpp_type_contains_generic_parameters(const Dn2CppTypeInfo* ti);
Dn2CppType* dn2cpp_type_get_generic_type_definition(Dn2CppType* t);
Dn2CppArrayRef* dn2cpp_type_get_generic_arguments(Dn2CppType* t);
Dn2CppType* dn2cpp_type_make_generic(Dn2CppType* def, Dn2CppArrayRef* args);

// Type/Enum reflection completion. GetInterfaces returns the type's (transitive,
// as recorded in the dispatch table) implemented interfaces as a Type[] — empty for a
// type with no emitted interface table (a never-boxed value type / abstract / interface).
// The enum bridge reads the per-enum (name, value) table + underlying handle, so the
// non-generic Enum.GetNames(Type)/GetName(Type, value)/IsDefined(Type, value)/
// Parse(Type, string[, ignoreCase]) and Type.GetEnumUnderlyingType work via a runtime
// Type, complementing the generic Enum.*<T> inline lowering.
Dn2CppArrayRef* dn2cpp_type_get_interfaces(Dn2CppType* t);
// Type.GetInterface(name[, ignoreCase]): the lone matching row of the same table,
// or null on no match. The name splits at its last '.'; the simple part matches the
// interface's simple name (the generic DEFINITION's for a closed generic, so
// "IEnumerable`1" names every instantiation); a namespace part is compared
// case-sensitively even under ignoreCase, which folds the simple part only — pinned
// against real .NET. Two matches throw AmbiguousMatchException, a null name
// ArgumentNullException.
Dn2CppType* dn2cpp_type_get_interface(Dn2CppType* t, Dn2CppString* name, int32_t ignoreCase);
Dn2CppType* dn2cpp_type_get_enum_underlying(Dn2CppType* t);
Dn2CppArrayRef* dn2cpp_enum_get_names(Dn2CppType* t);
// Enum.GetValues(Type) (non-generic): the declared values as object[] of boxed enums, so a
// foreach over the returned System.Array yields boxed `object` elements.
Dn2CppArrayRef* dn2cpp_enum_get_values_boxed(Dn2CppType* t);
// Enum.GetValuesAsUnderlyingType(Type) (non-generic): the declared values as an array of the
// UNDERLYING primitive (byte[]/int[]/long[]/...), typed with that element's precise identity so
// GetType()/GetValue read it. Built on dn2cpp_array_create_instance; returned as the array object.
Dn2CppObject* dn2cpp_enum_get_values_underlying(Dn2CppType* t);
// Nullable.GetUnderlyingType(Type): U for a closed Nullable<U>, else null.
Dn2CppType* dn2cpp_nullable_get_underlying(Dn2CppType* t);
// The value arrives BOXED: the enum type is chosen at run time at these call sites, so
// only this side can read the payload at the enum's own model width.
Dn2CppString* dn2cpp_enum_get_name(Dn2CppType* t, Dn2CppObject* value);
int32_t dn2cpp_enum_is_defined(Dn2CppType* t, Dn2CppObject* value);
Dn2CppObject* dn2cpp_enum_parse_type(Dn2CppType* t, Dn2CppString* s, int32_t ignoreCase);
// Enum.TryParse(Type, string[, ignoreCase], out object): returns 1 + *out=boxed enum on
// success, 0 + *out=null on a value parse miss; the TYPE is still validated (null / non-enum
// throw). The non-throwing complement of dn2cpp_enum_parse_type.
int32_t dn2cpp_enum_try_parse_type(Dn2CppType* t, Dn2CppString* s, int32_t ignoreCase, Dn2CppObject** out);
// Enum.ToObject(Type, <integral>): the value boxed as the enum type, truncated to the
// underlying width and re-extended per its signedness (the box payload keeps the model
// invariant: int32 = the extended underlying value, int64 for a 64-bit underlying).
// A null type throws ArgumentNullException, a non-enum type ArgumentException, as .NET.
Dn2CppObject* dn2cpp_enum_to_object(Dn2CppType* t, int64_t value);
// Enum.ToObject(Type, object): the boxed-value form — the eight integer widths plus
// Char/Boolean (and a boxed enum, read at its underlying) are accepted; float/double/
// anything else throw ArgumentException, a null value ArgumentNullException, as .NET.
Dn2CppObject* dn2cpp_enum_to_object_boxed(Dn2CppType* t, Dn2CppObject* value);
// Enum.Format(Type, object value, string format): the non-generic static enum
// formatter. "G"/"g" = the member name (flags-decomposed when the enum is [Flags],
// else the decimal value on a miss); "F"/"f" = ALWAYS flags-decomposed; "D"/"d" =
// the underlying decimal; "X"/"x" = the underlying zero-padded UPPERCASE hex (both
// x and X uppercase, as .NET does). Null type/value/format throw ArgumentNullException,
// a non-enum type ArgumentException, an unrecognized format string FormatException.
Dn2CppString* dn2cpp_enum_format(Dn2CppType* t, Dn2CppObject* value, Dn2CppString* format);
// The Span<char> / ReadOnlySpan<char> an emitted interface-dispatch site passes BY
// VALUE, i.e. the shape an ISpanFormattable.TryFormat slot is called with.
struct Dn2CppItfCharSpan { char16_t* ptr; int32_t length; };
// System.Enum's ISpanFormattable.TryFormat, as the boxed-enum interface map's slot
// for it (CppEmitter.EmitEnumInterfaceMap). The real body is reach-cut by
// CoreIntrinsics.CvEnumTryFormat — it reaches an InternalCall — so this is that
// cut's route on the boxed mouth, and it formats through dn2cpp_enum_format so the
// span mouth and the IFormattable mouth cannot answer differently. An empty format
// span is "G", matching Enum.TryFormat's own default.
int32_t dn2cpp_enum_box_try_format(Dn2CppObject* box, Dn2CppItfCharSpan dest, int32_t* written,
                                   Dn2CppItfCharSpan fmt, const Dn2CppNumberFormatInfo* nfi);
// Type.GetNestedTypes()/GetNestedType(name): the type's public nested types
// (the emitted, non-generic ones), wrapped as Type[] / a single Type or null.
Dn2CppArrayRef* dn2cpp_type_get_nested_types(Dn2CppType* t);
Dn2CppType* dn2cpp_type_get_nested_type(Dn2CppType* t, Dn2CppString* name);

// Type name registry: CLR FullName -> Dn2CppTypeInfo*. CppEmitter emits the
// table (`dn2cpp_type_registry` / `_count` in generated.cpp) covering every reachable
// user type/enum plus the well-known primitive/BCL handles; dn2cpp_type_get_by_name
// backs Type.GetType(string). Like IL2CPP's metadata, only types built into the
// program resolve; nested/generic/array names are out of scope (their FullName isn't
// the reflection name). A trailing assembly-qualified suffix (after the first ',') is
// ignored. throwOnError != 0 raises TypeLoadException instead of returning null.
struct Dn2CppTypeRegEntry { const char* name; const Dn2CppTypeInfo* type; };
extern const Dn2CppTypeRegEntry dn2cpp_type_registry[];
extern const int32_t dn2cpp_type_registry_count;
Dn2CppType* dn2cpp_type_get_by_name(Dn2CppString* name, int32_t throwOnError);

// Runtime-instantiation templates: the clone sources dn2cpp_type_make_generic
// falls back to when the registry scan finds no AOT-generated instantiation of
// `def`. One row per eligible template chain level (the emitter proves the
// definition's bodies never give a type argument value semantics — typeof-only):
// the clone copies `templateTi`, stamps genericDef/genericArgs/name, clears the
// two template bits, synthesizes its base the same way when the template's base
// is itself a row (looked up by templateTi), and fills a fresh rgctx table with
// rgctx[i] = args[rgctxDesc[i]]'s type-info. Synthesized instantiations intern
// on (def, args) — same arguments, same pointer — and register their closed
// name on the registry's dynamic side-chain.
struct Dn2CppRuntimeTemplate
{
    const Dn2CppTypeInfo* def;        // the open-definition handle (gendef_*)
    const Dn2CppTypeInfo* templateTi; // the level's emitted template type-info
    const int32_t* rgctxDesc;         // slot i -> type-argument index
    int32_t rgctxDescCount;
    int32_t argCount;
};
extern const Dn2CppRuntimeTemplate* const dn2cpp_runtime_templates;
extern const int32_t dn2cpp_runtime_template_count;

// Startup type-info binds: the generated metadata for a type whose HANDLE the runtime
// owns (the runtime-raised exception types further down) — `target` is that handle,
// `meta` the type-info CppEmitter rendered for the same type. dn2cpp_runtime_init copies
// meta over target before any managed code runs, which is what gives the handle its
// vtable, its instance size and its reflection tables; a program that transpiles none of
// them emits an empty table. One handle per managed type is the invariant this preserves:
// catch/isinst/typeof and a GVM's type switch all compare type-info POINTERS, so a second
// type-info for the same type would make the answer depend on who allocated the object.
struct Dn2CppTypeBind { Dn2CppTypeInfo* target; const Dn2CppTypeInfo* meta; };
extern const Dn2CppTypeBind dn2cpp_type_binds[];
extern const int32_t dn2cpp_type_bind_count;
// The vtable slot of System.Exception::get_Message (or -1 in a corelib-less build), a
// program property the runtime cannot know. dn2cpp_exception_message dispatches through it
// so a derived exception's get_Message override is honored even for a base-typed receiver.
// Defined only in generated output (like dn2cpp_type_binds); the runtime is never linked
// without generated code.
extern const int32_t dn2cpp_exception_get_message_slot;
// The BCL exception messages this runtime raises, folded in from the CoreLib's own
// Strings.resources at transpile time. A runtime resource read is not an option:
// --no-manifest-resources may have emptied that table, and a fault that faults while
// building its own message is uncatchable. `text` is null for a key the CoreLib did
// not carry; the caller falls back to Exception.Message's type-name form. Defined
// only in generated output.
struct Dn2CppBclMessage { const char* key; const char* text; };
extern const Dn2CppBclMessage dn2cpp_bcl_messages[];
extern const int32_t dn2cpp_bcl_message_count;
// The keys the runtime asks for. Spelled once, here, because the lookup is by NAME:
// drift against the transpiler's list can only lose a message, never answer with the
// wrong one, and gates/build-and-run-doc-claims.sh diffs the two lists.
inline constexpr const char* DN2CPP_SR_OVERFLOW = "Arg_OverflowException";
inline constexpr const char* DN2CPP_SR_INDEX_OUT_OF_RANGE = "Arg_IndexOutOfRangeException";
inline constexpr const char* DN2CPP_SR_ARGUMENT = "Arg_ArgumentException";
inline constexpr const char* DN2CPP_SR_ARGUMENT_OUT_OF_RANGE = "Arg_ArgumentOutOfRangeException";
inline constexpr const char* DN2CPP_SR_ARGUMENT_NULL = "ArgumentNull_Generic";
inline constexpr const char* DN2CPP_SR_INVALID_OPERATION = "Arg_InvalidOperationException";
inline constexpr const char* DN2CPP_SR_OBJECT_DISPOSED = "ObjectDisposed_Generic";
inline constexpr const char* DN2CPP_SR_ARITHMETIC = "Arg_ArithmeticException";
inline constexpr const char* DN2CPP_SR_OUT_OF_MEMORY = "Arg_OutOfMemoryException";
inline constexpr const char* DN2CPP_SR_INVALID_CAST = "Arg_InvalidCastException";
inline constexpr const char* DN2CPP_SR_TYPE_LOAD = "Arg_TypeLoadException";
inline constexpr const char* DN2CPP_SR_NOT_SUPPORTED = "Arg_NotSupportedException";
inline constexpr const char* DN2CPP_SR_PLATFORM_NOT_SUPPORTED = "Arg_PlatformNotSupported";
inline constexpr const char* DN2CPP_SR_FORMAT = "Arg_FormatException";
inline constexpr const char* DN2CPP_SR_IO = "Arg_IOException";
inline constexpr const char* DN2CPP_SR_FILE_NOT_FOUND = "IO_FileNotFound";
inline constexpr const char* DN2CPP_SR_UNAUTHORIZED_ACCESS = "Arg_UnauthorizedAccessException";
inline constexpr const char* DN2CPP_SR_KEY_NOT_FOUND = "Arg_KeyNotFound";
inline constexpr const char* DN2CPP_SR_AMBIGUOUS_MATCH = "Arg_AmbiguousMatchException_NoMessage";
inline constexpr const char* DN2CPP_SR_MISSING_METHOD = "Arg_MissingMethodException";
inline constexpr const char* DN2CPP_SR_NULL_REFERENCE = "Arg_NullReferenceException";
inline constexpr const char* DN2CPP_SR_DIVIDE_BY_ZERO = "Arg_DivideByZero";
inline constexpr const char* DN2CPP_SR_SYNCHRONIZATION_LOCK = "Arg_SynchronizationLockException";
inline constexpr const char* DN2CPP_SR_FORMAT_INVALID_STRING_WITH_VALUE = "Format_InvalidStringWithValue";
inline constexpr const char* DN2CPP_SR_BAD_DATETIME = "Format_BadDateTime";
inline constexpr const char* DN2CPP_SR_BAD_DATEONLY = "Format_BadDateOnly";
inline constexpr const char* DN2CPP_SR_BAD_TIMEONLY = "Format_BadTimeOnly";
inline constexpr const char* DN2CPP_SR_BAD_TIMESPAN = "Format_BadTimeSpan";
inline constexpr const char* DN2CPP_SR_NEED_SINGLE_CHAR = "Format_NeedSingleChar";
inline constexpr const char* DN2CPP_SR_ENUM_VALUE_NOT_FOUND = "Arg_EnumValueNotFound";
inline constexpr const char* DN2CPP_SR_PATH_TOO_LONG_PATH = "IO_PathTooLong_Path";
inline constexpr const char* DN2CPP_SR_ADDING_DUPLICATE_WITH_KEY = "Argument_AddingDuplicateWithKey";
inline constexpr const char* DN2CPP_SR_KEY_NOT_FOUND_WITH_KEY = "Arg_KeyNotFoundWithKey";
inline constexpr const char* DN2CPP_SR_MUST_BE_NON_NEGATIVE = "ArgumentOutOfRange_Generic_MustBeNonNegative";
inline constexpr const char* DN2CPP_SR_START_INDEX_LARGER_THAN_LENGTH = "ArgumentOutOfRange_StartIndexLargerThanLength";
inline constexpr const char* DN2CPP_SR_INDEX_LENGTH = "ArgumentOutOfRange_IndexLength";
inline constexpr const char* DN2CPP_SR_PARAM_NAME = "Arg_ParamName_Name";
inline constexpr const char* DN2CPP_SR_ACTUAL_VALUE = "ArgumentOutOfRange_ActualValue";
// The text for a key, or null when this program carries none (no corelib, a corelib with
// no embedded resources, or a key outside Dn2Cpp.BclMessages).
const char* dn2cpp_sr_text(const char* key);
// Dynamic side-chain of the type-name registry: type-infos constructed at run
// time (the hot-update loader's patch types) registered by CLR FullName.
// Lookups scan the static table first — a built-in type can never be shadowed
// — then the chain newest-first, so re-registering a name (re-loading a
// same-named patch type) makes the newest registration the visible one while
// existing instances keep their original type-info (append-only loading).
// Name-lookup surfaces (Type.GetType(string), the loader's own binds) see the
// chain; enumerating surfaces (Assembly.GetTypes, the MakeGenericType scan)
// stay static-table-only. `name`/`type` must be GC-visible allocations (the
// chain head is a data-segment root; entries are dn2cpp_alloc'd).
struct Dn2CppDynTypeReg { const char* name; const Dn2CppTypeInfo* type; const Dn2CppDynTypeReg* next; };
void dn2cpp_type_registry_add(const char* name, const Dn2CppTypeInfo* type);
const Dn2CppTypeInfo* dn2cpp_type_registry_find(const char* name, int32_t len);
const Dn2CppDynTypeReg* dn2cpp_type_registry_dynamic_head();

// Reflection field enumeration. Type.GetFields(bindingFlags) returns a
// FieldInfo[] (filtered by the flags, walking the base chain like .NET), and
// Type.GetField(name, bindingFlags) the single match or null. The FieldInfo handle's
// members route through dn2cpp_fieldref_* / dn2cpp_memberinfo_* (the latter also
// serve System.Type, dispatching on the object header).
extern const Dn2CppTypeInfo dn2cpp_fieldinfo_type;
struct Dn2CppArrayRef;
Dn2CppArrayRef* dn2cpp_type_get_fields(Dn2CppType* t, int32_t bindingFlags);
Dn2CppFieldRef* dn2cpp_type_get_field(Dn2CppType* t, Dn2CppString* name, int32_t bindingFlags);
Dn2CppType* dn2cpp_fieldref_field_type(Dn2CppFieldRef* f);
int32_t dn2cpp_fieldref_is_static(Dn2CppFieldRef* f);
int32_t dn2cpp_fieldref_is_public(Dn2CppFieldRef* f);
int32_t dn2cpp_fieldref_is_initonly(Dn2CppFieldRef* f);
int32_t dn2cpp_fieldref_is_literal(Dn2CppFieldRef* f);
int32_t dn2cpp_methodref_is_specialname(Dn2CppMethodRef* m);
// FieldInfo.GetValue(obj) / SetValue(obj, value): obj is the instance (null
// for a static field). Value types are boxed on get / unboxed on set via the
// emitter-generated thunks; reference fields pass the object reference through.
Dn2CppObject* dn2cpp_fieldref_get_value(Dn2CppFieldRef* f, Dn2CppObject* obj);
void dn2cpp_fieldref_set_value(Dn2CppFieldRef* f, Dn2CppObject* obj, Dn2CppObject* value);
// MemberInfo.Name / DeclaringType — shared by FieldInfo, MethodInfo and Type;
// dispatch on the managed object header (a FieldRef carries dn2cpp_fieldinfo_type,
// a MethodRef dn2cpp_methodinfo_type).
Dn2CppString* dn2cpp_memberinfo_name(Dn2CppObject* m);
Dn2CppType* dn2cpp_memberinfo_declaring_type(Dn2CppObject* m);
// MemberInfo.ReflectedType: the mint-time-stamped type the member was obtained
// through (see the Dn2CppFieldRef note); null for a Type receiver, mirroring
// what DeclaringType answers there (nested-type enclosure is not modeled).
Dn2CppType* dn2cpp_memberinfo_reflected_type(Dn2CppObject* m);
// MemberInfo/MethodBase/MethodInfo/ConstructorInfo op_Equality: two reflected
// handles are equal when they wrap the same underlying metadata entry AND were
// obtained through the same type (row + reflectedType — .NET's rule, under
// which typeof(D).GetMethod(m) != typeof(Base).GetMethod(m) for an inherited
// m); Types compare by type-info identity.
int32_t dn2cpp_memberinfo_equals(Dn2CppObject* a, Dn2CppObject* b);

// Reflection method enumeration. Type.GetMethods(bindingFlags) returns a
// MethodInfo[] (filtered by the flags, walking the base chain like .NET, with virtual
// overrides collapsed onto their base slot), and Type.GetMethod(name, bindingFlags)
// the single match or null. Object's intrinsic methods (ToString/Equals/GetHashCode/
// GetType) are opaque to us, so — unlike real .NET — they are not enumerated.
extern const Dn2CppTypeInfo dn2cpp_methodinfo_type;
// The abstract MemberInfo/MethodBase bases and the ConstructorInfo header:
// the reflection handle hierarchy mirrors .NET's (Type/FieldInfo/PropertyInfo
// chain to MemberInfo; MethodInfo/ConstructorInfo to MethodBase), so
// casts/`is` against the reflection classes resolve like real .NET.
extern const Dn2CppTypeInfo dn2cpp_memberinfo_type;
extern const Dn2CppTypeInfo dn2cpp_methodbase_type;
extern const Dn2CppTypeInfo dn2cpp_constructorinfo_type;
extern const Dn2CppTypeInfo dn2cpp_parameterinfo_type;
extern const Dn2CppTypeInfo dn2cpp_void_type;
Dn2CppArrayRef* dn2cpp_type_get_methods(Dn2CppType* t, int32_t bindingFlags);
Dn2CppMethodRef* dn2cpp_type_get_method(Dn2CppType* t, Dn2CppString* name, int32_t bindingFlags);
// The unified worker behind EVERY Type.GetMethod overload. genericParamCount:
// the requested generic arity (-1 = no arity filter; 0 = non-generic only).
// paramTypes: exact parameter-type match (null = match by name only). callConv:
// accepted for overload fidelity and ignored — real .NET's filter is
// observably a no-op against Standard methods, which every transpiled method
// is. binder: a non-null (custom) Binder throws a catchable
// PlatformNotSupportedException — Type.DefaultBinder is modeled as null, so
// the BCL's null/DefaultBinder calls stay supported.
// Several undecidable matches throw AmbiguousMatchException like real .NET
// (sig-equal matches resolve to the most derived one; a generic method's
// instantiation rows collapse onto their shared definition token first).
Dn2CppMethodRef* dn2cpp_type_get_method_full(Dn2CppType* t, Dn2CppString* name,
                                             int32_t genericParamCount, Dn2CppArrayRef* paramTypes,
                                             int32_t bindingFlags, int32_t callConv,
                                             Dn2CppObject* binder);
Dn2CppType* dn2cpp_methodref_return_type(Dn2CppMethodRef* m);
int32_t dn2cpp_methodref_is_static(Dn2CppMethodRef* m);
int32_t dn2cpp_methodref_is_public(Dn2CppMethodRef* m);
Dn2CppArrayRef* dn2cpp_methodref_get_parameters(Dn2CppMethodRef* m);
// MethodInfo.ReturnParameter: a synthesized ParameterInfo over the return type
// (Position -1, Name null), matching real .NET's return pseudo-parameter.
Dn2CppObject* dn2cpp_methodref_return_parameter(Dn2CppMethodRef* m);
Dn2CppType* dn2cpp_paramref_parameter_type(Dn2CppParamRef* p);
int32_t dn2cpp_paramref_position(Dn2CppParamRef* p);
Dn2CppString* dn2cpp_paramref_name(Dn2CppParamRef* p);
// MethodInfo.Invoke(obj, object[] args): dispatches through the method's
// signature-deduplicated invoker thunk. obj is the instance (null/ignored for a
// static method); for a value-type receiver the unboxed payload is passed. Returns
// the boxed result (null for void). A method with no emitted body (invoker == null)
// throws InvalidOperationException; an arg-count mismatch throws ArgumentException.
Dn2CppObject* dn2cpp_methodref_invoke(Dn2CppMethodRef* m, Dn2CppObject* obj, Dn2CppArrayRef* args);

// Reflection constructor enumeration + invocation. Type.GetConstructors(flags)
// returns a ConstructorInfo[] (the type's own ctors only — never inherited), and
// Type.GetConstructor(paramTypes, flags) the ctor whose parameter types match or null.
// ConstructorInfo.Invoke(object[] args) allocates a new instance (a boxed value for a
// value type), runs the ctor, and returns it. A ConstructorInfo handle is a
// Dn2CppMethodRef (header dn2cpp_methodinfo_type) wrapping a ctor's Dn2CppMethodInfo.
// dn2cpp_activator_create_instance backs the non-generic Activator.CreateInstance(Type):
// it invokes the parameterless ctor, or zero-inits a value type that has none.
Dn2CppArrayRef* dn2cpp_type_get_constructors(Dn2CppType* t, int32_t bindingFlags);
Dn2CppMethodRef* dn2cpp_type_get_constructor(Dn2CppType* t, Dn2CppArrayRef* paramTypes, int32_t bindingFlags);
// The unified worker behind EVERY Type.GetConstructor overload — callConv and
// binder carry the same semantics as dn2cpp_type_get_method_full.
Dn2CppMethodRef* dn2cpp_type_get_constructor_full(Dn2CppType* t, Dn2CppArrayRef* paramTypes,
                                                  int32_t bindingFlags, int32_t callConv,
                                                  Dn2CppObject* binder);
Dn2CppObject* dn2cpp_ctorref_invoke(Dn2CppMethodRef* c, Dn2CppArrayRef* args);
Dn2CppObject* dn2cpp_activator_create_instance(Dn2CppType* t);
// Activator.CreateInstance(Type, bool nonPublic): the parameterless form with
// the visibility switch; the no-arg form above is nonPublic: false. Missing /
// invisible parameterless ctor and abstract/interface targets throw the
// catchable MissingMethodException (probed against real .NET).
Dn2CppObject* dn2cpp_activator_create_instance_nonpublic(Dn2CppType* t, int32_t nonPublic);
// Activator.CreateInstance(Type, object[] args[, BindingFlags, Binder,
// activationAttributes]): DefaultBinder-style ctor resolution — filter on
// flags + arity + boxed-argument admissibility (null matches everything,
// reference widening, the CLR primitive-widening matrix, enum-as-underlying
// sources), resolve multiple candidates to the unique most-specific one
// (AmbiguousMatchException when none dominates), adapt the bound arguments
// (re-box widened primitives, default(T) for null against a value type), and
// invoke. No candidate throws MissingMethodException; a non-null Binder the
// AOT PlatformNotSupportedException; non-empty activation attributes the
// PlatformNotSupportedException real .NET raises.
Dn2CppObject* dn2cpp_activator_create_instance_args(Dn2CppType* t, Dn2CppArrayRef* args,
                                                    int32_t bindingFlags, Dn2CppObject* binder,
                                                    Dn2CppArrayRef* activationAttrs);

// Reflection property enumeration + access. Type.GetProperties(flags) returns a
// PropertyInfo[] (base-chain-walked, filtered by flags); Type.GetProperty(name, flags)
// the first match or null. PropertyInfo.GetValue(obj)/SetValue(obj, value) dispatch
// through the property's getter/setter accessor invoker thunks. PropertyType/CanRead/
// CanWrite read the table; Name/DeclaringType route through dn2cpp_memberinfo_*.
extern const Dn2CppTypeInfo dn2cpp_propertyinfo_type;
Dn2CppArrayRef* dn2cpp_type_get_properties(Dn2CppType* t, int32_t bindingFlags);
Dn2CppPropRef* dn2cpp_type_get_property(Dn2CppType* t, Dn2CppString* name, int32_t bindingFlags);
// The unified worker behind EVERY Type.GetProperty overload. returnType (null =
// any) filters on the property type; indexTypes (null = any; empty = non-indexed
// only) filters on the indexer parameter types read off the accessor rows;
// binder carries the same semantics as dn2cpp_type_get_method_full. Several
// undecidable matches (same-name properties that differ in type or index
// signature after the filters) throw AmbiguousMatchException like real .NET.
Dn2CppPropRef* dn2cpp_type_get_property_full(Dn2CppType* t, Dn2CppString* name, int32_t bindingFlags,
                                             Dn2CppType* returnType, Dn2CppArrayRef* indexTypes,
                                             Dn2CppObject* binder);
// Type.GetMember(name[, MemberTypes][, BindingFlags]) / GetMembers([flags]) /
// GetDefaultMembers() -> MemberInfo[] mixing method/ctor/property/field/nested-
// type handles. `name` supports the documented trailing-'*' prefix wildcard
// (null = match every member, the GetMembers form). memberTypes is the
// System.Reflection.MemberTypes bit set (0xBF = All).
Dn2CppArrayRef* dn2cpp_type_get_member(Dn2CppType* t, Dn2CppString* name,
                                       int32_t memberTypes, int32_t bindingFlags);
Dn2CppArrayRef* dn2cpp_type_get_members(Dn2CppType* t, int32_t bindingFlags);
Dn2CppArrayRef* dn2cpp_type_get_default_members(Dn2CppType* t);
// Type.GetMemberWithSameMetadataDefinitionAs(MemberInfo): the member of `t`
// (or its base chain) wrapping the same metadata definition — matched by
// (metadata token, defining assembly) so it crosses generic instantiations.
// Throws ArgumentException when the type has no such member, like real .NET.
Dn2CppObject* dn2cpp_type_get_member_same_metadata(Dn2CppType* t, Dn2CppObject* member);
Dn2CppType* dn2cpp_propref_property_type(Dn2CppPropRef* p);
int32_t dn2cpp_propref_can_read(Dn2CppPropRef* p);
int32_t dn2cpp_propref_can_write(Dn2CppPropRef* p);
// GetGetMethod/GetSetMethod/get_GetMethod/get_SetMethod: the accessor's
// reflected MethodInfo (null when absent, or non-public without nonPublic).
Dn2CppMethodRef* dn2cpp_propref_accessor(Dn2CppPropRef* p, int32_t setter, int32_t nonPublic);
Dn2CppObject* dn2cpp_propref_get_value(Dn2CppPropRef* p, Dn2CppObject* obj);
void dn2cpp_propref_set_value(Dn2CppPropRef* p, Dn2CppObject* obj, Dn2CppObject* value);
// Indexed properties: GetIndexParameters() reads the indexer parameters off
// the accessor rows (getter's parameters, or the setter's minus the trailing
// value); the GetValue/SetValue index-array forms route the indices (plus the
// value, for the setter) through the accessor's boxed invoker. binder is the
// BindingFlags overloads' Binder argument (non-null -> the AOT
// PlatformNotSupportedException); the plain overloads pass null.
Dn2CppArrayRef* dn2cpp_propref_get_index_parameters(Dn2CppPropRef* p);
Dn2CppObject* dn2cpp_propref_get_value_indexed(Dn2CppPropRef* p, Dn2CppObject* obj,
                                               Dn2CppArrayRef* index, Dn2CppObject* binder);
void dn2cpp_propref_set_value_indexed(Dn2CppPropRef* p, Dn2CppObject* obj, Dn2CppObject* value,
                                      Dn2CppArrayRef* index, Dn2CppObject* binder);

// Boxed-value <-> element-storage conversion cores, defined in
// dn2cpp_system_reflection.cpp beside the Activator argument binder (which
// shares their CanPrimitiveWiden matrix and Nullable/enum layout rules) and
// consumed cross-TU by the non-generic Array surface in
// dn2cpp_system_array.cpp. dn2cpp_prim_code is the CorElementType-style
// primitive code of a primitive (or bool/char) type-info, -1 on a
// non-primitive; dn2cpp_prim_storage_width the packed array-element width of
// such a code (NOT the widened box-payload width).
int32_t dn2cpp_prim_code(const Dn2CppTypeInfo* ti);
int32_t dn2cpp_prim_storage_width(int32_t code);
Dn2CppObject* dn2cpp_array_box_element(const Dn2CppTypeInfo* elem, const void* src,
                                       int32_t elemSize, bool elemIsRef);
void dn2cpp_array_store_boxed(Dn2CppObject* v, const Dn2CppTypeInfo* elem,
                              void* dst, int32_t elemSize, bool elemIsRef);

// Non-generic System.Array reflection surface. GetValue/SetValue dispatch on
// the receiver's runtime type-info (any SZ rep or an MD array) and follow real
// .NET's coercion rules — see dn2cpp_array_store_boxed above.
// CreateInstance allocates under the element's storage rep with a precise
// (fabricated-and-interned when never statically instantiated) array identity;
// the *_dyn shape queries serve receivers whose static type degraded to
// System.Array/object. dn2cpp_mdarr_ti is the interned identity a `new T[,]`
// site stamps on its Dn2CppMDArray (null element keeps the legacy null header).
Dn2CppObject* dn2cpp_array_get_value(Dn2CppObject* a, int64_t index);
void dn2cpp_array_set_value(Dn2CppObject* a, Dn2CppObject* value, int64_t index);
Dn2CppObject* dn2cpp_array_get_value_indices(Dn2CppObject* a, Dn2CppObject* indices, int32_t isLong);
void dn2cpp_array_set_value_indices(Dn2CppObject* a, Dn2CppObject* value, Dn2CppObject* indices, int32_t isLong);
Dn2CppObject* dn2cpp_array_get_value_fixed(Dn2CppObject* a, const int32_t* idx, int32_t n);
void dn2cpp_array_set_value_fixed(Dn2CppObject* a, Dn2CppObject* value, const int32_t* idx, int32_t n);
Dn2CppObject* dn2cpp_array_create_instance(Dn2CppType* t, const int32_t* lengths, int32_t rank);
Dn2CppObject* dn2cpp_array_create_instance_lengths(Dn2CppType* t, Dn2CppArrayI4* lengths,
                                                   Dn2CppArrayI4* lowerBounds);
Dn2CppObject* dn2cpp_array_create_instance_from_arraytype(Dn2CppType* arrayType,
                                                          const int32_t* lengths, int32_t rank);
const Dn2CppTypeInfo* dn2cpp_array_ti(const Dn2CppTypeInfo* elem, int32_t rank);
// The registry's SZ-array type-info over `elem`, or null when the image never
// statically instantiated T[] (defined in the reflection unit's registry-scan
// cluster; dn2cpp_array_ti falls back to a fabricated identity on a miss).
const Dn2CppTypeInfo* dn2cpp_find_array_ti(const Dn2CppTypeInfo* elem);
const Dn2CppTypeInfo* dn2cpp_find_array_ti_rank(const Dn2CppTypeInfo* elem, int32_t rank);
const Dn2CppTypeInfo* dn2cpp_mdarr_ti(const Dn2CppTypeInfo* elem, int32_t rank);
int32_t dn2cpp_array_rank_dyn(Dn2CppObject* a);
int32_t dn2cpp_array_length_dyn(Dn2CppObject* a);
int32_t dn2cpp_array_get_length_dyn(Dn2CppObject* a, int32_t dim);
int32_t dn2cpp_array_get_lower_bound_dyn(Dn2CppObject* a, int32_t dim);
int32_t dn2cpp_array_get_upper_bound_dyn(Dn2CppObject* a, int32_t dim);
void dn2cpp_array_reverse_dyn(Dn2CppObject* a, int32_t index, int32_t length);

// Reflection custom attributes. One Dn2CppAttrInfo per attribute applied to a
// reflected element (type/field/method/ctor/property/parameter). `create` is a
// CppEmitter-generated factory that allocates the attribute instance, runs its ctor with
// the (constant) positional args, and sets the named field/property args — returning a
// fresh instance on each call, matching .NET's GetCustomAttributes semantics. Only
// app-module attribute types with a renderable shape are emitted (BCL/compiler attributes
// and unsupported arg shapes are not reflected, an IL2CPP-managed-stripping-style bound).
struct Dn2CppAttrInfo
{
    const Dn2CppTypeInfo* attrType;
    Dn2CppObject* (*create)();
};

// MemberInfo/ParameterInfo.GetCustomAttributes([attrType,] inherit) and
// IsDefined(attrType, inherit). `member` is the reflected handle (a Dn2CppType,
// Dn2CppFieldRef, Dn2CppMethodRef, Dn2CppPropRef or Dn2CppParamRef — dispatched on its
// object header). A null attrType returns all attributes; otherwise the result is
// filtered to attributes assignable to attrType (its instance is-a attrType). The
// `inherit` flag is ignored (attributes are reported on the declaring element only —
// AttributeUsage inheritance is a carve-out).
Dn2CppArrayRef* dn2cpp_get_custom_attributes(Dn2CppObject* member, Dn2CppType* attrType);
// Typed-filter array form (GetCustomAttributes(Type) / GetCustomAttributes<T>()): a null
// attrType means the named type isn't in the reachable set, so it matches nothing and the
// result is empty — unlike the no-argument form above, where null means "all".
Dn2CppArrayRef* dn2cpp_get_custom_attributes_typed(Dn2CppObject* member, Dn2CppType* attrType);
// Single-attribute form (Attribute.GetCustomAttribute / CustomAttributeExtensions
// .GetCustomAttribute<T>): returns the lone matching attribute instance, nullptr when
// none match, and throws AmbiguousMatchException when more than one matches —
// matching .NET's single-attribute contract.
Dn2CppObject* dn2cpp_get_custom_attribute(Dn2CppObject* member, Dn2CppType* attrType);
int32_t dn2cpp_is_defined(Dn2CppObject* member, Dn2CppType* attrType);

// One embedded manifest resource of an assembly: its manifest name (UTF-8, exactly the
// string the ManifestResource metadata row carries) and the raw payload bytes, emitted
// into .rodata beside the assembly registry. The payload is opaque — a .resources set,
// a text file and a binary asset are all just bytes here; interpreting one is the
// caller's business, as in real .NET.
struct Dn2CppManifestResource
{
    const char* name;
    const uint8_t* data;   // null when length is 0 (a zero-size C++ array is ill-formed)
    int32_t length;
};

// Assembly registry: one entry per loaded module (app + -r references), keyed by
// the assembly simple name — the same `const char*` handle System.Reflection.Assembly
// is modeled as (Type.Assembly / Assembly.GetEntryAssembly; equality is a name
// compare). CppEmitter emits the table (`dn2cpp_assembly_registry` / `_count` in
// generated.cpp); customAttrs carries the module's reflectable assembly-level custom
// attributes (same Dn2CppAttrInfo encoding/bounds as the member tables above).
struct Dn2CppAssemblyRegEntry
{
    const char* name;                 // assembly simple name (the Assembly handle)
    const Dn2CppAttrInfo* customAttrs;
    int32_t customAttrCount;
    // Assembly identity pieces read out of the module's metadata (the same
    // trailing-member 0-fill convention as Dn2CppTypeInfo): version is
    // "Major.Minor.Build.Revision", culture the metadata culture ("neutral" when
    // empty), publicKeyToken the 16-hex-char token of the assembly's public key
    // (null when unsigned — displayed as "null"). They compose the .NET display
    // name (Assembly.FullName / Type.AssemblyQualifiedName / AssemblyName).
    const char* version;
    const char* culture;
    const char* publicKeyToken;
    // The module's EMBEDDED manifest resources, in metadata order — what
    // Assembly.GetManifestResourceNames reports and GetManifestResourceStream reads.
    // Emitted only when some emitted body actually reads a manifest resource, so a
    // program that reads none pays nothing (a framework closure carries a few hundred
    // KB of raw bytes). A null/0 pair therefore conflates "no resource table emitted"
    // with "this assembly has no resources" — sound only because the two agree on
    // every answer a program in that state can ask for.
    const Dn2CppManifestResource* resources;
    int32_t resourceCount;
    // Nonzero when this assembly's embedded manifest resources were DROPPED at
    // transpile time (--no-manifest-resources <Assembly>) — the
    // DN2CPP_TF_METADATA_STRIPPED pattern one level up: from the table alone a dropped
    // assembly and one that genuinely carries no resources both read (nullptr, 0), so
    // the alternative to this bit is a silent lie. With it set, a query that MISSES
    // the table throws a catchable PlatformNotSupportedException naming the assembly
    // and the remedy; a HIT (a --manifest-resource-root kept the resource) still
    // answers truthfully. GetManifestResourceNames throws outright — any list it could
    // return is incomplete.
    int32_t resourcesDropped;
    // [assembly: NeutralResourcesLanguage(...)], read off the module's metadata at
    // transpile time. `neutralResourcesCulture` is the declared culture name, null when
    // the assembly declares the attribute not at all; `neutralResourcesSatellite` is 1
    // for UltimateResourceFallbackLocation.Satellite.
    //
    // The ONE piece of culture information a ResourceManager lookup can act on without
    // a satellite loader, and both directions matter. A request naming EXACTLY this
    // culture is answered from the main assembly's own blob, reproducing real .NET's
    // ManifestBasedResourceGroveler, which rewrites that ask to the invariant one and
    // never probes a satellite for it. Satellite says the opposite — the main
    // assembly's copy is NOT the fallback, every ask reads a satellite — so serving the
    // blob there would be a silent wrong answer. Emitted only alongside the resource
    // table, since a program that reads no manifest resource cannot ask.
    const char* neutralResourcesCulture;
    int32_t neutralResourcesSatellite;
};
extern const Dn2CppAssemblyRegEntry dn2cpp_assembly_registry[];
extern const int32_t dn2cpp_assembly_registry_count;
// Assembly.GetCustomAttributes([attrType,] inherit) / GetCustomAttribute / IsDefined
// over the registry entry named by the handle. Same filter semantics as the member
// forms: the no-type form passes a null attrType meaning "all"; the _typed form maps
// a null attrType (type absent from the reachable set) to an empty result. An unknown
// or null assembly handle has no attributes.
Dn2CppArrayRef* dn2cpp_assembly_get_custom_attributes(const char* name, Dn2CppType* attrType);
Dn2CppArrayRef* dn2cpp_assembly_get_custom_attributes_typed(const char* name, Dn2CppType* attrType);
Dn2CppObject* dn2cpp_assembly_get_custom_attribute(const char* name, Dn2CppType* attrType);
int32_t dn2cpp_assembly_is_defined(const char* name, Dn2CppType* attrType);
// Assembly.GetManifestResourceNames(): the embedded resource names, in metadata order
// (a string[]; empty when the assembly carries none). On a resourcesDropped assembly it
// throws the catchable PlatformNotSupportedException (see the struct member above).
Dn2CppArrayRef* dn2cpp_assembly_get_manifest_resource_names(const char* name);
// Assembly.GetManifestResourceStream(...): the named resource's bytes as a byte[] the
// caller wraps in a MemoryStream, or null when there is no such resource (real .NET's
// answer too) — unless the assembly is resourcesDropped, in which case a miss throws
// instead of answering the null that would mean "missing" (a --manifest-resource-root
// kept resource still hits and answers). `scope` is the optional Type of the
// (Type, string) overload, whose namespace prefixes the name; null for the plain
// (string) overload. A null resource
// name throws ArgumentNullException, an empty one ArgumentException — both as .NET.
Dn2CppArrayN* dn2cpp_assembly_get_manifest_resource_bytes(const char* name, Dn2CppType* scope,
    Dn2CppString* resourceName, const Dn2CppTypeInfo* byteArrayType);
// Assembly.GetManifestResourceInfo(name): whether the assembly carries the named
// embedded resource — the emit arm turns 1 into a ManifestResourceInfo(assembly=null,
// fileName=null, Embedded|ContainedInManifestFile) and 0 into null, as .NET does.
// Same resourcesDropped posture as the stream read: a miss on a dropped assembly throws.
int32_t dn2cpp_assembly_has_manifest_resource(const char* name, Dn2CppString* resourceName);
// The two halves of the manifest-resource read above, exported for the System.Resources
// unit so ResourceManager's lookup goes through EXACTLY the same table walk and the
// same dropped-bit refusal Assembly.GetManifestResourceStream does. A second private
// copy would be a second place for the honesty check to drift out of, and the drift is
// silent: a ResourceManager answering null off a dropped assembly reads as "no such
// key", the lie the bit exists to prevent.
const Dn2CppManifestResource* dn2cpp_assembly_manifest_resource(const char* asmName,
    const char* key);
void dn2cpp_assembly_require_manifest_resources(const char* asmName, const char* api);
// The assembly's declared neutral-resources culture (null when it declares none), with
// *satelliteOut set to 1 for UltimateResourceFallbackLocation.Satellite. Exported from
// the same unit and for the same reason as the two above: one registry walk, not a copy.
const char* dn2cpp_assembly_neutral_resources_culture(const char* asmName,
    int32_t* satelliteOut);
// Assembly.FullName — the .NET display name "Name, Version=a.b.c.d,
// Culture=xx, PublicKeyToken=hhhh…" composed from the registry entry (an
// assembly absent from the registry falls back to "Version=0.0.0.0,
// Culture=neutral, PublicKeyToken=null" after its simple name). Also feeds
// Assembly.GetName() (parsed back through the transpiled AssemblyName(string)
// ctor) and Type.AssemblyQualifiedName below.
Dn2CppString* dn2cpp_assembly_full_name(const char* name);
// Type.AssemblyQualifiedName — "FullName, <assembly display name>". Matches
// real .NET for non-generic types; a constructed generic prints the type-info's
// FullName form (INTENTIONAL DIVERGENCE: real .NET assembly-qualifies each
// type argument inside the brackets).
Dn2CppString* dn2cpp_type_assembly_qualified_name(Dn2CppType* t);
// Assembly.GetType(name[, throwOnError[, ignoreCase]]): the type-name registry
// lookup restricted to the receiver assembly's own types (dn2cpp_type_get_by_name
// resolves across every loaded assembly). A trailing assembly-qualified suffix
// (after the first ',') is ignored like dn2cpp_type_get_by_name; ignoreCase
// compares ASCII case-insensitively (CLR identifiers). A miss returns null, or
// throws TypeLoadException when throwOnError.
Dn2CppType* dn2cpp_assembly_get_type(const char* asmName, Dn2CppString* name,
    int32_t throwOnError, int32_t ignoreCase);
// Assembly.GetModules() — dn2cpp assemblies are single-module, so a one-element
// Module[] holding the manifest module (the same simple-name handle Assembly is).
Dn2CppArrayRef* dn2cpp_assembly_get_modules(const char* name);
// Assembly.Load(String/AssemblyName) / LoadWithPartialName(String): name-based
// assembly resolution is a lookup over the linked-assembly registry — an AOT
// image has every assembly it can ever "load" statically linked. The request's
// simple-name part (cut at the first ',', whitespace-trimmed) matches a
// registry name ASCII case-insensitively; a display name's Version/Culture/
// PublicKeyToken are ignored (see the implementation note). A miss follows real
// .NET: dn2cpp_assembly_load throws FileNotFoundException, the obsolete
// _load_partial returns null. A null name throws ArgumentNullException, an
// empty/blank one ArgumentException (both measured against real .NET).
const char* dn2cpp_assembly_load(Dn2CppString* name);
const char* dn2cpp_assembly_load_partial(Dn2CppString* name);
// Module.Name / ToString — "<AssemblyName>.dll", the manifest module's file name
// (matching real .NET; Module.FullyQualifiedName also maps here — INTENTIONAL
// DIVERGENCE: real .NET reports the full on-disk path, which a self-hosted
// native binary does not have).
Dn2CppString* dn2cpp_module_name(const char* name);

// A reflected System.Reflection.CustomAttributeData handle: wraps one attribute
// row of an element's attribute table. Only AttributeType is modeled (the
// declarative view's arguments would need the encoded ctor blob, which the
// tables don't carry); ConstructorArguments/NamedArguments throw the catchable
// PlatformNotSupportedException at the call site.
struct Dn2CppAttrDataRef : Dn2CppObject
{
    const Dn2CppAttrInfo* attr;
};
extern const Dn2CppTypeInfo dn2cpp_customattributedata_type;
// MemberInfo/ParameterInfo/Type.CustomAttributes / GetCustomAttributesData():
// the element's attribute rows as CustomAttributeData handles (dispatched on the
// member handle's object header, like dn2cpp_get_custom_attributes); the
// assembly form serves Assembly.CustomAttributes via the assembly registry.
Dn2CppArrayRef* dn2cpp_member_custom_attributes_data(Dn2CppObject* member);
Dn2CppArrayRef* dn2cpp_assembly_custom_attributes_data(const char* name);
// CustomAttributeData.AttributeType.
Dn2CppType* dn2cpp_attrdata_attribute_type(Dn2CppAttrDataRef* d);

// ---- reflection long-tail (flags/properties over the raw ECMA words) ----

// Type.Attributes: the raw ECMA TypeAttributes word. Emitted user types/enums
// carry the real word (ilAttrs); hand-written/synthetic type-infos synthesize a
// best-effort word from the flags bits (visibility Public — every hand-written
// handle is a public BCL type — plus Sealed/Interface/Abstract).
int32_t dn2cpp_type_il_attrs(const Dn2CppTypeInfo* ti);
int32_t dn2cpp_type_is_public(const Dn2CppTypeInfo* ti);
int32_t dn2cpp_type_is_not_public(const Dn2CppTypeInfo* ti);
int32_t dn2cpp_type_is_visible(const Dn2CppTypeInfo* ti);
// Type.HasElementType / GetRootElementType: only array Types materialize with
// an element in this model (no byref/pointer Type objects exist at runtime).
int32_t dn2cpp_type_has_element_type(const Dn2CppTypeInfo* ti);
Dn2CppType* dn2cpp_type_get_root_element_type(Dn2CppType* t);
// Type.FormatTypeName (internal; MethodInfo/ConstructorInfo.ToString's type
// rendering): the full name, with the "System." prefix stripped when the root
// element type is primitive/void, or the simple name for a nested root.
Dn2CppString* dn2cpp_type_format_type_name(Dn2CppType* t);
// Type.MakeArrayType(): the SZ-array Type for this element if the AOT image
// contains it (registry scan); an array type never statically instantiated
// throws NotSupportedException — the same AOT boundary as MakeGenericType.
Dn2CppType* dn2cpp_type_make_array_type(Dn2CppType* t);
// Type.GetEnumValuesAsUnderlyingType(): the declared constants as an array of
// the enum's underlying primitive (byte[]/short[]/int[]/long[]/... by width).
Dn2CppObject* dn2cpp_type_get_enum_values_as_underlying(Dn2CppType* t);
// Type.FindInterfaces(TypeFilter, object): GetInterfaces() filtered through the
// managed predicate delegate. Type.ImplementInterface(Type) (internal): whether
// the type's base-chain interface tables contain the interface.
Dn2CppArrayRef* dn2cpp_type_find_interfaces(Dn2CppType* t, Dn2CppObject* filter, Dn2CppObject* criteria);
int32_t dn2cpp_type_implement_interface(Dn2CppType* t, Dn2CppType* itf);
// MemberInfo.MemberType/MetadataToken/Module — dispatch on the handle's object
// header like dn2cpp_memberinfo_name. Module is modeled as the defining
// assembly's simple-name handle (dn2cpp is single-module per assembly).
int32_t dn2cpp_memberinfo_member_type(Dn2CppObject* m);
int32_t dn2cpp_memberinfo_metadata_token(Dn2CppObject* m);
const char* dn2cpp_memberinfo_module(Dn2CppObject* m);
// MethodBase/MethodInfo raw-word predicates + accessors.
int32_t dn2cpp_methodref_attributes(Dn2CppMethodRef* m);
int32_t dn2cpp_methodref_impl_flags(Dn2CppMethodRef* m);
int32_t dn2cpp_methodref_is_virtual(Dn2CppMethodRef* m);
int32_t dn2cpp_methodref_is_abstract(Dn2CppMethodRef* m);
int32_t dn2cpp_methodref_is_final(Dn2CppMethodRef* m);
int32_t dn2cpp_methodref_is_constructor(Dn2CppMethodRef* m);
int32_t dn2cpp_methodref_is_generic(Dn2CppMethodRef* m);
// MethodBase.GetParameterTypes (internal): the parameter types as a Type[].
Dn2CppArrayRef* dn2cpp_methodref_get_parameter_types(Dn2CppMethodRef* m);
// The generic-method dimension over per-closed-instantiation rows (see the
// Dn2CppMethodRef::isGenericDefView note for the definition-view model).
// MakeGenericMethod resolves within the image: rows on the declaring type
// sharing the receiver's definition metadata token are matched argument-wise
// against the requested Type[]; an instantiation the transpile never reached
// throws a catchable PlatformNotSupportedException (the AOT boundary).
Dn2CppMethodRef* dn2cpp_methodref_make_generic(Dn2CppMethodRef* m, Dn2CppArrayRef* types);
Dn2CppMethodRef* dn2cpp_methodref_get_generic_definition(Dn2CppMethodRef* m);
Dn2CppArrayRef* dn2cpp_methodref_get_generic_arguments(Dn2CppMethodRef* m);
int32_t dn2cpp_methodref_is_generic_def(Dn2CppMethodRef* m);
// MethodInfo.GetBaseDefinition: the shallowest base-chain ancestor declaring a
// row on the receiver's vtable slot (a non-virtual method returns itself).
Dn2CppMethodRef* dn2cpp_methodref_get_base_definition(Dn2CppMethodRef* m);
// FieldInfo raw-word accessors.
int32_t dn2cpp_fieldref_attributes(Dn2CppFieldRef* f);
int32_t dn2cpp_fieldref_is_private(Dn2CppFieldRef* f);
int32_t dn2cpp_fieldref_is_specialname(Dn2CppFieldRef* f);
// ParameterInfo.Member/Attributes/IsOptional.
Dn2CppObject* dn2cpp_paramref_member(Dn2CppParamRef* p);
int32_t dn2cpp_paramref_attributes(Dn2CppParamRef* p);
int32_t dn2cpp_paramref_is_optional(Dn2CppParamRef* p);
Dn2CppArrayRef* dn2cpp_paramref_custom_modifiers(Dn2CppParamRef* p, int32_t required);
// Enum.InternalGetCorElementType: the CorElementType code of the boxed enum's
// underlying primitive (read from the type-info's enumUnderlying handle).
int32_t dn2cpp_enum_cor_element_type(Dn2CppObject* e);
// Enum.GetValue(): boxes the enum's underlying primitive value under the
// underlying primitive's type-info (Byte/Int16/Int32/.../UInt64), matching what
// `box <underlying>` emits. Backs the IConvertible.To* overrides (which hand the
// box to Convert.ToXxx).
Dn2CppObject* dn2cpp_enum_get_value(Dn2CppObject* e);
// RuntimeHelpers.AllocateTypeAssociatedMemory(Type, int): zero-initialized,
// GC-visible (scanned), never-collected memory — dn2cpp_alloc_pinned semantics.
void* dn2cpp_alloc_type_associated(Dn2CppType* t, int32_t size);

// Built-in type metadata (defined in dn2cpp_typeinfo.cpp).
extern const Dn2CppTypeInfo dn2cpp_object_type;
Dn2CppObject* dn2cpp_default_equality_comparer(const Dn2CppTypeInfo* comparerType,
                                               const Dn2CppTypeInfo* genericInterface,
                                               const Dn2CppTypeInfo* nongenericInterface);
// Inline and lock-free — the per-element comparer dispatch is its caller, and a
// separate translation unit would put a call there.
static inline int32_t dn2cpp_is_default_equality_comparer(const Dn2CppObject* obj)
{
    return obj != nullptr && (obj->type->flags & DN2CPP_TF_DEFAULT_EQ_COMPARER) != 0;
}
// Non-const: generated code registers String's interface-dispatch map onto it at
// startup (dn2cpp_string_set_interfaces) — the interface set and its slot
// implementations are program-specific (tree-shaken CoreLib IL), so the runtime
// cannot bake them in.
extern Dn2CppTypeInfo dn2cpp_string_type;
// Installs String's interface-dispatch map (IEnumerable<char>/IComparable/… rows
// emitted by the transpiler). Called once from the generated init prologue before
// any managed code runs.
void dn2cpp_string_set_interfaces(const Dn2CppInterfaceEntry* entries, int32_t count);
// Installs the shared reference-element SZArray fallback dispatch table: the
// object-element interface map (IEnumerable<object>/IList<object>/… + the non-generic
// trio, each thunk wrapping the array into a fresh SZArrayEnumerable<object>).
// dn2cpp_resolve_interface_walk consults it for a rank-1 REFERENCE-element array whose
// own per-element map lacks the requested row, so a collection-interface CALL on an
// array reached through `object` dispatches instead of aborting. Sound because
// reference elements share one C++ layout; VALUE-element arrays are never served here
// (their layouts differ per element) and instead carry an eagerly-wired per-element
// map, or keep the loud abort when they have no emitted ti_ to attach one to.
// Called once from the generated init prologue before any managed code runs.
void dn2cpp_array_set_ref_fallback_interfaces(const Dn2CppInterfaceEntry* entries, int32_t count);
// Installs the multi-dimensional (rank >= 2) array dispatch table: the map for the six
// non-generic interfaces a CLR MD array implements (IEnumerable/ICollection/IList/
// ICloneable/IStructuralComparable/IStructuralEquatable), each thunk wrapping the
// receiver into a fresh Dn2Cpp.Runtime.MDArrayEnumerable. MD type-infos are
// runtime-interned with no interface rows of their own, so
// dn2cpp_resolve_interface_walk serves ANY rank>=2 array from this one table —
// element-agnostic (the wrapper goes through the System.Array reflection surface),
// unlike the per-element SZ maps. A program that mints no MD array installs nothing
// and an MD interface dispatch keeps the loud abort. Called once from the generated
// init prologue before any managed code runs.
void dn2cpp_array_set_md_fallback_interfaces(const Dn2CppInterfaceEntry* entries, int32_t count);
// Installs the RELATION-only row set for the six non-generic interfaces every CLR
// array implements — the same six the two tables above dispatch, but as rows with
// NULL slot tables, and for ENUMERATION rather than dispatch.
//
// The distinction is the whole point: `is IList` answers by flag
// (DN2CPP_TF_ARRAY_ITF) and a CALL dispatches through the maps above, but neither
// reaches Type.GetInterfaces(), which reads `ti->interfaces` and finds only what some
// emitted table happens to carry. A per-element SZArray map carries the three the
// SZArrayEnumerable<T> wrapper implements and structurally cannot carry
// ICloneable/IStructuralComparable/IStructuralEquatable; an MD array's runtime-interned
// type-info carries no rows at all.
//
// An SZArray whose element the eager value-map loop must SKIP gets no dispatch map;
// its ti_arr_ instead carries the five generic collection interfaces as per-element
// relation-only rows of this same nullptr-slot shape, so enumeration answers .NET's
// list while a dispatch through one keeps the loud "has no map" abort.
//
// One element-agnostic table serves both, because the six are non-generic. The
// nullptr slots are what make installing them safe beside the dispatch tables: every
// dispatch reader already guards on `slots != nullptr`, so these rows are invisible to
// dispatch and cannot shadow the maps above. Only the three ENUMERATING readers
// (GetInterfaces / GetInterface / FindInterfaces) consult them, through
// dn2cpp_array_nongeneric_interfaces below, each de-duplicating against the type's own
// rows so an SZArray map's IEnumerable/ICollection/IList are not reported twice.
//
// A program with no array type-info installs nothing and every reader is unchanged.
// Called once from the generated init prologue before any managed code runs.
void dn2cpp_array_set_nongeneric_interfaces(const Dn2CppInterfaceEntry* entries, int32_t count);
// The installed relation-only array rows, or nullptr when none were installed.
// Returns them only for a type-info carrying DN2CPP_TF_ARRAY or DN2CPP_TF_SYSTEM_ARRAY
// (the abstract System.Array class, whose real-.NET interface list IS exactly these six,
// and which is an intrinsic type so the emitter gives it no table of its own), so a
// caller may ask about any type; *count receives the row count (0 when null).
const Dn2CppInterfaceEntry* dn2cpp_array_nongeneric_interfaces(
    const Dn2CppTypeInfo* ti, int32_t* count);
// Installs an interface-dispatch map onto an INTRINSIC type's runtime-owned type-info.
// An intrinsic type has no per-class emitted type-info to carry a map, so without this
// every interface mouth — `using (…)`, an interface-typed local, an isinst/castclass —
// hits dn2cpp_resolve_interface's "has no map" abort while the direct call works. The
// rows point at program-specific emitted type-info, which the runtime cannot name
// statically, so the generated init prologue calls this once per mapped intrinsic
// before any managed code runs. Each slot the emitter bakes in must be a thunk of the
// interface method's EXACT signature: an int32_t-returning helper entered through a
// void fnptr is a wasm signature trap.
void dn2cpp_intrinsic_set_interfaces(Dn2CppTypeInfo* type, const Dn2CppInterfaceEntry* entries, int32_t count);
// The mapped intrinsics' type-info handles, named by the generated init prologue. Each
// is defined beside its own runtime helpers (dn2cpp_threading.cpp and the System.Threading
// / System.Collections.Concurrent intrinsic units) and is non-const for this reason
// alone. The transpiler-side table
// that pairs them with an interface and a slot thunk is Compilation.IntrinsicInterfaceRows.
extern Dn2CppTypeInfo dn2cpp_timer_type;
extern Dn2CppTypeInfo dn2cpp_timeprovider_timer_type;
extern Dn2CppTypeInfo dn2cpp_countdown_type;
extern Dn2CppTypeInfo dn2cpp_barrier_type;
extern Dn2CppTypeInfo dn2cpp_rwlock_type;
extern Dn2CppTypeInfo dn2cpp_threadlocal_type;
extern Dn2CppTypeInfo dn2cpp_blockingcollection_type;
// The rest of the runtime-allocated threading objects' handles. They need no interface
// wiring, but the emitter must be able to NAME them: an instance carries the handle, so
// a `ti_System_Threading_SemaphoreSlim` emitted beside one would be a second type-info
// for one CLR type — and a reflected member typed at one (a Stream's
// `_asyncActiveSemaphore` field row) is exactly where the emitter has to spell it.
// Kept const: nothing binds emitted metadata into them.
extern const Dn2CppTypeInfo dn2cpp_thread_type;
extern const Dn2CppTypeInfo dn2cpp_semaphore_type;
extern const Dn2CppTypeInfo dn2cpp_cancel_source_type;
extern const Dn2CppTypeInfo dn2cpp_parallel_loop_state_type;
extern const Dn2CppTypeInfo dn2cpp_parallel_options_type;
// The event family: four CLR types over one Dn2CppEvent, on their REAL base chain —
// ManualResetEvent and AutoResetEvent are sealed siblings under EventWaitHandle,
// EventWaitHandle is under WaitHandle, and ManualResetEventSlim is not a WaitHandle at
// all. The newobj lowering picks one and dn2cpp_event_new stamps it, so each answers
// exactly rather than sharing one over-accepting handle.
extern const Dn2CppTypeInfo dn2cpp_waithandle_type;
extern const Dn2CppTypeInfo dn2cpp_event_type;
extern const Dn2CppTypeInfo dn2cpp_manualresetevent_type;
extern const Dn2CppTypeInfo dn2cpp_autoresetevent_type;
extern const Dn2CppTypeInfo dn2cpp_manualreseteventslim_type;
extern const Dn2CppTypeInfo dn2cpp_array_i4_type;
extern const Dn2CppTypeInfo dn2cpp_array_ref_type;
extern const Dn2CppTypeInfo dn2cpp_array_n_type;
extern const Dn2CppTypeInfo dn2cpp_bool_type;
extern const Dn2CppTypeInfo dn2cpp_char_type;
extern const Dn2CppTypeInfo dn2cpp_byte_type;
extern const Dn2CppTypeInfo dn2cpp_sbyte_type;
extern const Dn2CppTypeInfo dn2cpp_int16_type;
extern const Dn2CppTypeInfo dn2cpp_uint16_type;
extern const Dn2CppTypeInfo dn2cpp_uint32_type;
extern const Dn2CppTypeInfo dn2cpp_int32_type;
extern const Dn2CppTypeInfo dn2cpp_int64_type;
extern const Dn2CppTypeInfo dn2cpp_uint64_type;
// IntPtr/UIntPtr's boxed type-info. Their payload is an 8-byte intptr_t (CppTypes.Of),
// so they read like int64/uint64; the signedness lives in the type-info so a boxed
// IntPtr ToStrings signed and UIntPtr unsigned.
extern const Dn2CppTypeInfo dn2cpp_intptr_type;
extern const Dn2CppTypeInfo dn2cpp_uintptr_type;
extern const Dn2CppTypeInfo dn2cpp_single_type;
extern const Dn2CppTypeInfo dn2cpp_double_type;
// System.Decimal's boxed type-info. Decimal is an intrinsic value
// type (Dn2CppDecimal), so unlike a user struct it has no emitted ti_* — boxing it
// (object o = someDecimal;) names this shared handle, and dn2cpp_object_tostring/
// _equals/_gethashcode read the Dn2CppDecimal payload back from it.
extern const Dn2CppTypeInfo dn2cpp_decimal_type;
// Boxed type-infos for the intrinsic Task-family awaiter structs. A real
// (un-adopted) async builder's AwaitUnsafeOnCompleted IL carries Roslyn's
// `(object)default(TAwaiter) != null` value-type probe, whose `box TAwaiter`
// names the awaiter's type-info even though the branch itself folds. Every
// awaiter that lowers to Dn2CppTaskAwaiter (TaskAwaiter(<T>), ValueTaskAwaiter(<T>),
// the Configured* forms) shares the first handle; YieldAwaiter the second.
extern const Dn2CppTypeInfo dn2cpp_task_awaiter_type;
extern const Dn2CppTypeInfo dn2cpp_yield_awaiter_type;
extern const Dn2CppTypeInfo dn2cpp_type_type;
extern const Dn2CppTypeInfo dn2cpp_exception_type;
// The fixed set of exception types the runtime *itself* raises (the trap helpers
// below + the File I/O / cast paths). Each carries a stable handle (base-chained to
// dn2cpp_exception_type, with the common intermediate ArgumentException/IOException
// preserved) so the EMITTED code can reference the SAME symbol the runtime stamps —
// a typed `catch`/`is` against one of these resolves to its handle here and matches
// both a runtime-trapped object and a managed `new` of the type (the two
// sources must share one type-info, else `is`/catch only match one of them). User
// exception types (and BCL exceptions reached only via managed `newobj`, e.g.
// BadImageFormatException) have no runtime trap and keep their emitted ti_*.
//
// Each is MUTABLE, and starts out a stub: a name and a base, no vtable, no instance
// size, no reflection tables. The runtime cannot know those — a type's vtable and its
// layout are properties of the transpiled program — so the generated code copies the
// metadata it emitted for the type INTO this handle at startup (dn2cpp_type_binds
// below, applied by dn2cpp_runtime_init). The alternative, an emitted ti_ beside this
// one, would be two type-infos for one managed type, and then which of them an object
// happened to carry would decide whether a `catch` saw it.
extern Dn2CppTypeInfo dn2cpp_overflow_exception_type;
extern Dn2CppTypeInfo dn2cpp_index_out_of_range_exception_type;
extern Dn2CppTypeInfo dn2cpp_argument_exception_type;
extern Dn2CppTypeInfo dn2cpp_argument_out_of_range_exception_type;
extern Dn2CppTypeInfo dn2cpp_argument_null_exception_type;
extern Dn2CppTypeInfo dn2cpp_invalid_operation_exception_type;
// System.ObjectDisposedException. Derives from InvalidOperationException, as in
// .NET — so `catch (InvalidOperationException)` still catches it, while a
// `catch (ObjectDisposedException)` (the one a caller of a disposed stream,
// file handle or CancellationTokenSource actually writes) finally matches.
extern Dn2CppTypeInfo dn2cpp_object_disposed_exception_type;
extern Dn2CppTypeInfo dn2cpp_arithmetic_exception_type;
// System.OutOfMemoryException. Raised ONLY where a size computation overflows before
// anything has been asked of the allocator. Deliberately NOT what the allocation
// failures in dn2cpp_gc.cpp raise: an exception object needs an allocation, so the one
// place a managed OutOfMemoryException cannot be thrown is where the allocator has just
// said no. Those stay dn2cpp_fail aborts.
extern Dn2CppTypeInfo dn2cpp_out_of_memory_exception_type;
extern Dn2CppTypeInfo dn2cpp_invalid_cast_exception_type;
extern Dn2CppTypeInfo dn2cpp_type_load_exception_type;
extern Dn2CppTypeInfo dn2cpp_not_supported_exception_type;
extern Dn2CppTypeInfo dn2cpp_platform_not_supported_exception_type;
extern Dn2CppTypeInfo dn2cpp_format_exception_type;
extern Dn2CppTypeInfo dn2cpp_io_exception_type;
extern Dn2CppTypeInfo dn2cpp_file_not_found_exception_type;
// System.IO.PathTooLongException. The Windows Path.GetFullPath arm raises it for
// the one Win32 error .NET's Win32Marshal maps to it; it derives from IOException
// there as here, so `catch (IOException)` matches either way.
extern Dn2CppTypeInfo dn2cpp_path_too_long_exception_type;
extern Dn2CppTypeInfo dn2cpp_unauthorized_access_exception_type;
extern Dn2CppTypeInfo dn2cpp_key_not_found_exception_type;
// System.RankException: raised by the array block-move helpers when the two
// operands' ranks disagree. The SZ and MD layouts share no field, so the pair
// cannot be moved at all — and .NET's answer is this type, not an Argument one.
extern Dn2CppTypeInfo dn2cpp_rank_exception_type;
// System.ArrayTypeMismatchException: raised by the array block-move helpers when
// the two operands' element types satisfy no arm of the CLR's Array.Copy
// compatibility verdict (which lives at dn2cpp_array_copy_checked).
extern Dn2CppTypeInfo dn2cpp_array_type_mismatch_exception_type;
// System.Reflection.AmbiguousMatchException: raised by the member-lookup
// helpers (GetMethod/GetProperty with several undecidable matches), matching
// real .NET's reflection contract.
extern Dn2CppTypeInfo dn2cpp_ambiguous_match_exception_type;
// System.MissingMethodException: raised by the Activator/ConstructorInfo
// helpers when constructor resolution finds no invokable match.
extern Dn2CppTypeInfo dn2cpp_missing_method_exception_type;
// System.Resources.MissingManifestResourceException: raised by ResourceManager when
// the `<BaseName>.resources` set it was asked for is embedded in no loaded assembly.
// Based on Exception rather than SystemException, like every other runtime-raised type
// here (dn2cpp does not model SystemException); the divergence is that a `catch
// (SystemException)` does not catch it, as with NotSupportedException.
extern Dn2CppTypeInfo dn2cpp_missing_manifest_resource_exception_type;
// System.NullReferenceException: raised by runtime entry points handed a null
// managed receiver they would otherwise dereference (a null FieldInfo's
// GetValue — real .NET's instance call on a null handle), so the failure is a
// catchable managed throw, never a native crash.
extern Dn2CppTypeInfo dn2cpp_null_reference_exception_type;
// System.DivideByZeroException: raised by the emitted integer div/rem lowering
// (dn2cpp_div_i4 and friends) and by the interpreter's and decimal's own zero
// guards. Derives from ArithmeticException, as in .NET — a `catch
// (ArithmeticException)` around a parse loop must still catch it. Without the zero
// test `1/0` is whatever the hardware does (SIGFPE on x86-64, a SILENT 0 on arm64,
// a wasm trap): none of those is catchable and none names anything.
extern Dn2CppTypeInfo dn2cpp_divide_by_zero_exception_type;
// System.Threading.LockRecursionException: raised by ReaderWriterLockSlim's
// per-thread ownership checks — a same-thread re-entry the recursion policy
// forbids throws instead of deadlocking against itself, as in .NET.
extern Dn2CppTypeInfo dn2cpp_lock_recursion_exception_type;
// System.Threading.SynchronizationLockException: raised by ReaderWriterLockSlim's
// Exit* paths when the calling thread does not hold the lock it is releasing.
extern Dn2CppTypeInfo dn2cpp_synchronization_lock_exception_type;
// System.AggregateException (System.Exception-derived). Built — not runtime-trapped —
// by the Parallel exception-aggregation path; the handle is also the one a managed
// `new AggregateException` stamps (see CoreIntrinsics.RuntimeExceptionTypeInfo).
extern const Dn2CppTypeInfo dn2cpp_aggregate_exception_type;
// The shared base of every per-enum type-info. A boxed enum's runtime type
// is its own synthesized Dn2CppTypeInfo (so `o is DayOfWeek` discriminates between
// enums and from int), with this as its `base` — the runtime recognizes an enum by
// `t->base == &dn2cpp_enum_type` and formats/hashes the int32 payload accordingly.
// Non-const like dn2cpp_string_type: the boxed-enum interface-dispatch map is
// installed onto it at startup (dn2cpp_enum_set_interfaces) — one map serves every
// enum, because dn2cpp_resolve_interface_walk consults the base chain and the
// per-enum type-infos deliberately carry no rows of their own.
extern Dn2CppTypeInfo dn2cpp_enum_type;
// Installs the shared boxed-enum interface-dispatch map (IComparable/IFormattable/
// IConvertible/ISpanFormattable rows whose slots are System.Enum's transpiled impls —
// the model makes System.Enum a reference type, so they take the box pointer directly).
// Called once from the generated init prologue before any managed code runs.
void dn2cpp_enum_set_interfaces(const Dn2CppInterfaceEntry* entries, int32_t count);

/// Heap-allocates a T that is never destroyed, for a namespace-scope object the
/// detached finalizer thread or a Task.Run pool worker can touch. Those threads keep
/// running while the process tears its statics down, and locking a std::mutex whose
/// destructor already ran returns EINVAL, which libc++ throws as an uncaught
/// std::system_error. NOT a complete answer for a shared library: unloading one unmaps
/// the code those threads execute, which no data lifetime can save — only stopping
/// them can (dn2cpp_runtime_quiesce).
template <class T>
T& dn2cpp_never_destroyed()
{
    return *new T;
}

void dn2cpp_runtime_init();
// Stop and join the background threads the runtime owns — the Task.Run pool
// workers and (when it runs at all) the finalizer thread — so a host can
// safely dlclose the library: dn2cpp_never_destroyed keeps the data those
// threads lock alive, but nothing can save a thread executing code the unload
// unmaps. The stop is cooperative (each thread's GC registration must unwind
// through its own return), so it is bounded by timeout_ms (< 0 waits
// forever): a worker still inside managed code at the deadline is detached
// and -1 comes back — unloading is then unsafe. Otherwise returns the number
// of threads joined. Queued-but-unstarted pool items are dropped; their tasks
// never settle, and nothing that could observe them survives an unload.
// Idempotent, and after a clean quiesce a later submit or finalizable
// allocation restarts the threads. Threads the managed program owns — running
// System.Threading.Thread bodies, undisposed Timers — are its own to stop, as
// in .NET. extern "C": host-facing ABI, dlsym'd by dlopen hosts before they
// close the library.
extern "C" DN2CPP_RT_EXPORT int32_t dn2cpp_runtime_quiesce(int32_t timeout_ms);
// The exit path of a generated executable's `main`, and the lowering of
// Environment.Exit(code). Flushes stdio and terminates without running static
// destructors or the atexit chain: the detached finalizer thread and pool
// workers keep running through teardown, and destroying the mutexes they lock
// makes them abort. Build with DN2CPP_EXIT_VIA_STDEXIT to fall back to
// std::exit for sanitizer runs (which report from an atexit handler).
[[noreturn]] void dn2cpp_environment_exit(int32_t code);

/// How the generated `main` leaves. An executable terminates the process there and
/// never returns. A plain shared library (DN2CPP_SHARED) does not: a native host
/// dlopens it and calls `main` only to initialize the runtime, so exiting would kill
/// the host. Return to it instead. (Environment.Exit still terminates either way, as
/// it does in .NET.)
inline void dn2cpp_main_exit(int32_t code)
{
#ifdef DN2CPP_SHARED
    (void)code;
#else
    dn2cpp_environment_exit(code);
#endif
}

/// The console sink's commit, declared rather than included: this header is
/// compiled into every generated TU, and pulling platform/dn2cpp_pal.h in for one
/// function would put the whole PAL surface on that budget. Defined per target
/// under runtime/core/platform/<os>/ — see the "Console" section of
/// platform/dn2cpp_pal.h for why the flush is a seam entry at all.
void dn2cpp_pal_console_flush(void);

/// How the generated `main` leaves on an unhandled exception. Real .NET
/// terminates abnormally (SIGABRT; a Unix shell reports 134), not with a clean
/// nonzero exit — and the gates pin the native exit status to real .NET's.
/// The console is flushed explicitly first: .NET dies with Console already
/// flushed, and Linux's abort(), unlike macOS's, does not flush on its own. A
/// shared library must not kill its host — return to it instead (the generated
/// catch falls through to `return 1`), as with dn2cpp_main_exit.
inline void dn2cpp_main_abort()
{
#ifndef DN2CPP_SHARED
    dn2cpp_pal_console_flush();
    std::abort();
#endif
}
// Set the default GC collection mode applied by dn2cpp_runtime_init when the
// DN2CPP_GC_INCREMENTAL env override is unset. Console leaves the default (0 =
// stop-the-world); the Godot GDExtension calls this with 1 before managed init so
// real-time builds get bounded incremental pauses out of the box. Inert under
// DN2CPP_NO_GC. Call before dn2cpp_runtime_init runs.
void dn2cpp_gc_set_incremental_default(int on);
// Default the collector to Apple self-roots mode (default off): disable Boehm's
// dynamic-library scanning and register only the runtime image's __DATA as
// static roots. The Godot hosts call this with 1 before managed init — a
// windowed engine process loads hundreds of frameworks, which overflows the
// collector's root-set table ("Too many root sets" abort). Overridable at
// runtime via DN2CPP_GC_SELF_ROOTS=0/1; no-op off Apple platforms and under
// DN2CPP_NO_GC. Call before dn2cpp_runtime_init runs.
void dn2cpp_gc_set_self_roots_default(int on);
// Enable manual finalizer-drain mode (default off). When off, a dedicated
// background thread runs managed Finalize() bodies. When on, that thread is never
// started; the ring is instead drained on whatever thread calls
// dn2cpp_gc_drain_finalizers() / GC.WaitForPendingFinalizers(). The Godot
// GDExtension flips this on before managed init so RefCounted engine teardown
// runs on the engine's main thread, not off-thread. Inert under DN2CPP_NO_GC.
// Call before the first finalizable allocation (before dn2cpp_runtime_init).
void dn2cpp_gc_set_manual_finalizer_drain(int on);
// Called at the top of every [UnmanagedCallersOnly] method body and marshalled
// delegate thunk (the transpiler injects it). Default: a cheap no-op. Delegate
// marshalling latches it on before publishing a thunk; a native host controls an
// independent opt-in via dn2cpp_set_native_callback_gc_registration. It registers
// the calling thread on first entry and unregisters a newly registered thread from
// its thread_local destructor, so both executor workers and short-lived native
// threads can allocate and survive a stack scan. Inert under DN2CPP_NO_GC.
// extern "C": both hooks are host-facing ABI — a dlopen host resolves the setter
// by unmangled name (dlsym) from a shared-library build.
extern "C" DN2CPP_RT_EXPORT void dn2cpp_native_callback_prologue();
// Control the host opt-in for GC registration performed by
// dn2cpp_native_callback_prologue. Disabling it cannot revoke the one-way latch
// set by a published delegate thunk, whose native owner may retain the pointer. A
// host that invokes exported [UnmanagedCallersOnly] entry points from threads it
// spawned itself flips this on before those calls (e.g. via dlsym on this symbol
// from a shared-library build).
extern "C" DN2CPP_RT_EXPORT void dn2cpp_set_native_callback_gc_registration(int on);
// Latch callback registration on before a delegate thunk becomes visible to
// native code. A published function pointer has no observable retirement point.
void dn2cpp_enable_native_delegate_callback_gc_registration();
// Register the calling thread with the collector, once, and never unregister —
// the shared hook the Godot lanes call at the top of every entry point the
// engine can invoke (the engine calls them from threads it spawned itself,
// which Boehm knows nothing about; engine threads are persistent, hence
// register-once-and-leave rather than dn2cpp_native_callback_prologue's RAII
// unregister-at-exit shape). thread_local-cached: the hot path is one branch.
// Requires GC_INIT (a pre-init call falls through uncached and retries later);
// a no-op in a non-threaded collector build (wasm) and under DN2CPP_NO_GC.
void dn2cpp_gc_ensure_thread_registered();

// Finalizer support. Called from newobj
// emission only for a type whose Dn2CppTypeInfo::finalize is non-null — a
// program that allocates no finalizable type never touches this, so the
// finalizer thread is started lazily on first use (byte-identical output for
// every existing gate). Registers obj with Boehm's finalizer queue
// (GC_register_finalizer_no_order — order among finalizable objects in a
// cycle is unspecified, matching .NET's own unordered finalization); the
// GC-invoked callback only pushes obj onto a queue the dedicated finalizer
// thread drains, since a Boehm finalization callback runs from GC context and
// cannot safely run arbitrary managed code (allocate, take arbitrary locks) itself.
void dn2cpp_register_finalizer(Dn2CppObject* obj);
// System.GC.SuppressFinalize(object) / GC.ReRegisterForFinalize(object):
// unregister/re-register obj's pending
// finalizer call. A no-op when the type has no Finalize override (matches
// real GC.SuppressFinalize, which is legal to call on any object).
// ReRegisterForFinalize simply re-runs the same registration
// dn2cpp_register_finalizer performs at newobj time — Boehm implicitly
// unregisters a finalizer once it is queued for finalization (see
// GC_register_finalizer's contract), which is exactly the case
// ReRegisterForFinalize exists to undo (a resurrected object opting back in).
void dn2cpp_gc_suppress_finalize(Dn2CppObject* obj);
void dn2cpp_gc_reregister_for_finalize(Dn2CppObject* obj);
// System.GC.Collect(): forces a full collection and drains any finalizer
// callbacks Boehm becomes ready to invoke as a result (queues them for the
// finalizer thread; does not itself block on managed Finalize() bodies —
// see dn2cpp_gc_wait_for_pending_finalizers for that).
void dn2cpp_gc_collect();
// System.GC.WaitForPendingFinalizers(): blocks the calling thread until every
// finalizer queued so far (by a prior Collect or organic collection) has
// finished running. Deterministic pairing with dn2cpp_gc_collect is what lets
// a finalizer gate assert on output instead of racing the finalizer thread.
void dn2cpp_gc_wait_for_pending_finalizers();
// Drain queued managed Finalize() bodies on the calling thread. Only does work in
// manual-drain mode (dn2cpp_gc_set_manual_finalizer_drain) — a no-op otherwise,
// since the dedicated finalizer thread is then the ring's sole consumer. The Godot
// GDExtension calls this from its per-frame process hook so finalizers that become
// due from organic collections run on the engine's main thread promptly.
void dn2cpp_gc_drain_finalizers();
// System.GC.KeepAlive(object): an opaque liveness barrier. Defined out of line
// in the runtime TU (the build does no LTO), so the optimizer must assume the
// callee reads `obj` — the reference stays live at the call site, which is the
// entire contract of the API. The real BCL body is empty and would be erased.
void dn2cpp_keep_alive(Dn2CppObject* obj);
// System.GC.GetTotalMemory(bool forceFullCollection): approximate bytes currently in
// use on the managed heap. Also the only reliable way to observe weak references
// working: the conservative collector can retain an individual referent via a stray
// register/stack copy indefinitely, so a test must assert that heap usage stays
// bounded rather than that any specific object was collected. forceFullCollection
// runs a Collect() first.
int64_t dn2cpp_gc_get_total_memory(int32_t forceFullCollection);

// System.GC.CollectionCount(generation): how many collections have run. Boehm is
// non-generational, so every generation reports the single collection-cycle
// counter (GC_get_gc_no); 0 under the calloc fallback (no collections occur).
int32_t dn2cpp_gc_collection_count();

// The two heap-accounting figures the vendored Boehm GC exposes directly, used
// to fill the honest fields of System.GC.GetGCMemoryInfo()'s GCMemoryInfoData
// (heap size, and the free/fragmented bytes within it); committed bytes mirror
// the heap size. The transpiler emits the field stores itself (it knows the
// managed layout), so these stay plain scalar getters. 0 under the calloc
// fallback, where no heap accounting is modeled.
int64_t dn2cpp_gc_heap_size_bytes();
int64_t dn2cpp_gc_free_bytes();

// GC.GetAllocatedBytesForCurrentThread / GC.GetTotalAllocatedBytes(precise): cumulative
// allocation counters, never decreasing. The per-thread one counts REQUESTED bytes (no
// granule rounding); pinned blocks are included, so both sit slightly above real .NET.
int64_t dn2cpp_gc_allocated_bytes_current_thread();
int64_t dn2cpp_gc_total_allocated_bytes();

[[noreturn]] void dn2cpp_fail(const char* message);

// What an EMPTY virtual slot holds. A null pointer there would jump to address 0 when
// called, and the crash names nothing; an unreachable slot is a bug in the
// reachability model, so it should say which type and which method it is about.
//
// The receiver reaches the trap as arg0, so it reads the dynamic type and scans its
// method tables for the methods whose slot holds a trap. Two limits, both acceptable
// in an already-fatal path: a virtual returning a large struct by value passes a
// hidden return buffer ahead of the receiver on some ABIs (so `self` is not the
// receiver, and the scan reports nothing rather than the wrong thing), and a
// metadata-stripped program reports the type but no method name.
//
// _named answers the first: for a struct-returning virtual the emitter bakes the
// (class, method) descriptor into a per-slot stub, so the abort names the slot without
// reading `self`. The receiver-scanning form stays the default, being more precise.
[[noreturn]] void dn2cpp_vcall_unimplemented(Dn2CppObject* self);
[[noreturn]] void dn2cpp_vcall_unimplemented_named(const char* slotDesc);
// The receiver-scanning form entered through a per-signature trap thunk. The emitter
// gives each trapped slot a thunk carrying the slot's exact C++ signature — a wasm
// call_indirect checks the callee's type immediate, so the shared symbol above dies
// there as an anonymous "function signature mismatch" before it can report — and with
// the signature exact, `self` is the receiver at the C++ level for EVERY return shape
// (the compiler owns any hidden struct-return buffer below it). `slotFn` is the thunk's
// own address: the candidate scan matches slots holding it, so the report stays as
// precise as the shared form's.
[[noreturn]] void dn2cpp_vcall_unimplemented_at(Dn2CppObject* self, const void* slotFn);
// The per-signature vtable trap thunks of the image, registered by the generated init
// prologue. For the callers that must recognise a trapped slot WITHOUT calling it
// (dn2cpp_exception_message's override probe); one image, one registration.
void dn2cpp_register_vcall_traps(const void* const* fns, int32_t count);

// Throws a managed OverflowException (catchable), unlike dn2cpp_fail.
[[noreturn]] void dn2cpp_overflow();

// ThrowHelper trap intrinsics. The BCL's System.ThrowHelper dead-throw closures
// (Span<T> bounds checks, Slice argument checks, …) are intrinsic-mapped to
// these, which raise a catchable managed exception of the matching type without
// pulling the large exception-construction IL into the program.
// Slow path behind the generated static-constructor first-use guards
// (X__ensure): run `body` exactly once across threads, with a release store on
// `flag` that pairs with the guard's inline acquire-load fast path. A thread
// racing an in-flight init blocks until it completes; a re-entrant call from
// the initializing thread itself returns immediately (recursive-cctor
// semantics — it observes the partially-initialized statics, like real .NET).
// A throwing body does NOT mark the flag: the failure is recorded against the flag's
// address and every later call re-raises that same managed exception, so a
// half-initialized type is never read as ready and the cctor is never retried (.NET's
// rule; it caches a TypeInitializationException, which dn2cpp does not model).
// `body` is null for a tree-shaken cctor (the guard then only latches the flag).
void dn2cpp_cctor_run_once(std::atomic<int8_t>* flag, void (*body)());

// One entry of the generated eager startup cctor pass: calls the type's
// __ensure and swallows a managed failure, which dn2cpp_cctor_run_once has
// already recorded for the first real use of the type to re-raise. Eager
// initialization is dn2cpp's stand-in for .NET's lazy one, so it must not
// terminate a program over a type .NET would never have initialized.
// `type` is the declaring type's reflection name, baked by the emitter beside the
// pointer (never null): a swallowed failure is REPORTED, and a report that cannot name
// the type whose initializer failed names a class of bug rather than a bug.
void dn2cpp_cctor_run_startup(void (*ensure)(), const char* type);

[[noreturn]] void dn2cpp_throw_index_out_of_range();
[[noreturn]] void dn2cpp_throw_argument_out_of_range();
[[noreturn]] void dn2cpp_throw_argument_null();
// The same with the " (Parameter 'x')" tail ArgumentException.Message appends, for the
// entry points whose rejection real .NET attributes to a named parameter.
[[noreturn]] void dn2cpp_throw_argument_null_param(const char* paramName);
[[noreturn]] void dn2cpp_throw_argument();
[[noreturn]] void dn2cpp_throw_argument_msg(const char* message);
[[noreturn]] void dn2cpp_throw_invalid_operation();
// The same catchable InvalidOperationException, carrying a diagnosable reason.
// ResourceManager.GetString over a non-String entry is the caller: real .NET names the
// type it found there, and the whole point of matching the family is that a caller's
// `catch (InvalidOperationException)` reads a message that says what to do instead.
[[noreturn]] void dn2cpp_throw_invalid_operation_msg(const char* message);
// The BCL's ObjectDisposedException throw helpers (ObjectDisposedException.ThrowIf,
// ThrowHelper.ThrowObjectDisposedException_StreamClosed, ...) — a use-after-Dispose
// on a stream, a file handle or a CancellationTokenSource.
[[noreturn]] void dn2cpp_throw_object_disposed();
// Math.Sign on a NaN input — .NET raises ArithmeticException (NaN has no sign).
[[noreturn]] void dn2cpp_throw_arithmetic();
// A size computation that cannot be satisfied because the RESULT does not fit,
// not because the allocator refused — see dn2cpp_out_of_memory_exception_type
// for why the allocator's own failures deliberately do not come here.
[[noreturn]] void dn2cpp_throw_out_of_memory();
[[noreturn]] void dn2cpp_throw_type_load();
[[noreturn]] void dn2cpp_throw_not_supported();
// The same catchable NotSupportedException, carrying a diagnosable reason —
// for AOT-boundary misses (MakeGenericType) where the bare throw names nothing.
[[noreturn]] void dn2cpp_throw_not_supported_msg(const char* message);
// The dynamic-code-generation surface trap (Reflection.Emit, DLR CallSite,
// Expression.Compile): catchable, message names the cut member.
[[noreturn]] void dn2cpp_throw_platform_not_supported(const char* message);
// ResourceManager's "no such resource set" — the family real .NET raises, so the
// `catch (MissingManifestResourceException)` its documentation prescribes fires.
[[noreturn]] void dn2cpp_throw_missing_manifest_resource(const char* message);
// DN2CPP_TF_LAYOUT_UNKNOWN guard: a type whose CLR layout extent the emitter could not
// model carries a meaningless instanceSize of 1, which no reader may hand to managed
// code as a size or use as an element stride. Called by exactly those readers —
// dn2cpp_layout_size, dn2cpp_marshal_sizeof, the two Marshal struct copies and
// Array.CreateInstance's element stride — and a no-op for every other type, so a
// genuinely field-less struct keeps answering 1. See the flag's own doc for why the
// bit, not the number, is the test, and why allocation sizing does not call it.
void dn2cpp_require_layout(const Dn2CppTypeInfo* ti);
// Numeric/date parse failures (the NumberStyles engine's Parse forms).
[[noreturn]] void dn2cpp_throw_format();
// The same, carrying the input real .NET's own message quotes ("The input string 'x' was
// not in a correct format.") — for the Parse forms, which hold the string they refused.
[[noreturn]] void dn2cpp_throw_format_value(Dn2CppString* value);
// Dictionary<K,V>'s missing-key indexer (ThrowHelper.ThrowKeyNotFoundException<T>).
// A typed `catch (KeyNotFoundException)` needs the matching type, so it has its
// own handle + trap (rather than falling through to the InvalidOperation trap).
[[noreturn]] void dn2cpp_throw_key_not_found();
// Reflection member lookup with several undecidable matches (Type.GetMethod /
// GetProperty), matching .NET's AmbiguousMatchException.
[[noreturn]] void dn2cpp_throw_ambiguous_match();
// An array block move whose two operands disagree on rank.
[[noreturn]] void dn2cpp_throw_rank();
// Constructor resolution with no invokable match (Activator.CreateInstance and
// friends), matching .NET's MissingMethodException; the message carries the
// diagnosable reason (like the dynamic-codegen PNSE trap).
[[noreturn]] void dn2cpp_throw_missing_method(const char* message);
// A runtime entry point's null managed receiver (matching real .NET's
// NullReferenceException for the instance call it stands in for) — catchable,
// where the dereference it replaces was a SIGSEGV.
[[noreturn]] void dn2cpp_throw_null_reference();
// An integer division or remainder whose divisor is zero, matching .NET's
// DivideByZeroException. Out of line and [[noreturn]] so the inline guards below
// carry no code on the taken-never path.
[[noreturn]] void dn2cpp_throw_divide_by_zero();

// The Dn2CppType receiver's metadata, with the managed-NRE guard the instance
// call it stands in for would raise. A null Type handle is a NORMAL value in
// this surface — Type.GetType, Assembly.GetType and GetNestedType all answer
// null on a miss and library code writes `Type.GetType(n)!` — so every
// reflection lowering that reads `->typeInfo` off a receiver goes through here
// rather than dereferencing. Answering a default (an empty member set, a null name)
// would be a silent wrong answer: from the metadata alone it is indistinguishable
// from a real one.
inline const Dn2CppTypeInfo* dn2cpp_type_require(Dn2CppType* t)
{
    if (t == nullptr || t->typeInfo == nullptr)
        dn2cpp_throw_null_reference();
    return t->typeInfo;
}

// The same read for the one member of that surface whose null is an ARGUMENT
// rather than a receiver: Type.GetTypeCode(Type) is STATIC, and real .NET answers
// TypeCode.Empty for a null argument instead of throwing.
inline const Dn2CppTypeInfo* dn2cpp_type_info_or_null(Dn2CppType* t)
{
    return t != nullptr ? t->typeInfo : nullptr;
}
// Generic typed trap (throw a bare managed exception of `ti`). Shared so the
// per-namespace intrinsic units (e.g. intrinsics/dn2cpp_system_io.cpp) can raise
// the matching .NET exception type without pulling in its construction IL.
[[noreturn]] void dn2cpp_throw_of(const Dn2CppTypeInfo* ti);
// The message real .NET's parameterless ctor of `ti` gives, or null for a type this
// runtime does not raise. dn2cpp_throw_of seeds every trap with it.
Dn2CppString* dn2cpp_default_message(const Dn2CppTypeInfo* ti);
// The same trap with the message the EMITTER already resolved (a ThrowHelper sink whose
// call site named its ExceptionResource).
[[noreturn]] void dn2cpp_throw_of_msg(const Dn2CppTypeInfo* ti, const char* message);
// The same trap with a one-argument SR composite format as its message — for the sites
// holding the operand .NET's own sentence names (the string that failed to parse, the
// duplicate dictionary key). Falls back to `ti`'s default text if the key is absent.
[[noreturn]] void dn2cpp_throw_sr1(const Dn2CppTypeInfo* ti, const char* key, Dn2CppString* a0);
// ArgumentOutOfRangeException with the paramName/actual-value tail real .NET's Message
// overrides append; `key` is a "{0} ('{1}')…" resource taking (paramName, value).
[[noreturn]] void dn2cpp_throw_argument_out_of_range_value(const char* key,
    const char* paramName, Dn2CppString* value);
// The same without an actual-value tail: `key` is a plain sentence, and only the
// " (Parameter 'x')" ArgumentException.Message appends is added.
[[noreturn]] void dn2cpp_throw_argument_out_of_range_param(const char* key,
    const char* paramName);

// Checked range conversion: trap unless the value fits the target range.
template <typename TTo, typename TFrom>
inline TTo dn2cpp_conv_ovf(TFrom v, TFrom lo, TFrom hi)
{
    if (v < lo || v > hi)
        dn2cpp_overflow();
    return static_cast<TTo>(v);
}

// INumberBase<T>.CreateChecked<TFrom>(value) / TryConvertFrom/ToChecked: the
// generic-math checked conversion onto a concrete integer primitive target.
// An integer value fits the target T iff the truncating cast round-trips back
// to the source AND the sign agrees — this single check is correct across every
// (signed/unsigned, narrow/widen) integer pairing, including the i64<->u64
// boundaries int64-bounded conv.ovf cannot represent. A float/double source
// traps on NaN and whenever its toward-zero truncation falls outside T's range
// (.NET's conv.ovf float semantics; the plain C++ cast would be UB there), the
// bounds compared in the float domain against an exactly-representable
// power-of-two (T's max itself may round when converted to the source type).
// Out of range raises a managed OverflowException, exactly like the BCL. The
// sign tests are guarded with `if constexpr` so an unsigned operand's `< 0`
// comparison (a tautology) is never compiled (warning-free).
template <typename TTo, typename TFrom>
inline TTo dn2cpp_create_checked(TFrom v)
{
    static_assert(std::is_integral<TTo>::value, "dn2cpp_create_checked targets integers only");
    if constexpr (std::is_floating_point<TFrom>::value)
    {
        if (std::isnan(v))
            dn2cpp_overflow();
        TFrom tv = std::trunc(v);
        // 2^(bits-1) for signed / 2^bits for unsigned targets: the valid truncated
        // range is [-bound, bound) / [0, bound).
        constexpr TFrom bound =
            static_cast<TFrom>(std::numeric_limits<TTo>::max() / 2 + 1) * static_cast<TFrom>(2);
        if (tv >= bound)
            dn2cpp_overflow();
        if constexpr (std::is_signed<TTo>::value)
        {
            if (tv < -bound)
                dn2cpp_overflow();
        }
        else
        {
            if (tv < static_cast<TFrom>(0))
                dn2cpp_overflow();
        }
        return static_cast<TTo>(tv);
    }
    else
    {
        TTo r = static_cast<TTo>(v);
        if (static_cast<TFrom>(r) != v)
            dn2cpp_overflow();
        bool vNeg = false, rNeg = false;
        if constexpr (std::is_signed<TFrom>::value)
            vNeg = v < static_cast<TFrom>(0);
        if constexpr (std::is_signed<TTo>::value)
            rNeg = r < static_cast<TTo>(0);
        if (vNeg != rNeg)
            dn2cpp_overflow();
        return r;
    }
}

// INumberBase<T>.CreateSaturating<TFrom>(value) / TryConvertFrom/ToSaturating:
// clamp an integer source into the target integer range instead of trapping.
// Like CreateChecked, a value is in range iff its truncating cast round-trips
// and the sign agrees; out of range it saturates to T.Min (source negative /
// below) or T.Max (above). A float source takes
// dn2cpp_convert_to_integer_native instead (same clamping, NaN -> 0). The
// bounds checks are `if constexpr`-guarded so an unsigned operand's `< 0`
// comparison is never compiled (warning-free).
template <typename TTo, typename TFrom>
inline TTo dn2cpp_create_saturating(TFrom v)
{
    static_assert(std::is_integral<TTo>::value && std::is_integral<TFrom>::value,
                  "dn2cpp_create_saturating is integer-only");
    TTo r = static_cast<TTo>(v);
    bool roundTrips = static_cast<TFrom>(r) == v;
    bool vNeg = false, rNeg = false;
    if constexpr (std::is_signed<TFrom>::value)
        vNeg = v < static_cast<TFrom>(0);
    if constexpr (std::is_signed<TTo>::value)
        rNeg = r < static_cast<TTo>(0);
    if (roundTrips && vNeg == rNeg)
        return r;
    // Out of range: a negative source clamps to the low bound, anything else to
    // the high bound (an out-of-range positive that round-trips to a negative TTo,
    // e.g. ulong.MaxValue -> long, is above the high bound).
    return vNeg ? std::numeric_limits<TTo>::min() : std::numeric_limits<TTo>::max();
}

// double/float.ConvertToIntegerNative<TInteger>(value): truncate toward zero,
// saturating on overflow with NaN -> 0 (.NET's defined float->int conversion
// semantics; arm64 fcvtzs behaves the same). Also the float-source arm of the
// generic-math TryConvertFrom/To{Saturating,Truncating} conversions, whose BCL
// bodies clamp float sources the same way. A plain C++ cast is undefined
// behavior on out-of-range/NaN input, so the range checks stay in the float
// domain against an exactly-representable power-of-two bound.
template <typename TTo, typename TFrom>
inline TTo dn2cpp_convert_to_integer_native(TFrom v)
{
    static_assert(std::is_integral<TTo>::value && std::is_floating_point<TFrom>::value,
                  "dn2cpp_convert_to_integer_native is float->integer only");
    // 2^(bits-1) for signed / 2^bits for unsigned targets: TTo's max itself may
    // round when converted to TFrom, but a power of two is always exact.
    constexpr TFrom bound =
        static_cast<TFrom>(std::numeric_limits<TTo>::max() / 2 + 1) * static_cast<TFrom>(2);
    if (std::isnan(v))
        return static_cast<TTo>(0);
    if (v >= bound)
        return std::numeric_limits<TTo>::max();
    if constexpr (std::is_signed<TTo>::value)
    {
        if (v < -bound)
            return std::numeric_limits<TTo>::min();
    }
    else
    {
        if (v <= static_cast<TFrom>(-1))
            return static_cast<TTo>(0);
    }
    // In range after truncation toward zero -> the cast is well-defined.
    return static_cast<TTo>(v);
}

// IBinaryInteger<T>.ReadBigEndian(ReadOnlySpan<byte>, bool isUnsigned) -> T. The
// static-virtual's default body calls the static-abstract TryReadBigEndian (an
// InternalCall) and throws OverflowException when the value does not fit T; this
// reproduces both. `src`/`n` are the span's {f__reference, f__length}. Reads the
// source big-endian; `isUnsigned` selects zero- vs sign-extension of the source's
// leading bit. Matches .NET exactly (verified against int/long/uint/ulong across
// lengths 0..9): an empty span is 0; the low sizeof(T) bytes form the value with the
// remaining high bytes sign/zero-filled; any leading byte past that window must be
// redundant fill (else overflow). A NEGATIVE (signed-source) value never fits an
// UNSIGNED target, at any length — so that overflows regardless of n. For a SIGNED
// target, once the low-W window is full the assembled sign bit must agree with the
// source's sign (an unsigned value exceeding T's signed range, or a mis-extended
// signed one, overflows). Integer T only (T is the closed IBinaryInteger self type).
template <typename T>
inline T dn2cpp_read_big_endian(const uint8_t* src, int32_t n, int32_t isUnsigned)
{
    static_assert(std::is_integral<T>::value, "dn2cpp_read_big_endian is integer-only");
    typedef typename std::make_unsigned<T>::type UT;
    constexpr int W = static_cast<int>(sizeof(T));
    if (n <= 0)
        return static_cast<T>(0);
    bool neg = (isUnsigned == 0) && ((src[0] & 0x80) != 0);
    uint8_t fill = neg ? static_cast<uint8_t>(0xFF) : static_cast<uint8_t>(0x00);
    // Leading bytes beyond the low-W window must be redundant sign/zero extension.
    if (n > W)
        for (int32_t i = 0; i < n - W; i++)
            if (src[i] != fill)
                dn2cpp_overflow();
    UT acc = neg ? static_cast<UT>(~static_cast<UT>(0)) : static_cast<UT>(0);
    int32_t start = (n > W) ? (n - W) : 0;
    for (int32_t i = start; i < n; i++)
        acc = static_cast<UT>((acc << 8) | static_cast<UT>(src[i]));
    if constexpr (!std::is_signed<T>::value)
    {
        // An unsigned target cannot hold a negative (signed-source) value at ANY
        // length — the n < W short-span case included.
        if (neg)
            dn2cpp_overflow();
    }
    else if (n >= W)
    {
        // Signed target: once the low-W window is full, the assembled sign bit must
        // agree with the source's sign; otherwise the value is out of T's range.
        bool accTop = ((acc >> (8 * W - 1)) & 1) != 0;
        if (accTop != neg)
            dn2cpp_overflow();
    }
    return static_cast<T>(acc);
}

// Math.Min/Max floating-point semantics (IEEE 754:2019 minimum/maximum),
// mirroring the BCL's exact algorithm: a NaN operand propagates and -0.0
// orders below +0.0 — both unlike std::fmin/fmax, which drop the NaN and
// treat the zeros as equal. T is float for the MathF overloads (the call
// site casts both operands) so single-precision stays in float.
template <typename T>
inline T dn2cpp_math_min(T x, T y)
{
    if (x != y)
        return std::isnan(x) ? x : (x < y ? x : y);
    return std::signbit(x) ? x : y;
}

template <typename T>
inline T dn2cpp_math_max(T x, T y)
{
    if (x != y)
        return std::isnan(x) ? x : (y < x ? x : y);
    return std::signbit(y) ? x : y;
}

// Math.MaxMagnitude/MinMagnitude (IEEE 754:2019 maximumMagnitude /
// minimumMagnitude), mirroring the BCL's exact algorithm: a NaN operand
// propagates; on equal magnitudes Max prefers the positive operand and Min
// the negative one (+0 orders above -0). T is float for the MathF overloads.
template <typename T>
inline T dn2cpp_math_maxmag(T x, T y)
{
    T ax = std::fabs(x), ay = std::fabs(y);
    if (ax > ay || std::isnan(ax))
        return x;
    if (ax == ay)
        return std::signbit(x) ? y : x;
    return y;
}

template <typename T>
inline T dn2cpp_math_minmag(T x, T y)
{
    T ax = std::fabs(x), ay = std::fabs(y);
    if (ax < ay || std::isnan(ax))
        return x;
    if (ax == ay)
        return std::signbit(x) ? x : y;
    return y;
}

// double/float MaxNumber/MinNumber/MaxMagnitudeNumber/MinMagnitudeNumber
// (IEEE 754:2019 maximumNumber/minimumNumber and the magnitude forms) — the
// BCL's exact comparison chains: a NaN operand is DROPPED (the other operand
// wins; NaN only when both are), and equal values / equal magnitudes order
// -0.0 below +0.0. T is float for the Single overloads.
template <typename T>
inline T dn2cpp_math_maxnum(T x, T y)
{
    if (x != y)
    {
        if (!std::isnan(y))
            return y < x ? x : y;
        return x;
    }
    return std::signbit(y) ? x : y;
}

template <typename T>
inline T dn2cpp_math_minnum(T x, T y)
{
    if (x != y)
    {
        if (!std::isnan(y))
            return x < y ? x : y;
        return x;
    }
    return std::signbit(x) ? x : y;
}

template <typename T>
inline T dn2cpp_math_maxmagnum(T x, T y)
{
    T ax = std::fabs(x), ay = std::fabs(y);
    if ((ax > ay) || std::isnan(ay))
        return x;
    if (ax == ay)
        return std::signbit(x) ? y : x;
    return y;
}

template <typename T>
inline T dn2cpp_math_minmagnum(T x, T y)
{
    T ax = std::fabs(x), ay = std::fabs(y);
    if ((ax < ay) || std::isnan(ay))
        return x;
    if (ax == ay)
        return std::signbit(x) ? x : y;
    return y;
}

// double/float MaxNative/MinNative — .NET's fastest-on-this-platform min/max,
// whose NaN and ±0 behavior is explicitly platform-defined (arm64 hardware
// answers fmax/fmin, x64 answers maxsd's second-operand rule): the plain
// comparison ternary is a conforming implementation, and only the unambiguous
// finite results are contractual.
template <typename T>
inline T dn2cpp_math_maxnative(T x, T y)
{
    return (x > y) ? x : y;
}

template <typename T>
inline T dn2cpp_math_minnative(T x, T y)
{
    return (x < y) ? x : y;
}

// Signed-integer Math.Abs: MinValue has no positive counterpart — .NET
// throws OverflowException (and -MinValue is UB in C++). The call site casts
// the operand to the overload's real storage width (sub-word values ride the
// stack as int32) so numeric_limits sees the declared type's MinValue.
template <typename T>
inline T dn2cpp_math_abs_int(T v)
{
    if (v == std::numeric_limits<T>::min())
        dn2cpp_overflow();
    return v < (T)0 ? (T)-v : v;
}

// Math.Clamp (integer and floating overloads): min > max raises
// ArgumentException like .NET; otherwise the BCL's comparison chain, which
// passes a NaN value through unchanged (both comparisons are false).
template <typename T>
inline T dn2cpp_math_clamp(T v, T lo, T hi)
{
    if (hi < lo)
        dn2cpp_throw_argument();
    return v < lo ? lo : (v > hi ? hi : v);
}

// double/float ClampNative: min > max raises ArgumentException like .NET (a
// NaN bound compares false, so it does not throw); the clamp itself composes
// the Native ternaries, so its NaN/±0 results are the platform-defined ones.
template <typename T>
inline T dn2cpp_math_clampnative(T v, T lo, T hi)
{
    if (lo > hi)
        dn2cpp_throw_argument();
    return dn2cpp_math_minnative(dn2cpp_math_maxnative(v, lo), hi);
}

// double/float.IsPow2 (IBinaryNumber) — the BCL's exact bit test: a positive
// finite value whose significand marks a single power of two (a normal value
// with an all-zero trailing significand, or a subnormal with exactly one
// significand bit set). Negative values, zeros, infinities and NaNs are false.
inline bool dn2cpp_math_ispow2(double v)
{
    uint64_t bits;
    std::memcpy(&bits, &v, 8);
    if ((int64_t)bits <= 0)
        return false;
    uint32_t biasedExponent = (uint32_t)((bits >> 52) & 0x7FF);
    uint64_t trailingSignificand = bits & 0xFFFFFFFFFFFFFULL;
    if (biasedExponent == 0)
        return __builtin_popcountll(trailingSignificand) == 1;
    if (biasedExponent == 0x7FF)
        return false;
    return trailingSignificand == 0;
}

inline bool dn2cpp_math_ispow2_f(float v)
{
    uint32_t bits;
    std::memcpy(&bits, &v, 4);
    if ((int32_t)bits <= 0)
        return false;
    uint32_t biasedExponent = (bits >> 23) & 0xFF;
    uint32_t trailingSignificand = bits & 0x7FFFFF;
    if (biasedExponent == 0)
        return __builtin_popcount(trailingSignificand) == 1;
    if (biasedExponent == 0xFF)
        return false;
    return trailingSignificand == 0;
}

// Managed exceptions ride on C++ exceptions carrying the managed object.
struct Dn2CppException
{
    Dn2CppObject* obj;
};

// In-flight exception rooting: between the throw and the handler that consumes
// it, the managed exception object may exist ONLY in the __cxa exception buffer
// — malloc memory the conservative collector never scans — while finally/fault
// bodies (arbitrary managed code) run and allocate. Push links the object into
// a static-rooted list; pop (by value; an unmatched pop is a no-op) unlinks it
// once a handler holds it in scanned memory. Generated catch clauses pop on
// entry and re-push on `rethrow`; runtime sites that swallow a managed
// exception pop too.
void dn2cpp_exc_inflight_push(Dn2CppObject* obj);
void dn2cpp_exc_inflight_pop(Dn2CppObject* obj);

// Whether the type's base chain reaches System.Exception — i.e. whether an
// instance carries the Dn2CppExceptionObject prefix (message/inner/hresult/
// trace slots).
bool dn2cpp_type_is_exception(const Dn2CppTypeInfo* ti);

// Captures the current call stack into the object's trace slot (overwriting
// any earlier capture — `throw ex;` re-stamps in real .NET too). When this
// thread's shadow stack holds frames (the transpiler's --shadow-stack mode
// planted guards), the capture is those frame names (kind 1); otherwise it is
// the per-frame function entries of the native PAL backtrace (kind 0). A
// no-op for an object
// whose base chain does not reach System.Exception (which therefore has no
// trace slot to write), and — with no shadow frames — on targets whose PAL
// backtrace reports nothing (WASM), where the slot stays null and StackTrace
// degrades to null.
void dn2cpp_exc_stamp_trace(Dn2CppObject* obj);

// Opt-in shadow stack: --shadow-stack plants a Dn2CppShadowFrame guard in the prologue
// of every managed body, and dn2cpp_exc_stamp_trace prefers the recorded names over a
// PC walk — names survive -O2 inlining (which deletes physical frames out from under
// the kind-0 backtrace) and exist on WASM (where the PAL backtrace reports nothing).
// With no guards planted the stack stays empty and everything here is inert.
//
// `frames` holds only process-lifetime strings (rodata literals, or image-lifetime
// interpreter frame names hung off the never-freed patch image) and never GC pointers,
// so the buffer is plain malloc memory the collector need not scan, owned and freed by
// a thread-local owner on thread exit. `depth` keeps counting past `cap` — an honest
// overflow record — and a failed buffer allocation degrades to cap == 0 (count-only
// mode) rather than losing the throw.
struct Dn2CppShadowStack
{
    int32_t depth;        // logical depth — keeps counting past cap (honest overflow record)
    int32_t cap;          // allocated capacity (0 if allocation failed: count-only mode)
    const char** frames;  // rodata literals or image-lifetime interp frame names — never GC-scanned, malloc-managed
};
extern thread_local Dn2CppShadowStack* dn2cpp_shadow_tls;
Dn2CppShadowStack* dn2cpp_shadow_stack_acquire();

// Whether this image was transpiled with --shadow-stack. The runtime library is
// flag-independent, so the flag is recorded in the BINARY by the generated init
// prologue calling the marker. The reader is the hot-update loader: an enabled image
// means every AOT body carries a frame guard, so interpreted frames must push onto the
// same shadow stack or the mixed trace would silently skip them.
void dn2cpp_shadow_stack_mark_enabled();
bool dn2cpp_shadow_stack_is_enabled();

// The per-body RAII guard. RAII is load-bearing, not stylistic: managed
// `finally` is emitted as catch(...) + rethrow, not as a C++ destructor, so
// a destructor is the ONLY construct guaranteed to run while a managed
// exception unwinds through a frame — pop any other way and every unwound
// frame would stay on the shadow stack forever. The pop decrements
// unconditionally while the push stores only below `cap`, so depth and the
// stored frames cannot fall out of step across overflow.
struct Dn2CppShadowFrame
{
    Dn2CppShadowStack* s;
    explicit Dn2CppShadowFrame(const char* name)
    {
        Dn2CppShadowStack* st = dn2cpp_shadow_tls;
        if (st == nullptr)
            st = dn2cpp_shadow_stack_acquire();
        if (st->depth < st->cap)
            st->frames[st->depth] = name;
        st->depth++;
        s = st;
    }
    ~Dn2CppShadowFrame() { s->depth--; }
};

[[noreturn]] inline void dn2cpp_throw(Dn2CppObject* obj)
{
    if (obj == nullptr)
        dn2cpp_throw_null_reference();
    dn2cpp_exc_stamp_trace(obj);
    dn2cpp_exc_inflight_push(obj);
    throw Dn2CppException{ obj };
}

// Re-raise preserving an already-captured trace (stamps only when the slot is
// still null): the runtime's equivalent of IL `rethrow` for the sites that
// re-raise a STORED exception through dn2cpp_throw — the interpreter's rethrow
// opcode and faulted/canceled Task propagation. IL rethrow in AOT code lowers
// to a bare C++ `throw;` and never comes through here.
[[noreturn]] void dn2cpp_rethrow(Dn2CppObject* obj);

// A managed exception object that carries its Message. Any `new <Exception-derived>`
// with a parameterless, (string) or (string, Exception) ctor is intercepted at newobj
// and allocated here instead of running the real (untranspilable) exception ctor IL.
// `ti` is the derived type's own type-info so `catch (NotSupportedException)` /
// GetType() stay exact. A null message is stored as null and read back by get_Message
// as the base-Exception default ("Exception of type 'X' was thrown.") — a documented
// approximation, since .NET's per-derived-type defaults live in resource strings
// dn2cpp never reads.
Dn2CppObject* dn2cpp_exception_new(const Dn2CppTypeInfo* ti, Dn2CppString* message, Dn2CppObject* inner);
// System.Exception.get_Message on an object dn2cpp_exception_new produced, with virtual
// dispatch: calls a derived get_Message override through the vtable
// (dn2cpp_exception_get_message_slot) when one is present, else the stored message. This
// is what `ex.Message` on a base-typed receiver lowers to.
Dn2CppString* dn2cpp_exception_message(Dn2CppObject* ex);
// Exception.get_StackTrace: the trace captured at throw resolved against the
// reflection method table, or null — for a never-thrown exception (exact), on
// a target with no stack walk (WASM), or when nothing resolves. Frame format
// declared at the definition; the get_StackTrace intrinsic lowers here.
Dn2CppString* dn2cpp_exception_stacktrace(Dn2CppObject* ex);
// The stored message (or base-default for null) WITHOUT dispatch — System.Exception's own
// non-overridden get_Message body. What a non-virtual `base.Message` inside a derived
// override resolves to (dispatching there would recurse).
Dn2CppString* dn2cpp_exception_message_stored(Dn2CppObject* ex);
// System.Exception.get_InnerException: the stored inner exception, or null.
Dn2CppObject* dn2cpp_exception_inner(Dn2CppObject* ex);
// System.Exception.get_HResult: the stored HResult (COR_E_EXCEPTION base default,
// overwritten by each derived ctor's set_HResult).
int32_t dn2cpp_exception_hresult(Dn2CppObject* ex);
// System.Exception.GetBaseException(): the innermost exception of the inner-chain,
// or the exception itself when it has no inner (exactly real .NET's identity case).
Dn2CppObject* dn2cpp_exception_get_base(Dn2CppObject* ex);
// System.Exception.ToString(): "FullTypeName: Message" with the " ---> " inner
// chain, then the stack-trace section ('\n' + the get_StackTrace lines) when a
// trace was captured at throw — absent for an unthrown exception, as in
// real .NET.
Dn2CppString* dn2cpp_exception_tostring(Dn2CppObject* ex);
// The console main wrapper's last-resort diagnostic: "Unhandled managed
// exception: <type>: <Message>" on stderr (Message via dn2cpp_exception_message,
// so a never-stored message prints the .NET-style base fallback), followed by
// the trace captured at throw when one resolves.
void dn2cpp_report_unhandled_exception(Dn2CppObject* ex);

// ---- the host boundary: ONE definition of what a managed fault costs ----
// A managed exception that reaches a NATIVE HOST FRAME cannot keep unwinding: the
// frame above is the engine's C ABI and has no landing pad, so continuing the unwind
// is std::terminate. Every such boundary therefore stops the exception, reports it,
// and returns the default its own caller expects. Only the SITE knows that default
// (a Variant, a bool, void, an error code), so the site keeps its own return; what it
// must not also own is the *report*, which is why this lives here.
//
// The two hosts have two different error logs, hence the sink: the GDExtension lane
// registers one calling the engine's print_error, the --dotnet-module lane one calling
// the transpiled GodotSharp ExceptionUtils.LogException, so a dn2cpp-owned boundary
// reports exactly where a real .NET one would. With no sink the report goes to stderr.
// It is never silent: a boundary that swallows without a word degrades a crash into a
// wrong answer.
typedef void (*Dn2CppBoundarySink)(const char* where, Dn2CppObject* exc);
void dn2cpp_set_boundary_exception_sink(Dn2CppBoundarySink sink);
// Whether a host that can survive a managed fault is present. The one question
// a core-side boundary has to ask before choosing between reporting and failing
// fast — see dn2cpp_sched_pump.
int dn2cpp_boundary_sink_installed();
// Report `exc` as having escaped the boundary named by the printf-style
// `where_fmt` (e.g. "%s.%s", class, method). Drops the in-flight root, routes to
// the sink when one is installed, and falls back to stderr when it is not or
// when the sink itself throws. NEVER throws and never returns non-locally: a
// caller may use it inside a catch that must go on to return a default.
void dn2cpp_report_boundary_exception(Dn2CppObject* exc, const char* where_fmt, ...);

// System.Environment.FailFast(message[, exception]) — an IMMEDIATE, uncatchable
// process abort. Not an exception: no unwinding, no catch/finally, no exit code
// negotiation. Reports the message (and the exception's ToString when the
// two-argument overload supplied one) on stderr, then goes through dn2cpp_fail,
// which flushes stdio and abort()s — so the shell sees SIGABRT (134 on Unix),
// exactly as real .NET's FailFast does. `message` and `exception` may be null.
[[noreturn]] void dn2cpp_environment_failfast(Dn2CppString* message, Dn2CppObject* exception);
// Build a System.AggregateException wrapping the Exception[] `inner` (reusable by any
// exception-aggregation site: Parallel, and later Task sync-wait). InnerException is
// the first element, or null when empty.
Dn2CppObject* dn2cpp_aggregate_exception_new(Dn2CppArrayRef* inner);
// AggregateException.get_InnerExceptions: the stored Exception[], stamped with the
// caller-supplied precise per-element array handle (ti_arr_System_Exception) so its
// SZArray interface-dispatch map serves IReadOnlyList<Exception> member calls.
Dn2CppArrayRef* dn2cpp_aggregate_inner_exceptions(Dn2CppObject* ex, const Dn2CppTypeInfo* arrTi);
// The memoized ReadOnlyCollection<Exception> get_InnerExceptions answers with, and its
// setter. The wrapper is managed BCL IL built by the emit arm; the runtime owns only the
// slot, which must live on the exception object so that two reads are reference-equal
// the way real .NET's stored field is.
Dn2CppObject* dn2cpp_aggregate_inner_wrapper(Dn2CppObject* ex);
void dn2cpp_aggregate_set_inner_wrapper(Dn2CppObject* ex, Dn2CppObject* wrapper);

// ---- managed stack traces (degraded when nothing real is available) ----
// System.Diagnostics.StackTrace / StackFrame are degraded intrinsics answering what
// the binary actually has (a native binary carries no CLR stack walker); the doctrine
// is docs/ARCHITECTURE.md §4-B ("Degrade or fail loud?"). Where a REAL frame source
// exists the answer is the real thing:
//   - the current stack: a --shadow-stack transpile plants frame guards, so the
//     capturing ctors materialize the live shadow stack's names. Flag-off they answer
//     zero frames.
//   - a thrown exception's stack: dn2cpp_throw stamps a trace in EVERY build (kind 0
//     PCs; kind 1 shadow names under --shadow-stack), so new StackTrace(Exception)
//     materializes it UNCONDITIONALLY — ex.StackTrace already renders it flag-off, and
//     answering "unavailable" beside that would be a silent inconsistency. An UNTHROWN
//     exception's trace is null and its StackTrace stays null.
// A zero-frame StackTrace.ToString() returns "   at <stack trace unavailable in AOT>\n"
// and never "" — the self-naming obligation. That and the materialized frame format are
// deliberate divergences, so a gate covering them must be a freeze gate, not a diff one.
//
// Neither struct is part of the exception object prefix nor of the hot-update BPI ABI,
// so growing them needs no LayoutPolicyVersion bump.
struct Dn2CppStackFrame : Dn2CppObject
{
    Dn2CppString* fileName;   // null unless a (fileName, line[, column]) ctor supplied one
    int32_t lineNumber;       // 0 when unknown
    int32_t columnNumber;     // 0 when unknown
    Dn2CppString* methodDesc; // rendered "Ns.Type.Method()[ [shared generic]]" text for a
                              // frame materialized from a shadow-stack / exception capture
                              // — null for caller-supplied and degraded frames. The
                              // frame is dn2cpp_alloc'd (GC-scanned), so a managed string
                              // slot is safe here.
};
struct Dn2CppStackTrace : Dn2CppObject
{
    Dn2CppArrayRef* frames;   // never null; length 0 when nothing real was available
    int32_t dropped;          // innermost frames lost to shadow-stack capacity at capture:
                              // a ToString marker line, NEVER a frame — a pseudo-frame
                              // would skew FrameCount/GetFrame(s) skip/count arithmetic
                              // and be iterated as if it were a method (the exception-side
                              // kind-1 render treats dropped as a line too, not an entry)
};
extern const Dn2CppTypeInfo dn2cpp_stacktrace_type;
extern const Dn2CppTypeInfo dn2cpp_stackframe_type;
// new StackTrace() / (bool) / (int) / (int, bool) — the current-stack capture. With no
// live shadow frames (flag-off, or a thread that has run no guarded body): the
// degraded zero-frame object. Otherwise the live shadow stack's names, innermost first;
// `skipFrames` consumes the innermost end of the LOGICAL stack (capacity-dropped
// frames first, then stored ones); negative clamps to 0, past-depth yields an empty
// trace (real .NET throws ArgumentOutOfRangeException on negative — declared
// divergence, this family never throws). `frameArrTi` is the emitted per-element
// handle for StackFrame[] (ti_arr_…), which GetFrames() hands back.
Dn2CppStackTrace* dn2cpp_stacktrace_new(const Dn2CppTypeInfo* frameArrTi, int32_t skipFrames);
// new StackTrace(Exception[, skipFrames]) — materializes the trace stamped into the
// exception at throw, UNCONDITIONALLY of --shadow-stack: the stamp exists flag-off too
// (kind 0) and ex.StackTrace already renders it there, so answering "unavailable"
// beside it would be the silent-inconsistency shape §4-B forbids. Null `ex` (real .NET:
// ArgumentNullException — declared divergence), a non-exception-shaped object, or an
// unthrown exception (trace == null) yield the zero-frame object. skipFrames as above;
// for a kind-0 trace it counts RESOLVED frames (an entry the fn table cannot resolve is
// dropped exactly as the render drops it, so skip counts only observable frames).
Dn2CppStackTrace* dn2cpp_stacktrace_of_exception(const Dn2CppTypeInfo* frameArrTi,
                                                 Dn2CppObject* ex, int32_t skipFrames);
// new StackTrace(StackFrame) — a trace over the ONE frame the caller supplied (kept
// verbatim, null included: real .NET stores it and reports FrameCount 1).
Dn2CppStackTrace* dn2cpp_stacktrace_of_frame(const Dn2CppTypeInfo* frameArrTi, Dn2CppObject* frame);
int32_t dn2cpp_stacktrace_frame_count(Dn2CppStackTrace* st);
Dn2CppArrayRef* dn2cpp_stacktrace_get_frames(Dn2CppStackTrace* st);
// StackTrace.GetFrame(i): the frame, or null when out of range (as in real .NET).
Dn2CppObject* dn2cpp_stacktrace_get_frame(Dn2CppStackTrace* st, int32_t index);
// StackTrace.ToString() — wired into the type-info's tostring slot, so Object.ToString
// on a StackTrace held as `object` dispatches here too.
Dn2CppString* dn2cpp_stacktrace_tostring(Dn2CppObject* st);
// new StackFrame(...): (null, 0, 0) for every capturing overload; the caller's values
// for the (fileName, line[, column]) forms.
Dn2CppStackFrame* dn2cpp_stackframe_new(Dn2CppString* fileName, int32_t line, int32_t column);
Dn2CppString* dn2cpp_stackframe_tostring(Dn2CppObject* sf);

// ── System.Diagnostics.Tracing.EventSource: the identity surface ─────────────────
// A native build delivers no events (no EventPipe/ETW, no EventListener), but a
// provider's IDENTITY — Name, Guid, Settings — is not about delivery: .NET computes all
// three from the type and the base-ctor arguments, with no listener involved, so dn2cpp
// can and does answer them exactly. See runtime/core/intrinsics/dn2cpp_eventsource.cpp
// for the boundary this does NOT cross.
//
// The receiver-taking forms read the type-info stamp (Dn2CppTypeInfo::eventSourceName /
// eventSourceGuid) unless the instance's base ctor supplied its own name/settings, which
// dn2cpp_eventsource_ctor records per instance.
Dn2CppString* dn2cpp_eventsource_name(Dn2CppObject* src);
void dn2cpp_eventsource_guid(Dn2CppObject* src, void* out16);
int32_t dn2cpp_eventsource_settings(Dn2CppObject* src);
// The base-ctor overloads that carry identity: `name` is null when the overload supplies
// none, `settings` is the raw EventSourceSettings the overload passes (validated here as
// .NET's ValidateSettings does). Recording is skipped when neither differs from what the
// type-info already implies, so a program whose providers all use [EventSource(Name=…)]
// records nothing at all.
void dn2cpp_eventsource_ctor(Dn2CppObject* src, Dn2CppString* name, int32_t settings);
// EventSource.GetName(Type) / GetGuid(Type): the same answers, keyed by the type rather
// than by an instance, so they never consult the per-instance record (neither does .NET).
Dn2CppString* dn2cpp_eventsource_type_name(const Dn2CppTypeInfo* ti);
void dn2cpp_eventsource_type_guid(const Dn2CppTypeInfo* ti, void* out16);
// EventSource.CurrentThreadActivityId / SetCurrentThreadActivityId: a per-thread Guid
// slot and nothing more, in .NET as here. `prev16` may be null (the one-argument
// overload); when non-null it receives the value the slot held.
void dn2cpp_eventsource_get_activity_id(void* out16);
void dn2cpp_eventsource_set_activity_id(const void* in16, void* prev16);

// ── System.Resources.ResourceManager ─────────────────────────────────────────────
// A ResourceManager is (base name, assembly) and nothing else here: the lookup reads
// the assembly's already-carried `<baseName>.resources` blob and parses the
// RuntimeResourceSet wire format below. The real type is NOT transpiled — its
// ManifestBasedResourceGroveler walks CultureInfo.Parent chains and probes satellite
// assemblies through RuntimeAssembly.InternalGetSatelliteAssembly, and dn2cpp models
// CultureInfo as a headerless `const Dn2CppNumberFormatInfo*` with no parent and no
// object identity, so that walk has nothing to walk. Intrinsic instead (the
// System.SR / ThrowHelper tree-shaking pattern): the whole groveler / ResourceReader /
// BinaryFormatter / satellite closure stays out of the tree.
struct Dn2CppResourceManager : Dn2CppObject
{
    Dn2CppString* baseName;      // ResourceManager.BaseName — never null
    const char* assemblyName;    // the assembly handle (a registry key), never null
};
extern const Dn2CppTypeInfo dn2cpp_resourcemanager_type;
// new ResourceManager(baseName, assembly) — `assembly` is the `const char*` handle
// System.Reflection.Assembly lowers to. A null baseName is ArgumentNullException, as
// .NET. No probing happens here: real .NET is lazy too, and the throw a missing set
// produces belongs to the first lookup, not to construction.
Dn2CppResourceManager* dn2cpp_resourcemanager_new(Dn2CppString* baseName, const char* assembly);
// new ResourceManager(Type resourceSource) — base name is the type's FullName and the
// assembly is the type's own, both one call away from the handle (which is why this is
// composed here rather than at the call site).
Dn2CppResourceManager* dn2cpp_resourcemanager_new_for_type(Dn2CppType* resourceSource);
Dn2CppString* dn2cpp_resourcemanager_base_name(Dn2CppResourceManager* rm);
// ResourceManager.GetString(name[, culture]) / GetObject(name[, culture]).
//
// `culture` is the requested CultureInfo — null for the no-culture overloads, which in
// real .NET read CurrentUICulture (folded to invariant here). The culture is the ONE
// place this model can be wrong, and it refuses rather than guess: dn2cpp links no
// satellite assemblies, so a request for a SPECIFIC culture cannot be distinguished
// from one whose satellite simply was not built — and serving the neutral string for
// both would be a silent wrong answer in a shipped game. Two asks are served: the
// invariant / empty-named one, and the assembly's DECLARED neutral culture (see
// Dn2CppAssemblyRegEntry::neutralResourcesCulture — real .NET rewrites that exact ask
// to the invariant one and probes no satellite for it). Any other culture name throws a
// catchable PlatformNotSupportedException naming the culture and the remedy — as does
// every ask, the culture-less one included, on an assembly whose declared ultimate
// fallback is a SATELLITE.
//
// A missing key is null (as .NET). A missing resource SET is a catchable
// MissingManifestResourceException — real .NET's own family — except on a
// --no-manifest-resources assembly, where the dropped-resources refusal fires first so
// a dropped set never reads as an absent one.
//
// GetObject decodes every PRIMITIVE ResourceTypeCode plus byte[], and refuses the two
// families it cannot answer: Stream (whose .NET answer is an UnmanagedMemoryStream over
// the mapped image) and the BinaryFormatter-serialized user types, permanently out of
// scope. `byteArrayType` is the Byte[] type-info the emit arm supplies, since only the
// emitter can name a precise array type-info. GetString over a non-String entry raises
// InvalidOperationException naming the type it found, as real .NET does.
Dn2CppString* dn2cpp_resourcemanager_get_string(Dn2CppResourceManager* rm, Dn2CppString* name,
    const Dn2CppNumberFormatInfo* culture);
Dn2CppObject* dn2cpp_resourcemanager_get_object(Dn2CppResourceManager* rm, Dn2CppString* name,
    const Dn2CppNumberFormatInfo* culture, const Dn2CppTypeInfo* byteArrayType);
// StackFrame's accessors are NULL-TOLERANT, deliberately diverging from .NET's
// NullReferenceException: GetFrame(i) is null for every i when nothing was walked, so a
// null StackFrame is something DN2CPP HANDS THE CALLER, not a caller bug — and a null
// frame is observationally identical to a walked one here (null method, null file,
// line 0). Trapping would abort any program that reads a stack frame at all, including
// GodotSharp's GD.PushError, which formats off GetCurrentStackFrame() with no null
// check. A frame the CALLER built from a (fileName, line) pair is never degraded.
Dn2CppString* dn2cpp_stackframe_file_name(Dn2CppStackFrame* sf);
int32_t dn2cpp_stackframe_line_number(Dn2CppStackFrame* sf);
int32_t dn2cpp_stackframe_column_number(Dn2CppStackFrame* sf);

void* dn2cpp_alloc(size_t size);

// Pointer-free ("atomic") GC allocation: the block is collectible and reachable
// like dn2cpp_alloc's, but the collector never scans its contents for pointers.
// Use it only for buffers that provably hold no managed references — string
// char16_t buffers, StringBuilder/interpolation buffers, primitive (int) array
// storage. Skipping the scan removes false-pointer retention and shortens the GC
// mark phase. Unlike dn2cpp_alloc, the storage is NOT zero-initialized; a caller
// that needs zeroed memory (e.g. `new int[]`) must memset it.
void* dn2cpp_alloc_atomic(size_t size);

// Pinned (uncollectable but scanned) allocation for objects whose only
// reference lives outside the GC heap (e.g. inside Godot engine objects).
void* dn2cpp_alloc_pinned(size_t size);
void dn2cpp_free_pinned(void* p);

// Incremental-GC write barriers. Call after publishing managed references.
// The first form requires an address inside GC-managed storage; the second is
// for an arbitrary byref which may instead name the stack or static data —
// both are always rescanned as roots, so neither needs dirtying.
//
// Barrier every managed-reference store rather than reasoning about the
// target's colour: a just-allocated object is not reliably white, since mark
// bits survive a generational cycle, and whether the conservative scan
// re-dirties an already-marked one depends on where the compiler kept the
// pointer — and stops happening at all under GC_ALL_INTERIOR_POINTERS.
void dn2cpp_gc_write_barrier(void* heapAddress);
void dn2cpp_gc_write_barrier_if_heap(void* address);

template <typename T>
inline void dn2cpp_gc_store_ref(T** slot, T* value)
{
    *slot = value;
    dn2cpp_gc_write_barrier(slot);
}

// The same store into a slot readers may load without holding the lock the writer
// holds: release, so acquiring the pointer acquires the referent's fields. The
// barrier still FOLLOWS the store and is unaffected by it — it takes the slot's
// address only, and dirties a page in the collector's own bitmap.
template <typename T>
inline void dn2cpp_gc_store_ref(std::atomic<T*>* slot, T* value)
{
    slot->store(value, std::memory_order_release);
    dn2cpp_gc_write_barrier(slot);
}

// Reference-bearing bulk moves must use this rather than raw memmove.
void dn2cpp_gc_memmove_refs(void* destination, const void* source, size_t bytes);

// True when `p` names memory the incremental collector may have write-protected.
//
// Both selectable backends run MANUAL_VDB, so no page is ever actually
// protected — but GC_incremental_protection_needs only says so on the fork
// (gcconfig.h forces MANUAL_VDB there, so it always answers GC_PROTECTS_NONE).
// Upstream is merely compiled with -DMANUAL_VDB, which leaves the platform's
// mprotect-style VDB compiled in and skips only the runtime probe that would
// zero this query out, so it answers a nonzero page-size heuristic instead —
// this function then answers 1 for a heap pointer under upstream's incremental
// mode, conservatively (never wrong, just never free there). It stays as the
// guard an mprotect-based VDB would need: a kernel store into a protected
// page fails EFAULT instead of reaching the fault handler a user-space store
// would trigger.
int dn2cpp_gc_kernel_write_unsafe(const void* p);

// Per-thread GC-visible block backing managed thread-static fields whose type
// holds GC references (generated code sizes and lays the block out). Lazily
// allocated on first access per thread — uncollectable (scanned as a GC root)
// and zero-filled, so a fresh thread sees default(T). Released when a
// runtime-spawned thread exits; the main thread's block lives for the process.
void* dn2cpp_threadstatic_block(int32_t size);

// Raw native (GC-unmanaged) heap — System.Runtime.InteropServices
// NativeMemory.{Alloc,AllocZeroed,Free,Realloc} and Marshal.{AllocHGlobal,
// FreeHGlobal}. These deliberately use the C allocator (std::malloc/calloc/
// realloc/free), NOT dn2cpp_alloc / the GC: the block is neither scanned nor
// collected, mirroring .NET's unmanaged heap. Do not store managed (GC)
// pointers in this memory — the collector cannot see them; blittable data only.
// Allocation failure raises OutOfMemoryException, matching .NET.
void* dn2cpp_native_alloc(size_t byteCount);
void* dn2cpp_native_alloc_checked(size_t elementCount, size_t elementSize);
void* dn2cpp_native_calloc(size_t elementCount, size_t elementSize);
void* dn2cpp_native_realloc(void* ptr, size_t byteCount);
void dn2cpp_native_free(void* ptr);

// Aligned native (GC-unmanaged) heap — NativeMemory.{AlignedAlloc,
// AlignedFree,AlignedRealloc}. Same GC-unmanaged C-heap as the raw native wrappers above,
// but the block is aligned to `alignment` (which must be a non-zero power of two,
// else ArgumentException). .NET rounds byteCount up to a multiple of alignment
// (std::aligned_alloc requires it); a zero byteCount still yields a valid aligned
// pointer. macOS/Linux release aligned_alloc memory with plain free, so
// AlignedFree shares the plain free path. (The Windows _aligned_malloc /
// _aligned_free ABI is carved out — self-contained binaries target clang/macOS.)
void* dn2cpp_native_aligned_alloc(size_t byteCount, size_t alignment);
void* dn2cpp_native_aligned_realloc(void* ptr, size_t byteCount, size_t alignment);
void dn2cpp_native_aligned_free(void* ptr);

// Non-generic Type-based blittable struct marshalling (the Type/object
// overloads carved out of the generic SizeOf<T>/PtrToStructure<T>/
// StructureToPtr<T>). The size and box layout come from the runtime type-info's
// instanceSize — for a blittable sequential value type the C++ struct's sizeof
// equals the .NET marshalled size. SizeOf returns instanceSize; PtrToStructure
// boxes instanceSize bytes read from native memory under the Type's type-info
// (IntPtr.Zero -> null, matching .NET); StructureToPtr copies a boxed struct's
// payload to native memory (size from the boxed object's own header).
int32_t dn2cpp_marshal_sizeof(const Dn2CppType* t);
// The shared marshalling verdict behind every Type-based Marshal entry point: the
// marshalled size of `ti`, or a throw naming why the type has none. See its definition
// in dn2cpp_marshal.cpp for the test ORDER, which is the contract.
int32_t dn2cpp_marshal_require_size(const Dn2CppTypeInfo* ti);
Dn2CppObject* dn2cpp_marshal_ptr_to_structure(const void* ptr, const Dn2CppType* t);
void dn2cpp_marshal_structure_to_ptr(Dn2CppObject* structure, void* ptr);

// Marshal.PtrToStringUni(IntPtr): decode a caller-owned NUL-terminated UTF-16
// buffer into a managed string. Unlike the P/Invoke string-return marshaller
// (dn2cpp_pinvoke_str_from_utf16) this never frees the buffer — the caller keeps
// ownership, matching Marshal.PtrToString* semantics. null -> null.
Dn2CppString* dn2cpp_marshal_ptr_to_string_utf16(const char16_t* p);

// Marshal.StringTo{HGlobal,CoTaskMem}{Ansi,Uni} / StringToCoTaskMemUTF8: encode a
// managed string into a freshly allocated NUL-terminated native buffer the CALLER
// must free (FreeHGlobal/FreeCoTaskMem — both dn2cpp_native_free). These allocate
// on the raw native (GC-unmanaged) heap, NOT via dn2cpp_alloc: the returned handle
// is handed to free(), so a GC-heap block here would corrupt the Boehm heap (which
// is why dn2cpp_pinvoke_str_to_utf8/utf16 — GC-allocated for [In] call lifetime —
// cannot be reused). Ansi == UTF-8 on Unix; HGlobal and CoTaskMem share the one
// process-heap allocator. null string -> null pointer, matching .NET.
void* dn2cpp_marshal_string_to_utf8(Dn2CppString* s);
// StringTo{HGlobal,CoTaskMem}Ansi: system-ANSI-code-page sibling of _to_utf8 (CP_ACP
// + best-fit on Windows, UTF-8 on Unix). StringToCoTaskMemUTF8 keeps _to_utf8.
void* dn2cpp_marshal_string_to_ansi(Dn2CppString* s);
void* dn2cpp_marshal_string_to_utf16(Dn2CppString* s);

// Marshal.ZeroFree{GlobalAlloc,CoTaskMem}{Ansi,Unicode} / ZeroFreeCoTaskMemUTF8:
// zero a NUL-terminated native string buffer (scanning to the NUL for its length,
// the .NET contract for buffers that held sensitive data), then free it. null is
// a no-op, matching .NET.
void dn2cpp_marshal_zero_free_utf8(void* p);
void dn2cpp_marshal_zero_free_utf16(void* p);

// Type checks and boxing.
const void** dn2cpp_resolve_interface(const Dn2CppTypeInfo* t, const Dn2CppTypeInfo* itf);
// Non-failing variant: nullptr when `t` does not implement `itf`.
const void** dn2cpp_try_resolve_interface(const Dn2CppTypeInfo* t, const Dn2CppTypeInfo* itf);
// The traps an interface / vtable slot with no implementing body is filled with (see the
// definitions). A call through 0x0 would say nothing. dn2cpp_itf_slot_missing reads the
// receiver out of argument 0 and names its type — the emitter enters it through a
// per-signature thunk carrying the slot's exact C++ signature (a wasm call_indirect
// checks the callee's type immediate, and with the signature exact `self` really is the
// receiver at the C++ level). For an indirect struct return the emitter prefers a tiny
// per-slot stub that calls _named with the (class, interface, method) descriptor baked
// in — exact even on a metadata-stripped image. _anon is kept for the rare slot the
// emitter has neither a signature nor a descriptor for.
[[noreturn]] void dn2cpp_itf_slot_missing(void* self);
[[noreturn]] void dn2cpp_itf_slot_missing_anon();
[[noreturn]] void dn2cpp_itf_slot_missing_named(const char* slotDesc);
Dn2CppObject* dn2cpp_isinst(Dn2CppObject* obj, const Dn2CppTypeInfo* ti);
// The pure (source type-info, target type-info) decision behind dn2cpp_isinst,
// cached per pair: base chain + interface rows, generic variance, and the array
// arms (array-to-array element covariance, System.Array, the non-generic and
// generic array collection interfaces). This is THE assignability rule —
// Type.IsAssignableFrom delegates here (dn2cpp_type_is_assignable_from), so a
// reflection answer and an isinst can never disagree about the same pair.
int32_t dn2cpp_typeinfo_assignable(const Dn2CppTypeInfo* st, const Dn2CppTypeInfo* ti);
Dn2CppObject* dn2cpp_castclass(Dn2CppObject* obj, const Dn2CppTypeInfo* ti);
Dn2CppObject* dn2cpp_box(const Dn2CppTypeInfo* ti, const void* value, size_t size);
// The same box where the caller cannot know the size — the type is a RuntimeTypeHandle,
// i.e. a run-time value (RuntimeHelpers.Box). Takes the payload width off the handle.
Dn2CppObject* dn2cpp_box_by_handle(const Dn2CppTypeInfo* ti, const void* value);
void* dn2cpp_unbox(Dn2CppObject* obj, const Dn2CppTypeInfo* ti);

// Builds a string from a static UTF-16 buffer (literals point at .rodata).
Dn2CppString* dn2cpp_string_literal(const char16_t* chars, int32_t length);
// string.Intern / IsInterned — the process-wide intern pool (every ldstr
// literal is interned at startup via dn2cpp_string_literal, matching .NET).
// Intern adds `s` on a miss and returns the pooled instance; IsInterned
// returns nullptr on a miss.
Dn2CppString* dn2cpp_string_intern(Dn2CppString* s);
Dn2CppString* dn2cpp_string_is_interned(Dn2CppString* s);
// Convert.ToChar(string) — exactly one code unit, else FormatException.
char16_t dn2cpp_convert_str_to_char(Dn2CppString* s);
// Convert.ToChar(integer) — OverflowException outside the code-unit range.
char16_t dn2cpp_convert_i32_to_char(int32_t v);

// Builds an independent (copied) string from `length` UTF-16 code units —
// `new string(ReadOnlySpan<char>/char[])` / span<char>.ToString() (the source may
// alias or be mutated later, so unlike _literal these copy). _repeat_char fills the
// `new string(char, count)` form.
Dn2CppString* dn2cpp_string_from_chars(const char16_t* chars, int32_t length);
// ISpanFormattable write of a formatted string into a char span (the .NET TryFormat
// contract): fits -> copy + *written = length + 1; too short -> untouched + *written = 0 + 0.
int32_t dn2cpp_string_try_copy_to_span(Dn2CppString* s, char16_t* dest, int32_t destLen, int32_t* written);
int32_t dn2cpp_string_try_copy_to_utf8_span(Dn2CppString* s, uint8_t* dest, int32_t destLen, int32_t* written);
// `new string(char* value)` — NUL-terminated; a null pointer yields Empty (the
// real BCL's documented behavior for this extern/VM-implemented ctor overload).
Dn2CppString* dn2cpp_string_from_wcs(const char16_t* value);
// `new string(sbyte* value)` — the narrow sibling of _from_wcs: a NUL-terminated
// byte run decoded through the system ANSI code page (CP_ACP on Windows, UTF-8 on
// POSIX — the dn2cpp_pal_ansi_decode seam), which is what the real ctor does.
// NOT Encoding.Default: .NET Core reports that as UTF-8 on every OS, yet this ctor
// still decodes with the CODE PAGE, and on a non-UTF-8 ACP the two genuinely disagree
// (on a CP932 host the bytes 82 A0 yield U+3042; read as UTF-8 they are mojibake), so
// decoding as UTF-8 would corrupt non-ASCII text silently. A null pointer yields Empty,
// like the char* overload.
Dn2CppString* dn2cpp_string_from_mbs(const char* value);
Dn2CppString* dn2cpp_string_repeat_char(char16_t c, int32_t count);

// String.ToCharArray() — a fresh char[] (a packed Dn2CppArrayN with
// elemSize 2) holding the string's UTF-16 code units verbatim (surrogate pairs stay
// as two separate code units; the empty string yields a length-0 array). `ti` is the
// precise `char[]` array type-info (so result.GetType() == typeof(char[])). A null
// receiver faults (NullReferenceException), matching .NET's instance call.
Dn2CppArrayN* dn2cpp_string_to_chararray(Dn2CppString* s, const Dn2CppTypeInfo* ti);

// Allocate an uninitialized writable string of `length` UTF-16 code units
// for string.Create<TState>; *outBuf receives the buffer the SpanAction fills
// (built into a Span<char> by the caller). Negative length throws
// ArgumentOutOfRangeException; length 0 yields the empty string.
Dn2CppString* dn2cpp_string_create_buffer(int32_t length, char16_t** outBuf);
// Low-level UTF-16 string allocator: reserves `length` code units (NUL-terminated)
// and hands back the writable buffer via *outBuf. The primitive behind most string
// builders; shared so per-namespace intrinsic units (e.g. DateTime formatting) can
// materialize result strings without re-implementing allocation.
Dn2CppString* dn2cpp_string_alloc(char16_t** outBuf, int32_t length);
// String.FastAllocateString(length): an uninitialized writable string the
// caller then fills through GetRawStringData (Guid.ToString and friends).
// Expression-shaped wrapper over dn2cpp_string_alloc.
Dn2CppString* dn2cpp_string_fast_allocate(int32_t length);
// Decodes a UTF-8 byte run into a freshly allocated UTF-16 string.
Dn2CppString* dn2cpp_string_from_utf8(const char* utf8, int32_t byteLength);
// Raw UTF-8 <-> UTF-16 transcode cores (no Dn2CppString allocation). Shared by the
// string codecs above and the POSIX ANSI PAL seam so the Unix Ansi path is byte-
// identical to UTF-8. _to_utf16 writes into `out` (>= byteLength+1 units) or, with a
// null `out`, only returns the unit count; _to_utf8 writes up to `bufSize` bytes or,
// with a null `buf`, only returns the required byte count.
int32_t dn2cpp_utf8_to_utf16(const char* utf8, int32_t byteLength, char16_t* out);
int32_t dn2cpp_utf16_to_utf8(const char16_t* src, int32_t len, char* buf, int32_t bufSize);
// System-ANSI-code-page string codec (real .NET's Ansi marshalling): the system code
// page on Windows, UTF-8 on POSIX. Mirrors dn2cpp_string_{to,from}_utf8 but routes
// through the ANSI PAL seam. `bestFit` selects best-fit substitution on encode: 1 for
// the default P/Invoke marshalling (é -> 'e'), 0 for Marshal.StringToHGlobalAnsi /
// StringToCoTaskMemAnsi (bestFit:false -> '?'). Ignored on POSIX.
int32_t dn2cpp_string_to_ansi(Dn2CppString* s, char* buf, int32_t bufSize, int32_t bestFit);
Dn2CppString* dn2cpp_string_from_ansi(const char* bytes, int32_t byteLength);
// Widens a known-ASCII byte run (digits/sign/exponent from a numeric render) to
// UTF-16 verbatim. Shared so per-namespace intrinsic units (e.g. Decimal) can
// build result strings without re-deriving the formatting machinery.
Dn2CppString* dn2cpp_string_from_ascii(const char* buf, int32_t len);
// Encodes a UTF-16 string as UTF-8. Returns the byte count (excluding the NUL).
// When `buf` is non-null, writes up to `bufSize` bytes (no NUL terminator).
// Pass a null `buf` to query the required size (two-call pattern).
int32_t dn2cpp_string_to_utf8(Dn2CppString* s, char* buf, int32_t bufSize);

// System.Text.Ascii.WidenAsciiToUtf16 / NarrowUtf16ToAscii — the leaf ASCII<->UTF-16
// transcode helpers (BCL: byte->char widen, char->byte narrow). The real bodies branch
// into SIMD `_Vector`/`_Intrinsified` fast paths over the generic ISimdVector
// abstraction (Vector128<byte>.ElementCount / Load / Widen ...) that dn2cpp does not
// transpile; at runtime the SIMD gate (Vector128.IsHardwareAccelerated) folds to false
// and the scalar fallback runs anyway. Each converts leading elements until the first
// non-ASCII (> 0x7F) element and returns the count converted — the BCL contract. The
// matching ResolveCallTarget cut keeps the real SIMD bodies out of the tree.
size_t dn2cpp_ascii_widen_to_utf16(const uint8_t* src, char16_t* dst, size_t count);
size_t dn2cpp_ascii_narrow_to_ascii(const char16_t* src, uint8_t* dst, size_t count);

// System.Text.Unicode.Utf8Utility.TranscodeToUtf8 — the strict UTF-16 -> UTF-8
// transcode workhorse shared by Utf8.FromUtf16 and UTF8Encoding.GetBytes. Returns the
// BCL OperationStatus (0 Done, 1 DestinationTooSmall, 2 NeedMoreData, 3 InvalidData)
// and writes the remaining input/output pointers (consumed prefix). The real body is
// a DWORD/SIMD pointer-stepped loop that dn2cpp mis-advances past a non-ASCII char
// following an ASCII run (dropping the tail / spuriously falling back); this scalar
// helper is exact. The matching ResolveCallTarget cut keeps the real body out of the
// tree. Strict (no replacement): a trailing high surrogate is NeedMoreData, any other
// ill-formed surrogate is InvalidData — the caller handles replacement/fallback.
int32_t dn2cpp_utf8_transcode_to_utf8(const char16_t* pIn, int32_t inLen,
                                      uint8_t* pOut, int32_t outLen,
                                      const char16_t** pInRem, uint8_t** pOutRem);

// Encoding.GetString. The real body reaches String.CreateStringFromEncoding -> the
// SIMD UTF-8 transcode subtree (Ascii.WidenAsciiToUtf16 / Vector128<byte>) dn2cpp never
// transpiles; these helpers replace that whole subtree with a portable, .NET-exact
// decode, and the matching ResolveCallTarget cut makes the real bodies unreachable.
// Includes each encoding's default DecoderReplacementFallback — see the per-helper
// notes for the exact replacement rule.
//
// _decode_ascii: maps bytes 0x00..0x7F directly and replaces each byte 0x80..0xFF
//   with '?' (ASCIIEncoding's default decoder replacement fallback).
Dn2CppString* dn2cpp_string_decode_ascii(const char* bytes, int32_t count);
// _decode_utf8: decodes a UTF-8 byte run, applying the Unicode "maximal subpart
//   of an ill-formed subsequence" replacement (Encoding.UTF8 / W3C best practice:
//   each maximal valid prefix of an ill-formed sequence yields ONE U+FFFD, and a
//   byte that cannot continue is reconsidered as a fresh start). This differs from
//   the lossy dn2cpp_string_from_utf8 (which emits a single U+FFFD per bad run);
//   GetString needs the exact .NET fallback, so it is a separate decoder.
Dn2CppString* dn2cpp_string_decode_utf8(const char* bytes, int32_t count);
// _decode_utf16le: decodes a little-endian UTF-16 byte run (UnicodeEncoding).
//   An odd trailing byte, a lone high surrogate, or a lone low surrogate each
//   decode to one U+FFFD (matching Encoding.Unicode's replacement fallback).
Dn2CppString* dn2cpp_string_decode_utf16le(const char* bytes, int32_t count);
// _decode_utf32le: decodes a little-endian UTF-32 byte run (UTF32Encoding).
//   An out-of-range or surrogate code point — and a trailing partial unit —
//   each decode to one U+FFFD (matching Encoding.UTF32's replacement fallback).
//   The GodotSharp bridge marshals godot_string (UTF-32) through this.
Dn2CppString* dn2cpp_string_decode_utf32le(const char* bytes, int32_t count);
// _encoding_get_string: the runtime entry the transpiler emits for a virtual
//   `Encoding::GetString(byte[], int, int)` call whose static receiver type is the
//   base System.Text.Encoding. Dispatches on the receiver's runtime type-info name:
//   System.Text.ASCIIEncoding -> _decode_ascii, System.Text.UTF8Encoding ->
//   _decode_utf8, System.Text.UnicodeEncoding -> _decode_utf16le,
//   System.Text.UTF32Encoding -> _decode_utf32le.
//   Any other encoding raises NotSupportedException (no silent
//   carve-out). `index`/`count` slice the byte[] (validated like .NET: null array ->
//   ArgumentNullException, out-of-range -> ArgumentOutOfRangeException). When the
//   transpiler statically knows the receiver is UTF8Encoding/UnicodeEncoding it
//   calls the specific decoder directly and never reaches this dispatcher.
Dn2CppString* dn2cpp_encoding_get_string(Dn2CppObject* encoding, Dn2CppArrayN* bytes,
                                         int32_t index, int32_t count);
// Slices and bounds-checks a byte[] for GetString(byte[], index, count), then calls
// the named decoder. Shared by the statically-typed UTF8Encoding/UnicodeEncoding
// paths (decoder chosen at emit time) so they validate identically to the dispatcher.
Dn2CppString* dn2cpp_encoding_decode_range(Dn2CppArrayN* bytes, int32_t index,
                                          int32_t count,
                                          Dn2CppString* (*decode)(const char*, int32_t));
// The pointer form Encoding.GetString(byte* bytes, int byteCount). Reached from SRM's
// metadata string decode (MetadataStringDecoder.GetString / MemoryBlock.PeekUtf8),
// which always uses the UTF-8 decoder. Dispatches on the receiver type-info name like
// dn2cpp_encoding_get_string. A negative count throws ArgumentOutOfRangeException; a
// null pointer throws ArgumentNullException even when count is zero, matching real
// .NET. The byte* is the raw buffer (no array header).
Dn2CppString* dn2cpp_encoding_get_string_ptr(Dn2CppObject* encoding, const char* bytes,
                                             int32_t count);

// P/Invoke string marshalling. The default/Ansi CharSet marshals
// strings as NUL-terminated UTF-8 on Unix.
//   _to_utf8:   managed string -> a NUL-terminated UTF-8 buffer for a native arg.
//     GC-allocated (no caller free) — it stays live across the [In] call and the
//     GC reclaims it afterwards. null string -> null pointer (matching .NET).
//   _from_utf8: a NUL-terminated UTF-8 pointer a native call returned -> a managed
//     string, then std::free(p): .NET's default string-return marshaller frees the
//     pointer (CoTaskMemFree == free on Unix), so a well-behaved callee returns
//     heap memory. null -> null.
char* dn2cpp_pinvoke_str_to_utf8(Dn2CppString* s);
Dn2CppString* dn2cpp_pinvoke_str_from_utf8(char* p);

// P/Invoke string marshalling under the default/Ansi CharSet (LPStr): the host default
// narrow encoding (system ANSI code page + best-fit on Windows, UTF-8 on Unix). Same
// GC-buffer / free-on-return discipline as the UTF-8 pair; only the byte encoding differs.
char* dn2cpp_pinvoke_str_to_ansi(Dn2CppString* s);
Dn2CppString* dn2cpp_pinvoke_str_from_ansi(char* p);

// P/Invoke string marshalling under CharSet.Unicode. LPWStr =
// NUL-terminated UTF-16, which is exactly Dn2CppString's internal representation, so
// these are a plain copy/decode (no transcoding).
//   _to_utf16:   managed string -> a NUL-terminated UTF-16 buffer for a native arg.
//     GC-allocated (no caller free) — it stays live across the [In] call and the GC
//     reclaims it afterwards. null string -> null pointer (matching .NET).
//   _from_utf16: a NUL-terminated UTF-16 pointer a native call returned -> a managed
//     string, then std::free(p) (the default string-return marshaller frees the
//     pointer, same as the UTF-8 path). null -> null.
char16_t* dn2cpp_pinvoke_str_to_utf16(Dn2CppString* s);
Dn2CppString* dn2cpp_pinvoke_str_from_utf16(char16_t* p);

// P/Invoke byref/[Out] string marshalling. An `out string` /
// `ref string` passes a pointer-to-pointer the native fills: dn2cpp hands it an
// intermediate void* temp by &tmp (seeded null for [Out], or with the [In] buffer
// for ref), then calls this to turn the post-call pointer into a managed string.
// `result` is what the native left in the temp; `inbuf` is the [In] buffer we seeded
// (null for [Out]). `enc` selects the decode: DN2CPP_STRENC_ANSI (system code page),
// DN2CPP_STRENC_UNICODE (UTF-16), or DN2CPP_STRENC_UTF8. Frees the pointer with free()
// (== CoTaskMemFree on Unix) ONLY when the native replaced it (result != inbuf): the
// [In] buffer is GC memory and must not be freed. null -> null.
//
// The P/Invoke string-encoding selector, shared by the transpiler and the flag-taking
// marshalling helpers. Values are stable (the transpiler emits the literal ints):
// Ansi = the host default narrow encoding, Unicode = UTF-16, Utf8 = explicit LPUTF8Str.
enum Dn2CppStrEnc
{
    DN2CPP_STRENC_ANSI = 0,
    DN2CPP_STRENC_UNICODE = 1,
    DN2CPP_STRENC_UTF8 = 2,
};
Dn2CppString* dn2cpp_pinvoke_byref_str_result(void* result, void* inbuf, int32_t enc);

// P/Invoke scalar char marshalling under the default/Ansi CharSet. On Unix that
// charset is UTF-8, and .NET marshals a single managed char to
// the FIRST byte of its UTF-8 encoding (lossy for non-ASCII — e.g. U+263A -> 0xE2;
// a lone surrogate encodes as the U+FFFD replacement, first byte 0xEF), and decodes
// a single native byte back as UTF-8 (only 0x00-0x7F form a complete sequence, so
// any 0x80-0xFF -> U+FFFD). Both verified vs real .NET. CharSet.Unicode needs no
// helper — it is a raw 2-byte UTF-16 code-unit passthrough woven at the call site.
uint8_t dn2cpp_pinvoke_char_to_ansi(char16_t c);
char16_t dn2cpp_pinvoke_char_from_ansi(uint8_t b);

// P/Invoke char[] marshalling under the default/Ansi CharSet = UTF-8 on Unix.
// Unlike a CharSet.Unicode char[] (a blittable UTF-16 buffer
// passed by data pointer), an Ansi char[] is encoded to a NUL-terminated UTF-8 buffer
// — the WHOLE array, embedded NULs included, so the buffer length and the array length
// diverge (probe-confirmed vs real .NET).
//   _chararr_to_ansi:   the array (char16_t elements) -> a GC-allocated NUL-terminated
//     UTF-8 buffer, over-sized so an [In,Out]/[Out] native can overwrite up to the
//     array length (over-allocation is unobservable — the write-back reads only to the
//     NUL). copyIn != 0 encodes the current content ([In]/[In,Out]); copyIn == 0 leaves
//     it empty ([Out]-only, no input copied). null array -> null pointer.
//   _chararr_from_ansi: decode the native's NUL-terminated UTF-8 back to UTF-16 and
//     copy up to the array length into the array; if shorter, write one NUL terminator
//     after it and leave the rest unchanged — exactly real .NET's char[] write-back.
//     Only emitted for [In,Out]/[Out]; the default direction is [In] (no write-back).
void* dn2cpp_pinvoke_chararr_to_ansi(Dn2CppArrayN* arr, int32_t copyIn);
void dn2cpp_pinvoke_chararr_from_ansi(Dn2CppArrayN* arr, void* buf);

// P/Invoke blittable-struct array marshalling. Unlike a primitive
// blittable array (int[]/double[]/...), which .NET pins and passes by pointer (so
// write-backs are visible regardless of [In]/[Out]), an array of a blittable value-type
// struct (Point[]) is marshalled BY COPY with direction semantics (probe-confirmed):
//   _blitarr_to_native: copy the managed buffer (length * elemSize bytes, the packed
//     Dn2CppArrayN stride == the native C array layout) into a fresh GC-allocated buffer
//     when copyIn != 0 ([In]/[In,Out]); zero it when copyIn == 0 ([Out]-only). null
//     array -> null pointer; non-null even for an empty array.
//   _blitarr_from_native: copy the native buffer back into the array (only emitted for
//     [In,Out]/[Out]; the default direction is [In], no write-back).
void* dn2cpp_pinvoke_blitarr_to_native(Dn2CppArrayN* arr, int32_t copyIn);
void dn2cpp_pinvoke_blitarr_from_native(Dn2CppArrayN* arr, void* buf);

// P/Invoke [MarshalAs(ByValArray, SizeConst=N)] inline fixed-length array
// struct field. The native struct embeds the elements INLINE (<elem> f[N]); the managed
// struct holds a managed array reference. A blittable element's packed managed stride
// equals its native width, so the marshal is a bounded memcpy.
//   _in (managed -> native): copy min(alen, n) elements from asrc into the inline buffer.
//     A source SHORTER than n throws ArgumentException (real .NET rejects it); a null
//     source (asrc == nullptr) zeroes the whole buffer; a longer source is truncated.
//   _out_{i4,n} (native -> managed): allocate a FRESH managed array of n elements (real
//     .NET never reuses the input on copy-back) in the element's rep (Dn2CppArrayI4 for
//     int/uint/4-byte-enum, Dn2CppArrayN otherwise), memcpy the n inline elements into it,
//     and return it for the field assignment.
void dn2cpp_pinvoke_byvalarr_in(void* dst, int32_t n, int32_t elemSize, const void* asrc, int32_t alen);
// `ti` is the element's precise array handle, supplied by the emitter's copy-back: real
// .NET allocates a genuine T[] here, so the fresh array must not report the untyped
// allocator's System.Object[] — which for the _n rep would also put a REF-array tag on
// packed value storage. Null degrades to the shared handle.
Dn2CppArrayI4* dn2cpp_pinvoke_byvalarr_out_i4(const void* src, int32_t n,
                                              const Dn2CppTypeInfo* ti);
Dn2CppArrayN* dn2cpp_pinvoke_byvalarr_out_n(const void* src, int32_t n, int32_t elemSize,
                                            const Dn2CppTypeInfo* ti);

// P/Invoke string[] marshalling. A managed string[] marshals as an
// array of pointers (char** / char16_t**), each element a NUL-terminated UTF-8
// (default/Ansi) or UTF-16 (CharSet.Unicode) buffer (a null element -> a null pointer).
// The pointer array is GC-allocated (Boehm-conservative scanning, so the per-element
// buffers stay live across the native call) and never null even for an empty array; a
// null array -> a null pointer.
//   _to_utf8/_to_utf16: encode each element into a slot when copyIn != 0 ([In]/[In,Out]);
//     zero the slots when copyIn == 0 ([Out]-only, no input read).
//   _from_utf8/_from_utf16: for [In,Out]/[Out], decode each slot's (possibly native-
//     replaced) pointer into a fresh managed string written back into the array. A slot the
//     native REPLACED (buf[i] != the pre-call snapshot inbuf[i]) is a native heap pointer,
//     so it is freed after decoding — matching .NET's CoTaskMemFree-on-Unix ownership. A
//     slot the native LEFT (buf[i] == inbuf[i]) still holds our GC-allocated [In] buffer
//     (or both are null), which is decoded but NEVER freed (free()ing a GC pointer would
//     corrupt the Boehm heap). null array -> no-op.
//   _ptrarr_dup: a GC-allocated snapshot of buf taken just before the native call, used as
//     the inbuf reference above; being GC memory it also roots every original [In] buffer
//     across the call. null buf -> null.
void** dn2cpp_pinvoke_strarr_to_utf8(Dn2CppArrayRef* arr, int32_t copyIn);
void** dn2cpp_pinvoke_strarr_to_utf16(Dn2CppArrayRef* arr, int32_t copyIn);
// Default/Ansi-CharSet sibling of _to_utf8/_from_utf8 (host default narrow encoding).
void** dn2cpp_pinvoke_strarr_to_ansi(Dn2CppArrayRef* arr, int32_t copyIn);
void dn2cpp_pinvoke_strarr_from_utf8(Dn2CppArrayRef* arr, void** buf, void** inbuf);
void dn2cpp_pinvoke_strarr_from_utf16(Dn2CppArrayRef* arr, void** buf, void** inbuf);
void dn2cpp_pinvoke_strarr_from_ansi(Dn2CppArrayRef* arr, void** buf, void** inbuf);
void** dn2cpp_pinvoke_ptrarr_dup(void** buf, int32_t n);

// P/Invoke last-error. A [DllImport(SetLastError = true)] call
// captures the platform error immediately after the native call into a per-thread
// slot, which Marshal.GetLastWin32Error/GetLastPInvokeError/GetLastSystemError read
// back. Matches .NET: only a SetLastError P/Invoke writes the slot, and it must be
// read before any later P/Invoke overwrites it.
//
// Two entry points into the same slot:
//   dn2cpp_pinvoke_capture_last_error(void) — the codegen'd post-call hook
//     (MethodCompiler.EmitPInvokeCall): captures the platform's OWN notion of the
//     last error (errno on POSIX, GetLastError() on Windows — see the .cpp) as its
//     first statement, so nothing else the function does can perturb it.
//   dn2cpp_pinvoke_set_last_error(int32_t e) — Marshal.SetLastPInvokeError's direct
//     write of an explicit managed value (no platform capture involved).
void dn2cpp_pinvoke_capture_last_error(void);
void dn2cpp_pinvoke_set_last_error(int32_t e);
int32_t dn2cpp_marshal_get_last_error(void);

// System.Runtime.InteropServices.NativeLibrary.GetSymbol(IntPtr handle, string
// symbolName, bool throwOnError)'s real body is a QCall ("NativeLibrary_GetSymbol")
// into the CLR's own native runtime, unlinkable from a transpiled image (there is no
// CoreCLR host). For a plain handle (no COM, no AssemblyLoadContext bookkeeping —
// dn2cpp has neither) that QCall reduces to exactly this: GetProcAddress/dlsym on
// the narrowed (always-ASCII) export name. `throwOnError` is handled by the caller
// (the intrinsic call site), not here — this returns null on a missing symbol either
// way. Reached from Path.GetTempPath's win-x64 GetTempPath2W feature probe (via
// NativeLibrary.TryGetExport/GetExport).
void* dn2cpp_native_library_get_symbol(void* handle, Dn2CppString* symbolName);

// The non-cryptographic random source behind Interop.GetRandomBytes
// (-> Sys::GetNonCryptographicallySecureRandomBytes on Unix), an InternalCall with
// no IL body. dn2cpp models it as a DETERMINISTIC fixed-seed byte fill: its only
// console-self-host consumer is System.HashCode.GenerateGlobalSeed, which needs one
// stable process-global seed. A fixed (non-random) seed keeps every hash function
// deterministic, so Dictionary/HashSet behaviour stays exact — lookups and
// insertion-order enumeration are hash-value-independent (a different seed only
// changes internal bucketing, never an observed result). Fills `length` bytes at
// `buffer` with a well-mixed xorshift32 stream from a fixed non-zero seed.
void dn2cpp_fill_nonsecure_random(void* buffer, int32_t length);
Dn2CppString* dn2cpp_int_to_string(int32_t v);
Dn2CppString* dn2cpp_long_to_string(int64_t v);
Dn2CppString* dn2cpp_double_to_string(double v);
Dn2CppString* dn2cpp_float_to_string(float v);
Dn2CppString* dn2cpp_bool_to_string(int32_t v);
Dn2CppString* dn2cpp_string_concat2(Dn2CppString* a, Dn2CppString* b);
Dn2CppString* dn2cpp_string_concat3(Dn2CppString* a, Dn2CppString* b, Dn2CppString* c);
Dn2CppString* dn2cpp_string_concat4(Dn2CppString* a, Dn2CppString* b, Dn2CppString* c, Dn2CppString* d);
// Concatenate `n` parts left-to-right (nulls skipped). Shared so per-namespace
// intrinsic units (e.g. value concatenation) build joined strings uniformly.
Dn2CppString* dn2cpp_string_concat_n(Dn2CppString** parts, int n);
// The OrdinalIgnoreCase single-char fold — .NET's internal
// System.Globalization.OrdinalCasing.ToUpper over the BMP, exact.
// Supplementary-plane folds remain per-code-unit identity. Shared across the
// per-namespace string/format intrinsic units, and defined INLINE here: no
// build uses LTO, so the OrdinalIgnoreCase equality/compare/hash loops would
// otherwise pay a cross-TU call per character. The generated two-level delta
// tables (fold(c) = char16_t(c + delta), identity pages deduplicated behind
// the page index) live in dn2cpp_ordinal_casing.cpp.
extern const uint8_t dn2cpp_ord_upper_page_index[256];
extern const uint16_t dn2cpp_ord_upper_delta[21][256];
inline char16_t dn2cpp_ordinal_upper(char16_t c)
{
    return static_cast<char16_t>(
        c + dn2cpp_ord_upper_delta[dn2cpp_ord_upper_page_index[c >> 8]][c & 0xFF]);
}
// The invariant simple one-to-one case maps — char.ToUpperInvariant /
// ToLowerInvariant over the BMP, exact (generated two-level delta tables in
// dn2cpp_invariant_casing.cpp). Distinct from dn2cpp_ordinal_upper: the
// invariant upper map uppercases U+017F LONG S to U+0053 while the ordinal
// fold keeps it. Supplementary-plane mappings remain per-code-unit identity.
char16_t dn2cpp_char_upper_invariant(char16_t c);
char16_t dn2cpp_char_lower_invariant(char16_t c);

// String.Concat(ReadOnlySpan<char>...) — modern Roslyn lowers a char in a
// string concat (`someChar.ToString() + s`) to the span overload to skip the
// single-char allocation. Each operand is a (ptr,len) pair lifted from the
// ReadOnlySpan<char> value; a null ptr / non-positive len contributes nothing.
Dn2CppString* dn2cpp_string_concat_spanchars2(const char16_t* p0, int32_t n0, const char16_t* p1, int32_t n1);
Dn2CppString* dn2cpp_string_concat_spanchars3(const char16_t* p0, int32_t n0, const char16_t* p1, int32_t n1, const char16_t* p2, int32_t n2);
Dn2CppString* dn2cpp_string_concat_spanchars4(const char16_t* p0, int32_t n0, const char16_t* p1, int32_t n1, const char16_t* p2, int32_t n2, const char16_t* p3, int32_t n3);
Dn2CppString* dn2cpp_string_concat_spanchars5(const char16_t* p0, int32_t n0, const char16_t* p1, int32_t n1, const char16_t* p2, int32_t n2, const char16_t* p3, int32_t n3, const char16_t* p4, int32_t n4);
int32_t dn2cpp_string_equals(Dn2CppString* a, Dn2CppString* b);
// Case-insensitive string equals (ordinal BMP fold when ignoreCase != 0);
// used by Enum.Parse/TryParse's ignoreCase overload.
int32_t dn2cpp_string_equals_ci(Dn2CppString* a, Dn2CppString* b, int32_t ignoreCase);
// Enum.Parse/TryParse worker: comma-separated name|numeric tokens OR'd
// into *out; returns 1 on success, 0 on a malformed/unmatched token.
// The numeric token's range is the UNDERLYING type's, never the model's:
// 300 fits the int32 model but not `enum E : byte`, where real .NET raises
// OverflowException. `uWidth` (1/2/4/8) + `uUnsigned` describe that range; a decimal
// outside it sets *overflow (may be null) and fails, which Parse renders as
// OverflowException while a name miss stays ArgumentException.
int32_t dn2cpp_enum_parse(Dn2CppString* s, const int32_t* values, Dn2CppString* const* names, int32_t count, int32_t ignoreCase, int32_t uWidth, int32_t uUnsigned, int32_t* out, int32_t* overflow);
// The same worker at the 64-bit model width, for a long/ulong-underlying enum
// whose members do not survive the int32 table.
int32_t dn2cpp_enum_parse64(Dn2CppString* s, const int64_t* values, Dn2CppString* const* names, int32_t count, int32_t ignoreCase, int32_t uWidth, int32_t uUnsigned, int64_t* out, int32_t* overflow);
// THE single StringComparison map (dn2cpp_system_string.cpp): folds every
// culture-sensitive StringComparison value — CurrentCulture (0),
// CurrentCultureIgnoreCase (1), InvariantCulture (2),
// InvariantCultureIgnoreCase (3) — onto Ordinal (4) / OrdinalIgnoreCase (5),
// preserving the IgnoreCase bit; an out-of-range value throws a catchable
// ArgumentException (.NET's CheckStringComparison). Every runtime helper that
// dispatches on a StringComparison operand routes through this, and so does
// the emitted GetHashCode(ReadOnlySpan<char>, StringComparison) dispatch —
// do not re-derive the mapping at a call site.
int32_t dn2cpp_str_comparison_fold(int32_t comparisonType);
// String.Compare / CompareOrdinal: ordinal (and OrdinalIgnoreCase)
// comparison over the UTF-16 buffer. Returns the .NET difference — the signed
// gap of the first differing code unit, else the length difference; null sorts
// first (null,null -> 0). `comparisonType` is the StringComparison enum,
// folded through dn2cpp_str_comparison_fold (culture-sensitive values compare
// ordinally — the invariant-globalization posture).
int32_t dn2cpp_str_compare(Dn2CppString* a, Dn2CppString* b, int32_t comparisonType);
int32_t dn2cpp_str_compare_sub(Dn2CppString* a, int32_t indexA, Dn2CppString* b,
                               int32_t indexB, int32_t length, int32_t comparisonType);
// Deterministic content hash for string keys (devirtualized
// EqualityComparer<string>.Default.GetHashCode). Not the BCL's randomized Marvin
// hash — we only need it stable + well-distributed within a run.
int32_t dn2cpp_string_hashcode(Dn2CppString* s);
// Ordinal (case-sensitive) hash over a raw char16_t buffer; string.GetHashCode
// delegates to it, so a ReadOnlySpan<char> and the equal string hash alike.
int32_t dn2cpp_chars_hashcode(const char16_t* p, int32_t length);
// OrdinalIgnoreCase comparison/hash over raw char16_t buffers (the exact BMP
// dn2cpp_ordinal_upper fold, the same primitive as dn2cpp_str_compare's case-5
// path). These back the Globalization.Ordinal.EqualsIgnoreCase /
// CompareStringIgnoreCase and String.GetHashCodeOrdinalIgnoreCase
// interceptions, replacing the real CompareStringIgnoreCaseNonAscii ->
// GlobalizationMode -> ICU cascade with the same per-code-unit results.
// Equality and hash share the fold so they stay consistent for hashed lookups
// (the case-insensitive JSON property-name dictionary).
int32_t dn2cpp_chars_equals_ignorecase_ordinal(const char16_t* a, const char16_t* b, int32_t length);
int32_t dn2cpp_chars_compare_ignorecase_ordinal(const char16_t* a, int32_t lengthA, const char16_t* b, int32_t lengthB);
// Case-sensitive sibling of the three-way compare: pure ordinal code-unit
// comparison, backing String.CompareOrdinal(ReadOnlySpan<char>, ReadOnlySpan<char>)
// (MemoryExtensions.CompareTo's Ordinal arm).
int32_t dn2cpp_chars_compare_ordinal(const char16_t* a, int32_t lengthA, const char16_t* b, int32_t lengthB);
int32_t dn2cpp_chars_indexof_ignorecase_ordinal(const char16_t* source, int32_t sourceLength,
                                              const char16_t* value, int32_t valueLength);
int32_t dn2cpp_chars_hashcode_oic(const char16_t* p, int32_t length);
int32_t dn2cpp_string_hashcode_oic(Dn2CppString* s);
int32_t dn2cpp_string_get_char(Dn2CppString* s, int32_t index);
// System.Char's (string, int) statics reject null and a bad index as argument errors,
// unlike String.get_Chars, whose corresponding failures are null-reference/index errors.
int32_t dn2cpp_char_get_string_char(Dn2CppString* s, int32_t index);
// char.GetUnicodeCategory(char) — the System.Globalization.UnicodeCategory
// value (0..29) from a two-level BMP table generated from the real .NET
// data (dn2cpp_unicode_category.cpp); exact for every BMP code point.
int32_t dn2cpp_char_unicode_category(char16_t c);

// Common System.String members (ordinal). UTF-16 code-unit based; the
// ignore-case comparisons use the exact BMP ordinal fold, and ToUpper/
// ToLower (dn2cpp_str_to_case) the exact BMP invariant case maps.
Dn2CppString* dn2cpp_str_substring(Dn2CppString* s, int32_t start, int32_t length);
// The "…to the end of the string" forms. Their missing bound is derived from the
// RECEIVER's own Length, and computing it at the CALL SITE would dereference the
// receiver before the helper's own null check can answer, turning a catchable
// NullReferenceException into a SIGSEGV. Computing it here is free.
Dn2CppString* dn2cpp_str_substring_to_end(Dn2CppString* s, int32_t start);
Dn2CppString* dn2cpp_str_remove_to_end(Dn2CppString* s, int32_t start);
int32_t dn2cpp_str_indexof_char_to_end(Dn2CppString* s, char16_t c, int32_t start);
int32_t dn2cpp_str_indexofany_to_end(Dn2CppString* s, const char16_t* set, int32_t setLen,
                                     int32_t start);
int32_t dn2cpp_str_lastindexofany_all(Dn2CppString* s, const char16_t* set, int32_t setLen);
int32_t dn2cpp_str_indexof_char(Dn2CppString* s, char16_t c, int32_t start);
int32_t dn2cpp_str_indexof_str(Dn2CppString* s, Dn2CppString* sub);
int32_t dn2cpp_str_startswith(Dn2CppString* s, Dn2CppString* prefix);
int32_t dn2cpp_str_endswith(Dn2CppString* s, Dn2CppString* suffix);
// char / (string, StringComparison) / last-index String overloads. The
// comparison helpers fold their StringComparison operand through
// dn2cpp_str_comparison_fold: Ordinal (4) exact, OrdinalIgnoreCase (5) the
// exact BMP fold, and the culture-sensitive values (0..3) mapped onto those
// two (the invariant-globalization posture).
int32_t dn2cpp_str_startswith_char(Dn2CppString* s, char16_t c);
int32_t dn2cpp_str_endswith_char(Dn2CppString* s, char16_t c);
int32_t dn2cpp_str_lastindexof_char(Dn2CppString* s, char16_t c);
int32_t dn2cpp_str_startswith_cmp(Dn2CppString* s, Dn2CppString* prefix, int32_t comparison);
int32_t dn2cpp_str_endswith_cmp(Dn2CppString* s, Dn2CppString* suffix, int32_t comparison);
// Contains(string, StringComparison) — substring search under the same
// Ordinal/OrdinalIgnoreCase comparisons.
int32_t dn2cpp_str_contains_cmp(Dn2CppString* s, Dn2CppString* sub, int32_t comparison);
// IndexOf(string, StringComparison): first match index or -1 (empty sub -> 0),
// same Ordinal/OrdinalIgnoreCase comparisons.
int32_t dn2cpp_str_indexof_cmp(Dn2CppString* s, Dn2CppString* sub, int32_t comparison);
int32_t dn2cpp_str_indexof_str_cmp(Dn2CppString* s, Dn2CppString* sub, int32_t startIndex, int32_t comparison);
// LastIndexOf(string, int startIndex[, int count], StringComparison): backward
// search over the count positions ending at startIndex; a match must lie
// entirely within that range (i.e. END at or before startIndex — the .NET 5+
// contract), -1 when absent. The _cmp form searches startIndex + 1 positions
// (the 2-arg delegation); startIndex == Length is adjusted down one, an empty
// needle reports startIndex + 1 post-adjustment, and bad start/count throws a
// catchable ArgumentOutOfRangeException.
int32_t dn2cpp_str_lastindexof_str_range(Dn2CppString* s, Dn2CppString* sub, int32_t startIndex,
                                         int32_t count, int32_t comparison);
int32_t dn2cpp_str_lastindexof_str_cmp(Dn2CppString* s, Dn2CppString* sub, int32_t startIndex, int32_t comparison);
// Whole-string LastIndexOf(string[, comparison]) — empty needle found at Length.
int32_t dn2cpp_str_lastindexof_cmp(Dn2CppString* s, Dn2CppString* sub, int32_t comparison);
int32_t dn2cpp_str_lastindexof_str(Dn2CppString* s, Dn2CppString* sub);
// IndexOf/LastIndexOf(char, startIndex[, count]) with the .NET range checks
// (catchable ArgumentOutOfRangeException; the backward form returns -1 for an
// empty source before validating), and IndexOf(char, StringComparison)
// (Ordinal/OrdinalIgnoreCase; culture values trap).
int32_t dn2cpp_str_indexof_char_range(Dn2CppString* s, char16_t c, int32_t start, int32_t count);
int32_t dn2cpp_str_lastindexof_char_range(Dn2CppString* s, char16_t c, int32_t start, int32_t count);
int32_t dn2cpp_str_indexof_char_cmp(Dn2CppString* s, char16_t c, int32_t comparison);
// IndexOfAny/LastIndexOfAny over an explicit char set (null set -> catchable
// ArgumentNullException, empty set -> -1; range checks as above).
int32_t dn2cpp_str_indexofany(Dn2CppString* s, const char16_t* set, int32_t setLen,
                              int32_t start, int32_t count);
int32_t dn2cpp_str_lastindexofany(Dn2CppString* s, const char16_t* set, int32_t setLen,
                                  int32_t start, int32_t count);
Dn2CppString* dn2cpp_str_to_case(Dn2CppString* s, int32_t toUpper);
Dn2CppString* dn2cpp_str_replace_char(Dn2CppString* s, char16_t oldc, char16_t newc);
Dn2CppString* dn2cpp_str_replace_str(Dn2CppString* s, Dn2CppString* oldValue, Dn2CppString* newValue);
// Replace(string, string, StringComparison): Ordinal (4) / OrdinalIgnoreCase
// (5, BMP fold); an out-of-range comparison value is a catchable
// ArgumentException checked before the oldValue checks, culture values trap.
Dn2CppString* dn2cpp_str_replace_str_cmp(Dn2CppString* s, Dn2CppString* oldValue,
                                         Dn2CppString* newValue, int32_t comparison);
// Normalize/IsNormalized under the invariant-globalization model: the input
// is treated as already normalized (returned as-is / true) after the
// NormalizationForm (1/2/5/6) is validated with a catchable ArgumentException.
Dn2CppString* dn2cpp_str_normalize(Dn2CppString* s, int32_t form);
int32_t dn2cpp_str_is_normalized(Dn2CppString* s, int32_t form);
// ToCharArray(startIndex, length) — substring copy into a fresh char[]; bad
// start/length raise a catchable ArgumentOutOfRangeException.
Dn2CppArrayN* dn2cpp_string_to_chararray_range(Dn2CppString* s, int32_t startIndex,
                                               int32_t length, const Dn2CppTypeInfo* ti);
// CopyTo(sourceIndex, char[], destinationIndex, count) — legacy array form
// (null destination ANE first, then catchable AOORE range checks).
void dn2cpp_str_copyto_chararray(Dn2CppString* s, int32_t sourceIndex, Dn2CppArrayN* destination,
                                 int32_t destinationIndex, int32_t count);
// IndexOf(string, startIndex, count[, comparison]) — forward search bounded
// to the count positions from startIndex (a match must fit inside).
int32_t dn2cpp_str_indexof_str_range(Dn2CppString* s, Dn2CppString* sub, int32_t startIndex,
                                     int32_t count, int32_t comparison);
// MemoryExtensions.ToUpperInvariant/ToLowerInvariant(src, dst): BMP invariant
// fold; returns srcLen, or -1 when dst is too short. No overlap detection.
int32_t dn2cpp_span_case_invariant(const char16_t* src, int32_t srcLen, char16_t* dst,
                                   int32_t dstLen, int32_t toUpper);
Dn2CppString* dn2cpp_str_trim(Dn2CppString* s);
// Trim/TrimStart/TrimEnd over an explicit set (mode 0 both / 1 start / 2 end);
// a null or empty set trims full .NET whitespace. Returns the original
// instance when nothing is trimmed (ReferenceEquals-visible, like .NET).
Dn2CppString* dn2cpp_str_trim_set(Dn2CppString* s, const char16_t* set, int32_t setLen, int32_t mode);
// PadLeft/PadRight (padLeft != 0 for left), Remove (count chars at start) and
// Insert (value at start); out-of-range traps like ArgumentOutOfRangeException.
Dn2CppString* dn2cpp_str_pad(Dn2CppString* s, int32_t totalWidth, char16_t pad, int32_t padLeft);
Dn2CppString* dn2cpp_str_remove(Dn2CppString* s, int32_t start, int32_t count);
Dn2CppString* dn2cpp_str_insert(Dn2CppString* s, int32_t start, Dn2CppString* value);
// string.Format composite formatting: holes `{index[,alignment][:spec]}`, with
// `{{`/`}}` for literal braces. The arity wrappers cover the (string, object...)
// overloads; the _arr variant the params object[] overload.
Dn2CppString* dn2cpp_string_format1(Dn2CppString* fmt, Dn2CppObject* a0);
Dn2CppString* dn2cpp_string_format2(Dn2CppString* fmt, Dn2CppObject* a0, Dn2CppObject* a1);
Dn2CppString* dn2cpp_string_format3(Dn2CppString* fmt, Dn2CppObject* a0, Dn2CppObject* a1, Dn2CppObject* a2);
Dn2CppString* dn2cpp_string_format_arr(Dn2CppString* fmt, Dn2CppArrayRef* args);
// string.Format(IFormatProvider, …) — composite formatting under a culture.
Dn2CppString* dn2cpp_string_format1_c(const Dn2CppNumberFormatInfo* n, Dn2CppString* fmt, Dn2CppObject* a0);
Dn2CppString* dn2cpp_string_format2_c(const Dn2CppNumberFormatInfo* n, Dn2CppString* fmt, Dn2CppObject* a0, Dn2CppObject* a1);
Dn2CppString* dn2cpp_string_format3_c(const Dn2CppNumberFormatInfo* n, Dn2CppString* fmt, Dn2CppObject* a0, Dn2CppObject* a1, Dn2CppObject* a2);
Dn2CppString* dn2cpp_string_format_arr_c(const Dn2CppNumberFormatInfo* n, Dn2CppString* fmt, Dn2CppArrayRef* args);
// The params ReadOnlySpan<object> overloads (net10; Roslyn prefers them for 4+
// loose args): the span lowers to its data pointer + length.
Dn2CppString* dn2cpp_string_format_spanobjs(Dn2CppString* fmt, Dn2CppObject* const* args, int32_t n);
Dn2CppString* dn2cpp_string_format_spanobjs_c(const Dn2CppNumberFormatInfo* nfi, Dn2CppString* fmt,
                                              Dn2CppObject* const* args, int32_t n);
// String.Split engine behind every Split overload -> string[]. Separator
// selection: string-array mode when `sepStrs` has entries (first matching
// entry in array order wins at each position, null/empty entries skipped,
// non-overlapping scan), else char-set mode over `sepChars` (any-of), else
// whitespace mode (any char.IsWhiteSpace char). `count` caps the KEPT entries
// (the remainder — separators included — is the last entry once count-1 are
// kept); `opts` is the StringSplitOptions value (bit 0 = RemoveEmptyEntries,
// bit 1 = TrimEntries; entries trim before the empty check). count < 0 throws
// ArgumentOutOfRangeException, opts outside [0, 3] ArgumentException (both
// catchable). The _char/_str forms wrap the single-separator overloads; a
// null/empty single STRING separator yields the whole string as sole entry
// (only a null/empty separator ARRAY means whitespace mode, like .NET).
Dn2CppArrayRef* dn2cpp_str_split(Dn2CppString* s, const char16_t* sepChars, int32_t nSepChars,
                                 Dn2CppArrayRef* sepStrs, int32_t count, int32_t opts);
Dn2CppArrayRef* dn2cpp_str_split_char(Dn2CppString* s, char16_t sep, int32_t count, int32_t opts);
Dn2CppArrayRef* dn2cpp_str_split_str(Dn2CppString* s, Dn2CppString* sep, int32_t count, int32_t opts);

// Integer parsing (invariant: optional surrounding whitespace, optional sign,
// decimal digits). TryParse writes the result and returns 0/1; Parse traps on a
// malformed/out-of-range input. IsNullOrWhiteSpace is null-or-all-whitespace.
int32_t dn2cpp_int_tryparse(Dn2CppString* s, int32_t* out);
int32_t dn2cpp_int_parse(Dn2CppString* s);
int32_t dn2cpp_long_tryparse(Dn2CppString* s, int64_t* out);
int64_t dn2cpp_long_parse(Dn2CppString* s);
// bool.TryParse: "True"/"False" ordinal-case-insensitive after trimming; 0/1.
int32_t dn2cpp_bool_tryparse(Dn2CppString* s, uint8_t* out);
// Boolean.IsTrueStringIgnoreCase / IsFalseStringIgnoreCase(ReadOnlySpan<char>):
// exact-literal ordinal-case-insensitive match (no trimming); 0/1.
int32_t dn2cpp_bool_is_true_chars(const char16_t* chars, int32_t len);
int32_t dn2cpp_bool_is_false_chars(const char16_t* chars, int32_t len);
// double/float Parse/TryParse: optional surrounding whitespace, then a decimal
// real number parsed by strtod under the C locale (invariant '.'), rejecting
// trailing garbage. float parses as double then narrows. Parse traps, TryParse
// writes the default (0) on failure and returns 0/1.
int32_t dn2cpp_double_tryparse(Dn2CppString* s, double* out);
double dn2cpp_double_parse(Dn2CppString* s);
int32_t dn2cpp_float_tryparse(Dn2CppString* s, float* out);
float dn2cpp_float_parse(Dn2CppString* s);
// Culture-aware parse: the decimal separator / negative sign / group
// separator are read from the provider before delegating to the invariant parser.
double dn2cpp_double_parse_c(Dn2CppString* s, const Dn2CppNumberFormatInfo* nfi);
float dn2cpp_float_parse_c(Dn2CppString* s, const Dn2CppNumberFormatInfo* nfi);
int32_t dn2cpp_double_tryparse_c(Dn2CppString* s, const Dn2CppNumberFormatInfo* nfi, double* out);
int32_t dn2cpp_float_tryparse_c(Dn2CppString* s, const Dn2CppNumberFormatInfo* nfi, float* out);
int32_t dn2cpp_int_parse_c(Dn2CppString* s, const Dn2CppNumberFormatInfo* nfi);
int64_t dn2cpp_long_parse_c(Dn2CppString* s, const Dn2CppNumberFormatInfo* nfi);
int32_t dn2cpp_int_tryparse_c(Dn2CppString* s, const Dn2CppNumberFormatInfo* nfi, int32_t* out);
int32_t dn2cpp_long_tryparse_c(Dn2CppString* s, const Dn2CppNumberFormatInfo* nfi, int64_t* out);
int32_t dn2cpp_str_is_null_or_whitespace(Dn2CppString* s);

// ---- NumberStyles parse engine (intrinsics/dn2cpp_parse.cpp) ----------------
// The System.Number TryParseNumber state machine over the modeled
// NumberFormatInfo, feeding width-parameterized integer conversion, the
// strtod/strtof-backed float conversion, and (via Dn2CppNumberParse) the
// decimal styles parser. Parse forms throw the mapped managed exception
// (Format/Overflow/Argument[Null]); TryParse forms return 0/1 and write the
// default on failure, but still throw ArgumentException on an invalid styles
// combination, like .NET. A null provider means the invariant culture.
#define DN2CPP_PARSE_MAX_DIGITS 800
// The parsed number: `count` significant digits (ASCII, NUL-terminated),
// value = 0.<digits> * 10^scale, negated when `negative`. hasNonZeroTail is
// set when digits beyond the buffer were truncated and non-zero.
struct Dn2CppNumberParse
{
    uint8_t digits[DN2CPP_PARSE_MAX_DIGITS + 8];
    int32_t count;
    int32_t scale;
    int32_t negative;
    int32_t hasNonZeroTail;
};
// kind: 0 = Integer, 1 = Float, 2 = Decimal. Returns 1 when the number
// grammar matched and consumed the entire input (styles must be pre-validated).
int32_t dn2cpp_parse_number_styles(const char16_t* p, int32_t n, int32_t styles,
                                   const Dn2CppNumberFormatInfo* nfi, int32_t kind,
                                   Dn2CppNumberParse* num);
// ValidateParseStyleFloatingPoint (throws ArgumentException / traps currency).
void dn2cpp_parse_validate_fp_styles(int32_t styles);
int32_t dn2cpp_integer_tryparse_chars(const char16_t* p, int32_t n, int32_t styles,
                                      const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                      int32_t isSigned, int64_t* out);
int32_t dn2cpp_integer_tryparse_str(Dn2CppString* s, int32_t styles,
                                    const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                    int32_t isSigned, int64_t* out);
int64_t dn2cpp_integer_parse_chars(const char16_t* p, int32_t n, int32_t styles,
                                   const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                   int32_t isSigned);
int64_t dn2cpp_integer_parse_str(Dn2CppString* s, int32_t styles,
                                 const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                 int32_t isSigned);
// isSingle selects strtof (exact float rounding); the result is the float
// value widened to double.
int32_t dn2cpp_fp_tryparse_chars(const char16_t* p, int32_t n, int32_t styles,
                                 const Dn2CppNumberFormatInfo* nfi, int32_t isSingle, double* out);
int32_t dn2cpp_fp_tryparse_str(Dn2CppString* s, int32_t styles,
                               const Dn2CppNumberFormatInfo* nfi, int32_t isSingle, double* out);
double dn2cpp_fp_parse_chars(const char16_t* p, int32_t n, int32_t styles,
                             const Dn2CppNumberFormatInfo* nfi, int32_t isSingle);
double dn2cpp_fp_parse_str(Dn2CppString* s, int32_t styles,
                           const Dn2CppNumberFormatInfo* nfi, int32_t isSingle);
// The ReadOnlySpan<byte> (IUtf8SpanParsable) input forms: widen the UTF-8 text and run
// the SAME engine as the char16/string forms, so the two lanes cannot disagree about what
// a number is. The NumberFormatInfo symbols are UTF-16, and a real culture supplies
// non-ASCII ones (U+00A0 group separator, U+2212 minus), so a byte-wise scanner would
// have to transcode them anyway — in a second place that can drift from the first. See
// the block comment in dn2cpp_parse.cpp.
int32_t dn2cpp_integer_tryparse_utf8(const char* p, int32_t n, int32_t styles,
                                     const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                     int32_t isSigned, int64_t* out);
int64_t dn2cpp_integer_parse_utf8(const char* p, int32_t n, int32_t styles,
                                  const Dn2CppNumberFormatInfo* nfi, int32_t bitWidth,
                                  int32_t isSigned);
int32_t dn2cpp_fp_tryparse_utf8(const char* p, int32_t n, int32_t styles,
                                const Dn2CppNumberFormatInfo* nfi, int32_t isSingle, double* out);
double dn2cpp_fp_parse_utf8(const char* p, int32_t n, int32_t styles,
                            const Dn2CppNumberFormatInfo* nfi, int32_t isSingle);

// Convert numeric conversions: double->int rounds ties-to-even (banker's)
// like the BCL, with an OverflowException range guard; the int64->int32 narrowing
// is range-checked; ToBoolean(string) parses "true"/"false" (case-insensitive).
int32_t dn2cpp_convert_r8_to_i32(double value);
int64_t dn2cpp_convert_r8_to_i64(double value);
int32_t dn2cpp_convert_i64_to_i32(int64_t value);
int32_t dn2cpp_convert_to_bool(Dn2CppString* s);
// Generalized checked narrowing for the sub-word / unsigned Convert.To* family:
// integer/rounded-floating sources range-checked into [lo, hi] (or the UInt64
// range), raising a catchable OverflowException. The floating forms round
// ties-to-even first, like the BCL. Convert.To*(string) yields the target's
// zero for a null string (unlike Parse) and otherwise runs the NumberStyles
// engine at the target width.
int64_t dn2cpp_convert_i64_checked(int64_t value, int64_t lo, int64_t hi);
int64_t dn2cpp_convert_u64_checked(uint64_t value, uint64_t hi);
uint64_t dn2cpp_convert_i64_to_u64(int64_t value);
int64_t dn2cpp_convert_r8_checked(double value, int64_t lo, int64_t hi);
uint64_t dn2cpp_convert_r8_to_u64(double value);
int64_t dn2cpp_convert_str_to_int(Dn2CppString* s, int32_t bits, int32_t isSigned);
// Convert.ChangeType(object, Type): convert a boxed value to the runtime target type via
// the IConvertible matrix (primitives + string + Decimal). Decimal targets/sources and a
// boxed-enum source (read as its underlying, or its member name for a string target) are
// covered. Null passes through; an unsupported source/target throws. Shares the
// parse/format helpers with the Convert.To* intrinsics.
Dn2CppObject* dn2cpp_convert_change_type(Dn2CppObject* value, Dn2CppType* targetType);
// Convert.ChangeType(object, TypeCode): same conversion matrix, but the target is named
// by a System.TypeCode value (the int the enum boxes to) rather than a runtime Type. The
// codes with a modeled boxed type-info (Boolean/Char/the integral widths/Single/Double/
// Decimal/String) are supported; others (UInt64/DateTime/Object/…) throw.
Dn2CppObject* dn2cpp_convert_change_type_code(Dn2CppObject* value, int32_t typeCode);
// Convert.GetTypeCode(object): the value's System.TypeCode. Null is TypeCode.Empty.
// Real .NET answers this by asking the value's own IConvertible.GetTypeCode() rather
// than by looking at its type, and the two can only disagree for a type whose type
// code is Object — every BCL type with any other code implements IConvertible
// returning exactly that code (an enum included: it reports its underlying's). So a
// non-Object code is answered from the type and is provably what real .NET says,
// while an Object-coded value that implements IConvertible is the one shape whose
// answer lives in a body we cannot ask: it throws a catchable
// PlatformNotSupportedException naming the type instead of reporting Object, which
// would be a plausible wrong answer with no diagnostic. A value that does not
// implement IConvertible is Object in real .NET too.
int32_t dn2cpp_convert_get_type_code(Dn2CppObject* value);
// Convert.To*(object, IFormatProvider): boxed-source IConvertible overloads. Read a
// boxed primitive through the ChangeType matrix; the provider is ignored (InvariantCulture).
// The caller's emit applies the final width/signedness cast. A null source yields zero.
int64_t dn2cpp_convert_obj_to_i64(Dn2CppObject* v);
double dn2cpp_convert_obj_to_f64(Dn2CppObject* v);
int32_t dn2cpp_convert_obj_to_bool(Dn2CppObject* v);
int32_t dn2cpp_convert_obj_to_char(Dn2CppObject* v);
// Convert.ToString(object[, provider]): the boxed value's virtual ToString via the
// shared dn2cpp_object_tostring dispatch; null yields "" (string.Empty), unlike the
// (string)null identity overload, which stays null and never routes here.
Dn2CppString* dn2cpp_convert_obj_to_string(Dn2CppObject* v);
// Convert radix overloads. ToString(value, toBase): base 10 is ordinary signed
// decimal; bases 2/8/16 format the two's-complement bit pattern (lowercase),
// so a negative shows its full-width unsigned value (ToString(-1, 16) =
// "ffffffff"). ToInt32/ToInt64(string, fromBase): bases 2/8/16 parse the digits
// as unsigned and reinterpret to the signed width; base 10 parses signed. Other
// bases / out-of-range input trap like the BCL.
Dn2CppString* dn2cpp_convert_to_string_base_i32(int32_t value, int32_t toBase);
Dn2CppString* dn2cpp_convert_to_string_base_i64(int64_t value, int32_t toBase);
int32_t dn2cpp_convert_from_base_i32(Dn2CppString* s, int32_t fromBase);
int64_t dn2cpp_convert_from_base_i64(Dn2CppString* s, int32_t fromBase);
// Width-parameterized radix parse behind every Convert.To*(string, fromBase)
// overload (the ParseNumbers model: '+' always allowed, '-' base-10-signed
// only, "0x" prefix in base 16, bit-pattern semantics in non-decimal bases,
// catchable Argument/Format/Overflow exceptions; a null string is 0). The
// result is the target's value in the int64 domain (sub-word signed targets
// come back sign-extended, unsigned 64-bit as the raw bit pattern).
int64_t dn2cpp_convert_from_base_any(Dn2CppString* s, int32_t fromBase, int32_t bits,
                                     int32_t isUnsigned);
// Convert Base64 encode/decode over the packed byte[] (Dn2CppArrayN, elemSize 1)
// / char[] (elemSize 2) / raw span bytes. Standard MIME base64 with `=` padding;
// `options` is the Base64FormattingOptions value (1 = InsertLineBreaks: CRLF
// after every full 76 chars, none trailing; other nonzero values raise a
// catchable ArgumentException). Decode skips whitespace anywhere, validates
// `=` padding strictly, and throws a catchable FormatException (the Try* forms
// return 0 with *written = 0 instead, including on a too-short destination).
// Offset/length forms validate like the BCL (catchable ArgumentNullException /
// ArgumentOutOfRangeException).
Dn2CppString* dn2cpp_convert_to_base64(Dn2CppArrayN* inArray, int32_t options);
Dn2CppString* dn2cpp_convert_to_base64_offset(Dn2CppArrayN* inArray, int32_t offset,
                                              int32_t length, int32_t options);
Dn2CppString* dn2cpp_convert_to_base64_raw(const uint8_t* data, int32_t n, int32_t options);
int32_t dn2cpp_convert_to_base64_chararray(Dn2CppArrayN* inArray, int32_t offsetIn,
                                           int32_t length, Dn2CppArrayN* outArray,
                                           int32_t offsetOut, int32_t options);
int32_t dn2cpp_convert_try_to_base64(const uint8_t* data, int32_t n, char16_t* dest,
                                     int32_t destLen, int32_t options, int32_t* written);
// `ti` is the Byte[] handle the emit arm supplies: the result escapes straight to
// managed code, so its GetType() has to be the image's byte[] and not the shared
// System.Object[] the untyped allocator would stamp. Null degrades to that shared
// handle.
Dn2CppArrayN* dn2cpp_convert_from_base64(Dn2CppString* s, const Dn2CppTypeInfo* ti);
Dn2CppArrayN* dn2cpp_convert_from_base64_chararray(Dn2CppArrayN* inArray, int32_t offset,
                                                   int32_t length, const Dn2CppTypeInfo* ti);
int32_t dn2cpp_convert_try_from_base64(const char16_t* p, int32_t n, uint8_t* dest,
                                       int32_t destLen, int32_t* written);
int32_t dn2cpp_convert_try_from_base64_str(Dn2CppString* s, uint8_t* dest, int32_t destLen,
                                           int32_t* written);
// Convert To/FromHexString over the packed byte[] (Dn2CppArrayN, elemSize 1).
// ToHexString emits uppercase (`lower` selects ToHexStringLower); FromHexString
// accepts either case and traps on odd length / non-hex chars like the BCL.
Dn2CppString* dn2cpp_convert_to_hex(Dn2CppArrayN* inArray, bool lower);
Dn2CppString* dn2cpp_convert_to_hex_raw(const uint8_t* data, int32_t n, bool lower);
// The (byte[], int offset, int count) overloads; catchable ArgumentNull /
// ArgumentOutOfRange validation like the BCL.
Dn2CppString* dn2cpp_convert_to_hex_offset(Dn2CppArrayN* inArray, int32_t offset, int32_t count, bool lower);
Dn2CppArrayN* dn2cpp_convert_from_hex(Dn2CppString* s, const Dn2CppTypeInfo* ti);
// The TryDecode-style FromHexString(ReadOnlySpan<char>, Span<byte>, out charsConsumed,
// out bytesWritten): decodes as many whole hex pairs as fit `bytes`, writing progress to
// the two out-refs, and returns the System.Buffers.OperationStatus (Done=0,
// DestinationTooSmall=1, NeedMoreData=2, InvalidData=3). Mirrors the BCL's HexConverter
// exactly, including its charsConsumed accounting on a bad low nibble.
int32_t dn2cpp_convert_from_hex_span(const char16_t* chars, int32_t charsLen,
                                     uint8_t* bytes, int32_t bytesLen,
                                     int32_t* charsConsumed, int32_t* bytesWritten);

// Math.Round(value, MidpointRounding mode): mode is the BCL enum value
// (ToEven=0, AwayFromZero=1, ToZero=2, ToNegativeInfinity=3, ToPositiveInfinity=4);
// an invalid mode raises a catchable ArgumentException like the BCL.
double dn2cpp_math_round_mode(double value, int32_t mode);

// Math/MathF.Round(value, digits[, mode]): the BCL's scale/round/unscale.
// digits outside 0..15 (double) / 0..6 (float) raise ArgumentOutOfRangeException;
// values at or beyond 1e16 / 1e8f pass through unscaled (so an invalid mode is
// only detected below the limit, like the BCL). The float variant keeps every
// operation in single precision.
double dn2cpp_math_round_digits(double value, int32_t digits, int32_t mode);
float dn2cpp_math_round_digits_f(float value, int32_t digits, int32_t mode);

// Math/MathF.ILogB: the unbiased base-2 exponent as an int32 with the BCL's
// exact special values (0 -> int.MinValue, ±infinity/NaN -> int.MaxValue,
// subnormals counted below MinExponent). Not std::ilogb — its FP_ILOGB0 /
// FP_ILOGBNAN return values are platform-defined; this is the BCL's own
// musl-derived bit-twiddle.
int32_t dn2cpp_math_ilogb(double x);
int32_t dn2cpp_math_ilogb_f(float x);

// Math.BigMul (64-bit overloads): the full 128-bit product — the high half is
// returned, the low half goes through *low. The signed form yields the true
// signed high half, not the unsigned product's.
uint64_t dn2cpp_math_bigmul_u64(uint64_t a, uint64_t b, uint64_t* low);
int64_t dn2cpp_math_bigmul_i64(int64_t a, int64_t b, int64_t* low);

// Double/Single.SinPi/CosPi/TanPi/SinCosPi: sin/cos/tan of pi*x computed on
// the fractional turn position — faithful ports of the BCL's managed
// AOCL-derived implementations (exact signed zeros/infinities at the
// half-turn positions; no libm counterpart matches in the last ulp).
// SinCosPi fills both results from one argument reduction.
double dn2cpp_math_sinpi(double x);
double dn2cpp_math_cospi(double x);
double dn2cpp_math_tanpi(double x);
void dn2cpp_math_sincospi(double x, double* sinResult, double* cosResult);
float dn2cpp_math_sinpi_f(float x);
float dn2cpp_math_cospi_f(float x);
float dn2cpp_math_tanpi_f(float x);
void dn2cpp_math_sincospi_f(float x, float* sinResult, float* cosResult);

// Double/Single.RootN(x, n): the BCL's managed algorithm — n == 2/3 route to
// sqrt/cbrt, the general case is pow(|x|, 1.0/n) with the sign copied back,
// n == 0 answers NaN, and every zero/infinity/parity special case is pinned.
// The float flavor's general case computes the DOUBLE pow like Single.cs.
double dn2cpp_math_rootn(double x, int32_t n);
float dn2cpp_math_rootn_f(float x, int32_t n);

// Double/Single.Hypot: sqrt(x^2 + y^2) without intermediate overflow —
// Double.Hypot is the BCL's managed AOCL head/tail algorithm (std::hypot
// differs in the last ulp), float.Hypot the exact double sum of squares
// under one sqrt. An infinite operand beats a NaN one, like IEEE 754.
double dn2cpp_math_hypot(double x, double y);
float dn2cpp_math_hypot_f(float x, float y);

// ---- Interlocked (real hardware atomics, seq_cst) ----
// The _i1/_i2 variants CAS/exchange at the declared width only (a ref byte may
// point into a packed struct/array where a wider RMW would clobber neighbors);
// the _r4/_r8 variants are one integer atomic over the bit pattern, giving
// .NET's bitwise CompareExchange comparison (-0.0 != +0.0; NaN matches itself).
Dn2CppObject* dn2cpp_interlocked_cmpxchg_ref(Dn2CppObject** loc, Dn2CppObject* value, Dn2CppObject* comparand);
int8_t dn2cpp_interlocked_cmpxchg_i1(int8_t* loc, int8_t value, int8_t comparand);
int16_t dn2cpp_interlocked_cmpxchg_i2(int16_t* loc, int16_t value, int16_t comparand);
int32_t dn2cpp_interlocked_cmpxchg_i4(int32_t* loc, int32_t value, int32_t comparand);
int64_t dn2cpp_interlocked_cmpxchg_i8(int64_t* loc, int64_t value, int64_t comparand);
float dn2cpp_interlocked_cmpxchg_r4(float* loc, float value, float comparand);
double dn2cpp_interlocked_cmpxchg_r8(double* loc, double value, double comparand);
Dn2CppObject* dn2cpp_interlocked_exchange_ref(Dn2CppObject** loc, Dn2CppObject* value);
int8_t dn2cpp_interlocked_exchange_i1(int8_t* loc, int8_t value);
int16_t dn2cpp_interlocked_exchange_i2(int16_t* loc, int16_t value);
int32_t dn2cpp_interlocked_exchange_i4(int32_t* loc, int32_t value);
int64_t dn2cpp_interlocked_exchange_i8(int64_t* loc, int64_t value);
float dn2cpp_interlocked_exchange_r4(float* loc, float value);
double dn2cpp_interlocked_exchange_r8(double* loc, double value);
int64_t dn2cpp_interlocked_read_i8(int64_t* loc);   // Interlocked.Read: seq_cst 64-bit load
void dn2cpp_interlocked_membarrier_processwide();   // Interlocked.MemoryBarrierProcessWide
int32_t dn2cpp_interlocked_add_i4(int32_t* loc, int32_t value);
int64_t dn2cpp_interlocked_add_i8(int64_t* loc, int64_t value);
int32_t dn2cpp_interlocked_or_i4(int32_t* loc, int32_t value);   // fetch_or: returns original
int64_t dn2cpp_interlocked_or_i8(int64_t* loc, int64_t value);
int32_t dn2cpp_interlocked_and_i4(int32_t* loc, int32_t value);  // fetch_and: returns original
int64_t dn2cpp_interlocked_and_i8(int64_t* loc, int64_t value);

// ---- Monitor / lock / System.Threading.Lock (real per-object mutex) ----
// Identity-keyed recursive lock (non-moving GC -> stable object pointers), built on
// a plain mutex + condition variables so Wait/Pulse can release and restore the
// recursion depth. Enter blocks until acquired (recursion supported); TryEnter
// returns 1/0. Wait releases the lock, blocks until pulsed, then reacquires; the
// _timeout forms wait at most ms and return 0 on timeout (still re-acquiring the lock
// before returning, per the .NET contract). Pulse wakes one waiter, PulseAll wakes all.
void dn2cpp_monitor_enter(Dn2CppObject* o);
void dn2cpp_monitor_exit(Dn2CppObject* o);
int32_t dn2cpp_monitor_try_enter(Dn2CppObject* o);
int32_t dn2cpp_monitor_try_enter_timeout(Dn2CppObject* o, int32_t ms); // TryEnter(obj, int/TimeSpan)
int32_t dn2cpp_monitor_wait(Dn2CppObject* o);
int32_t dn2cpp_monitor_wait_timeout(Dn2CppObject* o, int32_t ms);      // Wait(obj, int/TimeSpan)
void dn2cpp_monitor_pulse(Dn2CppObject* o);
void dn2cpp_monitor_pulse_all(Dn2CppObject* o);

// RAII guard around a [MethodImpl(MethodImplOptions.Synchronized)] body: enters
// the identity-keyed monitor on construction, exits on destruction — including
// exceptional unwind (managed exceptions are C++ exceptions). The monitor table
// never dereferences the key, so a static synchronized method may pass its
// declaring type's ti_* address (stable, data segment) as the identity.
struct Dn2CppMonitorGuard
{
    Dn2CppObject* obj;
    explicit Dn2CppMonitorGuard(Dn2CppObject* o) : obj(o) { dn2cpp_monitor_enter(o); }
    ~Dn2CppMonitorGuard() { dn2cpp_monitor_exit(obj); }
    Dn2CppMonitorGuard(const Dn2CppMonitorGuard&) = delete;
    Dn2CppMonitorGuard& operator=(const Dn2CppMonitorGuard&) = delete;
};

// ---- Volatile.Read/Write for float/double (bit-cast through an integer atomic) ----
// Integer/pointer Volatile.Read/Write are emitted inline as __atomic_load_n /
// __atomic_store_n; floating types need the bit-cast helpers below.
float dn2cpp_volatile_read_r4(const float* p);
double dn2cpp_volatile_read_r8(const double* p);
void dn2cpp_volatile_write_r4(float* p, float v);
void dn2cpp_volatile_write_r8(double* p, double v);

// ---- System.Threading.Thread (real OS threads, std::thread) ----
// Dn2CppThread is a managed object; its full layout (incl. the native std::thread
// handle) lives in dn2cpp_tasks.cpp. Each spawned thread registers with the GC and
// gets a fresh per-thread cooperative scheduler.
struct Dn2CppThread;
Dn2CppThread* dn2cpp_thread_new(Dn2CppObject* start);              // new Thread(ThreadStart/ParameterizedThreadStart)
void dn2cpp_thread_start(Dn2CppThread* t);                         // Thread.Start()
void dn2cpp_thread_start_param(Dn2CppThread* t, Dn2CppObject* arg);// Thread.Start(object)
void dn2cpp_thread_join(Dn2CppThread* t);                          // Thread.Join()
int32_t dn2cpp_thread_join_timeout(Dn2CppThread* t, int32_t ms);  // Thread.Join(int/TimeSpan): 1=joined, 0=timeout
void dn2cpp_thread_sleep(int32_t ms);                             // Thread.Sleep(int)
int32_t dn2cpp_thread_yield();                                    // Thread.Yield() (always returns 1)
Dn2CppThread* dn2cpp_thread_current();                            // Thread.CurrentThread
int32_t dn2cpp_thread_managed_id(Dn2CppThread* t);               // ManagedThreadId
int32_t dn2cpp_thread_is_alive(Dn2CppThread* t);                 // IsAlive
int32_t dn2cpp_thread_get_background(Dn2CppThread* t);           // IsBackground (get)
void dn2cpp_thread_set_background(Dn2CppThread* t, int32_t v);   // IsBackground (set)
Dn2CppString* dn2cpp_thread_get_name(Dn2CppThread* t);          // Name (get)
void dn2cpp_thread_set_name(Dn2CppThread* t, Dn2CppString* n);  // Name (set)

// ---- Task.Run / ThreadPool (real worker pool, bridged to the cooperative scheduler) ----
// Each submits the delegate to a fixed pool of GC-registered worker threads and returns
// a Task the worker completes; an awaiting cooperative thread is woken via the bridge
// (Dn2CppCont.owner). The result kind selects how the worker's return value is packed.
struct Dn2CppTask; // full definition below
Dn2CppTask* dn2cpp_task_run_void(Dn2CppObject* del); // Task.Run(Action)
Dn2CppTask* dn2cpp_task_run_i4(Dn2CppObject* del);   // Task.Run(Func<int/bool/...>)
Dn2CppTask* dn2cpp_task_run_i8(Dn2CppObject* del);   // Task.Run(Func<long/IntPtr/...>)
Dn2CppTask* dn2cpp_task_run_r4(Dn2CppObject* del);   // Task.Run(Func<float>)
Dn2CppTask* dn2cpp_task_run_r8(Dn2CppObject* del);   // Task.Run(Func<double>)
Dn2CppTask* dn2cpp_task_run_ref(Dn2CppObject* del);  // Task.Run(Func<T> : reference)
// Task.Run(Func<TStruct> : value type) — a struct result does not fit the 8-byte slot,
// so `invoke` is a transpiler-emitted trampoline that runs the delegate and returns a
// heap-boxed pointer (dn2cpp_struct_result_box) in the slot; the awaiting side copies it
// back by the same T (PushTaskResult's Struct arm). The struct's exact ABI is stamped into
// the trampoline at the call site, so the worker never needs a struct-return ABI.
Dn2CppTask* dn2cpp_task_run_struct(Dn2CppObject* del, uint64_t (*invoke)(Dn2CppObject*));
// Task.Run(Func<Task> / Func<Task<T>>): the delegate returns an INNER task; the outer
// task settles with the inner's status/result/exception (the async-lambda unwrap). One
// entry point for every result kind — the 8-byte result slot is copied opaquely and the
// awaiting side reinterprets it by T (0 for a void inner Task).
Dn2CppTask* dn2cpp_task_run_unwrap(Dn2CppObject* del);
// TaskFactory.StartNew(Func<Task> / Func<Task<T>>): NO unwrap — the outer Task<Task<T>>
// settles with the inner task pointer as soon as the delegate returns (real .NET's
// nested-task semantics). The pool worker then drives its own scheduler until the inner
// settles: a suspended async lambda parks its Task.Delay timers and continuations on the
// worker's thread-local scheduler, which no other thread can drain.
Dn2CppTask* dn2cpp_task_run_nested(Dn2CppObject* del);
// TaskFactory.StartNew(Action<object?>, object? state[, hints...]): like
// dn2cpp_task_run_void but the worker invokes the 1-arg delegate with `state`
// (the base Stream.FlushAsync shape). Both are GC-rooted while queued/running.
Dn2CppTask* dn2cpp_task_run_void_state(Dn2CppObject* del, Dn2CppObject* state);
Dn2CppTask* dn2cpp_task_run_i4_state(Dn2CppObject* del, Dn2CppObject* state);
Dn2CppTask* dn2cpp_task_run_i8_state(Dn2CppObject* del, Dn2CppObject* state);
Dn2CppTask* dn2cpp_task_run_r4_state(Dn2CppObject* del, Dn2CppObject* state);
Dn2CppTask* dn2cpp_task_run_r8_state(Dn2CppObject* del, Dn2CppObject* state);
Dn2CppTask* dn2cpp_task_run_ref_state(Dn2CppObject* del, Dn2CppObject* state);

// ---- cold tasks: `new Task(...)` / `new Task<T>(...)` + Start / RunSynchronously ----
// A cold (unstarted) task holds its delegate and result-kind thunk until Start()
// submits it to the worker pool or RunSynchronously() runs it inline on the calling
// thread. The result kind is fixed at the newobj site, mirroring dn2cpp_task_run_*.
Dn2CppTask* dn2cpp_task_cold_void(Dn2CppObject* del); // new Task(Action)
Dn2CppTask* dn2cpp_task_cold_i4(Dn2CppObject* del);   // new Task<int/bool/...>(Func<T>)
Dn2CppTask* dn2cpp_task_cold_i8(Dn2CppObject* del);
Dn2CppTask* dn2cpp_task_cold_r4(Dn2CppObject* del);
Dn2CppTask* dn2cpp_task_cold_r8(Dn2CppObject* del);
Dn2CppTask* dn2cpp_task_cold_ref(Dn2CppObject* del);
// new Task<TStruct>(Func<TStruct> : value type): a struct result rides a transpiler-emitted
// boxing trampoline (same `invoke` shape as dn2cpp_task_run_struct), stored as the cold
// task's result thunk until Start() / RunSynchronously() runs it.
Dn2CppTask* dn2cpp_task_cold_struct(Dn2CppObject* del, uint64_t (*invoke)(Dn2CppObject*));
// new Task(Action<object?>, object? state): the worker/inline run invokes the
// 1-arg delegate with `state` (same shape as dn2cpp_task_run_void_state).
Dn2CppTask* dn2cpp_task_cold_void_state(Dn2CppObject* del, Dn2CppObject* state);
// Task.Start(): claim the cold work exactly once and submit it to the worker pool.
// A second Start, or Start on a task that was never cold (an async-method task,
// Task.Run, a settled task), throws InvalidOperationException like real .NET.
void dn2cpp_task_start(Dn2CppTask* t);
// Task.RunSynchronously(): claim the cold work and run it inline on the calling
// thread. A fault settles the task FAULTED (observed at Wait/Result/await), it is
// not re-thrown here — matching real .NET. Same InvalidOperationException rules.
void dn2cpp_task_run_synchronously(Dn2CppTask* t);

// ---- Task.ContinueWith --------------------------------------------------------
// Register a continuation on `t` and return the continuation task: once `t`
// settles, the delegate runs with the settled antecedent as its argument (plus
// `state` for the Action<Task, object?> form) on the registering thread's
// scheduler, and its result settles the returned task (a thrown managed
// exception settles it FAULTED). `kind` picks how the delegate is invoked and
// how its return value packs into the 8-byte result slot, fixed at the call
// site exactly like dn2cpp_task_run_*'s result kinds.
enum
{
    DN2CPP_CONTWITH_VOID = 0,       // Action<Task>
    DN2CPP_CONTWITH_VOID_STATE = 1, // Action<Task, object?> (uses `state`)
    DN2CPP_CONTWITH_I4 = 2,         // Func<Task, int/bool/...>
    DN2CPP_CONTWITH_I8 = 3,
    DN2CPP_CONTWITH_R4 = 4,
    DN2CPP_CONTWITH_R8 = 5,
    DN2CPP_CONTWITH_REF = 6,
    DN2CPP_CONTWITH_STRUCT = 7,     // Func<Task, TStruct> (uses `invokeStruct`)
};
// `options` carries the caller's TaskContinuationOptions verbatim; only its three FILTER
// bits (NotOnRanToCompletion / NotOnFaulted / NotOnCanceled — the OnlyOn* names are pairs
// of those) are read, and a continuation they exclude settles CANCELED rather than running,
// which is what .NET does. Pass 0 for the overloads that take no options.
Dn2CppTask* dn2cpp_task_continue_with(Dn2CppTask* t, Dn2CppObject* del, Dn2CppObject* state,
                                     int32_t kind, int32_t options);
// Task.ContinueWith(Func<Task, TStruct> : value type): the continuation's struct result
// does not fit the 8-byte slot, so `invoke` is a transpiler-emitted trampoline taking
// (delegate, settled antecedent) that heap-boxes the result (dn2cpp_struct_result_box)
// into the slot — the ContinueWith mirror of dn2cpp_task_run_struct.
Dn2CppTask* dn2cpp_task_continue_with_struct(Dn2CppTask* t, Dn2CppObject* del,
                                            uint64_t (*invoke)(Dn2CppObject*, Dn2CppObject*),
                                            int32_t options);
// Task.WaitAsync(CancellationToken): a task that mirrors `t`'s outcome, but settles
// CANCELED as soon as `src` cancels — whichever happens first wins, exactly once. `t`
// itself keeps running; WaitAsync bounds the WAIT, not the work. A null source can never
// cancel, so the result is `t`'s outcome and nothing else. One asymmetry, and it is
// .NET's: an ALREADY-COMPLETE antecedent wins over an already-requested token, because
// WaitAsyncCore returns `this` before it looks at the token at all. (Forward declaration: the
// cancellation section defines the struct further down this header.)
struct Dn2CppCancelSource;
Dn2CppTask* dn2cpp_task_wait_async(Dn2CppTask* t, Dn2CppCancelSource* src);

// ThreadPool.QueueUserWorkItem(WaitCallback [, object state]) — fire-and-forget work on
// the same worker pool. Returns no Task (returns bool, always 1/true); a GC holder keeps
// the delegate + state reachable from enqueue until the worker finishes invoking them.
int32_t dn2cpp_threadpool_queue(Dn2CppObject* callback, Dn2CppObject* state);
template<typename T>
struct Dn2CppThreadPoolValueState : Dn2CppObject
{
    Dn2CppObject* callback;
    T state;
};
template<typename T>
inline void dn2cpp_action_value_invoke(Dn2CppObject* del, T state)
{
    if (del == nullptr)
        return;
    auto* dg = reinterpret_cast<Dn2CppDelegate*>(del);
    if (dg->prev != nullptr)
        dn2cpp_action_value_invoke<T>(dg->prev, state);
    reinterpret_cast<void (*)(Dn2CppObject*, T)>(dg->method)(dg->target, state);
}
template<typename T>
inline void dn2cpp_threadpool_value_thunk(Dn2CppObject* target, Dn2CppObject*)
{
    auto* item = reinterpret_cast<Dn2CppThreadPoolValueState<T>*>(target);
    dn2cpp_action_value_invoke<T>(item->callback, item->state);
}
template<typename T>
inline int32_t dn2cpp_threadpool_queue_value(Dn2CppObject* callback, T state)
{
    auto* item = static_cast<Dn2CppThreadPoolValueState<T>*>(
        dn2cpp_alloc(sizeof(Dn2CppThreadPoolValueState<T>)));
    item->type = &dn2cpp_object_type;
    item->callback = callback;
    item->state = state;
    auto* del = static_cast<Dn2CppDelegate*>(dn2cpp_alloc(sizeof(Dn2CppDelegate)));
    del->type = &dn2cpp_object_type;
    del->target = item;
    del->method = reinterpret_cast<void*>(&dn2cpp_threadpool_value_thunk<T>);
    del->prev = nullptr;
    return dn2cpp_threadpool_queue(del, nullptr);
}
// ThreadPool.UnsafeQueueUserWorkItem(IThreadPoolWorkItem, bool) — fire-and-forget
// Execute() on the same pool; `executeFn` is the Execute implementation the call
// site resolved through the receiver's interface table (void (*)(receiver)).
int32_t dn2cpp_threadpool_queue_workitem(Dn2CppObject* wi, const void* executeFn);
// `new ValueTask(<T>)(IValueTaskSource(<T>) source, short token)` — bridge the
// source onto a pending task completed via the source's OnCompleted/GetResult.
// GetStatus remains the authority for every ValueTask status read, even after the
// bridge task settles. All three implementations are resolved through the source's
// interface table. `actionTi` is the Action<object> type-info stamped on the runtime-built
// continuation delegate; `resultKind` packs GetResult's return into the task's
// result slot (0=void 1=int32 2=int64 3=reference 4=struct). A struct's ABI is
// known only at the call site, so `getStructResult` invokes the typed method and
// heap-copies its result into the slot.
Dn2CppTask* dn2cpp_vts_task(Dn2CppObject* vts, int16_t version,
                            const void* getStatusFn, const void* getResultFn,
                            const void* onCompletedFn,
                            const Dn2CppTypeInfo* actionTi, int32_t resultKind,
                            uint64_t (*getStructResult)(const void*, Dn2CppObject*, int16_t));

// Environment.ProcessorCount (the GetProcessorCount InternalCall): hardware
// concurrency, clamped to >= 1. A ConcurrentDictionary concurrency-level hint.
int32_t dn2cpp_environment_processor_count();

// ---- Parallel.For / ForEach / Invoke (real fan-out, deterministic join barrier) ----
// Each runs its body across up to hardware_concurrency OS threads (contiguous index
// chunks) and BLOCKS until every iteration finishes — a deterministic join barrier, so a
// gate that reads results after the call is deterministic vs real .NET. This is a
// dedicated per-call fan-out, kept separate from the Task.Run worker pool so it never
// interacts with the async bridge. Every iteration runs under its own try; any
// exceptions thrown are collected (ordered by iteration index) and rethrown on the
// calling thread after the join as a single AggregateException — matching real .NET,
// which always wraps Parallel.* body exceptions in an AggregateException, even one.
struct Dn2CppParallelLoopResult
{
    int8_t isCompleted;           // 0 once Break()/Stop() was ever called
    int8_t hasLowestBreak;        // 1 once Break() was ever called (LowestBreakIteration.HasValue)
    int64_t lowestBreakIteration; // the lowest Break()-called index, valid only if hasLowestBreak
};
// Every entry point below takes a trailing maxDop (ParallelOptions.MaxDegreeOfParallelism):
// <= 0 means unlimited (the hardware_concurrency fan-out), > 0 clamps the OS-thread count.
void dn2cpp_parallel_for_i4(int32_t from, int32_t to, Dn2CppObject* body, int32_t maxDop); // Parallel.For(int, int, Action<int>)
void dn2cpp_parallel_for_i8(int64_t from, int64_t to, Dn2CppObject* body, int32_t maxDop); // Parallel.For(long, long, Action<long>)
void dn2cpp_parallel_invoke(Dn2CppArrayRef* actions, int32_t maxDop);                      // Parallel.Invoke(params Action[])
void dn2cpp_parallel_foreach_ref(Dn2CppArrayRef* src, Dn2CppObject* body, int32_t maxDop); // Parallel.ForEach<T>(T[], Action<T>), T : reference
void dn2cpp_parallel_foreach_i4(Dn2CppArrayI4* src, Dn2CppObject* body, int32_t maxDop);   // T = int/uint/enum/bool/char
void dn2cpp_parallel_foreach_i8(Dn2CppArrayN* src, Dn2CppObject* body, int32_t maxDop);    // T = long/ulong/nint
void dn2cpp_parallel_foreach_r4(Dn2CppArrayN* src, Dn2CppObject* body, int32_t maxDop);    // T = float
void dn2cpp_parallel_foreach_r8(Dn2CppArrayN* src, Dn2CppObject* body, int32_t maxDop);    // T = double
// Count-aware variants for a List<T> source: List<T>'s backing array (_items) is sized
// to capacity, which may exceed the live element count (_size), so these take an
// explicit count instead of trusting src->length (mirrors dn2cpp_string_join_*_n's
// trailing-count convention, count placed right after the array it describes). The
// plain entry points above delegate to these with src->length.
void dn2cpp_parallel_foreach_ref_n(Dn2CppArrayRef* src, int32_t n, Dn2CppObject* body, int32_t maxDop);
void dn2cpp_parallel_foreach_i4_n(Dn2CppArrayI4* src, int32_t n, Dn2CppObject* body, int32_t maxDop);
void dn2cpp_parallel_foreach_i8_n(Dn2CppArrayN* src, int32_t n, Dn2CppObject* body, int32_t maxDop);
void dn2cpp_parallel_foreach_r4_n(Dn2CppArrayN* src, int32_t n, Dn2CppObject* body, int32_t maxDop);
void dn2cpp_parallel_foreach_r8_n(Dn2CppArrayN* src, int32_t n, Dn2CppObject* body, int32_t maxDop);

// ---- ParallelLoopState (Break/Stop) for the Action<int|long|T, ParallelLoopState>
// body overloads above ----
// ParallelLoopState is an opaque per-iteration Dn2CppObject* — its layout (a pointer to
// the loop call's shared Break/Stop/lowestBreak block, plus this iteration's own index)
// is private to dn2cpp_threading.cpp. The transpiler never reads/writes its fields
// directly, only calls through these helpers (unlike ParallelOptions, whose maxDop
// field the call sites do read/write directly).
void dn2cpp_parallel_loop_state_break(Dn2CppObject* state);
void dn2cpp_parallel_loop_state_stop(Dn2CppObject* state);
int32_t dn2cpp_parallel_loop_state_should_exit_current_iteration(Dn2CppObject* state);
int32_t dn2cpp_parallel_loop_state_is_stopped(Dn2CppObject* state);
int32_t dn2cpp_parallel_loop_state_is_exceptional(Dn2CppObject* state);
int32_t dn2cpp_parallel_loop_state_lowest_break_has_value(Dn2CppObject* state);
int64_t dn2cpp_parallel_loop_state_lowest_break_value(Dn2CppObject* state);
// The Action<int|long|T, ParallelLoopState> body forms: like the state-less entry
// points above, but the body's second argument is a fresh ParallelLoopState each
// iteration (all sharing one Break/Stop/lowestBreak block for the whole call), and the
// join barrier computes a REAL ParallelLoopResult instead of the always-completed one.
Dn2CppParallelLoopResult dn2cpp_parallel_for_i4_state(int32_t from, int32_t to, Dn2CppObject* body, int32_t maxDop);
Dn2CppParallelLoopResult dn2cpp_parallel_for_i8_state(int64_t from, int64_t to, Dn2CppObject* body, int32_t maxDop);
Dn2CppParallelLoopResult dn2cpp_parallel_foreach_ref_state(Dn2CppArrayRef* src, Dn2CppObject* body, int32_t maxDop);
Dn2CppParallelLoopResult dn2cpp_parallel_foreach_i4_state(Dn2CppArrayI4* src, Dn2CppObject* body, int32_t maxDop);
Dn2CppParallelLoopResult dn2cpp_parallel_foreach_i8_state(Dn2CppArrayN* src, Dn2CppObject* body, int32_t maxDop);
Dn2CppParallelLoopResult dn2cpp_parallel_foreach_r4_state(Dn2CppArrayN* src, Dn2CppObject* body, int32_t maxDop);
Dn2CppParallelLoopResult dn2cpp_parallel_foreach_r8_state(Dn2CppArrayN* src, Dn2CppObject* body, int32_t maxDop);
// List<T>-source count-aware variants of the _state forms above (same convention as
// the state-less _n group: explicit count right after the array, for a List<T>'s
// backing array whose capacity may exceed its live element count).
Dn2CppParallelLoopResult dn2cpp_parallel_foreach_ref_state_n(Dn2CppArrayRef* src, int32_t n, Dn2CppObject* body, int32_t maxDop);
Dn2CppParallelLoopResult dn2cpp_parallel_foreach_i4_state_n(Dn2CppArrayI4* src, int32_t n, Dn2CppObject* body, int32_t maxDop);
Dn2CppParallelLoopResult dn2cpp_parallel_foreach_i8_state_n(Dn2CppArrayN* src, int32_t n, Dn2CppObject* body, int32_t maxDop);
Dn2CppParallelLoopResult dn2cpp_parallel_foreach_r4_state_n(Dn2CppArrayN* src, int32_t n, Dn2CppObject* body, int32_t maxDop);
Dn2CppParallelLoopResult dn2cpp_parallel_foreach_r8_state_n(Dn2CppArrayN* src, int32_t n, Dn2CppObject* body, int32_t maxDop);

// System.Threading.Tasks.ParallelOptions — holds only MaxDegreeOfParallelism (the
// CancellationToken/TaskScheduler properties are a carve-out, raised loudly by the
// transpiler). No mutex: it is set up before a Parallel.* call and never mutated
// concurrently, unlike Timer.
struct Dn2CppParallelOptions : Dn2CppObject
{
    int32_t maxDop; // -1 = unlimited (ParallelOptions.MaxDegreeOfParallelism default)
};
Dn2CppObject* dn2cpp_parallel_options_new();

// ---- SemaphoreSlim / ManualResetEvent(Slim) / AutoResetEvent (real mutex + condvar) ----
// These are blocking primitives: Wait/WaitOne block the calling OS thread until the
// count/signal is available (matching .NET). They are native-allocated objects with a
// managed type header; gates must coordinate so a blocking wait is always released.
Dn2CppObject* dn2cpp_semaphore_new(int32_t initial, int32_t maxCount);
void dn2cpp_semaphore_wait(Dn2CppObject* s);                  // SemaphoreSlim.Wait()
int32_t dn2cpp_semaphore_wait_timeout(Dn2CppObject* s, int32_t ms); // Wait(int/TimeSpan): 1=acquired, 0=timeout
int32_t dn2cpp_semaphore_release(Dn2CppObject* s, int32_t count); // returns the previous count
int32_t dn2cpp_semaphore_count(Dn2CppObject* s);             // SemaphoreSlim.CurrentCount
// SemaphoreSlim.WaitAsync([CancellationToken]) -> Task: an immediately available
// token yields a pre-completed task; otherwise the blocking acquire escapes to
// the worker pool and the task completes when a Release hands over a token.
Dn2CppTask* dn2cpp_semaphore_wait_async(Dn2CppObject* s);
Dn2CppObject* dn2cpp_event_new(int32_t initial, int32_t manualReset, const Dn2CppTypeInfo* ti);
void dn2cpp_event_set(Dn2CppObject* e);                      // Set()
void dn2cpp_event_reset(Dn2CppObject* e);                    // Reset()
int32_t dn2cpp_event_wait(Dn2CppObject* e);                  // WaitOne()/Wait() (returns 1)
int32_t dn2cpp_event_wait_timeout(Dn2CppObject* e, int32_t ms); // WaitOne/Wait(int/TimeSpan): 1=signaled, 0=timeout
int32_t dn2cpp_event_is_set(Dn2CppObject* e);               // ManualResetEventSlim.IsSet
int32_t dn2cpp_event_wait_any(Dn2CppArrayRef* handles);     // WaitHandle.WaitAny(WaitHandle[]): index of first signaled

// ---- CountdownEvent / Barrier (counter-based sync) ----
// Native-allocated reference objects (like the semaphore/event above); the helpers cast
// internally, so a Dn2CppObject* handle suffices. The real handle-allocating ctors are
// skipped and the object is built at newobj.
Dn2CppObject* dn2cpp_countdown_new(int32_t initialCount);
int32_t dn2cpp_countdown_signal(Dn2CppObject* c, int32_t n); // Signal(n): 1 if count hit zero
void dn2cpp_countdown_add(Dn2CppObject* c, int32_t n);       // AddCount(n)
int32_t dn2cpp_countdown_try_add(Dn2CppObject* c, int32_t n); // TryAddCount(n): 1 if added
void dn2cpp_countdown_reset(Dn2CppObject* c, int32_t n);     // Reset(n): current+initial = n
void dn2cpp_countdown_reset_default(Dn2CppObject* c);        // Reset(): current = initial
void dn2cpp_countdown_wait(Dn2CppObject* c);                 // Wait() (blocks until zero)
int32_t dn2cpp_countdown_wait_timeout(Dn2CppObject* c, int32_t ms); // Wait(int/TimeSpan): 1=set, 0=timeout
int32_t dn2cpp_countdown_current(Dn2CppObject* c);          // CurrentCount
int32_t dn2cpp_countdown_initial(Dn2CppObject* c);          // InitialCount
int32_t dn2cpp_countdown_is_set(Dn2CppObject* c);           // IsSet (count == 0)
Dn2CppObject* dn2cpp_barrier_new(int32_t participantCount, Dn2CppObject* postPhaseAction);
int32_t dn2cpp_barrier_signal_and_wait(Dn2CppObject* b);    // SignalAndWait() (returns 1)
int32_t dn2cpp_barrier_signal_and_wait_timeout(Dn2CppObject* b, int32_t ms); // 1=passed, 0=timeout
int64_t dn2cpp_barrier_add(Dn2CppObject* b, int32_t n);     // AddParticipant(s): returns phase
void dn2cpp_barrier_remove(Dn2CppObject* b, int32_t n);     // RemoveParticipant(s)
int32_t dn2cpp_barrier_participant_count(Dn2CppObject* b);  // ParticipantCount
int32_t dn2cpp_barrier_participants_remaining(Dn2CppObject* b); // ParticipantsRemaining
int64_t dn2cpp_barrier_current_phase(Dn2CppObject* b);      // CurrentPhaseNumber

// ---- ReaderWriterLockSlim (real reader-writer lock) ----
// Many concurrent readers XOR one writer, with writer preference (a new reader yields to an
// active or waiting writer). An upgradeable-read lock (at most one) coexists with plain
// readers but excludes writers and other upgradeable holders. Per-thread ownership is
// tracked: a same-thread re-entry the lock's recursion policy forbids throws
// LockRecursionException instead of deadlocking against itself, a permitted re-entry
// (LockRecursionPolicy.SupportsRecursion, the upgradeable holder's read/upgrade paths)
// is granted immediately, and an Exit* by a non-holder throws
// SynchronizationLockException — all matching real .NET. Native-allocated reference
// object (like the primitives above); the helpers cast internally, so a Dn2CppObject*
// handle suffices. The real handle-allocating ctor is skipped and the object built at
// newobj; `policy` is the LockRecursionPolicy ctor argument (0 = NoRecursion).
Dn2CppObject* dn2cpp_rwlock_new(int32_t policy);
void dn2cpp_rwlock_enter_read(Dn2CppObject* r);            // EnterReadLock
void dn2cpp_rwlock_exit_read(Dn2CppObject* r);             // ExitReadLock
void dn2cpp_rwlock_enter_write(Dn2CppObject* r);           // EnterWriteLock
void dn2cpp_rwlock_exit_write(Dn2CppObject* r);            // ExitWriteLock
void dn2cpp_rwlock_enter_upgradeable(Dn2CppObject* r);     // EnterUpgradeableReadLock
void dn2cpp_rwlock_exit_upgradeable(Dn2CppObject* r);      // ExitUpgradeableReadLock
int32_t dn2cpp_rwlock_try_enter_read(Dn2CppObject* r, int32_t ms);        // TryEnterReadLock: 1/0
int32_t dn2cpp_rwlock_try_enter_write(Dn2CppObject* r, int32_t ms);       // TryEnterWriteLock: 1/0
int32_t dn2cpp_rwlock_try_enter_upgradeable(Dn2CppObject* r, int32_t ms); // TryEnterUpgradeableReadLock: 1/0
int32_t dn2cpp_rwlock_current_read_count(Dn2CppObject* r); // CurrentReadCount
int32_t dn2cpp_rwlock_waiting_write_count(Dn2CppObject* r); // WaitingWriteCount
int32_t dn2cpp_rwlock_is_read_held(Dn2CppObject* r);       // IsReadLockHeld (calling thread)
int32_t dn2cpp_rwlock_is_write_held(Dn2CppObject* r);      // IsWriteLockHeld (calling thread)
int32_t dn2cpp_rwlock_is_upgradeable_held(Dn2CppObject* r); // IsUpgradeableReadLockHeld
int32_t dn2cpp_rwlock_recursion_policy(Dn2CppObject* r);   // RecursionPolicy (as int32)

// ---- System.Threading.Timer (per-timer OS thread) ----
// A GC-allocated reference object that owns a dedicated OS thread. The thread waits
// dueMs, fires the TimerCallback(state) delegate, and (when periodMs > 0) re-fires every
// periodMs; dueMs < 0 means idle (Timeout.Infinite — fire only after a Change). The
// callback/state are held GC-reachable through the timer (which the running thread roots
// from its scanned stack). Change reschedules; Dispose stops the timer and joins the
// thread (so no callback fires afterward), detaching instead when called from the
// callback to avoid a self-join deadlock.
Dn2CppObject* dn2cpp_timer_new(Dn2CppObject* callback, Dn2CppObject* state,
                               int64_t dueMs, int64_t periodMs);
Dn2CppObject* dn2cpp_timeprovider_timer_new(Dn2CppObject* callback, Dn2CppObject* state,
                                            int64_t dueMs, int64_t periodMs);
int32_t dn2cpp_timer_change(Dn2CppObject* t, int64_t dueMs, int64_t periodMs); // Change: 0 after Dispose, else 1
int32_t dn2cpp_timer_dispose(Dn2CppObject* t); // Dispose: stop + join, returns 1

// ---- ThreadLocal<T> (per-instance, per-thread storage) ----
// Each thread sees its own value for a given ThreadLocal instance. The value is held
// boxed in a single uniform slot, so one runtime shape serves every T; the optional
// Func<T> valueFactory runs lazily on first access on each thread. The per-thread
// nodes hang off the GC-allocated instance, so the boxed values stay GC-reachable.
// kind: 0=reference, 1=int32, 2=int64/native-int, 3=float, 4=double, 5=struct.
// `ti`/`size` box a value-type T (null/0 for reference T).
//
// `boxFn` is how a value-type T's factory is CALLED, and it is emitted rather than
// switched on: a struct returned BY VALUE has no kind, only a layout, and the C ABI
// returning it is a function of that layout. A runtime switch can express
// `int32_t (*)(...)` but not `TimeSpan (*)(...)` without knowing TimeSpan, which only
// the emitter does — so the emitter hands over a captureless thunk that invokes the
// delegate through T's exact C++ ABI and boxes the result. INVARIANT: `boxFn != nullptr`
// and `kind != 0` are in bijection, which is why dn2cpp_threadlocal_get dispatches on
// the pointer rather than re-deriving the kind.
typedef Dn2CppObject* (*Dn2CppThreadLocalBoxFn)(Dn2CppObject* factory);
Dn2CppObject* dn2cpp_threadlocal_new(Dn2CppObject* factory, const Dn2CppTypeInfo* ti, int32_t size,
                                     int32_t kind, Dn2CppThreadLocalBoxFn boxFn);
Dn2CppObject* dn2cpp_threadlocal_get(Dn2CppObject* h);      // this thread's value (lazy factory / default(T))
void dn2cpp_threadlocal_set(Dn2CppObject* h, Dn2CppObject* boxedOrRef); // this thread's value
int32_t dn2cpp_threadlocal_is_created(Dn2CppObject* h);     // 1 if this thread has a value, else 0

// ---- BlockingCollection<T> (producer/consumer blocking queue) ----
// A real mutex + condition-variable bounded/unbounded FIFO. Elements are held in a
// single uniform Dn2CppObject* slot (boxed value-type element, or reference element
// directly), boxed/unboxed by T's kind at each Add/Take call site — one runtime shape
// serves every T. The queued elements live in GC-allocated nodes hung off the
// GC-allocated collection, so they stay reachable while a consumer is blocked.
// boundedCapacity 0 => unbounded. Add/Take throw a catchable InvalidOperationException
// when adding is completed (Add after CompleteAdding; Take on a completed-empty queue).
Dn2CppObject* dn2cpp_blockingcoll_new(int32_t boundedCapacity);
void dn2cpp_blockingcoll_add(Dn2CppObject* coll, Dn2CppObject* boxedOrRef);
int32_t dn2cpp_blockingcoll_tryadd(Dn2CppObject* coll, Dn2CppObject* boxedOrRef, int32_t timeoutMs);
Dn2CppObject* dn2cpp_blockingcoll_take(Dn2CppObject* coll);
int32_t dn2cpp_blockingcoll_trytake(Dn2CppObject* coll, int32_t timeoutMs, Dn2CppObject** out);
void dn2cpp_blockingcoll_complete_adding(Dn2CppObject* coll);
int32_t dn2cpp_blockingcoll_count(Dn2CppObject* coll);
int32_t dn2cpp_blockingcoll_is_adding_completed(Dn2CppObject* coll);
int32_t dn2cpp_blockingcoll_is_completed(Dn2CppObject* coll);
int32_t dn2cpp_blockingcoll_bounded_capacity(Dn2CppObject* coll);

// ---- Array.Sort / Array.Reverse (in place) ----
int32_t dn2cpp_str_compare_ordinal(Dn2CppString* a, Dn2CppString* b);
// In-place over the sub-range [start, start+count). The whole-array overloads pass
// (0, length); the index/length overloads pass the slice.
void dn2cpp_array_reverse_i4(Dn2CppArrayI4* a, int32_t start, int32_t count);
void dn2cpp_array_reverse_ref(Dn2CppArrayRef* a, int32_t start, int32_t count);
void dn2cpp_array_reverse_n(Dn2CppArrayN* a, int32_t start, int32_t count);
void dn2cpp_array_sort_i4(Dn2CppArrayI4* a, int32_t start, int32_t count);   // signed int32
void dn2cpp_array_sort_i8(Dn2CppArrayN* a, int32_t start, int32_t count);    // int64 (long[])
void dn2cpp_array_sort_r8(Dn2CppArrayN* a, int32_t start, int32_t count);    // double[]
void dn2cpp_array_sort_str(Dn2CppArrayRef* a, int32_t start, int32_t count); // ordinal string[]
// Comparer-driven sort: `cmp(ctx, x, y)` is a transpiler-emitted thunk that
// dispatches IComparer<T>.Compare; `ctx` is the comparer. Null comparer is handled
// by the caller (routes to the natural-order helpers above).
void dn2cpp_array_sort_cmp_i4(Dn2CppArrayI4* a, int32_t start, int32_t count, void* ctx, int32_t (*cmp)(void*, int32_t, int32_t));
void dn2cpp_array_sort_cmp_i8(Dn2CppArrayN* a, int32_t start, int32_t count, void* ctx, int32_t (*cmp)(void*, int64_t, int64_t));
void dn2cpp_array_sort_cmp_r8(Dn2CppArrayN* a, int32_t start, int32_t count, void* ctx, int32_t (*cmp)(void*, double, double));
void dn2cpp_array_sort_cmp_ref(Dn2CppArrayRef* a, int32_t start, int32_t count, void* ctx, int32_t (*cmp)(void*, Dn2CppObject*, Dn2CppObject*));
// Non-generic Array.Sort(Array[, index, length][, IComparer]) — the boxed-element sort whose
// element rep is unknown until run time (int[], object[], enum[], user-class[]). Boxes the
// [index, index+length) window into a GC-scanned ref buffer, sorts it with the ref primitive
// above, and writes the permutation back through the reflection SetValue coercion. Comparison is
// the shared boxed order: an explicit IComparer.Compare dispatched through the interface the caller
// names, else — including a null comparer, i.e. Comparer.Default — dn2cpp_object_compare. Both
// interface type-infos arrive as arguments (comparer null / icomparer_ti null when there is no
// comparer), so the runtime names no generated ti_ symbol. Element ordering matches
// MethodCompiler.TryCompareLValue; distinct keys give the unique permutation real .NET produces.
void dn2cpp_array_sort_object(Dn2CppObject* arr, int32_t index, int32_t length,
                              const Dn2CppTypeInfo* icomparable_ti, Dn2CppObject* comparer,
                              const Dn2CppTypeInfo* icomparer_ti, int32_t comparer_slot);
// Element-sized elements (struct / long / double / short / 64-bit enum …): the element
// moves as an opaque byte block and reaches the comparer BY ADDRESS — a struct of arbitrary
// width has no uniform by-value C signature. Covers every element type the i4/ref reps do
// not, so it is the arm Comparer<T>.Default lands on for a value-type element.
void dn2cpp_array_sort_cmp_n(Dn2CppArrayN* a, int32_t start, int32_t count, void* ctx,
                             int32_t (*cmp)(void*, const void*, const void*));
// span.Sort(): a span is a {f__reference, f__length} value whose reference
// points straight at the elements (no array header), so these take a typed element
// pointer + length, mirroring the array helpers above. Natural order, then the
// comparer-driven variants (same thunk shape as the array `_cmp_*` helpers).
void dn2cpp_span_sort_i4(int32_t* p, int32_t n);          // signed int32
void dn2cpp_span_sort_i8(int64_t* p, int32_t n);          // int64
void dn2cpp_span_sort_r8(double* p, int32_t n);           // double
void dn2cpp_span_sort_str(Dn2CppObject** p, int32_t n);   // ordinal string
void dn2cpp_span_sort_cmp_i4(int32_t* p, int32_t n, void* ctx, int32_t (*cmp)(void*, int32_t, int32_t));
void dn2cpp_span_sort_cmp_i8(int64_t* p, int32_t n, void* ctx, int32_t (*cmp)(void*, int64_t, int64_t));
void dn2cpp_span_sort_cmp_r8(double* p, int32_t n, void* ctx, int32_t (*cmp)(void*, double, double));
void dn2cpp_span_sort_cmp_ref(Dn2CppObject** p, int32_t n, void* ctx, int32_t (*cmp)(void*, Dn2CppObject*, Dn2CppObject*));
void dn2cpp_span_sort_cmp_n(void* p, int32_t n, int32_t elemSize, void* ctx,
                            int32_t (*cmp)(void*, const void*, const void*));
// Parallel key+value sort — Array.Sort<TKey,TValue>(keys, items[, cmp]) and
// MemoryExtensions.Sort<TKey,TValue>(Span keys, Span items[, cmp]) alike. `keys`/`items`
// are the two element buffers (an array's data, a span's f__reference) at the strides their
// reps pack at; `items` may be null (a bare key sort, which .NET allows). Keys reach the
// comparison by address, so one routine serves every key rep.
void dn2cpp_sort_pair(void* keys, int32_t keyStride, void* items, int32_t itemStride,
                      int32_t start, int32_t count, void* ctx,
                      int32_t (*cmp)(void*, const void*, const void*));

// System.Buffers.SearchValues<byte|char> — a membership set over byte/char values,
// built by SearchValues.Create and queried by MemoryExtensions.IndexOfAny(span, sv).
// `set8` is a 0..255 membership table (covers byte fully and ASCII char); `hi` holds
// sorted values > 255 (char only; null/0 when none) for a binary-search fallback.
struct Dn2CppSearchValues {
    uint8_t set8[256];
    int32_t* hi;
    int32_t hiCount;
    // String flavor (SearchValues<string>, Regex's leading-strings prefix
    // optimization): candidate strings + comparison. `strs` is GC-scanned so
    // the candidates stay alive through the set.
    Dn2CppString** strs;
    int32_t strCount;
    int32_t strIgnoreCase; // 1 = OrdinalIgnoreCase (ordinal BMP fold), 0 = Ordinal
};

// The *_index_of_any_* helpers take `except`: 0 = first/last index whose element IS in
// the set (IndexOfAny), 1 = first/last index whose element is NOT in it (IndexOfAnyExcept).
// ContainsAny[Except] is lowered to `index_of_any(... except) >= 0` at the call site.
Dn2CppSearchValues* dn2cpp_search_values_create_u8(const uint8_t* vals, int32_t n);
Dn2CppSearchValues* dn2cpp_search_values_create_u16(const uint16_t* vals, int32_t n);
int32_t dn2cpp_search_values_index_of_any_u8(const uint8_t* span, int32_t n, const Dn2CppSearchValues* sv, int32_t except);
int32_t dn2cpp_search_values_index_of_any_u16(const uint16_t* span, int32_t n, const Dn2CppSearchValues* sv, int32_t except);
int32_t dn2cpp_search_values_last_index_of_any_u8(const uint8_t* span, int32_t n, const Dn2CppSearchValues* sv, int32_t except);
int32_t dn2cpp_search_values_last_index_of_any_u16(const uint16_t* span, int32_t n, const Dn2CppSearchValues* sv, int32_t except);
int32_t dn2cpp_search_values_contains_u8(uint8_t value, const Dn2CppSearchValues* sv);
int32_t dn2cpp_search_values_contains_u16(uint16_t value, const Dn2CppSearchValues* sv);
// SearchValues<string> (SearchValues.Create(ReadOnlySpan<string>, StringComparison);
// only Ordinal / OrdinalIgnoreCase exist in .NET). index_of_any returns the leftmost
// start index in the span where any candidate matches, -1 when none does; the
// OrdinalIgnoreCase fold is the exact BMP ordinal fold (dn2cpp_ordinal_upper).
Dn2CppSearchValues* dn2cpp_search_values_create_str(Dn2CppString** vals, int32_t n, int32_t ignoreCase);
int32_t dn2cpp_search_values_index_of_any_str(const char16_t* span, int32_t n, const Dn2CppSearchValues* sv);

// String concatenation of boxed values — and the shared formatter behind
// Object.ToString. A boxed primitive formats by its runtime type; a string
// returns itself; any other object falls back to its type name (matching .NET's
// default Object.ToString). A NULL operand folds to string.Empty, which is what
// every concat/format funnel wants and what an explicit ToString() must NOT do:
// a `x.ToString()` lowering names dn2cpp_object_tostring_virtual instead, which
// throws NullReferenceException on null and then shares this body. The full
// two-caller invariant is written out at the definition.
Dn2CppString* dn2cpp_object_tostring(Dn2CppObject* obj);
Dn2CppString* dn2cpp_object_tostring_virtual(Dn2CppObject* obj);
// Object.GetHashCode / Object.Equals(object) virtual dispatch. If the
// runtime type wires a `gethashcode`/`equals` override, call it; otherwise fall
// back to .NET's defaults — an identity hash derived from the object pointer, and
// reference equality. Null-safe (a null hashes to 0; equals is null-aware).
int32_t dn2cpp_object_gethashcode(Dn2CppObject* obj);
// Object.MemberwiseClone — shallow copy of a fixed-size instance (class or boxed
// value type); fails on variable-size objects (string/array), whose managed
// Clone paths never call it.
Dn2CppObject* dn2cpp_object_memberwise_clone(Dn2CppObject* obj);
int32_t dn2cpp_object_equals(Dn2CppObject* a, Dn2CppObject* b);
// Three-way ordering (-1/0/+1) of two boxed values by runtime type — the object-element
// counterpart of dn2cpp_object_equals, for the non-generic Array.Sort/BinarySearch(Array, …)
// lowerings whose element type is unknown until run time (MethodCompiler.EmitIntrinsic.EnumArray).
// .NET Comparer.Default null order: null sorts first. Boxed primitive/enum/string compare INLINE
// because the inline arms are tried FIRST (not for want of a map) — each integer at its own width
// AND signedness, since ordering cannot lump widths the way equality does, and float/double on the
// NaN-aware TOTAL order the sole static comparison window (MethodCompiler.TryCompareLValue) emits,
// so a value ordered here and one ordered inline agree. Decimal and the date/time value types are
// inline too, through that same window's intrinsic three-ways — this is the ladder the
// boxed-built-in IComparable thunk delegates to, and it may not refuse a type whose type test
// claims IComparable. A user reference type dispatches the non-generic
// System.IComparable.CompareTo(object) through the closed type-info the caller supplies
// (icomparable_ti — nullptr if the transpiler could not resolve System.IComparable). A value that is
// neither is refused with a catchable PlatformNotSupportedException naming the type, never a silent 0.
// String order is ORDINAL (dn2cpp_str_compare(…,4)) — the same deliberate divergence from
// culture-sensitive Comparer<string>.Default the generic sort/search path already makes.
int32_t dn2cpp_object_compare(Dn2CppObject* a, Dn2CppObject* b, const Dn2CppTypeInfo* icomparable_ti);

// System.Enum::CompareTo(object): the synthesized enum value body (BrEnumInstanceFormat) calls
// this. Null target sorts first (this > null -> 1), a cross-enum-type target is an
// ArgumentException, and same type delegates to dn2cpp_object_compare's boxed-enum width ladder.
int32_t dn2cpp_enum_compareto(Dn2CppObject* a, Dn2CppObject* b);

// Double/Single value semantics, shared by the two callers that must agree: the
// boxed arms of dn2cpp_object_equals/_gethashcode, and the transpiler's inline
// emit for a Dictionary<double,V>/HashSet<float> key. `Equals` is NOT `==` (NaN
// equals NaN) and the hash is NOT the number (it is the bits, with every NaN and
// both zeros normalized) — so a key hashed inline and the same key hashed boxed
// land in the same bucket only because both go through these.
int32_t dn2cpp_double_equals(double a, double b);
int32_t dn2cpp_single_equals(float a, float b);
int32_t dn2cpp_double_hash(double v);
int32_t dn2cpp_single_hash(float v);
Dn2CppString* dn2cpp_string_concat_objects(Dn2CppArrayRef* objs);
// Concat over the first `n` elements (a List<T>'s live prefix).
Dn2CppString* dn2cpp_string_concat_objects_n(Dn2CppArrayRef* objs, int32_t n);

// string.Join over an array of T, separated by `sep` (no trailing separator).
// Element formatting matches Object.ToString per kind. The `_n` forms join
// only the first `n` elements — a List<T>'s live prefix (`n` = Count), since its
// backing array's allocated length is the capacity ≥ Count.
Dn2CppString* dn2cpp_string_join_i4(Dn2CppString* sep, Dn2CppArrayI4* a);
Dn2CppString* dn2cpp_string_join_i8(Dn2CppString* sep, Dn2CppArrayN* a);
Dn2CppString* dn2cpp_string_join_r8(Dn2CppString* sep, Dn2CppArrayN* a);
Dn2CppString* dn2cpp_string_join_ref(Dn2CppString* sep, Dn2CppArrayRef* a);
Dn2CppString* dn2cpp_string_join_ch(Dn2CppString* sep, Dn2CppArrayN* a);
// Unsigned 32/64-bit element joins (join_i4/join_i8 would format signed).
Dn2CppString* dn2cpp_string_join_u4(Dn2CppString* sep, Dn2CppArrayI4* a);
Dn2CppString* dn2cpp_string_join_u8(Dn2CppString* sep, Dn2CppArrayN* a);
Dn2CppString* dn2cpp_string_join_i4_n(Dn2CppString* sep, Dn2CppArrayI4* a, int32_t n);
Dn2CppString* dn2cpp_string_join_i8_n(Dn2CppString* sep, Dn2CppArrayN* a, int32_t n);
Dn2CppString* dn2cpp_string_join_u4_n(Dn2CppString* sep, Dn2CppArrayI4* a, int32_t n);
Dn2CppString* dn2cpp_string_join_u8_n(Dn2CppString* sep, Dn2CppArrayN* a, int32_t n);
Dn2CppString* dn2cpp_string_join_r8_n(Dn2CppString* sep, Dn2CppArrayN* a, int32_t n);
Dn2CppString* dn2cpp_string_join_ref_n(Dn2CppString* sep, Dn2CppArrayRef* a, int32_t n);
// Join(separator, string[], startIndex, count) — the 4-arg slice form (null
// array ANE, bad slice catchable AOORE, both like .NET).
Dn2CppString* dn2cpp_string_join_ref_range(Dn2CppString* sep, Dn2CppArrayRef* a,
                                           int32_t startIndex, int32_t count);
// Join/Concat over a span's data pointer + length (the params
// ReadOnlySpan<object|string> overloads). Elements render via Object.ToString;
// null elements contribute nothing.
Dn2CppString* dn2cpp_string_join_objs(Dn2CppString* sep, Dn2CppObject* const* d, int32_t n);
Dn2CppString* dn2cpp_string_concat_objs(Dn2CppObject* const* d, int32_t n);
Dn2CppString* dn2cpp_string_join_ch_n(Dn2CppString* sep, Dn2CppArrayN* a, int32_t n);
int32_t dn2cpp_object_hashcode(Dn2CppObject* obj);

Dn2CppArrayI4* dn2cpp_newarr_i4(int32_t length);
Dn2CppArrayRef* dn2cpp_newarr_ref(int32_t length);
Dn2CppArrayN* dn2cpp_newarr_n(int32_t length, int32_t elemSize);
// Typed array allocation: header set to the precise per-element ti_arr_<T>
// (null falls back to the shared array_{ref,i4} handle). The untyped forms above are
// these with the shared default — used for runtime-internal arrays.
Dn2CppArrayI4* dn2cpp_newarr_i4_t(int32_t length, const Dn2CppTypeInfo* ti);
Dn2CppArrayRef* dn2cpp_newarr_ref_t(int32_t length, const Dn2CppTypeInfo* ti);
Dn2CppArrayN* dn2cpp_newarr_n_t(int32_t length, int32_t elemSize, const Dn2CppTypeInfo* ti);
Dn2CppArrayN* dn2cpp_newarr_n_atomic_t(int32_t length, int32_t elemSize, const Dn2CppTypeInfo* ti);
// Array.Empty<T>: the per-element-type cached length-0 singleton (same instance
// every call, no per-call allocation — .NET's EmptyArray<T>.Value). Keyed on the
// precise ti_arr_<T> handle; rep/elemSize selection happens at the emit site,
// exactly as for the newarr helpers above.
Dn2CppArrayI4* dn2cpp_array_empty_i4(const Dn2CppTypeInfo* ti);
Dn2CppArrayRef* dn2cpp_array_empty_ref(const Dn2CppTypeInfo* ti);
Dn2CppArrayN* dn2cpp_array_empty_n(const Dn2CppTypeInfo* ti, int32_t elemSize);
Dn2CppArrayN* dn2cpp_array_empty_n_atomic(const Dn2CppTypeInfo* ti, int32_t elemSize);
Dn2CppMDArray* dn2cpp_newmdarr(const Dn2CppTypeInfo* ti, int32_t rank, const int32_t* lengths, int32_t elemSize);
// Materialize a fixed list of int32_t length/index expressions as a temporary
// a callee can point at: `dn2cpp_i32s(a, b).v` decays to const int32_t*.
// Standard-C++ stand-in for a C99 compound literal
// `(const int32_t[]){a, b}` at MD-array sites
// (newobj int[,] / Get / Set / Address) — valid C, but only a GNU extension in
// C++ and rejected outright by MSVC (C4576). The temporary lives to the end of
// the full expression, and dn2cpp_newmdarr / dn2cpp_md_elem_addr copy/consume
// the values during the call, so an argument-position temporary is exactly
// enough.
template <int N>
struct Dn2CppI32Pack { int32_t v[N]; };
template <class... A>
inline Dn2CppI32Pack<static_cast<int>(sizeof...(A))> dn2cpp_i32s(A... a)
{
    return { { static_cast<int32_t>(a)... } };
}

// Array.Clone() — a shallow copy. A new array of the same precise
// type-info (so clone.GetType() == src.GetType()) and length, with the element
// block memcpy'd verbatim (value-type elements copied bitwise; reference-type
// element pointers shared). One helper per rep, dispatched at emit time on the
// receiver's static C++ array type (the real body routes through Array.Copy /
// MethodTable internals we don't model). Returns object (the call site casts it
// back). A null receiver faults like .NET's NullReferenceException.
Dn2CppArrayI4* dn2cpp_array_clone_i4(Dn2CppArrayI4* src);
Dn2CppArrayRef* dn2cpp_array_clone_ref(Dn2CppArrayRef* src);
Dn2CppArrayN* dn2cpp_array_clone_n(Dn2CppArrayN* src);
Dn2CppObject* dn2cpp_array_clone_dyn(Dn2CppObject* src); // rep from runtime type-info
// Array.Copy/Clear on non-concrete static array types (shared-generic T[]
// bodies, and every Copy pair the emitter cannot prove same-element); rep from
// the source/target's runtime type-info. Two identical identities memmove/memset
// like EmitArrayCopy/EmitArrayClear's typed arms; a mixed Copy pair runs the
// CLR's full compatibility verdict (dn2cpp_array_copy_checked). Clear
// takes one array, so no type question arises on it.
void dn2cpp_array_copy_dyn(Dn2CppObject* src, int32_t srcIdx, Dn2CppObject* dst, int32_t dstIdx, int32_t len);
void dn2cpp_array_clear_dyn(Dn2CppObject* arr, int32_t idx, int32_t len);
// The mixed-identity half of dn2cpp_array_copy_dyn (defined beside the
// CanPrimitiveWiden matrix in dn2cpp_system_reflection.cpp): .NET's Array.Copy
// type-compatibility verdict and its per-element widen/box/unbox/cast arms.
// Callers have already validated null, rank equality and both ranges.
void dn2cpp_array_copy_checked(Dn2CppObject* src, int32_t srcIdx, Dn2CppObject* dst, int32_t dstIdx, int32_t len);

// RuntimeHelpers.GetSubArray<T>(T[], Range) = array[range]. The Range's
// two Index ._value fields are resolved against the source length into (offset, length)
// — matching System.Range.GetOffsetAndLength exactly (from-end index when _value < 0,
// (uint) bounds checks reject end > length / start > end with a catchable
// ArgumentOutOfRangeException) — then a fresh array of the source's precise type-info +
// slice length is allocated and the [offset, offset+length) element run is copied verbatim
// (shallow: reference-element pointers shared; a null source throws ArgumentNullException).
// One copy helper per rep, dispatched at emit time on the source's static C++ array type.
int32_t dn2cpp_range_offset_length(int32_t startVal, int32_t endVal, int32_t srcLen, int32_t* outOffset);
Dn2CppArrayI4* dn2cpp_array_subarray_i4(Dn2CppArrayI4* src, int32_t offset, int32_t length);
Dn2CppArrayRef* dn2cpp_array_subarray_ref(Dn2CppArrayRef* src, int32_t offset, int32_t length);
Dn2CppArrayN* dn2cpp_array_subarray_n(Dn2CppArrayN* src, int32_t offset, int32_t length);

// Build the managed `string[] args` a `static Main(string[])` entry point
// receives, from the native `(argc, argv)`. .NET's `args` excludes the program
// name (argv[0]), so this copies argv[1..argc-1] as UTF-8-decoded strings into a
// fresh ref array tagged with `ti` (the precise ti_arr_string handle so
// args.GetType() is String[]). argv[0]-only / argc 0 yields an empty array.
Dn2CppArrayRef* dn2cpp_argv_to_string_array(int argc, char** argv, const Dn2CppTypeInfo* ti);

// System.IO.Path (pure lexical, Unix '/' separator). Semantics probed
// against real .NET and matched exactly. GetDirectoryName returns null (not "")
// for null/empty/root, like .NET. GetFullPath collapses '.'/'..'/'//' against
// the cwd without resolving symlinks or requiring the path to exist.
int32_t dn2cpp_path_is_rooted(Dn2CppString* p);
Dn2CppString* dn2cpp_path_get_filename(Dn2CppString* p);
Dn2CppString* dn2cpp_path_get_directory_name(Dn2CppString* p);
Dn2CppString* dn2cpp_path_get_extension(Dn2CppString* p);
Dn2CppString* dn2cpp_path_get_filename_without_extension(Dn2CppString* p);
Dn2CppString* dn2cpp_path_combine2(Dn2CppString* a, Dn2CppString* b);
Dn2CppString* dn2cpp_path_combine3(Dn2CppString* a, Dn2CppString* b, Dn2CppString* c);
Dn2CppString* dn2cpp_path_combine4(Dn2CppString* a, Dn2CppString* b, Dn2CppString* c, Dn2CppString* d);
Dn2CppString* dn2cpp_path_get_full_path(Dn2CppString* p);

// System.IO.File. UTF-8 with no BOM on write; a leading UTF-8 BOM is
// stripped on read (matching .NET). Error paths throw the .NET exception types
// (FileNotFoundException / UnauthorizedAccessException / IOException). ReadAllBytes
// takes the precise ti_arr_Byte handle so the result's GetType() is Byte[].
int32_t dn2cpp_file_exists(Dn2CppString* path);
void dn2cpp_file_delete(Dn2CppString* path);
Dn2CppString* dn2cpp_file_read_all_text(Dn2CppString* path);
Dn2CppArrayN* dn2cpp_file_read_all_bytes(Dn2CppString* path, const Dn2CppTypeInfo* ti);
void dn2cpp_file_write_all_text(Dn2CppString* path, Dn2CppString* contents);
void dn2cpp_file_write_all_bytes(Dn2CppString* path, Dn2CppArrayN* bytes);

// System.Net.Http transport (dn2cpp_http2_stream.cpp — libcurl over Mbed TLS,
// DN2CPP_USE_CURL; http:// and https:// alike): the streaming call family
// (dn2cpp_http2_call_*), the route EVERY managed request takes, so that a caller
// returns at the response HEADER block (HttpCompletionOption.ResponseHeadersRead)
// and a cancel reaches the transfer through abort()'s curl_multi_wakeup rather
// than waiting out a poll interval.
//
// The calls never throw — a null/empty url or a transport failure (DNS/TCP refused,
// timeout, unverifiable TLS certificate) is a failed call whose error() names the
// cause, the shape HttpClient reports *before* EnsureSuccessStatusCode(); an HTTP
// response, even 404/500, signals headers-ready with that status. Every pointer an
// accessor hands back is owned by the handle and valid only until free().
//
// HTTP version selection: httpVersion is Major*10+Minor (0 = unset = HTTP/1.1,
// matching real .NET's request default), versionPolicy the System.Net.HttpVersionPolicy
// value (0 RequestVersionOrLower, 1 Exact, 2 OrHigher). The 1.x rows always pin
// CURLOPT_HTTP_VERSION explicitly — with nghttp2 linked curl's own default is
// attempt-h2-over-TLS, and the transport must not change protocol under a request that
// never asked. HTTP/2 negotiates per policy (ALPN over https, prior knowledge over
// cleartext http); a policy the negotiated protocol cannot satisfy is a TRANSPORT
// failure naming both sides, and HTTP/3 is refused the same way. http_version()
// reports the NEGOTIATED protocol (10/11/20, 0 unknown), which the managed side must
// surface as HttpResponseMessage.Version — grpc-dotnet rejects anything but 2.0.
//
// Trailers are collected SEPARATELY from headers and never folded into the header
// list: gRPC reads grpc-status from the trailer section, and a grpc-status among the
// HEADERS means a trailers-only response, so merging the two would change what a
// response says. Nothing is synthesized when there are no trailers.
//
// Requires DN2CPP_USE_CURL (the default): with -DDN2CPP_USE_CURL=OFF these symbols are
// undefined and the link fails loudly, as with the zlib/brotli surfaces. Emscripten is
// the exception, not a softening — a browser has no TCP socket layer, so the option is
// forced off and dn2cpp_http2_stream.cpp's fallback arm defines this whole surface with
// every call failing with an error naming the platform. "You declined the transport"
// and "this target cannot have one" are different questions; only the first is a link
// error.
//
// TLS: peer and hostname verification are ON and cannot be turned off through this
// surface. The trust anchors are the Mozilla bundle COMPILED INTO the binary (Mbed TLS
// reads no OS trust store), overridable per process with DN2CPP_HTTP_CAINFO=<pem path>,
// which swaps the anchor set and never the verification.
//
// A plain C ABI on purpose: the DnHttp managed shim binds it with
// [DllImport("dn2cpp_http")], which resolves by NAME, so the entry points must be
// UNMANGLED C symbols taking the shapes dn2cpp's P/Invoke marshalling emits — string ->
// UTF-8 char*, string[] -> char**, byte[] -> element pointer, out int -> int* — never a
// raw Dn2CppString*/Dn2CppArray*. Dropping the extern "C" mangles the symbols and the
// shim's [DllImport] cannot bind.
//
// One opaque handle per call. open() starts the transfer, handed to the ONE
// process-wide transport thread whose single multi handle is what lets concurrent calls
// MULTIPLEX one h2 connection. bodyMode 0 is bodyless; 2 arms an unknown-length
// incremental upload (h2 DATA frames, chunked over HTTP/1.1) fed by
// write()/finish_send(); 1 hands the WHOLE body over here, going out through
// CURLOPT_POSTFIELDS — Content-Length rather than chunked, and rewindable so a redirect
// can re-send it, neither of which the incremental route can do.
//
// The two pool seconds are SocketsHttpHandler's connection-lifetime knobs applied per
// easy handle (0 = no limit, how Timeout.InfiniteTimeSpan crosses). The two structural
// pool knobs are HONORED, not merely reported: maxConnsPerServer (0 = unlimited) caps
// CONCURRENT connections per host and transfers past it queue, as real .NET queues past
// MaxConnectionsPerServer; multipleH2 == 0 (EnableMultipleHttp2Connections's default)
// makes an h2-capable request wait for the connection already being established and
// multiplex onto it. The cap's one approximation is scope — per process, not per
// handler.
//
// write() never blocks and never takes a chunk partially: 0 = the call already
// failed/finished, 1 = taken, 2 = taken and the send queue is at its bound, so the
// caller must not write again until await_send_drain() answers (1 writable, 0 call
// ended). That wait blocks, so it belongs on a pool worker — it is what turns a full
// queue into a pending Task instead of a stalled scheduler thread. Bounds:
// DN2CPP_HTTP_SEND_HIGH_WATER (default 1 MiB, above any gRPC message so a
// message-sized writer never parks) and DN2CPP_HTTP_RECV_HIGH_WATER (default 4 MiB,
// past which delivery pauses until read() drains); DN2CPP_HTTP_SEND_STATS=1 reports
// park count and peak backlog at free.
//
// wait_headers() blocks until the response header block is complete (1) or the
// transport failed (0 — a version policy the negotiated protocol cannot satisfy fails
// HERE, before any body). read() blocks for the next body bytes (>0), EOS (0 — trailers
// are final from then on), or a mid-stream failure (-1; buffered bytes drain first, so
// the stream breaks where the transfer did); cap == 0 is the zero-byte availability
// probe, which consumes nothing and answers 0 only when the call failed — only a SIZED
// read's 0 means EOS. abort() releases every blocked caller and stops the transfer;
// free() aborts, joins and releases, and the caller must serialize it against in-flight
// calls on the same handle (the DnHttp wrapper refcounts).
struct Dn2CppHttp2Call; // opaque; real def is private to dn2cpp_http2_stream.cpp
extern "C" {
Dn2CppHttp2Call* dn2cpp_http2_call_open(
    const char* method, const char* url,
    const char* const* headers, int32_t headerCount,
    int32_t bodyMode,        // 0 none, 1 complete (body/bodyLen), 2 incremental (write())
    const uint8_t* body,     // read only under bodyMode 1
    int32_t bodyLen,         // length of `body` under bodyMode 1
    int32_t httpVersion, int32_t versionPolicy,
    int32_t maxIdleSeconds,      // SocketsHttpHandler.PooledConnectionIdleTimeout, 0 = none
    int32_t maxLifetimeSeconds,  // SocketsHttpHandler.PooledConnectionLifetime, 0 = none
    int32_t maxConnsPerServer,   // SocketsHttpHandler.MaxConnectionsPerServer, 0 = unlimited
    int32_t multipleH2);         // SocketsHttpHandler.EnableMultipleHttp2Connections (0/1)
int32_t     dn2cpp_http2_call_write(Dn2CppHttp2Call* c, const uint8_t* buf, int32_t len);
int32_t     dn2cpp_http2_call_await_send_drain(Dn2CppHttp2Call* c); // blocks; pool worker only
void        dn2cpp_http2_call_finish_send(Dn2CppHttp2Call* c);
int32_t     dn2cpp_http2_call_wait_headers(Dn2CppHttp2Call* c);
int32_t     dn2cpp_http2_call_status(Dn2CppHttp2Call* c);
int32_t     dn2cpp_http2_call_http_version(Dn2CppHttp2Call* c); // negotiated 10/11/20; 0 unknown
const char* dn2cpp_http2_call_reason(Dn2CppHttp2Call* c);       // owned by handle
const char* dn2cpp_http2_call_error(Dn2CppHttp2Call* c);        // nullptr when not failed
int32_t     dn2cpp_http2_call_header_count(Dn2CppHttp2Call* c);
const char* dn2cpp_http2_call_header_name(Dn2CppHttp2Call* c, int32_t index);
const char* dn2cpp_http2_call_header_value(Dn2CppHttp2Call* c, int32_t index);
int32_t     dn2cpp_http2_call_read(Dn2CppHttp2Call* c, uint8_t* buf, int32_t cap);
int32_t     dn2cpp_http2_call_trailer_count(Dn2CppHttp2Call* c);
const char* dn2cpp_http2_call_trailer_name(Dn2CppHttp2Call* c, int32_t index);
const char* dn2cpp_http2_call_trailer_value(Dn2CppHttp2Call* c, int32_t index);
void        dn2cpp_http2_call_abort(Dn2CppHttp2Call* c);
void        dn2cpp_http2_call_free(Dn2CppHttp2Call* c);
}

// System.Environment + System.IO.Directory. GetEnvironmentVariable returns
// null when unset (matching .NET). CurrentDirectory get/set map to getcwd/chdir;
// Directory.Exists is true only for a directory (false for missing/file/null).
Dn2CppString* dn2cpp_env_get_variable(Dn2CppString* name);
Dn2CppString* dn2cpp_env_get_current_directory();
void dn2cpp_env_set_current_directory(Dn2CppString* path);
int32_t dn2cpp_directory_exists(Dn2CppString* path);

// The User/Machine target of Environment.GetEnvironmentVariable, whose Windows
// body reads the registry through the Advapi32 P/Invokes (no intrinsic mapping).
// Reads HKCU\Environment (fromMachine=0) / HKLM\...\Session Manager\Environment
// (fromMachine=1) directly, so the native binary and real .NET agree on the value:
// REG_SZ raw, REG_EXPAND_SZ expanded via ExpandEnvironmentStringsW (the internal
// RegistryKey.GetValue's REG_EXPAND_SZ arm ends in ExpandEnvironmentVariables);
// null for a missing key/value or a non-string type. Always null on POSIX (no
// registry — the Unix CoreLib body returns null too).
Dn2CppString* dn2cpp_env_get_variable_from_registry(Dn2CppString* name, int32_t fromMachine);

// The running process image, asked of the kernel (never derived from argv[0]).
// Environment.ProcessPath is the executable's absolute path, or null on a host
// that has none (wasm). AppContext.BaseDirectory is the directory holding it,
// *with* a trailing separator — as real .NET's GetBaseDirectoryCore leaves it —
// or "" when the path is unavailable. A binary produced by dn2cpp has no managed
// entry assembly to derive either from, so both come from the process image, the
// way NativeAOT resolves them.
Dn2CppString* dn2cpp_process_path();
Dn2CppString* dn2cpp_app_base_directory();

// Directory.CreateDirectory — recursive mkdir (creates every
// missing parent), idempotent on an existing directory (no throw). null throws
// ArgumentNullException, "" throws ArgumentException, any other failure surfaces
// a catchable IOException (DirectoryNotFoundException — when a path component is
// a file — is not modelled, matching the cwd-set helper). The caller
// discards the .NET DirectoryInfo return value, so no value is produced here.
void dn2cpp_directory_create(Dn2CppString* path);

// ─── System.IO.MemoryMappedFiles (file-backed, POSIX mmap) ──────────────────
// A bounded MemoryMappedFile subset mapped through POSIX mmap/munmap on the
// macOS/POSIX target. We model the three BCL reference types as small by-value
// intrinsic structs (a non-moving handle, like GCHandle): the MemoryMappedFile
// is an open fd + length + access; a MemoryMappedViewAccessor is one mmap'd
// (page-aligned) range; its SafeMemoryMappedViewHandle is the {addr, byteLength}
// pair the raw-pointer scan (AcquirePointer) reads. The real BCL bodies are the
// SafeHandle/UnmanagedMemoryAccessor + OS-mapping P/Invoke cascade we don't model.
// Named maps / cross-process / CreateNew / non-null mapName / CreateViewStream /
// the Windows path are carve-outs (loud NotSupportedException) for the X epic.
struct Dn2CppMappedFile
{
    int32_t fd;       // open file descriptor (-1 once disposed)
    int32_t access;   // MemoryMappedFileAccess: 0 = ReadWrite, 1 = Read
    int64_t length;   // file length in bytes at open time
};
struct Dn2CppMappedView
{
    uint8_t* addr;    // user-visible offset 0 (page base + intra-page delta)
    uint8_t* mapBase; // page-aligned mmap base (the munmap/msync pointer)
    int64_t  mapLen;  // bytes passed to mmap (the munmap/msync length)
    int64_t  capacity;// user-visible view size
    int32_t  access;
};
struct Dn2CppMappedSafeHandle
{
    uint8_t* addr;       // the mapped region base (== view.addr)
    int64_t  byteLength; // SafeBuffer.ByteLength (== view.capacity)
};

// FileMode (System.IO): Open=3 / OpenOrCreate=4 supported. MemoryMappedFileAccess:
// ReadWrite=0 / Read=1 supported. mapName must be null. Any other value throws
// NotSupportedException; a missing file throws FileNotFoundException; an mmap/io
// failure throws IOException.
Dn2CppMappedFile dn2cpp_mmap_create_from_file(Dn2CppString* path, Dn2CppString* mapName,
                                              int32_t fileMode, int32_t access, int64_t capacity);
void dn2cpp_mmap_file_dispose(Dn2CppMappedFile f);
// CreateViewAccessor: mmap [offset, offset+size) (size 0 = rest of file from offset).
Dn2CppMappedView dn2cpp_mmap_create_view(Dn2CppMappedFile f, int64_t offset, int64_t size, int32_t access);
void dn2cpp_mmap_view_flush(Dn2CppMappedView v);   // msync(MS_SYNC)
void dn2cpp_mmap_view_dispose(Dn2CppMappedView v); // munmap
// ReadArray<T>/WriteArray<T>: bulk copy between the view and a managed array's
// element buffer (the transpiler passes element-0+offset). ReadArray returns the
// element count actually transferred (clamped to the view capacity).
int32_t dn2cpp_mmap_read_into(Dn2CppMappedView v, int64_t pos, void* dst, int32_t count, int32_t elemSize);
void dn2cpp_mmap_write_from(Dn2CppMappedView v, int64_t pos, const void* src, int32_t count, int32_t elemSize);

// HashHelpers: the BCL's prime-bucket sizing + fast-modulo helpers, used by
// Dictionary<K,V>. Its real Primes table is a ReadOnlySpan over RVA blob data
// (RuntimeHelpers.CreateSpan) we don't model; reimplement the small, stable
// algorithm here and map HashHelpers to intrinsics.
int32_t dn2cpp_hashhelpers_getprime(int32_t min);
int32_t dn2cpp_hashhelpers_expandprime(int32_t oldSize);
// HashHelpers.Primes: a ReadOnlySpan<int> over the cached prime ladder. The BCL
// builds it via RuntimeHelpers.CreateSpan over an RVA blob (untranspilable); hand
// out the same table from file scope instead.
const int32_t* dn2cpp_hashhelpers_primes_data();
int32_t dn2cpp_hashhelpers_primes_count();
uint64_t dn2cpp_hashhelpers_getfastmodmultiplier(uint32_t divisor);
uint32_t dn2cpp_hashhelpers_fastmod(uint32_t value, uint32_t divisor, uint64_t multiplier);

// The SZArray bounds check every emitted element access goes through — ldelem,
// stelem and ldelema, in all four reps (dn2cpp_ldelem_i4 / dn2cpp_stelem_i4 /
// dn2cpp_ldelem_ref / dn2cpp_stelem_ref / dn2cpp_elem_addr all call it), so an
// out-of-range `arr[i]` anywhere in a transpiled program lands here. Real .NET
// raises IndexOutOfRangeException for every one of those shapes, read and write,
// negative index and past-the-end alike (measured on CoreCLR), and a catchable
// throw is what a `catch (IndexOutOfRangeException)` around a parse loop needs;
// dn2cpp_fail would end the process. [HotPath] opts the check out entirely
// (MethodInfo.SkipBoundsChecks), which is unchanged: this is the throw taken
// when the check is present and fires.
inline void dn2cpp_bounds_check(Dn2CppArray* arr, int32_t index)
{
    if (arr == nullptr)
        dn2cpp_throw_null_reference();
    if (index < 0 || index >= arr->length)
        dn2cpp_throw_index_out_of_range();
}

// An SZArray `.Length` read (ldlen, and the Array.get_Length / GetLength /
// GetUpperBound lowerings whose receiver is statically an SZArray). The read
// itself is the dereference, so without this the EMITTED body faults on a null
// array — a SIGSEGV naming nothing, where .NET raises a catchable
// NullReferenceException. The happy path is one compare on a pointer the same
// expression dereferences anyway, and where the length feeds a loop guard the
// branch hoists out with it. Keep it a function rather than splicing
// `((Dn2CppArray*)a)->length` at the call site: the check must be sequenced BEFORE
// the member address is formed, and only a call guarantees that.
inline int32_t dn2cpp_array_length(Dn2CppArray* arr)
{
    if (arr == nullptr)
        dn2cpp_throw_null_reference();
    return arr->length;
}

// The two operand guards for the array helpers the emitter lowers to an inline
// memmove/memset off the element pointers (Array.Copy, Array.Clear,
// Array.CopyTo). Those arms dereference their operands directly, so the check
// belongs at the call site — and its exception type depends on which surface the
// operand came from, exactly the line the _dyn fallbacks already draw:
// Array.Copy/Array.Clear are STATIC and take the array as an ARGUMENT (.NET:
// ArgumentNullException), while Array.CopyTo's source operand IS the instance
// (.NET: NullReferenceException). Each returns the pointer so the guard wraps
// the cast that feeds the move — the call is sequenced before the member access
// on its result, which forming the address off an unchecked pointer would not be.
// One predictable compare in front of an O(n) block move.
template <typename TArray>
inline TArray* dn2cpp_array_require_arg(TArray* arr)
{
    if (arr == nullptr)
        dn2cpp_throw_argument_null();
    return arr;
}

template <typename TArray>
inline TArray* dn2cpp_array_require_receiver(TArray* arr)
{
    if (arr == nullptr)
        dn2cpp_throw_null_reference();
    return arr;
}

// The RANGE half of the same contract. One block move serves the whole call, so
// an unchecked index or length is an out-of-bounds memmove/memset over
// neighbouring heap objects — one check per call, never per element. The two
// exception families are .NET's own and they differ: Array.Copy answers a
// negative operand with ArgumentOutOfRangeException and an overrun with
// ArgumentException, while Array.Clear answers both with
// IndexOutOfRangeException. The sums are 64-bit because `idx + len` in int32
// wraps back under the length test (Array.Clear(a, 1, int.MaxValue)).
inline void dn2cpp_array_copy_range(int32_t srcLen, int32_t srcIdx,
                                    int32_t dstLen, int32_t dstIdx, int32_t len)
{
    if ((srcIdx | dstIdx | len) < 0)
        dn2cpp_throw_argument_out_of_range();
    if (static_cast<int64_t>(srcIdx) + len > srcLen
        || static_cast<int64_t>(dstIdx) + len > dstLen)
        dn2cpp_throw_argument();
}

inline void dn2cpp_array_clear_range(int32_t arrLen, int32_t idx, int32_t len)
{
    if ((idx | len) < 0 || static_cast<int64_t>(idx) + len > arrLen)
        dn2cpp_throw_index_out_of_range();
}

// ── The two managed-fault guards the EMITTED body carries ────────────────────
//
// Everything above guards one BCL lowering each. These two guard the two IL shapes
// that fault in ordinary transpiled code — an instance access on a null receiver,
// and an integer division by zero — so they sit on every emitted
// `callvirt`/`ldfld`/`div` rather than on a named helper. Each is an inline
// function over a single-assignment temp, so where the value is provably non-null
// or the divisor is constant clang folds the branch away, and otherwise the compare
// is on a register the next instruction dereferences anyway. See
// docs/ARCHITECTURE.md §4-F for the measurement and the rejected alternatives.
//
// DN2CPP_NO_NULL_CHECKS / DN2CPP_NO_ARITH_CHECKS (CMake DN2CPP_NULL_CHECKS=OFF /
// DN2CPP_ARITH_CHECKS=OFF) degrade each family to the unguarded operation, and
// restore its undefined behaviour. They are NATIVE-BUILD switches and deliberately
// not transpiler flags: the emitted C++ is byte-identical either way, so the
// self-host fixpoint cannot see them and an A/B measurement compares the same
// generated.cpp with only the check removed.

// The instance-access guard. `x.M()`, `x.f`, `x->type->vtable[n]` and the
// interface-dispatch loads all form a member address off a receiver the
// emitted body never tested; a null one is a SIGSEGV that names nothing, where
// .NET raises a catchable NullReferenceException that a Godot engine
// trampoline catches and logs (runtime/godot/dn2cpp_godot.cpp guards all six
// entry points), so one broken node degrades instead of killing the boot. Under
// -O2 with a statically-null receiver it is worse than a SIGSEGV: the deref is UB,
// so clang may delete the call and let control fall through silently.
//
// It returns the pointer so the guard WRAPS the cast that forms the address:
// `((T*)dn2cpp_null_check(p))->f` is still an lvalue, so one splice serves read,
// write and `ldflda` alike, and the call is sequenced before the member access.
template <typename T>
inline T* dn2cpp_null_check(T* obj)
{
#ifndef DN2CPP_NO_NULL_CHECKS
    if (obj == nullptr)
        dn2cpp_throw_null_reference();
#endif
    return obj;
}

// The integer div/rem guards. Real .NET: `1/0`, `1%0` and `1u/0u` all raise
// DivideByZeroException; `int.MinValue / -1` AND `int.MinValue % -1` both raise
// OverflowException — the remainder too, which is NOT the C answer (`% -1` is 0).
// The MIN/-1 pair is also C++ UB, so that arm keeps the emitted `/` out of
// undefined behaviour rather than merely naming the fault.
//
// Signed and unsigned are separate templates, not one body with a signedness test:
// on unsigned operands `(T)-1` is the maximum value and the MIN test degenerates to
// `a == 0`, so a shared body would raise OverflowException for `0u / UINT_MAX`.
//
// The signed minimum goes through an unsigned intermediate on purpose:
// `(T)1 << (bits - 1)` shifts into the sign bit, which is the very signed-overflow
// UB these guards exist to stop. Widening to `unsigned long long` and narrowing back
// is well-defined for every T the emitter passes (int8_t..int64_t and intptr_t —
// Math.DivRem's sub-word overloads reach here too).
template <typename T>
inline T dn2cpp_int_min()
{
    return (T)(1ull << (sizeof(T) * 8 - 1));
}

template <typename T>
inline T dn2cpp_div_signed(T a, T b)
{
#ifndef DN2CPP_NO_ARITH_CHECKS
    if (b == (T)0)
        dn2cpp_throw_divide_by_zero();
    if (b == (T)-1 && a == dn2cpp_int_min<T>())
        dn2cpp_overflow();
#endif
    return (T)(a / b);
}

template <typename T>
inline T dn2cpp_rem_signed(T a, T b)
{
#ifndef DN2CPP_NO_ARITH_CHECKS
    if (b == (T)0)
        dn2cpp_throw_divide_by_zero();
    if (b == (T)-1 && a == dn2cpp_int_min<T>())
        dn2cpp_overflow();
#endif
    return (T)(a % b);
}

template <typename T>
inline T dn2cpp_div_unsigned(T a, T b)
{
#ifndef DN2CPP_NO_ARITH_CHECKS
    if (b == (T)0)
        dn2cpp_throw_divide_by_zero();
#endif
    return (T)(a / b);
}

template <typename T>
inline T dn2cpp_rem_unsigned(T a, T b)
{
#ifndef DN2CPP_NO_ARITH_CHECKS
    if (b == (T)0)
        dn2cpp_throw_divide_by_zero();
#endif
    return (T)(a % b);
}

inline int32_t dn2cpp_ldelem_i4(Dn2CppArrayI4* arr, int32_t index)
{
    dn2cpp_bounds_check(arr, index);
    return arr->data[index];
}

inline void dn2cpp_stelem_i4(Dn2CppArrayI4* arr, int32_t index, int32_t value)
{
    dn2cpp_bounds_check(arr, index);
    arr->data[index] = value;
}

inline Dn2CppObject* dn2cpp_ldelem_ref(Dn2CppArrayRef* arr, int32_t index)
{
    dn2cpp_bounds_check(arr, index);
    return arr->data[index];
}

inline void dn2cpp_stelem_ref(Dn2CppArrayRef* arr, int32_t index, Dn2CppObject* value)
{
    dn2cpp_bounds_check(arr, index);
    dn2cpp_gc_store_ref(&arr->data[index], value);
}

inline void* dn2cpp_elem_addr(Dn2CppArrayN* arr, int32_t index)
{
    dn2cpp_bounds_check(arr, index);
    return arr->data + static_cast<size_t>(index) * arr->elemSize;
}

// Whether a type-info describes an SZArray whose elements are REFERENCES, i.e. an
// object of that type has the Dn2CppArrayRef layout (`length` then `Dn2CppObject*
// data[]`) rather than Dn2CppArrayI4's or Dn2CppArrayN's. A reference element is any
// non-value-type; of the null-elementType handles only dn2cpp_array_ref_type is ref —
// the imprecise packed dn2cpp_array_n_type answers false, since its layout is
// Dn2CppArrayN. Shared because two callers must agree and a disagreement is a wrong
// CAST, not a wrong answer: dn2cpp_pinned_data_addr picks the data offset with it, and
// the BPI interpreter's argument-type guard refuses a `String.Concat(string[])` import
// whose operand is not one — reading a string's `chars` field as `data[0]` is exactly
// what that guard exists to stop.
inline bool dn2cpp_is_ref_array(const Dn2CppTypeInfo* t)
{
    if (t == nullptr || (t->flags & DN2CPP_TF_ARRAY) == 0)
        return false;
    return t->elementType == nullptr ? (t == &dn2cpp_array_ref_type)
                                     : (t->elementType->flags & DN2CPP_TF_VALUETYPE) == 0;
}

// Whether a value element packs into Dn2CppArrayI4 rather than Dn2CppArrayN — int32,
// uint32, or an enum backed by either. A null element (the imprecise packed handle)
// is not one: Dn2CppArrayN is what dn2cpp_newarr_n allocated. The two reps put their
// data at different offsets, so every layout reader here asks this one question.
inline bool dn2cpp_array_is_i4_elem(const Dn2CppTypeInfo* el)
{
    return el == &dn2cpp_int32_type || el == &dn2cpp_uint32_type
        || (el != nullptr && (el->flags & DN2CPP_TF_ENUM) != 0
            && (el->enumUnderlying == &dn2cpp_int32_type
                || el->enumUnderlying == &dn2cpp_uint32_type));
}

// The data address a pinned object exposes via GCHandle.AddrOfPinnedObject when the
// pinned value's static C++ rep is NOT known at the Alloc site (it arrives typed as
// `object`/an interface, e.g. SRM's ByteArrayMemoryProvider pins a byte[] held in an
// `object` field) — so the rep must be discovered from the runtime type-info instead
// of assumed to be `header + 1` (that assumption read an array's length field as
// data and broke PEReader). Mirrors MethodCompiler.GCHandleDataAddr / CppTypes.RepOf:
// a string -> its chars, an SZArray -> &element[0] at the rep's real data offset
// (ref/i4/packed), an MD array -> its detached payload block, any other object -> its
// body past the type header. The heap is non-moving, so the address is stable for the
// handle's lifetime.
inline void* dn2cpp_pinned_data_addr(Dn2CppObject* o)
{
    if (o == nullptr)
        return nullptr;
    const Dn2CppTypeInfo* t = o->type;
    if (t == &dn2cpp_string_type)
        return const_cast<char16_t*>(static_cast<Dn2CppString*>(o)->chars);
    if ((t->flags & DN2CPP_TF_ARRAY) != 0)
    {
        // Rank decides before the element does: an MD array keeps its elements in a
        // separate block and shares no header field with the SZ reps, so reading one
        // as an SZArray answers a pointer into the MD header. .NET answers element 0
        // at every rank and lower bound.
        if (t->arrayRank > 1)
            return static_cast<Dn2CppMDArray*>(o)->data;
        if (dn2cpp_is_ref_array(t))
            return &static_cast<Dn2CppArrayRef*>(o)->data[0];
        if (dn2cpp_array_is_i4_elem(t->elementType))
            return &static_cast<Dn2CppArrayI4*>(o)->data[0];
        return &static_cast<Dn2CppArrayN*>(o)->data[0];
    }
    return o + 1;
}

// Total element count of a multi-dimensional array (Array.Length / LongLength) —
// the product of the per-dimension lengths.
inline int32_t dn2cpp_md_total_length(Dn2CppMDArray* arr)
{
    if (arr == nullptr)
        dn2cpp_throw_null_reference();
    int32_t total = 1;
    for (int32_t i = 0; i < arr->rank; i++)
        total *= arr->lengths[i];
    return total;
}

// ── The SHAPE questions the block-move lowerings ask of an operand whose static
//    C++ type does not answer them ─────────────────────────────────────────────
//
// An MD array carries no `length` header at all — the field at that offset is
// its rank — and its elements live in a separate data block, so an operand read
// as an SZArray validates a six-element array against 2 and then memmoves over
// the other layout's header. Both faults are silent.

// arrayRank is 1 for an emitted ti_arr_<T> and 0 for the shared reference-element
// fallback header; both mean the SZ layout, so only a value above them is MD.
inline int32_t dn2cpp_array_rank_of(Dn2CppObject* o)
{
    int32_t r = o->type != nullptr ? o->type->arrayRank : 1;
    return r > 1 ? r : 1;
}

// The element WIDTH in bytes of an array whose static C++ type does not state one
// (RuntimeHelpers.GetElementSize, reached from Marshal.UnsafeAddrOfPinnedArrayElement).
// Answered from the LAYOUT, never from the element handle, so an imprecise runtime
// handle answers exactly: Dn2CppArrayN and Dn2CppMDArray carry the width as a
// field, and the other two reps have one width each. .NET reports the pointer width for
// a reference element, which is what Dn2CppArrayRef stores.
inline int32_t dn2cpp_array_element_size(Dn2CppObject* o)
{
    if (o == nullptr)
        dn2cpp_throw_null_reference();
    if (dn2cpp_array_rank_of(o) > 1)
        return static_cast<Dn2CppMDArray*>(o)->elemSize;
    const Dn2CppTypeInfo* t = o->type;
    if (dn2cpp_is_ref_array(t))
        return static_cast<int32_t>(sizeof(Dn2CppObject*));
    if (dn2cpp_array_is_i4_elem(t != nullptr ? t->elementType : nullptr))
        return static_cast<int32_t>(sizeof(int32_t));
    return static_cast<Dn2CppArrayN*>(o)->elemSize;
}

inline int32_t dn2cpp_array_total_length(Dn2CppObject* o)
{
    return dn2cpp_array_rank_of(o) > 1
        ? dn2cpp_md_total_length(reinterpret_cast<Dn2CppMDArray*>(o))
        : static_cast<Dn2CppArray*>(o)->length;
}

// Array.Copy's destination, whose source the emitter proved SZ statically: .NET
// refuses a rank mismatch outright, between the null checks and the range one.
template <typename TArray>
inline TArray* dn2cpp_array_require_copy_dest(TArray* arr)
{
    if (arr == nullptr)
        dn2cpp_throw_argument_null();
    if (dn2cpp_array_rank_of(reinterpret_cast<Dn2CppObject*>(arr)) > 1)
        dn2cpp_throw_rank();
    return arr;
}

// Array.CopyTo's destination is the SAME refusal in a different family, because
// CopyTo tests it itself instead of leaving it to the Copy underneath — so a
// rank>=2 destination is an ArgumentException here while a rank>=2 RECEIVER
// still falls through to Copy's rank match. Both measured against real .NET.
template <typename TArray>
inline TArray* dn2cpp_array_require_copyto_dest(TArray* arr)
{
    if (arr == nullptr)
        dn2cpp_throw_argument_null();
    if (dn2cpp_array_rank_of(reinterpret_cast<Dn2CppObject*>(arr)) > 1)
        dn2cpp_throw_argument();
    return arr;
}

// Array.Resize's own newSize check, ahead of the allocation: .NET rejects a
// negative size with ArgumentOutOfRangeException, while the newarr the resize
// lowers to answers OverflowException — a different family, and it is the one a
// caller's catch names.
inline int32_t dn2cpp_array_resize_size(int32_t newSize)
{
    if (newSize < 0)
        dn2cpp_throw_argument_out_of_range();
    return newSize;
}

// ── Buffer's byte-granular surface, whose bounds are BYTES and whose operand must
//    be primitive (BlockCopy, ByteLength) ────────────────────────────────────────
//
// Both move or measure the whole element payload, so their bounds are the array's
// byte extent — a number no operand's static C++ type states: the SZ reps put their
// data at different offsets, only Dn2CppArrayN carries an elemSize, and an MD array's
// extent is a product of its lengths. The emit site passes its verdict in as a
// constant so the switches fold, and the refusals are answered here in real .NET's own
// order (measured): null src, null dst, non-primitive src, non-primitive dst, negative
// operand, overrun. The primitive one is not a formality — a struct[] or object[] blit
// .NET refuses moves bytes across GC-visible fields, so it would leave torn references
// behind.
//
// The element verdict is the EMIT site's, not the header's. NONPRIM is every element
// the emitter proved unblittable, references included; its layout is never read,
// because the refusal precedes every extent. DYN is the one operand nothing was proved
// about — a System.Array-typed one, or a shared body's T[] — and it stays fail-CLOSED
// on an element-UNKNOWN handle: dn2cpp_array_n_type states the packed layout but not
// the element kind, and a blit over a struct[] .NET refuses would move bytes across
// GC-visible fields. A precise runtime ti answers exactly.
enum Dn2CppBlockCopyRep : int32_t
{
    DN2CPP_BCREP_I4 = 0,
    DN2CPP_BCREP_N = 1,
    DN2CPP_BCREP_MD = 2,
    DN2CPP_BCREP_NONPRIM = 3,
    DN2CPP_BCREP_DYN = 4,
};

// DN2CPP_BCREP_DYN resolved off the runtime type-info — the dn2cpp_array_rep_dyn /
// CppTypes.RepOf discrimination, with the element-kind test folded in so one answer
// carries both. An enum counts as primitive, matching .NET, which asks the element's
// CorElementType rather than Type.IsPrimitive.
inline int32_t dn2cpp_blockcopy_rep_dyn(Dn2CppObject* o)
{
    const Dn2CppTypeInfo* t = o->type;
    const Dn2CppTypeInfo* el = t != nullptr && (t->flags & DN2CPP_TF_ARRAY) != 0 ? t->elementType : nullptr;
    if (el == nullptr || (el->flags & (DN2CPP_TF_PRIMITIVE | DN2CPP_TF_ENUM)) == 0)
        return DN2CPP_BCREP_NONPRIM;
    if (t->arrayRank > 1)
        return DN2CPP_BCREP_MD;
    return dn2cpp_array_is_i4_elem(el) ? DN2CPP_BCREP_I4 : DN2CPP_BCREP_N;
}

inline int32_t dn2cpp_blockcopy_rep(Dn2CppObject* o, int32_t rep)
{
    return rep == DN2CPP_BCREP_DYN ? dn2cpp_blockcopy_rep_dyn(o) : rep;
}

// Both take a RESOLVED rep, and neither has a NONPRIM arm: that verdict is answered
// before either is reached.
inline int64_t dn2cpp_blockcopy_byte_length(Dn2CppObject* o, int32_t rep)
{
    switch (rep)
    {
        case DN2CPP_BCREP_I4:
            return static_cast<int64_t>(static_cast<Dn2CppArray*>(o)->length) * 4;
        case DN2CPP_BCREP_MD:
        {
            Dn2CppMDArray* md = static_cast<Dn2CppMDArray*>(o);
            return static_cast<int64_t>(dn2cpp_md_total_length(md)) * md->elemSize;
        }
        default:
        {
            Dn2CppArrayN* a = static_cast<Dn2CppArrayN*>(o);
            return static_cast<int64_t>(a->length) * a->elemSize;
        }
    }
}

inline char* dn2cpp_blockcopy_base(Dn2CppObject* o, int32_t rep)
{
    switch (rep)
    {
        case DN2CPP_BCREP_I4:
            return reinterpret_cast<char*>(static_cast<Dn2CppArrayI4*>(o)->data);
        case DN2CPP_BCREP_MD:
            return static_cast<Dn2CppMDArray*>(o)->data;
        default:
            return static_cast<Dn2CppArrayN*>(o)->data;
    }
}

inline void dn2cpp_buffer_blockcopy(Dn2CppObject* src, int32_t srcRep, int32_t srcOffset,
                                    Dn2CppObject* dst, int32_t dstRep, int32_t dstOffset,
                                    int32_t count)
{
    if (src == nullptr || dst == nullptr)
        dn2cpp_throw_argument_null();
    srcRep = dn2cpp_blockcopy_rep(src, srcRep);
    dstRep = dn2cpp_blockcopy_rep(dst, dstRep);
    if (srcRep == DN2CPP_BCREP_NONPRIM || dstRep == DN2CPP_BCREP_NONPRIM)
        dn2cpp_throw_argument();
    if ((srcOffset | dstOffset | count) < 0)
        dn2cpp_throw_argument_out_of_range();
    // 64-bit, because `offset + count` in int32 wraps back under the extent.
    if (static_cast<int64_t>(srcOffset) + count > dn2cpp_blockcopy_byte_length(src, srcRep)
        || static_cast<int64_t>(dstOffset) + count > dn2cpp_blockcopy_byte_length(dst, dstRep))
        dn2cpp_throw_argument();
    std::memmove(dn2cpp_blockcopy_base(dst, dstRep) + dstOffset,
                 dn2cpp_blockcopy_base(src, srcRep) + srcOffset,
                 static_cast<size_t>(count));
}

// Buffer.ByteLength — the extent BlockCopy bounds itself by, behind the same two
// refusals. .NET narrows it CHECKED, so an array whose byte extent overruns int32
// answers OverflowException rather than a truncated length.
inline int32_t dn2cpp_buffer_bytelength(Dn2CppObject* o, int32_t rep)
{
    if (o == nullptr)
        dn2cpp_throw_argument_null();
    rep = dn2cpp_blockcopy_rep(o, rep);
    if (rep == DN2CPP_BCREP_NONPRIM)
        dn2cpp_throw_argument();
    int64_t n = dn2cpp_blockcopy_byte_length(o, rep);
    if (n > static_cast<int64_t>(0x7fffffff))
        dn2cpp_throw_of(&dn2cpp_overflow_exception_type);
    return static_cast<int32_t>(n);
}

// Buffer.GetByte / SetByte — one byte of that same extent. .NET tests the index against
// ByteLength(array) itself, so the null and non-primitive refusals AND its checked
// narrowing all precede the index test, and a negative index arrives as a huge unsigned
// (measured). Going through dn2cpp_buffer_bytelength is what keeps that order one fact
// rather than a copy of it.
inline char* dn2cpp_buffer_byte_addr(Dn2CppObject* o, int32_t rep, int32_t index)
{
    if (static_cast<uint32_t>(index) >= static_cast<uint32_t>(dn2cpp_buffer_bytelength(o, rep)))
        dn2cpp_throw_argument_out_of_range();
    return dn2cpp_blockcopy_base(o, dn2cpp_blockcopy_rep(o, rep)) + index;
}

inline uint8_t dn2cpp_buffer_getbyte(Dn2CppObject* o, int32_t rep, int32_t index)
{
    return static_cast<uint8_t>(*dn2cpp_buffer_byte_addr(o, rep, index));
}

inline void dn2cpp_buffer_setbyte(Dn2CppObject* o, int32_t rep, int32_t index, uint8_t value)
{
    *dn2cpp_buffer_byte_addr(o, rep, index) = static_cast<char>(value);
}

// The rank-2/3/N counterparts of dn2cpp_bounds_check: every `md[i,j]` access an
// emitted body performs goes through dn2cpp_md_elem_addr{2,3,} into one of these.
// Real .NET answers an out-of-range index on ANY dimension with the same
// IndexOutOfRangeException it raises for an SZArray (measured), so these throw the
// same catchable fault rather than aborting.
inline int32_t dn2cpp_md_flat_index2(Dn2CppMDArray* arr, int32_t i0, int32_t i1)
{
    if (arr == nullptr)
        dn2cpp_throw_null_reference();
    if (i0 < 0 || i0 >= arr->lengths[0] || i1 < 0 || i1 >= arr->lengths[1])
        dn2cpp_throw_index_out_of_range();
    return i0 * arr->lengths[1] + i1;
}

inline int32_t dn2cpp_md_flat_index3(Dn2CppMDArray* arr, int32_t i0, int32_t i1, int32_t i2)
{
    if (arr == nullptr)
        dn2cpp_throw_null_reference();
    if (i0 < 0 || i0 >= arr->lengths[0] || i1 < 0 || i1 >= arr->lengths[1] || i2 < 0 || i2 >= arr->lengths[2])
        dn2cpp_throw_index_out_of_range();
    return (i0 * arr->lengths[1] + i1) * arr->lengths[2] + i2;
}

inline int32_t dn2cpp_md_flat_index(Dn2CppMDArray* arr, const int32_t* indices)
{
    if (arr == nullptr)
        dn2cpp_throw_null_reference();
    int32_t flat = 0;
    for (int32_t i = 0; i < arr->rank; i++)
    {
        int32_t idx = indices[i];
        if (idx < 0 || idx >= arr->lengths[i])
            dn2cpp_throw_index_out_of_range();
        flat = flat * arr->lengths[i] + idx;
    }
    return flat;
}

inline void* dn2cpp_md_elem_addr2(Dn2CppMDArray* arr, int32_t i0, int32_t i1)
{
    int32_t flat = dn2cpp_md_flat_index2(arr, i0, i1);
    return arr->data + static_cast<size_t>(flat) * arr->elemSize;
}

inline void* dn2cpp_md_elem_addr3(Dn2CppMDArray* arr, int32_t i0, int32_t i1, int32_t i2)
{
    int32_t flat = dn2cpp_md_flat_index3(arr, i0, i1, i2);
    return arr->data + static_cast<size_t>(flat) * arr->elemSize;
}

inline void* dn2cpp_md_elem_addr(Dn2CppMDArray* arr, const int32_t* indices)
{
    int32_t flat = dn2cpp_md_flat_index(arr, indices);
    return arr->data + static_cast<size_t>(flat) * arr->elemSize;
}

// ---- async/await ----
//
// Task / Task<T> are reference types (heap objects). A Task<T>'s result is held
// in a raw slot reinterpreted by the result type at the SetResult/get_Result
// intrinsics. AsyncTaskMethodBuilder<T> and TaskAwaiter/TaskAwaiter<T> are value
// types (embedded by value inside the compiler-generated state-machine struct),
// each just wrapping the Task they operate on.
//
// When a state machine awaits a not-yet-complete task, MoveNext saves its state
// and calls AwaitUnsafeOnCompleted, which (1) boxes the struct state machine to
// the heap the first time so it
// survives the returning frame and (2) registers the boxed MoveNext as a
// continuation on the awaited task. Completing a task posts its continuations to
// a single-threaded cooperative run queue (the "scheduler"); a blocking
// Task.Wait/.Result at the top level drains that queue until the task completes.
// Task.Delay produces a pending task the scheduler completes on its next turn
// (duration is not modeled — there is no wall clock).
enum
{
    DN2CPP_TASK_PENDING = 0,
    DN2CPP_TASK_SUCCEEDED = 1,
    DN2CPP_TASK_FAULTED = 2,
    DN2CPP_TASK_CANCELED = 3,   // canceled via a CancellationToken
};

// The per-thread cooperative scheduler (run queue + virtual clock). Defined in
// dn2cpp_tasks.cpp; opaque here so generated TUs never pull in <thread>/<mutex>.
struct Dn2CppScheduler;

// A queued resumption: re-enter a boxed state machine's MoveNext (fn) with the
// boxed state machine pointer (state). Used both for the scheduler run queue and
// for the per-task list of continuations waiting on a task's completion.
// `owner` is the scheduler the resumption must run on — the thread that registered
// the await. A worker thread that completes the task posts the continuation back to
// `owner` (cross-thread) instead of its own queue, so the state machine resumes on
// the awaiting thread. For pure single-thread async, owner is always the caller's
// own scheduler and the cross-thread path is never taken.
struct Dn2CppCont
{
    void (*fn)(void*);
    void* state;
    Dn2CppCont* next;
    Dn2CppScheduler* owner;
    // While queued on a scheduler the node is also linked into a global
    // static-rooted list: the scheduler itself lives in thread-local storage,
    // which the conservative collector does not scan on every platform, so a
    // queued resumption (and the boxed state machine it points at) must stay
    // reachable through scanned memory until it runs.
    Dn2CppCont* gcprev;
    Dn2CppCont* gcnext;
};

// The not-yet-started work of a cold task (`new Task(...)`): the delegate, its
// optional state argument, and the result-kind invoke thunk chosen at the newobj
// site (exactly one of invoke/invoke2 is set). Claimed — nulled off the task —
// exactly once by Start()/RunSynchronously(); GC-scanned through the task object.
struct Dn2CppTaskCold
{
    Dn2CppObject* del;
    Dn2CppObject* state;
    uint64_t (*invoke)(Dn2CppObject*);
    uint64_t (*invoke2)(Dn2CppObject*, Dn2CppObject*);
};

struct Dn2CppTask : Dn2CppObject
{
    // The publication point: a settle stores it last with release, so every read —
    // all of them a plain `->status`, hence a seq_cst load, generated code included
    // — acquires `result` and `exception` with it. Atomic in the struct, because a
    // drain-only accessor would leave the generated and intrinsic readers racing.
    std::atomic<int32_t> status; // DN2CPP_TASK_*
    int32_t id;                // Task.Id — positive, minted at alloc (fills the pad after status)
    Dn2CppObject* exception;   // managed exception object when faulted
    // Task.Exception cache: the AggregateException wrapper, minted on first
    // get_Exception read (identity-stable). Never stored into `exception` —
    // await/Wait re-raise the bare fault. Atomic because that mint happens AFTER the
    // settle, so unlike every other field here it rides no publication edge of its
    // own; readers hold no lock (they allocate, which must not run under one).
    std::atomic<Dn2CppObject*> exceptionAggregate;
    uint64_t result;           // Task<T> result, reinterpreted by T (0 for Task)
    Dn2CppCont* continuations; // resumptions waiting on this task (fired on complete)
    Dn2CppObject* workerKeepAlive; // Task.Run: keeps the worker delegate GC-reachable
    // `new Task(...)` work not yet claimed by Start (else null). Atomic for the
    // null-ness test generated Task.Status makes without the claim's lock; nothing
    // dereferences it unlocked, so there is nothing to publish through it.
    std::atomic<Dn2CppTaskCold*> cold;
    // The IValueTaskSource bridge this task fronts, or null for every other task.
    // Written once before the task is published and never cleared: it is what lets a
    // synchronous read tell a source-backed ValueTask from a Task-backed one, which the
    // CLR distinguishes and dn2cpp otherwise could not. Appended last — generated code
    // reads the fields above by offset.
    Dn2CppObject* vtsBridge;
};

struct Dn2CppTaskCompletionSource : Dn2CppObject
{
    Dn2CppTask* task;
};

// Generated code reads Dn2CppTask by offset: an atomic field that is not shaped like
// the scalar it replaces moves every field after it, and a lock-backed one puts a
// mutex inside the struct. Pointer-shaped is also what keeps the conservative
// collector able to see a referent through the slot.
static_assert(sizeof(std::atomic<int32_t>) == sizeof(int32_t)
                  && alignof(std::atomic<int32_t>) == alignof(int32_t)
                  && std::atomic<int32_t>::is_always_lock_free
                  && sizeof(std::atomic<void*>) == sizeof(void*)
                  && alignof(std::atomic<void*>) == alignof(void*)
                  && std::atomic<void*>::is_always_lock_free,
              "Dn2CppTask's atomic fields must keep the ABI of the plain scalars");

// AsyncTaskMethodBuilder / AsyncTaskMethodBuilder<T> — value type. `boxed` is the
// heap copy of the owning state machine once it has suspended (null until then);
// it doubles as the "already boxed" flag so later awaits in the same method reuse
// the same heap object instead of re-copying.
struct Dn2CppAsyncBuilder
{
    Dn2CppTask* task;
    void* boxed;
};

// TaskAwaiter / TaskAwaiter<T> — value type.
struct Dn2CppTaskAwaiter
{
    Dn2CppTask* task;
};

// default(ValueTask) / default(ValueTask<T>) carries a NULL task pointer — the
// BCL encodes "completed successfully with default(T)" as `_obj == null` (e.g.
// MemoryStream.WriteAsync(ReadOnlyMemory<byte>) returns `default` after
// completing its write synchronously). dn2cpp_vtask normalizes that for the
// ValueTask read paths (IsCompleted / GetResult / Result / AsTask): null maps
// to a shared immutable pre-completed sentinel (SUCCEEDED, result 0). The
// sentinel is only ever read — every reader sees a non-PENDING status, so the
// suspend/continuation paths (which mutate a task) are unreachable for it.
extern Dn2CppTask dn2cpp_task_default_completed;
static inline Dn2CppTask* dn2cpp_vtask(Dn2CppTask* t)
{
    return t != nullptr ? t : &dn2cpp_task_default_completed;
}

// YieldAwaitable / YieldAwaitable.YieldAwaiter (Task.Yield) — both lower to this
// stateless value type. The awaiter never reports completed (IsCompleted is
// always false), so awaiting it always takes the suspend path; the suspension
// posts the resumption straight onto the cooperative run queue, yielding the
// current turn to other ready continuations.
struct Dn2CppYieldAwaiter { };

// System.Threading.Lock.Scope (.NET 9+). The ref struct EnterScope() returns and
// the `lock(lockVar)` body's finally disposes. It carries the Lock object so Dispose
// can release the real per-object mutex acquired by EnterScope.
struct Dn2CppLockScope { Dn2CppObject* lock; };

// StringBuilder.ChunkEnumerator (sb.GetChunks()): the runtime StringBuilder is
// one contiguous buffer, so enumeration yields exactly one chunk — a snapshot
// string taken at GetChunks time. `state` 0 = before the chunk, 1 = done.
struct Dn2CppSbChunkEnum { Dn2CppString* snapshot; int32_t state; };

extern const Dn2CppTypeInfo dn2cpp_task_type;

// A fresh pending task (status PENDING). The builder completes it via SetResult/
// SetException once MoveNext finishes.
Dn2CppTask* dn2cpp_task_alloc();
// The shared already-completed (void-result) task behind Task.CompletedTask.
Dn2CppTask* dn2cpp_task_completed();
// A completed Task<T> carrying a result (behind Task.FromResult). The raw slot
// is reinterpreted by T at the get_Result/GetResult intrinsics.
Dn2CppTask* dn2cpp_task_from_result(uint64_t result);
// Stamp the closed managed Task identity at the producer mouth that knows it. A
// default(ValueTask)'s shared sentinel is cloned before stamping.
Dn2CppTask* dn2cpp_task_stamp(Dn2CppTask* task, const Dn2CppTypeInfo* type);
Dn2CppTask* dn2cpp_vtask_as_task(Dn2CppTask* task, const Dn2CppTypeInfo* type);
Dn2CppTaskCompletionSource* dn2cpp_tcs_alloc(const Dn2CppTypeInfo* type,
                                              const Dn2CppTypeInfo* taskType);
// TaskAwaiter.GetResult: re-raise the stored exception if the task faulted,
// otherwise a no-op. (Task<T>.GetResult adds the typed result load inline.)
void dn2cpp_task_throw_if_faulted(Dn2CppTask* t);
// Task.ThrowAsync(Exception, SynchronizationContext): re-raise an async void fault as an
// unhandled exception on the worker pool (crashing the process, as .NET does). The
// SynchronizationContext is not honored.
void dn2cpp_task_throw_async(Dn2CppObject* exc, Dn2CppObject* syncCtx);
// TaskScheduler.FromCurrentSynchronizationContext: always throws — .NET's own
// InvalidOperationException with no installed context, NotSupportedException with
// one (a hint-only TaskScheduler cannot honor a context-wrapping scheduler).
[[noreturn]] Dn2CppObject* dn2cpp_taskscheduler_from_sync_ctx();
// Task.get_Exception: non-null for FAULTED only — the fault wrapped in an
// AggregateException, minted on first access and cached (identity-stable across
// reads). CANCELED answers null, matching real .NET. The bare `->exception` is
// left untouched: awaiting/blocking re-raises it directly.
Dn2CppObject* dn2cpp_task_exception(Dn2CppTask* t);

// Complete a task and post its continuations to the scheduler (SetResult /
// SetException intrinsics route here so awaiters resume).
void dn2cpp_task_set_result(Dn2CppTask* t, uint64_t result);
void dn2cpp_task_set_exception(Dn2CppTask* t, Dn2CppObject* exception);
// Async Task/ValueTask builders and source-backed bridges classify
// OperationCanceledException (and derived exceptions) as cancellation while
// preserving the thrown object for await.
void dn2cpp_task_set_exception_or_canceled(Dn2CppTask* t, Dn2CppObject* exception);
// Register a resumption to run when `t` completes (the suspension path). If `t`
// is already complete the resumption is posted immediately.
void dn2cpp_task_on_completed(Dn2CppTask* t, void (*fn)(void*), void* state);
// Invoke a no-arg System.Action delegate (and its multicast chain) generically, via
// the uniform delegate layout.
void dn2cpp_action_invoke(Dn2CppObject* action);
void dn2cpp_paramthread_invoke(Dn2CppObject* action, Dn2CppObject* state);
// Register a System.Action to run when `t` completes — backs a user awaiter's
// TaskAwaiter.OnCompleted(Action) and the custom-awaitable await continuation.
void dn2cpp_task_on_completed_action(Dn2CppTask* t, Dn2CppObject* action);
// Task.Delay(ms): a task that completes after `ms` of virtual time. There is no
// wall clock — a logical clock advances only when no work is runnable, so concurrent
// delays complete in duration order, deterministically and without real sleeping.
// ms <= 0 completes immediately (like Task.Yield).
Dn2CppTask* dn2cpp_task_delay(int64_t ms);
// Task.WhenAll<TResult>(Task<TResult>[]): a task whose result is a TResult[] of
// each input task's result, completing once every input completes (or faulting
// with the first input's fault). `kind` selects how each input's raw result slot
// is written into the result array and which array struct is allocated:
// 0 = int[] (Dn2CppArrayI4), 1 = 8-byte element[] (long/double, Dn2CppArrayN),
// 2 = reference[] (Dn2CppArrayRef), 3 = void (non-generic Task.WhenAll — completes
// with no result array, only the first input fault). The cooperative join is
// single-threaded: each input posts a continuation that, on the last completion,
// builds the result array.
enum { DN2CPP_WHENALL_I4 = 0, DN2CPP_WHENALL_N8 = 1, DN2CPP_WHENALL_REF = 2,
       DN2CPP_WHENALL_VOID = 3, DN2CPP_WHENALL_STRUCT = 4 };
// `arrTi` is the TResult[] handle the emit arm supplies; it rides on the join state
// because the array is built in the completion callback, where no call site is left
// to retag. Null (the DN2CPP_WHENALL_VOID arm, which builds no array) degrades
// to the shared handle.
Dn2CppTask* dn2cpp_task_when_all(Dn2CppArrayRef* tasks, int32_t kind, const Dn2CppTypeInfo* arrTi);
// Task.WhenAll<TStruct>(Task<TStruct>[]) -> Task<TStruct[]>: copies each input's
// heap-boxed struct result into a value array of elemSize-byte elements.
Dn2CppTask* dn2cpp_task_when_all_struct(Dn2CppArrayRef* tasks, int32_t elemSize,
                                        const Dn2CppTypeInfo* arrTi);
// Task.WhenAny(Task[]) / WhenAny<T>(Task<T>[]): a Task<Task>/Task<Task<T>> whose
// result is the first input task to complete (always succeeds — the winner's own
// fault surfaces through its result, not WhenAny's). Empty list faults.
Dn2CppTask* dn2cpp_task_when_any(Dn2CppArrayRef* tasks);
// A growable Dn2CppObject* buffer that materializes an IEnumerable<Task<T>> (a
// non-array source: List<Task>, a LINQ result, …) into a Dn2CppArrayRef* for the
// WhenAll/WhenAny combinators. The emitted interface-enumeration loop appends each
// task, then converts to a ref array. GC-safe: `data` is a scanned dn2cpp_alloc
// buffer; growth allocates a new one and copies.
struct Dn2CppRefList { Dn2CppObject** data; int32_t count; int32_t cap; };
Dn2CppRefList* dn2cpp_reflist_new();
void dn2cpp_reflist_add(Dn2CppRefList* l, Dn2CppObject* o);
Dn2CppArrayRef* dn2cpp_reflist_to_array(Dn2CppRefList* l);
// Copy a ReadOnlySpan<Task<T>>'s {reference,length} into a fresh ref array — the
// .NET 9+ `params ReadOnlySpan<Task>` combinator overload (3+ loose tasks).
Dn2CppArrayRef* dn2cpp_refspan_to_array(Dn2CppObject** data, int32_t len);
// Heap-box a struct Task<T> result so it fits the 8-byte slot as a pointer.
void* dn2cpp_struct_result_box(const void* src, int32_t size);
// Task.FromException<T>(ex) / Task.FromException(ex): a pre-completed FAULTED task
// carrying the exception; awaiting it or reading .Result re-raises it.
Dn2CppTask* dn2cpp_task_from_exception(Dn2CppObject* exception);
// Task.FromCanceled(<T>) / ValueTask.FromCanceled(<T>): a pre-completed CANCELED
// task carrying an OperationCanceledException; awaiting it or reading .Result
// re-raises it (same path as an awaited canceled Task.Delay).
Dn2CppTask* dn2cpp_task_from_canceled();
// TaskCompletionSource(<T>) owns a separate Dn2CppTask* it completes. Exactly-once
// transitions: TrySet* returns 1 on the winning transition and 0 when the task
// had already settled (thread-safe — concurrent TrySet* calls see one winner);
// the Set* forms throw InvalidOperationException instead of returning 0.
int32_t dn2cpp_task_try_set_result(Dn2CppTask* t, uint64_t result);
int32_t dn2cpp_task_try_set_exception(Dn2CppTask* t, Dn2CppObject* exception);
int32_t dn2cpp_task_try_set_canceled(Dn2CppTask* t);
void dn2cpp_tcs_set_result(Dn2CppTask* t, uint64_t result);
void dn2cpp_tcs_set_exception(Dn2CppTask* t, Dn2CppObject* exception);
void dn2cpp_tcs_set_canceled(Dn2CppTask* t);
// Enqueue a resumption on the cooperative run queue.
void dn2cpp_sched_post(void (*fn)(void*), void* state);
// Action form of dn2cpp_sched_post: enqueue a System.Action (and its multicast
// chain) on this thread's run queue. Backs YieldAwaiter.OnCompleted(Action).
void dn2cpp_sched_post_action(Dn2CppObject* action);
// Per-thread SynchronizationContext.Current: read / install the calling thread's
// context (a GC-visible pinned slot; null until something installs one — the
// real .NET default). Backs SynchronizationContext.Current /
// SetSynchronizationContext; a host lane installing its own context (e.g. the
// Godot main-thread context) goes through the same slot.
Dn2CppObject* dn2cpp_sync_ctx_get();
void dn2cpp_sync_ctx_set(Dn2CppObject* ctx);
// Non-blocking frame pump for THIS thread's scheduler: complete the Task.Delay
// timers whose due falls within the real time elapsed since the previous pump,
// then run the continuations queued at entry (one stolen snapshot — work posted
// during the pump waits for the next call, so a self-reposting continuation
// cannot spin a frame forever). Never blocks and never sleeps; a cheap no-op
// when nothing is pending. Backs a host's per-frame main-thread callback
// (Godot), where nothing ever reaches the blocking drain, so cross-thread
// completions (await Task.Run) would otherwise sit on the queue unrun.
void dn2cpp_sched_pump();
// Drain the run queue until `t` completes, then re-raise a stored fault. Returns
// `t`. Backs the blocking Task.Wait()/.Result at the top of an async program.
// A wait no principal can ever satisfy (nothing runnable here, no pool work in
// flight, no other live user thread) reports on stderr and then throws a catchable
// InvalidOperationException carrying the diagnosis — a deliberate divergence from
// real .NET, which hangs; the invariant is written out at the throw site in
// dn2cpp_tasks.cpp, and the stderr line is there because a caught exception would
// otherwise leave the defeat with no trace at all.
Dn2CppTask* dn2cpp_task_block(Dn2CppTask* t);
// The ValueTask sync-read funnel. Blocking is right for a Task- or builder-backed
// ValueTask, which is all dn2cpp_task_block sees; a still-pending SOURCE-backed one is
// not a task at all to the CLR, which reads the source's GetResult on the spot and lets
// it refuse. So route that case there instead of sleeping until somebody settles it —
// the exception is then the source's own, which is the only way its text can match.
Dn2CppTask* dn2cpp_vts_block(Dn2CppTask* t);
// ValueTask status reads use the source's GetStatus(token) when source-backed;
// Task-/builder-backed values use the task state, and default(ValueTask) succeeds.
int32_t dn2cpp_vtask_status(Dn2CppTask* t);
// The BLOCKING-WAIT flavor of the same drain: Task.Wait()/Wait(timeout)/
// Task<T>.Result wrap a fault or cancellation in an AggregateException, like real
// .NET — the wrap is the contract of the blocking wait, not of the task, which is
// why it is this one funnel. await / GetAwaiter().GetResult() / ValueTask stay on
// dn2cpp_task_block (unwrapped). The unsatisfiable-wait verdict is shared:
// the deadlock InvalidOperationException is dn2cpp's own diagnosis, never wrapped.
Dn2CppTask* dn2cpp_task_block_wait(Dn2CppTask* t);
// Task.WaitAll(Task[]): block until every input settles, then throw one
// AggregateException carrying every failure (a single fault is still wrapped, like
// real .NET's WaitAllCore) — so it does NOT re-raise as it goes.
void dn2cpp_task_wait_all(Dn2CppArrayRef* tasks);
// Task.WaitAny(Task[]) -> index of the first input to settle. Never raises the
// winner's fault: real .NET hands back the index and leaves the fault to be
// observed through that task.
int32_t dn2cpp_task_wait_any(Dn2CppArrayRef* tasks);

// ---- cancellation ----
// A CancellationTokenSource: a canceled flag, one LIFO list of registrations, and the
// CancelAfter timer's state. A registration is either a pending Task.Delay (Cancel()
// transitions it to the CANCELED state) or a callback (CancellationToken.Register(Action),
// run on Cancel()). A CancellationToken is the {source} struct (null = .None). All
// register/cancel/unregister/timer paths are serialized by an internal mutex, so any
// thread may Cancel()/Register()/CancelAfter() concurrently without a data race.
struct Dn2CppCancelReg;
struct Dn2CppCancelSource : Dn2CppObject
{
    int32_t canceled;
    int32_t disposed;          // Dispose() ran: any pending CancelAfter timer is dead
    int32_t timerLive;         // a CancelAfter timer thread is running for this source
    int64_t timerDueNs;        // its deadline (steady_clock ns since epoch); 0 = disarmed
    Dn2CppCancelReg* regs;     // LIFO list of delay-task + callback registrations
};
// A CancellationToken value: just a (possibly null) pointer to its source.
struct Dn2CppCancelToken
{
    Dn2CppCancelSource* source;
};
Dn2CppCancelSource* dn2cpp_cts_new();
// new CancellationTokenSource(delay): a fresh source already armed to cancel after `ms`
// (dn2cpp_cts_cancel_after's rules, including the negative-delay contract).
Dn2CppCancelSource* dn2cpp_cts_new_after(int64_t ms);
Dn2CppCancelSource* dn2cpp_cts_canceled();  // a pre-canceled source (new CancellationToken(true))
// CancellationTokenSource.CreateLinkedTokenSource: a fresh source that cancels when ANY of
// the given tokens does (and immediately, if one already has). Either token may be
// CancellationToken.None (a null source), which can never cancel and so links nothing. The
// [EnumeratorCancellation] plumbing behind an `await foreach` over an async iterator —
// File.ReadLinesAsync — links the token passed to ReadLinesAsync with the one the consumer
// hands GetAsyncEnumerator, which is what put these here.
Dn2CppCancelSource* dn2cpp_cts_link2(Dn2CppCancelSource* a, Dn2CppCancelSource* b);
Dn2CppCancelSource* dn2cpp_cts_link_array(Dn2CppArrayN* tokens);
void dn2cpp_cts_cancel(Dn2CppCancelSource* src);
// CancellationTokenSource.CancelAfter(delay): cancel `src` after `ms` milliseconds on the
// source's timer thread — at most one exists per source, so a second CancelAfter
// RESCHEDULES rather than arming a second cancel (real .NET's `_timer.Change`). ms == 0
// cancels now; ms == -1 (Timeout.Infinite) disarms; ms < -1 throws
// ArgumentOutOfRangeException; a null source is a no-op.
void dn2cpp_cts_cancel_after(Dn2CppCancelSource* src, int64_t ms);
// CancellationTokenSource.Dispose(): disarm the timer for good. The source itself is
// GC-managed and its `canceled` flag stays readable, so this releases no memory — what it
// releases is the pending cancel, matching real .NET, where a disposed source never fires.
void dn2cpp_cts_dispose(Dn2CppCancelSource* src);
int32_t dn2cpp_cts_is_cancelled(Dn2CppCancelSource* src);  // null source -> 0
// Register an Action callback to run when `src` is canceled (CancellationToken
// .Register). If `src` is already canceled, the callback runs immediately and
// synchronously on the calling thread; otherwise it is queued and run, in LIFO
// order, on the thread that calls Cancel(). Thread-safe. Returns an opaque
// registration handle (the node; null when run immediately or src is null) that
// dn2cpp_cts_unregister can later detach — this is the CancellationTokenRegistration.
Dn2CppCancelReg* dn2cpp_cts_register(Dn2CppCancelSource* src, Dn2CppObject* callback);
// The state-carrying sibling (CancellationToken.Register(Action<object>, object)): the
// callback is invoked with `state` on cancel. Same LIFO / already-canceled discipline.
Dn2CppCancelReg* dn2cpp_cts_register_state(Dn2CppCancelSource* src, Dn2CppObject* callback, Dn2CppObject* state);
// CancellationToken.Register/UnsafeRegister(Action<object, CancellationToken>, object): the
// node fires callback(state, <this source's token>). Both spellings land here — the only
// difference between Register and UnsafeRegister is ExecutionContext capture, and dn2cpp
// flows none — so the pair cannot drift apart.
Dn2CppCancelReg* dn2cpp_cts_register_state_token(Dn2CppCancelSource* src, Dn2CppObject* callback,
                                                 Dn2CppObject* state);
// Invoke an Action<object, CancellationToken> delegate chain with (state, token).
void dn2cpp_tokenthread_invoke(Dn2CppObject* del, Dn2CppObject* arg, Dn2CppCancelToken tok);
// Detach a still-pending callback registration (CancellationTokenRegistration
// .Dispose). A no-op if the registration already fired or was detached.
void dn2cpp_cts_unregister(Dn2CppCancelReg* reg);
int32_t dn2cpp_ctr_unregister(Dn2CppCancelReg* reg);
// The token associated with a registration remains observable after the callback fires
// or Dispose detaches it. A default registration returns CancellationToken.None.
Dn2CppCancelToken dn2cpp_ctr_token(Dn2CppCancelReg* reg);
// A Task.Delay(ms) bound to a token source: if the source cancels before the timer
// fires, the delay task transitions to CANCELED instead of completing.
Dn2CppTask* dn2cpp_task_delay_ct(int64_t ms, Dn2CppCancelSource* src);
// Register the real OperationCanceledException type-info (emitted in generated code)
// so a CANCELED task / ThrowIfCancellationRequested throws an object catchable by a
// typed `catch (OperationCanceledException)` — not just catch (Exception).
void dn2cpp_set_canceled_exception_type(const Dn2CppTypeInfo* ti);
// Register the real TaskCanceledException type-info (emitted in generated code): a
// CANCELED **task** then carries a TaskCanceledException, as in real .NET, while the
// token-side throw (ThrowIfCancellationRequested) keeps OperationCanceledException.
// Unregistered, canceled tasks fall back to the OCE identity above — a program whose
// CoreLib does not carry the type cannot observe the difference.
void dn2cpp_set_task_canceled_exception_type(const Dn2CppTypeInfo* ti);
// Throw an OperationCanceledException now (CancellationToken.ThrowIfCancellationRequested).
[[noreturn]] void dn2cpp_throw_canceled();

// Reinterpret a Task<T> result slot as a double and back (C++17, no
// std::bit_cast). float results ride the same R8 stack slot, so they round-trip
// through double losslessly; integer/reference results pass through directly.
inline uint64_t dn2cpp_r8_bits(double d) { uint64_t u; std::memcpy(&u, &d, 8); return u; }
inline double dn2cpp_bits_r8(uint64_t u) { double d; std::memcpy(&d, &u, 8); return d; }
// The float-width pair, for the generic-math bit-pattern members on Single
// (op_OnesComplement, AllBitsSet) where the exact 32-bit pattern is
// observable and a NaN literal would not preserve it.
inline uint32_t dn2cpp_r4_bits(float f) { uint32_t u; std::memcpy(&u, &f, 4); return u; }
inline float dn2cpp_bits_r4(uint32_t u) { float f; std::memcpy(&f, &u, 4); return f; }

// ---- interpolated string builder ----
//
// DefaultInterpolatedStringHandler is the struct the C# compiler lowers $"..."
// to. Its real body routes through Buffer.Memmove (InternalCall) which we don't
// model, so we replace it with a growable UTF-16 buffer that produces the same
// output as .NET for the supported hole types.
struct Dn2CppISB
{
    char16_t* buf;
    int32_t length;
    int32_t capacity;
};

// Create a new builder. The literalLength/formattedCount args hint the initial
// capacity but are not binding (the buffer grows as needed).
Dn2CppISB dn2cpp_isb_new(int32_t literalLength, int32_t formattedCount);
// Append a string literal (the fixed text between holes — never aligned).
void dn2cpp_isb_append_literal(Dn2CppISB* h, Dn2CppString* s);
// Append an already-formatted hole value, padded to a minimum width: when
// `alignment` > 0 the value is right-aligned (leading spaces), when < 0 it is
// left-aligned (trailing spaces); 0 means no padding. Mirrors .NET's `,N`
// alignment component of an interpolation hole.
void dn2cpp_isb_append_aligned(Dn2CppISB* h, Dn2CppString* s, int32_t alignment);
// Format an integer/double hole value honoring the standard numeric format
// string `fmt` (the `:fmt` component of a hole), or default formatting when
// `fmt` is null. Supported: int X/x (hex), D (min-width decimal); double F
// (fixed-point), N (grouped). An unrecognized specifier falls back to default.
// The integer formatters take the type width in bytes (1/2/4/8) so hex and the
// signed/unsigned interpretation match the declared CLR type.
Dn2CppString* dn2cpp_format_int(int64_t v, int32_t byteWidth, Dn2CppString* fmt);
Dn2CppString* dn2cpp_format_uint(uint64_t v, int32_t byteWidth, Dn2CppString* fmt);
// The integer-primitive span-write formatter, `<T>.TryFormat(
// Span<char> dest, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider)`.
// Formats `v` with `fmt` (same byteWidth-aware semantics as dn2cpp_format_int/uint —
// `nullptr`/empty fmt = "G" = default decimal), then copies the result into `dest`
// (capacity `destLen` char16_t): if it fits, write the code units, set
// *charsWritten = length, return true; otherwise leave `dest` untouched, set
// *charsWritten = 0, return false (matches real .NET, probed). `nfi` is the popped
// IFormatProvider; null keeps .NET's null-provider rule (current culture). The real
// bodies route through System.Number.TryFormat* — the dominant remaining
// console-self-host cascade (reached from SRM SignatureDecoder).
bool dn2cpp_try_format_int_c(int64_t v, int32_t byteWidth, Dn2CppString* fmt,
    char16_t* dest, int32_t destLen, int32_t* charsWritten, const Dn2CppNumberFormatInfo* nfi);
bool dn2cpp_try_format_uint_c(uint64_t v, int32_t byteWidth, Dn2CppString* fmt,
    char16_t* dest, int32_t destLen, int32_t* charsWritten, const Dn2CppNumberFormatInfo* nfi);
// UTF-8 twins for `IUtf8SpanFormattable.TryFormat(Span<byte> utf8Destination,
// out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider)`: the same
// formatter cores, narrowed into the byte span as UTF-8 (multi-byte for any
// non-ASCII culture symbol) with the same fits ⇒ write+true / else untouched +
// bytesWritten=0 + false semantics.
bool dn2cpp_try_format_int_utf8_c(int64_t v, int32_t byteWidth, Dn2CppString* fmt,
    char* dest, int32_t destLen, int32_t* bytesWritten, const Dn2CppNumberFormatInfo* nfi);
bool dn2cpp_try_format_uint_utf8_c(uint64_t v, int32_t byteWidth, Dn2CppString* fmt,
    char* dest, int32_t destLen, int32_t* bytesWritten, const Dn2CppNumberFormatInfo* nfi);
Dn2CppString* dn2cpp_format_r8(double v, Dn2CppString* fmt);
Dn2CppString* dn2cpp_format_r4(float v, Dn2CppString* fmt);

// --- Culture-aware (IFormatProvider / CultureInfo / NumberFormatInfo) ---
// The invariant singleton; a named-culture lookup (a built-in table whose
// membership rule is stated at kCultures in
// intrinsics/dn2cpp_system_globalization.cpp — an unknown name still gets an
// invariant-symbol copy carrying the requested name); and a fresh mutable
// instance (copy of invariant) for `new NumberFormatInfo()` + setters.
const Dn2CppNumberFormatInfo* dn2cpp_nfi_invariant();
// NumberFormatInfo.InvariantInfo: a DISTINCT singleton with the invariant
// field values but isNfi=1, so an InvariantInfo that escapes to `object`
// through an IFormatProvider-typed slot reports the NumberFormatInfo identity
// .NET reports (dn2cpp_nfi_invariant is the CultureInfo singleton). The two
// format identically; only the recovered managed identity differs.
const Dn2CppNumberFormatInfo* dn2cpp_nfi_invariant_info();
const Dn2CppNumberFormatInfo* dn2cpp_culture_by_name(Dn2CppString* name);
Dn2CppNumberFormatInfo* dn2cpp_nfi_new();
// The process-wide CultureInfo.CurrentCulture: defaults to the culture the HOST
// says the user is in (resolved through dn2cpp_pal_default_locale_name)
// until user code assigns it. The getter never returns null; the setter
// normalizes null back to invariant. CultureInfo.Name / ToString read the
// culture's name through dn2cpp_culture_name ("" when unset).
const Dn2CppNumberFormatInfo* dn2cpp_culture_current();
void dn2cpp_culture_set_current(const Dn2CppNumberFormatInfo* c);
// CultureInfo.CurrentUICulture — a SECOND process-wide slot over the same host
// default, deliberately not an alias of the CurrentCulture slot: .NET lets the two
// differ, and a program that pins one must not thereby move the other.
// CultureInfo.InstalledUICulture is the host default itself, unaffected by either
// setter, as .NET's OS-installed-language answer is.
const Dn2CppNumberFormatInfo* dn2cpp_culture_current_ui();
void dn2cpp_culture_set_current_ui(const Dn2CppNumberFormatInfo* c);
const Dn2CppNumberFormatInfo* dn2cpp_culture_installed_ui();
Dn2CppString* dn2cpp_culture_name(const Dn2CppNumberFormatInfo* c);
// CultureInfo.LCID (see Dn2CppNumberFormatInfo::lcid for the three answers), and
// the reverse lookup CultureInfo.GetCultureInfo(int) needs. The reverse lookup
// returns NULL for an LCID no modeled culture carries — including 4096 itself,
// which real .NET also refuses (it names the ABSENCE of an LCID, so it can never
// identify a culture) — and the caller raises the ArgumentException family
// CultureNotFoundException belongs to.
int32_t dn2cpp_culture_lcid(const Dn2CppNumberFormatInfo* c);
const Dn2CppNumberFormatInfo* dn2cpp_culture_by_lcid(int32_t lcid);
int32_t dn2cpp_culture_is_neutral(const Dn2CppNumberFormatInfo* c);
void dn2cpp_nfi_set_number_decimal(Dn2CppNumberFormatInfo* n, Dn2CppString* v);
void dn2cpp_nfi_set_number_group(Dn2CppNumberFormatInfo* n, Dn2CppString* v);
void dn2cpp_nfi_set_negative_sign(Dn2CppNumberFormatInfo* n, Dn2CppString* v);
void dn2cpp_nfi_set_nan(Dn2CppNumberFormatInfo* n, Dn2CppString* v);
void dn2cpp_nfi_set_pos_inf(Dn2CppNumberFormatInfo* n, Dn2CppString* v);
void dn2cpp_nfi_set_neg_inf(Dn2CppNumberFormatInfo* n, Dn2CppString* v);
void dn2cpp_nfi_set_percent_symbol(Dn2CppNumberFormatInfo* n, Dn2CppString* v);
void dn2cpp_nfi_set_currency_symbol(Dn2CppNumberFormatInfo* n, Dn2CppString* v);
// NumberGroupSizes / CurrencyGroupSizes / PercentGroupSizes. The getter builds a
// fresh int[] per call, as real .NET's does (two reads are not reference-equal, and
// mutating the returned array does not move the culture's formatting). The setter
// validates against the modeled array and traps on anything else — `which` names the
// property in the message, since the three share one array and honoring a write to
// one would silently move the others.
// `ti` is the Int32[] handle the emit arms supply — see dn2cpp_decimal_get_bits for
// why the shared int-array handle is not enough. Null degrades to it.
Dn2CppArrayI4* dn2cpp_nfi_group_sizes(const Dn2CppNumberFormatInfo* n, const Dn2CppTypeInfo* ti);
void dn2cpp_nfi_set_group_sizes(const Dn2CppNumberFormatInfo* n, Dn2CppArrayI4* v, const char* which);
// Shared culture helpers used by every numeric formatter: resolve a possibly-null
// NFI to the CURRENT culture (.NET's null-provider rule — see the contract at the
// definition), and re-render a known-ASCII numeric buffer with the culture's
// (multi-char) decimal/sign/exponent symbols. Exposed so per-namespace intrinsic
// units (e.g. Decimal) localize identically to the core formatters.
const Dn2CppNumberFormatInfo* dn2cpp_nfi_or_current(const Dn2CppNumberFormatInfo* n);
Dn2CppString* dn2cpp_localize_ascii(const char* buf, int len, const Dn2CppNumberFormatInfo* n);
// Culture-bearing formatter variants. The no-suffix forms above pass a null NFI,
// i.e. they format in the CURRENT culture — which is what the provider-less .NET
// overloads they serve do. Pass dn2cpp_nfi_invariant() to mean invariant.
Dn2CppString* dn2cpp_double_to_string_c(double v, const Dn2CppNumberFormatInfo* n);
Dn2CppString* dn2cpp_float_to_string_c(float v, const Dn2CppNumberFormatInfo* n);
Dn2CppString* dn2cpp_format_int_c(int64_t v, int32_t byteWidth, Dn2CppString* fmt, const Dn2CppNumberFormatInfo* n);
Dn2CppString* dn2cpp_format_uint_c(uint64_t v, int32_t byteWidth, Dn2CppString* fmt, const Dn2CppNumberFormatInfo* n);
Dn2CppString* dn2cpp_format_r8_c(double v, Dn2CppString* fmt, const Dn2CppNumberFormatInfo* n);
Dn2CppString* dn2cpp_format_r4_c(float v, Dn2CppString* fmt, const Dn2CppNumberFormatInfo* n);

// --- System.Decimal ---
// A value type modeled as an intrinsic (the real corelib Decimal.ToString reaches
// Number.FormatDecimal -> ArrayPool -> EventSource -> Calli, which we cannot
// transpile). value = (-1)^sign * mantissa * 10^-scale, the mantissa a 96-bit integer
// in _hi32:_lo64. `scale` is 0..28 and trailing zeros are significant, so 1.50m keeps
// mantissa 150 / scale 2 and prints "1.50". Passed by value.
//
// The fields are .NET's, in .NET's order, with sign and scale packed into _flags the
// way .NET packs them, because a serializer that treats an unmanaged value as a blob
// memcpys the whole thing: these sixteen bytes ARE the wire image, and GetBits' flags
// word is _flags verbatim.
struct Dn2CppDecimal
{
    int32_t  _flags;  // scale in bits 16..23, sign in bit 31; every other bit zero
    uint32_t _hi32;   // high 32 bits of the 96-bit mantissa
    uint64_t _lo64;   // low 64 bits of the 96-bit mantissa

    constexpr int32_t scale() const { return (int32_t)(((uint32_t)_flags >> 16) & 0xFFu); }
    constexpr int32_t sign() const { return (int32_t)((uint32_t)_flags >> 31); }
};
// The flags assembly, shared by the runtime's two writers and by the assertions below.
// The shift is unsigned and cast back: `1 << 31` on a signed int32 is undefined
// behaviour under C++17, the tree's standard.
constexpr int32_t dn2cpp_dec_flags(int32_t scale, int32_t sign)
{
    return (int32_t)(((uint32_t)(scale & 0xFF) << 16) | ((uint32_t)(sign & 1) << 31));
}
// The layout, pinned where the compiler can check it: a host that laid the fields out
// any other way would put different bytes on the wire, which is a silently wrong answer
// rather than a failure — so make it fail to COMPILE instead. The bit positions are
// asserted through the accessors, so the packing and its inverse cannot drift apart.
static_assert(sizeof(Dn2CppDecimal) == 16, "decimal must be .NET's sixteen bytes");
static_assert(alignof(Dn2CppDecimal) == 8, "decimal must be eight-byte aligned");
static_assert(offsetof(Dn2CppDecimal, _flags) == 0 && offsetof(Dn2CppDecimal, _hi32) == 4
    && offsetof(Dn2CppDecimal, _lo64) == 8, "the field order is .NET's");
static_assert(dn2cpp_dec_flags(0, 0) == 0, "zero scale and a positive sign is the zero word");
static_assert(dn2cpp_dec_flags(2, 0) == 0x00020000, "the scale byte sits at bits 16..23");
static_assert((uint32_t)dn2cpp_dec_flags(3, 1) == 0x80030000u, "the sign is bit 31");
static_assert(Dn2CppDecimal{ dn2cpp_dec_flags(28, 1), 0, 1 }.scale() == 28
    && Dn2CppDecimal{ dn2cpp_dec_flags(28, 1), 0, 1 }.sign() == 1,
    "the accessors invert the packing");

// Constructors. `from_parts` is the .NET `Decimal(int lo, int mid, int hi, bool
// isNegative, byte scale)` ctor (and the bit-pattern form); the rest are the
// numeric implicit/explicit conversions into decimal.
Dn2CppDecimal dn2cpp_decimal_from_parts(int32_t lo, int32_t mid, int32_t hi, int32_t isNeg, int32_t scale);
Dn2CppDecimal dn2cpp_decimal_from_i4(int32_t v);
Dn2CppDecimal dn2cpp_decimal_from_u4(uint32_t v);
// Decimal.GetBits: the .NET int[4] layout { lo32, mid32, hi32, flags } — lo32|(mid32<<32)
// is the low 64 mantissa bits, hi32 the high 32, flags packs scale in bits 16..23 and the
// sign in bit 31. The array form allocates a fresh int32[4]; the span form writes into a
// caller Span<int> (>= 4 required — a shorter one raises a catchable ArgumentException)
// and returns 4 (the element count written).
Dn2CppArrayI4* dn2cpp_decimal_get_bits(Dn2CppDecimal a, const Dn2CppTypeInfo* ti);
int32_t dn2cpp_decimal_get_bits_span(Dn2CppDecimal a, int32_t* dst, int32_t destLen);
Dn2CppDecimal dn2cpp_decimal_from_i8(int64_t v);
Dn2CppDecimal dn2cpp_decimal_from_u8(uint64_t v);
Dn2CppDecimal dn2cpp_decimal_from_double(double v);
Dn2CppDecimal dn2cpp_decimal_from_float(float v);

// Arithmetic. Results are rounded to fit the 96-bit mantissa / 28-digit scale
// using round-half-away-from-zero, matching .NET's operator semantics.
Dn2CppDecimal dn2cpp_decimal_add(Dn2CppDecimal a, Dn2CppDecimal b);
Dn2CppDecimal dn2cpp_decimal_sub(Dn2CppDecimal a, Dn2CppDecimal b);
Dn2CppDecimal dn2cpp_decimal_mul(Dn2CppDecimal a, Dn2CppDecimal b);
Dn2CppDecimal dn2cpp_decimal_div(Dn2CppDecimal a, Dn2CppDecimal b);
Dn2CppDecimal dn2cpp_decimal_rem(Dn2CppDecimal a, Dn2CppDecimal b);
Dn2CppDecimal dn2cpp_decimal_neg(Dn2CppDecimal a);

// Three-way compare: -1 if a<b, 0 if equal, 1 if a>b (scale-independent).
int32_t dn2cpp_decimal_cmp(Dn2CppDecimal a, Dn2CppDecimal b);

// INumberBase<decimal> predicates and magnitude selectors, matching the BCL
// Decimal bodies: IsCanonical is representation-sensitive (scale 0, or a
// mantissa with no trailing decimal zero); Is{Even,Odd}Integer require an
// integer value and test the truncated 96-bit mantissa's low bit (a
// non-integer is neither); Max/MinMagnitude compare |x| vs |y| and break
// magnitude ties toward the non-negative / negative x respectively (the
// *MagnitudeNumber variants are identical — decimal has no NaN).
int32_t dn2cpp_decimal_is_canonical(Dn2CppDecimal a);
int32_t dn2cpp_decimal_is_even_integer(Dn2CppDecimal a);
int32_t dn2cpp_decimal_is_odd_integer(Dn2CppDecimal a);
Dn2CppDecimal dn2cpp_decimal_max_magnitude(Dn2CppDecimal x, Dn2CppDecimal y);
Dn2CppDecimal dn2cpp_decimal_min_magnitude(Dn2CppDecimal x, Dn2CppDecimal y);

// Conversions out (explicit casts). The integer forms truncate toward zero.
double  dn2cpp_decimal_to_double(Dn2CppDecimal a);
float   dn2cpp_decimal_to_float(Dn2CppDecimal a);
int64_t dn2cpp_decimal_to_i8(Dn2CppDecimal a);
uint64_t dn2cpp_decimal_to_u8(Dn2CppDecimal a);

// Rounding statics. `digits` is the kept decimal places (Round); `mode` is the
// MidpointRounding enum value (0 = ToEven/banker's, 1 = AwayFromZero,
// 2 = ToZero, 3 = ToNegativeInfinity, 4 = ToPositiveInfinity) — an invalid
// mode raises a catchable ArgumentException like the BCL, even when no digit
// would be dropped.
Dn2CppDecimal dn2cpp_decimal_round(Dn2CppDecimal a, int32_t digits, int32_t mode);
Dn2CppDecimal dn2cpp_decimal_truncate(Dn2CppDecimal a);
Dn2CppDecimal dn2cpp_decimal_floor(Dn2CppDecimal a);
Dn2CppDecimal dn2cpp_decimal_ceiling(Dn2CppDecimal a);
Dn2CppDecimal dn2cpp_decimal_abs(Dn2CppDecimal a);

// ToString: default ("G", preserves scale) and the standard-format-string form
// (F/N/C/P/E/G/D-less number specifiers via the shared number path); culture
// variant takes the NumberFormatInfo (decimal/group separators, sign symbols).
Dn2CppString* dn2cpp_decimal_to_string(Dn2CppDecimal a);
Dn2CppString* dn2cpp_decimal_format(Dn2CppDecimal a, Dn2CppString* fmt);
Dn2CppString* dn2cpp_decimal_format_c(Dn2CppDecimal a, Dn2CppString* fmt, const Dn2CppNumberFormatInfo* n);
// Parse (invariant): optional whitespace, optional sign, digits with one optional
// decimal point and optional group separators. TryParse writes *out and returns
// 1/0; Parse traps on malformed input.
int32_t dn2cpp_decimal_try_parse(Dn2CppString* s, Dn2CppDecimal* out);
Dn2CppDecimal dn2cpp_decimal_parse(Dn2CppString* s);
// NumberStyles-honoring parse (the dn2cpp_parse.cpp state machine + a .NET
// NumberToDecimal-style conversion: scale preserved, fraction digits beyond
// 28 rounded half-even). Parse forms throw Format/Overflow/Argument[Null];
// TryParse forms return 0/1 but still throw ArgumentException on an invalid
// styles combination. A null provider means the invariant culture.
int32_t dn2cpp_decimal_tryparse_styles_chars(const char16_t* p, int32_t n, int32_t styles,
                                             const Dn2CppNumberFormatInfo* nfi, Dn2CppDecimal* out);
int32_t dn2cpp_decimal_tryparse_styles_str(Dn2CppString* s, int32_t styles,
                                           const Dn2CppNumberFormatInfo* nfi, Dn2CppDecimal* out);
Dn2CppDecimal dn2cpp_decimal_parse_styles_chars(const char16_t* p, int32_t n, int32_t styles,
                                                const Dn2CppNumberFormatInfo* nfi);
Dn2CppDecimal dn2cpp_decimal_parse_styles_str(Dn2CppString* s, int32_t styles,
                                              const Dn2CppNumberFormatInfo* nfi);
// Scale-insensitive hash for a boxed decimal: equal values (1.0m == 1.00m) hash
// equal — canonicalizes by stripping trailing zero digits before mixing the
// mantissa/sign (the exact value need not match .NET's, only the contract). Used
// by dn2cpp_object_gethashcode for a boxed System.Decimal.
int32_t dn2cpp_decimal_hash(Dn2CppDecimal a);
// Convert.To*(decimal): banker's-round at 0 digits then range-check (catchable
// OverflowException). Convert.ToDecimal(string/object): null -> 0m, a string
// parses NumberStyles.Number, a boxed source reads through the IConvertible
// matrix.
int64_t dn2cpp_convert_dec_checked(Dn2CppDecimal value, int64_t lo, int64_t hi);
uint64_t dn2cpp_convert_dec_to_u64(Dn2CppDecimal value);
Dn2CppDecimal dn2cpp_convert_str_to_decimal(Dn2CppString* s);
Dn2CppDecimal dn2cpp_convert_obj_to_decimal(Dn2CppObject* v);

// --- System.TimeSpan / System.DateTime ---
// Two intrinsic value types (like Dn2CppDecimal): the real corelib DateTime /
// TimeSpan reach Number formatting / InternalCall / culture, which we cannot
// transpile, so they are modeled here with C++ helpers (InvariantCulture only).
// A tick is 100ns. TimeSpan is a signed 64-bit tick count. DateTime is a tick
// count since 0001-01-01 00:00:00 (always 0..3155378975999999999) plus a
// DateTimeKind (0=Unspecified, 1=Utc, 2=Local). Both passed by value.
struct Dn2CppTimeSpan { int64_t ticks; };

// DateTime is ONE int64 word, laid out exactly as .NET's `DateTime._dateData`: the
// tick count in the low 62 bits and the DateTimeKind in the top two. That is what
// makes `sizeof(DateTime)` 8, which every size reader answers with — the emit-time
// `Unsafe.SizeOf<T>()` / IL `sizeof` lowerings, the reflected `dn2cpp_layout_size`,
// the box payload, the array element stride, the `Unsafe.Add`/`ByteOffset`
// arithmetic. `Nullable<DateTime>` follows for free at 16.
//
// A SINGLE SCALAR with shift/mask accessors, deliberately NOT a bit-field: a
// bit-field's allocation unit is implementation-defined (MSVC lays a 62-bit signed
// field beside its 2-bit neighbour differently from clang), which would make the byte
// image host-compiler-dependent in a type that crosses the hot-update ABI boundary and
// is serialized by the Godot lane.
//
// DateTime.MaxValue.Ticks is 3155378975999999999 < 2^62, so every legal value is
// representable — but an ILLEGAL one is not, and would smear into the kind bits. That
// is why dn2cpp_datetime_pack VALIDATES rather than masking, throwing the
// ArgumentOutOfRangeException real .NET throws.
struct Dn2CppDateTime
{
    int64_t _dateData;

    // The tick count (0 .. 3155378975999999999).
    constexpr int64_t ticks() const { return (int64_t)((uint64_t)_dateData & 0x3FFFFFFFFFFFFFFFULL); }
    // The DateTimeKind (0=Unspecified, 1=Utc, 2=Local). The raw flag value 3 is
    // .NET's KindLocalAmbiguousDst, which its public `Kind` property reports as
    // Local; nothing in dn2cpp mints it, but a program can reach one through
    // Unsafe.As<long, DateTime> now that the two representations are the same
    // bits, so answer it the way .NET's property does rather than inventing a
    // fourth kind.
    constexpr int32_t kind() const
    {
        int32_t flags = (int32_t)((uint64_t)_dateData >> 62);
        return flags == 3 ? 2 : flags;
    }
};
// Ticks past DateTime.MaxValue, kept next to the struct that bounds on it.
#define DN2CPP_DT_MAX_TICKS 3155378975999999999LL
// The word assembly, shared by the two constructors below. The shift is done in
// UNSIGNED arithmetic and cast back: kind 2 sets bit 63, and `2LL << 62` on a signed
// int64 is undefined behaviour under C++17 (the tree's standard), whereas the
// unsigned shift and the implementation-defined-but-two's-complement narrowing back
// are what every target dn2cpp emits for actually does.
constexpr Dn2CppDateTime dn2cpp_dt_word(int64_t ticks, int32_t kind)
{
    return Dn2CppDateTime{ (int64_t)((uint64_t)ticks | ((uint64_t)(uint32_t)(kind & 3) << 62)) };
}
// The layout, pinned where the compiler can check it. This is the WINDOWS argument
// made testable: nothing here can be laid out two ways — the struct has one member,
// and the accessors are `&`/`>>` on a uint64_t rather than a bit-field, whose
// ALLOCATION UNIT is implementation-defined (MSVC starts a new unit when the declared
// type changes, so `int64_t:62` beside an `int32_t:2` is 16 bytes there and 8 under
// clang, and even same-type fields differ in which end the first one occupies). So a
// host whose layout disagreed would fail to COMPILE, in a type that crosses the
// hot-update ABI boundary and whose bit image DateTimeLayoutSubset diffs against real
// .NET's. The magic words are .NET's own: MinValue, MaxValue, and 2024-03-05T06:07:08
// at each kind, read off CoreCLR through Unsafe.As.
static_assert(sizeof(Dn2CppDateTime) == 8, "DateTime must be .NET's eight bytes");
static_assert(alignof(Dn2CppDateTime) == 8, "DateTime must be eight-byte aligned");
static_assert(dn2cpp_dt_word(0, 0)._dateData == 0, "MinValue is the zero word");
static_assert((uint64_t)dn2cpp_dt_word(DN2CPP_DT_MAX_TICKS, 0)._dateData == 0x2BCA2875F4373FFFULL,
    "MaxValue is the tick count with no kind bits");
static_assert((uint64_t)dn2cpp_dt_word(638452156280000000LL, 0)._dateData == 0x08DC3CDA7D26CE00ULL,
    "Unspecified leaves the top two bits clear");
static_assert((uint64_t)dn2cpp_dt_word(638452156280000000LL, 1)._dateData == 0x48DC3CDA7D26CE00ULL,
    "Utc is 0b01 in the top two bits");
static_assert((uint64_t)dn2cpp_dt_word(638452156280000000LL, 2)._dateData == 0x88DC3CDA7D26CE00ULL,
    "Local is 0b10 in the top two bits");
static_assert(dn2cpp_dt_word(638452156280000000LL, 2).ticks() == 638452156280000000LL
    && dn2cpp_dt_word(638452156280000000LL, 2).kind() == 2, "the accessors invert the packing");
static_assert(Dn2CppDateTime{ (int64_t)0xC000000000000000ULL }.kind() == 2,
    "the KindLocalAmbiguousDst flag pair reads as Local, as .NET's Kind property does");
// The checked constructor: every .NET-visible DateTime goes through here, so no
// unrepresentable value can exist. Out of range throws ArgumentOutOfRangeException,
// matching `new DateTime(long)`, `AddTicks`, `Add*` and the +/- operators.
inline Dn2CppDateTime dn2cpp_datetime_pack(int64_t ticks, int32_t kind)
{
    if (ticks < 0 || ticks > DN2CPP_DT_MAX_TICKS)
        dn2cpp_throw_argument_out_of_range();
    return dn2cpp_dt_word(ticks, kind);
}
// The clamping constructor, for the two conversions real .NET clamps rather than
// throws: DateTime.ToLocalTime/ToUniversalTime saturate at MinValue/MaxValue
// (measured: MinValue.ToUniversalTime() answers 0, MaxValue.ToLocalTime() answers
// MaxValue.Ticks). Use it ONLY there — everywhere else a wrong answer is worse
// than the exception .NET raises.
inline Dn2CppDateTime dn2cpp_datetime_pack_clamped(int64_t ticks, int32_t kind)
{
    if (ticks < 0) ticks = 0;
    else if (ticks > DN2CPP_DT_MAX_TICKS) ticks = DN2CPP_DT_MAX_TICKS;
    return dn2cpp_dt_word(ticks, kind);
}
// System.DateTimeOffset. Mirrors the real BCL layout: a "clock" tick count
// (the wall-clock value as observed at this offset, = the DateTime property's ticks)
// plus the offset from UTC in whole minutes. UtcTicks = ticks - offsetMinutes*TPM.
struct Dn2CppDateTimeOffset { int64_t ticks; int32_t offsetMinutes; };
// System.DateOnly: a date with no time, stored as a day number (days since
// 0001-01-01; DayNumber 0..3652058). System.TimeOnly: a time within a day, stored as
// a tick count since midnight (0..863999999999). Both passed by value, InvariantCulture
// only — modeled like DateTime/TimeSpan because the real corelib types reach the same
// Number formatting / calendar / culture paths.
struct Dn2CppDateOnly { int32_t dayNumber; };
struct Dn2CppTimeOnly { int64_t ticks; };

// System.Runtime.InteropServices.GCHandle. dn2cpp's managed heap is NON-MOVING (Boehm
// is conservative mark-and-sweep; the fallback allocator is calloc), so an object's
// address is stable for its lifetime and GC pinning is *identity* — nothing to pin and
// nothing to release. GCHandle is in s_intrinsicTypes, so the BCL members' real bodies
// are cut and each is lowered at the call site.
//
// Cell model — the equivalent of real .NET's handle-table slot. Every allocated handle
// owns one shared Dn2CppGCHandleCell (runtime-private) and the struct is a single
// pointer to it, exactly one IntPtr wide like the real GCHandle. A Normal/Pinned cell
// is GC_MALLOC_UNCOLLECTABLE — scanned but never collected — so the cell itself roots
// the target: a handle whose only surviving representation is its ToIntPtr value parked
// outside GC-visible memory still keeps its target alive, as the real handle table
// does. Pinned additionally caches the data address captured at Alloc time.
// Weak/WeakTrackResurrection cells delegate to the low-level IntPtr-handle table below
// and carry no strong `target`, so a weak handle really is weak. Cell kind =
// GCHandleType + 1 (0 = freed).
//
// Free clears the SHARED cell, so it propagates to every copy of the struct and through
// ToIntPtr/FromIntPtr round trips, like real .NET invalidating the table slot. Mirroring
// real .NET: the receiver Free was called on is zeroed, so on THAT copy IsAllocated goes
// false and Target/Free throw InvalidOperationException; a STALE copy still reads
// IsAllocated true, its Target is null without throwing, and its Free is a no-op.
// ToIntPtr returns the stable cell address and never throws (0 for a zeroed handle);
// FromIntPtr(0) throws. AddrOfPinnedObject on a live non-Pinned handle throws. The
// Target setter writes the shared cell and on a Pinned handle re-pins.
//
// A freed cell is NEVER GC_FREE'd: a stale struct copy may still point at it, and
// GC_FREE would let the block be reallocated as an unrelated object, turning that stale
// read into type confusion. It is cleared (kind -> 0, detectable from any copy without
// touching freed memory) and recycled through a private free pool. Consequences, all
// shared with the real handle table: a stale copy can observe a recycled slot as some
// LATER handle's live cell; pooled cells never return to the OS; and Target/Free races
// on one handle are out of scope (single-threaded handle use only).
struct Dn2CppGCHandleCell;
struct Dn2CppGCHandle
{
    Dn2CppGCHandleCell* cell; // shared slot; null = default(GCHandle) / this copy Free'd
};
// handleType is the raw GCHandleType (0=Weak,1=WeakTrackResurrection,2=Normal,3=Pinned).
Dn2CppGCHandle dn2cpp_gchandle_alloc(Dn2CppObject* target, void* dataAddr, int32_t handleType);
Dn2CppObject* dn2cpp_gchandle_target(Dn2CppGCHandle h);   // null cell -> InvalidOperationException; freed cell -> null
void dn2cpp_gchandle_set_target(Dn2CppGCHandle h, Dn2CppObject* value); // writes the cell: reaches all copies
void* dn2cpp_gchandle_addr(Dn2CppGCHandle h);             // non-Pinned -> InvalidOperationException
int32_t dn2cpp_gchandle_is_allocated(Dn2CppGCHandle h);   // cell != null (never throws)
void dn2cpp_gchandle_free(Dn2CppGCHandle* h);             // clears the cell + zeroes *h
void dn2cpp_gchandle_free_value(Dn2CppGCHandle h);        // rvalue receiver: clears the cell only
intptr_t dn2cpp_gchandle_to_intptr(Dn2CppGCHandle h);     // the cell address (stable); 0 for a zeroed handle
Dn2CppGCHandle dn2cpp_gchandle_from_intptr(intptr_t v);   // 0 -> InvalidOperationException

// GCHandle's low-level IntPtr-handle table (GCHandle.InternalAlloc/Get/Set/Free/
// CompareExchange), distinct from the struct pinning model above. WeakReference /
// WeakReference<T> store a raw nint handle and drive it through these — reached
// from JsonSerializerOptions' caching-context WeakReference. GCHandle.Alloc's
// public Normal/Pinned path is intercepted separately (the struct pinning model
// above) and never reaches here, so handleType is Weak (0) or
// WeakTrackResurrection (1) in practice.
//
// Each handle is a Dn2CppWeakCell holding the target plus which kind of Boehm
// disappearing link it is registered with:
//   - Weak (short): GC_general_register_disappearing_link — cleared as soon as the
//     referent is found unreachable, before any finalizer runs (matches
//     WeakReference<T>(x, trackResurrection: false), the default).
//   - WeakTrackResurrection (long): GC_register_long_link — cleared only once the
//     referent is truly unreachable, matching trackResurrection: true.
//
// INVARIANT: the cell must be allocated ATOMIC (dn2cpp_alloc_atomic), not with the
// ordinary pointer-scanning allocator, and hiddenTarget must hold
// GC_HIDE_POINTER(target) rather than a raw pointer. The atomic allocator is the
// load-bearing half: a disappearing link registered against a slot inside a scanned
// block never clears, because the conservative mark phase re-marks the referent live
// right through this very cell — silently making every weak reference strong.
// GC_HIDE_POINTER on top only keeps the raw pointer out of a memory dump.
// dn2cpp_gchandle_internal_get reveals it with GC_REVEAL_POINTER. The handle is the
// cell's address as an intptr_t.
struct Dn2CppWeakCell { intptr_t hiddenTarget; int32_t isLong; };
intptr_t dn2cpp_gchandle_internal_alloc(Dn2CppObject* target, int32_t handleType);
Dn2CppObject* dn2cpp_gchandle_internal_get(intptr_t handle);
void dn2cpp_gchandle_internal_set(intptr_t handle, Dn2CppObject* target);
void dn2cpp_gchandle_internal_free(intptr_t handle);
Dn2CppObject* dn2cpp_gchandle_internal_compare_exchange(intptr_t handle, Dn2CppObject* value, Dn2CppObject* oldValue);

// System.Runtime.CompilerServices.DependentHandle — the ephemeron primitive
// behind ConditionalWeakTable (real .NET: a handle-table InternalCall surface).
// A cell pairs a weak target with a strong dependent:
//   - target: a low-level weak handle (dn2cpp_gchandle_internal_*, short link) —
//     reads yield null once the target was collected or set to null;
//   - dependent: an ordinary scanned pointer in the cell, so it stays strongly
//     reachable for the cell's lifetime. Reads hand it out only while the
//     target is still alive, matching the ephemeron read contract.
// Approximation bound (vs a real ephemeron): the dependent's liveness is not
// CONDITIONED on the target's — a dependent that itself references its target
// keeps the pair alive (a leak real .NET avoids). A ConditionalWeakTable whose
// values are null or independent of their keys — ArrayPool<T>.Shared's
// TLS-bucket registry, the only framework driver in the tree — is exact.
// The cell is ordinary GC memory: the Dn2CppDependentHandle value embedded in
// CWT's Entry structs (scanned) is what keeps it alive; free retires the weak
// handle and drops the dependent, and the empty cell itself is reclaimed by
// the collector once no handle copy survives.
struct Dn2CppDependentCell { intptr_t targetWeak; Dn2CppObject* dependent; };
struct Dn2CppDependentHandle
{
    Dn2CppDependentCell* cell; // null = default(DependentHandle) / Dispose'd
};
Dn2CppDependentHandle dn2cpp_dependenthandle_alloc(Dn2CppObject* target, Dn2CppObject* dependent);
int32_t dn2cpp_dependenthandle_is_allocated(Dn2CppDependentHandle h);
Dn2CppObject* dn2cpp_dependenthandle_target(Dn2CppDependentHandle h);
Dn2CppObject* dn2cpp_dependenthandle_target_and_dependent(Dn2CppDependentHandle h, Dn2CppObject** dependent);
void dn2cpp_dependenthandle_set_target_null(Dn2CppDependentHandle h);
void dn2cpp_dependenthandle_set_dependent(Dn2CppDependentHandle h, Dn2CppObject* dependent);
void dn2cpp_dependenthandle_free(Dn2CppDependentHandle* h);      // retires the cell + zeroes *h
void dn2cpp_dependenthandle_free_value(Dn2CppDependentHandle h); // rvalue receiver: retires the cell only

// Environment.TickCount64 — monotonic milliseconds since an arbitrary origin
// (std::chrono::steady_clock; the real body is the GetLowResolutionTimestamp
// QCall). Environment.TickCount is plain IL truncating this to int32.
int64_t dn2cpp_tickcount64();

// Thread.GetCurrentProcessorId — a partition-selection HINT (the BCL contract
// allows any value). Each thread gets a stable round-robin id in
// [0, hardware_concurrency); the real body is ProcessorIdCache over the
// SchedGetCpu P/Invoke plus a refresh-rate heuristic.
int32_t dn2cpp_current_processor_id();

extern const Dn2CppTypeInfo dn2cpp_timespan_type;
extern const Dn2CppTypeInfo dn2cpp_datetime_type;
extern const Dn2CppTypeInfo dn2cpp_datetimeoffset_type;
extern const Dn2CppTypeInfo dn2cpp_dateonly_type;
extern const Dn2CppTypeInfo dn2cpp_timeonly_type;

// TimeSpan ctors / static factories. `from_unit(value, ticksPerUnit)` backs
// FromDays/FromHours/FromMinutes/FromSeconds/FromMilliseconds — modern .NET
// truncates value*ticksPerUnit toward zero (no millisecond rounding).
Dn2CppTimeSpan dn2cpp_timespan_from_ticks(int64_t ticks);
Dn2CppTimeSpan dn2cpp_timespan_from_hms(int32_t h, int32_t m, int32_t s);
Dn2CppTimeSpan dn2cpp_timespan_from_dhms(int32_t d, int32_t h, int32_t m, int32_t s);
Dn2CppTimeSpan dn2cpp_timespan_from_dhmsms(int32_t d, int32_t h, int32_t m, int32_t s, int32_t ms);
Dn2CppTimeSpan dn2cpp_timespan_from_unit(double value, int64_t ticksPerUnit);
Dn2CppTimeSpan dn2cpp_timespan_add(Dn2CppTimeSpan a, Dn2CppTimeSpan b);
Dn2CppTimeSpan dn2cpp_timespan_sub(Dn2CppTimeSpan a, Dn2CppTimeSpan b);
Dn2CppTimeSpan dn2cpp_timespan_neg(Dn2CppTimeSpan a);
Dn2CppTimeSpan dn2cpp_timespan_duration(Dn2CppTimeSpan a);
int32_t dn2cpp_timespan_cmp(Dn2CppTimeSpan a, Dn2CppTimeSpan b);
// Component properties carry the sign of the whole span (Days/Hours/... of a
// negative TimeSpan are all <= 0), matching .NET.
int32_t dn2cpp_timespan_days(Dn2CppTimeSpan a);
int32_t dn2cpp_timespan_hours(Dn2CppTimeSpan a);
int32_t dn2cpp_timespan_minutes(Dn2CppTimeSpan a);
int32_t dn2cpp_timespan_seconds(Dn2CppTimeSpan a);
int32_t dn2cpp_timespan_milliseconds(Dn2CppTimeSpan a);
double  dn2cpp_timespan_total(Dn2CppTimeSpan a, int64_t ticksPerUnit);
// Default ToString: the culture-independent "c" format
// [-][d'.']hh':'mm':'ss['.'fffffff] (fraction shown only when non-zero, 7 digits).
Dn2CppString* dn2cpp_timespan_to_string(Dn2CppTimeSpan a);
int32_t dn2cpp_timespan_hash(Dn2CppTimeSpan a);
// ToString(format) — InvariantCulture. Standard specifiers "c"/"t"/"T" (the
// constant format), "g" (general short), "G" (general long); otherwise treated as a
// custom format string (d/h/m/s/f/F specifiers, ' " quotes, \ escape). null/empty == "c".
Dn2CppString* dn2cpp_timespan_format(Dn2CppTimeSpan a, Dn2CppString* fmt);
// Parse / TryParse / ParseExact — InvariantCulture. The try_* forms return
// false on a malformed input (back TryParse); the throwing wrappers raise
// FormatException (back Parse). Parse reads the general invariant form
// [-]{d | [d.]hh:mm[:ss[.fffffff]]}; ParseExact reads the c/t/T standard or a custom
// format (d/h/m/s/f/F). Out-of-range components fail (TryParse false / Parse throw).
bool dn2cpp_timespan_try_parse(Dn2CppString* s, Dn2CppTimeSpan* out);
bool dn2cpp_timespan_try_parse_exact(Dn2CppString* s, Dn2CppString* fmt, Dn2CppTimeSpan* out);
Dn2CppTimeSpan dn2cpp_timespan_parse(Dn2CppString* s);
Dn2CppTimeSpan dn2cpp_timespan_parse_exact(Dn2CppString* s, Dn2CppString* fmt);

// DateTime ctors. The ymd* forms compute ticks from the proleptic Gregorian
// calendar; `kind` is the DateTimeKind int. Range validation is loose
// (no throw on an out-of-range field — a carve-out).
Dn2CppDateTime dn2cpp_datetime_from_ticks(int64_t ticks, int32_t kind);
// Windows FILETIME (100ns since 1601-01-01 UTC) <-> DateTime.ticks. FromFileTimeUtc validates
// fileTime's range (throws ArgumentOutOfRangeException like the real BCL); ToFileTimeUtc
// converts a Local-kind value first (Unspecified's ticks pass through as-is — matches the real
// BCL's ToFileTimeUtc, a deliberate asymmetry vs. ToUniversalTime) and throws if the result
// would be negative (a DateTime before 1601-01-01).
Dn2CppDateTime dn2cpp_datetime_from_file_time_utc(int64_t fileTime);
int64_t dn2cpp_datetime_to_file_time_utc(Dn2CppDateTime a);
Dn2CppDateTime dn2cpp_datetime_ymd(int32_t y, int32_t mo, int32_t d, int32_t kind);
Dn2CppDateTime dn2cpp_datetime_ymdhms(int32_t y, int32_t mo, int32_t d, int32_t h, int32_t mi, int32_t s, int32_t kind);
Dn2CppDateTime dn2cpp_datetime_ymdhmsms(int32_t y, int32_t mo, int32_t d, int32_t h, int32_t mi, int32_t s, int32_t ms, int32_t kind);
int32_t dn2cpp_datetime_year(Dn2CppDateTime a);
int32_t dn2cpp_datetime_month(Dn2CppDateTime a);
int32_t dn2cpp_datetime_day(Dn2CppDateTime a);
int32_t dn2cpp_datetime_hour(Dn2CppDateTime a);
int32_t dn2cpp_datetime_minute(Dn2CppDateTime a);
int32_t dn2cpp_datetime_second(Dn2CppDateTime a);
int32_t dn2cpp_datetime_millisecond(Dn2CppDateTime a);
int32_t dn2cpp_datetime_dayofweek(Dn2CppDateTime a);   // 0=Sunday .. 6=Saturday
int32_t dn2cpp_datetime_dayofyear(Dn2CppDateTime a);   // 1-based
void dn2cpp_datetime_get_date(Dn2CppDateTime a, int32_t* year, int32_t* month, int32_t* day);
void dn2cpp_datetime_get_time(Dn2CppDateTime a, int32_t* hour, int32_t* minute, int32_t* second);
void dn2cpp_datetime_get_time_ms(Dn2CppDateTime a, int32_t* hour, int32_t* minute, int32_t* second, int32_t* millisecond);
void dn2cpp_datetime_get_time_precise(Dn2CppDateTime a, int32_t* hour, int32_t* minute, int32_t* second, int32_t* tick);
// AddTicks is exact; Add*(double) truncates value*ticksPerUnit toward zero (no
// millisecond rounding); AddMonths/AddYears clamp the day to the target month.
Dn2CppDateTime dn2cpp_datetime_add_ticks(Dn2CppDateTime a, int64_t ticks);
Dn2CppDateTime dn2cpp_datetime_add_unit(Dn2CppDateTime a, double value, int64_t ticksPerUnit);
Dn2CppDateTime dn2cpp_datetime_add_months(Dn2CppDateTime a, int32_t months);
Dn2CppDateTime dn2cpp_datetime_add_years(Dn2CppDateTime a, int32_t years);
int32_t dn2cpp_datetime_cmp(Dn2CppDateTime a, Dn2CppDateTime b); // by ticks (ignores Kind, matching .NET)
int32_t dn2cpp_datetime_is_leap_year(int32_t y);
int32_t dn2cpp_datetime_days_in_month(int32_t y, int32_t mo);
// Default ToString: the InvariantCulture "G" pattern MM/dd/yyyy HH:mm:ss.
Dn2CppString* dn2cpp_datetime_to_string(Dn2CppDateTime a);
int32_t dn2cpp_datetime_hash(Dn2CppDateTime a);
// ToString(format) — InvariantCulture. A single-char standard specifier
// (d/D/f/F/g/G/M/m/O/o/R/r/s/t/T/u/Y/y) expands to its invariant pattern; anything
// longer is a custom format string (y/M/d/H/h/m/s/f/F/t/g/K specifiers, ' " quotes,
// \ escape, % single-specifier). null/empty == "G". Time-zone offset (z/zzz) and the
// "U" specifier are carve-outs (no time-zone model — invariant only).
Dn2CppString* dn2cpp_datetime_format(Dn2CppDateTime a, Dn2CppString* fmt);
// Parse / TryParse / ParseExact — InvariantCulture. Parse reads ISO
// (yyyy-MM-dd[THH:mm:ss[.fff]]) and the US-invariant short forms (M/d/yyyy[ HH:mm[:ss]]);
// ParseExact reads a custom format (y/M[/MMM/MMMM]/d/H/h/m/s/f/F/t literals) or a
// single-char standard specifier expanded to its pattern. A format with no date part
// defaults to 0001-01-01 (no clock — .NET uses today; carve-out). Time-zone
// offsets are consumed but not applied (invariant only).
bool dn2cpp_datetime_try_parse(Dn2CppString* s, Dn2CppDateTime* out);
bool dn2cpp_datetime_try_parse_exact(Dn2CppString* s, Dn2CppString* fmt, Dn2CppDateTime* out);
Dn2CppDateTime dn2cpp_datetime_parse(Dn2CppString* s);
Dn2CppDateTime dn2cpp_datetime_parse_exact(Dn2CppString* s, Dn2CppString* fmt);
// Convert.ToDateTime(string/object): null -> DateTime.MinValue; a string parses
// (catchable FormatException on bad input), a boxed DateTime passes through.
Dn2CppDateTime dn2cpp_convert_str_to_datetime(Dn2CppString* s);
Dn2CppDateTime dn2cpp_convert_obj_to_datetime(Dn2CppObject* v);
// Wall clock. The only non-deterministic DateTime helpers — gates assert
// invariants (Kind, a sane year range, Today at midnight), never an exact instant.
// Now/Today read local time (Kind=Local); UtcNow reads UTC (Kind=Utc).
Dn2CppDateTime dn2cpp_datetime_now();
Dn2CppDateTime dn2cpp_datetime_utc_now();
Dn2CppDateTime dn2cpp_datetime_today();
// Time-zone conversion against the host's local zone (DST included). Like
// the wall-clock helpers above these are host-dependent, so gates assert only
// round-trip / Kind invariants. to_local: Utc/Unspecified -> Local; an already-
// Local value is returned unchanged. to_universal: Local/Unspecified -> Utc; an
// already-Utc value is returned unchanged. mktime / localtime_r apply the offset.
Dn2CppDateTime dn2cpp_datetime_to_local(Dn2CppDateTime a);
Dn2CppDateTime dn2cpp_datetime_to_universal(Dn2CppDateTime a);
// The host's UTC offset (whole minutes, DST included) for a local wall-clock instant,
// via mktime — the offset is (clock instant - UTC instant). Backs the DateTimeOffset
// ctor / implicit conversion from a Local/Unspecified DateTime.
int32_t dn2cpp_local_offset_minutes(Dn2CppDateTime localClock);

// System.DateTimeOffset (core value type + properties + Unix time +
// equality/comparison + default ToString + conversions; all deterministic). The clock
// ticks + offset model means component properties (Year..Ticks) read the clock, while
// ordering/equality/Unix time read the UTC instant. InvariantCulture only; named /
// historical time zones stay a carve-out (host local zone only).
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_make(int64_t clockTicks, int32_t offsetMinutes);
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_from_datetime(Dn2CppDateTime dt);
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_from_dt_offset(Dn2CppDateTime dt, Dn2CppTimeSpan offset);
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_to_offset(Dn2CppDateTimeOffset d, Dn2CppTimeSpan offset);
Dn2CppDateTime dn2cpp_datetimeoffset_clock(Dn2CppDateTimeOffset d);   // DateTime property (Kind=Unspecified)
Dn2CppDateTime dn2cpp_datetimeoffset_utc(Dn2CppDateTimeOffset d);     // UtcDateTime property (Kind=Utc)
Dn2CppTimeSpan dn2cpp_datetimeoffset_offset(Dn2CppDateTimeOffset d);  // Offset property
int64_t dn2cpp_datetimeoffset_utc_ticks(Dn2CppDateTimeOffset d);
int32_t dn2cpp_datetimeoffset_cmp(Dn2CppDateTimeOffset a, Dn2CppDateTimeOffset b);
int32_t dn2cpp_datetimeoffset_equals(Dn2CppDateTimeOffset a, Dn2CppDateTimeOffset b);
int32_t dn2cpp_datetimeoffset_hash(Dn2CppDateTimeOffset d);
int64_t dn2cpp_datetimeoffset_to_unix_seconds(Dn2CppDateTimeOffset d);
int64_t dn2cpp_datetimeoffset_to_unix_millis(Dn2CppDateTimeOffset d);
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_from_unix_seconds(int64_t s);
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_from_unix_millis(int64_t ms);
Dn2CppString* dn2cpp_datetimeoffset_to_string(Dn2CppDateTimeOffset d);
// DateTimeOffset: formatting / parsing / arithmetic / wall clock,
// reusing the DateTime formatter/parser. ToString(format) renders the clock components
// for every standard specifier and custom format, with the value's own offset rendered
// by z/zz/zzz/K; the universal specifiers 'R'/'r'/'u' render the UTC instant instead.
// Parse/ParseExact capture an explicit offset ([+-]HH[:]mm or 'Z'=UTC); a missing offset
// falls back to the host local offset (non-deterministic, like Now). Add* keep the offset
// and operate on the clock (= ClockDateTime arithmetic). Now/UtcNow are the only
// non-deterministic helpers (host clock + zone) — gates assert invariants only.
Dn2CppString* dn2cpp_datetimeoffset_format(Dn2CppDateTimeOffset d, Dn2CppString* fmt);
bool dn2cpp_datetimeoffset_try_parse(Dn2CppString* s, Dn2CppDateTimeOffset* out);
bool dn2cpp_datetimeoffset_try_parse_exact(Dn2CppString* s, Dn2CppString* fmt, Dn2CppDateTimeOffset* out);
// TryParseExact(input, string[] formats, ...): first format that matches wins.
bool dn2cpp_datetimeoffset_try_parse_exact_multi(Dn2CppString* s, Dn2CppArrayRef* formats, Dn2CppDateTimeOffset* out);
// The invariant DTFI's abbreviated month/day names (string[]), for the synthesized
// CultureInfo.DateTimeFormat — see dn2cpp_system_datetime.cpp. arrType is the precise
// string[] type-info so the getter's castclass to it succeeds.
Dn2CppArrayRef* dn2cpp_dtfi_invariant_abbrev_month_names(const Dn2CppTypeInfo* arrType);
Dn2CppArrayRef* dn2cpp_dtfi_invariant_abbrev_day_names(const Dn2CppTypeInfo* arrType);
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_parse(Dn2CppString* s);
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_parse_exact(Dn2CppString* s, Dn2CppString* fmt);
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_add_ticks(Dn2CppDateTimeOffset d, int64_t ticks);
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_add_unit(Dn2CppDateTimeOffset d, double value, int64_t ticksPerUnit);
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_add_months(Dn2CppDateTimeOffset d, int32_t months);
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_add_years(Dn2CppDateTimeOffset d, int32_t years);
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_now();
Dn2CppDateTimeOffset dn2cpp_datetimeoffset_utc_now();

// System.DateOnly. A day number (days since 0001-01-01); the date
// components reuse the DateTime proleptic-Gregorian calendar helpers (ticks = dayNumber
// * ticks-per-day). DayOfWeek is 0=Sunday..6=Saturday; DayOfYear is 1-based. AddMonths/
// AddYears clamp the day to the target month (Jan 31 + 1 month -> Feb 28), like DateTime.
Dn2CppDateOnly dn2cpp_dateonly_from_daynum(int32_t dayNumber);
Dn2CppDateOnly dn2cpp_dateonly_ymd(int32_t y, int32_t mo, int32_t d);
Dn2CppDateOnly dn2cpp_dateonly_from_datetime(Dn2CppDateTime dt);
int32_t dn2cpp_dateonly_year(Dn2CppDateOnly d);
int32_t dn2cpp_dateonly_month(Dn2CppDateOnly d);
int32_t dn2cpp_dateonly_day(Dn2CppDateOnly d);
int32_t dn2cpp_dateonly_dayofweek(Dn2CppDateOnly d);
int32_t dn2cpp_dateonly_dayofyear(Dn2CppDateOnly d);
Dn2CppDateOnly dn2cpp_dateonly_add_days(Dn2CppDateOnly d, int32_t days);
Dn2CppDateOnly dn2cpp_dateonly_add_months(Dn2CppDateOnly d, int32_t months);
Dn2CppDateOnly dn2cpp_dateonly_add_years(Dn2CppDateOnly d, int32_t years);
int32_t dn2cpp_dateonly_cmp(Dn2CppDateOnly a, Dn2CppDateOnly b);
int32_t dn2cpp_dateonly_hash(Dn2CppDateOnly d); // == _dayNumber (matches DateOnly.GetHashCode)
// Default ToString(): the InvariantCulture short-date pattern "MM/dd/yyyy" (the "d" format).
Dn2CppString* dn2cpp_dateonly_to_string(Dn2CppDateOnly d);
// ToString(format) — InvariantCulture. Standard date specifiers d/D/M/m/O/o/R/r/Y/y expand
// to their invariant date-only patterns (unlike DateTime, no time component); anything longer
// is a custom format rendered by the shared date/time formatter (time specifiers render as 0 —
// a carve-out; .NET DateOnly throws on them). null/empty == "d".
Dn2CppString* dn2cpp_dateonly_format(Dn2CppDateOnly d, Dn2CppString* fmt);
// Parse / TryParse / ParseExact — InvariantCulture. Reuse the DateTime parser and keep the
// date part (a time component in the input is accepted and dropped — a carve-out; .NET throws).
bool dn2cpp_dateonly_try_parse(Dn2CppString* s, Dn2CppDateOnly* out);
bool dn2cpp_dateonly_try_parse_exact(Dn2CppString* s, Dn2CppString* fmt, Dn2CppDateOnly* out);
Dn2CppDateOnly dn2cpp_dateonly_parse(Dn2CppString* s);
Dn2CppDateOnly dn2cpp_dateonly_parse_exact(Dn2CppString* s, Dn2CppString* fmt);

// System.TimeOnly. A time within a day, stored as a tick count since
// midnight (0..863999999999). Component getters reuse the tick scale; Add* wrap within
// 24h; TimeOnly - TimeOnly is the wrapped elapsed TimeSpan ((a-b+day)%day).
Dn2CppTimeOnly dn2cpp_timeonly_from_ticks(int64_t ticks);
Dn2CppTimeOnly dn2cpp_timeonly_hms(int32_t h, int32_t m, int32_t s);
Dn2CppTimeOnly dn2cpp_timeonly_hmsms(int32_t h, int32_t m, int32_t s, int32_t ms);
Dn2CppTimeOnly dn2cpp_timeonly_from_timespan(Dn2CppTimeSpan ts);
Dn2CppTimeOnly dn2cpp_timeonly_from_datetime(Dn2CppDateTime dt);
int32_t dn2cpp_timeonly_hour(Dn2CppTimeOnly t);
int32_t dn2cpp_timeonly_minute(Dn2CppTimeOnly t);
int32_t dn2cpp_timeonly_second(Dn2CppTimeOnly t);
int32_t dn2cpp_timeonly_millisecond(Dn2CppTimeOnly t);
Dn2CppTimeOnly dn2cpp_timeonly_add(Dn2CppTimeOnly t, int64_t deltaTicks);          // wraps within 24h
Dn2CppTimeOnly dn2cpp_timeonly_add_unit(Dn2CppTimeOnly t, double value, int64_t ticksPerUnit);
Dn2CppTimeSpan dn2cpp_timeonly_diff(Dn2CppTimeOnly a, Dn2CppTimeOnly b);            // (a-b+day)%day
int32_t dn2cpp_timeonly_cmp(Dn2CppTimeOnly a, Dn2CppTimeOnly b);
int32_t dn2cpp_timeonly_hash(Dn2CppTimeOnly t);
// Default ToString(): the InvariantCulture short-time pattern "HH:mm" (the "t" format).
Dn2CppString* dn2cpp_timeonly_to_string(Dn2CppTimeOnly t);
// ToString(format) — InvariantCulture. Standard t/T/o/O/r/R expand to their invariant
// time patterns; anything longer is a custom format rendered by the shared formatter.
Dn2CppString* dn2cpp_timeonly_format(Dn2CppTimeOnly t, Dn2CppString* fmt);
// Parse / TryParse / ParseExact — InvariantCulture. Parse reads [H]H:mm[:ss[.fffffff]] with an
// optional AM/PM; ParseExact reuses the DateTime custom parser with a fixed date prefix.
bool dn2cpp_timeonly_try_parse(Dn2CppString* s, Dn2CppTimeOnly* out);
bool dn2cpp_timeonly_try_parse_exact(Dn2CppString* s, Dn2CppString* fmt, Dn2CppTimeOnly* out);
Dn2CppTimeOnly dn2cpp_timeonly_parse(Dn2CppString* s);
Dn2CppTimeOnly dn2cpp_timeonly_parse_exact(Dn2CppString* s, Dn2CppString* fmt);
// Enum numeric fallback (no member name matched, or a numeric specifier). Unlike
// a plain integer, an enum's hex zero-pads to the underlying type's width (2 hex
// digits per byte): `((Color)2).ToString("X")` is "00000002" for an int-backed
// enum. `byteWidth`/`isSigned` describe the underlying type.
Dn2CppString* dn2cpp_format_enum(int64_t v, int32_t byteWidth, int32_t isSigned, Dn2CppString* fmt);
// True when `fmt` is a numeric specifier (D/d/X/x) — for an enum hole this means
// "render the underlying value", not the member name.
bool dn2cpp_fmt_numeric(Dn2CppString* fmt);
// Combine the set flags of a [Flags] enum value into "A, B, C" (ascending by
// value), like .NET. `vals`/`names` are the enum's members sorted by descending
// unsigned value. Returns null when the value has leftover bits (caller renders
// the decimal) or is 0 with no zero-named member.
Dn2CppString* dn2cpp_enum_flags_to_string(int32_t value, const int32_t* vals,
                                          Dn2CppString* const* names, int32_t count);
// The 64-bit twin, for a long/ulong-underlying [Flags] enum.
Dn2CppString* dn2cpp_enum_flags_to_string64(int64_t value, const int64_t* vals,
                                            Dn2CppString* const* names, int32_t count);
// A single UTF-16 code unit as a length-1 string (a `char` hole).
Dn2CppString* dn2cpp_char_to_string(char16_t c);
// A UTF-32 scalar as a string: one code unit for the BMP, a surrogate pair
// above it (char.ConvertFromUtf32; no range validation — see the intrinsic).
Dn2CppString* dn2cpp_string_from_utf32(int32_t cp);
// char classification over the full BMP. IsControl is C0/DEL/C1 (exactly the
// Unicode Cc set); IsPunctuation/IsSymbol test the P*/S* categories via the
// generated dn2cpp_char_unicode_category table; IsWhiteSpace is the .NET
// definition (Zs/Zl/Zp plus U+0009-000D and U+0085) — distinct from the
// ASCII-only dn2cpp_is_ws the numeric parsers trim with. GetNumericValue is
// the generated BMP table in dn2cpp_char_numeric.cpp (-1.0 = no value).
int32_t dn2cpp_char_is_control(char16_t c);
int32_t dn2cpp_char_is_punctuation(char16_t c);
int32_t dn2cpp_char_is_symbol(char16_t c);
// IsWhiteSpace is defined INLINE (no build uses LTO): the Trim / split /
// whitespace-scan loops call it per character, and the Latin-1 arm — the hot
// one — folds to a few compares at the call site.
inline int32_t dn2cpp_char_is_whitespace(char16_t c)
{
    // The .NET char.IsWhiteSpace definition: the separator categories
    // Zs/Zl/Zp plus the Cc members U+0009-000D and U+0085. Latin-1 is an
    // explicit set (only U+0020 and U+00A0 are Zs there); above it the
    // category table decides.
    if (c <= 0xFF)
        return (c == u' ' || (c >= u'\t' && c <= u'\r') || c == 0x85 || c == 0xA0) ? 1 : 0;
    int32_t cat = dn2cpp_char_unicode_category(c);
    return (cat >= 11 && cat <= 13) ? 1 : 0;
}
double dn2cpp_char_get_numeric_value(char16_t c);
// Internal ASCII-whitespace test (space/tab/newline/CR/FF/VT) used by the
// numeric-parse scanners — deliberately distinct from char.IsWhiteSpace
// (.NET's number parser trims ASCII-only whitespace). Shared so the
// per-namespace intrinsic units (e.g. Convert parsing) scan identically.
bool dn2cpp_is_ws(char16_t c);
// Finalize: produce the Dn2CppString and release the builder's buffer.
Dn2CppString* dn2cpp_isb_to_string(Dn2CppISB* h);

// System.Text.StringBuilder — a heap (reference) growable UTF-16 buffer, modeled
// as an intrinsic rather than transpiling the real chunked corelib type (whose
// Append reaches Buffer.Memmove/InternalCall). ToString copies (does not reset),
// and the mutating members return the builder for fluent chaining.
struct Dn2CppStringBuilder : Dn2CppObject
{
    char16_t* buf;
    int32_t length;
    int32_t capacity;
};
extern const Dn2CppTypeInfo dn2cpp_stringbuilder_type;
Dn2CppStringBuilder* dn2cpp_sb_new();
// Capacity-aware ctors: new(int) and new(string, int). The Capacity is .NET's
// initial buffer size — new(0)/new() -> 16, new(n>0) -> n, new(str) -> max(16,
// len), new(str, n) -> max(n, len). Modeled because a P/Invoke StringBuilder
// passes sb.Capacity as the native buffer length.
Dn2CppStringBuilder* dn2cpp_sb_new_cap(int32_t capacity);
Dn2CppStringBuilder* dn2cpp_sb_new_str(Dn2CppString* s);
Dn2CppStringBuilder* dn2cpp_sb_new_str_cap(Dn2CppString* s, int32_t capacity);
Dn2CppStringBuilder* dn2cpp_sb_append_str(Dn2CppStringBuilder* sb, Dn2CppString* s);
Dn2CppStringBuilder* dn2cpp_sb_append_char(Dn2CppStringBuilder* sb, char16_t c);
// Appends Environment.NewLine ("\r\n" on Windows, "\n" elsewhere) — used by the
// AppendLine() family instead of a single append_char('\n'), so StringBuilder's
// line terminator matches Console.WriteLine's on every platform.
Dn2CppStringBuilder* dn2cpp_sb_append_newline(Dn2CppStringBuilder* sb);
Dn2CppString* dn2cpp_sb_tostring(Dn2CppStringBuilder* sb);
int32_t dn2cpp_sb_length(Dn2CppStringBuilder* sb);
int32_t dn2cpp_sb_capacity(Dn2CppStringBuilder* sb);
// StringBuilder.CopyTo(int sourceIndex, char[] destination, int destinationIndex,
// int count): bounds-checked copy of `count` code units from the builder into the
// caller char[]. Raises the BCL's catchable ArgumentOutOfRange (a negative index/count)
// / ArgumentException (source or destination overrun) like the managed member.
void dn2cpp_sb_copy_to(Dn2CppStringBuilder* sb, int32_t sourceIndex, Dn2CppArrayN* dest,
                       int32_t destinationIndex, int32_t count);
Dn2CppStringBuilder* dn2cpp_sb_clear(Dn2CppStringBuilder* sb);
// Insert (string/char at index), Remove (count chars at start) and Replace
// (char or string, all occurrences) over the growable UTF-16 buffer; validation
// failures throw the catchable managed exception the BCL member specifies.
Dn2CppStringBuilder* dn2cpp_sb_insert_str(Dn2CppStringBuilder* sb, int32_t index, Dn2CppString* value);
Dn2CppStringBuilder* dn2cpp_sb_insert_char(Dn2CppStringBuilder* sb, int32_t index, char16_t c);
Dn2CppStringBuilder* dn2cpp_sb_remove(Dn2CppStringBuilder* sb, int32_t start, int32_t count);
Dn2CppStringBuilder* dn2cpp_sb_replace_char(Dn2CppStringBuilder* sb, char16_t oldc, char16_t newc);
Dn2CppStringBuilder* dn2cpp_sb_replace_str(Dn2CppStringBuilder* sb, Dn2CppString* oldValue, Dn2CppString* newValue);
// The remaining StringBuilder surface: range Replace (a match must lie fully
// inside [startIndex, startIndex + count)), raw char-run / char[]-slice /
// StringBuilder Appends, repeated Insert, the Length setter (a grow zero-pads),
// the indexer (the getter throws IndexOutOfRange, the setter ArgumentOutOfRange
// — .NET's asymmetry) and grow-only exact-size EnsureCapacity.
Dn2CppStringBuilder* dn2cpp_sb_replace_str_range(Dn2CppStringBuilder* sb, Dn2CppString* oldValue,
                                                 Dn2CppString* newValue, int32_t startIndex, int32_t count);
Dn2CppStringBuilder* dn2cpp_sb_replace_char_range(Dn2CppStringBuilder* sb, char16_t oldc, char16_t newc,
                                                  int32_t startIndex, int32_t count);
Dn2CppStringBuilder* dn2cpp_sb_append_chars(Dn2CppStringBuilder* sb, const char16_t* chars, int32_t count);
// (char[] value, int startIndex, int charCount) slice validation shared by the
// Append/Insert array-slice overloads, materialized as a string: null is valid
// only as (null, 0, 0) (else ArgumentNullException), a bad slice is
// ArgumentOutOfRangeException.
Dn2CppString* dn2cpp_sb_char_arr_str(Dn2CppArrayN* arr, int32_t startIndex, int32_t charCount);
Dn2CppStringBuilder* dn2cpp_sb_append_sb(Dn2CppStringBuilder* sb, Dn2CppStringBuilder* value);
Dn2CppStringBuilder* dn2cpp_sb_append_sb_range(Dn2CppStringBuilder* sb, Dn2CppStringBuilder* value,
                                               int32_t startIndex, int32_t count);
Dn2CppStringBuilder* dn2cpp_sb_insert_str_count(Dn2CppStringBuilder* sb, int32_t index,
                                                Dn2CppString* value, int32_t count);
void dn2cpp_sb_set_length(Dn2CppStringBuilder* sb, int32_t value);
char16_t dn2cpp_sb_get_char(Dn2CppStringBuilder* sb, int32_t index);
void dn2cpp_sb_set_char(Dn2CppStringBuilder* sb, int32_t index, char16_t value);
int32_t dn2cpp_sb_ensure_capacity(Dn2CppStringBuilder* sb, int32_t capacity);

// P/Invoke StringBuilder marshalling. A StringBuilder argument
// is a caller-allocated [In,Out] buffer the native fills (the Win32 idiom, e.g.
// GetWindowText(sb, sb.Capacity)). _to_buffer copies the builder's current
// content into a GC-allocated, NUL-terminated native buffer sized for its
// Capacity (so the native can write up to Capacity chars); _from_buffer reads
// the (possibly mutated) buffer back up to its NUL and replaces the builder's
// content. Ansi (unicode == 0) marshals as UTF-8 on Unix, CharSet.Unicode
// (unicode != 0) as UTF-16, mirroring the string codecs. The buffer is
// GC-scanned, so no caller free; null builder -> null pointer.
void* dn2cpp_pinvoke_sb_to_buffer(Dn2CppStringBuilder* sb, int32_t unicode);
void dn2cpp_pinvoke_sb_from_buffer(Dn2CppStringBuilder* sb, void* buf, int32_t unicode);

// Hot update: load a Baked Patch Image (docs/BPI-FORMAT.md) and run its entry
// method in the runtime IL interpreter. Lowered from
// Dn2Cpp.Runtime.HotUpdate.Run(string). Defined in dn2cpp_interp.cpp, which the
// static archive only links into programs that call this.
void dn2cpp_hotupdate_run(Dn2CppString* bpiPath);

// Hot-update deployment drivers (lowered from Dn2Cpp.Runtime.HotUpdate.Load /
// LoadDirectory). _load loads a single BPI like _run but treats the entry
// method as optional: it runs only if present (a registration-only BPI is a
// valid deployment unit). _load_dir enumerates the regular `*.bpi` files
// (byte-wise, lowercase) of a directory, skips — with a stderr diagnostic —
// any whose header fails pre-validation (length / magic / formatVersion /
// baseImageAbiHash: stale patches after a base update are a routine state and
// must never block startup), then loads the survivors in ascending
// (patchVersion, byte-wise filename) order — the highest version registers
// last and wins name lookups — running each entry method if present, and
// returns the number of BPIs loaded. A missing or unopenable directory is the
// fresh-install state: returns 0. Failures past header pre-validation throw
// like a single-file load, leaving the earlier loads published (loading is
// per-BPI atomic, append-only).
void dn2cpp_hotupdate_load(Dn2CppString* bpiPath);
int32_t dn2cpp_hotupdate_load_dir(Dn2CppString* dirPath);

// Hot update: optional external allocation hook for the interpreter's `newobj`
// of a patch-declared type. An embedding layer registers it when fresh patch
// instances may need host-side backing the plain allocation path cannot
// provide — the Godot GDExtension layer backs a patch type deriving from a
// ClassDB-registered class with an engine object, mirroring what the AOT
// `new` path emits inline. The hook runs BEFORE the ctor chain (host state
// must exist when the ctor body runs); it must allocate GC-visible storage of
// `size` bytes and stamp `type` into the header, or return null to decline —
// the interpreter then performs its default GC allocation. Finalizer
// registration stays with the interpreter either way (driven by the
// type-info's inherited finalize entry). Defined in dn2cpp_gc.cpp so
// registering the hook never links the interpreter TU into a program that
// does not use hot update.
extern Dn2CppObject* (*dn2cpp_interp_alloc_hook)(const Dn2CppTypeInfo* type, size_t size);
void dn2cpp_interp_set_alloc_hook(Dn2CppObject* (*hook)(const Dn2CppTypeInfo* type, size_t size));

// One interpreter eval-stack slot as seen across the N2M trampoline boundary
// (the public mirror of the interpreter's internal slot union): i32 values are
// sign-extended into `i`, f32 values widened into `f`, references live in
// `ref`. Trampoline argument arrays are locals on the native stack, so their
// reference slots stay visible to the conservative collector.
union Dn2CppInterpSlot
{
    int64_t i;
    double f;
    void* ref;
};

// One pre-emitted N2M vtable trampoline: a native function with the AOT C++
// ABI of a base-image vtable slot whose body forwards into the interpreter
// (dn2cpp_interp_vcall). A --hotupdate-base build emits one per distinct
// (slot, signature shape) pair over the emitted vtables plus this registration
// table; the patch loader looks bridges up here when it applies a patch type's
// vtable overrides. The baked slot number is safe to freeze because every
// vtable slot's signature is hashed into the base-image ABI contract — a base
// rebuild that moves slots invalidates stale patches mechanically.
struct Dn2CppN2MTrampoline
{
    int32_t slot;         // the base-image vtable slot the bridge serves
    const char* sigShape; // "(paramTypes):retType" (the BPI sigShape rendering)
    const void* fn;       // the trampoline function
};

// Dispatches a patched vtable slot into the interpreter: called only from the
// emitted N2M trampolines, with args[0] = the receiver (an instance of a
// loader-constructed patch type) followed by the marshalled arguments.
// Resolves (receiver type-info, slot) to the overriding patch method and runs
// it; returns the method's result slot (zero for void). Defined in
// dn2cpp_interp.cpp (only hot-update-enabled programs link it).
Dn2CppInterpSlot dn2cpp_interp_vcall(int32_t slot, const Dn2CppInterpSlot* args, int32_t argCount);

// One pre-emitted N2M interface trampoline: a native function with the AOT C++
// ABI of a base-image interface method slot whose body forwards into the
// interpreter (dn2cpp_interp_itfcall). A --hotupdate-base build emits one per
// distinct (emitted interface, method slot) pair with a bridgeable signature,
// plus this registration table; the patch loader looks a bridge up here by
// (interface type-info, slot) when it builds a patch type's interface-dispatch
// map. Unlike vtable slots, an interface method's identity is (interface, slot)
// — the interface pointer disambiguates two interfaces sharing a slot number —
// so both are baked into the trampoline body and matched here.
struct Dn2CppN2MItfTrampoline
{
    const Dn2CppTypeInfo* itf; // the interface the bridge's method slot belongs to
    int32_t slot;              // the method slot within that interface
    const void* fn;            // the trampoline function
};

// Dispatches a patch type's interface-method implementation into the
// interpreter: called only from the emitted N2M interface trampolines, with
// args[0] = the receiver (an instance of a loader-constructed patch type)
// followed by the marshalled arguments. Resolves (receiver type-info, itf,
// slot) to the implementing patch method and runs it; returns the method's
// result slot (zero for void). Defined in dn2cpp_interp.cpp.
Dn2CppInterpSlot dn2cpp_interp_itfcall(const Dn2CppTypeInfo* itf, int32_t slot,
    const Dn2CppInterpSlot* args, int32_t argCount);

// One pre-emitted delegate bridge row, keyed by the delegate's type-info. A
// --hotupdate-base build emits one per bridgeable emitted delegate type into
// two registration tables (a delegate's Invoke signature is not a vtable/
// interface slot, so the type-info pointer disambiguates), each with a matching
// interp bridge function:
//   dn2cpp_n2m_delegate_thunks[] — the f_method a patch method bound into a
//   delegate carries: a native function with the delegate's Invoke C++ ABI
//   (a leading Dn2CppObject* target = the interpreter closure) that packs the
//   invoke arguments into interpreter slots and forwards into
//   dn2cpp_interp_dgcall (the closure carries the image + method + bound this).
//   dn2cpp_dg_invoke_bridges[] — the reverse: an interpreted Invoke on a
//   delegate hands its receiver + argument slots here, and the bridge unpacks
//   the slots into the Invoke C++ ABI and calls the delegate's own emitted
//   invoker (walking the multicast chain, dispatching each f_method) —
//   so a delegate wrapping either an interpreted or an AOT method invokes
//   correctly from patch code.
struct Dn2CppN2MDelegate
{
    const Dn2CppTypeInfo* dg; // the delegate type-info the bridge serves
    const void* fn;           // the bridge function
};

// The interpreter dispatch a delegate thunk forwards into: `closure` is the
// loader-built closure carried as the delegate's target (image + patch method
// index + bound `this`), `args` the marshalled invoke arguments (excluding the
// bound this). Prepends the bound this for an instance patch method, runs the
// method, and returns its result slot (zero for void). Defined in
// dn2cpp_interp.cpp (only hot-update-enabled programs link it).
Dn2CppInterpSlot dn2cpp_interp_dgcall(Dn2CppObject* closure,
    const Dn2CppInterpSlot* args, int32_t argCount);

void dn2cpp_console_writeline_empty();
void dn2cpp_console_writeline_str(Dn2CppString* s);
void dn2cpp_console_writeline_i4(int32_t v);
void dn2cpp_console_writeline_i8(int64_t v);
void dn2cpp_console_writeline_r8(double v);
void dn2cpp_console_writeline_r4(float v);
void dn2cpp_console_writeline_bool(int32_t v);
void dn2cpp_console_write_str(Dn2CppString* s);
void dn2cpp_console_write_i4(int32_t v);
void dn2cpp_console_write_i8(int64_t v);
void dn2cpp_console_write_r8(double v);
void dn2cpp_console_write_r4(float v);
void dn2cpp_console_write_bool(int32_t v);

// Console.Error is the process stderr TextWriter. A TextWriter is an
// opaque handle carrying the destination FILE* stream (defined in the runtime .cpp);
// console code only passes it back to these dn2cpp_textwriter_* helpers — never
// dereferences it — so a forward declaration suffices in this public header (no
// <cstdio>). These mirror the dn2cpp_console_* (stdout) family above byte-for-byte,
// writing to the writer's stream instead, so Console.Error.Write/WriteLine produce the
// same text as Console.Write/WriteLine.
struct Dn2CppTextWriter;
Dn2CppTextWriter* dn2cpp_console_error();
void dn2cpp_textwriter_writeline_empty(Dn2CppTextWriter* w);
void dn2cpp_textwriter_writeline_str(Dn2CppTextWriter* w, Dn2CppString* s);
void dn2cpp_textwriter_writeline_i4(Dn2CppTextWriter* w, int32_t v);
void dn2cpp_textwriter_writeline_i8(Dn2CppTextWriter* w, int64_t v);
void dn2cpp_textwriter_writeline_r8(Dn2CppTextWriter* w, double v);
void dn2cpp_textwriter_writeline_r4(Dn2CppTextWriter* w, float v);
void dn2cpp_textwriter_writeline_bool(Dn2CppTextWriter* w, int32_t v);
void dn2cpp_textwriter_write_str(Dn2CppTextWriter* w, Dn2CppString* s);
void dn2cpp_textwriter_write_i4(Dn2CppTextWriter* w, int32_t v);
void dn2cpp_textwriter_write_i8(Dn2CppTextWriter* w, int64_t v);
void dn2cpp_textwriter_write_r8(Dn2CppTextWriter* w, double v);
void dn2cpp_textwriter_write_r4(Dn2CppTextWriter* w, float v);
void dn2cpp_textwriter_write_bool(Dn2CppTextWriter* w, int32_t v);
