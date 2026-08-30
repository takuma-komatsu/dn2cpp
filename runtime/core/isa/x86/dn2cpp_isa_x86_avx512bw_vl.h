#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx512BW+VL: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_blendvariable_v128i16_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_epi8(_mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_blendvariable_v128i16_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_blendvariable_v128i8_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_epi8(_mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_blendvariable_v128i8_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_blendvariable_v128u16_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_epi8(_mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_blendvariable_v128u16_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_blendvariable_v128u8_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.BlendVariable");
    return dn2cpp_isa_vec<16>(_mm_mask_blend_epi8(_mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a2)), dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_blendvariable_v128u8_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_blendvariable_v256i16_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_epi8(_mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_blendvariable_v256i16_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_blendvariable_v256i8_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_epi8(_mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_blendvariable_v256i8_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_blendvariable_v256u16_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_epi8(_mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_blendvariable_v256u16_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_blendvariable_v256u8_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_mask_blend_epi8(_mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a2)), dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_blendvariable_v256u8_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_compareequal_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmpeq_epi16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_compareequal_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_compareequal_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmpeq_epi8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_compareequal_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_compareequal_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmpeq_epu16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_compareequal_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_compareequal_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmpeq_epu8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_compareequal_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_compareequal_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmpeq_epi16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_compareequal_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_compareequal_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmpeq_epi8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_compareequal_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_compareequal_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmpeq_epu16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_compareequal_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_compareequal_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmpeq_epu8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_compareequal_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthan_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmpgt_epi16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthan_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthan_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmpgt_epi8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthan_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthan_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmpgt_epu16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthan_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthan_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmpgt_epu8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthan_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthan_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmpgt_epi16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthan_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthan_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmpgt_epi8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthan_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthan_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmpgt_epu16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthan_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthan_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmpgt_epu8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthan_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthanorequal_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmpge_epi16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthanorequal_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthanorequal_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmpge_epi8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthanorequal_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthanorequal_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmpge_epu16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthanorequal_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthanorequal_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmpge_epu8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthanorequal_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthanorequal_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmpge_epi16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthanorequal_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthanorequal_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmpge_epi8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthanorequal_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthanorequal_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmpge_epu16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthanorequal_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthanorequal_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmpge_epu8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparegreaterthanorequal_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparelessthan_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmplt_epi16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparelessthan_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparelessthan_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmplt_epi8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparelessthan_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparelessthan_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmplt_epu16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparelessthan_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparelessthan_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmplt_epu8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparelessthan_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparelessthan_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmplt_epi16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparelessthan_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparelessthan_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmplt_epi8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparelessthan_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparelessthan_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmplt_epu16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparelessthan_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparelessthan_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmplt_epu8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparelessthan_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparelessthanorequal_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmple_epi16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparelessthanorequal_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparelessthanorequal_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmple_epi8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparelessthanorequal_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparelessthanorequal_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmple_epu16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparelessthanorequal_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparelessthanorequal_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmple_epu8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparelessthanorequal_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparelessthanorequal_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmple_epi16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparelessthanorequal_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparelessthanorequal_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmple_epi8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparelessthanorequal_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparelessthanorequal_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmple_epu16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparelessthanorequal_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparelessthanorequal_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmple_epu8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparelessthanorequal_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparenotequal_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmpneq_epi16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparenotequal_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparenotequal_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmpneq_epi8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparenotequal_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparenotequal_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi16(_mm_cmpneq_epu16_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparenotequal_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparenotequal_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_movm_epi8(_mm_cmpneq_epu8_mask(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_comparenotequal_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparenotequal_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmpneq_epi16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparenotequal_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparenotequal_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmpneq_epi8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparenotequal_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparenotequal_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi16(_mm256_cmpneq_epu16_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparenotequal_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparenotequal_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_movm_epi8(_mm256_cmpneq_epu8_mask(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_comparenotequal_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128byte_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi16_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128byte_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128byte_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi16_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128byte_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128byte_v256i16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi16_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128byte_v256i16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128byte_v256u16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi16_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128byte_v256u16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128bytewithsaturation_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128ByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtusepi16_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128bytewithsaturation_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128ByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128bytewithsaturation_v256u16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128ByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtusepi16_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128bytewithsaturation_v256u16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128ByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128sbyte_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi16_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128sbyte_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128sbyte_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm_cvtepi16_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128sbyte_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128sbyte_v256i16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi16_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128sbyte_v256i16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128sbyte_v256u16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi16_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128sbyte_v256u16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128sbytewithsaturation_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128SByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm_cvtsepi16_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128sbytewithsaturation_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128SByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128sbytewithsaturation_v256i16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128SByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm256_cvtsepi16_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_converttovector128sbytewithsaturation_v256i16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ConvertToVector128SByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_maskload_pi16_v128i16_v128i16(int16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_epi16(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_maskload_pi16_v128i16_v128i16(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_maskload_pi16_v256i16_v256i16(int16_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_epi16(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_maskload_pi16_v256i16_v256i16(int16_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_maskload_pi8_v128i8_v128i8(int8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_epi8(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_maskload_pi8_v128i8_v128i8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_maskload_pi8_v256i8_v256i8(int8_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_epi8(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_maskload_pi8_v256i8_v256i8(int8_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_maskload_pu16_v128u16_v128u16(uint16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_epi16(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_maskload_pu16_v128u16_v128u16(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_maskload_pu16_v256u16_v256u16(uint16_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_epi16(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_maskload_pu16_v256u16_v256u16(uint16_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_maskload_pu8_v128u8_v128u8(uint8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_loadu_epi8(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_maskload_pu8_v128u8_v128u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_maskload_pu8_v256u8_v256u8(uint8_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_loadu_epi8(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_maskload_pu8_v256u8_v256u8(uint8_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_vl_maskstore_pi16_v128i16_v128i16(int16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskStore");
    _mm_mask_storeu_epi16((void*)a0, _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_vl_maskstore_pi16_v128i16_v128i16(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_vl_maskstore_pi16_v256i16_v256i16(int16_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskStore");
    _mm256_mask_storeu_epi16((void*)a0, _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_vl_maskstore_pi16_v256i16_v256i16(int16_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_vl_maskstore_pi8_v128i8_v128i8(int8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskStore");
    _mm_mask_storeu_epi8((void*)a0, _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_vl_maskstore_pi8_v128i8_v128i8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_vl_maskstore_pi8_v256i8_v256i8(int8_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskStore");
    _mm256_mask_storeu_epi8((void*)a0, _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_vl_maskstore_pi8_v256i8_v256i8(int8_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_vl_maskstore_pu16_v128u16_v128u16(uint16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskStore");
    _mm_mask_storeu_epi16((void*)a0, _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_vl_maskstore_pu16_v128u16_v128u16(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_vl_maskstore_pu16_v256u16_v256u16(uint16_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskStore");
    _mm256_mask_storeu_epi16((void*)a0, _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_vl_maskstore_pu16_v256u16_v256u16(uint16_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_vl_maskstore_pu8_v128u8_v128u8(uint8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskStore");
    _mm_mask_storeu_epi8((void*)a0, _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_vl_maskstore_pu8_v128u8_v128u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_vl_maskstore_pu8_v256u8_v256u8(uint8_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskStore");
    _mm256_mask_storeu_epi8((void*)a0, _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_vl_maskstore_pu8_v256u8_v256u8(uint8_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_permutevar16x16_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.PermuteVar16x16");
    return dn2cpp_isa_vec<32>(_mm256_permutexvar_epi16(dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_permutevar16x16_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.PermuteVar16x16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_permutevar16x16_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.PermuteVar16x16");
    return dn2cpp_isa_vec<32>(_mm256_permutexvar_epi16(dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_permutevar16x16_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.PermuteVar16x16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_permutevar16x16x2_v256i16_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.PermuteVar16x16x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_permutevar16x16x2_v256i16_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.PermuteVar16x16x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_permutevar16x16x2_v256u16_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.PermuteVar16x16x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_permutevar16x16x2_v256u16_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.PermuteVar16x16x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_permutevar8x16_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.PermuteVar8x16");
    return dn2cpp_isa_vec<16>(_mm_permutexvar_epi16(dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_permutevar8x16_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.PermuteVar8x16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_permutevar8x16_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.PermuteVar8x16");
    return dn2cpp_isa_vec<16>(_mm_permutexvar_epi16(dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_permutevar8x16_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.PermuteVar8x16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_permutevar8x16x2_v128i16_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.PermuteVar8x16x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_permutevar8x16x2_v128i16_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.PermuteVar8x16x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_permutevar8x16x2_v128u16_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.PermuteVar8x16x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_permutevar8x16x2_v128u16_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.PermuteVar8x16x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_shiftleftlogicalvariable_v128i16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<16>(_mm_sllv_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_shiftleftlogicalvariable_v128i16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_shiftleftlogicalvariable_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<16>(_mm_sllv_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_shiftleftlogicalvariable_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_shiftleftlogicalvariable_v256i16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<32>(_mm256_sllv_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_shiftleftlogicalvariable_v256i16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_shiftleftlogicalvariable_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<32>(_mm256_sllv_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_shiftleftlogicalvariable_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_shiftrightarithmeticvariable_v128i16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftRightArithmeticVariable");
    return dn2cpp_isa_vec<16>(_mm_srav_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_shiftrightarithmeticvariable_v128i16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftRightArithmeticVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_shiftrightarithmeticvariable_v256i16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftRightArithmeticVariable");
    return dn2cpp_isa_vec<32>(_mm256_srav_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_shiftrightarithmeticvariable_v256i16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftRightArithmeticVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_shiftrightlogicalvariable_v128i16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<16>(_mm_srlv_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_shiftrightlogicalvariable_v128i16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_shiftrightlogicalvariable_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<16>(_mm_srlv_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_shiftrightlogicalvariable_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_shiftrightlogicalvariable_v256i16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<32>(_mm256_srlv_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_shiftrightlogicalvariable_v256i16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_shiftrightlogicalvariable_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<32>(_mm256_srlv_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_shiftrightlogicalvariable_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_sumabsolutedifferencesinblock32_v128u8_v128u8_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.SumAbsoluteDifferencesInBlock32");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_dbsad_epu8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512bw_vl_sumabsolutedifferencesinblock32_v128u8_v128u8_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.SumAbsoluteDifferencesInBlock32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_sumabsolutedifferencesinblock32_v256u8_v256u8_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW_VL, "System.Runtime.Intrinsics.X86.Avx512BW+VL.SumAbsoluteDifferencesInBlock32");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_dbsad_epu8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_vl_sumabsolutedifferencesinblock32_v256u8_v256u8_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW+VL.SumAbsoluteDifferencesInBlock32");
}
#endif
