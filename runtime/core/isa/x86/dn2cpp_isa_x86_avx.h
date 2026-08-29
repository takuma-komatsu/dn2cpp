#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_add_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Add");
    return dn2cpp_isa_vec<32>(_mm256_add_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_add_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_add_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Add");
    return dn2cpp_isa_vec<32>(_mm256_add_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_add_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_addsubtract_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.AddSubtract");
    return dn2cpp_isa_vec<32>(_mm256_addsub_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_addsubtract_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.AddSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_addsubtract_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.AddSubtract");
    return dn2cpp_isa_vec<32>(_mm256_addsub_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_addsubtract_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.AddSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_and_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.And");
    return dn2cpp_isa_vec<32>(_mm256_and_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_and_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_and_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.And");
    return dn2cpp_isa_vec<32>(_mm256_and_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_and_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_andnot_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.AndNot");
    return dn2cpp_isa_vec<32>(_mm256_andnot_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_andnot_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_andnot_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.AndNot");
    return dn2cpp_isa_vec<32>(_mm256_andnot_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_andnot_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_blend_v256f32_v256f32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Blend");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_blend_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_blend_v256f32_v256f32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Blend");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_blend_v256f64_v256f64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Blend");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_blend_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), DN2CPP_IMM & 15)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_blend_v256f64_v256f64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Blend");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_blendvariable_v256f32_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_blendv_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), dn2cpp_isa_bits<__m256>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_blendvariable_v256f32_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_blendvariable_v256f64_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_blendv_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), dn2cpp_isa_bits<__m256d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_blendvariable_v256f64_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_broadcastscalartovector128_pf32(float* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_broadcast_ss(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_broadcastscalartovector128_pf32(float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_broadcastscalartovector256_pf32(float* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcast_ss(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_broadcastscalartovector256_pf32(float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_broadcastscalartovector256_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcast_sd(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_broadcastscalartovector256_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_broadcastvector128tovector256_pf32(float* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.BroadcastVector128ToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcast_ps((const __m128*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_broadcastvector128tovector256_pf32(float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.BroadcastVector128ToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_broadcastvector128tovector256_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.BroadcastVector128ToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcast_pd((const __m128d*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_broadcastvector128tovector256_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.BroadcastVector128ToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_ceiling_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Ceiling");
    return dn2cpp_isa_vec<32>(_mm256_ceil_ps(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_ceiling_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Ceiling");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_ceiling_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Ceiling");
    return dn2cpp_isa_vec<32>(_mm256_ceil_pd(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_ceiling_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Ceiling");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_compare_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Compare");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<16>(_mm_cmp_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_compare_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Compare");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_compare_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Compare");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<16>(_mm_cmp_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_compare_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Compare");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_compare_v256f32_v256f32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Compare");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<32>(_mm256_cmp_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_compare_v256f32_v256f32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Compare");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_compare_v256f64_v256f64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Compare");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<32>(_mm256_cmp_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_compare_v256f64_v256f64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Compare");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_compareequal_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmp_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_EQ_OQ));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_compareequal_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_compareequal_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmp_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_EQ_OQ));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_compareequal_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparegreaterthan_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_cmp_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_GT_OS));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparegreaterthan_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparegreaterthan_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_cmp_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_GT_OS));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparegreaterthan_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparegreaterthanorequal_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmp_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_GE_OS));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparegreaterthanorequal_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparegreaterthanorequal_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmp_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_GE_OS));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparegreaterthanorequal_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparelessthan_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_cmp_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_LT_OS));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparelessthan_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparelessthan_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareLessThan");
    return dn2cpp_isa_vec<32>(_mm256_cmp_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_LT_OS));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparelessthan_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparelessthanorequal_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmp_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_LE_OS));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparelessthanorequal_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparelessthanorequal_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmp_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_LE_OS));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparelessthanorequal_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotequal_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmp_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_NEQ_UQ));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotequal_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotequal_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareNotEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmp_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_NEQ_UQ));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotequal_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotgreaterthan_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareNotGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_cmp_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_NGT_US));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotgreaterthan_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareNotGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotgreaterthan_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareNotGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_cmp_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_NGT_US));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotgreaterthan_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareNotGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotgreaterthanorequal_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareNotGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmp_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_NGE_US));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotgreaterthanorequal_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareNotGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotgreaterthanorequal_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareNotGreaterThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmp_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_NGE_US));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotgreaterthanorequal_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareNotGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotlessthan_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareNotLessThan");
    return dn2cpp_isa_vec<32>(_mm256_cmp_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_NLT_US));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotlessthan_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareNotLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotlessthan_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareNotLessThan");
    return dn2cpp_isa_vec<32>(_mm256_cmp_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_NLT_US));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotlessthan_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareNotLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotlessthanorequal_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareNotLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmp_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_NLE_US));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotlessthanorequal_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareNotLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotlessthanorequal_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareNotLessThanOrEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmp_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_NLE_US));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_comparenotlessthanorequal_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareNotLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_compareordered_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareOrdered");
    return dn2cpp_isa_vec<32>(_mm256_cmp_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_ORD_Q));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_compareordered_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareOrdered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_compareordered_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareOrdered");
    return dn2cpp_isa_vec<32>(_mm256_cmp_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_ORD_Q));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_compareordered_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareOrdered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_comparescalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<16>(_mm_cmp_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_comparescalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_comparescalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<16>(_mm_cmp_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_comparescalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_compareunordered_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareUnordered");
    return dn2cpp_isa_vec<32>(_mm256_cmp_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), _CMP_UNORD_Q));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_compareunordered_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareUnordered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_compareunordered_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.CompareUnordered");
    return dn2cpp_isa_vec<32>(_mm256_cmp_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), _CMP_UNORD_Q));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_compareunordered_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.CompareUnordered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_converttovector128int32_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm256_cvtpd_epi32(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_converttovector128int32_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_converttovector128int32withtruncation_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ConvertToVector128Int32WithTruncation");
    return dn2cpp_isa_vec<16>(_mm256_cvttpd_epi32(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_converttovector128int32withtruncation_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ConvertToVector128Int32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_converttovector128single_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ConvertToVector128Single");
    return dn2cpp_isa_vec<16>(_mm256_cvtpd_ps(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_converttovector128single_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ConvertToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_converttovector256double_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ConvertToVector256Double");
    return dn2cpp_isa_vec<32>(_mm256_cvtps_pd(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_converttovector256double_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ConvertToVector256Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_converttovector256double_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ConvertToVector256Double");
    return dn2cpp_isa_vec<32>(_mm256_cvtepi32_pd(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_converttovector256double_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ConvertToVector256Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_converttovector256int32_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ConvertToVector256Int32");
    return dn2cpp_isa_vec<32>(_mm256_cvtps_epi32(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_converttovector256int32_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ConvertToVector256Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_converttovector256int32withtruncation_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ConvertToVector256Int32WithTruncation");
    return dn2cpp_isa_vec<32>(_mm256_cvttps_epi32(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_converttovector256int32withtruncation_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ConvertToVector256Int32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_converttovector256single_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ConvertToVector256Single");
    return dn2cpp_isa_vec<32>(_mm256_cvtepi32_ps(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_converttovector256single_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ConvertToVector256Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_divide_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Divide");
    return dn2cpp_isa_vec<32>(_mm256_div_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_divide_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Divide");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_divide_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Divide");
    return dn2cpp_isa_vec<32>(_mm256_div_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_divide_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Divide");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_dotproduct_v256f32_v256f32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.DotProduct");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_dp_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_dotproduct_v256f32_v256f32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.DotProduct");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_duplicateevenindexed_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.DuplicateEvenIndexed");
    return dn2cpp_isa_vec<32>(_mm256_moveldup_ps(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_duplicateevenindexed_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.DuplicateEvenIndexed");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_duplicateevenindexed_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.DuplicateEvenIndexed");
    return dn2cpp_isa_vec<32>(_mm256_movedup_pd(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_duplicateevenindexed_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.DuplicateEvenIndexed");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_duplicateoddindexed_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.DuplicateOddIndexed");
    return dn2cpp_isa_vec<32>(_mm256_movehdup_ps(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_duplicateoddindexed_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.DuplicateOddIndexed");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256f32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm256_extractf128_ps(dn2cpp_isa_bits<__m256>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256f32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256f64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm256_extractf128_pd(dn2cpp_isa_bits<__m256d>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256f64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256i16_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm256_extractf128_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256i16_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256i32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm256_extractf128_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256i32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256i64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm256_extractf128_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256i64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256i8_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm256_extractf128_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256i8_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256u16_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm256_extractf128_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256u16_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256u32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm256_extractf128_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256u32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256u64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm256_extractf128_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256u64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256u8_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm256_extractf128_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_extractvector128_v256u8_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_floor_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Floor");
    return dn2cpp_isa_vec<32>(_mm256_floor_ps(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_floor_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Floor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_floor_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Floor");
    return dn2cpp_isa_vec<32>(_mm256_floor_pd(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_floor_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Floor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_horizontaladd_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.HorizontalAdd");
    return dn2cpp_isa_vec<32>(_mm256_hadd_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_horizontaladd_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.HorizontalAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_horizontaladd_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.HorizontalAdd");
    return dn2cpp_isa_vec<32>(_mm256_hadd_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_horizontaladd_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.HorizontalAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_horizontalsubtract_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.HorizontalSubtract");
    return dn2cpp_isa_vec<32>(_mm256_hsub_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_horizontalsubtract_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.HorizontalSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_horizontalsubtract_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.HorizontalSubtract");
    return dn2cpp_isa_vec<32>(_mm256_hsub_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_horizontalsubtract_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.HorizontalSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256f32_v128f32_u8(const Dn2CppVector256& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_insertf128_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256f32_v128f32_u8(const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256f64_v128f64_u8(const Dn2CppVector256& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_insertf128_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256f64_v128f64_u8(const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256i16_v128i16_u8(const Dn2CppVector256& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_insertf128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256i16_v128i16_u8(const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256i32_v128i32_u8(const Dn2CppVector256& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_insertf128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256i32_v128i32_u8(const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256i64_v128i64_u8(const Dn2CppVector256& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_insertf128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256i64_v128i64_u8(const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256i8_v128i8_u8(const Dn2CppVector256& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_insertf128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256i8_v128i8_u8(const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256u16_v128u16_u8(const Dn2CppVector256& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_insertf128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256u16_v128u16_u8(const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256u32_v128u32_u8(const Dn2CppVector256& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_insertf128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256u32_v128u32_u8(const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256u64_v128u64_u8(const Dn2CppVector256& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_insertf128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256u64_v128u64_u8(const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256u8_v128u8_u8(const Dn2CppVector256& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_insertf128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_insertvector128_v256u8_v128u8_u8(const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pf32(float* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
    return dn2cpp_isa_vec<32>(_mm256_load_ps(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pf32(float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
    return dn2cpp_isa_vec<32>(_mm256_load_pd(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
    return dn2cpp_isa_vec<32>(_mm256_load_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
    return dn2cpp_isa_vec<32>(_mm256_load_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
    return dn2cpp_isa_vec<32>(_mm256_load_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
    return dn2cpp_isa_vec<32>(_mm256_load_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
    return dn2cpp_isa_vec<32>(_mm256_load_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
    return dn2cpp_isa_vec<32>(_mm256_load_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
    return dn2cpp_isa_vec<32>(_mm256_load_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
    return dn2cpp_isa_vec<32>(_mm256_load_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadalignedvector256_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadAlignedVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loaddquvector256_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadDquVector256");
    return dn2cpp_isa_vec<32>(_mm256_lddqu_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loaddquvector256_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadDquVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loaddquvector256_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadDquVector256");
    return dn2cpp_isa_vec<32>(_mm256_lddqu_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loaddquvector256_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadDquVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loaddquvector256_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadDquVector256");
    return dn2cpp_isa_vec<32>(_mm256_lddqu_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loaddquvector256_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadDquVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loaddquvector256_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadDquVector256");
    return dn2cpp_isa_vec<32>(_mm256_lddqu_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loaddquvector256_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadDquVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loaddquvector256_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadDquVector256");
    return dn2cpp_isa_vec<32>(_mm256_lddqu_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loaddquvector256_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadDquVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loaddquvector256_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadDquVector256");
    return dn2cpp_isa_vec<32>(_mm256_lddqu_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loaddquvector256_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadDquVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loaddquvector256_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadDquVector256");
    return dn2cpp_isa_vec<32>(_mm256_lddqu_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loaddquvector256_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadDquVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loaddquvector256_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadDquVector256");
    return dn2cpp_isa_vec<32>(_mm256_lddqu_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loaddquvector256_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadDquVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pf32(float* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadVector256");
    return dn2cpp_isa_vec<32>(_mm256_loadu_ps(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pf32(float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadVector256");
    return dn2cpp_isa_vec<32>(_mm256_loadu_pd(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadVector256");
    return dn2cpp_isa_vec<32>(_mm256_loadu_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadVector256");
    return dn2cpp_isa_vec<32>(_mm256_loadu_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadVector256");
    return dn2cpp_isa_vec<32>(_mm256_loadu_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadVector256");
    return dn2cpp_isa_vec<32>(_mm256_loadu_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadVector256");
    return dn2cpp_isa_vec<32>(_mm256_loadu_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadVector256");
    return dn2cpp_isa_vec<32>(_mm256_loadu_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadVector256");
    return dn2cpp_isa_vec<32>(_mm256_loadu_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.LoadVector256");
    return dn2cpp_isa_vec<32>(_mm256_loadu_si256((const __m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_loadvector256_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.LoadVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_maskload_pf32_v128f32(float* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_maskload_ps(a0, dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_maskload_pf32_v128f32(float*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_maskload_pf32_v256f32(float* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_maskload_ps(a0, dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_maskload_pf32_v256f32(float*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_maskload_pf64_v128f64(double* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_maskload_pd(a0, dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_maskload_pf64_v128f64(double*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_maskload_pf64_v256f64(double* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_maskload_pd(a0, dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_maskload_pf64_v256f64(double*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_maskstore_pf32_v128f32_v128f32(float* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.MaskStore");
    _mm_maskstore_ps(a0, dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_maskstore_pf32_v128f32_v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_maskstore_pf32_v256f32_v256f32(float* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.MaskStore");
    _mm256_maskstore_ps(a0, dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_maskstore_pf32_v256f32_v256f32(float*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_maskstore_pf64_v128f64_v128f64(double* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.MaskStore");
    _mm_maskstore_pd(a0, dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128d>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_maskstore_pf64_v128f64_v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_maskstore_pf64_v256f64_v256f64(double* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.MaskStore");
    _mm256_maskstore_pd(a0, dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256d>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_maskstore_pf64_v256f64_v256f64(double*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_max_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Max");
    return dn2cpp_isa_vec<32>(_mm256_max_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_max_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_max_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Max");
    return dn2cpp_isa_vec<32>(_mm256_max_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_max_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_min_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Min");
    return dn2cpp_isa_vec<32>(_mm256_min_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_min_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_min_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Min");
    return dn2cpp_isa_vec<32>(_mm256_min_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_min_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx_movemask_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.MoveMask");
    return _mm256_movemask_ps(dn2cpp_isa_bits<__m256>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx_movemask_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx_movemask_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.MoveMask");
    return _mm256_movemask_pd(dn2cpp_isa_bits<__m256d>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx_movemask_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_multiply_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Multiply");
    return dn2cpp_isa_vec<32>(_mm256_mul_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_multiply_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Multiply");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_multiply_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Multiply");
    return dn2cpp_isa_vec<32>(_mm256_mul_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_multiply_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Multiply");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_or_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Or");
    return dn2cpp_isa_vec<32>(_mm256_or_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_or_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_or_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Or");
    return dn2cpp_isa_vec<32>(_mm256_or_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_or_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256f32_v256f32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Permute2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_permute2f128_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256f32_v256f32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Permute2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256f64_v256f64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Permute2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_permute2f128_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256f64_v256f64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Permute2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256i16_v256i16_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Permute2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_permute2f128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256i16_v256i16_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Permute2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256i32_v256i32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Permute2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_permute2f128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256i32_v256i32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Permute2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256i64_v256i64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Permute2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_permute2f128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256i64_v256i64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Permute2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256i8_v256i8_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Permute2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_permute2f128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256i8_v256i8_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Permute2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256u16_v256u16_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Permute2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_permute2f128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256u16_v256u16_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Permute2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256u32_v256u32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Permute2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_permute2f128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256u32_v256u32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Permute2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256u64_v256u64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Permute2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_permute2f128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256u64_v256u64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Permute2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256u8_v256u8_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Permute2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_permute2f128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute2x128_v256u8_v256u8_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Permute2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_permute_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Permute");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_permute_ps(dn2cpp_isa_bits<__m128>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_permute_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Permute");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_permute_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Permute");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_permute_pd(dn2cpp_isa_bits<__m128d>(a0), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_permute_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Permute");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute_v256f32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Permute");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_permute_ps(dn2cpp_isa_bits<__m256>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute_v256f32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Permute");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute_v256f64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Permute");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_permute_pd(dn2cpp_isa_bits<__m256d>(a0), DN2CPP_IMM & 15)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permute_v256f64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Permute");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_permutevar_v128f32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.PermuteVar");
    return dn2cpp_isa_vec<16>(_mm_permutevar_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_permutevar_v128f32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.PermuteVar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_permutevar_v128f64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.PermuteVar");
    return dn2cpp_isa_vec<16>(_mm_permutevar_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx_permutevar_v128f64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.PermuteVar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permutevar_v256f32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.PermuteVar");
    return dn2cpp_isa_vec<32>(_mm256_permutevar_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permutevar_v256f32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.PermuteVar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permutevar_v256f64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.PermuteVar");
    return dn2cpp_isa_vec<32>(_mm256_permutevar_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_permutevar_v256f64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.PermuteVar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_reciprocal_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Reciprocal");
    return dn2cpp_isa_vec<32>(_mm256_rcp_ps(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_reciprocal_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Reciprocal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_reciprocalsqrt_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.ReciprocalSqrt");
    return dn2cpp_isa_vec<32>(_mm256_rsqrt_ps(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_reciprocalsqrt_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.ReciprocalSqrt");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundcurrentdirection_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.RoundCurrentDirection");
    return dn2cpp_isa_vec<32>(_mm256_round_ps(dn2cpp_isa_bits<__m256>(a0), _MM_FROUND_CUR_DIRECTION));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundcurrentdirection_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.RoundCurrentDirection");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundcurrentdirection_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.RoundCurrentDirection");
    return dn2cpp_isa_vec<32>(_mm256_round_pd(dn2cpp_isa_bits<__m256d>(a0), _MM_FROUND_CUR_DIRECTION));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundcurrentdirection_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.RoundCurrentDirection");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundtonearestinteger_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.RoundToNearestInteger");
    return dn2cpp_isa_vec<32>(_mm256_round_ps(dn2cpp_isa_bits<__m256>(a0), _MM_FROUND_TO_NEAREST_INT | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundtonearestinteger_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.RoundToNearestInteger");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundtonearestinteger_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.RoundToNearestInteger");
    return dn2cpp_isa_vec<32>(_mm256_round_pd(dn2cpp_isa_bits<__m256d>(a0), _MM_FROUND_TO_NEAREST_INT | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundtonearestinteger_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.RoundToNearestInteger");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundtonegativeinfinity_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.RoundToNegativeInfinity");
    return dn2cpp_isa_vec<32>(_mm256_round_ps(dn2cpp_isa_bits<__m256>(a0), _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundtonegativeinfinity_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.RoundToNegativeInfinity");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundtonegativeinfinity_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.RoundToNegativeInfinity");
    return dn2cpp_isa_vec<32>(_mm256_round_pd(dn2cpp_isa_bits<__m256d>(a0), _MM_FROUND_TO_NEG_INF | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundtonegativeinfinity_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.RoundToNegativeInfinity");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundtopositiveinfinity_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.RoundToPositiveInfinity");
    return dn2cpp_isa_vec<32>(_mm256_round_ps(dn2cpp_isa_bits<__m256>(a0), _MM_FROUND_TO_POS_INF | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundtopositiveinfinity_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.RoundToPositiveInfinity");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundtopositiveinfinity_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.RoundToPositiveInfinity");
    return dn2cpp_isa_vec<32>(_mm256_round_pd(dn2cpp_isa_bits<__m256d>(a0), _MM_FROUND_TO_POS_INF | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundtopositiveinfinity_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.RoundToPositiveInfinity");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundtozero_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.RoundToZero");
    return dn2cpp_isa_vec<32>(_mm256_round_ps(dn2cpp_isa_bits<__m256>(a0), _MM_FROUND_TO_ZERO | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundtozero_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.RoundToZero");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundtozero_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.RoundToZero");
    return dn2cpp_isa_vec<32>(_mm256_round_pd(dn2cpp_isa_bits<__m256d>(a0), _MM_FROUND_TO_ZERO | _MM_FROUND_NO_EXC));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_roundtozero_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.RoundToZero");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_shuffle_v256f32_v256f32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Shuffle");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_shuffle_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_shuffle_v256f32_v256f32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Shuffle");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_shuffle_v256f64_v256f64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Shuffle");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_shuffle_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), DN2CPP_IMM & 15)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_shuffle_v256f64_v256f64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Shuffle");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_sqrt_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Sqrt");
    return dn2cpp_isa_vec<32>(_mm256_sqrt_ps(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_sqrt_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Sqrt");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_sqrt_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Sqrt");
    return dn2cpp_isa_vec<32>(_mm256_sqrt_pd(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_sqrt_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Sqrt");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pf32_v256f32(float* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Store");
    _mm256_storeu_ps(a0, dn2cpp_isa_bits<__m256>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pf32_v256f32(float*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pf64_v256f64(double* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Store");
    _mm256_storeu_pd(a0, dn2cpp_isa_bits<__m256d>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pf64_v256f64(double*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pi16_v256i16(int16_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Store");
    _mm256_storeu_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pi16_v256i16(int16_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pi32_v256i32(int32_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Store");
    _mm256_storeu_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pi32_v256i32(int32_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pi64_v256i64(int64_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Store");
    _mm256_storeu_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pi64_v256i64(int64_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pi8_v256i8(int8_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Store");
    _mm256_storeu_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pi8_v256i8(int8_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pu16_v256u16(uint16_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Store");
    _mm256_storeu_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pu16_v256u16(uint16_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pu32_v256u32(uint32_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Store");
    _mm256_storeu_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pu32_v256u32(uint32_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pu64_v256u64(uint64_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Store");
    _mm256_storeu_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pu64_v256u64(uint64_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pu8_v256u8(uint8_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Store");
    _mm256_storeu_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_store_pu8_v256u8(uint8_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pf32_v256f32(float* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAligned");
    _mm256_store_ps(a0, dn2cpp_isa_bits<__m256>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pf32_v256f32(float*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pf64_v256f64(double* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAligned");
    _mm256_store_pd(a0, dn2cpp_isa_bits<__m256d>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pf64_v256f64(double*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pi16_v256i16(int16_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAligned");
    _mm256_store_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pi16_v256i16(int16_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pi32_v256i32(int32_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAligned");
    _mm256_store_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pi32_v256i32(int32_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pi64_v256i64(int64_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAligned");
    _mm256_store_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pi64_v256i64(int64_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pi8_v256i8(int8_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAligned");
    _mm256_store_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pi8_v256i8(int8_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pu16_v256u16(uint16_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAligned");
    _mm256_store_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pu16_v256u16(uint16_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pu32_v256u32(uint32_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAligned");
    _mm256_store_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pu32_v256u32(uint32_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pu64_v256u64(uint64_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAligned");
    _mm256_store_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pu64_v256u64(uint64_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pu8_v256u8(uint8_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAligned");
    _mm256_store_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealigned_pu8_v256u8(uint8_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pf32_v256f32(float* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
    _mm256_stream_ps(a0, dn2cpp_isa_bits<__m256>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pf32_v256f32(float*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pf64_v256f64(double* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
    _mm256_stream_pd(a0, dn2cpp_isa_bits<__m256d>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pf64_v256f64(double*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pi16_v256i16(int16_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
    _mm256_stream_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pi16_v256i16(int16_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pi32_v256i32(int32_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
    _mm256_stream_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pi32_v256i32(int32_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pi64_v256i64(int64_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
    _mm256_stream_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pi64_v256i64(int64_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pi8_v256i8(int8_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
    _mm256_stream_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pi8_v256i8(int8_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pu16_v256u16(uint16_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
    _mm256_stream_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pu16_v256u16(uint16_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pu32_v256u32(uint32_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
    _mm256_stream_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pu32_v256u32(uint32_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pu64_v256u64(uint64_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
    _mm256_stream_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pu64_v256u64(uint64_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pu8_v256u8(uint8_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
    _mm256_stream_si256((__m256i*)a0, dn2cpp_isa_bits<__m256i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx_storealignednontemporal_pu8_v256u8(uint8_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_subtract_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Subtract");
    return dn2cpp_isa_vec<32>(_mm256_sub_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_subtract_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_subtract_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Subtract");
    return dn2cpp_isa_vec<32>(_mm256_sub_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_subtract_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestC");
    return _mm_testc_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestC");
    return _mm_testc_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestC");
    return _mm256_testc_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestC");
    return _mm256_testc_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestC");
    return _mm256_testc_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestC");
    return _mm256_testc_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestC");
    return _mm256_testc_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestC");
    return _mm256_testc_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestC");
    return _mm256_testc_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestC");
    return _mm256_testc_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestC");
    return _mm256_testc_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestC");
    return _mm256_testc_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testc_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
    return _mm_testnzc_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
    return _mm_testnzc_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
    return _mm256_testnzc_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
    return _mm256_testnzc_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
    return _mm256_testnzc_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
    return _mm256_testnzc_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
    return _mm256_testnzc_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
    return _mm256_testnzc_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
    return _mm256_testnzc_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
    return _mm256_testnzc_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
    return _mm256_testnzc_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
    return _mm256_testnzc_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testnotzandnotc_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestNotZAndNotC");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestZ");
    return _mm_testz_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestZ");
    return _mm_testz_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestZ");
    return _mm256_testz_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestZ");
    return _mm256_testz_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestZ");
    return _mm256_testz_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestZ");
    return _mm256_testz_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestZ");
    return _mm256_testz_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestZ");
    return _mm256_testz_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestZ");
    return _mm256_testz_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestZ");
    return _mm256_testz_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestZ");
    return _mm256_testz_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.TestZ");
    return _mm256_testz_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_avx_testz_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.TestZ");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_unpackhigh_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.UnpackHigh");
    return dn2cpp_isa_vec<32>(_mm256_unpackhi_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_unpackhigh_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_unpackhigh_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.UnpackHigh");
    return dn2cpp_isa_vec<32>(_mm256_unpackhi_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_unpackhigh_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_unpacklow_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.UnpackLow");
    return dn2cpp_isa_vec<32>(_mm256_unpacklo_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_unpacklow_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_unpacklow_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.UnpackLow");
    return dn2cpp_isa_vec<32>(_mm256_unpacklo_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_unpacklow_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_xor_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Xor");
    return dn2cpp_isa_vec<32>(_mm256_xor_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_xor_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_xor_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx, "System.Runtime.Intrinsics.X86.Avx.Xor");
    return dn2cpp_isa_vec<32>(_mm256_xor_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx_xor_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx.Xor");
}
#endif
