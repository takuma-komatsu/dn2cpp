#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.Arm.ArmBase+Arm64: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_arm_armbase_arm64_leadingsigncount_i32(int32_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_ArmBase_Arm64, "System.Runtime.Intrinsics.Arm.ArmBase+Arm64.LeadingSignCount");
    return dn2cpp_isa_cls32(a0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_arm_armbase_arm64_leadingsigncount_i32(int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.ArmBase+Arm64.LeadingSignCount");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_arm_armbase_arm64_leadingsigncount_i64(int64_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_ArmBase_Arm64, "System.Runtime.Intrinsics.Arm.ArmBase+Arm64.LeadingSignCount");
    return dn2cpp_isa_cls64(a0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_arm_armbase_arm64_leadingsigncount_i64(int64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.ArmBase+Arm64.LeadingSignCount");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_arm_armbase_arm64_leadingzerocount_i64(int64_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_ArmBase_Arm64, "System.Runtime.Intrinsics.Arm.ArmBase+Arm64.LeadingZeroCount");
    return dn2cpp_isa_clz64((uint64_t)a0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_arm_armbase_arm64_leadingzerocount_i64(int64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.ArmBase+Arm64.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_arm_armbase_arm64_leadingzerocount_u64(uint64_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_ArmBase_Arm64, "System.Runtime.Intrinsics.Arm.ArmBase+Arm64.LeadingZeroCount");
    return dn2cpp_isa_clz64(a0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_arm_armbase_arm64_leadingzerocount_u64(uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.ArmBase+Arm64.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE int64_t dn2cpp_isa_arm_armbase_arm64_multiplyhigh_i64_i64(int64_t a0, int64_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_ArmBase_Arm64, "System.Runtime.Intrinsics.Arm.ArmBase+Arm64.MultiplyHigh");
    return dn2cpp_isa_smulh64(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int64_t dn2cpp_isa_arm_armbase_arm64_multiplyhigh_i64_i64(int64_t, int64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.ArmBase+Arm64.MultiplyHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_arm_armbase_arm64_multiplyhigh_u64_u64(uint64_t a0, uint64_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_ArmBase_Arm64, "System.Runtime.Intrinsics.Arm.ArmBase+Arm64.MultiplyHigh");
    return dn2cpp_isa_umulh64(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_arm_armbase_arm64_multiplyhigh_u64_u64(uint64_t, uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.ArmBase+Arm64.MultiplyHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE int64_t dn2cpp_isa_arm_armbase_arm64_reverseelementbits_i64(int64_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_ArmBase_Arm64, "System.Runtime.Intrinsics.Arm.ArmBase+Arm64.ReverseElementBits");
    return (int64_t)dn2cpp_isa_rbit64((uint64_t)a0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int64_t dn2cpp_isa_arm_armbase_arm64_reverseelementbits_i64(int64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.ArmBase+Arm64.ReverseElementBits");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_arm_armbase_arm64_reverseelementbits_u64(uint64_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_ArmBase_Arm64, "System.Runtime.Intrinsics.Arm.ArmBase+Arm64.ReverseElementBits");
    return dn2cpp_isa_rbit64(a0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_arm_armbase_arm64_reverseelementbits_u64(uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.ArmBase+Arm64.ReverseElementBits");
}
#endif
