#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx512F+VL: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_abs_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Abs");
    return dn2cpp_isa_vec<16>(_mm_abs_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_abs_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Abs");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_abs_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Abs");
    return dn2cpp_isa_vec<32>(_mm256_abs_epi64(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_abs_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Abs");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_alignright32_v128i32_v128i32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.AlignRight32");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_alignr_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & (4 - 1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_alignright32_v128i32_v128i32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.AlignRight32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_alignright32_v128u32_v128u32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.AlignRight32");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_alignr_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & (4 - 1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_alignright32_v128u32_v128u32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.AlignRight32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_alignright32_v256i32_v256i32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.AlignRight32");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_alignr_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & (8 - 1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_alignright32_v256i32_v256i32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.AlignRight32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_alignright32_v256u32_v256u32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.AlignRight32");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_alignr_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & (8 - 1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_alignright32_v256u32_v256u32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.AlignRight32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_alignright64_v128i64_v128i64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.AlignRight64");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_alignr_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & (2 - 1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_alignright64_v128i64_v128i64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.AlignRight64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_alignright64_v128u64_v128u64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.AlignRight64");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_alignr_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & (2 - 1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_alignright64_v128u64_v128u64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.AlignRight64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_alignright64_v256i64_v256i64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.AlignRight64");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_alignr_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & (4 - 1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_alignright64_v256i64_v256i64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.AlignRight64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_alignright64_v256u64_v256u64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.AlignRight64");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_alignr_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & (4 - 1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_alignright64_v256u64_v256u64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.AlignRight64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_blendvariable_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_ps(_mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_blendvariable_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_blendvariable_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_pd(_mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_blendvariable_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_blendvariable_v128i32_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_epi32(_mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_blendvariable_v128i32_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_blendvariable_v128i64_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_epi64(_mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_blendvariable_v128i64_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_blendvariable_v128u32_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_epi32(_mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_blendvariable_v128u32_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_blendvariable_v128u64_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_epi64(_mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_blendvariable_v128u64_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_blendvariable_v256f32_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_ps(_mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_blendvariable_v256f32_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_blendvariable_v256f64_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_pd(_mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_blendvariable_v256f64_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_blendvariable_v256i32_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_epi32(_mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_blendvariable_v256i32_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_blendvariable_v256i64_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_epi64(_mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_blendvariable_v256i64_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_blendvariable_v256u32_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_epi32(_mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_blendvariable_v256u32_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_blendvariable_v256u64_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_epi64(_mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_blendvariable_v256u64_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compare_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Compare");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compare_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Compare");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compare_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Compare");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compare_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Compare");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compare_v256f32_v256f32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Compare");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compare_v256f32_v256f32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Compare");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compare_v256f64_v256f64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Compare");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compare_v256f64_v256f64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Compare");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_EQ_OQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_EQ_OQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareequal_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmpeq_epi32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareequal_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareequal_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmpeq_epi64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareequal_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareequal_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmpeq_epu32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareequal_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareequal_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmpeq_epu64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareequal_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareequal_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_EQ_OQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareequal_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareequal_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_EQ_OQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareequal_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareequal_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmpeq_epi32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareequal_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareequal_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmpeq_epi64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareequal_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareequal_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmpeq_epu32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareequal_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareequal_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmpeq_epu64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareequal_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_GT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_GT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmpgt_epi32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmpgt_epi64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmpgt_epu32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmpgt_epu64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_GT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_GT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmpgt_epi32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmpgt_epi64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmpgt_epu32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmpgt_epu64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthan_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_GE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_GE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmpge_epi32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmpge_epi64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmpge_epu32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmpge_epu64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_GE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_GE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmpge_epi32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmpge_epi64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmpge_epu32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmpge_epu64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparegreaterthanorequal_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_LT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_LT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmplt_epi32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmplt_epi64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmplt_epu32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmplt_epu64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_LT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_LT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmplt_epi32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmplt_epi64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmplt_epu32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmplt_epu64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthan_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_LE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_LE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmple_epi32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmple_epi64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmple_epu32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmple_epu64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_LE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_LE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmple_epi32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmple_epi64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmple_epu32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmple_epu64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparelessthanorequal_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_NEQ_UQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_NEQ_UQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmpneq_epi32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmpneq_epi64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmpneq_epu32_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmpneq_epu64_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_NEQ_UQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_NEQ_UQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmpneq_epi32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmpneq_epi64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmpneq_epu32_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmpneq_epu64_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotequal_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotgreaterthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_NGT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotgreaterthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotgreaterthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_NGT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotgreaterthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotgreaterthan_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_NGT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotgreaterthan_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotgreaterthan_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_NGT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotgreaterthan_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotgreaterthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_NGE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotgreaterthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotgreaterthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_NGE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotgreaterthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotgreaterthanorequal_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_NGE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotgreaterthanorequal_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotgreaterthanorequal_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_NGE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotgreaterthanorequal_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotlessthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_NLT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotlessthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotlessthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_NLT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotlessthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotlessthan_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_NLT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotlessthan_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotlessthan_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_NLT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotlessthan_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotlessthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_NLE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotlessthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotlessthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_NLE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_comparenotlessthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotlessthanorequal_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_NLE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotlessthanorequal_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotlessthanorequal_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_NLE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_comparenotlessthanorequal_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareNotLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareordered_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareOrdered");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_ORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareordered_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareOrdered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareordered_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareOrdered");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_ORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareordered_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareOrdered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareordered_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareOrdered");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_ORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareordered_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareOrdered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareordered_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareOrdered");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_ORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareordered_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareOrdered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareunordered_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareUnordered");
    return dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_cmp_ps_mask(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), _CMP_UNORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareunordered_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareUnordered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareunordered_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareUnordered");
    return dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_cmp_pd_mask(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), _CMP_UNORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compareunordered_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareUnordered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareunordered_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareUnordered");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_cmp_ps_mask(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_UNORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareunordered_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareUnordered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareunordered_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompareUnordered");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_cmp_pd_mask(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_UNORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compareunordered_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompareUnordered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compress_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_ps(dn2cpp_isa_bits<__m128>(a0), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compress_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compress_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_pd(dn2cpp_isa_bits<__m128d>(a0), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compress_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compress_v128i32_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_epi32(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compress_v128i32_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compress_v128i64_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_epi64(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compress_v128i64_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compress_v128u32_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_epi32(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compress_v128u32_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compress_v128u64_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_epi64(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_compress_v128u64_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compress_v256f32_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_ps(dn2cpp_isa_bits<__m256>(a0), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compress_v256f32_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compress_v256f64_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_pd(dn2cpp_isa_bits<__m256d>(a0), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compress_v256f64_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compress_v256i32_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_epi32(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compress_v256i32_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compress_v256i64_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_epi64(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compress_v256i64_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compress_v256u32_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_epi32(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compress_v256u32_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compress_v256u64_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_epi64(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_compress_v256u64_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pf32_v128f32_v128f32(float* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
    _mm_mask_compressstoreu_ps((void*)a0, _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pf32_v128f32_v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pf32_v256f32_v256f32(float* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
    _mm256_mask_compressstoreu_ps((void*)a0, _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pf32_v256f32_v256f32(float*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pf64_v128f64_v128f64(double* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
    _mm_mask_compressstoreu_pd((void*)a0, _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128d>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pf64_v128f64_v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pf64_v256f64_v256f64(double* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
    _mm256_mask_compressstoreu_pd((void*)a0, _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256d>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pf64_v256f64_v256f64(double*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pi32_v128i32_v128i32(int32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
    _mm_mask_compressstoreu_epi32((void*)a0, _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pi32_v128i32_v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pi32_v256i32_v256i32(int32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
    _mm256_mask_compressstoreu_epi32((void*)a0, _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pi32_v256i32_v256i32(int32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pi64_v128i64_v128i64(int64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
    _mm_mask_compressstoreu_epi64((void*)a0, _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pi64_v128i64_v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pi64_v256i64_v256i64(int64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
    _mm256_mask_compressstoreu_epi64((void*)a0, _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pi64_v256i64_v256i64(int64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pu32_v128u32_v128u32(uint32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
    _mm_mask_compressstoreu_epi32((void*)a0, _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pu32_v128u32_v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pu32_v256u32_v256u32(uint32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
    _mm256_mask_compressstoreu_epi32((void*)a0, _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pu32_v256u32_v256u32(uint32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pu64_v128u64_v128u64(uint64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
    _mm_mask_compressstoreu_epi64((void*)a0, _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pu64_v128u64_v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pu64_v256u64_v256u64(uint64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
    _mm256_mask_compressstoreu_epi64((void*)a0, _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_compressstore_pu64_v256u64_v256u64(uint64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128byte_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128byte_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128byte_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128byte_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128byte_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128byte_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128byte_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128byte_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128byte_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi32_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128byte_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128byte_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128byte_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128byte_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi32_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128byte_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128byte_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128byte_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128bytewithsaturation_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128ByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtusepi32_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128bytewithsaturation_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128ByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128bytewithsaturation_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128ByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtusepi64_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128bytewithsaturation_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128ByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128bytewithsaturation_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128ByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtusepi32_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128bytewithsaturation_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128ByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128bytewithsaturation_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128ByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtusepi64_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128bytewithsaturation_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128ByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128double_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Double");
    return dn2cpp_isa_vec<16>(_mm_cvtepu32_pd(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128double_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi32_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi32_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16withsaturation_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16WithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtsepi32_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16withsaturation_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16withsaturation_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16WithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtsepi64_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16withsaturation_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16withsaturation_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16WithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtsepi32_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16withsaturation_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16withsaturation_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16WithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtsepi64_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int16withsaturation_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int32_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int32_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int32_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int32_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int32_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int32_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int32_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int32_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int32withsaturation_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int32WithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtsepi64_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int32withsaturation_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int32WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int32withsaturation_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int32WithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtsepi64_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128int32withsaturation_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Int32WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbyte_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbyte_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbyte_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbyte_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbyte_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbyte_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbyte_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbyte_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbyte_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi32_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbyte_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbyte_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbyte_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbyte_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi32_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbyte_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbyte_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbyte_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbytewithsaturation_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtsepi32_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbytewithsaturation_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbytewithsaturation_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtsepi64_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbytewithsaturation_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbytewithsaturation_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtsepi32_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbytewithsaturation_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbytewithsaturation_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtsepi64_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128sbytewithsaturation_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128SByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128single_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Single");
    return dn2cpp_isa_vec<16>(_mm_cvtepu32_ps(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128single_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi32_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi32_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16withsaturation_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16WithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtusepi32_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16withsaturation_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16withsaturation_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16WithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtusepi64_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16withsaturation_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16withsaturation_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16WithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtusepi32_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16withsaturation_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16withsaturation_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16WithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtusepi64_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint16withsaturation_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32");
    return dn2cpp_isa_vec<16>(_mm_cvtps_epu32(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32");
    return dn2cpp_isa_vec<16>(_mm_cvtpd_epu32(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32");
    return dn2cpp_isa_vec<16>(_mm256_cvtpd_epu32(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32withsaturation_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32WithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtusepi64_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32withsaturation_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32withsaturation_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32WithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtusepi64_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32withsaturation_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32withtruncation_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32WithTruncation");
    return dn2cpp_isa_vec<16>(_mm_cvttps_epu32(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32withtruncation_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32withtruncation_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32WithTruncation");
    return dn2cpp_isa_vec<16>(_mm_cvttpd_epu32(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32withtruncation_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32withtruncation_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32WithTruncation");
    return dn2cpp_isa_vec<16>(_mm256_cvttpd_epu32(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_converttovector128uint32withtruncation_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector128UInt32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_converttovector256double_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector256Double");
    return dn2cpp_isa_vec<32>(_mm256_cvtepu32_pd(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_converttovector256double_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector256Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_converttovector256single_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector256Single");
    return dn2cpp_isa_vec<32>(_mm256_cvtepu32_ps(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_converttovector256single_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector256Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_converttovector256uint32_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector256UInt32");
    return dn2cpp_isa_vec<32>(_mm256_cvtps_epu32(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_converttovector256uint32_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector256UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_converttovector256uint32withtruncation_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector256UInt32WithTruncation");
    return dn2cpp_isa_vec<32>(_mm256_cvttps_epu32(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_converttovector256uint32withtruncation_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ConvertToVector256UInt32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expand_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_ps(dn2cpp_isa_bits<__m128>(a0), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expand_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expand_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_pd(dn2cpp_isa_bits<__m128d>(a0), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expand_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expand_v128i32_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_epi32(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expand_v128i32_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expand_v128i64_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_epi64(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expand_v128i64_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expand_v128u32_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_epi32(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expand_v128u32_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expand_v128u64_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_epi64(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expand_v128u64_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expand_v256f32_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_ps(dn2cpp_isa_bits<__m256>(a0), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expand_v256f32_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expand_v256f64_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_pd(dn2cpp_isa_bits<__m256d>(a0), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expand_v256f64_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expand_v256i32_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_epi32(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expand_v256i32_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expand_v256i64_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_epi64(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expand_v256i64_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expand_v256u32_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_epi32(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expand_v256u32_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expand_v256u64_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_epi64(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expand_v256u64_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expandload_pf32_v128f32_v128f32(float* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_ps(dn2cpp_isa_bits<__m128>(a2), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expandload_pf32_v128f32_v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expandload_pf32_v256f32_v256f32(float* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_ps(dn2cpp_isa_bits<__m256>(a2), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expandload_pf32_v256f32_v256f32(float*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expandload_pf64_v128f64_v128f64(double* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_pd(dn2cpp_isa_bits<__m128d>(a2), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expandload_pf64_v128f64_v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expandload_pf64_v256f64_v256f64(double* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_pd(dn2cpp_isa_bits<__m256d>(a2), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expandload_pf64_v256f64_v256f64(double*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expandload_pi32_v128i32_v128i32(int32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_epi32(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expandload_pi32_v128i32_v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expandload_pi32_v256i32_v256i32(int32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_epi32(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expandload_pi32_v256i32_v256i32(int32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expandload_pi64_v128i64_v128i64(int64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_epi64(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expandload_pi64_v128i64_v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expandload_pi64_v256i64_v256i64(int64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_epi64(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expandload_pi64_v256i64_v256i64(int64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expandload_pu32_v128u32_v128u32(uint32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_epi32(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expandload_pu32_v128u32_v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expandload_pu32_v256u32_v256u32(uint32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_epi32(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expandload_pu32_v256u32_v256u32(uint32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expandload_pu64_v128u64_v128u64(uint64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_epi64(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_expandload_pu64_v128u64_v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expandload_pu64_v256u64_v256u64(uint64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_epi64(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_expandload_pu64_v256u64_v256u64(uint64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_fixup_v128f32_v128f32_v128i32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Fixup");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_fixupimm_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_fixup_v128f32_v128f32_v128i32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Fixup");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_fixup_v128f64_v128f64_v128i64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Fixup");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_fixupimm_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_fixup_v128f64_v128f64_v128i64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Fixup");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_fixup_v256f32_v256f32_v256i32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Fixup");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_fixupimm_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_fixup_v256f32_v256f32_v256i32_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Fixup");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_fixup_v256f64_v256f64_v256i64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Fixup");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_fixupimm_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_fixup_v256f64_v256f64_v256i64_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Fixup");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_getexponent_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.GetExponent");
    return dn2cpp_isa_vec<16>(_mm_getexp_ps(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_getexponent_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.GetExponent");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_getexponent_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.GetExponent");
    return dn2cpp_isa_vec<16>(_mm_getexp_pd(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_getexponent_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.GetExponent");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_getexponent_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.GetExponent");
    return dn2cpp_isa_vec<32>(_mm256_getexp_ps(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_getexponent_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.GetExponent");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_getexponent_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.GetExponent");
    return dn2cpp_isa_vec<32>(_mm256_getexp_pd(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_getexponent_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.GetExponent");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_getmantissa_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.GetMantissa");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<16>(_mm_getmant_ps(dn2cpp_isa_bits<__m128>(a0), (_MM_MANTISSA_NORM_ENUM)(DN2CPP_IMM & 3), (_MM_MANTISSA_SIGN_ENUM)(DN2CPP_IMM >> 2))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_getmantissa_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.GetMantissa");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_getmantissa_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.GetMantissa");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<16>(_mm_getmant_pd(dn2cpp_isa_bits<__m128d>(a0), (_MM_MANTISSA_NORM_ENUM)(DN2CPP_IMM & 3), (_MM_MANTISSA_SIGN_ENUM)(DN2CPP_IMM >> 2))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_getmantissa_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.GetMantissa");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_getmantissa_v256f32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.GetMantissa");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<32>(_mm256_getmant_ps(dn2cpp_isa_bits<__m256>(a0), (_MM_MANTISSA_NORM_ENUM)(DN2CPP_IMM & 3), (_MM_MANTISSA_SIGN_ENUM)(DN2CPP_IMM >> 2))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_getmantissa_v256f32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.GetMantissa");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_getmantissa_v256f64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.GetMantissa");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<32>(_mm256_getmant_pd(dn2cpp_isa_bits<__m256d>(a0), (_MM_MANTISSA_NORM_ENUM)(DN2CPP_IMM & 3), (_MM_MANTISSA_SIGN_ENUM)(DN2CPP_IMM >> 2))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_getmantissa_v256f64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.GetMantissa");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskload_pf32_v128f32_v128f32(float* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_ps(dn2cpp_isa_bits<__m128>(a2), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskload_pf32_v128f32_v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskload_pf32_v256f32_v256f32(float* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_ps(dn2cpp_isa_bits<__m256>(a2), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskload_pf32_v256f32_v256f32(float*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskload_pf64_v128f64_v128f64(double* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_pd(dn2cpp_isa_bits<__m128d>(a2), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskload_pf64_v128f64_v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskload_pf64_v256f64_v256f64(double* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_pd(dn2cpp_isa_bits<__m256d>(a2), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskload_pf64_v256f64_v256f64(double*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskload_pi32_v128i32_v128i32(int32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_epi32(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskload_pi32_v128i32_v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskload_pi32_v256i32_v256i32(int32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_epi32(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskload_pi32_v256i32_v256i32(int32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskload_pi64_v128i64_v128i64(int64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_epi64(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskload_pi64_v128i64_v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskload_pi64_v256i64_v256i64(int64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_epi64(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskload_pi64_v256i64_v256i64(int64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskload_pu32_v128u32_v128u32(uint32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_epi32(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskload_pu32_v128u32_v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskload_pu32_v256u32_v256u32(uint32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_epi32(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskload_pu32_v256u32_v256u32(uint32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskload_pu64_v128u64_v128u64(uint64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_epi64(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskload_pu64_v128u64_v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskload_pu64_v256u64_v256u64(uint64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_epi64(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskload_pu64_v256u64_v256u64(uint64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pf32_v128f32_v128f32(float* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
    return dn2cpp_isa_vec<16>(_mm_mask_load_ps(dn2cpp_isa_bits<__m128>(a2), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pf32_v128f32_v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pf32_v256f32_v256f32(float* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
    return dn2cpp_isa_vec<32>(_mm256_mask_load_ps(dn2cpp_isa_bits<__m256>(a2), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pf32_v256f32_v256f32(float*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pf64_v128f64_v128f64(double* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
    return dn2cpp_isa_vec<16>(_mm_mask_load_pd(dn2cpp_isa_bits<__m128d>(a2), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pf64_v128f64_v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pf64_v256f64_v256f64(double* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
    return dn2cpp_isa_vec<32>(_mm256_mask_load_pd(dn2cpp_isa_bits<__m256d>(a2), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pf64_v256f64_v256f64(double*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pi32_v128i32_v128i32(int32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
    return dn2cpp_isa_vec<16>(_mm_mask_load_epi32(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pi32_v128i32_v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pi32_v256i32_v256i32(int32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
    return dn2cpp_isa_vec<32>(_mm256_mask_load_epi32(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pi32_v256i32_v256i32(int32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pi64_v128i64_v128i64(int64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
    return dn2cpp_isa_vec<16>(_mm_mask_load_epi64(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pi64_v128i64_v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pi64_v256i64_v256i64(int64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
    return dn2cpp_isa_vec<32>(_mm256_mask_load_epi64(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pi64_v256i64_v256i64(int64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pu32_v128u32_v128u32(uint32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
    return dn2cpp_isa_vec<16>(_mm_mask_load_epi32(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pu32_v128u32_v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pu32_v256u32_v256u32(uint32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
    return dn2cpp_isa_vec<32>(_mm256_mask_load_epi32(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pu32_v256u32_v256u32(uint32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pu64_v128u64_v128u64(uint64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
    return dn2cpp_isa_vec<16>(_mm_mask_load_epi64(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pu64_v128u64_v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pu64_v256u64_v256u64(uint64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
    return dn2cpp_isa_vec<32>(_mm256_mask_load_epi64(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_maskloadaligned_pu64_v256u64_v256u64(uint64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pf32_v128f32_v128f32(float* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
    _mm_mask_storeu_ps((void*)a0, _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pf32_v128f32_v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pf32_v256f32_v256f32(float* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
    _mm256_mask_storeu_ps((void*)a0, _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pf32_v256f32_v256f32(float*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pf64_v128f64_v128f64(double* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
    _mm_mask_storeu_pd((void*)a0, _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128d>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pf64_v128f64_v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pf64_v256f64_v256f64(double* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
    _mm256_mask_storeu_pd((void*)a0, _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256d>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pf64_v256f64_v256f64(double*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pi32_v128i32_v128i32(int32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
    _mm_mask_storeu_epi32((void*)a0, _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pi32_v128i32_v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pi32_v256i32_v256i32(int32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
    _mm256_mask_storeu_epi32((void*)a0, _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pi32_v256i32_v256i32(int32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pi64_v128i64_v128i64(int64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
    _mm_mask_storeu_epi64((void*)a0, _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pi64_v128i64_v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pi64_v256i64_v256i64(int64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
    _mm256_mask_storeu_epi64((void*)a0, _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pi64_v256i64_v256i64(int64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pu32_v128u32_v128u32(uint32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
    _mm_mask_storeu_epi32((void*)a0, _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pu32_v128u32_v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pu32_v256u32_v256u32(uint32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
    _mm256_mask_storeu_epi32((void*)a0, _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pu32_v256u32_v256u32(uint32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pu64_v128u64_v128u64(uint64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
    _mm_mask_storeu_epi64((void*)a0, _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pu64_v128u64_v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pu64_v256u64_v256u64(uint64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
    _mm256_mask_storeu_epi64((void*)a0, _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstore_pu64_v256u64_v256u64(uint64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pf32_v128f32_v128f32(float* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
    _mm_mask_store_ps((void*)a0, _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pf32_v128f32_v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pf32_v256f32_v256f32(float* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
    _mm256_mask_store_ps((void*)a0, _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pf32_v256f32_v256f32(float*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pf64_v128f64_v128f64(double* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
    _mm_mask_store_pd((void*)a0, _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128d>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pf64_v128f64_v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pf64_v256f64_v256f64(double* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
    _mm256_mask_store_pd((void*)a0, _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256d>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pf64_v256f64_v256f64(double*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pi32_v128i32_v128i32(int32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
    _mm_mask_store_epi32((void*)a0, _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pi32_v128i32_v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pi32_v256i32_v256i32(int32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
    _mm256_mask_store_epi32((void*)a0, _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pi32_v256i32_v256i32(int32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pi64_v128i64_v128i64(int64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
    _mm_mask_store_epi64((void*)a0, _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pi64_v128i64_v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pi64_v256i64_v256i64(int64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
    _mm256_mask_store_epi64((void*)a0, _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pi64_v256i64_v256i64(int64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pu32_v128u32_v128u32(uint32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
    _mm_mask_store_epi32((void*)a0, _mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pu32_v128u32_v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pu32_v256u32_v256u32(uint32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
    _mm256_mask_store_epi32((void*)a0, _mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pu32_v256u32_v256u32(uint32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pu64_v128u64_v128u64(uint64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
    _mm_mask_store_epi64((void*)a0, _mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pu64_v128u64_v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pu64_v256u64_v256u64(uint64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
    _mm256_mask_store_epi64((void*)a0, _mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_vl_maskstorealigned_pu64_v256u64_v256u64(uint64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_max_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Max");
    return dn2cpp_isa_vec<16>(_mm_max_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_max_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_max_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Max");
    return dn2cpp_isa_vec<16>(_mm_max_epu64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_max_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_max_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Max");
    return dn2cpp_isa_vec<32>(_mm256_max_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_max_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_max_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Max");
    return dn2cpp_isa_vec<32>(_mm256_max_epu64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_max_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_min_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Min");
    return dn2cpp_isa_vec<16>(_mm_min_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_min_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_min_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Min");
    return dn2cpp_isa_vec<16>(_mm_min_epu64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_min_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_min_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Min");
    return dn2cpp_isa_vec<32>(_mm256_min_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_min_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_min_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Min");
    return dn2cpp_isa_vec<32>(_mm256_min_epu64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_min_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_permutevar2x64x2_v128f64_v128i64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar2x64x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_permutevar2x64x2_v128f64_v128i64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar2x64x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_permutevar2x64x2_v128i64_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar2x64x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_permutevar2x64x2_v128i64_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar2x64x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_permutevar2x64x2_v128u64_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar2x64x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_permutevar2x64x2_v128u64_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar2x64x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_permutevar4x32x2_v128f32_v128i32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar4x32x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_permutevar4x32x2_v128f32_v128i32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar4x32x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_permutevar4x32x2_v128i32_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar4x32x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_permutevar4x32x2_v128i32_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar4x32x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_permutevar4x32x2_v128u32_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar4x32x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_permutevar4x32x2_v128u32_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar4x32x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_permutevar4x64_v256f64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar4x64");
    return dn2cpp_isa_vec<32>(_mm256_permutexvar_pd(dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_permutevar4x64_v256f64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar4x64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_permutevar4x64_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar4x64");
    return dn2cpp_isa_vec<32>(_mm256_permutexvar_epi64(dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_permutevar4x64_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar4x64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_permutevar4x64_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar4x64");
    return dn2cpp_isa_vec<32>(_mm256_permutexvar_epi64(dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_permutevar4x64_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar4x64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_permutevar4x64x2_v256f64_v256i64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar4x64x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_permutevar4x64x2_v256f64_v256i64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar4x64x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_permutevar4x64x2_v256i64_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar4x64x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_permutevar4x64x2_v256i64_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar4x64x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_permutevar4x64x2_v256u64_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar4x64x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_permutevar4x64x2_v256u64_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar4x64x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_permutevar8x32x2_v256f32_v256i32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar8x32x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_permutevar8x32x2_v256f32_v256i32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar8x32x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_permutevar8x32x2_v256i32_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar8x32x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_permutevar8x32x2_v256i32_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar8x32x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_permutevar8x32x2_v256u32_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar8x32x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_permutevar8x32x2_v256u32_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.PermuteVar8x32x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_reciprocal14_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Reciprocal14");
    return dn2cpp_isa_vec<16>(_mm_rcp14_ps(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_reciprocal14_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Reciprocal14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_reciprocal14_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Reciprocal14");
    return dn2cpp_isa_vec<16>(_mm_rcp14_pd(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_reciprocal14_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Reciprocal14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_reciprocal14_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Reciprocal14");
    return dn2cpp_isa_vec<32>(_mm256_rcp14_ps(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_reciprocal14_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Reciprocal14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_reciprocal14_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Reciprocal14");
    return dn2cpp_isa_vec<32>(_mm256_rcp14_pd(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_reciprocal14_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Reciprocal14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_reciprocalsqrt14_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ReciprocalSqrt14");
    return dn2cpp_isa_vec<16>(_mm_rsqrt14_ps(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_reciprocalsqrt14_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ReciprocalSqrt14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_reciprocalsqrt14_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ReciprocalSqrt14");
    return dn2cpp_isa_vec<16>(_mm_rsqrt14_pd(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_reciprocalsqrt14_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ReciprocalSqrt14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_reciprocalsqrt14_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ReciprocalSqrt14");
    return dn2cpp_isa_vec<32>(_mm256_rsqrt14_ps(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_reciprocalsqrt14_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ReciprocalSqrt14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_reciprocalsqrt14_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ReciprocalSqrt14");
    return dn2cpp_isa_vec<32>(_mm256_rsqrt14_pd(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_reciprocalsqrt14_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ReciprocalSqrt14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateleft_v128i32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_rol_epi32(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateleft_v128i32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateleft_v128i64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_rol_epi64(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateleft_v128i64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateleft_v128u32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_rol_epi32(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateleft_v128u32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateleft_v128u64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_rol_epi64(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateleft_v128u64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateleft_v256i32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_rol_epi32(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateleft_v256i32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateleft_v256i64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_rol_epi64(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateleft_v256i64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateleft_v256u32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_rol_epi32(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateleft_v256u32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateleft_v256u64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_rol_epi64(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateleft_v256u64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateleftvariable_v128i32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeftVariable");
    return dn2cpp_isa_vec<16>(_mm_rolv_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateleftvariable_v128i32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateleftvariable_v128i64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeftVariable");
    return dn2cpp_isa_vec<16>(_mm_rolv_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateleftvariable_v128i64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateleftvariable_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeftVariable");
    return dn2cpp_isa_vec<16>(_mm_rolv_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateleftvariable_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateleftvariable_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeftVariable");
    return dn2cpp_isa_vec<16>(_mm_rolv_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateleftvariable_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateleftvariable_v256i32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeftVariable");
    return dn2cpp_isa_vec<32>(_mm256_rolv_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateleftvariable_v256i32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateleftvariable_v256i64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeftVariable");
    return dn2cpp_isa_vec<32>(_mm256_rolv_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateleftvariable_v256i64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateleftvariable_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeftVariable");
    return dn2cpp_isa_vec<32>(_mm256_rolv_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateleftvariable_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateleftvariable_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeftVariable");
    return dn2cpp_isa_vec<32>(_mm256_rolv_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateleftvariable_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateright_v128i32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_ror_epi32(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateright_v128i32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateright_v128i64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_ror_epi64(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateright_v128i64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateright_v128u32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_ror_epi32(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateright_v128u32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateright_v128u64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_ror_epi64(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotateright_v128u64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateright_v256i32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_ror_epi32(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateright_v256i32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateright_v256i64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_ror_epi64(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateright_v256i64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateright_v256u32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_ror_epi32(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateright_v256u32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateright_v256u64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_ror_epi64(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotateright_v256u64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotaterightvariable_v128i32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRightVariable");
    return dn2cpp_isa_vec<16>(_mm_rorv_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotaterightvariable_v128i32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotaterightvariable_v128i64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRightVariable");
    return dn2cpp_isa_vec<16>(_mm_rorv_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotaterightvariable_v128i64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotaterightvariable_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRightVariable");
    return dn2cpp_isa_vec<16>(_mm_rorv_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotaterightvariable_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotaterightvariable_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRightVariable");
    return dn2cpp_isa_vec<16>(_mm_rorv_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_rotaterightvariable_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotaterightvariable_v256i32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRightVariable");
    return dn2cpp_isa_vec<32>(_mm256_rorv_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotaterightvariable_v256i32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotaterightvariable_v256i64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRightVariable");
    return dn2cpp_isa_vec<32>(_mm256_rorv_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotaterightvariable_v256i64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotaterightvariable_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRightVariable");
    return dn2cpp_isa_vec<32>(_mm256_rorv_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotaterightvariable_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotaterightvariable_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRightVariable");
    return dn2cpp_isa_vec<32>(_mm256_rorv_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_rotaterightvariable_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_roundscale_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RoundScale");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_roundscale_ps(dn2cpp_isa_bits<__m128>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_roundscale_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RoundScale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_roundscale_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RoundScale");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_roundscale_pd(dn2cpp_isa_bits<__m128d>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_roundscale_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RoundScale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_roundscale_v256f32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RoundScale");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_roundscale_ps(dn2cpp_isa_bits<__m256>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_roundscale_v256f32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RoundScale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_roundscale_v256f64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.RoundScale");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_roundscale_pd(dn2cpp_isa_bits<__m256d>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_roundscale_v256f64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.RoundScale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_scale_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Scale");
    return dn2cpp_isa_vec<16>(_mm_scalef_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_scale_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Scale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_scale_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Scale");
    return dn2cpp_isa_vec<16>(_mm_scalef_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_scale_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Scale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_scale_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Scale");
    return dn2cpp_isa_vec<32>(_mm256_scalef_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_scale_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Scale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_scale_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Scale");
    return dn2cpp_isa_vec<32>(_mm256_scalef_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_scale_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Scale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_shiftrightarithmetic_v128i64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ShiftRightArithmetic");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_srai_epi64(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_shiftrightarithmetic_v128i64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_shiftrightarithmetic_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ShiftRightArithmetic");
    return dn2cpp_isa_vec<16>(_mm_sra_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_shiftrightarithmetic_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_shiftrightarithmetic_v256i64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ShiftRightArithmetic");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_srai_epi64(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_shiftrightarithmetic_v256i64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_shiftrightarithmetic_v256i64_v128i64(const Dn2CppVector256& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ShiftRightArithmetic");
    return dn2cpp_isa_vec<32>(_mm256_sra_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_shiftrightarithmetic_v256i64_v128i64(const Dn2CppVector256&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_shiftrightarithmeticvariable_v128i64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ShiftRightArithmeticVariable");
    return dn2cpp_isa_vec<16>(_mm_srav_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_shiftrightarithmeticvariable_v128i64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ShiftRightArithmeticVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_shiftrightarithmeticvariable_v256i64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.ShiftRightArithmeticVariable");
    return dn2cpp_isa_vec<32>(_mm256_srav_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_shiftrightarithmeticvariable_v256i64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.ShiftRightArithmeticVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_shuffle2x128_v256f32_v256f32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Shuffle2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_shuffle_f32x4(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_shuffle2x128_v256f32_v256f32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Shuffle2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_shuffle2x128_v256f64_v256f64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Shuffle2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_shuffle_f64x2(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_shuffle2x128_v256f64_v256f64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Shuffle2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_shuffle2x128_v256i32_v256i32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Shuffle2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_shuffle_i32x4(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_shuffle2x128_v256i32_v256i32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Shuffle2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_shuffle2x128_v256i64_v256i64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Shuffle2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_shuffle_i64x2(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_shuffle2x128_v256i64_v256i64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Shuffle2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_shuffle2x128_v256u32_v256u32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Shuffle2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_shuffle_i32x4(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_shuffle2x128_v256u32_v256u32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Shuffle2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_shuffle2x128_v256u64_v256u64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.Shuffle2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_shuffle_i64x2(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_shuffle2x128_v256u64_v256u64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.Shuffle2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128f32_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128f32_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128f64_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128f64_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128i16_v128i16_v128i16_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128i16_v128i16_v128i16_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128i32_v128i32_v128i32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128i32_v128i32_v128i32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128i64_v128i64_v128i64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128i64_v128i64_v128i64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128i8_v128i8_v128i8_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128i8_v128i8_v128i8_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128u16_v128u16_v128u16_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128u16_v128u16_v128u16_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128u32_v128u32_v128u32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128u32_v128u32_v128u32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128u64_v128u64_v128u64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128u64_v128u64_v128u64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128u8_v128u8_v128u8_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_ternarylogic_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v128u8_v128u8_v128u8_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256f32_v256f32_v256f32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256f32_v256f32_v256f32_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256f64_v256f64_v256f64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256f64_v256f64_v256f64_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256i16_v256i16_v256i16_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256i16_v256i16_v256i16_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256i32_v256i32_v256i32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256i32_v256i32_v256i32_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256i64_v256i64_v256i64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256i64_v256i64_v256i64_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256i8_v256i8_v256i8_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256i8_v256i8_v256i8_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256u16_v256u16_v256u16_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256u16_v256u16_v256u16_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256u32_v256u32_v256u32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256u32_v256u32_v256u32_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256u64_v256u64_v256u64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256u64_v256u64_v256u64_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256u8_v256u8_v256u8_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F_VL, "System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<32>(_mm256_ternarylogic_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_vl_ternarylogic_v256u8_v256u8_v256u8_u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F+VL.TernaryLogic");
}
#endif
