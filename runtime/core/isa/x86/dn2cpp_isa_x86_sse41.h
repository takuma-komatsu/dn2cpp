#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Sse41: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blend_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Blend");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_blend_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM & 15)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blend_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Blend");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blend_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Blend");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_blend_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blend_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Blend");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blend_v128i16_v128i16_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Blend");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_blend_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blend_v128i16_v128i16_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Blend");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blend_v128u16_v128u16_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Blend");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_blend_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blend_v128u16_v128u16_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Blend");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_blendv_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_blendv_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128i16_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_blendv_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128i16_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128i32_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_blendv_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128i32_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128i64_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_blendv_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128i64_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128i8_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_blendv_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128i8_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128u16_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_blendv_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128u16_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128u32_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_blendv_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128u32_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128u64_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_blendv_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128u64_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128u8_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_blendv_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_blendvariable_v128u8_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_ceiling_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Ceiling");
    return dn2cpp_isa_vec<16>(_mm_ceil_ps(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_ceiling_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Ceiling");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_ceiling_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Ceiling");
    return dn2cpp_isa_vec<16>(_mm_ceil_pd(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_ceiling_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Ceiling");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_ceilingscalar_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.CeilingScalar");
    return dn2cpp_isa_vec<16>(_mm_ceil_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_ceilingscalar_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.CeilingScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_ceilingscalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.CeilingScalar");
    return dn2cpp_isa_vec<16>(_mm_ceil_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_ceilingscalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.CeilingScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_ceilingscalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.CeilingScalar");
    return dn2cpp_isa_vec<16>(_mm_ceil_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_ceilingscalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.CeilingScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_ceilingscalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.CeilingScalar");
    return dn2cpp_isa_vec<16>(_mm_ceil_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_ceilingscalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.CeilingScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_compareequal_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpeq_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_compareequal_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_compareequal_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpeq_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_compareequal_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int16_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm_cvtepi8_epi16(_mm_loadl_epi64((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int16_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int16_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm_cvtepu8_epi16(_mm_loadl_epi64((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int16_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int16_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm_cvtepi8_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int16_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int16_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm_cvtepu8_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int16_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int32_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm_cvtepi16_epi32(_mm_loadl_epi64((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int32_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int32_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm_cvtepi8_epi32(_mm_loadu_si32(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int32_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int32_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm_cvtepu16_epi32(_mm_loadl_epi64((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int32_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int32_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm_cvtepu8_epi32(_mm_loadu_si32(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int32_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int32_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm_cvtepi16_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int32_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int32_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm_cvtepi8_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int32_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int32_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm_cvtepu16_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int32_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int32_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm_cvtepu8_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int32_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
    return dn2cpp_isa_vec<16>(_mm_cvtepi16_epi64(_mm_loadu_si32(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_epi64(_mm_loadl_epi64((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
    return dn2cpp_isa_vec<16>(_mm_cvtepi8_epi64(_mm_loadu_si16(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
    return dn2cpp_isa_vec<16>(_mm_cvtepu16_epi64(_mm_loadu_si32(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
    return dn2cpp_isa_vec<16>(_mm_cvtepu32_epi64(_mm_loadl_epi64((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
    return dn2cpp_isa_vec<16>(_mm_cvtepu8_epi64(_mm_loadu_si16(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
    return dn2cpp_isa_vec<16>(_mm_cvtepi16_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
    return dn2cpp_isa_vec<16>(_mm_cvtepi8_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
    return dn2cpp_isa_vec<16>(_mm_cvtepu16_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
    return dn2cpp_isa_vec<16>(_mm_cvtepu32_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
    return dn2cpp_isa_vec<16>(_mm_cvtepu8_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_converttovector128int64_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.ConvertToVector128Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_dotproduct_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.DotProduct");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_dp_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_dotproduct_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.DotProduct");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_dotproduct_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.DotProduct");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_dp_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_dotproduct_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.DotProduct");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE float dn2cpp_isa_x86_sse41_extract_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Extract");
    DN2CPP_ISA_IMM_WRAP_SWITCH(4, a1, dn2cpp_isa_bitcast<float>(_mm_extract_ps(dn2cpp_isa_bits<__m128>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE float dn2cpp_isa_x86_sse41_extract_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Extract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse41_extract_v128i32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Extract");
    DN2CPP_ISA_IMM_WRAP_SWITCH(4, a1, _mm_extract_epi32(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse41_extract_v128i32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Extract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_sse41_extract_v128u32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Extract");
    DN2CPP_ISA_IMM_WRAP_SWITCH(4, a1, (uint32_t)_mm_extract_epi32(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_sse41_extract_v128u32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Extract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE uint8_t dn2cpp_isa_x86_sse41_extract_v128u8_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Extract");
    DN2CPP_ISA_IMM_WRAP_SWITCH(16, a1, (uint8_t)_mm_extract_epi8(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint8_t dn2cpp_isa_x86_sse41_extract_v128u8_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Extract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_floor_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Floor");
    return dn2cpp_isa_vec<16>(_mm_floor_ps(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_floor_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Floor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_floor_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Floor");
    return dn2cpp_isa_vec<16>(_mm_floor_pd(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_floor_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Floor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_floorscalar_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.FloorScalar");
    return dn2cpp_isa_vec<16>(_mm_floor_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_floorscalar_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.FloorScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_floorscalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.FloorScalar");
    return dn2cpp_isa_vec<16>(_mm_floor_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_floorscalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.FloorScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_floorscalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.FloorScalar");
    return dn2cpp_isa_vec<16>(_mm_floor_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_floorscalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.FloorScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_floorscalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.FloorScalar");
    return dn2cpp_isa_vec<16>(_mm_floor_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_floorscalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.FloorScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_insert_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Insert");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_insert_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_insert_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Insert");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_insert_v128i32_i32_u8(const Dn2CppVector128& a0, int32_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Insert");
    DN2CPP_ISA_IMM_WRAP_SWITCH(4, a2, dn2cpp_isa_vec<16>(_mm_insert_epi32(dn2cpp_isa_bits<__m128i>(a0), (int)a1, DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_insert_v128i32_i32_u8(const Dn2CppVector128&, int32_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Insert");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_insert_v128i8_i8_u8(const Dn2CppVector128& a0, int8_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Insert");
    DN2CPP_ISA_IMM_WRAP_SWITCH(16, a2, dn2cpp_isa_vec<16>(_mm_insert_epi8(dn2cpp_isa_bits<__m128i>(a0), a1, DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_insert_v128i8_i8_u8(const Dn2CppVector128&, int8_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Insert");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_insert_v128u32_u32_u8(const Dn2CppVector128& a0, uint32_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Insert");
    DN2CPP_ISA_IMM_WRAP_SWITCH(4, a2, dn2cpp_isa_vec<16>(_mm_insert_epi32(dn2cpp_isa_bits<__m128i>(a0), (int)a1, DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_insert_v128u32_u32_u8(const Dn2CppVector128&, uint32_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Insert");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_insert_v128u8_u8_u8(const Dn2CppVector128& a0, uint8_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Insert");
    DN2CPP_ISA_IMM_WRAP_SWITCH(16, a2, dn2cpp_isa_vec<16>(_mm_insert_epi8(dn2cpp_isa_bits<__m128i>(a0), a1, DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_insert_v128u8_u8_u8(const Dn2CppVector128&, uint8_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Insert");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_loadalignedvector128nontemporal_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.LoadAlignedVector128NonTemporal");
    return dn2cpp_isa_vec<16>(_mm_stream_load_si128((__m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_loadalignedvector128nontemporal_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.LoadAlignedVector128NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_loadalignedvector128nontemporal_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.LoadAlignedVector128NonTemporal");
    return dn2cpp_isa_vec<16>(_mm_stream_load_si128((__m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_loadalignedvector128nontemporal_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.LoadAlignedVector128NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_loadalignedvector128nontemporal_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.LoadAlignedVector128NonTemporal");
    return dn2cpp_isa_vec<16>(_mm_stream_load_si128((__m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_loadalignedvector128nontemporal_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.LoadAlignedVector128NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_loadalignedvector128nontemporal_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.LoadAlignedVector128NonTemporal");
    return dn2cpp_isa_vec<16>(_mm_stream_load_si128((__m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_loadalignedvector128nontemporal_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.LoadAlignedVector128NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_loadalignedvector128nontemporal_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.LoadAlignedVector128NonTemporal");
    return dn2cpp_isa_vec<16>(_mm_stream_load_si128((__m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_loadalignedvector128nontemporal_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.LoadAlignedVector128NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_loadalignedvector128nontemporal_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.LoadAlignedVector128NonTemporal");
    return dn2cpp_isa_vec<16>(_mm_stream_load_si128((__m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_loadalignedvector128nontemporal_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.LoadAlignedVector128NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_loadalignedvector128nontemporal_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.LoadAlignedVector128NonTemporal");
    return dn2cpp_isa_vec<16>(_mm_stream_load_si128((__m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_loadalignedvector128nontemporal_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.LoadAlignedVector128NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_loadalignedvector128nontemporal_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.LoadAlignedVector128NonTemporal");
    return dn2cpp_isa_vec<16>(_mm_stream_load_si128((__m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_loadalignedvector128nontemporal_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.LoadAlignedVector128NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_max_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Max");
    return dn2cpp_isa_vec<16>(_mm_max_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_max_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_max_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Max");
    return dn2cpp_isa_vec<16>(_mm_max_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_max_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_max_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Max");
    return dn2cpp_isa_vec<16>(_mm_max_epu16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_max_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_max_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Max");
    return dn2cpp_isa_vec<16>(_mm_max_epu32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_max_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_min_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Min");
    return dn2cpp_isa_vec<16>(_mm_min_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_min_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_min_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Min");
    return dn2cpp_isa_vec<16>(_mm_min_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_min_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_min_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Min");
    return dn2cpp_isa_vec<16>(_mm_min_epu16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_min_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_min_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Min");
    return dn2cpp_isa_vec<16>(_mm_min_epu32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_min_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_minhorizontal_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.MinHorizontal");
    return dn2cpp_isa_vec<16>(_mm_minpos_epu16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_minhorizontal_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.MinHorizontal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_multiplesumabsolutedifferences_v128u8_v128u8_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.MultipleSumAbsoluteDifferences");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_mpsadbw_epu8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_multiplesumabsolutedifferences_v128u8_v128u8_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.MultipleSumAbsoluteDifferences");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_multiply_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.Multiply");
    return dn2cpp_isa_vec<16>(_mm_mul_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_multiply_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.Multiply");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_multiplylow_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.MultiplyLow");
    return dn2cpp_isa_vec<16>(_mm_mullo_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_multiplylow_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_multiplylow_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.MultiplyLow");
    return dn2cpp_isa_vec<16>(_mm_mullo_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_multiplylow_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_packunsignedsaturate_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.PackUnsignedSaturate");
    return dn2cpp_isa_vec<16>(_mm_packus_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_packunsignedsaturate_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.PackUnsignedSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundcurrentdirection_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundCurrentDirection");
    return dn2cpp_isa_vec<16>(_mm_round_ps(dn2cpp_isa_bits<__m128>(a0), _MM_FROUND_CUR_DIRECTION));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundcurrentdirection_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundCurrentDirection");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundcurrentdirection_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundCurrentDirection");
    return dn2cpp_isa_vec<16>(_mm_round_pd(dn2cpp_isa_bits<__m128d>(a0), _MM_FROUND_CUR_DIRECTION));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundcurrentdirection_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundCurrentDirection");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundcurrentdirectionscalar_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundCurrentDirectionScalar");
    return dn2cpp_isa_vec<16>(_mm_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0), _MM_FROUND_CUR_DIRECTION));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundcurrentdirectionscalar_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundCurrentDirectionScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundcurrentdirectionscalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundCurrentDirectionScalar");
    return dn2cpp_isa_vec<16>(_mm_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _MM_FROUND_CUR_DIRECTION));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundcurrentdirectionscalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundCurrentDirectionScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundcurrentdirectionscalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundCurrentDirectionScalar");
    return dn2cpp_isa_vec<16>(_mm_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0), _MM_FROUND_CUR_DIRECTION));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundcurrentdirectionscalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundCurrentDirectionScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundcurrentdirectionscalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundCurrentDirectionScalar");
    return dn2cpp_isa_vec<16>(_mm_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _MM_FROUND_CUR_DIRECTION));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundcurrentdirectionscalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundCurrentDirectionScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonearestinteger_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToNearestInteger");
    return dn2cpp_isa_vec<16>(_mm_round_ps(dn2cpp_isa_bits<__m128>(a0), _MM_FROUND_TO_NEAREST_INT | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonearestinteger_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToNearestInteger");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonearestinteger_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToNearestInteger");
    return dn2cpp_isa_vec<16>(_mm_round_pd(dn2cpp_isa_bits<__m128d>(a0), _MM_FROUND_TO_NEAREST_INT | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonearestinteger_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToNearestInteger");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonearestintegerscalar_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToNearestIntegerScalar");
    return dn2cpp_isa_vec<16>(_mm_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0), _MM_FROUND_TO_NEAREST_INT | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonearestintegerscalar_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToNearestIntegerScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonearestintegerscalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToNearestIntegerScalar");
    return dn2cpp_isa_vec<16>(_mm_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _MM_FROUND_TO_NEAREST_INT | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonearestintegerscalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToNearestIntegerScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonearestintegerscalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToNearestIntegerScalar");
    return dn2cpp_isa_vec<16>(_mm_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0), _MM_FROUND_TO_NEAREST_INT | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonearestintegerscalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToNearestIntegerScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonearestintegerscalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToNearestIntegerScalar");
    return dn2cpp_isa_vec<16>(_mm_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _MM_FROUND_TO_NEAREST_INT | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonearestintegerscalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToNearestIntegerScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonegativeinfinity_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToNegativeInfinity");
    return dn2cpp_isa_vec<16>(_mm_round_ps(dn2cpp_isa_bits<__m128>(a0), _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonegativeinfinity_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToNegativeInfinity");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonegativeinfinity_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToNegativeInfinity");
    return dn2cpp_isa_vec<16>(_mm_round_pd(dn2cpp_isa_bits<__m128d>(a0), _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonegativeinfinity_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToNegativeInfinity");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonegativeinfinityscalar_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToNegativeInfinityScalar");
    return dn2cpp_isa_vec<16>(_mm_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0), _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonegativeinfinityscalar_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToNegativeInfinityScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonegativeinfinityscalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToNegativeInfinityScalar");
    return dn2cpp_isa_vec<16>(_mm_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonegativeinfinityscalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToNegativeInfinityScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonegativeinfinityscalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToNegativeInfinityScalar");
    return dn2cpp_isa_vec<16>(_mm_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0), _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonegativeinfinityscalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToNegativeInfinityScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonegativeinfinityscalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToNegativeInfinityScalar");
    return dn2cpp_isa_vec<16>(_mm_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtonegativeinfinityscalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToNegativeInfinityScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtopositiveinfinity_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToPositiveInfinity");
    return dn2cpp_isa_vec<16>(_mm_round_ps(dn2cpp_isa_bits<__m128>(a0), _MM_FROUND_TO_POS_INF | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtopositiveinfinity_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToPositiveInfinity");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtopositiveinfinity_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToPositiveInfinity");
    return dn2cpp_isa_vec<16>(_mm_round_pd(dn2cpp_isa_bits<__m128d>(a0), _MM_FROUND_TO_POS_INF | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtopositiveinfinity_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToPositiveInfinity");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtopositiveinfinityscalar_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToPositiveInfinityScalar");
    return dn2cpp_isa_vec<16>(_mm_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0), _MM_FROUND_TO_POS_INF | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtopositiveinfinityscalar_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToPositiveInfinityScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtopositiveinfinityscalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToPositiveInfinityScalar");
    return dn2cpp_isa_vec<16>(_mm_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _MM_FROUND_TO_POS_INF | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtopositiveinfinityscalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToPositiveInfinityScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtopositiveinfinityscalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToPositiveInfinityScalar");
    return dn2cpp_isa_vec<16>(_mm_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0), _MM_FROUND_TO_POS_INF | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtopositiveinfinityscalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToPositiveInfinityScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtopositiveinfinityscalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToPositiveInfinityScalar");
    return dn2cpp_isa_vec<16>(_mm_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _MM_FROUND_TO_POS_INF | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtopositiveinfinityscalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToPositiveInfinityScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtozero_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToZero");
    return dn2cpp_isa_vec<16>(_mm_round_ps(dn2cpp_isa_bits<__m128>(a0), _MM_FROUND_TO_ZERO | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtozero_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToZero");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtozero_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToZero");
    return dn2cpp_isa_vec<16>(_mm_round_pd(dn2cpp_isa_bits<__m128d>(a0), _MM_FROUND_TO_ZERO | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtozero_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToZero");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtozeroscalar_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToZeroScalar");
    return dn2cpp_isa_vec<16>(_mm_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0), _MM_FROUND_TO_ZERO | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtozeroscalar_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToZeroScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtozeroscalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToZeroScalar");
    return dn2cpp_isa_vec<16>(_mm_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _MM_FROUND_TO_ZERO | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtozeroscalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToZeroScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtozeroscalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToZeroScalar");
    return dn2cpp_isa_vec<16>(_mm_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0), _MM_FROUND_TO_ZERO | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtozeroscalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToZeroScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtozeroscalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.RoundToZeroScalar");
    return dn2cpp_isa_vec<16>(_mm_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _MM_FROUND_TO_ZERO | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_roundtozeroscalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.RoundToZeroScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testc_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestC");
    return _mm_testc_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testc_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testc_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestC");
    return _mm_testc_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testc_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testc_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestC");
    return _mm_testc_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testc_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testc_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestC");
    return _mm_testc_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testc_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testc_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestC");
    return _mm_testc_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testc_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testc_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestC");
    return _mm_testc_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testc_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testc_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestC");
    return _mm_testc_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testc_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testc_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestC");
    return _mm_testc_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testc_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testnotzandnotc_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestNotZAndNotC");
    return _mm_testnzc_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testnotzandnotc_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testnotzandnotc_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestNotZAndNotC");
    return _mm_testnzc_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testnotzandnotc_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testnotzandnotc_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestNotZAndNotC");
    return _mm_testnzc_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testnotzandnotc_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testnotzandnotc_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestNotZAndNotC");
    return _mm_testnzc_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testnotzandnotc_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testnotzandnotc_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestNotZAndNotC");
    return _mm_testnzc_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testnotzandnotc_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testnotzandnotc_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestNotZAndNotC");
    return _mm_testnzc_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testnotzandnotc_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testnotzandnotc_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestNotZAndNotC");
    return _mm_testnzc_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testnotzandnotc_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testnotzandnotc_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestNotZAndNotC");
    return _mm_testnzc_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testnotzandnotc_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testz_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestZ");
    return _mm_testz_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testz_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testz_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestZ");
    return _mm_testz_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testz_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testz_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestZ");
    return _mm_testz_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testz_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testz_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestZ");
    return _mm_testz_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testz_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testz_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestZ");
    return _mm_testz_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testz_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testz_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestZ");
    return _mm_testz_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testz_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testz_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestZ");
    return _mm_testz_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testz_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testz_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41, "System.Runtime.Intrinsics.X86.Sse41.TestZ");
    return _mm_testz_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse41_testz_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41.TestZ");
}
#endif
