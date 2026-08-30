#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Pclmulqdq+V256: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("vpclmulqdq") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_pclmulqdq_v256_carrylessmultiply_v256i64_v256i64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Pclmulqdq_V256, "System.Runtime.Intrinsics.X86.Pclmulqdq+V256.CarrylessMultiply");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_clmulepi64_epi128(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_pclmulqdq_v256_carrylessmultiply_v256i64_v256i64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Pclmulqdq+V256.CarrylessMultiply");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("vpclmulqdq") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_pclmulqdq_v256_carrylessmultiply_v256u64_v256u64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Pclmulqdq_V256, "System.Runtime.Intrinsics.X86.Pclmulqdq+V256.CarrylessMultiply");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_clmulepi64_epi128(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_pclmulqdq_v256_carrylessmultiply_v256u64_v256u64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Pclmulqdq+V256.CarrylessMultiply");
}
#endif
