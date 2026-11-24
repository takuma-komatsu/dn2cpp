#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.AvxVnni+V512: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64 && DN2CPP_HAS_X86_VNNI_EVEX
DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnni_v512_multiplywideningandadd_v512i32_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnni_V512, "System.Runtime.Intrinsics.X86.AvxVnni+V512.MultiplyWideningAndAdd");
    return DN2CPP_ISA_VNNI_EVEX(_mm512_dpwssd, a0, a1, a2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnni_v512_multiplywideningandadd_v512i32_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnni+V512.MultiplyWideningAndAdd");
}
#endif

#if DN2CPP_TARGET_X64 && DN2CPP_HAS_X86_VNNI_EVEX
DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnni_v512_multiplywideningandadd_v512i32_v512u8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnni_V512, "System.Runtime.Intrinsics.X86.AvxVnni+V512.MultiplyWideningAndAdd");
    return DN2CPP_ISA_VNNI_EVEX(_mm512_dpbusd, a0, a1, a2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnni_v512_multiplywideningandadd_v512i32_v512u8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnni+V512.MultiplyWideningAndAdd");
}
#endif

#if DN2CPP_TARGET_X64 && DN2CPP_HAS_X86_VNNI_EVEX
DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnni_v512_multiplywideningandaddsaturate_v512i32_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnni_V512, "System.Runtime.Intrinsics.X86.AvxVnni+V512.MultiplyWideningAndAddSaturate");
    return DN2CPP_ISA_VNNI_EVEX(_mm512_dpwssds, a0, a1, a2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnni_v512_multiplywideningandaddsaturate_v512i32_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnni+V512.MultiplyWideningAndAddSaturate");
}
#endif

#if DN2CPP_TARGET_X64 && DN2CPP_HAS_X86_VNNI_EVEX
DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnni_v512_multiplywideningandaddsaturate_v512i32_v512u8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnni_V512, "System.Runtime.Intrinsics.X86.AvxVnni+V512.MultiplyWideningAndAddSaturate");
    return DN2CPP_ISA_VNNI_EVEX(_mm512_dpbusds, a0, a1, a2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avxvnni_v512_multiplywideningandaddsaturate_v512i32_v512u8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnni+V512.MultiplyWideningAndAddSaturate");
}
#endif
