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
