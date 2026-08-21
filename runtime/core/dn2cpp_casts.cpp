// dn2cpp_casts.cpp — dynamic type tests of the dn2cpp runtime:
// interface dispatch (+ its resolution cache),
// isinst / castclass (incl. variance), box/unbox, and delegates.

#include "dn2cpp_core.h"
#include <algorithm>
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

static int32_t dn2cpp_array_elem_assignable(const Dn2CppTypeInfo* se, const Dn2CppTypeInfo* de);
static int32_t dn2cpp_array_elem_covariant(const Dn2CppTypeInfo* se, const Dn2CppTypeInfo* de);

// Shared reference-element SZArray fallback dispatch table: the object-element
// interface map the generated init prologue installs, or nullptr when the program
// wired none (the walk's fallback arm is then inert and array dispatch aborts
// loudly). Set once at init before any managed code runs — same publication
// argument as the cache soundness note below.
static const Dn2CppInterfaceEntry* g_array_ref_fallback_itfs = nullptr;
static int32_t g_array_ref_fallback_itf_count = 0;

void dn2cpp_array_set_ref_fallback_interfaces(const Dn2CppInterfaceEntry* entries, int32_t count)
{
    g_array_ref_fallback_itfs = entries;
    g_array_ref_fallback_itf_count = count;
}

// Multi-dimensional (rank >= 2) array dispatch table, installed by the same init
// prologue. MD type-infos are always runtime-interned with interfaces == nullptr,
// so there is no per-element type-info to attach rows to: one table serves every
// element type and rank. That is sound because every row is one of the six
// non-generic interfaces a CLR MD array implements and every thunk goes through
// the element-agnostic System.Array reflection surface.
static const Dn2CppInterfaceEntry* g_array_md_fallback_itfs = nullptr;
static int32_t g_array_md_fallback_itf_count = 0;

void dn2cpp_array_set_md_fallback_interfaces(const Dn2CppInterfaceEntry* entries, int32_t count)
{
    g_array_md_fallback_itfs = entries;
    g_array_md_fallback_itf_count = count;
}

// Relation-only rows for the six non-generic array interfaces — the ENUMERATION
// counterpart of the two dispatch tables above. Their slot tables are null, so every
// dispatch reader here skips them (`slots != nullptr`) and they cannot shadow the
// maps above; only the enumerating reflection readers ask for them.
static const Dn2CppInterfaceEntry* g_array_nongeneric_itfs = nullptr;
static int32_t g_array_nongeneric_itf_count = 0;

void dn2cpp_array_set_nongeneric_interfaces(const Dn2CppInterfaceEntry* entries, int32_t count)
{
    g_array_nongeneric_itfs = entries;
    g_array_nongeneric_itf_count = count;
}

const Dn2CppInterfaceEntry* dn2cpp_array_nongeneric_interfaces(
    const Dn2CppTypeInfo* ti, int32_t* count)
{
    // Gated on the ARRAY flag rather than on the caller: a reader that forgot the
    // guard would report ICloneable on every type in the program. System.Array itself
    // is admitted beside it — it is an intrinsic type, so the emitter renders it as an
    // opaque shell with no interface table, and on real .NET these six ARE its whole
    // interface list.
    if (ti == nullptr
        || (ti->flags & (DN2CPP_TF_ARRAY | DN2CPP_TF_SYSTEM_ARRAY)) == 0
        || g_array_nongeneric_itfs == nullptr)
    {
        *count = 0;
        return nullptr;
    }
    *count = g_array_nongeneric_itf_count;
    return g_array_nongeneric_itfs;
}

// Is `itf` a closed generic whose definition declares a variant (`in`/`out`) type
// parameter — i.e. can a request for it be answered by a DIFFERENT instantiation? The
// pre-filter guarding every variant probe below, so the exact-match paths pay one flag
// test. DN2CPP_TF_COVARIANT is honored beside DN2CPP_TF_VARIANT: a type-info emitted
// against the older layout carries only the former (and no varianceMask).
static inline int32_t dn2cpp_itf_is_variant(const Dn2CppTypeInfo* itf)
{
    return itf != nullptr && itf->genericDef != nullptr
        && (itf->genericDef->flags & (DN2CPP_TF_VARIANT | DN2CPP_TF_COVARIANT)) != 0;
}

// Generic variance: does a type implementing interface `have` satisfy a request for the
// variant interface `want`? True when both are closed instantiations of the same generic
// definition and every type argument is assignable in the direction that parameter
// declares: `out` forward (IEnumerable<Cat> answers IEnumerable<Animal>), `in` backward
// (IComparer<Animal> answers IComparer<Cat>), invariant exactly. All arities and both
// directions — IGrouping<out K, out E> and IComparer<in T> are as much variance as
// IEnumerable<out T> is. Argument assignability reuses dn2cpp_array_elem_assignable,
// which holds value-type arguments invariant exactly as the CLR does.
static int32_t dn2cpp_itf_variant_match(const Dn2CppTypeInfo* have, const Dn2CppTypeInfo* want)
{
    const Dn2CppTypeInfo* def = want->genericDef;
    if (def == nullptr || have->genericDef != def)
        return 0;
    int32_t n = want->genericArgCount;
    if (n <= 0 || n > DN2CPP_VARIANCE_MAX_PARAMS || have->genericArgCount != n)
        return 0;
    int32_t mask = def->varianceMask;
    if (mask == 0)
    {
        // A definition emitted against the older layout: no per-parameter mask, and
        // DN2CPP_TF_COVARIANT meant "single covariant parameter" outright.
        if (n != 1 || (def->flags & DN2CPP_TF_COVARIANT) == 0)
            return 0;
        mask = DN2CPP_VAR_OUT;
    }
    for (int32_t i = 0; i < n; i++)
    {
        const Dn2CppTypeInfo* h = have->genericArgs[i];
        const Dn2CppTypeInfo* w = want->genericArgs[i];
        switch ((mask >> (2 * i)) & 3)
        {
            case DN2CPP_VAR_OUT:
                if (!dn2cpp_array_elem_assignable(h, w))
                    return 0;
                break;
            case DN2CPP_VAR_IN:
                if (!dn2cpp_array_elem_assignable(w, h))
                    return 0;
                break;
            default:
                if (h != w)
                    return 0;
                break;
        }
    }
    return 1;
}

// (type, query-type) → answer side cache for the dispatch/cast walks below
// (dn2cpp_try_resolve_interface, dn2cpp_isinst). Generated code resolves an
// interface per CALL SITE (`dn2cpp_resolve_interface(recv->type, &ti_...)`), so
// the base-chain × interface-table walk — and, for a variant request, the
// variance match on every row — runs once per interface call; the cache turns
// the steady state into one hashed probe.
//
// Soundness rests on three properties, each load-bearing:
//  - The answer is a pure function of the (type, query) pair: a type-info's base
//    chain and interface table never change after managed code can run. Every
//    mutation of a published type-info, and both non-type-info walk inputs (the
//    ref-element and MD array fallback tables), sit in the generated init prologue.
//    Runtime-constructed type-infos are fully built before publication, and patch
//    re-loading is append-only — a new type-info is minted, never a rewrite.
//  - Keys and values are immortal: every Dn2CppTypeInfo is a data-segment static or
//    GC-rooted forever, and a slots table points into its owner. So the cache holds
//    no managed pointer the GC must see, can never dangle, and needs no invalidation.
//  - Entries are write-once and published with atomics: a writer claims `type` by
//    CAS, stores the value, then release-stores `query`. Every racy intermediate
//    state degrades to a MISS, never a wrong hit. Fixed size, no eviction: once
//    full, new pairs stay uncached.
namespace
{
template <typename V>
struct Dn2CppTypePairCache
{
    static constexpr uint32_t kSize = 2048;      // power of two; ~48 KB of BSS
    static constexpr uint32_t kProbeLimit = 8;

    struct Entry
    {
        // Invariant: callers pass non-null query keys — a claimed-but-unpublished
        // entry's query == nullptr doubles as the "not yet published" marker.
        std::atomic<const Dn2CppTypeInfo*> type{nullptr};
        std::atomic<const Dn2CppTypeInfo*> query{nullptr};
        std::atomic<V> value{V{}};
    };
    Entry entries[kSize];

    static uint32_t Slot(const Dn2CppTypeInfo* t, const Dn2CppTypeInfo* q)
    {
        uintptr_t a = reinterpret_cast<uintptr_t>(t) >> 4;
        uintptr_t b = reinterpret_cast<uintptr_t>(q) >> 4;
        return static_cast<uint32_t>((a * 31u + b) & (kSize - 1));
    }

    bool Lookup(const Dn2CppTypeInfo* t, const Dn2CppTypeInfo* q, V* out) const
    {
        uint32_t slot = Slot(t, q);
        for (uint32_t i = 0; i < kProbeLimit; i++)
        {
            const Entry& e = entries[(slot + i) & (kSize - 1)];
            const Dn2CppTypeInfo* et = e.type.load(std::memory_order_relaxed);
            if (et == nullptr)
                return false; // empty slot ends the probe chain
            if (et == t && e.query.load(std::memory_order_acquire) == q)
            {
                *out = e.value.load(std::memory_order_relaxed);
                return true;
            }
        }
        return false;
    }

    void Insert(const Dn2CppTypeInfo* t, const Dn2CppTypeInfo* q, V value)
    {
        uint32_t slot = Slot(t, q);
        for (uint32_t i = 0; i < kProbeLimit; i++)
        {
            Entry& e = entries[(slot + i) & (kSize - 1)];
            const Dn2CppTypeInfo* expected = nullptr;
            if (e.type.compare_exchange_strong(expected, t,
                    std::memory_order_acq_rel, std::memory_order_relaxed))
            {
                e.value.store(value, std::memory_order_relaxed);
                e.query.store(q, std::memory_order_release);
                return;
            }
            if (expected == t && e.query.load(std::memory_order_acquire) == q)
                return; // another thread published this same pair
        }
        // Probe window exhausted: leave the pair uncached — lookups fall back
        // to the walk, so a full table costs time, never correctness.
    }
};

// Plain statics, not dn2cpp_never_destroyed: all-atomic PODs are constant-
// initialized into BSS and trivially destructible, so there is no destruction
// order to defend against and no dynamic allocation at startup.
Dn2CppTypePairCache<const void**> g_itf_slots_cache;
Dn2CppTypePairCache<uintptr_t> g_isinst_cache;
}

// ==== Interface dispatch on a boxed built-in ====
// The hand-written primitive / Decimal / date-time type-infos are const, so no init
// prologue can wire a map onto them the way dn2cpp_enum_set_interfaces does for enums.
// The DISPATCH set is therefore keyed off the same predicate pair the type TEST uses
// (dn2cpp_wellknown_itf_mask / _bit, below), so the two cannot drift.
static int32_t dn2cpp_wellknown_itf_bit(const Dn2CppTypeInfo* ti);
static int32_t dn2cpp_wellknown_itf_mask(const Dn2CppTypeInfo* st);
static int32_t dn2cpp_wellknown_self_generic_itf(const Dn2CppTypeInfo* st, const Dn2CppTypeInfo* ti);
static int32_t dn2cpp_wellknown_self_generic_canon(const Dn2CppTypeInfo* st, const Dn2CppTypeInfo* ti);

// Every `bbi` thunk takes the box pointer as its receiver, exactly as an emitted
// unboxing thunk does, and an NFI pointer wherever the interface declares an
// IFormatProvider (that parameter lowers to the NFI singletons everywhere).

// IFormattable.ToString(string, IFormatProvider). Sub-word integers widen to an int32
// payload in the box (see dn2cpp_object_tostring), so their reads are 4-byte.
static Dn2CppString* dn2cpp_bbi_format(Dn2CppObject* box, Dn2CppString* fmt,
                                       const Dn2CppNumberFormatInfo* nfi)
{
    const Dn2CppTypeInfo* t = box->type;
    const void* p = box + 1; // boxed value sits right after the header
    if (t == &dn2cpp_int32_type)
        return dn2cpp_format_int_c(*reinterpret_cast<const int32_t*>(p), 4, fmt, nfi);
    // Sub-words carry their own byte width so a hex/binary mask covers exactly
    // their two's-complement pattern, as at the direct-call arms.
    if (t == &dn2cpp_sbyte_type)
        return dn2cpp_format_int_c(static_cast<int8_t>(*reinterpret_cast<const int32_t*>(p)), 1, fmt, nfi);
    if (t == &dn2cpp_int16_type)
        return dn2cpp_format_int_c(static_cast<int16_t>(*reinterpret_cast<const int32_t*>(p)), 2, fmt, nfi);
    if (t == &dn2cpp_byte_type)
        return dn2cpp_format_uint_c(static_cast<uint8_t>(*reinterpret_cast<const int32_t*>(p)), 1, fmt, nfi);
    if (t == &dn2cpp_uint16_type)
        return dn2cpp_format_uint_c(static_cast<uint16_t>(*reinterpret_cast<const int32_t*>(p)), 2, fmt, nfi);
    if (t == &dn2cpp_uint32_type)
        return dn2cpp_format_uint_c(*reinterpret_cast<const uint32_t*>(p), 4, fmt, nfi);
    if (t == &dn2cpp_int64_type)
        return dn2cpp_format_int_c(*reinterpret_cast<const int64_t*>(p), 8, fmt, nfi);
    if (t == &dn2cpp_uint64_type)
        return dn2cpp_format_uint_c(*reinterpret_cast<const uint64_t*>(p), 8, fmt, nfi);
    if (t == &dn2cpp_intptr_type)
        return dn2cpp_format_int_c(*reinterpret_cast<const intptr_t*>(p), 8, fmt, nfi);
    if (t == &dn2cpp_uintptr_type)
        return dn2cpp_format_uint_c(*reinterpret_cast<const uintptr_t*>(p), 8, fmt, nfi);
    if (t == &dn2cpp_double_type)
        return dn2cpp_format_r8_c(*reinterpret_cast<const double*>(p), fmt, nfi);
    if (t == &dn2cpp_single_type)
        return dn2cpp_format_r4_c(*reinterpret_cast<const float*>(p), fmt, nfi);
    if (t->formatspec != nullptr)
        return t->formatspec(box, fmt, nfi);
    // No spec asked for (IConvertible.ToString routes here that way), or Char, whose
    // real ToString(format, provider) ignores the spec: the default text.
    if (fmt == nullptr || fmt->length == 0 || t == &dn2cpp_char_type)
        return dn2cpp_object_tostring(box);
    // A spec this type has no formatter for. dn2cpp_format_hole_value's tail answers
    // the default text here; a dispatch mouth must not, since the caller ASKED for the
    // spec and would get a plausible wrong string carrying no diagnostic.
    dn2cpp_throw_platform_not_supported(
        (std::string("IFormattable.ToString: '") + (t->name != nullptr ? t->name : "<unknown>")
         + "' has no format-spec formatter in this runtime").c_str());
}

// IComparable.CompareTo(object) — the shape every built-in's real body has: null sorts
// last, a foreign runtime type is an ArgumentException, and the same type orders
// through the ONE ladder. That ladder is asked with a null IComparable ti, so its own
// interface probe is skipped and it cannot re-enter this thunk.
static int32_t dn2cpp_bbi_compareto(Dn2CppObject* box, Dn2CppObject* other)
{
    if (other == nullptr)
        return 1;
    if (other->type != box->type)
        dn2cpp_throw_argument_msg("Object must be of the same type as the value being compared.");
    return dn2cpp_object_compare(box, other, nullptr);
}

// ISpanFormattable.TryFormat(Span<char>, out int, ReadOnlySpan<char>, IFormatProvider):
// format, then the .NET fits / does-not-fit copy contract. The spans arrive by value
// (Dn2CppItfCharSpan, shared with the boxed-enum slot in dn2cpp_system_reflection.cpp).
static int32_t dn2cpp_bbi_try_format(Dn2CppObject* box, Dn2CppItfCharSpan dest, int32_t* written,
                                     Dn2CppItfCharSpan fmt, const Dn2CppNumberFormatInfo* nfi)
{
    Dn2CppString* spec = fmt.length > 0 ? dn2cpp_string_from_chars(fmt.ptr, fmt.length) : nullptr;
    return dn2cpp_string_try_copy_to_span(dn2cpp_bbi_format(box, spec, nfi),
        dest.ptr, dest.length, written);
}

// IConvertible. Each To* reads the box through the same helper the statically typed
// Convert.To* intrinsic uses, so a value converted at a call site and the same value
// converted through this mouth agree by construction (widths range-checked, a floating
// source banker's-rounded). Char rides the I4 stack slot, hence int32_t.
static int32_t dn2cpp_bbi_conv_typecode(Dn2CppObject* box)
{
    return dn2cpp_convert_get_type_code(box);
}
static int32_t dn2cpp_bbi_conv_bool(Dn2CppObject* box, const Dn2CppNumberFormatInfo*)
{
    return dn2cpp_convert_obj_to_bool(box);
}
static int32_t dn2cpp_bbi_conv_char(Dn2CppObject* box, const Dn2CppNumberFormatInfo*)
{
    return static_cast<int32_t>(dn2cpp_convert_i64_checked(dn2cpp_convert_obj_to_i64(box), 0, 65535));
}
static int32_t dn2cpp_bbi_conv_i1(Dn2CppObject* box, const Dn2CppNumberFormatInfo*)
{
    return static_cast<int32_t>(dn2cpp_convert_i64_checked(dn2cpp_convert_obj_to_i64(box), -128, 127));
}
static int32_t dn2cpp_bbi_conv_u1(Dn2CppObject* box, const Dn2CppNumberFormatInfo*)
{
    return static_cast<int32_t>(dn2cpp_convert_i64_checked(dn2cpp_convert_obj_to_i64(box), 0, 255));
}
static int32_t dn2cpp_bbi_conv_i2(Dn2CppObject* box, const Dn2CppNumberFormatInfo*)
{
    return static_cast<int32_t>(dn2cpp_convert_i64_checked(dn2cpp_convert_obj_to_i64(box), -32768, 32767));
}
static int32_t dn2cpp_bbi_conv_u2(Dn2CppObject* box, const Dn2CppNumberFormatInfo*)
{
    return static_cast<int32_t>(dn2cpp_convert_i64_checked(dn2cpp_convert_obj_to_i64(box), 0, 65535));
}
static int32_t dn2cpp_bbi_conv_i4(Dn2CppObject* box, const Dn2CppNumberFormatInfo*)
{
    return static_cast<int32_t>(dn2cpp_convert_i64_checked(dn2cpp_convert_obj_to_i64(box),
        INT64_C(-2147483648), INT64_C(2147483647)));
}
static uint32_t dn2cpp_bbi_conv_u4(Dn2CppObject* box, const Dn2CppNumberFormatInfo*)
{
    return static_cast<uint32_t>(dn2cpp_convert_i64_checked(dn2cpp_convert_obj_to_i64(box),
        0, INT64_C(4294967295)));
}
static int64_t dn2cpp_bbi_conv_i8(Dn2CppObject* box, const Dn2CppNumberFormatInfo*)
{
    return dn2cpp_convert_obj_to_i64(box);
}
static uint64_t dn2cpp_bbi_conv_u8(Dn2CppObject* box, const Dn2CppNumberFormatInfo*)
{
    return dn2cpp_convert_i64_to_u64(dn2cpp_convert_obj_to_i64(box));
}
static float dn2cpp_bbi_conv_r4(Dn2CppObject* box, const Dn2CppNumberFormatInfo*)
{
    return static_cast<float>(dn2cpp_convert_obj_to_f64(box));
}
static double dn2cpp_bbi_conv_r8(Dn2CppObject* box, const Dn2CppNumberFormatInfo*)
{
    return dn2cpp_convert_obj_to_f64(box);
}
// Decimal and DateTime have no widening read of their own; the ChangeType matrix owns
// both (where a numeric source has no DateTime conversion, as in real .NET).
static Dn2CppDecimal dn2cpp_bbi_conv_dec(Dn2CppObject* box, const Dn2CppNumberFormatInfo*)
{
    return *reinterpret_cast<const Dn2CppDecimal*>(dn2cpp_convert_change_type_code(box, 15) + 1);
}
static Dn2CppDateTime dn2cpp_bbi_conv_dt(Dn2CppObject* box, const Dn2CppNumberFormatInfo*)
{
    return *reinterpret_cast<const Dn2CppDateTime*>(dn2cpp_convert_change_type_code(box, 16) + 1);
}
static Dn2CppString* dn2cpp_bbi_conv_str(Dn2CppObject* box, const Dn2CppNumberFormatInfo* nfi)
{
    return dn2cpp_bbi_format(box, nullptr, nfi);
}
static Dn2CppObject* dn2cpp_bbi_conv_type(Dn2CppObject* box, Dn2CppType* target,
                                          const Dn2CppNumberFormatInfo*)
{
    return dn2cpp_convert_change_type(box, target);
}

// IComparable<Self>.CompareTo(Self) / IEquatable<Self>.Equals(Self). The argument
// arrives UNBOXED and by value, so a thunk is a function of the C++ type the emitter
// passes (CppTypes.Of), not of the CLR type — seven scalar groups plus the five
// hand-written value structs. Re-boxing it with the RECEIVER's type-info is what makes
// one thunk per group enough: dn2cpp_wellknown_self_generic_itf admits the pair only at
// T = Self, so the two operands are the same type by construction. Both then delegate to
// the ONE ladder (dn2cpp_object_compare / dn2cpp_object_equals), which is what keeps a
// value ordered here and one ordered at a static call site from disagreeing.
#define DN2CPP_BBI_SELF_GEN(sfx, CT)                                                      \
    static int32_t dn2cpp_bbi_selfcmp_##sfx(Dn2CppObject* box, CT v)                      \
    { return dn2cpp_object_compare(box, dn2cpp_box(box->type, &v, sizeof v), nullptr); }  \
    static int32_t dn2cpp_bbi_selfeq_##sfx(Dn2CppObject* box, CT v)                       \
    { return dn2cpp_object_equals(box, dn2cpp_box(box->type, &v, sizeof v)); }
DN2CPP_BBI_SELF_GEN(i4, int32_t)   // bool/char/sbyte/byte/short/ushort/int all ride int32
DN2CPP_BBI_SELF_GEN(u4, uint32_t)
DN2CPP_BBI_SELF_GEN(i8, int64_t)
DN2CPP_BBI_SELF_GEN(u8, uint64_t)
DN2CPP_BBI_SELF_GEN(r4, float)
DN2CPP_BBI_SELF_GEN(r8, double)
DN2CPP_BBI_SELF_GEN(ip, intptr_t) // IntPtr and UIntPtr; the receiver's ti carries the sign
DN2CPP_BBI_SELF_GEN(dec, Dn2CppDecimal)
DN2CPP_BBI_SELF_GEN(dt, Dn2CppDateTime)
DN2CPP_BBI_SELF_GEN(dto, Dn2CppDateTimeOffset)
DN2CPP_BBI_SELF_GEN(ts, Dn2CppTimeSpan)
DN2CPP_BBI_SELF_GEN(don, Dn2CppDateOnly)
DN2CPP_BBI_SELF_GEN(ton, Dn2CppTimeOnly)
#undef DN2CPP_BBI_SELF_GEN

namespace {
// The group a built-in's self-instantiated IComparable/IEquatable dispatches through.
// The name set must stay the value-type half of dn2cpp_wellknown_itf_mask: that mask is
// what the pair predicate gates on, so a name here with no mask entry is unreachable and
// a mask entry with no row here is a type test whose dispatch aborts.
struct Dn2CppBbiSelfRow { const char* type; const void* cmp; const void* eq; };
#define DN2CPP_BBI_SELF_ROW(nm, sfx) \
    { nm, reinterpret_cast<const void*>(&dn2cpp_bbi_selfcmp_##sfx), \
          reinterpret_cast<const void*>(&dn2cpp_bbi_selfeq_##sfx) }
const Dn2CppBbiSelfRow kBbiSelfRows[] = {
    DN2CPP_BBI_SELF_ROW("System.Boolean", i4), DN2CPP_BBI_SELF_ROW("System.Char", i4),
    DN2CPP_BBI_SELF_ROW("System.SByte", i4), DN2CPP_BBI_SELF_ROW("System.Byte", i4),
    DN2CPP_BBI_SELF_ROW("System.Int16", i4), DN2CPP_BBI_SELF_ROW("System.UInt16", i4),
    DN2CPP_BBI_SELF_ROW("System.Int32", i4), DN2CPP_BBI_SELF_ROW("System.UInt32", u4),
    DN2CPP_BBI_SELF_ROW("System.Int64", i8), DN2CPP_BBI_SELF_ROW("System.UInt64", u8),
    DN2CPP_BBI_SELF_ROW("System.Single", r4), DN2CPP_BBI_SELF_ROW("System.Double", r8),
    DN2CPP_BBI_SELF_ROW("System.IntPtr", ip), DN2CPP_BBI_SELF_ROW("System.UIntPtr", ip),
    DN2CPP_BBI_SELF_ROW("System.Decimal", dec), DN2CPP_BBI_SELF_ROW("System.DateTime", dt),
    DN2CPP_BBI_SELF_ROW("System.DateTimeOffset", dto),
    DN2CPP_BBI_SELF_ROW("System.TimeSpan", ts),
    DN2CPP_BBI_SELF_ROW("System.DateOnly", don),
    DN2CPP_BBI_SELF_ROW("System.TimeOnly", ton),
};
#undef DN2CPP_BBI_SELF_ROW

// The thunk serving one declared method of one well-known interface. Keyed by NAME,
// because the SLOT is not ours to choose: the dispatch site indexes by the
// transpiler's per-interface declaration order, so this table is turned into a slot
// array through the interface's own emitted method rows.
struct Dn2CppBbiRow { const char* itf; const char* method; const void* thunk; };

const Dn2CppBbiRow kBbiRows[] = {
    { "System.IFormattable", "ToString", reinterpret_cast<const void*>(&dn2cpp_bbi_format) },
    { "System.IComparable", "CompareTo", reinterpret_cast<const void*>(&dn2cpp_bbi_compareto) },
    { "System.ISpanFormattable", "TryFormat", reinterpret_cast<const void*>(&dn2cpp_bbi_try_format) },
    { "System.IConvertible", "GetTypeCode", reinterpret_cast<const void*>(&dn2cpp_bbi_conv_typecode) },
    { "System.IConvertible", "ToBoolean", reinterpret_cast<const void*>(&dn2cpp_bbi_conv_bool) },
    { "System.IConvertible", "ToChar", reinterpret_cast<const void*>(&dn2cpp_bbi_conv_char) },
    { "System.IConvertible", "ToSByte", reinterpret_cast<const void*>(&dn2cpp_bbi_conv_i1) },
    { "System.IConvertible", "ToByte", reinterpret_cast<const void*>(&dn2cpp_bbi_conv_u1) },
    { "System.IConvertible", "ToInt16", reinterpret_cast<const void*>(&dn2cpp_bbi_conv_i2) },
    { "System.IConvertible", "ToUInt16", reinterpret_cast<const void*>(&dn2cpp_bbi_conv_u2) },
    { "System.IConvertible", "ToInt32", reinterpret_cast<const void*>(&dn2cpp_bbi_conv_i4) },
    { "System.IConvertible", "ToUInt32", reinterpret_cast<const void*>(&dn2cpp_bbi_conv_u4) },
    { "System.IConvertible", "ToInt64", reinterpret_cast<const void*>(&dn2cpp_bbi_conv_i8) },
    { "System.IConvertible", "ToUInt64", reinterpret_cast<const void*>(&dn2cpp_bbi_conv_u8) },
    { "System.IConvertible", "ToSingle", reinterpret_cast<const void*>(&dn2cpp_bbi_conv_r4) },
    { "System.IConvertible", "ToDouble", reinterpret_cast<const void*>(&dn2cpp_bbi_conv_r8) },
    { "System.IConvertible", "ToDecimal", reinterpret_cast<const void*>(&dn2cpp_bbi_conv_dec) },
    { "System.IConvertible", "ToDateTime", reinterpret_cast<const void*>(&dn2cpp_bbi_conv_dt) },
    { "System.IConvertible", "ToString", reinterpret_cast<const void*>(&dn2cpp_bbi_conv_str) },
    { "System.IConvertible", "ToType", reinterpret_cast<const void*>(&dn2cpp_bbi_conv_type) },
};

// One built slot array per (interface TYPE-INFO, self argument), built once and never
// freed: program-lifetime, like every other dispatch table. A LIST rather than a fixed
// array because the self-instantiated IComparable<T>/IEquatable<T> rows put no bound on
// the count — a full table would fall back to "no map", i.e. a dispatch abort on a pair
// the type test admits. `self` is null for the four non-generic interfaces, whose slots
// are a function of the interface alone.
struct Dn2CppBbiTable
{
    const Dn2CppTypeInfo* itf;
    const Dn2CppTypeInfo* self;
    const void** slots;
    Dn2CppBbiTable* next;
};
std::mutex g_bbi_mutex;
Dn2CppBbiTable* g_bbi_tables;

// `selfKind` (1 = IComparable<T>, 2 = IEquatable<T>, 0 = neither) and `self` come from
// the caller because the canonical form carries neither: a shared body's dispatch names
// the group's canonical interface, which has no generic arguments to read.
const void* dn2cpp_bbi_thunk(const Dn2CppTypeInfo* itf, const char* method,
                             int32_t selfKind, const Dn2CppTypeInfo* self)
{
    if (itf->name != nullptr)
        for (const auto& r : kBbiRows)
            if (std::strcmp(r.itf, itf->name) == 0 && std::strcmp(r.method, method) == 0)
                return r.thunk;
    // Each of the two declares exactly one method, so the method name is as much of an
    // identification as the definition's name is.
    if (selfKind == 0 || self == nullptr || self->name == nullptr
        || std::strcmp(method, selfKind == 1 ? "CompareTo" : "Equals") != 0)
        return nullptr;
    for (const auto& r : kBbiSelfRows)
        if (std::strcmp(r.type, self->name) == 0)
            return selfKind == 1 ? r.cmp : r.eq;
    return nullptr;
}
} // namespace

// The slot array for `itf` over a boxed built-in, built from the interface's OWN
// emitted method rows so slot i is by construction the method the call site put at i.
// A hardcoded order would dispatch ToDouble where ToInt32 was asked the day a CoreLib
// reordered the interface, and say nothing. A declared method the table does not name
// degrades to the named dispatch trap, as the boxed-enum map's unreached slots do.
static const void** dn2cpp_bbi_slots(const Dn2CppTypeInfo* itf, int32_t selfKind,
                                     const Dn2CppTypeInfo* self)
{
    std::lock_guard<std::mutex> lock(g_bbi_mutex);
    for (Dn2CppBbiTable* e = g_bbi_tables; e != nullptr; e = e->next)
        if (e->itf == itf && e->self == self)
            return e->slots;
    const void** slots = nullptr;
    if (itf->methods != nullptr && itf->methodCount > 0)
    {
        slots = new const void*[static_cast<size_t>(itf->methodCount)];
        for (int32_t i = 0; i < itf->methodCount; i++)
            slots[i] = reinterpret_cast<const void*>(&dn2cpp_itf_slot_missing_anon);
        for (int32_t i = 0; i < itf->methodCount; i++)
        {
            int32_t s = itf->methods[i].vtableSlot;
            if (s < 0 || s >= itf->methodCount)
                continue;
            if (const void* thunk = dn2cpp_bbi_thunk(itf, itf->methods[i].name, selfKind, self))
                slots[s] = thunk;
        }
    }
    else
    {
        // No method rows to read (a metadata-stripped interface, or a shared body's
        // canonical one). Sound only for an interface the table gives exactly ONE
        // method: its slot can only be 0. The two self-instantiated generics declare
        // one each, so they qualify too.
        const void* only = nullptr;
        int32_t rows = 0;
        if (itf->name != nullptr)
            for (const auto& r : kBbiRows)
                if (std::strcmp(r.itf, itf->name) == 0)
                {
                    only = r.thunk;
                    rows++;
                }
        if (rows == 0)
            if (const void* g = dn2cpp_bbi_thunk(itf, selfKind == 1 ? "CompareTo" : "Equals",
                                                 selfKind, self))
            {
                only = g;
                rows = 1;
            }
        if (rows == 1)
            slots = new const void*[1]{ only };
    }
    if (slots == nullptr)
        return nullptr;
    g_bbi_tables = new Dn2CppBbiTable{ itf, self, slots, g_bbi_tables };
    return slots;
}

// The uncached base-chain × interface-table walk behind
// dn2cpp_try_resolve_interface (which fronts it with g_itf_slots_cache).
//
// A row whose slots pointer is null is RELATION-ONLY (an interface or abstract
// class's table, emitted so IsAssignableFrom / GetInterfaces / isinst see the
// relation; see CppEmitter.RenderItfTables) and every slot-resolving loop here
// skips it: it must not shadow a real slot table deeper in the base chain (an
// abstract base's relation row over a concrete ancestor's implementation —
// dn2cpp_try_resolve_interface is called with non-receiver types, e.g. the
// interp's BPI base resolution), and it must never be handed to a dispatch
// site. A real 0-method interface row still resolves — its slots pointer is a
// non-null pooled dummy, never nullptr.
static const void** dn2cpp_resolve_interface_walk(const Dn2CppTypeInfo* t, const Dn2CppTypeInfo* itf)
{
    for (const Dn2CppTypeInfo* c = t; c != nullptr; c = c->base)
    {
        for (int32_t i = 0; i < c->interfaceCount; i++)
        {
            if (c->interfaces[i].itf == itf && c->interfaces[i].slots != nullptr)
                return c->interfaces[i].slots;
        }
    }
    // Variant fallback: a request for a variant I<X> is served by an implemented I<Y>
    // whose arguments sit the right way round (see dn2cpp_itf_variant_match). Only
    // entered when itf is a variant closed generic, so the exact path above is unchanged.
    // An argument may itself be a variant instantiation (I<I<Cat>> answering I<I<Animal>>):
    // dn2cpp_array_elem_assignable recurses into dn2cpp_itf_variant_match for that, each
    // nested level re-applying its own definition's mask. The transpiler's reachability
    // closure runs the same rule at every depth (Compilation's ReachVariantItfImpl ->
    // VariantMatches -> RefAssignable) — it has to, or the row this lands on holds an
    // unfilled slot.
    if (dn2cpp_itf_is_variant(itf))
        for (const Dn2CppTypeInfo* c = t; c != nullptr; c = c->base)
            for (int32_t i = 0; i < c->interfaceCount; i++)
                if (c->interfaces[i].slots != nullptr
                    && dn2cpp_itf_variant_match(c->interfaces[i].itf, itf))
                    return c->interfaces[i].slots;
    // Reference-element SZArray fallback: a rank-1 array of reference elements
    // services the SZArray collection interfaces through the shared object-element
    // table even when its own per-element map was never wired. Sound because every
    // reference element shares one C++ layout, so the object-keyed thunks read the
    // same words a per-element thunk would. Exact row match answers first; a request
    // for a different single-argument instantiation of the five generic collection
    // definitions answers when the element is covariant-compatible with the argument
    // — the CLR's array-interface rule (Cat[] is IList<Animal>), which variance alone
    // cannot serve for the invariant IList<T>/ICollection<T>. The VALUETYPE reject is
    // load-bearing: value layouts differ per element, so a silent object-thunk answer
    // would read garbage — keep it a loud dispatch abort.
    if ((t->flags & DN2CPP_TF_ARRAY) != 0 && t->arrayRank == 1
        && g_array_ref_fallback_itfs != nullptr
        // The imprecise packed handle is a Dn2CppArrayN of unknown element: the
        // object-keyed thunks would read its packed payload as pointers. The
        // null-elementType coercion below is for dn2cpp_array_ref_type alone, whose
        // layout the thunks were written against.
        && t != &dn2cpp_array_n_type)
    {
        const Dn2CppTypeInfo* e = t->elementType != nullptr ? t->elementType : &dn2cpp_object_type;
        if ((e->flags & DN2CPP_TF_VALUETYPE) == 0)
        {
            for (int32_t i = 0; i < g_array_ref_fallback_itf_count; i++)
                if (g_array_ref_fallback_itfs[i].itf == itf)
                    return g_array_ref_fallback_itfs[i].slots;
            if (itf->genericDef != nullptr
                && (itf->genericDef->flags & DN2CPP_TF_ARRAY_GEN_ITF) != 0
                && itf->genericArgCount == 1
                && dn2cpp_array_elem_covariant(e, itf->genericArgs[0]))
                for (int32_t i = 0; i < g_array_ref_fallback_itf_count; i++)
                    if (g_array_ref_fallback_itfs[i].itf->genericDef == itf->genericDef)
                        return g_array_ref_fallback_itfs[i].slots;
        }
    }
    // Multi-dimensional (rank >= 2) array dispatch: an MD array's runtime-interned
    // type-info carries interfaces == nullptr, so EVERY dispatch on one lands here
    // and is served from the one shared table. Exact row match only — a CLR MD array
    // implements exactly the six non-generic interfaces the table carries, so there
    // is no variance or element-covariance arm — and the thunks wrap the receiver in
    // the element-agnostic MDArrayEnumerable, sound for every element type and rank
    // because all element access goes through the System.Array reflection surface.
    if ((t->flags & DN2CPP_TF_ARRAY) != 0 && t->arrayRank > 1
        && g_array_md_fallback_itfs != nullptr)
    {
        for (int32_t i = 0; i < g_array_md_fallback_itf_count; i++)
            if (g_array_md_fallback_itfs[i].itf == itf)
                return g_array_md_fallback_itfs[i].slots;
    }
    // Ref-erased wrapper rows: the SZArrayEnumerable<object> wrapper the fallback
    // table hands back is dispatched at the CALLER's element typing, which variance
    // cannot serve (covariance runs object -> Attribute, the wrong way). A type-info
    // flagged DN2CPP_TF_REF_ERASED_ITF declares its closed-generic rows
    // element-erased: any instantiation of the same definition whose arguments are
    // ALL reference types is answered by the implemented row. A value-type argument
    // is rejected (an erased row cannot box/unbox), and the isinst walk never enters
    // here, so type tests are unchanged.
    if ((t->flags & DN2CPP_TF_REF_ERASED_ITF) != 0 && itf->genericDef != nullptr)
        for (const Dn2CppTypeInfo* c = t; c != nullptr; c = c->base)
            for (int32_t i = 0; i < c->interfaceCount; i++)
            {
                const Dn2CppTypeInfo* have = c->interfaces[i].itf;
                if (c->interfaces[i].slots == nullptr
                    || have->genericDef != itf->genericDef
                    || have->genericArgCount != itf->genericArgCount)
                    continue;
                int32_t allRef = 1;
                for (int32_t a = 0; a < itf->genericArgCount; a++)
                    if (itf->genericArgs[a] == nullptr
                        || (itf->genericArgs[a]->flags & DN2CPP_TF_VALUETYPE) != 0)
                    {
                        allRef = 0;
                        break;
                    }
                if (allRef)
                    return c->interfaces[i].slots;
            }
    // Boxed built-in dispatch: the hand-written primitive and formatspec-slotted
    // type-infos take no map. The gate is the type TEST's own predicate pair, so the
    // set that answers `is IConvertible` and the set that can dispatch one are the
    // SAME set. Last arm: a real map always wins.
    if ((dn2cpp_wellknown_itf_mask(t) & dn2cpp_wellknown_itf_bit(itf)) != 0)
        return dn2cpp_bbi_slots(itf, 0, nullptr);
    // The self-instantiated generic pair, in both the concrete and the canonical form.
    // Its thunks are chosen by the ARGUMENT, which the canonical form does not carry —
    // hence the receiver, and hence a slot table keyed by the pair rather than by the
    // interface alone.
    if (int32_t k = dn2cpp_wellknown_self_generic_itf(t, itf))
        return dn2cpp_bbi_slots(itf, k, t);
    if (int32_t k = dn2cpp_wellknown_self_generic_canon(t, itf))
        return dn2cpp_bbi_slots(itf, k, t);
    return nullptr;
}

// The cache probe + walk behind dn2cpp_try_resolve_interface's fast path.
// Kept out of line so the fast path stays small enough to inline into the
// per-call-site dn2cpp_resolve_interface entry. The nullptr answer is cached
// like any other (presence lives in the key words, so a cached "does not
// implement" is distinguishable from an empty entry).
static const void** dn2cpp_resolve_interface_cached(const Dn2CppTypeInfo* t, const Dn2CppTypeInfo* itf)
{
    const void** slots;
    if (g_itf_slots_cache.Lookup(t, itf, &slots))
        return slots;
    slots = dn2cpp_resolve_interface_walk(t, itf);
    g_itf_slots_cache.Insert(t, itf, slots);
    return slots;
}

// Returns the interface slot table for `itf` on `t`, or nullptr if `t` does not
// implement it (no fail). Used to discriminate a real user IEqualityComparer<T>
// (has the interface map) from the default-string-dict NonRandomizedString
// EqualityComparer wrapper, which dn2cpp emits as an opaque struct with no map —
// a non-null _comparer that must still fall back to the default op.
const void** dn2cpp_try_resolve_interface(const Dn2CppTypeInfo* t, const Dn2CppTypeInfo* itf)
{
    // Fast path ahead of the cache: an exact row in the receiver type's OWN
    // table. That is the dominant dispatch shape, it is exactly what the
    // pre-cache code did first, and for a short table it is cheaper than a
    // cache probe — measured on a 1-row table, probing first costs ~25% on a
    // tight dispatch loop. The cache earns its keep on everything past this:
    // base-chain rows, variant fallbacks, and negative answers.
    // A relation-only row (nullptr slots — an interface/abstract type's table)
    // falls through to the cached walk, which skips such rows.
    for (int32_t i = 0; i < t->interfaceCount; i++)
    {
        if (t->interfaces[i].itf == itf && t->interfaces[i].slots != nullptr)
            return t->interfaces[i].slots;
    }
    return dn2cpp_resolve_interface_cached(t, itf);
}

// A dispatched slot whose implementing body the transpiler never reached. The emitter
// installs these rather than a null pointer: such a slot is unreachable by
// construction (a class that does not implement the method cannot have the method
// dispatched on it), so degrading it is right — but "degrade" must not mean a call
// through 0x0, which lands as a bare SIGSEGV with nothing on it to read. Whoever gets
// here has found a hole in the reachability closure, and the abort says so.
//
// One shared symbol, not a stub per (class, interface): a per-pair stub could name the
// interface too, at ~1.9% of the binary — the wrong trade for a transpiler that ships
// to size-sensitive targets. The name comes out of the receiver instead.
//
// These aborts stay aborts. They are TRANSPILER BUGS, not bad input, so there is
// nothing for a caller to catch: a catchable throw would let a game swallow the one
// signal that its build is malformed, and the next symptom would be a wrong answer
// from whatever the empty slot was supposed to compute.
//
// "EntryPointNotFoundException" in the abort text below DESCRIBES the failure's
// flavour; nothing raises or can catch it. The fprintf lines carry the diagnosis.
[[noreturn]] static void dn2cpp_slot_missing_report(const char* kind, void* self)
{
    // `self` is argument 0 of the dispatch, and for the slots routed here it really is the
    // receiver: an interface slot is entered through a pointer cast to the site's own
    // signature, whose first parameter is the receiver. The exception is a by-value struct
    // return, where x86-64 SysV and Win64 spend the first integer argument register on the
    // caller's hidden result buffer and shift the receiver to the second — so the emitter
    // (CppEmitter.ReceiverIsFirstArg) keeps every such slot away from this entry point and
    // sends it to the anonymous trap below. Belt-and-braces on the way in anyway: a trap
    // that faults while reporting a fault reports nothing.
    const char* name = "(unknown)";
    if (self != nullptr)
    {
        const Dn2CppTypeInfo* t = static_cast<Dn2CppObject*>(self)->type;
        if (t != nullptr && t->name != nullptr)
            name = t->name;
    }
    std::fprintf(stderr,
        "dn2cpp fatal: %s dispatch: no implementation reached for a slot on %s\n"
        "  (the slot was emitted as a trap: the transpiler's reachability closure never\n"
        "   reached a body for it, but the runtime dispatched through it anyway)\n",
        kind, name);
    dn2cpp_fail("EntryPointNotFoundException (unimplemented dispatch slot)");
}

[[noreturn]] void dn2cpp_itf_slot_missing(void* self)
{
    dn2cpp_slot_missing_report("interface", self);
}

// The same, for a slot whose return type is a by-value struct: argument 0 may be the
// hidden result pointer rather than the receiver, so there is nothing here that can be
// read safely. It loses the name; it does not lose the abort.
[[noreturn]] void dn2cpp_itf_slot_missing_anon()
{
    dn2cpp_slot_missing_report("interface", nullptr);
}

// The by-value-struct slots that once fell to the anonymous trap above now reach here
// instead: the emitter cannot read the receiver from argument 0 (the hidden result
// buffer may sit there), so it BAKES the slot's (class, member) descriptor into a tiny
// per-slot stub that calls this with the text ready-made. No receiver is touched; the
// name comes from the compile, not the crash. See CppEmitter.RenderItfTables /
// RenderVtable and CppEmitter.ReceiverIsFirstArg. `kind` is "interface" or "virtual".
[[noreturn]] static void dn2cpp_slot_missing_report_named(const char* kind, const char* slotDesc)
{
    std::fprintf(stderr,
        "dn2cpp fatal: %s dispatch: no implementation reached for slot %s\n"
        "  (the slot was emitted as a trap: the transpiler's reachability closure never\n"
        "   reached a body for it, but the runtime dispatched through it anyway)\n",
        kind, slotDesc != nullptr ? slotDesc : "(unknown)");
    dn2cpp_fail("EntryPointNotFoundException (unimplemented dispatch slot)");
}

[[noreturn]] void dn2cpp_itf_slot_missing_named(const char* slotDesc)
{
    dn2cpp_slot_missing_report_named("interface", slotDesc);
}

// The vtable analogue. dn2cpp_vcall_unimplemented recovers the receiver's type and the
// candidate methods from the method table, but a struct-returning virtual leaves it with
// the hidden result buffer in `self` and nothing to read (":581-582 — receiver
// unreadable"). For those slots the emitter bakes the descriptor here, the same way.
[[noreturn]] void dn2cpp_vcall_unimplemented_named(const char* slotDesc)
{
    dn2cpp_slot_missing_report_named("virtual", slotDesc);
}

const void** dn2cpp_resolve_interface(const Dn2CppTypeInfo* t, const Dn2CppTypeInfo* itf)
{
    // The fast path is spelled out here rather than reached through
    // dn2cpp_try_resolve_interface: this is the per-call-site dispatch entry
    // generated code jumps into, and keeping the own-table scan in the entry's
    // body (call on miss only) measurably matters on a tight dispatch loop —
    // routing the hit path through one more call/ret costs ~5% there.
    // A relation-only row (nullptr slots) falls through to the cached walk.
    for (int32_t i = 0; i < t->interfaceCount; i++)
    {
        if (t->interfaces[i].itf == itf && t->interfaces[i].slots != nullptr)
            return t->interfaces[i].slots;
    }
    if (const void** slots = dn2cpp_resolve_interface_cached(t, itf))
        return slots;
    // Fatal anyway — name the failing pair so the miss is diagnosable from the
    // crash line alone (the abort backtrace rarely survives release builds).
    std::fprintf(stderr, "dn2cpp fatal: interface dispatch: %s has no map for %s\n",
        t != nullptr ? t->name : "(null)", itf != nullptr ? itf->name : "(null)");
    dn2cpp_fail("EntryPointNotFoundException (interface dispatch)");
}

// Array covariance: is an array whose element type-info is `se` assignable to
// an array of element `de`? Reference-element arrays are covariant (Derived[] is a
// Base[] when Derived is reference-assignable to Base); value-element arrays (incl.
// primitive/enum) are invariant — exact element match only, since the layouts differ.
// A null element handle means "object" (the shared array handles set their real element,
// but be defensive). Element assignability walks se's base chain + interface map for de.
static int32_t dn2cpp_array_elem_assignable(const Dn2CppTypeInfo* se, const Dn2CppTypeInfo* de)
{
    if (se == nullptr)
        se = &dn2cpp_object_type;
    if (de == nullptr)
        de = &dn2cpp_object_type;
    if (se == de)
        return 1;
    // A value-type element on either side requires an exact match (handled above).
    if ((se->flags & DN2CPP_TF_VALUETYPE) != 0 || (de->flags & DN2CPP_TF_VALUETYPE) != 0)
        return 0;
    if (de == &dn2cpp_object_type)
        return 1; // every reference element is assignable to object
    for (const Dn2CppTypeInfo* t = se; t != nullptr; t = t->base)
    {
        if (t == de)
            return 1;
        for (int32_t i = 0; i < t->interfaceCount; i++)
            if (t->interfaces[i].itf == de)
                return 1;
    }
    // Nested / interface-graph variance: when `de` is a variant interface instantiation, an
    // `se` that is not assignable to it by identity above may be by variance — either `se`
    // IS an instantiation of de's definition whose arguments sit the right way round (the
    // nested type-argument case: ICovariant<Cat> answers ICovariant<Animal>), or `se`
    // IMPLEMENTS one somewhere in its interface map (Impl : ICovariant<Cat> answers
    // ICovariant<Animal>). Mirrors dn2cpp_try_resolve_interface's variant fallback and closes
    // the mutual recursion with dn2cpp_itf_variant_match — bounded by the finite type-argument
    // nesting. The transpiler's RefAssignable runs the identical rule; if this accepted a
    // match the closure did not reach, the dispatched slot would be an unreached trap.
    if (dn2cpp_itf_is_variant(de))
    {
        if (dn2cpp_itf_variant_match(se, de))
            return 1;
        for (const Dn2CppTypeInfo* t = se; t != nullptr; t = t->base)
            for (int32_t i = 0; i < t->interfaceCount; i++)
                if (dn2cpp_itf_variant_match(t->interfaces[i].itf, de))
                    return 1;
    }
    return 0;
}

// The CLR array-element equivalence class of a primitive/enum type, or 0 for a
// type that has none (char, bool, float, double, every reference/struct type). Two
// array element types are covariant-compatible when they share a NON-zero class:
// the signed/unsigned integer pairs of one width are interchangeable as array
// elements (int[] casts to uint[], and `int[] is IList<uint>` is True), an enum is
// interchangeable with its underlying integer (and so with anything that integer is),
// but char (U2) is NOT interchangeable with short/ushort and bool (1 byte) is NOT with
// byte/sbyte — the CLR keeps those distinct. Enums reduce to their underlying first.
static int32_t dn2cpp_prim_elem_class(const Dn2CppTypeInfo* t)
{
    if (t == nullptr)
        return 0;
    if ((t->flags & DN2CPP_TF_ENUM) != 0 && t->enumUnderlying != nullptr)
        t = t->enumUnderlying;
    const char* n = t->name;
    if (n == nullptr)
        return 0;
    // Compare by name, not by handle: the element may be a runtime primitive handle
    // (dn2cpp_int32_type) or a transpiled-CoreLib's own emitted Int32 ti, and both
    // carry the same CLR name. isinst is cached per pair, so the strcmp runs once.
    if (std::strcmp(n, "System.SByte") == 0 || std::strcmp(n, "System.Byte") == 0)
        return 1;
    if (std::strcmp(n, "System.Int16") == 0 || std::strcmp(n, "System.UInt16") == 0)
        return 2;
    if (std::strcmp(n, "System.Int32") == 0 || std::strcmp(n, "System.UInt32") == 0)
        return 3;
    if (std::strcmp(n, "System.Int64") == 0 || std::strcmp(n, "System.UInt64") == 0)
        return 4;
    if (std::strcmp(n, "System.IntPtr") == 0 || std::strcmp(n, "System.UIntPtr") == 0)
        return 5;
    return 0;
}

// Array element covariance, the CLR's full rule: an array whose element is `se` is
// assignable to an array of element `de` when se == de, se is reference-assignable to
// de (Derived[] is a Base[], string[] is an object[] — dn2cpp_array_elem_assignable,
// which also carries interface-graph variance), OR se and de are the same primitive/
// enum equivalence class (the int/uint, byte/sbyte, enum↔underlying interchange above).
// This is BROADER than dn2cpp_array_elem_assignable, which holds value elements invariant
// — the extra breadth is exactly the array-covariance quirk, and it must NOT leak into
// generic-interface variance (IEnumerable<int> is not IEnumerable<uint>), so the variant
// matcher keeps using the assignable form and only the array arms use this one.
static int32_t dn2cpp_array_elem_covariant(const Dn2CppTypeInfo* se, const Dn2CppTypeInfo* de)
{
    if (se == nullptr)
        se = &dn2cpp_object_type;
    if (de == nullptr)
        de = &dn2cpp_object_type;
    if (dn2cpp_array_elem_assignable(se, de))
        return 1;
    int32_t c = dn2cpp_prim_elem_class(se);
    return c != 0 && c == dn2cpp_prim_elem_class(de);
}

// Well-known non-generic BCL interfaces of the primitive/enum/hand-written value
// types, for the fallback arm at the tail of dn2cpp_isinst_walk. Those type-infos carry
// no interface rows — their members are intrinsic-dispatched — so without this table
// `typeof(IConvertible).IsAssignableFrom(typeof(long))` answers False where real .NET
// answers True, and library code that gates a conversion on that pair silently takes
// its failure path.
//
// The bit sets are the real-.NET oracle per (type, interface): bool and string
// implement IConvertible+IComparable but NOT IFormattable, while IntPtr/UIntPtr and the
// DateTimeOffset/TimeSpan/DateOnly/TimeOnly family implement everything BUT
// IConvertible. IComparable<T>/IEquatable<T> need the closed instantiation's identity,
// so they are not bits here — see dn2cpp_wellknown_self_generic_itf below.
//
// Both sides compare by CLR name, not handle, for the same reason
// dn2cpp_prim_elem_class does: the source may be the runtime primitive handle OR a
// transpiled CoreLib's own emitted ti of the same primitive, and both carry the
// same name. The pair verdict is cached (g_isinst_cache), so the strcmps run once.
//
// This is a fallback AFTER the real row walk, shared by isinst/castclass and
// IsAssignableFrom (one rule, dn2cpp_typeinfo_assignable). The DISPATCH side reads
// the same pair: dn2cpp_resolve_interface_walk's last arm hands back a boxed-built-in
// slot table for exactly the pairs this answers. Do not narrow one side alone — a
// test that is true without a table aborts loudly at the call.
// A real map still wins: String's and every boxed enum's are found by the row walk
// long before the fallback.
static int32_t dn2cpp_wellknown_itf_bit(const Dn2CppTypeInfo* ti)
{
    if ((ti->flags & DN2CPP_TF_INTERFACE) == 0 || ti->name == nullptr)
        return 0;
    if (std::strcmp(ti->name, "System.IConvertible") == 0)
        return 1;
    if (std::strcmp(ti->name, "System.IComparable") == 0)
        return 2;
    if (std::strcmp(ti->name, "System.IFormattable") == 0)
        return 4;
    if (std::strcmp(ti->name, "System.ISpanFormattable") == 0)
        return 8;
    return 0;
}

static int32_t dn2cpp_wellknown_itf_mask(const Dn2CppTypeInfo* st)
{
    // Every enum (System.Enum implements all four; the abstract class's own
    // handle is name-matched below since it carries no TF_ENUM).
    if ((st->flags & DN2CPP_TF_ENUM) != 0)
        return 1 | 2 | 4 | 8;
    const char* n = st->name;
    if (n == nullptr)
        return 0;
    if (std::strcmp(n, "System.Boolean") == 0 || std::strcmp(n, "System.String") == 0)
        return 1 | 2;
    if (std::strcmp(n, "System.Char") == 0 || std::strcmp(n, "System.SByte") == 0
        || std::strcmp(n, "System.Byte") == 0 || std::strcmp(n, "System.Int16") == 0
        || std::strcmp(n, "System.UInt16") == 0 || std::strcmp(n, "System.Int32") == 0
        || std::strcmp(n, "System.UInt32") == 0 || std::strcmp(n, "System.Int64") == 0
        || std::strcmp(n, "System.UInt64") == 0 || std::strcmp(n, "System.Single") == 0
        || std::strcmp(n, "System.Double") == 0 || std::strcmp(n, "System.Decimal") == 0
        || std::strcmp(n, "System.DateTime") == 0 || std::strcmp(n, "System.Enum") == 0)
        return 1 | 2 | 4 | 8;
    if (std::strcmp(n, "System.IntPtr") == 0 || std::strcmp(n, "System.UIntPtr") == 0
        || std::strcmp(n, "System.DateTimeOffset") == 0 || std::strcmp(n, "System.TimeSpan") == 0
        || std::strcmp(n, "System.DateOnly") == 0 || std::strcmp(n, "System.TimeOnly") == 0)
        return 2 | 4 | 8;
    return 0;
}

// The other half of the same question, for the CLOSED GENERIC pair every one of those
// value types also implements at T = ITSELF: IComparable<T> and IEquatable<T>. It
// cannot be a bit in the mask above, because the verdict is a fact about the
// interface's ARGUMENT rather than about its identity — `(object)5 is IComparable<int>`
// is True and `is IComparable<long>` is False. Returns 1 for IComparable<T>,
// 2 for IEquatable<T>, 0 otherwise.
//
// Value types only. An ENUM implements neither in .NET, and String's real map already
// carries both rows — admitting it here would give one question two answers.
static bool dn2cpp_wellknown_self_generic_eligible(const Dn2CppTypeInfo* st)
{
    return (st->flags & DN2CPP_TF_VALUETYPE) != 0 && (st->flags & DN2CPP_TF_ENUM) == 0
        && st->name != nullptr && dn2cpp_wellknown_itf_mask(st) != 0;
}

static int32_t dn2cpp_wellknown_self_generic_itf(const Dn2CppTypeInfo* st, const Dn2CppTypeInfo* ti)
{
    if ((ti->flags & DN2CPP_TF_INTERFACE) == 0 || ti->genericDef == nullptr
        || ti->genericDef->name == nullptr || ti->genericArgCount != 1
        || ti->genericArgs[0] == nullptr || ti->genericArgs[0]->name == nullptr
        || !dn2cpp_wellknown_self_generic_eligible(st)
        || std::strcmp(ti->genericArgs[0]->name, st->name) != 0)
        return 0;
    if (std::strcmp(ti->genericDef->name, "System.IComparable`1") == 0)
        return 1;
    if (std::strcmp(ti->genericDef->name, "System.IEquatable`1") == 0)
        return 2;
    return 0;
}

// The same pair as seen from inside a SHARED canonical body, which dispatches through
// the group's canonical interface — a type-info carrying DN2CPP_TF_SHARED_CANON and no
// generic arguments at all, so the argument that decides the verdict is not on it. The
// receiver supplies it instead: a canonical `IComparable<T>` receiver IS a T. That is
// sound only for DISPATCH, and only after the fact — the isinst/castclass that admitted
// the reference was emitted against the group member's CONCRETE interface (rgctx), so
// the pair has already been decided by the predicate above. The type test therefore
// never asks this one, and a canonical row here can only serve a cast that passed.
static int32_t dn2cpp_wellknown_self_generic_canon(const Dn2CppTypeInfo* st, const Dn2CppTypeInfo* ti)
{
    if ((ti->flags & DN2CPP_TF_SHARED_CANON) == 0 || (ti->flags & DN2CPP_TF_INTERFACE) == 0
        || ti->name == nullptr || !dn2cpp_wellknown_self_generic_eligible(st))
        return 0;
    if (std::strncmp(ti->name, "System.IComparable_$", 20) == 0)
        return 1;
    if (std::strncmp(ti->name, "System.IEquatable_$", 19) == 0)
        return 2;
    return 0;
}

// The uncached isinst decision — a pure function of (source type, target type):
// every branch below reads only the two type-infos, so the answer is cacheable
// per pair (g_isinst_cache in dn2cpp_isinst).
static int32_t dn2cpp_isinst_walk(const Dn2CppTypeInfo* st, const Dn2CppTypeInfo* ti)
{
    // Array-to-array cast: rank must match (int[] is NOT an int[,] — an SZArray and a
    // rank-2 array are distinct types) AND the element must be covariant-compatible
    // (string[] is castable to object[], int[] to uint[]; but Span<object> over string[],
    // an exact-handle guard rather than a cast, is rejected). Both operands carry
    // DN2CPP_TF_ARRAY and a real arrayRank (1 for SZArray).
    if ((st->flags & DN2CPP_TF_ARRAY) != 0 && (ti->flags & DN2CPP_TF_ARRAY) != 0)
    {
        // The imprecise packed handle states a value-element layout with NO element
        // identity, so no covariance is provable either way: identity only. Otherwise
        // the null elementType coerces to object below and a packed array claims to be
        // an object[], whose ldelem.ref reads raw payload bytes as managed pointers.
        if (st == &dn2cpp_array_n_type || ti == &dn2cpp_array_n_type)
            return st == ti;
        return st->arrayRank == ti->arrayRank
            && dn2cpp_array_elem_covariant(st->elementType, ti->elementType);
    }
    // An array to one of the six non-generic interfaces every array implements
    // (IEnumerable/ICollection/IList + ICloneable/IStructuralComparable/IStructural-
    // Equatable, all flagged DN2CPP_TF_ARRAY_ITF). True for SZ and MD alike, independent
    // of whether the lazy SZArray dispatch map was wired; a visible call site still wires
    // the map for the actual dispatch.
    if ((st->flags & DN2CPP_TF_ARRAY) != 0 && (ti->flags & DN2CPP_TF_ARRAY_ITF) != 0)
        return 1;
    // An SZArray (rank 1) to one of the five generic collection interfaces (IList<T> etc.,
    // whose DEFINITION carries DN2CPP_TF_ARRAY_GEN_ITF): true when the array's element is
    // array-element-compatible with the interface's type argument. A multidim array
    // implements none of these, so this arm is gated on rank == 1. Independent of the map.
    if ((st->flags & DN2CPP_TF_ARRAY) != 0 && st->arrayRank == 1
        && ti->genericDef != nullptr && (ti->genericDef->flags & DN2CPP_TF_ARRAY_GEN_ITF) != 0
        && ti->genericArgCount == 1)
        // Element-unknown packed handle: no argument compatibility is provable, and a
        // covariant match would route the dispatch into the object-keyed fallback thunks
        // over a packed layout. Fail closed.
        return st != &dn2cpp_array_n_type
            && dn2cpp_array_elem_covariant(st->elementType, ti->genericArgs[0]);
    // A cast of any array to the abstract System.Array base. Every array — SZ or MD,
    // any element — is a System.Array, but array type-infos carry base=nullptr, so
    // the base-chain walk below never reaches System.Array. The emitter stamps
    // DN2CPP_TF_SYSTEM_ARRAY on that one type-info; recognizing it here mirrors the
    // System.Object special case in dn2cpp_isinst.
    if ((st->flags & DN2CPP_TF_ARRAY) != 0 && (ti->flags & DN2CPP_TF_SYSTEM_ARRAY) != 0)
        return 1;
    for (const Dn2CppTypeInfo* t = st; t != nullptr; t = t->base)
    {
        if (t == ti)
            return 1;
        for (int32_t i = 0; i < t->interfaceCount; i++)
        {
            if (t->interfaces[i].itf == ti)
                return 1;
        }
    }
    // Variant interface cast: obj's I<Y> satisfies a variant I<X> when the arguments sit
    // the right way round — covariantly (IEnumerable<Cat> is an IEnumerable<Animal>) or
    // contravariantly (IComparer<Animal> is an IComparer<Cat>). Only entered when ti is a
    // variant closed generic, so exact casts are unchanged.
    //
    // `st` ITSELF is tried before its rows, and that arm is load-bearing: this walk also
    // answers Type.IsAssignableFrom, whose SOURCE is an arbitrary Type and may be the
    // variant interface itself. It matters for isinst too — variance is legal on
    // DELEGATES, whose instances do carry a variant closed generic as their header type.
    if (dn2cpp_itf_is_variant(ti))
    {
        if (dn2cpp_itf_variant_match(st, ti))
            return 1;
        for (const Dn2CppTypeInfo* t = st; t != nullptr; t = t->base)
            for (int32_t i = 0; i < t->interfaceCount; i++)
                if (dn2cpp_itf_variant_match(t->interfaces[i].itf, ti))
                    return 1;
    }
    // A generic type DEFINITION needs no arm of its own: the walk above IS the answer.
    // Its shell carries the definition's ARGUMENT-FREE relations — nearest non-generic
    // ancestor as base, one row per non-generic interface in the closure (CppEmitter's
    // GenericDefRelations) — which hold for the definition exactly when they hold for
    // every instantiation, so none need exist. A relation with a generic end
    // (`IEnumerable<>` <- `List<>`) is False by construction: no row can name one, and
    // the variance walk cannot lift it, every gendef row being non-generic. MessagePipe's
    // `AddGlobalMessageHandlerFilter(typeof(LoggingFilter<>))` is the load-bearing reader.

    // Well-known-interface fallback for the row-less primitive/enum/hand-written
    // type-infos (table + rationale at dn2cpp_wellknown_itf_bit above). Last, so a
    // real interface row — a transpiled CoreLib ti that does carry one — always
    // answers first and this only fills the holes the oracle names. Deliberately
    // NOT consulted by dn2cpp_array_elem_assignable / the variance matcher: the
    // transpiler's reachability closure (RefAssignable) runs the identical rule
    // for those, and a runtime-only widening there would accept matches whose
    // dispatch rows the closure never filled.
    if (int32_t bit = dn2cpp_wellknown_itf_bit(ti))
        if ((dn2cpp_wellknown_itf_mask(st) & bit) != 0)
            return 1;
    if (dn2cpp_wellknown_self_generic_itf(st, ti) != 0)
        return 1;
    return 0;
}

// The pure (source, target) type-test — dn2cpp_isinst_walk fronted by the pair
// cache plus the two identity fast paths. Exported because it answers TWO
// surfaces with one rule: `isinst`/`castclass` on an instance's type, and
// Type.IsAssignableFrom on a pair of reflection Types
// (dn2cpp_type_is_assignable_from delegates here). Before that delegation,
// IsAssignableFrom kept a private base-chain + interface-row walk that knew
// nothing about arrays — so `typeof(IEnumerable).IsAssignableFrom(stringArrayType)`
// answered False while `(IEnumerable)stringArray` cast fine, and Newtonsoft's
// DefaultContractResolver classified a string[] member as a *string* contract
// (the Thrive organelles.json deserialization failure). One rule, one answer.
int32_t dn2cpp_typeinfo_assignable(const Dn2CppTypeInfo* st, const Dn2CppTypeInfo* ti)
{
    if (st == nullptr || ti == nullptr)
        return 0;
    if (st == ti)
        return 1;
    // Everything is a System.Object; per-type base chains (runtime trap
    // exceptions, arrays, a transpiled corelib's own ti_System_Object) never
    // point at this shared runtime handle, so match it unconditionally.
    if (ti == &dn2cpp_object_type)
        return 1;
    uintptr_t hit;
    if (g_isinst_cache.Lookup(st, ti, &hit))
        return hit != 0 ? 1 : 0;
    int32_t r = dn2cpp_isinst_walk(st, ti);
    g_isinst_cache.Insert(st, ti, r != 0 ? 1u : 0u);
    return r != 0 ? 1 : 0;
}

Dn2CppObject* dn2cpp_isinst(Dn2CppObject* obj, const Dn2CppTypeInfo* ti)
{
    if (obj == nullptr)
        return nullptr;
    // Fast path ahead of the helper's own identity tests: the exact-type match
    // — the dominant isinst shape, one compare, cheaper than a cache probe.
    if (obj->type == ti)
        return obj;
    return dn2cpp_typeinfo_assignable(obj->type, ti) != 0 ? obj : nullptr;
}

// A failed castclass/unbox raises the real .NET exception — catchable, with the
// .NET message shape — so `catch (InvalidCastException)` and e.Message match
// real .NET (the shared runtime handle keeps a typed catch and a runtime-raised
// object on one type-info, like the other trap exceptions above).
[[noreturn]] static void dn2cpp_throw_invalid_cast(const Dn2CppTypeInfo* from, const Dn2CppTypeInfo* to)
{
    char buf[512];
    std::snprintf(buf, sizeof buf, "Unable to cast object of type '%s' to type '%s'.",
        from != nullptr && from->name != nullptr ? from->name : "?",
        to != nullptr && to->name != nullptr ? to->name : "?");
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_invalid_cast_exception_type,
        dn2cpp_string_from_utf8(buf, static_cast<int32_t>(std::strlen(buf))), nullptr));
}

Dn2CppObject* dn2cpp_castclass(Dn2CppObject* obj, const Dn2CppTypeInfo* ti)
{
    if (obj == nullptr)
        return nullptr;
    if (dn2cpp_isinst(obj, ti) == nullptr)
        dn2cpp_throw_invalid_cast(obj->type, ti);
    return obj;
}

// ---- a headerless representation with no managed identity escaping ---------
// One intrinsic representation is headerless like the NFI trio but has NO managed
// identity to wrap into: System.Buffers.SearchValues<T>. Escaping it into `object`
// would pun the raw pointer and every object-generic consumer would read a 256-entry
// bitmap's first bytes as a type header — silently, not as a crash. So fail, and
// catchably: a wrap would have to invent the private per-shape subclass identity .NET
// reports, i.e. answer differently wrong rather than not at all. Assembly/Module have
// a FIXED private identity, so they take the interned-wrapper treatment instead
// (dn2cpp_asm_wrap).
Dn2CppObject* dn2cpp_headerless_escape(const char* managedTypeName)
{
    char buf[512];
    std::snprintf(buf, sizeof(buf),
        "A value of type '%s' cannot be used as an 'object' in a transpiled image: "
        "dn2cpp represents it as a headerless intrinsic handle with no managed type "
        "identity, so boxing it would make GetType(), ToString(), equality and every "
        "object-typed collection read the handle's own bytes as an object header. "
        "Keep the value in its own static type (members called on it work normally).",
        managedTypeName);
    dn2cpp_throw_platform_not_supported(buf);
    return nullptr;
}

// ---- castclass/isinst with an NFI-mapped target ----------------------------
// (CultureInfo / NumberFormatInfo / TextInfo / IFormatProvider — the managed
// types the transpiler lowers to `const Dn2CppNumberFormatInfo*`.) The source
// is object-typed and may hold the interned Dn2CppNfiBox wrapper an escape
// minted (dn2cpp_nfi_wrap); the cast must both VERIFY and UNWRAP, so the
// generic dn2cpp_castclass alone is not enough. A wrapper is matched by kind —
// PROVIDER accepts the CultureInfo and NumberFormatInfo identities, exactly
// the classes that implement the interface (TextInfo does not). A non-wrapper
// object takes the generic path against the emitted target type-info, which
// preserves pre-wrapper behavior: a real mismatch (a string) throws/answers
// null with the proper message, and a user IFormatProvider implementation
// still matches through its interface table.
static bool dn2cpp_nfi_kind_match(const Dn2CppTypeInfo* boxTi, int32_t kind)
{
    switch (kind)
    {
        case DN2CPP_NFI_KIND_CULTURE: return boxTi == &dn2cpp_cultureinfo_type;
        case DN2CPP_NFI_KIND_NFI: return boxTi == &dn2cpp_numberformatinfo_type;
        case DN2CPP_NFI_KIND_TEXTINFO: return boxTi == &dn2cpp_textinfo_type;
        default: return boxTi == &dn2cpp_cultureinfo_type
                     || boxTi == &dn2cpp_numberformatinfo_type;
    }
}

static bool dn2cpp_nfi_is_box(const Dn2CppObject* o)
{
    const Dn2CppTypeInfo* t = o->type;
    return t == &dn2cpp_cultureinfo_type || t == &dn2cpp_numberformatinfo_type
        || t == &dn2cpp_textinfo_type;
}

const Dn2CppNumberFormatInfo* dn2cpp_nfi_castclass(Dn2CppObject* o, int32_t kind,
                                                   const Dn2CppTypeInfo* fallbackTi)
{
    if (o == nullptr)
        return nullptr;
    if (dn2cpp_nfi_is_box(o))
    {
        if (!dn2cpp_nfi_kind_match(o->type, kind))
            dn2cpp_throw_invalid_cast(o->type, fallbackTi);
        return reinterpret_cast<Dn2CppNfiBox*>(o)->nfi;
    }
    return reinterpret_cast<const Dn2CppNumberFormatInfo*>(dn2cpp_castclass(o, fallbackTi));
}

const Dn2CppNumberFormatInfo* dn2cpp_nfi_isinst(Dn2CppObject* o, int32_t kind,
                                                const Dn2CppTypeInfo* fallbackTi)
{
    if (o == nullptr)
        return nullptr;
    if (dn2cpp_nfi_is_box(o))
        return dn2cpp_nfi_kind_match(o->type, kind)
            ? reinterpret_cast<Dn2CppNfiBox*>(o)->nfi : nullptr;
    return reinterpret_cast<const Dn2CppNumberFormatInfo*>(dn2cpp_isinst(o, fallbackTi));
}

Dn2CppObject* dn2cpp_box(const Dn2CppTypeInfo* ti, const void* value, size_t size)
{
    auto* obj = static_cast<Dn2CppObject*>(dn2cpp_alloc(sizeof(Dn2CppObject) + size));
    obj->type = ti;
    dn2cpp_gc_memmove_refs(obj + 1, value, size); // the payload may carry managed refs
    return obj;
}

// ECMA-335 unbox compatibility beyond the exact match (III.4.32's
// verifier-assignable-to, the rule real .NET's CastHelpers.Unbox applies): a boxed enum
// unboxes as its underlying primitive, a boxed primitive as an enum over that
// primitive, and two enums over one underlying type interchange. Everything else is
// rejected — a pure-primitive pair (int/uint) throws InvalidCastException even at equal
// width, and the underlying comparison is exact in width AND signedness. Underlyings
// compare by CLR name, not handle, for the same reason dn2cpp_prim_elem_class does.
// The widening is unbox-only: isinst/castclass keep the CLR's asymmetry
// (`boxedEnum is ushort` is false while `(ushort)boxedEnum` succeeds). Payload widths
// agree by construction — an enum and its underlying box into the same slot.
static bool dn2cpp_unbox_compatible(const Dn2CppTypeInfo* st, const Dn2CppTypeInfo* ti)
{
    bool srcEnum = (st->flags & DN2CPP_TF_ENUM) != 0 && st->enumUnderlying != nullptr;
    bool dstEnum = (ti->flags & DN2CPP_TF_ENUM) != 0 && ti->enumUnderlying != nullptr;
    if (!srcEnum && !dstEnum)
        return false; // int ↔ uint and friends: real .NET rejects
    const Dn2CppTypeInfo* su = srcEnum ? st->enumUnderlying : st;
    const Dn2CppTypeInfo* du = dstEnum ? ti->enumUnderlying : ti;
    if (su == du)
        return true;
    return su->name != nullptr && du->name != nullptr && std::strcmp(su->name, du->name) == 0;
}

void* dn2cpp_unbox(Dn2CppObject* obj, const Dn2CppTypeInfo* ti)
{
    if (obj == nullptr)
        dn2cpp_throw_null_reference();
    if (obj->type != ti && !dn2cpp_unbox_compatible(obj->type, ti))
        dn2cpp_throw_invalid_cast(obj->type, ti);
    return obj + 1;
}

Dn2CppObject* dn2cpp_delegate_combine(Dn2CppObject* a, Dn2CppObject* b)
{
    if (a == nullptr)
        return b;
    if (b == nullptr)
        return a;
    auto* bd = reinterpret_cast<Dn2CppDelegate*>(b);
    auto* copy = static_cast<Dn2CppDelegate*>(dn2cpp_alloc(sizeof(Dn2CppDelegate)));
    copy->type = bd->type;
    copy->target = bd->target;
    copy->method = bd->method;
    dn2cpp_gc_store_ref(&copy->prev, dn2cpp_delegate_combine(a, bd->prev));
    return copy;
}

Dn2CppObject* dn2cpp_delegate_remove(Dn2CppObject* source, Dn2CppObject* value)
{
    if (source == nullptr || value == nullptr)
        return source;
    auto* v = reinterpret_cast<Dn2CppDelegate*>(value);
    auto* s = reinterpret_cast<Dn2CppDelegate*>(source);
    // Remove the most recent matching entry (.NET removes the last occurrence).
    if (s->target == v->target && s->method == v->method)
        return s->prev;
    Dn2CppObject* rest = dn2cpp_delegate_remove(s->prev, value);
    if (rest == s->prev)
        return source; // no match further down
    auto* copy = static_cast<Dn2CppDelegate*>(dn2cpp_alloc(sizeof(Dn2CppDelegate)));
    copy->type = s->type;
    copy->target = s->target;
    copy->method = s->method;
    copy->prev = rest;
    return copy;
}

// Target-slot identity, with reflection-bind nodes (CreateDelegate) compared by
// content: two separately created bindings of the same (method row, target,
// mode) are equal delegates, matching .NET.
static bool dn2cpp_delegate_target_equal(Dn2CppObject* a, Dn2CppObject* b)
{
    if (a == b)
        return true;
    if (a == nullptr || b == nullptr
        || a->type != &dn2cpp_reflbind_type || b->type != &dn2cpp_reflbind_type)
        return false;
    auto* ra = reinterpret_cast<Dn2CppReflBind*>(a);
    auto* rb = reinterpret_cast<Dn2CppReflBind*>(b);
    return ra->method == rb->method && ra->target == rb->target && ra->mode == rb->mode;
}

int32_t dn2cpp_delegate_equal(Dn2CppObject* a, Dn2CppObject* b)
{
    if (a == b)
        return 1; // same reference (or both null)
    if (a == nullptr || b == nullptr)
        return 0;
    // .NET Delegate.Equals first requires the exact same delegate type; two
    // different delegate types over the same method are never equal.
    if (a->type != b->type)
        return 0;
    auto* da = reinterpret_cast<Dn2CppDelegate*>(a);
    auto* db = reinterpret_cast<Dn2CppDelegate*>(b);
    // Pairwise chain comparison (MulticastDelegate compares invocation lists
    // element-wise; a length mismatch is unequal).
    while (da != nullptr && db != nullptr)
    {
        if (!dn2cpp_delegate_target_equal(da->target, db->target) || da->method != db->method)
            return 0;
        da = reinterpret_cast<Dn2CppDelegate*>(da->prev);
        db = reinterpret_cast<Dn2CppDelegate*>(db->prev);
    }
    return (da == nullptr && db == nullptr) ? 1 : 0;
}

int32_t dn2cpp_delegate_hash(Dn2CppObject* d)
{
    if (d == nullptr)
        return 0;
    // FNV-1a fold over the chain's (target, method) pairs: equal delegates
    // (same type, matching chains) always agree, and distinct instances over
    // the same method/target agree too (the property engine-side Callable
    // dedup relies on). Stable per process; the exact .NET number (which is
    // type-identity based) is not modeled.
    uint64_t h = 1469598103934665603ull;
    for (auto* n = reinterpret_cast<Dn2CppDelegate*>(d); n != nullptr;
         n = reinterpret_cast<Dn2CppDelegate*>(n->prev))
    {
        // A reflection-bind node hashes by content (method row + bound target),
        // so the separately created equal bindings agree with the equality above.
        Dn2CppObject* t = n->target;
        if (t != nullptr && t->type == &dn2cpp_reflbind_type)
        {
            auto* rb = reinterpret_cast<Dn2CppReflBind*>(t);
            h = (h ^ static_cast<uint64_t>(reinterpret_cast<uintptr_t>(rb->method))) * 1099511628211ull;
            h = (h ^ static_cast<uint64_t>(reinterpret_cast<uintptr_t>(rb->target))) * 1099511628211ull;
        }
        else
            h = (h ^ static_cast<uint64_t>(reinterpret_cast<uintptr_t>(t))) * 1099511628211ull;
        h = (h ^ static_cast<uint64_t>(reinterpret_cast<uintptr_t>(n->method))) * 1099511628211ull;
    }
    return static_cast<int32_t>((h ^ (h >> 32)) & 0x7fffffff);
}

