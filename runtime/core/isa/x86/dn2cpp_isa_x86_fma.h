#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Fma: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyadd_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplyAdd");
    return dn2cpp_isa_vec<16>(_mm_fmadd_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyadd_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplyAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyadd_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplyAdd");
    return dn2cpp_isa_vec<16>(_mm_fmadd_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyadd_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplyAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplyadd_v256f32_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplyAdd");
    return dn2cpp_isa_vec<32>(_mm256_fmadd_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), dn2cpp_isa_bits<__m256>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplyadd_v256f32_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplyAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplyadd_v256f64_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplyAdd");
    return dn2cpp_isa_vec<32>(_mm256_fmadd_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), dn2cpp_isa_bits<__m256d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplyadd_v256f64_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplyAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyaddnegated_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplyAddNegated");
    return dn2cpp_isa_vec<16>(_mm_fnmadd_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyaddnegated_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplyAddNegated");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyaddnegated_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplyAddNegated");
    return dn2cpp_isa_vec<16>(_mm_fnmadd_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyaddnegated_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplyAddNegated");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplyaddnegated_v256f32_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplyAddNegated");
    return dn2cpp_isa_vec<32>(_mm256_fnmadd_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), dn2cpp_isa_bits<__m256>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplyaddnegated_v256f32_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplyAddNegated");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplyaddnegated_v256f64_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplyAddNegated");
    return dn2cpp_isa_vec<32>(_mm256_fnmadd_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), dn2cpp_isa_bits<__m256d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplyaddnegated_v256f64_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplyAddNegated");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyaddnegatedscalar_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplyAddNegatedScalar");
    return dn2cpp_isa_vec<16>(_mm_fnmadd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyaddnegatedscalar_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplyAddNegatedScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyaddnegatedscalar_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplyAddNegatedScalar");
    return dn2cpp_isa_vec<16>(_mm_fnmadd_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyaddnegatedscalar_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplyAddNegatedScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyaddscalar_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplyAddScalar");
    return dn2cpp_isa_vec<16>(_mm_fmadd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyaddscalar_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplyAddScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyaddscalar_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplyAddScalar");
    return dn2cpp_isa_vec<16>(_mm_fmadd_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyaddscalar_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplyAddScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyaddsubtract_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplyAddSubtract");
    return dn2cpp_isa_vec<16>(_mm_fmaddsub_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyaddsubtract_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplyAddSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyaddsubtract_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplyAddSubtract");
    return dn2cpp_isa_vec<16>(_mm_fmaddsub_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplyaddsubtract_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplyAddSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplyaddsubtract_v256f32_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplyAddSubtract");
    return dn2cpp_isa_vec<32>(_mm256_fmaddsub_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), dn2cpp_isa_bits<__m256>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplyaddsubtract_v256f32_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplyAddSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplyaddsubtract_v256f64_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplyAddSubtract");
    return dn2cpp_isa_vec<32>(_mm256_fmaddsub_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), dn2cpp_isa_bits<__m256d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplyaddsubtract_v256f64_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplyAddSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtract_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplySubtract");
    return dn2cpp_isa_vec<16>(_mm_fmsub_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtract_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplySubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtract_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplySubtract");
    return dn2cpp_isa_vec<16>(_mm_fmsub_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtract_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplySubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplysubtract_v256f32_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplySubtract");
    return dn2cpp_isa_vec<32>(_mm256_fmsub_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), dn2cpp_isa_bits<__m256>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplysubtract_v256f32_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplySubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplysubtract_v256f64_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplySubtract");
    return dn2cpp_isa_vec<32>(_mm256_fmsub_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), dn2cpp_isa_bits<__m256d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplysubtract_v256f64_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplySubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtractadd_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplySubtractAdd");
    return dn2cpp_isa_vec<16>(_mm_fmsubadd_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtractadd_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplySubtractAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtractadd_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplySubtractAdd");
    return dn2cpp_isa_vec<16>(_mm_fmsubadd_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtractadd_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplySubtractAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplysubtractadd_v256f32_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplySubtractAdd");
    return dn2cpp_isa_vec<32>(_mm256_fmsubadd_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), dn2cpp_isa_bits<__m256>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplysubtractadd_v256f32_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplySubtractAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplysubtractadd_v256f64_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplySubtractAdd");
    return dn2cpp_isa_vec<32>(_mm256_fmsubadd_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), dn2cpp_isa_bits<__m256d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplysubtractadd_v256f64_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplySubtractAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtractnegated_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplySubtractNegated");
    return dn2cpp_isa_vec<16>(_mm_fnmsub_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtractnegated_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplySubtractNegated");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtractnegated_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplySubtractNegated");
    return dn2cpp_isa_vec<16>(_mm_fnmsub_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtractnegated_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplySubtractNegated");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplysubtractnegated_v256f32_v256f32_v256f32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplySubtractNegated");
    return dn2cpp_isa_vec<32>(_mm256_fnmsub_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), dn2cpp_isa_bits<__m256>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplysubtractnegated_v256f32_v256f32_v256f32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplySubtractNegated");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplysubtractnegated_v256f64_v256f64_v256f64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplySubtractNegated");
    return dn2cpp_isa_vec<32>(_mm256_fnmsub_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), dn2cpp_isa_bits<__m256d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_fma_multiplysubtractnegated_v256f64_v256f64_v256f64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplySubtractNegated");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtractnegatedscalar_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplySubtractNegatedScalar");
    return dn2cpp_isa_vec<16>(_mm_fnmsub_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtractnegatedscalar_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplySubtractNegatedScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtractnegatedscalar_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplySubtractNegatedScalar");
    return dn2cpp_isa_vec<16>(_mm_fnmsub_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtractnegatedscalar_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplySubtractNegatedScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtractscalar_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplySubtractScalar");
    return dn2cpp_isa_vec<16>(_mm_fmsub_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtractscalar_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplySubtractScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("fma") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtractscalar_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Fma, "System.Runtime.Intrinsics.X86.Fma.MultiplySubtractScalar");
    return dn2cpp_isa_vec<16>(_mm_fmsub_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_fma_multiplysubtractscalar_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Fma.MultiplySubtractScalar");
}
#endif
