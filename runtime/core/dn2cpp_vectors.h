#pragma once
// dn2cpp_vectors.h
//
// Software (scalar) emulation of the portable SIMD vector types. There are NO
// hardware intrinsics here: every operation is a plain scalar loop over the
// elements, chosen for correctness and portability over speed. This header is
// used when the transpiler lowers System.Runtime.Intrinsics.Vector64/128/256/512<T>
// and System.Numerics.Vector<T>.
//
// A fixed-width vector is a plain primitive-data POD byte buffer (no managed
// references, never GC-scanned), returned BY VALUE. Element access goes through
// std::memcpy so it stays strict-aliasing-safe regardless of T.
//
// The vec-op shims take their vector OPERANDS by const reference: a by-value
// Dn2CppVec argument is ABI-coerced to general-purpose register pairs in the
// caller's IR, and even after full inlining clang canonicalizes the bitwise ops
// of such values onto the GPR halves — under the Highway backend that mixed
// SIMD/GPR dataflow cost ~1.6x on the BCL's Vector<T> loops. By-value RETURNS
// carry no such penalty.

#include <cstdint>
#include <cstring>
#include <cmath>
#include <limits>
#include <type_traits>

// ---------------------------------------------------------------------------
// Representation (LOCKED)
// ---------------------------------------------------------------------------

template <int N>
struct alignas(N <= 16 ? N : 16) Dn2CppVec {
    unsigned char b[N];
};

using Dn2CppVector64  = Dn2CppVec<8>;
using Dn2CppVector128 = Dn2CppVec<16>;
using Dn2CppVector256 = Dn2CppVec<32>;
using Dn2CppVector512 = Dn2CppVec<64>;
using Dn2CppVectorT   = Dn2CppVec<16>;   // System.Numerics.Vector<T>, fixed at 128-bit

// Optional Google Highway backing for a subset of the ops below (opt-in, default
// OFF via the CMake option DN2CPP_USE_HIGHWAY). Requires Dn2CppVec<N> declared
// above. The default scalar build never includes this and stays byte-for-byte.
#if defined(DN2CPP_USE_HIGHWAY)
#include "dn2cpp_vectors_hwy.h"
#endif

// Portable SIMD hardware-acceleration gate. The transpiler emits this token for
// the IsSupported / IsHardwareAccelerated getters of the portable vector types
// (System.Runtime.Intrinsics.Vector64/128/256/512<T> and System.Numerics.Vector<T>).
// In the scalar build it resolves to 0, so the BCL keeps its software fallback and
// behaviour is byte-for-byte identical; under the opt-in Highway backend it resolves
// to 1, lighting up the BCL's vectorized fast paths against the SIMD-backed vec ops.
// Both builds emit the same token, so the self-host text fixpoint is preserved.
#if defined(DN2CPP_USE_HIGHWAY)
#define DN2CPP_SIMD_HW_ACCEL 1
#else
#define DN2CPP_SIMD_HW_ACCEL 0
#endif

// The same gate as read from inside the System.Linq assembly, kept as an independent
// lever because its trade-off has flipped once already. With real trap-on-overflow
// semantics the software fallback cannot legally vectorize, while the BCL's Vector<T>
// reductions (Enumerable.Sum/Min/Max) are exact and faster under Highway — so the gate
// follows the backend (measure-linqsum.sh quantifies both axes).
#define DN2CPP_SIMD_HW_ACCEL_LINQ DN2CPP_SIMD_HW_ACCEL

// ---------------------------------------------------------------------------
// Raw lane access (strict-aliasing-safe via memcpy)
// ---------------------------------------------------------------------------

template <class T, int N>
static inline T dn2cpp_vec_get(const Dn2CppVec<N>& v, int i) {
    T x;
    std::memcpy(&x, v.b + (size_t)i * sizeof(T), sizeof(T));
    return x;
}

template <class T, int N>
static inline void dn2cpp_vec_set(Dn2CppVec<N>& v, int i, T x) {
    std::memcpy(v.b + (size_t)i * sizeof(T), &x, sizeof(T));
}

// CopyTo validates the managed destination before forming the native store
// address. The index-taking overload rejects the index before testing the
// remaining capacity; the no-index overload reports an empty array as short.
static inline void dn2cpp_vec_copy_array_check(
    Dn2CppArray* destination, int32_t startIndex, int32_t count,
    int32_t hasStartIndex)
{
    if (destination == nullptr)
        dn2cpp_throw_null_reference();
    if (hasStartIndex != 0
        && static_cast<uint32_t>(startIndex) >= static_cast<uint32_t>(destination->length))
        dn2cpp_throw_argument_out_of_range();
    if (destination->length - startIndex < count)
        dn2cpp_throw_argument();
}

static inline void dn2cpp_vec_copy_span_check(int32_t destinationLength, int32_t count)
{
    if (destinationLength < count)
        dn2cpp_throw_argument();
}

// Render one portable-vector lane through the same culture-aware scalar
// formatters used by numeric ToString. The vector families accept only numeric
// element types, so the signed/floating split is exhaustive for supported T.
template <class T>
static inline Dn2CppString* dn2cpp_vec_format_lane(
    T value, Dn2CppString* format, const Dn2CppNumberFormatInfo* nfi)
{
    if constexpr (std::is_floating_point<T>::value)
    {
        if constexpr (sizeof(T) == sizeof(float))
            return dn2cpp_format_r4_c((float)value, format, nfi);
        else
            return dn2cpp_format_r8_c((double)value, format, nfi);
    }
    else if constexpr (std::is_signed<T>::value)
        return dn2cpp_format_int_c((int64_t)value, (int32_t)sizeof(T), format, nfi);
    else
        return dn2cpp_format_uint_c((uint64_t)value, (int32_t)sizeof(T), format, nfi);
}

// Vector64/128/256/512<T> always use invariant lane formatting and ", " as
// their separator. System.Numerics.Vector<T> uses the requested/current
// provider for both lanes and the separator: .NET spells the separator as the
// provider's NumberGroupSeparator followed by a space.
template <class T, int N>
static inline Dn2CppString* dn2cpp_vec_tostring(
    const Dn2CppVec<N>& value, Dn2CppString* format,
    const Dn2CppNumberFormatInfo* nfi, int32_t numericsVector)
{
    nfi = numericsVector ? dn2cpp_nfi_or_current(nfi) : dn2cpp_nfi_invariant();
    if (!numericsVector)
        format = nullptr;

    Dn2CppISB h = dn2cpp_isb_new(2, N / (int32_t)sizeof(T));
    dn2cpp_isb_append_literal(&h, dn2cpp_string_from_utf8("<", 1));
    const int32_t lanes = N / (int32_t)sizeof(T);
    for (int32_t i = 0; i < lanes; i++)
    {
        if (i != 0)
        {
            dn2cpp_isb_append_literal(&h, numericsVector
                ? nfi->numberGroup : dn2cpp_string_from_utf8(",", 1));
            dn2cpp_isb_append_literal(&h, dn2cpp_string_from_utf8(" ", 1));
        }
        dn2cpp_isb_append_literal(&h,
            dn2cpp_vec_format_lane<T>(dn2cpp_vec_get<T>(value, i), format, nfi));
    }
    dn2cpp_isb_append_literal(&h, dn2cpp_string_from_utf8(">", 1));
    return dn2cpp_isb_to_string(&h);
}

template <class T, int N, int NumericsVector>
static inline Dn2CppString* dn2cpp_vec_box_tostring(Dn2CppObject* boxed)
{
    Dn2CppVec<N> value;
    std::memcpy(&value, boxed + 1, sizeof(value));
    return dn2cpp_vec_tostring<T, N>(value, nullptr, nullptr, NumericsVector);
}

// ---------------------------------------------------------------------------
// Construction / constants
// ---------------------------------------------------------------------------

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_zero() {
    Dn2CppVec<N> r;
    std::memset(r.b, 0, sizeof(r.b));
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_all_bits_set() {
    Dn2CppVec<N> r;
    std::memset(r.b, 0xFF, sizeof(r.b));
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_create_broadcast(T value) {
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        dn2cpp_vec_set<T>(r, i, value);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_create_scalar(T value) {
    Dn2CppVec<N> r;
    std::memset(r.b, 0, sizeof(r.b));
    dn2cpp_vec_set<T>(r, 0, value);
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_create_scalar_unsafe(T value) {
    // "Unsafe" leaves the upper lanes unspecified; emulate as create_scalar.
    return dn2cpp_vec_create_scalar<T, N>(value);
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_create_indices() {
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        dn2cpp_vec_set<T>(r, i, (T)i);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_create_elems(const T* elems) {
    Dn2CppVec<N> r;
    std::memset(r.b, 0, sizeof(r.b));
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        dn2cpp_vec_set<T>(r, i, elems[i]);
    }
    return r;
}

// ---------------------------------------------------------------------------
// Element / lane access
// ---------------------------------------------------------------------------

template <class T, int N>
static inline T dn2cpp_vec_get_element(const Dn2CppVec<N>& v, int index) {
    return dn2cpp_vec_get<T>(v, index);
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_with_element(const Dn2CppVec<N>& v, int index, T value) {
    Dn2CppVec<N> r = v;
    dn2cpp_vec_set<T>(r, index, value);
    return r;
}

template <class T, int N>
static inline T dn2cpp_vec_to_scalar(const Dn2CppVec<N>& v) {
    return dn2cpp_vec_get<T>(v, 0);
}

// Halves -- byte copies; T is informational only.
template <class T, int N>
static inline Dn2CppVec<N / 2> dn2cpp_vec_get_lower(const Dn2CppVec<N>& v) {
    Dn2CppVec<N / 2> r;
    std::memcpy(r.b, v.b, N / 2);
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N / 2> dn2cpp_vec_get_upper(const Dn2CppVec<N>& v) {
    Dn2CppVec<N / 2> r;
    std::memcpy(r.b, v.b + N / 2, N / 2);
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_with_lower(const Dn2CppVec<N>& v, const Dn2CppVec<N / 2>& lo) {
    Dn2CppVec<N> r = v;
    std::memcpy(r.b, lo.b, N / 2);
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_with_upper(const Dn2CppVec<N>& v, const Dn2CppVec<N / 2>& hi) {
    Dn2CppVec<N> r = v;
    std::memcpy(r.b + N / 2, hi.b, N / 2);
    return r;
}

// Widen storage to double byte-width: low N bytes = v, high N bytes = 0.
template <int N>
static inline Dn2CppVec<N * 2> dn2cpp_vec_widen_bytes(const Dn2CppVec<N>& v) {
    Dn2CppVec<N * 2> r;
    std::memset(r.b, 0, sizeof(r.b));
    std::memcpy(r.b, v.b, N);
    return r;
}

template <int N>
static inline Dn2CppVec<N * 2> dn2cpp_vec_extend_unsafe(const Dn2CppVec<N>& v) {
    // Upper bytes are unspecified; emulate as widen_bytes (zero-extend).
    return dn2cpp_vec_widen_bytes<N>(v);
}

// ---------------------------------------------------------------------------
// Load / Store (pointer-based; aligned / non-temporal variants alias here)
// ---------------------------------------------------------------------------

template <int N>
static inline Dn2CppVec<N> dn2cpp_vec_load(const void* p) {
    Dn2CppVec<N> r;
    std::memcpy(r.b, p, N);
    return r;
}

template <int N>
static inline void dn2cpp_vec_store(const Dn2CppVec<N>& v, void* p) {
    std::memcpy(p, v.b, N);
}

template <int N>
static inline Dn2CppVec<N> dn2cpp_vec_load_off(const void* base, intptr_t elemOffsetBytes) {
    Dn2CppVec<N> r;
    std::memcpy(r.b, (const unsigned char*)base + elemOffsetBytes, N);
    return r;
}

// ---------------------------------------------------------------------------
// Arithmetic (element-wise; integers wrap, floats are IEEE)
// ---------------------------------------------------------------------------

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_add(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_add<T, N>(a, b);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        T av = dn2cpp_vec_get<T>(a, i), bv = dn2cpp_vec_get<T>(b, i);
        if constexpr (std::is_integral<T>::value) {
            using U = std::make_unsigned_t<T>;
            dn2cpp_vec_set<T>(r, i, (T)((U)av + (U)bv));
        } else {
            dn2cpp_vec_set<T>(r, i, (T)(av + bv));
        }
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_subtract(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_subtract<T, N>(a, b);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        T av = dn2cpp_vec_get<T>(a, i), bv = dn2cpp_vec_get<T>(b, i);
        if constexpr (std::is_integral<T>::value) {
            using U = std::make_unsigned_t<T>;
            dn2cpp_vec_set<T>(r, i, (T)((U)av - (U)bv));
        } else {
            dn2cpp_vec_set<T>(r, i, (T)(av - bv));
        }
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_multiply(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_multiply<T, N>(a, b);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        T av = dn2cpp_vec_get<T>(a, i), bv = dn2cpp_vec_get<T>(b, i);
        if constexpr (std::is_integral<T>::value) {
            using U = std::make_unsigned_t<T>;
            dn2cpp_vec_set<T>(r, i, (T)((U)av * (U)bv));
        } else {
            dn2cpp_vec_set<T>(r, i, (T)(av * bv));
        }
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_divide(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_divide<T, N>(a, b);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        T av = dn2cpp_vec_get<T>(a, i), bv = dn2cpp_vec_get<T>(b, i);
        dn2cpp_vec_set<T>(r, i, (T)(av / bv));
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_negate(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_negate<T, N>(v);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        T x = dn2cpp_vec_get<T>(v, i);
        if constexpr (std::is_integral<T>::value) {
            using U = std::make_unsigned_t<T>;
            dn2cpp_vec_set<T>(r, i, (T)(-(U)x));
        } else {
            dn2cpp_vec_set<T>(r, i, (T)(-x));
        }
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_abs(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_abs<T, N>(v);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        T x = dn2cpp_vec_get<T>(v, i);
        if constexpr (std::is_floating_point<T>::value) {
            dn2cpp_vec_set<T>(r, i, (T)std::fabs(x));
        } else if constexpr (std::is_signed<T>::value) {
            using U = std::make_unsigned_t<T>;
            dn2cpp_vec_set<T>(r, i, x < 0 ? (T)(-(U)x) : x);
        } else {
            dn2cpp_vec_set<T>(r, i, x);
        }
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_sqrt(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_sqrt<T, N>(v);
#endif
    if constexpr (std::is_floating_point<T>::value) {
        Dn2CppVec<N> r;
        const int lanes = N / (int)sizeof(T);
        for (int i = 0; i < lanes; ++i) {
            dn2cpp_vec_set<T>(r, i, (T)std::sqrt(dn2cpp_vec_get<T>(v, i)));
        }
        return r;
    } else {
        // sqrt is float/double only; integer instantiations return v unchanged.
        return v;
    }
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_min(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_min<T, N>(a, b);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        T av = dn2cpp_vec_get<T>(a, i), bv = dn2cpp_vec_get<T>(b, i);
        dn2cpp_vec_set<T>(r, i, av < bv ? av : bv);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_max(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_max<T, N>(a, b);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        T av = dn2cpp_vec_get<T>(a, i), bv = dn2cpp_vec_get<T>(b, i);
        dn2cpp_vec_set<T>(r, i, av < bv ? bv : av);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_clamp(const Dn2CppVec<N>& v, const Dn2CppVec<N>& lo, const Dn2CppVec<N>& hi) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_clamp<T, N>(v, lo, hi);
#endif
    // min(max(v, lo), hi)
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        T x = dn2cpp_vec_get<T>(v, i);
        T loV = dn2cpp_vec_get<T>(lo, i);
        T hiV = dn2cpp_vec_get<T>(hi, i);
        T mx = x < loV ? loV : x;       // max(x, lo)
        T res = hiV < mx ? hiV : mx;    // min(mx, hi)
        dn2cpp_vec_set<T>(r, i, res);
    }
    return r;
}

template <class T, int N>
static inline T dn2cpp_vec_sum(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_sum<T, N>(v);
#endif
    const int lanes = N / (int)sizeof(T);
    if constexpr (std::is_integral<T>::value) {
        using U = std::make_unsigned_t<T>;
        U acc = 0;
        for (int i = 0; i < lanes; ++i) {
            acc = (U)(acc + (U)dn2cpp_vec_get<T>(v, i));
        }
        return (T)acc;
    } else {
        T acc = (T)0;
        for (int i = 0; i < lanes; ++i) {
            acc += dn2cpp_vec_get<T>(v, i);
        }
        return acc;
    }
}

template <class T, int N>
static inline T dn2cpp_vec_dot(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_dot<T, N>(a, b);
#endif
    const int lanes = N / (int)sizeof(T);
    if constexpr (std::is_integral<T>::value) {
        using U = std::make_unsigned_t<T>;
        U acc = 0;
        for (int i = 0; i < lanes; ++i) {
            U prod = (U)((U)dn2cpp_vec_get<T>(a, i) * (U)dn2cpp_vec_get<T>(b, i));
            acc = (U)(acc + prod);
        }
        return (T)acc;
    } else {
        T acc = (T)0;
        for (int i = 0; i < lanes; ++i) {
            acc += dn2cpp_vec_get<T>(a, i) * dn2cpp_vec_get<T>(b, i);
        }
        return acc;
    }
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_fma(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b, const Dn2CppVec<N>& c) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_fma<T, N>(a, b, c);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        T av = dn2cpp_vec_get<T>(a, i);
        T bv = dn2cpp_vec_get<T>(b, i);
        T cv = dn2cpp_vec_get<T>(c, i);
        if constexpr (std::is_floating_point<T>::value) {
            dn2cpp_vec_set<T>(r, i, (T)std::fma(av, bv, cv));
        } else {
            using U = std::make_unsigned_t<T>;
            dn2cpp_vec_set<T>(r, i, (T)((U)((U)av * (U)bv) + (U)cv));
        }
    }
    return r;
}

// ---------------------------------------------------------------------------
// Saturating arithmetic (integer lanes clamp at the type bounds; float lanes
// are a plain add/sub -- IEEE floats have no integer-style overflow to clamp).
// ---------------------------------------------------------------------------

// Scalar saturating add for one integer lane. The bound checks are written so
// the comparison operand never itself overflows: for add we test against
// max-b / min-b (each in range for the relevant sign of b), so the wrap is only
// computed on the non-saturating path.
template <class T>
static inline T dn2cpp_sat_add_int(T a, T b) {
    constexpr T mx = std::numeric_limits<T>::max();
    constexpr T mn = std::numeric_limits<T>::min();
    if constexpr (std::is_signed<T>::value) {
        if (b >= 0) { if (a > (T)(mx - b)) return mx; }
        else        { if (a < (T)(mn - b)) return mn; }
        using U = std::make_unsigned_t<T>;
        return (T)((U)a + (U)b);
    } else {
        T s = (T)(a + b);
        return s < a ? mx : s;          // unsigned wrap shows up as a smaller sum
    }
}

template <class T>
static inline T dn2cpp_sat_sub_int(T a, T b) {
    constexpr T mx = std::numeric_limits<T>::max();
    constexpr T mn = std::numeric_limits<T>::min();
    if constexpr (std::is_signed<T>::value) {
        if (b >= 0) { if (a < (T)(mn + b)) return mn; }
        else        { if (a > (T)(mx + b)) return mx; }
        using U = std::make_unsigned_t<T>;
        return (T)((U)a - (U)b);
    } else {
        return a < b ? (T)0 : (T)(a - b);
    }
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_add_saturate(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_add_saturate<T, N>(a, b);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        T av = dn2cpp_vec_get<T>(a, i), bv = dn2cpp_vec_get<T>(b, i);
        if constexpr (std::is_floating_point<T>::value) {
            dn2cpp_vec_set<T>(r, i, (T)(av + bv));
        } else {
            dn2cpp_vec_set<T>(r, i, dn2cpp_sat_add_int<T>(av, bv));
        }
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_subtract_saturate(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_subtract_saturate<T, N>(a, b);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        T av = dn2cpp_vec_get<T>(a, i), bv = dn2cpp_vec_get<T>(b, i);
        if constexpr (std::is_floating_point<T>::value) {
            dn2cpp_vec_set<T>(r, i, (T)(av - bv));
        } else {
            dn2cpp_vec_set<T>(r, i, dn2cpp_sat_sub_int<T>(av, bv));
        }
    }
    return r;
}

// CopySign: float uses IEEE copysign (so -0 / NaN signs propagate bit-exactly);
// signed integers take the magnitude of `a` with the sign of `b`; unsigned has
// no sign to copy, so `a` passes through unchanged.
template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_copysign(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_copysign<T, N>(a, b);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        T av = dn2cpp_vec_get<T>(a, i), bv = dn2cpp_vec_get<T>(b, i);
        if constexpr (std::is_floating_point<T>::value) {
            dn2cpp_vec_set<T>(r, i, (T)std::copysign(av, bv));
        } else if constexpr (std::is_signed<T>::value) {
            using U = std::make_unsigned_t<T>;
            U mag = av < 0 ? (U)(-(U)av) : (U)av;       // |a|, two's-complement safe
            dn2cpp_vec_set<T>(r, i, bv < 0 ? (T)(-(U)mag) : (T)mag);
        } else {
            dn2cpp_vec_set<T>(r, i, av);
        }
    }
    return r;
}

// ---------------------------------------------------------------------------
// Rounding (float lanes only; integer lanes are already whole, so identity).
// Round is round-half-to-even -- std::nearbyint under the default FE_TONEAREST.
// ---------------------------------------------------------------------------

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_ceiling(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_ceiling<T, N>(v);
#endif
    if constexpr (std::is_floating_point<T>::value) {
        Dn2CppVec<N> r;
        const int lanes = N / (int)sizeof(T);
        for (int i = 0; i < lanes; ++i)
            dn2cpp_vec_set<T>(r, i, (T)std::ceil(dn2cpp_vec_get<T>(v, i)));
        return r;
    } else {
        return v;
    }
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_floor(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_floor<T, N>(v);
#endif
    if constexpr (std::is_floating_point<T>::value) {
        Dn2CppVec<N> r;
        const int lanes = N / (int)sizeof(T);
        for (int i = 0; i < lanes; ++i)
            dn2cpp_vec_set<T>(r, i, (T)std::floor(dn2cpp_vec_get<T>(v, i)));
        return r;
    } else {
        return v;
    }
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_round(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_round<T, N>(v);
#endif
    if constexpr (std::is_floating_point<T>::value) {
        Dn2CppVec<N> r;
        const int lanes = N / (int)sizeof(T);
        for (int i = 0; i < lanes; ++i)
            dn2cpp_vec_set<T>(r, i, (T)std::nearbyint(dn2cpp_vec_get<T>(v, i)));
        return r;
    } else {
        return v;
    }
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_truncate(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_truncate<T, N>(v);
#endif
    if constexpr (std::is_floating_point<T>::value) {
        Dn2CppVec<N> r;
        const int lanes = N / (int)sizeof(T);
        for (int i = 0; i < lanes; ++i)
            dn2cpp_vec_set<T>(r, i, (T)std::trunc(dn2cpp_vec_get<T>(v, i)));
        return r;
    } else {
        return v;
    }
}

// ---------------------------------------------------------------------------
// Transcendental / unit-conversion (float lanes only; the integer
// instantiations exist purely so every T compiles -- they are never reached for
// integers and just return the input unchanged).
// ---------------------------------------------------------------------------

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_asin(const Dn2CppVec<N>& v) {
    if constexpr (std::is_floating_point<T>::value) {
        Dn2CppVec<N> r;
        const int lanes = N / (int)sizeof(T);
        for (int i = 0; i < lanes; ++i)
            dn2cpp_vec_set<T>(r, i, (T)std::asin(dn2cpp_vec_get<T>(v, i)));
        return r;
    } else {
        return v;
    }
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_cos(const Dn2CppVec<N>& v) {
    if constexpr (std::is_floating_point<T>::value) {
        Dn2CppVec<N> r;
        const int lanes = N / (int)sizeof(T);
        for (int i = 0; i < lanes; ++i)
            dn2cpp_vec_set<T>(r, i, (T)std::cos(dn2cpp_vec_get<T>(v, i)));
        return r;
    } else {
        return v;
    }
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_exp(const Dn2CppVec<N>& v) {
    if constexpr (std::is_floating_point<T>::value) {
        Dn2CppVec<N> r;
        const int lanes = N / (int)sizeof(T);
        for (int i = 0; i < lanes; ++i)
            dn2cpp_vec_set<T>(r, i, (T)std::exp(dn2cpp_vec_get<T>(v, i)));
        return r;
    } else {
        return v;
    }
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_hypot(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    if constexpr (std::is_floating_point<T>::value) {
        Dn2CppVec<N> r;
        const int lanes = N / (int)sizeof(T);
        for (int i = 0; i < lanes; ++i)
            dn2cpp_vec_set<T>(r, i, (T)std::hypot(dn2cpp_vec_get<T>(a, i), dn2cpp_vec_get<T>(b, i)));
        return r;
    } else {
        return a;
    }
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_degrees_to_radians(const Dn2CppVec<N>& v) {
    if constexpr (std::is_floating_point<T>::value) {
        Dn2CppVec<N> r;
        const int lanes = N / (int)sizeof(T);
        for (int i = 0; i < lanes; ++i) {
            T x = dn2cpp_vec_get<T>(v, i);
            // Match the BCL's single multiply by the precomputed pi/180 factor.
            dn2cpp_vec_set<T>(r, i, (T)(x * (T)(3.141592653589793238462643383279502884 / 180.0)));
        }
        return r;
    } else {
        return v;
    }
}

// ---------------------------------------------------------------------------
// Bitwise (operate on the raw bytes; T is for uniform dispatch only)
// ---------------------------------------------------------------------------

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_and(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_and<T, N>(a, b);
#endif
    Dn2CppVec<N> r;
    for (int i = 0; i < N; ++i) {
        r.b[i] = (unsigned char)(a.b[i] & b.b[i]);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_or(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_or<T, N>(a, b);
#endif
    Dn2CppVec<N> r;
    for (int i = 0; i < N; ++i) {
        r.b[i] = (unsigned char)(a.b[i] | b.b[i]);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_xor(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_xor<T, N>(a, b);
#endif
    Dn2CppVec<N> r;
    for (int i = 0; i < N; ++i) {
        r.b[i] = (unsigned char)(a.b[i] ^ b.b[i]);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_andnot(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_andnot<T, N>(a, b);
#endif
    // a & ~b
    Dn2CppVec<N> r;
    for (int i = 0; i < N; ++i) {
        r.b[i] = (unsigned char)(a.b[i] & (unsigned char)~b.b[i]);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_ones_complement(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_ones_complement<T, N>(v);
#endif
    Dn2CppVec<N> r;
    for (int i = 0; i < N; ++i) {
        r.b[i] = (unsigned char)~v.b[i];
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_shift_left(const Dn2CppVec<N>& v, int count) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_shift_left<T, N>(v, count);
#endif
    if constexpr (std::is_integral<T>::value) {
        using U = std::make_unsigned_t<T>;
        Dn2CppVec<N> r;
        const int lanes = N / (int)sizeof(T);
        for (int i = 0; i < lanes; ++i) {
            U x = (U)dn2cpp_vec_get<T>(v, i);
            dn2cpp_vec_set<T>(r, i, (T)(U)(x << count));
        }
        return r;
    } else {
        return v;
    }
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_shift_right_logical(const Dn2CppVec<N>& v, int count) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_shift_right_logical<T, N>(v, count);
#endif
    if constexpr (std::is_integral<T>::value) {
        using U = std::make_unsigned_t<T>;
        Dn2CppVec<N> r;
        const int lanes = N / (int)sizeof(T);
        for (int i = 0; i < lanes; ++i) {
            U x = (U)dn2cpp_vec_get<T>(v, i);
            dn2cpp_vec_set<T>(r, i, (T)(U)(x >> count));
        }
        return r;
    } else {
        return v;
    }
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_shift_right_arithmetic(const Dn2CppVec<N>& v, int count) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_shift_right_arithmetic<T, N>(v, count);
#endif
    if constexpr (std::is_integral<T>::value) {
        if constexpr (std::is_signed<T>::value) {
            Dn2CppVec<N> r;
            const int lanes = N / (int)sizeof(T);
            for (int i = 0; i < lanes; ++i) {
                T x = dn2cpp_vec_get<T>(v, i);
                // Portable arithmetic shift: logical shift, then fill sign bits.
                using U = std::make_unsigned_t<T>;
                U ux = (U)x >> count;
                if (x < 0 && count > 0) {
                    const int bits = (int)sizeof(T) * 8;
                    U fill = (U)(~(U)0) << (bits - count);
                    ux |= fill;
                }
                dn2cpp_vec_set<T>(r, i, (T)ux);
            }
            return r;
        } else {
            // Unsigned T: arithmetic == logical.
            return dn2cpp_vec_shift_right_logical<T, N>(v, count);
        }
    } else {
        return v;
    }
}

// ---------------------------------------------------------------------------
// Comparison -> vector mask (true lane = all-ones of the element width)
// ---------------------------------------------------------------------------

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_equals(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_equals<T, N>(a, b);
#endif
    Dn2CppVec<N> r;
    std::memset(r.b, 0, sizeof(r.b));
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        if (dn2cpp_vec_get<T>(a, i) == dn2cpp_vec_get<T>(b, i)) {
            std::memset(r.b + (size_t)i * sizeof(T), 0xFF, sizeof(T));
        }
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_not_equals(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_not_equals<T, N>(a, b);
#endif
    Dn2CppVec<N> r;
    std::memset(r.b, 0, sizeof(r.b));
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        if (dn2cpp_vec_get<T>(a, i) != dn2cpp_vec_get<T>(b, i)) {
            std::memset(r.b + (size_t)i * sizeof(T), 0xFF, sizeof(T));
        }
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_greater_than(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_greater_than<T, N>(a, b);
#endif
    Dn2CppVec<N> r;
    std::memset(r.b, 0, sizeof(r.b));
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        if (dn2cpp_vec_get<T>(a, i) > dn2cpp_vec_get<T>(b, i)) {
            std::memset(r.b + (size_t)i * sizeof(T), 0xFF, sizeof(T));
        }
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_less_than(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_less_than<T, N>(a, b);
#endif
    Dn2CppVec<N> r;
    std::memset(r.b, 0, sizeof(r.b));
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        if (dn2cpp_vec_get<T>(a, i) < dn2cpp_vec_get<T>(b, i)) {
            std::memset(r.b + (size_t)i * sizeof(T), 0xFF, sizeof(T));
        }
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_greater_than_or_equal(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_greater_than_or_equal<T, N>(a, b);
#endif
    Dn2CppVec<N> r;
    std::memset(r.b, 0, sizeof(r.b));
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        if (dn2cpp_vec_get<T>(a, i) >= dn2cpp_vec_get<T>(b, i)) {
            std::memset(r.b + (size_t)i * sizeof(T), 0xFF, sizeof(T));
        }
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_less_than_or_equal(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_less_than_or_equal<T, N>(a, b);
#endif
    Dn2CppVec<N> r;
    std::memset(r.b, 0, sizeof(r.b));
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        if (dn2cpp_vec_get<T>(a, i) <= dn2cpp_vec_get<T>(b, i)) {
            std::memset(r.b + (size_t)i * sizeof(T), 0xFF, sizeof(T));
        }
    }
    return r;
}

// ---------------------------------------------------------------------------
// Numeric-category predicates -> vector mask (true lane = all-ones of the
// element width, matching the comparison ops). Each is the vector form of the
// scalar Type.IsXxx helpers. For integer T most categories are constant (an
// integer is never NaN/Infinity/Subnormal, always Finite/Integer), so the
// `if constexpr` branches fold the float-only math away and keep every
// instantiation warning-free.
// ---------------------------------------------------------------------------

// Writes lane i as an all-ones (true) or all-zero (false) mask of T's width.
template <class T, int N>
static inline void dn2cpp_vec_set_lane_mask(Dn2CppVec<N>& r, int i, bool on) {
    std::memset(r.b + (size_t)i * sizeof(T), on ? 0xFF : 0x00, sizeof(T));
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_is_nan(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_is_nan<T, N>(v);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        bool on;
        if constexpr (std::is_floating_point<T>::value) on = std::isnan(dn2cpp_vec_get<T>(v, i));
        else on = false;
        dn2cpp_vec_set_lane_mask<T>(r, i, on);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_is_finite(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_is_finite<T, N>(v);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        bool on;
        if constexpr (std::is_floating_point<T>::value) on = std::isfinite(dn2cpp_vec_get<T>(v, i));
        else on = true;             // every integer is finite
        dn2cpp_vec_set_lane_mask<T>(r, i, on);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_is_infinity(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_is_infinity<T, N>(v);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        bool on;
        if constexpr (std::is_floating_point<T>::value) on = std::isinf(dn2cpp_vec_get<T>(v, i));
        else on = false;
        dn2cpp_vec_set_lane_mask<T>(r, i, on);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_is_positive_infinity(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_is_positive_infinity<T, N>(v);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        bool on;
        if constexpr (std::is_floating_point<T>::value) {
            T x = dn2cpp_vec_get<T>(v, i);
            on = std::isinf(x) && !std::signbit(x);
        } else on = false;
        dn2cpp_vec_set_lane_mask<T>(r, i, on);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_is_negative_infinity(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_is_negative_infinity<T, N>(v);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        bool on;
        if constexpr (std::is_floating_point<T>::value) {
            T x = dn2cpp_vec_get<T>(v, i);
            on = std::isinf(x) && std::signbit(x);
        } else on = false;
        dn2cpp_vec_set_lane_mask<T>(r, i, on);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_is_negative(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_is_negative<T, N>(v);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        T x = dn2cpp_vec_get<T>(v, i);
        bool on;
        if constexpr (std::is_floating_point<T>::value) on = std::signbit(x);   // -0 / -NaN count
        else if constexpr (std::is_signed<T>::value) on = x < 0;
        else on = false;
        dn2cpp_vec_set_lane_mask<T>(r, i, on);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_is_positive(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_is_positive<T, N>(v);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        T x = dn2cpp_vec_get<T>(v, i);
        bool on;
        if constexpr (std::is_floating_point<T>::value) on = !std::signbit(x);   // complement of IsNegative
        else if constexpr (std::is_signed<T>::value) on = x >= 0;
        else on = true;
        dn2cpp_vec_set_lane_mask<T>(r, i, on);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_is_zero(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_is_zero<T, N>(v);
#endif
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        // For float, == 0 treats +0 and -0 alike, matching the BCL.
        dn2cpp_vec_set_lane_mask<T>(r, i, dn2cpp_vec_get<T>(v, i) == (T)0);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_is_normal(const Dn2CppVec<N>& v) {
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        T x = dn2cpp_vec_get<T>(v, i);
        bool on;
        if constexpr (std::is_floating_point<T>::value) on = std::isnormal(x);
        else on = x != (T)0;        // BCL: an integer is "normal" iff nonzero
        dn2cpp_vec_set_lane_mask<T>(r, i, on);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_is_subnormal(const Dn2CppVec<N>& v) {
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        bool on;
        if constexpr (std::is_floating_point<T>::value) on = std::fpclassify(dn2cpp_vec_get<T>(v, i)) == FP_SUBNORMAL;
        else on = false;
        dn2cpp_vec_set_lane_mask<T>(r, i, on);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_is_integer(const Dn2CppVec<N>& v) {
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        bool on;
        if constexpr (std::is_floating_point<T>::value) {
            T x = dn2cpp_vec_get<T>(v, i);
            on = std::isfinite(x) && std::trunc(x) == x;
        } else on = true;
        dn2cpp_vec_set_lane_mask<T>(r, i, on);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_is_even_integer(const Dn2CppVec<N>& v) {
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        T x = dn2cpp_vec_get<T>(v, i);
        bool on;
        if constexpr (std::is_floating_point<T>::value) {
            bool isInt = std::isfinite(x) && std::trunc(x) == x;
            on = isInt && std::fmod(x, (T)2) == (T)0;
        } else {
            on = (x % (T)2) == (T)0;    // negative even still has remainder 0
        }
        dn2cpp_vec_set_lane_mask<T>(r, i, on);
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_is_odd_integer(const Dn2CppVec<N>& v) {
    Dn2CppVec<N> r;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        T x = dn2cpp_vec_get<T>(v, i);
        bool on;
        if constexpr (std::is_floating_point<T>::value) {
            bool isInt = std::isfinite(x) && std::trunc(x) == x;
            on = isInt && std::fabs(std::fmod(x, (T)2)) == (T)1;
        } else {
            on = (x % (T)2) != (T)0;    // sign-agnostic: any odd has nonzero remainder
        }
        dn2cpp_vec_set_lane_mask<T>(r, i, on);
    }
    return r;
}

// ---------------------------------------------------------------------------
// Comparison reductions
// ---------------------------------------------------------------------------

template <class T, int N>
static inline bool dn2cpp_vec_equals_all(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_equals_all<T, N>(a, b);
#endif
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        if (!(dn2cpp_vec_get<T>(a, i) == dn2cpp_vec_get<T>(b, i))) {
            return false;
        }
    }
    return true;
}

template <class T, int N>
static inline bool dn2cpp_vec_equals_any(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_equals_any<T, N>(a, b);
#endif
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        if (dn2cpp_vec_get<T>(a, i) == dn2cpp_vec_get<T>(b, i)) {
            return true;
        }
    }
    return false;
}

template <class T, int N>
static inline bool dn2cpp_vec_gt_all(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_gt_all<T, N>(a, b);
#endif
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        if (!(dn2cpp_vec_get<T>(a, i) > dn2cpp_vec_get<T>(b, i))) {
            return false;
        }
    }
    return true;
}

template <class T, int N>
static inline bool dn2cpp_vec_gt_any(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_gt_any<T, N>(a, b);
#endif
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        if (dn2cpp_vec_get<T>(a, i) > dn2cpp_vec_get<T>(b, i)) {
            return true;
        }
    }
    return false;
}

template <class T, int N>
static inline bool dn2cpp_vec_lt_all(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_lt_all<T, N>(a, b);
#endif
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        if (!(dn2cpp_vec_get<T>(a, i) < dn2cpp_vec_get<T>(b, i))) {
            return false;
        }
    }
    return true;
}

template <class T, int N>
static inline bool dn2cpp_vec_lt_any(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_lt_any<T, N>(a, b);
#endif
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        if (dn2cpp_vec_get<T>(a, i) < dn2cpp_vec_get<T>(b, i)) {
            return true;
        }
    }
    return false;
}

// ---------------------------------------------------------------------------
// Element search / mask population (return int / bool scalars, not vectors).
// "All bits set" tests the raw lane bytes (all 0xFF) so it works uniformly for
// float lanes too -- an all-ones float is a NaN bit pattern, but we only look
// at the bits, matching the BCL's WhereAllBitsSet family.
// ---------------------------------------------------------------------------

template <class T, int N>
static inline bool dn2cpp_vec_lane_all_bits(const Dn2CppVec<N>& v, int i) {
    for (size_t k = 0; k < sizeof(T); ++k) {
        if (v.b[(size_t)i * sizeof(T) + k] != 0xFF) {
            return false;
        }
    }
    return true;
}

template <class T, int N>
static inline int32_t dn2cpp_vec_count(const Dn2CppVec<N>& v, T value) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_count<T, N>(v, value);
#endif
    int32_t c = 0;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i)
        if (dn2cpp_vec_get<T>(v, i) == value) ++c;
    return c;
}

template <class T, int N>
static inline int32_t dn2cpp_vec_index_of(const Dn2CppVec<N>& v, T value) {
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i)
        if (dn2cpp_vec_get<T>(v, i) == value) return i;
    return -1;
}

template <class T, int N>
static inline int32_t dn2cpp_vec_last_index_of(const Dn2CppVec<N>& v, T value) {
    const int lanes = N / (int)sizeof(T);
    for (int i = lanes - 1; i >= 0; --i)
        if (dn2cpp_vec_get<T>(v, i) == value) return i;
    return -1;
}

template <class T, int N>
static inline int32_t dn2cpp_vec_count_all_bits(const Dn2CppVec<N>& v) {
    int32_t c = 0;
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i)
        if (dn2cpp_vec_lane_all_bits<T>(v, i)) ++c;
    return c;
}

template <class T, int N>
static inline int32_t dn2cpp_vec_index_of_all_bits(const Dn2CppVec<N>& v) {
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i)
        if (dn2cpp_vec_lane_all_bits<T>(v, i)) return i;
    return -1;
}

template <class T, int N>
static inline int32_t dn2cpp_vec_last_index_of_all_bits(const Dn2CppVec<N>& v) {
    const int lanes = N / (int)sizeof(T);
    for (int i = lanes - 1; i >= 0; --i)
        if (dn2cpp_vec_lane_all_bits<T>(v, i)) return i;
    return -1;
}

template <class T, int N>
static inline bool dn2cpp_vec_all_all_bits(const Dn2CppVec<N>& v) {
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i)
        if (!dn2cpp_vec_lane_all_bits<T>(v, i)) return false;
    return true;
}

template <class T, int N>
static inline bool dn2cpp_vec_any_all_bits(const Dn2CppVec<N>& v) {
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i)
        if (dn2cpp_vec_lane_all_bits<T>(v, i)) return true;
    return false;
}

template <class T, int N>
static inline bool dn2cpp_vec_all(const Dn2CppVec<N>& v, T value) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_all<T, N>(v, value);
#endif
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i)
        if (!(dn2cpp_vec_get<T>(v, i) == value)) return false;
    return true;
}

template <class T, int N>
static inline bool dn2cpp_vec_any(const Dn2CppVec<N>& v, T value) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_any<T, N>(v, value);
#endif
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i)
        if (dn2cpp_vec_get<T>(v, i) == value) return true;
    return false;
}

// Per-bit blend: (mask & a) | (~mask & b), computed byte-wise.
template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_conditional_select(const Dn2CppVec<N>& mask, const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_conditional_select<T, N>(mask, a, b);
#endif
    Dn2CppVec<N> r;
    for (int i = 0; i < N; ++i) {
        r.b[i] = (unsigned char)((mask.b[i] & a.b[i]) | ((unsigned char)~mask.b[i] & b.b[i]));
    }
    return r;
}

// ---------------------------------------------------------------------------
// Mask extraction
// ---------------------------------------------------------------------------

template <class T, int N>
static inline uint64_t dn2cpp_vec_extract_msb(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_extract_msb<T, N>(v);
#endif
    const int lanes = N / (int)sizeof(T);
    uint64_t mask = 0;
    for (int i = 0; i < lanes; ++i) {
        T x = dn2cpp_vec_get<T>(v, i);
        bool msb;
        if constexpr (std::is_floating_point<T>::value) {
            msb = std::signbit(x);
        } else if constexpr (std::is_signed<T>::value) {
            msb = x < 0;
        } else {
            msb = ((x >> (sizeof(T) * 8 - 1)) & 1) != 0;
        }
        if (msb) {
            mask |= (uint64_t)1 << i;
        }
    }
    return mask;
}

// ---------------------------------------------------------------------------
// Reinterpret (As<>, AsByte, AsInt32, ...) -- identity, width unchanged
// ---------------------------------------------------------------------------

template <int N>
static inline Dn2CppVec<N> dn2cpp_vec_reinterpret(const Dn2CppVec<N>& v) {
    return v;
}

// ---------------------------------------------------------------------------
// Convert (element-type changing, same lane count / byte width)
// ---------------------------------------------------------------------------

template <class TFrom, class TTo, int N>
static inline Dn2CppVec<N> dn2cpp_vec_convert(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_convert<TFrom, TTo, N>(v);
#endif
    // Same N, same lane count (sizeof(TFrom) == sizeof(TTo)).
    Dn2CppVec<N> r;
    std::memset(r.b, 0, sizeof(r.b));
    const int lanes = N / (int)sizeof(TFrom);
    for (int i = 0; i < lanes; ++i) {
        TFrom x = dn2cpp_vec_get<TFrom>(v, i);
        dn2cpp_vec_set<TTo>(r, i, (TTo)x);
    }
    return r;
}

// Narrow: two TSrc vectors -> one vector of half-width TDst lanes.
// lower fills the low half of the result lanes, upper the high half.
template <class TSrc, class TDst, int N>
static inline Dn2CppVec<N> dn2cpp_vec_narrow(const Dn2CppVec<N>& lower, const Dn2CppVec<N>& upper) {
    Dn2CppVec<N> r;
    std::memset(r.b, 0, sizeof(r.b));
    const int srcLanes = N / (int)sizeof(TSrc);
    for (int i = 0; i < srcLanes; ++i) {
        dn2cpp_vec_set<TDst>(r, i, (TDst)dn2cpp_vec_get<TSrc>(lower, i));
    }
    for (int i = 0; i < srcLanes; ++i) {
        dn2cpp_vec_set<TDst>(r, srcLanes + i, (TDst)dn2cpp_vec_get<TSrc>(upper, i));
    }
    return r;
}

// Widen lower: low-half TSrc lanes -> TDst (double-width) lanes.
template <class TSrc, class TDst, int N>
static inline Dn2CppVec<N> dn2cpp_vec_widen_lower(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_widen_lower<TSrc, TDst, N>(v);
#endif
    Dn2CppVec<N> r;
    std::memset(r.b, 0, sizeof(r.b));
    const int dstLanes = N / (int)sizeof(TDst);
    for (int i = 0; i < dstLanes; ++i) {
        TSrc x = dn2cpp_vec_get<TSrc>(v, i);
        dn2cpp_vec_set<TDst>(r, i, (TDst)x);
    }
    return r;
}

// Widen upper: high-half TSrc lanes -> TDst (double-width) lanes.
template <class TSrc, class TDst, int N>
static inline Dn2CppVec<N> dn2cpp_vec_widen_upper(const Dn2CppVec<N>& v) {
#if defined(DN2CPP_USE_HIGHWAY)
    return dn2cpp_hwy::vec_widen_upper<TSrc, TDst, N>(v);
#endif
    Dn2CppVec<N> r;
    std::memset(r.b, 0, sizeof(r.b));
    const int dstLanes = N / (int)sizeof(TDst);
    for (int i = 0; i < dstLanes; ++i) {
        TSrc x = dn2cpp_vec_get<TSrc>(v, dstLanes + i);
        dn2cpp_vec_set<TDst>(r, i, (TDst)x);
    }
    return r;
}

// Unpack low / high: interleave the low (high) halves of two vectors,
// r[2i] = a[i], r[2i+1] = b[i] over the first (second) half of the lanes —
// SSE2 UNPCKL/UNPCKH, NEON ZIP1/ZIP2, the internal Vector128.UnpackLow/High the
// BCL's hex and ASCII paths use.
template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_unpack_low(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    Dn2CppVec<N> r;
    const int half = N / (int)sizeof(T) / 2;
    for (int i = 0; i < half; ++i) {
        dn2cpp_vec_set<T>(r, 2 * i, dn2cpp_vec_get<T>(a, i));
        dn2cpp_vec_set<T>(r, 2 * i + 1, dn2cpp_vec_get<T>(b, i));
    }
    return r;
}

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_unpack_high(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    Dn2CppVec<N> r;
    const int half = N / (int)sizeof(T) / 2;
    for (int i = 0; i < half; ++i) {
        dn2cpp_vec_set<T>(r, 2 * i, dn2cpp_vec_get<T>(a, half + i));
        dn2cpp_vec_set<T>(r, 2 * i + 1, dn2cpp_vec_get<T>(b, half + i));
    }
    return r;
}

// ---------------------------------------------------------------------------
// Shuffle
// ---------------------------------------------------------------------------

template <class T, int N>
static inline Dn2CppVec<N> dn2cpp_vec_shuffle(const Dn2CppVec<N>& v, const Dn2CppVec<N>& indices) {
    // result lane i = v[indices[i]]; an out-of-range index (>= lanes, which
    // includes any index whose high bit is set) yields a zero lane. The index
    // is read as the unsigned integer occupying the same byte-width as T, so
    // the high-bit / negative semantics work uniformly across element types.
    Dn2CppVec<N> r;
    std::memset(r.b, 0, sizeof(r.b));
    const int lanes = N / (int)sizeof(T);
    for (int i = 0; i < lanes; ++i) {
        uint64_t idx = 0;
        std::memcpy(&idx, indices.b + (size_t)i * sizeof(T), sizeof(T));
        if (idx < (uint64_t)lanes) {
            std::memcpy(r.b + (size_t)i * sizeof(T),
                        v.b + (size_t)idx * sizeof(T),
                        sizeof(T));
        }
    }
    return r;
}

// The hardware-intrinsic surface hangs off this header because generated code
// includes it and the runtime's own TUs never do: they must not pay for the
// intrinsic headers the ISA helpers pull in.
#include "dn2cpp_cpu_features.h"
#include "isa/dn2cpp_isa.h"
