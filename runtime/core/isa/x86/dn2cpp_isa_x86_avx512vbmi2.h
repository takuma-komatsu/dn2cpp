#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx512Vbmi2: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_compress_v512i16_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2, "System.Runtime.Intrinsics.X86.Avx512Vbmi2.Compress");
    return dn2cpp_isa_vec<64>(_mm512_mask_compress_epi16(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_compress_v512i16_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_compress_v512i8_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2, "System.Runtime.Intrinsics.X86.Avx512Vbmi2.Compress");
    return dn2cpp_isa_vec<64>(_mm512_mask_compress_epi8(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_compress_v512i8_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_compress_v512u16_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2, "System.Runtime.Intrinsics.X86.Avx512Vbmi2.Compress");
    return dn2cpp_isa_vec<64>(_mm512_mask_compress_epi16(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_compress_v512u16_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_compress_v512u8_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2, "System.Runtime.Intrinsics.X86.Avx512Vbmi2.Compress");
    return dn2cpp_isa_vec<64>(_mm512_mask_compress_epi8(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_compress_v512u8_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2.Compress");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_compressstore_pi16_v512i16_v512i16(int16_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2, "System.Runtime.Intrinsics.X86.Avx512Vbmi2.CompressStore");
    _mm512_mask_compressstoreu_epi16((void*)a0, _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_compressstore_pi16_v512i16_v512i16(int16_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_compressstore_pi8_v512i8_v512i8(int8_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2, "System.Runtime.Intrinsics.X86.Avx512Vbmi2.CompressStore");
    _mm512_mask_compressstoreu_epi8((void*)a0, _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_compressstore_pi8_v512i8_v512i8(int8_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_compressstore_pu16_v512u16_v512u16(uint16_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2, "System.Runtime.Intrinsics.X86.Avx512Vbmi2.CompressStore");
    _mm512_mask_compressstoreu_epi16((void*)a0, _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_compressstore_pu16_v512u16_v512u16(uint16_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_compressstore_pu8_v512u8_v512u8(uint8_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2, "System.Runtime.Intrinsics.X86.Avx512Vbmi2.CompressStore");
    _mm512_mask_compressstoreu_epi8((void*)a0, _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512vbmi2_compressstore_pu8_v512u8_v512u8(uint8_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2.CompressStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_expand_v512i16_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2, "System.Runtime.Intrinsics.X86.Avx512Vbmi2.Expand");
    return dn2cpp_isa_vec<64>(_mm512_mask_expand_epi16(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_expand_v512i16_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_expand_v512i8_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2, "System.Runtime.Intrinsics.X86.Avx512Vbmi2.Expand");
    return dn2cpp_isa_vec<64>(_mm512_mask_expand_epi8(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_expand_v512i8_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_expand_v512u16_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2, "System.Runtime.Intrinsics.X86.Avx512Vbmi2.Expand");
    return dn2cpp_isa_vec<64>(_mm512_mask_expand_epi16(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_expand_v512u16_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_expand_v512u8_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2, "System.Runtime.Intrinsics.X86.Avx512Vbmi2.Expand");
    return dn2cpp_isa_vec<64>(_mm512_mask_expand_epi8(dn2cpp_isa_bits<__m512i>(a0), _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_expand_v512u8_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2.Expand");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_expandload_pi16_v512i16_v512i16(int16_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2, "System.Runtime.Intrinsics.X86.Avx512Vbmi2.ExpandLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_expandloadu_epi16(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_expandload_pi16_v512i16_v512i16(int16_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_expandload_pi8_v512i8_v512i8(int8_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2, "System.Runtime.Intrinsics.X86.Avx512Vbmi2.ExpandLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_expandloadu_epi8(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_expandload_pi8_v512i8_v512i8(int8_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_expandload_pu16_v512u16_v512u16(uint16_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2, "System.Runtime.Intrinsics.X86.Avx512Vbmi2.ExpandLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_expandloadu_epi16(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_expandload_pu16_v512u16_v512u16(uint16_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2.ExpandLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512vbmi2,avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_expandload_pu8_v512u8_v512u8(uint8_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512Vbmi2, "System.Runtime.Intrinsics.X86.Avx512Vbmi2.ExpandLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_expandloadu_epi8(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512vbmi2_expandload_pu8_v512u8_v512u8(uint8_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512Vbmi2.ExpandLoad");
}
#endif
