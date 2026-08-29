#pragma once
// dn2cpp_isa_common.h — support shared by the generated per-family ISA helper
// headers: bit-copies between Dn2CppVec<N> and the compiler's vector types, the
// target attribute that lets a helper use an ISA the TU is not compiled for,
// immediate-operand dispatch, and the 128-bit scalar arithmetic the X86Base /
// ArmBase families need. Reached only through dn2cpp_vectors.h, so the runtime's
// own TUs never pay for the intrinsic headers.

#include "dn2cpp_core.h"
#include "dn2cpp_cpu_features.h"

#include <cstddef>
#include <cstdint>
#include <cstring>

#if DN2CPP_TARGET_X64
#include <immintrin.h>
#if defined(_MSC_VER)
#include <intrin.h>
#endif
#elif DN2CPP_TARGET_ARM64
#include <arm_neon.h>
#if defined(_MSC_VER)
#include <intrin.h>
#endif
#if defined(__has_include)
#if __has_include(<arm_acle.h>)
#include <arm_acle.h>
#endif
#endif
#elif DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
#include <wasm_simd128.h>
#endif

// A helper compiled for an ISA above the TU's baseline. The attribute keeps the
// helper out of line of callers that lack the ISA, which is what makes the
// IsSupported check meaningful: the instruction executes only behind it. MSVC
// has no per-function ISA, so its intrinsics compile unconditionally.
#if defined(__GNUC__) || defined(__clang__)
#define DN2CPP_ISA_TARGET(x) __attribute__((target(x)))
#else
#define DN2CPP_ISA_TARGET(x)
#endif
#define DN2CPP_ISA_INLINE static inline

template <class To, int N>
static inline To dn2cpp_isa_bits(const Dn2CppVec<N>& v)
{
    static_assert(sizeof(To) == (size_t)N, "vector width mismatch");
    To r;
    std::memcpy(&r, v.b, sizeof(r));
    return r;
}

template <int N, class From>
static inline Dn2CppVec<N> dn2cpp_isa_vec(const From& x)
{
    static_assert(sizeof(From) == (size_t)N, "vector width mismatch");
    Dn2CppVec<N> r;
    std::memcpy(r.b, &x, sizeof(r.b));
    return r;
}

// A method whose IsSupported token is true on this target but whose body has no
// lowering yet. Throws PlatformNotSupportedException, as .NET does for a call
// on an unsupported ISA.
[[noreturn]] static inline void dn2cpp_isa_not_lowered(const char* what)
{
    dn2cpp_throw_platform_not_supported(what);
}

// Immediate-operand dispatch. Hardware encodes the immediate in the instruction,
// so a runtime value has to select among one instantiation per value; EXPR
// names the immediate as DN2CPP_IMM, an integral constant in every case body.
// Out-of-range values throw ArgumentOutOfRangeException as .NET does.
#define DN2CPP_ISA_IMM_CASE(v, EXPR) \
    case (v): { enum : int { DN2CPP_IMM = (v) }; return (EXPR); }
#define DN2CPP_ISA_IMM_CASES_2(b, EXPR)   DN2CPP_ISA_IMM_CASE((b) + 0, EXPR)  DN2CPP_ISA_IMM_CASE((b) + 1, EXPR)
#define DN2CPP_ISA_IMM_CASES_4(b, EXPR)   DN2CPP_ISA_IMM_CASES_2(b, EXPR)     DN2CPP_ISA_IMM_CASES_2((b) + 2, EXPR)
#define DN2CPP_ISA_IMM_CASES_8(b, EXPR)   DN2CPP_ISA_IMM_CASES_4(b, EXPR)     DN2CPP_ISA_IMM_CASES_4((b) + 4, EXPR)
#define DN2CPP_ISA_IMM_CASES_16(b, EXPR)  DN2CPP_ISA_IMM_CASES_8(b, EXPR)     DN2CPP_ISA_IMM_CASES_8((b) + 8, EXPR)
#define DN2CPP_ISA_IMM_CASES_32(b, EXPR)  DN2CPP_ISA_IMM_CASES_16(b, EXPR)    DN2CPP_ISA_IMM_CASES_16((b) + 16, EXPR)
#define DN2CPP_ISA_IMM_CASES_64(b, EXPR)  DN2CPP_ISA_IMM_CASES_32(b, EXPR)    DN2CPP_ISA_IMM_CASES_32((b) + 32, EXPR)
#define DN2CPP_ISA_IMM_CASES_128(b, EXPR) DN2CPP_ISA_IMM_CASES_64(b, EXPR)    DN2CPP_ISA_IMM_CASES_64((b) + 64, EXPR)
#define DN2CPP_ISA_IMM_CASES_256(b, EXPR) DN2CPP_ISA_IMM_CASES_128(b, EXPR)   DN2CPP_ISA_IMM_CASES_128((b) + 128, EXPR)

// Every value of a byte immediate is valid; the default arm only satisfies
// return-path analysis.
#define DN2CPP_ISA_IMM8_SWITCH(imm, EXPR) \
    switch ((int)((imm) & 0xFF)) { \
        DN2CPP_ISA_IMM_CASES_256(0, EXPR) \
        default: dn2cpp_throw_argument_out_of_range(); \
    }

// n in {2, 4, 8, 16, 32, 64}: the immediate is a lane or element index.
#define DN2CPP_ISA_IMM_SWITCH_N(n, imm, EXPR) \
    { \
        if ((uint32_t)(imm) >= (uint32_t)(n)) \
            dn2cpp_throw_argument_out_of_range(); \
        switch ((int)(imm)) { \
            DN2CPP_ISA_IMM_CASES_##n(0, EXPR) \
            default: dn2cpp_throw_argument_out_of_range(); \
        } \
    }

// ---------------------------------------------------------------------------
// 128-bit scalar arithmetic (X86Base.X64.DivRem, Bmi2.X64.MultiplyNoFlags,
// ArmBase.Arm64.MultiplyHigh).
// ---------------------------------------------------------------------------

static inline uint64_t dn2cpp_isa_umulh64(uint64_t a, uint64_t b)
{
#if defined(__SIZEOF_INT128__)
    return (uint64_t)(((unsigned __int128)a * b) >> 64);
#elif defined(_MSC_VER)
    return __umulh(a, b);
#else
    uint64_t a0 = (uint32_t)a, a1 = a >> 32, b0 = (uint32_t)b, b1 = b >> 32;
    uint64_t p00 = a0 * b0, p01 = a0 * b1, p10 = a1 * b0, p11 = a1 * b1;
    uint64_t mid = (p00 >> 32) + (uint32_t)p01 + (uint32_t)p10;
    return p11 + (p01 >> 32) + (p10 >> 32) + (mid >> 32);
#endif
}

static inline int64_t dn2cpp_isa_smulh64(int64_t a, int64_t b)
{
#if defined(__SIZEOF_INT128__)
    return (int64_t)(((__int128)a * b) >> 64);
#elif defined(_MSC_VER)
    return __mulh(a, b);
#else
    // Unsigned high product corrected by the sign-extension terms.
    uint64_t hi = dn2cpp_isa_umulh64((uint64_t)a, (uint64_t)b);
    if (a < 0) hi -= (uint64_t)b;
    if (b < 0) hi -= (uint64_t)a;
    return (int64_t)hi;
#endif
}

// (hi:lo) / divisor. The hardware instruction faults (#DE) on a zero divisor
// and on a quotient that does not fit 64 bits; .NET surfaces that fault as
// DivideByZeroException, so both are rejected up front and never reach the
// division.
static inline uint64_t dn2cpp_isa_udivrem128(uint64_t lo, uint64_t hi, uint64_t divisor, uint64_t* rem)
{
    if (divisor == 0 || hi >= divisor)
        dn2cpp_throw_divide_by_zero();
#if defined(__SIZEOF_INT128__)
    unsigned __int128 n = ((unsigned __int128)hi << 64) | lo;
    *rem = (uint64_t)(n % divisor);
    return (uint64_t)(n / divisor);
#elif defined(_MSC_VER) && defined(_M_X64)
    return _udiv128(hi, lo, divisor, rem);
#else
    // Restoring division, one bit per step; hi < divisor keeps every partial
    // remainder below 2*divisor, so the carry out of the shift is the only
    // overflow to track.
    uint64_t q = 0, r = hi;
    for (int i = 63; i >= 0; --i)
    {
        uint64_t carry = r >> 63;
        r = (r << 1) | ((lo >> i) & 1);
        if (carry != 0 || r >= divisor)
        {
            r -= divisor;
            q |= 1ull << i;
        }
    }
    *rem = r;
    return q;
#endif
}

static inline int64_t dn2cpp_isa_sdivrem128(int64_t lo, int64_t hi, int64_t divisor, int64_t* rem)
{
    if (divisor == 0)
        dn2cpp_throw_divide_by_zero();
    // Reduce to the unsigned case on magnitudes, then check that the signed
    // quotient fits: at most INT64_MAX, or exactly 2^63 when negative.
    bool nneg = hi < 0;
    bool dneg = divisor < 0;
    uint64_t mlo = (uint64_t)lo, mhi = (uint64_t)hi;
    if (nneg)
    {
        mlo = ~mlo + 1;
        mhi = ~mhi + (mlo == 0 ? 1 : 0);
    }
    uint64_t md = dneg ? (~(uint64_t)divisor + 1) : (uint64_t)divisor;
    if (mhi >= md)
        dn2cpp_throw_divide_by_zero();
    uint64_t ur;
    uint64_t uq = dn2cpp_isa_udivrem128(mlo, mhi, md, &ur);
    bool qneg = nneg != dneg;
    if (uq > (qneg ? (1ull << 63) : (uint64_t)INT64_MAX))
        dn2cpp_throw_divide_by_zero();
    *rem = (int64_t)(nneg ? (~ur + 1) : ur);
    return (int64_t)(qneg ? (~uq + 1) : uq);
}

// Portable bit reversal for targets without an rbit instruction.
static inline uint32_t dn2cpp_isa_bitreverse32(uint32_t x)
{
    x = ((x >> 1) & 0x55555555u) | ((x & 0x55555555u) << 1);
    x = ((x >> 2) & 0x33333333u) | ((x & 0x33333333u) << 2);
    x = ((x >> 4) & 0x0F0F0F0Fu) | ((x & 0x0F0F0F0Fu) << 4);
    x = ((x >> 8) & 0x00FF00FFu) | ((x & 0x00FF00FFu) << 8);
    return (x >> 16) | (x << 16);
}

static inline uint64_t dn2cpp_isa_bitreverse64(uint64_t x)
{
    return ((uint64_t)dn2cpp_isa_bitreverse32((uint32_t)x) << 32)
         | dn2cpp_isa_bitreverse32((uint32_t)(x >> 32));
}
