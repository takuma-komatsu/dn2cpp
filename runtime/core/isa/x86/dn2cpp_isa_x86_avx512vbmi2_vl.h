#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_compress_v128i16_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_epi16(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_compress_v128i16_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_compress_v128i8_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_epi8(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_compress_v128i8_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_compress_v128u16_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_epi16(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_compress_v128u16_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_compress_v128u8_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Compress");
    return dn2cpp_isa_vec<16>(_mm_mask_compress_epi8(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_compress_v128u8_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_compress_v256i16_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_epi16(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_compress_v256i16_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_compress_v256i8_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_epi8(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_compress_v256i8_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_compress_v256u16_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_epi16(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_compress_v256u16_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_compress_v256u8_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Compress");
    return dn2cpp_isa_vec<32>(_mm256_mask_compress_epi8(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_compress_v256u8_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_vl_compressstore_pi16_v128i16_v128i16(int16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.CompressStore");
    _mm_mask_compressstoreu_epi16((void*)a0, _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_vl_compressstore_pi16_v128i16_v128i16(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_vl_compressstore_pi16_v256i16_v256i16(int16_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.CompressStore");
    _mm256_mask_compressstoreu_epi16((void*)a0, _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_vl_compressstore_pi16_v256i16_v256i16(int16_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_vl_compressstore_pi8_v128i8_v128i8(int8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.CompressStore");
    _mm_mask_compressstoreu_epi8((void*)a0, _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_vl_compressstore_pi8_v128i8_v128i8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_vl_compressstore_pi8_v256i8_v256i8(int8_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.CompressStore");
    _mm256_mask_compressstoreu_epi8((void*)a0, _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_vl_compressstore_pi8_v256i8_v256i8(int8_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_vl_compressstore_pu16_v128u16_v128u16(uint16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.CompressStore");
    _mm_mask_compressstoreu_epi16((void*)a0, _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_vl_compressstore_pu16_v128u16_v128u16(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_vl_compressstore_pu16_v256u16_v256u16(uint16_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.CompressStore");
    _mm256_mask_compressstoreu_epi16((void*)a0, _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_vl_compressstore_pu16_v256u16_v256u16(uint16_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_vl_compressstore_pu8_v128u8_v128u8(uint8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.CompressStore");
    _mm_mask_compressstoreu_epi8((void*)a0, _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_vl_compressstore_pu8_v128u8_v128u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_vl_compressstore_pu8_v256u8_v256u8(uint8_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.CompressStore");
    _mm256_mask_compressstoreu_epi8((void*)a0, _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_vl_compressstore_pu8_v256u8_v256u8(uint8_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_expand_v128i16_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_epi16(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_expand_v128i16_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_expand_v128i8_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_epi8(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_expand_v128i8_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_expand_v128u16_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_epi16(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_expand_v128u16_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_expand_v128u8_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Expand");
    return dn2cpp_isa_vec<16>(_mm_mask_expand_epi8(dn2cpp_isa_bits<__m128i>(a0), _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_expand_v128u8_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_expand_v256i16_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_epi16(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_expand_v256i16_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_expand_v256i8_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_epi8(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_expand_v256i8_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_expand_v256u16_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_epi16(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_expand_v256u16_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_expand_v256u8_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Expand");
    return dn2cpp_isa_vec<32>(_mm256_mask_expand_epi8(dn2cpp_isa_bits<__m256i>(a0), _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_expand_v256u8_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_expandload_pi16_v128i16_v128i16(int16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_epi16(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_expandload_pi16_v128i16_v128i16(int16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_expandload_pi16_v256i16_v256i16(int16_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_epi16(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_expandload_pi16_v256i16_v256i16(int16_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_expandload_pi8_v128i8_v128i8(int8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_epi8(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_expandload_pi8_v128i8_v128i8(int8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_expandload_pi8_v256i8_v256i8(int8_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_epi8(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_expandload_pi8_v256i8_v256i8(int8_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_expandload_pu16_v128u16_v128u16(uint16_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_epi16(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi16_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_expandload_pu16_v128u16_v128u16(uint16_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_expandload_pu16_v256u16_v256u16(uint16_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_epi16(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi16_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_expandload_pu16_v256u16_v256u16(uint16_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_expandload_pu8_v128u8_v128u8(uint8_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.ExpandLoad");
    return dn2cpp_isa_vec<16>(_mm_mask_expandloadu_epi8(dn2cpp_isa_bits<__m128i>(a2), _mm_movepi8_mask(dn2cpp_isa_bits<__m128i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx512vbmi2_vl_expandload_pu8_v128u8_v128u8(uint8_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_expandload_pu8_v256u8_v256u8(uint8_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2_VL, "System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.ExpandLoad");
    return dn2cpp_isa_vec<32>(_mm256_mask_expandloadu_epi8(dn2cpp_isa_bits<__m256i>(a2), _mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512vbmi2_vl_expandload_pu8_v256u8_v256u8(uint8_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2+VL.ExpandLoad");
}
#endif
