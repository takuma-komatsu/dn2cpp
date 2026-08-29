#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Bmi1: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi1_andnot_u32_u32(uint32_t a0, uint32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi1, "System.Runtime.Intrinsics.X86.Bmi1.AndNot");
    return _andn_u32(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi1_andnot_u32_u32(uint32_t, uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi1.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi1_bitfieldextract_u32_u16(uint32_t a0, uint16_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi1, "System.Runtime.Intrinsics.X86.Bmi1.BitFieldExtract");
    return _bextr_u32(a0, (uint32_t)(a1 & 0xFFu), (uint32_t)(a1 >> 8));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi1_bitfieldextract_u32_u16(uint32_t, uint16_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi1.BitFieldExtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi1_bitfieldextract_u32_u8_u8(uint32_t a0, uint8_t a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi1, "System.Runtime.Intrinsics.X86.Bmi1.BitFieldExtract");
    return _bextr_u32(a0, a1, a2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi1_bitfieldextract_u32_u8_u8(uint32_t, uint8_t, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi1.BitFieldExtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi1_extractlowestsetbit_u32(uint32_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi1, "System.Runtime.Intrinsics.X86.Bmi1.ExtractLowestSetBit");
    return _blsi_u32(a0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi1_extractlowestsetbit_u32(uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi1.ExtractLowestSetBit");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi1_getmaskuptolowestsetbit_u32(uint32_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi1, "System.Runtime.Intrinsics.X86.Bmi1.GetMaskUpToLowestSetBit");
    return _blsmsk_u32(a0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi1_getmaskuptolowestsetbit_u32(uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi1.GetMaskUpToLowestSetBit");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi1_resetlowestsetbit_u32(uint32_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi1, "System.Runtime.Intrinsics.X86.Bmi1.ResetLowestSetBit");
    return _blsr_u32(a0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi1_resetlowestsetbit_u32(uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi1.ResetLowestSetBit");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("bmi") DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi1_trailingzerocount_u32(uint32_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Bmi1, "System.Runtime.Intrinsics.X86.Bmi1.TrailingZeroCount");
    return _tzcnt_u32(a0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_bmi1_trailingzerocount_u32(uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Bmi1.TrailingZeroCount");
}
#endif
