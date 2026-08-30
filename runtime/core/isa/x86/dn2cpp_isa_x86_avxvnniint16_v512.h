#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.AvxVnniInt16+V512: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64 && DN2CPP_HAS_X86_AVX10V2_INTRINSICS
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint16_v512_multiplywideningandadd_v512i32_v512i16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt16_V512, "System.Runtime.Intrinsics.X86.AvxVnniInt16+V512.MultiplyWideningAndAdd");
    return dn2cpp_isa_vec<64>(_mm512_dpwsud_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint16_v512_multiplywideningandadd_v512i32_v512i16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt16+V512.MultiplyWideningAndAdd");
}
#endif

#if DN2CPP_TARGET_X64 && DN2CPP_HAS_X86_AVX10V2_INTRINSICS
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint16_v512_multiplywideningandadd_v512i32_v512u16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt16_V512, "System.Runtime.Intrinsics.X86.AvxVnniInt16+V512.MultiplyWideningAndAdd");
    return dn2cpp_isa_vec<64>(_mm512_dpwusd_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint16_v512_multiplywideningandadd_v512i32_v512u16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt16+V512.MultiplyWideningAndAdd");
}
#endif

#if DN2CPP_TARGET_X64 && DN2CPP_HAS_X86_AVX10V2_INTRINSICS
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint16_v512_multiplywideningandadd_v512u32_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt16_V512, "System.Runtime.Intrinsics.X86.AvxVnniInt16+V512.MultiplyWideningAndAdd");
    return dn2cpp_isa_vec<64>(_mm512_dpwuud_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint16_v512_multiplywideningandadd_v512u32_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt16+V512.MultiplyWideningAndAdd");
}
#endif

#if DN2CPP_TARGET_X64 && DN2CPP_HAS_X86_AVX10V2_INTRINSICS
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint16_v512_multiplywideningandaddsaturate_v512i32_v512i16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt16_V512, "System.Runtime.Intrinsics.X86.AvxVnniInt16+V512.MultiplyWideningAndAddSaturate");
    return dn2cpp_isa_vec<64>(_mm512_dpwsuds_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint16_v512_multiplywideningandaddsaturate_v512i32_v512i16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt16+V512.MultiplyWideningAndAddSaturate");
}
#endif

#if DN2CPP_TARGET_X64 && DN2CPP_HAS_X86_AVX10V2_INTRINSICS
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint16_v512_multiplywideningandaddsaturate_v512i32_v512u16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt16_V512, "System.Runtime.Intrinsics.X86.AvxVnniInt16+V512.MultiplyWideningAndAddSaturate");
    return dn2cpp_isa_vec<64>(_mm512_dpwusds_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint16_v512_multiplywideningandaddsaturate_v512i32_v512u16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt16+V512.MultiplyWideningAndAddSaturate");
}
#endif

#if DN2CPP_TARGET_X64 && DN2CPP_HAS_X86_AVX10V2_INTRINSICS
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint16_v512_multiplywideningandaddsaturate_v512u32_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt16_V512, "System.Runtime.Intrinsics.X86.AvxVnniInt16+V512.MultiplyWideningAndAddSaturate");
    return dn2cpp_isa_vec<64>(_mm512_dpwuuds_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint16_v512_multiplywideningandaddsaturate_v512u32_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt16+V512.MultiplyWideningAndAddSaturate");
}
#endif
