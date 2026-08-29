#pragma once
// dn2cpp_vectors_hwy.h
//
// Google Highway backing for the portable-SIMD `dn2cpp_vec_*` ops. Included by
// dn2cpp_vectors.h ONLY when the runtime is built with -DDN2CPP_USE_HIGHWAY
// (CMake option DN2CPP_USE_HIGHWAY, default OFF). The default build never sees
// this header and keeps the scalar emulation byte-for-byte.
//
// Static single-ISA dispatch (HWY_STATIC_DISPATCH): the ISA is fixed at compile
// time by -march/-mcpu; this is header-only (no Highway .cc to link).
//
// Representation is UNCHANGED: a vector is still the `Dn2CppVec<N>` POD byte
// buffer passed BY VALUE. Each op loads the bytes into a Highway vector, runs the
// native op, and stores back. With clang -O2 the load/op/store of adjacent inlined
// ops fuse to bare SIMD instructions with no memory round-trip (verified: a chained
// add+mul over Vector128<int> lowers to `add.4s` + `mul.4s`, no stack spill).
//
// Width handling: dn2cpp pins Vector<T> at 128-bit and supports Vector64/128/256/512.
// A `CappedTag<T, N/sizeof(T)>` yields min(buffer-lanes, native-lanes); the loop walks
// the fixed buffer in `Lanes(d)`-sized chunks, so a 256/512-bit dn2cpp vector decomposes
// into native-width ops on a 128-bit ISA, or a single op when the ISA is wide enough.
// N/sizeof(T) and the native lane count are both powers of two, so the chunking divides
// evenly.

#include "hwy/highway.h"

namespace dn2cpp_hwy {

namespace hn = hwy::HWY_NAMESPACE;

// Highway only accepts the fixed-width integer / float types as lane types. dn2cpp's
// element types also include char16_t (.NET System.Char) and the pointer-width ints
// (intptr_t/uintptr_t), which on some targets are a distinct C++ type Highway rejects
// (e.g. on Apple int64_t is `long long` but intptr_t is `long`; char16_t is never a
// lane type). lane_t<T> maps each integer element to the same-width, same-signedness
// fixed-width type Highway does accept, and leaves float/double unchanged. SIMD ops
// are bit-identical and the .NET comparison semantics are preserved (char is unsigned
// → uint16_t gives the unsigned ordering the BCL uses for UTF-16 code units).
template <size_t S, bool Signed> struct fixed_int;
template <> struct fixed_int<1, true>  { using type = int8_t;   };
template <> struct fixed_int<2, true>  { using type = int16_t;  };
template <> struct fixed_int<4, true>  { using type = int32_t;  };
template <> struct fixed_int<8, true>  { using type = int64_t;  };
template <> struct fixed_int<1, false> { using type = uint8_t;  };
template <> struct fixed_int<2, false> { using type = uint16_t; };
template <> struct fixed_int<4, false> { using type = uint32_t; };
template <> struct fixed_int<8, false> { using type = uint64_t; };
template <class T, bool IsFloat = std::is_floating_point<T>::value>
struct lane_of { using type = typename fixed_int<sizeof(T), std::is_signed<T>::value>::type; };
template <class T> struct lane_of<T, true> { using type = T; };  // float / double pass through
template <class T> using lane_t = typename lane_of<T>::type;

// Walk the fixed buffer in native-width chunks, applying `op(d, va, vb)` per chunk.
// `op` receives the tag so mask-producing ops can call VecFromMask(d, ...).
template <class T, int N, class Op>
static inline Dn2CppVec<N> map2(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b, Op op) {
    Dn2CppVec<N> r;
    const hn::CappedTag<lane_t<T>, (size_t)(N / (int)sizeof(T))> d;
    const size_t total = (size_t)N / sizeof(T);
    const size_t step = hn::Lanes(d);
    for (size_t i = 0; i < total; i += step) {
        auto va = hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(a.b) + i);
        auto vb = hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(b.b) + i);
        hn::StoreU(op(d, va, vb), d, reinterpret_cast<lane_t<T>*>(r.b) + i);
    }
    return r;
}

// --- arithmetic ---
template <class T, int N>
static inline Dn2CppVec<N> vec_add(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    return map2<T, N>(a, b, [](auto d, auto x, auto y) { (void)d; return hn::Add(x, y); });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_subtract(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    return map2<T, N>(a, b, [](auto d, auto x, auto y) { (void)d; return hn::Sub(x, y); });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_multiply(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    return map2<T, N>(a, b, [](auto d, auto x, auto y) { (void)d; return hn::Mul(x, y); });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_min(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    return map2<T, N>(a, b, [](auto d, auto x, auto y) { (void)d; return hn::Min(x, y); });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_max(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    return map2<T, N>(a, b, [](auto d, auto x, auto y) { (void)d; return hn::Max(x, y); });
}

// --- bitwise (T selects lane width only; the result bits are identical) ---
template <class T, int N>
static inline Dn2CppVec<N> vec_and(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    return map2<T, N>(a, b, [](auto d, auto x, auto y) { (void)d; return hn::And(x, y); });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_or(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    return map2<T, N>(a, b, [](auto d, auto x, auto y) { (void)d; return hn::Or(x, y); });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_xor(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    return map2<T, N>(a, b, [](auto d, auto x, auto y) { (void)d; return hn::Xor(x, y); });
}

// --- comparison -> all-ones-lane mask (matches the scalar dn2cpp mask ABI) ---
template <class T, int N>
static inline Dn2CppVec<N> vec_equals(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    return map2<T, N>(a, b, [](auto d, auto x, auto y) { return hn::VecFromMask(d, hn::Eq(x, y)); });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_greater_than(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    return map2<T, N>(a, b, [](auto d, auto x, auto y) { return hn::VecFromMask(d, hn::Gt(x, y)); });
}

// --- reductions (accumulate across chunks, then one horizontal reduce) ---
template <class T, int N>
static inline T vec_sum(const Dn2CppVec<N>& a) {
    const hn::CappedTag<lane_t<T>, (size_t)(N / (int)sizeof(T))> d;
    const size_t total = (size_t)N / sizeof(T);
    const size_t step = hn::Lanes(d);
    auto acc = hn::Zero(d);
    for (size_t i = 0; i < total; i += step) {
        acc = hn::Add(acc, hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(a.b) + i));
    }
    return (T)hn::ReduceSum(d, acc);
}

// Unary chunked map (the single-operand analogue of map2).
template <class T, int N, class Op>
static inline Dn2CppVec<N> map1(const Dn2CppVec<N>& a, Op op) {
    Dn2CppVec<N> r;
    const hn::CappedTag<lane_t<T>, (size_t)(N / (int)sizeof(T))> d;
    const size_t total = (size_t)N / sizeof(T);
    const size_t step = hn::Lanes(d);
    for (size_t i = 0; i < total; i += step) {
        hn::StoreU(op(d, hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(a.b) + i)), d, reinterpret_cast<lane_t<T>*>(r.b) + i);
    }
    return r;
}

// --- more arithmetic ---
template <class T, int N>
static inline Dn2CppVec<N> vec_negate(const Dn2CppVec<N>& a) {
    return map1<T, N>(a, [](auto d, auto x) {
        if constexpr (std::is_unsigned<T>::value) return hn::Sub(hn::Zero(d), x);
        else { (void)d; return hn::Neg(x); }
    });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_abs(const Dn2CppVec<N>& a) {
    return map1<T, N>(a, [](auto d, auto x) {
        (void)d;
        if constexpr (std::is_unsigned<T>::value) return x;
        else return hn::Abs(x);
    });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_sqrt(const Dn2CppVec<N>& a) {
    return map1<T, N>(a, [](auto d, auto x) {
        (void)d;
        if constexpr (std::is_floating_point<T>::value) return hn::Sqrt(x);
        else return x;
    });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_clamp(const Dn2CppVec<N>& v, const Dn2CppVec<N>& lo, const Dn2CppVec<N>& hi) {
    Dn2CppVec<N> r;
    const hn::CappedTag<lane_t<T>, (size_t)(N / (int)sizeof(T))> d;
    const size_t total = (size_t)N / sizeof(T), step = hn::Lanes(d);
    for (size_t i = 0; i < total; i += step) {
        auto xv = hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(v.b) + i);
        auto lov = hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(lo.b) + i);
        auto hiv = hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(hi.b) + i);
        hn::StoreU(hn::Min(hn::Max(xv, lov), hiv), d, reinterpret_cast<lane_t<T>*>(r.b) + i);
    }
    return r;
}
template <class T, int N>
static inline Dn2CppVec<N> vec_fma(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b, const Dn2CppVec<N>& c) {
    Dn2CppVec<N> r;
    const hn::CappedTag<lane_t<T>, (size_t)(N / (int)sizeof(T))> d;
    const size_t total = (size_t)N / sizeof(T), step = hn::Lanes(d);
    for (size_t i = 0; i < total; i += step) {
        auto av = hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(a.b) + i);
        auto bv = hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(b.b) + i);
        auto cv = hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(c.b) + i);
        if constexpr (std::is_floating_point<T>::value) {
            hn::StoreU(hn::MulAdd(av, bv, cv), d, reinterpret_cast<lane_t<T>*>(r.b) + i);
        } else {
            hn::StoreU(hn::Add(hn::Mul(av, bv), cv), d, reinterpret_cast<lane_t<T>*>(r.b) + i);
        }
    }
    return r;
}
template <class T, int N>
static inline T vec_dot(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    const hn::CappedTag<lane_t<T>, (size_t)(N / (int)sizeof(T))> d;
    const size_t total = (size_t)N / sizeof(T), step = hn::Lanes(d);
    auto acc = hn::Zero(d);
    for (size_t i = 0; i < total; i += step) {
        auto av = hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(a.b) + i);
        auto bv = hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(b.b) + i);
        acc = hn::Add(acc, hn::Mul(av, bv));
    }
    return (T)hn::ReduceSum(d, acc);
}
// CopySign: float = IEEE; signed int = |a| with sign of b; unsigned = a.
template <class T, int N>
static inline Dn2CppVec<N> vec_copysign(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    if constexpr (std::is_unsigned<T>::value) {
        return a;
    } else {
        return map2<T, N>(a, b, [](auto d, auto x, auto y) {
            if constexpr (std::is_floating_point<T>::value) { (void)d; return hn::CopySign(x, y); }
            else return hn::IfThenElse(hn::IsNegative(y), hn::Neg(hn::Abs(x)), hn::Abs(x));
        });
    }
}

// --- bitwise (remaining) ---
template <class T, int N>
static inline Dn2CppVec<N> vec_andnot(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    // dn2cpp andnot(a,b) = a & ~b == AndNot(b, a) in Highway (~first & second).
    return map2<T, N>(a, b, [](auto d, auto x, auto y) { (void)d; return hn::AndNot(y, x); });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_ones_complement(const Dn2CppVec<N>& a) {
    return map1<T, N>(a, [](auto d, auto x) { (void)d; return hn::Not(x); });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_shift_left(const Dn2CppVec<N>& a, int count) {
    if constexpr (std::is_integral<T>::value) {
        return map1<T, N>(a, [count](auto d, auto x) { (void)d; return hn::ShiftLeftSame(x, count); });
    } else {
        (void)count; return a;
    }
}
template <class T, int N>
static inline Dn2CppVec<N> vec_shift_right_logical(const Dn2CppVec<N>& a, int count) {
    if constexpr (std::is_integral<T>::value) {
        return map1<T, N>(a, [count](auto d, auto x) {
            const hn::RebindToUnsigned<decltype(d)> du;
            return hn::BitCast(d, hn::ShiftRightSame(hn::BitCast(du, x), count));
        });
    } else {
        (void)count; return a;
    }
}
template <class T, int N>
static inline Dn2CppVec<N> vec_shift_right_arithmetic(const Dn2CppVec<N>& a, int count) {
    if constexpr (std::is_integral<T>::value) {
        // Signed T -> arithmetic (sign-filling) shift; unsigned T -> logical (same as the BCL).
        return map1<T, N>(a, [count](auto d, auto x) { (void)d; return hn::ShiftRightSame(x, count); });
    } else {
        (void)count; return a;
    }
}

// --- comparison (remaining) -> all-ones-lane mask ---
template <class T, int N>
static inline Dn2CppVec<N> vec_not_equals(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    return map2<T, N>(a, b, [](auto d, auto x, auto y) { return hn::VecFromMask(d, hn::Ne(x, y)); });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_less_than(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    return map2<T, N>(a, b, [](auto d, auto x, auto y) { return hn::VecFromMask(d, hn::Lt(x, y)); });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_greater_than_or_equal(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    return map2<T, N>(a, b, [](auto d, auto x, auto y) { return hn::VecFromMask(d, hn::Ge(x, y)); });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_less_than_or_equal(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    return map2<T, N>(a, b, [](auto d, auto x, auto y) { return hn::VecFromMask(d, hn::Le(x, y)); });
}

// --- rounding (float lanes; integer lanes are identity) ---
template <class T, int N>
static inline Dn2CppVec<N> vec_ceiling(const Dn2CppVec<N>& a) {
    return map1<T, N>(a, [](auto d, auto x) { (void)d; if constexpr (std::is_floating_point<T>::value) return hn::Ceil(x); else return x; });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_floor(const Dn2CppVec<N>& a) {
    return map1<T, N>(a, [](auto d, auto x) { (void)d; if constexpr (std::is_floating_point<T>::value) return hn::Floor(x); else return x; });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_round(const Dn2CppVec<N>& a) {
    // Highway Round is round-half-to-even, matching the BCL / std::nearbyint default.
    return map1<T, N>(a, [](auto d, auto x) { (void)d; if constexpr (std::is_floating_point<T>::value) return hn::Round(x); else return x; });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_truncate(const Dn2CppVec<N>& a) {
    return map1<T, N>(a, [](auto d, auto x) { (void)d; if constexpr (std::is_floating_point<T>::value) return hn::Trunc(x); else return x; });
}

// --- category predicates -> all-ones-lane mask (the covered subset) ---
template <class T, int N>
static inline Dn2CppVec<N> vec_is_zero(const Dn2CppVec<N>& a) {
    return map1<T, N>(a, [](auto d, auto x) { return hn::VecFromMask(d, hn::Eq(x, hn::Zero(d))); });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_is_nan(const Dn2CppVec<N>& a) {
    return map1<T, N>(a, [](auto d, auto x) {
        if constexpr (std::is_floating_point<T>::value) return hn::VecFromMask(d, hn::IsNaN(x));
        else { (void)x; return hn::Zero(d); }   // an integer is never NaN
    });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_is_finite(const Dn2CppVec<N>& a) {
    return map1<T, N>(a, [](auto d, auto x) {
        if constexpr (std::is_floating_point<T>::value) return hn::VecFromMask(d, hn::IsFinite(x));
        else return hn::VecFromMask(d, hn::Eq(x, x));   // every integer is finite (all-ones)
    });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_is_infinity(const Dn2CppVec<N>& a) {
    return map1<T, N>(a, [](auto d, auto x) {
        if constexpr (std::is_floating_point<T>::value) return hn::VecFromMask(d, hn::IsInf(x));
        else { (void)x; return hn::Zero(d); }
    });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_is_negative(const Dn2CppVec<N>& a) {
    return map1<T, N>(a, [](auto d, auto x) {
        if constexpr (std::is_floating_point<T>::value) return hn::VecFromMask(d, hn::IsNegative(x));         // sign bit (-0 counts)
        else if constexpr (std::is_signed<T>::value) return hn::VecFromMask(d, hn::Lt(x, hn::Zero(d)));
        else { (void)x; return hn::Zero(d); }
    });
}

// Per-bit blend (mask & a) | (~mask & b), on byte lanes so non-canonical masks
// blend bit-exactly like the scalar ConditionalSelect.
template <class T, int N>
static inline Dn2CppVec<N> vec_conditional_select(const Dn2CppVec<N>& mask, const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    Dn2CppVec<N> r;
    const hn::CappedTag<uint8_t, (size_t)N> d;
    const size_t total = (size_t)N, step = hn::Lanes(d);
    for (size_t i = 0; i < total; i += step) {
        auto m = hn::LoadU(d, mask.b + i);
        auto av = hn::LoadU(d, a.b + i);
        auto bv = hn::LoadU(d, b.b + i);
        hn::StoreU(hn::Or(hn::And(m, av), hn::AndNot(m, bv)), d, r.b + i);
    }
    return r;
}

// --- boolean reductions over equality ---
template <class T, int N>
static inline bool vec_equals_all(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    const hn::CappedTag<lane_t<T>, (size_t)(N / (int)sizeof(T))> d;
    const size_t total = (size_t)N / sizeof(T), step = hn::Lanes(d);
    for (size_t i = 0; i < total; i += step) {
        auto av = hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(a.b) + i);
        auto bv = hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(b.b) + i);
        if (!hn::AllTrue(d, hn::Eq(av, bv))) return false;
    }
    return true;
}
template <class T, int N>
static inline bool vec_equals_any(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    const hn::CappedTag<lane_t<T>, (size_t)(N / (int)sizeof(T))> d;
    const size_t total = (size_t)N / sizeof(T), step = hn::Lanes(d);
    for (size_t i = 0; i < total; i += step) {
        auto av = hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(a.b) + i);
        auto bv = hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(b.b) + i);
        if (!hn::AllFalse(d, hn::Eq(av, bv))) return true;
    }
    return false;
}

// --- ordered boolean reductions (two vectors) ---
template <class T, int N>
static inline bool vec_gt_all(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    const hn::CappedTag<lane_t<T>, (size_t)(N / (int)sizeof(T))> d;
    const size_t total = (size_t)N / sizeof(T), step = hn::Lanes(d);
    for (size_t i = 0; i < total; i += step)
        if (!hn::AllTrue(d, hn::Gt(hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(a.b) + i), hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(b.b) + i)))) return false;
    return true;
}
template <class T, int N>
static inline bool vec_gt_any(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    const hn::CappedTag<lane_t<T>, (size_t)(N / (int)sizeof(T))> d;
    const size_t total = (size_t)N / sizeof(T), step = hn::Lanes(d);
    for (size_t i = 0; i < total; i += step)
        if (!hn::AllFalse(d, hn::Gt(hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(a.b) + i), hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(b.b) + i)))) return true;
    return false;
}
template <class T, int N>
static inline bool vec_lt_all(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    const hn::CappedTag<lane_t<T>, (size_t)(N / (int)sizeof(T))> d;
    const size_t total = (size_t)N / sizeof(T), step = hn::Lanes(d);
    for (size_t i = 0; i < total; i += step)
        if (!hn::AllTrue(d, hn::Lt(hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(a.b) + i), hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(b.b) + i)))) return false;
    return true;
}
template <class T, int N>
static inline bool vec_lt_any(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    const hn::CappedTag<lane_t<T>, (size_t)(N / (int)sizeof(T))> d;
    const size_t total = (size_t)N / sizeof(T), step = hn::Lanes(d);
    for (size_t i = 0; i < total; i += step)
        if (!hn::AllFalse(d, hn::Lt(hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(a.b) + i), hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(b.b) + i)))) return true;
    return false;
}

// --- element search by value (scalar results) ---
template <class T, int N>
static inline int32_t vec_count(const Dn2CppVec<N>& v, T value) {
    const hn::CappedTag<lane_t<T>, (size_t)(N / (int)sizeof(T))> d;
    const size_t total = (size_t)N / sizeof(T), step = hn::Lanes(d);
    const auto val = hn::Set(d, (lane_t<T>)value);
    int32_t c = 0;
    for (size_t i = 0; i < total; i += step)
        c += (int32_t)hn::CountTrue(d, hn::Eq(hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(v.b) + i), val));
    return c;
}
template <class T, int N>
static inline bool vec_all(const Dn2CppVec<N>& v, T value) {
    const hn::CappedTag<lane_t<T>, (size_t)(N / (int)sizeof(T))> d;
    const size_t total = (size_t)N / sizeof(T), step = hn::Lanes(d);
    const auto val = hn::Set(d, (lane_t<T>)value);
    for (size_t i = 0; i < total; i += step)
        if (!hn::AllTrue(d, hn::Eq(hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(v.b) + i), val))) return false;
    return true;
}
template <class T, int N>
static inline bool vec_any(const Dn2CppVec<N>& v, T value) {
    const hn::CappedTag<lane_t<T>, (size_t)(N / (int)sizeof(T))> d;
    const size_t total = (size_t)N / sizeof(T), step = hn::Lanes(d);
    const auto val = hn::Set(d, (lane_t<T>)value);
    for (size_t i = 0; i < total; i += step)
        if (!hn::AllFalse(d, hn::Eq(hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(v.b) + i), val))) return true;
    return false;
}

// --- most-significant-bit extraction (sign bit per lane -> bitfield) ---
// MaskFromVec is defined only for lanes that are all ones or all zeros, and
// BitsFromMask relies on that (the wasm target narrows the lanes with a
// saturating pack, which moves the top bit of an arbitrary value), so the sign
// bit is broadcast over the lane first: every target then sees a real mask.
template <class T, int N>
static inline uint64_t vec_extract_msb(const Dn2CppVec<N>& v) {
    const hn::CappedTag<lane_t<T>, (size_t)(N / (int)sizeof(T))> d;
    const hn::RebindToSigned<decltype(d)> di;
    const size_t total = (size_t)N / sizeof(T), step = hn::Lanes(d);
    uint64_t mask = 0;
    for (size_t i = 0; i < total; i += step) {
        auto vv = hn::LoadU(d, reinterpret_cast<const lane_t<T>*>(v.b) + i);
        auto sign = hn::BroadcastSignBit(hn::BitCast(di, vv));
        mask |= (uint64_t)hn::BitsFromMask(di, hn::MaskFromVec(sign)) << i;
    }
    return mask;
}

// --- element-type conversion, same lane count (e.g. ConvertToInt32) ---
template <class TFrom, class TTo, int N>
static inline Dn2CppVec<N> vec_convert(const Dn2CppVec<N>& v) {
    Dn2CppVec<N> r;
    const hn::CappedTag<lane_t<TFrom>, (size_t)(N / (int)sizeof(TFrom))> dfrom;
    const hn::Rebind<lane_t<TTo>, decltype(dfrom)> dto;
    const size_t total = (size_t)N / sizeof(TFrom), step = hn::Lanes(dfrom);
    for (size_t i = 0; i < total; i += step) {
        hn::StoreU(hn::ConvertTo(dto, hn::LoadU(dfrom, reinterpret_cast<const lane_t<TFrom>*>(v.b) + i)),
                   dto, reinterpret_cast<lane_t<TTo>*>(r.b) + i);
    }
    return r;
}

// --- widening promotion: low / high half of TSrc lanes -> double-width TDst lanes ---
// PromoteLowerTo/PromoteUpperTo need the source vector to hold 2*Lanes(ddst)
// lanes in one register; that holds only when the whole Dn2CppVec<N> fits a
// native register (the source tag is not lane-capped). For a register-capped
// shape (e.g. a 256-bit vector on a 128-bit ISA) fall back to the scalar lane
// loop -- calling the scalar helper would recurse back through its prologue.
template <class TSrc, class TDst, int N>
static inline Dn2CppVec<N> vec_widen_lower(const Dn2CppVec<N>& v) {
    if constexpr (hn::Lanes(hn::CappedTag<lane_t<TSrc>, (size_t)(N / (int)sizeof(TSrc))>()) ==
                  (size_t)(N / (int)sizeof(TSrc))) {
        Dn2CppVec<N> r;
        const hn::CappedTag<lane_t<TSrc>, (size_t)(N / (int)sizeof(TSrc))> dsrc;
        const hn::CappedTag<lane_t<TDst>, (size_t)(N / (int)sizeof(TDst))> ddst;
        const auto vsrc = hn::LoadU(dsrc, reinterpret_cast<const lane_t<TSrc>*>(v.b));
        hn::StoreU(hn::PromoteLowerTo(ddst, vsrc), ddst, reinterpret_cast<lane_t<TDst>*>(r.b));
        return r;
    } else {
        Dn2CppVec<N> r;
        std::memset(r.b, 0, sizeof(r.b));
        const int dstLanes = N / (int)sizeof(TDst);
        const TSrc* src = reinterpret_cast<const TSrc*>(v.b);
        TDst* dst = reinterpret_cast<TDst*>(r.b);
        for (int i = 0; i < dstLanes; ++i) dst[i] = (TDst)src[i];
        return r;
    }
}
template <class TSrc, class TDst, int N>
static inline Dn2CppVec<N> vec_widen_upper(const Dn2CppVec<N>& v) {
    if constexpr (hn::Lanes(hn::CappedTag<lane_t<TSrc>, (size_t)(N / (int)sizeof(TSrc))>()) ==
                  (size_t)(N / (int)sizeof(TSrc))) {
        Dn2CppVec<N> r;
        const hn::CappedTag<lane_t<TSrc>, (size_t)(N / (int)sizeof(TSrc))> dsrc;
        const hn::CappedTag<lane_t<TDst>, (size_t)(N / (int)sizeof(TDst))> ddst;
        const auto vsrc = hn::LoadU(dsrc, reinterpret_cast<const lane_t<TSrc>*>(v.b));
        hn::StoreU(hn::PromoteUpperTo(ddst, vsrc), ddst, reinterpret_cast<lane_t<TDst>*>(r.b));
        return r;
    } else {
        Dn2CppVec<N> r;
        std::memset(r.b, 0, sizeof(r.b));
        const int dstLanes = N / (int)sizeof(TDst);
        const TSrc* src = reinterpret_cast<const TSrc*>(v.b);
        TDst* dst = reinterpret_cast<TDst*>(r.b);
        for (int i = 0; i < dstLanes; ++i) dst[i] = (TDst)src[dstLanes + i];
        return r;
    }
}

// --- division (float: IEEE Div; integer: scalar lane loop, Highway has no int divide) ---
template <class T, int N>
static inline Dn2CppVec<N> vec_divide(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    if constexpr (std::is_floating_point<T>::value) {
        return map2<T, N>(a, b, [](auto d, auto x, auto y) { (void)d; return hn::Div(x, y); });
    } else {
        Dn2CppVec<N> r;
        const int lanes = N / (int)sizeof(T);
        const T* ra = reinterpret_cast<const T*>(a.b);
        const T* rb = reinterpret_cast<const T*>(b.b);
        T* rr = reinterpret_cast<T*>(r.b);
        for (int i = 0; i < lanes; ++i) rr[i] = (T)(ra[i] / rb[i]);
        return r;
    }
}

// --- saturating add/sub. float = plain add/sub (no clamp); 8/16-bit int = native
//     SaturatedAdd/Sub; 32/64-bit int = scalar (Highway has no SaturatedAdd there).
//     The scalar path mirrors dn2cpp_sat_add_int / _sub_int bit-for-bit. ---
template <class T, int N>
static inline Dn2CppVec<N> vec_add_saturate(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    if constexpr (std::is_floating_point<T>::value) {
        return map2<T, N>(a, b, [](auto d, auto x, auto y) { (void)d; return hn::Add(x, y); });
    } else if constexpr (sizeof(T) <= 2) {
        return map2<T, N>(a, b, [](auto d, auto x, auto y) { (void)d; return hn::SaturatedAdd(x, y); });
    } else {
        Dn2CppVec<N> r;
        const int lanes = N / (int)sizeof(T);
        constexpr T mx = std::numeric_limits<T>::max();
        constexpr T mn = std::numeric_limits<T>::min();
        const T* ra = reinterpret_cast<const T*>(a.b);
        const T* rb = reinterpret_cast<const T*>(b.b);
        T* rr = reinterpret_cast<T*>(r.b);
        for (int i = 0; i < lanes; ++i) {
            T av = ra[i], bv = rb[i];
            if constexpr (std::is_signed<T>::value) {
                if (bv >= 0) { if (av > (T)(mx - bv)) { rr[i] = mx; continue; } }
                else         { if (av < (T)(mn - bv)) { rr[i] = mn; continue; } }
                using U = std::make_unsigned_t<T>;
                rr[i] = (T)((U)av + (U)bv);
            } else {
                T s = (T)(av + bv);
                rr[i] = s < av ? mx : s;
            }
        }
        return r;
    }
}
template <class T, int N>
static inline Dn2CppVec<N> vec_subtract_saturate(const Dn2CppVec<N>& a, const Dn2CppVec<N>& b) {
    if constexpr (std::is_floating_point<T>::value) {
        return map2<T, N>(a, b, [](auto d, auto x, auto y) { (void)d; return hn::Sub(x, y); });
    } else if constexpr (sizeof(T) <= 2) {
        return map2<T, N>(a, b, [](auto d, auto x, auto y) { (void)d; return hn::SaturatedSub(x, y); });
    } else {
        Dn2CppVec<N> r;
        const int lanes = N / (int)sizeof(T);
        constexpr T mx = std::numeric_limits<T>::max();
        constexpr T mn = std::numeric_limits<T>::min();
        const T* ra = reinterpret_cast<const T*>(a.b);
        const T* rb = reinterpret_cast<const T*>(b.b);
        T* rr = reinterpret_cast<T*>(r.b);
        for (int i = 0; i < lanes; ++i) {
            T av = ra[i], bv = rb[i];
            if constexpr (std::is_signed<T>::value) {
                if (bv >= 0) { if (av < (T)(mn + bv)) { rr[i] = mn; continue; } }
                else         { if (av > (T)(mx + bv)) { rr[i] = mx; continue; } }
                using U = std::make_unsigned_t<T>;
                rr[i] = (T)((U)av - (U)bv);
            } else {
                rr[i] = av < bv ? (T)0 : (T)(av - bv);
            }
        }
        return r;
    }
}

// --- remaining category predicates -> all-ones-lane mask ---
template <class T, int N>
static inline Dn2CppVec<N> vec_is_positive(const Dn2CppVec<N>& a) {
    return map1<T, N>(a, [](auto d, auto x) {
        if constexpr (std::is_floating_point<T>::value) return hn::VecFromMask(d, hn::Not(hn::IsNegative(x)));   // sign bit clear (complement of IsNegative)
        else if constexpr (std::is_signed<T>::value) return hn::VecFromMask(d, hn::Ge(x, hn::Zero(d)));
        else return hn::VecFromMask(d, hn::Eq(x, x));   // every unsigned lane is positive (all-ones)
    });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_is_positive_infinity(const Dn2CppVec<N>& a) {
    return map1<T, N>(a, [](auto d, auto x) {
        if constexpr (std::is_floating_point<T>::value) return hn::VecFromMask(d, hn::Eq(x, hn::Set(d, std::numeric_limits<T>::infinity())));
        else { (void)x; return hn::Zero(d); }
    });
}
template <class T, int N>
static inline Dn2CppVec<N> vec_is_negative_infinity(const Dn2CppVec<N>& a) {
    return map1<T, N>(a, [](auto d, auto x) {
        if constexpr (std::is_floating_point<T>::value) return hn::VecFromMask(d, hn::Eq(x, hn::Set(d, -std::numeric_limits<T>::infinity())));
        else { (void)x; return hn::Zero(d); }
    });
}

}  // namespace dn2cpp_hwy
