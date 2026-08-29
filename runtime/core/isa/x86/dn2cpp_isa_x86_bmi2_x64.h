#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Bmi2+X64: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi2") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi2_x64_multiplynoflags_u64_u64(uint64_t a0, uint64_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi2_X64, "System.Runtime.Intrinsics.X86.Bmi2+X64.MultiplyNoFlags");
    return dn2cpp_isa_umulh64(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi2_x64_multiplynoflags_u64_u64(uint64_t, uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi2+X64.MultiplyNoFlags");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi2") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi2_x64_multiplynoflags_u64_u64_pu64(uint64_t a0, uint64_t a1, uint64_t* a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi2_X64, "System.Runtime.Intrinsics.X86.Bmi2+X64.MultiplyNoFlags");
    return dn2cpp_isa_mulx64(a0, a1, a2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi2_x64_multiplynoflags_u64_u64_pu64(uint64_t, uint64_t, uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi2+X64.MultiplyNoFlags");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi2") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi2_x64_parallelbitdeposit_u64_u64(uint64_t a0, uint64_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi2_X64, "System.Runtime.Intrinsics.X86.Bmi2+X64.ParallelBitDeposit");
    return _pdep_u64(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi2_x64_parallelbitdeposit_u64_u64(uint64_t, uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi2+X64.ParallelBitDeposit");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi2") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi2_x64_parallelbitextract_u64_u64(uint64_t a0, uint64_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi2_X64, "System.Runtime.Intrinsics.X86.Bmi2+X64.ParallelBitExtract");
    return _pext_u64(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi2_x64_parallelbitextract_u64_u64(uint64_t, uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi2+X64.ParallelBitExtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi2") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi2_x64_zerohighbits_u64_u64(uint64_t a0, uint64_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi2_X64, "System.Runtime.Intrinsics.X86.Bmi2+X64.ZeroHighBits");
    return _bzhi_u64(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi2_x64_zerohighbits_u64_u64(uint64_t, uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi2+X64.ZeroHighBits");
}
#endif
