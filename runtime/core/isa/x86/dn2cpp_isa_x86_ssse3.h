#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Ssse3: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_abs_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.Abs");
    return dn2cpp_isa_vec<16>(_mm_abs_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_abs_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.Abs");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_abs_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.Abs");
    return dn2cpp_isa_vec<16>(_mm_abs_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_abs_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.Abs");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_abs_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.Abs");
    return dn2cpp_isa_vec<16>(_mm_abs_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_abs_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.Abs");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_alignright_v128i16_v128i16_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.AlignRight");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_alignr_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_alignright_v128i16_v128i16_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.AlignRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_alignright_v128i32_v128i32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.AlignRight");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_alignr_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_alignright_v128i32_v128i32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.AlignRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_alignright_v128i64_v128i64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.AlignRight");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_alignr_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_alignright_v128i64_v128i64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.AlignRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_alignright_v128i8_v128i8_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.AlignRight");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_alignr_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_alignright_v128i8_v128i8_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.AlignRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_alignright_v128u16_v128u16_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.AlignRight");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_alignr_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_alignright_v128u16_v128u16_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.AlignRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_alignright_v128u32_v128u32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.AlignRight");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_alignr_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_alignright_v128u32_v128u32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.AlignRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_alignright_v128u64_v128u64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.AlignRight");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_alignr_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_alignright_v128u64_v128u64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.AlignRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_alignright_v128u8_v128u8_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.AlignRight");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_alignr_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_alignright_v128u8_v128u8_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.AlignRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_horizontaladd_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.HorizontalAdd");
    return dn2cpp_isa_vec<16>(_mm_hadd_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_horizontaladd_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.HorizontalAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_horizontaladd_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.HorizontalAdd");
    return dn2cpp_isa_vec<16>(_mm_hadd_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_horizontaladd_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.HorizontalAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_horizontaladdsaturate_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.HorizontalAddSaturate");
    return dn2cpp_isa_vec<16>(_mm_hadds_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_horizontaladdsaturate_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.HorizontalAddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_horizontalsubtract_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.HorizontalSubtract");
    return dn2cpp_isa_vec<16>(_mm_hsub_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_horizontalsubtract_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.HorizontalSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_horizontalsubtract_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.HorizontalSubtract");
    return dn2cpp_isa_vec<16>(_mm_hsub_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_horizontalsubtract_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.HorizontalSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_horizontalsubtractsaturate_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.HorizontalSubtractSaturate");
    return dn2cpp_isa_vec<16>(_mm_hsubs_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_horizontalsubtractsaturate_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.HorizontalSubtractSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_multiplyaddadjacent_v128u8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.MultiplyAddAdjacent");
    return dn2cpp_isa_vec<16>(_mm_maddubs_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_multiplyaddadjacent_v128u8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.MultiplyAddAdjacent");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_multiplyhighroundscale_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.MultiplyHighRoundScale");
    return dn2cpp_isa_vec<16>(_mm_mulhrs_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_multiplyhighroundscale_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.MultiplyHighRoundScale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_shuffle_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.Shuffle");
    return dn2cpp_isa_vec<16>(_mm_shuffle_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_shuffle_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.Shuffle");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_shuffle_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.Shuffle");
    return dn2cpp_isa_vec<16>(_mm_shuffle_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_shuffle_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.Shuffle");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_sign_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.Sign");
    return dn2cpp_isa_vec<16>(_mm_sign_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_sign_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.Sign");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_sign_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.Sign");
    return dn2cpp_isa_vec<16>(_mm_sign_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_sign_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.Sign");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("ssse3") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_sign_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Ssse3, "System.Runtime.Intrinsics.X86.Ssse3.Sign");
    return dn2cpp_isa_vec<16>(_mm_sign_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_ssse3_sign_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Ssse3.Sign");
}
#endif
