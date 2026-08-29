#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx512DQ+VL: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_broadcastpairscalartovector128_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.BroadcastPairScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_broadcast_i32x2(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_broadcastpairscalartovector128_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.BroadcastPairScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_broadcastpairscalartovector128_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.BroadcastPairScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_broadcast_i32x2(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_broadcastpairscalartovector128_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.BroadcastPairScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_broadcastpairscalartovector256_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.BroadcastPairScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcast_f32x2(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_broadcastpairscalartovector256_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.BroadcastPairScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_broadcastpairscalartovector256_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.BroadcastPairScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcast_i32x2(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_broadcastpairscalartovector256_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.BroadcastPairScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_broadcastpairscalartovector256_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.BroadcastPairScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcast_i32x2(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_broadcastpairscalartovector256_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.BroadcastPairScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_classify_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.Classify");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_movm_epi32(_mm_fpclass_ps_mask(dn2cpp_isa_bits<__m128>(a0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_classify_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.Classify");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_classify_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.Classify");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_movm_epi64(_mm_fpclass_pd_mask(dn2cpp_isa_bits<__m128d>(a0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_classify_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.Classify");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_classify_v256f32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.Classify");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_movm_epi32(_mm256_fpclass_ps_mask(dn2cpp_isa_bits<__m256>(a0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_classify_v256f32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.Classify");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_classify_v256f64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.Classify");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_movm_epi64(_mm256_fpclass_pd_mask(dn2cpp_isa_bits<__m256d>(a0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_classify_v256f64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.Classify");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128double_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Double");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_pd(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128double_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128double_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Double");
    return dn2cpp_isa_vec<16>(_mm_cvtepu64_pd(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128double_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128int64_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Int64");
    return dn2cpp_isa_vec<16>(_mm_cvtps_epi64(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128int64_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128int64_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Int64");
    return dn2cpp_isa_vec<16>(_mm_cvtpd_epi64(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128int64_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128int64withtruncation_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Int64WithTruncation");
    return dn2cpp_isa_vec<16>(_mm_cvttps_epi64(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128int64withtruncation_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Int64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128int64withtruncation_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Int64WithTruncation");
    return dn2cpp_isa_vec<16>(_mm_cvttpd_epi64(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128int64withtruncation_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Int64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128single_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Single");
    return dn2cpp_isa_vec<16>(_mm_cvtepi64_ps(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128single_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128single_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Single");
    return dn2cpp_isa_vec<16>(_mm_cvtepu64_ps(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128single_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128single_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Single");
    return dn2cpp_isa_vec<16>(_mm256_cvtepi64_ps(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128single_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128single_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Single");
    return dn2cpp_isa_vec<16>(_mm256_cvtepu64_ps(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128single_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128uint64_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128UInt64");
    return dn2cpp_isa_vec<16>(_mm_cvtps_epu64(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128uint64_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128uint64_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128UInt64");
    return dn2cpp_isa_vec<16>(_mm_cvtpd_epu64(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128uint64_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128uint64withtruncation_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128UInt64WithTruncation");
    return dn2cpp_isa_vec<16>(_mm_cvttps_epu64(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128uint64withtruncation_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128UInt64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128uint64withtruncation_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128UInt64WithTruncation");
    return dn2cpp_isa_vec<16>(_mm_cvttpd_epu64(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_converttovector128uint64withtruncation_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector128UInt64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256double_v256i64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256Double");
    return dn2cpp_isa_vec<32>(_mm256_cvtepi64_pd(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256double_v256i64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256double_v256u64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256Double");
    return dn2cpp_isa_vec<32>(_mm256_cvtepu64_pd(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256double_v256u64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256int64_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256Int64");
    return dn2cpp_isa_vec<32>(_mm256_cvtps_epi64(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256int64_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256int64_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256Int64");
    return dn2cpp_isa_vec<32>(_mm256_cvtpd_epi64(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256int64_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256int64withtruncation_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256Int64WithTruncation");
    return dn2cpp_isa_vec<32>(_mm256_cvttps_epi64(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256int64withtruncation_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256Int64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256int64withtruncation_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256Int64WithTruncation");
    return dn2cpp_isa_vec<32>(_mm256_cvttpd_epi64(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256int64withtruncation_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256Int64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256uint64_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256UInt64");
    return dn2cpp_isa_vec<32>(_mm256_cvtps_epu64(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256uint64_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256uint64_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256UInt64");
    return dn2cpp_isa_vec<32>(_mm256_cvtpd_epu64(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256uint64_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256uint64withtruncation_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256UInt64WithTruncation");
    return dn2cpp_isa_vec<32>(_mm256_cvttps_epu64(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256uint64withtruncation_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256UInt64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256uint64withtruncation_v256f64(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256UInt64WithTruncation");
    return dn2cpp_isa_vec<32>(_mm256_cvttpd_epu64(dn2cpp_isa_bits<__m256d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_converttovector256uint64withtruncation_v256f64(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.ConvertToVector256UInt64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_multiplylow_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.MultiplyLow");
    return dn2cpp_isa_vec<16>(_mm_mullo_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_multiplylow_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_multiplylow_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.MultiplyLow");
    return dn2cpp_isa_vec<16>(_mm_mullo_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_multiplylow_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_multiplylow_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.MultiplyLow");
    return dn2cpp_isa_vec<32>(_mm256_mullo_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_multiplylow_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_multiplylow_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.MultiplyLow");
    return dn2cpp_isa_vec<32>(_mm256_mullo_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_multiplylow_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_range_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.Range");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<16>(_mm_range_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_range_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.Range");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_range_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.Range");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<16>(_mm_range_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_range_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.Range");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_range_v256f32_v256f32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.Range");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<32>(_mm256_range_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_range_v256f32_v256f32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.Range");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_range_v256f64_v256f64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.Range");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<32>(_mm256_range_pd(dn2cpp_isa_bits<__m256d>(a0), dn2cpp_isa_bits<__m256d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_range_v256f64_v256f64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.Range");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_reduce_v128f32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.Reduce");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_reduce_ps(dn2cpp_isa_bits<__m128>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_reduce_v128f32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.Reduce");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_reduce_v128f64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.Reduce");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_reduce_pd(dn2cpp_isa_bits<__m128d>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512dq_vl_reduce_v128f64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.Reduce");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_reduce_v256f32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.Reduce");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_reduce_ps(dn2cpp_isa_bits<__m256>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_reduce_v256f32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.Reduce");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_reduce_v256f64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512DQ_VL, "System.Runtime.Intrinsics.X86.Avx512DQ+VL.Reduce");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_reduce_pd(dn2cpp_isa_bits<__m256d>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512dq_vl_reduce_v256f64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512DQ+VL.Reduce");
}
#endif
