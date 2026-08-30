#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Pclmulqdq+V512: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,vpclmulqdq") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_pclmulqdq_v512_carrylessmultiply_v512i64_v512i64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Pclmulqdq_V512, "System.Runtime.Intrinsics.X86.Pclmulqdq+V512.CarrylessMultiply");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_clmulepi64_epi128(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_pclmulqdq_v512_carrylessmultiply_v512i64_v512i64_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Pclmulqdq+V512.CarrylessMultiply");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,vpclmulqdq") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_pclmulqdq_v512_carrylessmultiply_v512u64_v512u64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Pclmulqdq_V512, "System.Runtime.Intrinsics.X86.Pclmulqdq+V512.CarrylessMultiply");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_clmulepi64_epi128(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_pclmulqdq_v512_carrylessmultiply_v512u64_v512u64_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Pclmulqdq+V512.CarrylessMultiply");
}
#endif
