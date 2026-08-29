#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.Arm.Rdm+Arm64: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingandaddsaturatehighscalar_v64i16_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm_Arm64, "System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingAndAddSaturateHighScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrdmlahh_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a2), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingandaddsaturatehighscalar_v64i16_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingAndAddSaturateHighScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingandaddsaturatehighscalar_v64i32_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm_Arm64, "System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingAndAddSaturateHighScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrdmlahs_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a2), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingandaddsaturatehighscalar_v64i32_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingAndAddSaturateHighScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingandsubtractsaturatehighscalar_v64i16_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm_Arm64, "System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingAndSubtractSaturateHighScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrdmlshh_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a2), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingandsubtractsaturatehighscalar_v64i16_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingAndSubtractSaturateHighScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingandsubtractsaturatehighscalar_v64i32_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm_Arm64, "System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingAndSubtractSaturateHighScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrdmlshs_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a2), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingandsubtractsaturatehighscalar_v64i32_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingAndSubtractSaturateHighScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingscalarbyselectedscalarandaddsaturatehigh_v64i16_v64i16_v128i16_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm_Arm64, "System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrdmlahh_laneq_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0), dn2cpp_isa_bits<int16x8_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingscalarbyselectedscalarandaddsaturatehigh_v64i16_v64i16_v128i16_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingscalarbyselectedscalarandaddsaturatehigh_v64i16_v64i16_v64i16_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm_Arm64, "System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrdmlahh_lane_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0), dn2cpp_isa_bits<int16x4_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingscalarbyselectedscalarandaddsaturatehigh_v64i16_v64i16_v64i16_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingscalarbyselectedscalarandaddsaturatehigh_v64i32_v64i32_v128i32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm_Arm64, "System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrdmlahs_laneq_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0), dn2cpp_isa_bits<int32x4_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingscalarbyselectedscalarandaddsaturatehigh_v64i32_v64i32_v128i32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingscalarbyselectedscalarandaddsaturatehigh_v64i32_v64i32_v64i32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm_Arm64, "System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrdmlahs_lane_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0), dn2cpp_isa_bits<int32x2_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingscalarbyselectedscalarandaddsaturatehigh_v64i32_v64i32_v64i32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingscalarbyselectedscalarandsubtractsaturatehigh_v64i16_v64i16_v128i16_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm_Arm64, "System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrdmlshh_laneq_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0), dn2cpp_isa_bits<int16x8_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingscalarbyselectedscalarandsubtractsaturatehigh_v64i16_v64i16_v128i16_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingscalarbyselectedscalarandsubtractsaturatehigh_v64i16_v64i16_v64i16_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm_Arm64, "System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrdmlshh_lane_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0), dn2cpp_isa_bits<int16x4_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingscalarbyselectedscalarandsubtractsaturatehigh_v64i16_v64i16_v64i16_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingscalarbyselectedscalarandsubtractsaturatehigh_v64i32_v64i32_v128i32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm_Arm64, "System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrdmlshs_laneq_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0), dn2cpp_isa_bits<int32x4_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingscalarbyselectedscalarandsubtractsaturatehigh_v64i32_v64i32_v128i32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+rdm") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingscalarbyselectedscalarandsubtractsaturatehigh_v64i32_v64i32_v64i32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Rdm_Arm64, "System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrdmlshs_lane_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0), dn2cpp_isa_bits<int32x2_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_rdm_arm64_multiplyroundeddoublingscalarbyselectedscalarandsubtractsaturatehigh_v64i32_v64i32_v64i32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Rdm+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh");
}
#endif
