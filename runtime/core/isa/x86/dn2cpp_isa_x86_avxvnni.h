#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.AvxVnni: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64 && (DN2CPP_HAS_X86_VNNI_VEX || DN2CPP_HAS_X86_VNNI_EVEX)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnni_multiplywideningandadd_v128i32_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnni, "System.Runtime.Intrinsics.X86.AvxVnni.MultiplyWideningAndAdd");
    return DN2CPP_ISA_VNNI(_mm_dpwssd, a0, a1, a2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnni_multiplywideningandadd_v128i32_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnni.MultiplyWideningAndAdd");
}
#endif

#if DN2CPP_TARGET_X64 && (DN2CPP_HAS_X86_VNNI_VEX || DN2CPP_HAS_X86_VNNI_EVEX)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnni_multiplywideningandadd_v128i32_v128u8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnni, "System.Runtime.Intrinsics.X86.AvxVnni.MultiplyWideningAndAdd");
    return DN2CPP_ISA_VNNI(_mm_dpbusd, a0, a1, a2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnni_multiplywideningandadd_v128i32_v128u8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnni.MultiplyWideningAndAdd");
}
#endif

#if DN2CPP_TARGET_X64 && (DN2CPP_HAS_X86_VNNI_VEX || DN2CPP_HAS_X86_VNNI_EVEX)
DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnni_multiplywideningandadd_v256i32_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnni, "System.Runtime.Intrinsics.X86.AvxVnni.MultiplyWideningAndAdd");
    return DN2CPP_ISA_VNNI(_mm256_dpwssd, a0, a1, a2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnni_multiplywideningandadd_v256i32_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnni.MultiplyWideningAndAdd");
}
#endif

#if DN2CPP_TARGET_X64 && (DN2CPP_HAS_X86_VNNI_VEX || DN2CPP_HAS_X86_VNNI_EVEX)
DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnni_multiplywideningandadd_v256i32_v256u8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnni, "System.Runtime.Intrinsics.X86.AvxVnni.MultiplyWideningAndAdd");
    return DN2CPP_ISA_VNNI(_mm256_dpbusd, a0, a1, a2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnni_multiplywideningandadd_v256i32_v256u8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnni.MultiplyWideningAndAdd");
}
#endif

#if DN2CPP_TARGET_X64 && (DN2CPP_HAS_X86_VNNI_VEX || DN2CPP_HAS_X86_VNNI_EVEX)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnni_multiplywideningandaddsaturate_v128i32_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnni, "System.Runtime.Intrinsics.X86.AvxVnni.MultiplyWideningAndAddSaturate");
    return DN2CPP_ISA_VNNI(_mm_dpwssds, a0, a1, a2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnni_multiplywideningandaddsaturate_v128i32_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnni.MultiplyWideningAndAddSaturate");
}
#endif

#if DN2CPP_TARGET_X64 && (DN2CPP_HAS_X86_VNNI_VEX || DN2CPP_HAS_X86_VNNI_EVEX)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnni_multiplywideningandaddsaturate_v128i32_v128u8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnni, "System.Runtime.Intrinsics.X86.AvxVnni.MultiplyWideningAndAddSaturate");
    return DN2CPP_ISA_VNNI(_mm_dpbusds, a0, a1, a2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnni_multiplywideningandaddsaturate_v128i32_v128u8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnni.MultiplyWideningAndAddSaturate");
}
#endif

#if DN2CPP_TARGET_X64 && (DN2CPP_HAS_X86_VNNI_VEX || DN2CPP_HAS_X86_VNNI_EVEX)
DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnni_multiplywideningandaddsaturate_v256i32_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnni, "System.Runtime.Intrinsics.X86.AvxVnni.MultiplyWideningAndAddSaturate");
    return DN2CPP_ISA_VNNI(_mm256_dpwssds, a0, a1, a2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnni_multiplywideningandaddsaturate_v256i32_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnni.MultiplyWideningAndAddSaturate");
}
#endif

#if DN2CPP_TARGET_X64 && (DN2CPP_HAS_X86_VNNI_VEX || DN2CPP_HAS_X86_VNNI_EVEX)
DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnni_multiplywideningandaddsaturate_v256i32_v256u8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnni, "System.Runtime.Intrinsics.X86.AvxVnni.MultiplyWideningAndAddSaturate");
    return DN2CPP_ISA_VNNI(_mm256_dpbusds, a0, a1, a2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnni_multiplywideningandaddsaturate_v256i32_v256u8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnni.MultiplyWideningAndAddSaturate");
}
#endif
