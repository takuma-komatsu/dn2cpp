#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Sse41+X64: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_sse41_x64_extract_v128i64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41_X64, "System.Runtime.Intrinsics.X86.Sse41+X64.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, _mm_extract_epi64(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_sse41_x64_extract_v128i64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41+X64.Extract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_sse41_x64_extract_v128u64_u8(const Dn2CppVector128& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41_X64, "System.Runtime.Intrinsics.X86.Sse41+X64.Extract");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a1, (uint64_t)_mm_extract_epi64(dn2cpp_isa_bits<__m128i>(a0), DN2CPP_IMM));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_sse41_x64_extract_v128u64_u8(const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41+X64.Extract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_x64_insert_v128i64_i64_u8(const Dn2CppVector128& a0, int64_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41_X64, "System.Runtime.Intrinsics.X86.Sse41+X64.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, dn2cpp_isa_vec<16>(_mm_insert_epi64(dn2cpp_isa_bits<__m128i>(a0), (long long)a1, DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_x64_insert_v128i64_i64_u8(const Dn2CppVector128&, int64_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41+X64.Insert");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.1") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_x64_insert_v128u64_u64_u8(const Dn2CppVector128& a0, uint64_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse41_X64, "System.Runtime.Intrinsics.X86.Sse41+X64.Insert");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 2, a2, dn2cpp_isa_vec<16>(_mm_insert_epi64(dn2cpp_isa_bits<__m128i>(a0), (long long)a1, DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse41_x64_insert_v128u64_u64_u8(const Dn2CppVector128&, uint64_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse41+X64.Insert");
}
#endif
