#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.Arm.ArmBase: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_arm_armbase_leadingzerocount_i32(int32_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_ArmBase, "System.Runtime.Intrinsics.Arm.ArmBase.LeadingZeroCount");
    return dn2cpp_isa_clz32((uint32_t)a0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_arm_armbase_leadingzerocount_i32(int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.ArmBase.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_arm_armbase_leadingzerocount_u32(uint32_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_ArmBase, "System.Runtime.Intrinsics.Arm.ArmBase.LeadingZeroCount");
    return dn2cpp_isa_clz32(a0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_arm_armbase_leadingzerocount_u32(uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.ArmBase.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_arm_armbase_reverseelementbits_i32(int32_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_ArmBase, "System.Runtime.Intrinsics.Arm.ArmBase.ReverseElementBits");
    return (int32_t)dn2cpp_isa_rbit32((uint32_t)a0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_arm_armbase_reverseelementbits_i32(int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.ArmBase.ReverseElementBits");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_arm_armbase_reverseelementbits_u32(uint32_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_ArmBase, "System.Runtime.Intrinsics.Arm.ArmBase.ReverseElementBits");
    return dn2cpp_isa_rbit32(a0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_arm_armbase_reverseelementbits_u32(uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.ArmBase.ReverseElementBits");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_armbase_yield()
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_ArmBase, "System.Runtime.Intrinsics.Arm.ArmBase.Yield");
    dn2cpp_isa_arm_yield();
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_armbase_yield()
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.ArmBase.Yield");
}
#endif
