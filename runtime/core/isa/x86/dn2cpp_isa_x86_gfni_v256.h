#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Gfni+V256: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx,gfni") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_gfni_v256_galoisfieldaffinetransform_v256u8_v256u8_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Gfni_V256, "System.Runtime.Intrinsics.X86.Gfni+V256.GaloisFieldAffineTransform");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_gf2p8affine_epi64_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_gfni_v256_galoisfieldaffinetransform_v256u8_v256u8_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Gfni+V256.GaloisFieldAffineTransform");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx,gfni") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_gfni_v256_galoisfieldaffinetransforminverse_v256u8_v256u8_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Gfni_V256, "System.Runtime.Intrinsics.X86.Gfni+V256.GaloisFieldAffineTransformInverse");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_gf2p8affineinv_epi64_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_gfni_v256_galoisfieldaffinetransforminverse_v256u8_v256u8_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Gfni+V256.GaloisFieldAffineTransformInverse");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx,gfni") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_gfni_v256_galoisfieldmultiply_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Gfni_V256, "System.Runtime.Intrinsics.X86.Gfni+V256.GaloisFieldMultiply");
    return dn2cpp_isa_vec<32>(_mm256_gf2p8mul_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_gfni_v256_galoisfieldmultiply_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Gfni+V256.GaloisFieldMultiply");
}
#endif
