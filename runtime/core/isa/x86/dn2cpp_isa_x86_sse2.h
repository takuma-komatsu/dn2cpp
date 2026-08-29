#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Sse2: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_add_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Add");
    return dn2cpp_isa_vec<16>(_mm_add_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_add_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_add_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Add");
    return dn2cpp_isa_vec<16>(_mm_add_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_add_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_add_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Add");
    return dn2cpp_isa_vec<16>(_mm_add_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_add_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_add_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Add");
    return dn2cpp_isa_vec<16>(_mm_add_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_add_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_add_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Add");
    return dn2cpp_isa_vec<16>(_mm_add_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_add_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_add_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Add");
    return dn2cpp_isa_vec<16>(_mm_add_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_add_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_add_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Add");
    return dn2cpp_isa_vec<16>(_mm_add_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_add_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_add_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Add");
    return dn2cpp_isa_vec<16>(_mm_add_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_add_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_add_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Add");
    return dn2cpp_isa_vec<16>(_mm_add_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_add_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_addsaturate_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.AddSaturate");
    return dn2cpp_isa_vec<16>(_mm_adds_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_addsaturate_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.AddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_addsaturate_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.AddSaturate");
    return dn2cpp_isa_vec<16>(_mm_adds_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_addsaturate_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.AddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_addsaturate_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.AddSaturate");
    return dn2cpp_isa_vec<16>(_mm_adds_epu16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_addsaturate_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.AddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_addsaturate_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.AddSaturate");
    return dn2cpp_isa_vec<16>(_mm_adds_epu8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_addsaturate_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.AddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_addscalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.AddScalar");
    return dn2cpp_isa_vec<16>(_mm_add_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_addscalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.AddScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_and_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.And");
    return dn2cpp_isa_vec<16>(_mm_and_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_and_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_and_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.And");
    return dn2cpp_isa_vec<16>(_mm_and_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_and_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_and_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.And");
    return dn2cpp_isa_vec<16>(_mm_and_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_and_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_and_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.And");
    return dn2cpp_isa_vec<16>(_mm_and_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_and_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_and_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.And");
    return dn2cpp_isa_vec<16>(_mm_and_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_and_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_and_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.And");
    return dn2cpp_isa_vec<16>(_mm_and_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_and_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_and_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.And");
    return dn2cpp_isa_vec<16>(_mm_and_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_and_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_and_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.And");
    return dn2cpp_isa_vec<16>(_mm_and_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_and_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_and_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.And");
    return dn2cpp_isa_vec<16>(_mm_and_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_and_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_andnot_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.AndNot");
    return dn2cpp_isa_vec<16>(_mm_andnot_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_andnot_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_andnot_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.AndNot");
    return dn2cpp_isa_vec<16>(_mm_andnot_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_andnot_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_andnot_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.AndNot");
    return dn2cpp_isa_vec<16>(_mm_andnot_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_andnot_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_andnot_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.AndNot");
    return dn2cpp_isa_vec<16>(_mm_andnot_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_andnot_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_andnot_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.AndNot");
    return dn2cpp_isa_vec<16>(_mm_andnot_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_andnot_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_andnot_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.AndNot");
    return dn2cpp_isa_vec<16>(_mm_andnot_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_andnot_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_andnot_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.AndNot");
    return dn2cpp_isa_vec<16>(_mm_andnot_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_andnot_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_andnot_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.AndNot");
    return dn2cpp_isa_vec<16>(_mm_andnot_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_andnot_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_andnot_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.AndNot");
    return dn2cpp_isa_vec<16>(_mm_andnot_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_andnot_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_average_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Average");
    return dn2cpp_isa_vec<16>(_mm_avg_epu16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_average_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Average");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_average_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Average");
    return dn2cpp_isa_vec<16>(_mm_avg_epu8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_average_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Average");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_compareequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpeq_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_compareequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_compareequal_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpeq_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_compareequal_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_compareequal_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpeq_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_compareequal_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_compareequal_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpeq_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_compareequal_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_compareequal_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpeq_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_compareequal_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_compareequal_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpeq_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_compareequal_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_compareequal_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpeq_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_compareequal_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparegreaterthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_cmpgt_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparegreaterthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparegreaterthan_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_cmpgt_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparegreaterthan_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparegreaterthan_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_cmpgt_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparegreaterthan_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparegreaterthan_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_cmpgt_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparegreaterthan_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparegreaterthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpge_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparegreaterthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparelessthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_cmplt_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparelessthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparelessthan_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_cmplt_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparelessthan_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparelessthan_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_cmplt_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparelessthan_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparelessthan_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_cmplt_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparelessthan_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparelessthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_cmple_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparelessthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparenotequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpneq_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparenotequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparenotgreaterthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareNotGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_cmpngt_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparenotgreaterthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareNotGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparenotgreaterthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareNotGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpnge_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparenotgreaterthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareNotGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparenotlessthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareNotLessThan");
    return dn2cpp_isa_vec<16>(_mm_cmpnlt_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparenotlessthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareNotLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparenotlessthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareNotLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpnle_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparenotlessthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareNotLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_compareordered_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareOrdered");
    return dn2cpp_isa_vec<16>(_mm_cmpord_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_compareordered_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareOrdered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpeq_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalargreaterthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_cmpgt_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalargreaterthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalargreaterthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpge_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalargreaterthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarlessthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarLessThan");
    return dn2cpp_isa_vec<16>(_mm_cmplt_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarlessthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarlessthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_cmple_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarlessthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarnotequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarNotEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpneq_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarnotequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarnotgreaterthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarNotGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_cmpngt_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarnotgreaterthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarNotGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarnotgreaterthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarNotGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpnge_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarnotgreaterthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarNotGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarnotlessthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarNotLessThan");
    return dn2cpp_isa_vec<16>(_mm_cmpnlt_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarnotlessthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarNotLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarnotlessthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarNotLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpnle_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarnotlessthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarNotLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarordered_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarOrdered");
    return dn2cpp_isa_vec<16>(_mm_cmpord_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarordered_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarOrdered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarorderedequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarOrderedEqual");
    return _mm_comieq_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarorderedequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarOrderedEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarorderedgreaterthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarOrderedGreaterThan");
    return _mm_comigt_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarorderedgreaterthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarOrderedGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarorderedgreaterthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarOrderedGreaterThanOrEqual");
    return _mm_comige_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarorderedgreaterthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarOrderedGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarorderedlessthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarOrderedLessThan");
    return _mm_comilt_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarorderedlessthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarOrderedLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarorderedlessthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarOrderedLessThanOrEqual");
    return _mm_comile_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarorderedlessthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarOrderedLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarorderednotequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarOrderedNotEqual");
    return _mm_comineq_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarorderednotequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarOrderedNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarunordered_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarUnordered");
    return dn2cpp_isa_vec<16>(_mm_cmpunord_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_comparescalarunordered_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarUnordered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarunorderedequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarUnorderedEqual");
    return _mm_ucomieq_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarunorderedequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarUnorderedEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarunorderedgreaterthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarUnorderedGreaterThan");
    return _mm_ucomigt_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarunorderedgreaterthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarUnorderedGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarunorderedgreaterthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarUnorderedGreaterThanOrEqual");
    return _mm_ucomige_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarunorderedgreaterthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarUnorderedGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarunorderedlessthan_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarUnorderedLessThan");
    return _mm_ucomilt_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarunorderedlessthan_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarUnorderedLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarunorderedlessthanorequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarUnorderedLessThanOrEqual");
    return _mm_ucomile_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarunorderedlessthanorequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarUnorderedLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarunorderednotequal_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareScalarUnorderedNotEqual");
    return _mm_ucomineq_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse2_comparescalarunorderednotequal_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareScalarUnorderedNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_compareunordered_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.CompareUnordered");
    return dn2cpp_isa_vec<16>(_mm_cmpunord_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_compareunordered_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.CompareUnordered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_convertscalartovector128double_v128f64_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ConvertScalarToVector128Double");
    return dn2cpp_isa_vec<16>(_mm_cvtsi32_sd(dn2cpp_isa_bits<__m128d>(a0), a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_convertscalartovector128double_v128f64_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ConvertScalarToVector128Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_convertscalartovector128double_v128f64_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ConvertScalarToVector128Double");
    return dn2cpp_isa_vec<16>(_mm_cvtss_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_convertscalartovector128double_v128f64_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ConvertScalarToVector128Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_convertscalartovector128int32_i32(int32_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ConvertScalarToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm_cvtsi32_si128(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_convertscalartovector128int32_i32(int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ConvertScalarToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_convertscalartovector128single_v128f32_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ConvertScalarToVector128Single");
    return dn2cpp_isa_vec<16>(_mm_cvtsd_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_convertscalartovector128single_v128f32_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ConvertScalarToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_convertscalartovector128uint32_u32(uint32_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ConvertScalarToVector128UInt32");
    return dn2cpp_isa_vec<16>(_mm_cvtsi32_si128((int)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_convertscalartovector128uint32_u32(uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ConvertScalarToVector128UInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse2_converttoint32_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ConvertToInt32");
    return _mm_cvtsd_si32(dn2cpp_isa_bits<__m128d>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse2_converttoint32_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ConvertToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse2_converttoint32_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ConvertToInt32");
    return _mm_cvtsi128_si32(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse2_converttoint32_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ConvertToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse2_converttoint32withtruncation_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ConvertToInt32WithTruncation");
    return _mm_cvttsd_si32(dn2cpp_isa_bits<__m128d>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse2_converttoint32withtruncation_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ConvertToInt32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_sse2_converttouint32_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ConvertToUInt32");
    return (uint32_t)_mm_cvtsi128_si32(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_sse2_converttouint32_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ConvertToUInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_converttovector128double_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ConvertToVector128Double");
    return dn2cpp_isa_vec<16>(_mm_cvtps_pd(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_converttovector128double_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ConvertToVector128Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_converttovector128double_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ConvertToVector128Double");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_pd(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_converttovector128double_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ConvertToVector128Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_converttovector128int32_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm_cvtps_epi32(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_converttovector128int32_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_converttovector128int32_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ConvertToVector128Int32");
    return dn2cpp_isa_vec<16>(_mm_cvtpd_epi32(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_converttovector128int32_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ConvertToVector128Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_converttovector128int32withtruncation_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ConvertToVector128Int32WithTruncation");
    return dn2cpp_isa_vec<16>(_mm_cvttps_epi32(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_converttovector128int32withtruncation_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ConvertToVector128Int32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_converttovector128int32withtruncation_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ConvertToVector128Int32WithTruncation");
    return dn2cpp_isa_vec<16>(_mm_cvttpd_epi32(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_converttovector128int32withtruncation_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ConvertToVector128Int32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_converttovector128single_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ConvertToVector128Single");
    return dn2cpp_isa_vec<16>(_mm_cvtpd_ps(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_converttovector128single_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ConvertToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_converttovector128single_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ConvertToVector128Single");
    return dn2cpp_isa_vec<16>(_mm_cvtepi32_ps(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_converttovector128single_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ConvertToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_divide_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Divide");
    return dn2cpp_isa_vec<16>(_mm_div_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_divide_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Divide");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_dividescalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.DivideScalar");
    return dn2cpp_isa_vec<16>(_mm_div_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_dividescalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.DivideScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE uint16_t dn2cpp_isa_x86_sse2_extract_v128u16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a1, (uint16_t)_mm_extract_epi16(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint16_t dn2cpp_isa_x86_sse2_extract_v128u16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Extract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_insert_v128i16_i16_u8(const Dn2CppVector128& a0, int16_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a2, dn2cpp_isa_vec<16>(_mm_insert_epi16(dn2cpp_isa_bits<__m128i>(a0), (int)a1, DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_insert_v128i16_i16_u8(const Dn2CppVector128&, int16_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Insert");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_insert_v128u16_u16_u8(const Dn2CppVector128& a0, uint16_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 8, a2, dn2cpp_isa_vec<16>(_mm_insert_epi16(dn2cpp_isa_bits<__m128i>(a0), (int)a1, DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_insert_v128u16_u16_u8(const Dn2CppVector128&, uint16_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Insert");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadalignedvector128_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadAlignedVector128");
    return dn2cpp_isa_vec<16>(_mm_load_pd(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadalignedvector128_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadAlignedVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadalignedvector128_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadAlignedVector128");
    return dn2cpp_isa_vec<16>(_mm_load_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadalignedvector128_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadAlignedVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadalignedvector128_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadAlignedVector128");
    return dn2cpp_isa_vec<16>(_mm_load_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadalignedvector128_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadAlignedVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadalignedvector128_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadAlignedVector128");
    return dn2cpp_isa_vec<16>(_mm_load_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadalignedvector128_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadAlignedVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadalignedvector128_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadAlignedVector128");
    return dn2cpp_isa_vec<16>(_mm_load_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadalignedvector128_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadAlignedVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadalignedvector128_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadAlignedVector128");
    return dn2cpp_isa_vec<16>(_mm_load_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadalignedvector128_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadAlignedVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadalignedvector128_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadAlignedVector128");
    return dn2cpp_isa_vec<16>(_mm_load_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadalignedvector128_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadAlignedVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadalignedvector128_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadAlignedVector128");
    return dn2cpp_isa_vec<16>(_mm_load_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadalignedvector128_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadAlignedVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadalignedvector128_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadAlignedVector128");
    return dn2cpp_isa_vec<16>(_mm_load_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadalignedvector128_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadAlignedVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_loadfence()
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadFence");
    _mm_lfence();
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_loadfence()
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadFence");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadhigh_v128f64_pf64(const Dn2CppVector128& a0, double* a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadHigh");
    return dn2cpp_isa_vec<16>(_mm_loadh_pd(dn2cpp_isa_bits<__m128d>(a0), a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadhigh_v128f64_pf64(const Dn2CppVector128&, double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadlow_v128f64_pf64(const Dn2CppVector128& a0, double* a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadLow");
    return dn2cpp_isa_vec<16>(_mm_loadl_pd(dn2cpp_isa_bits<__m128d>(a0), a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadlow_v128f64_pf64(const Dn2CppVector128&, double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadscalarvector128_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadScalarVector128");
    return dn2cpp_isa_vec<16>(_mm_load_sd(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadscalarvector128_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadScalarVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadscalarvector128_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadScalarVector128");
    return dn2cpp_isa_vec<16>(_mm_loadu_si32(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadscalarvector128_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadScalarVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadscalarvector128_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadScalarVector128");
    return dn2cpp_isa_vec<16>(_mm_loadl_epi64((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadscalarvector128_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadScalarVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadscalarvector128_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadScalarVector128");
    return dn2cpp_isa_vec<16>(_mm_loadu_si32(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadscalarvector128_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadScalarVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadscalarvector128_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadScalarVector128");
    return dn2cpp_isa_vec<16>(_mm_loadl_epi64((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadscalarvector128_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadScalarVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadvector128_pf64(double* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadVector128");
    return dn2cpp_isa_vec<16>(_mm_loadu_pd(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadvector128_pf64(double*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadvector128_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadVector128");
    return dn2cpp_isa_vec<16>(_mm_loadu_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadvector128_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadvector128_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadVector128");
    return dn2cpp_isa_vec<16>(_mm_loadu_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadvector128_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadvector128_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadVector128");
    return dn2cpp_isa_vec<16>(_mm_loadu_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadvector128_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadvector128_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadVector128");
    return dn2cpp_isa_vec<16>(_mm_loadu_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadvector128_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadvector128_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadVector128");
    return dn2cpp_isa_vec<16>(_mm_loadu_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadvector128_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadvector128_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadVector128");
    return dn2cpp_isa_vec<16>(_mm_loadu_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadvector128_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadvector128_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadVector128");
    return dn2cpp_isa_vec<16>(_mm_loadu_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadvector128_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadvector128_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.LoadVector128");
    return dn2cpp_isa_vec<16>(_mm_loadu_si128((const __m128i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_loadvector128_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.LoadVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_maskmove_v128i8_v128i8_pi8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, int8_t* a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.MaskMove");
    _mm_maskmoveu_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), (char*)a2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_maskmove_v128i8_v128i8_pi8(const Dn2CppVector128&, const Dn2CppVector128&, int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.MaskMove");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_maskmove_v128u8_v128u8_pu8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t* a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.MaskMove");
    _mm_maskmoveu_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), (char*)a2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_maskmove_v128u8_v128u8_pu8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.MaskMove");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_max_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Max");
    return dn2cpp_isa_vec<16>(_mm_max_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_max_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_max_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Max");
    return dn2cpp_isa_vec<16>(_mm_max_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_max_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_max_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Max");
    return dn2cpp_isa_vec<16>(_mm_max_epu8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_max_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_maxscalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.MaxScalar");
    return dn2cpp_isa_vec<16>(_mm_max_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_maxscalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.MaxScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_memoryfence()
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.MemoryFence");
    _mm_mfence();
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_memoryfence()
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.MemoryFence");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_min_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Min");
    return dn2cpp_isa_vec<16>(_mm_min_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_min_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_min_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Min");
    return dn2cpp_isa_vec<16>(_mm_min_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_min_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_min_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Min");
    return dn2cpp_isa_vec<16>(_mm_min_epu8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_min_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_minscalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.MinScalar");
    return dn2cpp_isa_vec<16>(_mm_min_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_minscalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.MinScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse2_movemask_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.MoveMask");
    return _mm_movemask_pd(dn2cpp_isa_bits<__m128d>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse2_movemask_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse2_movemask_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.MoveMask");
    return _mm_movemask_epi8(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse2_movemask_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse2_movemask_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.MoveMask");
    return _mm_movemask_epi8(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse2_movemask_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_movescalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.MoveScalar");
    return dn2cpp_isa_vec<16>(_mm_move_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_movescalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.MoveScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_movescalar_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.MoveScalar");
    return dn2cpp_isa_vec<16>(_mm_move_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_movescalar_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.MoveScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_movescalar_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.MoveScalar");
    return dn2cpp_isa_vec<16>(_mm_move_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_movescalar_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.MoveScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_multiply_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Multiply");
    return dn2cpp_isa_vec<16>(_mm_mul_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_multiply_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Multiply");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_multiply_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Multiply");
    return dn2cpp_isa_vec<16>(_mm_mul_epu32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_multiply_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Multiply");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_multiplyaddadjacent_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.MultiplyAddAdjacent");
    return dn2cpp_isa_vec<16>(_mm_madd_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_multiplyaddadjacent_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.MultiplyAddAdjacent");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_multiplyhigh_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.MultiplyHigh");
    return dn2cpp_isa_vec<16>(_mm_mulhi_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_multiplyhigh_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.MultiplyHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_multiplyhigh_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.MultiplyHigh");
    return dn2cpp_isa_vec<16>(_mm_mulhi_epu16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_multiplyhigh_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.MultiplyHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_multiplylow_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.MultiplyLow");
    return dn2cpp_isa_vec<16>(_mm_mullo_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_multiplylow_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_multiplylow_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.MultiplyLow");
    return dn2cpp_isa_vec<16>(_mm_mullo_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_multiplylow_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_multiplyscalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.MultiplyScalar");
    return dn2cpp_isa_vec<16>(_mm_mul_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_multiplyscalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.MultiplyScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_or_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Or");
    return dn2cpp_isa_vec<16>(_mm_or_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_or_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_or_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Or");
    return dn2cpp_isa_vec<16>(_mm_or_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_or_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_or_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Or");
    return dn2cpp_isa_vec<16>(_mm_or_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_or_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_or_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Or");
    return dn2cpp_isa_vec<16>(_mm_or_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_or_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_or_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Or");
    return dn2cpp_isa_vec<16>(_mm_or_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_or_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_or_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Or");
    return dn2cpp_isa_vec<16>(_mm_or_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_or_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_or_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Or");
    return dn2cpp_isa_vec<16>(_mm_or_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_or_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_or_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Or");
    return dn2cpp_isa_vec<16>(_mm_or_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_or_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_or_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Or");
    return dn2cpp_isa_vec<16>(_mm_or_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_or_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_packsignedsaturate_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.PackSignedSaturate");
    return dn2cpp_isa_vec<16>(_mm_packs_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_packsignedsaturate_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.PackSignedSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_packsignedsaturate_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.PackSignedSaturate");
    return dn2cpp_isa_vec<16>(_mm_packs_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_packsignedsaturate_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.PackSignedSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_packunsignedsaturate_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.PackUnsignedSaturate");
    return dn2cpp_isa_vec<16>(_mm_packus_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_packunsignedsaturate_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.PackUnsignedSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical128bitlane_v128i16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_slli_si128(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical128bitlane_v128i16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical128bitlane_v128i32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_slli_si128(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical128bitlane_v128i32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical128bitlane_v128i64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_slli_si128(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical128bitlane_v128i64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical128bitlane_v128i8_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_slli_si128(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical128bitlane_v128i8_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical128bitlane_v128u16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_slli_si128(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical128bitlane_v128u16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical128bitlane_v128u32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_slli_si128(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical128bitlane_v128u32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical128bitlane_v128u64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_slli_si128(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical128bitlane_v128u64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical128bitlane_v128u8_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_slli_si128(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical128bitlane_v128u8_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128i16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_slli_epi16(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128i16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
    return dn2cpp_isa_vec<16>(_mm_sll_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128i32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_slli_epi32(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128i32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
    return dn2cpp_isa_vec<16>(_mm_sll_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128i64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_slli_epi64(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128i64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
    return dn2cpp_isa_vec<16>(_mm_sll_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128u16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_slli_epi16(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128u16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
    return dn2cpp_isa_vec<16>(_mm_sll_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128u32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_slli_epi32(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128u32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
    return dn2cpp_isa_vec<16>(_mm_sll_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128u64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_slli_epi64(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128u64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
    return dn2cpp_isa_vec<16>(_mm_sll_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftleftlogical_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightarithmetic_v128i16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightArithmetic");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_srai_epi16(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightarithmetic_v128i16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightarithmetic_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightArithmetic");
    return dn2cpp_isa_vec<16>(_mm_sra_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightarithmetic_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightarithmetic_v128i32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightArithmetic");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_srai_epi32(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightarithmetic_v128i32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightarithmetic_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightArithmetic");
    return dn2cpp_isa_vec<16>(_mm_sra_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightarithmetic_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical128bitlane_v128i16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_srli_si128(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical128bitlane_v128i16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical128bitlane_v128i32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_srli_si128(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical128bitlane_v128i32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical128bitlane_v128i64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_srli_si128(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical128bitlane_v128i64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical128bitlane_v128i8_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_srli_si128(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical128bitlane_v128i8_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical128bitlane_v128u16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_srli_si128(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical128bitlane_v128u16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical128bitlane_v128u32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_srli_si128(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical128bitlane_v128u32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical128bitlane_v128u64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_srli_si128(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical128bitlane_v128u64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical128bitlane_v128u8_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_srli_si128(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical128bitlane_v128u8_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128i16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_srli_epi16(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128i16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
    return dn2cpp_isa_vec<16>(_mm_srl_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128i32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_srli_epi32(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128i32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
    return dn2cpp_isa_vec<16>(_mm_srl_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128i64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_srli_epi64(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128i64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
    return dn2cpp_isa_vec<16>(_mm_srl_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128u16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_srli_epi16(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128u16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
    return dn2cpp_isa_vec<16>(_mm_srl_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128u32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_srli_epi32(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128u32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
    return dn2cpp_isa_vec<16>(_mm_srl_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128u64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_srli_epi64(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128u64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
    return dn2cpp_isa_vec<16>(_mm_srl_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shiftrightlogical_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shuffle_v128f64_v128f64_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Shuffle");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_shuffle_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1), DN2CPP_IMM & 3)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shuffle_v128f64_v128f64_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Shuffle");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shuffle_v128i32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Shuffle");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_shuffle_epi32(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shuffle_v128i32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Shuffle");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shuffle_v128u32_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Shuffle");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_shuffle_epi32(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shuffle_v128u32_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Shuffle");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shufflehigh_v128i16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShuffleHigh");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_shufflehi_epi16(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shufflehigh_v128i16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShuffleHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shufflehigh_v128u16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShuffleHigh");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_shufflehi_epi16(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shufflehigh_v128u16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShuffleHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shufflelow_v128i16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShuffleLow");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_shufflelo_epi16(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shufflelow_v128i16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShuffleLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shufflelow_v128u16_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.ShuffleLow");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm_shufflelo_epi16(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_shufflelow_v128u16_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.ShuffleLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_sqrt_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Sqrt");
    return dn2cpp_isa_vec<16>(_mm_sqrt_pd(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_sqrt_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Sqrt");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_sqrtscalar_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.SqrtScalar");
    return dn2cpp_isa_vec<16>(_mm_sqrt_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_sqrtscalar_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.SqrtScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_sqrtscalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.SqrtScalar");
    return dn2cpp_isa_vec<16>(_mm_sqrt_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_sqrtscalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.SqrtScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_store_pf64_v128f64(double* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Store");
    _mm_storeu_pd(a0, dn2cpp_isa_bits<__m128d>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_store_pf64_v128f64(double*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_store_pi16_v128i16(int16_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Store");
    _mm_storeu_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_store_pi16_v128i16(int16_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_store_pi32_v128i32(int32_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Store");
    _mm_storeu_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_store_pi32_v128i32(int32_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_store_pi64_v128i64(int64_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Store");
    _mm_storeu_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_store_pi64_v128i64(int64_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_store_pi8_v128i8(int8_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Store");
    _mm_storeu_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_store_pi8_v128i8(int8_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_store_pu16_v128u16(uint16_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Store");
    _mm_storeu_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_store_pu16_v128u16(uint16_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_store_pu32_v128u32(uint32_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Store");
    _mm_storeu_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_store_pu32_v128u32(uint32_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_store_pu64_v128u64(uint64_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Store");
    _mm_storeu_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_store_pu64_v128u64(uint64_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_store_pu8_v128u8(uint8_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Store");
    _mm_storeu_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_store_pu8_v128u8(uint8_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealigned_pf64_v128f64(double* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreAligned");
    _mm_store_pd(a0, dn2cpp_isa_bits<__m128d>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealigned_pf64_v128f64(double*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealigned_pi16_v128i16(int16_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreAligned");
    _mm_store_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealigned_pi16_v128i16(int16_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealigned_pi32_v128i32(int32_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreAligned");
    _mm_store_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealigned_pi32_v128i32(int32_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealigned_pi64_v128i64(int64_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreAligned");
    _mm_store_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealigned_pi64_v128i64(int64_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealigned_pi8_v128i8(int8_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreAligned");
    _mm_store_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealigned_pi8_v128i8(int8_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealigned_pu16_v128u16(uint16_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreAligned");
    _mm_store_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealigned_pu16_v128u16(uint16_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealigned_pu32_v128u32(uint32_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreAligned");
    _mm_store_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealigned_pu32_v128u32(uint32_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealigned_pu64_v128u64(uint64_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreAligned");
    _mm_store_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealigned_pu64_v128u64(uint64_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealigned_pu8_v128u8(uint8_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreAligned");
    _mm_store_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealigned_pu8_v128u8(uint8_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealignednontemporal_pf64_v128f64(double* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreAlignedNonTemporal");
    _mm_stream_pd(a0, dn2cpp_isa_bits<__m128d>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealignednontemporal_pf64_v128f64(double*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealignednontemporal_pi16_v128i16(int16_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreAlignedNonTemporal");
    _mm_stream_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealignednontemporal_pi16_v128i16(int16_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealignednontemporal_pi32_v128i32(int32_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreAlignedNonTemporal");
    _mm_stream_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealignednontemporal_pi32_v128i32(int32_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealignednontemporal_pi64_v128i64(int64_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreAlignedNonTemporal");
    _mm_stream_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealignednontemporal_pi64_v128i64(int64_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealignednontemporal_pi8_v128i8(int8_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreAlignedNonTemporal");
    _mm_stream_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealignednontemporal_pi8_v128i8(int8_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealignednontemporal_pu16_v128u16(uint16_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreAlignedNonTemporal");
    _mm_stream_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealignednontemporal_pu16_v128u16(uint16_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealignednontemporal_pu32_v128u32(uint32_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreAlignedNonTemporal");
    _mm_stream_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealignednontemporal_pu32_v128u32(uint32_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealignednontemporal_pu64_v128u64(uint64_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreAlignedNonTemporal");
    _mm_stream_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealignednontemporal_pu64_v128u64(uint64_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealignednontemporal_pu8_v128u8(uint8_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreAlignedNonTemporal");
    _mm_stream_si128((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storealignednontemporal_pu8_v128u8(uint8_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storehigh_pf64_v128f64(double* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreHigh");
    _mm_storeh_pd(a0, dn2cpp_isa_bits<__m128d>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storehigh_pf64_v128f64(double*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storelow_pf64_v128f64(double* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreLow");
    _mm_storel_pd(a0, dn2cpp_isa_bits<__m128d>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storelow_pf64_v128f64(double*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storenontemporal_pi32_i32(int32_t* a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreNonTemporal");
    _mm_stream_si32((int*)a0, (int)a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storenontemporal_pi32_i32(int32_t*, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storenontemporal_pu32_u32(uint32_t* a0, uint32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreNonTemporal");
    _mm_stream_si32((int*)a0, (int)a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storenontemporal_pu32_u32(uint32_t*, uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storescalar_pf64_v128f64(double* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreScalar");
    _mm_store_sd(a0, dn2cpp_isa_bits<__m128d>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storescalar_pf64_v128f64(double*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storescalar_pi32_v128i32(int32_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreScalar");
    _mm_storeu_si32(a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storescalar_pi32_v128i32(int32_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storescalar_pi64_v128i64(int64_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreScalar");
    _mm_storel_epi64((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storescalar_pi64_v128i64(int64_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storescalar_pu32_v128u32(uint32_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreScalar");
    _mm_storeu_si32(a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storescalar_pu32_v128u32(uint32_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storescalar_pu64_v128u64(uint64_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.StoreScalar");
    _mm_storel_epi64((__m128i*)a0, dn2cpp_isa_bits<__m128i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_storescalar_pu64_v128u64(uint64_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.StoreScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtract_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Subtract");
    return dn2cpp_isa_vec<16>(_mm_sub_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtract_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtract_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Subtract");
    return dn2cpp_isa_vec<16>(_mm_sub_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtract_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtract_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Subtract");
    return dn2cpp_isa_vec<16>(_mm_sub_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtract_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtract_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Subtract");
    return dn2cpp_isa_vec<16>(_mm_sub_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtract_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtract_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Subtract");
    return dn2cpp_isa_vec<16>(_mm_sub_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtract_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtract_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Subtract");
    return dn2cpp_isa_vec<16>(_mm_sub_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtract_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtract_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Subtract");
    return dn2cpp_isa_vec<16>(_mm_sub_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtract_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtract_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Subtract");
    return dn2cpp_isa_vec<16>(_mm_sub_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtract_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtract_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Subtract");
    return dn2cpp_isa_vec<16>(_mm_sub_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtract_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtractsaturate_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.SubtractSaturate");
    return dn2cpp_isa_vec<16>(_mm_subs_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtractsaturate_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtractsaturate_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.SubtractSaturate");
    return dn2cpp_isa_vec<16>(_mm_subs_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtractsaturate_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtractsaturate_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.SubtractSaturate");
    return dn2cpp_isa_vec<16>(_mm_subs_epu16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtractsaturate_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtractsaturate_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.SubtractSaturate");
    return dn2cpp_isa_vec<16>(_mm_subs_epu8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtractsaturate_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtractscalar_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.SubtractScalar");
    return dn2cpp_isa_vec<16>(_mm_sub_sd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_subtractscalar_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.SubtractScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_sumabsolutedifferences_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.SumAbsoluteDifferences");
    return dn2cpp_isa_vec<16>(_mm_sad_epu8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_sumabsolutedifferences_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.SumAbsoluteDifferences");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpackhigh_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.UnpackHigh");
    return dn2cpp_isa_vec<16>(_mm_unpackhi_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpackhigh_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpackhigh_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.UnpackHigh");
    return dn2cpp_isa_vec<16>(_mm_unpackhi_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpackhigh_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpackhigh_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.UnpackHigh");
    return dn2cpp_isa_vec<16>(_mm_unpackhi_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpackhigh_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpackhigh_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.UnpackHigh");
    return dn2cpp_isa_vec<16>(_mm_unpackhi_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpackhigh_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpackhigh_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.UnpackHigh");
    return dn2cpp_isa_vec<16>(_mm_unpackhi_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpackhigh_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpackhigh_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.UnpackHigh");
    return dn2cpp_isa_vec<16>(_mm_unpackhi_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpackhigh_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpackhigh_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.UnpackHigh");
    return dn2cpp_isa_vec<16>(_mm_unpackhi_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpackhigh_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpackhigh_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.UnpackHigh");
    return dn2cpp_isa_vec<16>(_mm_unpackhi_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpackhigh_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpackhigh_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.UnpackHigh");
    return dn2cpp_isa_vec<16>(_mm_unpackhi_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpackhigh_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpacklow_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.UnpackLow");
    return dn2cpp_isa_vec<16>(_mm_unpacklo_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpacklow_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpacklow_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.UnpackLow");
    return dn2cpp_isa_vec<16>(_mm_unpacklo_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpacklow_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpacklow_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.UnpackLow");
    return dn2cpp_isa_vec<16>(_mm_unpacklo_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpacklow_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpacklow_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.UnpackLow");
    return dn2cpp_isa_vec<16>(_mm_unpacklo_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpacklow_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpacklow_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.UnpackLow");
    return dn2cpp_isa_vec<16>(_mm_unpacklo_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpacklow_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpacklow_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.UnpackLow");
    return dn2cpp_isa_vec<16>(_mm_unpacklo_epi16(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpacklow_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpacklow_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.UnpackLow");
    return dn2cpp_isa_vec<16>(_mm_unpacklo_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpacklow_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpacklow_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.UnpackLow");
    return dn2cpp_isa_vec<16>(_mm_unpacklo_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpacklow_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpacklow_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.UnpackLow");
    return dn2cpp_isa_vec<16>(_mm_unpacklo_epi8(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_unpacklow_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_xor_v128f64_v128f64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Xor");
    return dn2cpp_isa_vec<16>(_mm_xor_pd(dn2cpp_isa_bits<__m128d>(a0), dn2cpp_isa_bits<__m128d>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_xor_v128f64_v128f64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_xor_v128i16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Xor");
    return dn2cpp_isa_vec<16>(_mm_xor_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_xor_v128i16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_xor_v128i32_v128i32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Xor");
    return dn2cpp_isa_vec<16>(_mm_xor_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_xor_v128i32_v128i32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_xor_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Xor");
    return dn2cpp_isa_vec<16>(_mm_xor_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_xor_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_xor_v128i8_v128i8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Xor");
    return dn2cpp_isa_vec<16>(_mm_xor_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_xor_v128i8_v128i8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_xor_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Xor");
    return dn2cpp_isa_vec<16>(_mm_xor_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_xor_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_xor_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Xor");
    return dn2cpp_isa_vec<16>(_mm_xor_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_xor_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_xor_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Xor");
    return dn2cpp_isa_vec<16>(_mm_xor_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_xor_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_xor_v128u8_v128u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2, "System.Runtime.Intrinsics.X86.Sse2.Xor");
    return dn2cpp_isa_vec<16>(_mm_xor_si128(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_xor_v128u8_v128u8(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2.Xor");
}
#endif
