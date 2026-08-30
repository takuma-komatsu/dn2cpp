#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Sse3: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_addsubtract_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse3, "System.Runtime.Intrinsics.X86.Sse3.AddSubtract");
    return dn2cpp_isa_vec<16>(_mm_addsub_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_addsubtract_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse3.AddSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_addsubtract_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse3, "System.Runtime.Intrinsics.X86.Sse3.AddSubtract");
    return dn2cpp_isa_vec<16>(_mm_addsub_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_addsubtract_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse3.AddSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_horizontaladd_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse3, "System.Runtime.Intrinsics.X86.Sse3.HorizontalAdd");
    return dn2cpp_isa_vec<16>(_mm_hadd_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_horizontaladd_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse3.HorizontalAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_horizontaladd_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse3, "System.Runtime.Intrinsics.X86.Sse3.HorizontalAdd");
    return dn2cpp_isa_vec<16>(_mm_hadd_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_horizontaladd_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse3.HorizontalAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_horizontalsubtract_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse3, "System.Runtime.Intrinsics.X86.Sse3.HorizontalSubtract");
    return dn2cpp_isa_vec<16>(_mm_hsub_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_horizontalsubtract_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse3.HorizontalSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_horizontalsubtract_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse3, "System.Runtime.Intrinsics.X86.Sse3.HorizontalSubtract");
    return dn2cpp_isa_vec<16>(_mm_hsub_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_horizontalsubtract_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse3.HorizontalSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_loadandduplicatetovector128_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse3, "System.Runtime.Intrinsics.X86.Sse3.LoadAndDuplicateToVector128");
    return dn2cpp_isa_vec<16>(_mm_loaddup_pd(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_loadandduplicatetovector128_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse3.LoadAndDuplicateToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_loaddquvector128_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse3, "System.Runtime.Intrinsics.X86.Sse3.LoadDquVector128");
    return dn2cpp_isa_vec<16>(_mm_lddqu_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_loaddquvector128_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse3.LoadDquVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_loaddquvector128_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse3, "System.Runtime.Intrinsics.X86.Sse3.LoadDquVector128");
    return dn2cpp_isa_vec<16>(_mm_lddqu_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_loaddquvector128_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse3.LoadDquVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_loaddquvector128_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse3, "System.Runtime.Intrinsics.X86.Sse3.LoadDquVector128");
    return dn2cpp_isa_vec<16>(_mm_lddqu_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_loaddquvector128_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse3.LoadDquVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_loaddquvector128_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse3, "System.Runtime.Intrinsics.X86.Sse3.LoadDquVector128");
    return dn2cpp_isa_vec<16>(_mm_lddqu_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_loaddquvector128_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse3.LoadDquVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_loaddquvector128_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse3, "System.Runtime.Intrinsics.X86.Sse3.LoadDquVector128");
    return dn2cpp_isa_vec<16>(_mm_lddqu_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_loaddquvector128_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse3.LoadDquVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_loaddquvector128_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse3, "System.Runtime.Intrinsics.X86.Sse3.LoadDquVector128");
    return dn2cpp_isa_vec<16>(_mm_lddqu_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_loaddquvector128_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse3.LoadDquVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_loaddquvector128_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse3, "System.Runtime.Intrinsics.X86.Sse3.LoadDquVector128");
    return dn2cpp_isa_vec<16>(_mm_lddqu_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_loaddquvector128_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse3.LoadDquVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_loaddquvector128_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse3, "System.Runtime.Intrinsics.X86.Sse3.LoadDquVector128");
    return dn2cpp_isa_vec<16>(_mm_lddqu_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_loaddquvector128_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse3.LoadDquVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_moveandduplicate_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse3, "System.Runtime.Intrinsics.X86.Sse3.MoveAndDuplicate");
    return dn2cpp_isa_vec<16>(_mm_movedup_pd(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_moveandduplicate_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse3.MoveAndDuplicate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_movehighandduplicate_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse3, "System.Runtime.Intrinsics.X86.Sse3.MoveHighAndDuplicate");
    return dn2cpp_isa_vec<16>(_mm_movehdup_ps(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_movehighandduplicate_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse3.MoveHighAndDuplicate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_movelowandduplicate_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse3, "System.Runtime.Intrinsics.X86.Sse3.MoveLowAndDuplicate");
    return dn2cpp_isa_vec<16>(_mm_moveldup_ps(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse3_movelowandduplicate_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse3.MoveLowAndDuplicate");
}
#endif
