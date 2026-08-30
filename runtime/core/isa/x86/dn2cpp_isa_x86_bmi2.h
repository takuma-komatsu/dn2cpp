#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Bmi2: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi2") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi2_multiplynoflags_u32_u32(uint32_t a0, uint32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi2, "System.Runtime.Intrinsics.X86.Bmi2.MultiplyNoFlags");
    return (uint32_t)(((uint64_t)a0 * a1) >> 32);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi2_multiplynoflags_u32_u32(uint32_t, uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi2.MultiplyNoFlags");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi2") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi2_multiplynoflags_u32_u32_pu32(uint32_t a0, uint32_t a1, uint32_t* a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi2, "System.Runtime.Intrinsics.X86.Bmi2.MultiplyNoFlags");
    return dn2cpp_isa_mulx32(a0, a1, a2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi2_multiplynoflags_u32_u32_pu32(uint32_t, uint32_t, uint32_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi2.MultiplyNoFlags");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi2") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi2_parallelbitdeposit_u32_u32(uint32_t a0, uint32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi2, "System.Runtime.Intrinsics.X86.Bmi2.ParallelBitDeposit");
    return _pdep_u32(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi2_parallelbitdeposit_u32_u32(uint32_t, uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi2.ParallelBitDeposit");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi2") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi2_parallelbitextract_u32_u32(uint32_t a0, uint32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi2, "System.Runtime.Intrinsics.X86.Bmi2.ParallelBitExtract");
    return _pext_u32(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi2_parallelbitextract_u32_u32(uint32_t, uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi2.ParallelBitExtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi2") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi2_zerohighbits_u32_u32(uint32_t a0, uint32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi2, "System.Runtime.Intrinsics.X86.Bmi2.ZeroHighBits");
    return _bzhi_u32(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi2_zerohighbits_u32_u32(uint32_t, uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi2.ZeroHighBits");
}
#endif
