#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx10v1+V512: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_and_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.And");
    return dn2cpp_isa_vec<64>(_mm512_and_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_and_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_and_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.And");
    return dn2cpp_isa_vec<64>(_mm512_and_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_and_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_andnot_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.AndNot");
    return dn2cpp_isa_vec<64>(_mm512_andnot_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_andnot_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_andnot_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.AndNot");
    return dn2cpp_isa_vec<64>(_mm512_andnot_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_andnot_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_broadcastpairscalartovector512_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.BroadcastPairScalarToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_f32x2(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_broadcastpairscalartovector512_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.BroadcastPairScalarToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_broadcastpairscalartovector512_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.BroadcastPairScalarToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_i32x2(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_broadcastpairscalartovector512_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.BroadcastPairScalarToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_broadcastpairscalartovector512_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.BroadcastPairScalarToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_i32x2(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_broadcastpairscalartovector512_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.BroadcastPairScalarToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_broadcastvector128tovector512_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.BroadcastVector128ToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_f64x2(_mm_loadu_pd(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_broadcastvector128tovector512_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.BroadcastVector128ToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_broadcastvector128tovector512_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.BroadcastVector128ToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_i64x2(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_broadcastvector128tovector512_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.BroadcastVector128ToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_broadcastvector128tovector512_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.BroadcastVector128ToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_i64x2(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_broadcastvector128tovector512_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.BroadcastVector128ToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_broadcastvector256tovector512_pf32(float* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.BroadcastVector256ToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_f32x8(_mm256_loadu_ps(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_broadcastvector256tovector512_pf32(float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.BroadcastVector256ToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_broadcastvector256tovector512_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.BroadcastVector256ToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_i32x8(_mm256_loadu_si256((const __m256i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_broadcastvector256tovector512_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.BroadcastVector256ToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_broadcastvector256tovector512_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.BroadcastVector256ToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcast_i32x8(_mm256_loadu_si256((const __m256i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_broadcastvector256tovector512_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.BroadcastVector256ToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_classify_v512f32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.Classify");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_movm_epi32(_mm512_fpclass_ps_mask(dn2cpp_isa_bits<__m512>(a0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_classify_v512f32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.Classify");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_classify_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.Classify");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_movm_epi64(_mm512_fpclass_pd_mask(dn2cpp_isa_bits<__m512d>(a0), DN2CPP_IMM))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_classify_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.Classify");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_compress_v512i16_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.Compress");
    return dn2cpp_isa_vec<64>(_mm512_mask_compress_epi16(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_compress_v512i16_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_compress_v512i8_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.Compress");
    return dn2cpp_isa_vec<64>(_mm512_mask_compress_epi8(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_compress_v512i8_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_compress_v512u16_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.Compress");
    return dn2cpp_isa_vec<64>(_mm512_mask_compress_epi16(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_compress_v512u16_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_compress_v512u8_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.Compress");
    return dn2cpp_isa_vec<64>(_mm512_mask_compress_epi8(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_compress_v512u8_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_v512_compressstore_pi16_v512i16_v512i16(int16_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.CompressStore");
    _mm512_mask_compressstoreu_epi16((void*)a0, _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_v512_compressstore_pi16_v512i16_v512i16(int16_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_v512_compressstore_pi8_v512i8_v512i8(int8_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.CompressStore");
    _mm512_mask_compressstoreu_epi8((void*)a0, _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_v512_compressstore_pi8_v512i8_v512i8(int8_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_v512_compressstore_pu16_v512u16_v512u16(uint16_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.CompressStore");
    _mm512_mask_compressstoreu_epi16((void*)a0, _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_v512_compressstore_pu16_v512u16_v512u16(uint16_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_v512_compressstore_pu8_v512u8_v512u8(uint8_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.CompressStore");
    _mm512_mask_compressstoreu_epi8((void*)a0, _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx10v1_v512_compressstore_pu8_v512u8_v512u8(uint8_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_v512_converttovector256single_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector256Single");
    return dn2cpp_isa_vec<32>(_mm512_cvtepi64_ps(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_v512_converttovector256single_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector256Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_v512_converttovector256single_v512i64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector256Single");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<32>(_mm512_cvt_roundepi64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_v512_converttovector256single_v512i64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector256Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_v512_converttovector256single_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector256Single");
    return dn2cpp_isa_vec<32>(_mm512_cvtepu64_ps(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_v512_converttovector256single_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector256Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_v512_converttovector256single_v512u64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector256Single");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<32>(_mm512_cvt_roundepu64_ps(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_v512_converttovector256single_v512u64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector256Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512double_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Double");
    return dn2cpp_isa_vec<64>(_mm512_cvtepi64_pd(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512double_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512double_v512i64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Double");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_cvt_roundepi64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512double_v512i64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512double_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Double");
    return dn2cpp_isa_vec<64>(_mm512_cvtepu64_pd(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512double_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512double_v512u64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Double");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_cvt_roundepu64_pd(dn2cpp_isa_bits<__m512i>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512double_v512u64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512int64_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Int64");
    return dn2cpp_isa_vec<64>(_mm512_cvtps_epi64(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512int64_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512int64_v256f32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Int64");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epi64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512int64_v256f32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512int64_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Int64");
    return dn2cpp_isa_vec<64>(_mm512_cvtpd_epi64(dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512int64_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512int64_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Int64");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epi64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512int64_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512int64withtruncation_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Int64WithTruncation");
    return dn2cpp_isa_vec<64>(_mm512_cvttps_epi64(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512int64withtruncation_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Int64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512int64withtruncation_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Int64WithTruncation");
    return dn2cpp_isa_vec<64>(_mm512_cvttpd_epi64(dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512int64withtruncation_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512Int64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512uint64_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512UInt64");
    return dn2cpp_isa_vec<64>(_mm512_cvtps_epu64(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512uint64_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512uint64_v256f32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512UInt64");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_cvt_roundps_epu64(dn2cpp_isa_bits<__m256>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512uint64_v256f32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512uint64_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512UInt64");
    return dn2cpp_isa_vec<64>(_mm512_cvtpd_epu64(dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512uint64_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512uint64_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512UInt64");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_cvt_roundpd_epu64(dn2cpp_isa_bits<__m512d>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512uint64_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512uint64withtruncation_v256f32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512UInt64WithTruncation");
    return dn2cpp_isa_vec<64>(_mm512_cvttps_epu64(dn2cpp_isa_bits<__m256>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512uint64withtruncation_v256f32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512UInt64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512uint64withtruncation_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512UInt64WithTruncation");
    return dn2cpp_isa_vec<64>(_mm512_cvttpd_epu64(dn2cpp_isa_bits<__m512d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_converttovector512uint64withtruncation_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ConvertToVector512UInt64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_detectconflicts_v512i32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.DetectConflicts");
    return dn2cpp_isa_vec<64>(_mm512_conflict_epi32(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_detectconflicts_v512i32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_detectconflicts_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.DetectConflicts");
    return dn2cpp_isa_vec<64>(_mm512_conflict_epi64(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_detectconflicts_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_detectconflicts_v512u32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.DetectConflicts");
    return dn2cpp_isa_vec<64>(_mm512_conflict_epi32(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_detectconflicts_v512u32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_detectconflicts_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.DetectConflicts");
    return dn2cpp_isa_vec<64>(_mm512_conflict_epi64(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_detectconflicts_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.DetectConflicts");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_expand_v512i16_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.Expand");
    return dn2cpp_isa_vec<64>(_mm512_mask_expand_epi16(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_expand_v512i16_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_expand_v512i8_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.Expand");
    return dn2cpp_isa_vec<64>(_mm512_mask_expand_epi8(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_expand_v512i8_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_expand_v512u16_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.Expand");
    return dn2cpp_isa_vec<64>(_mm512_mask_expand_epi16(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_expand_v512u16_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_expand_v512u8_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.Expand");
    return dn2cpp_isa_vec<64>(_mm512_mask_expand_epi8(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_expand_v512u8_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_expandload_pi16_v512i16_v512i16(int16_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ExpandLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_expandloadu_epi16(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_expandload_pi16_v512i16_v512i16(int16_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_expandload_pi8_v512i8_v512i8(int8_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ExpandLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_expandloadu_epi8(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_expandload_pi8_v512i8_v512i8(int8_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_expandload_pu16_v512u16_v512u16(uint16_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ExpandLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_expandloadu_epi16(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_expandload_pu16_v512u16_v512u16(uint16_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_expandload_pu8_v512u8_v512u8(uint8_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ExpandLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_expandloadu_epi8(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_expandload_pu8_v512u8_v512u8(uint8_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_v512_extractvector128_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm512_extractf64x2_pd(dn2cpp_isa_bits<__m512d>(a0), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_v512_extractvector128_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_v512_extractvector128_v512i64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm512_extracti64x2_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_v512_extractvector128_v512i64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_v512_extractvector128_v512u64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm512_extracti64x2_epi64(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx10v1_v512_extractvector128_v512u64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_v512_extractvector256_v512f32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ExtractVector256");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm512_extractf32x8_ps(dn2cpp_isa_bits<__m512>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_v512_extractvector256_v512f32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ExtractVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_v512_extractvector256_v512i32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ExtractVector256");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm512_extracti32x8_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_v512_extractvector256_v512i32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ExtractVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_v512_extractvector256_v512u32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.ExtractVector256");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm512_extracti32x8_epi32(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx10v1_v512_extractvector256_v512u32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.ExtractVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_insertvector128_v512f64_v128f64_u8(const Dn2CppVector512& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_insertf64x2(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_insertvector128_v512f64_v128f64_u8(const Dn2CppVector512&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_insertvector128_v512i64_v128i64_u8(const Dn2CppVector512& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti64x2(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_insertvector128_v512i64_v128i64_u8(const Dn2CppVector512&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_insertvector128_v512u64_v128u64_u8(const Dn2CppVector512& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti64x2(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_insertvector128_v512u64_v128u64_u8(const Dn2CppVector512&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_insertvector256_v512f32_v256f32_u8(const Dn2CppVector512& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.InsertVector256");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_insertf32x8(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m256>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_insertvector256_v512f32_v256f32_u8(const Dn2CppVector512&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.InsertVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_insertvector256_v512i32_v256i32_u8(const Dn2CppVector512& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.InsertVector256");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti32x8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_insertvector256_v512i32_v256i32_u8(const Dn2CppVector512&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.InsertVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_insertvector256_v512u32_v256u32_u8(const Dn2CppVector512& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.InsertVector256");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_inserti32x8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_insertvector256_v512u32_v256u32_u8(const Dn2CppVector512&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.InsertVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_leadingzerocount_v512i32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.LeadingZeroCount");
    return dn2cpp_isa_vec<64>(_mm512_lzcnt_epi32(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_leadingzerocount_v512i32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_leadingzerocount_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.LeadingZeroCount");
    return dn2cpp_isa_vec<64>(_mm512_lzcnt_epi64(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_leadingzerocount_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_leadingzerocount_v512u32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.LeadingZeroCount");
    return dn2cpp_isa_vec<64>(_mm512_lzcnt_epi32(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_leadingzerocount_v512u32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_leadingzerocount_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.LeadingZeroCount");
    return dn2cpp_isa_vec<64>(_mm512_lzcnt_epi64(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_leadingzerocount_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.LeadingZeroCount");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_v512_movemask_v512f64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.MoveMask");
    return (int32_t)_mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_v512_movemask_v512f64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_v512_movemask_v512i64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.MoveMask");
    return (int32_t)_mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_v512_movemask_v512i64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_v512_movemask_v512u64(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.MoveMask");
    return (int32_t)_mm512_movepi64_mask(dn2cpp_isa_bits<__m512i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx10v1_v512_movemask_v512u64(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_multiplylow_v512i64_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.MultiplyLow");
    return dn2cpp_isa_vec<64>(_mm512_mullo_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_multiplylow_v512i64_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_multiplylow_v512u64_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.MultiplyLow");
    return dn2cpp_isa_vec<64>(_mm512_mullo_epi64(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_multiplylow_v512u64_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_multishift_v512i8_v512i64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.MultiShift");
    return dn2cpp_isa_vec<64>(_mm512_multishift_epi64_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_multishift_v512i8_v512i64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.MultiShift");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_multishift_v512u8_v512u64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.MultiShift");
    return dn2cpp_isa_vec<64>(_mm512_multishift_epi64_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_multishift_v512u8_v512u64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.MultiShift");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_or_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.Or");
    return dn2cpp_isa_vec<64>(_mm512_or_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_or_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_or_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.Or");
    return dn2cpp_isa_vec<64>(_mm512_or_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_or_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_permutevar64x8_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.PermuteVar64x8");
    return dn2cpp_isa_vec<64>(_mm512_permutexvar_epi8(dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_permutevar64x8_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.PermuteVar64x8");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_permutevar64x8_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.PermuteVar64x8");
    return dn2cpp_isa_vec<64>(_mm512_permutexvar_epi8(dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_permutevar64x8_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.PermuteVar64x8");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_permutevar64x8x2_v512i8_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.PermuteVar64x8x2");
    return dn2cpp_isa_vec<64>(_mm512_permutex2var_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_permutevar64x8x2_v512i8_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.PermuteVar64x8x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_permutevar64x8x2_v512u8_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.PermuteVar64x8x2");
    return dn2cpp_isa_vec<64>(_mm512_permutex2var_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_permutevar64x8x2_v512u8_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.PermuteVar64x8x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_range_v512f32_v512f32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.Range");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<64>(_mm512_range_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_range_v512f32_v512f32_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.Range");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_range_v512f64_v512f64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.Range");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 16, a2, dn2cpp_isa_vec<64>(_mm512_range_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_range_v512f64_v512f64_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.Range");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_reduce_v512f32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.Reduce");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_reduce_ps(dn2cpp_isa_bits<__m512>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_reduce_v512f32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.Reduce");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_reduce_v512f64_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.Reduce");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_reduce_pd(dn2cpp_isa_bits<__m512d>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_reduce_v512f64_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.Reduce");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_xor_v512f32_v512f32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.Xor");
    return dn2cpp_isa_vec<64>(_mm512_xor_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_xor_v512f32_v512f32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.1-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_xor_v512f64_v512f64(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v1_V512, "System.Runtime.Intrinsics.X86.Avx10v1+V512.Xor");
    return dn2cpp_isa_vec<64>(_mm512_xor_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v1_v512_xor_v512f64_v512f64(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v1+V512.Xor");
}
#endif
