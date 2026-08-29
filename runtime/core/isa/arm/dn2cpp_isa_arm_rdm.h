#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.Arm.Rdm: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingandaddsaturatehigh_v128i16_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingAndAddSaturateHigh");
    return dn2cpp_isa_vec<16>(vqrdmlahq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1), dn2cpp_isa_bits<int16x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingandaddsaturatehigh_v128i16_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingAndAddSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingandaddsaturatehigh_v128i32_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingAndAddSaturateHigh");
    return dn2cpp_isa_vec<16>(vqrdmlahq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1), dn2cpp_isa_bits<int32x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingandaddsaturatehigh_v128i32_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingAndAddSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingandaddsaturatehigh_v64i16_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingAndAddSaturateHigh");
    return dn2cpp_isa_vec<8>(vqrdmlah_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1), dn2cpp_isa_bits<int16x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingandaddsaturatehigh_v64i16_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingAndAddSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingandaddsaturatehigh_v64i32_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingAndAddSaturateHigh");
    return dn2cpp_isa_vec<8>(vqrdmlah_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1), dn2cpp_isa_bits<int32x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingandaddsaturatehigh_v64i32_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingAndAddSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingandsubtractsaturatehigh_v128i16_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingAndSubtractSaturateHigh");
    return dn2cpp_isa_vec<16>(vqrdmlshq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1), dn2cpp_isa_bits<int16x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingandsubtractsaturatehigh_v128i16_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingAndSubtractSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingandsubtractsaturatehigh_v128i32_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingAndSubtractSaturateHigh");
    return dn2cpp_isa_vec<16>(vqrdmlshq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1), dn2cpp_isa_bits<int32x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingandsubtractsaturatehigh_v128i32_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingAndSubtractSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingandsubtractsaturatehigh_v64i16_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingAndSubtractSaturateHigh");
    return dn2cpp_isa_vec<8>(vqrdmlsh_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1), dn2cpp_isa_bits<int16x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingandsubtractsaturatehigh_v64i16_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingAndSubtractSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingandsubtractsaturatehigh_v64i32_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingAndSubtractSaturateHigh");
    return dn2cpp_isa_vec<8>(vqrdmlsh_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1), dn2cpp_isa_bits<int32x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingandsubtractsaturatehigh_v64i32_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingAndSubtractSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandaddsaturatehigh_v128i16_v128i16_v128i16_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a3, dn2cpp_isa_vec<16>(vqrdmlahq_laneq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1), dn2cpp_isa_bits<int16x8_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandaddsaturatehigh_v128i16_v128i16_v128i16_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandaddsaturatehigh_v128i16_v128i16_v64i16_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<16>(vqrdmlahq_lane_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1), dn2cpp_isa_bits<int16x4_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandaddsaturatehigh_v128i16_v128i16_v64i16_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandaddsaturatehigh_v128i32_v128i32_v128i32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<16>(vqrdmlahq_laneq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1), dn2cpp_isa_bits<int32x4_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandaddsaturatehigh_v128i32_v128i32_v128i32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandaddsaturatehigh_v128i32_v128i32_v64i32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<16>(vqrdmlahq_lane_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1), dn2cpp_isa_bits<int32x2_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandaddsaturatehigh_v128i32_v128i32_v64i32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandaddsaturatehigh_v64i16_v64i16_v128i16_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a3, dn2cpp_isa_vec<8>(vqrdmlah_laneq_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1), dn2cpp_isa_bits<int16x8_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandaddsaturatehigh_v64i16_v64i16_v128i16_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandaddsaturatehigh_v64i16_v64i16_v64i16_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<8>(vqrdmlah_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1), dn2cpp_isa_bits<int16x4_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandaddsaturatehigh_v64i16_v64i16_v64i16_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandaddsaturatehigh_v64i32_v64i32_v128i32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<8>(vqrdmlah_laneq_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1), dn2cpp_isa_bits<int32x4_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandaddsaturatehigh_v64i32_v64i32_v128i32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandaddsaturatehigh_v64i32_v64i32_v64i32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<8>(vqrdmlah_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1), dn2cpp_isa_bits<int32x2_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandaddsaturatehigh_v64i32_v64i32_v64i32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandsubtractsaturatehigh_v128i16_v128i16_v128i16_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a3, dn2cpp_isa_vec<16>(vqrdmlshq_laneq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1), dn2cpp_isa_bits<int16x8_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandsubtractsaturatehigh_v128i16_v128i16_v128i16_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandsubtractsaturatehigh_v128i16_v128i16_v64i16_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<16>(vqrdmlshq_lane_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1), dn2cpp_isa_bits<int16x4_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandsubtractsaturatehigh_v128i16_v128i16_v64i16_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandsubtractsaturatehigh_v128i32_v128i32_v128i32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<16>(vqrdmlshq_laneq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1), dn2cpp_isa_bits<int32x4_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandsubtractsaturatehigh_v128i32_v128i32_v128i32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandsubtractsaturatehigh_v128i32_v128i32_v64i32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<16>(vqrdmlshq_lane_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1), dn2cpp_isa_bits<int32x2_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandsubtractsaturatehigh_v128i32_v128i32_v64i32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandsubtractsaturatehigh_v64i16_v64i16_v128i16_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a3, dn2cpp_isa_vec<8>(vqrdmlsh_laneq_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1), dn2cpp_isa_bits<int16x8_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandsubtractsaturatehigh_v64i16_v64i16_v128i16_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandsubtractsaturatehigh_v64i16_v64i16_v64i16_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<8>(vqrdmlsh_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1), dn2cpp_isa_bits<int16x4_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandsubtractsaturatehigh_v64i16_v64i16_v64i16_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandsubtractsaturatehigh_v64i32_v64i32_v128i32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<8>(vqrdmlsh_laneq_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1), dn2cpp_isa_bits<int32x4_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandsubtractsaturatehigh_v64i32_v64i32_v128i32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandsubtractsaturatehigh_v64i32_v64i32_v64i32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm, "System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<8>(vqrdmlsh_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1), dn2cpp_isa_bits<int32x2_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_multiplyroundeddoublingbyselectedscalarandsubtractsaturatehigh_v64i32_v64i32_v64i32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm.MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh");
}
#endif
