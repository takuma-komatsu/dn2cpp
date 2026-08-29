#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx512Vbmi+VL: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi_vl_multishift_v128i8_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.MultiShift");
    return dn2cpp_isa_vec<16>(_mm_multishift_epi64_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi_vl_multishift_v128i8_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.MultiShift");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi_vl_multishift_v128u8_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.MultiShift");
    return dn2cpp_isa_vec<16>(_mm_multishift_epi64_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi_vl_multishift_v128u8_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.MultiShift");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi_vl_multishift_v256i8_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.MultiShift");
    return dn2cpp_isa_vec<32>(_mm256_multishift_epi64_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi_vl_multishift_v256i8_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.MultiShift");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi_vl_multishift_v256u8_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.MultiShift");
    return dn2cpp_isa_vec<32>(_mm256_multishift_epi64_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi_vl_multishift_v256u8_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.MultiShift");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi_vl_permutevar16x8_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.PermuteVar16x8");
    return dn2cpp_isa_vec<16>(_mm_permutexvar_epi8(dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi_vl_permutevar16x8_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.PermuteVar16x8");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi_vl_permutevar16x8_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.PermuteVar16x8");
    return dn2cpp_isa_vec<16>(_mm_permutexvar_epi8(dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi_vl_permutevar16x8_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.PermuteVar16x8");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi_vl_permutevar16x8x2_v128i8_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.PermuteVar16x8x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi_vl_permutevar16x8x2_v128i8_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.PermuteVar16x8x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi_vl_permutevar16x8x2_v128u8_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.PermuteVar16x8x2");
    return dn2cpp_isa_vec<16>(_mm_permutex2var_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi_vl_permutevar16x8x2_v128u8_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.PermuteVar16x8x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi_vl_permutevar32x8_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.PermuteVar32x8");
    return dn2cpp_isa_vec<32>(_mm256_permutexvar_epi8(dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi_vl_permutevar32x8_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.PermuteVar32x8");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi_vl_permutevar32x8_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.PermuteVar32x8");
    return dn2cpp_isa_vec<32>(_mm256_permutexvar_epi8(dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi_vl_permutevar32x8_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.PermuteVar32x8");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi_vl_permutevar32x8x2_v256i8_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.PermuteVar32x8x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi_vl_permutevar32x8x2_v256i8_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.PermuteVar32x8x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi_vl_permutevar32x8x2_v256u8_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.PermuteVar32x8x2");
    return dn2cpp_isa_vec<32>(_mm256_permutex2var_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi_vl_permutevar32x8x2_v256u8_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi+VL.PermuteVar32x8x2");
}
#endif
