#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx512CD: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512cd_detectconflicts_v512i32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD, "System.Runtime.Intrinsics.X86.Avx512CD.DetectConflicts");
    return dn2cpp_isa_vec<64>(_mm512_conflict_epi32(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512cd_detectconflicts_v512i32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512cd_detectconflicts_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD, "System.Runtime.Intrinsics.X86.Avx512CD.DetectConflicts");
    return dn2cpp_isa_vec<64>(_mm512_conflict_epi64(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512cd_detectconflicts_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512cd_detectconflicts_v512u32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD, "System.Runtime.Intrinsics.X86.Avx512CD.DetectConflicts");
    return dn2cpp_isa_vec<64>(_mm512_conflict_epi32(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512cd_detectconflicts_v512u32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512cd_detectconflicts_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD, "System.Runtime.Intrinsics.X86.Avx512CD.DetectConflicts");
    return dn2cpp_isa_vec<64>(_mm512_conflict_epi64(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512cd_detectconflicts_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512cd_leadingzerocount_v512i32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD, "System.Runtime.Intrinsics.X86.Avx512CD.LeadingZeroCount");
    return dn2cpp_isa_vec<64>(_mm512_lzcnt_epi32(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512cd_leadingzerocount_v512i32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512cd_leadingzerocount_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD, "System.Runtime.Intrinsics.X86.Avx512CD.LeadingZeroCount");
    return dn2cpp_isa_vec<64>(_mm512_lzcnt_epi64(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512cd_leadingzerocount_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512cd_leadingzerocount_v512u32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD, "System.Runtime.Intrinsics.X86.Avx512CD.LeadingZeroCount");
    return dn2cpp_isa_vec<64>(_mm512_lzcnt_epi32(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512cd_leadingzerocount_v512u32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512cd") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512cd_leadingzerocount_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512CD, "System.Runtime.Intrinsics.X86.Avx512CD.LeadingZeroCount");
    return dn2cpp_isa_vec<64>(_mm512_lzcnt_epi64(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512cd_leadingzerocount_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512CD.LeadingZeroCount");
}
#endif
