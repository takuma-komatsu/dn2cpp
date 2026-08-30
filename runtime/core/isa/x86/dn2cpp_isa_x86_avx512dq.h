#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx512DQ: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_and_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.And");
    return dn2cpp_isa_vec<64>(_mm512_and_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_and_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_and_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.And");
    return dn2cpp_isa_vec<64>(_mm512_and_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_and_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_andnot_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.AndNot");
    return dn2cpp_isa_vec<64>(_mm512_andnot_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_andnot_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_andnot_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.AndNot");
    return dn2cpp_isa_vec<64>(_mm512_andnot_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_andnot_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_broadcastpairscalartovector512_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.BroadcastPairScalarToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_f32x2(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_broadcastpairscalartovector512_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.BroadcastPairScalarToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_broadcastpairscalartovector512_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.BroadcastPairScalarToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_i32x2(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_broadcastpairscalartovector512_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.BroadcastPairScalarToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_broadcastpairscalartovector512_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.BroadcastPairScalarToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_i32x2(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_broadcastpairscalartovector512_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.BroadcastPairScalarToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_broadcastvector128tovector512_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.BroadcastVector128ToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_f64x2(_mm_loadu_pd(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_broadcastvector128tovector512_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.BroadcastVector128ToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_broadcastvector128tovector512_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.BroadcastVector128ToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_i64x2(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_broadcastvector128tovector512_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.BroadcastVector128ToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_broadcastvector128tovector512_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.BroadcastVector128ToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_i64x2(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_broadcastvector128tovector512_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.BroadcastVector128ToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_broadcastvector256tovector512_pf32(float* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.BroadcastVector256ToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_f32x8(_mm256_loadu_ps(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_broadcastvector256tovector512_pf32(float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.BroadcastVector256ToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_broadcastvector256tovector512_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.BroadcastVector256ToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_i32x8(_mm256_loadu_si256((const __m256i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_broadcastvector256tovector512_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.BroadcastVector256ToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_broadcastvector256tovector512_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.BroadcastVector256ToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_i32x8(_mm256_loadu_si256((const __m256i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_broadcastvector256tovector512_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.BroadcastVector256ToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_classify_v512f32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.Classify");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_fpclass_ps_mask(dn2cpp_isa_bits<__m512>(a0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_classify_v512f32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.Classify");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_classify_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.Classify");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_fpclass_pd_mask(dn2cpp_isa_bits<__m512d>(a0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_classify_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.Classify");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_classifyscalar_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ClassifyScalar");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_fpclass_ss_mask(dn2cpp_isa_bits<__m128>(a0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_classifyscalar_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ClassifyScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_classifyscalar_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ClassifyScalar");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_fpclass_sd_mask(dn2cpp_isa_bits<__m128d>(a0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_classifyscalar_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ClassifyScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_converttovector256single_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector256Single");
    return dn2cpp_isa_vec<32>(_mm512_cvtepi64_ps(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_converttovector256single_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector256Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_converttovector256single_v512i64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector256Single");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_converttovector256single_v512i64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector256Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_converttovector256single_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector256Single");
    return dn2cpp_isa_vec<32>(_mm512_cvtepu64_ps(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_converttovector256single_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector256Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_converttovector256single_v512u64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector256Single");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_converttovector256single_v512u64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector256Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512double_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Double");
    return dn2cpp_isa_vec<64>(_mm512_cvtepi64_pd(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512double_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512double_v512i64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Double");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512double_v512i64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512double_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Double");
    return dn2cpp_isa_vec<64>(_mm512_cvtepu64_pd(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512double_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512double_v512u64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Double");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512double_v512u64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512int64_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Int64");
    return dn2cpp_isa_vec<64>(_mm512_cvtps_epi64(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512int64_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512int64_v256f32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Int64");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512int64_v256f32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512int64_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Int64");
    return dn2cpp_isa_vec<64>(_mm512_cvtpd_epi64(dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512int64_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512int64_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Int64");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512int64_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512int64withtruncation_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Int64WithTruncation");
    return dn2cpp_isa_vec<64>(_mm512_cvttps_epi64(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512int64withtruncation_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Int64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512int64withtruncation_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Int64WithTruncation");
    return dn2cpp_isa_vec<64>(_mm512_cvttpd_epi64(dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512int64withtruncation_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512Int64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512uint64_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512UInt64");
    return dn2cpp_isa_vec<64>(_mm512_cvtps_epu64(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512uint64_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512uint64_v256f32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512UInt64");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512uint64_v256f32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512uint64_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512UInt64");
    return dn2cpp_isa_vec<64>(_mm512_cvtpd_epu64(dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512uint64_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512uint64_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512UInt64");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512uint64_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512uint64withtruncation_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512UInt64WithTruncation");
    return dn2cpp_isa_vec<64>(_mm512_cvttps_epu64(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512uint64withtruncation_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512UInt64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512uint64withtruncation_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512UInt64WithTruncation");
    return dn2cpp_isa_vec<64>(_mm512_cvttpd_epu64(dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_converttovector512uint64withtruncation_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ConvertToVector512UInt64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_extractvector128_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm512_extractf64x2_pd(dn2cpp_isa_bits<__m512d>(a0), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_extractvector128_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_extractvector128_v512i64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm512_extracti64x2_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_extractvector128_v512i64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_extractvector128_v512u64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm512_extracti64x2_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_extractvector128_v512u64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_extractvector256_v512f32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ExtractVector256");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm512_extractf32x8_ps(dn2cpp_isa_bits<__m512>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_extractvector256_v512f32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ExtractVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_extractvector256_v512i32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ExtractVector256");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm512_extracti32x8_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_extractvector256_v512i32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ExtractVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_extractvector256_v512u32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ExtractVector256");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm512_extracti32x8_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_extractvector256_v512u32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ExtractVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_insertvector128_v512f64_v128f64_u8(const Dn2CppVector512& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_insertf64x2(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_insertvector128_v512f64_v128f64_u8(const Dn2CppVector512&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_insertvector128_v512i64_v128i64_u8(const Dn2CppVector512& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti64x2(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_insertvector128_v512i64_v128i64_u8(const Dn2CppVector512&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_insertvector128_v512u64_v128u64_u8(const Dn2CppVector512& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti64x2(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_insertvector128_v512u64_v128u64_u8(const Dn2CppVector512&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_insertvector256_v512f32_v256f32_u8(const Dn2CppVector512& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.InsertVector256");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_insertf32x8(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m256>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_insertvector256_v512f32_v256f32_u8(const Dn2CppVector512&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.InsertVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_insertvector256_v512i32_v256i32_u8(const Dn2CppVector512& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.InsertVector256");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti32x8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_insertvector256_v512i32_v256i32_u8(const Dn2CppVector512&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.InsertVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_insertvector256_v512u32_v256u32_u8(const Dn2CppVector512& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.InsertVector256");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti32x8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_insertvector256_v512u32_v256u32_u8(const Dn2CppVector512&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.InsertVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
    return (int32_t)_mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
    return (int32_t)_mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
    return (int32_t)_mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
    return (int32_t)_mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
    return (int32_t)_mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
    return (int32_t)_mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
    return (int32_t)_mm_movepi32_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
    return (int32_t)_mm_movepi64_mask(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
    return (int32_t)_mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
    return (int32_t)_mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
    return (int32_t)_mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
    return (int32_t)_mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
    return (int32_t)_mm256_movepi32_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
    return (int32_t)_mm256_movepi64_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
    return (int32_t)_mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
    return (int32_t)_mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
    return (int32_t)_mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512dq_movemask_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_multiplylow_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MultiplyLow");
    return dn2cpp_isa_vec<64>(_mm512_mullo_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_multiplylow_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_multiplylow_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.MultiplyLow");
    return dn2cpp_isa_vec<64>(_mm512_mullo_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_multiplylow_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_or_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.Or");
    return dn2cpp_isa_vec<64>(_mm512_or_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_or_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_or_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.Or");
    return dn2cpp_isa_vec<64>(_mm512_or_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_or_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_range_v512f32_v512f32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.Range");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<64>(_mm512_range_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_range_v512f32_v512f32_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.Range");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_range_v512f64_v512f64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.Range");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<64>(_mm512_range_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_range_v512f64_v512f64_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.Range");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_rangescalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.RangeScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<16>(_mm_range_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_rangescalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.RangeScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_rangescalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.RangeScalar");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<16>(_mm_range_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_rangescalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.RangeScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_reduce_v512f32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.Reduce");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_reduce_ps(dn2cpp_isa_bits<__m512>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_reduce_v512f32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.Reduce");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_reduce_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.Reduce");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_reduce_pd(dn2cpp_isa_bits<__m512d>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_reduce_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.Reduce");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_reducescalar_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ReduceScalar");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_reduce_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_reducescalar_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ReduceScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_reducescalar_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ReduceScalar");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_reduce_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_reducescalar_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ReduceScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_reducescalar_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ReduceScalar");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_reduce_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_reducescalar_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ReduceScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_reducescalar_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.ReduceScalar");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_reduce_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_reducescalar_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.ReduceScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_xor_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.Xor");
    return dn2cpp_isa_vec<64>(_mm512_xor_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_xor_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_xor_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ, "System.Runtime.Intrinsics.X86.Avx512DQ.Xor");
    return dn2cpp_isa_vec<64>(_mm512_xor_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512dq_xor_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ.Xor");
}
#endif
