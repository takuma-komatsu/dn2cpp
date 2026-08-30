#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.X86Base: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_cpuid_i32_i32(int32_t a0, int32_t a1, int32_t* item1, int32_t* item2, int32_t* item3, int32_t* item4)
{
    dn2cpp_isa_cpuid(a0, a1, item1, item2, item3, item4);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_cpuid_i32_i32(int32_t, int32_t, int32_t*, int32_t*, int32_t*, int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.X86Base.CpuId");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_divrem_nuint_nint_nint(uintptr_t a0, intptr_t a1, intptr_t a2, intptr_t* item1, intptr_t* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_X86Base, "System.Runtime.Intrinsics.X86.X86Base.DivRem");
    dn2cpp_isa_divrem_nint(a0, a1, a2, item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_divrem_nuint_nint_nint(uintptr_t, intptr_t, intptr_t, intptr_t*, intptr_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.X86Base.DivRem");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_divrem_nuint_nuint_nuint(uintptr_t a0, uintptr_t a1, uintptr_t a2, uintptr_t* item1, uintptr_t* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_X86Base, "System.Runtime.Intrinsics.X86.X86Base.DivRem");
    dn2cpp_isa_divrem_nuint(a0, a1, a2, item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_divrem_nuint_nuint_nuint(uintptr_t, uintptr_t, uintptr_t, uintptr_t*, uintptr_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.X86Base.DivRem");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_divrem_u32_i32_i32(uint32_t a0, int32_t a1, int32_t a2, int32_t* item1, int32_t* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_X86Base, "System.Runtime.Intrinsics.X86.X86Base.DivRem");
    dn2cpp_isa_divrem_i32(a0, a1, a2, item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_divrem_u32_i32_i32(uint32_t, int32_t, int32_t, int32_t*, int32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.X86Base.DivRem");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_divrem_u32_u32_u32(uint32_t a0, uint32_t a1, uint32_t a2, uint32_t* item1, uint32_t* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_X86Base, "System.Runtime.Intrinsics.X86.X86Base.DivRem");
    dn2cpp_isa_divrem_u32(a0, a1, a2, item1, item2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_divrem_u32_u32_u32(uint32_t, uint32_t, uint32_t, uint32_t*, uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.X86Base.DivRem");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_pause()
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_X86Base, "System.Runtime.Intrinsics.X86.X86Base.Pause");
    _mm_pause();
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_pause()
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.X86Base.Pause");
}
#endif
