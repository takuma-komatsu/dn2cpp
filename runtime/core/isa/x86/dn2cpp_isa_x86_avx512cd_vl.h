#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx512CD+VL: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512cd_vl_detectconflicts_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD_VL, "System.Runtime.Intrinsics.X86.Avx512CD+VL.DetectConflicts");
    return dn2cpp_isa_vec<16>(_mm_conflict_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512cd_vl_detectconflicts_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD+VL.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512cd_vl_detectconflicts_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD_VL, "System.Runtime.Intrinsics.X86.Avx512CD+VL.DetectConflicts");
    return dn2cpp_isa_vec<16>(_mm_conflict_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512cd_vl_detectconflicts_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD+VL.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512cd_vl_detectconflicts_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD_VL, "System.Runtime.Intrinsics.X86.Avx512CD+VL.DetectConflicts");
    return dn2cpp_isa_vec<16>(_mm_conflict_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512cd_vl_detectconflicts_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD+VL.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512cd_vl_detectconflicts_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD_VL, "System.Runtime.Intrinsics.X86.Avx512CD+VL.DetectConflicts");
    return dn2cpp_isa_vec<16>(_mm_conflict_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512cd_vl_detectconflicts_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD+VL.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512cd_vl_detectconflicts_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD_VL, "System.Runtime.Intrinsics.X86.Avx512CD+VL.DetectConflicts");
    return dn2cpp_isa_vec<32>(_mm256_conflict_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512cd_vl_detectconflicts_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD+VL.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512cd_vl_detectconflicts_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD_VL, "System.Runtime.Intrinsics.X86.Avx512CD+VL.DetectConflicts");
    return dn2cpp_isa_vec<32>(_mm256_conflict_epi64(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512cd_vl_detectconflicts_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD+VL.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512cd_vl_detectconflicts_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD_VL, "System.Runtime.Intrinsics.X86.Avx512CD+VL.DetectConflicts");
    return dn2cpp_isa_vec<32>(_mm256_conflict_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512cd_vl_detectconflicts_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD+VL.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512cd_vl_detectconflicts_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD_VL, "System.Runtime.Intrinsics.X86.Avx512CD+VL.DetectConflicts");
    return dn2cpp_isa_vec<32>(_mm256_conflict_epi64(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512cd_vl_detectconflicts_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD+VL.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512cd_vl_leadingzerocount_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD_VL, "System.Runtime.Intrinsics.X86.Avx512CD+VL.LeadingZeroCount");
    return dn2cpp_isa_vec<16>(_mm_lzcnt_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512cd_vl_leadingzerocount_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD+VL.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512cd_vl_leadingzerocount_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD_VL, "System.Runtime.Intrinsics.X86.Avx512CD+VL.LeadingZeroCount");
    return dn2cpp_isa_vec<16>(_mm_lzcnt_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512cd_vl_leadingzerocount_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD+VL.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512cd_vl_leadingzerocount_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD_VL, "System.Runtime.Intrinsics.X86.Avx512CD+VL.LeadingZeroCount");
    return dn2cpp_isa_vec<16>(_mm_lzcnt_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512cd_vl_leadingzerocount_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD+VL.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512cd_vl_leadingzerocount_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD_VL, "System.Runtime.Intrinsics.X86.Avx512CD+VL.LeadingZeroCount");
    return dn2cpp_isa_vec<16>(_mm_lzcnt_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512cd_vl_leadingzerocount_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD+VL.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512cd_vl_leadingzerocount_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD_VL, "System.Runtime.Intrinsics.X86.Avx512CD+VL.LeadingZeroCount");
    return dn2cpp_isa_vec<32>(_mm256_lzcnt_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512cd_vl_leadingzerocount_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD+VL.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512cd_vl_leadingzerocount_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD_VL, "System.Runtime.Intrinsics.X86.Avx512CD+VL.LeadingZeroCount");
    return dn2cpp_isa_vec<32>(_mm256_lzcnt_epi64(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512cd_vl_leadingzerocount_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD+VL.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512cd_vl_leadingzerocount_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD_VL, "System.Runtime.Intrinsics.X86.Avx512CD+VL.LeadingZeroCount");
    return dn2cpp_isa_vec<32>(_mm256_lzcnt_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512cd_vl_leadingzerocount_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD+VL.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512cd_vl_leadingzerocount_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD_VL, "System.Runtime.Intrinsics.X86.Avx512CD+VL.LeadingZeroCount");
    return dn2cpp_isa_vec<32>(_mm256_lzcnt_epi64(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512cd_vl_leadingzerocount_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD+VL.LeadingZeroCount");
}
#endif
