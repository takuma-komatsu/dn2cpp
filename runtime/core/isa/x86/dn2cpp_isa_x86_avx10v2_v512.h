#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx10v2+V512: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v2_v512_converttobytewithsaturationandzeroextendtoint32_v512f32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2_V512, "System.Runtime.Intrinsics.X86.Avx10v2+V512.ConvertToByteWithSaturationAndZeroExtendToInt32");
    return dn2cpp_isa_vec<64>(_mm512_ipcvts_ps_epu8(dn2cpp_isa_bits<__m512>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v2_v512_converttobytewithsaturationandzeroextendtoint32_v512f32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2+V512.ConvertToByteWithSaturationAndZeroExtendToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v2_v512_converttobytewithsaturationandzeroextendtoint32_v512f32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2_V512, "System.Runtime.Intrinsics.X86.Avx10v2+V512.ConvertToByteWithSaturationAndZeroExtendToInt32");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epu8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epu8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epu8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epu8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epu8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epu8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epu8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epu8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epu8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epu8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epu8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epu8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v2_v512_converttobytewithsaturationandzeroextendtoint32_v512f32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2+V512.ConvertToByteWithSaturationAndZeroExtendToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v2_v512_converttobytewithtruncatedsaturationandzeroextendtoint32_v512f32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2_V512, "System.Runtime.Intrinsics.X86.Avx10v2+V512.ConvertToByteWithTruncatedSaturationAndZeroExtendToInt32");
    return dn2cpp_isa_vec<64>(_mm512_ipcvtts_ps_epu8(dn2cpp_isa_bits<__m512>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v2_v512_converttobytewithtruncatedsaturationandzeroextendtoint32_v512f32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2+V512.ConvertToByteWithTruncatedSaturationAndZeroExtendToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v2_v512_converttosbytewithsaturationandzeroextendtoint32_v512f32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2_V512, "System.Runtime.Intrinsics.X86.Avx10v2+V512.ConvertToSByteWithSaturationAndZeroExtendToInt32");
    return dn2cpp_isa_vec<64>(_mm512_ipcvts_ps_epi8(dn2cpp_isa_bits<__m512>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v2_v512_converttosbytewithsaturationandzeroextendtoint32_v512f32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2+V512.ConvertToSByteWithSaturationAndZeroExtendToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v2_v512_converttosbytewithsaturationandzeroextendtoint32_v512f32_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2_V512, "System.Runtime.Intrinsics.X86.Avx10v2+V512.ConvertToSByteWithSaturationAndZeroExtendToInt32");
    switch ((int)a1) { DN2CPP_ISA_IMM_CASE(0, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epi8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(1, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epi8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(2, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epi8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(3, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epi8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(4, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epi8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(5, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epi8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(6, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epi8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(7, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epi8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(8, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epi8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(9, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epi8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(10, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epi8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) DN2CPP_ISA_IMM_CASE(11, dn2cpp_isa_vec<64>(_mm512_ipcvts_roundps_epi8(dn2cpp_isa_bits<__m512>(a0), (DN2CPP_IMM & 3) | 8))) default: dn2cpp_throw_argument_out_of_range(); }
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v2_v512_converttosbytewithsaturationandzeroextendtoint32_v512f32_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2+V512.ConvertToSByteWithSaturationAndZeroExtendToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v2_v512_converttosbytewithtruncatedsaturationandzeroextendtoint32_v512f32(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2_V512, "System.Runtime.Intrinsics.X86.Avx10v2+V512.ConvertToSByteWithTruncatedSaturationAndZeroExtendToInt32");
    return dn2cpp_isa_vec<64>(_mm512_ipcvtts_ps_epi8(dn2cpp_isa_bits<__m512>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v2_v512_converttosbytewithtruncatedsaturationandzeroextendtoint32_v512f32(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2+V512.ConvertToSByteWithTruncatedSaturationAndZeroExtendToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v2_v512_minmax_v512f32_v512f32_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2_V512, "System.Runtime.Intrinsics.X86.Avx10v2+V512.MinMax");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<64>(_mm512_minmax_ps(dn2cpp_isa_bits<__m512>(a0), dn2cpp_isa_bits<__m512>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v2_v512_minmax_v512f32_v512f32_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2+V512.MinMax");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v2_v512_minmax_v512f64_v512f64_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2_V512, "System.Runtime.Intrinsics.X86.Avx10v2+V512.MinMax");
    DN2CPP_ISA_IMM_RANGE_SWITCH(0, 32, a2, dn2cpp_isa_vec<64>(_mm512_minmax_pd(dn2cpp_isa_bits<__m512d>(a0), dn2cpp_isa_bits<__m512d>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v2_v512_minmax_v512f64_v512f64_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2+V512.MinMax");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx10.2-512") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v2_v512_multiplesumabsolutedifferences_v512u8_v512u8_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx10v2_V512, "System.Runtime.Intrinsics.X86.Avx10v2+V512.MultipleSumAbsoluteDifferences");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_mpsadbw_epu8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx10v2_v512_multiplesumabsolutedifferences_v512u8_v512u8_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx10v2+V512.MultipleSumAbsoluteDifferences");
}
#endif
