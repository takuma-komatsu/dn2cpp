#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Gfni: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("gfni") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_gfni_galoisfieldaffinetransform_v128u8_v128u8_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Gfni, "System.Runtime.Intrinsics.X86.Gfni.GaloisFieldAffineTransform");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_gf2p8affine_epi64_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_gfni_galoisfieldaffinetransform_v128u8_v128u8_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Gfni.GaloisFieldAffineTransform");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("gfni") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_gfni_galoisfieldaffinetransforminverse_v128u8_v128u8_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Gfni, "System.Runtime.Intrinsics.X86.Gfni.GaloisFieldAffineTransformInverse");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_gf2p8affineinv_epi64_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_gfni_galoisfieldaffinetransforminverse_v128u8_v128u8_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Gfni.GaloisFieldAffineTransformInverse");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("gfni") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_gfni_galoisfieldmultiply_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Gfni, "System.Runtime.Intrinsics.X86.Gfni.GaloisFieldMultiply");
    return dn2cpp_isa_vec<16>(_mm_gf2p8mul_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_gfni_galoisfieldmultiply_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Gfni.GaloisFieldMultiply");
}
#endif
