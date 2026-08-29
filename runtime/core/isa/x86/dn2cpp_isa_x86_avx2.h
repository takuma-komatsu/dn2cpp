#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx2: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_abs_v256i16(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Abs");
    return dn2cpp_isa_vec<32>(_mm256_abs_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_abs_v256i16(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Abs");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_abs_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Abs");
    return dn2cpp_isa_vec<32>(_mm256_abs_epi32(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_abs_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Abs");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_abs_v256i8(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Abs");
    return dn2cpp_isa_vec<32>(_mm256_abs_epi8(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_abs_v256i8(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Abs");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_add_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Add");
    return dn2cpp_isa_vec<32>(_mm256_add_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_add_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_add_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Add");
    return dn2cpp_isa_vec<32>(_mm256_add_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_add_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_add_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Add");
    return dn2cpp_isa_vec<32>(_mm256_add_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_add_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_add_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Add");
    return dn2cpp_isa_vec<32>(_mm256_add_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_add_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_add_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Add");
    return dn2cpp_isa_vec<32>(_mm256_add_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_add_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_add_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Add");
    return dn2cpp_isa_vec<32>(_mm256_add_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_add_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_add_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Add");
    return dn2cpp_isa_vec<32>(_mm256_add_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_add_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_add_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Add");
    return dn2cpp_isa_vec<32>(_mm256_add_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_add_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_addsaturate_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AddSaturate");
    return dn2cpp_isa_vec<32>(_mm256_adds_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_addsaturate_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_addsaturate_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AddSaturate");
    return dn2cpp_isa_vec<32>(_mm256_adds_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_addsaturate_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_addsaturate_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AddSaturate");
    return dn2cpp_isa_vec<32>(_mm256_adds_epu16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_addsaturate_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_addsaturate_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AddSaturate");
    return dn2cpp_isa_vec<32>(_mm256_adds_epu8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_addsaturate_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_alignright_v256i16_v256i16_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AlignRight");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_alignr_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_alignright_v256i16_v256i16_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AlignRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_alignright_v256i32_v256i32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AlignRight");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_alignr_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_alignright_v256i32_v256i32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AlignRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_alignright_v256i64_v256i64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AlignRight");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_alignr_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_alignright_v256i64_v256i64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AlignRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_alignright_v256i8_v256i8_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AlignRight");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_alignr_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_alignright_v256i8_v256i8_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AlignRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_alignright_v256u16_v256u16_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AlignRight");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_alignr_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_alignright_v256u16_v256u16_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AlignRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_alignright_v256u32_v256u32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AlignRight");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_alignr_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_alignright_v256u32_v256u32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AlignRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_alignright_v256u64_v256u64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AlignRight");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_alignr_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_alignright_v256u64_v256u64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AlignRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_alignright_v256u8_v256u8_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AlignRight");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_alignr_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_alignright_v256u8_v256u8_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AlignRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_and_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.And");
    return dn2cpp_isa_vec<32>(_mm256_and_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_and_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_and_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.And");
    return dn2cpp_isa_vec<32>(_mm256_and_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_and_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_and_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.And");
    return dn2cpp_isa_vec<32>(_mm256_and_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_and_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_and_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.And");
    return dn2cpp_isa_vec<32>(_mm256_and_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_and_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_and_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.And");
    return dn2cpp_isa_vec<32>(_mm256_and_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_and_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_and_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.And");
    return dn2cpp_isa_vec<32>(_mm256_and_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_and_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_and_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.And");
    return dn2cpp_isa_vec<32>(_mm256_and_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_and_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_and_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.And");
    return dn2cpp_isa_vec<32>(_mm256_and_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_and_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_andnot_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AndNot");
    return dn2cpp_isa_vec<32>(_mm256_andnot_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_andnot_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_andnot_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AndNot");
    return dn2cpp_isa_vec<32>(_mm256_andnot_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_andnot_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_andnot_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AndNot");
    return dn2cpp_isa_vec<32>(_mm256_andnot_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_andnot_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_andnot_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AndNot");
    return dn2cpp_isa_vec<32>(_mm256_andnot_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_andnot_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_andnot_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AndNot");
    return dn2cpp_isa_vec<32>(_mm256_andnot_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_andnot_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_andnot_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AndNot");
    return dn2cpp_isa_vec<32>(_mm256_andnot_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_andnot_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_andnot_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AndNot");
    return dn2cpp_isa_vec<32>(_mm256_andnot_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_andnot_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_andnot_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.AndNot");
    return dn2cpp_isa_vec<32>(_mm256_andnot_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_andnot_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_average_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Average");
    return dn2cpp_isa_vec<32>(_mm256_avg_epu16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_average_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Average");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_average_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Average");
    return dn2cpp_isa_vec<32>(_mm256_avg_epu8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_average_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Average");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_blend_v128i32_v128i32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Blend");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_blend_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 15)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_blend_v128i32_v128i32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Blend");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_blend_v128u32_v128u32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Blend");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_blend_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 15)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_blend_v128u32_v128u32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Blend");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blend_v256i16_v256i16_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Blend");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_blend_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blend_v256i16_v256i16_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Blend");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blend_v256i32_v256i32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Blend");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_blend_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blend_v256i32_v256i32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Blend");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blend_v256u16_v256u16_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Blend");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_blend_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blend_v256u16_v256u16_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Blend");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blend_v256u32_v256u32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Blend");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_blend_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blend_v256u32_v256u32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Blend");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blendvariable_v256i16_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_blendv_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blendvariable_v256i16_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blendvariable_v256i32_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_blendv_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blendvariable_v256i32_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blendvariable_v256i64_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_blendv_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blendvariable_v256i64_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blendvariable_v256i8_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_blendv_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blendvariable_v256i8_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blendvariable_v256u16_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_blendv_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blendvariable_v256u16_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blendvariable_v256u32_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_blendv_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blendvariable_v256u32_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blendvariable_v256u64_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_blendv_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blendvariable_v256u64_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blendvariable_v256u8_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BlendVariable");
    return dn2cpp_isa_vec<32>(_mm256_blendv_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_blendvariable_v256u8_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_set1_epi16((short)*a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_set1_epi32((int)*a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_set1_epi64x((long long)*a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_set1_epi8((char)*a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_set1_epi16((short)*a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_set1_epi32((int)*a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_set1_epi64x((long long)*a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_set1_epi8((char)*a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_broadcastss_ps(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_broadcastsd_pd(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_broadcastw_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_broadcastd_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_broadcastq_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_broadcastb_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_broadcastw_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_broadcastd_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_broadcastq_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
    return dn2cpp_isa_vec<16>(_mm_broadcastb_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_broadcastscalartovector128_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_set1_epi16((short)*a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_set1_epi32((int)*a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_set1_epi64x((long long)*a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_set1_epi8((char)*a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_set1_epi16((short)*a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_set1_epi32((int)*a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_set1_epi64x((long long)*a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_set1_epi8((char)*a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcastss_ps(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcastsd_pd(dn2cpp_isa_bits<__m128d>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcastw_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcastd_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcastq_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcastb_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcastw_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcastd_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcastq_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcastb_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastscalartovector256_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastScalarToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastvector128tovector256_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastVector128ToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcastsi128_si256(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastvector128tovector256_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastVector128ToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastvector128tovector256_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastVector128ToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcastsi128_si256(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastvector128tovector256_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastVector128ToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastvector128tovector256_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastVector128ToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcastsi128_si256(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastvector128tovector256_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastVector128ToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastvector128tovector256_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastVector128ToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcastsi128_si256(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastvector128tovector256_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastVector128ToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastvector128tovector256_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastVector128ToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcastsi128_si256(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastvector128tovector256_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastVector128ToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastvector128tovector256_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastVector128ToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcastsi128_si256(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastvector128tovector256_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastVector128ToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastvector128tovector256_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastVector128ToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcastsi128_si256(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastvector128tovector256_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastVector128ToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastvector128tovector256_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.BroadcastVector128ToVector256");
    return dn2cpp_isa_vec<32>(_mm256_broadcastsi128_si256(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_broadcastvector128tovector256_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.BroadcastVector128ToVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_compareequal_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmpeq_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_compareequal_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_compareequal_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmpeq_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_compareequal_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_compareequal_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmpeq_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_compareequal_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_compareequal_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmpeq_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_compareequal_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_compareequal_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmpeq_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_compareequal_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_compareequal_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmpeq_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_compareequal_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_compareequal_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmpeq_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_compareequal_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_compareequal_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.CompareEqual");
    return dn2cpp_isa_vec<32>(_mm256_cmpeq_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_compareequal_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_comparegreaterthan_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_cmpgt_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_comparegreaterthan_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_comparegreaterthan_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_cmpgt_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_comparegreaterthan_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_comparegreaterthan_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_cmpgt_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_comparegreaterthan_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_comparegreaterthan_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.CompareGreaterThan");
    return dn2cpp_isa_vec<32>(_mm256_cmpgt_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_comparegreaterthan_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx2_converttoint32_v256i32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToInt32");
    return _mm256_cvtsi256_si32(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx2_converttoint32_v256i32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx2_converttouint32_v256u32(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToUInt32");
    return (uint32_t)_mm256_cvtsi256_si32(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_avx2_converttouint32_v256u32(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToUInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int16_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int16");
    return dn2cpp_isa_vec<32>(_mm256_cvtepi8_epi16(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int16_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int16_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int16");
    return dn2cpp_isa_vec<32>(_mm256_cvtepu8_epi16(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int16_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int16_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int16");
    return dn2cpp_isa_vec<32>(_mm256_cvtepi8_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int16_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int16_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int16");
    return dn2cpp_isa_vec<32>(_mm256_cvtepu8_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int16_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int32_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int32");
    return dn2cpp_isa_vec<32>(_mm256_cvtepi16_epi32(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int32_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int32_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int32");
    return dn2cpp_isa_vec<32>(_mm256_cvtepi8_epi32(_mm_loadl_epi64((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int32_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int32_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int32");
    return dn2cpp_isa_vec<32>(_mm256_cvtepu16_epi32(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int32_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int32_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int32");
    return dn2cpp_isa_vec<32>(_mm256_cvtepu8_epi32(_mm_loadl_epi64((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int32_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int32_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int32");
    return dn2cpp_isa_vec<32>(_mm256_cvtepi16_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int32_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int32_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int32");
    return dn2cpp_isa_vec<32>(_mm256_cvtepi8_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int32_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int32_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int32");
    return dn2cpp_isa_vec<32>(_mm256_cvtepu16_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int32_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int32_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int32");
    return dn2cpp_isa_vec<32>(_mm256_cvtepu8_epi32(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int32_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
    return dn2cpp_isa_vec<32>(_mm256_cvtepi16_epi64(_mm_loadl_epi64((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
    return dn2cpp_isa_vec<32>(_mm256_cvtepi32_epi64(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
    return dn2cpp_isa_vec<32>(_mm256_cvtepi8_epi64(_mm_loadu_si32(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
    return dn2cpp_isa_vec<32>(_mm256_cvtepu16_epi64(_mm_loadl_epi64((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
    return dn2cpp_isa_vec<32>(_mm256_cvtepu32_epi64(_mm_loadu_si128((const __m128i*)a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
    return dn2cpp_isa_vec<32>(_mm256_cvtepu8_epi64(_mm_loadu_si32(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
    return dn2cpp_isa_vec<32>(_mm256_cvtepi16_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_v128i32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
    return dn2cpp_isa_vec<32>(_mm256_cvtepi32_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_v128i32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
    return dn2cpp_isa_vec<32>(_mm256_cvtepi8_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
    return dn2cpp_isa_vec<32>(_mm256_cvtepu16_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_v128u32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
    return dn2cpp_isa_vec<32>(_mm256_cvtepu32_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_v128u32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
    return dn2cpp_isa_vec<32>(_mm256_cvtepu8_epi64(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_converttovector256int64_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ConvertToVector256Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_extractvector128_v256i16_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm256_extracti128_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_extractvector128_v256i16_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_extractvector128_v256i32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm256_extracti128_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_extractvector128_v256i32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_extractvector128_v256i64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm256_extracti128_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_extractvector128_v256i64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_extractvector128_v256i8_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm256_extracti128_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_extractvector128_v256i8_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_extractvector128_v256u16_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm256_extracti128_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_extractvector128_v256u16_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_extractvector128_v256u32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm256_extracti128_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_extractvector128_v256u32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_extractvector128_v256u64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm256_extracti128_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_extractvector128_v256u64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_extractvector128_v256u8_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ExtractVector128");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<16>(_mm256_extracti128_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_extractvector128_v256u8_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ExtractVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128f32_pf32_v128i32_v128f32_u8(const Dn2CppVector128& a0, float* a1, const Dn2CppVector128& a2, const Dn2CppVector128& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_mask_i32gather_ps(dn2cpp_isa_bits<__m128>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_mask_i32gather_ps(dn2cpp_isa_bits<__m128>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_mask_i32gather_ps(dn2cpp_isa_bits<__m128>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_mask_i32gather_ps(dn2cpp_isa_bits<__m128>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128f32_pf32_v128i32_v128f32_u8(const Dn2CppVector128&, float*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128f32_pf32_v128i64_v128f32_u8(const Dn2CppVector128& a0, float* a1, const Dn2CppVector128& a2, const Dn2CppVector128& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_mask_i64gather_ps(dn2cpp_isa_bits<__m128>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_mask_i64gather_ps(dn2cpp_isa_bits<__m128>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_mask_i64gather_ps(dn2cpp_isa_bits<__m128>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_mask_i64gather_ps(dn2cpp_isa_bits<__m128>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128f32_pf32_v128i64_v128f32_u8(const Dn2CppVector128&, float*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128f32_pf32_v256i64_v128f32_u8(const Dn2CppVector128& a0, float* a1, const Dn2CppVector256& a2, const Dn2CppVector128& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm256_mask_i64gather_ps(dn2cpp_isa_bits<__m128>(a0), a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m128>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm256_mask_i64gather_ps(dn2cpp_isa_bits<__m128>(a0), a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m128>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm256_mask_i64gather_ps(dn2cpp_isa_bits<__m128>(a0), a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m128>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm256_mask_i64gather_ps(dn2cpp_isa_bits<__m128>(a0), a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m128>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128f32_pf32_v256i64_v128f32_u8(const Dn2CppVector128&, float*, const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128f64_pf64_v128i32_v128f64_u8(const Dn2CppVector128& a0, double* a1, const Dn2CppVector128& a2, const Dn2CppVector128& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_mask_i32gather_pd(dn2cpp_isa_bits<__m128d>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128d>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_mask_i32gather_pd(dn2cpp_isa_bits<__m128d>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128d>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_mask_i32gather_pd(dn2cpp_isa_bits<__m128d>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128d>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_mask_i32gather_pd(dn2cpp_isa_bits<__m128d>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128d>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128f64_pf64_v128i32_v128f64_u8(const Dn2CppVector128&, double*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128f64_pf64_v128i64_v128f64_u8(const Dn2CppVector128& a0, double* a1, const Dn2CppVector128& a2, const Dn2CppVector128& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_mask_i64gather_pd(dn2cpp_isa_bits<__m128d>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128d>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_mask_i64gather_pd(dn2cpp_isa_bits<__m128d>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128d>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_mask_i64gather_pd(dn2cpp_isa_bits<__m128d>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128d>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_mask_i64gather_pd(dn2cpp_isa_bits<__m128d>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128d>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128f64_pf64_v128i64_v128f64_u8(const Dn2CppVector128&, double*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128i32_pi32_v128i32_v128i32_u8(const Dn2CppVector128& a0, int32_t* a1, const Dn2CppVector128& a2, const Dn2CppVector128& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_mask_i32gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_mask_i32gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_mask_i32gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_mask_i32gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128i32_pi32_v128i32_v128i32_u8(const Dn2CppVector128&, int32_t*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128i32_pi32_v128i64_v128i32_u8(const Dn2CppVector128& a0, int32_t* a1, const Dn2CppVector128& a2, const Dn2CppVector128& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_mask_i64gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_mask_i64gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_mask_i64gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_mask_i64gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128i32_pi32_v128i64_v128i32_u8(const Dn2CppVector128&, int32_t*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128i32_pi32_v256i64_v128i32_u8(const Dn2CppVector128& a0, int32_t* a1, const Dn2CppVector256& a2, const Dn2CppVector128& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm256_mask_i64gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm256_mask_i64gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm256_mask_i64gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm256_mask_i64gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128i32_pi32_v256i64_v128i32_u8(const Dn2CppVector128&, int32_t*, const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128i64_pi64_v128i32_v128i64_u8(const Dn2CppVector128& a0, int64_t* a1, const Dn2CppVector128& a2, const Dn2CppVector128& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_mask_i32gather_epi64(dn2cpp_isa_bits<__m128i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_mask_i32gather_epi64(dn2cpp_isa_bits<__m128i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_mask_i32gather_epi64(dn2cpp_isa_bits<__m128i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_mask_i32gather_epi64(dn2cpp_isa_bits<__m128i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128i64_pi64_v128i32_v128i64_u8(const Dn2CppVector128&, int64_t*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128i64_pi64_v128i64_v128i64_u8(const Dn2CppVector128& a0, int64_t* a1, const Dn2CppVector128& a2, const Dn2CppVector128& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_mask_i64gather_epi64(dn2cpp_isa_bits<__m128i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_mask_i64gather_epi64(dn2cpp_isa_bits<__m128i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_mask_i64gather_epi64(dn2cpp_isa_bits<__m128i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_mask_i64gather_epi64(dn2cpp_isa_bits<__m128i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128i64_pi64_v128i64_v128i64_u8(const Dn2CppVector128&, int64_t*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128u32_pu32_v128i32_v128u32_u8(const Dn2CppVector128& a0, uint32_t* a1, const Dn2CppVector128& a2, const Dn2CppVector128& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_mask_i32gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_mask_i32gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_mask_i32gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_mask_i32gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128u32_pu32_v128i32_v128u32_u8(const Dn2CppVector128&, uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128u32_pu32_v128i64_v128u32_u8(const Dn2CppVector128& a0, uint32_t* a1, const Dn2CppVector128& a2, const Dn2CppVector128& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_mask_i64gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_mask_i64gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_mask_i64gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_mask_i64gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128u32_pu32_v128i64_v128u32_u8(const Dn2CppVector128&, uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128u32_pu32_v256i64_v128u32_u8(const Dn2CppVector128& a0, uint32_t* a1, const Dn2CppVector256& a2, const Dn2CppVector128& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm256_mask_i64gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm256_mask_i64gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm256_mask_i64gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm256_mask_i64gather_epi32(dn2cpp_isa_bits<__m128i>(a0), (const int*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128u32_pu32_v256i64_v128u32_u8(const Dn2CppVector128&, uint32_t*, const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128u64_pu64_v128i32_v128u64_u8(const Dn2CppVector128& a0, uint64_t* a1, const Dn2CppVector128& a2, const Dn2CppVector128& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_mask_i32gather_epi64(dn2cpp_isa_bits<__m128i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_mask_i32gather_epi64(dn2cpp_isa_bits<__m128i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_mask_i32gather_epi64(dn2cpp_isa_bits<__m128i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_mask_i32gather_epi64(dn2cpp_isa_bits<__m128i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128u64_pu64_v128i32_v128u64_u8(const Dn2CppVector128&, uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128u64_pu64_v128i64_v128u64_u8(const Dn2CppVector128& a0, uint64_t* a1, const Dn2CppVector128& a2, const Dn2CppVector128& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_mask_i64gather_epi64(dn2cpp_isa_bits<__m128i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_mask_i64gather_epi64(dn2cpp_isa_bits<__m128i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_mask_i64gather_epi64(dn2cpp_isa_bits<__m128i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_mask_i64gather_epi64(dn2cpp_isa_bits<__m128i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m128i>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathermaskvector128_v128u64_pu64_v128i64_v128u64_u8(const Dn2CppVector128&, uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathermaskvector256_v256f32_pf32_v256i32_v256f32_u8(const Dn2CppVector256& a0, float* a1, const Dn2CppVector256& a2, const Dn2CppVector256& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector256");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_ps(dn2cpp_isa_bits<__m256>(a0), a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_ps(dn2cpp_isa_bits<__m256>(a0), a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_ps(dn2cpp_isa_bits<__m256>(a0), a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_ps(dn2cpp_isa_bits<__m256>(a0), a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathermaskvector256_v256f32_pf32_v256i32_v256f32_u8(const Dn2CppVector256&, float*, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathermaskvector256_v256f64_pf64_v128i32_v256f64_u8(const Dn2CppVector256& a0, double* a1, const Dn2CppVector128& a2, const Dn2CppVector256& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector256");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_pd(dn2cpp_isa_bits<__m256d>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m256d>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_pd(dn2cpp_isa_bits<__m256d>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m256d>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_pd(dn2cpp_isa_bits<__m256d>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m256d>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_pd(dn2cpp_isa_bits<__m256d>(a0), a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m256d>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathermaskvector256_v256f64_pf64_v128i32_v256f64_u8(const Dn2CppVector256&, double*, const Dn2CppVector128&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathermaskvector256_v256f64_pf64_v256i64_v256f64_u8(const Dn2CppVector256& a0, double* a1, const Dn2CppVector256& a2, const Dn2CppVector256& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector256");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm256_mask_i64gather_pd(dn2cpp_isa_bits<__m256d>(a0), a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256d>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm256_mask_i64gather_pd(dn2cpp_isa_bits<__m256d>(a0), a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256d>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm256_mask_i64gather_pd(dn2cpp_isa_bits<__m256d>(a0), a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256d>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm256_mask_i64gather_pd(dn2cpp_isa_bits<__m256d>(a0), a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256d>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathermaskvector256_v256f64_pf64_v256i64_v256f64_u8(const Dn2CppVector256&, double*, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathermaskvector256_v256i32_pi32_v256i32_v256i32_u8(const Dn2CppVector256& a0, int32_t* a1, const Dn2CppVector256& a2, const Dn2CppVector256& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector256");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_epi32(dn2cpp_isa_bits<__m256i>(a0), (const int*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_epi32(dn2cpp_isa_bits<__m256i>(a0), (const int*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_epi32(dn2cpp_isa_bits<__m256i>(a0), (const int*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_epi32(dn2cpp_isa_bits<__m256i>(a0), (const int*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathermaskvector256_v256i32_pi32_v256i32_v256i32_u8(const Dn2CppVector256&, int32_t*, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathermaskvector256_v256i64_pi64_v128i32_v256i64_u8(const Dn2CppVector256& a0, int64_t* a1, const Dn2CppVector128& a2, const Dn2CppVector256& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector256");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_epi64(dn2cpp_isa_bits<__m256i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_epi64(dn2cpp_isa_bits<__m256i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_epi64(dn2cpp_isa_bits<__m256i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_epi64(dn2cpp_isa_bits<__m256i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathermaskvector256_v256i64_pi64_v128i32_v256i64_u8(const Dn2CppVector256&, int64_t*, const Dn2CppVector128&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathermaskvector256_v256i64_pi64_v256i64_v256i64_u8(const Dn2CppVector256& a0, int64_t* a1, const Dn2CppVector256& a2, const Dn2CppVector256& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector256");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm256_mask_i64gather_epi64(dn2cpp_isa_bits<__m256i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm256_mask_i64gather_epi64(dn2cpp_isa_bits<__m256i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm256_mask_i64gather_epi64(dn2cpp_isa_bits<__m256i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm256_mask_i64gather_epi64(dn2cpp_isa_bits<__m256i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathermaskvector256_v256i64_pi64_v256i64_v256i64_u8(const Dn2CppVector256&, int64_t*, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathermaskvector256_v256u32_pu32_v256i32_v256u32_u8(const Dn2CppVector256& a0, uint32_t* a1, const Dn2CppVector256& a2, const Dn2CppVector256& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector256");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_epi32(dn2cpp_isa_bits<__m256i>(a0), (const int*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_epi32(dn2cpp_isa_bits<__m256i>(a0), (const int*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_epi32(dn2cpp_isa_bits<__m256i>(a0), (const int*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_epi32(dn2cpp_isa_bits<__m256i>(a0), (const int*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathermaskvector256_v256u32_pu32_v256i32_v256u32_u8(const Dn2CppVector256&, uint32_t*, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathermaskvector256_v256u64_pu64_v128i32_v256u64_u8(const Dn2CppVector256& a0, uint64_t* a1, const Dn2CppVector128& a2, const Dn2CppVector256& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector256");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_epi64(dn2cpp_isa_bits<__m256i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_epi64(dn2cpp_isa_bits<__m256i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_epi64(dn2cpp_isa_bits<__m256i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm256_mask_i32gather_epi64(dn2cpp_isa_bits<__m256i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m128i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathermaskvector256_v256u64_pu64_v128i32_v256u64_u8(const Dn2CppVector256&, uint64_t*, const Dn2CppVector128&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathermaskvector256_v256u64_pu64_v256i64_v256u64_u8(const Dn2CppVector256& a0, uint64_t* a1, const Dn2CppVector256& a2, const Dn2CppVector256& a3, uint8_t a4)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector256");
    switch ((int)a4) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm256_mask_i64gather_epi64(dn2cpp_isa_bits<__m256i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm256_mask_i64gather_epi64(dn2cpp_isa_bits<__m256i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm256_mask_i64gather_epi64(dn2cpp_isa_bits<__m256i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm256_mask_i64gather_epi64(dn2cpp_isa_bits<__m256i>(a0), (const long long*)a1, dn2cpp_isa_bits<__m256i>(a2), dn2cpp_isa_bits<__m256i>(a3), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathermaskvector256_v256u64_pu64_v256i64_v256u64_u8(const Dn2CppVector256&, uint64_t*, const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherMaskVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pf32_v128i32_u8(float* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_i32gather_ps(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_i32gather_ps(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_i32gather_ps(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_i32gather_ps(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pf32_v128i32_u8(float*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pf32_v128i64_u8(float* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_i64gather_ps(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_i64gather_ps(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_i64gather_ps(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_i64gather_ps(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pf32_v128i64_u8(float*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pf32_v256i64_u8(float* a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm256_i64gather_ps(a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm256_i64gather_ps(a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm256_i64gather_ps(a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm256_i64gather_ps(a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pf32_v256i64_u8(float*, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pf64_v128i32_u8(double* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_i32gather_pd(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_i32gather_pd(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_i32gather_pd(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_i32gather_pd(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pf64_v128i32_u8(double*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pf64_v128i64_u8(double* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_i64gather_pd(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_i64gather_pd(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_i64gather_pd(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_i64gather_pd(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pf64_v128i64_u8(double*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pi32_v128i32_u8(int32_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_i32gather_epi32((const int*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_i32gather_epi32((const int*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_i32gather_epi32((const int*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_i32gather_epi32((const int*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pi32_v128i32_u8(int32_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pi32_v128i64_u8(int32_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_i64gather_epi32((const int*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_i64gather_epi32((const int*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_i64gather_epi32((const int*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_i64gather_epi32((const int*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pi32_v128i64_u8(int32_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pi32_v256i64_u8(int32_t* a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm256_i64gather_epi32((const int*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm256_i64gather_epi32((const int*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm256_i64gather_epi32((const int*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm256_i64gather_epi32((const int*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pi32_v256i64_u8(int32_t*, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pi64_v128i32_u8(int64_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_i32gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_i32gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_i32gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_i32gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pi64_v128i32_u8(int64_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pi64_v128i64_u8(int64_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_i64gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_i64gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_i64gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_i64gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pi64_v128i64_u8(int64_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pu32_v128i32_u8(uint32_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_i32gather_epi32((const int*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_i32gather_epi32((const int*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_i32gather_epi32((const int*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_i32gather_epi32((const int*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pu32_v128i32_u8(uint32_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pu32_v128i64_u8(uint32_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_i64gather_epi32((const int*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_i64gather_epi32((const int*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_i64gather_epi32((const int*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_i64gather_epi32((const int*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pu32_v128i64_u8(uint32_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pu32_v256i64_u8(uint32_t* a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm256_i64gather_epi32((const int*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm256_i64gather_epi32((const int*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm256_i64gather_epi32((const int*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm256_i64gather_epi32((const int*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pu32_v256i64_u8(uint32_t*, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pu64_v128i32_u8(uint64_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_i32gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_i32gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_i32gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_i32gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pu64_v128i32_u8(uint64_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pu64_v128i64_u8(uint64_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<16>(_mm_i64gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<16>(_mm_i64gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<16>(_mm_i64gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<16>(_mm_i64gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_gathervector128_pu64_v128i64_u8(uint64_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathervector256_pf32_v256i32_u8(float* a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector256");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm256_i32gather_ps(a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm256_i32gather_ps(a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm256_i32gather_ps(a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm256_i32gather_ps(a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathervector256_pf32_v256i32_u8(float*, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathervector256_pf64_v128i32_u8(double* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector256");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm256_i32gather_pd(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm256_i32gather_pd(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm256_i32gather_pd(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm256_i32gather_pd(a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathervector256_pf64_v128i32_u8(double*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathervector256_pf64_v256i64_u8(double* a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector256");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm256_i64gather_pd(a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm256_i64gather_pd(a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm256_i64gather_pd(a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm256_i64gather_pd(a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathervector256_pf64_v256i64_u8(double*, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathervector256_pi32_v256i32_u8(int32_t* a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector256");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm256_i32gather_epi32((const int*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm256_i32gather_epi32((const int*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm256_i32gather_epi32((const int*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm256_i32gather_epi32((const int*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathervector256_pi32_v256i32_u8(int32_t*, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathervector256_pi64_v128i32_u8(int64_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector256");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm256_i32gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm256_i32gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm256_i32gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm256_i32gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathervector256_pi64_v128i32_u8(int64_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathervector256_pi64_v256i64_u8(int64_t* a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector256");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm256_i64gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm256_i64gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm256_i64gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm256_i64gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathervector256_pi64_v256i64_u8(int64_t*, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathervector256_pu32_v256i32_u8(uint32_t* a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector256");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm256_i32gather_epi32((const int*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm256_i32gather_epi32((const int*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm256_i32gather_epi32((const int*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm256_i32gather_epi32((const int*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathervector256_pu32_v256i32_u8(uint32_t*, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathervector256_pu64_v128i32_u8(uint64_t* a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector256");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm256_i32gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm256_i32gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm256_i32gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm256_i32gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathervector256_pu64_v128i32_u8(uint64_t*, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathervector256_pu64_v256i64_u8(uint64_t* a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.GatherVector256");
    switch ((int)a2) { DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<32>(_mm256_i64gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<32>(_mm256_i64gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<32>(_mm256_i64gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<32>(_mm256_i64gather_epi64((const long long*)a0, dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_gathervector256_pu64_v256i64_u8(uint64_t*, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.GatherVector256");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_horizontaladd_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.HorizontalAdd");
    return dn2cpp_isa_vec<32>(_mm256_hadd_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_horizontaladd_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.HorizontalAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_horizontaladd_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.HorizontalAdd");
    return dn2cpp_isa_vec<32>(_mm256_hadd_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_horizontaladd_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.HorizontalAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_horizontaladdsaturate_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.HorizontalAddSaturate");
    return dn2cpp_isa_vec<32>(_mm256_hadds_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_horizontaladdsaturate_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.HorizontalAddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_horizontalsubtract_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.HorizontalSubtract");
    return dn2cpp_isa_vec<32>(_mm256_hsub_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_horizontalsubtract_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.HorizontalSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_horizontalsubtract_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.HorizontalSubtract");
    return dn2cpp_isa_vec<32>(_mm256_hsub_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_horizontalsubtract_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.HorizontalSubtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_horizontalsubtractsaturate_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.HorizontalSubtractSaturate");
    return dn2cpp_isa_vec<32>(_mm256_hsubs_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_horizontalsubtractsaturate_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.HorizontalSubtractSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_insertvector128_v256i16_v128i16_u8(const Dn2CppVector256& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_inserti128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_insertvector128_v256i16_v128i16_u8(const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_insertvector128_v256i32_v128i32_u8(const Dn2CppVector256& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_inserti128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_insertvector128_v256i32_v128i32_u8(const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_insertvector128_v256i64_v128i64_u8(const Dn2CppVector256& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_inserti128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_insertvector128_v256i64_v128i64_u8(const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_insertvector128_v256i8_v128i8_u8(const Dn2CppVector256& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_inserti128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_insertvector128_v256i8_v128i8_u8(const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_insertvector128_v256u16_v128u16_u8(const Dn2CppVector256& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_inserti128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_insertvector128_v256u16_v128u16_u8(const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_insertvector128_v256u32_v128u32_u8(const Dn2CppVector256& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_inserti128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_insertvector128_v256u32_v128u32_u8(const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_insertvector128_v256u64_v128u64_u8(const Dn2CppVector256& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_inserti128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_insertvector128_v256u64_v128u64_u8(const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_insertvector128_v256u8_v128u8_u8(const Dn2CppVector256& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.InsertVector128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_inserti128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1), DN2CPP_IMM & 1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_insertvector128_v256u8_v128u8_u8(const Dn2CppVector256&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.InsertVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_loadalignedvector256nontemporal_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.LoadAlignedVector256NonTemporal");
    return dn2cpp_isa_vec<32>(_mm256_stream_load_si256((__m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_loadalignedvector256nontemporal_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.LoadAlignedVector256NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_loadalignedvector256nontemporal_pi32(int32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.LoadAlignedVector256NonTemporal");
    return dn2cpp_isa_vec<32>(_mm256_stream_load_si256((__m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_loadalignedvector256nontemporal_pi32(int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.LoadAlignedVector256NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_loadalignedvector256nontemporal_pi64(int64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.LoadAlignedVector256NonTemporal");
    return dn2cpp_isa_vec<32>(_mm256_stream_load_si256((__m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_loadalignedvector256nontemporal_pi64(int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.LoadAlignedVector256NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_loadalignedvector256nontemporal_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.LoadAlignedVector256NonTemporal");
    return dn2cpp_isa_vec<32>(_mm256_stream_load_si256((__m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_loadalignedvector256nontemporal_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.LoadAlignedVector256NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_loadalignedvector256nontemporal_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.LoadAlignedVector256NonTemporal");
    return dn2cpp_isa_vec<32>(_mm256_stream_load_si256((__m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_loadalignedvector256nontemporal_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.LoadAlignedVector256NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_loadalignedvector256nontemporal_pu32(uint32_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.LoadAlignedVector256NonTemporal");
    return dn2cpp_isa_vec<32>(_mm256_stream_load_si256((__m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_loadalignedvector256nontemporal_pu32(uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.LoadAlignedVector256NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_loadalignedvector256nontemporal_pu64(uint64_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.LoadAlignedVector256NonTemporal");
    return dn2cpp_isa_vec<32>(_mm256_stream_load_si256((__m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_loadalignedvector256nontemporal_pu64(uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.LoadAlignedVector256NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_loadalignedvector256nontemporal_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.LoadAlignedVector256NonTemporal");
    return dn2cpp_isa_vec<32>(_mm256_stream_load_si256((__m256i*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_loadalignedvector256nontemporal_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.LoadAlignedVector256NonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_maskload_pi32_v128i32(int32_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_maskload_epi32((const int*)a0, dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_maskload_pi32_v128i32(int32_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_maskload_pi32_v256i32(int32_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_maskload_epi32((const int*)a0, dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_maskload_pi32_v256i32(int32_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_maskload_pi64_v128i64(int64_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_maskload_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_maskload_pi64_v128i64(int64_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_maskload_pi64_v256i64(int64_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_maskload_epi64((const long long*)a0, dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_maskload_pi64_v256i64(int64_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_maskload_pu32_v128u32(uint32_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_maskload_epi32((const int*)a0, dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_maskload_pu32_v128u32(uint32_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_maskload_pu32_v256u32(uint32_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_maskload_epi32((const int*)a0, dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_maskload_pu32_v256u32(uint32_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_maskload_pu64_v128u64(uint64_t* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MaskLoad");
    return dn2cpp_isa_vec<16>(_mm_maskload_epi64((const long long*)a0, dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_maskload_pu64_v128u64(uint64_t*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_maskload_pu64_v256u64(uint64_t* a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MaskLoad");
    return dn2cpp_isa_vec<32>(_mm256_maskload_epi64((const long long*)a0, dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_maskload_pu64_v256u64(uint64_t*, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx2_maskstore_pi32_v128i32_v128i32(int32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MaskStore");
    _mm_maskstore_epi32((int*)a0, dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx2_maskstore_pi32_v128i32_v128i32(int32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx2_maskstore_pi32_v256i32_v256i32(int32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MaskStore");
    _mm256_maskstore_epi32((int*)a0, dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx2_maskstore_pi32_v256i32_v256i32(int32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx2_maskstore_pi64_v128i64_v128i64(int64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MaskStore");
    _mm_maskstore_epi64((long long*)a0, dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx2_maskstore_pi64_v128i64_v128i64(int64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx2_maskstore_pi64_v256i64_v256i64(int64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MaskStore");
    _mm256_maskstore_epi64((long long*)a0, dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx2_maskstore_pi64_v256i64_v256i64(int64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx2_maskstore_pu32_v128u32_v128u32(uint32_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MaskStore");
    _mm_maskstore_epi32((int*)a0, dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx2_maskstore_pu32_v128u32_v128u32(uint32_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx2_maskstore_pu32_v256u32_v256u32(uint32_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MaskStore");
    _mm256_maskstore_epi32((int*)a0, dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx2_maskstore_pu32_v256u32_v256u32(uint32_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx2_maskstore_pu64_v128u64_v128u64(uint64_t* a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MaskStore");
    _mm_maskstore_epi64((long long*)a0, dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx2_maskstore_pu64_v128u64_v128u64(uint64_t*, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx2_maskstore_pu64_v256u64_v256u64(uint64_t* a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MaskStore");
    _mm256_maskstore_epi64((long long*)a0, dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx2_maskstore_pu64_v256u64_v256u64(uint64_t*, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_max_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Max");
    return dn2cpp_isa_vec<32>(_mm256_max_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_max_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_max_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Max");
    return dn2cpp_isa_vec<32>(_mm256_max_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_max_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_max_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Max");
    return dn2cpp_isa_vec<32>(_mm256_max_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_max_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_max_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Max");
    return dn2cpp_isa_vec<32>(_mm256_max_epu16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_max_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_max_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Max");
    return dn2cpp_isa_vec<32>(_mm256_max_epu32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_max_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_max_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Max");
    return dn2cpp_isa_vec<32>(_mm256_max_epu8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_max_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_min_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Min");
    return dn2cpp_isa_vec<32>(_mm256_min_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_min_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_min_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Min");
    return dn2cpp_isa_vec<32>(_mm256_min_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_min_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_min_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Min");
    return dn2cpp_isa_vec<32>(_mm256_min_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_min_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_min_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Min");
    return dn2cpp_isa_vec<32>(_mm256_min_epu16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_min_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_min_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Min");
    return dn2cpp_isa_vec<32>(_mm256_min_epu32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_min_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_min_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Min");
    return dn2cpp_isa_vec<32>(_mm256_min_epu8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_min_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx2_movemask_v256i8(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MoveMask");
    return _mm256_movemask_epi8(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx2_movemask_v256i8(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx2_movemask_v256u8(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MoveMask");
    return _mm256_movemask_epi8(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx2_movemask_v256u8(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplesumabsolutedifferences_v256u8_v256u8_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MultipleSumAbsoluteDifferences");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_mpsadbw_epu8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplesumabsolutedifferences_v256u8_v256u8_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MultipleSumAbsoluteDifferences");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiply_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Multiply");
    return dn2cpp_isa_vec<32>(_mm256_mul_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiply_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Multiply");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiply_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Multiply");
    return dn2cpp_isa_vec<32>(_mm256_mul_epu32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiply_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Multiply");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplyaddadjacent_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MultiplyAddAdjacent");
    return dn2cpp_isa_vec<32>(_mm256_madd_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplyaddadjacent_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MultiplyAddAdjacent");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplyaddadjacent_v256u8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MultiplyAddAdjacent");
    return dn2cpp_isa_vec<32>(_mm256_maddubs_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplyaddadjacent_v256u8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MultiplyAddAdjacent");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplyhigh_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MultiplyHigh");
    return dn2cpp_isa_vec<32>(_mm256_mulhi_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplyhigh_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MultiplyHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplyhigh_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MultiplyHigh");
    return dn2cpp_isa_vec<32>(_mm256_mulhi_epu16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplyhigh_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MultiplyHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplyhighroundscale_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MultiplyHighRoundScale");
    return dn2cpp_isa_vec<32>(_mm256_mulhrs_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplyhighroundscale_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MultiplyHighRoundScale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplylow_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MultiplyLow");
    return dn2cpp_isa_vec<32>(_mm256_mullo_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplylow_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplylow_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MultiplyLow");
    return dn2cpp_isa_vec<32>(_mm256_mullo_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplylow_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplylow_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MultiplyLow");
    return dn2cpp_isa_vec<32>(_mm256_mullo_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplylow_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplylow_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.MultiplyLow");
    return dn2cpp_isa_vec<32>(_mm256_mullo_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_multiplylow_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_or_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Or");
    return dn2cpp_isa_vec<32>(_mm256_or_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_or_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_or_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Or");
    return dn2cpp_isa_vec<32>(_mm256_or_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_or_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_or_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Or");
    return dn2cpp_isa_vec<32>(_mm256_or_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_or_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_or_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Or");
    return dn2cpp_isa_vec<32>(_mm256_or_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_or_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_or_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Or");
    return dn2cpp_isa_vec<32>(_mm256_or_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_or_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_or_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Or");
    return dn2cpp_isa_vec<32>(_mm256_or_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_or_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_or_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Or");
    return dn2cpp_isa_vec<32>(_mm256_or_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_or_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_or_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Or");
    return dn2cpp_isa_vec<32>(_mm256_or_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_or_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_packsignedsaturate_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.PackSignedSaturate");
    return dn2cpp_isa_vec<32>(_mm256_packs_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_packsignedsaturate_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.PackSignedSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_packsignedsaturate_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.PackSignedSaturate");
    return dn2cpp_isa_vec<32>(_mm256_packs_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_packsignedsaturate_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.PackSignedSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_packunsignedsaturate_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.PackUnsignedSaturate");
    return dn2cpp_isa_vec<32>(_mm256_packus_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_packunsignedsaturate_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.PackUnsignedSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_packunsignedsaturate_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.PackUnsignedSaturate");
    return dn2cpp_isa_vec<32>(_mm256_packus_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_packunsignedsaturate_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.PackUnsignedSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute2x128_v256i16_v256i16_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Permute2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_permute2x128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute2x128_v256i16_v256i16_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Permute2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute2x128_v256i32_v256i32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Permute2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_permute2x128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute2x128_v256i32_v256i32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Permute2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute2x128_v256i64_v256i64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Permute2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_permute2x128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute2x128_v256i64_v256i64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Permute2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute2x128_v256i8_v256i8_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Permute2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_permute2x128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute2x128_v256i8_v256i8_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Permute2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute2x128_v256u16_v256u16_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Permute2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_permute2x128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute2x128_v256u16_v256u16_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Permute2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute2x128_v256u32_v256u32_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Permute2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_permute2x128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute2x128_v256u32_v256u32_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Permute2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute2x128_v256u64_v256u64_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Permute2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_permute2x128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute2x128_v256u64_v256u64_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Permute2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute2x128_v256u8_v256u8_u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Permute2x128");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<32>(_mm256_permute2x128_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute2x128_v256u8_v256u8_u8(const Dn2CppVector256&, const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Permute2x128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute4x64_v256f64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Permute4x64");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_permute4x64_pd(dn2cpp_isa_bits<__m256d>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute4x64_v256f64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Permute4x64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute4x64_v256i64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Permute4x64");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_permute4x64_epi64(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute4x64_v256i64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Permute4x64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute4x64_v256u64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Permute4x64");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_permute4x64_epi64(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permute4x64_v256u64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Permute4x64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permutevar8x32_v256f32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.PermuteVar8x32");
    return dn2cpp_isa_vec<32>(_mm256_permutevar8x32_ps(dn2cpp_isa_bits<__m256>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permutevar8x32_v256f32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.PermuteVar8x32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permutevar8x32_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.PermuteVar8x32");
    return dn2cpp_isa_vec<32>(_mm256_permutevar8x32_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permutevar8x32_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.PermuteVar8x32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permutevar8x32_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.PermuteVar8x32");
    return dn2cpp_isa_vec<32>(_mm256_permutevar8x32_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_permutevar8x32_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.PermuteVar8x32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical128bitlane_v256i16_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_slli_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical128bitlane_v256i16_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical128bitlane_v256i32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_slli_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical128bitlane_v256i32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical128bitlane_v256i64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_slli_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical128bitlane_v256i64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical128bitlane_v256i8_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_slli_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical128bitlane_v256i8_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical128bitlane_v256u16_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_slli_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical128bitlane_v256u16_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical128bitlane_v256u32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_slli_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical128bitlane_v256u32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical128bitlane_v256u64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_slli_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical128bitlane_v256u64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical128bitlane_v256u8_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_slli_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical128bitlane_v256u8_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256i16_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_slli_epi16(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256i16_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256i16_v128i16(const Dn2CppVector256& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
    return dn2cpp_isa_vec<32>(_mm256_sll_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256i16_v128i16(const Dn2CppVector256&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256i32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_slli_epi32(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256i32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256i32_v128i32(const Dn2CppVector256& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
    return dn2cpp_isa_vec<32>(_mm256_sll_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256i32_v128i32(const Dn2CppVector256&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256i64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_slli_epi64(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256i64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256i64_v128i64(const Dn2CppVector256& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
    return dn2cpp_isa_vec<32>(_mm256_sll_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256i64_v128i64(const Dn2CppVector256&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256u16_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_slli_epi16(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256u16_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256u16_v128u16(const Dn2CppVector256& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
    return dn2cpp_isa_vec<32>(_mm256_sll_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256u16_v128u16(const Dn2CppVector256&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256u32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_slli_epi32(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256u32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256u32_v128u32(const Dn2CppVector256& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
    return dn2cpp_isa_vec<32>(_mm256_sll_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256u32_v128u32(const Dn2CppVector256&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256u64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_slli_epi64(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256u64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256u64_v128u64(const Dn2CppVector256& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
    return dn2cpp_isa_vec<32>(_mm256_sll_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogical_v256u64_v128u64(const Dn2CppVector256&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_shiftleftlogicalvariable_v128i32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<16>(_mm_sllv_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_shiftleftlogicalvariable_v128i32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_shiftleftlogicalvariable_v128i64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<16>(_mm_sllv_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_shiftleftlogicalvariable_v128i64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_shiftleftlogicalvariable_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<16>(_mm_sllv_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_shiftleftlogicalvariable_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_shiftleftlogicalvariable_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<16>(_mm_sllv_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_shiftleftlogicalvariable_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogicalvariable_v256i32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<32>(_mm256_sllv_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogicalvariable_v256i32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogicalvariable_v256i64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<32>(_mm256_sllv_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogicalvariable_v256i64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogicalvariable_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<32>(_mm256_sllv_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogicalvariable_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogicalvariable_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<32>(_mm256_sllv_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftleftlogicalvariable_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightarithmetic_v256i16_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightArithmetic");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_srai_epi16(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightarithmetic_v256i16_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightarithmetic_v256i16_v128i16(const Dn2CppVector256& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightArithmetic");
    return dn2cpp_isa_vec<32>(_mm256_sra_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightarithmetic_v256i16_v128i16(const Dn2CppVector256&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightarithmetic_v256i32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightArithmetic");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_srai_epi32(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightarithmetic_v256i32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightarithmetic_v256i32_v128i32(const Dn2CppVector256& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightArithmetic");
    return dn2cpp_isa_vec<32>(_mm256_sra_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightarithmetic_v256i32_v128i32(const Dn2CppVector256&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_shiftrightarithmeticvariable_v128i32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightArithmeticVariable");
    return dn2cpp_isa_vec<16>(_mm_srav_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_shiftrightarithmeticvariable_v128i32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightArithmeticVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightarithmeticvariable_v256i32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightArithmeticVariable");
    return dn2cpp_isa_vec<32>(_mm256_srav_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightarithmeticvariable_v256i32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightArithmeticVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical128bitlane_v256i16_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_srli_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical128bitlane_v256i16_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical128bitlane_v256i32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_srli_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical128bitlane_v256i32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical128bitlane_v256i64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_srli_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical128bitlane_v256i64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical128bitlane_v256i8_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_srli_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical128bitlane_v256i8_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical128bitlane_v256u16_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_srli_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical128bitlane_v256u16_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical128bitlane_v256u32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_srli_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical128bitlane_v256u32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical128bitlane_v256u64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_srli_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical128bitlane_v256u64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical128bitlane_v256u8_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_srli_si256(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical128bitlane_v256u8_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256i16_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_srli_epi16(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256i16_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256i16_v128i16(const Dn2CppVector256& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
    return dn2cpp_isa_vec<32>(_mm256_srl_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256i16_v128i16(const Dn2CppVector256&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256i32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_srli_epi32(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256i32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256i32_v128i32(const Dn2CppVector256& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
    return dn2cpp_isa_vec<32>(_mm256_srl_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256i32_v128i32(const Dn2CppVector256&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256i64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_srli_epi64(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256i64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256i64_v128i64(const Dn2CppVector256& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
    return dn2cpp_isa_vec<32>(_mm256_srl_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256i64_v128i64(const Dn2CppVector256&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256u16_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_srli_epi16(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256u16_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256u16_v128u16(const Dn2CppVector256& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
    return dn2cpp_isa_vec<32>(_mm256_srl_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256u16_v128u16(const Dn2CppVector256&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256u32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_srli_epi32(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256u32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256u32_v128u32(const Dn2CppVector256& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
    return dn2cpp_isa_vec<32>(_mm256_srl_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256u32_v128u32(const Dn2CppVector256&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256u64_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_srli_epi64(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256u64_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256u64_v128u64(const Dn2CppVector256& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
    return dn2cpp_isa_vec<32>(_mm256_srl_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogical_v256u64_v128u64(const Dn2CppVector256&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_shiftrightlogicalvariable_v128i32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<16>(_mm_srlv_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_shiftrightlogicalvariable_v128i32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_shiftrightlogicalvariable_v128i64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<16>(_mm_srlv_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_shiftrightlogicalvariable_v128i64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_shiftrightlogicalvariable_v128u32_v128u32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<16>(_mm_srlv_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_shiftrightlogicalvariable_v128u32_v128u32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_shiftrightlogicalvariable_v128u64_v128u64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<16>(_mm_srlv_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avx2_shiftrightlogicalvariable_v128u64_v128u64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogicalvariable_v256i32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<32>(_mm256_srlv_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogicalvariable_v256i32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogicalvariable_v256i64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<32>(_mm256_srlv_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogicalvariable_v256i64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogicalvariable_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<32>(_mm256_srlv_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogicalvariable_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogicalvariable_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<32>(_mm256_srlv_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shiftrightlogicalvariable_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shuffle_v256i32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Shuffle");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_shuffle_epi32(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shuffle_v256i32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Shuffle");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shuffle_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Shuffle");
    return dn2cpp_isa_vec<32>(_mm256_shuffle_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shuffle_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Shuffle");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shuffle_v256u32_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Shuffle");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_shuffle_epi32(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shuffle_v256u32_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Shuffle");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shuffle_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Shuffle");
    return dn2cpp_isa_vec<32>(_mm256_shuffle_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shuffle_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Shuffle");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shufflehigh_v256i16_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShuffleHigh");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_shufflehi_epi16(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shufflehigh_v256i16_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShuffleHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shufflehigh_v256u16_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShuffleHigh");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_shufflehi_epi16(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shufflehigh_v256u16_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShuffleHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shufflelow_v256i16_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShuffleLow");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_shufflelo_epi16(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shufflelow_v256i16_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShuffleLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shufflelow_v256u16_u8(const Dn2CppVector256& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.ShuffleLow");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<32>(_mm256_shufflelo_epi16(dn2cpp_isa_bits<__m256i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_shufflelow_v256u16_u8(const Dn2CppVector256&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.ShuffleLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_sign_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Sign");
    return dn2cpp_isa_vec<32>(_mm256_sign_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_sign_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Sign");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_sign_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Sign");
    return dn2cpp_isa_vec<32>(_mm256_sign_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_sign_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Sign");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_sign_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Sign");
    return dn2cpp_isa_vec<32>(_mm256_sign_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_sign_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Sign");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtract_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Subtract");
    return dn2cpp_isa_vec<32>(_mm256_sub_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtract_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtract_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Subtract");
    return dn2cpp_isa_vec<32>(_mm256_sub_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtract_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtract_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Subtract");
    return dn2cpp_isa_vec<32>(_mm256_sub_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtract_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtract_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Subtract");
    return dn2cpp_isa_vec<32>(_mm256_sub_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtract_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtract_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Subtract");
    return dn2cpp_isa_vec<32>(_mm256_sub_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtract_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtract_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Subtract");
    return dn2cpp_isa_vec<32>(_mm256_sub_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtract_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtract_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Subtract");
    return dn2cpp_isa_vec<32>(_mm256_sub_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtract_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtract_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Subtract");
    return dn2cpp_isa_vec<32>(_mm256_sub_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtract_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtractsaturate_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.SubtractSaturate");
    return dn2cpp_isa_vec<32>(_mm256_subs_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtractsaturate_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtractsaturate_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.SubtractSaturate");
    return dn2cpp_isa_vec<32>(_mm256_subs_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtractsaturate_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtractsaturate_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.SubtractSaturate");
    return dn2cpp_isa_vec<32>(_mm256_subs_epu16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtractsaturate_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtractsaturate_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.SubtractSaturate");
    return dn2cpp_isa_vec<32>(_mm256_subs_epu8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_subtractsaturate_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_sumabsolutedifferences_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.SumAbsoluteDifferences");
    return dn2cpp_isa_vec<32>(_mm256_sad_epu8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_sumabsolutedifferences_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.SumAbsoluteDifferences");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpackhigh_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.UnpackHigh");
    return dn2cpp_isa_vec<32>(_mm256_unpackhi_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpackhigh_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpackhigh_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.UnpackHigh");
    return dn2cpp_isa_vec<32>(_mm256_unpackhi_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpackhigh_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpackhigh_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.UnpackHigh");
    return dn2cpp_isa_vec<32>(_mm256_unpackhi_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpackhigh_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpackhigh_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.UnpackHigh");
    return dn2cpp_isa_vec<32>(_mm256_unpackhi_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpackhigh_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpackhigh_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.UnpackHigh");
    return dn2cpp_isa_vec<32>(_mm256_unpackhi_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpackhigh_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpackhigh_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.UnpackHigh");
    return dn2cpp_isa_vec<32>(_mm256_unpackhi_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpackhigh_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpackhigh_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.UnpackHigh");
    return dn2cpp_isa_vec<32>(_mm256_unpackhi_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpackhigh_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpackhigh_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.UnpackHigh");
    return dn2cpp_isa_vec<32>(_mm256_unpackhi_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpackhigh_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpacklow_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.UnpackLow");
    return dn2cpp_isa_vec<32>(_mm256_unpacklo_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpacklow_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpacklow_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.UnpackLow");
    return dn2cpp_isa_vec<32>(_mm256_unpacklo_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpacklow_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpacklow_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.UnpackLow");
    return dn2cpp_isa_vec<32>(_mm256_unpacklo_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpacklow_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpacklow_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.UnpackLow");
    return dn2cpp_isa_vec<32>(_mm256_unpacklo_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpacklow_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpacklow_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.UnpackLow");
    return dn2cpp_isa_vec<32>(_mm256_unpacklo_epi16(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpacklow_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpacklow_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.UnpackLow");
    return dn2cpp_isa_vec<32>(_mm256_unpacklo_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpacklow_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpacklow_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.UnpackLow");
    return dn2cpp_isa_vec<32>(_mm256_unpacklo_epi64(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpacklow_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpacklow_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.UnpackLow");
    return dn2cpp_isa_vec<32>(_mm256_unpacklo_epi8(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_unpacklow_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_xor_v256i16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Xor");
    return dn2cpp_isa_vec<32>(_mm256_xor_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_xor_v256i16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_xor_v256i32_v256i32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Xor");
    return dn2cpp_isa_vec<32>(_mm256_xor_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_xor_v256i32_v256i32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_xor_v256i64_v256i64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Xor");
    return dn2cpp_isa_vec<32>(_mm256_xor_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_xor_v256i64_v256i64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_xor_v256i8_v256i8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Xor");
    return dn2cpp_isa_vec<32>(_mm256_xor_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_xor_v256i8_v256i8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_xor_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Xor");
    return dn2cpp_isa_vec<32>(_mm256_xor_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_xor_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_xor_v256u32_v256u32(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Xor");
    return dn2cpp_isa_vec<32>(_mm256_xor_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_xor_v256u32_v256u32(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_xor_v256u64_v256u64(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Xor");
    return dn2cpp_isa_vec<32>(_mm256_xor_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_xor_v256u64_v256u64(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Xor");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx2") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_xor_v256u8_v256u8(const Dn2CppVector256& a0, const Dn2CppVector256& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx2, "System.Runtime.Intrinsics.X86.Avx2.Xor");
    return dn2cpp_isa_vec<32>(_mm256_xor_si256(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx2_xor_v256u8_v256u8(const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx2.Xor");
}
#endif
