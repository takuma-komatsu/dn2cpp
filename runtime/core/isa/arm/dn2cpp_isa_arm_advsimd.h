#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.Arm.AdvSimd: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_abs_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Abs");
    return dn2cpp_isa_vec<16>(vabsq_f32(dn2cpp_isa_bits<float32x4_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_abs_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Abs");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_abs_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Abs");
    return dn2cpp_isa_vec<16>(vabsq_s16(dn2cpp_isa_bits<int16x8_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_abs_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Abs");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_abs_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Abs");
    return dn2cpp_isa_vec<16>(vabsq_s32(dn2cpp_isa_bits<int32x4_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_abs_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Abs");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_abs_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Abs");
    return dn2cpp_isa_vec<16>(vabsq_s8(dn2cpp_isa_bits<int8x16_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_abs_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Abs");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_abs_v64f32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Abs");
    return dn2cpp_isa_vec<8>(vabs_f32(dn2cpp_isa_bits<float32x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_abs_v64f32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Abs");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_abs_v64i16(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Abs");
    return dn2cpp_isa_vec<8>(vabs_s16(dn2cpp_isa_bits<int16x4_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_abs_v64i16(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Abs");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_abs_v64i32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Abs");
    return dn2cpp_isa_vec<8>(vabs_s32(dn2cpp_isa_bits<int32x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_abs_v64i32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Abs");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_abs_v64i8(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Abs");
    return dn2cpp_isa_vec<8>(vabs_s8(dn2cpp_isa_bits<int8x8_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_abs_v64i8(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Abs");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_add_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Add");
    return dn2cpp_isa_vec<16>(vaddq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_add_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Add");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_add_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Add");
    return dn2cpp_isa_vec<16>(vaddq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_add_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Add");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_add_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Add");
    return dn2cpp_isa_vec<16>(vaddq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_add_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Add");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_add_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Add");
    return dn2cpp_isa_vec<16>(vaddq_s64(dn2cpp_isa_bits<int64x2_t>(a0), dn2cpp_isa_bits<int64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_add_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Add");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_add_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Add");
    return dn2cpp_isa_vec<16>(vaddq_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_add_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Add");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_add_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Add");
    return dn2cpp_isa_vec<16>(vaddq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_add_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Add");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_add_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Add");
    return dn2cpp_isa_vec<16>(vaddq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_add_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Add");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_add_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Add");
    return dn2cpp_isa_vec<16>(vaddq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_add_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Add");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_add_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Add");
    return dn2cpp_isa_vec<16>(vaddq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_add_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Add");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_add_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Add");
    return dn2cpp_isa_vec<8>(vadd_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_add_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Add");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_add_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Add");
    return dn2cpp_isa_vec<8>(vadd_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_add_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Add");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_add_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Add");
    return dn2cpp_isa_vec<8>(vadd_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_add_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Add");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_add_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Add");
    return dn2cpp_isa_vec<8>(vadd_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_add_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Add");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_add_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Add");
    return dn2cpp_isa_vec<8>(vadd_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_add_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Add");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_add_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Add");
    return dn2cpp_isa_vec<8>(vadd_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_add_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Add");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_add_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Add");
    return dn2cpp_isa_vec<8>(vadd_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_add_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Add");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_addsaturate_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
    return dn2cpp_isa_vec<16>(vqaddq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_addsaturate_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_addsaturate_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
    return dn2cpp_isa_vec<16>(vqaddq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_addsaturate_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_addsaturate_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
    return dn2cpp_isa_vec<16>(vqaddq_s64(dn2cpp_isa_bits<int64x2_t>(a0), dn2cpp_isa_bits<int64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_addsaturate_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_addsaturate_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
    return dn2cpp_isa_vec<16>(vqaddq_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_addsaturate_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_addsaturate_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
    return dn2cpp_isa_vec<16>(vqaddq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_addsaturate_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_addsaturate_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
    return dn2cpp_isa_vec<16>(vqaddq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_addsaturate_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_addsaturate_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
    return dn2cpp_isa_vec<16>(vqaddq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_addsaturate_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_addsaturate_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
    return dn2cpp_isa_vec<16>(vqaddq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_addsaturate_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_addsaturate_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
    return dn2cpp_isa_vec<8>(vqadd_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_addsaturate_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_addsaturate_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
    return dn2cpp_isa_vec<8>(vqadd_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_addsaturate_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_addsaturate_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
    return dn2cpp_isa_vec<8>(vqadd_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_addsaturate_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_addsaturate_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
    return dn2cpp_isa_vec<8>(vqadd_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_addsaturate_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_addsaturate_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
    return dn2cpp_isa_vec<8>(vqadd_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_addsaturate_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_addsaturate_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
    return dn2cpp_isa_vec<8>(vqadd_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_addsaturate_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.AddSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<16>(vandq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<16>(vandq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<16>(vandq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<16>(vandq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<16>(vandq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<16>(vandq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<16>(vandq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<16>(vandq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<16>(vandq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<16>(vandq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_and_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<8>(vand_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<8>(vand_u64(dn2cpp_isa_bits<uint64x1_t>(a0), dn2cpp_isa_bits<uint64x1_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<8>(vand_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<8>(vand_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64i64_v64i64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<8>(vand_u64(dn2cpp_isa_bits<uint64x1_t>(a0), dn2cpp_isa_bits<uint64x1_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64i64_v64i64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<8>(vand_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<8>(vand_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<8>(vand_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64u64_v64u64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<8>(vand_u64(dn2cpp_isa_bits<uint64x1_t>(a0), dn2cpp_isa_bits<uint64x1_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64u64_v64u64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.And");
    return dn2cpp_isa_vec<8>(vand_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_and_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.And");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<16>(vbicq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<16>(vbicq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<16>(vbicq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<16>(vbicq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<16>(vbicq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<16>(vbicq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<16>(vbicq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<16>(vbicq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<16>(vbicq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<16>(vbicq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseclear_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<8>(vbic_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<8>(vbic_u64(dn2cpp_isa_bits<uint64x1_t>(a0), dn2cpp_isa_bits<uint64x1_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<8>(vbic_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<8>(vbic_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64i64_v64i64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<8>(vbic_u64(dn2cpp_isa_bits<uint64x1_t>(a0), dn2cpp_isa_bits<uint64x1_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64i64_v64i64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<8>(vbic_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<8>(vbic_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<8>(vbic_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64u64_v64u64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<8>(vbic_u64(dn2cpp_isa_bits<uint64x1_t>(a0), dn2cpp_isa_bits<uint64x1_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64u64_v64u64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
    return dn2cpp_isa_vec<8>(vbic_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseclear_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseClear");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(vbslq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1), dn2cpp_isa_bits<uint32x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(vbslq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1), dn2cpp_isa_bits<uint64x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128i16_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(vbslq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1), dn2cpp_isa_bits<uint16x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128i16_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128i32_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(vbslq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1), dn2cpp_isa_bits<uint32x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128i32_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128i64_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(vbslq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1), dn2cpp_isa_bits<uint64x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128i64_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128i8_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(vbslq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1), dn2cpp_isa_bits<uint8x16_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128i8_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128u16_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(vbslq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1), dn2cpp_isa_bits<uint16x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128u16_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128u32_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(vbslq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1), dn2cpp_isa_bits<uint32x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128u32_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128u64_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(vbslq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1), dn2cpp_isa_bits<uint64x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128u64_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128u8_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(vbslq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1), dn2cpp_isa_bits<uint8x16_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_bitwiseselect_v128u8_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64f32_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<8>(vbsl_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1), dn2cpp_isa_bits<uint32x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64f32_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64f64_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<8>(vbsl_u64(dn2cpp_isa_bits<uint64x1_t>(a0), dn2cpp_isa_bits<uint64x1_t>(a1), dn2cpp_isa_bits<uint64x1_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64f64_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64i16_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<8>(vbsl_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1), dn2cpp_isa_bits<uint16x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64i16_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64i32_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<8>(vbsl_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1), dn2cpp_isa_bits<uint32x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64i32_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64i64_v64i64_v64i64(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<8>(vbsl_u64(dn2cpp_isa_bits<uint64x1_t>(a0), dn2cpp_isa_bits<uint64x1_t>(a1), dn2cpp_isa_bits<uint64x1_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64i64_v64i64_v64i64(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64i8_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<8>(vbsl_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1), dn2cpp_isa_bits<uint8x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64i8_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64u16_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<8>(vbsl_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1), dn2cpp_isa_bits<uint16x4_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64u16_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64u32_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<8>(vbsl_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1), dn2cpp_isa_bits<uint32x2_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64u32_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64u64_v64u64_v64u64(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<8>(vbsl_u64(dn2cpp_isa_bits<uint64x1_t>(a0), dn2cpp_isa_bits<uint64x1_t>(a1), dn2cpp_isa_bits<uint64x1_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64u64_v64u64_v64u64(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64u8_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
    return dn2cpp_isa_vec<8>(vbsl_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1), dn2cpp_isa_bits<uint8x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_bitwiseselect_v64u8_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_compareequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(vceqq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_compareequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_compareequal_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(vceqq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_compareequal_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_compareequal_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(vceqq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_compareequal_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_compareequal_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(vceqq_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_compareequal_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_compareequal_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(vceqq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_compareequal_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_compareequal_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(vceqq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_compareequal_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_compareequal_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(vceqq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_compareequal_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_compareequal_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
    return dn2cpp_isa_vec<8>(vceq_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_compareequal_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_compareequal_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
    return dn2cpp_isa_vec<8>(vceq_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_compareequal_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_compareequal_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
    return dn2cpp_isa_vec<8>(vceq_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_compareequal_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_compareequal_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
    return dn2cpp_isa_vec<8>(vceq_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_compareequal_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_compareequal_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
    return dn2cpp_isa_vec<8>(vceq_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_compareequal_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_compareequal_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
    return dn2cpp_isa_vec<8>(vceq_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_compareequal_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_compareequal_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
    return dn2cpp_isa_vec<8>(vceq_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_compareequal_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(vcgtq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthan_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(vcgtq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthan_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthan_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(vcgtq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthan_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthan_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(vcgtq_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthan_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthan_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(vcgtq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthan_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthan_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(vcgtq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthan_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthan_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(vcgtq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthan_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthan_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<8>(vcgt_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthan_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthan_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<8>(vcgt_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthan_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthan_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<8>(vcgt_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthan_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthan_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<8>(vcgt_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthan_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthan_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<8>(vcgt_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthan_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthan_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<8>(vcgt_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthan_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthan_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<8>(vcgt_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthan_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(vcgeq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(vcgeq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(vcgeq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(vcgeq_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(vcgeq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(vcgeq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(vcgeq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<8>(vcge_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<8>(vcge_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<8>(vcge_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<8>(vcge_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<8>(vcge_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<8>(vcge_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<8>(vcge_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparegreaterthanorequal_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(vcltq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthan_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(vcltq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthan_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthan_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(vcltq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthan_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthan_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(vcltq_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthan_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthan_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(vcltq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthan_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthan_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(vcltq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthan_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthan_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(vcltq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthan_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthan_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
    return dn2cpp_isa_vec<8>(vclt_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthan_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthan_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
    return dn2cpp_isa_vec<8>(vclt_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthan_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthan_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
    return dn2cpp_isa_vec<8>(vclt_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthan_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthan_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
    return dn2cpp_isa_vec<8>(vclt_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthan_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthan_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
    return dn2cpp_isa_vec<8>(vclt_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthan_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthan_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
    return dn2cpp_isa_vec<8>(vclt_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthan_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthan_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
    return dn2cpp_isa_vec<8>(vclt_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthan_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(vcleq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(vcleq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(vcleq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(vcleq_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(vcleq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(vcleq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(vcleq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<8>(vcle_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<8>(vcle_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<8>(vcle_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<8>(vcle_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<8>(vcle_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<8>(vcle_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<8>(vcle_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparelessthanorequal_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparetest_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
    return dn2cpp_isa_vec<16>(vtstq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparetest_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparetest_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
    return dn2cpp_isa_vec<16>(vtstq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparetest_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparetest_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
    return dn2cpp_isa_vec<16>(vtstq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparetest_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparetest_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
    return dn2cpp_isa_vec<16>(vtstq_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparetest_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparetest_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
    return dn2cpp_isa_vec<16>(vtstq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparetest_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparetest_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
    return dn2cpp_isa_vec<16>(vtstq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparetest_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparetest_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
    return dn2cpp_isa_vec<16>(vtstq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_comparetest_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparetest_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
    return dn2cpp_isa_vec<8>(vtst_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparetest_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparetest_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
    return dn2cpp_isa_vec<8>(vtst_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparetest_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparetest_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
    return dn2cpp_isa_vec<8>(vtst_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparetest_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparetest_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
    return dn2cpp_isa_vec<8>(vtst_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparetest_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparetest_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
    return dn2cpp_isa_vec<8>(vtst_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparetest_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparetest_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
    return dn2cpp_isa_vec<8>(vtst_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparetest_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparetest_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
    return dn2cpp_isa_vec<8>(vtst_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_comparetest_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.CompareTest");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<16>(vdupq_laneq_f32(dn2cpp_isa_bits<float32x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v128i16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<16>(vdupq_laneq_s16(dn2cpp_isa_bits<int16x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v128i16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v128i32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<16>(vdupq_laneq_s32(dn2cpp_isa_bits<int32x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v128i32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v128i8_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<16>(vdupq_laneq_s8(dn2cpp_isa_bits<int8x16_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v128i8_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v128u16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<16>(vdupq_laneq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v128u16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v128u32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<16>(vdupq_laneq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v128u32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v128u8_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<16>(vdupq_laneq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v128u8_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v64f32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_vec<16>(vdupq_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v64f32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v64i16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<16>(vdupq_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v64i16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v64i32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_vec<16>(vdupq_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v64i32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v64i8_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<16>(vdupq_lane_s8(dn2cpp_isa_bits<int8x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v64i8_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v64u16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<16>(vdupq_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v64u16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v64u32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_vec<16>(vdupq_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v64u32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v64u8_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<16>(vdupq_lane_u8(dn2cpp_isa_bits<uint8x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector128_v64u8_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<8>(vdup_laneq_f32(dn2cpp_isa_bits<float32x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v128i16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<8>(vdup_laneq_s16(dn2cpp_isa_bits<int16x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v128i16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v128i32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<8>(vdup_laneq_s32(dn2cpp_isa_bits<int32x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v128i32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v128i8_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<8>(vdup_laneq_s8(dn2cpp_isa_bits<int8x16_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v128i8_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v128u16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<8>(vdup_laneq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v128u16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v128u32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<8>(vdup_laneq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v128u32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v128u8_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<8>(vdup_laneq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v128u8_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v64f32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_vec<8>(vdup_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v64f32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v64i16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<8>(vdup_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v64i16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v64i32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_vec<8>(vdup_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v64i32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v64i8_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<8>(vdup_lane_s8(dn2cpp_isa_bits<int8x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v64i8_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v64u16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<8>(vdup_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v64u16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v64u32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_vec<8>(vdup_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v64u32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v64u8_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<8>(vdup_lane_u8(dn2cpp_isa_bits<uint8x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_duplicateselectedscalartovector64_v64u8_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.DuplicateSelectedScalarToVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE float dn2cpp_isa_arm_advsimd_extract_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, vgetq_lane_f32(dn2cpp_isa_bits<float32x4_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE float dn2cpp_isa_arm_advsimd_extract_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE double dn2cpp_isa_arm_advsimd_extract_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, vgetq_lane_f64(dn2cpp_isa_bits<float64x2_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE double dn2cpp_isa_arm_advsimd_extract_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE int16_t dn2cpp_isa_arm_advsimd_extract_v128i16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, vgetq_lane_s16(dn2cpp_isa_bits<int16x8_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int16_t dn2cpp_isa_arm_advsimd_extract_v128i16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_arm_advsimd_extract_v128i32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, vgetq_lane_s32(dn2cpp_isa_bits<int32x4_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_arm_advsimd_extract_v128i32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE int64_t dn2cpp_isa_arm_advsimd_extract_v128i64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, vgetq_lane_s64(dn2cpp_isa_bits<int64x2_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int64_t dn2cpp_isa_arm_advsimd_extract_v128i64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE int8_t dn2cpp_isa_arm_advsimd_extract_v128i8_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, vgetq_lane_s8(dn2cpp_isa_bits<int8x16_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int8_t dn2cpp_isa_arm_advsimd_extract_v128i8_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE uint16_t dn2cpp_isa_arm_advsimd_extract_v128u16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, vgetq_lane_u16(dn2cpp_isa_bits<uint16x8_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint16_t dn2cpp_isa_arm_advsimd_extract_v128u16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_arm_advsimd_extract_v128u32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, vgetq_lane_u32(dn2cpp_isa_bits<uint32x4_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_arm_advsimd_extract_v128u32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_arm_advsimd_extract_v128u64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, vgetq_lane_u64(dn2cpp_isa_bits<uint64x2_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_arm_advsimd_extract_v128u64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE uint8_t dn2cpp_isa_arm_advsimd_extract_v128u8_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, vgetq_lane_u8(dn2cpp_isa_bits<uint8x16_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint8_t dn2cpp_isa_arm_advsimd_extract_v128u8_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE float dn2cpp_isa_arm_advsimd_extract_v64f32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, vget_lane_f32(dn2cpp_isa_bits<float32x2_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE float dn2cpp_isa_arm_advsimd_extract_v64f32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE int16_t dn2cpp_isa_arm_advsimd_extract_v64i16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, vget_lane_s16(dn2cpp_isa_bits<int16x4_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int16_t dn2cpp_isa_arm_advsimd_extract_v64i16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_arm_advsimd_extract_v64i32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, vget_lane_s32(dn2cpp_isa_bits<int32x2_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_arm_advsimd_extract_v64i32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE int8_t dn2cpp_isa_arm_advsimd_extract_v64i8_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, vget_lane_s8(dn2cpp_isa_bits<int8x8_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int8_t dn2cpp_isa_arm_advsimd_extract_v64i8_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE uint16_t dn2cpp_isa_arm_advsimd_extract_v64u16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, vget_lane_u16(dn2cpp_isa_bits<uint16x4_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint16_t dn2cpp_isa_arm_advsimd_extract_v64u16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_arm_advsimd_extract_v64u32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, vget_lane_u32(dn2cpp_isa_bits<uint32x2_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_arm_advsimd_extract_v64u32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE uint8_t dn2cpp_isa_arm_advsimd_extract_v64u8_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, vget_lane_u8(dn2cpp_isa_bits<uint8x8_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint8_t dn2cpp_isa_arm_advsimd_extract_v64u8_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Extract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128f32_u8_f32(const Dn2CppVector128& a0, uint8_t a1, float a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<16>(vsetq_lane_f32(a2, dn2cpp_isa_bits<float32x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128f32_u8_f32(const Dn2CppVector128&, uint8_t, float)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128f64_u8_f64(const Dn2CppVector128& a0, uint8_t a1, double a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_vec<16>(vsetq_lane_f64(a2, dn2cpp_isa_bits<float64x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128f64_u8_f64(const Dn2CppVector128&, uint8_t, double)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128i16_u8_i16(const Dn2CppVector128& a0, uint8_t a1, int16_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<16>(vsetq_lane_s16(a2, dn2cpp_isa_bits<int16x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128i16_u8_i16(const Dn2CppVector128&, uint8_t, int16_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128i32_u8_i32(const Dn2CppVector128& a0, uint8_t a1, int32_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<16>(vsetq_lane_s32(a2, dn2cpp_isa_bits<int32x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128i32_u8_i32(const Dn2CppVector128&, uint8_t, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128i64_u8_i64(const Dn2CppVector128& a0, uint8_t a1, int64_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_vec<16>(vsetq_lane_s64(a2, dn2cpp_isa_bits<int64x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128i64_u8_i64(const Dn2CppVector128&, uint8_t, int64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128i8_u8_i8(const Dn2CppVector128& a0, uint8_t a1, int8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<16>(vsetq_lane_s8(a2, dn2cpp_isa_bits<int8x16_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128i8_u8_i8(const Dn2CppVector128&, uint8_t, int8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128u16_u8_u16(const Dn2CppVector128& a0, uint8_t a1, uint16_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<16>(vsetq_lane_u16(a2, dn2cpp_isa_bits<uint16x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128u16_u8_u16(const Dn2CppVector128&, uint8_t, uint16_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128u32_u8_u32(const Dn2CppVector128& a0, uint8_t a1, uint32_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<16>(vsetq_lane_u32(a2, dn2cpp_isa_bits<uint32x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128u32_u8_u32(const Dn2CppVector128&, uint8_t, uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128u64_u8_u64(const Dn2CppVector128& a0, uint8_t a1, uint64_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_vec<16>(vsetq_lane_u64(a2, dn2cpp_isa_bits<uint64x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128u64_u8_u64(const Dn2CppVector128&, uint8_t, uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128u8_u8_u8(const Dn2CppVector128& a0, uint8_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<16>(vsetq_lane_u8(a2, dn2cpp_isa_bits<uint8x16_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_insert_v128u8_u8_u8(const Dn2CppVector128&, uint8_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_insert_v64f32_u8_f32(const Dn2CppVector64& a0, uint8_t a1, float a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_vec<8>(vset_lane_f32(a2, dn2cpp_isa_bits<float32x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_insert_v64f32_u8_f32(const Dn2CppVector64&, uint8_t, float)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_insert_v64i16_u8_i16(const Dn2CppVector64& a0, uint8_t a1, int16_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<8>(vset_lane_s16(a2, dn2cpp_isa_bits<int16x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_insert_v64i16_u8_i16(const Dn2CppVector64&, uint8_t, int16_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_insert_v64i32_u8_i32(const Dn2CppVector64& a0, uint8_t a1, int32_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_vec<8>(vset_lane_s32(a2, dn2cpp_isa_bits<int32x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_insert_v64i32_u8_i32(const Dn2CppVector64&, uint8_t, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_insert_v64i8_u8_i8(const Dn2CppVector64& a0, uint8_t a1, int8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<8>(vset_lane_s8(a2, dn2cpp_isa_bits<int8x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_insert_v64i8_u8_i8(const Dn2CppVector64&, uint8_t, int8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_insert_v64u16_u8_u16(const Dn2CppVector64& a0, uint8_t a1, uint16_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<8>(vset_lane_u16(a2, dn2cpp_isa_bits<uint16x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_insert_v64u16_u8_u16(const Dn2CppVector64&, uint8_t, uint16_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_insert_v64u32_u8_u32(const Dn2CppVector64& a0, uint8_t a1, uint32_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_vec<8>(vset_lane_u32(a2, dn2cpp_isa_bits<uint32x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_insert_v64u32_u8_u32(const Dn2CppVector64&, uint8_t, uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_insert_v64u8_u8_u8(const Dn2CppVector64& a0, uint8_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<8>(vset_lane_u8(a2, dn2cpp_isa_bits<uint8x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_insert_v64u8_u8_u8(const Dn2CppVector64&, uint8_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Insert");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load2xvector64_pf32(float* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load2xVector64");
    dn2cpp_isa_scatter(vld1_f32_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load2xvector64_pf32(float*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load2xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load2xvector64_pi16(int16_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load2xVector64");
    dn2cpp_isa_scatter(vld1_s16_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load2xvector64_pi16(int16_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load2xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load2xvector64_pi32(int32_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load2xVector64");
    dn2cpp_isa_scatter(vld1_s32_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load2xvector64_pi32(int32_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load2xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load2xvector64_pi8(int8_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load2xVector64");
    dn2cpp_isa_scatter(vld1_s8_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load2xvector64_pi8(int8_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load2xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load2xvector64_pu16(uint16_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load2xVector64");
    dn2cpp_isa_scatter(vld1_u16_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load2xvector64_pu16(uint16_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load2xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load2xvector64_pu32(uint32_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load2xVector64");
    dn2cpp_isa_scatter(vld1_u32_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load2xvector64_pu32(uint32_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load2xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load2xvector64_pu8(uint8_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load2xVector64");
    dn2cpp_isa_scatter(vld1_u8_x2(a0), item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load2xvector64_pu8(uint8_t*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load2xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load3xvector64_pf32(float* a0, Dn2CppVector64* item1, Dn2CppVector64* item2, Dn2CppVector64* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load3xVector64");
    dn2cpp_isa_scatter(vld1_f32_x3(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load3xvector64_pf32(float*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load3xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load3xvector64_pi16(int16_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2, Dn2CppVector64* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load3xVector64");
    dn2cpp_isa_scatter(vld1_s16_x3(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load3xvector64_pi16(int16_t*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load3xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load3xvector64_pi32(int32_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2, Dn2CppVector64* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load3xVector64");
    dn2cpp_isa_scatter(vld1_s32_x3(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load3xvector64_pi32(int32_t*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load3xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load3xvector64_pi8(int8_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2, Dn2CppVector64* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load3xVector64");
    dn2cpp_isa_scatter(vld1_s8_x3(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load3xvector64_pi8(int8_t*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load3xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load3xvector64_pu16(uint16_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2, Dn2CppVector64* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load3xVector64");
    dn2cpp_isa_scatter(vld1_u16_x3(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load3xvector64_pu16(uint16_t*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load3xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load3xvector64_pu32(uint32_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2, Dn2CppVector64* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load3xVector64");
    dn2cpp_isa_scatter(vld1_u32_x3(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load3xvector64_pu32(uint32_t*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load3xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load3xvector64_pu8(uint8_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2, Dn2CppVector64* item3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load3xVector64");
    dn2cpp_isa_scatter(vld1_u8_x3(a0), item1, item2, item3);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load3xvector64_pu8(uint8_t*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load3xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load4xvector64_pf32(float* a0, Dn2CppVector64* item1, Dn2CppVector64* item2, Dn2CppVector64* item3, Dn2CppVector64* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load4xVector64");
    dn2cpp_isa_scatter(vld1_f32_x4(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load4xvector64_pf32(float*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load4xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load4xvector64_pi16(int16_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2, Dn2CppVector64* item3, Dn2CppVector64* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load4xVector64");
    dn2cpp_isa_scatter(vld1_s16_x4(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load4xvector64_pi16(int16_t*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load4xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load4xvector64_pi32(int32_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2, Dn2CppVector64* item3, Dn2CppVector64* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load4xVector64");
    dn2cpp_isa_scatter(vld1_s32_x4(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load4xvector64_pi32(int32_t*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load4xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load4xvector64_pi8(int8_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2, Dn2CppVector64* item3, Dn2CppVector64* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load4xVector64");
    dn2cpp_isa_scatter(vld1_s8_x4(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load4xvector64_pi8(int8_t*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load4xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load4xvector64_pu16(uint16_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2, Dn2CppVector64* item3, Dn2CppVector64* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load4xVector64");
    dn2cpp_isa_scatter(vld1_u16_x4(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load4xvector64_pu16(uint16_t*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load4xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load4xvector64_pu32(uint32_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2, Dn2CppVector64* item3, Dn2CppVector64* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load4xVector64");
    dn2cpp_isa_scatter(vld1_u32_x4(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load4xvector64_pu32(uint32_t*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load4xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load4xvector64_pu8(uint8_t* a0, Dn2CppVector64* item1, Dn2CppVector64* item2, Dn2CppVector64* item3, Dn2CppVector64* item4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Load4xVector64");
    dn2cpp_isa_scatter(vld1_u8_x4(a0), item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_load4xvector64_pu8(uint8_t*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*, Dn2CppVector64*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Load4xVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pf32(float* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(vld1q_f32(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pf32(float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(vld1q_f64(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(vld1q_s16(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(vld1q_s32(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(vld1q_s64(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(vld1q_s8(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(vld1q_u16(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(vld1q_u32(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(vld1q_u64(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(vld1q_u8(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_loadvector128_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pf32(float* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
    return dn2cpp_isa_vec<8>(vld1_f32(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pf32(float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
    return dn2cpp_isa_vec<8>(vld1_f64(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
    return dn2cpp_isa_vec<8>(vld1_s16(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
    return dn2cpp_isa_vec<8>(vld1_s32(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
    return dn2cpp_isa_vec<8>(vld1_s64(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
    return dn2cpp_isa_vec<8>(vld1_s8(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
    return dn2cpp_isa_vec<8>(vld1_u16(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
    return dn2cpp_isa_vec<8>(vld1_u32(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
    return dn2cpp_isa_vec<8>(vld1_u64(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
    return dn2cpp_isa_vec<8>(vld1_u8(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_loadvector64_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.LoadVector64");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_max_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Max");
    return dn2cpp_isa_vec<16>(vmaxq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_max_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Max");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_max_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Max");
    return dn2cpp_isa_vec<16>(vmaxq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_max_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Max");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_max_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Max");
    return dn2cpp_isa_vec<16>(vmaxq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_max_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Max");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_max_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Max");
    return dn2cpp_isa_vec<16>(vmaxq_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_max_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Max");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_max_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Max");
    return dn2cpp_isa_vec<16>(vmaxq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_max_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Max");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_max_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Max");
    return dn2cpp_isa_vec<16>(vmaxq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_max_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Max");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_max_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Max");
    return dn2cpp_isa_vec<16>(vmaxq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_max_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Max");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_max_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Max");
    return dn2cpp_isa_vec<8>(vmax_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_max_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Max");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_max_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Max");
    return dn2cpp_isa_vec<8>(vmax_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_max_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Max");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_max_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Max");
    return dn2cpp_isa_vec<8>(vmax_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_max_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Max");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_max_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Max");
    return dn2cpp_isa_vec<8>(vmax_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_max_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Max");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_max_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Max");
    return dn2cpp_isa_vec<8>(vmax_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_max_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Max");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_max_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Max");
    return dn2cpp_isa_vec<8>(vmax_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_max_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Max");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_max_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Max");
    return dn2cpp_isa_vec<8>(vmax_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_max_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Max");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_min_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Min");
    return dn2cpp_isa_vec<16>(vminq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_min_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Min");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_min_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Min");
    return dn2cpp_isa_vec<16>(vminq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_min_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Min");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_min_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Min");
    return dn2cpp_isa_vec<16>(vminq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_min_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Min");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_min_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Min");
    return dn2cpp_isa_vec<16>(vminq_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_min_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Min");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_min_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Min");
    return dn2cpp_isa_vec<16>(vminq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_min_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Min");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_min_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Min");
    return dn2cpp_isa_vec<16>(vminq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_min_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Min");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_min_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Min");
    return dn2cpp_isa_vec<16>(vminq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_min_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Min");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_min_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Min");
    return dn2cpp_isa_vec<8>(vmin_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_min_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Min");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_min_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Min");
    return dn2cpp_isa_vec<8>(vmin_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_min_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Min");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_min_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Min");
    return dn2cpp_isa_vec<8>(vmin_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_min_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Min");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_min_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Min");
    return dn2cpp_isa_vec<8>(vmin_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_min_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Min");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_min_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Min");
    return dn2cpp_isa_vec<8>(vmin_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_min_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Min");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_min_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Min");
    return dn2cpp_isa_vec<8>(vmin_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_min_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Min");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_min_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Min");
    return dn2cpp_isa_vec<8>(vmin_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_min_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Min");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_multiply_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
    return dn2cpp_isa_vec<16>(vmulq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_multiply_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_multiply_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
    return dn2cpp_isa_vec<16>(vmulq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_multiply_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_multiply_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
    return dn2cpp_isa_vec<16>(vmulq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_multiply_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_multiply_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
    return dn2cpp_isa_vec<16>(vmulq_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_multiply_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_multiply_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
    return dn2cpp_isa_vec<16>(vmulq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_multiply_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_multiply_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
    return dn2cpp_isa_vec<16>(vmulq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_multiply_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_multiply_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
    return dn2cpp_isa_vec<16>(vmulq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_multiply_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_multiply_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
    return dn2cpp_isa_vec<8>(vmul_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_multiply_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_multiply_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
    return dn2cpp_isa_vec<8>(vmul_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_multiply_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_multiply_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
    return dn2cpp_isa_vec<8>(vmul_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_multiply_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_multiply_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
    return dn2cpp_isa_vec<8>(vmul_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_multiply_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_multiply_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
    return dn2cpp_isa_vec<8>(vmul_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_multiply_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_multiply_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
    return dn2cpp_isa_vec<8>(vmul_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_multiply_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_multiply_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
    return dn2cpp_isa_vec<8>(vmul_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_multiply_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_negate_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Negate");
    return dn2cpp_isa_vec<16>(vnegq_f32(dn2cpp_isa_bits<float32x4_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_negate_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Negate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_negate_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Negate");
    return dn2cpp_isa_vec<16>(vnegq_s16(dn2cpp_isa_bits<int16x8_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_negate_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Negate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_negate_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Negate");
    return dn2cpp_isa_vec<16>(vnegq_s32(dn2cpp_isa_bits<int32x4_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_negate_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Negate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_negate_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Negate");
    return dn2cpp_isa_vec<16>(vnegq_s8(dn2cpp_isa_bits<int8x16_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_negate_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Negate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_negate_v64f32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Negate");
    return dn2cpp_isa_vec<8>(vneg_f32(dn2cpp_isa_bits<float32x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_negate_v64f32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Negate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_negate_v64i16(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Negate");
    return dn2cpp_isa_vec<8>(vneg_s16(dn2cpp_isa_bits<int16x4_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_negate_v64i16(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Negate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_negate_v64i32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Negate");
    return dn2cpp_isa_vec<8>(vneg_s32(dn2cpp_isa_bits<int32x2_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_negate_v64i32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Negate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_negate_v64i8(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Negate");
    return dn2cpp_isa_vec<8>(vneg_s8(dn2cpp_isa_bits<int8x8_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_negate_v64i8(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Negate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<16>(vmvnq_u8(dn2cpp_isa_bits<uint8x16_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<16>(vmvnq_u8(dn2cpp_isa_bits<uint8x16_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<16>(vmvnq_u8(dn2cpp_isa_bits<uint8x16_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<16>(vmvnq_u8(dn2cpp_isa_bits<uint8x16_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<16>(vmvnq_u8(dn2cpp_isa_bits<uint8x16_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<16>(vmvnq_u8(dn2cpp_isa_bits<uint8x16_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<16>(vmvnq_u8(dn2cpp_isa_bits<uint8x16_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<16>(vmvnq_u8(dn2cpp_isa_bits<uint8x16_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<16>(vmvnq_u8(dn2cpp_isa_bits<uint8x16_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<16>(vmvnq_u8(dn2cpp_isa_bits<uint8x16_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_not_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64f32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<8>(vmvn_u8(dn2cpp_isa_bits<uint8x8_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64f32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64f64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<8>(vmvn_u8(dn2cpp_isa_bits<uint8x8_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64f64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64i16(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<8>(vmvn_u8(dn2cpp_isa_bits<uint8x8_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64i16(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64i32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<8>(vmvn_u8(dn2cpp_isa_bits<uint8x8_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64i32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64i64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<8>(vmvn_u8(dn2cpp_isa_bits<uint8x8_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64i64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64i8(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<8>(vmvn_u8(dn2cpp_isa_bits<uint8x8_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64i8(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64u16(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<8>(vmvn_u8(dn2cpp_isa_bits<uint8x8_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64u16(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64u32(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<8>(vmvn_u8(dn2cpp_isa_bits<uint8x8_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64u32(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64u64(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<8>(vmvn_u8(dn2cpp_isa_bits<uint8x8_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64u64(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64u8(const Dn2CppVector64& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Not");
    return dn2cpp_isa_vec<8>(vmvn_u8(dn2cpp_isa_bits<uint8x8_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_not_v64u8(const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Not");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<16>(vorrq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<16>(vorrq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<16>(vorrq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<16>(vorrq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<16>(vorrq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<16>(vorrq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<16>(vorrq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<16>(vorrq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<16>(vorrq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<16>(vorrq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_or_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<8>(vorr_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<8>(vorr_u64(dn2cpp_isa_bits<uint64x1_t>(a0), dn2cpp_isa_bits<uint64x1_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<8>(vorr_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<8>(vorr_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64i64_v64i64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<8>(vorr_u64(dn2cpp_isa_bits<uint64x1_t>(a0), dn2cpp_isa_bits<uint64x1_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64i64_v64i64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<8>(vorr_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<8>(vorr_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<8>(vorr_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64u64_v64u64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<8>(vorr_u64(dn2cpp_isa_bits<uint64x1_t>(a0), dn2cpp_isa_bits<uint64x1_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64u64_v64u64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Or");
    return dn2cpp_isa_vec<8>(vorr_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_or_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Or");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftleftlogical_v128i16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<16>(vshlq_n_s16(dn2cpp_isa_bits<int16x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftleftlogical_v128i16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftleftlogical_v128i64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 64, a1, dn2cpp_isa_vec<16>(vshlq_n_s64(dn2cpp_isa_bits<int64x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftleftlogical_v128i64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftleftlogical_v128i8_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<16>(vshlq_n_s8(dn2cpp_isa_bits<int8x16_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftleftlogical_v128i8_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftleftlogical_v128u16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<16>(vshlq_n_u16(dn2cpp_isa_bits<uint16x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftleftlogical_v128u16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftleftlogical_v128u32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a1, dn2cpp_isa_vec<16>(vshlq_n_u32(dn2cpp_isa_bits<uint32x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftleftlogical_v128u32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftleftlogical_v128u64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 64, a1, dn2cpp_isa_vec<16>(vshlq_n_u64(dn2cpp_isa_bits<uint64x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftleftlogical_v128u64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftleftlogical_v128u8_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<16>(vshlq_n_u8(dn2cpp_isa_bits<uint8x16_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftleftlogical_v128u8_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftleftlogical_v64i16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<8>(vshl_n_s16(dn2cpp_isa_bits<int16x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftleftlogical_v64i16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftleftlogical_v64i32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a1, dn2cpp_isa_vec<8>(vshl_n_s32(dn2cpp_isa_bits<int32x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftleftlogical_v64i32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftleftlogical_v64i8_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<8>(vshl_n_s8(dn2cpp_isa_bits<int8x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftleftlogical_v64i8_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftleftlogical_v64u16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<8>(vshl_n_u16(dn2cpp_isa_bits<uint16x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftleftlogical_v64u16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftleftlogical_v64u32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a1, dn2cpp_isa_vec<8>(vshl_n_u32(dn2cpp_isa_bits<uint32x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftleftlogical_v64u32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftleftlogical_v64u8_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<8>(vshl_n_u8(dn2cpp_isa_bits<uint8x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftleftlogical_v64u8_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightarithmetic_v128i16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightArithmetic");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 16, a1, dn2cpp_isa_vec<16>(vshrq_n_s16(dn2cpp_isa_bits<int16x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightarithmetic_v128i16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightarithmetic_v128i32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightArithmetic");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 32, a1, dn2cpp_isa_vec<16>(vshrq_n_s32(dn2cpp_isa_bits<int32x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightarithmetic_v128i32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightarithmetic_v128i64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightArithmetic");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 64, a1, dn2cpp_isa_vec<16>(vshrq_n_s64(dn2cpp_isa_bits<int64x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightarithmetic_v128i64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightarithmetic_v128i8_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightArithmetic");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 8, a1, dn2cpp_isa_vec<16>(vshrq_n_s8(dn2cpp_isa_bits<int8x16_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightarithmetic_v128i8_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftrightarithmetic_v64i16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightArithmetic");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 16, a1, dn2cpp_isa_vec<8>(vshr_n_s16(dn2cpp_isa_bits<int16x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftrightarithmetic_v64i16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftrightarithmetic_v64i32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightArithmetic");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 32, a1, dn2cpp_isa_vec<8>(vshr_n_s32(dn2cpp_isa_bits<int32x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftrightarithmetic_v64i32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftrightarithmetic_v64i8_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightArithmetic");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 8, a1, dn2cpp_isa_vec<8>(vshr_n_s8(dn2cpp_isa_bits<int8x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftrightarithmetic_v64i8_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightlogical_v128i16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 16, a1, dn2cpp_isa_vec<16>(vshrq_n_u16(dn2cpp_isa_bits<uint16x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightlogical_v128i16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightlogical_v128i32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 32, a1, dn2cpp_isa_vec<16>(vshrq_n_u32(dn2cpp_isa_bits<uint32x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightlogical_v128i32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightlogical_v128i64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 64, a1, dn2cpp_isa_vec<16>(vshrq_n_u64(dn2cpp_isa_bits<uint64x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightlogical_v128i64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightlogical_v128i8_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 8, a1, dn2cpp_isa_vec<16>(vshrq_n_u8(dn2cpp_isa_bits<uint8x16_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightlogical_v128i8_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightlogical_v128u16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 16, a1, dn2cpp_isa_vec<16>(vshrq_n_u16(dn2cpp_isa_bits<uint16x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightlogical_v128u16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightlogical_v128u32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 32, a1, dn2cpp_isa_vec<16>(vshrq_n_u32(dn2cpp_isa_bits<uint32x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightlogical_v128u32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightlogical_v128u64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 64, a1, dn2cpp_isa_vec<16>(vshrq_n_u64(dn2cpp_isa_bits<uint64x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightlogical_v128u64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightlogical_v128u8_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 8, a1, dn2cpp_isa_vec<16>(vshrq_n_u8(dn2cpp_isa_bits<uint8x16_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_shiftrightlogical_v128u8_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftrightlogical_v64i16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 16, a1, dn2cpp_isa_vec<8>(vshr_n_u16(dn2cpp_isa_bits<uint16x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftrightlogical_v64i16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftrightlogical_v64i32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 32, a1, dn2cpp_isa_vec<8>(vshr_n_u32(dn2cpp_isa_bits<uint32x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftrightlogical_v64i32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftrightlogical_v64i8_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 8, a1, dn2cpp_isa_vec<8>(vshr_n_u8(dn2cpp_isa_bits<uint8x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftrightlogical_v64i8_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftrightlogical_v64u16_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 16, a1, dn2cpp_isa_vec<8>(vshr_n_u16(dn2cpp_isa_bits<uint16x4_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftrightlogical_v64u16_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftrightlogical_v64u32_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 32, a1, dn2cpp_isa_vec<8>(vshr_n_u32(dn2cpp_isa_bits<uint32x2_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftrightlogical_v64u32_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftrightlogical_v64u8_u8(const Dn2CppVector64& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
    DN2CPP_ISA_IMM_RANGE_SWITCH(1, 8, a1, dn2cpp_isa_vec<8>(vshr_n_u8(dn2cpp_isa_bits<uint8x8_t>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_shiftrightlogical_v64u8_u8(const Dn2CppVector64&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pf32_t2v64f32(float* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_f32_x2(a0, (float32x2x2_t{{dn2cpp_isa_bits<float32x2_t>(a1_1), dn2cpp_isa_bits<float32x2_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pf32_t2v64f32(float*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pf32_t3v64f32(float* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2, const Dn2CppVector64& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_f32_x3(a0, (float32x2x3_t{{dn2cpp_isa_bits<float32x2_t>(a1_1), dn2cpp_isa_bits<float32x2_t>(a1_2), dn2cpp_isa_bits<float32x2_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pf32_t3v64f32(float*, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pf32_t4v64f32(float* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2, const Dn2CppVector64& a1_3, const Dn2CppVector64& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_f32_x4(a0, (float32x2x4_t{{dn2cpp_isa_bits<float32x2_t>(a1_1), dn2cpp_isa_bits<float32x2_t>(a1_2), dn2cpp_isa_bits<float32x2_t>(a1_3), dn2cpp_isa_bits<float32x2_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pf32_t4v64f32(float*, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pf32_v128f32(float* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1q_f32(a0, dn2cpp_isa_bits<float32x4_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pf32_v128f32(float*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pf32_v64f32(float* a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_f32(a0, dn2cpp_isa_bits<float32x2_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pf32_v64f32(float*, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pf64_v128f64(double* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1q_f64(a0, dn2cpp_isa_bits<float64x2_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pf64_v128f64(double*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pf64_v64f64(double* a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_f64(a0, dn2cpp_isa_bits<float64x1_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pf64_v64f64(double*, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi16_t2v64i16(int16_t* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_s16_x2(a0, (int16x4x2_t{{dn2cpp_isa_bits<int16x4_t>(a1_1), dn2cpp_isa_bits<int16x4_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi16_t2v64i16(int16_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi16_t3v64i16(int16_t* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2, const Dn2CppVector64& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_s16_x3(a0, (int16x4x3_t{{dn2cpp_isa_bits<int16x4_t>(a1_1), dn2cpp_isa_bits<int16x4_t>(a1_2), dn2cpp_isa_bits<int16x4_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi16_t3v64i16(int16_t*, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi16_t4v64i16(int16_t* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2, const Dn2CppVector64& a1_3, const Dn2CppVector64& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_s16_x4(a0, (int16x4x4_t{{dn2cpp_isa_bits<int16x4_t>(a1_1), dn2cpp_isa_bits<int16x4_t>(a1_2), dn2cpp_isa_bits<int16x4_t>(a1_3), dn2cpp_isa_bits<int16x4_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi16_t4v64i16(int16_t*, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi16_v128i16(int16_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1q_s16(a0, dn2cpp_isa_bits<int16x8_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi16_v128i16(int16_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi16_v64i16(int16_t* a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_s16(a0, dn2cpp_isa_bits<int16x4_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi16_v64i16(int16_t*, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi32_t2v64i32(int32_t* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_s32_x2(a0, (int32x2x2_t{{dn2cpp_isa_bits<int32x2_t>(a1_1), dn2cpp_isa_bits<int32x2_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi32_t2v64i32(int32_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi32_t3v64i32(int32_t* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2, const Dn2CppVector64& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_s32_x3(a0, (int32x2x3_t{{dn2cpp_isa_bits<int32x2_t>(a1_1), dn2cpp_isa_bits<int32x2_t>(a1_2), dn2cpp_isa_bits<int32x2_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi32_t3v64i32(int32_t*, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi32_t4v64i32(int32_t* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2, const Dn2CppVector64& a1_3, const Dn2CppVector64& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_s32_x4(a0, (int32x2x4_t{{dn2cpp_isa_bits<int32x2_t>(a1_1), dn2cpp_isa_bits<int32x2_t>(a1_2), dn2cpp_isa_bits<int32x2_t>(a1_3), dn2cpp_isa_bits<int32x2_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi32_t4v64i32(int32_t*, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi32_v128i32(int32_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1q_s32(a0, dn2cpp_isa_bits<int32x4_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi32_v128i32(int32_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi32_v64i32(int32_t* a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_s32(a0, dn2cpp_isa_bits<int32x2_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi32_v64i32(int32_t*, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi64_v128i64(int64_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1q_s64(a0, dn2cpp_isa_bits<int64x2_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi64_v128i64(int64_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi64_v64i64(int64_t* a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_s64(a0, dn2cpp_isa_bits<int64x1_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi64_v64i64(int64_t*, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi8_t2v64i8(int8_t* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_s8_x2(a0, (int8x8x2_t{{dn2cpp_isa_bits<int8x8_t>(a1_1), dn2cpp_isa_bits<int8x8_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi8_t2v64i8(int8_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi8_t3v64i8(int8_t* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2, const Dn2CppVector64& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_s8_x3(a0, (int8x8x3_t{{dn2cpp_isa_bits<int8x8_t>(a1_1), dn2cpp_isa_bits<int8x8_t>(a1_2), dn2cpp_isa_bits<int8x8_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi8_t3v64i8(int8_t*, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi8_t4v64i8(int8_t* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2, const Dn2CppVector64& a1_3, const Dn2CppVector64& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_s8_x4(a0, (int8x8x4_t{{dn2cpp_isa_bits<int8x8_t>(a1_1), dn2cpp_isa_bits<int8x8_t>(a1_2), dn2cpp_isa_bits<int8x8_t>(a1_3), dn2cpp_isa_bits<int8x8_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi8_t4v64i8(int8_t*, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi8_v128i8(int8_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1q_s8(a0, dn2cpp_isa_bits<int8x16_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi8_v128i8(int8_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi8_v64i8(int8_t* a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_s8(a0, dn2cpp_isa_bits<int8x8_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pi8_v64i8(int8_t*, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu16_t2v64u16(uint16_t* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_u16_x2(a0, (uint16x4x2_t{{dn2cpp_isa_bits<uint16x4_t>(a1_1), dn2cpp_isa_bits<uint16x4_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu16_t2v64u16(uint16_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu16_t3v64u16(uint16_t* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2, const Dn2CppVector64& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_u16_x3(a0, (uint16x4x3_t{{dn2cpp_isa_bits<uint16x4_t>(a1_1), dn2cpp_isa_bits<uint16x4_t>(a1_2), dn2cpp_isa_bits<uint16x4_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu16_t3v64u16(uint16_t*, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu16_t4v64u16(uint16_t* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2, const Dn2CppVector64& a1_3, const Dn2CppVector64& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_u16_x4(a0, (uint16x4x4_t{{dn2cpp_isa_bits<uint16x4_t>(a1_1), dn2cpp_isa_bits<uint16x4_t>(a1_2), dn2cpp_isa_bits<uint16x4_t>(a1_3), dn2cpp_isa_bits<uint16x4_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu16_t4v64u16(uint16_t*, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu16_v128u16(uint16_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1q_u16(a0, dn2cpp_isa_bits<uint16x8_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu16_v128u16(uint16_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu16_v64u16(uint16_t* a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_u16(a0, dn2cpp_isa_bits<uint16x4_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu16_v64u16(uint16_t*, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu32_t2v64u32(uint32_t* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_u32_x2(a0, (uint32x2x2_t{{dn2cpp_isa_bits<uint32x2_t>(a1_1), dn2cpp_isa_bits<uint32x2_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu32_t2v64u32(uint32_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu32_t3v64u32(uint32_t* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2, const Dn2CppVector64& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_u32_x3(a0, (uint32x2x3_t{{dn2cpp_isa_bits<uint32x2_t>(a1_1), dn2cpp_isa_bits<uint32x2_t>(a1_2), dn2cpp_isa_bits<uint32x2_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu32_t3v64u32(uint32_t*, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu32_t4v64u32(uint32_t* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2, const Dn2CppVector64& a1_3, const Dn2CppVector64& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_u32_x4(a0, (uint32x2x4_t{{dn2cpp_isa_bits<uint32x2_t>(a1_1), dn2cpp_isa_bits<uint32x2_t>(a1_2), dn2cpp_isa_bits<uint32x2_t>(a1_3), dn2cpp_isa_bits<uint32x2_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu32_t4v64u32(uint32_t*, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu32_v128u32(uint32_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1q_u32(a0, dn2cpp_isa_bits<uint32x4_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu32_v128u32(uint32_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu32_v64u32(uint32_t* a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_u32(a0, dn2cpp_isa_bits<uint32x2_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu32_v64u32(uint32_t*, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu64_v128u64(uint64_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1q_u64(a0, dn2cpp_isa_bits<uint64x2_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu64_v128u64(uint64_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu64_v64u64(uint64_t* a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_u64(a0, dn2cpp_isa_bits<uint64x1_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu64_v64u64(uint64_t*, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu8_t2v64u8(uint8_t* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_u8_x2(a0, (uint8x8x2_t{{dn2cpp_isa_bits<uint8x8_t>(a1_1), dn2cpp_isa_bits<uint8x8_t>(a1_2)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu8_t2v64u8(uint8_t*, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu8_t3v64u8(uint8_t* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2, const Dn2CppVector64& a1_3)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_u8_x3(a0, (uint8x8x3_t{{dn2cpp_isa_bits<uint8x8_t>(a1_1), dn2cpp_isa_bits<uint8x8_t>(a1_2), dn2cpp_isa_bits<uint8x8_t>(a1_3)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu8_t3v64u8(uint8_t*, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu8_t4v64u8(uint8_t* a0, const Dn2CppVector64& a1_1, const Dn2CppVector64& a1_2, const Dn2CppVector64& a1_3, const Dn2CppVector64& a1_4)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_u8_x4(a0, (uint8x8x4_t{{dn2cpp_isa_bits<uint8x8_t>(a1_1), dn2cpp_isa_bits<uint8x8_t>(a1_2), dn2cpp_isa_bits<uint8x8_t>(a1_3), dn2cpp_isa_bits<uint8x8_t>(a1_4)}}));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu8_t4v64u8(uint8_t*, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu8_v128u8(uint8_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1q_u8(a0, dn2cpp_isa_bits<uint8x16_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu8_v128u8(uint8_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu8_v64u8(uint8_t* a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Store");
    vst1_u8(a0, dn2cpp_isa_bits<uint8x8_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_arm_advsimd_store_pu8_v64u8(uint8_t*, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Store");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtract_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
    return dn2cpp_isa_vec<16>(vsubq_f32(dn2cpp_isa_bits<float32x4_t>(a0), dn2cpp_isa_bits<float32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtract_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtract_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
    return dn2cpp_isa_vec<16>(vsubq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtract_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtract_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
    return dn2cpp_isa_vec<16>(vsubq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtract_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtract_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
    return dn2cpp_isa_vec<16>(vsubq_s64(dn2cpp_isa_bits<int64x2_t>(a0), dn2cpp_isa_bits<int64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtract_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtract_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
    return dn2cpp_isa_vec<16>(vsubq_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtract_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtract_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
    return dn2cpp_isa_vec<16>(vsubq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtract_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtract_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
    return dn2cpp_isa_vec<16>(vsubq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtract_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtract_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
    return dn2cpp_isa_vec<16>(vsubq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtract_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtract_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
    return dn2cpp_isa_vec<16>(vsubq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtract_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtract_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
    return dn2cpp_isa_vec<8>(vsub_f32(dn2cpp_isa_bits<float32x2_t>(a0), dn2cpp_isa_bits<float32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtract_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtract_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
    return dn2cpp_isa_vec<8>(vsub_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtract_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtract_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
    return dn2cpp_isa_vec<8>(vsub_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtract_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtract_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
    return dn2cpp_isa_vec<8>(vsub_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtract_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtract_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
    return dn2cpp_isa_vec<8>(vsub_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtract_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtract_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
    return dn2cpp_isa_vec<8>(vsub_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtract_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtract_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
    return dn2cpp_isa_vec<8>(vsub_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtract_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtractsaturate_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
    return dn2cpp_isa_vec<16>(vqsubq_s16(dn2cpp_isa_bits<int16x8_t>(a0), dn2cpp_isa_bits<int16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtractsaturate_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtractsaturate_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
    return dn2cpp_isa_vec<16>(vqsubq_s32(dn2cpp_isa_bits<int32x4_t>(a0), dn2cpp_isa_bits<int32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtractsaturate_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtractsaturate_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
    return dn2cpp_isa_vec<16>(vqsubq_s64(dn2cpp_isa_bits<int64x2_t>(a0), dn2cpp_isa_bits<int64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtractsaturate_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtractsaturate_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
    return dn2cpp_isa_vec<16>(vqsubq_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtractsaturate_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtractsaturate_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
    return dn2cpp_isa_vec<16>(vqsubq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtractsaturate_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtractsaturate_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
    return dn2cpp_isa_vec<16>(vqsubq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtractsaturate_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtractsaturate_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
    return dn2cpp_isa_vec<16>(vqsubq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtractsaturate_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtractsaturate_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
    return dn2cpp_isa_vec<16>(vqsubq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_subtractsaturate_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtractsaturate_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
    return dn2cpp_isa_vec<8>(vqsub_s16(dn2cpp_isa_bits<int16x4_t>(a0), dn2cpp_isa_bits<int16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtractsaturate_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtractsaturate_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
    return dn2cpp_isa_vec<8>(vqsub_s32(dn2cpp_isa_bits<int32x2_t>(a0), dn2cpp_isa_bits<int32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtractsaturate_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtractsaturate_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
    return dn2cpp_isa_vec<8>(vqsub_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtractsaturate_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtractsaturate_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
    return dn2cpp_isa_vec<8>(vqsub_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtractsaturate_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtractsaturate_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
    return dn2cpp_isa_vec<8>(vqsub_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtractsaturate_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtractsaturate_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
    return dn2cpp_isa_vec<8>(vqsub_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_subtractsaturate_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookup_t2v128i8_v64i8(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookup");
    return dn2cpp_isa_vec<8>(vqtbl2_s8((int8x16x2_t{{dn2cpp_isa_bits<int8x16_t>(a0_1), dn2cpp_isa_bits<int8x16_t>(a0_2)}}), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookup_t2v128i8_v64i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookup");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookup_t2v128u8_v64u8(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookup");
    return dn2cpp_isa_vec<8>(vqtbl2_u8((uint8x16x2_t{{dn2cpp_isa_bits<uint8x16_t>(a0_1), dn2cpp_isa_bits<uint8x16_t>(a0_2)}}), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookup_t2v128u8_v64u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookup");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookup_t3v128i8_v64i8(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookup");
    return dn2cpp_isa_vec<8>(vqtbl3_s8((int8x16x3_t{{dn2cpp_isa_bits<int8x16_t>(a0_1), dn2cpp_isa_bits<int8x16_t>(a0_2), dn2cpp_isa_bits<int8x16_t>(a0_3)}}), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookup_t3v128i8_v64i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookup");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookup_t3v128u8_v64u8(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookup");
    return dn2cpp_isa_vec<8>(vqtbl3_u8((uint8x16x3_t{{dn2cpp_isa_bits<uint8x16_t>(a0_1), dn2cpp_isa_bits<uint8x16_t>(a0_2), dn2cpp_isa_bits<uint8x16_t>(a0_3)}}), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookup_t3v128u8_v64u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookup");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookup_t4v128i8_v64i8(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, const Dn2CppVector128& a0_4, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookup");
    return dn2cpp_isa_vec<8>(vqtbl4_s8((int8x16x4_t{{dn2cpp_isa_bits<int8x16_t>(a0_1), dn2cpp_isa_bits<int8x16_t>(a0_2), dn2cpp_isa_bits<int8x16_t>(a0_3), dn2cpp_isa_bits<int8x16_t>(a0_4)}}), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookup_t4v128i8_v64i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookup");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookup_t4v128u8_v64u8(const Dn2CppVector128& a0_1, const Dn2CppVector128& a0_2, const Dn2CppVector128& a0_3, const Dn2CppVector128& a0_4, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookup");
    return dn2cpp_isa_vec<8>(vqtbl4_u8((uint8x16x4_t{{dn2cpp_isa_bits<uint8x16_t>(a0_1), dn2cpp_isa_bits<uint8x16_t>(a0_2), dn2cpp_isa_bits<uint8x16_t>(a0_3), dn2cpp_isa_bits<uint8x16_t>(a0_4)}}), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookup_t4v128u8_v64u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookup");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookup_v128i8_v64i8(const Dn2CppVector128& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookup");
    return dn2cpp_isa_vec<8>(vqtbl1_s8(dn2cpp_isa_bits<int8x16_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookup_v128i8_v64i8(const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookup");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookup_v128u8_v64u8(const Dn2CppVector128& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookup");
    return dn2cpp_isa_vec<8>(vqtbl1_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookup_v128u8_v64u8(const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookup");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookupextension_v64i8_t2v128i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookupExtension");
    return dn2cpp_isa_vec<8>(vqtbx2_s8(dn2cpp_isa_bits<int8x8_t>(a0), (int8x16x2_t{{dn2cpp_isa_bits<int8x16_t>(a1_1), dn2cpp_isa_bits<int8x16_t>(a1_2)}}), dn2cpp_isa_bits<uint8x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookupextension_v64i8_t2v128i8_v64i8(const Dn2CppVector64&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookupExtension");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookupextension_v64i8_t3v128i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookupExtension");
    return dn2cpp_isa_vec<8>(vqtbx3_s8(dn2cpp_isa_bits<int8x8_t>(a0), (int8x16x3_t{{dn2cpp_isa_bits<int8x16_t>(a1_1), dn2cpp_isa_bits<int8x16_t>(a1_2), dn2cpp_isa_bits<int8x16_t>(a1_3)}}), dn2cpp_isa_bits<uint8x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookupextension_v64i8_t3v128i8_v64i8(const Dn2CppVector64&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookupExtension");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookupextension_v64i8_t4v128i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookupExtension");
    return dn2cpp_isa_vec<8>(vqtbx4_s8(dn2cpp_isa_bits<int8x8_t>(a0), (int8x16x4_t{{dn2cpp_isa_bits<int8x16_t>(a1_1), dn2cpp_isa_bits<int8x16_t>(a1_2), dn2cpp_isa_bits<int8x16_t>(a1_3), dn2cpp_isa_bits<int8x16_t>(a1_4)}}), dn2cpp_isa_bits<uint8x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookupextension_v64i8_t4v128i8_v64i8(const Dn2CppVector64&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookupExtension");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookupextension_v64i8_v128i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector128& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookupExtension");
    return dn2cpp_isa_vec<8>(vqtbx1_s8(dn2cpp_isa_bits<int8x8_t>(a0), dn2cpp_isa_bits<int8x16_t>(a1), dn2cpp_isa_bits<uint8x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookupextension_v64i8_v128i8_v64i8(const Dn2CppVector64&, const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookupExtension");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookupextension_v64u8_t2v128u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookupExtension");
    return dn2cpp_isa_vec<8>(vqtbx2_u8(dn2cpp_isa_bits<uint8x8_t>(a0), (uint8x16x2_t{{dn2cpp_isa_bits<uint8x16_t>(a1_1), dn2cpp_isa_bits<uint8x16_t>(a1_2)}}), dn2cpp_isa_bits<uint8x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookupextension_v64u8_t2v128u8_v64u8(const Dn2CppVector64&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookupExtension");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookupextension_v64u8_t3v128u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookupExtension");
    return dn2cpp_isa_vec<8>(vqtbx3_u8(dn2cpp_isa_bits<uint8x8_t>(a0), (uint8x16x3_t{{dn2cpp_isa_bits<uint8x16_t>(a1_1), dn2cpp_isa_bits<uint8x16_t>(a1_2), dn2cpp_isa_bits<uint8x16_t>(a1_3)}}), dn2cpp_isa_bits<uint8x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookupextension_v64u8_t3v128u8_v64u8(const Dn2CppVector64&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookupExtension");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookupextension_v64u8_t4v128u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector128& a1_1, const Dn2CppVector128& a1_2, const Dn2CppVector128& a1_3, const Dn2CppVector128& a1_4, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookupExtension");
    return dn2cpp_isa_vec<8>(vqtbx4_u8(dn2cpp_isa_bits<uint8x8_t>(a0), (uint8x16x4_t{{dn2cpp_isa_bits<uint8x16_t>(a1_1), dn2cpp_isa_bits<uint8x16_t>(a1_2), dn2cpp_isa_bits<uint8x16_t>(a1_3), dn2cpp_isa_bits<uint8x16_t>(a1_4)}}), dn2cpp_isa_bits<uint8x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookupextension_v64u8_t4v128u8_v64u8(const Dn2CppVector64&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookupExtension");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookupextension_v64u8_v128u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector128& a1, const Dn2CppVector64& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookupExtension");
    return dn2cpp_isa_vec<8>(vqtbx1_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1), dn2cpp_isa_bits<uint8x8_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_vectortablelookupextension_v64u8_v128u8_v64u8(const Dn2CppVector64&, const Dn2CppVector128&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.VectorTableLookupExtension");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<16>(veorq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<16>(veorq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<16>(veorq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<16>(veorq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<16>(veorq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<16>(veorq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<16>(veorq_u16(dn2cpp_isa_bits<uint16x8_t>(a0), dn2cpp_isa_bits<uint16x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<16>(veorq_u32(dn2cpp_isa_bits<uint32x4_t>(a0), dn2cpp_isa_bits<uint32x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<16>(veorq_u64(dn2cpp_isa_bits<uint64x2_t>(a0), dn2cpp_isa_bits<uint64x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<16>(veorq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_advsimd_xor_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64f32_v64f32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<8>(veor_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64f32_v64f32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64f64_v64f64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<8>(veor_u64(dn2cpp_isa_bits<uint64x1_t>(a0), dn2cpp_isa_bits<uint64x1_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64f64_v64f64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64i16_v64i16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<8>(veor_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64i16_v64i16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64i32_v64i32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<8>(veor_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64i32_v64i32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64i64_v64i64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<8>(veor_u64(dn2cpp_isa_bits<uint64x1_t>(a0), dn2cpp_isa_bits<uint64x1_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64i64_v64i64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64i8_v64i8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<8>(veor_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64i8_v64i8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64u16_v64u16(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<8>(veor_u16(dn2cpp_isa_bits<uint16x4_t>(a0), dn2cpp_isa_bits<uint16x4_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64u16_v64u16(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64u32_v64u32(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<8>(veor_u32(dn2cpp_isa_bits<uint32x2_t>(a0), dn2cpp_isa_bits<uint32x2_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64u32_v64u32(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64u64_v64u64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<8>(veor_u64(dn2cpp_isa_bits<uint64x1_t>(a0), dn2cpp_isa_bits<uint64x1_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64u64_v64u64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64u8_v64u8(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_AdvSimd, "System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
    return dn2cpp_isa_vec<8>(veor_u8(dn2cpp_isa_bits<uint8x8_t>(a0), dn2cpp_isa_bits<uint8x8_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector64 dn2cpp_isa_arm_advsimd_xor_v64u8_v64u8(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.AdvSimd.Xor");
}
#endif
