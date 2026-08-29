#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.Arm.Aes: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+aes") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_aes_decrypt_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Aes, "System.Runtime.Intrinsics.Arm.Aes.Decrypt");
    return dn2cpp_isa_vec<16>(vaesdq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_aes_decrypt_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Aes.Decrypt");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+aes") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_aes_encrypt_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Aes, "System.Runtime.Intrinsics.Arm.Aes.Encrypt");
    return dn2cpp_isa_vec<16>(vaeseq_u8(dn2cpp_isa_bits<uint8x16_t>(a0), dn2cpp_isa_bits<uint8x16_t>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_aes_encrypt_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Aes.Encrypt");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+aes") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_aes_inversemixcolumns_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Aes, "System.Runtime.Intrinsics.Arm.Aes.InverseMixColumns");
    return dn2cpp_isa_vec<16>(vaesimcq_u8(dn2cpp_isa_bits<uint8x16_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_aes_inversemixcolumns_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Aes.InverseMixColumns");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+aes") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_aes_mixcolumns_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Aes, "System.Runtime.Intrinsics.Arm.Aes.MixColumns");
    return dn2cpp_isa_vec<16>(vaesmcq_u8(dn2cpp_isa_bits<uint8x16_t>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_aes_mixcolumns_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Aes.MixColumns");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+aes") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_aes_polynomialmultiplywideninglower_v64i64_v64i64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Aes, "System.Runtime.Intrinsics.Arm.Aes.PolynomialMultiplyWideningLower");
    return dn2cpp_isa_vec<16>(vmull_p64((poly64_t)vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a0), 0), (poly64_t)vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a1), 0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_aes_polynomialmultiplywideninglower_v64i64_v64i64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Aes.PolynomialMultiplyWideningLower");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+aes") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_aes_polynomialmultiplywideninglower_v64u64_v64u64(const Dn2CppVector64& a0, const Dn2CppVector64& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Aes, "System.Runtime.Intrinsics.Arm.Aes.PolynomialMultiplyWideningLower");
    return dn2cpp_isa_vec<16>(vmull_p64((poly64_t)vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a0), 0), (poly64_t)vget_lane_u64(dn2cpp_isa_bits<uint64x1_t>(a1), 0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_aes_polynomialmultiplywideninglower_v64u64_v64u64(const Dn2CppVector64&, const Dn2CppVector64&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Aes.PolynomialMultiplyWideningLower");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+aes") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_aes_polynomialmultiplywideningupper_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Aes, "System.Runtime.Intrinsics.Arm.Aes.PolynomialMultiplyWideningUpper");
    return dn2cpp_isa_vec<16>(vmull_high_p64(vreinterpretq_p64_u64(dn2cpp_isa_bits<uint64x2_t>(a0)), vreinterpretq_p64_u64(dn2cpp_isa_bits<uint64x2_t>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_aes_polynomialmultiplywideningupper_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Aes.PolynomialMultiplyWideningUpper");
}
#endif

#if DN2CPP_TARGET_ARM64
DN2CPP_ISA_TARGET("+aes") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_aes_polynomialmultiplywideningupper_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_Arm_Aes, "System.Runtime.Intrinsics.Arm.Aes.PolynomialMultiplyWideningUpper");
    return dn2cpp_isa_vec<16>(vmull_high_p64(vreinterpretq_p64_u64(dn2cpp_isa_bits<uint64x2_t>(a0)), vreinterpretq_p64_u64(dn2cpp_isa_bits<uint64x2_t>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_arm_aes_polynomialmultiplywideningupper_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.Arm.Aes.PolynomialMultiplyWideningUpper");
}
#endif
