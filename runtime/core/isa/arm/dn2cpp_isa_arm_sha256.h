#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.Arm.Sha256: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+sha2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_sha256_hashupdate1_v128u32_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Sha256, "System.Runtime.Intrinsics.Arm.Sha256.HashUpdate1");
    return dn2cpp_isa_vec<16>(vsha256hq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1), dn2cpp_isa_bits<uint32x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_sha256_hashupdate1_v128u32_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Sha256.HashUpdate1");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+sha2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_sha256_hashupdate2_v128u32_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Sha256, "System.Runtime.Intrinsics.Arm.Sha256.HashUpdate2");
    return dn2cpp_isa_vec<16>(vsha256h2q_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1), dn2cpp_isa_bits<uint32x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_sha256_hashupdate2_v128u32_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Sha256.HashUpdate2");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+sha2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_sha256_scheduleupdate0_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Sha256, "System.Runtime.Intrinsics.Arm.Sha256.ScheduleUpdate0");
    return dn2cpp_isa_vec<16>(vsha256su0q_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_sha256_scheduleupdate0_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Sha256.ScheduleUpdate0");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+sha2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_sha256_scheduleupdate1_v128u32_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Sha256, "System.Runtime.Intrinsics.Arm.Sha256.ScheduleUpdate1");
    return dn2cpp_isa_vec<16>(vsha256su1q_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1), dn2cpp_isa_bits<uint32x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_sha256_scheduleupdate1_v128u32_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Sha256.ScheduleUpdate1");
}
#endif
