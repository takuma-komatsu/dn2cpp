#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx512F: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_abs_v512i32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Abs");
    return dn2cpp_isa_vec<64>(_mm512_abs_epi32(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_abs_v512i32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Abs");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_abs_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Abs");
    return dn2cpp_isa_vec<64>(_mm512_abs_epi64(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_abs_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Abs");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_add_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Add");
    return dn2cpp_isa_vec<64>(_mm512_add_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_add_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_add_v512f32_v512f32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Add");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_add_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_add_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_add_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_add_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_add_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_add_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_add_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_add_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_add_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_add_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_add_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_add_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_add_v512f32_v512f32_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_add_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Add");
    return dn2cpp_isa_vec<64>(_mm512_add_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_add_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_add_v512f64_v512f64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Add");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_add_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_add_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_add_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_add_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_add_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_add_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_add_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_add_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_add_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_add_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_add_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_add_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_add_v512f64_v512f64_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_add_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Add");
    return dn2cpp_isa_vec<64>(_mm512_add_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_add_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_add_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Add");
    return dn2cpp_isa_vec<64>(_mm512_add_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_add_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_add_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Add");
    return dn2cpp_isa_vec<64>(_mm512_add_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_add_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_add_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Add");
    return dn2cpp_isa_vec<64>(_mm512_add_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_add_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_addscalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.AddScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_add_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_addscalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.AddScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_addscalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.AddScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_add_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_addscalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.AddScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_alignright32_v512i32_v512i32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.AlignRight32");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_alignr_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), DN2CPP_IMM & 15)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_alignright32_v512i32_v512i32_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.AlignRight32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_alignright32_v512u32_v512u32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.AlignRight32");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_alignr_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), DN2CPP_IMM & 15)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_alignright32_v512u32_v512u32_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.AlignRight32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_alignright64_v512i64_v512i64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.AlignRight64");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_alignr_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), DN2CPP_IMM & 7)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_alignright64_v512i64_v512i64_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.AlignRight64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_alignright64_v512u64_v512u64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.AlignRight64");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_alignr_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), DN2CPP_IMM & 7)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_alignright64_v512u64_v512u64_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.AlignRight64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_and_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.And");
    return dn2cpp_isa_vec<64>(_mm512_and_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_and_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_and_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.And");
    return dn2cpp_isa_vec<64>(_mm512_and_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_and_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_and_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.And");
    return dn2cpp_isa_vec<64>(_mm512_and_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_and_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_and_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.And");
    return dn2cpp_isa_vec<64>(_mm512_and_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_and_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_and_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.And");
    return dn2cpp_isa_vec<64>(_mm512_and_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_and_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_and_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.And");
    return dn2cpp_isa_vec<64>(_mm512_and_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_and_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_and_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.And");
    return dn2cpp_isa_vec<64>(_mm512_and_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_and_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_and_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.And");
    return dn2cpp_isa_vec<64>(_mm512_and_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_and_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_andnot_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.AndNot");
    return dn2cpp_isa_vec<64>(_mm512_andnot_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_andnot_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_andnot_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.AndNot");
    return dn2cpp_isa_vec<64>(_mm512_andnot_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_andnot_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_andnot_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.AndNot");
    return dn2cpp_isa_vec<64>(_mm512_andnot_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_andnot_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_andnot_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.AndNot");
    return dn2cpp_isa_vec<64>(_mm512_andnot_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_andnot_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_andnot_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.AndNot");
    return dn2cpp_isa_vec<64>(_mm512_andnot_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_andnot_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_andnot_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.AndNot");
    return dn2cpp_isa_vec<64>(_mm512_andnot_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_andnot_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_andnot_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.AndNot");
    return dn2cpp_isa_vec<64>(_mm512_andnot_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_andnot_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_andnot_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.AndNot");
    return dn2cpp_isa_vec<64>(_mm512_andnot_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_andnot_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_blendvariable_v512f32_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.BlendVariable");
    return dn2cpp_isa_vec<64>(_mm512_mask_blend_ps(_mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a2)), dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_blendvariable_v512f32_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_blendvariable_v512f64_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.BlendVariable");
    return dn2cpp_isa_vec<64>(_mm512_mask_blend_pd(_mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a2)), dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_blendvariable_v512f64_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_blendvariable_v512i32_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.BlendVariable");
    return dn2cpp_isa_vec<64>(_mm512_mask_blend_epi32(_mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a2)), dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_blendvariable_v512i32_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_blendvariable_v512i64_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.BlendVariable");
    return dn2cpp_isa_vec<64>(_mm512_mask_blend_epi64(_mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a2)), dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_blendvariable_v512i64_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_blendvariable_v512u32_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.BlendVariable");
    return dn2cpp_isa_vec<64>(_mm512_mask_blend_epi32(_mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a2)), dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_blendvariable_v512u32_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_blendvariable_v512u64_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.BlendVariable");
    return dn2cpp_isa_vec<64>(_mm512_mask_blend_epi64(_mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a2)), dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_blendvariable_v512u64_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastscalartovector512_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.BroadcastScalarToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcastss_ps(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastscalartovector512_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.BroadcastScalarToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastscalartovector512_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.BroadcastScalarToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcastsd_pd(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastscalartovector512_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.BroadcastScalarToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastscalartovector512_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.BroadcastScalarToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcastd_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastscalartovector512_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.BroadcastScalarToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastscalartovector512_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.BroadcastScalarToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcastq_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastscalartovector512_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.BroadcastScalarToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastscalartovector512_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.BroadcastScalarToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcastd_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastscalartovector512_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.BroadcastScalarToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastscalartovector512_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.BroadcastScalarToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcastq_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastscalartovector512_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.BroadcastScalarToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastvector128tovector512_pf32(float* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.BroadcastVector128ToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_f32x4(_mm_loadu_ps(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastvector128tovector512_pf32(float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.BroadcastVector128ToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastvector128tovector512_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.BroadcastVector128ToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_i32x4(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastvector128tovector512_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.BroadcastVector128ToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastvector128tovector512_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.BroadcastVector128ToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_i32x4(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastvector128tovector512_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.BroadcastVector128ToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastvector256tovector512_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.BroadcastVector256ToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_f64x4(_mm256_loadu_pd(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastvector256tovector512_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.BroadcastVector256ToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastvector256tovector512_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.BroadcastVector256ToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_i64x4(_mm256_loadu_si256((const __m256i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastvector256tovector512_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.BroadcastVector256ToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastvector256tovector512_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.BroadcastVector256ToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_i64x4(_mm256_loadu_si256((const __m256i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_broadcastvector256tovector512_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.BroadcastVector256ToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compare_v512f32_v512f32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Compare");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmp_ps_mask(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compare_v512f32_v512f32_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Compare");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compare_v512f64_v512f64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Compare");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmp_pd_mask(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compare_v512f64_v512f64_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Compare");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareequal_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmp_ps_mask(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), _CMP_EQ_OQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareequal_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareequal_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmp_pd_mask(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), _CMP_EQ_OQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareequal_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareequal_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmpeq_epi32_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareequal_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareequal_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmpeq_epi64_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareequal_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareequal_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmpeq_epu32_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareequal_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareequal_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmpeq_epu64_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareequal_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthan_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmp_ps_mask(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), _CMP_GT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthan_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthan_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmp_pd_mask(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), _CMP_GT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthan_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthan_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmpgt_epi32_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthan_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthan_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmpgt_epi64_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthan_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthan_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmpgt_epu32_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthan_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthan_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmpgt_epu64_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthan_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthanorequal_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmp_ps_mask(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), _CMP_GE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthanorequal_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthanorequal_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmp_pd_mask(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), _CMP_GE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthanorequal_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthanorequal_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmpge_epi32_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthanorequal_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthanorequal_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmpge_epi64_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthanorequal_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthanorequal_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmpge_epu32_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthanorequal_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthanorequal_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmpge_epu64_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparegreaterthanorequal_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthan_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareLessThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmp_ps_mask(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), _CMP_LT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthan_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthan_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareLessThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmp_pd_mask(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), _CMP_LT_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthan_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthan_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareLessThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmplt_epi32_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthan_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthan_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareLessThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmplt_epi64_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthan_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthan_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareLessThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmplt_epu32_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthan_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthan_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareLessThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmplt_epu64_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthan_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthanorequal_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmp_ps_mask(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), _CMP_LE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthanorequal_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthanorequal_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmp_pd_mask(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), _CMP_LE_OS)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthanorequal_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthanorequal_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmple_epi32_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthanorequal_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthanorequal_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmple_epi64_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthanorequal_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthanorequal_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmple_epu32_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthanorequal_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthanorequal_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmple_epu64_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparelessthanorequal_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotequal_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareNotEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmp_ps_mask(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), _CMP_NEQ_UQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotequal_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotequal_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareNotEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmp_pd_mask(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), _CMP_NEQ_UQ)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotequal_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotequal_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareNotEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmpneq_epi32_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotequal_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotequal_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareNotEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmpneq_epi64_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotequal_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotequal_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareNotEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmpneq_epu32_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotequal_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotequal_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareNotEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmpneq_epu64_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotequal_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotgreaterthan_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareNotGreaterThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmp_ps_mask(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), _CMP_NGT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotgreaterthan_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareNotGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotgreaterthan_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareNotGreaterThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmp_pd_mask(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), _CMP_NGT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotgreaterthan_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareNotGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotgreaterthanorequal_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareNotGreaterThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmp_ps_mask(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), _CMP_NGE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotgreaterthanorequal_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareNotGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotgreaterthanorequal_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareNotGreaterThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmp_pd_mask(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), _CMP_NGE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotgreaterthanorequal_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareNotGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotlessthan_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareNotLessThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmp_ps_mask(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), _CMP_NLT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotlessthan_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareNotLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotlessthan_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareNotLessThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmp_pd_mask(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), _CMP_NLT_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotlessthan_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareNotLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotlessthanorequal_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareNotLessThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmp_ps_mask(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), _CMP_NLE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotlessthanorequal_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareNotLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotlessthanorequal_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareNotLessThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmp_pd_mask(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), _CMP_NLE_US)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_comparenotlessthanorequal_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareNotLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareordered_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareOrdered");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmp_ps_mask(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), _CMP_ORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareordered_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareOrdered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareordered_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareOrdered");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmp_pd_mask(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), _CMP_ORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareordered_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareOrdered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareunordered_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareUnordered");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_cmp_ps_mask(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), _CMP_UNORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareunordered_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareUnordered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareunordered_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompareUnordered");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_cmp_pd_mask(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), _CMP_UNORD_Q)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compareunordered_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompareUnordered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compress_v512f32_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Compress");
    return dn2cpp_isa_vec<64>(_mm512_mask_compress_ps(dn2cpp_isa_bits<__m512>(a0), _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compress_v512f32_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compress_v512f64_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Compress");
    return dn2cpp_isa_vec<64>(_mm512_mask_compress_pd(dn2cpp_isa_bits<__m512d>(a0), _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compress_v512f64_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compress_v512i32_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Compress");
    return dn2cpp_isa_vec<64>(_mm512_mask_compress_epi32(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compress_v512i32_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compress_v512i64_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Compress");
    return dn2cpp_isa_vec<64>(_mm512_mask_compress_epi64(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compress_v512i64_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compress_v512u32_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Compress");
    return dn2cpp_isa_vec<64>(_mm512_mask_compress_epi32(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compress_v512u32_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compress_v512u64_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Compress");
    return dn2cpp_isa_vec<64>(_mm512_mask_compress_epi64(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_compress_v512u64_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_compressstore_pf32_v512f32_v512f32(float* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompressStore");
    _mm512_mask_compressstoreu_ps((void*)a0, _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_compressstore_pf32_v512f32_v512f32(float*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_compressstore_pf64_v512f64_v512f64(double* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompressStore");
    _mm512_mask_compressstoreu_pd((void*)a0, _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512d>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_compressstore_pf64_v512f64_v512f64(double*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_compressstore_pi32_v512i32_v512i32(int32_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompressStore");
    _mm512_mask_compressstoreu_epi32((void*)a0, _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_compressstore_pi32_v512i32_v512i32(int32_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_compressstore_pi64_v512i64_v512i64(int64_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompressStore");
    _mm512_mask_compressstoreu_epi64((void*)a0, _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_compressstore_pi64_v512i64_v512i64(int64_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_compressstore_pu32_v512u32_v512u32(uint32_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompressStore");
    _mm512_mask_compressstoreu_epi32((void*)a0, _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_compressstore_pu32_v512u32_v512u32(uint32_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_compressstore_pu64_v512u64_v512u64(uint64_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.CompressStore");
    _mm512_mask_compressstoreu_epi64((void*)a0, _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_compressstore_pu64_v512u64_v512u64(uint64_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_convertscalartovector128double_v128f64_u32(const Dn2CppVector128& a0, uint32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertScalarToVector128Double");
    return dn2cpp_isa_vec<16>(_mm_cvtu32_sd(dn2cpp_isa_bits<__m128d>(a0), a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_convertscalartovector128double_v128f64_u32(const Dn2CppVector128&, uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertScalarToVector128Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_convertscalartovector128single_v128f32_i32_u8(const Dn2CppVector128& a0, int32_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertScalarToVector128Single");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_cvt_roundi32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_convertscalartovector128single_v128f32_i32_u8(const Dn2CppVector128&, int32_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertScalarToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_convertscalartovector128single_v128f32_u32(const Dn2CppVector128& a0, uint32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertScalarToVector128Single");
    return dn2cpp_isa_vec<16>(_mm_cvtu32_ss(dn2cpp_isa_bits<__m128>(a0), a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_convertscalartovector128single_v128f32_u32(const Dn2CppVector128&, uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertScalarToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_convertscalartovector128single_v128f32_u32_u8(const Dn2CppVector128& a0, uint32_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertScalarToVector128Single");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_cvt_roundu32_ss(dn2cpp_isa_bits<__m128>(a0), a1, (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_convertscalartovector128single_v128f32_u32_u8(const Dn2CppVector128&, uint32_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertScalarToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_convertscalartovector128single_v128f32_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertScalarToVector128Single");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_cvt_roundsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_convertscalartovector128single_v128f32_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertScalarToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512f_converttoint32_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToInt32");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(1, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(2, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(3, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(4, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(5, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(6, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(7, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(8, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(9, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(10, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(11, _mm_cvt_roundss_si32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512f_converttoint32_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512f_converttoint32_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToInt32");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(1, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(2, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(3, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(4, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(5, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(6, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(7, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(8, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(9, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(10, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(11, _mm_cvt_roundsd_si32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512f_converttoint32_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx512f_converttouint32_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToUInt32");
    return _mm_cvtss_u32(dn2cpp_isa_bits<__m128>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx512f_converttouint32_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToUInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx512f_converttouint32_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToUInt32");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(1, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(2, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(3, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(4, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(5, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(6, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(7, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(8, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(9, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(10, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(11, _mm_cvt_roundss_u32(dn2cpp_isa_bits<__m128>(a0), (DN2CPP_IMM & 3) | 8)) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx512f_converttouint32_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToUInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx512f_converttouint32_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToUInt32");
    return _mm_cvtsd_u32(dn2cpp_isa_bits<__m128d>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx512f_converttouint32_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToUInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx512f_converttouint32_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToUInt32");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(1, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(2, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(3, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(4, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(5, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(6, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(7, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(8, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(9, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(10, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) DN2CPP_ISA_IMM_CASE(11, _mm_cvt_roundsd_u32(dn2cpp_isa_bits<__m128d>(a0), (DN2CPP_IMM & 3) | 8)) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx512f_converttouint32_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToUInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx512f_converttouint32withtruncation_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToUInt32WithTruncation");
    return _mm_cvttss_u32(dn2cpp_isa_bits<__m128>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx512f_converttouint32withtruncation_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToUInt32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx512f_converttouint32withtruncation_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToUInt32WithTruncation");
    return _mm_cvttsd_u32(dn2cpp_isa_bits<__m128d>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx512f_converttouint32withtruncation_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToUInt32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128byte_v512i32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm512_cvtepi32_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128byte_v512i32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128byte_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm512_cvtepi64_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128byte_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128byte_v512u32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm512_cvtepi32_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128byte_v512u32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128byte_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128Byte");
    return dn2cpp_isa_vec<16>(_mm512_cvtepi64_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128byte_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128bytewithsaturation_v512u32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128ByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm512_cvtusepi32_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128bytewithsaturation_v512u32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128ByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128bytewithsaturation_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128ByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm512_cvtusepi64_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128bytewithsaturation_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128ByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128int16_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm512_cvtepi64_epi16(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128int16_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128int16_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128Int16");
    return dn2cpp_isa_vec<16>(_mm512_cvtepi64_epi16(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128int16_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128int16withsaturation_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128Int16WithSaturation");
    return dn2cpp_isa_vec<16>(_mm512_cvtsepi64_epi16(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128int16withsaturation_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128Int16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128sbyte_v512i32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm512_cvtepi32_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128sbyte_v512i32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128sbyte_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm512_cvtepi64_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128sbyte_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128sbyte_v512u32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm512_cvtepi32_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128sbyte_v512u32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128sbyte_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128SByte");
    return dn2cpp_isa_vec<16>(_mm512_cvtepi64_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128sbyte_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128sbytewithsaturation_v512i32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128SByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm512_cvtsepi32_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128sbytewithsaturation_v512i32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128SByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128sbytewithsaturation_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128SByteWithSaturation");
    return dn2cpp_isa_vec<16>(_mm512_cvtsepi64_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128sbytewithsaturation_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128SByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128uint16_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128UInt16");
    return dn2cpp_isa_vec<16>(_mm512_cvtepi64_epi16(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128uint16_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128uint16_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128UInt16");
    return dn2cpp_isa_vec<16>(_mm512_cvtepi64_epi16(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128uint16_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128uint16withsaturation_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128UInt16WithSaturation");
    return dn2cpp_isa_vec<16>(_mm512_cvtusepi64_epi16(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_converttovector128uint16withsaturation_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector128UInt16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256int16_v512i32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Int16");
    return dn2cpp_isa_vec<32>(_mm512_cvtepi32_epi16(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256int16_v512i32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256int16_v512u32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Int16");
    return dn2cpp_isa_vec<32>(_mm512_cvtepi32_epi16(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256int16_v512u32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256int16withsaturation_v512i32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Int16WithSaturation");
    return dn2cpp_isa_vec<32>(_mm512_cvtsepi32_epi16(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256int16withsaturation_v512i32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Int16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256int32_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Int32");
    return dn2cpp_isa_vec<32>(_mm512_cvtpd_epi32(dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256int32_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256int32_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Int32");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epi32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epi32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epi32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epi32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epi32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epi32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epi32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epi32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epi32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epi32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epi32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epi32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256int32_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256int32_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Int32");
    return dn2cpp_isa_vec<32>(_mm512_cvtepi64_epi32(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256int32_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256int32_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Int32");
    return dn2cpp_isa_vec<32>(_mm512_cvtepi64_epi32(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256int32_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256int32withsaturation_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Int32WithSaturation");
    return dn2cpp_isa_vec<32>(_mm512_cvtsepi64_epi32(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256int32withsaturation_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Int32WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256int32withtruncation_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Int32WithTruncation");
    return dn2cpp_isa_vec<32>(_mm512_cvttpd_epi32(dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256int32withtruncation_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Int32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256single_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Single");
    return dn2cpp_isa_vec<32>(_mm512_cvtpd_ps(dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256single_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256single_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Single");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_ps(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_ps(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_ps(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_ps(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_ps(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_ps(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_ps(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_ps(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_ps(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_ps(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_ps(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_ps(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256single_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256uint16_v512i32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256UInt16");
    return dn2cpp_isa_vec<32>(_mm512_cvtepi32_epi16(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256uint16_v512i32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256uint16_v512u32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256UInt16");
    return dn2cpp_isa_vec<32>(_mm512_cvtepi32_epi16(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256uint16_v512u32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256uint16withsaturation_v512u32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256UInt16WithSaturation");
    return dn2cpp_isa_vec<32>(_mm512_cvtusepi32_epi16(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256uint16withsaturation_v512u32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256UInt16WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256uint32_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256UInt32");
    return dn2cpp_isa_vec<32>(_mm512_cvtpd_epu32(dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256uint32_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256uint32_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256UInt32");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epu32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epu32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epu32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epu32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epu32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epu32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epu32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epu32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epu32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epu32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epu32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<32>(_mm512_cvt_roundpd_epu32(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256uint32_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256uint32_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256UInt32");
    return dn2cpp_isa_vec<32>(_mm512_cvtepi64_epi32(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256uint32_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256uint32_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256UInt32");
    return dn2cpp_isa_vec<32>(_mm512_cvtepi64_epi32(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256uint32_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256uint32withsaturation_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256UInt32WithSaturation");
    return dn2cpp_isa_vec<32>(_mm512_cvtusepi64_epi32(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256uint32withsaturation_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256UInt32WithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256uint32withtruncation_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256UInt32WithTruncation");
    return dn2cpp_isa_vec<32>(_mm512_cvttpd_epu32(dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_converttovector256uint32withtruncation_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector256UInt32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512double_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Double");
    return dn2cpp_isa_vec<64>(_mm512_cvtps_pd(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512double_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512double_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Double");
    return dn2cpp_isa_vec<64>(_mm512_cvtepi32_pd(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512double_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512double_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Double");
    return dn2cpp_isa_vec<64>(_mm512_cvtepu32_pd(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512double_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int32_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int32");
    return dn2cpp_isa_vec<64>(_mm512_cvtepi8_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int32_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int32_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int32");
    return dn2cpp_isa_vec<64>(_mm512_cvtepu8_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int32_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int32_v256i16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int32");
    return dn2cpp_isa_vec<64>(_mm512_cvtepi16_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int32_v256i16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int32_v256u16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int32");
    return dn2cpp_isa_vec<64>(_mm512_cvtepu16_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int32_v256u16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int32_v512f32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int32");
    return dn2cpp_isa_vec<64>(_mm512_cvtps_epi32(dn2cpp_isa_bits<__m512>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int32_v512f32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int32_v512f32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int32");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int32_v512f32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int32withtruncation_v512f32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int32WithTruncation");
    return dn2cpp_isa_vec<64>(_mm512_cvttps_epi32(dn2cpp_isa_bits<__m512>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int32withtruncation_v512f32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int64_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int64");
    return dn2cpp_isa_vec<64>(_mm512_cvtepi16_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int64_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int64_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int64");
    return dn2cpp_isa_vec<64>(_mm512_cvtepi8_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int64_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int64_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int64");
    return dn2cpp_isa_vec<64>(_mm512_cvtepu16_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int64_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int64_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int64");
    return dn2cpp_isa_vec<64>(_mm512_cvtepu8_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int64_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int64_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int64");
    return dn2cpp_isa_vec<64>(_mm512_cvtepi32_epi64(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int64_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int64_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int64");
    return dn2cpp_isa_vec<64>(_mm512_cvtepu32_epi64(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512int64_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512single_v512i32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Single");
    return dn2cpp_isa_vec<64>(_mm512_cvtepi32_ps(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512single_v512i32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512single_v512i32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Single");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512single_v512i32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512single_v512u32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Single");
    return dn2cpp_isa_vec<64>(_mm512_cvtepu32_ps(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512single_v512u32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512single_v512u32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Single");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu32_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512single_v512u32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint32_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt32");
    return dn2cpp_isa_vec<64>(_mm512_cvtepi8_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint32_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint32_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt32");
    return dn2cpp_isa_vec<64>(_mm512_cvtepu8_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint32_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint32_v256i16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt32");
    return dn2cpp_isa_vec<64>(_mm512_cvtepi16_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint32_v256i16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint32_v256u16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt32");
    return dn2cpp_isa_vec<64>(_mm512_cvtepu16_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint32_v256u16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint32_v512f32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt32");
    return dn2cpp_isa_vec<64>(_mm512_cvtps_epu32(dn2cpp_isa_bits<__m512>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint32_v512f32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint32_v512f32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt32");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu32(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint32_v512f32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint32withtruncation_v512f32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt32WithTruncation");
    return dn2cpp_isa_vec<64>(_mm512_cvttps_epu32(dn2cpp_isa_bits<__m512>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint32withtruncation_v512f32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint64_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt64");
    return dn2cpp_isa_vec<64>(_mm512_cvtepi16_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint64_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint64_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt64");
    return dn2cpp_isa_vec<64>(_mm512_cvtepi8_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint64_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint64_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt64");
    return dn2cpp_isa_vec<64>(_mm512_cvtepu16_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint64_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint64_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt64");
    return dn2cpp_isa_vec<64>(_mm512_cvtepu8_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint64_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint64_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt64");
    return dn2cpp_isa_vec<64>(_mm512_cvtepi32_epi64(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint64_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint64_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt64");
    return dn2cpp_isa_vec<64>(_mm512_cvtepu32_epi64(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_converttovector512uint64_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ConvertToVector512UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_divide_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Divide");
    return dn2cpp_isa_vec<64>(_mm512_div_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_divide_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Divide");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_divide_v512f32_v512f32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Divide");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_div_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_div_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_div_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_div_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_div_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_div_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_div_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_div_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_div_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_div_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_div_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_div_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_divide_v512f32_v512f32_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Divide");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_divide_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Divide");
    return dn2cpp_isa_vec<64>(_mm512_div_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_divide_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Divide");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_divide_v512f64_v512f64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Divide");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_div_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_div_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_div_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_div_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_div_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_div_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_div_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_div_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_div_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_div_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_div_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_div_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_divide_v512f64_v512f64_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Divide");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_dividescalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.DivideScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_div_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_dividescalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.DivideScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_dividescalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.DivideScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_div_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_dividescalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.DivideScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_duplicateevenindexed_v512f32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.DuplicateEvenIndexed");
    return dn2cpp_isa_vec<64>(_mm512_moveldup_ps(dn2cpp_isa_bits<__m512>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_duplicateevenindexed_v512f32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.DuplicateEvenIndexed");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_duplicateevenindexed_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.DuplicateEvenIndexed");
    return dn2cpp_isa_vec<64>(_mm512_movedup_pd(dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_duplicateevenindexed_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.DuplicateEvenIndexed");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_duplicateoddindexed_v512f32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.DuplicateOddIndexed");
    return dn2cpp_isa_vec<64>(_mm512_movehdup_ps(dn2cpp_isa_bits<__m512>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_duplicateoddindexed_v512f32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.DuplicateOddIndexed");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expand_v512f32_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Expand");
    return dn2cpp_isa_vec<64>(_mm512_mask_expand_ps(dn2cpp_isa_bits<__m512>(a0), _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expand_v512f32_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expand_v512f64_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Expand");
    return dn2cpp_isa_vec<64>(_mm512_mask_expand_pd(dn2cpp_isa_bits<__m512d>(a0), _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expand_v512f64_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expand_v512i32_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Expand");
    return dn2cpp_isa_vec<64>(_mm512_mask_expand_epi32(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expand_v512i32_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expand_v512i64_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Expand");
    return dn2cpp_isa_vec<64>(_mm512_mask_expand_epi64(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expand_v512i64_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expand_v512u32_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Expand");
    return dn2cpp_isa_vec<64>(_mm512_mask_expand_epi32(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expand_v512u32_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expand_v512u64_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Expand");
    return dn2cpp_isa_vec<64>(_mm512_mask_expand_epi64(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expand_v512u64_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expandload_pf32_v512f32_v512f32(float* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExpandLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_expandloadu_ps(dn2cpp_isa_bits<__m512>(a2), _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expandload_pf32_v512f32_v512f32(float*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expandload_pf64_v512f64_v512f64(double* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExpandLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_expandloadu_pd(dn2cpp_isa_bits<__m512d>(a2), _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expandload_pf64_v512f64_v512f64(double*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expandload_pi32_v512i32_v512i32(int32_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExpandLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_expandloadu_epi32(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expandload_pi32_v512i32_v512i32(int32_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expandload_pi64_v512i64_v512i64(int64_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExpandLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_expandloadu_epi64(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expandload_pi64_v512i64_v512i64(int64_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expandload_pu32_v512u32_v512u32(uint32_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExpandLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_expandloadu_epi32(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expandload_pu32_v512u32_v512u32(uint32_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expandload_pu64_v512u64_v512u64(uint64_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExpandLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_expandloadu_epi64(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_expandload_pu64_v512u64_v512u64(uint64_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512f32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm512_extractf32x4_ps(dn2cpp_isa_bits<__m512>(a0), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512f32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm512_extractf64x2_pd(dn2cpp_isa_bits<__m512d>(a0), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512i16_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm512_extracti32x4_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512i16_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512i32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm512_extracti32x4_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512i32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512i64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm512_extracti32x4_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512i64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512i8_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm512_extracti32x4_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512i8_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512u16_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm512_extracti32x4_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512u16_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512u32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm512_extracti32x4_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512u32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512u64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm512_extracti32x4_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512u64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512u8_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm512_extracti32x4_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_extractvector128_v512u8_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512f32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm512_extractf32x8_ps(dn2cpp_isa_bits<__m512>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512f32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm512_extractf64x4_pd(dn2cpp_isa_bits<__m512d>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512i16_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm512_extracti64x4_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512i16_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512i32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm512_extracti64x4_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512i32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512i64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm512_extracti64x4_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512i64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512i8_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm512_extracti64x4_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512i8_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512u16_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm512_extracti64x4_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512u16_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512u32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm512_extracti64x4_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512u32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512u64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm512_extracti64x4_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512u64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512u8_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm512_extracti64x4_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512f_extractvector256_v512u8_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ExtractVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fixup_v512f32_v512f32_v512i32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Fixup");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<64>(_mm512_fixupimm_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fixup_v512f32_v512f32_v512i32_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Fixup");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fixup_v512f64_v512f64_v512i64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Fixup");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<64>(_mm512_fixupimm_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fixup_v512f64_v512f64_v512i64_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Fixup");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fixupscalar_v128f32_v128f32_v128i32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FixupScalar");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_fixupimm_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fixupscalar_v128f32_v128f32_v128i32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FixupScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fixupscalar_v128f64_v128f64_v128i64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FixupScalar");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<16>(_mm_fixupimm_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fixupscalar_v128f64_v128f64_v128i64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FixupScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyadd_v512f32_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAdd");
    return dn2cpp_isa_vec<64>(_mm512_fmadd_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyadd_v512f32_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyadd_v512f32_v512f32_v512f32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAdd");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_fmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_fmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_fmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_fmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_fmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_fmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_fmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_fmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_fmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_fmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_fmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_fmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyadd_v512f32_v512f32_v512f32_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyadd_v512f64_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAdd");
    return dn2cpp_isa_vec<64>(_mm512_fmadd_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyadd_v512f64_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyadd_v512f64_v512f64_v512f64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAdd");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_fmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_fmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_fmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_fmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_fmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_fmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_fmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_fmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_fmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_fmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_fmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_fmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyadd_v512f64_v512f64_v512f64_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyaddnegated_v512f32_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddNegated");
    return dn2cpp_isa_vec<64>(_mm512_fnmadd_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyaddnegated_v512f32_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddNegated");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyaddnegated_v512f32_v512f32_v512f32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddNegated");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyaddnegated_v512f32_v512f32_v512f32_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddNegated");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyaddnegated_v512f64_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddNegated");
    return dn2cpp_isa_vec<64>(_mm512_fnmadd_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyaddnegated_v512f64_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddNegated");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyaddnegated_v512f64_v512f64_v512f64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddNegated");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_fnmadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyaddnegated_v512f64_v512f64_v512f64_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddNegated");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fusedmultiplyaddnegatedscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddNegatedScalar");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_fnmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fusedmultiplyaddnegatedscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddNegatedScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fusedmultiplyaddnegatedscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddNegatedScalar");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_fnmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fusedmultiplyaddnegatedscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddNegatedScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fusedmultiplyaddscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddScalar");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_fmadd_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fusedmultiplyaddscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fusedmultiplyaddscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddScalar");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_fmadd_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fusedmultiplyaddscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyaddsubtract_v512f32_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddSubtract");
    return dn2cpp_isa_vec<64>(_mm512_fmaddsub_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyaddsubtract_v512f32_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyaddsubtract_v512f32_v512f32_v512f32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddSubtract");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyaddsubtract_v512f32_v512f32_v512f32_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyaddsubtract_v512f64_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddSubtract");
    return dn2cpp_isa_vec<64>(_mm512_fmaddsub_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyaddsubtract_v512f64_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyaddsubtract_v512f64_v512f64_v512f64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddSubtract");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_fmaddsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplyaddsubtract_v512f64_v512f64_v512f64_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplyAddSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtract_v512f32_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtract");
    return dn2cpp_isa_vec<64>(_mm512_fmsub_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtract_v512f32_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtract_v512f32_v512f32_v512f32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtract");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_fmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_fmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_fmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_fmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_fmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_fmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_fmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_fmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_fmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_fmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_fmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_fmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtract_v512f32_v512f32_v512f32_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtract_v512f64_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtract");
    return dn2cpp_isa_vec<64>(_mm512_fmsub_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtract_v512f64_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtract_v512f64_v512f64_v512f64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtract");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_fmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_fmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_fmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_fmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_fmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_fmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_fmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_fmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_fmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_fmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_fmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_fmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtract_v512f64_v512f64_v512f64_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractadd_v512f32_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractAdd");
    return dn2cpp_isa_vec<64>(_mm512_fmsubadd_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractadd_v512f32_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractadd_v512f32_v512f32_v512f32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractAdd");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractadd_v512f32_v512f32_v512f32_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractadd_v512f64_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractAdd");
    return dn2cpp_isa_vec<64>(_mm512_fmsubadd_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractadd_v512f64_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractadd_v512f64_v512f64_v512f64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractAdd");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_fmsubadd_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractadd_v512f64_v512f64_v512f64_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractnegated_v512f32_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractNegated");
    return dn2cpp_isa_vec<64>(_mm512_fnmsub_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractnegated_v512f32_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractNegated");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractnegated_v512f32_v512f32_v512f32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractNegated");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), dn2cpp_isa_bits<__m512>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractnegated_v512f32_v512f32_v512f32_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractNegated");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractnegated_v512f64_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractNegated");
    return dn2cpp_isa_vec<64>(_mm512_fnmsub_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractnegated_v512f64_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractNegated");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractnegated_v512f64_v512f64_v512f64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractNegated");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_fnmsub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), dn2cpp_isa_bits<__m512d>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractnegated_v512f64_v512f64_v512f64_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractNegated");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractnegatedscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractNegatedScalar");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_fnmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractnegatedscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractNegatedScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractnegatedscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractNegatedScalar");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_fnmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractnegatedscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractNegatedScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractScalar");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_fmsub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), dn2cpp_isa_bits<__m128>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractscalar_v128f32_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractScalar");
    switch ((int)a3) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_fmsub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), dn2cpp_isa_bits<__m128d>(a2), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_fusedmultiplysubtractscalar_v128f64_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.FusedMultiplySubtractScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_getexponent_v512f32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.GetExponent");
    return dn2cpp_isa_vec<64>(_mm512_getexp_ps(dn2cpp_isa_bits<__m512>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_getexponent_v512f32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.GetExponent");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_getexponent_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.GetExponent");
    return dn2cpp_isa_vec<64>(_mm512_getexp_pd(dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_getexponent_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.GetExponent");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_getexponentscalar_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.GetExponentScalar");
    return dn2cpp_isa_vec<16>(_mm_getexp_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_getexponentscalar_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.GetExponentScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_getexponentscalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.GetExponentScalar");
    return dn2cpp_isa_vec<16>(_mm_getexp_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_getexponentscalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.GetExponentScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_getexponentscalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.GetExponentScalar");
    return dn2cpp_isa_vec<16>(_mm_getexp_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_getexponentscalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.GetExponentScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_getexponentscalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.GetExponentScalar");
    return dn2cpp_isa_vec<16>(_mm_getexp_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_getexponentscalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.GetExponentScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_getmantissa_v512f32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.GetMantissa");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<64>(_mm512_getmant_ps(dn2cpp_isa_bits<__m512>(a0), (_MM_MANTISSA_NORM_ENUM)(DN2CPP_IMM & 3), (_MM_MANTISSA_SIGN_ENUM)(DN2CPP_IMM >> 2))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_getmantissa_v512f32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.GetMantissa");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_getmantissa_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.GetMantissa");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<64>(_mm512_getmant_pd(dn2cpp_isa_bits<__m512d>(a0), (_MM_MANTISSA_NORM_ENUM)(DN2CPP_IMM & 3), (_MM_MANTISSA_SIGN_ENUM)(DN2CPP_IMM >> 2))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_getmantissa_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.GetMantissa");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_getmantissascalar_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.GetMantissaScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<16>(_mm_getmant_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0), (_MM_MANTISSA_NORM_ENUM)(DN2CPP_IMM & 3), (_MM_MANTISSA_SIGN_ENUM)(DN2CPP_IMM >> 2))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_getmantissascalar_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.GetMantissaScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_getmantissascalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.GetMantissaScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<16>(_mm_getmant_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (_MM_MANTISSA_NORM_ENUM)(DN2CPP_IMM & 3), (_MM_MANTISSA_SIGN_ENUM)(DN2CPP_IMM >> 2))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_getmantissascalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.GetMantissaScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_getmantissascalar_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.GetMantissaScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a1, dn2cpp_isa_vec<16>(_mm_getmant_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0), (_MM_MANTISSA_NORM_ENUM)(DN2CPP_IMM & 3), (_MM_MANTISSA_SIGN_ENUM)(DN2CPP_IMM >> 2))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_getmantissascalar_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.GetMantissaScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_getmantissascalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.GetMantissaScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<16>(_mm_getmant_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (_MM_MANTISSA_NORM_ENUM)(DN2CPP_IMM & 3), (_MM_MANTISSA_SIGN_ENUM)(DN2CPP_IMM >> 2))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_getmantissascalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.GetMantissaScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512f32_v128f32_u8(const Dn2CppVector512& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_insertf32x4(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512f32_v128f32_u8(const Dn2CppVector512&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512f64_v128f64_u8(const Dn2CppVector512& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_insertf64x2(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512f64_v128f64_u8(const Dn2CppVector512&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512i16_v128i16_u8(const Dn2CppVector512& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti32x4(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512i16_v128i16_u8(const Dn2CppVector512&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512i32_v128i32_u8(const Dn2CppVector512& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti32x4(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512i32_v128i32_u8(const Dn2CppVector512&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512i64_v128i64_u8(const Dn2CppVector512& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti32x4(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512i64_v128i64_u8(const Dn2CppVector512&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512i8_v128i8_u8(const Dn2CppVector512& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti32x4(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512i8_v128i8_u8(const Dn2CppVector512&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512u16_v128u16_u8(const Dn2CppVector512& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti32x4(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512u16_v128u16_u8(const Dn2CppVector512&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512u32_v128u32_u8(const Dn2CppVector512& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti32x4(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512u32_v128u32_u8(const Dn2CppVector512&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512u64_v128u64_u8(const Dn2CppVector512& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti32x4(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512u64_v128u64_u8(const Dn2CppVector512&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512u8_v128u8_u8(const Dn2CppVector512& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti32x4(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector128_v512u8_v128u8_u8(const Dn2CppVector512&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512f32_v256f32_u8(const Dn2CppVector512& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_insertf32x8(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m256>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512f32_v256f32_u8(const Dn2CppVector512&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512f64_v256f64_u8(const Dn2CppVector512& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_insertf64x4(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m256d>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512f64_v256f64_u8(const Dn2CppVector512&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512i16_v256i16_u8(const Dn2CppVector512& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti64x4(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512i16_v256i16_u8(const Dn2CppVector512&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512i32_v256i32_u8(const Dn2CppVector512& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti64x4(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512i32_v256i32_u8(const Dn2CppVector512&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512i64_v256i64_u8(const Dn2CppVector512& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti64x4(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512i64_v256i64_u8(const Dn2CppVector512&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512i8_v256i8_u8(const Dn2CppVector512& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti64x4(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512i8_v256i8_u8(const Dn2CppVector512&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512u16_v256u16_u8(const Dn2CppVector512& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti64x4(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512u16_v256u16_u8(const Dn2CppVector512&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512u32_v256u32_u8(const Dn2CppVector512& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti64x4(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512u32_v256u32_u8(const Dn2CppVector512&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512u64_v256u64_u8(const Dn2CppVector512& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti64x4(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512u64_v256u64_u8(const Dn2CppVector512&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512u8_v256u8_u8(const Dn2CppVector512& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti64x4(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_insertvector256_v512u8_v256u8_u8(const Dn2CppVector512&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.InsertVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pf32(float* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
    return dn2cpp_isa_vec<64>(_mm512_load_ps(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pf32(float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
    return dn2cpp_isa_vec<64>(_mm512_load_pd(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
    return dn2cpp_isa_vec<64>(_mm512_load_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
    return dn2cpp_isa_vec<64>(_mm512_load_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
    return dn2cpp_isa_vec<64>(_mm512_load_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
    return dn2cpp_isa_vec<64>(_mm512_load_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
    return dn2cpp_isa_vec<64>(_mm512_load_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
    return dn2cpp_isa_vec<64>(_mm512_load_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
    return dn2cpp_isa_vec<64>(_mm512_load_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
    return dn2cpp_isa_vec<64>(_mm512_load_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512nontemporal_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512NonTemporal");
    return dn2cpp_isa_vec<64>(_mm512_stream_load_si512((void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512nontemporal_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512nontemporal_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512NonTemporal");
    return dn2cpp_isa_vec<64>(_mm512_stream_load_si512((void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512nontemporal_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512nontemporal_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512NonTemporal");
    return dn2cpp_isa_vec<64>(_mm512_stream_load_si512((void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512nontemporal_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512nontemporal_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512NonTemporal");
    return dn2cpp_isa_vec<64>(_mm512_stream_load_si512((void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512nontemporal_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512nontemporal_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512NonTemporal");
    return dn2cpp_isa_vec<64>(_mm512_stream_load_si512((void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512nontemporal_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512nontemporal_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512NonTemporal");
    return dn2cpp_isa_vec<64>(_mm512_stream_load_si512((void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512nontemporal_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512nontemporal_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512NonTemporal");
    return dn2cpp_isa_vec<64>(_mm512_stream_load_si512((void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512nontemporal_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512nontemporal_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512NonTemporal");
    return dn2cpp_isa_vec<64>(_mm512_stream_load_si512((void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadalignedvector512nontemporal_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadAlignedVector512NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pf32(float* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
    return dn2cpp_isa_vec<64>(_mm512_loadu_ps(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pf32(float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
    return dn2cpp_isa_vec<64>(_mm512_loadu_pd(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
    return dn2cpp_isa_vec<64>(_mm512_loadu_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
    return dn2cpp_isa_vec<64>(_mm512_loadu_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
    return dn2cpp_isa_vec<64>(_mm512_loadu_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
    return dn2cpp_isa_vec<64>(_mm512_loadu_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
    return dn2cpp_isa_vec<64>(_mm512_loadu_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
    return dn2cpp_isa_vec<64>(_mm512_loadu_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
    return dn2cpp_isa_vec<64>(_mm512_loadu_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
    return dn2cpp_isa_vec<64>(_mm512_loadu_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_loadvector512_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.LoadVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskload_pf32_v512f32_v512f32(float* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_loadu_ps(dn2cpp_isa_bits<__m512>(a2), _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskload_pf32_v512f32_v512f32(float*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskload_pf64_v512f64_v512f64(double* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_loadu_pd(dn2cpp_isa_bits<__m512d>(a2), _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskload_pf64_v512f64_v512f64(double*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskload_pi32_v512i32_v512i32(int32_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_loadu_epi32(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskload_pi32_v512i32_v512i32(int32_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskload_pi64_v512i64_v512i64(int64_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_loadu_epi64(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskload_pi64_v512i64_v512i64(int64_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskload_pu32_v512u32_v512u32(uint32_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_loadu_epi32(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskload_pu32_v512u32_v512u32(uint32_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskload_pu64_v512u64_v512u64(uint64_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_loadu_epi64(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskload_pu64_v512u64_v512u64(uint64_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskloadaligned_pf32_v512f32_v512f32(float* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskLoadAligned");
    return dn2cpp_isa_vec<64>(_mm512_mask_load_ps(dn2cpp_isa_bits<__m512>(a2), _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskloadaligned_pf32_v512f32_v512f32(float*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskloadaligned_pf64_v512f64_v512f64(double* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskLoadAligned");
    return dn2cpp_isa_vec<64>(_mm512_mask_load_pd(dn2cpp_isa_bits<__m512d>(a2), _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskloadaligned_pf64_v512f64_v512f64(double*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskloadaligned_pi32_v512i32_v512i32(int32_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskLoadAligned");
    return dn2cpp_isa_vec<64>(_mm512_mask_load_epi32(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskloadaligned_pi32_v512i32_v512i32(int32_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskloadaligned_pi64_v512i64_v512i64(int64_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskLoadAligned");
    return dn2cpp_isa_vec<64>(_mm512_mask_load_epi64(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskloadaligned_pi64_v512i64_v512i64(int64_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskloadaligned_pu32_v512u32_v512u32(uint32_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskLoadAligned");
    return dn2cpp_isa_vec<64>(_mm512_mask_load_epi32(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskloadaligned_pu32_v512u32_v512u32(uint32_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskloadaligned_pu64_v512u64_v512u64(uint64_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskLoadAligned");
    return dn2cpp_isa_vec<64>(_mm512_mask_load_epi64(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_maskloadaligned_pu64_v512u64_v512u64(uint64_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskLoadAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstore_pf32_v512f32_v512f32(float* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskStore");
    _mm512_mask_storeu_ps((void*)a0, _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstore_pf32_v512f32_v512f32(float*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstore_pf64_v512f64_v512f64(double* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskStore");
    _mm512_mask_storeu_pd((void*)a0, _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512d>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstore_pf64_v512f64_v512f64(double*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstore_pi32_v512i32_v512i32(int32_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskStore");
    _mm512_mask_storeu_epi32((void*)a0, _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstore_pi32_v512i32_v512i32(int32_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstore_pi64_v512i64_v512i64(int64_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskStore");
    _mm512_mask_storeu_epi64((void*)a0, _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstore_pi64_v512i64_v512i64(int64_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstore_pu32_v512u32_v512u32(uint32_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskStore");
    _mm512_mask_storeu_epi32((void*)a0, _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstore_pu32_v512u32_v512u32(uint32_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstore_pu64_v512u64_v512u64(uint64_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskStore");
    _mm512_mask_storeu_epi64((void*)a0, _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstore_pu64_v512u64_v512u64(uint64_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstorealigned_pf32_v512f32_v512f32(float* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskStoreAligned");
    _mm512_mask_store_ps((void*)a0, _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstorealigned_pf32_v512f32_v512f32(float*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstorealigned_pf64_v512f64_v512f64(double* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskStoreAligned");
    _mm512_mask_store_pd((void*)a0, _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512d>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstorealigned_pf64_v512f64_v512f64(double*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstorealigned_pi32_v512i32_v512i32(int32_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskStoreAligned");
    _mm512_mask_store_epi32((void*)a0, _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstorealigned_pi32_v512i32_v512i32(int32_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstorealigned_pi64_v512i64_v512i64(int64_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskStoreAligned");
    _mm512_mask_store_epi64((void*)a0, _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstorealigned_pi64_v512i64_v512i64(int64_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstorealigned_pu32_v512u32_v512u32(uint32_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskStoreAligned");
    _mm512_mask_store_epi32((void*)a0, _mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstorealigned_pu32_v512u32_v512u32(uint32_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstorealigned_pu64_v512u64_v512u64(uint64_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MaskStoreAligned");
    _mm512_mask_store_epi64((void*)a0, _mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_maskstorealigned_pu64_v512u64_v512u64(uint64_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MaskStoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_max_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Max");
    return dn2cpp_isa_vec<64>(_mm512_max_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_max_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_max_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Max");
    return dn2cpp_isa_vec<64>(_mm512_max_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_max_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_max_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Max");
    return dn2cpp_isa_vec<64>(_mm512_max_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_max_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_max_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Max");
    return dn2cpp_isa_vec<64>(_mm512_max_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_max_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_max_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Max");
    return dn2cpp_isa_vec<64>(_mm512_max_epu32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_max_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_max_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Max");
    return dn2cpp_isa_vec<64>(_mm512_max_epu64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_max_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_min_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Min");
    return dn2cpp_isa_vec<64>(_mm512_min_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_min_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_min_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Min");
    return dn2cpp_isa_vec<64>(_mm512_min_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_min_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_min_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Min");
    return dn2cpp_isa_vec<64>(_mm512_min_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_min_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_min_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Min");
    return dn2cpp_isa_vec<64>(_mm512_min_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_min_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_min_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Min");
    return dn2cpp_isa_vec<64>(_mm512_min_epu32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_min_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_min_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Min");
    return dn2cpp_isa_vec<64>(_mm512_min_epu64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_min_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512f_movemask_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MoveMask");
    return (int32_t)_mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512f_movemask_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512f_movemask_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MoveMask");
    return (int32_t)_mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512f_movemask_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512f_movemask_v256i16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MoveMask");
    return (int32_t)_mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512f_movemask_v256i16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512f_movemask_v256u16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MoveMask");
    return (int32_t)_mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512f_movemask_v256u16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512f_movemask_v512f32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MoveMask");
    return (int32_t)_mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512f_movemask_v512f32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512f_movemask_v512i32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MoveMask");
    return (int32_t)_mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512f_movemask_v512i32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512f_movemask_v512u32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MoveMask");
    return (int32_t)_mm512_movepi32_mask(dn2cpp_isa_bits<__m512i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512f_movemask_v512u32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_multiply_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Multiply");
    return dn2cpp_isa_vec<64>(_mm512_mul_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_multiply_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Multiply");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_multiply_v512f32_v512f32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Multiply");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_mul_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_mul_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_mul_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_mul_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_mul_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_mul_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_mul_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_mul_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_mul_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_mul_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_mul_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_mul_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_multiply_v512f32_v512f32_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Multiply");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_multiply_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Multiply");
    return dn2cpp_isa_vec<64>(_mm512_mul_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_multiply_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Multiply");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_multiply_v512f64_v512f64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Multiply");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_mul_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_mul_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_mul_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_mul_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_mul_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_mul_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_mul_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_mul_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_mul_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_mul_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_mul_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_mul_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_multiply_v512f64_v512f64_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Multiply");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_multiply_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Multiply");
    return dn2cpp_isa_vec<64>(_mm512_mul_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_multiply_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Multiply");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_multiply_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Multiply");
    return dn2cpp_isa_vec<64>(_mm512_mul_epu32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_multiply_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Multiply");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_multiplylow_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MultiplyLow");
    return dn2cpp_isa_vec<64>(_mm512_mullo_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_multiplylow_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_multiplylow_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MultiplyLow");
    return dn2cpp_isa_vec<64>(_mm512_mullo_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_multiplylow_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_multiplyscalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MultiplyScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_mul_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_multiplyscalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MultiplyScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_multiplyscalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.MultiplyScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_mul_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_multiplyscalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.MultiplyScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_or_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Or");
    return dn2cpp_isa_vec<64>(_mm512_or_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_or_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_or_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Or");
    return dn2cpp_isa_vec<64>(_mm512_or_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_or_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_or_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Or");
    return dn2cpp_isa_vec<64>(_mm512_or_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_or_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_or_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Or");
    return dn2cpp_isa_vec<64>(_mm512_or_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_or_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_or_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Or");
    return dn2cpp_isa_vec<64>(_mm512_or_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_or_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_or_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Or");
    return dn2cpp_isa_vec<64>(_mm512_or_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_or_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_or_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Or");
    return dn2cpp_isa_vec<64>(_mm512_or_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_or_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_or_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Or");
    return dn2cpp_isa_vec<64>(_mm512_or_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_or_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permute2x64_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Permute2x64");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_permute_pd(dn2cpp_isa_bits<__m512d>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permute2x64_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Permute2x64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permute4x32_v512f32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Permute4x32");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_permute_ps(dn2cpp_isa_bits<__m512>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permute4x32_v512f32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Permute4x32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permute4x64_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Permute4x64");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_permutex_pd(dn2cpp_isa_bits<__m512d>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permute4x64_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Permute4x64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permute4x64_v512i64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Permute4x64");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_permutex_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permute4x64_v512i64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Permute4x64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permute4x64_v512u64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Permute4x64");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_permutex_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permute4x64_v512u64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Permute4x64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar16x32_v512f32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.PermuteVar16x32");
    return dn2cpp_isa_vec<64>(_mm512_permutexvar_ps(dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar16x32_v512f32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.PermuteVar16x32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar16x32_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.PermuteVar16x32");
    return dn2cpp_isa_vec<64>(_mm512_permutexvar_epi32(dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar16x32_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.PermuteVar16x32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar16x32_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.PermuteVar16x32");
    return dn2cpp_isa_vec<64>(_mm512_permutexvar_epi32(dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar16x32_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.PermuteVar16x32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar16x32x2_v512f32_v512i32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.PermuteVar16x32x2");
    return dn2cpp_isa_vec<64>(_mm512_permutex2var_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar16x32x2_v512f32_v512i32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.PermuteVar16x32x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar16x32x2_v512i32_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.PermuteVar16x32x2");
    return dn2cpp_isa_vec<64>(_mm512_permutex2var_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar16x32x2_v512i32_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.PermuteVar16x32x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar16x32x2_v512u32_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.PermuteVar16x32x2");
    return dn2cpp_isa_vec<64>(_mm512_permutex2var_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar16x32x2_v512u32_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.PermuteVar16x32x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar2x64_v512f64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.PermuteVar2x64");
    return dn2cpp_isa_vec<64>(_mm512_permutevar_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar2x64_v512f64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.PermuteVar2x64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar4x32_v512f32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.PermuteVar4x32");
    return dn2cpp_isa_vec<64>(_mm512_permutevar_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar4x32_v512f32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.PermuteVar4x32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar8x64_v512f64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.PermuteVar8x64");
    return dn2cpp_isa_vec<64>(_mm512_permutexvar_pd(dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar8x64_v512f64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.PermuteVar8x64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar8x64_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.PermuteVar8x64");
    return dn2cpp_isa_vec<64>(_mm512_permutexvar_epi64(dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar8x64_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.PermuteVar8x64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar8x64_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.PermuteVar8x64");
    return dn2cpp_isa_vec<64>(_mm512_permutexvar_epi64(dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar8x64_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.PermuteVar8x64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar8x64x2_v512f64_v512i64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.PermuteVar8x64x2");
    return dn2cpp_isa_vec<64>(_mm512_permutex2var_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512d>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar8x64x2_v512f64_v512i64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.PermuteVar8x64x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar8x64x2_v512i64_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.PermuteVar8x64x2");
    return dn2cpp_isa_vec<64>(_mm512_permutex2var_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar8x64x2_v512i64_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.PermuteVar8x64x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar8x64x2_v512u64_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.PermuteVar8x64x2");
    return dn2cpp_isa_vec<64>(_mm512_permutex2var_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_permutevar8x64x2_v512u64_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.PermuteVar8x64x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_reciprocal14_v512f32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Reciprocal14");
    return dn2cpp_isa_vec<64>(_mm512_rcp14_ps(dn2cpp_isa_bits<__m512>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_reciprocal14_v512f32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Reciprocal14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_reciprocal14_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Reciprocal14");
    return dn2cpp_isa_vec<64>(_mm512_rcp14_pd(dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_reciprocal14_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Reciprocal14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_reciprocal14scalar_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Reciprocal14Scalar");
    return dn2cpp_isa_vec<16>(_mm_rcp14_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_reciprocal14scalar_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Reciprocal14Scalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_reciprocal14scalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Reciprocal14Scalar");
    return dn2cpp_isa_vec<16>(_mm_rcp14_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_reciprocal14scalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Reciprocal14Scalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_reciprocal14scalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Reciprocal14Scalar");
    return dn2cpp_isa_vec<16>(_mm_rcp14_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_reciprocal14scalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Reciprocal14Scalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_reciprocal14scalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Reciprocal14Scalar");
    return dn2cpp_isa_vec<16>(_mm_rcp14_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_reciprocal14scalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Reciprocal14Scalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_reciprocalsqrt14_v512f32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ReciprocalSqrt14");
    return dn2cpp_isa_vec<64>(_mm512_rsqrt14_ps(dn2cpp_isa_bits<__m512>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_reciprocalsqrt14_v512f32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ReciprocalSqrt14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_reciprocalsqrt14_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ReciprocalSqrt14");
    return dn2cpp_isa_vec<64>(_mm512_rsqrt14_pd(dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_reciprocalsqrt14_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ReciprocalSqrt14");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_reciprocalsqrt14scalar_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ReciprocalSqrt14Scalar");
    return dn2cpp_isa_vec<16>(_mm_rsqrt14_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_reciprocalsqrt14scalar_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ReciprocalSqrt14Scalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_reciprocalsqrt14scalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ReciprocalSqrt14Scalar");
    return dn2cpp_isa_vec<16>(_mm_rsqrt14_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_reciprocalsqrt14scalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ReciprocalSqrt14Scalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_reciprocalsqrt14scalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ReciprocalSqrt14Scalar");
    return dn2cpp_isa_vec<16>(_mm_rsqrt14_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_reciprocalsqrt14scalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ReciprocalSqrt14Scalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_reciprocalsqrt14scalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ReciprocalSqrt14Scalar");
    return dn2cpp_isa_vec<16>(_mm_rsqrt14_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_reciprocalsqrt14scalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ReciprocalSqrt14Scalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateleft_v512i32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_rol_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateleft_v512i32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateleft_v512i64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_rol_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateleft_v512i64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateleft_v512u32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_rol_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateleft_v512u32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateleft_v512u64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RotateLeft");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_rol_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateleft_v512u64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RotateLeft");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateleftvariable_v512i32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RotateLeftVariable");
    return dn2cpp_isa_vec<64>(_mm512_rolv_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateleftvariable_v512i32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateleftvariable_v512i64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RotateLeftVariable");
    return dn2cpp_isa_vec<64>(_mm512_rolv_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateleftvariable_v512i64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateleftvariable_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RotateLeftVariable");
    return dn2cpp_isa_vec<64>(_mm512_rolv_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateleftvariable_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateleftvariable_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RotateLeftVariable");
    return dn2cpp_isa_vec<64>(_mm512_rolv_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateleftvariable_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RotateLeftVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateright_v512i32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_ror_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateright_v512i32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateright_v512i64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_ror_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateright_v512i64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateright_v512u32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_ror_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateright_v512u32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateright_v512u64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RotateRight");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_ror_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotateright_v512u64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RotateRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotaterightvariable_v512i32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RotateRightVariable");
    return dn2cpp_isa_vec<64>(_mm512_rorv_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotaterightvariable_v512i32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotaterightvariable_v512i64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RotateRightVariable");
    return dn2cpp_isa_vec<64>(_mm512_rorv_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotaterightvariable_v512i64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotaterightvariable_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RotateRightVariable");
    return dn2cpp_isa_vec<64>(_mm512_rorv_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotaterightvariable_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotaterightvariable_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RotateRightVariable");
    return dn2cpp_isa_vec<64>(_mm512_rorv_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_rotaterightvariable_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RotateRightVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_roundscale_v512f32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RoundScale");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_roundscale_ps(dn2cpp_isa_bits<__m512>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_roundscale_v512f32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RoundScale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_roundscale_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RoundScale");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_roundscale_pd(dn2cpp_isa_bits<__m512d>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_roundscale_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RoundScale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_roundscalescalar_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RoundScaleScalar");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_roundscale_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_roundscalescalar_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RoundScaleScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_roundscalescalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RoundScaleScalar");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_roundscale_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_roundscalescalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RoundScaleScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_roundscalescalar_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RoundScaleScalar");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_roundscale_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_roundscalescalar_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RoundScaleScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_roundscalescalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.RoundScaleScalar");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_roundscale_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_roundscalescalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.RoundScaleScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_scale_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Scale");
    return dn2cpp_isa_vec<64>(_mm512_scalef_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_scale_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Scale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_scale_v512f32_v512f32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Scale");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_scalef_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_scalef_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_scalef_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_scalef_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_scalef_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_scalef_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_scalef_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_scalef_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_scalef_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_scalef_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_scalef_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_scalef_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_scale_v512f32_v512f32_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Scale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_scale_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Scale");
    return dn2cpp_isa_vec<64>(_mm512_scalef_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_scale_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Scale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_scale_v512f64_v512f64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Scale");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_scalef_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_scalef_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_scalef_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_scalef_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_scalef_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_scalef_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_scalef_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_scalef_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_scalef_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_scalef_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_scalef_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_scalef_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_scale_v512f64_v512f64_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Scale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_scalescalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ScaleScalar");
    return dn2cpp_isa_vec<16>(_mm_scalef_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_scalescalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ScaleScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_scalescalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ScaleScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_scalef_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_scalescalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ScaleScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_scalescalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ScaleScalar");
    return dn2cpp_isa_vec<16>(_mm_scalef_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_scalescalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ScaleScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_scalescalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ScaleScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_scalef_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_scalescalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ScaleScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogical_v512i32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_slli_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogical_v512i32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogical_v512i32_v128i32(const Dn2CppVector512& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogical");
    return dn2cpp_isa_vec<64>(_mm512_sll_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogical_v512i32_v128i32(const Dn2CppVector512&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogical_v512i64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_slli_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogical_v512i64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogical_v512i64_v128i64(const Dn2CppVector512& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogical");
    return dn2cpp_isa_vec<64>(_mm512_sll_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogical_v512i64_v128i64(const Dn2CppVector512&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogical_v512u32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_slli_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogical_v512u32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogical_v512u32_v128u32(const Dn2CppVector512& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogical");
    return dn2cpp_isa_vec<64>(_mm512_sll_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogical_v512u32_v128u32(const Dn2CppVector512&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogical_v512u64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_slli_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogical_v512u64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogical_v512u64_v128u64(const Dn2CppVector512& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogical");
    return dn2cpp_isa_vec<64>(_mm512_sll_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogical_v512u64_v128u64(const Dn2CppVector512&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogicalvariable_v512i32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<64>(_mm512_sllv_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogicalvariable_v512i32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogicalvariable_v512i64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<64>(_mm512_sllv_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogicalvariable_v512i64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogicalvariable_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<64>(_mm512_sllv_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogicalvariable_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogicalvariable_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<64>(_mm512_sllv_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftleftlogicalvariable_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightarithmetic_v512i32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftRightArithmetic");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_srai_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightarithmetic_v512i32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightarithmetic_v512i32_v128i32(const Dn2CppVector512& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftRightArithmetic");
    return dn2cpp_isa_vec<64>(_mm512_sra_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightarithmetic_v512i32_v128i32(const Dn2CppVector512&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightarithmetic_v512i64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftRightArithmetic");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_srai_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightarithmetic_v512i64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightarithmetic_v512i64_v128i64(const Dn2CppVector512& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftRightArithmetic");
    return dn2cpp_isa_vec<64>(_mm512_sra_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightarithmetic_v512i64_v128i64(const Dn2CppVector512&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightarithmeticvariable_v512i32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftRightArithmeticVariable");
    return dn2cpp_isa_vec<64>(_mm512_srav_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightarithmeticvariable_v512i32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftRightArithmeticVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightarithmeticvariable_v512i64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftRightArithmeticVariable");
    return dn2cpp_isa_vec<64>(_mm512_srav_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightarithmeticvariable_v512i64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftRightArithmeticVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogical_v512i32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_srli_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogical_v512i32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogical_v512i32_v128i32(const Dn2CppVector512& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogical");
    return dn2cpp_isa_vec<64>(_mm512_srl_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogical_v512i32_v128i32(const Dn2CppVector512&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogical_v512i64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_srli_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogical_v512i64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogical_v512i64_v128i64(const Dn2CppVector512& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogical");
    return dn2cpp_isa_vec<64>(_mm512_srl_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogical_v512i64_v128i64(const Dn2CppVector512&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogical_v512u32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_srli_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogical_v512u32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogical_v512u32_v128u32(const Dn2CppVector512& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogical");
    return dn2cpp_isa_vec<64>(_mm512_srl_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogical_v512u32_v128u32(const Dn2CppVector512&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogical_v512u64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_srli_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogical_v512u64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogical_v512u64_v128u64(const Dn2CppVector512& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogical");
    return dn2cpp_isa_vec<64>(_mm512_srl_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogical_v512u64_v128u64(const Dn2CppVector512&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogicalvariable_v512i32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<64>(_mm512_srlv_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogicalvariable_v512i32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogicalvariable_v512i64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<64>(_mm512_srlv_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogicalvariable_v512i64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogicalvariable_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<64>(_mm512_srlv_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogicalvariable_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogicalvariable_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<64>(_mm512_srlv_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shiftrightlogicalvariable_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle4x128_v512f32_v512f32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Shuffle4x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_shuffle_f32x4(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle4x128_v512f32_v512f32_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Shuffle4x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle4x128_v512f64_v512f64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Shuffle4x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_shuffle_f64x2(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle4x128_v512f64_v512f64_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Shuffle4x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle4x128_v512i32_v512i32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Shuffle4x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_shuffle_i32x4(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle4x128_v512i32_v512i32_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Shuffle4x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle4x128_v512i64_v512i64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Shuffle4x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_shuffle_i64x2(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle4x128_v512i64_v512i64_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Shuffle4x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle4x128_v512u32_v512u32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Shuffle4x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_shuffle_i32x4(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle4x128_v512u32_v512u32_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Shuffle4x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle4x128_v512u64_v512u64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Shuffle4x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_shuffle_i64x2(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle4x128_v512u64_v512u64_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Shuffle4x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle_v512f32_v512f32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Shuffle");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_shuffle_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle_v512f32_v512f32_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Shuffle");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle_v512f64_v512f64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Shuffle");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_shuffle_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle_v512f64_v512f64_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Shuffle");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle_v512i32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Shuffle");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_shuffle_epi32(dn2cpp_isa_bits<__m512i>(a0), (_MM_PERM_ENUM)DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle_v512i32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Shuffle");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle_v512u32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Shuffle");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_shuffle_epi32(dn2cpp_isa_bits<__m512i>(a0), (_MM_PERM_ENUM)DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_shuffle_v512u32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Shuffle");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_sqrt_v512f32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Sqrt");
    return dn2cpp_isa_vec<64>(_mm512_sqrt_ps(dn2cpp_isa_bits<__m512>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_sqrt_v512f32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Sqrt");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_sqrt_v512f32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Sqrt");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_sqrt_round_ps(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_sqrt_round_ps(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_sqrt_round_ps(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_sqrt_round_ps(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_sqrt_round_ps(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_sqrt_round_ps(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_sqrt_round_ps(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_sqrt_round_ps(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_sqrt_round_ps(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_sqrt_round_ps(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_sqrt_round_ps(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_sqrt_round_ps(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_sqrt_v512f32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Sqrt");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_sqrt_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Sqrt");
    return dn2cpp_isa_vec<64>(_mm512_sqrt_pd(dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_sqrt_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Sqrt");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_sqrt_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Sqrt");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_sqrt_round_pd(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_sqrt_round_pd(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_sqrt_round_pd(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_sqrt_round_pd(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_sqrt_round_pd(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_sqrt_round_pd(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_sqrt_round_pd(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_sqrt_round_pd(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_sqrt_round_pd(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_sqrt_round_pd(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_sqrt_round_pd(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_sqrt_round_pd(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_sqrt_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Sqrt");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_sqrtscalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.SqrtScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_sqrt_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_sqrtscalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.SqrtScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_sqrtscalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.SqrtScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_sqrt_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_sqrtscalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.SqrtScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pf32_v512f32(float* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Store");
    _mm512_storeu_ps(a0, dn2cpp_isa_bits<__m512>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pf32_v512f32(float*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pf64_v512f64(double* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Store");
    _mm512_storeu_pd(a0, dn2cpp_isa_bits<__m512d>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pf64_v512f64(double*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pi16_v512i16(int16_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Store");
    _mm512_storeu_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pi16_v512i16(int16_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pi32_v512i32(int32_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Store");
    _mm512_storeu_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pi32_v512i32(int32_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pi64_v512i64(int64_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Store");
    _mm512_storeu_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pi64_v512i64(int64_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pi8_v512i8(int8_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Store");
    _mm512_storeu_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pi8_v512i8(int8_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pu16_v512u16(uint16_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Store");
    _mm512_storeu_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pu16_v512u16(uint16_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pu32_v512u32(uint32_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Store");
    _mm512_storeu_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pu32_v512u32(uint32_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pu64_v512u64(uint64_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Store");
    _mm512_storeu_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pu64_v512u64(uint64_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pu8_v512u8(uint8_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Store");
    _mm512_storeu_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_store_pu8_v512u8(uint8_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pf32_v512f32(float* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
    _mm512_store_ps(a0, dn2cpp_isa_bits<__m512>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pf32_v512f32(float*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pf64_v512f64(double* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
    _mm512_store_pd(a0, dn2cpp_isa_bits<__m512d>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pf64_v512f64(double*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pi16_v512i16(int16_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
    _mm512_store_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pi16_v512i16(int16_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pi32_v512i32(int32_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
    _mm512_store_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pi32_v512i32(int32_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pi64_v512i64(int64_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
    _mm512_store_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pi64_v512i64(int64_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pi8_v512i8(int8_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
    _mm512_store_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pi8_v512i8(int8_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pu16_v512u16(uint16_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
    _mm512_store_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pu16_v512u16(uint16_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pu32_v512u32(uint32_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
    _mm512_store_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pu32_v512u32(uint32_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pu64_v512u64(uint64_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
    _mm512_store_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pu64_v512u64(uint64_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pu8_v512u8(uint8_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
    _mm512_store_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealigned_pu8_v512u8(uint8_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pf32_v512f32(float* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
    _mm512_stream_ps(a0, dn2cpp_isa_bits<__m512>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pf32_v512f32(float*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pf64_v512f64(double* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
    _mm512_stream_pd(a0, dn2cpp_isa_bits<__m512d>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pf64_v512f64(double*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pi16_v512i16(int16_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
    _mm512_stream_si512((__m512i*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pi16_v512i16(int16_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pi32_v512i32(int32_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
    _mm512_stream_si512((__m512i*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pi32_v512i32(int32_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pi64_v512i64(int64_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
    _mm512_stream_si512((__m512i*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pi64_v512i64(int64_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pi8_v512i8(int8_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
    _mm512_stream_si512((__m512i*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pi8_v512i8(int8_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pu16_v512u16(uint16_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
    _mm512_stream_si512((__m512i*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pu16_v512u16(uint16_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pu32_v512u32(uint32_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
    _mm512_stream_si512((__m512i*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pu32_v512u32(uint32_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pu64_v512u64(uint64_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
    _mm512_stream_si512((__m512i*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pu64_v512u64(uint64_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pu8_v512u8(uint8_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
    _mm512_stream_si512((__m512i*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512f_storealignednontemporal_pu8_v512u8(uint8_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_subtract_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Subtract");
    return dn2cpp_isa_vec<64>(_mm512_sub_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_subtract_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_subtract_v512f32_v512f32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Subtract");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_sub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_sub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_sub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_sub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_sub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_sub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_sub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_sub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_sub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_sub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_sub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_sub_round_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_subtract_v512f32_v512f32_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_subtract_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Subtract");
    return dn2cpp_isa_vec<64>(_mm512_sub_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_subtract_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_subtract_v512f64_v512f64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Subtract");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_sub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_sub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_sub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_sub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_sub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_sub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_sub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_sub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_sub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_sub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_sub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_sub_round_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_subtract_v512f64_v512f64_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_subtract_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Subtract");
    return dn2cpp_isa_vec<64>(_mm512_sub_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_subtract_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_subtract_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Subtract");
    return dn2cpp_isa_vec<64>(_mm512_sub_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_subtract_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_subtract_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Subtract");
    return dn2cpp_isa_vec<64>(_mm512_sub_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_subtract_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_subtract_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Subtract");
    return dn2cpp_isa_vec<64>(_mm512_sub_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_subtract_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_subtractscalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.SubtractScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_sub_round_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_subtractscalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.SubtractScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_subtractscalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.SubtractScalar");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<16>(_mm_sub_round_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512f_subtractscalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.SubtractScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512f32_v512f32_v512f32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<64>(_mm512_ternarylogic_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512f32_v512f32_v512f32_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512f64_v512f64_v512f64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<64>(_mm512_ternarylogic_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512f64_v512f64_v512f64_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512i16_v512i16_v512i16_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<64>(_mm512_ternarylogic_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512i16_v512i16_v512i16_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512i32_v512i32_v512i32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<64>(_mm512_ternarylogic_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512i32_v512i32_v512i32_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512i64_v512i64_v512i64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<64>(_mm512_ternarylogic_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512i64_v512i64_v512i64_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512i8_v512i8_v512i8_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<64>(_mm512_ternarylogic_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512i8_v512i8_v512i8_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512u16_v512u16_v512u16_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<64>(_mm512_ternarylogic_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512u16_v512u16_v512u16_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512u32_v512u32_v512u32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<64>(_mm512_ternarylogic_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512u32_v512u32_v512u32_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512u64_v512u64_v512u64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<64>(_mm512_ternarylogic_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512u64_v512u64_v512u64_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512u8_v512u8_v512u8_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2, uint8_t a3)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
    DN2CPP_ISA_IMM8_SWITCH(a3, dn2cpp_isa_vec<64>(_mm512_ternarylogic_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_ternarylogic_v512u8_v512u8_v512u8_u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.TernaryLogic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpackhigh_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.UnpackHigh");
    return dn2cpp_isa_vec<64>(_mm512_unpackhi_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpackhigh_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpackhigh_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.UnpackHigh");
    return dn2cpp_isa_vec<64>(_mm512_unpackhi_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpackhigh_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpackhigh_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.UnpackHigh");
    return dn2cpp_isa_vec<64>(_mm512_unpackhi_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpackhigh_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpackhigh_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.UnpackHigh");
    return dn2cpp_isa_vec<64>(_mm512_unpackhi_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpackhigh_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpackhigh_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.UnpackHigh");
    return dn2cpp_isa_vec<64>(_mm512_unpackhi_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpackhigh_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpackhigh_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.UnpackHigh");
    return dn2cpp_isa_vec<64>(_mm512_unpackhi_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpackhigh_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpacklow_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.UnpackLow");
    return dn2cpp_isa_vec<64>(_mm512_unpacklo_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpacklow_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpacklow_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.UnpackLow");
    return dn2cpp_isa_vec<64>(_mm512_unpacklo_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpacklow_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpacklow_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.UnpackLow");
    return dn2cpp_isa_vec<64>(_mm512_unpacklo_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpacklow_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpacklow_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.UnpackLow");
    return dn2cpp_isa_vec<64>(_mm512_unpacklo_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpacklow_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpacklow_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.UnpackLow");
    return dn2cpp_isa_vec<64>(_mm512_unpacklo_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpacklow_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpacklow_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.UnpackLow");
    return dn2cpp_isa_vec<64>(_mm512_unpacklo_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_unpacklow_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_xor_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Xor");
    return dn2cpp_isa_vec<64>(_mm512_xor_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_xor_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_xor_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Xor");
    return dn2cpp_isa_vec<64>(_mm512_xor_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_xor_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_xor_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Xor");
    return dn2cpp_isa_vec<64>(_mm512_xor_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_xor_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_xor_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Xor");
    return dn2cpp_isa_vec<64>(_mm512_xor_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_xor_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_xor_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Xor");
    return dn2cpp_isa_vec<64>(_mm512_xor_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_xor_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_xor_v512u32_v512u32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Xor");
    return dn2cpp_isa_vec<64>(_mm512_xor_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_xor_v512u32_v512u32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_xor_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Xor");
    return dn2cpp_isa_vec<64>(_mm512_xor_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_xor_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_xor_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512F, "System.Runtime.Intrinsics.X86.Avx512F.Xor");
    return dn2cpp_isa_vec<64>(_mm512_xor_si512(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512f_xor_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512F.Xor");
}
#endif
