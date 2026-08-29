#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.X86Base+X64: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_x64_divrem_u64_i64_i64(uint64_t a0, int64_t a1, int64_t a2, int64_t* item1, int64_t* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_X86Base_X64, "System.Runtime.Intrinsics.X86.X86Base+X64.DivRem");
    dn2cpp_isa_divrem_i64(a0, a1, a2, item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_x64_divrem_u64_i64_i64(uint64_t, int64_t, int64_t, int64_t*, int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.X86Base+X64.DivRem");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_x64_divrem_u64_u64_u64(uint64_t a0, uint64_t a1, uint64_t a2, uint64_t* item1, uint64_t* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_X86Base_X64, "System.Runtime.Intrinsics.X86.X86Base+X64.DivRem");
    dn2cpp_isa_divrem_u64(a0, a1, a2, item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_x64_divrem_u64_u64_u64(uint64_t, uint64_t, uint64_t, uint64_t*, uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.X86Base+X64.DivRem");
}
#endif
