#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.Arm.Sha1: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+sha2") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_sha1_fixedrotate_v64u32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Sha1, "System.Runtime.Intrinsics.Arm.Sha1.FixedRotate");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vsha1h_u32(vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_sha1_fixedrotate_v64u32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Sha1.FixedRotate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+sha2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_sha1_hashupdatechoose_v128u32_v64u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Sha1, "System.Runtime.Intrinsics.Arm.Sha1.HashUpdateChoose");
    return dn2cpp_isa_vec<16>(vsha1cq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a1), 0), dn2cpp_isa_bits<uint32x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_sha1_hashupdatechoose_v128u32_v64u32_v128u32(const Dn2CppVector128&, const Dn2CppVector64&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Sha1.HashUpdateChoose");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+sha2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_sha1_hashupdatemajority_v128u32_v64u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Sha1, "System.Runtime.Intrinsics.Arm.Sha1.HashUpdateMajority");
    return dn2cpp_isa_vec<16>(vsha1mq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a1), 0), dn2cpp_isa_bits<uint32x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_sha1_hashupdatemajority_v128u32_v64u32_v128u32(const Dn2CppVector128&, const Dn2CppVector64&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Sha1.HashUpdateMajority");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+sha2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_sha1_hashupdateparity_v128u32_v64u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Sha1, "System.Runtime.Intrinsics.Arm.Sha1.HashUpdateParity");
    return dn2cpp_isa_vec<16>(vsha1pq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a1), 0), dn2cpp_isa_bits<uint32x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_sha1_hashupdateparity_v128u32_v64u32_v128u32(const Dn2CppVector128&, const Dn2CppVector64&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Sha1.HashUpdateParity");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+sha2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_sha1_scheduleupdate0_v128u32_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Sha1, "System.Runtime.Intrinsics.Arm.Sha1.ScheduleUpdate0");
    return dn2cpp_isa_vec<16>(vsha1su0q_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1), dn2cpp_isa_bits<uint32x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_sha1_scheduleupdate0_v128u32_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Sha1.ScheduleUpdate0");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+sha2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_sha1_scheduleupdate1_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Sha1, "System.Runtime.Intrinsics.Arm.Sha1.ScheduleUpdate1");
    return dn2cpp_isa_vec<16>(vsha1su1q_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_sha1_scheduleupdate1_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Sha1.ScheduleUpdate1");
}
#endif
