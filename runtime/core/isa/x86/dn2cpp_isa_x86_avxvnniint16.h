#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.AvxVnniInt16: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avxvnniint16") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnniint16_multiplywideningandadd_v128i32_v128i16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt16, "System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAdd");
    return dn2cpp_isa_vec<16>(_mm_dpwsud_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnniint16_multiplywideningandadd_v128i32_v128i16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avxvnniint16") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnniint16_multiplywideningandadd_v128i32_v128u16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt16, "System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAdd");
    return dn2cpp_isa_vec<16>(_mm_dpwusd_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnniint16_multiplywideningandadd_v128i32_v128u16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avxvnniint16") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnniint16_multiplywideningandadd_v128u32_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt16, "System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAdd");
    return dn2cpp_isa_vec<16>(_mm_dpwuud_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnniint16_multiplywideningandadd_v128u32_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avxvnniint16") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnniint16_multiplywideningandadd_v256i32_v256i16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt16, "System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAdd");
    return dn2cpp_isa_vec<32>(_mm256_dpwsud_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnniint16_multiplywideningandadd_v256i32_v256i16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avxvnniint16") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnniint16_multiplywideningandadd_v256i32_v256u16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt16, "System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAdd");
    return dn2cpp_isa_vec<32>(_mm256_dpwusd_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnniint16_multiplywideningandadd_v256i32_v256u16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avxvnniint16") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnniint16_multiplywideningandadd_v256u32_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt16, "System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAdd");
    return dn2cpp_isa_vec<32>(_mm256_dpwuud_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnniint16_multiplywideningandadd_v256u32_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAdd");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avxvnniint16") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnniint16_multiplywideningandaddsaturate_v128i32_v128i16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt16, "System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAddSaturate");
    return dn2cpp_isa_vec<16>(_mm_dpwsuds_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnniint16_multiplywideningandaddsaturate_v128i32_v128i16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avxvnniint16") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnniint16_multiplywideningandaddsaturate_v128i32_v128u16_v128i16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt16, "System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAddSaturate");
    return dn2cpp_isa_vec<16>(_mm_dpwusds_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnniint16_multiplywideningandaddsaturate_v128i32_v128u16_v128i16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avxvnniint16") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnniint16_multiplywideningandaddsaturate_v128u32_v128u16_v128u16(const Dn2CppVector128& a0, const Dn2CppVector128& a1, const Dn2CppVector128& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt16, "System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAddSaturate");
    return dn2cpp_isa_vec<16>(_mm_dpwuuds_epi32(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1), dn2cpp_isa_bits<__m128i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_avxvnniint16_multiplywideningandaddsaturate_v128u32_v128u16_v128u16(const Dn2CppVector128&, const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avxvnniint16") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnniint16_multiplywideningandaddsaturate_v256i32_v256i16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt16, "System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAddSaturate");
    return dn2cpp_isa_vec<32>(_mm256_dpwsuds_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnniint16_multiplywideningandaddsaturate_v256i32_v256i16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avxvnniint16") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnniint16_multiplywideningandaddsaturate_v256i32_v256u16_v256i16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt16, "System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAddSaturate");
    return dn2cpp_isa_vec<32>(_mm256_dpwusds_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnniint16_multiplywideningandaddsaturate_v256i32_v256u16_v256i16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avxvnniint16") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnniint16_multiplywideningandaddsaturate_v256u32_v256u16_v256u16(const Dn2CppVector256& a0, const Dn2CppVector256& a1, const Dn2CppVector256& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_AvxVnniInt16, "System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAddSaturate");
    return dn2cpp_isa_vec<32>(_mm256_dpwuuds_epi32(dn2cpp_isa_bits<__m256i>(a0), dn2cpp_isa_bits<__m256i>(a1), dn2cpp_isa_bits<__m256i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avxvnniint16_multiplywideningandaddsaturate_v256u32_v256u16_v256u16(const Dn2CppVector256&, const Dn2CppVector256&, const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.AvxVnniInt16.MultiplyWideningAndAddSaturate");
}
#endif
