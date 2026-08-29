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
#if defined(_MSC_VER) && !defined(__clang__)
#include <intrin.h>
#else
#include <cpuid.h>
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

// wasm SIMD has no unsigned 64-bit ordering compare (i64x2.lt_u is what .NET
// documents, i64x2.lt_s is what the instruction set has); flipping the sign bit
// of both operands makes the signed compare answer the unsigned question.
static inline v128_t dn2cpp_isa_wasm_flip_sign64(v128_t v)
{
    return wasm_v128_xor(v, wasm_i64x2_splat(INT64_MIN));
}
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

// Every lowered helper starts by testing its family's IsSupported token: a call
// made while the token is false (the CPU lacks the ISA, or DN2CPP_CPU_FEATURES
// masked it) throws PlatformNotSupportedException in .NET, and would execute an
// instruction the CPU may not have here.
static inline void dn2cpp_isa_require(int32_t supported, const char* what)
{
    if (!supported)
        dn2cpp_throw_platform_not_supported(what);
}

// Immediate-operand dispatch. Hardware encodes the immediate in the instruction,
// so a runtime value has to select among one instantiation per value; EXPR
// names the immediate as DN2CPP_IMM (and a second one as DN2CPP_IMM2), an
// integral constant in every case body. Out-of-range values throw
// ArgumentOutOfRangeException, the check .NET inserts for a non-constant
// immediate. The case-list macros take the case macro as their first argument;
// the two-immediate form nests a second, independently named case list so the
// outer expansion never meets its own name inside an argument.
#define DN2CPP_ISA_IMM_CASE(v, EXPR) \
    case (v): { enum : int { DN2CPP_IMM = (v) }; return (EXPR); }
#define DN2CPP_ISA_IMM_CASE_OUTER(v, STMT) \
    case (v): { enum : int { DN2CPP_IMM = (v) }; STMT }
#define DN2CPP_ISA_IMM_CASE2(v, EXPR) \
    case (v): { enum : int { DN2CPP_IMM2 = (v) }; return (EXPR); }

#define DN2CPP_ISA_IMM_CASES_1(C, b, EXPR)   C((b) + 0, EXPR)
#define DN2CPP_ISA_IMM_CASES_2(C, b, EXPR)   DN2CPP_ISA_IMM_CASES_1(C, b, EXPR)   DN2CPP_ISA_IMM_CASES_1(C, (b) + 1, EXPR)
#define DN2CPP_ISA_IMM_CASES_4(C, b, EXPR)   DN2CPP_ISA_IMM_CASES_2(C, b, EXPR)   DN2CPP_ISA_IMM_CASES_2(C, (b) + 2, EXPR)
#define DN2CPP_ISA_IMM_CASES_8(C, b, EXPR)   DN2CPP_ISA_IMM_CASES_4(C, b, EXPR)   DN2CPP_ISA_IMM_CASES_4(C, (b) + 4, EXPR)
#define DN2CPP_ISA_IMM_CASES_16(C, b, EXPR)  DN2CPP_ISA_IMM_CASES_8(C, b, EXPR)   DN2CPP_ISA_IMM_CASES_8(C, (b) + 8, EXPR)
#define DN2CPP_ISA_IMM_CASES_32(C, b, EXPR)  DN2CPP_ISA_IMM_CASES_16(C, b, EXPR)  DN2CPP_ISA_IMM_CASES_16(C, (b) + 16, EXPR)
#define DN2CPP_ISA_IMM_CASES_64(C, b, EXPR)  DN2CPP_ISA_IMM_CASES_32(C, b, EXPR)  DN2CPP_ISA_IMM_CASES_32(C, (b) + 32, EXPR)
#define DN2CPP_ISA_IMM_CASES_128(C, b, EXPR) DN2CPP_ISA_IMM_CASES_64(C, b, EXPR)  DN2CPP_ISA_IMM_CASES_64(C, (b) + 64, EXPR)
#define DN2CPP_ISA_IMM_CASES_256(C, b, EXPR) DN2CPP_ISA_IMM_CASES_128(C, b, EXPR) DN2CPP_ISA_IMM_CASES_128(C, (b) + 128, EXPR)

#define DN2CPP_ISA_IMM2_CASES_1(C, b, EXPR)   C((b) + 0, EXPR)
#define DN2CPP_ISA_IMM2_CASES_2(C, b, EXPR)   DN2CPP_ISA_IMM2_CASES_1(C, b, EXPR)  DN2CPP_ISA_IMM2_CASES_1(C, (b) + 1, EXPR)
#define DN2CPP_ISA_IMM2_CASES_4(C, b, EXPR)   DN2CPP_ISA_IMM2_CASES_2(C, b, EXPR)  DN2CPP_ISA_IMM2_CASES_2(C, (b) + 2, EXPR)
#define DN2CPP_ISA_IMM2_CASES_8(C, b, EXPR)   DN2CPP_ISA_IMM2_CASES_4(C, b, EXPR)  DN2CPP_ISA_IMM2_CASES_4(C, (b) + 4, EXPR)
#define DN2CPP_ISA_IMM2_CASES_16(C, b, EXPR)  DN2CPP_ISA_IMM2_CASES_8(C, b, EXPR)  DN2CPP_ISA_IMM2_CASES_8(C, (b) + 8, EXPR)
#define DN2CPP_ISA_IMM2_CASES_32(C, b, EXPR)  DN2CPP_ISA_IMM2_CASES_16(C, b, EXPR) DN2CPP_ISA_IMM2_CASES_16(C, (b) + 16, EXPR)
#define DN2CPP_ISA_IMM2_CASES_64(C, b, EXPR)  DN2CPP_ISA_IMM2_CASES_32(C, b, EXPR) DN2CPP_ISA_IMM2_CASES_32(C, (b) + 32, EXPR)

// Every value of a byte immediate is valid; the default arm only satisfies
// return-path analysis.
#define DN2CPP_ISA_IMM8_SWITCH(imm, EXPR) \
    switch ((int)((imm) & 0xFF)) { \
        DN2CPP_ISA_IMM_CASES_256(DN2CPP_ISA_IMM_CASE, 0, EXPR) \
        default: dn2cpp_throw_argument_out_of_range(); \
    }

// The immediate is valid in [lo, lo + count), count a power of two up to 256:
// a lane index (0..lanes-1), a left shift (0..bits-1) or a right shift
// (1..bits). The subtraction is unsigned so a negative value is out of range.
#define DN2CPP_ISA_IMM_RANGE_SWITCH(lo, count, imm, EXPR) \
    { \
        if ((uint32_t)(imm) - (uint32_t)(lo) >= (uint32_t)(count)) \
            dn2cpp_throw_argument_out_of_range(); \
        switch ((int)(imm)) { \
            DN2CPP_ISA_IMM_CASES_##count(DN2CPP_ISA_IMM_CASE, lo, EXPR) \
            default: dn2cpp_throw_argument_out_of_range(); \
        } \
    }

// Two immediates (INS Vd.B[lane1], Vn.B[lane2]): the outer switch fixes
// DN2CPP_IMM, the inner one DN2CPP_IMM2; count1 * count2 case bodies.
#define DN2CPP_ISA_IMM2_INNER_SWITCH(lo, count, imm, EXPR) \
    switch ((int)(imm)) { \
        DN2CPP_ISA_IMM2_CASES_##count(DN2CPP_ISA_IMM_CASE2, lo, EXPR) \
        default: dn2cpp_throw_argument_out_of_range(); \
    }
#define DN2CPP_ISA_IMM_RANGE_SWITCH2(lo1, count1, imm1, lo2, count2, imm2, EXPR) \
    { \
        if ((uint32_t)(imm1) - (uint32_t)(lo1) >= (uint32_t)(count1) \
            || (uint32_t)(imm2) - (uint32_t)(lo2) >= (uint32_t)(count2)) \
            dn2cpp_throw_argument_out_of_range(); \
        switch ((int)(imm1)) { \
            DN2CPP_ISA_IMM_CASES_##count1(DN2CPP_ISA_IMM_CASE_OUTER, lo1, DN2CPP_ISA_IMM2_INNER_SWITCH(lo2, count2, imm2, EXPR)) \
            default: dn2cpp_throw_argument_out_of_range(); \
        } \
    }

// ---------------------------------------------------------------------------
// Multi-register and one-lane shapes of the NEON families.
// ---------------------------------------------------------------------------

// A scalar result written to one lane of Vd (ADDV Bd, FMAXV Sd, SHA1H Sd,
// SQRDMLAH Hd): .NET returns it as lane 0 of a Vector64 whose other lanes are
// zero, the register's contents after the write.
template <int N, class S>
static inline Dn2CppVec<N> dn2cpp_isa_lane0(S s)
{
    static_assert(sizeof(S) <= (size_t)N, "scalar wider than the vector");
    Dn2CppVec<N> r{};
    std::memcpy(r.b, &s, sizeof(s));
    return r;
}

// A multi-register result (LD1 {Vn, Vn+1}, ZIP pairs) scattered into the
// out-pointers of a ValueTuple return, in .val order.
template <class X, int N>
static inline void dn2cpp_isa_scatter(const X& x, Dn2CppVec<N>* item1, Dn2CppVec<N>* item2)
{
    *item1 = dn2cpp_isa_vec<N>(x.val[0]);
    *item2 = dn2cpp_isa_vec<N>(x.val[1]);
}

template <class X, int N>
static inline void dn2cpp_isa_scatter(const X& x, Dn2CppVec<N>* item1, Dn2CppVec<N>* item2, Dn2CppVec<N>* item3)
{
    dn2cpp_isa_scatter(x, item1, item2);
    *item3 = dn2cpp_isa_vec<N>(x.val[2]);
}

template <class X, int N>
static inline void dn2cpp_isa_scatter(const X& x, Dn2CppVec<N>* item1, Dn2CppVec<N>* item2, Dn2CppVec<N>* item3, Dn2CppVec<N>* item4)
{
    dn2cpp_isa_scatter(x, item1, item2, item3);
    *item4 = dn2cpp_isa_vec<N>(x.val[3]);
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

// ---------------------------------------------------------------------------
// X86Base.DivRem: (upper:lower) / divisor into quotient and remainder. The
// instruction faults (#DE) on a zero divisor and on a quotient wider than the
// operand; .NET surfaces both as DivideByZeroException, so both are rejected
// before any division.
// ---------------------------------------------------------------------------

static inline void dn2cpp_isa_divrem_u32(uint32_t lo, uint32_t hi, uint32_t divisor, uint32_t* q, uint32_t* r)
{
    if (divisor == 0 || hi >= divisor)
        dn2cpp_throw_divide_by_zero();
    uint64_t n = ((uint64_t)hi << 32) | lo;
    *q = (uint32_t)(n / divisor);
    *r = (uint32_t)(n % divisor);
}

static inline void dn2cpp_isa_divrem_i32(uint32_t lo, int32_t hi, int32_t divisor, int32_t* q, int32_t* r)
{
    if (divisor == 0)
        dn2cpp_throw_divide_by_zero();
    int64_t n = (int64_t)(((uint64_t)(uint32_t)hi << 32) | lo);
    // INT64_MIN / -1 is the one 64-bit quotient C++ cannot form; it does not
    // fit 32 bits either.
    if (divisor == -1 && n == INT64_MIN)
        dn2cpp_throw_divide_by_zero();
    int64_t quotient = n / divisor;
    if (quotient < INT32_MIN || quotient > INT32_MAX)
        dn2cpp_throw_divide_by_zero();
    *q = (int32_t)quotient;
    *r = (int32_t)(n % divisor);
}

static inline void dn2cpp_isa_divrem_u64(uint64_t lo, uint64_t hi, uint64_t divisor, uint64_t* q, uint64_t* r)
{
    *q = dn2cpp_isa_udivrem128(lo, hi, divisor, r);
}

static inline void dn2cpp_isa_divrem_i64(uint64_t lo, int64_t hi, int64_t divisor, int64_t* q, int64_t* r)
{
    *q = dn2cpp_isa_sdivrem128((int64_t)lo, hi, divisor, r);
}

#if DN2CPP_TARGET_X64
// The native-integer overloads are the 64-bit ones: every x86 target is x86-64.
static_assert(sizeof(intptr_t) == 8, "X86Base.DivRem on native integers is the 128/64 division");

static inline void dn2cpp_isa_divrem_nuint(uintptr_t lo, uintptr_t hi, uintptr_t divisor, uintptr_t* q, uintptr_t* r)
{
    uint64_t rem;
    *q = (uintptr_t)dn2cpp_isa_udivrem128(lo, hi, divisor, &rem);
    *r = (uintptr_t)rem;
}

static inline void dn2cpp_isa_divrem_nint(uintptr_t lo, intptr_t hi, intptr_t divisor, intptr_t* q, intptr_t* r)
{
    int64_t rem;
    *q = (intptr_t)dn2cpp_isa_sdivrem128((int64_t)lo, hi, divisor, &rem);
    *r = (intptr_t)rem;
}

// X86Base.CpuId: CPUID with EAX = functionId and ECX = subFunctionId.
static inline void dn2cpp_isa_cpuid(int32_t fn, int32_t sub, int32_t* eax, int32_t* ebx, int32_t* ecx, int32_t* edx)
{
#if defined(_MSC_VER) && !defined(__clang__)
    int info[4];
    __cpuidex(info, fn, sub);
    *eax = info[0];
    *ebx = info[1];
    *ecx = info[2];
    *edx = info[3];
#else
    unsigned int a, b, c, d;
    __cpuid_count((unsigned int)fn, (unsigned int)sub, a, b, c, d);
    *eax = (int32_t)a;
    *ebx = (int32_t)b;
    *ecx = (int32_t)c;
    *edx = (int32_t)d;
#endif
}

// Bmi2.MultiplyNoFlags with the low-half pointer: the high half is returned.
// MULX is a flagless multiply, so no ISA above the baseline is involved.
static inline uint32_t dn2cpp_isa_mulx32(uint32_t a, uint32_t b, uint32_t* low)
{
    uint64_t p = (uint64_t)a * b;
    *low = (uint32_t)p;
    return (uint32_t)(p >> 32);
}

static inline uint64_t dn2cpp_isa_mulx64(uint64_t a, uint64_t b, uint64_t* low)
{
    *low = a * b;
    return dn2cpp_isa_umulh64(a, b);
}
#endif // DN2CPP_TARGET_X64

// ---------------------------------------------------------------------------
// ArmBase scalar operations. CLZ and CLS are defined at zero (the operand
// width, and one less), which the C builtins are not, so zero is handled here.
// ---------------------------------------------------------------------------

#if DN2CPP_TARGET_ARM64
static inline int32_t dn2cpp_isa_clz32(uint32_t x)
{
#if defined(_MSC_VER) && !defined(__clang__)
    return (int32_t)_CountLeadingZeros(x);
#else
    return x == 0 ? 32 : __builtin_clz(x);
#endif
}

static inline int32_t dn2cpp_isa_clz64(uint64_t x)
{
#if defined(_MSC_VER) && !defined(__clang__)
    return (int32_t)_CountLeadingZeros64(x);
#else
    return x == 0 ? 64 : __builtin_clzll(x);
#endif
}

// CLS: the leading bits equal to the sign bit, the sign bit itself excluded.
static inline int32_t dn2cpp_isa_cls32(int32_t x)
{
    return dn2cpp_isa_clz32((uint32_t)(x ^ (x >> 31))) - 1;
}

static inline int32_t dn2cpp_isa_cls64(int64_t x)
{
    return dn2cpp_isa_clz64((uint64_t)(x ^ (x >> 63))) - 1;
}

// RBIT. clang folds the builtin to the instruction; the other compilers take
// the portable reversal, which is the same function.
static inline uint32_t dn2cpp_isa_rbit32(uint32_t x)
{
#if defined(__clang__)
    return __builtin_bitreverse32(x);
#else
    return dn2cpp_isa_bitreverse32(x);
#endif
}

static inline uint64_t dn2cpp_isa_rbit64(uint64_t x)
{
#if defined(__clang__)
    return __builtin_bitreverse64(x);
#else
    return dn2cpp_isa_bitreverse64(x);
#endif
}

static inline void dn2cpp_isa_arm_yield()
{
#if defined(_MSC_VER) && !defined(__clang__)
    __yield();
#else
    __asm__ __volatile__("yield");
#endif
}
#endif // DN2CPP_TARGET_ARM64
