#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx10v1: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_abs_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Abs");
    return dn2cpp_isa_vec<16>(_mm_abs_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_abs_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Abs");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_abs_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Abs");
    return dn2cpp_isa_vec<32>(_mm256_abs_epi64(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_abs_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Abs");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_addscalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.AddScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_addscalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.AddScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_addscalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.AddScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_addscalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.AddScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_alignright32_v128i32_v128i32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.AlignRight32");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_alignr_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & (4 - 1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_alignright32_v128i32_v128i32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.AlignRight32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_alignright32_v128u32_v128u32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.AlignRight32");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_alignr_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & (4 - 1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_alignright32_v128u32_v128u32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.AlignRight32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_alignright32_v256i32_v256i32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.AlignRight32");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_alignr_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & (8 - 1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_alignright32_v256i32_v256i32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.AlignRight32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_alignright32_v256u32_v256u32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.AlignRight32");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_alignr_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & (8 - 1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_alignright32_v256u32_v256u32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.AlignRight32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_alignright64_v128i64_v128i64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.AlignRight64");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_alignr_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & (2 - 1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_alignright64_v128i64_v128i64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.AlignRight64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_alignright64_v128u64_v128u64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.AlignRight64");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_alignr_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & (2 - 1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_alignright64_v128u64_v128u64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.AlignRight64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_alignright64_v256i64_v256i64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.AlignRight64");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_alignr_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & (4 - 1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_alignright64_v256i64_v256i64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.AlignRight64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_alignright64_v256u64_v256u64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.AlignRight64");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_alignr_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & (4 - 1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_alignright64_v256u64_v256u64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.AlignRight64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_ps(_mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_pd(_mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128i16_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_epi16(_mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128i16_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128i32_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_epi32(_mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128i32_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128i64_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_epi64(_mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128i64_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128i8_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_epi8(_mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128i8_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128u16_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_epi16(_mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128u16_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128u32_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_epi32(_mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128u32_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128u64_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_epi64(_mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128u64_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128u8_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_epi8(_mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_blendvariable_v128u8_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256f32_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_ps(_mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256f32_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256f64_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_pd(_mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256f64_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256i16_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_epi16(_mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256i16_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256i32_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_epi32(_mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256i32_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256i64_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_epi64(_mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256i64_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256i8_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_epi8(_mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256i8_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256u16_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_epi16(_mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256u16_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256u32_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_epi32(_mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256u32_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256u64_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_epi64(_mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256u64_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256u8_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_epi8(_mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_blendvariable_v256u8_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_broadcastpairscalartovector128_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BroadcastPairScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_broadcast_i32x2(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_broadcastpairscalartovector128_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BroadcastPairScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_broadcastpairscalartovector128_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BroadcastPairScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_broadcast_i32x2(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_broadcastpairscalartovector128_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BroadcastPairScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_broadcastpairscalartovector256_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BroadcastPairScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcast_f32x2(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_broadcastpairscalartovector256_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BroadcastPairScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_broadcastpairscalartovector256_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BroadcastPairScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcast_i32x2(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_broadcastpairscalartovector256_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BroadcastPairScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_broadcastpairscalartovector256_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.BroadcastPairScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcast_i32x2(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_broadcastpairscalartovector256_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.BroadcastPairScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_classify_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Classify");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_fpclass_ps_mask(dn2cpp_isa_bits<__m128>(a0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_classify_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Classify");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_classify_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Classify");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_fpclass_pd_mask(dn2cpp_isa_bits<__m128d>(a0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_classify_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Classify");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_classify_v256f32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Classify");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_fpclass_ps_mask(dn2cpp_isa_bits<__m256>(a0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_classify_v256f32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Classify");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_classify_v256f64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Classify");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_fpclass_pd_mask(dn2cpp_isa_bits<__m256d>(a0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_classify_v256f64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Classify");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_classifyscalar_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ClassifyScalar");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_fpclass_ss_mask(dn2cpp_isa_bits<__m128>(a0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_classifyscalar_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ClassifyScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_classifyscalar_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ClassifyScalar");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_fpclass_sd_mask(dn2cpp_isa_bits<__m128d>(a0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_classifyscalar_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ClassifyScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compare_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compare");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compare_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compare");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compare_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compare");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compare_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compare");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compare_v256f32_v256f32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compare");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compare_v256f32_v256f32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compare");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compare_v256f64_v256f64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compare");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compare_v256f64_v256f64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compare");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_EQ_OQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_EQ_OQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmpeq_epi16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmpeq_epi32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmpeq_epi64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmpeq_epi8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmpeq_epu16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmpeq_epu32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmpeq_epu64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmpeq_epu8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareequal_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_EQ_OQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_EQ_OQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmpeq_epi16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmpeq_epi32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmpeq_epi64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmpeq_epi8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmpeq_epu16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmpeq_epu32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmpeq_epu64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmpeq_epu8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareequal_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_GT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_GT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmpgt_epi16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmpgt_epi32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmpgt_epi64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmpgt_epi8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmpgt_epu16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmpgt_epu32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmpgt_epu64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmpgt_epu8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_GT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_GT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmpgt_epi16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmpgt_epi32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmpgt_epi64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmpgt_epi8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmpgt_epu16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmpgt_epu32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmpgt_epu64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmpgt_epu8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthan_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_GE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_GE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmpge_epi16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmpge_epi32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmpge_epi64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmpge_epi8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmpge_epu16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmpge_epu32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmpge_epu64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmpge_epu8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_GE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_GE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmpge_epi16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmpge_epi32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmpge_epi64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmpge_epi8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmpge_epu16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmpge_epu32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmpge_epu64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmpge_epu8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparegreaterthanorequal_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_LT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_LT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmplt_epi16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmplt_epi32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmplt_epi64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmplt_epi8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmplt_epu16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmplt_epu32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmplt_epu64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmplt_epu8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthan_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_LT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_LT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmplt_epi16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmplt_epi32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmplt_epi64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmplt_epi8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmplt_epu16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmplt_epu32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmplt_epu64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmplt_epu8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthan_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_LE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_LE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmple_epi16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmple_epi32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmple_epi64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmple_epi8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmple_epu16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmple_epu32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmple_epu64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmple_epu8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_LE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_LE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmple_epi16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmple_epi32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmple_epi64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmple_epi8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmple_epu16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmple_epu32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmple_epu64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmple_epu8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparelessthanorequal_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_NEQ_UQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_NEQ_UQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmpneq_epi16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmpneq_epi32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmpneq_epi64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmpneq_epi8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmpneq_epu16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmpneq_epu32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmpneq_epu64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmpneq_epu8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotequal_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_NEQ_UQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_NEQ_UQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmpneq_epi16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmpneq_epi32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmpneq_epi64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmpneq_epi8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmpneq_epu16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmpneq_epu32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmpneq_epu64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmpneq_epu8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotequal_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotgreaterthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_NGT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotgreaterthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotgreaterthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_NGT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotgreaterthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotgreaterthan_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_NGT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotgreaterthan_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotgreaterthan_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_NGT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotgreaterthan_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotgreaterthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_NGE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotgreaterthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotgreaterthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_NGE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotgreaterthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotgreaterthanorequal_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_NGE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotgreaterthanorequal_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotgreaterthanorequal_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_NGE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotgreaterthanorequal_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotlessthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_NLT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotlessthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotlessthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_NLT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotlessthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotlessthan_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_NLT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotlessthan_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotlessthan_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_NLT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotlessthan_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotlessthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_NLE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotlessthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotlessthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_NLE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_comparenotlessthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotlessthanorequal_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_NLE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotlessthanorequal_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotlessthanorequal_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareNotLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_NLE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_comparenotlessthanorequal_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareNotLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareordered_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareOrdered");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_ORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareordered_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareOrdered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareordered_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareOrdered");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_ORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareordered_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareOrdered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareordered_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareOrdered");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_ORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareordered_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareOrdered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareordered_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareOrdered");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_ORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareordered_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareOrdered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareunordered_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareUnordered");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_UNORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareunordered_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareUnordered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareunordered_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareUnordered");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_UNORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compareunordered_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareUnordered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareunordered_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareUnordered");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_UNORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareunordered_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareUnordered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareunordered_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompareUnordered");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_UNORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compareunordered_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompareUnordered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_ps(dn2cpp_isa_bits<__m128>(a0), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_pd(dn2cpp_isa_bits<__m128d>(a0), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128i16_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_epi16(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128i16_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128i32_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_epi32(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128i32_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128i64_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_epi64(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128i64_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128i8_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_epi8(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128i8_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128u16_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_epi16(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128u16_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128u32_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_epi32(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128u32_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128u64_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_epi64(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128u64_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128u8_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_epi8(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_compress_v128u8_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256f32_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_ps(dn2cpp_isa_bits<__m256>(a0), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256f32_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256f64_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_pd(dn2cpp_isa_bits<__m256d>(a0), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256f64_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256i16_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_epi16(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256i16_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256i32_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_epi32(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256i32_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256i64_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_epi64(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256i64_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256i8_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_epi8(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256i8_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256u16_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_epi16(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256u16_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256u32_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_epi32(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256u32_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256u64_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_epi64(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256u64_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256u8_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_epi8(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_compress_v256u8_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pf32_v128f32_v128f32(float* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm_mask_compressstoreu_ps((void*)a0, _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pf32_v128f32_v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pf32_v256f32_v256f32(float* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm256_mask_compressstoreu_ps((void*)a0, _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pf32_v256f32_v256f32(float*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pf64_v128f64_v128f64(double* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm_mask_compressstoreu_pd((void*)a0, _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128d>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pf64_v128f64_v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pf64_v256f64_v256f64(double* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm256_mask_compressstoreu_pd((void*)a0, _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256d>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pf64_v256f64_v256f64(double*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pi16_v128i16_v128i16(int16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm_mask_compressstoreu_epi16((void*)a0, _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pi16_v128i16_v128i16(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pi16_v256i16_v256i16(int16_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm256_mask_compressstoreu_epi16((void*)a0, _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pi16_v256i16_v256i16(int16_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pi32_v128i32_v128i32(int32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm_mask_compressstoreu_epi32((void*)a0, _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pi32_v128i32_v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pi32_v256i32_v256i32(int32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm256_mask_compressstoreu_epi32((void*)a0, _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pi32_v256i32_v256i32(int32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pi64_v128i64_v128i64(int64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm_mask_compressstoreu_epi64((void*)a0, _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pi64_v128i64_v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pi64_v256i64_v256i64(int64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm256_mask_compressstoreu_epi64((void*)a0, _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pi64_v256i64_v256i64(int64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pi8_v128i8_v128i8(int8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm_mask_compressstoreu_epi8((void*)a0, _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pi8_v128i8_v128i8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pi8_v256i8_v256i8(int8_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm256_mask_compressstoreu_epi8((void*)a0, _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pi8_v256i8_v256i8(int8_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pu16_v128u16_v128u16(uint16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm_mask_compressstoreu_epi16((void*)a0, _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pu16_v128u16_v128u16(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pu16_v256u16_v256u16(uint16_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm256_mask_compressstoreu_epi16((void*)a0, _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pu16_v256u16_v256u16(uint16_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pu32_v128u32_v128u32(uint32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm_mask_compressstoreu_epi32((void*)a0, _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pu32_v128u32_v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pu32_v256u32_v256u32(uint32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm256_mask_compressstoreu_epi32((void*)a0, _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pu32_v256u32_v256u32(uint32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pu64_v128u64_v128u64(uint64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm_mask_compressstoreu_epi64((void*)a0, _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pu64_v128u64_v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pu64_v256u64_v256u64(uint64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm256_mask_compressstoreu_epi64((void*)a0, _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pu64_v256u64_v256u64(uint64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pu8_v128u8_v128u8(uint8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm_mask_compressstoreu_epi8((void*)a0, _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pu8_v128u8_v128u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pu8_v256u8_v256u8(uint8_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
    _mm256_mask_compressstoreu_epi8((void*)a0, _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_compressstore_pu8_v256u8_v256u8(uint8_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_convertscalartovector128double_v128f64_u32(const Dn2CppVector128& a0, uint32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertScalarToVector128Double");
    return dn2cpp_isa_vec<16>(_mm_cvtu32_sd(dn2cpp_isa_bits<__m128d>(a0), a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_convertscalartovector128double_v128f64_u32(const Dn2CppVector128&, uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertScalarToVector128Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_convertscalartovector128single_v128f32_i32_u8(const Dn2CppVector128& a0, int32_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertScalarToVector128Single");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_convertscalartovector128single_v128f32_i32_u8(const Dn2CppVector128&, int32_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertScalarToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_convertscalartovector128single_v128f32_u32(const Dn2CppVector128& a0, uint32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertScalarToVector128Single");
    return dn2cpp_isa_vec<16>(_mm_cvtu32_ss(dn2cpp_isa_bits<__m128>(a0), a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_convertscalartovector128single_v128f32_u32(const Dn2CppVector128&, uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertScalarToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_convertscalartovector128single_v128f32_u32_u8(const Dn2CppVector128& a0, uint32_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertScalarToVector128Single");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_convertscalartovector128single_v128f32_u32_u8(const Dn2CppVector128&, uint32_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertScalarToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_convertscalartovector128single_v128f32_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertScalarToVector128Single");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_convertscalartovector128single_v128f32_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertScalarToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_converttoint32_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToInt32");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(1, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(2, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(3, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(4, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(5, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(6, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(7, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(8, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(9, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(10, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(11, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_converttoint32_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_converttoint32_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToInt32");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(1, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(2, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(3, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(4, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(5, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(6, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(7, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(8, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(9, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(10, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(11, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_converttoint32_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx10v1_converttouint32_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToUInt32");
    return _mm_cvtss_u32(dn2cpp_isa_bits<__m128>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx10v1_converttouint32_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToUInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx10v1_converttouint32_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToUInt32");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(1, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(2, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(3, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(4, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(5, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(6, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(7, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(8, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(9, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(10, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(11, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx10v1_converttouint32_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToUInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx10v1_converttouint32_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToUInt32");
    return _mm_cvtsd_u32(dn2cpp_isa_bits<__m128d>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx10v1_converttouint32_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToUInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx10v1_converttouint32_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToUInt32");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(1, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(2, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(3, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(4, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(5, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(6, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(7, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(8, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(9, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(10, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(11, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx10v1_converttouint32_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToUInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx10v1_converttouint32withtruncation_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToUInt32WithTruncation");
    return _mm_cvttss_u32(dn2cpp_isa_bits<__m128>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx10v1_converttouint32withtruncation_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToUInt32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx10v1_converttouint32withtruncation_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToUInt32WithTruncation");
    return _mm_cvttsd_u32(dn2cpp_isa_bits<__m128d>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx10v1_converttouint32withtruncation_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToUInt32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi16_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi16_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v256i16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi16_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v256i16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi32_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v256u16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi16_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v256u16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi32_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128byte_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128bytewithsaturation_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128ByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtusepi16_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128bytewithsaturation_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128ByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128bytewithsaturation_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128ByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtusepi32_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128bytewithsaturation_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128ByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128bytewithsaturation_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128ByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtusepi64_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128bytewithsaturation_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128ByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128bytewithsaturation_v256u16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128ByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtusepi16_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128bytewithsaturation_v256u16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128ByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128bytewithsaturation_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128ByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtusepi32_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128bytewithsaturation_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128ByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128bytewithsaturation_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128ByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtusepi64_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128bytewithsaturation_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128ByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128double_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Double");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_pd(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128double_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128double_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Double");
    return dn2cpp_isa_vec<16>(_mm_cvtepu32_pd(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128double_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128double_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Double");
    return dn2cpp_isa_vec<16>(_mm_cvtepu64_pd(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128double_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi32_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi32_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16withsaturation_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16WithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtsepi32_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16withsaturation_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16withsaturation_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16WithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtsepi64_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16withsaturation_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16withsaturation_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16WithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtsepi32_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16withsaturation_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16withsaturation_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16WithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtsepi64_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int16withsaturation_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int32_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int32_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int32_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int32_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int32_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int32_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int32_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int32_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int32withsaturation_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int32WithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtsepi64_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int32withsaturation_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int32WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int32withsaturation_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int32WithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtsepi64_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int32withsaturation_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int32WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int64_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int64");
    return dn2cpp_isa_vec<16>(_mm_cvtps_epi64(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int64_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int64_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int64");
    return dn2cpp_isa_vec<16>(_mm_cvtpd_epi64(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int64_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int64withtruncation_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int64WithTruncation");
    return dn2cpp_isa_vec<16>(_mm_cvttps_epi64(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int64withtruncation_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int64withtruncation_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int64WithTruncation");
    return dn2cpp_isa_vec<16>(_mm_cvttpd_epi64(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128int64withtruncation_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Int64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi16_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi16_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v256i16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi16_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v256i16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi32_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v256u16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi16_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v256u16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi32_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbyte_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbytewithsaturation_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtsepi16_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbytewithsaturation_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbytewithsaturation_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtsepi32_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbytewithsaturation_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbytewithsaturation_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtsepi64_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbytewithsaturation_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbytewithsaturation_v256i16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtsepi16_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbytewithsaturation_v256i16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbytewithsaturation_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtsepi32_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbytewithsaturation_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbytewithsaturation_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtsepi64_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128sbytewithsaturation_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128SByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128single_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Single");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_ps(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128single_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128single_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Single");
    return dn2cpp_isa_vec<16>(_mm_cvtepu32_ps(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128single_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128single_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Single");
    return dn2cpp_isa_vec<16>(_mm_cvtepu64_ps(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128single_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128single_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Single");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_ps(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128single_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128single_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Single");
    return dn2cpp_isa_vec<16>(_mm256_cvtepu64_ps(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128single_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi32_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi32_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16withsaturation_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16WithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtusepi32_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16withsaturation_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16withsaturation_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16WithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtusepi64_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16withsaturation_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16withsaturation_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16WithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtusepi32_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16withsaturation_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16withsaturation_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16WithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtusepi64_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint16withsaturation_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32");
    return dn2cpp_isa_vec<16>(_mm_cvtps_epu32(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32");
    return dn2cpp_isa_vec<16>(_mm_cvtpd_epu32(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32");
    return dn2cpp_isa_vec<16>(_mm256_cvtpd_epu32(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32withsaturation_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32WithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtusepi64_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32withsaturation_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32withsaturation_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32WithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtusepi64_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32withsaturation_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32withtruncation_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32WithTruncation");
    return dn2cpp_isa_vec<16>(_mm_cvttps_epu32(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32withtruncation_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32withtruncation_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32WithTruncation");
    return dn2cpp_isa_vec<16>(_mm_cvttpd_epu32(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32withtruncation_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32withtruncation_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32WithTruncation");
    return dn2cpp_isa_vec<16>(_mm256_cvttpd_epu32(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint32withtruncation_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint64_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt64");
    return dn2cpp_isa_vec<16>(_mm_cvtps_epu64(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint64_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint64_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt64");
    return dn2cpp_isa_vec<16>(_mm_cvtpd_epu64(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint64_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint64withtruncation_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt64WithTruncation");
    return dn2cpp_isa_vec<16>(_mm_cvttps_epu64(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint64withtruncation_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint64withtruncation_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt64WithTruncation");
    return dn2cpp_isa_vec<16>(_mm_cvttpd_epu64(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_converttovector128uint64withtruncation_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector128UInt64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256double_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256Double");
    return dn2cpp_isa_vec<32>(_mm256_cvtepu32_pd(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256double_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256double_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256Double");
    return dn2cpp_isa_vec<32>(_mm256_cvtepi64_pd(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256double_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256double_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256Double");
    return dn2cpp_isa_vec<32>(_mm256_cvtepu64_pd(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256double_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256int64_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256Int64");
    return dn2cpp_isa_vec<32>(_mm256_cvtps_epi64(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256int64_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256int64_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256Int64");
    return dn2cpp_isa_vec<32>(_mm256_cvtpd_epi64(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256int64_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256int64withtruncation_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256Int64WithTruncation");
    return dn2cpp_isa_vec<32>(_mm256_cvttps_epi64(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256int64withtruncation_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256Int64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256int64withtruncation_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256Int64WithTruncation");
    return dn2cpp_isa_vec<32>(_mm256_cvttpd_epi64(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256int64withtruncation_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256Int64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256single_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256Single");
    return dn2cpp_isa_vec<32>(_mm256_cvtepu32_ps(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256single_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256uint32_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256UInt32");
    return dn2cpp_isa_vec<32>(_mm256_cvtps_epu32(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256uint32_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256uint32withtruncation_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256UInt32WithTruncation");
    return dn2cpp_isa_vec<32>(_mm256_cvttps_epu32(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256uint32withtruncation_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256UInt32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256uint64_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256UInt64");
    return dn2cpp_isa_vec<32>(_mm256_cvtps_epu64(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256uint64_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256uint64_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256UInt64");
    return dn2cpp_isa_vec<32>(_mm256_cvtpd_epu64(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256uint64_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256uint64withtruncation_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256UInt64WithTruncation");
    return dn2cpp_isa_vec<32>(_mm256_cvttps_epu64(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256uint64withtruncation_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256UInt64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256uint64withtruncation_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256UInt64WithTruncation");
    return dn2cpp_isa_vec<32>(_mm256_cvttpd_epu64(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_converttovector256uint64withtruncation_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ConvertToVector256UInt64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_detectconflicts_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.DetectConflicts");
    return dn2cpp_isa_vec<16>(_mm_conflict_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_detectconflicts_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_detectconflicts_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.DetectConflicts");
    return dn2cpp_isa_vec<16>(_mm_conflict_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_detectconflicts_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_detectconflicts_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.DetectConflicts");
    return dn2cpp_isa_vec<16>(_mm_conflict_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_detectconflicts_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_detectconflicts_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.DetectConflicts");
    return dn2cpp_isa_vec<16>(_mm_conflict_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_detectconflicts_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_detectconflicts_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.DetectConflicts");
    return dn2cpp_isa_vec<32>(_mm256_conflict_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_detectconflicts_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_detectconflicts_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.DetectConflicts");
    return dn2cpp_isa_vec<32>(_mm256_conflict_epi64(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_detectconflicts_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_detectconflicts_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.DetectConflicts");
    return dn2cpp_isa_vec<32>(_mm256_conflict_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_detectconflicts_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_detectconflicts_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.DetectConflicts");
    return dn2cpp_isa_vec<32>(_mm256_conflict_epi64(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_detectconflicts_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_dividescalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.DivideScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_dividescalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.DivideScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_dividescalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.DivideScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_dividescalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.DivideScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_ps(dn2cpp_isa_bits<__m128>(a0), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_pd(dn2cpp_isa_bits<__m128d>(a0), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128i16_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_epi16(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128i16_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128i32_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_epi32(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128i32_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128i64_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_epi64(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128i64_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128i8_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_epi8(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128i8_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128u16_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_epi16(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128u16_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128u32_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_epi32(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128u32_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128u64_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_epi64(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128u64_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128u8_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_epi8(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expand_v128u8_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256f32_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_ps(dn2cpp_isa_bits<__m256>(a0), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256f32_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256f64_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_pd(dn2cpp_isa_bits<__m256d>(a0), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256f64_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256i16_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_epi16(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256i16_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256i32_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_epi32(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256i32_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256i64_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_epi64(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256i64_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256i8_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_epi8(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256i8_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256u16_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_epi16(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256u16_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256u32_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_epi32(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256u32_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256u64_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_epi64(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256u64_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256u8_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_epi8(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expand_v256u8_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pf32_v128f32_v128f32(float* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_ps(dn2cpp_isa_bits<__m128>(a2), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pf32_v128f32_v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pf32_v256f32_v256f32(float* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_ps(dn2cpp_isa_bits<__m256>(a2), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pf32_v256f32_v256f32(float*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pf64_v128f64_v128f64(double* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_pd(dn2cpp_isa_bits<__m128d>(a2), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pf64_v128f64_v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pf64_v256f64_v256f64(double* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_pd(dn2cpp_isa_bits<__m256d>(a2), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pf64_v256f64_v256f64(double*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pi16_v128i16_v128i16(int16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_epi16(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pi16_v128i16_v128i16(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pi16_v256i16_v256i16(int16_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_epi16(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pi16_v256i16_v256i16(int16_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pi32_v128i32_v128i32(int32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_epi32(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pi32_v128i32_v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pi32_v256i32_v256i32(int32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_epi32(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pi32_v256i32_v256i32(int32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pi64_v128i64_v128i64(int64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_epi64(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pi64_v128i64_v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pi64_v256i64_v256i64(int64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_epi64(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pi64_v256i64_v256i64(int64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pi8_v128i8_v128i8(int8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_epi8(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pi8_v128i8_v128i8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pi8_v256i8_v256i8(int8_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_epi8(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pi8_v256i8_v256i8(int8_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pu16_v128u16_v128u16(uint16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_epi16(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pu16_v128u16_v128u16(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pu16_v256u16_v256u16(uint16_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_epi16(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pu16_v256u16_v256u16(uint16_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pu32_v128u32_v128u32(uint32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_epi32(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pu32_v128u32_v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pu32_v256u32_v256u32(uint32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_epi32(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pu32_v256u32_v256u32(uint32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pu64_v128u64_v128u64(uint64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_epi64(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pu64_v128u64_v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pu64_v256u64_v256u64(uint64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_epi64(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pu64_v256u64_v256u64(uint64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pu8_v128u8_v128u8(uint8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_epi8(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_expandload_pu8_v128u8_v128u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pu8_v256u8_v256u8(uint8_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_epi8(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_expandload_pu8_v256u8_v256u8(uint8_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fixup_v128f32_v128f32_v128i32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Fixup");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_fixupimm_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fixup_v128f32_v128f32_v128i32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Fixup");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fixup_v128f64_v128f64_v128i64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Fixup");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_fixupimm_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fixup_v128f64_v128f64_v128i64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Fixup");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_fixup_v256f32_v256f32_v256i32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Fixup");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_fixupimm_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_fixup_v256f32_v256f32_v256i32_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Fixup");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_fixup_v256f64_v256f64_v256i64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Fixup");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_fixupimm_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_fixup_v256f64_v256f64_v256i64_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Fixup");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fixupscalar_v128f32_v128f32_v128i32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.FixupScalar");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_fixupimm_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fixupscalar_v128f32_v128f32_v128i32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.FixupScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fixupscalar_v128f64_v128f64_v128i64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.FixupScalar");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_fixupimm_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fixupscalar_v128f64_v128f64_v128i64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.FixupScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fusedmultiplyaddnegatedscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.FusedMultiplyAddNegatedScalar");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fusedmultiplyaddnegatedscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.FusedMultiplyAddNegatedScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fusedmultiplyaddnegatedscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.FusedMultiplyAddNegatedScalar");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fusedmultiplyaddnegatedscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.FusedMultiplyAddNegatedScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fusedmultiplyaddscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.FusedMultiplyAddScalar");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fusedmultiplyaddscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.FusedMultiplyAddScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fusedmultiplyaddscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.FusedMultiplyAddScalar");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fusedmultiplyaddscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.FusedMultiplyAddScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fusedmultiplysubtractnegatedscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.FusedMultiplySubtractNegatedScalar");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fusedmultiplysubtractnegatedscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.FusedMultiplySubtractNegatedScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fusedmultiplysubtractnegatedscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.FusedMultiplySubtractNegatedScalar");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fusedmultiplysubtractnegatedscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.FusedMultiplySubtractNegatedScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fusedmultiplysubtractscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.FusedMultiplySubtractScalar");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fusedmultiplysubtractscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.FusedMultiplySubtractScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fusedmultiplysubtractscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.FusedMultiplySubtractScalar");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_fusedmultiplysubtractscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.FusedMultiplySubtractScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getexponent_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.GetExponent");
    return dn2cpp_isa_vec<16>(_mm_getexp_ps(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getexponent_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.GetExponent");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getexponent_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.GetExponent");
    return dn2cpp_isa_vec<16>(_mm_getexp_pd(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getexponent_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.GetExponent");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_getexponent_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.GetExponent");
    return dn2cpp_isa_vec<32>(_mm256_getexp_ps(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_getexponent_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.GetExponent");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_getexponent_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.GetExponent");
    return dn2cpp_isa_vec<32>(_mm256_getexp_pd(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_getexponent_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.GetExponent");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getexponentscalar_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.GetExponentScalar");
    return dn2cpp_isa_vec<16>(_mm_getexp_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getexponentscalar_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.GetExponentScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getexponentscalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.GetExponentScalar");
    return dn2cpp_isa_vec<16>(_mm_getexp_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getexponentscalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.GetExponentScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getexponentscalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.GetExponentScalar");
    return dn2cpp_isa_vec<16>(_mm_getexp_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getexponentscalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.GetExponentScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getexponentscalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.GetExponentScalar");
    return dn2cpp_isa_vec<16>(_mm_getexp_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getexponentscalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.GetExponentScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getmantissa_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.GetMantissa");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<16>(_mm_getmant_ps(dn2cpp_isa_bits<__m128>(a0), (_MM_MANTISSA_NORM_ENUM)(DN2CPP_IMM & 3), (_MM_MANTISSA_SIGN_ENUM)(DN2CPP_IMM >> 2))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getmantissa_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.GetMantissa");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getmantissa_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.GetMantissa");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<16>(_mm_getmant_pd(dn2cpp_isa_bits<__m128d>(a0), (_MM_MANTISSA_NORM_ENUM)(DN2CPP_IMM & 3), (_MM_MANTISSA_SIGN_ENUM)(DN2CPP_IMM >> 2))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getmantissa_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.GetMantissa");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_getmantissa_v256f32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.GetMantissa");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<32>(_mm256_getmant_ps(dn2cpp_isa_bits<__m256>(a0), (_MM_MANTISSA_NORM_ENUM)(DN2CPP_IMM & 3), (_MM_MANTISSA_SIGN_ENUM)(DN2CPP_IMM >> 2))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_getmantissa_v256f32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.GetMantissa");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_getmantissa_v256f64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.GetMantissa");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<32>(_mm256_getmant_pd(dn2cpp_isa_bits<__m256d>(a0), (_MM_MANTISSA_NORM_ENUM)(DN2CPP_IMM & 3), (_MM_MANTISSA_SIGN_ENUM)(DN2CPP_IMM >> 2))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_getmantissa_v256f64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.GetMantissa");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getmantissascalar_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.GetMantissaScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<16>(_mm_getmant_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0), (_MM_MANTISSA_NORM_ENUM)(DN2CPP_IMM & 3), (_MM_MANTISSA_SIGN_ENUM)(DN2CPP_IMM >> 2))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getmantissascalar_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.GetMantissaScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getmantissascalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.GetMantissaScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<16>(_mm_getmant_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (_MM_MANTISSA_NORM_ENUM)(DN2CPP_IMM & 3), (_MM_MANTISSA_SIGN_ENUM)(DN2CPP_IMM >> 2))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getmantissascalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.GetMantissaScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getmantissascalar_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.GetMantissaScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<16>(_mm_getmant_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0), (_MM_MANTISSA_NORM_ENUM)(DN2CPP_IMM & 3), (_MM_MANTISSA_SIGN_ENUM)(DN2CPP_IMM >> 2))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getmantissascalar_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.GetMantissaScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getmantissascalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.GetMantissaScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<16>(_mm_getmant_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (_MM_MANTISSA_NORM_ENUM)(DN2CPP_IMM & 3), (_MM_MANTISSA_SIGN_ENUM)(DN2CPP_IMM >> 2))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_getmantissascalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.GetMantissaScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_leadingzerocount_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.LeadingZeroCount");
    return dn2cpp_isa_vec<16>(_mm_lzcnt_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_leadingzerocount_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_leadingzerocount_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.LeadingZeroCount");
    return dn2cpp_isa_vec<16>(_mm_lzcnt_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_leadingzerocount_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_leadingzerocount_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.LeadingZeroCount");
    return dn2cpp_isa_vec<16>(_mm_lzcnt_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_leadingzerocount_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_leadingzerocount_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.LeadingZeroCount");
    return dn2cpp_isa_vec<16>(_mm_lzcnt_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_leadingzerocount_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_leadingzerocount_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.LeadingZeroCount");
    return dn2cpp_isa_vec<32>(_mm256_lzcnt_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_leadingzerocount_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_leadingzerocount_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.LeadingZeroCount");
    return dn2cpp_isa_vec<32>(_mm256_lzcnt_epi64(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_leadingzerocount_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_leadingzerocount_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.LeadingZeroCount");
    return dn2cpp_isa_vec<32>(_mm256_lzcnt_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_leadingzerocount_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_leadingzerocount_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.LeadingZeroCount");
    return dn2cpp_isa_vec<32>(_mm256_lzcnt_epi64(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_leadingzerocount_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pf32_v128f32_v128f32(float* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_ps(dn2cpp_isa_bits<__m128>(a2), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pf32_v128f32_v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pf32_v256f32_v256f32(float* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_ps(dn2cpp_isa_bits<__m256>(a2), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pf32_v256f32_v256f32(float*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pf64_v128f64_v128f64(double* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_pd(dn2cpp_isa_bits<__m128d>(a2), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pf64_v128f64_v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pf64_v256f64_v256f64(double* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_pd(dn2cpp_isa_bits<__m256d>(a2), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pf64_v256f64_v256f64(double*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pi16_v128i16_v128i16(int16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_epi16(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pi16_v128i16_v128i16(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pi16_v256i16_v256i16(int16_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_epi16(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pi16_v256i16_v256i16(int16_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pi32_v128i32_v128i32(int32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_epi32(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pi32_v128i32_v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pi32_v256i32_v256i32(int32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_epi32(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pi32_v256i32_v256i32(int32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pi64_v128i64_v128i64(int64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_epi64(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pi64_v128i64_v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pi64_v256i64_v256i64(int64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_epi64(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pi64_v256i64_v256i64(int64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pi8_v128i8_v128i8(int8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_epi8(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pi8_v128i8_v128i8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pi8_v256i8_v256i8(int8_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_epi8(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pi8_v256i8_v256i8(int8_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pu16_v128u16_v128u16(uint16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_epi16(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pu16_v128u16_v128u16(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pu16_v256u16_v256u16(uint16_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_epi16(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pu16_v256u16_v256u16(uint16_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pu32_v128u32_v128u32(uint32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_epi32(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pu32_v128u32_v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pu32_v256u32_v256u32(uint32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_epi32(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pu32_v256u32_v256u32(uint32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pu64_v128u64_v128u64(uint64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_epi64(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pu64_v128u64_v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pu64_v256u64_v256u64(uint64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_epi64(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pu64_v256u64_v256u64(uint64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pu8_v128u8_v128u8(uint8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_epi8(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskload_pu8_v128u8_v128u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pu8_v256u8_v256u8(uint8_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_epi8(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskload_pu8_v256u8_v256u8(uint8_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskloadaligned_pf32_v128f32_v128f32(float* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
    return dn2cpp_isa_vec<16>(_mm_mask_load_ps(dn2cpp_isa_bits<__m128>(a2), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskloadaligned_pf32_v128f32_v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskloadaligned_pf32_v256f32_v256f32(float* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
    return dn2cpp_isa_vec<32>(_mm256_mask_load_ps(dn2cpp_isa_bits<__m256>(a2), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskloadaligned_pf32_v256f32_v256f32(float*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskloadaligned_pf64_v128f64_v128f64(double* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
    return dn2cpp_isa_vec<16>(_mm_mask_load_pd(dn2cpp_isa_bits<__m128d>(a2), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskloadaligned_pf64_v128f64_v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskloadaligned_pf64_v256f64_v256f64(double* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
    return dn2cpp_isa_vec<32>(_mm256_mask_load_pd(dn2cpp_isa_bits<__m256d>(a2), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskloadaligned_pf64_v256f64_v256f64(double*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskloadaligned_pi32_v128i32_v128i32(int32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
    return dn2cpp_isa_vec<16>(_mm_mask_load_epi32(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskloadaligned_pi32_v128i32_v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskloadaligned_pi32_v256i32_v256i32(int32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
    return dn2cpp_isa_vec<32>(_mm256_mask_load_epi32(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskloadaligned_pi32_v256i32_v256i32(int32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskloadaligned_pi64_v128i64_v128i64(int64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
    return dn2cpp_isa_vec<16>(_mm_mask_load_epi64(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskloadaligned_pi64_v128i64_v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskloadaligned_pi64_v256i64_v256i64(int64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
    return dn2cpp_isa_vec<32>(_mm256_mask_load_epi64(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskloadaligned_pi64_v256i64_v256i64(int64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskloadaligned_pu32_v128u32_v128u32(uint32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
    return dn2cpp_isa_vec<16>(_mm_mask_load_epi32(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskloadaligned_pu32_v128u32_v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskloadaligned_pu32_v256u32_v256u32(uint32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
    return dn2cpp_isa_vec<32>(_mm256_mask_load_epi32(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskloadaligned_pu32_v256u32_v256u32(uint32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskloadaligned_pu64_v128u64_v128u64(uint64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
    return dn2cpp_isa_vec<16>(_mm_mask_load_epi64(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_maskloadaligned_pu64_v128u64_v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskloadaligned_pu64_v256u64_v256u64(uint64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
    return dn2cpp_isa_vec<32>(_mm256_mask_load_epi64(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_maskloadaligned_pu64_v256u64_v256u64(uint64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pf32_v128f32_v128f32(float* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm_mask_storeu_ps((void*)a0, _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pf32_v128f32_v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pf32_v256f32_v256f32(float* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm256_mask_storeu_ps((void*)a0, _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pf32_v256f32_v256f32(float*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pf64_v128f64_v128f64(double* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm_mask_storeu_pd((void*)a0, _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128d>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pf64_v128f64_v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pf64_v256f64_v256f64(double* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm256_mask_storeu_pd((void*)a0, _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256d>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pf64_v256f64_v256f64(double*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pi16_v128i16_v128i16(int16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm_mask_storeu_epi16((void*)a0, _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pi16_v128i16_v128i16(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pi16_v256i16_v256i16(int16_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm256_mask_storeu_epi16((void*)a0, _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pi16_v256i16_v256i16(int16_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pi32_v128i32_v128i32(int32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm_mask_storeu_epi32((void*)a0, _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pi32_v128i32_v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pi32_v256i32_v256i32(int32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm256_mask_storeu_epi32((void*)a0, _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pi32_v256i32_v256i32(int32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pi64_v128i64_v128i64(int64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm_mask_storeu_epi64((void*)a0, _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pi64_v128i64_v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pi64_v256i64_v256i64(int64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm256_mask_storeu_epi64((void*)a0, _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pi64_v256i64_v256i64(int64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pi8_v128i8_v128i8(int8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm_mask_storeu_epi8((void*)a0, _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pi8_v128i8_v128i8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pi8_v256i8_v256i8(int8_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm256_mask_storeu_epi8((void*)a0, _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pi8_v256i8_v256i8(int8_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pu16_v128u16_v128u16(uint16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm_mask_storeu_epi16((void*)a0, _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pu16_v128u16_v128u16(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pu16_v256u16_v256u16(uint16_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm256_mask_storeu_epi16((void*)a0, _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pu16_v256u16_v256u16(uint16_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pu32_v128u32_v128u32(uint32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm_mask_storeu_epi32((void*)a0, _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pu32_v128u32_v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pu32_v256u32_v256u32(uint32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm256_mask_storeu_epi32((void*)a0, _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pu32_v256u32_v256u32(uint32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pu64_v128u64_v128u64(uint64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm_mask_storeu_epi64((void*)a0, _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pu64_v128u64_v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pu64_v256u64_v256u64(uint64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm256_mask_storeu_epi64((void*)a0, _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pu64_v256u64_v256u64(uint64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pu8_v128u8_v128u8(uint8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm_mask_storeu_epi8((void*)a0, _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pu8_v128u8_v128u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pu8_v256u8_v256u8(uint8_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
    _mm256_mask_storeu_epi8((void*)a0, _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstore_pu8_v256u8_v256u8(uint8_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pf32_v128f32_v128f32(float* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
    _mm_mask_store_ps((void*)a0, _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pf32_v128f32_v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pf32_v256f32_v256f32(float* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
    _mm256_mask_store_ps((void*)a0, _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pf32_v256f32_v256f32(float*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pf64_v128f64_v128f64(double* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
    _mm_mask_store_pd((void*)a0, _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128d>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pf64_v128f64_v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pf64_v256f64_v256f64(double* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
    _mm256_mask_store_pd((void*)a0, _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256d>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pf64_v256f64_v256f64(double*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pi32_v128i32_v128i32(int32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
    _mm_mask_store_epi32((void*)a0, _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pi32_v128i32_v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pi32_v256i32_v256i32(int32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
    _mm256_mask_store_epi32((void*)a0, _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pi32_v256i32_v256i32(int32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pi64_v128i64_v128i64(int64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
    _mm_mask_store_epi64((void*)a0, _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pi64_v128i64_v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pi64_v256i64_v256i64(int64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
    _mm256_mask_store_epi64((void*)a0, _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pi64_v256i64_v256i64(int64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pu32_v128u32_v128u32(uint32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
    _mm_mask_store_epi32((void*)a0, _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pu32_v128u32_v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pu32_v256u32_v256u32(uint32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
    _mm256_mask_store_epi32((void*)a0, _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pu32_v256u32_v256u32(uint32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pu64_v128u64_v128u64(uint64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
    _mm_mask_store_epi64((void*)a0, _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pu64_v128u64_v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pu64_v256u64_v256u64(uint64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
    _mm256_mask_store_epi64((void*)a0, _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_maskstorealigned_pu64_v256u64_v256u64(uint64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_max_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Max");
    return dn2cpp_isa_vec<16>(_mm_max_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_max_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_max_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Max");
    return dn2cpp_isa_vec<16>(_mm_max_epu64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_max_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_max_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Max");
    return dn2cpp_isa_vec<32>(_mm256_max_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_max_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_max_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Max");
    return dn2cpp_isa_vec<32>(_mm256_max_epu64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_max_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_min_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Min");
    return dn2cpp_isa_vec<16>(_mm_min_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_min_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_min_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Min");
    return dn2cpp_isa_vec<16>(_mm_min_epu64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_min_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_min_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Min");
    return dn2cpp_isa_vec<32>(_mm256_min_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_min_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_min_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Min");
    return dn2cpp_isa_vec<32>(_mm256_min_epu64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_min_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256i16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256i16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256i8(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256i8(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256u16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256u16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256u8(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
    return (int32_t)_mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_movemask_v256u8(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_multiplylow_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MultiplyLow");
    return dn2cpp_isa_vec<16>(_mm_mullo_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_multiplylow_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_multiplylow_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MultiplyLow");
    return dn2cpp_isa_vec<16>(_mm_mullo_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_multiplylow_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_multiplylow_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MultiplyLow");
    return dn2cpp_isa_vec<32>(_mm256_mullo_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_multiplylow_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_multiplylow_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MultiplyLow");
    return dn2cpp_isa_vec<32>(_mm256_mullo_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_multiplylow_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_multiplyscalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MultiplyScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_multiplyscalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MultiplyScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_multiplyscalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MultiplyScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_multiplyscalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MultiplyScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_multishift_v128i8_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MultiShift");
    return dn2cpp_isa_vec<16>(_mm_multishift_epi64_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_multishift_v128i8_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MultiShift");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_multishift_v128u8_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MultiShift");
    return dn2cpp_isa_vec<16>(_mm_multishift_epi64_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_multishift_v128u8_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MultiShift");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_multishift_v256i8_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MultiShift");
    return dn2cpp_isa_vec<32>(_mm256_multishift_epi64_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_multishift_v256i8_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MultiShift");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_multishift_v256u8_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.MultiShift");
    return dn2cpp_isa_vec<32>(_mm256_multishift_epi64_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_multishift_v256u8_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.MultiShift");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar16x16_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar16x16");
    return dn2cpp_isa_vec<32>(_mm256_permutexvar_epi16(dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar16x16_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar16x16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar16x16_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar16x16");
    return dn2cpp_isa_vec<32>(_mm256_permutexvar_epi16(dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar16x16_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar16x16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar16x16x2_v256i16_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar16x16x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar16x16x2_v256i16_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar16x16x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar16x16x2_v256u16_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar16x16x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar16x16x2_v256u16_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar16x16x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar16x8_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar16x8");
    return dn2cpp_isa_vec<16>(_mm_permutexvar_epi8(dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar16x8_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar16x8");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar16x8_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar16x8");
    return dn2cpp_isa_vec<16>(_mm_permutexvar_epi8(dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar16x8_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar16x8");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar16x8x2_v128i8_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar16x8x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar16x8x2_v128i8_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar16x8x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar16x8x2_v128u8_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar16x8x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar16x8x2_v128u8_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar16x8x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar2x64x2_v128f64_v128i64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar2x64x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar2x64x2_v128f64_v128i64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar2x64x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar2x64x2_v128i64_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar2x64x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar2x64x2_v128i64_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar2x64x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar2x64x2_v128u64_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar2x64x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar2x64x2_v128u64_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar2x64x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar32x8_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar32x8");
    return dn2cpp_isa_vec<32>(_mm256_permutexvar_epi8(dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar32x8_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar32x8");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar32x8_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar32x8");
    return dn2cpp_isa_vec<32>(_mm256_permutexvar_epi8(dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar32x8_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar32x8");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar32x8x2_v256i8_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar32x8x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar32x8x2_v256i8_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar32x8x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar32x8x2_v256u8_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar32x8x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar32x8x2_v256u8_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar32x8x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar4x32x2_v128f32_v128i32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar4x32x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar4x32x2_v128f32_v128i32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar4x32x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar4x32x2_v128i32_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar4x32x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar4x32x2_v128i32_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar4x32x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar4x32x2_v128u32_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar4x32x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar4x32x2_v128u32_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar4x32x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar4x64_v256f64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar4x64");
    return dn2cpp_isa_vec<32>(_mm256_permutexvar_pd(dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar4x64_v256f64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar4x64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar4x64_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar4x64");
    return dn2cpp_isa_vec<32>(_mm256_permutexvar_epi64(dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar4x64_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar4x64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar4x64_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar4x64");
    return dn2cpp_isa_vec<32>(_mm256_permutexvar_epi64(dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar4x64_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar4x64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar4x64x2_v256f64_v256i64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar4x64x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar4x64x2_v256f64_v256i64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar4x64x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar4x64x2_v256i64_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar4x64x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar4x64x2_v256i64_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar4x64x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar4x64x2_v256u64_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar4x64x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar4x64x2_v256u64_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar4x64x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar8x16_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar8x16");
    return dn2cpp_isa_vec<16>(_mm_permutexvar_epi16(dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar8x16_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar8x16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar8x16_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar8x16");
    return dn2cpp_isa_vec<16>(_mm_permutexvar_epi16(dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar8x16_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar8x16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar8x16x2_v128i16_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar8x16x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar8x16x2_v128i16_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar8x16x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar8x16x2_v128u16_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar8x16x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_permutevar8x16x2_v128u16_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar8x16x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar8x32x2_v256f32_v256i32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar8x32x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar8x32x2_v256f32_v256i32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar8x32x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar8x32x2_v256i32_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar8x32x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar8x32x2_v256i32_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar8x32x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar8x32x2_v256u32_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar8x32x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_permutevar8x32x2_v256u32_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.PermuteVar8x32x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_range_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Range");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<16>(_mm_range_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_range_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Range");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_range_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Range");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<16>(_mm_range_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_range_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Range");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_range_v256f32_v256f32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Range");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<32>(_mm256_range_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_range_v256f32_v256f32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Range");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_range_v256f64_v256f64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Range");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<32>(_mm256_range_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_range_v256f64_v256f64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Range");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rangescalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RangeScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<16>(_mm_range_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rangescalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RangeScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rangescalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RangeScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<16>(_mm_range_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rangescalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RangeScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocal14_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Reciprocal14");
    return dn2cpp_isa_vec<16>(_mm_rcp14_ps(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocal14_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Reciprocal14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocal14_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Reciprocal14");
    return dn2cpp_isa_vec<16>(_mm_rcp14_pd(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocal14_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Reciprocal14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_reciprocal14_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Reciprocal14");
    return dn2cpp_isa_vec<32>(_mm256_rcp14_ps(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_reciprocal14_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Reciprocal14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_reciprocal14_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Reciprocal14");
    return dn2cpp_isa_vec<32>(_mm256_rcp14_pd(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_reciprocal14_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Reciprocal14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocal14scalar_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Reciprocal14Scalar");
    return dn2cpp_isa_vec<16>(_mm_rcp14_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocal14scalar_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Reciprocal14Scalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocal14scalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Reciprocal14Scalar");
    return dn2cpp_isa_vec<16>(_mm_rcp14_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocal14scalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Reciprocal14Scalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocal14scalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Reciprocal14Scalar");
    return dn2cpp_isa_vec<16>(_mm_rcp14_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocal14scalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Reciprocal14Scalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocal14scalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Reciprocal14Scalar");
    return dn2cpp_isa_vec<16>(_mm_rcp14_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocal14scalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Reciprocal14Scalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocalsqrt14_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ReciprocalSqrt14");
    return dn2cpp_isa_vec<16>(_mm_rsqrt14_ps(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocalsqrt14_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ReciprocalSqrt14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocalsqrt14_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ReciprocalSqrt14");
    return dn2cpp_isa_vec<16>(_mm_rsqrt14_pd(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocalsqrt14_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ReciprocalSqrt14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_reciprocalsqrt14_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ReciprocalSqrt14");
    return dn2cpp_isa_vec<32>(_mm256_rsqrt14_ps(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_reciprocalsqrt14_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ReciprocalSqrt14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_reciprocalsqrt14_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ReciprocalSqrt14");
    return dn2cpp_isa_vec<32>(_mm256_rsqrt14_pd(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_reciprocalsqrt14_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ReciprocalSqrt14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocalsqrt14scalar_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ReciprocalSqrt14Scalar");
    return dn2cpp_isa_vec<16>(_mm_rsqrt14_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocalsqrt14scalar_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ReciprocalSqrt14Scalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocalsqrt14scalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ReciprocalSqrt14Scalar");
    return dn2cpp_isa_vec<16>(_mm_rsqrt14_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocalsqrt14scalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ReciprocalSqrt14Scalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocalsqrt14scalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ReciprocalSqrt14Scalar");
    return dn2cpp_isa_vec<16>(_mm_rsqrt14_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocalsqrt14scalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ReciprocalSqrt14Scalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocalsqrt14scalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ReciprocalSqrt14Scalar");
    return dn2cpp_isa_vec<16>(_mm_rsqrt14_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reciprocalsqrt14scalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ReciprocalSqrt14Scalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reduce_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Reduce");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_reduce_ps(dn2cpp_isa_bits<__m128>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reduce_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Reduce");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reduce_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Reduce");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_reduce_pd(dn2cpp_isa_bits<__m128d>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reduce_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Reduce");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_reduce_v256f32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Reduce");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_reduce_ps(dn2cpp_isa_bits<__m256>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_reduce_v256f32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Reduce");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_reduce_v256f64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Reduce");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_reduce_pd(dn2cpp_isa_bits<__m256d>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_reduce_v256f64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Reduce");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reducescalar_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ReduceScalar");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_reduce_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reducescalar_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ReduceScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reducescalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ReduceScalar");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_reduce_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reducescalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ReduceScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reducescalar_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ReduceScalar");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_reduce_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reducescalar_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ReduceScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reducescalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ReduceScalar");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_reduce_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_reducescalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ReduceScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateleft_v128i32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_rol_epi32(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateleft_v128i32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateleft_v128i64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_rol_epi64(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateleft_v128i64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateleft_v128u32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_rol_epi32(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateleft_v128u32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateleft_v128u64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_rol_epi64(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateleft_v128u64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateleft_v256i32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_rol_epi32(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateleft_v256i32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateleft_v256i64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_rol_epi64(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateleft_v256i64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateleft_v256u32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_rol_epi32(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateleft_v256u32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateleft_v256u64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_rol_epi64(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateleft_v256u64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateleftvariable_v128i32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateLeftVariable");
    return dn2cpp_isa_vec<16>(_mm_rolv_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateleftvariable_v128i32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateleftvariable_v128i64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateLeftVariable");
    return dn2cpp_isa_vec<16>(_mm_rolv_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateleftvariable_v128i64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateleftvariable_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateLeftVariable");
    return dn2cpp_isa_vec<16>(_mm_rolv_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateleftvariable_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateleftvariable_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateLeftVariable");
    return dn2cpp_isa_vec<16>(_mm_rolv_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateleftvariable_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateleftvariable_v256i32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateLeftVariable");
    return dn2cpp_isa_vec<32>(_mm256_rolv_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateleftvariable_v256i32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateleftvariable_v256i64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateLeftVariable");
    return dn2cpp_isa_vec<32>(_mm256_rolv_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateleftvariable_v256i64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateleftvariable_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateLeftVariable");
    return dn2cpp_isa_vec<32>(_mm256_rolv_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateleftvariable_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateleftvariable_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateLeftVariable");
    return dn2cpp_isa_vec<32>(_mm256_rolv_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateleftvariable_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateright_v128i32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_ror_epi32(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateright_v128i32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateright_v128i64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_ror_epi64(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateright_v128i64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateright_v128u32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_ror_epi32(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateright_v128u32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateright_v128u64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_ror_epi64(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotateright_v128u64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateright_v256i32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_ror_epi32(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateright_v256i32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateright_v256i64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_ror_epi64(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateright_v256i64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateright_v256u32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_ror_epi32(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateright_v256u32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateright_v256u64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_ror_epi64(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotateright_v256u64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotaterightvariable_v128i32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateRightVariable");
    return dn2cpp_isa_vec<16>(_mm_rorv_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotaterightvariable_v128i32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotaterightvariable_v128i64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateRightVariable");
    return dn2cpp_isa_vec<16>(_mm_rorv_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotaterightvariable_v128i64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotaterightvariable_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateRightVariable");
    return dn2cpp_isa_vec<16>(_mm_rorv_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotaterightvariable_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotaterightvariable_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateRightVariable");
    return dn2cpp_isa_vec<16>(_mm_rorv_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_rotaterightvariable_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotaterightvariable_v256i32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateRightVariable");
    return dn2cpp_isa_vec<32>(_mm256_rorv_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotaterightvariable_v256i32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotaterightvariable_v256i64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateRightVariable");
    return dn2cpp_isa_vec<32>(_mm256_rorv_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotaterightvariable_v256i64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotaterightvariable_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateRightVariable");
    return dn2cpp_isa_vec<32>(_mm256_rorv_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotaterightvariable_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotaterightvariable_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RotateRightVariable");
    return dn2cpp_isa_vec<32>(_mm256_rorv_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_rotaterightvariable_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_roundscale_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RoundScale");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_roundscale_ps(dn2cpp_isa_bits<__m128>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_roundscale_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RoundScale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_roundscale_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RoundScale");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_roundscale_pd(dn2cpp_isa_bits<__m128d>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_roundscale_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RoundScale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_roundscale_v256f32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RoundScale");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_roundscale_ps(dn2cpp_isa_bits<__m256>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_roundscale_v256f32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RoundScale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_roundscale_v256f64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RoundScale");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_roundscale_pd(dn2cpp_isa_bits<__m256d>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_roundscale_v256f64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RoundScale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_roundscalescalar_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RoundScaleScalar");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_roundscale_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_roundscalescalar_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RoundScaleScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_roundscalescalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RoundScaleScalar");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_roundscale_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_roundscalescalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RoundScaleScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_roundscalescalar_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RoundScaleScalar");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_roundscale_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_roundscalescalar_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RoundScaleScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_roundscalescalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.RoundScaleScalar");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_roundscale_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_roundscalescalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.RoundScaleScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_scale_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Scale");
    return dn2cpp_isa_vec<16>(_mm_scalef_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_scale_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Scale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_scale_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Scale");
    return dn2cpp_isa_vec<16>(_mm_scalef_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_scale_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Scale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_scale_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Scale");
    return dn2cpp_isa_vec<32>(_mm256_scalef_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_scale_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Scale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_scale_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Scale");
    return dn2cpp_isa_vec<32>(_mm256_scalef_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_scale_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Scale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_scalescalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ScaleScalar");
    return dn2cpp_isa_vec<16>(_mm_scalef_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_scalescalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ScaleScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_scalescalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ScaleScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_scalescalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ScaleScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_scalescalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ScaleScalar");
    return dn2cpp_isa_vec<16>(_mm_scalef_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_scalescalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ScaleScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_scalescalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ScaleScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_scalescalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ScaleScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_shiftleftlogicalvariable_v128i16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<16>(_mm_sllv_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_shiftleftlogicalvariable_v128i16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_shiftleftlogicalvariable_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<16>(_mm_sllv_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_shiftleftlogicalvariable_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shiftleftlogicalvariable_v256i16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<32>(_mm256_sllv_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shiftleftlogicalvariable_v256i16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shiftleftlogicalvariable_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<32>(_mm256_sllv_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shiftleftlogicalvariable_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_shiftrightarithmetic_v128i64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightArithmetic");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_srai_epi64(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_shiftrightarithmetic_v128i64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_shiftrightarithmetic_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightArithmetic");
    return dn2cpp_isa_vec<16>(_mm_sra_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_shiftrightarithmetic_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shiftrightarithmetic_v256i64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightArithmetic");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_srai_epi64(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shiftrightarithmetic_v256i64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shiftrightarithmetic_v256i64_v128i64(const Dn2CppVector256& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightArithmetic");
    return dn2cpp_isa_vec<32>(_mm256_sra_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shiftrightarithmetic_v256i64_v128i64(const Dn2CppVector256&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_shiftrightarithmeticvariable_v128i16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightArithmeticVariable");
    return dn2cpp_isa_vec<16>(_mm_srav_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_shiftrightarithmeticvariable_v128i16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightArithmeticVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_shiftrightarithmeticvariable_v128i64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightArithmeticVariable");
    return dn2cpp_isa_vec<16>(_mm_srav_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_shiftrightarithmeticvariable_v128i64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightArithmeticVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shiftrightarithmeticvariable_v256i16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightArithmeticVariable");
    return dn2cpp_isa_vec<32>(_mm256_srav_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shiftrightarithmeticvariable_v256i16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightArithmeticVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shiftrightarithmeticvariable_v256i64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightArithmeticVariable");
    return dn2cpp_isa_vec<32>(_mm256_srav_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shiftrightarithmeticvariable_v256i64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightArithmeticVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_shiftrightlogicalvariable_v128i16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<16>(_mm_srlv_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_shiftrightlogicalvariable_v128i16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_shiftrightlogicalvariable_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<16>(_mm_srlv_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_shiftrightlogicalvariable_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shiftrightlogicalvariable_v256i16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<32>(_mm256_srlv_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shiftrightlogicalvariable_v256i16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shiftrightlogicalvariable_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<32>(_mm256_srlv_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shiftrightlogicalvariable_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shuffle2x128_v256f32_v256f32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Shuffle2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_shuffle_f32x4(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shuffle2x128_v256f32_v256f32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Shuffle2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shuffle2x128_v256f64_v256f64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Shuffle2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_shuffle_f64x2(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shuffle2x128_v256f64_v256f64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Shuffle2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shuffle2x128_v256i32_v256i32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Shuffle2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_shuffle_i32x4(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shuffle2x128_v256i32_v256i32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Shuffle2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shuffle2x128_v256i64_v256i64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Shuffle2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_shuffle_i64x2(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shuffle2x128_v256i64_v256i64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Shuffle2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shuffle2x128_v256u32_v256u32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Shuffle2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_shuffle_i32x4(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shuffle2x128_v256u32_v256u32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Shuffle2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shuffle2x128_v256u64_v256u64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.Shuffle2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_shuffle_i64x2(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_shuffle2x128_v256u64_v256u64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.Shuffle2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_sqrtscalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.SqrtScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_sqrtscalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.SqrtScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_sqrtscalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.SqrtScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_sqrtscalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.SqrtScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_subtractscalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.SubtractScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_subtractscalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.SubtractScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_subtractscalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.SubtractScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_subtractscalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.SubtractScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_sumabsolutedifferencesinblock32_v128u8_v128u8_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.SumAbsoluteDifferencesInBlock32");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_dbsad_epu8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_sumabsolutedifferencesinblock32_v128u8_v128u8_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.SumAbsoluteDifferencesInBlock32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_sumabsolutedifferencesinblock32_v256u8_v256u8_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.SumAbsoluteDifferencesInBlock32");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_dbsad_epu8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_sumabsolutedifferencesinblock32_v256u8_v256u8_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.SumAbsoluteDifferencesInBlock32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128f32_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128f32_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128f64_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128f64_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128i16_v128i16_v128i16_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128i16_v128i16_v128i16_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128i32_v128i32_v128i32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128i32_v128i32_v128i32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128i64_v128i64_v128i64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128i64_v128i64_v128i64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128i8_v128i8_v128i8_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128i8_v128i8_v128i8_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128u16_v128u16_v128u16_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128u16_v128u16_v128u16_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128u32_v128u32_v128u32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128u32_v128u32_v128u32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128u64_v128u64_v128u64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128u64_v128u64_v128u64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128u8_v128u8_v128u8_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_ternarylogic_v128u8_v128u8_v128u8_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256f32_v256f32_v256f32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256f32_v256f32_v256f32_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256f64_v256f64_v256f64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256f64_v256f64_v256f64_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256i16_v256i16_v256i16_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256i16_v256i16_v256i16_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256i32_v256i32_v256i32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256i32_v256i32_v256i32_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256i64_v256i64_v256i64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256i64_v256i64_v256i64_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256i8_v256i8_v256i8_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256i8_v256i8_v256i8_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256u16_v256u16_v256u16_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256u16_v256u16_v256u16_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256u32_v256u32_v256u32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256u32_v256u32_v256u32_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256u64_v256u64_v256u64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256u64_v256u64_v256u64_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256u8_v256u8_v256u8_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1, "System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_ternarylogic_v256u8_v256u8_v256u8_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1.TernaryLogic");
}
#endif
