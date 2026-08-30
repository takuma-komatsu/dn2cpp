#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.Arm.Dp: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+dotprod") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_dp_dotproduct_v128i32_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Dp, "System.Runtime.Intrinsics.Arm.Dp.DotProduct");
    return dn2cpp_isa_vec<16>(vdotq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1), dn2cpp_isa_bits<int8x16_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_dp_dotproduct_v128i32_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Dp.DotProduct");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+dotprod") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_dp_dotproduct_v128u32_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Dp, "System.Runtime.Intrinsics.Arm.Dp.DotProduct");
    return dn2cpp_isa_vec<16>(vdotq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1), dn2cpp_isa_bits<uint8x16_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_dp_dotproduct_v128u32_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Dp.DotProduct");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+dotprod") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_dp_dotproduct_v64i32_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Dp, "System.Runtime.Intrinsics.Arm.Dp.DotProduct");
    return dn2cpp_isa_vec<8>(vdot_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1), dn2cpp_isa_bits<int8x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_dp_dotproduct_v64i32_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Dp.DotProduct");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+dotprod") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_dp_dotproduct_v64u32_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Dp, "System.Runtime.Intrinsics.Arm.Dp.DotProduct");
    return dn2cpp_isa_vec<8>(vdot_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1), dn2cpp_isa_bits<uint8x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_dp_dotproduct_v64u32_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Dp.DotProduct");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+dotprod") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_dp_dotproductbyselectedquadruplet_v128i32_v128i8_v128i8_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Dp, "System.Runtime.Intrinsics.Arm.Dp.DotProductBySelectedQuadruplet");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<16>(vdotq_laneq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1), dn2cpp_isa_bits<int8x16_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_dp_dotproductbyselectedquadruplet_v128i32_v128i8_v128i8_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Dp.DotProductBySelectedQuadruplet");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+dotprod") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_dp_dotproductbyselectedquadruplet_v128i32_v128i8_v64i8_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Dp, "System.Runtime.Intrinsics.Arm.Dp.DotProductBySelectedQuadruplet");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<16>(vdotq_lane_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1), dn2cpp_isa_bits<int8x8_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_dp_dotproductbyselectedquadruplet_v128i32_v128i8_v64i8_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Dp.DotProductBySelectedQuadruplet");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+dotprod") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_dp_dotproductbyselectedquadruplet_v128u32_v128u8_v128u8_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Dp, "System.Runtime.Intrinsics.Arm.Dp.DotProductBySelectedQuadruplet");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<16>(vdotq_laneq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1), dn2cpp_isa_bits<uint8x16_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_dp_dotproductbyselectedquadruplet_v128u32_v128u8_v128u8_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Dp.DotProductBySelectedQuadruplet");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+dotprod") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_dp_dotproductbyselectedquadruplet_v128u32_v128u8_v64u8_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Dp, "System.Runtime.Intrinsics.Arm.Dp.DotProductBySelectedQuadruplet");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<16>(vdotq_lane_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1), dn2cpp_isa_bits<uint8x8_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_dp_dotproductbyselectedquadruplet_v128u32_v128u8_v64u8_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Dp.DotProductBySelectedQuadruplet");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+dotprod") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_dp_dotproductbyselectedquadruplet_v64i32_v64i8_v128i8_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Dp, "System.Runtime.Intrinsics.Arm.Dp.DotProductBySelectedQuadruplet");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<8>(vdot_laneq_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1), dn2cpp_isa_bits<int8x16_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_dp_dotproductbyselectedquadruplet_v64i32_v64i8_v128i8_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Dp.DotProductBySelectedQuadruplet");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+dotprod") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_dp_dotproductbyselectedquadruplet_v64i32_v64i8_v64i8_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Dp, "System.Runtime.Intrinsics.Arm.Dp.DotProductBySelectedQuadruplet");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<8>(vdot_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1), dn2cpp_isa_bits<int8x8_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_dp_dotproductbyselectedquadruplet_v64i32_v64i8_v64i8_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Dp.DotProductBySelectedQuadruplet");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+dotprod") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_dp_dotproductbyselectedquadruplet_v64u32_v64u8_v128u8_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Dp, "System.Runtime.Intrinsics.Arm.Dp.DotProductBySelectedQuadruplet");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<8>(vdot_laneq_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1), dn2cpp_isa_bits<uint8x16_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_dp_dotproductbyselectedquadruplet_v64u32_v64u8_v128u8_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Dp.DotProductBySelectedQuadruplet");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+dotprod") DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_dp_dotproductbyselectedquadruplet_v64u32_v64u8_v64u8_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Dp, "System.Runtime.Intrinsics.Arm.Dp.DotProductBySelectedQuadruplet");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<8>(vdot_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1), dn2cpp_isa_bits<uint8x8_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_dp_dotproductbyselectedquadruplet_v64u32_v64u8_v64u8_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Dp.DotProductBySelectedQuadruplet");
}
#endif
