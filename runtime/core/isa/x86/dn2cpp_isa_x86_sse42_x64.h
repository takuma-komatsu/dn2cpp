#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Sse42+X64: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse4.2") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_sse42_x64_crc32_u64_u64(uint64_t a0, uint64_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse42_X64, "System.Runtime.Intrinsics.X86.Sse42+X64.Crc32");
    return _mm_crc32_u64(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_sse42_x64_crc32_u64_u64(uint64_t, uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse42+X64.Crc32");
}
#endif
