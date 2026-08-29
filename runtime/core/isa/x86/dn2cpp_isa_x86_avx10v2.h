#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx10v2: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_converttobytewithsaturationandzeroextendtoint32_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.ConvertToByteWithSaturationAndZeroExtendToInt32");
    return dn2cpp_isa_vec<16>(_mm_ipcvts_ps_epu8(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_converttobytewithsaturationandzeroextendtoint32_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.ConvertToByteWithSaturationAndZeroExtendToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v2_converttobytewithsaturationandzeroextendtoint32_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.ConvertToByteWithSaturationAndZeroExtendToInt32");
    return dn2cpp_isa_vec<32>(_mm256_ipcvts_ps_epu8(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v2_converttobytewithsaturationandzeroextendtoint32_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.ConvertToByteWithSaturationAndZeroExtendToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_converttobytewithtruncatedsaturationandzeroextendtoint32_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.ConvertToByteWithTruncatedSaturationAndZeroExtendToInt32");
    return dn2cpp_isa_vec<16>(_mm_ipcvtts_ps_epu8(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_converttobytewithtruncatedsaturationandzeroextendtoint32_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.ConvertToByteWithTruncatedSaturationAndZeroExtendToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v2_converttobytewithtruncatedsaturationandzeroextendtoint32_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.ConvertToByteWithTruncatedSaturationAndZeroExtendToInt32");
    return dn2cpp_isa_vec<32>(_mm256_ipcvtts_ps_epu8(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v2_converttobytewithtruncatedsaturationandzeroextendtoint32_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.ConvertToByteWithTruncatedSaturationAndZeroExtendToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_converttosbytewithsaturationandzeroextendtoint32_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.ConvertToSByteWithSaturationAndZeroExtendToInt32");
    return dn2cpp_isa_vec<16>(_mm_ipcvts_ps_epi8(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_converttosbytewithsaturationandzeroextendtoint32_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.ConvertToSByteWithSaturationAndZeroExtendToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v2_converttosbytewithsaturationandzeroextendtoint32_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.ConvertToSByteWithSaturationAndZeroExtendToInt32");
    return dn2cpp_isa_vec<32>(_mm256_ipcvts_ps_epi8(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v2_converttosbytewithsaturationandzeroextendtoint32_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.ConvertToSByteWithSaturationAndZeroExtendToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_converttosbytewithtruncatedsaturationandzeroextendtoint32_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.ConvertToSByteWithTruncatedSaturationAndZeroExtendToInt32");
    return dn2cpp_isa_vec<16>(_mm_ipcvtts_ps_epi8(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_converttosbytewithtruncatedsaturationandzeroextendtoint32_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.ConvertToSByteWithTruncatedSaturationAndZeroExtendToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v2_converttosbytewithtruncatedsaturationandzeroextendtoint32_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.ConvertToSByteWithTruncatedSaturationAndZeroExtendToInt32");
    return dn2cpp_isa_vec<32>(_mm256_ipcvtts_ps_epi8(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v2_converttosbytewithtruncatedsaturationandzeroextendtoint32_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.ConvertToSByteWithTruncatedSaturationAndZeroExtendToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_minmax_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.MinMax");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<16>(_mm_minmax_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_minmax_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.MinMax");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_minmax_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.MinMax");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<16>(_mm_minmax_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_minmax_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.MinMax");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v2_minmax_v256f32_v256f32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.MinMax");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<32>(_mm256_minmax_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v2_minmax_v256f32_v256f32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.MinMax");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v2_minmax_v256f64_v256f64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.MinMax");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<32>(_mm256_minmax_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v2_minmax_v256f64_v256f64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.MinMax");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_minmaxscalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.MinMaxScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<16>(_mm_minmax_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_minmaxscalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.MinMaxScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_minmaxscalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.MinMaxScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<16>(_mm_minmax_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_minmaxscalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.MinMaxScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_movescalar_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.MoveScalar");
    return dn2cpp_isa_vec<16>(_mm_move_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_movescalar_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.MoveScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_movescalar_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.MoveScalar");
    return dn2cpp_isa_vec<16>(_mm_move_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_movescalar_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.MoveScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_movescalar_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.MoveScalar");
    return dn2cpp_isa_vec<16>(_mm_move_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_movescalar_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.MoveScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_movescalar_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.MoveScalar");
    return dn2cpp_isa_vec<16>(_mm_move_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v2_movescalar_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.MoveScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v2_storescalar_pi16_v128i16(int16_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.StoreScalar");
    _mm_storeu_si16(a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v2_storescalar_pi16_v128i16(int16_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.StoreScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v2_storescalar_pu16_v128u16(uint16_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2, "System.Runtime.Intrinsics.X86.Avx10v2.StoreScalar");
    _mm_storeu_si16(a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v2_storescalar_pu16_v128u16(uint16_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2.StoreScalar");
}
#endif
