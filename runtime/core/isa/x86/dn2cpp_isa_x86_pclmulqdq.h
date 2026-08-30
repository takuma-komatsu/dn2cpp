#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Pclmulqdq: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("pclmul") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_pclmulqdq_carrylessmultiply_v128i64_v128i64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Pclmulqdq, "System.Runtime.Intrinsics.X86.Pclmulqdq.CarrylessMultiply");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_clmulepi64_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_pclmulqdq_carrylessmultiply_v128i64_v128i64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Pclmulqdq.CarrylessMultiply");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("pclmul") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_pclmulqdq_carrylessmultiply_v128u64_v128u64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Pclmulqdq, "System.Runtime.Intrinsics.X86.Pclmulqdq.CarrylessMultiply");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_clmulepi64_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_pclmulqdq_carrylessmultiply_v128u64_v128u64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Pclmulqdq.CarrylessMultiply");
}
#endif
