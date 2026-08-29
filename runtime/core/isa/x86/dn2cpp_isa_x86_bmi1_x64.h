#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Bmi1+X64: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi1_x64_andnot_u64_u64(uint64_t a0, uint64_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi1_X64, "System.Runtime.Intrinsics.X86.Bmi1+X64.AndNot");
    return _andn_u64(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi1_x64_andnot_u64_u64(uint64_t, uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi1+X64.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi1_x64_bitfieldextract_u64_u16(uint64_t a0, uint16_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi1_X64, "System.Runtime.Intrinsics.X86.Bmi1+X64.BitFieldExtract");
    return _bextr_u64(a0, (uint32_t)(a1 & 0xFFu), (uint32_t)(a1 >> 8));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi1_x64_bitfieldextract_u64_u16(uint64_t, uint16_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi1+X64.BitFieldExtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi1_x64_bitfieldextract_u64_u8_u8(uint64_t a0, uint8_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi1_X64, "System.Runtime.Intrinsics.X86.Bmi1+X64.BitFieldExtract");
    return _bextr_u64(a0, a1, a2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi1_x64_bitfieldextract_u64_u8_u8(uint64_t, uint8_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi1+X64.BitFieldExtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi1_x64_extractlowestsetbit_u64(uint64_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi1_X64, "System.Runtime.Intrinsics.X86.Bmi1+X64.ExtractLowestSetBit");
    return _blsi_u64(a0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi1_x64_extractlowestsetbit_u64(uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi1+X64.ExtractLowestSetBit");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi1_x64_getmaskuptolowestsetbit_u64(uint64_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi1_X64, "System.Runtime.Intrinsics.X86.Bmi1+X64.GetMaskUpToLowestSetBit");
    return _blsmsk_u64(a0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi1_x64_getmaskuptolowestsetbit_u64(uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi1+X64.GetMaskUpToLowestSetBit");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi1_x64_resetlowestsetbit_u64(uint64_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi1_X64, "System.Runtime.Intrinsics.X86.Bmi1+X64.ResetLowestSetBit");
    return _blsr_u64(a0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi1_x64_resetlowestsetbit_u64(uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi1+X64.ResetLowestSetBit");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi1_x64_trailingzerocount_u64(uint64_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi1_X64, "System.Runtime.Intrinsics.X86.Bmi1+X64.TrailingZeroCount");
    return _tzcnt_u64(a0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_bmi1_x64_trailingzerocount_u64(uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi1+X64.TrailingZeroCount");
}
#endif
