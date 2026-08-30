#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.Arm.Crc32: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+crc") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_arm_crc32_computecrc32_u32_u16(uint32_t a0, uint16_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Crc32, "System.Runtime.Intrinsics.Arm.Crc32.ComputeCrc32");
    return __crc32h(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_arm_crc32_computecrc32_u32_u16(uint32_t, uint16_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Crc32.ComputeCrc32");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+crc") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_arm_crc32_computecrc32_u32_u32(uint32_t a0, uint32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Crc32, "System.Runtime.Intrinsics.Arm.Crc32.ComputeCrc32");
    return __crc32w(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_arm_crc32_computecrc32_u32_u32(uint32_t, uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Crc32.ComputeCrc32");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+crc") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_arm_crc32_computecrc32_u32_u8(uint32_t a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Crc32, "System.Runtime.Intrinsics.Arm.Crc32.ComputeCrc32");
    return __crc32b(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_arm_crc32_computecrc32_u32_u8(uint32_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Crc32.ComputeCrc32");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+crc") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_arm_crc32_computecrc32c_u32_u16(uint32_t a0, uint16_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Crc32, "System.Runtime.Intrinsics.Arm.Crc32.ComputeCrc32C");
    return __crc32ch(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_arm_crc32_computecrc32c_u32_u16(uint32_t, uint16_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Crc32.ComputeCrc32C");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+crc") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_arm_crc32_computecrc32c_u32_u32(uint32_t a0, uint32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Crc32, "System.Runtime.Intrinsics.Arm.Crc32.ComputeCrc32C");
    return __crc32cw(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_arm_crc32_computecrc32c_u32_u32(uint32_t, uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Crc32.ComputeCrc32C");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+crc") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_arm_crc32_computecrc32c_u32_u8(uint32_t a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Crc32, "System.Runtime.Intrinsics.Arm.Crc32.ComputeCrc32C");
    return __crc32cb(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_arm_crc32_computecrc32c_u32_u8(uint32_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Crc32.ComputeCrc32C");
}
#endif
