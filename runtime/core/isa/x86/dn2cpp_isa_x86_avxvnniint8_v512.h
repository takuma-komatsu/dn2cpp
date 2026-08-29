#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.AvxVnniInt8+V512: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint8_v512_multiplywideningandadd_v512i32_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt8_V512, "System.Runtime.Intrinsics.X86.AvxVnniInt8+V512.MultiplyWideningAndAdd");
    return dn2cpp_isa_vec<64>(_mm512_dpbssd_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint8_v512_multiplywideningandadd_v512i32_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt8+V512.MultiplyWideningAndAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint8_v512_multiplywideningandadd_v512i32_v512i8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt8_V512, "System.Runtime.Intrinsics.X86.AvxVnniInt8+V512.MultiplyWideningAndAdd");
    return dn2cpp_isa_vec<64>(_mm512_dpbsud_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint8_v512_multiplywideningandadd_v512i32_v512i8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt8+V512.MultiplyWideningAndAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint8_v512_multiplywideningandadd_v512u32_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt8_V512, "System.Runtime.Intrinsics.X86.AvxVnniInt8+V512.MultiplyWideningAndAdd");
    return dn2cpp_isa_vec<64>(_mm512_dpbuud_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint8_v512_multiplywideningandadd_v512u32_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt8+V512.MultiplyWideningAndAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint8_v512_multiplywideningandaddsaturate_v512i32_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt8_V512, "System.Runtime.Intrinsics.X86.AvxVnniInt8+V512.MultiplyWideningAndAddSaturate");
    return dn2cpp_isa_vec<64>(_mm512_dpbssds_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint8_v512_multiplywideningandaddsaturate_v512i32_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt8+V512.MultiplyWideningAndAddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint8_v512_multiplywideningandaddsaturate_v512i32_v512i8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt8_V512, "System.Runtime.Intrinsics.X86.AvxVnniInt8+V512.MultiplyWideningAndAddSaturate");
    return dn2cpp_isa_vec<64>(_mm512_dpbsuds_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint8_v512_multiplywideningandaddsaturate_v512i32_v512i8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt8+V512.MultiplyWideningAndAddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint8_v512_multiplywideningandaddsaturate_v512u32_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt8_V512, "System.Runtime.Intrinsics.X86.AvxVnniInt8+V512.MultiplyWideningAndAddSaturate");
    return dn2cpp_isa_vec<64>(_mm512_dpbuuds_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnniint8_v512_multiplywideningandaddsaturate_v512u32_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt8+V512.MultiplyWideningAndAddSaturate");
}
#endif
