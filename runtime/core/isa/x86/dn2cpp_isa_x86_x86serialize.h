#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.X86Serialize: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("serialize") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86serialize_serialize()
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_X86Serialize, "System.Runtime.Intrinsics.X86.X86Serialize.Serialize");
    _serialize();
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86serialize_serialize()
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.X86Serialize.Serialize");
}
#endif
