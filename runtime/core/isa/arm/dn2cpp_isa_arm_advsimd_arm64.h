#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.Arm.AdvSimd+Arm64: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_abs_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Abs");
    return dn2cpp_isa_vec<16>(vabsq_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_abs_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Abs");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_abs_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Abs");
    return dn2cpp_isa_vec<16>(vabsq_s64(dn2cpp_isa_bits<int64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_abs_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Abs");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_absolutecomparegreaterthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareGreaterThan");
    return dn2cpp_isa_vec<16>(vcagtq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_absolutecomparegreaterthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_absolutecomparegreaterthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(vcageq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_absolutecomparegreaterthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutecomparegreaterthanorequalscalar_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareGreaterThanOrEqualScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcages_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutecomparegreaterthanorequalscalar_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareGreaterThanOrEqualScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutecomparegreaterthanorequalscalar_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareGreaterThanOrEqualScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcaged_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0), vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutecomparegreaterthanorequalscalar_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareGreaterThanOrEqualScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutecomparegreaterthanscalar_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareGreaterThanScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcagts_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutecomparegreaterthanscalar_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareGreaterThanScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutecomparegreaterthanscalar_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareGreaterThanScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcagtd_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0), vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutecomparegreaterthanscalar_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareGreaterThanScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_absolutecomparelessthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareLessThan");
    return dn2cpp_isa_vec<16>(vcaltq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_absolutecomparelessthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareLessThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_absolutecomparelessthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(vcaleq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_absolutecomparelessthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutecomparelessthanorequalscalar_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareLessThanOrEqualScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcales_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutecomparelessthanorequalscalar_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareLessThanOrEqualScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutecomparelessthanorequalscalar_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareLessThanOrEqualScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcaled_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0), vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutecomparelessthanorequalscalar_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareLessThanOrEqualScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutecomparelessthanscalar_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareLessThanScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcalts_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutecomparelessthanscalar_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareLessThanScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutecomparelessthanscalar_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareLessThanScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcaltd_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0), vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutecomparelessthanscalar_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteCompareLessThanScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_absolutedifference_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteDifference");
    return dn2cpp_isa_vec<16>(vabdq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_absolutedifference_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteDifference");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutedifferencescalar_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteDifferenceScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vabds_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutedifferencescalar_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteDifferenceScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutedifferencescalar_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteDifferenceScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vabdd_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0), vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absolutedifferencescalar_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsoluteDifferenceScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_abssaturate_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsSaturate");
    return dn2cpp_isa_vec<16>(vqabsq_s64(dn2cpp_isa_bits<int64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_abssaturate_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_abssaturatescalar_v64i16(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqabsh_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_abssaturatescalar_v64i16(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_abssaturatescalar_v64i32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqabss_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_abssaturatescalar_v64i32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_abssaturatescalar_v64i64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqabsd_s64(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_abssaturatescalar_v64i64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_abssaturatescalar_v64i8(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqabsb_s8(vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_abssaturatescalar_v64i8(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absscalar_v64i64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsScalar");
    return dn2cpp_isa_vec<8>(vabs_s64(dn2cpp_isa_bits<int64x1_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_absscalar_v64i64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AbsScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_add_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Add");
    return dn2cpp_isa_vec<16>(vaddq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_add_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Add");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddvq_s16(dn2cpp_isa_bits<int16x8_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddvq_s32(dn2cpp_isa_bits<int32x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddvq_s8(dn2cpp_isa_bits<int8x16_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddvq_u16(dn2cpp_isa_bits<uint16x8_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddvq_u32(dn2cpp_isa_bits<uint32x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddvq_u8(dn2cpp_isa_bits<uint8x16_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v64i16(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddv_s16(dn2cpp_isa_bits<int16x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v64i16(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v64i8(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddv_s8(dn2cpp_isa_bits<int8x8_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v64i8(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v64u16(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddv_u16(dn2cpp_isa_bits<uint16x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v64u16(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v64u8(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddv_u8(dn2cpp_isa_bits<uint8x8_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacross_v64u8(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddlvq_s16(dn2cpp_isa_bits<int16x8_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddlvq_s32(dn2cpp_isa_bits<int32x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddlvq_s8(dn2cpp_isa_bits<int8x16_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddlvq_u16(dn2cpp_isa_bits<uint16x8_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddlvq_u32(dn2cpp_isa_bits<uint32x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddlvq_u8(dn2cpp_isa_bits<uint8x16_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v64i16(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddlv_s16(dn2cpp_isa_bits<int16x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v64i16(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v64i8(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddlv_s8(dn2cpp_isa_bits<int8x8_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v64i8(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v64u16(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddlv_u16(dn2cpp_isa_bits<uint16x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v64u16(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v64u8(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vaddlv_u8(dn2cpp_isa_bits<uint8x8_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addacrosswidening_v64u8(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddAcrossWidening");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
    return dn2cpp_isa_vec<16>(vpaddq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
    return dn2cpp_isa_vec<16>(vpaddq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
    return dn2cpp_isa_vec<16>(vpaddq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
    return dn2cpp_isa_vec<16>(vpaddq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
    return dn2cpp_isa_vec<16>(vpaddq_s64(dn2cpp_isa_bits<int64x2_t>(a0), dn2cpp_isa_bits<int64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
    return dn2cpp_isa_vec<16>(vpaddq_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
    return dn2cpp_isa_vec<16>(vpaddq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
    return dn2cpp_isa_vec<16>(vpaddq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
    return dn2cpp_isa_vec<16>(vpaddq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
    return dn2cpp_isa_vec<16>(vpaddq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addpairwise_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addpairwisescalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwiseScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vpaddd_f64(dn2cpp_isa_bits<float64x2_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addpairwisescalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwiseScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addpairwisescalar_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwiseScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vpaddd_s64(dn2cpp_isa_bits<int64x2_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addpairwisescalar_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwiseScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addpairwisescalar_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwiseScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vpaddd_u64(dn2cpp_isa_bits<uint64x2_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addpairwisescalar_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwiseScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addpairwisescalar_v64f32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwiseScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vpadds_f32(dn2cpp_isa_bits<float32x2_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addpairwisescalar_v64f32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddPairwiseScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v128i16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
    return dn2cpp_isa_vec<16>(vuqaddq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v128i16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v128i32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
    return dn2cpp_isa_vec<16>(vuqaddq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v128i32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v128i64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
    return dn2cpp_isa_vec<16>(vuqaddq_s64(dn2cpp_isa_bits<int64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v128i64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v128i8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
    return dn2cpp_isa_vec<16>(vuqaddq_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v128i8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v128u16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
    return dn2cpp_isa_vec<16>(vsqaddq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v128u16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v128u32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
    return dn2cpp_isa_vec<16>(vsqaddq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v128u32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v128u64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
    return dn2cpp_isa_vec<16>(vsqaddq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<int64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v128u64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v128u8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
    return dn2cpp_isa_vec<16>(vsqaddq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v128u8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v64i16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
    return dn2cpp_isa_vec<8>(vuqadd_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v64i16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v64i32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
    return dn2cpp_isa_vec<8>(vuqadd_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v64i32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v64i8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
    return dn2cpp_isa_vec<8>(vuqadd_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v64i8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v64u16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
    return dn2cpp_isa_vec<8>(vsqadd_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v64u16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v64u32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
    return dn2cpp_isa_vec<8>(vsqadd_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v64u32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v64u8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
    return dn2cpp_isa_vec<8>(vsqadd_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturate_v64u8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqaddh_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64i16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vuqaddh_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), vget_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64i16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqadds_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64i32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vuqadds_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64i32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64i64_v64u64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vuqaddd_s64(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0), vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64i64_v64u64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqaddb_s8(vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a0), 0), vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64i8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vuqaddb_s8(vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a0), 0), vget_lane_u8(dn2cpp_isa_bits<uint8x8_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64i8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64u16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vsqaddh_u16(vget_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64u16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqaddh_u16(vget_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a0), 0), vget_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64u32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vsqadds_u32(vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64u32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqadds_u32(vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), 0), vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64u64_v64i64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vsqaddd_u64(vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a0), 0), vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64u64_v64i64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64u8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vsqaddb_u8(vget_lane_u8(dn2cpp_isa_bits<uint8x8_t>(a0), 0), vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64u8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqaddb_u8(vget_lane_u8(dn2cpp_isa_bits<uint8x8_t>(a0), 0), vget_lane_u8(dn2cpp_isa_bits<uint8x8_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_addsaturatescalar_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.AddSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ceiling_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Ceiling");
    return dn2cpp_isa_vec<16>(vrndpq_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ceiling_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Ceiling");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_compareequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareEqual");
    return dn2cpp_isa_vec<16>(vceqq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_compareequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_compareequal_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareEqual");
    return dn2cpp_isa_vec<16>(vceqq_s64(dn2cpp_isa_bits<int64x2_t>(a0), dn2cpp_isa_bits<int64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_compareequal_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_compareequal_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareEqual");
    return dn2cpp_isa_vec<16>(vceqq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_compareequal_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_compareequalscalar_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareEqualScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vceqs_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_compareequalscalar_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareEqualScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_compareequalscalar_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareEqualScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vceqd_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0), vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_compareequalscalar_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareEqualScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_compareequalscalar_v64i64_v64i64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareEqualScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vceqd_s64(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0), vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_compareequalscalar_v64i64_v64i64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareEqualScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_compareequalscalar_v64u64_v64u64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareEqualScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vceqd_u64(vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a0), 0), vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_compareequalscalar_v64u64_v64u64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareEqualScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(vcgtq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthan_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(vcgtq_s64(dn2cpp_isa_bits<int64x2_t>(a0), dn2cpp_isa_bits<int64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthan_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthan_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(vcgtq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthan_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(vcgeq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanorequal_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(vcgeq_s64(dn2cpp_isa_bits<int64x2_t>(a0), dn2cpp_isa_bits<int64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanorequal_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanorequal_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(vcgeq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanorequal_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanorequalscalar_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanOrEqualScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcges_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanorequalscalar_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanOrEqualScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanorequalscalar_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanOrEqualScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcged_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0), vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanorequalscalar_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanOrEqualScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanorequalscalar_v64i64_v64i64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanOrEqualScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcged_s64(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0), vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanorequalscalar_v64i64_v64i64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanOrEqualScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanorequalscalar_v64u64_v64u64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanOrEqualScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcged_u64(vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a0), 0), vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanorequalscalar_v64u64_v64u64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanOrEqualScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanscalar_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcgts_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanscalar_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanscalar_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcgtd_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0), vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanscalar_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanscalar_v64i64_v64i64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcgtd_s64(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0), vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanscalar_v64i64_v64i64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanscalar_v64u64_v64u64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcgtd_u64(vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a0), 0), vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparegreaterthanscalar_v64u64_v64u64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareGreaterThanScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparelessthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThan");
    return dn2cpp_isa_vec<16>(vcltq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparelessthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparelessthan_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThan");
    return dn2cpp_isa_vec<16>(vcltq_s64(dn2cpp_isa_bits<int64x2_t>(a0), dn2cpp_isa_bits<int64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparelessthan_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparelessthan_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThan");
    return dn2cpp_isa_vec<16>(vcltq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparelessthan_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparelessthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(vcleq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparelessthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparelessthanorequal_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(vcleq_s64(dn2cpp_isa_bits<int64x2_t>(a0), dn2cpp_isa_bits<int64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparelessthanorequal_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparelessthanorequal_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(vcleq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparelessthanorequal_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparelessthanorequalscalar_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanOrEqualScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcles_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparelessthanorequalscalar_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanOrEqualScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparelessthanorequalscalar_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanOrEqualScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcled_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0), vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparelessthanorequalscalar_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanOrEqualScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparelessthanorequalscalar_v64i64_v64i64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanOrEqualScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcled_s64(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0), vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparelessthanorequalscalar_v64i64_v64i64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanOrEqualScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparelessthanorequalscalar_v64u64_v64u64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanOrEqualScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcled_u64(vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a0), 0), vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparelessthanorequalscalar_v64u64_v64u64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanOrEqualScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparelessthanscalar_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vclts_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparelessthanscalar_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparelessthanscalar_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcltd_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0), vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparelessthanscalar_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparelessthanscalar_v64i64_v64i64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcltd_s64(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0), vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparelessthanscalar_v64i64_v64i64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparelessthanscalar_v64u64_v64u64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vcltd_u64(vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a0), 0), vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparelessthanscalar_v64u64_v64u64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareLessThanScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparetest_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareTest");
    return dn2cpp_isa_vec<16>(vtstq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparetest_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareTest");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparetest_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareTest");
    return dn2cpp_isa_vec<16>(vtstq_s64(dn2cpp_isa_bits<int64x2_t>(a0), dn2cpp_isa_bits<int64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparetest_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareTest");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparetest_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareTest");
    return dn2cpp_isa_vec<16>(vtstq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_comparetest_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareTest");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparetestscalar_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareTestScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vtstd_u64(vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a0), 0), vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparetestscalar_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareTestScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparetestscalar_v64i64_v64i64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareTestScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vtstd_s64(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0), vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparetestscalar_v64i64_v64i64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareTestScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparetestscalar_v64u64_v64u64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareTestScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vtstd_u64(vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a0), 0), vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_comparetestscalar_v64u64_v64u64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.CompareTestScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttodouble_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToDouble");
    return dn2cpp_isa_vec<16>(vcvtq_f64_s64(dn2cpp_isa_bits<int64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttodouble_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToDouble");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttodouble_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToDouble");
    return dn2cpp_isa_vec<16>(vcvtq_f64_u64(dn2cpp_isa_bits<uint64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttodouble_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToDouble");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttodouble_v64f32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToDouble");
    return dn2cpp_isa_vec<16>(vcvt_f64_f32(dn2cpp_isa_bits<float32x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttodouble_v64f32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToDouble");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttodoublescalar_v64i64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToDoubleScalar");
    return dn2cpp_isa_vec<8>(vcvt_f64_s64(dn2cpp_isa_bits<int64x1_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttodoublescalar_v64i64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToDoubleScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttodoublescalar_v64u64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToDoubleScalar");
    return dn2cpp_isa_vec<8>(vcvt_f64_u64(dn2cpp_isa_bits<uint64x1_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttodoublescalar_v64u64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToDoubleScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttodoubleupper_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToDoubleUpper");
    return dn2cpp_isa_vec<16>(vcvt_high_f64_f32(dn2cpp_isa_bits<float32x4_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttodoubleupper_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToDoubleUpper");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundawayfromzero_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundAwayFromZero");
    return dn2cpp_isa_vec<16>(vcvtaq_s64_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundawayfromzero_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundAwayFromZero");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundawayfromzeroscalar_v64f64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundAwayFromZeroScalar");
    return dn2cpp_isa_vec<8>(vcvta_s64_f64(dn2cpp_isa_bits<float64x1_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundawayfromzeroscalar_v64f64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundAwayFromZeroScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundtoeven_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundToEven");
    return dn2cpp_isa_vec<16>(vcvtnq_s64_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundtoeven_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundToEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundtoevenscalar_v64f64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundToEvenScalar");
    return dn2cpp_isa_vec<8>(vcvtn_s64_f64(dn2cpp_isa_bits<float64x1_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundtoevenscalar_v64f64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundToEvenScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundtonegativeinfinity_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundToNegativeInfinity");
    return dn2cpp_isa_vec<16>(vcvtmq_s64_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundtonegativeinfinity_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundToNegativeInfinity");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundtonegativeinfinityscalar_v64f64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundToNegativeInfinityScalar");
    return dn2cpp_isa_vec<8>(vcvtm_s64_f64(dn2cpp_isa_bits<float64x1_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundtonegativeinfinityscalar_v64f64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundToNegativeInfinityScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundtopositiveinfinity_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundToPositiveInfinity");
    return dn2cpp_isa_vec<16>(vcvtpq_s64_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundtopositiveinfinity_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundToPositiveInfinity");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundtopositiveinfinityscalar_v64f64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundToPositiveInfinityScalar");
    return dn2cpp_isa_vec<8>(vcvtp_s64_f64(dn2cpp_isa_bits<float64x1_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundtopositiveinfinityscalar_v64f64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundToPositiveInfinityScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundtozero_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundToZero");
    return dn2cpp_isa_vec<16>(vcvtq_s64_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundtozero_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundToZero");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundtozeroscalar_v64f64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundToZeroScalar");
    return dn2cpp_isa_vec<8>(vcvt_s64_f64(dn2cpp_isa_bits<float64x1_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttoint64roundtozeroscalar_v64f64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToInt64RoundToZeroScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttosinglelower_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToSingleLower");
    return dn2cpp_isa_vec<8>(vcvt_f32_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttosinglelower_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToSingleLower");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttosingleroundtooddlower_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToSingleRoundToOddLower");
    return dn2cpp_isa_vec<8>(vcvtx_f32_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttosingleroundtooddlower_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToSingleRoundToOddLower");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttosingleroundtooddupper_v64f32_v128f64(const Dn2CppVector64& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToSingleRoundToOddUpper");
    return dn2cpp_isa_vec<16>(vcvtx_high_f32_f64(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttosingleroundtooddupper_v64f32_v128f64(const Dn2CppVector64&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToSingleRoundToOddUpper");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttosingleupper_v64f32_v128f64(const Dn2CppVector64& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToSingleUpper");
    return dn2cpp_isa_vec<16>(vcvt_high_f32_f64(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttosingleupper_v64f32_v128f64(const Dn2CppVector64&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToSingleUpper");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundawayfromzero_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundAwayFromZero");
    return dn2cpp_isa_vec<16>(vcvtaq_u64_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundawayfromzero_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundAwayFromZero");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundawayfromzeroscalar_v64f64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundAwayFromZeroScalar");
    return dn2cpp_isa_vec<8>(vcvta_u64_f64(dn2cpp_isa_bits<float64x1_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundawayfromzeroscalar_v64f64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundAwayFromZeroScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundtoeven_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundToEven");
    return dn2cpp_isa_vec<16>(vcvtnq_u64_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundtoeven_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundToEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundtoevenscalar_v64f64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundToEvenScalar");
    return dn2cpp_isa_vec<8>(vcvtn_u64_f64(dn2cpp_isa_bits<float64x1_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundtoevenscalar_v64f64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundToEvenScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundtonegativeinfinity_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundToNegativeInfinity");
    return dn2cpp_isa_vec<16>(vcvtmq_u64_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundtonegativeinfinity_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundToNegativeInfinity");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundtonegativeinfinityscalar_v64f64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundToNegativeInfinityScalar");
    return dn2cpp_isa_vec<8>(vcvtm_u64_f64(dn2cpp_isa_bits<float64x1_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundtonegativeinfinityscalar_v64f64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundToNegativeInfinityScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundtopositiveinfinity_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundToPositiveInfinity");
    return dn2cpp_isa_vec<16>(vcvtpq_u64_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundtopositiveinfinity_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundToPositiveInfinity");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundtopositiveinfinityscalar_v64f64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundToPositiveInfinityScalar");
    return dn2cpp_isa_vec<8>(vcvtp_u64_f64(dn2cpp_isa_bits<float64x1_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundtopositiveinfinityscalar_v64f64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundToPositiveInfinityScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundtozero_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundToZero");
    return dn2cpp_isa_vec<16>(vcvtq_u64_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundtozero_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundToZero");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundtozeroscalar_v64f64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundToZeroScalar");
    return dn2cpp_isa_vec<8>(vcvt_u64_f64(dn2cpp_isa_bits<float64x1_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_converttouint64roundtozeroscalar_v64f64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ConvertToUInt64RoundToZeroScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_divide_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Divide");
    return dn2cpp_isa_vec<16>(vdivq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_divide_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Divide");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_divide_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Divide");
    return dn2cpp_isa_vec<16>(vdivq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_divide_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Divide");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_divide_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Divide");
    return dn2cpp_isa_vec<8>(vdiv_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_divide_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Divide");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_duplicateselectedscalartovector128_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.DuplicateSelectedScalarToVector128");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_vec<16>(vdupq_laneq_f64(dn2cpp_isa_bits<float64x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_duplicateselectedscalartovector128_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.DuplicateSelectedScalarToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_duplicateselectedscalartovector128_v128i64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.DuplicateSelectedScalarToVector128");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_vec<16>(vdupq_laneq_s64(dn2cpp_isa_bits<int64x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_duplicateselectedscalartovector128_v128i64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.DuplicateSelectedScalarToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_duplicateselectedscalartovector128_v128u64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.DuplicateSelectedScalarToVector128");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_vec<16>(vdupq_laneq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_duplicateselectedscalartovector128_v128u64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.DuplicateSelectedScalarToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_duplicatetovector128_f64(double a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.DuplicateToVector128");
    return dn2cpp_isa_vec<16>(vdupq_n_f64(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_duplicatetovector128_f64(double)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.DuplicateToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_duplicatetovector128_i64(int64_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.DuplicateToVector128");
    return dn2cpp_isa_vec<16>(vdupq_n_s64(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_duplicatetovector128_i64(int64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.DuplicateToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_duplicatetovector128_u64(uint64_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.DuplicateToVector128");
    return dn2cpp_isa_vec<16>(vdupq_n_u64(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_duplicatetovector128_u64(uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.DuplicateToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_extractnarrowingsaturatescalar_v64i16(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ExtractNarrowingSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqmovnh_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_extractnarrowingsaturatescalar_v64i16(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ExtractNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_extractnarrowingsaturatescalar_v64i32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ExtractNarrowingSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqmovns_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_extractnarrowingsaturatescalar_v64i32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ExtractNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_extractnarrowingsaturatescalar_v64i64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ExtractNarrowingSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqmovnd_s64(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_extractnarrowingsaturatescalar_v64i64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ExtractNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_extractnarrowingsaturatescalar_v64u16(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ExtractNarrowingSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqmovnh_u16(vget_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_extractnarrowingsaturatescalar_v64u16(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ExtractNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_extractnarrowingsaturatescalar_v64u32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ExtractNarrowingSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqmovns_u32(vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_extractnarrowingsaturatescalar_v64u32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ExtractNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_extractnarrowingsaturatescalar_v64u64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ExtractNarrowingSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqmovnd_u64(vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_extractnarrowingsaturatescalar_v64u64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ExtractNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_extractnarrowingsaturateunsignedscalar_v64i16(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ExtractNarrowingSaturateUnsignedScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqmovunh_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_extractnarrowingsaturateunsignedscalar_v64i16(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ExtractNarrowingSaturateUnsignedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_extractnarrowingsaturateunsignedscalar_v64i32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ExtractNarrowingSaturateUnsignedScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqmovuns_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_extractnarrowingsaturateunsignedscalar_v64i32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ExtractNarrowingSaturateUnsignedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_extractnarrowingsaturateunsignedscalar_v64i64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ExtractNarrowingSaturateUnsignedScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqmovund_s64(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_extractnarrowingsaturateunsignedscalar_v64i64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ExtractNarrowingSaturateUnsignedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_floor_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Floor");
    return dn2cpp_isa_vec<16>(vrndmq_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_floor_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Floor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyadd_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAdd");
    return dn2cpp_isa_vec<16>(vfmaq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1), dn2cpp_isa_bits<float64x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyadd_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddbyscalar_v128f32_v128f32_v64f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddByScalar");
    return dn2cpp_isa_vec<16>(vfmaq_lane_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1), dn2cpp_isa_bits<float32x2_t>(a2), 0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddbyscalar_v128f32_v128f32_v64f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddByScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddbyscalar_v128f64_v128f64_v64f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddByScalar");
    return dn2cpp_isa_vec<16>(vfmaq_lane_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1), dn2cpp_isa_bits<float64x1_t>(a2), 0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddbyscalar_v128f64_v128f64_v64f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddByScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddbyscalar_v64f32_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddByScalar");
    return dn2cpp_isa_vec<8>(vfma_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1), dn2cpp_isa_bits<float32x2_t>(a2), 0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddbyscalar_v64f32_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddByScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddbyselectedscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<16>(vfmaq_laneq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1), dn2cpp_isa_bits<float32x4_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddbyselectedscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddbyselectedscalar_v128f32_v128f32_v64f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<16>(vfmaq_lane_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1), dn2cpp_isa_bits<float32x2_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddbyselectedscalar_v128f32_v128f32_v64f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddbyselectedscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<16>(vfmaq_laneq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1), dn2cpp_isa_bits<float64x2_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddbyselectedscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddbyselectedscalar_v64f32_v64f32_v128f32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<8>(vfma_laneq_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1), dn2cpp_isa_bits<float32x4_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddbyselectedscalar_v64f32_v64f32_v128f32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddbyselectedscalar_v64f32_v64f32_v64f32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<8>(vfma_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1), dn2cpp_isa_bits<float32x2_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddbyselectedscalar_v64f32_v64f32_v64f32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddscalarbyselectedscalar_v64f32_v64f32_v128f32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddScalarBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vfmas_laneq_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0), dn2cpp_isa_bits<float32x4_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddscalarbyselectedscalar_v64f32_v64f32_v128f32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddScalarBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddscalarbyselectedscalar_v64f32_v64f32_v64f32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddScalarBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vfmas_lane_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0), dn2cpp_isa_bits<float32x2_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddscalarbyselectedscalar_v64f32_v64f32_v64f32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddScalarBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddscalarbyselectedscalar_v64f64_v64f64_v128f64_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddScalarBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vfmad_laneq_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0), vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a1), 0), dn2cpp_isa_bits<float64x2_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplyaddscalarbyselectedscalar_v64f64_v64f64_v128f64_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplyAddScalarBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtract_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtract");
    return dn2cpp_isa_vec<16>(vfmsq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1), dn2cpp_isa_bits<float64x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtract_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractbyscalar_v128f32_v128f32_v64f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractByScalar");
    return dn2cpp_isa_vec<16>(vfmsq_lane_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1), dn2cpp_isa_bits<float32x2_t>(a2), 0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractbyscalar_v128f32_v128f32_v64f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractByScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractbyscalar_v128f64_v128f64_v64f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractByScalar");
    return dn2cpp_isa_vec<16>(vfmsq_lane_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1), dn2cpp_isa_bits<float64x1_t>(a2), 0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractbyscalar_v128f64_v128f64_v64f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractByScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractbyscalar_v64f32_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractByScalar");
    return dn2cpp_isa_vec<8>(vfms_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1), dn2cpp_isa_bits<float32x2_t>(a2), 0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractbyscalar_v64f32_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractByScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractbyselectedscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<16>(vfmsq_laneq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1), dn2cpp_isa_bits<float32x4_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractbyselectedscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractbyselectedscalar_v128f32_v128f32_v64f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<16>(vfmsq_lane_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1), dn2cpp_isa_bits<float32x2_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractbyselectedscalar_v128f32_v128f32_v64f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractbyselectedscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<16>(vfmsq_laneq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1), dn2cpp_isa_bits<float64x2_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractbyselectedscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractbyselectedscalar_v64f32_v64f32_v128f32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<8>(vfms_laneq_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1), dn2cpp_isa_bits<float32x4_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractbyselectedscalar_v64f32_v64f32_v128f32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractbyselectedscalar_v64f32_v64f32_v64f32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<8>(vfms_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1), dn2cpp_isa_bits<float32x2_t>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractbyselectedscalar_v64f32_v64f32_v64f32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractscalarbyselectedscalar_v64f32_v64f32_v128f32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractScalarBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vfmss_laneq_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0), dn2cpp_isa_bits<float32x4_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractscalarbyselectedscalar_v64f32_v64f32_v128f32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractScalarBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractscalarbyselectedscalar_v64f32_v64f32_v64f32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractScalarBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vfmss_lane_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0), dn2cpp_isa_bits<float32x2_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractscalarbyselectedscalar_v64f32_v64f32_v64f32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractScalarBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractscalarbyselectedscalar_v64f64_v64f64_v128f64_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractScalarBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vfmsd_laneq_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0), vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a1), 0), dn2cpp_isa_bits<float64x2_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_fusedmultiplysubtractscalarbyselectedscalar_v64f64_v64f64_v128f64_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.FusedMultiplySubtractScalarBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128f32_u8_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 4, a1, 0, 4, a3, dn2cpp_isa_vec<16>(vcopyq_laneq_f32(dn2cpp_isa_bits<float32x4_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<float32x4_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128f32_u8_v128f32_u8(const Dn2CppVector128&, uint8_t, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128f32_u8_v64f32_u8(const Dn2CppVector128& a0, uint8_t a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 4, a1, 0, 2, a3, dn2cpp_isa_vec<16>(vcopyq_lane_f32(dn2cpp_isa_bits<float32x4_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<float32x2_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128f32_u8_v64f32_u8(const Dn2CppVector128&, uint8_t, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128f64_u8_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 2, a1, 0, 2, a3, dn2cpp_isa_vec<16>(vcopyq_laneq_f64(dn2cpp_isa_bits<float64x2_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<float64x2_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128f64_u8_v128f64_u8(const Dn2CppVector128&, uint8_t, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128i16_u8_v128i16_u8(const Dn2CppVector128& a0, uint8_t a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 8, a1, 0, 8, a3, dn2cpp_isa_vec<16>(vcopyq_laneq_s16(dn2cpp_isa_bits<int16x8_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<int16x8_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128i16_u8_v128i16_u8(const Dn2CppVector128&, uint8_t, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128i16_u8_v64i16_u8(const Dn2CppVector128& a0, uint8_t a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 8, a1, 0, 4, a3, dn2cpp_isa_vec<16>(vcopyq_lane_s16(dn2cpp_isa_bits<int16x8_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<int16x4_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128i16_u8_v64i16_u8(const Dn2CppVector128&, uint8_t, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128i32_u8_v128i32_u8(const Dn2CppVector128& a0, uint8_t a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 4, a1, 0, 4, a3, dn2cpp_isa_vec<16>(vcopyq_laneq_s32(dn2cpp_isa_bits<int32x4_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<int32x4_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128i32_u8_v128i32_u8(const Dn2CppVector128&, uint8_t, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128i32_u8_v64i32_u8(const Dn2CppVector128& a0, uint8_t a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 4, a1, 0, 2, a3, dn2cpp_isa_vec<16>(vcopyq_lane_s32(dn2cpp_isa_bits<int32x4_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<int32x2_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128i32_u8_v64i32_u8(const Dn2CppVector128&, uint8_t, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128i64_u8_v128i64_u8(const Dn2CppVector128& a0, uint8_t a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 2, a1, 0, 2, a3, dn2cpp_isa_vec<16>(vcopyq_laneq_s64(dn2cpp_isa_bits<int64x2_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<int64x2_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128i64_u8_v128i64_u8(const Dn2CppVector128&, uint8_t, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128i8_u8_v128i8_u8(const Dn2CppVector128& a0, uint8_t a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 16, a1, 0, 16, a3, dn2cpp_isa_vec<16>(vcopyq_laneq_s8(dn2cpp_isa_bits<int8x16_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<int8x16_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128i8_u8_v128i8_u8(const Dn2CppVector128&, uint8_t, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128i8_u8_v64i8_u8(const Dn2CppVector128& a0, uint8_t a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 16, a1, 0, 8, a3, dn2cpp_isa_vec<16>(vcopyq_lane_s8(dn2cpp_isa_bits<int8x16_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<int8x8_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128i8_u8_v64i8_u8(const Dn2CppVector128&, uint8_t, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128u16_u8_v128u16_u8(const Dn2CppVector128& a0, uint8_t a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 8, a1, 0, 8, a3, dn2cpp_isa_vec<16>(vcopyq_laneq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<uint16x8_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128u16_u8_v128u16_u8(const Dn2CppVector128&, uint8_t, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128u16_u8_v64u16_u8(const Dn2CppVector128& a0, uint8_t a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 8, a1, 0, 4, a3, dn2cpp_isa_vec<16>(vcopyq_lane_u16(dn2cpp_isa_bits<uint16x8_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<uint16x4_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128u16_u8_v64u16_u8(const Dn2CppVector128&, uint8_t, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128u32_u8_v128u32_u8(const Dn2CppVector128& a0, uint8_t a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 4, a1, 0, 4, a3, dn2cpp_isa_vec<16>(vcopyq_laneq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<uint32x4_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128u32_u8_v128u32_u8(const Dn2CppVector128&, uint8_t, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128u32_u8_v64u32_u8(const Dn2CppVector128& a0, uint8_t a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 4, a1, 0, 2, a3, dn2cpp_isa_vec<16>(vcopyq_lane_u32(dn2cpp_isa_bits<uint32x4_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<uint32x2_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128u32_u8_v64u32_u8(const Dn2CppVector128&, uint8_t, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128u64_u8_v128u64_u8(const Dn2CppVector128& a0, uint8_t a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 2, a1, 0, 2, a3, dn2cpp_isa_vec<16>(vcopyq_laneq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<uint64x2_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128u64_u8_v128u64_u8(const Dn2CppVector128&, uint8_t, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128u8_u8_v128u8_u8(const Dn2CppVector128& a0, uint8_t a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 16, a1, 0, 16, a3, dn2cpp_isa_vec<16>(vcopyq_laneq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<uint8x16_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128u8_u8_v128u8_u8(const Dn2CppVector128&, uint8_t, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128u8_u8_v64u8_u8(const Dn2CppVector128& a0, uint8_t a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 16, a1, 0, 8, a3, dn2cpp_isa_vec<16>(vcopyq_lane_u8(dn2cpp_isa_bits<uint8x16_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<uint8x8_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v128u8_u8_v64u8_u8(const Dn2CppVector128&, uint8_t, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64f32_u8_v128f32_u8(const Dn2CppVector64& a0, uint8_t a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 2, a1, 0, 4, a3, dn2cpp_isa_vec<8>(vcopy_laneq_f32(dn2cpp_isa_bits<float32x2_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<float32x4_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64f32_u8_v128f32_u8(const Dn2CppVector64&, uint8_t, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64f32_u8_v64f32_u8(const Dn2CppVector64& a0, uint8_t a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 2, a1, 0, 2, a3, dn2cpp_isa_vec<8>(vcopy_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<float32x2_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64f32_u8_v64f32_u8(const Dn2CppVector64&, uint8_t, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64i16_u8_v128i16_u8(const Dn2CppVector64& a0, uint8_t a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 4, a1, 0, 8, a3, dn2cpp_isa_vec<8>(vcopy_laneq_s16(dn2cpp_isa_bits<int16x4_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<int16x8_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64i16_u8_v128i16_u8(const Dn2CppVector64&, uint8_t, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64i16_u8_v64i16_u8(const Dn2CppVector64& a0, uint8_t a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 4, a1, 0, 4, a3, dn2cpp_isa_vec<8>(vcopy_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<int16x4_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64i16_u8_v64i16_u8(const Dn2CppVector64&, uint8_t, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64i32_u8_v128i32_u8(const Dn2CppVector64& a0, uint8_t a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 2, a1, 0, 4, a3, dn2cpp_isa_vec<8>(vcopy_laneq_s32(dn2cpp_isa_bits<int32x2_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<int32x4_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64i32_u8_v128i32_u8(const Dn2CppVector64&, uint8_t, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64i32_u8_v64i32_u8(const Dn2CppVector64& a0, uint8_t a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 2, a1, 0, 2, a3, dn2cpp_isa_vec<8>(vcopy_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<int32x2_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64i32_u8_v64i32_u8(const Dn2CppVector64&, uint8_t, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64i8_u8_v128i8_u8(const Dn2CppVector64& a0, uint8_t a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 8, a1, 0, 16, a3, dn2cpp_isa_vec<8>(vcopy_laneq_s8(dn2cpp_isa_bits<int8x8_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<int8x16_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64i8_u8_v128i8_u8(const Dn2CppVector64&, uint8_t, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64i8_u8_v64i8_u8(const Dn2CppVector64& a0, uint8_t a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 8, a1, 0, 8, a3, dn2cpp_isa_vec<8>(vcopy_lane_s8(dn2cpp_isa_bits<int8x8_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<int8x8_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64i8_u8_v64i8_u8(const Dn2CppVector64&, uint8_t, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64u16_u8_v128u16_u8(const Dn2CppVector64& a0, uint8_t a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 4, a1, 0, 8, a3, dn2cpp_isa_vec<8>(vcopy_laneq_u16(dn2cpp_isa_bits<uint16x4_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<uint16x8_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64u16_u8_v128u16_u8(const Dn2CppVector64&, uint8_t, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64u16_u8_v64u16_u8(const Dn2CppVector64& a0, uint8_t a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 4, a1, 0, 4, a3, dn2cpp_isa_vec<8>(vcopy_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<uint16x4_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64u16_u8_v64u16_u8(const Dn2CppVector64&, uint8_t, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64u32_u8_v128u32_u8(const Dn2CppVector64& a0, uint8_t a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 2, a1, 0, 4, a3, dn2cpp_isa_vec<8>(vcopy_laneq_u32(dn2cpp_isa_bits<uint32x2_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<uint32x4_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64u32_u8_v128u32_u8(const Dn2CppVector64&, uint8_t, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64u32_u8_v64u32_u8(const Dn2CppVector64& a0, uint8_t a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 2, a1, 0, 2, a3, dn2cpp_isa_vec<8>(vcopy_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<uint32x2_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64u32_u8_v64u32_u8(const Dn2CppVector64&, uint8_t, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64u8_u8_v128u8_u8(const Dn2CppVector64& a0, uint8_t a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 8, a1, 0, 16, a3, dn2cpp_isa_vec<8>(vcopy_laneq_u8(dn2cpp_isa_bits<uint8x8_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<uint8x16_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64u8_u8_v128u8_u8(const Dn2CppVector64&, uint8_t, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64u8_u8_v64u8_u8(const Dn2CppVector64& a0, uint8_t a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH2(0, 8, a1, 0, 8, a3, dn2cpp_isa_vec<8>(vcopy_lane_u8(dn2cpp_isa_bits<uint8x8_t>(a0), DN2CPP_IMM, dn2cpp_isa_bits<uint8x8_t>(a2), DN2CPP_IMM2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_insertselectedscalar_v64u8_u8_v64u8_u8(const Dn2CppVector64&, uint8_t, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.InsertSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pf32(float* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
    dn2cpp_isa_scatter(vld1q_f32_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pf32(float*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pf64(double* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
    dn2cpp_isa_scatter(vld1q_f64_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pf64(double*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pi16(int16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
    dn2cpp_isa_scatter(vld1q_s16_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pi16(int16_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pi32(int32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
    dn2cpp_isa_scatter(vld1q_s32_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pi32(int32_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pi64(int64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
    dn2cpp_isa_scatter(vld1q_s64_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pi64(int64_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pi8(int8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
    dn2cpp_isa_scatter(vld1q_s8_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pi8(int8_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pu16(uint16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
    dn2cpp_isa_scatter(vld1q_u16_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pu16(uint16_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pu32(uint32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
    dn2cpp_isa_scatter(vld1q_u32_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pu32(uint32_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pu64(uint64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
    dn2cpp_isa_scatter(vld1q_u64_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pu64(uint64_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pu8(uint8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
    dn2cpp_isa_scatter(vld1q_u8_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128_pu8(uint8_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pf32(float* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
    dn2cpp_isa_scatter(vld2q_f32(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pf32(float*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pf64(double* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
    dn2cpp_isa_scatter(vld2q_f64(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pf64(double*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pi16(int16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
    dn2cpp_isa_scatter(vld2q_s16(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pi16(int16_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pi32(int32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
    dn2cpp_isa_scatter(vld2q_s32(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pi32(int32_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pi64(int64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
    dn2cpp_isa_scatter(vld2q_s64(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pi64(int64_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pi8(int8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
    dn2cpp_isa_scatter(vld2q_s8(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pi8(int8_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pu16(uint16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
    dn2cpp_isa_scatter(vld2q_u16(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pu16(uint16_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pu32(uint32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
    dn2cpp_isa_scatter(vld2q_u32(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pu32(uint32_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pu64(uint64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
    dn2cpp_isa_scatter(vld2q_u64(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pu64(uint64_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pu8(uint8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
    dn2cpp_isa_scatter(vld2q_u8(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load2xvector128andunzip_pu8(uint8_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load2xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pf32(float* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
    dn2cpp_isa_scatter(vld1q_f32_x3(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pf32(float*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pf64(double* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
    dn2cpp_isa_scatter(vld1q_f64_x3(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pf64(double*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pi16(int16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
    dn2cpp_isa_scatter(vld1q_s16_x3(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pi16(int16_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pi32(int32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
    dn2cpp_isa_scatter(vld1q_s32_x3(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pi32(int32_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pi64(int64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
    dn2cpp_isa_scatter(vld1q_s64_x3(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pi64(int64_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pi8(int8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
    dn2cpp_isa_scatter(vld1q_s8_x3(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pi8(int8_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pu16(uint16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
    dn2cpp_isa_scatter(vld1q_u16_x3(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pu16(uint16_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pu32(uint32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
    dn2cpp_isa_scatter(vld1q_u32_x3(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pu32(uint32_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pu64(uint64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
    dn2cpp_isa_scatter(vld1q_u64_x3(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pu64(uint64_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pu8(uint8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
    dn2cpp_isa_scatter(vld1q_u8_x3(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128_pu8(uint8_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pf32(float* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
    dn2cpp_isa_scatter(vld3q_f32(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pf32(float*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pf64(double* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
    dn2cpp_isa_scatter(vld3q_f64(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pf64(double*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pi16(int16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
    dn2cpp_isa_scatter(vld3q_s16(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pi16(int16_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pi32(int32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
    dn2cpp_isa_scatter(vld3q_s32(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pi32(int32_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pi64(int64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
    dn2cpp_isa_scatter(vld3q_s64(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pi64(int64_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pi8(int8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
    dn2cpp_isa_scatter(vld3q_s8(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pi8(int8_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pu16(uint16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
    dn2cpp_isa_scatter(vld3q_u16(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pu16(uint16_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pu32(uint32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
    dn2cpp_isa_scatter(vld3q_u32(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pu32(uint32_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pu64(uint64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
    dn2cpp_isa_scatter(vld3q_u64(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pu64(uint64_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pu8(uint8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
    dn2cpp_isa_scatter(vld3q_u8(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load3xvector128andunzip_pu8(uint8_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load3xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pf32(float* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
    dn2cpp_isa_scatter(vld1q_f32_x4(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pf32(float*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pf64(double* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
    dn2cpp_isa_scatter(vld1q_f64_x4(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pf64(double*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pi16(int16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
    dn2cpp_isa_scatter(vld1q_s16_x4(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pi16(int16_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pi32(int32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
    dn2cpp_isa_scatter(vld1q_s32_x4(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pi32(int32_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pi64(int64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
    dn2cpp_isa_scatter(vld1q_s64_x4(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pi64(int64_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pi8(int8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
    dn2cpp_isa_scatter(vld1q_s8_x4(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pi8(int8_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pu16(uint16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
    dn2cpp_isa_scatter(vld1q_u16_x4(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pu16(uint16_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pu32(uint32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
    dn2cpp_isa_scatter(vld1q_u32_x4(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pu32(uint32_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pu64(uint64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
    dn2cpp_isa_scatter(vld1q_u64_x4(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pu64(uint64_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pu8(uint8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
    dn2cpp_isa_scatter(vld1q_u8_x4(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128_pu8(uint8_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pf32(float* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
    dn2cpp_isa_scatter(vld4q_f32(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pf32(float*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pf64(double* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
    dn2cpp_isa_scatter(vld4q_f64(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pf64(double*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pi16(int16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
    dn2cpp_isa_scatter(vld4q_s16(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pi16(int16_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pi32(int32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
    dn2cpp_isa_scatter(vld4q_s32(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pi32(int32_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pi64(int64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
    dn2cpp_isa_scatter(vld4q_s64(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pi64(int64_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pi8(int8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
    dn2cpp_isa_scatter(vld4q_s8(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pi8(int8_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pu16(uint16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
    dn2cpp_isa_scatter(vld4q_u16(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pu16(uint16_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pu32(uint32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
    dn2cpp_isa_scatter(vld4q_u32(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pu32(uint32_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pu64(uint64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
    dn2cpp_isa_scatter(vld4q_u64(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pu64(uint64_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pu8(uint8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
    dn2cpp_isa_scatter(vld4q_u8(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_load4xvector128andunzip_pu8(uint8_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Load4xVector128AndUnzip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128f32_u8_pf32(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, uint8_t a1, float* a2, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_scatter(vld2q_lane_f32(a2, (float32x4x2_t{{dn2cpp_isa_bits<float32x4_t>(a0_1), dn2cpp_isa_bits<float32x4_t>(a0_2)}}), DN2CPP_IMM), item1, item2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128f32_u8_pf32(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, float*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128f64_u8_pf64(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, uint8_t a1, double* a2, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_scatter(vld2q_lane_f64(a2, (float64x2x2_t{{dn2cpp_isa_bits<float64x2_t>(a0_1), dn2cpp_isa_bits<float64x2_t>(a0_2)}}), DN2CPP_IMM), item1, item2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128f64_u8_pf64(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, double*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128i16_u8_pi16(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, uint8_t a1, int16_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_scatter(vld2q_lane_s16(a2, (int16x8x2_t{{dn2cpp_isa_bits<int16x8_t>(a0_1), dn2cpp_isa_bits<int16x8_t>(a0_2)}}), DN2CPP_IMM), item1, item2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128i16_u8_pi16(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, int16_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128i32_u8_pi32(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, uint8_t a1, int32_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_scatter(vld2q_lane_s32(a2, (int32x4x2_t{{dn2cpp_isa_bits<int32x4_t>(a0_1), dn2cpp_isa_bits<int32x4_t>(a0_2)}}), DN2CPP_IMM), item1, item2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128i32_u8_pi32(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, int32_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128i64_u8_pi64(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, uint8_t a1, int64_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_scatter(vld2q_lane_s64(a2, (int64x2x2_t{{dn2cpp_isa_bits<int64x2_t>(a0_1), dn2cpp_isa_bits<int64x2_t>(a0_2)}}), DN2CPP_IMM), item1, item2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128i64_u8_pi64(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, int64_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128i8_u8_pi8(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, uint8_t a1, int8_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_scatter(vld2q_lane_s8(a2, (int8x16x2_t{{dn2cpp_isa_bits<int8x16_t>(a0_1), dn2cpp_isa_bits<int8x16_t>(a0_2)}}), DN2CPP_IMM), item1, item2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128i8_u8_pi8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, int8_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128u16_u8_pu16(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, uint8_t a1, uint16_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_scatter(vld2q_lane_u16(a2, (uint16x8x2_t{{dn2cpp_isa_bits<uint16x8_t>(a0_1), dn2cpp_isa_bits<uint16x8_t>(a0_2)}}), DN2CPP_IMM), item1, item2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128u16_u8_pu16(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, uint16_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128u32_u8_pu32(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, uint8_t a1, uint32_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_scatter(vld2q_lane_u32(a2, (uint32x4x2_t{{dn2cpp_isa_bits<uint32x4_t>(a0_1), dn2cpp_isa_bits<uint32x4_t>(a0_2)}}), DN2CPP_IMM), item1, item2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128u32_u8_pu32(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, uint32_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128u64_u8_pu64(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, uint8_t a1, uint64_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_scatter(vld2q_lane_u64(a2, (uint64x2x2_t{{dn2cpp_isa_bits<uint64x2_t>(a0_1), dn2cpp_isa_bits<uint64x2_t>(a0_2)}}), DN2CPP_IMM), item1, item2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128u64_u8_pu64(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, uint64_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128u8_u8_pu8(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, uint8_t a1, uint8_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_scatter(vld2q_lane_u8(a2, (uint8x16x2_t{{dn2cpp_isa_bits<uint8x16_t>(a0_1), dn2cpp_isa_bits<uint8x16_t>(a0_2)}}), DN2CPP_IMM), item1, item2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t2v128u8_u8_pu8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, uint8_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128f32_u8_pf32(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, uint8_t a1, float* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_scatter(vld3q_lane_f32(a2, (float32x4x3_t{{dn2cpp_isa_bits<float32x4_t>(a0_1), dn2cpp_isa_bits<float32x4_t>(a0_2), dn2cpp_isa_bits<float32x4_t>(a0_3)}}), DN2CPP_IMM), item1, item2, item3));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128f32_u8_pf32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, float*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128f64_u8_pf64(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, uint8_t a1, double* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_scatter(vld3q_lane_f64(a2, (float64x2x3_t{{dn2cpp_isa_bits<float64x2_t>(a0_1), dn2cpp_isa_bits<float64x2_t>(a0_2), dn2cpp_isa_bits<float64x2_t>(a0_3)}}), DN2CPP_IMM), item1, item2, item3));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128f64_u8_pf64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, double*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128i16_u8_pi16(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, uint8_t a1, int16_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_scatter(vld3q_lane_s16(a2, (int16x8x3_t{{dn2cpp_isa_bits<int16x8_t>(a0_1), dn2cpp_isa_bits<int16x8_t>(a0_2), dn2cpp_isa_bits<int16x8_t>(a0_3)}}), DN2CPP_IMM), item1, item2, item3));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128i16_u8_pi16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, int16_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128i32_u8_pi32(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, uint8_t a1, int32_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_scatter(vld3q_lane_s32(a2, (int32x4x3_t{{dn2cpp_isa_bits<int32x4_t>(a0_1), dn2cpp_isa_bits<int32x4_t>(a0_2), dn2cpp_isa_bits<int32x4_t>(a0_3)}}), DN2CPP_IMM), item1, item2, item3));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128i32_u8_pi32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, int32_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128i64_u8_pi64(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, uint8_t a1, int64_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_scatter(vld3q_lane_s64(a2, (int64x2x3_t{{dn2cpp_isa_bits<int64x2_t>(a0_1), dn2cpp_isa_bits<int64x2_t>(a0_2), dn2cpp_isa_bits<int64x2_t>(a0_3)}}), DN2CPP_IMM), item1, item2, item3));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128i64_u8_pi64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, int64_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128i8_u8_pi8(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, uint8_t a1, int8_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_scatter(vld3q_lane_s8(a2, (int8x16x3_t{{dn2cpp_isa_bits<int8x16_t>(a0_1), dn2cpp_isa_bits<int8x16_t>(a0_2), dn2cpp_isa_bits<int8x16_t>(a0_3)}}), DN2CPP_IMM), item1, item2, item3));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128i8_u8_pi8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, int8_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128u16_u8_pu16(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, uint8_t a1, uint16_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_scatter(vld3q_lane_u16(a2, (uint16x8x3_t{{dn2cpp_isa_bits<uint16x8_t>(a0_1), dn2cpp_isa_bits<uint16x8_t>(a0_2), dn2cpp_isa_bits<uint16x8_t>(a0_3)}}), DN2CPP_IMM), item1, item2, item3));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128u16_u8_pu16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, uint16_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128u32_u8_pu32(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, uint8_t a1, uint32_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_scatter(vld3q_lane_u32(a2, (uint32x4x3_t{{dn2cpp_isa_bits<uint32x4_t>(a0_1), dn2cpp_isa_bits<uint32x4_t>(a0_2), dn2cpp_isa_bits<uint32x4_t>(a0_3)}}), DN2CPP_IMM), item1, item2, item3));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128u32_u8_pu32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, uint32_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128u64_u8_pu64(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, uint8_t a1, uint64_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_scatter(vld3q_lane_u64(a2, (uint64x2x3_t{{dn2cpp_isa_bits<uint64x2_t>(a0_1), dn2cpp_isa_bits<uint64x2_t>(a0_2), dn2cpp_isa_bits<uint64x2_t>(a0_3)}}), DN2CPP_IMM), item1, item2, item3));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128u64_u8_pu64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, uint64_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128u8_u8_pu8(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, uint8_t a1, uint8_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_scatter(vld3q_lane_u8(a2, (uint8x16x3_t{{dn2cpp_isa_bits<uint8x16_t>(a0_1), dn2cpp_isa_bits<uint8x16_t>(a0_2), dn2cpp_isa_bits<uint8x16_t>(a0_3)}}), DN2CPP_IMM), item1, item2, item3));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t3v128u8_u8_pu8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, uint8_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128f32_u8_pf32(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, const Dn2CppVector128& a0_4, uint8_t a1, float* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_scatter(vld4q_lane_f32(a2, (float32x4x4_t{{dn2cpp_isa_bits<float32x4_t>(a0_1), dn2cpp_isa_bits<float32x4_t>(a0_2), dn2cpp_isa_bits<float32x4_t>(a0_3), dn2cpp_isa_bits<float32x4_t>(a0_4)}}), DN2CPP_IMM), item1, item2, item3, item4));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128f32_u8_pf32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, float*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128f64_u8_pf64(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, const Dn2CppVector128& a0_4, uint8_t a1, double* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_scatter(vld4q_lane_f64(a2, (float64x2x4_t{{dn2cpp_isa_bits<float64x2_t>(a0_1), dn2cpp_isa_bits<float64x2_t>(a0_2), dn2cpp_isa_bits<float64x2_t>(a0_3), dn2cpp_isa_bits<float64x2_t>(a0_4)}}), DN2CPP_IMM), item1, item2, item3, item4));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128f64_u8_pf64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, double*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128i16_u8_pi16(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, const Dn2CppVector128& a0_4, uint8_t a1, int16_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_scatter(vld4q_lane_s16(a2, (int16x8x4_t{{dn2cpp_isa_bits<int16x8_t>(a0_1), dn2cpp_isa_bits<int16x8_t>(a0_2), dn2cpp_isa_bits<int16x8_t>(a0_3), dn2cpp_isa_bits<int16x8_t>(a0_4)}}), DN2CPP_IMM), item1, item2, item3, item4));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128i16_u8_pi16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, int16_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128i32_u8_pi32(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, const Dn2CppVector128& a0_4, uint8_t a1, int32_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_scatter(vld4q_lane_s32(a2, (int32x4x4_t{{dn2cpp_isa_bits<int32x4_t>(a0_1), dn2cpp_isa_bits<int32x4_t>(a0_2), dn2cpp_isa_bits<int32x4_t>(a0_3), dn2cpp_isa_bits<int32x4_t>(a0_4)}}), DN2CPP_IMM), item1, item2, item3, item4));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128i32_u8_pi32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, int32_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128i64_u8_pi64(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, const Dn2CppVector128& a0_4, uint8_t a1, int64_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_scatter(vld4q_lane_s64(a2, (int64x2x4_t{{dn2cpp_isa_bits<int64x2_t>(a0_1), dn2cpp_isa_bits<int64x2_t>(a0_2), dn2cpp_isa_bits<int64x2_t>(a0_3), dn2cpp_isa_bits<int64x2_t>(a0_4)}}), DN2CPP_IMM), item1, item2, item3, item4));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128i64_u8_pi64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, int64_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128i8_u8_pi8(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, const Dn2CppVector128& a0_4, uint8_t a1, int8_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_scatter(vld4q_lane_s8(a2, (int8x16x4_t{{dn2cpp_isa_bits<int8x16_t>(a0_1), dn2cpp_isa_bits<int8x16_t>(a0_2), dn2cpp_isa_bits<int8x16_t>(a0_3), dn2cpp_isa_bits<int8x16_t>(a0_4)}}), DN2CPP_IMM), item1, item2, item3, item4));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128i8_u8_pi8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, int8_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128u16_u8_pu16(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, const Dn2CppVector128& a0_4, uint8_t a1, uint16_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_scatter(vld4q_lane_u16(a2, (uint16x8x4_t{{dn2cpp_isa_bits<uint16x8_t>(a0_1), dn2cpp_isa_bits<uint16x8_t>(a0_2), dn2cpp_isa_bits<uint16x8_t>(a0_3), dn2cpp_isa_bits<uint16x8_t>(a0_4)}}), DN2CPP_IMM), item1, item2, item3, item4));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128u16_u8_pu16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, uint16_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128u32_u8_pu32(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, const Dn2CppVector128& a0_4, uint8_t a1, uint32_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_scatter(vld4q_lane_u32(a2, (uint32x4x4_t{{dn2cpp_isa_bits<uint32x4_t>(a0_1), dn2cpp_isa_bits<uint32x4_t>(a0_2), dn2cpp_isa_bits<uint32x4_t>(a0_3), dn2cpp_isa_bits<uint32x4_t>(a0_4)}}), DN2CPP_IMM), item1, item2, item3, item4));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128u32_u8_pu32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, uint32_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128u64_u8_pu64(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, const Dn2CppVector128& a0_4, uint8_t a1, uint64_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_scatter(vld4q_lane_u64(a2, (uint64x2x4_t{{dn2cpp_isa_bits<uint64x2_t>(a0_1), dn2cpp_isa_bits<uint64x2_t>(a0_2), dn2cpp_isa_bits<uint64x2_t>(a0_3), dn2cpp_isa_bits<uint64x2_t>(a0_4)}}), DN2CPP_IMM), item1, item2, item3, item4));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128u64_u8_pu64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, uint64_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128u8_u8_pu8(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, const Dn2CppVector128& a0_4, uint8_t a1, uint8_t* a2, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_scatter(vld4q_lane_u8(a2, (uint8x16x4_t{{dn2cpp_isa_bits<uint8x16_t>(a0_1), dn2cpp_isa_bits<uint8x16_t>(a0_2), dn2cpp_isa_bits<uint8x16_t>(a0_3), dn2cpp_isa_bits<uint8x16_t>(a0_4)}}), DN2CPP_IMM), item1, item2, item3, item4));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandinsertscalar_t4v128u8_u8_pu8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t, uint8_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndInsertScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128");
    return dn2cpp_isa_vec<16>(vld1q_dup_f64(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128");
    return dn2cpp_isa_vec<16>(vld1q_dup_s64(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128");
    return dn2cpp_isa_vec<16>(vld1q_dup_u64(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pf32(float* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
    dn2cpp_isa_scatter(vld2q_dup_f32(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pf32(float*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pf64(double* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
    dn2cpp_isa_scatter(vld2q_dup_f64(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pf64(double*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pi16(int16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
    dn2cpp_isa_scatter(vld2q_dup_s16(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pi16(int16_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pi32(int32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
    dn2cpp_isa_scatter(vld2q_dup_s32(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pi32(int32_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pi64(int64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
    dn2cpp_isa_scatter(vld2q_dup_s64(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pi64(int64_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pi8(int8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
    dn2cpp_isa_scatter(vld2q_dup_s8(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pi8(int8_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pu16(uint16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
    dn2cpp_isa_scatter(vld2q_dup_u16(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pu16(uint16_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pu32(uint32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
    dn2cpp_isa_scatter(vld2q_dup_u32(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pu32(uint32_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pu64(uint64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
    dn2cpp_isa_scatter(vld2q_dup_u64(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pu64(uint64_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pu8(uint8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
    dn2cpp_isa_scatter(vld2q_dup_u8(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x2_pu8(uint8_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x2");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pf32(float* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
    dn2cpp_isa_scatter(vld3q_dup_f32(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pf32(float*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pf64(double* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
    dn2cpp_isa_scatter(vld3q_dup_f64(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pf64(double*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pi16(int16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
    dn2cpp_isa_scatter(vld3q_dup_s16(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pi16(int16_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pi32(int32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
    dn2cpp_isa_scatter(vld3q_dup_s32(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pi32(int32_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pi64(int64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
    dn2cpp_isa_scatter(vld3q_dup_s64(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pi64(int64_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pi8(int8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
    dn2cpp_isa_scatter(vld3q_dup_s8(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pi8(int8_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pu16(uint16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
    dn2cpp_isa_scatter(vld3q_dup_u16(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pu16(uint16_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pu32(uint32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
    dn2cpp_isa_scatter(vld3q_dup_u32(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pu32(uint32_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pu64(uint64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
    dn2cpp_isa_scatter(vld3q_dup_u64(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pu64(uint64_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pu8(uint8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
    dn2cpp_isa_scatter(vld3q_dup_u8(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x3_pu8(uint8_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x3");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pf32(float* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
    dn2cpp_isa_scatter(vld4q_dup_f32(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pf32(float*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pf64(double* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
    dn2cpp_isa_scatter(vld4q_dup_f64(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pf64(double*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pi16(int16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
    dn2cpp_isa_scatter(vld4q_dup_s16(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pi16(int16_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pi32(int32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
    dn2cpp_isa_scatter(vld4q_dup_s32(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pi32(int32_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pi64(int64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
    dn2cpp_isa_scatter(vld4q_dup_s64(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pi64(int64_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pi8(int8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
    dn2cpp_isa_scatter(vld4q_dup_s8(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pi8(int8_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pu16(uint16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
    dn2cpp_isa_scatter(vld4q_dup_u16(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pu16(uint16_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pu32(uint32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
    dn2cpp_isa_scatter(vld4q_dup_u32(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pu32(uint32_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pu64(uint64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
    dn2cpp_isa_scatter(vld4q_dup_u64(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pu64(uint64_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pu8(uint8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2, Dn2CppVector128* item3, Dn2CppVector128* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
    dn2cpp_isa_scatter(vld4q_dup_u8(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadandreplicatetovector128x4_pu8(uint8_t*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadAndReplicateToVector128x4");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairscalarvector64_pf32(float* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairScalarVector64");
    (void)((*item1) = dn2cpp_isa_lane0<8>(a0[0]), (*item2) = dn2cpp_isa_lane0<8>(a0[1]));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairscalarvector64_pf32(float*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairScalarVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairscalarvector64_pi32(int32_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairScalarVector64");
    (void)((*item1) = dn2cpp_isa_lane0<8>(a0[0]), (*item2) = dn2cpp_isa_lane0<8>(a0[1]));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairscalarvector64_pi32(int32_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairScalarVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairscalarvector64_pu32(uint32_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairScalarVector64");
    (void)((*item1) = dn2cpp_isa_lane0<8>(a0[0]), (*item2) = dn2cpp_isa_lane0<8>(a0[1]));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairscalarvector64_pu32(uint32_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairScalarVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairscalarvector64nontemporal_pf32(float* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairScalarVector64NonTemporal");
    (void)((*item1) = dn2cpp_isa_lane0<8>(a0[0]), (*item2) = dn2cpp_isa_lane0<8>(a0[1]));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairscalarvector64nontemporal_pf32(float*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairScalarVector64NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairscalarvector64nontemporal_pi32(int32_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairScalarVector64NonTemporal");
    (void)((*item1) = dn2cpp_isa_lane0<8>(a0[0]), (*item2) = dn2cpp_isa_lane0<8>(a0[1]));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairscalarvector64nontemporal_pi32(int32_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairScalarVector64NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairscalarvector64nontemporal_pu32(uint32_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairScalarVector64NonTemporal");
    (void)((*item1) = dn2cpp_isa_lane0<8>(a0[0]), (*item2) = dn2cpp_isa_lane0<8>(a0[1]));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairscalarvector64nontemporal_pu32(uint32_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairScalarVector64NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pf32(float* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
    dn2cpp_isa_scatter(vld1q_f32_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pf32(float*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pf64(double* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
    dn2cpp_isa_scatter(vld1q_f64_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pf64(double*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pi16(int16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
    dn2cpp_isa_scatter(vld1q_s16_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pi16(int16_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pi32(int32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
    dn2cpp_isa_scatter(vld1q_s32_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pi32(int32_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pi64(int64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
    dn2cpp_isa_scatter(vld1q_s64_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pi64(int64_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pi8(int8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
    dn2cpp_isa_scatter(vld1q_s8_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pi8(int8_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pu16(uint16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
    dn2cpp_isa_scatter(vld1q_u16_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pu16(uint16_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pu32(uint32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
    dn2cpp_isa_scatter(vld1q_u32_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pu32(uint32_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pu64(uint64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
    dn2cpp_isa_scatter(vld1q_u64_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pu64(uint64_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pu8(uint8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
    dn2cpp_isa_scatter(vld1q_u8_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128_pu8(uint8_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pf32(float* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
    dn2cpp_isa_scatter(vld1q_f32_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pf32(float*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pf64(double* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
    dn2cpp_isa_scatter(vld1q_f64_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pf64(double*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pi16(int16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
    dn2cpp_isa_scatter(vld1q_s16_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pi16(int16_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pi32(int32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
    dn2cpp_isa_scatter(vld1q_s32_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pi32(int32_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pi64(int64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
    dn2cpp_isa_scatter(vld1q_s64_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pi64(int64_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pi8(int8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
    dn2cpp_isa_scatter(vld1q_s8_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pi8(int8_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pu16(uint16_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
    dn2cpp_isa_scatter(vld1q_u16_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pu16(uint16_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pu32(uint32_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
    dn2cpp_isa_scatter(vld1q_u32_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pu32(uint32_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pu64(uint64_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
    dn2cpp_isa_scatter(vld1q_u64_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pu64(uint64_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pu8(uint8_t* a0, Dn2CppVector128* item1, Dn2CppVector128* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
    dn2cpp_isa_scatter(vld1q_u8_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector128nontemporal_pu8(uint8_t*, Dn2CppVector128*, Dn2CppVector128*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector128NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pf32(float* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
    dn2cpp_isa_scatter(vld1_f32_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pf32(float*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pf64(double* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
    dn2cpp_isa_scatter(vld1_f64_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pf64(double*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pi16(int16_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
    dn2cpp_isa_scatter(vld1_s16_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pi16(int16_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pi32(int32_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
    dn2cpp_isa_scatter(vld1_s32_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pi32(int32_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pi64(int64_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
    dn2cpp_isa_scatter(vld1_s64_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pi64(int64_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pi8(int8_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
    dn2cpp_isa_scatter(vld1_s8_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pi8(int8_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pu16(uint16_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
    dn2cpp_isa_scatter(vld1_u16_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pu16(uint16_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pu32(uint32_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
    dn2cpp_isa_scatter(vld1_u32_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pu32(uint32_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pu64(uint64_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
    dn2cpp_isa_scatter(vld1_u64_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pu64(uint64_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pu8(uint8_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
    dn2cpp_isa_scatter(vld1_u8_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64_pu8(uint8_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pf32(float* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
    dn2cpp_isa_scatter(vld1_f32_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pf32(float*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pf64(double* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
    dn2cpp_isa_scatter(vld1_f64_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pf64(double*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pi16(int16_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
    dn2cpp_isa_scatter(vld1_s16_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pi16(int16_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pi32(int32_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
    dn2cpp_isa_scatter(vld1_s32_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pi32(int32_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pi64(int64_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
    dn2cpp_isa_scatter(vld1_s64_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pi64(int64_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pi8(int8_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
    dn2cpp_isa_scatter(vld1_s8_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pi8(int8_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pu16(uint16_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
    dn2cpp_isa_scatter(vld1_u16_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pu16(uint16_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pu32(uint32_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
    dn2cpp_isa_scatter(vld1_u32_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pu32(uint32_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pu64(uint64_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
    dn2cpp_isa_scatter(vld1_u64_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pu64(uint64_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pu8(uint8_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
    dn2cpp_isa_scatter(vld1_u8_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_loadpairvector64nontemporal_pu8(uint8_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.LoadPairVector64NonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_max_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Max");
    return dn2cpp_isa_vec<16>(vmaxq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_max_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Max");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vmaxvq_f32(dn2cpp_isa_bits<float32x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vmaxvq_s16(dn2cpp_isa_bits<int16x8_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vmaxvq_s32(dn2cpp_isa_bits<int32x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vmaxvq_s8(dn2cpp_isa_bits<int8x16_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vmaxvq_u16(dn2cpp_isa_bits<uint16x8_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vmaxvq_u32(dn2cpp_isa_bits<uint32x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vmaxvq_u8(dn2cpp_isa_bits<uint8x16_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v64i16(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vmaxv_s16(dn2cpp_isa_bits<int16x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v64i16(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v64i8(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vmaxv_s8(dn2cpp_isa_bits<int8x8_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v64i8(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v64u16(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vmaxv_u16(dn2cpp_isa_bits<uint16x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v64u16(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v64u8(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vmaxv_u8(dn2cpp_isa_bits<uint8x8_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxacross_v64u8(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxnumber_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxNumber");
    return dn2cpp_isa_vec<16>(vmaxnmq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxnumber_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxNumber");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxnumberacross_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxNumberAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vmaxnmvq_f32(dn2cpp_isa_bits<float32x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxnumberacross_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxNumberAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxnumberpairwise_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxNumberPairwise");
    return dn2cpp_isa_vec<16>(vpmaxnmq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxnumberpairwise_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxNumberPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxnumberpairwise_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxNumberPairwise");
    return dn2cpp_isa_vec<16>(vpmaxnmq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxnumberpairwise_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxNumberPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxnumberpairwise_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxNumberPairwise");
    return dn2cpp_isa_vec<8>(vpmaxnm_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxnumberpairwise_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxNumberPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxnumberpairwisescalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxNumberPairwiseScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vpmaxnmqd_f64(dn2cpp_isa_bits<float64x2_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxnumberpairwisescalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxNumberPairwiseScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxnumberpairwisescalar_v64f32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxNumberPairwiseScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vpmaxnms_f32(dn2cpp_isa_bits<float32x2_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxnumberpairwisescalar_v64f32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxNumberPairwiseScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxpairwise_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwise");
    return dn2cpp_isa_vec<16>(vpmaxq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxpairwise_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxpairwise_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwise");
    return dn2cpp_isa_vec<16>(vpmaxq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxpairwise_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxpairwise_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwise");
    return dn2cpp_isa_vec<16>(vpmaxq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxpairwise_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxpairwise_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwise");
    return dn2cpp_isa_vec<16>(vpmaxq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxpairwise_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxpairwise_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwise");
    return dn2cpp_isa_vec<16>(vpmaxq_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxpairwise_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxpairwise_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwise");
    return dn2cpp_isa_vec<16>(vpmaxq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxpairwise_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxpairwise_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwise");
    return dn2cpp_isa_vec<16>(vpmaxq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxpairwise_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxpairwise_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwise");
    return dn2cpp_isa_vec<16>(vpmaxq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_maxpairwise_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxpairwisescalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwiseScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vpmaxqd_f64(dn2cpp_isa_bits<float64x2_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxpairwisescalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwiseScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxpairwisescalar_v64f32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwiseScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vpmaxs_f32(dn2cpp_isa_bits<float32x2_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxpairwisescalar_v64f32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxPairwiseScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxscalar_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vget_lane_f32(vmax_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)), 0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxscalar_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxscalar_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vget_lane_f64(vmax_f64(dn2cpp_isa_bits<float64x1_t>(a0), dn2cpp_isa_bits<float64x1_t>(a1)), 0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_maxscalar_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MaxScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_min_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Min");
    return dn2cpp_isa_vec<16>(vminq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_min_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Min");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vminvq_f32(dn2cpp_isa_bits<float32x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vminvq_s16(dn2cpp_isa_bits<int16x8_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vminvq_s32(dn2cpp_isa_bits<int32x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vminvq_s8(dn2cpp_isa_bits<int8x16_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vminvq_u16(dn2cpp_isa_bits<uint16x8_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vminvq_u32(dn2cpp_isa_bits<uint32x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vminvq_u8(dn2cpp_isa_bits<uint8x16_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v64i16(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vminv_s16(dn2cpp_isa_bits<int16x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v64i16(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v64i8(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vminv_s8(dn2cpp_isa_bits<int8x8_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v64i8(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v64u16(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vminv_u16(dn2cpp_isa_bits<uint16x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v64u16(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v64u8(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vminv_u8(dn2cpp_isa_bits<uint8x8_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minacross_v64u8(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minnumber_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinNumber");
    return dn2cpp_isa_vec<16>(vminnmq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minnumber_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinNumber");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minnumberacross_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinNumberAcross");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vminnmvq_f32(dn2cpp_isa_bits<float32x4_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minnumberacross_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinNumberAcross");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minnumberpairwise_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinNumberPairwise");
    return dn2cpp_isa_vec<16>(vpminnmq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minnumberpairwise_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinNumberPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minnumberpairwise_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinNumberPairwise");
    return dn2cpp_isa_vec<16>(vpminnmq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minnumberpairwise_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinNumberPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minnumberpairwise_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinNumberPairwise");
    return dn2cpp_isa_vec<8>(vpminnm_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minnumberpairwise_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinNumberPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minnumberpairwisescalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinNumberPairwiseScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vpminnmqd_f64(dn2cpp_isa_bits<float64x2_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minnumberpairwisescalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinNumberPairwiseScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minnumberpairwisescalar_v64f32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinNumberPairwiseScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vpminnms_f32(dn2cpp_isa_bits<float32x2_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minnumberpairwisescalar_v64f32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinNumberPairwiseScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minpairwise_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwise");
    return dn2cpp_isa_vec<16>(vpminq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minpairwise_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minpairwise_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwise");
    return dn2cpp_isa_vec<16>(vpminq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minpairwise_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minpairwise_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwise");
    return dn2cpp_isa_vec<16>(vpminq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minpairwise_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minpairwise_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwise");
    return dn2cpp_isa_vec<16>(vpminq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minpairwise_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minpairwise_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwise");
    return dn2cpp_isa_vec<16>(vpminq_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minpairwise_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minpairwise_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwise");
    return dn2cpp_isa_vec<16>(vpminq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minpairwise_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minpairwise_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwise");
    return dn2cpp_isa_vec<16>(vpminq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minpairwise_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minpairwise_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwise");
    return dn2cpp_isa_vec<16>(vpminq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_minpairwise_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwise");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minpairwisescalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwiseScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vpminqd_f64(dn2cpp_isa_bits<float64x2_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minpairwisescalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwiseScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minpairwisescalar_v64f32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwiseScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vpmins_f32(dn2cpp_isa_bits<float32x2_t>(a0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minpairwisescalar_v64f32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinPairwiseScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minscalar_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vget_lane_f32(vmin_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)), 0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minscalar_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minscalar_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vget_lane_f64(vmin_f64(dn2cpp_isa_bits<float64x1_t>(a0), dn2cpp_isa_bits<float64x1_t>(a1)), 0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_minscalar_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MinScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_multiply_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Multiply");
    return dn2cpp_isa_vec<16>(vmulq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_multiply_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Multiply");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_multiplybyscalar_v128f64_v64f64(const Dn2CppVector128& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyByScalar");
    return dn2cpp_isa_vec<16>(vmulq_lane_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x1_t>(a1), 0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_multiplybyscalar_v128f64_v64f64(const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyByScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_multiplybyselectedscalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, dn2cpp_isa_vec<16>(vmulq_laneq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_multiplybyselectedscalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingsaturatehighscalar_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingSaturateHighScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmulhh_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingsaturatehighscalar_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingSaturateHighScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingsaturatehighscalar_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingSaturateHighScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmulhs_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingsaturatehighscalar_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingSaturateHighScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingscalarbyselectedscalarsaturatehigh_v64i16_v128i16_u8(const Dn2CppVector64& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingScalarBySelectedScalarSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a2, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmulhh_laneq_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), dn2cpp_isa_bits<int16x8_t>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingscalarbyselectedscalarsaturatehigh_v64i16_v128i16_u8(const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingScalarBySelectedScalarSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingscalarbyselectedscalarsaturatehigh_v64i16_v64i16_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingScalarBySelectedScalarSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmulhh_lane_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), dn2cpp_isa_bits<int16x4_t>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingscalarbyselectedscalarsaturatehigh_v64i16_v64i16_u8(const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingScalarBySelectedScalarSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingscalarbyselectedscalarsaturatehigh_v64i32_v128i32_u8(const Dn2CppVector64& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingScalarBySelectedScalarSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmulhs_laneq_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), dn2cpp_isa_bits<int32x4_t>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingscalarbyselectedscalarsaturatehigh_v64i32_v128i32_u8(const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingScalarBySelectedScalarSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingscalarbyselectedscalarsaturatehigh_v64i32_v64i32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingScalarBySelectedScalarSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmulhs_lane_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), dn2cpp_isa_bits<int32x2_t>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingscalarbyselectedscalarsaturatehigh_v64i32_v64i32_u8(const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingScalarBySelectedScalarSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningandaddsaturatescalar_v64i32_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningAndAddSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmlalh_s16(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a2), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningandaddsaturatescalar_v64i32_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningAndAddSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningandaddsaturatescalar_v64i64_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningAndAddSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmlals_s32(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a2), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningandaddsaturatescalar_v64i64_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningAndAddSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningandsubtractsaturatescalar_v64i32_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningAndSubtractSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmlslh_s16(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a2), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningandsubtractsaturatescalar_v64i32_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningAndSubtractSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningandsubtractsaturatescalar_v64i64_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningAndSubtractSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmlsls_s32(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a2), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningandsubtractsaturatescalar_v64i64_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningAndSubtractSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningsaturatescalar_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmullh_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningsaturatescalar_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningsaturatescalar_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmulls_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningsaturatescalar_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningsaturatescalarbyselectedscalar_v64i16_v128i16_u8(const Dn2CppVector64& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningSaturateScalarBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a2, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmullh_laneq_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), dn2cpp_isa_bits<int16x8_t>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningsaturatescalarbyselectedscalar_v64i16_v128i16_u8(const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningSaturateScalarBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningsaturatescalarbyselectedscalar_v64i16_v64i16_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningSaturateScalarBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmullh_lane_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), dn2cpp_isa_bits<int16x4_t>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningsaturatescalarbyselectedscalar_v64i16_v64i16_u8(const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningSaturateScalarBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningsaturatescalarbyselectedscalar_v64i32_v128i32_u8(const Dn2CppVector64& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningSaturateScalarBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmulls_laneq_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), dn2cpp_isa_bits<int32x4_t>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningsaturatescalarbyselectedscalar_v64i32_v128i32_u8(const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningSaturateScalarBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningsaturatescalarbyselectedscalar_v64i32_v64i32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningSaturateScalarBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmulls_lane_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), dn2cpp_isa_bits<int32x2_t>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningsaturatescalarbyselectedscalar_v64i32_v64i32_u8(const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningSaturateScalarBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningscalarbyselectedscalarandaddsaturate_v64i32_v64i16_v128i16_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningScalarBySelectedScalarAndAddSaturate");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmlalh_laneq_s16(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0), dn2cpp_isa_bits<int16x8_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningscalarbyselectedscalarandaddsaturate_v64i32_v64i16_v128i16_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningScalarBySelectedScalarAndAddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningscalarbyselectedscalarandaddsaturate_v64i32_v64i16_v64i16_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningScalarBySelectedScalarAndAddSaturate");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmlalh_lane_s16(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0), dn2cpp_isa_bits<int16x4_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningscalarbyselectedscalarandaddsaturate_v64i32_v64i16_v64i16_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningScalarBySelectedScalarAndAddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningscalarbyselectedscalarandaddsaturate_v64i64_v64i32_v128i32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningScalarBySelectedScalarAndAddSaturate");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmlals_laneq_s32(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0), dn2cpp_isa_bits<int32x4_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningscalarbyselectedscalarandaddsaturate_v64i64_v64i32_v128i32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningScalarBySelectedScalarAndAddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningscalarbyselectedscalarandaddsaturate_v64i64_v64i32_v64i32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningScalarBySelectedScalarAndAddSaturate");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmlals_lane_s32(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0), dn2cpp_isa_bits<int32x2_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningscalarbyselectedscalarandaddsaturate_v64i64_v64i32_v64i32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningScalarBySelectedScalarAndAddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningscalarbyselectedscalarandsubtractsaturate_v64i32_v64i16_v128i16_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningScalarBySelectedScalarAndSubtractSaturate");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmlslh_laneq_s16(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0), dn2cpp_isa_bits<int16x8_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningscalarbyselectedscalarandsubtractsaturate_v64i32_v64i16_v128i16_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningScalarBySelectedScalarAndSubtractSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningscalarbyselectedscalarandsubtractsaturate_v64i32_v64i16_v64i16_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningScalarBySelectedScalarAndSubtractSaturate");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmlslh_lane_s16(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0), dn2cpp_isa_bits<int16x4_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningscalarbyselectedscalarandsubtractsaturate_v64i32_v64i16_v64i16_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningScalarBySelectedScalarAndSubtractSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningscalarbyselectedscalarandsubtractsaturate_v64i64_v64i32_v128i32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningScalarBySelectedScalarAndSubtractSaturate");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmlsls_laneq_s32(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0), dn2cpp_isa_bits<int32x4_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningscalarbyselectedscalarandsubtractsaturate_v64i64_v64i32_v128i32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningScalarBySelectedScalarAndSubtractSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningscalarbyselectedscalarandsubtractsaturate_v64i64_v64i32_v64i32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningScalarBySelectedScalarAndSubtractSaturate");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a3, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqdmlsls_lane_s32(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0), dn2cpp_isa_bits<int32x2_t>(a2), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplydoublingwideningscalarbyselectedscalarandsubtractsaturate_v64i64_v64i32_v64i32_u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyDoublingWideningScalarBySelectedScalarAndSubtractSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_multiplyextended_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtended");
    return dn2cpp_isa_vec<16>(vmulxq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_multiplyextended_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtended");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_multiplyextended_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtended");
    return dn2cpp_isa_vec<16>(vmulxq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_multiplyextended_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtended");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyextended_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtended");
    return dn2cpp_isa_vec<8>(vmulx_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyextended_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtended");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedbyscalar_v128f64_v64f64(const Dn2CppVector128& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedByScalar");
    return dn2cpp_isa_vec<16>(vmulxq_lane_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x1_t>(a1), 0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedbyscalar_v128f64_v64f64(const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedByScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedbyselectedscalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, dn2cpp_isa_vec<16>(vmulxq_laneq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedbyselectedscalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedbyselectedscalar_v128f32_v64f32_u8(const Dn2CppVector128& a0, const Dn2CppVector64& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, dn2cpp_isa_vec<16>(vmulxq_lane_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedbyselectedscalar_v128f32_v64f32_u8(const Dn2CppVector128&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedbyselectedscalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, dn2cpp_isa_vec<16>(vmulxq_laneq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedbyselectedscalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedbyselectedscalar_v64f32_v128f32_u8(const Dn2CppVector64& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, dn2cpp_isa_vec<8>(vmulx_laneq_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedbyselectedscalar_v64f32_v128f32_u8(const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedbyselectedscalar_v64f32_v64f32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, dn2cpp_isa_vec<8>(vmulx_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedbyselectedscalar_v64f32_v64f32_u8(const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedscalar_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vmulxs_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedscalar_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedscalar_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vmulxd_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0), vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedscalar_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedscalarbyselectedscalar_v64f32_v128f32_u8(const Dn2CppVector64& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedScalarBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vmulxs_laneq_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), dn2cpp_isa_bits<float32x4_t>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedscalarbyselectedscalar_v64f32_v128f32_u8(const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedScalarBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedscalarbyselectedscalar_v64f32_v64f32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedScalarBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vmulxs_lane_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), dn2cpp_isa_bits<float32x2_t>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedscalarbyselectedscalar_v64f32_v64f32_u8(const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedScalarBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedscalarbyselectedscalar_v64f64_v128f64_u8(const Dn2CppVector64& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedScalarBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vmulxd_laneq_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0), dn2cpp_isa_bits<float64x2_t>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyextendedscalarbyselectedscalar_v64f64_v128f64_u8(const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyExtendedScalarBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyroundeddoublingsaturatehighscalar_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyRoundedDoublingSaturateHighScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrdmulhh_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyroundeddoublingsaturatehighscalar_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyRoundedDoublingSaturateHighScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyroundeddoublingsaturatehighscalar_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyRoundedDoublingSaturateHighScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrdmulhs_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyroundeddoublingsaturatehighscalar_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyRoundedDoublingSaturateHighScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyroundeddoublingscalarbyselectedscalarsaturatehigh_v64i16_v128i16_u8(const Dn2CppVector64& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a2, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrdmulhh_laneq_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), dn2cpp_isa_bits<int16x8_t>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyroundeddoublingscalarbyselectedscalarsaturatehigh_v64i16_v128i16_u8(const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyroundeddoublingscalarbyselectedscalarsaturatehigh_v64i16_v64i16_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrdmulhh_lane_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), dn2cpp_isa_bits<int16x4_t>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyroundeddoublingscalarbyselectedscalarsaturatehigh_v64i16_v64i16_u8(const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyroundeddoublingscalarbyselectedscalarsaturatehigh_v64i32_v128i32_u8(const Dn2CppVector64& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrdmulhs_laneq_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), dn2cpp_isa_bits<int32x4_t>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyroundeddoublingscalarbyselectedscalarsaturatehigh_v64i32_v128i32_u8(const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyroundeddoublingscalarbyselectedscalarsaturatehigh_v64i32_v64i32_u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarSaturateHigh");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrdmulhs_lane_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), dn2cpp_isa_bits<int32x2_t>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyroundeddoublingscalarbyselectedscalarsaturatehigh_v64i32_v64i32_u8(const Dn2CppVector64&, const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyRoundedDoublingScalarBySelectedScalarSaturateHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyscalarbyselectedscalar_v64f64_v128f64_u8(const Dn2CppVector64& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyScalarBySelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vmuld_laneq_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0), dn2cpp_isa_bits<float64x2_t>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_multiplyscalarbyselectedscalar_v64f64_v128f64_u8(const Dn2CppVector64&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.MultiplyScalarBySelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_negate_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Negate");
    return dn2cpp_isa_vec<16>(vnegq_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_negate_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Negate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_negate_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Negate");
    return dn2cpp_isa_vec<16>(vnegq_s64(dn2cpp_isa_bits<int64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_negate_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Negate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_negatesaturate_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.NegateSaturate");
    return dn2cpp_isa_vec<16>(vqnegq_s64(dn2cpp_isa_bits<int64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_negatesaturate_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.NegateSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_negatesaturatescalar_v64i16(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.NegateSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqnegh_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_negatesaturatescalar_v64i16(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.NegateSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_negatesaturatescalar_v64i32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.NegateSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqnegs_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_negatesaturatescalar_v64i32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.NegateSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_negatesaturatescalar_v64i64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.NegateSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqnegd_s64(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_negatesaturatescalar_v64i64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.NegateSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_negatesaturatescalar_v64i8(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.NegateSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqnegb_s8(vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_negatesaturatescalar_v64i8(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.NegateSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_negatescalar_v64i64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.NegateScalar");
    return dn2cpp_isa_vec<8>(vneg_s64(dn2cpp_isa_bits<int64x1_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_negatescalar_v64i64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.NegateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_reciprocalestimate_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalEstimate");
    return dn2cpp_isa_vec<16>(vrecpeq_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_reciprocalestimate_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalEstimate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalestimatescalar_v64f32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalEstimateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vrecpes_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalestimatescalar_v64f32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalEstimateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalestimatescalar_v64f64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalEstimateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vrecped_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalestimatescalar_v64f64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalEstimateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalexponentscalar_v64f32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalExponentScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vrecpxs_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalexponentscalar_v64f32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalExponentScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalexponentscalar_v64f64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalExponentScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vrecpxd_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalexponentscalar_v64f64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalExponentScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_reciprocalsquarerootestimate_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalSquareRootEstimate");
    return dn2cpp_isa_vec<16>(vrsqrteq_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_reciprocalsquarerootestimate_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalSquareRootEstimate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalsquarerootestimatescalar_v64f32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalSquareRootEstimateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vrsqrtes_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalsquarerootestimatescalar_v64f32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalSquareRootEstimateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalsquarerootestimatescalar_v64f64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalSquareRootEstimateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vrsqrted_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalsquarerootestimatescalar_v64f64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalSquareRootEstimateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_reciprocalsquarerootstep_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalSquareRootStep");
    return dn2cpp_isa_vec<16>(vrsqrtsq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_reciprocalsquarerootstep_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalSquareRootStep");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalsquarerootstepscalar_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalSquareRootStepScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vrsqrtss_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalsquarerootstepscalar_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalSquareRootStepScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalsquarerootstepscalar_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalSquareRootStepScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vrsqrtsd_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0), vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalsquarerootstepscalar_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalSquareRootStepScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_reciprocalstep_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalStep");
    return dn2cpp_isa_vec<16>(vrecpsq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_reciprocalstep_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalStep");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalstepscalar_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalStepScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vrecpss_f32(vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), 0), vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalstepscalar_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalStepScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalstepscalar_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalStepScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vrecpsd_f64(vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a0), 0), vget_lane_f64(dn2cpp_isa_bits<float64x1_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reciprocalstepscalar_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReciprocalStepScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_reverseelementbits_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReverseElementBits");
    return dn2cpp_isa_vec<16>(vrbitq_s8(dn2cpp_isa_bits<int8x16_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_reverseelementbits_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReverseElementBits");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_reverseelementbits_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReverseElementBits");
    return dn2cpp_isa_vec<16>(vrbitq_u8(dn2cpp_isa_bits<uint8x16_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_reverseelementbits_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReverseElementBits");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reverseelementbits_v64i8(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReverseElementBits");
    return dn2cpp_isa_vec<8>(vrbit_s8(dn2cpp_isa_bits<int8x8_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reverseelementbits_v64i8(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReverseElementBits");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reverseelementbits_v64u8(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReverseElementBits");
    return dn2cpp_isa_vec<8>(vrbit_u8(dn2cpp_isa_bits<uint8x8_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_reverseelementbits_v64u8(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ReverseElementBits");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_roundawayfromzero_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.RoundAwayFromZero");
    return dn2cpp_isa_vec<16>(vrndaq_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_roundawayfromzero_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.RoundAwayFromZero");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_roundtonearest_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.RoundToNearest");
    return dn2cpp_isa_vec<16>(vrndnq_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_roundtonearest_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.RoundToNearest");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_roundtonegativeinfinity_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.RoundToNegativeInfinity");
    return dn2cpp_isa_vec<16>(vrndmq_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_roundtonegativeinfinity_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.RoundToNegativeInfinity");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_roundtopositiveinfinity_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.RoundToPositiveInfinity");
    return dn2cpp_isa_vec<16>(vrndpq_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_roundtopositiveinfinity_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.RoundToPositiveInfinity");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_roundtozero_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.RoundToZero");
    return dn2cpp_isa_vec<16>(vrndq_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_roundtozero_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.RoundToZero");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftarithmeticroundedsaturatescalar_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftArithmeticRoundedSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshlh_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftarithmeticroundedsaturatescalar_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftArithmeticRoundedSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftarithmeticroundedsaturatescalar_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftArithmeticRoundedSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshls_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftarithmeticroundedsaturatescalar_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftArithmeticRoundedSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftarithmeticroundedsaturatescalar_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftArithmeticRoundedSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshlb_s8(vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a0), 0), vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftarithmeticroundedsaturatescalar_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftArithmeticRoundedSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftarithmeticsaturatescalar_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftArithmeticSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshlh_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftarithmeticsaturatescalar_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftArithmeticSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftarithmeticsaturatescalar_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftArithmeticSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshls_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftarithmeticsaturatescalar_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftArithmeticSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftarithmeticsaturatescalar_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftArithmeticSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshlb_s8(vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a0), 0), vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftarithmeticsaturatescalar_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftArithmeticSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftleftlogicalsaturatescalar_v64i16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLeftLogicalSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshlh_n_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftleftlogicalsaturatescalar_v64i16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLeftLogicalSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftleftlogicalsaturatescalar_v64i32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLeftLogicalSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshls_n_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftleftlogicalsaturatescalar_v64i32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLeftLogicalSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftleftlogicalsaturatescalar_v64i8_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLeftLogicalSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshlb_n_s8(vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftleftlogicalsaturatescalar_v64i8_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLeftLogicalSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftleftlogicalsaturatescalar_v64u16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLeftLogicalSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshlh_n_u16(vget_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftleftlogicalsaturatescalar_v64u16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLeftLogicalSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftleftlogicalsaturatescalar_v64u32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLeftLogicalSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshls_n_u32(vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftleftlogicalsaturatescalar_v64u32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLeftLogicalSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftleftlogicalsaturatescalar_v64u8_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLeftLogicalSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshlb_n_u8(vget_lane_u8(dn2cpp_isa_bits<uint8x8_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftleftlogicalsaturatescalar_v64u8_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLeftLogicalSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftleftlogicalsaturateunsignedscalar_v64i16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLeftLogicalSaturateUnsignedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshluh_n_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftleftlogicalsaturateunsignedscalar_v64i16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLeftLogicalSaturateUnsignedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftleftlogicalsaturateunsignedscalar_v64i32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLeftLogicalSaturateUnsignedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshlus_n_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftleftlogicalsaturateunsignedscalar_v64i32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLeftLogicalSaturateUnsignedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftleftlogicalsaturateunsignedscalar_v64i8_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLeftLogicalSaturateUnsignedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshlub_n_s8(vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftleftlogicalsaturateunsignedscalar_v64i8_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLeftLogicalSaturateUnsignedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalroundedsaturatescalar_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalRoundedSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshlh_u16(vget_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalroundedsaturatescalar_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalRoundedSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalroundedsaturatescalar_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalRoundedSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshls_u32(vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalroundedsaturatescalar_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalRoundedSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalroundedsaturatescalar_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalRoundedSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshlb_u8(vget_lane_u8(dn2cpp_isa_bits<uint8x8_t>(a0), 0), vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalroundedsaturatescalar_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalRoundedSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalroundedsaturatescalar_v64u16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalRoundedSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshlh_u16(vget_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalroundedsaturatescalar_v64u16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalRoundedSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalroundedsaturatescalar_v64u32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalRoundedSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshls_u32(vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalroundedsaturatescalar_v64u32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalRoundedSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalroundedsaturatescalar_v64u8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalRoundedSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshlb_u8(vget_lane_u8(dn2cpp_isa_bits<uint8x8_t>(a0), 0), vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalroundedsaturatescalar_v64u8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalRoundedSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalsaturatescalar_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshlh_u16(vget_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalsaturatescalar_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalsaturatescalar_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshls_u32(vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalsaturatescalar_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalsaturatescalar_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshlb_u8(vget_lane_u8(dn2cpp_isa_bits<uint8x8_t>(a0), 0), vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalsaturatescalar_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalsaturatescalar_v64u16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshlh_u16(vget_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalsaturatescalar_v64u16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalsaturatescalar_v64u32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshls_u32(vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalsaturatescalar_v64u32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalsaturatescalar_v64u8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshlb_u8(vget_lane_u8(dn2cpp_isa_bits<uint8x8_t>(a0), 0), vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftlogicalsaturatescalar_v64u8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftLogicalSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticnarrowingsaturatescalar_v64i16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticNarrowingSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 8, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshrnh_n_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticnarrowingsaturatescalar_v64i16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticnarrowingsaturatescalar_v64i32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticNarrowingSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 16, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshrns_n_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticnarrowingsaturatescalar_v64i32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticnarrowingsaturatescalar_v64i64_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticNarrowingSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 32, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshrnd_n_s64(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticnarrowingsaturatescalar_v64i64_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticnarrowingsaturateunsignedscalar_v64i16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticNarrowingSaturateUnsignedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 8, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshrunh_n_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticnarrowingsaturateunsignedscalar_v64i16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticNarrowingSaturateUnsignedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticnarrowingsaturateunsignedscalar_v64i32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticNarrowingSaturateUnsignedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 16, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshruns_n_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticnarrowingsaturateunsignedscalar_v64i32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticNarrowingSaturateUnsignedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticnarrowingsaturateunsignedscalar_v64i64_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticNarrowingSaturateUnsignedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 32, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshrund_n_s64(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticnarrowingsaturateunsignedscalar_v64i64_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticNarrowingSaturateUnsignedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticroundednarrowingsaturatescalar_v64i16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticRoundedNarrowingSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 8, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshrnh_n_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticroundednarrowingsaturatescalar_v64i16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticRoundedNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticroundednarrowingsaturatescalar_v64i32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticRoundedNarrowingSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 16, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshrns_n_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticroundednarrowingsaturatescalar_v64i32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticRoundedNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticroundednarrowingsaturatescalar_v64i64_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticRoundedNarrowingSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 32, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshrnd_n_s64(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticroundednarrowingsaturatescalar_v64i64_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticRoundedNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticroundednarrowingsaturateunsignedscalar_v64i16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticRoundedNarrowingSaturateUnsignedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 8, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshrunh_n_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticroundednarrowingsaturateunsignedscalar_v64i16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticRoundedNarrowingSaturateUnsignedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticroundednarrowingsaturateunsignedscalar_v64i32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticRoundedNarrowingSaturateUnsignedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 16, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshruns_n_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticroundednarrowingsaturateunsignedscalar_v64i32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticRoundedNarrowingSaturateUnsignedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticroundednarrowingsaturateunsignedscalar_v64i64_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticRoundedNarrowingSaturateUnsignedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 32, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshrund_n_s64(vget_lane_s64(dn2cpp_isa_bits<int64x1_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightarithmeticroundednarrowingsaturateunsignedscalar_v64i64_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightArithmeticRoundedNarrowingSaturateUnsignedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalnarrowingsaturatescalar_v64i16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalNarrowingSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 8, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshrnh_n_u16(vget_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalnarrowingsaturatescalar_v64i16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalnarrowingsaturatescalar_v64i32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalNarrowingSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 16, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshrns_n_u32(vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalnarrowingsaturatescalar_v64i32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalnarrowingsaturatescalar_v64i64_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalNarrowingSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 32, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshrnd_n_u64(vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalnarrowingsaturatescalar_v64i64_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalnarrowingsaturatescalar_v64u16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalNarrowingSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 8, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshrnh_n_u16(vget_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalnarrowingsaturatescalar_v64u16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalnarrowingsaturatescalar_v64u32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalNarrowingSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 16, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshrns_n_u32(vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalnarrowingsaturatescalar_v64u32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalnarrowingsaturatescalar_v64u64_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalNarrowingSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 32, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqshrnd_n_u64(vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalnarrowingsaturatescalar_v64u64_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalroundednarrowingsaturatescalar_v64i16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalRoundedNarrowingSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 8, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshrnh_n_u16(vget_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalroundednarrowingsaturatescalar_v64i16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalRoundedNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalroundednarrowingsaturatescalar_v64i32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalRoundedNarrowingSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 16, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshrns_n_u32(vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalroundednarrowingsaturatescalar_v64i32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalRoundedNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalroundednarrowingsaturatescalar_v64i64_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalRoundedNarrowingSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 32, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshrnd_n_u64(vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalroundednarrowingsaturatescalar_v64i64_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalRoundedNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalroundednarrowingsaturatescalar_v64u16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalRoundedNarrowingSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 8, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshrnh_n_u16(vget_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalroundednarrowingsaturatescalar_v64u16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalRoundedNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalroundednarrowingsaturatescalar_v64u32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalRoundedNarrowingSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 16, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshrns_n_u32(vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalroundednarrowingsaturatescalar_v64u32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalRoundedNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalroundednarrowingsaturatescalar_v64u64_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalRoundedNarrowingSaturateScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 32, a1, dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqrshrnd_n_u64(vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a0), 0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_shiftrightlogicalroundednarrowingsaturatescalar_v64u64_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ShiftRightLogicalRoundedNarrowingSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_sqrt_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Sqrt");
    return dn2cpp_isa_vec<16>(vsqrtq_f32(dn2cpp_isa_bits<float32x4_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_sqrt_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Sqrt");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_sqrt_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Sqrt");
    return dn2cpp_isa_vec<16>(vsqrtq_f64(dn2cpp_isa_bits<float64x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_sqrt_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Sqrt");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_sqrt_v64f32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Sqrt");
    return dn2cpp_isa_vec<8>(vsqrt_f32(dn2cpp_isa_bits<float32x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_sqrt_v64f32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Sqrt");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pf32_t2v128f32(float* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_f32_x2(a0, (float32x4x2_t{{dn2cpp_isa_bits<float32x4_t>(a1_1), dn2cpp_isa_bits<float32x4_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pf32_t2v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pf32_t3v128f32(float* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_f32_x3(a0, (float32x4x3_t{{dn2cpp_isa_bits<float32x4_t>(a1_1), dn2cpp_isa_bits<float32x4_t>(a1_2), dn2cpp_isa_bits<float32x4_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pf32_t3v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pf32_t4v128f32(float* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_f32_x4(a0, (float32x4x4_t{{dn2cpp_isa_bits<float32x4_t>(a1_1), dn2cpp_isa_bits<float32x4_t>(a1_2), dn2cpp_isa_bits<float32x4_t>(a1_3), dn2cpp_isa_bits<float32x4_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pf32_t4v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pf64_t2v128f64(double* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_f64_x2(a0, (float64x2x2_t{{dn2cpp_isa_bits<float64x2_t>(a1_1), dn2cpp_isa_bits<float64x2_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pf64_t2v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pf64_t3v128f64(double* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_f64_x3(a0, (float64x2x3_t{{dn2cpp_isa_bits<float64x2_t>(a1_1), dn2cpp_isa_bits<float64x2_t>(a1_2), dn2cpp_isa_bits<float64x2_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pf64_t3v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pf64_t4v128f64(double* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_f64_x4(a0, (float64x2x4_t{{dn2cpp_isa_bits<float64x2_t>(a1_1), dn2cpp_isa_bits<float64x2_t>(a1_2), dn2cpp_isa_bits<float64x2_t>(a1_3), dn2cpp_isa_bits<float64x2_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pf64_t4v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi16_t2v128i16(int16_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_s16_x2(a0, (int16x8x2_t{{dn2cpp_isa_bits<int16x8_t>(a1_1), dn2cpp_isa_bits<int16x8_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi16_t2v128i16(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi16_t3v128i16(int16_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_s16_x3(a0, (int16x8x3_t{{dn2cpp_isa_bits<int16x8_t>(a1_1), dn2cpp_isa_bits<int16x8_t>(a1_2), dn2cpp_isa_bits<int16x8_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi16_t3v128i16(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi16_t4v128i16(int16_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_s16_x4(a0, (int16x8x4_t{{dn2cpp_isa_bits<int16x8_t>(a1_1), dn2cpp_isa_bits<int16x8_t>(a1_2), dn2cpp_isa_bits<int16x8_t>(a1_3), dn2cpp_isa_bits<int16x8_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi16_t4v128i16(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi32_t2v128i32(int32_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_s32_x2(a0, (int32x4x2_t{{dn2cpp_isa_bits<int32x4_t>(a1_1), dn2cpp_isa_bits<int32x4_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi32_t2v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi32_t3v128i32(int32_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_s32_x3(a0, (int32x4x3_t{{dn2cpp_isa_bits<int32x4_t>(a1_1), dn2cpp_isa_bits<int32x4_t>(a1_2), dn2cpp_isa_bits<int32x4_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi32_t3v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi32_t4v128i32(int32_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_s32_x4(a0, (int32x4x4_t{{dn2cpp_isa_bits<int32x4_t>(a1_1), dn2cpp_isa_bits<int32x4_t>(a1_2), dn2cpp_isa_bits<int32x4_t>(a1_3), dn2cpp_isa_bits<int32x4_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi32_t4v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi64_t2v128i64(int64_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_s64_x2(a0, (int64x2x2_t{{dn2cpp_isa_bits<int64x2_t>(a1_1), dn2cpp_isa_bits<int64x2_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi64_t2v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi64_t3v128i64(int64_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_s64_x3(a0, (int64x2x3_t{{dn2cpp_isa_bits<int64x2_t>(a1_1), dn2cpp_isa_bits<int64x2_t>(a1_2), dn2cpp_isa_bits<int64x2_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi64_t3v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi64_t4v128i64(int64_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_s64_x4(a0, (int64x2x4_t{{dn2cpp_isa_bits<int64x2_t>(a1_1), dn2cpp_isa_bits<int64x2_t>(a1_2), dn2cpp_isa_bits<int64x2_t>(a1_3), dn2cpp_isa_bits<int64x2_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi64_t4v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi8_t2v128i8(int8_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_s8_x2(a0, (int8x16x2_t{{dn2cpp_isa_bits<int8x16_t>(a1_1), dn2cpp_isa_bits<int8x16_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi8_t2v128i8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi8_t3v128i8(int8_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_s8_x3(a0, (int8x16x3_t{{dn2cpp_isa_bits<int8x16_t>(a1_1), dn2cpp_isa_bits<int8x16_t>(a1_2), dn2cpp_isa_bits<int8x16_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi8_t3v128i8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi8_t4v128i8(int8_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_s8_x4(a0, (int8x16x4_t{{dn2cpp_isa_bits<int8x16_t>(a1_1), dn2cpp_isa_bits<int8x16_t>(a1_2), dn2cpp_isa_bits<int8x16_t>(a1_3), dn2cpp_isa_bits<int8x16_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pi8_t4v128i8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu16_t2v128u16(uint16_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_u16_x2(a0, (uint16x8x2_t{{dn2cpp_isa_bits<uint16x8_t>(a1_1), dn2cpp_isa_bits<uint16x8_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu16_t2v128u16(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu16_t3v128u16(uint16_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_u16_x3(a0, (uint16x8x3_t{{dn2cpp_isa_bits<uint16x8_t>(a1_1), dn2cpp_isa_bits<uint16x8_t>(a1_2), dn2cpp_isa_bits<uint16x8_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu16_t3v128u16(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu16_t4v128u16(uint16_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_u16_x4(a0, (uint16x8x4_t{{dn2cpp_isa_bits<uint16x8_t>(a1_1), dn2cpp_isa_bits<uint16x8_t>(a1_2), dn2cpp_isa_bits<uint16x8_t>(a1_3), dn2cpp_isa_bits<uint16x8_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu16_t4v128u16(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu32_t2v128u32(uint32_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_u32_x2(a0, (uint32x4x2_t{{dn2cpp_isa_bits<uint32x4_t>(a1_1), dn2cpp_isa_bits<uint32x4_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu32_t2v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu32_t3v128u32(uint32_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_u32_x3(a0, (uint32x4x3_t{{dn2cpp_isa_bits<uint32x4_t>(a1_1), dn2cpp_isa_bits<uint32x4_t>(a1_2), dn2cpp_isa_bits<uint32x4_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu32_t3v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu32_t4v128u32(uint32_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_u32_x4(a0, (uint32x4x4_t{{dn2cpp_isa_bits<uint32x4_t>(a1_1), dn2cpp_isa_bits<uint32x4_t>(a1_2), dn2cpp_isa_bits<uint32x4_t>(a1_3), dn2cpp_isa_bits<uint32x4_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu32_t4v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu64_t2v128u64(uint64_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_u64_x2(a0, (uint64x2x2_t{{dn2cpp_isa_bits<uint64x2_t>(a1_1), dn2cpp_isa_bits<uint64x2_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu64_t2v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu64_t3v128u64(uint64_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_u64_x3(a0, (uint64x2x3_t{{dn2cpp_isa_bits<uint64x2_t>(a1_1), dn2cpp_isa_bits<uint64x2_t>(a1_2), dn2cpp_isa_bits<uint64x2_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu64_t3v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu64_t4v128u64(uint64_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_u64_x4(a0, (uint64x2x4_t{{dn2cpp_isa_bits<uint64x2_t>(a1_1), dn2cpp_isa_bits<uint64x2_t>(a1_2), dn2cpp_isa_bits<uint64x2_t>(a1_3), dn2cpp_isa_bits<uint64x2_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu64_t4v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu8_t2v128u8(uint8_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_u8_x2(a0, (uint8x16x2_t{{dn2cpp_isa_bits<uint8x16_t>(a1_1), dn2cpp_isa_bits<uint8x16_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu8_t2v128u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu8_t3v128u8(uint8_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_u8_x3(a0, (uint8x16x3_t{{dn2cpp_isa_bits<uint8x16_t>(a1_1), dn2cpp_isa_bits<uint8x16_t>(a1_2), dn2cpp_isa_bits<uint8x16_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu8_t3v128u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu8_t4v128u8(uint8_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
    vst1q_u8_x4(a0, (uint8x16x4_t{{dn2cpp_isa_bits<uint8x16_t>(a1_1), dn2cpp_isa_bits<uint8x16_t>(a1_2), dn2cpp_isa_bits<uint8x16_t>(a1_3), dn2cpp_isa_bits<uint8x16_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_store_pu8_t4v128u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pf32_v128f32_v128f32(float* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1q_f32(a0, dn2cpp_isa_bits<float32x4_t>(a1)), vst1q_f32(a0 + 4, dn2cpp_isa_bits<float32x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pf32_v128f32_v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pf32_v64f32_v64f32(float* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1_f32(a0, dn2cpp_isa_bits<float32x2_t>(a1)), vst1_f32(a0 + 2, dn2cpp_isa_bits<float32x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pf32_v64f32_v64f32(float*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pf64_v128f64_v128f64(double* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1q_f64(a0, dn2cpp_isa_bits<float64x2_t>(a1)), vst1q_f64(a0 + 2, dn2cpp_isa_bits<float64x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pf64_v128f64_v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pf64_v64f64_v64f64(double* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1_f64(a0, dn2cpp_isa_bits<float64x1_t>(a1)), vst1_f64(a0 + 1, dn2cpp_isa_bits<float64x1_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pf64_v64f64_v64f64(double*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pi16_v128i16_v128i16(int16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1q_s16(a0, dn2cpp_isa_bits<int16x8_t>(a1)), vst1q_s16(a0 + 8, dn2cpp_isa_bits<int16x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pi16_v128i16_v128i16(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pi16_v64i16_v64i16(int16_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1_s16(a0, dn2cpp_isa_bits<int16x4_t>(a1)), vst1_s16(a0 + 4, dn2cpp_isa_bits<int16x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pi16_v64i16_v64i16(int16_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pi32_v128i32_v128i32(int32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1q_s32(a0, dn2cpp_isa_bits<int32x4_t>(a1)), vst1q_s32(a0 + 4, dn2cpp_isa_bits<int32x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pi32_v128i32_v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pi32_v64i32_v64i32(int32_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1_s32(a0, dn2cpp_isa_bits<int32x2_t>(a1)), vst1_s32(a0 + 2, dn2cpp_isa_bits<int32x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pi32_v64i32_v64i32(int32_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pi64_v128i64_v128i64(int64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1q_s64(a0, dn2cpp_isa_bits<int64x2_t>(a1)), vst1q_s64(a0 + 2, dn2cpp_isa_bits<int64x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pi64_v128i64_v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pi64_v64i64_v64i64(int64_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1_s64(a0, dn2cpp_isa_bits<int64x1_t>(a1)), vst1_s64(a0 + 1, dn2cpp_isa_bits<int64x1_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pi64_v64i64_v64i64(int64_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pi8_v128i8_v128i8(int8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1q_s8(a0, dn2cpp_isa_bits<int8x16_t>(a1)), vst1q_s8(a0 + 16, dn2cpp_isa_bits<int8x16_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pi8_v128i8_v128i8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pi8_v64i8_v64i8(int8_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1_s8(a0, dn2cpp_isa_bits<int8x8_t>(a1)), vst1_s8(a0 + 8, dn2cpp_isa_bits<int8x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pi8_v64i8_v64i8(int8_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pu16_v128u16_v128u16(uint16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1q_u16(a0, dn2cpp_isa_bits<uint16x8_t>(a1)), vst1q_u16(a0 + 8, dn2cpp_isa_bits<uint16x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pu16_v128u16_v128u16(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pu16_v64u16_v64u16(uint16_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1_u16(a0, dn2cpp_isa_bits<uint16x4_t>(a1)), vst1_u16(a0 + 4, dn2cpp_isa_bits<uint16x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pu16_v64u16_v64u16(uint16_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pu32_v128u32_v128u32(uint32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1q_u32(a0, dn2cpp_isa_bits<uint32x4_t>(a1)), vst1q_u32(a0 + 4, dn2cpp_isa_bits<uint32x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pu32_v128u32_v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pu32_v64u32_v64u32(uint32_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1_u32(a0, dn2cpp_isa_bits<uint32x2_t>(a1)), vst1_u32(a0 + 2, dn2cpp_isa_bits<uint32x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pu32_v64u32_v64u32(uint32_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pu64_v128u64_v128u64(uint64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1q_u64(a0, dn2cpp_isa_bits<uint64x2_t>(a1)), vst1q_u64(a0 + 2, dn2cpp_isa_bits<uint64x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pu64_v128u64_v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pu64_v64u64_v64u64(uint64_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1_u64(a0, dn2cpp_isa_bits<uint64x1_t>(a1)), vst1_u64(a0 + 1, dn2cpp_isa_bits<uint64x1_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pu64_v64u64_v64u64(uint64_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pu8_v128u8_v128u8(uint8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1q_u8(a0, dn2cpp_isa_bits<uint8x16_t>(a1)), vst1q_u8(a0 + 16, dn2cpp_isa_bits<uint8x16_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pu8_v128u8_v128u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pu8_v64u8_v64u8(uint8_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
    (vst1_u8(a0, dn2cpp_isa_bits<uint8x8_t>(a1)), vst1_u8(a0 + 8, dn2cpp_isa_bits<uint8x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepair_pu8_v64u8_v64u8(uint8_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePair");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pf32_v128f32_v128f32(float* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1q_f32(a0, dn2cpp_isa_bits<float32x4_t>(a1)), vst1q_f32(a0 + 4, dn2cpp_isa_bits<float32x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pf32_v128f32_v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pf32_v64f32_v64f32(float* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1_f32(a0, dn2cpp_isa_bits<float32x2_t>(a1)), vst1_f32(a0 + 2, dn2cpp_isa_bits<float32x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pf32_v64f32_v64f32(float*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pf64_v128f64_v128f64(double* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1q_f64(a0, dn2cpp_isa_bits<float64x2_t>(a1)), vst1q_f64(a0 + 2, dn2cpp_isa_bits<float64x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pf64_v128f64_v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pf64_v64f64_v64f64(double* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1_f64(a0, dn2cpp_isa_bits<float64x1_t>(a1)), vst1_f64(a0 + 1, dn2cpp_isa_bits<float64x1_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pf64_v64f64_v64f64(double*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pi16_v128i16_v128i16(int16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1q_s16(a0, dn2cpp_isa_bits<int16x8_t>(a1)), vst1q_s16(a0 + 8, dn2cpp_isa_bits<int16x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pi16_v128i16_v128i16(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pi16_v64i16_v64i16(int16_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1_s16(a0, dn2cpp_isa_bits<int16x4_t>(a1)), vst1_s16(a0 + 4, dn2cpp_isa_bits<int16x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pi16_v64i16_v64i16(int16_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pi32_v128i32_v128i32(int32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1q_s32(a0, dn2cpp_isa_bits<int32x4_t>(a1)), vst1q_s32(a0 + 4, dn2cpp_isa_bits<int32x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pi32_v128i32_v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pi32_v64i32_v64i32(int32_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1_s32(a0, dn2cpp_isa_bits<int32x2_t>(a1)), vst1_s32(a0 + 2, dn2cpp_isa_bits<int32x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pi32_v64i32_v64i32(int32_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pi64_v128i64_v128i64(int64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1q_s64(a0, dn2cpp_isa_bits<int64x2_t>(a1)), vst1q_s64(a0 + 2, dn2cpp_isa_bits<int64x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pi64_v128i64_v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pi64_v64i64_v64i64(int64_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1_s64(a0, dn2cpp_isa_bits<int64x1_t>(a1)), vst1_s64(a0 + 1, dn2cpp_isa_bits<int64x1_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pi64_v64i64_v64i64(int64_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pi8_v128i8_v128i8(int8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1q_s8(a0, dn2cpp_isa_bits<int8x16_t>(a1)), vst1q_s8(a0 + 16, dn2cpp_isa_bits<int8x16_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pi8_v128i8_v128i8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pi8_v64i8_v64i8(int8_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1_s8(a0, dn2cpp_isa_bits<int8x8_t>(a1)), vst1_s8(a0 + 8, dn2cpp_isa_bits<int8x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pi8_v64i8_v64i8(int8_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pu16_v128u16_v128u16(uint16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1q_u16(a0, dn2cpp_isa_bits<uint16x8_t>(a1)), vst1q_u16(a0 + 8, dn2cpp_isa_bits<uint16x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pu16_v128u16_v128u16(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pu16_v64u16_v64u16(uint16_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1_u16(a0, dn2cpp_isa_bits<uint16x4_t>(a1)), vst1_u16(a0 + 4, dn2cpp_isa_bits<uint16x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pu16_v64u16_v64u16(uint16_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pu32_v128u32_v128u32(uint32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1q_u32(a0, dn2cpp_isa_bits<uint32x4_t>(a1)), vst1q_u32(a0 + 4, dn2cpp_isa_bits<uint32x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pu32_v128u32_v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pu32_v64u32_v64u32(uint32_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1_u32(a0, dn2cpp_isa_bits<uint32x2_t>(a1)), vst1_u32(a0 + 2, dn2cpp_isa_bits<uint32x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pu32_v64u32_v64u32(uint32_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pu64_v128u64_v128u64(uint64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1q_u64(a0, dn2cpp_isa_bits<uint64x2_t>(a1)), vst1q_u64(a0 + 2, dn2cpp_isa_bits<uint64x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pu64_v128u64_v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pu64_v64u64_v64u64(uint64_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1_u64(a0, dn2cpp_isa_bits<uint64x1_t>(a1)), vst1_u64(a0 + 1, dn2cpp_isa_bits<uint64x1_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pu64_v64u64_v64u64(uint64_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pu8_v128u8_v128u8(uint8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1q_u8(a0, dn2cpp_isa_bits<uint8x16_t>(a1)), vst1q_u8(a0 + 16, dn2cpp_isa_bits<uint8x16_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pu8_v128u8_v128u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pu8_v64u8_v64u8(uint8_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
    (vst1_u8(a0, dn2cpp_isa_bits<uint8x8_t>(a1)), vst1_u8(a0 + 8, dn2cpp_isa_bits<uint8x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairnontemporal_pu8_v64u8_v64u8(uint8_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairscalar_pf32_v64f32_v64f32(float* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairScalar");
    (void)(a0[0] = vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0), a0[1] = vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a2), 0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairscalar_pf32_v64f32_v64f32(float*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairscalar_pi32_v64i32_v64i32(int32_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairScalar");
    (void)(a0[0] = vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0), a0[1] = vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a2), 0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairscalar_pi32_v64i32_v64i32(int32_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairscalar_pu32_v64u32_v64u32(uint32_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairScalar");
    (void)(a0[0] = vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a1), 0), a0[1] = vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a2), 0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairscalar_pu32_v64u32_v64u32(uint32_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairscalarnontemporal_pf32_v64f32_v64f32(float* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairScalarNonTemporal");
    (void)(a0[0] = vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a1), 0), a0[1] = vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a2), 0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairscalarnontemporal_pf32_v64f32_v64f32(float*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairScalarNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairscalarnontemporal_pi32_v64i32_v64i32(int32_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairScalarNonTemporal");
    (void)(a0[0] = vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0), a0[1] = vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a2), 0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairscalarnontemporal_pi32_v64i32_v64i32(int32_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairScalarNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairscalarnontemporal_pu32_v64u32_v64u32(uint32_t* a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairScalarNonTemporal");
    (void)(a0[0] = vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a1), 0), a0[1] = vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a2), 0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storepairscalarnontemporal_pu32_v64u32_v64u32(uint32_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StorePairScalarNonTemporal");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pf32_t2v128f32_u8(float* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, vst2q_lane_f32(a0, (float32x4x2_t{{dn2cpp_isa_bits<float32x4_t>(a1_1), dn2cpp_isa_bits<float32x4_t>(a1_2)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pf32_t2v128f32_u8(float*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pf32_t3v128f32_u8(float* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, vst3q_lane_f32(a0, (float32x4x3_t{{dn2cpp_isa_bits<float32x4_t>(a1_1), dn2cpp_isa_bits<float32x4_t>(a1_2), dn2cpp_isa_bits<float32x4_t>(a1_3)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pf32_t3v128f32_u8(float*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pf32_t4v128f32_u8(float* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, vst4q_lane_f32(a0, (float32x4x4_t{{dn2cpp_isa_bits<float32x4_t>(a1_1), dn2cpp_isa_bits<float32x4_t>(a1_2), dn2cpp_isa_bits<float32x4_t>(a1_3), dn2cpp_isa_bits<float32x4_t>(a1_4)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pf32_t4v128f32_u8(float*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pf64_t2v128f64_u8(double* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, vst2q_lane_f64(a0, (float64x2x2_t{{dn2cpp_isa_bits<float64x2_t>(a1_1), dn2cpp_isa_bits<float64x2_t>(a1_2)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pf64_t2v128f64_u8(double*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pf64_t3v128f64_u8(double* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, vst3q_lane_f64(a0, (float64x2x3_t{{dn2cpp_isa_bits<float64x2_t>(a1_1), dn2cpp_isa_bits<float64x2_t>(a1_2), dn2cpp_isa_bits<float64x2_t>(a1_3)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pf64_t3v128f64_u8(double*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pf64_t4v128f64_u8(double* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, vst4q_lane_f64(a0, (float64x2x4_t{{dn2cpp_isa_bits<float64x2_t>(a1_1), dn2cpp_isa_bits<float64x2_t>(a1_2), dn2cpp_isa_bits<float64x2_t>(a1_3), dn2cpp_isa_bits<float64x2_t>(a1_4)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pf64_t4v128f64_u8(double*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi16_t2v128i16_u8(int16_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a2, vst2q_lane_s16(a0, (int16x8x2_t{{dn2cpp_isa_bits<int16x8_t>(a1_1), dn2cpp_isa_bits<int16x8_t>(a1_2)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi16_t2v128i16_u8(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi16_t3v128i16_u8(int16_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a2, vst3q_lane_s16(a0, (int16x8x3_t{{dn2cpp_isa_bits<int16x8_t>(a1_1), dn2cpp_isa_bits<int16x8_t>(a1_2), dn2cpp_isa_bits<int16x8_t>(a1_3)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi16_t3v128i16_u8(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi16_t4v128i16_u8(int16_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a2, vst4q_lane_s16(a0, (int16x8x4_t{{dn2cpp_isa_bits<int16x8_t>(a1_1), dn2cpp_isa_bits<int16x8_t>(a1_2), dn2cpp_isa_bits<int16x8_t>(a1_3), dn2cpp_isa_bits<int16x8_t>(a1_4)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi16_t4v128i16_u8(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi32_t2v128i32_u8(int32_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, vst2q_lane_s32(a0, (int32x4x2_t{{dn2cpp_isa_bits<int32x4_t>(a1_1), dn2cpp_isa_bits<int32x4_t>(a1_2)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi32_t2v128i32_u8(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi32_t3v128i32_u8(int32_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, vst3q_lane_s32(a0, (int32x4x3_t{{dn2cpp_isa_bits<int32x4_t>(a1_1), dn2cpp_isa_bits<int32x4_t>(a1_2), dn2cpp_isa_bits<int32x4_t>(a1_3)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi32_t3v128i32_u8(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi32_t4v128i32_u8(int32_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, vst4q_lane_s32(a0, (int32x4x4_t{{dn2cpp_isa_bits<int32x4_t>(a1_1), dn2cpp_isa_bits<int32x4_t>(a1_2), dn2cpp_isa_bits<int32x4_t>(a1_3), dn2cpp_isa_bits<int32x4_t>(a1_4)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi32_t4v128i32_u8(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi64_t2v128i64_u8(int64_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, vst2q_lane_s64(a0, (int64x2x2_t{{dn2cpp_isa_bits<int64x2_t>(a1_1), dn2cpp_isa_bits<int64x2_t>(a1_2)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi64_t2v128i64_u8(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi64_t3v128i64_u8(int64_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, vst3q_lane_s64(a0, (int64x2x3_t{{dn2cpp_isa_bits<int64x2_t>(a1_1), dn2cpp_isa_bits<int64x2_t>(a1_2), dn2cpp_isa_bits<int64x2_t>(a1_3)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi64_t3v128i64_u8(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi64_t4v128i64_u8(int64_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, vst4q_lane_s64(a0, (int64x2x4_t{{dn2cpp_isa_bits<int64x2_t>(a1_1), dn2cpp_isa_bits<int64x2_t>(a1_2), dn2cpp_isa_bits<int64x2_t>(a1_3), dn2cpp_isa_bits<int64x2_t>(a1_4)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi64_t4v128i64_u8(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi8_t2v128i8_u8(int8_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, vst2q_lane_s8(a0, (int8x16x2_t{{dn2cpp_isa_bits<int8x16_t>(a1_1), dn2cpp_isa_bits<int8x16_t>(a1_2)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi8_t2v128i8_u8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi8_t3v128i8_u8(int8_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, vst3q_lane_s8(a0, (int8x16x3_t{{dn2cpp_isa_bits<int8x16_t>(a1_1), dn2cpp_isa_bits<int8x16_t>(a1_2), dn2cpp_isa_bits<int8x16_t>(a1_3)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi8_t3v128i8_u8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi8_t4v128i8_u8(int8_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, vst4q_lane_s8(a0, (int8x16x4_t{{dn2cpp_isa_bits<int8x16_t>(a1_1), dn2cpp_isa_bits<int8x16_t>(a1_2), dn2cpp_isa_bits<int8x16_t>(a1_3), dn2cpp_isa_bits<int8x16_t>(a1_4)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pi8_t4v128i8_u8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu16_t2v128u16_u8(uint16_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a2, vst2q_lane_u16(a0, (uint16x8x2_t{{dn2cpp_isa_bits<uint16x8_t>(a1_1), dn2cpp_isa_bits<uint16x8_t>(a1_2)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu16_t2v128u16_u8(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu16_t3v128u16_u8(uint16_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a2, vst3q_lane_u16(a0, (uint16x8x3_t{{dn2cpp_isa_bits<uint16x8_t>(a1_1), dn2cpp_isa_bits<uint16x8_t>(a1_2), dn2cpp_isa_bits<uint16x8_t>(a1_3)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu16_t3v128u16_u8(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu16_t4v128u16_u8(uint16_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a2, vst4q_lane_u16(a0, (uint16x8x4_t{{dn2cpp_isa_bits<uint16x8_t>(a1_1), dn2cpp_isa_bits<uint16x8_t>(a1_2), dn2cpp_isa_bits<uint16x8_t>(a1_3), dn2cpp_isa_bits<uint16x8_t>(a1_4)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu16_t4v128u16_u8(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu32_t2v128u32_u8(uint32_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, vst2q_lane_u32(a0, (uint32x4x2_t{{dn2cpp_isa_bits<uint32x4_t>(a1_1), dn2cpp_isa_bits<uint32x4_t>(a1_2)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu32_t2v128u32_u8(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu32_t3v128u32_u8(uint32_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, vst3q_lane_u32(a0, (uint32x4x3_t{{dn2cpp_isa_bits<uint32x4_t>(a1_1), dn2cpp_isa_bits<uint32x4_t>(a1_2), dn2cpp_isa_bits<uint32x4_t>(a1_3)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu32_t3v128u32_u8(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu32_t4v128u32_u8(uint32_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, vst4q_lane_u32(a0, (uint32x4x4_t{{dn2cpp_isa_bits<uint32x4_t>(a1_1), dn2cpp_isa_bits<uint32x4_t>(a1_2), dn2cpp_isa_bits<uint32x4_t>(a1_3), dn2cpp_isa_bits<uint32x4_t>(a1_4)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu32_t4v128u32_u8(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu64_t2v128u64_u8(uint64_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, vst2q_lane_u64(a0, (uint64x2x2_t{{dn2cpp_isa_bits<uint64x2_t>(a1_1), dn2cpp_isa_bits<uint64x2_t>(a1_2)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu64_t2v128u64_u8(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu64_t3v128u64_u8(uint64_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, vst3q_lane_u64(a0, (uint64x2x3_t{{dn2cpp_isa_bits<uint64x2_t>(a1_1), dn2cpp_isa_bits<uint64x2_t>(a1_2), dn2cpp_isa_bits<uint64x2_t>(a1_3)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu64_t3v128u64_u8(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu64_t4v128u64_u8(uint64_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, vst4q_lane_u64(a0, (uint64x2x4_t{{dn2cpp_isa_bits<uint64x2_t>(a1_1), dn2cpp_isa_bits<uint64x2_t>(a1_2), dn2cpp_isa_bits<uint64x2_t>(a1_3), dn2cpp_isa_bits<uint64x2_t>(a1_4)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu64_t4v128u64_u8(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu8_t2v128u8_u8(uint8_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, vst2q_lane_u8(a0, (uint8x16x2_t{{dn2cpp_isa_bits<uint8x16_t>(a1_1), dn2cpp_isa_bits<uint8x16_t>(a1_2)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu8_t2v128u8_u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu8_t3v128u8_u8(uint8_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, vst3q_lane_u8(a0, (uint8x16x3_t{{dn2cpp_isa_bits<uint8x16_t>(a1_1), dn2cpp_isa_bits<uint8x16_t>(a1_2), dn2cpp_isa_bits<uint8x16_t>(a1_3)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu8_t3v128u8_u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu8_t4v128u8_u8(uint8_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, vst4q_lane_u8(a0, (uint8x16x4_t{{dn2cpp_isa_bits<uint8x16_t>(a1_1), dn2cpp_isa_bits<uint8x16_t>(a1_2), dn2cpp_isa_bits<uint8x16_t>(a1_3), dn2cpp_isa_bits<uint8x16_t>(a1_4)}}), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storeselectedscalar_pu8_t4v128u8_u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pf32_t2v128f32(float* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst2q_f32(a0, (float32x4x2_t{{dn2cpp_isa_bits<float32x4_t>(a1_1), dn2cpp_isa_bits<float32x4_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pf32_t2v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pf32_t3v128f32(float* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst3q_f32(a0, (float32x4x3_t{{dn2cpp_isa_bits<float32x4_t>(a1_1), dn2cpp_isa_bits<float32x4_t>(a1_2), dn2cpp_isa_bits<float32x4_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pf32_t3v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pf32_t4v128f32(float* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst4q_f32(a0, (float32x4x4_t{{dn2cpp_isa_bits<float32x4_t>(a1_1), dn2cpp_isa_bits<float32x4_t>(a1_2), dn2cpp_isa_bits<float32x4_t>(a1_3), dn2cpp_isa_bits<float32x4_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pf32_t4v128f32(float*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pf64_t2v128f64(double* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst2q_f64(a0, (float64x2x2_t{{dn2cpp_isa_bits<float64x2_t>(a1_1), dn2cpp_isa_bits<float64x2_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pf64_t2v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pf64_t3v128f64(double* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst3q_f64(a0, (float64x2x3_t{{dn2cpp_isa_bits<float64x2_t>(a1_1), dn2cpp_isa_bits<float64x2_t>(a1_2), dn2cpp_isa_bits<float64x2_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pf64_t3v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pf64_t4v128f64(double* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst4q_f64(a0, (float64x2x4_t{{dn2cpp_isa_bits<float64x2_t>(a1_1), dn2cpp_isa_bits<float64x2_t>(a1_2), dn2cpp_isa_bits<float64x2_t>(a1_3), dn2cpp_isa_bits<float64x2_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pf64_t4v128f64(double*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi16_t2v128i16(int16_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst2q_s16(a0, (int16x8x2_t{{dn2cpp_isa_bits<int16x8_t>(a1_1), dn2cpp_isa_bits<int16x8_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi16_t2v128i16(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi16_t3v128i16(int16_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst3q_s16(a0, (int16x8x3_t{{dn2cpp_isa_bits<int16x8_t>(a1_1), dn2cpp_isa_bits<int16x8_t>(a1_2), dn2cpp_isa_bits<int16x8_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi16_t3v128i16(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi16_t4v128i16(int16_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst4q_s16(a0, (int16x8x4_t{{dn2cpp_isa_bits<int16x8_t>(a1_1), dn2cpp_isa_bits<int16x8_t>(a1_2), dn2cpp_isa_bits<int16x8_t>(a1_3), dn2cpp_isa_bits<int16x8_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi16_t4v128i16(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi32_t2v128i32(int32_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst2q_s32(a0, (int32x4x2_t{{dn2cpp_isa_bits<int32x4_t>(a1_1), dn2cpp_isa_bits<int32x4_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi32_t2v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi32_t3v128i32(int32_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst3q_s32(a0, (int32x4x3_t{{dn2cpp_isa_bits<int32x4_t>(a1_1), dn2cpp_isa_bits<int32x4_t>(a1_2), dn2cpp_isa_bits<int32x4_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi32_t3v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi32_t4v128i32(int32_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst4q_s32(a0, (int32x4x4_t{{dn2cpp_isa_bits<int32x4_t>(a1_1), dn2cpp_isa_bits<int32x4_t>(a1_2), dn2cpp_isa_bits<int32x4_t>(a1_3), dn2cpp_isa_bits<int32x4_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi32_t4v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi64_t2v128i64(int64_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst2q_s64(a0, (int64x2x2_t{{dn2cpp_isa_bits<int64x2_t>(a1_1), dn2cpp_isa_bits<int64x2_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi64_t2v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi64_t3v128i64(int64_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst3q_s64(a0, (int64x2x3_t{{dn2cpp_isa_bits<int64x2_t>(a1_1), dn2cpp_isa_bits<int64x2_t>(a1_2), dn2cpp_isa_bits<int64x2_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi64_t3v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi64_t4v128i64(int64_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst4q_s64(a0, (int64x2x4_t{{dn2cpp_isa_bits<int64x2_t>(a1_1), dn2cpp_isa_bits<int64x2_t>(a1_2), dn2cpp_isa_bits<int64x2_t>(a1_3), dn2cpp_isa_bits<int64x2_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi64_t4v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi8_t2v128i8(int8_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst2q_s8(a0, (int8x16x2_t{{dn2cpp_isa_bits<int8x16_t>(a1_1), dn2cpp_isa_bits<int8x16_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi8_t2v128i8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi8_t3v128i8(int8_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst3q_s8(a0, (int8x16x3_t{{dn2cpp_isa_bits<int8x16_t>(a1_1), dn2cpp_isa_bits<int8x16_t>(a1_2), dn2cpp_isa_bits<int8x16_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi8_t3v128i8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi8_t4v128i8(int8_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst4q_s8(a0, (int8x16x4_t{{dn2cpp_isa_bits<int8x16_t>(a1_1), dn2cpp_isa_bits<int8x16_t>(a1_2), dn2cpp_isa_bits<int8x16_t>(a1_3), dn2cpp_isa_bits<int8x16_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pi8_t4v128i8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu16_t2v128u16(uint16_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst2q_u16(a0, (uint16x8x2_t{{dn2cpp_isa_bits<uint16x8_t>(a1_1), dn2cpp_isa_bits<uint16x8_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu16_t2v128u16(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu16_t3v128u16(uint16_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst3q_u16(a0, (uint16x8x3_t{{dn2cpp_isa_bits<uint16x8_t>(a1_1), dn2cpp_isa_bits<uint16x8_t>(a1_2), dn2cpp_isa_bits<uint16x8_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu16_t3v128u16(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu16_t4v128u16(uint16_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst4q_u16(a0, (uint16x8x4_t{{dn2cpp_isa_bits<uint16x8_t>(a1_1), dn2cpp_isa_bits<uint16x8_t>(a1_2), dn2cpp_isa_bits<uint16x8_t>(a1_3), dn2cpp_isa_bits<uint16x8_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu16_t4v128u16(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu32_t2v128u32(uint32_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst2q_u32(a0, (uint32x4x2_t{{dn2cpp_isa_bits<uint32x4_t>(a1_1), dn2cpp_isa_bits<uint32x4_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu32_t2v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu32_t3v128u32(uint32_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst3q_u32(a0, (uint32x4x3_t{{dn2cpp_isa_bits<uint32x4_t>(a1_1), dn2cpp_isa_bits<uint32x4_t>(a1_2), dn2cpp_isa_bits<uint32x4_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu32_t3v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu32_t4v128u32(uint32_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst4q_u32(a0, (uint32x4x4_t{{dn2cpp_isa_bits<uint32x4_t>(a1_1), dn2cpp_isa_bits<uint32x4_t>(a1_2), dn2cpp_isa_bits<uint32x4_t>(a1_3), dn2cpp_isa_bits<uint32x4_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu32_t4v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu64_t2v128u64(uint64_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst2q_u64(a0, (uint64x2x2_t{{dn2cpp_isa_bits<uint64x2_t>(a1_1), dn2cpp_isa_bits<uint64x2_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu64_t2v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu64_t3v128u64(uint64_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst3q_u64(a0, (uint64x2x3_t{{dn2cpp_isa_bits<uint64x2_t>(a1_1), dn2cpp_isa_bits<uint64x2_t>(a1_2), dn2cpp_isa_bits<uint64x2_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu64_t3v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu64_t4v128u64(uint64_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst4q_u64(a0, (uint64x2x4_t{{dn2cpp_isa_bits<uint64x2_t>(a1_1), dn2cpp_isa_bits<uint64x2_t>(a1_2), dn2cpp_isa_bits<uint64x2_t>(a1_3), dn2cpp_isa_bits<uint64x2_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu64_t4v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu8_t2v128u8(uint8_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst2q_u8(a0, (uint8x16x2_t{{dn2cpp_isa_bits<uint8x16_t>(a1_1), dn2cpp_isa_bits<uint8x16_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu8_t2v128u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu8_t3v128u8(uint8_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst3q_u8(a0, (uint8x16x3_t{{dn2cpp_isa_bits<uint8x16_t>(a1_1), dn2cpp_isa_bits<uint8x16_t>(a1_2), dn2cpp_isa_bits<uint8x16_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu8_t3v128u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu8_t4v128u8(uint8_t* a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
    vst4q_u8(a0, (uint8x16x4_t{{dn2cpp_isa_bits<uint8x16_t>(a1_1), dn2cpp_isa_bits<uint8x16_t>(a1_2), dn2cpp_isa_bits<uint8x16_t>(a1_3), dn2cpp_isa_bits<uint8x16_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_arm64_storevectorandzip_pu8_t4v128u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.StoreVectorAndZip");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_subtract_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Subtract");
    return dn2cpp_isa_vec<16>(vsubq_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_subtract_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.Subtract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_subtractsaturatescalar_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.SubtractSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqsubh_s16(vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), 0), vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_subtractsaturatescalar_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.SubtractSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_subtractsaturatescalar_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.SubtractSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqsubs_s32(vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), 0), vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_subtractsaturatescalar_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.SubtractSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_subtractsaturatescalar_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.SubtractSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqsubb_s8(vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a0), 0), vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_subtractsaturatescalar_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.SubtractSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_subtractsaturatescalar_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.SubtractSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqsubh_u16(vget_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a0), 0), vget_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_subtractsaturatescalar_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.SubtractSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_subtractsaturatescalar_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.SubtractSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqsubs_u32(vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), 0), vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_subtractsaturatescalar_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.SubtractSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_subtractsaturatescalar_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.SubtractSaturateScalar");
    return dn2cpp_isa_vec<8>(dn2cpp_isa_lane0<8>(vqsubb_u8(vget_lane_u8(dn2cpp_isa_bits<uint8x8_t>(a0), 0), vget_lane_u8(dn2cpp_isa_bits<uint8x8_t>(a1), 0))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_subtractsaturatescalar_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.SubtractSaturateScalar");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
    return dn2cpp_isa_vec<16>(vtrn1q_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
    return dn2cpp_isa_vec<16>(vtrn1q_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
    return dn2cpp_isa_vec<16>(vtrn1q_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
    return dn2cpp_isa_vec<16>(vtrn1q_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
    return dn2cpp_isa_vec<16>(vtrn1q_s64(dn2cpp_isa_bits<int64x2_t>(a0), dn2cpp_isa_bits<int64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
    return dn2cpp_isa_vec<16>(vtrn1q_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
    return dn2cpp_isa_vec<16>(vtrn1q_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
    return dn2cpp_isa_vec<16>(vtrn1q_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
    return dn2cpp_isa_vec<16>(vtrn1q_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
    return dn2cpp_isa_vec<16>(vtrn1q_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
    return dn2cpp_isa_vec<8>(vtrn1_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
    return dn2cpp_isa_vec<8>(vtrn1_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
    return dn2cpp_isa_vec<8>(vtrn1_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
    return dn2cpp_isa_vec<8>(vtrn1_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
    return dn2cpp_isa_vec<8>(vtrn1_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
    return dn2cpp_isa_vec<8>(vtrn1_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
    return dn2cpp_isa_vec<8>(vtrn1_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeeven_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
    return dn2cpp_isa_vec<16>(vtrn2q_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
    return dn2cpp_isa_vec<16>(vtrn2q_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
    return dn2cpp_isa_vec<16>(vtrn2q_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
    return dn2cpp_isa_vec<16>(vtrn2q_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
    return dn2cpp_isa_vec<16>(vtrn2q_s64(dn2cpp_isa_bits<int64x2_t>(a0), dn2cpp_isa_bits<int64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
    return dn2cpp_isa_vec<16>(vtrn2q_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
    return dn2cpp_isa_vec<16>(vtrn2q_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
    return dn2cpp_isa_vec<16>(vtrn2q_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
    return dn2cpp_isa_vec<16>(vtrn2q_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
    return dn2cpp_isa_vec<16>(vtrn2q_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
    return dn2cpp_isa_vec<8>(vtrn2_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
    return dn2cpp_isa_vec<8>(vtrn2_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
    return dn2cpp_isa_vec<8>(vtrn2_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
    return dn2cpp_isa_vec<8>(vtrn2_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
    return dn2cpp_isa_vec<8>(vtrn2_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
    return dn2cpp_isa_vec<8>(vtrn2_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
    return dn2cpp_isa_vec<8>(vtrn2_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_transposeodd_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.TransposeOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
    return dn2cpp_isa_vec<16>(vuzp1q_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
    return dn2cpp_isa_vec<16>(vuzp1q_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
    return dn2cpp_isa_vec<16>(vuzp1q_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
    return dn2cpp_isa_vec<16>(vuzp1q_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
    return dn2cpp_isa_vec<16>(vuzp1q_s64(dn2cpp_isa_bits<int64x2_t>(a0), dn2cpp_isa_bits<int64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
    return dn2cpp_isa_vec<16>(vuzp1q_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
    return dn2cpp_isa_vec<16>(vuzp1q_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
    return dn2cpp_isa_vec<16>(vuzp1q_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
    return dn2cpp_isa_vec<16>(vuzp1q_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
    return dn2cpp_isa_vec<16>(vuzp1q_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
    return dn2cpp_isa_vec<8>(vuzp1_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
    return dn2cpp_isa_vec<8>(vuzp1_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
    return dn2cpp_isa_vec<8>(vuzp1_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
    return dn2cpp_isa_vec<8>(vuzp1_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
    return dn2cpp_isa_vec<8>(vuzp1_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
    return dn2cpp_isa_vec<8>(vuzp1_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
    return dn2cpp_isa_vec<8>(vuzp1_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipeven_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipEven");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
    return dn2cpp_isa_vec<16>(vuzp2q_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
    return dn2cpp_isa_vec<16>(vuzp2q_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
    return dn2cpp_isa_vec<16>(vuzp2q_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
    return dn2cpp_isa_vec<16>(vuzp2q_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
    return dn2cpp_isa_vec<16>(vuzp2q_s64(dn2cpp_isa_bits<int64x2_t>(a0), dn2cpp_isa_bits<int64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
    return dn2cpp_isa_vec<16>(vuzp2q_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
    return dn2cpp_isa_vec<16>(vuzp2q_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
    return dn2cpp_isa_vec<16>(vuzp2q_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
    return dn2cpp_isa_vec<16>(vuzp2q_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
    return dn2cpp_isa_vec<16>(vuzp2q_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
    return dn2cpp_isa_vec<8>(vuzp2_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
    return dn2cpp_isa_vec<8>(vuzp2_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
    return dn2cpp_isa_vec<8>(vuzp2_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
    return dn2cpp_isa_vec<8>(vuzp2_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
    return dn2cpp_isa_vec<8>(vuzp2_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
    return dn2cpp_isa_vec<8>(vuzp2_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
    return dn2cpp_isa_vec<8>(vuzp2_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_unzipodd_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.UnzipOdd");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookup_t2v128i8_v128i8(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookup");
    return dn2cpp_isa_vec<16>(vqtbl2q_s8((int8x16x2_t{{dn2cpp_isa_bits<int8x16_t>(a0_1), dn2cpp_isa_bits<int8x16_t>(a0_2)}}), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookup_t2v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookup");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookup_t2v128u8_v128u8(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookup");
    return dn2cpp_isa_vec<16>(vqtbl2q_u8((uint8x16x2_t{{dn2cpp_isa_bits<uint8x16_t>(a0_1), dn2cpp_isa_bits<uint8x16_t>(a0_2)}}), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookup_t2v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookup");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookup_t3v128i8_v128i8(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookup");
    return dn2cpp_isa_vec<16>(vqtbl3q_s8((int8x16x3_t{{dn2cpp_isa_bits<int8x16_t>(a0_1), dn2cpp_isa_bits<int8x16_t>(a0_2), dn2cpp_isa_bits<int8x16_t>(a0_3)}}), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookup_t3v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookup");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookup_t3v128u8_v128u8(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookup");
    return dn2cpp_isa_vec<16>(vqtbl3q_u8((uint8x16x3_t{{dn2cpp_isa_bits<uint8x16_t>(a0_1), dn2cpp_isa_bits<uint8x16_t>(a0_2), dn2cpp_isa_bits<uint8x16_t>(a0_3)}}), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookup_t3v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookup");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookup_t4v128i8_v128i8(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, const Dn2CppVector128& a0_4, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookup");
    return dn2cpp_isa_vec<16>(vqtbl4q_s8((int8x16x4_t{{dn2cpp_isa_bits<int8x16_t>(a0_1), dn2cpp_isa_bits<int8x16_t>(a0_2), dn2cpp_isa_bits<int8x16_t>(a0_3), dn2cpp_isa_bits<int8x16_t>(a0_4)}}), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookup_t4v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookup");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookup_t4v128u8_v128u8(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, const Dn2CppVector128& a0_4, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookup");
    return dn2cpp_isa_vec<16>(vqtbl4q_u8((uint8x16x4_t{{dn2cpp_isa_bits<uint8x16_t>(a0_1), dn2cpp_isa_bits<uint8x16_t>(a0_2), dn2cpp_isa_bits<uint8x16_t>(a0_3), dn2cpp_isa_bits<uint8x16_t>(a0_4)}}), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookup_t4v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookup");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookup_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookup");
    return dn2cpp_isa_vec<16>(vqtbl1q_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookup_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookup");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookup_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookup");
    return dn2cpp_isa_vec<16>(vqtbl1q_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookup_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookup");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookupextension_v128i8_t2v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookupExtension");
    return dn2cpp_isa_vec<16>(vqtbx2q_s8(dn2cpp_isa_bits<int8x16_t>(a0), (int8x16x2_t{{dn2cpp_isa_bits<int8x16_t>(a1_1), dn2cpp_isa_bits<int8x16_t>(a1_2)}}), dn2cpp_isa_bits<uint8x16_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookupextension_v128i8_t2v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookupExtension");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookupextension_v128i8_t3v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookupExtension");
    return dn2cpp_isa_vec<16>(vqtbx3q_s8(dn2cpp_isa_bits<int8x16_t>(a0), (int8x16x3_t{{dn2cpp_isa_bits<int8x16_t>(a1_1), dn2cpp_isa_bits<int8x16_t>(a1_2), dn2cpp_isa_bits<int8x16_t>(a1_3)}}), dn2cpp_isa_bits<uint8x16_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookupextension_v128i8_t3v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookupExtension");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookupextension_v128i8_t4v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookupExtension");
    return dn2cpp_isa_vec<16>(vqtbx4q_s8(dn2cpp_isa_bits<int8x16_t>(a0), (int8x16x4_t{{dn2cpp_isa_bits<int8x16_t>(a1_1), dn2cpp_isa_bits<int8x16_t>(a1_2), dn2cpp_isa_bits<int8x16_t>(a1_3), dn2cpp_isa_bits<int8x16_t>(a1_4)}}), dn2cpp_isa_bits<uint8x16_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookupextension_v128i8_t4v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookupExtension");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookupextension_v128i8_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookupExtension");
    return dn2cpp_isa_vec<16>(vqtbx1q_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1), dn2cpp_isa_bits<uint8x16_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookupextension_v128i8_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookupExtension");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookupextension_v128u8_t2v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookupExtension");
    return dn2cpp_isa_vec<16>(vqtbx2q_u8(dn2cpp_isa_bits<uint8x16_t>(a0), (uint8x16x2_t{{dn2cpp_isa_bits<uint8x16_t>(a1_1), dn2cpp_isa_bits<uint8x16_t>(a1_2)}}), dn2cpp_isa_bits<uint8x16_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookupextension_v128u8_t2v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookupExtension");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookupextension_v128u8_t3v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookupExtension");
    return dn2cpp_isa_vec<16>(vqtbx3q_u8(dn2cpp_isa_bits<uint8x16_t>(a0), (uint8x16x3_t{{dn2cpp_isa_bits<uint8x16_t>(a1_1), dn2cpp_isa_bits<uint8x16_t>(a1_2), dn2cpp_isa_bits<uint8x16_t>(a1_3)}}), dn2cpp_isa_bits<uint8x16_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookupextension_v128u8_t3v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookupExtension");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookupextension_v128u8_t4v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookupExtension");
    return dn2cpp_isa_vec<16>(vqtbx4q_u8(dn2cpp_isa_bits<uint8x16_t>(a0), (uint8x16x4_t{{dn2cpp_isa_bits<uint8x16_t>(a1_1), dn2cpp_isa_bits<uint8x16_t>(a1_2), dn2cpp_isa_bits<uint8x16_t>(a1_3), dn2cpp_isa_bits<uint8x16_t>(a1_4)}}), dn2cpp_isa_bits<uint8x16_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookupextension_v128u8_t4v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookupExtension");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookupextension_v128u8_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookupExtension");
    return dn2cpp_isa_vec<16>(vqtbx1q_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1), dn2cpp_isa_bits<uint8x16_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_vectortablelookupextension_v128u8_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.VectorTableLookupExtension");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
    return dn2cpp_isa_vec<16>(vzip2q_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
    return dn2cpp_isa_vec<16>(vzip2q_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
    return dn2cpp_isa_vec<16>(vzip2q_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
    return dn2cpp_isa_vec<16>(vzip2q_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
    return dn2cpp_isa_vec<16>(vzip2q_s64(dn2cpp_isa_bits<int64x2_t>(a0), dn2cpp_isa_bits<int64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
    return dn2cpp_isa_vec<16>(vzip2q_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
    return dn2cpp_isa_vec<16>(vzip2q_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
    return dn2cpp_isa_vec<16>(vzip2q_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
    return dn2cpp_isa_vec<16>(vzip2q_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
    return dn2cpp_isa_vec<16>(vzip2q_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
    return dn2cpp_isa_vec<8>(vzip2_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
    return dn2cpp_isa_vec<8>(vzip2_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
    return dn2cpp_isa_vec<8>(vzip2_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
    return dn2cpp_isa_vec<8>(vzip2_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
    return dn2cpp_isa_vec<8>(vzip2_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
    return dn2cpp_isa_vec<8>(vzip2_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
    return dn2cpp_isa_vec<8>(vzip2_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziphigh_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipHigh");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
    return dn2cpp_isa_vec<16>(vzip1q_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
    return dn2cpp_isa_vec<16>(vzip1q_f64(dn2cpp_isa_bits<float64x2_t>(a0), dn2cpp_isa_bits<float64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
    return dn2cpp_isa_vec<16>(vzip1q_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
    return dn2cpp_isa_vec<16>(vzip1q_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
    return dn2cpp_isa_vec<16>(vzip1q_s64(dn2cpp_isa_bits<int64x2_t>(a0), dn2cpp_isa_bits<int64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
    return dn2cpp_isa_vec<16>(vzip1q_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
    return dn2cpp_isa_vec<16>(vzip1q_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
    return dn2cpp_isa_vec<16>(vzip1q_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
    return dn2cpp_isa_vec<16>(vzip1q_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
    return dn2cpp_isa_vec<16>(vzip1q_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_arm64_ziplow_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziplow_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
    return dn2cpp_isa_vec<8>(vzip1_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziplow_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziplow_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
    return dn2cpp_isa_vec<8>(vzip1_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziplow_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziplow_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
    return dn2cpp_isa_vec<8>(vzip1_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziplow_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziplow_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
    return dn2cpp_isa_vec<8>(vzip1_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziplow_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziplow_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
    return dn2cpp_isa_vec<8>(vzip1_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziplow_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziplow_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
    return dn2cpp_isa_vec<8>(vzip1_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziplow_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziplow_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd_Arm64, "System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
    return dn2cpp_isa_vec<8>(vzip1_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_arm64_ziplow_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd+Arm64.ZipLow");
}
#endif
