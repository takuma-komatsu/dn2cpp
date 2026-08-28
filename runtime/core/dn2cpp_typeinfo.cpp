// dn2cpp_typeinfo.cpp — type metadata of the dn2cpp runtime:
// built-in type-infos + their interned Type
// companions, the Type intern table, the type registry, and reflection-lite.

#include "dn2cpp_core.h"
#include <algorithm>
#include <limits>  // the float/double primitives' Epsilon/MaxValue/NaN/Infinity field rows
#include <chrono>  // DateTime.Now/UtcNow wall clock
#include <ctime>   // localtime_r for DateTime.Now local components
#include <string>  // Path/File/Environment helpers
#include <vector>  // Path.GetFullPath segment stack
#ifndef _WIN32
#include <dlfcn.h> // dlsym for dn2cpp_native_library_get_symbol
#endif
#if defined(DN2CPP_USE_BOEHM_GC) && defined(__APPLE__)
#include <dlfcn.h>          // dladdr — locate the image holding the runtime
#include <mach-o/getsect.h> // getsegmentdata — its __DATA segment bounds
#include <mach-o/loader.h>  // mach_header_64
#endif
#include <cstdio>   // File read/write (fopen/fread/fwrite)
#include <cerrno>   // File.Delete errno/ENOENT
#include <cstdlib>  // Environment.GetEnvironmentVariable getenv
#include <cstring>  // Type.Assembly identity name compare (dn2cpp_assembly_equals)
#ifdef _MSC_VER
#include <malloc.h> // _aligned_malloc/_aligned_realloc/_aligned_free (no std::aligned_alloc on MSVC)
#endif
#ifdef _WIN32
// GetLastError() for dn2cpp_pinvoke_capture_last_error. NOMINMAX / WIN32_LEAN_AND_MEAN
// are project-wide compile definitions (runtime/CMakeLists.txt's MSVC arm), not
// repeated here — see platform/windows/dn2cpp_pal_windows.cpp's header comment.
#include <windows.h>
#include <io.h>     // _setmode / _fileno — put stdout/stderr in binary mode at init
#include <fcntl.h>  // _O_BINARY
#endif
#include <thread>              // real OS threads (Thread, worker pool)
#include <mutex>               // scheduler run-queue + task-completion + console locks
#include <condition_variable> // task_block sleeps until a cross-thread completion
#include <atomic>             // g_inflight_async_tasks, real Interlocked
#include <deque>             // Task.Run worker-pool work queue
#include <new>               // placement new (in-place ctor of a GC-allocated Timer)
#include <unordered_map>     // Dn2CppType intern table (one Type per type-info handle)

// Allocation-size query for NativeMemory.AlignedRealloc routes through the PAL
// (malloc_size on macOS, malloc_usable_size on glibc/musl/BSD).
#include "platform/dn2cpp_pal.h"

// Built-in type metadata.
// Each static type-info bakes its interned Type companion in (lock-free typeof/GetType).
extern const Dn2CppType dn2cpp_object_type_obj;
const Dn2CppTypeInfo dn2cpp_object_type =
    dn2cpp_ti_with_typeobject({ "System.Object", nullptr, 0, nullptr, nullptr, 0 }, &dn2cpp_object_type_obj);
const Dn2CppType dn2cpp_object_type_obj = { { &dn2cpp_type_type }, &dn2cpp_object_type };

// EqualityComparer<T>.Default is read once per element on the dictionary-probe and
// span-scan paths, so the cache must be readable without a lock: an APPEND-ONLY list
// published through an atomic head. A node is immutable once published and is never
// freed — a concurrent reader may be walking it — and the list holds one node per
// closed IEqualityComparer<T> the program reaches, so the walk is over a handful.
namespace {
struct Dn2CppDefaultComparerNode
{
    const Dn2CppTypeInfo* genericInterface;
    Dn2CppObject* comparer;
    Dn2CppDefaultComparerNode* next;
};
}
static std::atomic<Dn2CppDefaultComparerNode*> g_default_equality_comparers{ nullptr };
static std::mutex& g_default_equality_comparer_lock = dn2cpp_never_destroyed<std::mutex>();

static Dn2CppObject* dn2cpp_find_default_equality_comparer(const Dn2CppDefaultComparerNode* head,
                                                           const Dn2CppTypeInfo* genericInterface)
{
    for (const auto* n = head; n != nullptr; n = n->next)
        if (n->genericInterface == genericInterface)
            return n->comparer;
    return nullptr;
}

Dn2CppObject* dn2cpp_default_equality_comparer(const Dn2CppTypeInfo* comparerType,
                                               const Dn2CppTypeInfo* genericInterface,
                                               const Dn2CppTypeInfo* nongenericInterface)
{
    if (auto* hit = dn2cpp_find_default_equality_comparer(
            g_default_equality_comparers.load(std::memory_order_acquire), genericInterface))
        return hit;
    if (comparerType == nullptr)
        comparerType = &dn2cpp_object_type;
    std::lock_guard<std::mutex> guard(g_default_equality_comparer_lock);
    // Re-check under the lock: one comparer per closed interface, for the whole
    // process — callers compare the returned reference by identity.
    if (auto* hit = dn2cpp_find_default_equality_comparer(
            g_default_equality_comparers.load(std::memory_order_relaxed), genericInterface))
        return hit;
    auto* interfaceEntries = new Dn2CppInterfaceEntry[2]{
        { genericInterface, nullptr }, { nongenericInterface, nullptr }
    };
    auto* concreteType = new Dn2CppTypeInfo{};
    const char* elementName = genericInterface->genericArgCount == 1
        && genericInterface->genericArgs != nullptr
        && genericInterface->genericArgs[0] != nullptr
        ? genericInterface->genericArgs[0]->name : "?";
    std::string concreteName = "System.Collections.Generic.DefaultEqualityComparer<";
    concreteName += elementName;
    concreteName += ">";
    auto* ownedName = new char[concreteName.size() + 1];
    std::memcpy(ownedName, concreteName.c_str(), concreteName.size() + 1);
    concreteType->name = ownedName;
    concreteType->base = comparerType;
    concreteType->instanceSize = (int32_t)sizeof(Dn2CppObject);
    concreteType->interfaces = interfaceEntries;
    concreteType->interfaceCount = 2;
    // The bit is what dn2cpp_is_default_equality_comparer reads; nothing else mints
    // a type-info carrying it, so the bit and this object's identity coincide.
    concreteType->flags = DN2CPP_TF_SEALED | DN2CPP_TF_DEFAULT_EQ_COMPARER;
    auto* comparer = new Dn2CppObject{ concreteType };
    auto* node = new Dn2CppDefaultComparerNode{
        genericInterface, comparer, g_default_equality_comparers.load(std::memory_order_relaxed)
    };
    // Release: the node's fields and the comparer's type-info must be visible to a
    // reader that acquires this head.
    g_default_equality_comparers.store(node, std::memory_order_release);
    return comparer;
}
// String's one public field, real .NET's whole surface for it (the argument for
// hand-writing an owned handle's field table is at dn2cpp_primflds_bool).
static Dn2CppObject* dn2cpp_ownfld_string_Empty(Dn2CppObject*)
{ return reinterpret_cast<Dn2CppObject*>(dn2cpp_string_literal(u"", 0)); }
static const Dn2CppFieldInfo dn2cpp_ownflds_string[] = {
    { "Empty", &dn2cpp_string_type, &dn2cpp_string_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_INITONLY,
      dn2cpp_ownfld_string_Empty, nullptr, nullptr, 0, 0x36, 0 },
};
// String is a sealed reference type — carries SEALED (not VALUETYPE). Non-const
// (alone among the built-ins): its interface rows point at program-specific
// transpiled CoreLib IL, so the generated init prologue wires them in at startup.
extern const Dn2CppType dn2cpp_string_type_obj;
Dn2CppTypeInfo dn2cpp_string_type =
    dn2cpp_ti_with_typeobject({ "System.String", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, DN2CPP_TF_SEALED, dn2cpp_ownflds_string, 1 }, &dn2cpp_string_type_obj);
const Dn2CppType dn2cpp_string_type_obj = { { &dn2cpp_type_type }, &dn2cpp_string_type };

void dn2cpp_string_set_interfaces(const Dn2CppInterfaceEntry* entries, int32_t count)
{
    dn2cpp_string_type.interfaces = entries;
    dn2cpp_string_type.interfaceCount = count;
}

// The same wiring for the intrinsic types that model a disposable CLR type: one setter
// over an explicit type-info handle instead of one named setter per type.
// The handles live with their own runtime helpers (dn2cpp_threading.cpp and the
// System.Threading / concurrent intrinsics); each is non-const for this reason alone.
void dn2cpp_intrinsic_set_interfaces(Dn2CppTypeInfo* type, const Dn2CppInterfaceEntry* entries, int32_t count)
{
    type->interfaces = entries;
    type->interfaceCount = count;
}
// The array type-infos carry DN2CPP_TF_ARRAY so Type.IsArray answers true via the
// flag path (all other hand-written type-infos leave flags 0). The middle slots
// (tostring/gethashcode/equals) are spelled out as nullptr to reach the flags
// field. The trailing elementType/arrayRank give the shared handles
// their real element (int32 / object) and rank 1, so a runtime-internal array headed
// by one casts compatibly with the precise per-element handles (dn2cpp_array_elem_
// assignable compares element handles, not array handles) and reports GetElementType /
// GetArrayRank. The 18 metadata slots between flags and elementType are 0.
extern const Dn2CppType dn2cpp_array_i4_type_obj;
const Dn2CppTypeInfo dn2cpp_array_i4_type =
    dn2cpp_ti_with_typeobject({ "System.Int32[]", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_ARRAY | DN2CPP_TF_SEALED), nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, 0, nullptr, 0, &dn2cpp_int32_type, 1 }, &dn2cpp_array_i4_type_obj);
const Dn2CppType dn2cpp_array_i4_type_obj = { { &dn2cpp_type_type }, &dn2cpp_array_i4_type };
extern const Dn2CppType dn2cpp_array_ref_type_obj;
const Dn2CppTypeInfo dn2cpp_array_ref_type =
    dn2cpp_ti_with_typeobject({ "System.Object[]", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_ARRAY | DN2CPP_TF_SEALED), nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, 0, nullptr, 0, &dn2cpp_object_type, 1 }, &dn2cpp_array_ref_type_obj);
const Dn2CppType dn2cpp_array_ref_type_obj = { { &dn2cpp_type_type }, &dn2cpp_array_ref_type };
// The imprecise PACKED handle: a runtime-allocated Dn2CppArrayN with no precise
// type-info. elementType stays null — deliberately, the unknown must read as unknown:
// dn2cpp_array_ref_type here would claim the ArrayRef layout to every layout reader
// (dn2cpp_is_ref_array / dn2cpp_pinned_data_addr / dn2cpp_array_rep_dyn), and any
// "X[]" name or element would be a wrong answer some reader trusts. The name is the
// abstract base's — unreachable from a real GetType() in .NET, so it reads as the
// sentinel it is. Readers fail closed on it: casts match identity only, the ref-fallback
// dispatch skips it, Buffer's DYN verdict refuses it (dn2cpp_blockcopy_rep_dyn).
extern const Dn2CppType dn2cpp_array_n_type_obj;
const Dn2CppTypeInfo dn2cpp_array_n_type =
    dn2cpp_ti_with_typeobject({ "System.Array", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_ARRAY | DN2CPP_TF_SEALED), nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, 0, nullptr, 0, nullptr, 1 }, &dn2cpp_array_n_type_obj);
const Dn2CppType dn2cpp_array_n_type_obj = { { &dn2cpp_type_type }, &dn2cpp_array_n_type };
// ---- The primitives' reflection field tables ----
//
// The fourteen DN2CPP_TF_PRIMITIVE handles below are runtime-OWNED: the emitter
// references them and mints no rival ti_, so without these rows
// `typeof(short).GetField("MaxValue")` answers null and the natural next line,
// `.FieldType`, is an NRE in a shipped game. Hand-written rather than bound from
// emitted metadata for two reasons:
//
//   - Eight of the fourteen are intrinsic types the emitter deliberately gives no
//     metadata, so a bind would fill exactly half the set — one primitive answering
//     while another does not is worse than neither.
//   - Every row is a LITERAL (or a static readonly), which has no storage to
//     thunk-read: the emitter renders no getter, so even the bindable half would
//     answer InvalidOperationException from GetValue rather than a value.
//
// Hand-writing also makes the answer a fact about the CLR rather than about the
// program — these tables do not move with the load set or with tree-shaking, so a gate
// may diff them against real .NET. Values are boxed at dn2cpp's model width
// (char/byte/sbyte/short/ushort as int32_t), which every boxed-primitive reader
// assumes. The setter stays null on every row: SetValue then raises
// InvalidOperationException (real .NET raises FieldAccessException — a declared
// divergence, not a silence, since the call still fails).
static Dn2CppObject* dn2cpp_primfld_bool_FalseString(Dn2CppObject*)
{ return reinterpret_cast<Dn2CppObject*>(dn2cpp_string_literal(u"False", 5)); }
static Dn2CppObject* dn2cpp_primfld_bool_TrueString(Dn2CppObject*)
{ return reinterpret_cast<Dn2CppObject*>(dn2cpp_string_literal(u"True", 4)); }
static const Dn2CppFieldInfo dn2cpp_primflds_bool[] = {
    { "FalseString", &dn2cpp_bool_type, &dn2cpp_string_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_INITONLY,
      dn2cpp_primfld_bool_FalseString, nullptr, nullptr, 0, 0x36, 0 },
    { "TrueString", &dn2cpp_bool_type, &dn2cpp_string_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_INITONLY,
      dn2cpp_primfld_bool_TrueString, nullptr, nullptr, 0, 0x36, 0 },
};
static Dn2CppObject* dn2cpp_primfld_char_MaxValue(Dn2CppObject*)
{ int32_t v = 0xFFFF; return dn2cpp_box(&dn2cpp_char_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_char_MinValue(Dn2CppObject*)
{ int32_t v = 0; return dn2cpp_box(&dn2cpp_char_type, &v, sizeof(v)); }
static const Dn2CppFieldInfo dn2cpp_primflds_char[] = {
    { "MaxValue", &dn2cpp_char_type, &dn2cpp_char_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_char_MaxValue, nullptr, nullptr, 0, 0x8056, 0 },
    { "MinValue", &dn2cpp_char_type, &dn2cpp_char_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_char_MinValue, nullptr, nullptr, 0, 0x8056, 0 },
};
static Dn2CppObject* dn2cpp_primfld_sbyte_MaxValue(Dn2CppObject*)
{ int32_t v = 127; return dn2cpp_box(&dn2cpp_sbyte_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_sbyte_MinValue(Dn2CppObject*)
{ int32_t v = -128; return dn2cpp_box(&dn2cpp_sbyte_type, &v, sizeof(v)); }
static const Dn2CppFieldInfo dn2cpp_primflds_sbyte[] = {
    { "MaxValue", &dn2cpp_sbyte_type, &dn2cpp_sbyte_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_sbyte_MaxValue, nullptr, nullptr, 0, 0x8056, 0 },
    { "MinValue", &dn2cpp_sbyte_type, &dn2cpp_sbyte_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_sbyte_MinValue, nullptr, nullptr, 0, 0x8056, 0 },
};
static Dn2CppObject* dn2cpp_primfld_byte_MaxValue(Dn2CppObject*)
{ int32_t v = 255; return dn2cpp_box(&dn2cpp_byte_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_byte_MinValue(Dn2CppObject*)
{ int32_t v = 0; return dn2cpp_box(&dn2cpp_byte_type, &v, sizeof(v)); }
static const Dn2CppFieldInfo dn2cpp_primflds_byte[] = {
    { "MaxValue", &dn2cpp_byte_type, &dn2cpp_byte_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_byte_MaxValue, nullptr, nullptr, 0, 0x8056, 0 },
    { "MinValue", &dn2cpp_byte_type, &dn2cpp_byte_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_byte_MinValue, nullptr, nullptr, 0, 0x8056, 0 },
};
static Dn2CppObject* dn2cpp_primfld_int16_MaxValue(Dn2CppObject*)
{ int32_t v = 32767; return dn2cpp_box(&dn2cpp_int16_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_int16_MinValue(Dn2CppObject*)
{ int32_t v = -32768; return dn2cpp_box(&dn2cpp_int16_type, &v, sizeof(v)); }
static const Dn2CppFieldInfo dn2cpp_primflds_int16[] = {
    { "MaxValue", &dn2cpp_int16_type, &dn2cpp_int16_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_int16_MaxValue, nullptr, nullptr, 0, 0x8056, 0 },
    { "MinValue", &dn2cpp_int16_type, &dn2cpp_int16_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_int16_MinValue, nullptr, nullptr, 0, 0x8056, 0 },
};
static Dn2CppObject* dn2cpp_primfld_uint16_MaxValue(Dn2CppObject*)
{ int32_t v = 65535; return dn2cpp_box(&dn2cpp_uint16_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_uint16_MinValue(Dn2CppObject*)
{ int32_t v = 0; return dn2cpp_box(&dn2cpp_uint16_type, &v, sizeof(v)); }
static const Dn2CppFieldInfo dn2cpp_primflds_uint16[] = {
    { "MaxValue", &dn2cpp_uint16_type, &dn2cpp_uint16_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_uint16_MaxValue, nullptr, nullptr, 0, 0x8056, 0 },
    { "MinValue", &dn2cpp_uint16_type, &dn2cpp_uint16_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_uint16_MinValue, nullptr, nullptr, 0, 0x8056, 0 },
};
static Dn2CppObject* dn2cpp_primfld_int32_MaxValue(Dn2CppObject*)
{ int32_t v = 2147483647; return dn2cpp_box(&dn2cpp_int32_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_int32_MinValue(Dn2CppObject*)
{ int32_t v = (-2147483647 - 1); return dn2cpp_box(&dn2cpp_int32_type, &v, sizeof(v)); }
static const Dn2CppFieldInfo dn2cpp_primflds_int32[] = {
    { "MaxValue", &dn2cpp_int32_type, &dn2cpp_int32_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_int32_MaxValue, nullptr, nullptr, 0, 0x8056, 0 },
    { "MinValue", &dn2cpp_int32_type, &dn2cpp_int32_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_int32_MinValue, nullptr, nullptr, 0, 0x8056, 0 },
};
static Dn2CppObject* dn2cpp_primfld_uint32_MaxValue(Dn2CppObject*)
{ int32_t v = (int32_t)0xFFFFFFFFu; return dn2cpp_box(&dn2cpp_uint32_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_uint32_MinValue(Dn2CppObject*)
{ int32_t v = 0; return dn2cpp_box(&dn2cpp_uint32_type, &v, sizeof(v)); }
static const Dn2CppFieldInfo dn2cpp_primflds_uint32[] = {
    { "MaxValue", &dn2cpp_uint32_type, &dn2cpp_uint32_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_uint32_MaxValue, nullptr, nullptr, 0, 0x8056, 0 },
    { "MinValue", &dn2cpp_uint32_type, &dn2cpp_uint32_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_uint32_MinValue, nullptr, nullptr, 0, 0x8056, 0 },
};
static Dn2CppObject* dn2cpp_primfld_int64_MaxValue(Dn2CppObject*)
{ int64_t v = 9223372036854775807LL; return dn2cpp_box(&dn2cpp_int64_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_int64_MinValue(Dn2CppObject*)
{ int64_t v = (-9223372036854775807LL - 1); return dn2cpp_box(&dn2cpp_int64_type, &v, sizeof(v)); }
static const Dn2CppFieldInfo dn2cpp_primflds_int64[] = {
    { "MaxValue", &dn2cpp_int64_type, &dn2cpp_int64_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_int64_MaxValue, nullptr, nullptr, 0, 0x8056, 0 },
    { "MinValue", &dn2cpp_int64_type, &dn2cpp_int64_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_int64_MinValue, nullptr, nullptr, 0, 0x8056, 0 },
};
static Dn2CppObject* dn2cpp_primfld_uint64_MaxValue(Dn2CppObject*)
{ int64_t v = (int64_t)0xFFFFFFFFFFFFFFFFULL; return dn2cpp_box(&dn2cpp_uint64_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_uint64_MinValue(Dn2CppObject*)
{ int64_t v = 0; return dn2cpp_box(&dn2cpp_uint64_type, &v, sizeof(v)); }
static const Dn2CppFieldInfo dn2cpp_primflds_uint64[] = {
    { "MaxValue", &dn2cpp_uint64_type, &dn2cpp_uint64_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_uint64_MaxValue, nullptr, nullptr, 0, 0x8056, 0 },
    { "MinValue", &dn2cpp_uint64_type, &dn2cpp_uint64_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_uint64_MinValue, nullptr, nullptr, 0, 0x8056, 0 },
};
static Dn2CppObject* dn2cpp_primfld_intptr_Zero(Dn2CppObject*)
{ intptr_t v = 0; return dn2cpp_box(&dn2cpp_intptr_type, &v, sizeof(v)); }
static const Dn2CppFieldInfo dn2cpp_primflds_intptr[] = {
    { "Zero", &dn2cpp_intptr_type, &dn2cpp_intptr_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_INITONLY,
      dn2cpp_primfld_intptr_Zero, nullptr, nullptr, 0, 0x36, 0 },
};
static Dn2CppObject* dn2cpp_primfld_uintptr_Zero(Dn2CppObject*)
{ intptr_t v = 0; return dn2cpp_box(&dn2cpp_uintptr_type, &v, sizeof(v)); }
static const Dn2CppFieldInfo dn2cpp_primflds_uintptr[] = {
    { "Zero", &dn2cpp_uintptr_type, &dn2cpp_uintptr_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_INITONLY,
      dn2cpp_primfld_uintptr_Zero, nullptr, nullptr, 0, 0x36, 0 },
};
static Dn2CppObject* dn2cpp_primfld_single_E(Dn2CppObject*)
{ float v = 2.7182817f; return dn2cpp_box(&dn2cpp_single_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_single_Epsilon(Dn2CppObject*)
{ float v = std::numeric_limits<float>::denorm_min(); return dn2cpp_box(&dn2cpp_single_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_single_MaxValue(Dn2CppObject*)
{ float v = std::numeric_limits<float>::max(); return dn2cpp_box(&dn2cpp_single_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_single_MinValue(Dn2CppObject*)
{ float v = (-std::numeric_limits<float>::max()); return dn2cpp_box(&dn2cpp_single_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_single_NaN(Dn2CppObject*)
{ float v = std::numeric_limits<float>::quiet_NaN(); return dn2cpp_box(&dn2cpp_single_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_single_NegativeInfinity(Dn2CppObject*)
{ float v = (-std::numeric_limits<float>::infinity()); return dn2cpp_box(&dn2cpp_single_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_single_NegativeZero(Dn2CppObject*)
{ float v = (-0.0f); return dn2cpp_box(&dn2cpp_single_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_single_Pi(Dn2CppObject*)
{ float v = 3.1415927f; return dn2cpp_box(&dn2cpp_single_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_single_PositiveInfinity(Dn2CppObject*)
{ float v = std::numeric_limits<float>::infinity(); return dn2cpp_box(&dn2cpp_single_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_single_Tau(Dn2CppObject*)
{ float v = 6.2831855f; return dn2cpp_box(&dn2cpp_single_type, &v, sizeof(v)); }
static const Dn2CppFieldInfo dn2cpp_primflds_single[] = {
    { "E", &dn2cpp_single_type, &dn2cpp_single_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_single_E, nullptr, nullptr, 0, 0x8056, 0 },
    { "Epsilon", &dn2cpp_single_type, &dn2cpp_single_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_single_Epsilon, nullptr, nullptr, 0, 0x8056, 0 },
    { "MaxValue", &dn2cpp_single_type, &dn2cpp_single_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_single_MaxValue, nullptr, nullptr, 0, 0x8056, 0 },
    { "MinValue", &dn2cpp_single_type, &dn2cpp_single_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_single_MinValue, nullptr, nullptr, 0, 0x8056, 0 },
    { "NaN", &dn2cpp_single_type, &dn2cpp_single_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_single_NaN, nullptr, nullptr, 0, 0x8056, 0 },
    { "NegativeInfinity", &dn2cpp_single_type, &dn2cpp_single_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_single_NegativeInfinity, nullptr, nullptr, 0, 0x8056, 0 },
    { "NegativeZero", &dn2cpp_single_type, &dn2cpp_single_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_single_NegativeZero, nullptr, nullptr, 0, 0x8056, 0 },
    { "Pi", &dn2cpp_single_type, &dn2cpp_single_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_single_Pi, nullptr, nullptr, 0, 0x8056, 0 },
    { "PositiveInfinity", &dn2cpp_single_type, &dn2cpp_single_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_single_PositiveInfinity, nullptr, nullptr, 0, 0x8056, 0 },
    { "Tau", &dn2cpp_single_type, &dn2cpp_single_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_single_Tau, nullptr, nullptr, 0, 0x8056, 0 },
};
static Dn2CppObject* dn2cpp_primfld_double_E(Dn2CppObject*)
{ double v = 2.718281828459045; return dn2cpp_box(&dn2cpp_double_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_double_Epsilon(Dn2CppObject*)
{ double v = std::numeric_limits<double>::denorm_min(); return dn2cpp_box(&dn2cpp_double_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_double_MaxValue(Dn2CppObject*)
{ double v = std::numeric_limits<double>::max(); return dn2cpp_box(&dn2cpp_double_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_double_MinValue(Dn2CppObject*)
{ double v = (-std::numeric_limits<double>::max()); return dn2cpp_box(&dn2cpp_double_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_double_NaN(Dn2CppObject*)
{ double v = std::numeric_limits<double>::quiet_NaN(); return dn2cpp_box(&dn2cpp_double_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_double_NegativeInfinity(Dn2CppObject*)
{ double v = (-std::numeric_limits<double>::infinity()); return dn2cpp_box(&dn2cpp_double_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_double_NegativeZero(Dn2CppObject*)
{ double v = (-0.0); return dn2cpp_box(&dn2cpp_double_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_double_Pi(Dn2CppObject*)
{ double v = 3.141592653589793; return dn2cpp_box(&dn2cpp_double_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_double_PositiveInfinity(Dn2CppObject*)
{ double v = std::numeric_limits<double>::infinity(); return dn2cpp_box(&dn2cpp_double_type, &v, sizeof(v)); }
static Dn2CppObject* dn2cpp_primfld_double_Tau(Dn2CppObject*)
{ double v = 6.283185307179586; return dn2cpp_box(&dn2cpp_double_type, &v, sizeof(v)); }
static const Dn2CppFieldInfo dn2cpp_primflds_double[] = {
    { "E", &dn2cpp_double_type, &dn2cpp_double_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_double_E, nullptr, nullptr, 0, 0x8056, 0 },
    { "Epsilon", &dn2cpp_double_type, &dn2cpp_double_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_double_Epsilon, nullptr, nullptr, 0, 0x8056, 0 },
    { "MaxValue", &dn2cpp_double_type, &dn2cpp_double_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_double_MaxValue, nullptr, nullptr, 0, 0x8056, 0 },
    { "MinValue", &dn2cpp_double_type, &dn2cpp_double_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_double_MinValue, nullptr, nullptr, 0, 0x8056, 0 },
    { "NaN", &dn2cpp_double_type, &dn2cpp_double_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_double_NaN, nullptr, nullptr, 0, 0x8056, 0 },
    { "NegativeInfinity", &dn2cpp_double_type, &dn2cpp_double_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_double_NegativeInfinity, nullptr, nullptr, 0, 0x8056, 0 },
    { "NegativeZero", &dn2cpp_double_type, &dn2cpp_double_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_double_NegativeZero, nullptr, nullptr, 0, 0x8056, 0 },
    { "Pi", &dn2cpp_double_type, &dn2cpp_double_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_double_Pi, nullptr, nullptr, 0, 0x8056, 0 },
    { "PositiveInfinity", &dn2cpp_double_type, &dn2cpp_double_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_double_PositiveInfinity, nullptr, nullptr, 0, 0x8056, 0 },
    { "Tau", &dn2cpp_double_type, &dn2cpp_double_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_primfld_double_Tau, nullptr, nullptr, 0, 0x8056, 0 },
};

extern const Dn2CppType dn2cpp_bool_type_obj;
const Dn2CppTypeInfo dn2cpp_bool_type =
    dn2cpp_ti_with_typeobject({ "System.Boolean", nullptr, 4, nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_PRIMITIVE | DN2CPP_TF_SEALED), dn2cpp_primflds_bool, 2 }, &dn2cpp_bool_type_obj);
const Dn2CppType dn2cpp_bool_type_obj = { { &dn2cpp_type_type }, &dn2cpp_bool_type };
// char + the small integer primitives. Their boxed payload is the int32/uint32
// stack width (CppTypes.Of widens char/byte/short to int32_t), so dn2cpp_box
// stores 4 bytes and ToString reads them back at that width.
extern const Dn2CppType dn2cpp_char_type_obj;
const Dn2CppTypeInfo dn2cpp_char_type =
    dn2cpp_ti_with_typeobject({ "System.Char", nullptr, 4, nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_PRIMITIVE | DN2CPP_TF_SEALED), dn2cpp_primflds_char, 2 }, &dn2cpp_char_type_obj);
const Dn2CppType dn2cpp_char_type_obj = { { &dn2cpp_type_type }, &dn2cpp_char_type };
extern const Dn2CppType dn2cpp_byte_type_obj;
const Dn2CppTypeInfo dn2cpp_byte_type =
    dn2cpp_ti_with_typeobject({ "System.Byte", nullptr, 4, nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_PRIMITIVE | DN2CPP_TF_SEALED), dn2cpp_primflds_byte, 2 }, &dn2cpp_byte_type_obj);
const Dn2CppType dn2cpp_byte_type_obj = { { &dn2cpp_type_type }, &dn2cpp_byte_type };
extern const Dn2CppType dn2cpp_sbyte_type_obj;
const Dn2CppTypeInfo dn2cpp_sbyte_type =
    dn2cpp_ti_with_typeobject({ "System.SByte", nullptr, 4, nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_PRIMITIVE | DN2CPP_TF_SEALED), dn2cpp_primflds_sbyte, 2 }, &dn2cpp_sbyte_type_obj);
const Dn2CppType dn2cpp_sbyte_type_obj = { { &dn2cpp_type_type }, &dn2cpp_sbyte_type };
extern const Dn2CppType dn2cpp_int16_type_obj;
const Dn2CppTypeInfo dn2cpp_int16_type =
    dn2cpp_ti_with_typeobject({ "System.Int16", nullptr, 4, nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_PRIMITIVE | DN2CPP_TF_SEALED), dn2cpp_primflds_int16, 2 }, &dn2cpp_int16_type_obj);
const Dn2CppType dn2cpp_int16_type_obj = { { &dn2cpp_type_type }, &dn2cpp_int16_type };
extern const Dn2CppType dn2cpp_uint16_type_obj;
const Dn2CppTypeInfo dn2cpp_uint16_type =
    dn2cpp_ti_with_typeobject({ "System.UInt16", nullptr, 4, nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_PRIMITIVE | DN2CPP_TF_SEALED), dn2cpp_primflds_uint16, 2 }, &dn2cpp_uint16_type_obj);
const Dn2CppType dn2cpp_uint16_type_obj = { { &dn2cpp_type_type }, &dn2cpp_uint16_type };
extern const Dn2CppType dn2cpp_uint32_type_obj;
const Dn2CppTypeInfo dn2cpp_uint32_type =
    dn2cpp_ti_with_typeobject({ "System.UInt32", nullptr, 4, nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_PRIMITIVE | DN2CPP_TF_SEALED), dn2cpp_primflds_uint32, 2 }, &dn2cpp_uint32_type_obj);
const Dn2CppType dn2cpp_uint32_type_obj = { { &dn2cpp_type_type }, &dn2cpp_uint32_type };
extern const Dn2CppType dn2cpp_int32_type_obj;
const Dn2CppTypeInfo dn2cpp_int32_type =
    dn2cpp_ti_with_typeobject({ "System.Int32", nullptr, 4, nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_PRIMITIVE | DN2CPP_TF_SEALED), dn2cpp_primflds_int32, 2 }, &dn2cpp_int32_type_obj);
const Dn2CppType dn2cpp_int32_type_obj = { { &dn2cpp_type_type }, &dn2cpp_int32_type };
extern const Dn2CppType dn2cpp_int64_type_obj;
const Dn2CppTypeInfo dn2cpp_int64_type =
    dn2cpp_ti_with_typeobject({ "System.Int64", nullptr, 8, nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_PRIMITIVE | DN2CPP_TF_SEALED), dn2cpp_primflds_int64, 2 }, &dn2cpp_int64_type_obj);
const Dn2CppType dn2cpp_int64_type_obj = { { &dn2cpp_type_type }, &dn2cpp_int64_type };
extern const Dn2CppType dn2cpp_uint64_type_obj;
const Dn2CppTypeInfo dn2cpp_uint64_type =
    dn2cpp_ti_with_typeobject({ "System.UInt64", nullptr, 8, nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_PRIMITIVE | DN2CPP_TF_SEALED), dn2cpp_primflds_uint64, 2 }, &dn2cpp_uint64_type_obj);
const Dn2CppType dn2cpp_uint64_type_obj = { { &dn2cpp_type_type }, &dn2cpp_uint64_type };
// IntPtr/UIntPtr: an 8-byte intptr_t payload (CppTypes.Of), read like int64/uint64.
// IntPtr ToStrings signed, UIntPtr unsigned. GetType().FullName reports
// "System.IntPtr"/"System.UIntPtr" (the box type-info name).
extern const Dn2CppType dn2cpp_intptr_type_obj;
const Dn2CppTypeInfo dn2cpp_intptr_type =
    dn2cpp_ti_with_typeobject({ "System.IntPtr", nullptr, (int32_t)sizeof(intptr_t), nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_PRIMITIVE | DN2CPP_TF_SEALED), dn2cpp_primflds_intptr, 1 }, &dn2cpp_intptr_type_obj);
const Dn2CppType dn2cpp_intptr_type_obj = { { &dn2cpp_type_type }, &dn2cpp_intptr_type };
extern const Dn2CppType dn2cpp_uintptr_type_obj;
const Dn2CppTypeInfo dn2cpp_uintptr_type =
    dn2cpp_ti_with_typeobject({ "System.UIntPtr", nullptr, (int32_t)sizeof(intptr_t), nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_PRIMITIVE | DN2CPP_TF_SEALED), dn2cpp_primflds_uintptr, 1 }, &dn2cpp_uintptr_type_obj);
const Dn2CppType dn2cpp_uintptr_type_obj = { { &dn2cpp_type_type }, &dn2cpp_uintptr_type };
extern const Dn2CppType dn2cpp_single_type_obj;
const Dn2CppTypeInfo dn2cpp_single_type =
    dn2cpp_ti_with_typeobject({ "System.Single", nullptr, 4, nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_PRIMITIVE | DN2CPP_TF_SEALED), dn2cpp_primflds_single, 10 }, &dn2cpp_single_type_obj);
const Dn2CppType dn2cpp_single_type_obj = { { &dn2cpp_type_type }, &dn2cpp_single_type };
extern const Dn2CppType dn2cpp_double_type_obj;
const Dn2CppTypeInfo dn2cpp_double_type =
    dn2cpp_ti_with_typeobject({ "System.Double", nullptr, 8, nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_PRIMITIVE | DN2CPP_TF_SEALED), dn2cpp_primflds_double, 10 }, &dn2cpp_double_type_obj);
const Dn2CppType dn2cpp_double_type_obj = { { &dn2cpp_type_type }, &dn2cpp_double_type };
extern const Dn2CppType dn2cpp_yield_awaiter_type_obj;
const Dn2CppTypeInfo dn2cpp_yield_awaiter_type =
    dn2cpp_ti_with_typeobject({ "System.Runtime.CompilerServices.YieldAwaitable+YieldAwaiter", nullptr, (int32_t)sizeof(Dn2CppYieldAwaiter), nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_SEALED | DN2CPP_TF_NOT_MARSHALABLE) }, &dn2cpp_yield_awaiter_type_obj);
const Dn2CppType dn2cpp_yield_awaiter_type_obj = { { &dn2cpp_type_type }, &dn2cpp_yield_awaiter_type };
extern const Dn2CppType dn2cpp_parallel_loop_result_type_obj;
const Dn2CppTypeInfo dn2cpp_parallel_loop_result_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.Tasks.ParallelLoopResult", nullptr, (int32_t)sizeof(Dn2CppParallelLoopResult), nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_SEALED) }, &dn2cpp_parallel_loop_result_type_obj);
const Dn2CppType dn2cpp_parallel_loop_result_type_obj = { { &dn2cpp_type_type }, &dn2cpp_parallel_loop_result_type };
// The System.Decimal type-info lives in intrinsics/dn2cpp_system_decimal.cpp, and
// the System.TimeSpan / DateTime / DateTimeOffset / DateOnly / TimeOnly ones in
// intrinsics/dn2cpp_system_datetime.cpp — each next to the formatting, hashing and
// comparison code its tostring/gethashcode/equals/formatspec slots point at, so a
// program that never mentions those types does not link those translation units.
// System.Type derives MemberInfo like real .NET (the handle is declared just
// below), so a Type handle answers `is MemberInfo` and GetMember results mix.
//
// System.Type and System.Reflection.Module carry NO field table although real .NET
// declares public static fields on them: those are MemberFilter/TypeFilter delegates
// and Missing.Value, which have no representation here. A partially filled table is
// worse than none — it makes a missing row look like a field the CLR does not
// declare.
extern const Dn2CppType dn2cpp_type_type_obj;
const Dn2CppTypeInfo dn2cpp_type_type =
    dn2cpp_ti_with_typeobject({ "System.Type", &dn2cpp_memberinfo_type, (int32_t)sizeof(Dn2CppType), nullptr, nullptr, 0 }, &dn2cpp_type_type_obj);
const Dn2CppType dn2cpp_type_type_obj = { { &dn2cpp_type_type }, &dn2cpp_type_type };
// The reflection handle hierarchy mirrors .NET's: MemberInfo is the root,
// MethodBase sits between it and MethodInfo/ConstructorInfo, and Type itself
// derives from MemberInfo (its base is stamped where dn2cpp_type_type is
// defined above). Casts/`is` against these CLASSES resolve to these shared
// handles (MethodCompiler.TypeInfoExprOf), so `member is PropertyInfo` /
// `(MethodInfo)member` behave like real .NET over Get* results. MemberInfo and
// MethodBase are abstract — no instance ever carries them as its header.
extern const Dn2CppType dn2cpp_memberinfo_type_obj;
const Dn2CppTypeInfo dn2cpp_memberinfo_type =
    dn2cpp_ti_with_typeobject({ "System.Reflection.MemberInfo", nullptr, 0, nullptr, nullptr, 0 }, &dn2cpp_memberinfo_type_obj);
const Dn2CppType dn2cpp_memberinfo_type_obj = { { &dn2cpp_type_type }, &dn2cpp_memberinfo_type };
extern const Dn2CppType dn2cpp_methodbase_type_obj;
const Dn2CppTypeInfo dn2cpp_methodbase_type =
    dn2cpp_ti_with_typeobject({ "System.Reflection.MethodBase", &dn2cpp_memberinfo_type, 0, nullptr, nullptr, 0 }, &dn2cpp_methodbase_type_obj);
const Dn2CppType dn2cpp_methodbase_type_obj = { { &dn2cpp_type_type }, &dn2cpp_methodbase_type };
extern const Dn2CppType dn2cpp_fieldinfo_type_obj;
const Dn2CppTypeInfo dn2cpp_fieldinfo_type =
    dn2cpp_ti_with_typeobject({ "System.Reflection.FieldInfo", &dn2cpp_memberinfo_type, (int32_t)sizeof(Dn2CppFieldRef), nullptr, nullptr, 0, &dn2cpp_reflection_handle_tostring }, &dn2cpp_fieldinfo_type_obj);
const Dn2CppType dn2cpp_fieldinfo_type_obj = { { &dn2cpp_type_type }, &dn2cpp_fieldinfo_type };
extern const Dn2CppType dn2cpp_methodinfo_type_obj;
const Dn2CppTypeInfo dn2cpp_methodinfo_type =
    dn2cpp_ti_with_typeobject({ "System.Reflection.MethodInfo", &dn2cpp_methodbase_type, (int32_t)sizeof(Dn2CppMethodRef), nullptr, nullptr, 0, &dn2cpp_reflection_handle_tostring }, &dn2cpp_methodinfo_type_obj);
const Dn2CppType dn2cpp_methodinfo_type_obj = { { &dn2cpp_type_type }, &dn2cpp_methodinfo_type };
// A reflected constructor handle: the same Dn2CppMethodRef representation as a
// method (wrapping a ctortab row), but its own header type so `is
// ConstructorInfo` / `is MethodInfo` discriminate like real .NET.
// Its two public fields are the names the CLR gives the two constructor kinds.
static Dn2CppObject* dn2cpp_ownfld_ctorinfo_ConstructorName(Dn2CppObject*)
{ return reinterpret_cast<Dn2CppObject*>(dn2cpp_string_literal(u".ctor", 5)); }
static Dn2CppObject* dn2cpp_ownfld_ctorinfo_TypeConstructorName(Dn2CppObject*)
{ return reinterpret_cast<Dn2CppObject*>(dn2cpp_string_literal(u".cctor", 6)); }
static const Dn2CppFieldInfo dn2cpp_ownflds_ctorinfo[] = {
    { "ConstructorName", &dn2cpp_constructorinfo_type, &dn2cpp_string_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_INITONLY,
      dn2cpp_ownfld_ctorinfo_ConstructorName, nullptr, nullptr, 0, 0x36, 0 },
    { "TypeConstructorName", &dn2cpp_constructorinfo_type, &dn2cpp_string_type, DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_INITONLY,
      dn2cpp_ownfld_ctorinfo_TypeConstructorName, nullptr, nullptr, 0, 0x36, 0 },
};
extern const Dn2CppType dn2cpp_constructorinfo_type_obj;
const Dn2CppTypeInfo dn2cpp_constructorinfo_type =
    dn2cpp_ti_with_typeobject({ "System.Reflection.ConstructorInfo", &dn2cpp_methodbase_type, (int32_t)sizeof(Dn2CppMethodRef), nullptr, nullptr, 0, &dn2cpp_reflection_handle_tostring, nullptr, nullptr, 0, dn2cpp_ownflds_ctorinfo, 2 }, &dn2cpp_constructorinfo_type_obj);
const Dn2CppType dn2cpp_constructorinfo_type_obj = { { &dn2cpp_type_type }, &dn2cpp_constructorinfo_type };
extern const Dn2CppType dn2cpp_parameterinfo_type_obj;
const Dn2CppTypeInfo dn2cpp_parameterinfo_type =
    dn2cpp_ti_with_typeobject({ "System.Reflection.ParameterInfo", nullptr, (int32_t)sizeof(Dn2CppParamRef), nullptr, nullptr, 0, &dn2cpp_reflection_handle_tostring }, &dn2cpp_parameterinfo_type_obj);
const Dn2CppType dn2cpp_parameterinfo_type_obj = { { &dn2cpp_type_type }, &dn2cpp_parameterinfo_type };
extern const Dn2CppType dn2cpp_propertyinfo_type_obj;
const Dn2CppTypeInfo dn2cpp_propertyinfo_type =
    dn2cpp_ti_with_typeobject({ "System.Reflection.PropertyInfo", &dn2cpp_memberinfo_type, (int32_t)sizeof(Dn2CppPropRef), nullptr, nullptr, 0, &dn2cpp_reflection_handle_tostring }, &dn2cpp_propertyinfo_type_obj);
const Dn2CppType dn2cpp_propertyinfo_type_obj = { { &dn2cpp_type_type }, &dn2cpp_propertyinfo_type };
extern const Dn2CppType dn2cpp_customattributedata_type_obj;
const Dn2CppTypeInfo dn2cpp_customattributedata_type =
    dn2cpp_ti_with_typeobject({ "System.Reflection.CustomAttributeData", nullptr, (int32_t)sizeof(Dn2CppAttrDataRef), nullptr, nullptr, 0, &dn2cpp_reflection_handle_tostring }, &dn2cpp_customattributedata_type_obj);
const Dn2CppType dn2cpp_customattributedata_type_obj = { { &dn2cpp_type_type }, &dn2cpp_customattributedata_type };
// System.Void, so a void method's MethodInfo.ReturnType reports Name "Void".
extern const Dn2CppType dn2cpp_void_type_obj;
const Dn2CppTypeInfo dn2cpp_void_type =
    dn2cpp_ti_with_typeobject({ "System.Void", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, (DN2CPP_TF_VALUETYPE | DN2CPP_TF_SEALED) }, &dn2cpp_void_type_obj);
const Dn2CppType dn2cpp_void_type_obj = { { &dn2cpp_type_type }, &dn2cpp_void_type };
extern const Dn2CppType dn2cpp_exception_type_obj;
const Dn2CppTypeInfo dn2cpp_exception_type =
    dn2cpp_ti_with_typeobject({ "System.Exception", nullptr, 0, nullptr, nullptr, 0 }, &dn2cpp_exception_type_obj);
const Dn2CppType dn2cpp_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_exception_type };
// System.Enum is non-const like String: every emitted per-enum type-info bases on
// this handle and deliberately carries no interface rows of its own, so the ONE
// interface-dispatch map every boxed enum shares (IComparable/IFormattable/
// IConvertible/ISpanFormattable, slots pointing at program-specific transpiled
// System.Enum impls) is installed here at startup and found by the resolve walk
// through the base chain.
extern const Dn2CppType dn2cpp_enum_type_obj;
Dn2CppTypeInfo dn2cpp_enum_type =
    dn2cpp_ti_with_typeobject({ "System.Enum", nullptr, 0, nullptr, nullptr, 0 }, &dn2cpp_enum_type_obj);
const Dn2CppType dn2cpp_enum_type_obj = { { &dn2cpp_type_type }, &dn2cpp_enum_type };

// SafeWaitHandle is runtime-allocated, but its inherited SafeHandle methods and
// reflection metadata are emitted from CoreLib. Keep one stable base handle and let
// dn2cpp_type_binds replace this stub with that complete emitted metadata at startup.
extern const Dn2CppType dn2cpp_safehandle_type_obj;
Dn2CppTypeInfo dn2cpp_safehandle_type =
    dn2cpp_ti_with_typeobject({ "System.Runtime.InteropServices.SafeHandle", nullptr, 0, nullptr, nullptr, 0 }, &dn2cpp_safehandle_type_obj);
const Dn2CppType dn2cpp_safehandle_type_obj = { { &dn2cpp_type_type }, &dn2cpp_safehandle_type };
extern const Dn2CppType dn2cpp_safehandle_zero_or_minus_one_type_obj;
Dn2CppTypeInfo dn2cpp_safehandle_zero_or_minus_one_type =
    dn2cpp_ti_with_typeobject({ "Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid", &dn2cpp_safehandle_type, 0, nullptr, nullptr, 0 }, &dn2cpp_safehandle_zero_or_minus_one_type_obj);
const Dn2CppType dn2cpp_safehandle_zero_or_minus_one_type_obj =
    { { &dn2cpp_type_type }, &dn2cpp_safehandle_zero_or_minus_one_type };

void dn2cpp_enum_set_interfaces(const Dn2CppInterfaceEntry* entries, int32_t count)
{
    dn2cpp_enum_type.interfaces = entries;
    dn2cpp_enum_type.interfaceCount = count;
}
// Runtime-raised exception type-infos: externally visible so the emitted code
// can name the SAME handle the trap helpers stamp. Each is base-chained to
// dn2cpp_exception_type (forward-referenceable — declared in the header), keeping the
// commonly-tested ArgumentException / IOException intermediates so e.g. a runtime-
// trapped ArgumentNullException is caught by `catch (ArgumentException)`. The
// SystemException intermediate is collapsed (no runtime handle for it) — an
// intentional carve-out (`is SystemException` on a trapped exception reports false).
extern const Dn2CppType dn2cpp_overflow_exception_type_obj;
Dn2CppTypeInfo dn2cpp_overflow_exception_type =
    dn2cpp_ti_with_typeobject({ "System.OverflowException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_overflow_exception_type_obj);
const Dn2CppType dn2cpp_overflow_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_overflow_exception_type };
extern const Dn2CppType dn2cpp_index_out_of_range_exception_type_obj;
Dn2CppTypeInfo dn2cpp_index_out_of_range_exception_type =
    dn2cpp_ti_with_typeobject({ "System.IndexOutOfRangeException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_index_out_of_range_exception_type_obj);
const Dn2CppType dn2cpp_index_out_of_range_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_index_out_of_range_exception_type };
extern const Dn2CppType dn2cpp_argument_exception_type_obj;
Dn2CppTypeInfo dn2cpp_argument_exception_type =
    dn2cpp_ti_with_typeobject({ "System.ArgumentException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_argument_exception_type_obj);
const Dn2CppType dn2cpp_argument_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_argument_exception_type };
extern const Dn2CppType dn2cpp_com_exception_type_obj;
Dn2CppTypeInfo dn2cpp_com_exception_type =
    dn2cpp_ti_with_typeobject({ "System.Runtime.InteropServices.COMException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_com_exception_type_obj);
const Dn2CppType dn2cpp_com_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_com_exception_type };
extern const Dn2CppType dn2cpp_argument_out_of_range_exception_type_obj;
Dn2CppTypeInfo dn2cpp_argument_out_of_range_exception_type =
    dn2cpp_ti_with_typeobject({ "System.ArgumentOutOfRangeException", &dn2cpp_argument_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_argument_out_of_range_exception_type_obj);
const Dn2CppType dn2cpp_argument_out_of_range_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_argument_out_of_range_exception_type };
extern const Dn2CppType dn2cpp_argument_null_exception_type_obj;
Dn2CppTypeInfo dn2cpp_argument_null_exception_type =
    dn2cpp_ti_with_typeobject({ "System.ArgumentNullException", &dn2cpp_argument_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_argument_null_exception_type_obj);
const Dn2CppType dn2cpp_argument_null_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_argument_null_exception_type };
extern const Dn2CppType dn2cpp_invalid_operation_exception_type_obj;
Dn2CppTypeInfo dn2cpp_invalid_operation_exception_type =
    dn2cpp_ti_with_typeobject({ "System.InvalidOperationException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_invalid_operation_exception_type_obj);
const Dn2CppType dn2cpp_invalid_operation_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_invalid_operation_exception_type };
// Based on InvalidOperationException (as in .NET: ObjectDisposedException :
// InvalidOperationException). The base link is what keeps a `catch
// (InvalidOperationException)` catching it — and getting the DERIVED type right is
// what makes `catch (ObjectDisposedException)` catch it at all.
extern const Dn2CppType dn2cpp_object_disposed_exception_type_obj;
Dn2CppTypeInfo dn2cpp_object_disposed_exception_type =
    dn2cpp_ti_with_typeobject({ "System.ObjectDisposedException", &dn2cpp_invalid_operation_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_object_disposed_exception_type_obj);
const Dn2CppType dn2cpp_object_disposed_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_object_disposed_exception_type };
extern const Dn2CppType dn2cpp_out_of_memory_exception_type_obj;
Dn2CppTypeInfo dn2cpp_out_of_memory_exception_type =
    dn2cpp_ti_with_typeobject({ "System.OutOfMemoryException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_out_of_memory_exception_type_obj);
const Dn2CppType dn2cpp_out_of_memory_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_out_of_memory_exception_type };
extern const Dn2CppType dn2cpp_arithmetic_exception_type_obj;
Dn2CppTypeInfo dn2cpp_arithmetic_exception_type =
    dn2cpp_ti_with_typeobject({ "System.ArithmeticException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_arithmetic_exception_type_obj);
const Dn2CppType dn2cpp_arithmetic_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_arithmetic_exception_type };
extern const Dn2CppType dn2cpp_invalid_cast_exception_type_obj;
Dn2CppTypeInfo dn2cpp_invalid_cast_exception_type =
    dn2cpp_ti_with_typeobject({ "System.InvalidCastException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_invalid_cast_exception_type_obj);
const Dn2CppType dn2cpp_invalid_cast_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_invalid_cast_exception_type };
extern const Dn2CppType dn2cpp_type_load_exception_type_obj;
Dn2CppTypeInfo dn2cpp_type_load_exception_type =
    dn2cpp_ti_with_typeobject({ "System.TypeLoadException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_type_load_exception_type_obj);
const Dn2CppType dn2cpp_type_load_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_type_load_exception_type };
extern const Dn2CppType dn2cpp_not_supported_exception_type_obj;
Dn2CppTypeInfo dn2cpp_not_supported_exception_type =
    dn2cpp_ti_with_typeobject({ "System.NotSupportedException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_not_supported_exception_type_obj);
const Dn2CppType dn2cpp_not_supported_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_not_supported_exception_type };
extern const Dn2CppType dn2cpp_format_exception_type_obj;
Dn2CppTypeInfo dn2cpp_format_exception_type =
    dn2cpp_ti_with_typeobject({ "System.FormatException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_format_exception_type_obj);
const Dn2CppType dn2cpp_format_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_format_exception_type };
// Based on NotSupportedException (as in .NET: PNSE : NotSupportedException), so
// `catch (NotSupportedException)` also matches the dynamic-code-generation traps.
extern const Dn2CppType dn2cpp_platform_not_supported_exception_type_obj;
Dn2CppTypeInfo dn2cpp_platform_not_supported_exception_type =
    dn2cpp_ti_with_typeobject({ "System.PlatformNotSupportedException", &dn2cpp_not_supported_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_platform_not_supported_exception_type_obj);
const Dn2CppType dn2cpp_platform_not_supported_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_platform_not_supported_exception_type };
// File error paths (real .NET exception types, so `catch`/GetType match).
extern const Dn2CppType dn2cpp_io_exception_type_obj;
Dn2CppTypeInfo dn2cpp_io_exception_type =
    dn2cpp_ti_with_typeobject({ "System.IO.IOException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_io_exception_type_obj);
const Dn2CppType dn2cpp_io_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_io_exception_type };
extern const Dn2CppType dn2cpp_file_not_found_exception_type_obj;
Dn2CppTypeInfo dn2cpp_file_not_found_exception_type =
    dn2cpp_ti_with_typeobject({ "System.IO.FileNotFoundException", &dn2cpp_io_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_file_not_found_exception_type_obj);
const Dn2CppType dn2cpp_file_not_found_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_file_not_found_exception_type };
extern const Dn2CppType dn2cpp_path_too_long_exception_type_obj;
Dn2CppTypeInfo dn2cpp_path_too_long_exception_type =
    dn2cpp_ti_with_typeobject({ "System.IO.PathTooLongException", &dn2cpp_io_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_path_too_long_exception_type_obj);
const Dn2CppType dn2cpp_path_too_long_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_path_too_long_exception_type };
extern const Dn2CppType dn2cpp_unauthorized_access_exception_type_obj;
Dn2CppTypeInfo dn2cpp_unauthorized_access_exception_type =
    dn2cpp_ti_with_typeobject({ "System.UnauthorizedAccessException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_unauthorized_access_exception_type_obj);
const Dn2CppType dn2cpp_unauthorized_access_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_unauthorized_access_exception_type };
extern const Dn2CppType dn2cpp_key_not_found_exception_type_obj;
Dn2CppTypeInfo dn2cpp_key_not_found_exception_type =
    dn2cpp_ti_with_typeobject({ "System.Collections.Generic.KeyNotFoundException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_key_not_found_exception_type_obj);
const Dn2CppType dn2cpp_key_not_found_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_key_not_found_exception_type };
// Array.Copy over two arrays of different rank, matching .NET's RankException.
extern const Dn2CppType dn2cpp_rank_exception_type_obj;
Dn2CppTypeInfo dn2cpp_rank_exception_type =
    dn2cpp_ti_with_typeobject({ "System.RankException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_rank_exception_type_obj);
const Dn2CppType dn2cpp_rank_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_rank_exception_type };
// Array.Copy over two arrays whose element types no arm of the CLR's
// compatibility verdict relates, matching .NET's
// ArrayTypeMismatchException. Direct System.Exception base, like RankException.
extern const Dn2CppType dn2cpp_array_type_mismatch_exception_type_obj;
Dn2CppTypeInfo dn2cpp_array_type_mismatch_exception_type =
    dn2cpp_ti_with_typeobject({ "System.ArrayTypeMismatchException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_array_type_mismatch_exception_type_obj);
const Dn2CppType dn2cpp_array_type_mismatch_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_array_type_mismatch_exception_type };
// Reflection member lookup with several undecidable matches (Type.GetMethod /
// GetProperty), matching .NET's AmbiguousMatchException.
extern const Dn2CppType dn2cpp_ambiguous_match_exception_type_obj;
Dn2CppTypeInfo dn2cpp_ambiguous_match_exception_type =
    dn2cpp_ti_with_typeobject({ "System.Reflection.AmbiguousMatchException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_ambiguous_match_exception_type_obj);
const Dn2CppType dn2cpp_ambiguous_match_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_ambiguous_match_exception_type };
// Activator/ConstructorInfo constructor resolution with no matching ctor,
// matching .NET's MissingMethodException. Direct System.Exception base (the
// MissingMemberException/MemberAccessException intermediates are not modeled,
// the same posture as AmbiguousMatchException's missing SystemException).
extern const Dn2CppType dn2cpp_missing_method_exception_type_obj;
Dn2CppTypeInfo dn2cpp_missing_method_exception_type =
    dn2cpp_ti_with_typeobject({ "System.MissingMethodException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_missing_method_exception_type_obj);
const Dn2CppType dn2cpp_missing_method_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_missing_method_exception_type };
extern const Dn2CppType dn2cpp_missing_manifest_resource_exception_type_obj;
Dn2CppTypeInfo dn2cpp_missing_manifest_resource_exception_type =
    dn2cpp_ti_with_typeobject({ "System.Resources.MissingManifestResourceException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_missing_manifest_resource_exception_type_obj);
const Dn2CppType dn2cpp_missing_manifest_resource_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_missing_manifest_resource_exception_type };
// A runtime entry point's null managed receiver (a null FieldInfo's GetValue),
// matching .NET's NullReferenceException for the instance call it stands in
// for. Direct System.Exception base (the SystemException intermediate is not
// modeled, the same posture as AmbiguousMatchException).
extern const Dn2CppType dn2cpp_null_reference_exception_type_obj;
Dn2CppTypeInfo dn2cpp_null_reference_exception_type =
    dn2cpp_ti_with_typeobject({ "System.NullReferenceException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_null_reference_exception_type_obj);
const Dn2CppType dn2cpp_null_reference_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_null_reference_exception_type };

// System.DivideByZeroException — raised by the emitted div/rem guards, the
// interpreter's binary arms and decimal's. Unlike its siblings it does NOT chain
// straight to System.Exception: .NET derives it from ArithmeticException, and
// `catch (ArithmeticException)` around numeric parsing is a shape real code writes.
extern const Dn2CppType dn2cpp_divide_by_zero_exception_type_obj;
Dn2CppTypeInfo dn2cpp_divide_by_zero_exception_type =
    dn2cpp_ti_with_typeobject({ "System.DivideByZeroException", &dn2cpp_arithmetic_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_divide_by_zero_exception_type_obj);
const Dn2CppType dn2cpp_divide_by_zero_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_divide_by_zero_exception_type };

// System.Threading.LockRecursionException — raised by ReaderWriterLockSlim's
// per-thread ownership checks: a same-thread re-entry the lock's
// recursion policy forbids throws this instead of deadlocking against itself,
// matching real .NET. Direct System.Exception base, as in .NET.
extern const Dn2CppType dn2cpp_lock_recursion_exception_type_obj;
Dn2CppTypeInfo dn2cpp_lock_recursion_exception_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.LockRecursionException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_lock_recursion_exception_type_obj);
const Dn2CppType dn2cpp_lock_recursion_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_lock_recursion_exception_type };
// System.Threading.SynchronizationLockException — raised by ReaderWriterLockSlim's
// Exit* paths when the calling thread does not hold the lock being released
// matching real .NET. Direct System.Exception base (the SystemException intermediate
// stays unmodeled, as everywhere).
extern const Dn2CppType dn2cpp_synchronization_lock_exception_type_obj;
Dn2CppTypeInfo dn2cpp_synchronization_lock_exception_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.SynchronizationLockException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_synchronization_lock_exception_type_obj);
const Dn2CppType dn2cpp_synchronization_lock_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_synchronization_lock_exception_type };

// Mutex-interned fallback behind the lock-free typeObject fast path (inline in
// dn2cpp.h): one Dn2CppType per type-info handle, so typeof(X)/GetType() stay
// reference-identical (lock(typeof(X)), ReferenceEquals, identity caches) even
// for a type-info nothing wired. Keying on the handle address is sound: every
// Dn2CppTypeInfo is either a data-segment static or GC-rooted forever (dyn
// registry / g_loadedImages), so a key can never dangle or be reused. Identity
// stays consistent per handle: typeObject is constant-initialized (or stamped
// before the ti is published), so a handle takes the field path always or the
// map path always — never a mix.
static std::mutex& g_type_intern_mtx = dn2cpp_never_destroyed<std::mutex>();
static std::unordered_map<const Dn2CppTypeInfo*, Dn2CppType*>& g_type_intern =
    dn2cpp_never_destroyed<std::unordered_map<const Dn2CppTypeInfo*, Dn2CppType*>>();

Dn2CppType* dn2cpp_get_type_from_handle_slow(const Dn2CppTypeInfo* handle)
{
    std::lock_guard<std::mutex> lk(g_type_intern_mtx);
    Dn2CppType*& slot = g_type_intern[handle];
    if (slot == nullptr)
    {
        // The map's internal buffer is not GC-scanned, so the interned object
        // is allocated uncollectable — a Type lives for the process, as in .NET.
        auto* t = static_cast<Dn2CppType*>(dn2cpp_alloc_pinned(sizeof(Dn2CppType)));
        t->type = &dn2cpp_type_type;
        t->typeInfo = handle;
        slot = t;
    }
    return slot;
}

// The type-name registry's dynamic side-chain (contract in dn2cpp.h): a
// prepend-only linked list rooted in a data-segment static, so the collector
// keeps every registered entry (and through it the constructed type-info and
// its name) alive. Registration is loader-driven and rare; lookups scan the
// static table first, then the chain newest-first.
static DN2CPP_GC_STATIC_ROOT const Dn2CppDynTypeReg* g_dyn_type_registry = nullptr;

void dn2cpp_type_registry_add(const char* name, const Dn2CppTypeInfo* type)
{
    auto* e = static_cast<Dn2CppDynTypeReg*>(dn2cpp_alloc(sizeof(Dn2CppDynTypeReg)));
    e->name = name;
    e->type = type;
    dn2cpp_gc_store_ref(&e->next, g_dyn_type_registry);
    g_dyn_type_registry = e;
}

const Dn2CppDynTypeReg* dn2cpp_type_registry_dynamic_head()
{
    return g_dyn_type_registry;
}

// Lazy hash index over the STATIC half of the type-name registry (the registry is
// generated const data, so it is built once on first lookup and published release/
// acquire). Runtime-side rather than an emitter-side sorted table so the emitted
// output stays byte-identical. Duplicate names keep the FIRST entry, matching
// linear-scan order; the dynamic side-chain is still scanned linearly AFTER a static
// miss, which preserves both "a built-in type can never be shadowed" and
// newest-first patch-type semantics — and is why the mutable chain is not indexed.
// The index holds table indices in native immortal memory: nothing for the GC.
namespace
{
struct Dn2CppTypeRegIndex
{
    uint32_t mask;   // slot count - 1 (power of two, >= 2x entry count)
    int32_t* slots;  // index into dn2cpp_type_registry; -1 = empty
};

uint32_t dn2cpp_reg_name_hash(const char* s, size_t len)
{
    // FNV-1a, matching the transpiler's own no-seeded-hashing discipline.
    uint32_t h = 2166136261u;
    for (size_t i = 0; i < len; i++)
    {
        h ^= static_cast<uint8_t>(s[i]);
        h *= 16777619u;
    }
    return h;
}
}

static std::atomic<const Dn2CppTypeRegIndex*> g_type_registry_index{nullptr};

static const Dn2CppTypeRegIndex* dn2cpp_type_registry_index()
{
    const Dn2CppTypeRegIndex* idx = g_type_registry_index.load(std::memory_order_acquire);
    if (idx != nullptr)
        return idx;
    static std::mutex& buildMtx = dn2cpp_never_destroyed<std::mutex>();
    std::lock_guard<std::mutex> lk(buildMtx);
    idx = g_type_registry_index.load(std::memory_order_acquire);
    if (idx != nullptr)
        return idx; // another thread built it while we waited on the mutex
    uint32_t cap = 16;
    while (cap < static_cast<uint32_t>(dn2cpp_type_registry_count) * 2u)
        cap <<= 1;
    auto* built = new Dn2CppTypeRegIndex;
    built->mask = cap - 1;
    built->slots = new int32_t[cap];
    for (uint32_t i = 0; i < cap; i++)
        built->slots[i] = -1;
    for (int32_t i = 0; i < dn2cpp_type_registry_count; i++)
    {
        const char* rn = dn2cpp_type_registry[i].name;
        size_t len = std::strlen(rn);
        uint32_t s = dn2cpp_reg_name_hash(rn, len) & built->mask;
        for (;;)
        {
            int32_t cur = built->slots[s];
            if (cur < 0)
            {
                built->slots[s] = i;
                break;
            }
            const char* cn = dn2cpp_type_registry[cur].name;
            if (std::strncmp(cn, rn, len) == 0 && cn[len] == '\0')
                break; // duplicate name: the first entry wins, as in the old scan
            s = (s + 1) & built->mask;
        }
    }
    g_type_registry_index.store(built, std::memory_order_release);
    return built;
}

const Dn2CppTypeInfo* dn2cpp_type_registry_find(const char* name, int32_t len)
{
    // Registry names (static and dynamic alike) are NUL-terminated C strings
    // and never contain an embedded NUL, so a query slice carrying one is a
    // definite miss — the old scan (strlen == len && memcmp, and the widened
    // compare's '\0' break) could never match one. Reject it before probing
    // either half: without this guard the strncmp below stops at the shared
    // NUL, returns 0, and `rn[len]` reads past the entry string's terminator
    // (OOB; a zero neighbor byte turns the overread into a false hit).
    if (std::memchr(name, '\0', static_cast<size_t>(len)) != nullptr)
        return nullptr;
    const Dn2CppTypeRegIndex* idx = dn2cpp_type_registry_index();
    // strncmp stops at the entry's NUL (never past the entry, whose remaining
    // suffix a longer query cannot match), and the query is NUL-free (guard
    // above), so `rn[len]` is in bounds: strncmp returning 0 over `len` bytes
    // means the entry has at least len non-NUL bytes.
    uint32_t s = dn2cpp_reg_name_hash(name, static_cast<size_t>(len)) & idx->mask;
    for (;;)
    {
        int32_t cur = idx->slots[s];
        if (cur < 0)
            break;
        const char* rn = dn2cpp_type_registry[cur].name;
        if (std::strncmp(rn, name, len) == 0 && rn[len] == '\0')
            return dn2cpp_type_registry[cur].type;
        s = (s + 1) & idx->mask;
    }
    for (const Dn2CppDynTypeReg* e = g_dyn_type_registry; e != nullptr; e = e->next)
    {
        if (std::strlen(e->name) == static_cast<size_t>(len) && std::memcmp(e->name, name, len) == 0)
            return e->type;
    }
    return nullptr;
}

// The simple-name tail of a CLR reflection name (see dn2cpp.h). '+' is the CLR
// nested-type separator, so it splits like '.': "Ns.Outer+Inner" -> "Inner".
const char* dn2cpp_simple_type_name(const char* full)
{
    const char* simple = full;
    for (const char* p = full; *p != '\0'; p++)
        if (*p == '.' || *p == '+')
            simple = p + 1;
    return simple;
}

Dn2CppString* dn2cpp_type_name(const Dn2CppTypeInfo* ti)
{
    // A closed generic reports its definition's simple name (e.g. "List`1"), matching
    // .NET (typeof(List<int>).Name == typeof(List<>).Name), not the dn2cpp-mangled
    // instantiation name. FullName/ToString compose from the same two members
    // (dn2cpp_type_fullname below); a NESTED closed generic has no genericDef —
    // CppEmitter.GenericDefInfo carves it out — so all three stay mangled there.
    if (ti->genericArgCount > 0 && ti->genericDef != nullptr)
        ti = ti->genericDef;
    const char* simple = dn2cpp_simple_type_name(ti->name);
    return dn2cpp_string_from_utf8(simple, static_cast<int32_t>(std::strlen(simple)));
}

int32_t dn2cpp_type_equals(Dn2CppType* a, Dn2CppType* b)
{
    if (a == b)
        return 1;
    if (a == nullptr || b == nullptr)
        return 0;
    return a->typeInfo == b->typeInfo ? 1 : 0;
}

// Whether `ti` is a closed generic instantiation, i.e. whether the two names below
// compose rather than read ti->name. A gendef handle self-references its genericDef
// with genericArgCount 0, so it answers false and the recursion terminates there.
static inline bool dn2cpp_ti_is_closed_generic(const Dn2CppTypeInfo* ti)
{
    return ti != nullptr && ti->genericArgCount > 0 && ti->genericDef != nullptr
        && ti->genericDef->name != nullptr;
}

// Whether `ti` is a generic DEFINITION whose ToString appends its parameter names —
// List`1[T]. FullName never does, so the qualifying spelling declines it.
static inline bool dn2cpp_ti_shows_generic_params(const Dn2CppTypeInfo* ti, bool qualify)
{
    return !qualify && ti != nullptr && (ti->flags & DN2CPP_TF_GENERICDEF) != 0
        && ti->genericParamNames != nullptr;
}

// Composes a closed generic's CLR name, Def`N[arg,arg], recursively. `qualify`
// wraps each argument as [<fullname>, <assembly display name>] — the difference
// between Type.FullName and Type.ToString. See dn2cpp_core.h for why ti->name is
// not the answer and may not be changed.
static void dn2cpp_append_type_display(const Dn2CppTypeInfo* ti, bool qualify, std::string& out)
{
    if (!dn2cpp_ti_is_closed_generic(ti))
    {
        out += ti != nullptr && ti->name != nullptr ? ti->name : "System.Object";
        if (dn2cpp_ti_shows_generic_params(ti, qualify))
        {
            out += '[';
            out += ti->genericParamNames;
            out += ']';
        }
        return;
    }
    out += ti->genericDef->name;
    out += '[';
    for (int32_t i = 0; i < ti->genericArgCount; i++)
    {
        if (i > 0)
            out += ',';
        const Dn2CppTypeInfo* a = ti->genericArgs[i];
        if (qualify)
        {
            out += '[';
            dn2cpp_append_type_display(a, true, out);
            out += ", ";
            out += dn2cpp_assembly_display_name_utf8(a != nullptr ? a->assemblyName : nullptr);
            out += ']';
        }
        else
        {
            dn2cpp_append_type_display(a, false, out);
        }
    }
    out += ']';
}

static Dn2CppString* dn2cpp_type_display(const Dn2CppTypeInfo* ti, bool qualify)
{
    if (!dn2cpp_ti_is_closed_generic(ti) && !dn2cpp_ti_shows_generic_params(ti, qualify))
        return dn2cpp_string_from_utf8(ti->name, static_cast<int32_t>(std::strlen(ti->name)));
    std::string s;
    dn2cpp_append_type_display(ti, qualify, s);
    return dn2cpp_string_from_utf8(s.c_str(), static_cast<int32_t>(s.size()));
}

Dn2CppString* dn2cpp_type_fullname(const Dn2CppTypeInfo* ti)
{
    return dn2cpp_type_display(ti, true);
}

Dn2CppString* dn2cpp_type_tostring(const Dn2CppTypeInfo* ti)
{
    return dn2cpp_type_display(ti, false);
}

// Type.Namespace: the declaring chain's namespace — everything before the last
// '.' of the OUTERMOST type's name, i.e. within the prefix up to the first '+'
// ("Ns.Outer+Inner" -> "Ns", like real .NET's nested-type Namespace). "" when
// the type has no namespace (.NET returns null there, but it renders identically).
Dn2CppString* dn2cpp_type_namespace(const Dn2CppTypeInfo* ti)
{
    const char* full = ti->name;
    const char* lastDot = nullptr;
    for (const char* p = full; *p != '\0' && *p != '+'; p++)
        if (*p == '.')
            lastDot = p;
    if (lastDot == nullptr)
        return dn2cpp_string_from_utf8("", 0);
    return dn2cpp_string_from_utf8(full, static_cast<int32_t>(lastDot - full));
}

int32_t dn2cpp_type_is_enum(const Dn2CppTypeInfo* ti)
{
    return (ti->flags & DN2CPP_TF_ENUM) != 0 ? 1 : 0;
}

int32_t dn2cpp_type_is_array(const Dn2CppTypeInfo* ti)
{
    return (ti->flags & DN2CPP_TF_ARRAY) != 0 ? 1 : 0;
}

int32_t dn2cpp_type_is_szarray(const Dn2CppTypeInfo* ti)
{
    // A single-rank array (the per-element ti_arr_* type-infos carry rank 1;
    // the shared array handles leave arrayRank 0, which also means rank 1).
    return ((ti->flags & DN2CPP_TF_ARRAY) != 0 && ti->arrayRank <= 1) ? 1 : 0;
}

int32_t dn2cpp_type_is_nested(const Dn2CppTypeInfo* ti)
{
    return (ti->flags & DN2CPP_TF_NESTED) != 0 ? 1 : 0;
}

int32_t dn2cpp_type_is_interface(const Dn2CppTypeInfo* ti)
{
    return (ti->flags & DN2CPP_TF_INTERFACE) != 0 ? 1 : 0;
}

int32_t dn2cpp_type_is_abstract(const Dn2CppTypeInfo* ti)
{
    return (ti->flags & DN2CPP_TF_ABSTRACT) != 0 ? 1 : 0;
}

int32_t dn2cpp_type_is_sealed(const Dn2CppTypeInfo* ti)
{
    return (ti->flags & DN2CPP_TF_SEALED) != 0 ? 1 : 0;
}

int32_t dn2cpp_type_is_by_ref_like(const Dn2CppTypeInfo* ti)
{
    return (ti->flags & DN2CPP_TF_BYREFLIKE) != 0 ? 1 : 0;
}

// dn2cpp never produces a pointer/byref Type value at runtime (no MakePointerType /
// MakeByRefType, and typeof of a pointer/byref folds statically), so a runtime
// IsPointer/IsByRef is always false. The type is taken (and ignored) only so the
// receiver expression is evaluated like the other getters.
int32_t dn2cpp_type_is_pointer(const Dn2CppTypeInfo* ti)
{
    (void)ti;
    return 0;
}

int32_t dn2cpp_type_is_by_ref(const Dn2CppTypeInfo* ti)
{
    (void)ti;
    return 0;
}

// Type.GetTypeCode: the type's System.TypeCode. An enum unwraps to its underlying
// integer's code (matching .NET); the primitives + String/Decimal/DateTime/DBNull
// map to their codes; every other type (incl. IntPtr/UIntPtr, arrays, structs and
// reference types) is TypeCode.Object. Null is TypeCode.Empty. The static
// typeof(T) fold (TypeCodeStatic) serves the real path; this covers a runtime Type.
int32_t dn2cpp_type_get_type_code(const Dn2CppTypeInfo* ti)
{
    if (ti == nullptr)
        return 0; // TypeCode.Empty
    if (ti->enumUnderlying != nullptr)
        ti = ti->enumUnderlying; // enum -> underlying integer type
    const char* n = ti->name;
    if (n == nullptr)
        return 1; // TypeCode.Object
    if (std::strcmp(n, "System.Boolean") == 0) return 3;
    if (std::strcmp(n, "System.Char") == 0) return 4;
    if (std::strcmp(n, "System.SByte") == 0) return 5;
    if (std::strcmp(n, "System.Byte") == 0) return 6;
    if (std::strcmp(n, "System.Int16") == 0) return 7;
    if (std::strcmp(n, "System.UInt16") == 0) return 8;
    if (std::strcmp(n, "System.Int32") == 0) return 9;
    if (std::strcmp(n, "System.UInt32") == 0) return 10;
    if (std::strcmp(n, "System.Int64") == 0) return 11;
    if (std::strcmp(n, "System.UInt64") == 0) return 12;
    if (std::strcmp(n, "System.Single") == 0) return 13;
    if (std::strcmp(n, "System.Double") == 0) return 14;
    if (std::strcmp(n, "System.Decimal") == 0) return 15;
    if (std::strcmp(n, "System.DateTime") == 0) return 16;
    if (std::strcmp(n, "System.DBNull") == 0) return 2;
    if (std::strcmp(n, "System.String") == 0) return 18;
    return 1; // TypeCode.Object
}

int32_t dn2cpp_object_has_component_size(Dn2CppObject* o)
{
    if (o == nullptr)
        return 0;
    const Dn2CppTypeInfo* t = o->type;
    return ((t->flags & DN2CPP_TF_ARRAY) != 0 || t == &dn2cpp_string_type) ? 1 : 0;
}

int32_t dn2cpp_type_is_value_type(const Dn2CppTypeInfo* ti)
{
    return (ti->flags & DN2CPP_TF_VALUETYPE) != 0 ? 1 : 0;
}

// .NET IsClass: a reference type — i.e. not a value type and not an interface.
// Arrays, delegates and System.Object are classes; enums/structs/primitives and
// interfaces are not.
int32_t dn2cpp_type_is_class(const Dn2CppTypeInfo* ti)
{
    return (ti->flags & (DN2CPP_TF_VALUETYPE | DN2CPP_TF_INTERFACE)) == 0 ? 1 : 0;
}

int32_t dn2cpp_type_is_primitive(const Dn2CppTypeInfo* ti)
{
    return (ti->flags & DN2CPP_TF_PRIMITIVE) != 0 ? 1 : 0;
}

Dn2CppType* dn2cpp_type_base_type(Dn2CppType* a)
{
    const Dn2CppTypeInfo* ti = dn2cpp_type_require(a);
    if (ti->base != nullptr)
        return dn2cpp_get_type_from_handle(ti->base);
    // Chain ended. A reference type's chain stops here instead of pointing at the
    // object handle, but .NET's base IS System.Object — synthesize it. Object
    // itself, interfaces, and value types report null/their own base (the latter
    // already had a non-null `base`, e.g. an enum -> System.Enum, so they don't
    // reach here).
    if (ti == &dn2cpp_object_type)
        return nullptr;
    if ((ti->flags & (DN2CPP_TF_INTERFACE | DN2CPP_TF_VALUETYPE)) != 0)
        return nullptr;
    // An array's .NET base is System.Array (then Object). Array type-infos carry
    // base=nullptr and the runtime has no static handle for the abstract Array
    // class (the emitter stamps DN2CPP_TF_SYSTEM_ARRAY on the emitted one), so
    // resolve it from the type registry — present whenever the program names
    // System.Array — and degrade to the System.Object synthesis when it is not.
    if ((ti->flags & DN2CPP_TF_ARRAY) != 0)
        if (const Dn2CppTypeInfo* arrTi = dn2cpp_type_registry_find("System.Array", 12))
            return dn2cpp_get_type_from_handle(arrTi);
    return dn2cpp_get_type_from_handle(&dn2cpp_object_type);
}

// Type.GetElementType(): the SZArray element's Type, or null for a non-array
// type — matching .NET, where GetElementType returns null when HasElementType is false.
// Only the per-element ti_arr_<T>/ti_md_<T> handles and the interned dynamic array
// identities carry a non-null elementType; the imprecise packed dn2cpp_array_n_type
// leaves it null (element unknown), so element typing is precise only along the typed
// path. (The shared ref/i4 handles carry their conservative object/int32 element.)
Dn2CppType* dn2cpp_type_get_element_type(Dn2CppType* a)
{
    const Dn2CppTypeInfo* et = dn2cpp_type_require(a)->elementType;
    return et != nullptr ? dn2cpp_get_type_from_handle(et) : nullptr;
}

// Type.GetArrayRank(): the array rank for an array type-info (1 for SZArray),
// throwing ArgumentException for a non-array, as .NET does. The shared array handles
// carry arrayRank 0 (trailing 0-fill) but are SZArrays, so fall back to 1.
int32_t dn2cpp_type_get_array_rank(Dn2CppType* a)
{
    // A null Type handle is the caller's NRE, not an ArgumentException. The
    // non-array is the ArgumentException real .NET raises (measured on CoreCLR:
    // "Must be an array type." for typeof(int) and for typeof(string) alike) —
    // catchable: a reflection walk asks this about types it does not control.
    if ((dn2cpp_type_require(a)->flags & DN2CPP_TF_ARRAY) == 0)
        dn2cpp_throw_argument();
    int32_t r = a->typeInfo->arrayRank;
    return r != 0 ? r : 1;
}

int32_t dn2cpp_type_is_assignable_from(Dn2CppType* a, Dn2CppType* b)
{
    // The RECEIVER's null is an NRE; the argument's is a plain false, as in .NET.
    dn2cpp_type_require(a);
    if (b == nullptr)
        return 0;
    // ONE rule with isinst (dn2cpp_typeinfo_assignable): a.IsAssignableFrom(b)
    // is exactly "is a b-headed instance an a?", so it must run the same walk a
    // cast runs — base chain + interface rows, generic variance, and the array
    // arms (System.Array, the non-generic DN2CPP_TF_ARRAY_ITF trio, the generic
    // collection interfaces with element covariance, array-to-array covariance).
    // A private walk here answered arrays with False while the cast succeeded,
    // which mis-classified string[] members in Newtonsoft's contract resolver.
    return dn2cpp_typeinfo_assignable(b->typeInfo, a->typeInfo);
}

// Type.IsAssignableTo(other): the argument-swapped IsAssignableFrom, and it
// cannot BE that call with the arguments swapped — the swap exchanges the two
// nulls, which mean opposite things. The receiver's null is the NRE; the target's
// answers false, exactly as .NET's `targetType?.IsAssignableFrom(this) ?? false`.
int32_t dn2cpp_type_is_assignable_to(Dn2CppType* t, Dn2CppType* target)
{
    dn2cpp_type_require(t);
    if (target == nullptr)
        return 0;
    return dn2cpp_type_is_assignable_from(target, t);
}

int32_t dn2cpp_type_is_instance_of_type(Dn2CppType* a, Dn2CppObject* obj)
{
    dn2cpp_type_require(a);
    if (obj == nullptr)
        return 0;
    const Dn2CppTypeInfo* ta = a->typeInfo;
    if (ta == &dn2cpp_object_type)
        return 1;
    return dn2cpp_isinst(obj, ta) != nullptr ? 1 : 0;
}

// Type.IsSubclassOf(c): true when `a` strictly derives from `c` (walk a's base chain,
// excluding a itself). Class inheritance only — interfaces don't count (matching .NET).
int32_t dn2cpp_type_is_subclass_of(Dn2CppType* a, Dn2CppType* c)
{
    dn2cpp_type_require(a);
    // Unlike the assignability pair, .NET's IsSubclassOf REJECTS a null argument
    // (ArgumentNullException) rather than answering false.
    if (c == nullptr)
        dn2cpp_throw_argument_null();
    const Dn2CppTypeInfo* ta = a->typeInfo;
    const Dn2CppTypeInfo* tc = c->typeInfo;
    // An array's real .NET base chain is System.Array -> System.Object, but array
    // type-infos carry base=nullptr — mirror dn2cpp_isinst's special cases (the
    // DN2CPP_TF_SYSTEM_ARRAY stamp / shared object handle). Strictness
    // holds: an array type-info never carries DN2CPP_TF_SYSTEM_ARRAY and is never
    // the object handle, so `ta` itself cannot be the target these arms accept.
    // No covariance here — string[].IsSubclassOf(object[]) is False in .NET too
    // (class inheritance only), and the chain walk below keeps that.
    if ((ta->flags & DN2CPP_TF_ARRAY) != 0
        && ((tc->flags & DN2CPP_TF_SYSTEM_ARRAY) != 0 || tc == &dn2cpp_object_type))
        return 1;
    for (const Dn2CppTypeInfo* t = ta->base; t != nullptr; t = t->base)
        if (t == tc)
            return 1;
    return 0;
}

// Type.Assembly identity. Assembly is modeled as the defining assembly's simple-name
// string; only hand-written CoreLib type-infos leave assemblyName null, so a null name
// resolves to System.Private.CoreLib. op_Equality is a name compare (the source-gen
// JsonConverter..ctor only asks "is this converter in the STJ assembly?").
const char* dn2cpp_type_assembly_name(Dn2CppType* a)
{
    const char* nm = dn2cpp_type_require(a)->assemblyName;
    return nm != nullptr ? nm : "System.Private.CoreLib";
}

int32_t dn2cpp_assembly_equals(const char* a, const char* b)
{
    if (a == b)
        return 1;
    if (a == nullptr || b == nullptr)
        return 0;
    return std::strcmp(a, b) == 0 ? 1 : 0;
}
