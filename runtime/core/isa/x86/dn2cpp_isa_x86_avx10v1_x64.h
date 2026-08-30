#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx10v1+X64: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_x64_convertscalartovector128double_v128f64_i64_u8(const Dn2CppVector128& a0, int64_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_X64, "System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertScalarToVector128Double");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_x64_convertscalartovector128double_v128f64_i64_u8(const Dn2CppVector128&, int64_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertScalarToVector128Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_x64_convertscalartovector128double_v128f64_u64(const Dn2CppVector128& a0, uint64_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_X64, "System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertScalarToVector128Double");
    return dn2cpp_isa_vec<16>(_mm_cvtu64_sd(dn2cpp_isa_bits<__m128d>(a0), a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_x64_convertscalartovector128double_v128f64_u64(const Dn2CppVector128&, uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertScalarToVector128Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_x64_convertscalartovector128double_v128f64_u64_u8(const Dn2CppVector128& a0, uint64_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_X64, "System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertScalarToVector128Double");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_sd(dn2cpp_isa_bits<__m128d>(a0), a1, (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_x64_convertscalartovector128double_v128f64_u64_u8(const Dn2CppVector128&, uint64_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertScalarToVector128Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_x64_convertscalartovector128single_v128f32_i64_u8(const Dn2CppVector128& a0, int64_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_X64, "System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertScalarToVector128Single");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_cvt_roundsi64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_x64_convertscalartovector128single_v128f32_i64_u8(const Dn2CppVector128&, int64_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertScalarToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_x64_convertscalartovector128single_v128f32_u64(const Dn2CppVector128& a0, uint64_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_X64, "System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertScalarToVector128Single");
    return dn2cpp_isa_vec<16>(_mm_cvtu64_ss(dn2cpp_isa_bits<__m128>(a0), a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_x64_convertscalartovector128single_v128f32_u64(const Dn2CppVector128&, uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertScalarToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_x64_convertscalartovector128single_v128f32_u64_u8(const Dn2CppVector128& a0, uint64_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_X64, "System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertScalarToVector128Single");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_cvt_roundu64_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_x64_convertscalartovector128single_v128f32_u64_u8(const Dn2CppVector128&, uint64_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertScalarToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_avx10v1_x64_converttoint64_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_X64, "System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertToInt64");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, _mm_cvt_roundss_si64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(1, _mm_cvt_roundss_si64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(2, _mm_cvt_roundss_si64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(3, _mm_cvt_roundss_si64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(4, _mm_cvt_roundss_si64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(5, _mm_cvt_roundss_si64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(6, _mm_cvt_roundss_si64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(7, _mm_cvt_roundss_si64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(8, _mm_cvt_roundss_si64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(9, _mm_cvt_roundss_si64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(10, _mm_cvt_roundss_si64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(11, _mm_cvt_roundss_si64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_avx10v1_x64_converttoint64_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertToInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_avx10v1_x64_converttoint64_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_X64, "System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertToInt64");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, _mm_cvt_roundsd_si64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(1, _mm_cvt_roundsd_si64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(2, _mm_cvt_roundsd_si64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(3, _mm_cvt_roundsd_si64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(4, _mm_cvt_roundsd_si64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(5, _mm_cvt_roundsd_si64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(6, _mm_cvt_roundsd_si64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(7, _mm_cvt_roundsd_si64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(8, _mm_cvt_roundsd_si64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(9, _mm_cvt_roundsd_si64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(10, _mm_cvt_roundsd_si64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(11, _mm_cvt_roundsd_si64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_avx10v1_x64_converttoint64_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertToInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_avx10v1_x64_converttouint64_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_X64, "System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertToUInt64");
    return _mm_cvtss_u64(dn2cpp_isa_bits<__m128>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_avx10v1_x64_converttouint64_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertToUInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_avx10v1_x64_converttouint64_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_X64, "System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertToUInt64");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, _mm_cvt_roundss_u64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(1, _mm_cvt_roundss_u64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(2, _mm_cvt_roundss_u64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(3, _mm_cvt_roundss_u64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(4, _mm_cvt_roundss_u64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(5, _mm_cvt_roundss_u64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(6, _mm_cvt_roundss_u64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(7, _mm_cvt_roundss_u64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(8, _mm_cvt_roundss_u64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(9, _mm_cvt_roundss_u64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(10, _mm_cvt_roundss_u64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(11, _mm_cvt_roundss_u64(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_avx10v1_x64_converttouint64_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertToUInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_avx10v1_x64_converttouint64_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_X64, "System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertToUInt64");
    return _mm_cvtsd_u64(dn2cpp_isa_bits<__m128d>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_avx10v1_x64_converttouint64_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertToUInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_avx10v1_x64_converttouint64_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_X64, "System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertToUInt64");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, _mm_cvt_roundsd_u64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(1, _mm_cvt_roundsd_u64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(2, _mm_cvt_roundsd_u64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(3, _mm_cvt_roundsd_u64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(4, _mm_cvt_roundsd_u64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(5, _mm_cvt_roundsd_u64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(6, _mm_cvt_roundsd_u64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(7, _mm_cvt_roundsd_u64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(8, _mm_cvt_roundsd_u64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(9, _mm_cvt_roundsd_u64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(10, _mm_cvt_roundsd_u64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(11, _mm_cvt_roundsd_u64(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_avx10v1_x64_converttouint64_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertToUInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_avx10v1_x64_converttouint64withtruncation_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_X64, "System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertToUInt64WithTruncation");
    return _mm_cvttss_u64(dn2cpp_isa_bits<__m128>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_avx10v1_x64_converttouint64withtruncation_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertToUInt64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-256") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_avx10v1_x64_converttouint64withtruncation_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_X64, "System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertToUInt64WithTruncation");
    return _mm_cvttsd_u64(dn2cpp_isa_bits<__m128d>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_avx10v1_x64_converttouint64withtruncation_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+X64.ConvertToUInt64WithTruncation");
}
#endif
