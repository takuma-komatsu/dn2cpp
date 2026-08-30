#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx512Vbmi: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi_multishift_v512i8_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi, "System.Runtime.Intrinsics.X86.Avx512Vbmi.MultiShift");
    return dn2cpp_isa_vec<64>(_mm512_multishift_epi64_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi_multishift_v512i8_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi.MultiShift");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi_multishift_v512u8_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi, "System.Runtime.Intrinsics.X86.Avx512Vbmi.MultiShift");
    return dn2cpp_isa_vec<64>(_mm512_multishift_epi64_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi_multishift_v512u8_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi.MultiShift");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi_permutevar64x8_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi, "System.Runtime.Intrinsics.X86.Avx512Vbmi.PermuteVar64x8");
    return dn2cpp_isa_vec<64>(_mm512_permutexvar_epi8(dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi_permutevar64x8_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi.PermuteVar64x8");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi_permutevar64x8_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi, "System.Runtime.Intrinsics.X86.Avx512Vbmi.PermuteVar64x8");
    return dn2cpp_isa_vec<64>(_mm512_permutexvar_epi8(dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi_permutevar64x8_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi.PermuteVar64x8");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi_permutevar64x8x2_v512i8_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi, "System.Runtime.Intrinsics.X86.Avx512Vbmi.PermuteVar64x8x2");
    return dn2cpp_isa_vec<64>(_mm512_permutex2var_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi_permutevar64x8x2_v512i8_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi.PermuteVar64x8x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi_permutevar64x8x2_v512u8_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi, "System.Runtime.Intrinsics.X86.Avx512Vbmi.PermuteVar64x8x2");
    return dn2cpp_isa_vec<64>(_mm512_permutex2var_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi_permutevar64x8x2_v512u8_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi.PermuteVar64x8x2");
}
#endif
