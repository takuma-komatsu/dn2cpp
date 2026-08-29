#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Sse42: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse42_comparegreaterthan_v128i64_v128i64(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse42, "System.Runtime.Intrinsics.X86.Sse42.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_cmpgt_epi64(dn2cpp_isa_bits<__m128i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse42_comparegreaterthan_v128i64_v128i64(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse42.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.2") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_sse42_crc32_u32_u16(uint32_t a0, uint16_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse42, "System.Runtime.Intrinsics.X86.Sse42.Crc32");
    return _mm_crc32_u16(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_sse42_crc32_u32_u16(uint32_t, uint16_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse42.Crc32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.2") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_sse42_crc32_u32_u32(uint32_t a0, uint32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse42, "System.Runtime.Intrinsics.X86.Sse42.Crc32");
    return _mm_crc32_u32(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_sse42_crc32_u32_u32(uint32_t, uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse42.Crc32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.2") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_sse42_crc32_u32_u8(uint32_t a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse42, "System.Runtime.Intrinsics.X86.Sse42.Crc32");
    return _mm_crc32_u8(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_sse42_crc32_u32_u8(uint32_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse42.Crc32");
}
#endif
