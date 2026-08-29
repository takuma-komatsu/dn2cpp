#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.Wasm.PackedSimd: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_abs_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Abs");
    return dn2cpp_isa_vec<16>(wasm_f32x4_abs(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_abs_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Abs");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_abs_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Abs");
    return dn2cpp_isa_vec<16>(wasm_f64x2_abs(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_abs_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Abs");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_abs_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Abs");
    return dn2cpp_isa_vec<16>(wasm_i16x8_abs(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_abs_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Abs");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_abs_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Abs");
    return dn2cpp_isa_vec<16>(wasm_i32x4_abs(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_abs_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Abs");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_abs_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Abs");
    return dn2cpp_isa_vec<16>(wasm_i64x2_abs(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_abs_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Abs");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_abs_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Abs");
    return dn2cpp_isa_vec<16>(wasm_i8x16_abs(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_abs_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Abs");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_abs_v128nint(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Abs");
    return dn2cpp_isa_vec<16>(wasm_i32x4_abs(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_abs_v128nint(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Abs");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
    return dn2cpp_isa_vec<16>(wasm_f32x4_add(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
    return dn2cpp_isa_vec<16>(wasm_f64x2_add(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
    return dn2cpp_isa_vec<16>(wasm_i16x8_add(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
    return dn2cpp_isa_vec<16>(wasm_i32x4_add(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
    return dn2cpp_isa_vec<16>(wasm_i64x2_add(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
    return dn2cpp_isa_vec<16>(wasm_i8x16_add(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128nint_v128nint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
    return dn2cpp_isa_vec<16>(wasm_i32x4_add(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128nint_v128nint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128nuint_v128nuint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
    return dn2cpp_isa_vec<16>(wasm_i32x4_add(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128nuint_v128nuint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
    return dn2cpp_isa_vec<16>(wasm_i16x8_add(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
    return dn2cpp_isa_vec<16>(wasm_i32x4_add(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
    return dn2cpp_isa_vec<16>(wasm_i64x2_add(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
    return dn2cpp_isa_vec<16>(wasm_i8x16_add(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_add_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Add");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_addpairwisewidening_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AddPairwiseWidening");
    return dn2cpp_isa_vec<16>(wasm_i32x4_extadd_pairwise_i16x8(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_addpairwisewidening_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AddPairwiseWidening");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_addpairwisewidening_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AddPairwiseWidening");
    return dn2cpp_isa_vec<16>(wasm_i16x8_extadd_pairwise_i8x16(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_addpairwisewidening_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AddPairwiseWidening");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_addpairwisewidening_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AddPairwiseWidening");
    return dn2cpp_isa_vec<16>(wasm_u32x4_extadd_pairwise_u16x8(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_addpairwisewidening_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AddPairwiseWidening");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_addpairwisewidening_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AddPairwiseWidening");
    return dn2cpp_isa_vec<16>(wasm_u16x8_extadd_pairwise_u8x16(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_addpairwisewidening_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AddPairwiseWidening");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_addsaturate_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AddSaturate");
    return dn2cpp_isa_vec<16>(wasm_i16x8_add_sat(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_addsaturate_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AddSaturate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_addsaturate_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AddSaturate");
    return dn2cpp_isa_vec<16>(wasm_i8x16_add_sat(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_addsaturate_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AddSaturate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_addsaturate_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AddSaturate");
    return dn2cpp_isa_vec<16>(wasm_u16x8_add_sat(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_addsaturate_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AddSaturate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_addsaturate_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AddSaturate");
    return dn2cpp_isa_vec<16>(wasm_u8x16_add_sat(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_addsaturate_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AddSaturate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
    return wasm_i16x8_all_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
    return wasm_i32x4_all_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
    return wasm_i64x2_all_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
    return wasm_i8x16_all_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128nint(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
    return wasm_i32x4_all_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128nint(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128nuint(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
    return wasm_i32x4_all_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128nuint(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
    return wasm_i16x8_all_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
    return wasm_i32x4_all_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
    return wasm_i64x2_all_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
    return wasm_i8x16_all_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_alltrue_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AllTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.And");
    return dn2cpp_isa_vec<16>(wasm_v128_and(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.And");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.And");
    return dn2cpp_isa_vec<16>(wasm_v128_and(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.And");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.And");
    return dn2cpp_isa_vec<16>(wasm_v128_and(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.And");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.And");
    return dn2cpp_isa_vec<16>(wasm_v128_and(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.And");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.And");
    return dn2cpp_isa_vec<16>(wasm_v128_and(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.And");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.And");
    return dn2cpp_isa_vec<16>(wasm_v128_and(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.And");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128nint_v128nint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.And");
    return dn2cpp_isa_vec<16>(wasm_v128_and(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128nint_v128nint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.And");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128nuint_v128nuint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.And");
    return dn2cpp_isa_vec<16>(wasm_v128_and(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128nuint_v128nuint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.And");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.And");
    return dn2cpp_isa_vec<16>(wasm_v128_and(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.And");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.And");
    return dn2cpp_isa_vec<16>(wasm_v128_and(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.And");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.And");
    return dn2cpp_isa_vec<16>(wasm_v128_and(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.And");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.And");
    return dn2cpp_isa_vec<16>(wasm_v128_and(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_and_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.And");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
    return dn2cpp_isa_vec<16>(wasm_v128_andnot(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
    return dn2cpp_isa_vec<16>(wasm_v128_andnot(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
    return dn2cpp_isa_vec<16>(wasm_v128_andnot(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
    return dn2cpp_isa_vec<16>(wasm_v128_andnot(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
    return dn2cpp_isa_vec<16>(wasm_v128_andnot(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
    return dn2cpp_isa_vec<16>(wasm_v128_andnot(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128nint_v128nint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
    return dn2cpp_isa_vec<16>(wasm_v128_andnot(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128nint_v128nint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128nuint_v128nuint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
    return dn2cpp_isa_vec<16>(wasm_v128_andnot(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128nuint_v128nuint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
    return dn2cpp_isa_vec<16>(wasm_v128_andnot(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
    return dn2cpp_isa_vec<16>(wasm_v128_andnot(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
    return dn2cpp_isa_vec<16>(wasm_v128_andnot(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
    return dn2cpp_isa_vec<16>(wasm_v128_andnot(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_andnot_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AndNot");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
    return wasm_v128_any_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
    return wasm_v128_any_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
    return wasm_v128_any_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
    return wasm_v128_any_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
    return wasm_v128_any_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
    return wasm_v128_any_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128nint(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
    return wasm_v128_any_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128nint(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128nuint(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
    return wasm_v128_any_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128nuint(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
    return wasm_v128_any_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
    return wasm_v128_any_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
    return wasm_v128_any_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
    return wasm_v128_any_true(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_wasm_packedsimd_anytrue_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AnyTrue");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_averagerounded_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AverageRounded");
    return dn2cpp_isa_vec<16>(wasm_u16x8_avgr(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_averagerounded_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AverageRounded");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_averagerounded_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.AverageRounded");
    return dn2cpp_isa_vec<16>(wasm_u8x16_avgr(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_averagerounded_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.AverageRounded");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
    return wasm_i16x8_bitmask(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
    return wasm_i32x4_bitmask(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
    return wasm_i64x2_bitmask(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
    return wasm_i8x16_bitmask(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128nint(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
    return wasm_i32x4_bitmask(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128nint(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128nuint(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
    return wasm_i32x4_bitmask(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128nuint(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
    return wasm_i16x8_bitmask(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
    return wasm_i32x4_bitmask(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
    return wasm_i64x2_bitmask(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
    return wasm_i8x16_bitmask(dn2cpp_isa_bits<v128_t>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_bitmask_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Bitmask");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128f32_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(wasm_v128_bitselect(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1), dn2cpp_isa_bits<v128_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128f32_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128f64_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(wasm_v128_bitselect(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1), dn2cpp_isa_bits<v128_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128f64_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128i16_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(wasm_v128_bitselect(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1), dn2cpp_isa_bits<v128_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128i16_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128i32_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(wasm_v128_bitselect(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1), dn2cpp_isa_bits<v128_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128i32_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128i64_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(wasm_v128_bitselect(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1), dn2cpp_isa_bits<v128_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128i64_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128i8_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(wasm_v128_bitselect(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1), dn2cpp_isa_bits<v128_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128i8_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128nint_v128nint_v128nint(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(wasm_v128_bitselect(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1), dn2cpp_isa_bits<v128_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128nint_v128nint_v128nint(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128nuint_v128nuint_v128nuint(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(wasm_v128_bitselect(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1), dn2cpp_isa_bits<v128_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128nuint_v128nuint_v128nuint(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128u16_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(wasm_v128_bitselect(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1), dn2cpp_isa_bits<v128_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128u16_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128u32_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(wasm_v128_bitselect(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1), dn2cpp_isa_bits<v128_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128u32_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128u64_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(wasm_v128_bitselect(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1), dn2cpp_isa_bits<v128_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128u64_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128u8_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
    return dn2cpp_isa_vec<16>(wasm_v128_bitselect(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1), dn2cpp_isa_bits<v128_t>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_bitwiseselect_v128u8_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.BitwiseSelect");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_ceiling_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Ceiling");
    return dn2cpp_isa_vec<16>(wasm_f32x4_ceil(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_ceiling_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Ceiling");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_ceiling_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Ceiling");
    return dn2cpp_isa_vec<16>(wasm_f64x2_ceil(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_ceiling_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Ceiling");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(wasm_f32x4_eq(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(wasm_f64x2_eq(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(wasm_i16x8_eq(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(wasm_i32x4_eq(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(wasm_i64x2_eq(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(wasm_i8x16_eq(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128nint_v128nint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(wasm_i32x4_eq(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128nint_v128nint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128nuint_v128nuint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(wasm_i32x4_eq(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128nuint_v128nuint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(wasm_i16x8_eq(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(wasm_i32x4_eq(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(wasm_i64x2_eq(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
    return dn2cpp_isa_vec<16>(wasm_i8x16_eq(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_compareequal_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(wasm_f32x4_gt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(wasm_f64x2_gt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(wasm_i16x8_gt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(wasm_i32x4_gt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(wasm_i64x2_gt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(wasm_i8x16_gt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128nint_v128nint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(wasm_i32x4_gt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128nint_v128nint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128nuint_v128nuint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(wasm_u32x4_gt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128nuint_v128nuint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(wasm_u16x8_gt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(wasm_u32x4_gt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(wasm_i64x2_gt(dn2cpp_isa_wasm_flip_sign64(dn2cpp_isa_bits<v128_t>(a0)), dn2cpp_isa_wasm_flip_sign64(dn2cpp_isa_bits<v128_t>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(wasm_u8x16_gt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthan_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_f32x4_ge(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_f64x2_ge(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_i16x8_ge(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_i32x4_ge(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_i64x2_ge(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_i8x16_ge(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128nint_v128nint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_i32x4_ge(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128nint_v128nint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128nuint_v128nuint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_u32x4_ge(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128nuint_v128nuint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_u16x8_ge(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_u32x4_ge(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_i64x2_ge(dn2cpp_isa_wasm_flip_sign64(dn2cpp_isa_bits<v128_t>(a0)), dn2cpp_isa_wasm_flip_sign64(dn2cpp_isa_bits<v128_t>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_u8x16_ge(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparegreaterthanorequal_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(wasm_f32x4_lt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(wasm_f64x2_lt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(wasm_i16x8_lt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(wasm_i32x4_lt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(wasm_i64x2_lt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(wasm_i8x16_lt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128nint_v128nint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(wasm_i32x4_lt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128nint_v128nint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128nuint_v128nuint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(wasm_u32x4_lt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128nuint_v128nuint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(wasm_u16x8_lt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(wasm_u32x4_lt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(wasm_i64x2_lt(dn2cpp_isa_wasm_flip_sign64(dn2cpp_isa_bits<v128_t>(a0)), dn2cpp_isa_wasm_flip_sign64(dn2cpp_isa_bits<v128_t>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
    return dn2cpp_isa_vec<16>(wasm_u8x16_lt(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthan_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_f32x4_le(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_f64x2_le(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_i16x8_le(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_i32x4_le(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_i64x2_le(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_i8x16_le(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128nint_v128nint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_i32x4_le(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128nint_v128nint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128nuint_v128nuint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_u32x4_le(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128nuint_v128nuint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_u16x8_le(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_u32x4_le(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_i64x2_le(dn2cpp_isa_wasm_flip_sign64(dn2cpp_isa_bits<v128_t>(a0)), dn2cpp_isa_wasm_flip_sign64(dn2cpp_isa_bits<v128_t>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(wasm_u8x16_le(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparelessthanorequal_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
    return dn2cpp_isa_vec<16>(wasm_f32x4_ne(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
    return dn2cpp_isa_vec<16>(wasm_f64x2_ne(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
    return dn2cpp_isa_vec<16>(wasm_i16x8_ne(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
    return dn2cpp_isa_vec<16>(wasm_i32x4_ne(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
    return dn2cpp_isa_vec<16>(wasm_i64x2_ne(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
    return dn2cpp_isa_vec<16>(wasm_i8x16_ne(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128nint_v128nint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
    return dn2cpp_isa_vec<16>(wasm_i32x4_ne(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128nint_v128nint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128nuint_v128nuint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
    return dn2cpp_isa_vec<16>(wasm_i32x4_ne(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128nuint_v128nuint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
    return dn2cpp_isa_vec<16>(wasm_i16x8_ne(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
    return dn2cpp_isa_vec<16>(wasm_i32x4_ne(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
    return dn2cpp_isa_vec<16>(wasm_i64x2_ne(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
    return dn2cpp_isa_vec<16>(wasm_i8x16_ne(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_comparenotequal_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_convertnarrowingsaturatesigned_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertNarrowingSaturateSigned");
    return dn2cpp_isa_vec<16>(wasm_i8x16_narrow_i16x8(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_convertnarrowingsaturatesigned_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertNarrowingSaturateSigned");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_convertnarrowingsaturatesigned_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertNarrowingSaturateSigned");
    return dn2cpp_isa_vec<16>(wasm_i16x8_narrow_i32x4(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_convertnarrowingsaturatesigned_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertNarrowingSaturateSigned");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_convertnarrowingsaturateunsigned_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertNarrowingSaturateUnsigned");
    return dn2cpp_isa_vec<16>(wasm_u8x16_narrow_i16x8(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_convertnarrowingsaturateunsigned_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertNarrowingSaturateUnsigned");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_convertnarrowingsaturateunsigned_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertNarrowingSaturateUnsigned");
    return dn2cpp_isa_vec<16>(wasm_u16x8_narrow_i32x4(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_convertnarrowingsaturateunsigned_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertNarrowingSaturateUnsigned");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttodoublelower_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToDoubleLower");
    return dn2cpp_isa_vec<16>(wasm_f64x2_promote_low_f32x4(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttodoublelower_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToDoubleLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttodoublelower_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToDoubleLower");
    return dn2cpp_isa_vec<16>(wasm_f64x2_convert_low_i32x4(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttodoublelower_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToDoubleLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttodoublelower_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToDoubleLower");
    return dn2cpp_isa_vec<16>(wasm_f64x2_convert_low_u32x4(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttodoublelower_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToDoubleLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttoint32saturate_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToInt32Saturate");
    return dn2cpp_isa_vec<16>(wasm_i32x4_trunc_sat_f32x4(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttoint32saturate_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToInt32Saturate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttoint32saturate_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToInt32Saturate");
    return dn2cpp_isa_vec<16>(wasm_i32x4_trunc_sat_f64x2_zero(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttoint32saturate_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToInt32Saturate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttosingle_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToSingle");
    return dn2cpp_isa_vec<16>(wasm_f32x4_demote_f64x2_zero(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttosingle_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToSingle");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttosingle_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToSingle");
    return dn2cpp_isa_vec<16>(wasm_f32x4_convert_i32x4(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttosingle_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToSingle");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttosingle_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToSingle");
    return dn2cpp_isa_vec<16>(wasm_f32x4_convert_u32x4(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttosingle_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToSingle");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttouint32saturate_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToUInt32Saturate");
    return dn2cpp_isa_vec<16>(wasm_u32x4_trunc_sat_f32x4(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttouint32saturate_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToUInt32Saturate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttouint32saturate_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToUInt32Saturate");
    return dn2cpp_isa_vec<16>(wasm_u32x4_trunc_sat_f64x2_zero(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_converttouint32saturate_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ConvertToUInt32Saturate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_divide_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Divide");
    return dn2cpp_isa_vec<16>(wasm_f32x4_div(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_divide_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Divide");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_divide_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Divide");
    return dn2cpp_isa_vec<16>(wasm_f64x2_div(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_divide_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Divide");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_dot_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Dot");
    return dn2cpp_isa_vec<16>(wasm_i32x4_dot_i16x8(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_dot_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Dot");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE float dn2cpp_isa_wasm_packedsimd_extractscalar_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, wasm_f32x4_extract_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE float dn2cpp_isa_wasm_packedsimd_extractscalar_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE double dn2cpp_isa_wasm_packedsimd_extractscalar_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, wasm_f64x2_extract_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE double dn2cpp_isa_wasm_packedsimd_extractscalar_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128i16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, wasm_i16x8_extract_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128i16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128i32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, wasm_i32x4_extract_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128i32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE int64_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128i64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, wasm_i64x2_extract_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int64_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128i64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128i8_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, wasm_i8x16_extract_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128i8_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE intptr_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128nint_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, wasm_i32x4_extract_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE intptr_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128nint_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE uintptr_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128nuint_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, wasm_u32x4_extract_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uintptr_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128nuint_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128u16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, wasm_u16x8_extract_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128u16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128u32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, wasm_u32x4_extract_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128u32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128u64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, wasm_u64x2_extract_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128u64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128u8_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, wasm_u8x16_extract_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_wasm_packedsimd_extractscalar_v128u8_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ExtractScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_floor_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Floor");
    return dn2cpp_isa_vec<16>(wasm_f32x4_floor(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_floor_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Floor");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_floor_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Floor");
    return dn2cpp_isa_vec<16>(wasm_f64x2_floor(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_floor_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Floor");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pf32_v128f32_u8(float* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, dn2cpp_isa_vec<16>(wasm_v128_load32_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pf32_v128f32_u8(float*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pf64_v128f64_u8(double* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, dn2cpp_isa_vec<16>(wasm_v128_load64_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pf64_v128f64_u8(double*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pi16_v128i16_u8(int16_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a2, dn2cpp_isa_vec<16>(wasm_v128_load16_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pi16_v128i16_u8(int16_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pi32_v128i32_u8(int32_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, dn2cpp_isa_vec<16>(wasm_v128_load32_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pi32_v128i32_u8(int32_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pi64_v128i64_u8(int64_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, dn2cpp_isa_vec<16>(wasm_v128_load64_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pi64_v128i64_u8(int64_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pi8_v128i8_u8(int8_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<16>(wasm_v128_load8_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pi8_v128i8_u8(int8_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pnint_v128nint_u8(intptr_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, dn2cpp_isa_vec<16>(wasm_v128_load32_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pnint_v128nint_u8(intptr_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pnuint_v128nuint_u8(uintptr_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, dn2cpp_isa_vec<16>(wasm_v128_load32_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pnuint_v128nuint_u8(uintptr_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pu16_v128u16_u8(uint16_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a2, dn2cpp_isa_vec<16>(wasm_v128_load16_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pu16_v128u16_u8(uint16_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pu32_v128u32_u8(uint32_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, dn2cpp_isa_vec<16>(wasm_v128_load32_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pu32_v128u32_u8(uint32_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pu64_v128u64_u8(uint64_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, dn2cpp_isa_vec<16>(wasm_v128_load64_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pu64_v128u64_u8(uint64_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pu8_v128u8_u8(uint8_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<16>(wasm_v128_load8_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandinsert_pu8_v128u8_u8(uint8_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndInsert");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pf32(float* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load32_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pf32(float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load64_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load16_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load32_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load64_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load8_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pnint(intptr_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load32_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pnint(intptr_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pnuint(uintptr_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load32_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pnuint(uintptr_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load16_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load32_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load64_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load8_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarandsplatvector128_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarAndSplatVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarvector128_pf32(float* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load32_zero(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarvector128_pf32(float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarvector128_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load64_zero(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarvector128_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarvector128_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load32_zero(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarvector128_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarvector128_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load64_zero(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarvector128_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarvector128_pnint(intptr_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load32_zero(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarvector128_pnint(intptr_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarvector128_pnuint(uintptr_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load32_zero(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarvector128_pnuint(uintptr_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarvector128_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load32_zero(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarvector128_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarvector128_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load64_zero(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadscalarvector128_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadScalarVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pf32(float* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pf32(float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pnint(intptr_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pnint(intptr_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pnuint(uintptr_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pnuint(uintptr_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
    return dn2cpp_isa_vec<16>(wasm_v128_load(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadvector128_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadwideningvector128_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadWideningVector128");
    return dn2cpp_isa_vec<16>(wasm_i32x4_load16x4(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadwideningvector128_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadWideningVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadwideningvector128_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadWideningVector128");
    return dn2cpp_isa_vec<16>(wasm_i64x2_load32x2(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadwideningvector128_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadWideningVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadwideningvector128_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadWideningVector128");
    return dn2cpp_isa_vec<16>(wasm_i16x8_load8x8(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadwideningvector128_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadWideningVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadwideningvector128_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadWideningVector128");
    return dn2cpp_isa_vec<16>(wasm_u32x4_load16x4(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadwideningvector128_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadWideningVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadwideningvector128_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadWideningVector128");
    return dn2cpp_isa_vec<16>(wasm_u64x2_load32x2(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadwideningvector128_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadWideningVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadwideningvector128_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.LoadWideningVector128");
    return dn2cpp_isa_vec<16>(wasm_u16x8_load8x8(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_loadwideningvector128_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.LoadWideningVector128");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_max_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Max");
    return dn2cpp_isa_vec<16>(wasm_f32x4_max(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_max_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Max");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_max_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Max");
    return dn2cpp_isa_vec<16>(wasm_f64x2_max(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_max_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Max");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_max_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Max");
    return dn2cpp_isa_vec<16>(wasm_i16x8_max(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_max_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Max");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_max_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Max");
    return dn2cpp_isa_vec<16>(wasm_i32x4_max(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_max_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Max");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_max_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Max");
    return dn2cpp_isa_vec<16>(wasm_i8x16_max(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_max_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Max");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_max_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Max");
    return dn2cpp_isa_vec<16>(wasm_u16x8_max(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_max_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Max");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_max_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Max");
    return dn2cpp_isa_vec<16>(wasm_u32x4_max(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_max_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Max");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_max_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Max");
    return dn2cpp_isa_vec<16>(wasm_u8x16_max(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_max_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Max");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_min_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Min");
    return dn2cpp_isa_vec<16>(wasm_f32x4_min(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_min_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Min");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_min_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Min");
    return dn2cpp_isa_vec<16>(wasm_f64x2_min(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_min_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Min");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_min_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Min");
    return dn2cpp_isa_vec<16>(wasm_i16x8_min(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_min_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Min");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_min_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Min");
    return dn2cpp_isa_vec<16>(wasm_i32x4_min(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_min_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Min");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_min_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Min");
    return dn2cpp_isa_vec<16>(wasm_i8x16_min(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_min_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Min");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_min_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Min");
    return dn2cpp_isa_vec<16>(wasm_u16x8_min(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_min_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Min");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_min_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Min");
    return dn2cpp_isa_vec<16>(wasm_u32x4_min(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_min_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Min");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_min_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Min");
    return dn2cpp_isa_vec<16>(wasm_u8x16_min(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_min_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Min");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
    return dn2cpp_isa_vec<16>(wasm_f32x4_mul(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
    return dn2cpp_isa_vec<16>(wasm_f64x2_mul(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
    return dn2cpp_isa_vec<16>(wasm_i16x8_mul(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
    return dn2cpp_isa_vec<16>(wasm_i32x4_mul(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
    return dn2cpp_isa_vec<16>(wasm_i64x2_mul(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128nint_v128nint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
    return dn2cpp_isa_vec<16>(wasm_i32x4_mul(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128nint_v128nint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128nuint_v128nuint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
    return dn2cpp_isa_vec<16>(wasm_i32x4_mul(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128nuint_v128nuint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
    return dn2cpp_isa_vec<16>(wasm_i16x8_mul(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
    return dn2cpp_isa_vec<16>(wasm_i32x4_mul(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
    return dn2cpp_isa_vec<16>(wasm_i64x2_mul(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiply_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Multiply");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplyroundedsaturateq15_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyRoundedSaturateQ15");
    return dn2cpp_isa_vec<16>(wasm_i16x8_q15mulr_sat(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplyroundedsaturateq15_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyRoundedSaturateQ15");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideninglower_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningLower");
    return dn2cpp_isa_vec<16>(wasm_i32x4_extmul_low_i16x8(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideninglower_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideninglower_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningLower");
    return dn2cpp_isa_vec<16>(wasm_i64x2_extmul_low_i32x4(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideninglower_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideninglower_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningLower");
    return dn2cpp_isa_vec<16>(wasm_i16x8_extmul_low_i8x16(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideninglower_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideninglower_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningLower");
    return dn2cpp_isa_vec<16>(wasm_u32x4_extmul_low_u16x8(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideninglower_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideninglower_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningLower");
    return dn2cpp_isa_vec<16>(wasm_u64x2_extmul_low_u32x4(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideninglower_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideninglower_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningLower");
    return dn2cpp_isa_vec<16>(wasm_u16x8_extmul_low_u8x16(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideninglower_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideningupper_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningUpper");
    return dn2cpp_isa_vec<16>(wasm_i32x4_extmul_high_i16x8(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideningupper_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningUpper");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideningupper_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningUpper");
    return dn2cpp_isa_vec<16>(wasm_i64x2_extmul_high_i32x4(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideningupper_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningUpper");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideningupper_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningUpper");
    return dn2cpp_isa_vec<16>(wasm_i16x8_extmul_high_i8x16(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideningupper_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningUpper");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideningupper_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningUpper");
    return dn2cpp_isa_vec<16>(wasm_u32x4_extmul_high_u16x8(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideningupper_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningUpper");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideningupper_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningUpper");
    return dn2cpp_isa_vec<16>(wasm_u64x2_extmul_high_u32x4(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideningupper_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningUpper");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideningupper_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningUpper");
    return dn2cpp_isa_vec<16>(wasm_u16x8_extmul_high_u8x16(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_multiplywideningupper_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.MultiplyWideningUpper");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
    return dn2cpp_isa_vec<16>(wasm_f32x4_neg(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
    return dn2cpp_isa_vec<16>(wasm_f64x2_neg(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
    return dn2cpp_isa_vec<16>(wasm_i16x8_neg(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
    return dn2cpp_isa_vec<16>(wasm_i32x4_neg(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
    return dn2cpp_isa_vec<16>(wasm_i64x2_neg(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
    return dn2cpp_isa_vec<16>(wasm_i8x16_neg(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128nint(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
    return dn2cpp_isa_vec<16>(wasm_i32x4_neg(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128nint(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128nuint(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
    return dn2cpp_isa_vec<16>(wasm_i32x4_neg(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128nuint(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
    return dn2cpp_isa_vec<16>(wasm_i16x8_neg(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
    return dn2cpp_isa_vec<16>(wasm_i32x4_neg(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
    return dn2cpp_isa_vec<16>(wasm_i64x2_neg(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
    return dn2cpp_isa_vec<16>(wasm_i8x16_neg(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_negate_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Negate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
    return dn2cpp_isa_vec<16>(wasm_v128_not(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
    return dn2cpp_isa_vec<16>(wasm_v128_not(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
    return dn2cpp_isa_vec<16>(wasm_v128_not(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
    return dn2cpp_isa_vec<16>(wasm_v128_not(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
    return dn2cpp_isa_vec<16>(wasm_v128_not(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
    return dn2cpp_isa_vec<16>(wasm_v128_not(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128nint(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
    return dn2cpp_isa_vec<16>(wasm_v128_not(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128nint(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128nuint(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
    return dn2cpp_isa_vec<16>(wasm_v128_not(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128nuint(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
    return dn2cpp_isa_vec<16>(wasm_v128_not(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
    return dn2cpp_isa_vec<16>(wasm_v128_not(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
    return dn2cpp_isa_vec<16>(wasm_v128_not(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
    return dn2cpp_isa_vec<16>(wasm_v128_not(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_not_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Not");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
    return dn2cpp_isa_vec<16>(wasm_v128_or(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
    return dn2cpp_isa_vec<16>(wasm_v128_or(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
    return dn2cpp_isa_vec<16>(wasm_v128_or(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
    return dn2cpp_isa_vec<16>(wasm_v128_or(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
    return dn2cpp_isa_vec<16>(wasm_v128_or(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
    return dn2cpp_isa_vec<16>(wasm_v128_or(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128nint_v128nint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
    return dn2cpp_isa_vec<16>(wasm_v128_or(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128nint_v128nint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128nuint_v128nuint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
    return dn2cpp_isa_vec<16>(wasm_v128_or(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128nuint_v128nuint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
    return dn2cpp_isa_vec<16>(wasm_v128_or(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
    return dn2cpp_isa_vec<16>(wasm_v128_or(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
    return dn2cpp_isa_vec<16>(wasm_v128_or(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
    return dn2cpp_isa_vec<16>(wasm_v128_or(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_or_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Or");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_popcount_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.PopCount");
    return dn2cpp_isa_vec<16>(wasm_i8x16_popcnt(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_popcount_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.PopCount");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_pseudomax_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.PseudoMax");
    return dn2cpp_isa_vec<16>(wasm_f32x4_pmax(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_pseudomax_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.PseudoMax");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_pseudomax_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.PseudoMax");
    return dn2cpp_isa_vec<16>(wasm_f64x2_pmax(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_pseudomax_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.PseudoMax");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_pseudomin_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.PseudoMin");
    return dn2cpp_isa_vec<16>(wasm_f32x4_pmin(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_pseudomin_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.PseudoMin");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_pseudomin_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.PseudoMin");
    return dn2cpp_isa_vec<16>(wasm_f64x2_pmin(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_pseudomin_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.PseudoMin");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128f32_u8_f32(const Dn2CppVector128& a0, uint8_t a1, float a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<16>(wasm_f32x4_replace_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM, a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128f32_u8_f32(const Dn2CppVector128&, uint8_t, float)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128f64_u8_f64(const Dn2CppVector128& a0, uint8_t a1, double a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_vec<16>(wasm_f64x2_replace_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM, a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128f64_u8_f64(const Dn2CppVector128&, uint8_t, double)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128i16_u8_i32(const Dn2CppVector128& a0, uint8_t a1, int32_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<16>(wasm_i16x8_replace_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM, (int16_t)a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128i16_u8_i32(const Dn2CppVector128&, uint8_t, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128i32_u8_i32(const Dn2CppVector128& a0, uint8_t a1, int32_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<16>(wasm_i32x4_replace_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM, a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128i32_u8_i32(const Dn2CppVector128&, uint8_t, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128i64_u8_i64(const Dn2CppVector128& a0, uint8_t a1, int64_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_vec<16>(wasm_i64x2_replace_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM, a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128i64_u8_i64(const Dn2CppVector128&, uint8_t, int64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128i8_u8_i32(const Dn2CppVector128& a0, uint8_t a1, int32_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<16>(wasm_i8x16_replace_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM, (int8_t)a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128i8_u8_i32(const Dn2CppVector128&, uint8_t, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128nint_u8_nint(const Dn2CppVector128& a0, uint8_t a1, intptr_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<16>(wasm_i32x4_replace_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM, (int32_t)a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128nint_u8_nint(const Dn2CppVector128&, uint8_t, intptr_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128nuint_u8_nuint(const Dn2CppVector128& a0, uint8_t a1, uintptr_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<16>(wasm_u32x4_replace_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM, (uint32_t)a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128nuint_u8_nuint(const Dn2CppVector128&, uint8_t, uintptr_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128u16_u8_u32(const Dn2CppVector128& a0, uint8_t a1, uint32_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, dn2cpp_isa_vec<16>(wasm_u16x8_replace_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM, (uint16_t)a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128u16_u8_u32(const Dn2CppVector128&, uint8_t, uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128u32_u8_u32(const Dn2CppVector128& a0, uint8_t a1, uint32_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a1, dn2cpp_isa_vec<16>(wasm_u32x4_replace_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM, a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128u32_u8_u32(const Dn2CppVector128&, uint8_t, uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128u64_u8_u64(const Dn2CppVector128& a0, uint8_t a1, uint64_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, dn2cpp_isa_vec<16>(wasm_u64x2_replace_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM, a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128u64_u8_u64(const Dn2CppVector128&, uint8_t, uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128u8_u8_u32(const Dn2CppVector128& a0, uint8_t a1, uint32_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<16>(wasm_u8x16_replace_lane(dn2cpp_isa_bits<v128_t>(a0), DN2CPP_IMM, (uint8_t)a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_replacescalar_v128u8_u8_u32(const Dn2CppVector128&, uint8_t, uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ReplaceScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_roundtonearest_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.RoundToNearest");
    return dn2cpp_isa_vec<16>(wasm_f32x4_nearest(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_roundtonearest_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.RoundToNearest");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_roundtonearest_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.RoundToNearest");
    return dn2cpp_isa_vec<16>(wasm_f64x2_nearest(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_roundtonearest_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.RoundToNearest");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128i16_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
    return dn2cpp_isa_vec<16>(wasm_i16x8_shl(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128i16_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128i32_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
    return dn2cpp_isa_vec<16>(wasm_i32x4_shl(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128i32_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128i64_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
    return dn2cpp_isa_vec<16>(wasm_i64x2_shl(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128i64_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128i8_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
    return dn2cpp_isa_vec<16>(wasm_i8x16_shl(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128i8_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128nint_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
    return dn2cpp_isa_vec<16>(wasm_i32x4_shl(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128nint_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128nuint_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
    return dn2cpp_isa_vec<16>(wasm_i32x4_shl(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128nuint_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128u16_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
    return dn2cpp_isa_vec<16>(wasm_i16x8_shl(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128u16_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128u32_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
    return dn2cpp_isa_vec<16>(wasm_i32x4_shl(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128u32_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128u64_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
    return dn2cpp_isa_vec<16>(wasm_i64x2_shl(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128u64_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128u8_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
    return dn2cpp_isa_vec<16>(wasm_i8x16_shl(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftleft_v128u8_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftLeft");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128i16_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
    return dn2cpp_isa_vec<16>(wasm_i16x8_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128i16_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128i32_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
    return dn2cpp_isa_vec<16>(wasm_i32x4_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128i32_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128i64_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
    return dn2cpp_isa_vec<16>(wasm_i64x2_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128i64_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128i8_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
    return dn2cpp_isa_vec<16>(wasm_i8x16_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128i8_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128nint_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
    return dn2cpp_isa_vec<16>(wasm_i32x4_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128nint_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128nuint_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
    return dn2cpp_isa_vec<16>(wasm_i32x4_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128nuint_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128u16_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
    return dn2cpp_isa_vec<16>(wasm_i16x8_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128u16_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128u32_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
    return dn2cpp_isa_vec<16>(wasm_i32x4_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128u32_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128u64_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
    return dn2cpp_isa_vec<16>(wasm_i64x2_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128u64_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128u8_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
    return dn2cpp_isa_vec<16>(wasm_i8x16_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightarithmetic_v128u8_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128i16_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
    return dn2cpp_isa_vec<16>(wasm_u16x8_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128i16_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128i32_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
    return dn2cpp_isa_vec<16>(wasm_u32x4_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128i32_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128i64_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
    return dn2cpp_isa_vec<16>(wasm_u64x2_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128i64_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128i8_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
    return dn2cpp_isa_vec<16>(wasm_u8x16_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128i8_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128nint_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
    return dn2cpp_isa_vec<16>(wasm_u32x4_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128nint_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128nuint_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
    return dn2cpp_isa_vec<16>(wasm_u32x4_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128nuint_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128u16_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
    return dn2cpp_isa_vec<16>(wasm_u16x8_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128u16_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128u32_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
    return dn2cpp_isa_vec<16>(wasm_u32x4_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128u32_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128u64_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
    return dn2cpp_isa_vec<16>(wasm_u64x2_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128u64_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128u8_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
    return dn2cpp_isa_vec<16>(wasm_u8x16_shr(dn2cpp_isa_bits<v128_t>(a0), (uint32_t)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_shiftrightlogical_v128u8_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideninglower_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningLower");
    return dn2cpp_isa_vec<16>(wasm_i32x4_extend_low_i16x8(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideninglower_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideninglower_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningLower");
    return dn2cpp_isa_vec<16>(wasm_i64x2_extend_low_i32x4(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideninglower_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideninglower_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningLower");
    return dn2cpp_isa_vec<16>(wasm_i16x8_extend_low_i8x16(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideninglower_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideninglower_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningLower");
    return dn2cpp_isa_vec<16>(wasm_i32x4_extend_low_i16x8(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideninglower_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideninglower_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningLower");
    return dn2cpp_isa_vec<16>(wasm_i64x2_extend_low_i32x4(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideninglower_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideninglower_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningLower");
    return dn2cpp_isa_vec<16>(wasm_i16x8_extend_low_i8x16(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideninglower_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideningupper_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningUpper");
    return dn2cpp_isa_vec<16>(wasm_i32x4_extend_high_i16x8(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideningupper_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningUpper");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideningupper_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningUpper");
    return dn2cpp_isa_vec<16>(wasm_i64x2_extend_high_i32x4(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideningupper_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningUpper");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideningupper_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningUpper");
    return dn2cpp_isa_vec<16>(wasm_i16x8_extend_high_i8x16(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideningupper_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningUpper");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideningupper_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningUpper");
    return dn2cpp_isa_vec<16>(wasm_i32x4_extend_high_i16x8(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideningupper_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningUpper");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideningupper_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningUpper");
    return dn2cpp_isa_vec<16>(wasm_i64x2_extend_high_i32x4(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideningupper_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningUpper");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideningupper_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningUpper");
    return dn2cpp_isa_vec<16>(wasm_i16x8_extend_high_i8x16(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_signextendwideningupper_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.SignExtendWideningUpper");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_f32(float a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
    return dn2cpp_isa_vec<16>(wasm_f32x4_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_f32(float)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_f64(double a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
    return dn2cpp_isa_vec<16>(wasm_f64x2_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_f64(double)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_i16(int16_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
    return dn2cpp_isa_vec<16>(wasm_i16x8_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_i16(int16_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_i32(int32_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
    return dn2cpp_isa_vec<16>(wasm_i32x4_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_i32(int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_i64(int64_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
    return dn2cpp_isa_vec<16>(wasm_i64x2_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_i64(int64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_i8(int8_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
    return dn2cpp_isa_vec<16>(wasm_i8x16_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_i8(int8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_nint(intptr_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
    return dn2cpp_isa_vec<16>(wasm_i32x4_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_nint(intptr_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_nuint(uintptr_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
    return dn2cpp_isa_vec<16>(wasm_i32x4_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_nuint(uintptr_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_u16(uint16_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
    return dn2cpp_isa_vec<16>(wasm_i16x8_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_u16(uint16_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_u32(uint32_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
    return dn2cpp_isa_vec<16>(wasm_i32x4_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_u32(uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_u64(uint64_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
    return dn2cpp_isa_vec<16>(wasm_i64x2_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_u64(uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_u8(uint8_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
    return dn2cpp_isa_vec<16>(wasm_i8x16_splat(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_splat_u8(uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Splat");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_sqrt_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Sqrt");
    return dn2cpp_isa_vec<16>(wasm_f32x4_sqrt(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_sqrt_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Sqrt");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_sqrt_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Sqrt");
    return dn2cpp_isa_vec<16>(wasm_f64x2_sqrt(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_sqrt_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Sqrt");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pf32_v128f32(float* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
    wasm_v128_store(a0, dn2cpp_isa_bits<v128_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pf32_v128f32(float*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pf64_v128f64(double* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
    wasm_v128_store(a0, dn2cpp_isa_bits<v128_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pf64_v128f64(double*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pi16_v128i16(int16_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
    wasm_v128_store(a0, dn2cpp_isa_bits<v128_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pi16_v128i16(int16_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pi32_v128i32(int32_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
    wasm_v128_store(a0, dn2cpp_isa_bits<v128_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pi32_v128i32(int32_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pi64_v128i64(int64_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
    wasm_v128_store(a0, dn2cpp_isa_bits<v128_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pi64_v128i64(int64_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pi8_v128i8(int8_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
    wasm_v128_store(a0, dn2cpp_isa_bits<v128_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pi8_v128i8(int8_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pnint_v128nint(intptr_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
    wasm_v128_store(a0, dn2cpp_isa_bits<v128_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pnint_v128nint(intptr_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pnuint_v128nuint(uintptr_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
    wasm_v128_store(a0, dn2cpp_isa_bits<v128_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pnuint_v128nuint(uintptr_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pu16_v128u16(uint16_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
    wasm_v128_store(a0, dn2cpp_isa_bits<v128_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pu16_v128u16(uint16_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pu32_v128u32(uint32_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
    wasm_v128_store(a0, dn2cpp_isa_bits<v128_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pu32_v128u32(uint32_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pu64_v128u64(uint64_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
    wasm_v128_store(a0, dn2cpp_isa_bits<v128_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pu64_v128u64(uint64_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pu8_v128u8(uint8_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
    wasm_v128_store(a0, dn2cpp_isa_bits<v128_t>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_store_pu8_v128u8(uint8_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Store");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pf32_v128f32_u8(float* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, wasm_v128_store32_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pf32_v128f32_u8(float*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pf64_v128f64_u8(double* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, wasm_v128_store64_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pf64_v128f64_u8(double*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pi16_v128i16_u8(int16_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a2, wasm_v128_store16_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pi16_v128i16_u8(int16_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pi32_v128i32_u8(int32_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, wasm_v128_store32_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pi32_v128i32_u8(int32_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pi64_v128i64_u8(int64_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, wasm_v128_store64_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pi64_v128i64_u8(int64_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pi8_v128i8_u8(int8_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, wasm_v128_store8_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pi8_v128i8_u8(int8_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pnint_v128nint_u8(intptr_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, wasm_v128_store32_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pnint_v128nint_u8(intptr_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pnuint_v128nuint_u8(uintptr_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, wasm_v128_store32_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pnuint_v128nuint_u8(uintptr_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pu16_v128u16_u8(uint16_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a2, wasm_v128_store16_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pu16_v128u16_u8(uint16_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pu32_v128u32_u8(uint32_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 4, a2, wasm_v128_store32_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pu32_v128u32_u8(uint32_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pu64_v128u64_u8(uint64_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, wasm_v128_store64_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pu64_v128u64_u8(uint64_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pu8_v128u8_u8(uint8_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, wasm_v128_store8_lane(a0, dn2cpp_isa_bits<v128_t>(a1), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_wasm_packedsimd_storeselectedscalar_pu8_v128u8_u8(uint8_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.StoreSelectedScalar");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
    return dn2cpp_isa_vec<16>(wasm_f32x4_sub(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
    return dn2cpp_isa_vec<16>(wasm_f64x2_sub(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
    return dn2cpp_isa_vec<16>(wasm_i16x8_sub(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
    return dn2cpp_isa_vec<16>(wasm_i32x4_sub(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
    return dn2cpp_isa_vec<16>(wasm_i64x2_sub(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
    return dn2cpp_isa_vec<16>(wasm_i8x16_sub(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128nint_v128nint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
    return dn2cpp_isa_vec<16>(wasm_i32x4_sub(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128nint_v128nint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128nuint_v128nuint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
    return dn2cpp_isa_vec<16>(wasm_i32x4_sub(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128nuint_v128nuint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
    return dn2cpp_isa_vec<16>(wasm_i16x8_sub(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
    return dn2cpp_isa_vec<16>(wasm_i32x4_sub(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
    return dn2cpp_isa_vec<16>(wasm_i64x2_sub(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
    return dn2cpp_isa_vec<16>(wasm_i8x16_sub(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtract_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Subtract");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtractsaturate_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.SubtractSaturate");
    return dn2cpp_isa_vec<16>(wasm_i16x8_sub_sat(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtractsaturate_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtractsaturate_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.SubtractSaturate");
    return dn2cpp_isa_vec<16>(wasm_i8x16_sub_sat(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtractsaturate_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtractsaturate_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.SubtractSaturate");
    return dn2cpp_isa_vec<16>(wasm_u16x8_sub_sat(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtractsaturate_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtractsaturate_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.SubtractSaturate");
    return dn2cpp_isa_vec<16>(wasm_u8x16_sub_sat(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_subtractsaturate_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_swizzle_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Swizzle");
    return dn2cpp_isa_vec<16>(wasm_i8x16_swizzle(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_swizzle_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Swizzle");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_swizzle_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Swizzle");
    return dn2cpp_isa_vec<16>(wasm_i8x16_swizzle(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_swizzle_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Swizzle");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_truncate_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Truncate");
    return dn2cpp_isa_vec<16>(wasm_f32x4_trunc(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_truncate_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Truncate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_truncate_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Truncate");
    return dn2cpp_isa_vec<16>(wasm_f64x2_trunc(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_truncate_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Truncate");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
    return dn2cpp_isa_vec<16>(wasm_v128_xor(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
    return dn2cpp_isa_vec<16>(wasm_v128_xor(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
    return dn2cpp_isa_vec<16>(wasm_v128_xor(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
    return dn2cpp_isa_vec<16>(wasm_v128_xor(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
    return dn2cpp_isa_vec<16>(wasm_v128_xor(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
    return dn2cpp_isa_vec<16>(wasm_v128_xor(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128nint_v128nint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
    return dn2cpp_isa_vec<16>(wasm_v128_xor(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128nint_v128nint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128nuint_v128nuint(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
    return dn2cpp_isa_vec<16>(wasm_v128_xor(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128nuint_v128nuint(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
    return dn2cpp_isa_vec<16>(wasm_v128_xor(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
    return dn2cpp_isa_vec<16>(wasm_v128_xor(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
    return dn2cpp_isa_vec<16>(wasm_v128_xor(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
    return dn2cpp_isa_vec<16>(wasm_v128_xor(dn2cpp_isa_bits<v128_t>(a0), dn2cpp_isa_bits<v128_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_xor_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.Xor");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideninglower_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningLower");
    return dn2cpp_isa_vec<16>(wasm_u32x4_extend_low_u16x8(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideninglower_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideninglower_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningLower");
    return dn2cpp_isa_vec<16>(wasm_u64x2_extend_low_u32x4(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideninglower_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideninglower_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningLower");
    return dn2cpp_isa_vec<16>(wasm_u16x8_extend_low_u8x16(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideninglower_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideninglower_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningLower");
    return dn2cpp_isa_vec<16>(wasm_u32x4_extend_low_u16x8(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideninglower_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideninglower_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningLower");
    return dn2cpp_isa_vec<16>(wasm_u64x2_extend_low_u32x4(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideninglower_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideninglower_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningLower");
    return dn2cpp_isa_vec<16>(wasm_u16x8_extend_low_u8x16(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideninglower_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningLower");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideningupper_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningUpper");
    return dn2cpp_isa_vec<16>(wasm_u32x4_extend_high_u16x8(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideningupper_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningUpper");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideningupper_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningUpper");
    return dn2cpp_isa_vec<16>(wasm_u64x2_extend_high_u32x4(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideningupper_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningUpper");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideningupper_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningUpper");
    return dn2cpp_isa_vec<16>(wasm_u16x8_extend_high_u8x16(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideningupper_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningUpper");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideningupper_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningUpper");
    return dn2cpp_isa_vec<16>(wasm_u32x4_extend_high_u16x8(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideningupper_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningUpper");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideningupper_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningUpper");
    return dn2cpp_isa_vec<16>(wasm_u64x2_extend_high_u32x4(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideningupper_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningUpper");
}
#endif

#if DN2CPP_TARGET_WASM32 && defined(__wasm_simd128__)
DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideningupper_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Wasm_PackedSimd, "System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningUpper");
    return dn2cpp_isa_vec<16>(wasm_u16x8_extend_high_u8x16(dn2cpp_isa_bits<v128_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_wasm_packedsimd_zeroextendwideningupper_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Wasm.PackedSimd.ZeroExtendWideningUpper");
}
#endif
