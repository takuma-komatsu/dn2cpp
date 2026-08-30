#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Avx512BW: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_abs_v512i16(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Abs");
    return dn2cpp_isa_vec<64>(_mm512_abs_epi16(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_abs_v512i16(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Abs");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_abs_v512i8(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Abs");
    return dn2cpp_isa_vec<64>(_mm512_abs_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_abs_v512i8(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Abs");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_add_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Add");
    return dn2cpp_isa_vec<64>(_mm512_add_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_add_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_add_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Add");
    return dn2cpp_isa_vec<64>(_mm512_add_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_add_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_add_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Add");
    return dn2cpp_isa_vec<64>(_mm512_add_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_add_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_add_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Add");
    return dn2cpp_isa_vec<64>(_mm512_add_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_add_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_addsaturate_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.AddSaturate");
    return dn2cpp_isa_vec<64>(_mm512_adds_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_addsaturate_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.AddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_addsaturate_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.AddSaturate");
    return dn2cpp_isa_vec<64>(_mm512_adds_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_addsaturate_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.AddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_addsaturate_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.AddSaturate");
    return dn2cpp_isa_vec<64>(_mm512_adds_epu16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_addsaturate_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.AddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_addsaturate_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.AddSaturate");
    return dn2cpp_isa_vec<64>(_mm512_adds_epu8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_addsaturate_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.AddSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_alignright_v512i8_v512i8_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.AlignRight");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_alignr_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_alignright_v512i8_v512i8_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.AlignRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_alignright_v512u8_v512u8_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.AlignRight");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_alignr_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_alignright_v512u8_v512u8_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.AlignRight");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_average_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Average");
    return dn2cpp_isa_vec<64>(_mm512_avg_epu16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_average_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Average");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_average_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Average");
    return dn2cpp_isa_vec<64>(_mm512_avg_epu8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_average_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Average");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_blendvariable_v512i16_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.BlendVariable");
    return dn2cpp_isa_vec<64>(_mm512_mask_blend_epi16(_mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a2)), dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_blendvariable_v512i16_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_blendvariable_v512i8_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.BlendVariable");
    return dn2cpp_isa_vec<64>(_mm512_mask_blend_epi8(_mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a2)), dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_blendvariable_v512i8_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_blendvariable_v512u16_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.BlendVariable");
    return dn2cpp_isa_vec<64>(_mm512_mask_blend_epi16(_mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a2)), dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_blendvariable_v512u16_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_blendvariable_v512u8_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.BlendVariable");
    return dn2cpp_isa_vec<64>(_mm512_mask_blend_epi8(_mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a2)), dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_blendvariable_v512u8_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.BlendVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_broadcastscalartovector512_v128i16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.BroadcastScalarToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcastw_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_broadcastscalartovector512_v128i16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.BroadcastScalarToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_broadcastscalartovector512_v128i8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.BroadcastScalarToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcastb_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_broadcastscalartovector512_v128i8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.BroadcastScalarToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_broadcastscalartovector512_v128u16(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.BroadcastScalarToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcastw_epi16(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_broadcastscalartovector512_v128u16(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.BroadcastScalarToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_broadcastscalartovector512_v128u8(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.BroadcastScalarToVector512");
    return dn2cpp_isa_vec<64>(_mm512_broadcastb_epi8(dn2cpp_isa_bits<__m128i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_broadcastscalartovector512_v128u8(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.BroadcastScalarToVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_compareequal_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi16(_mm512_cmpeq_epi16_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_compareequal_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_compareequal_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi8(_mm512_cmpeq_epi8_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_compareequal_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_compareequal_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi16(_mm512_cmpeq_epu16_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_compareequal_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_compareequal_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi8(_mm512_cmpeq_epu8_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_compareequal_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparegreaterthan_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareGreaterThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi16(_mm512_cmpgt_epi16_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparegreaterthan_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparegreaterthan_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareGreaterThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi8(_mm512_cmpgt_epi8_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparegreaterthan_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparegreaterthan_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareGreaterThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi16(_mm512_cmpgt_epu16_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparegreaterthan_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparegreaterthan_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareGreaterThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi8(_mm512_cmpgt_epu8_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparegreaterthan_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparegreaterthanorequal_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi16(_mm512_cmpge_epi16_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparegreaterthanorequal_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparegreaterthanorequal_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi8(_mm512_cmpge_epi8_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparegreaterthanorequal_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparegreaterthanorequal_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi16(_mm512_cmpge_epu16_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparegreaterthanorequal_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparegreaterthanorequal_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi8(_mm512_cmpge_epu8_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparegreaterthanorequal_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparelessthan_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareLessThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi16(_mm512_cmplt_epi16_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparelessthan_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparelessthan_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareLessThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi8(_mm512_cmplt_epi8_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparelessthan_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparelessthan_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareLessThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi16(_mm512_cmplt_epu16_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparelessthan_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparelessthan_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareLessThan");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi8(_mm512_cmplt_epu8_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparelessthan_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparelessthanorequal_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi16(_mm512_cmple_epi16_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparelessthanorequal_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparelessthanorequal_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi8(_mm512_cmple_epi8_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparelessthanorequal_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparelessthanorequal_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi16(_mm512_cmple_epu16_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparelessthanorequal_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparelessthanorequal_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi8(_mm512_cmple_epu8_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparelessthanorequal_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparenotequal_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareNotEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi16(_mm512_cmpneq_epi16_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparenotequal_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparenotequal_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareNotEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi8(_mm512_cmpneq_epi8_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparenotequal_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparenotequal_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareNotEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi16(_mm512_cmpneq_epu16_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparenotequal_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparenotequal_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.CompareNotEqual");
    return dn2cpp_isa_vec<64>(_mm512_movm_epi8(_mm512_cmpneq_epu8_mask(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_comparenotequal_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_converttovector256byte_v512i16(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector256Byte");
    return dn2cpp_isa_vec<32>(_mm512_cvtepi16_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_converttovector256byte_v512i16(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector256Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_converttovector256byte_v512u16(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector256Byte");
    return dn2cpp_isa_vec<32>(_mm512_cvtepi16_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_converttovector256byte_v512u16(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector256Byte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_converttovector256bytewithsaturation_v512u16(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector256ByteWithSaturation");
    return dn2cpp_isa_vec<32>(_mm512_cvtusepi16_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_converttovector256bytewithsaturation_v512u16(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector256ByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_converttovector256sbyte_v512i16(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector256SByte");
    return dn2cpp_isa_vec<32>(_mm512_cvtepi16_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_converttovector256sbyte_v512i16(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector256SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_converttovector256sbyte_v512u16(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector256SByte");
    return dn2cpp_isa_vec<32>(_mm512_cvtepi16_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_converttovector256sbyte_v512u16(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector256SByte");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_converttovector256sbytewithsaturation_v512i16(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector256SByteWithSaturation");
    return dn2cpp_isa_vec<32>(_mm512_cvtsepi16_epi8(dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector256 dn2cpp_isa_x86_avx512bw_converttovector256sbytewithsaturation_v512i16(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector256SByteWithSaturation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_converttovector512int16_v256i8(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector512Int16");
    return dn2cpp_isa_vec<64>(_mm512_cvtepi8_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_converttovector512int16_v256i8(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector512Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_converttovector512int16_v256u8(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector512Int16");
    return dn2cpp_isa_vec<64>(_mm512_cvtepu8_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_converttovector512int16_v256u8(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector512Int16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_converttovector512uint16_v256i8(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector512UInt16");
    return dn2cpp_isa_vec<64>(_mm512_cvtepi8_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_converttovector512uint16_v256i8(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector512UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_converttovector512uint16_v256u8(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector512UInt16");
    return dn2cpp_isa_vec<64>(_mm512_cvtepu8_epi16(dn2cpp_isa_bits<__m256i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_converttovector512uint16_v256u8(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ConvertToVector512UInt16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_loadvector512_pi16(int16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.LoadVector512");
    return dn2cpp_isa_vec<64>(_mm512_loadu_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_loadvector512_pi16(int16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.LoadVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_loadvector512_pi8(int8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.LoadVector512");
    return dn2cpp_isa_vec<64>(_mm512_loadu_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_loadvector512_pi8(int8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.LoadVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_loadvector512_pu16(uint16_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.LoadVector512");
    return dn2cpp_isa_vec<64>(_mm512_loadu_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_loadvector512_pu16(uint16_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.LoadVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_loadvector512_pu8(uint8_t* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.LoadVector512");
    return dn2cpp_isa_vec<64>(_mm512_loadu_si512((const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_loadvector512_pu8(uint8_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.LoadVector512");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_maskload_pi16_v512i16_v512i16(int16_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MaskLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_loadu_epi16(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_maskload_pi16_v512i16_v512i16(int16_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_maskload_pi8_v512i8_v512i8(int8_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MaskLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_loadu_epi8(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_maskload_pi8_v512i8_v512i8(int8_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_maskload_pu16_v512u16_v512u16(uint16_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MaskLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_loadu_epi16(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_maskload_pu16_v512u16_v512u16(uint16_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_maskload_pu8_v512u8_v512u8(uint8_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MaskLoad");
    return dn2cpp_isa_vec<64>(_mm512_mask_loadu_epi8(dn2cpp_isa_bits<__m512i>(a2), _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), (const void*)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_maskload_pu8_v512u8_v512u8(uint8_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MaskLoad");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_maskstore_pi16_v512i16_v512i16(int16_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MaskStore");
    _mm512_mask_storeu_epi16((void*)a0, _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_maskstore_pi16_v512i16_v512i16(int16_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_maskstore_pi8_v512i8_v512i8(int8_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MaskStore");
    _mm512_mask_storeu_epi8((void*)a0, _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_maskstore_pi8_v512i8_v512i8(int8_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_maskstore_pu16_v512u16_v512u16(uint16_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MaskStore");
    _mm512_mask_storeu_epi16((void*)a0, _mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_maskstore_pu16_v512u16_v512u16(uint16_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_maskstore_pu8_v512u8_v512u8(uint8_t* a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MaskStore");
    _mm512_mask_storeu_epi8((void*)a0, _mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a1)), dn2cpp_isa_bits<__m512i>(a2));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_maskstore_pu8_v512u8_v512u8(uint8_t*, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MaskStore");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_max_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Max");
    return dn2cpp_isa_vec<64>(_mm512_max_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_max_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_max_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Max");
    return dn2cpp_isa_vec<64>(_mm512_max_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_max_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_max_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Max");
    return dn2cpp_isa_vec<64>(_mm512_max_epu16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_max_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_max_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Max");
    return dn2cpp_isa_vec<64>(_mm512_max_epu8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_max_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_min_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Min");
    return dn2cpp_isa_vec<64>(_mm512_min_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_min_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_min_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Min");
    return dn2cpp_isa_vec<64>(_mm512_min_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_min_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_min_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Min");
    return dn2cpp_isa_vec<64>(_mm512_min_epu16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_min_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_min_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Min");
    return dn2cpp_isa_vec<64>(_mm512_min_epu8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_min_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512bw_movemask_v256i8(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MoveMask");
    return (int32_t)_mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512bw_movemask_v256i8(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512bw_movemask_v256u8(const Dn2CppVector256& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MoveMask");
    return (int32_t)_mm256_movepi8_mask(dn2cpp_isa_bits<__m256i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512bw_movemask_v256u8(const Dn2CppVector256&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512bw_movemask_v512i16(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MoveMask");
    return (int32_t)_mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512bw_movemask_v512i16(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_avx512bw_movemask_v512i8(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MoveMask");
    return (int64_t)_mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_avx512bw_movemask_v512i8(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512bw_movemask_v512u16(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MoveMask");
    return (int32_t)_mm512_movepi16_mask(dn2cpp_isa_bits<__m512i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_avx512bw_movemask_v512u16(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_avx512bw_movemask_v512u8(const Dn2CppVector512& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MoveMask");
    return (int64_t)_mm512_movepi8_mask(dn2cpp_isa_bits<__m512i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_avx512bw_movemask_v512u8(const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_multiplyaddadjacent_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MultiplyAddAdjacent");
    return dn2cpp_isa_vec<64>(_mm512_madd_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_multiplyaddadjacent_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MultiplyAddAdjacent");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_multiplyaddadjacent_v512u8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MultiplyAddAdjacent");
    return dn2cpp_isa_vec<64>(_mm512_maddubs_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_multiplyaddadjacent_v512u8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MultiplyAddAdjacent");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_multiplyhigh_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MultiplyHigh");
    return dn2cpp_isa_vec<64>(_mm512_mulhi_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_multiplyhigh_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MultiplyHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_multiplyhigh_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MultiplyHigh");
    return dn2cpp_isa_vec<64>(_mm512_mulhi_epu16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_multiplyhigh_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MultiplyHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_multiplyhighroundscale_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MultiplyHighRoundScale");
    return dn2cpp_isa_vec<64>(_mm512_mulhrs_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_multiplyhighroundscale_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MultiplyHighRoundScale");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_multiplylow_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MultiplyLow");
    return dn2cpp_isa_vec<64>(_mm512_mullo_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_multiplylow_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_multiplylow_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.MultiplyLow");
    return dn2cpp_isa_vec<64>(_mm512_mullo_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_multiplylow_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.MultiplyLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_packsignedsaturate_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.PackSignedSaturate");
    return dn2cpp_isa_vec<64>(_mm512_packs_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_packsignedsaturate_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.PackSignedSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_packsignedsaturate_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.PackSignedSaturate");
    return dn2cpp_isa_vec<64>(_mm512_packs_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_packsignedsaturate_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.PackSignedSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_packunsignedsaturate_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.PackUnsignedSaturate");
    return dn2cpp_isa_vec<64>(_mm512_packus_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_packunsignedsaturate_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.PackUnsignedSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_packunsignedsaturate_v512i32_v512i32(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.PackUnsignedSaturate");
    return dn2cpp_isa_vec<64>(_mm512_packus_epi32(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_packunsignedsaturate_v512i32_v512i32(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.PackUnsignedSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_permutevar32x16_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.PermuteVar32x16");
    return dn2cpp_isa_vec<64>(_mm512_permutexvar_epi16(dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_permutevar32x16_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.PermuteVar32x16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_permutevar32x16_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.PermuteVar32x16");
    return dn2cpp_isa_vec<64>(_mm512_permutexvar_epi16(dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_permutevar32x16_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.PermuteVar32x16");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_permutevar32x16x2_v512i16_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.PermuteVar32x16x2");
    return dn2cpp_isa_vec<64>(_mm512_permutex2var_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_permutevar32x16x2_v512i16_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.PermuteVar32x16x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_permutevar32x16x2_v512u16_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1, const Dn2CppVector512& a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.PermuteVar32x16x2");
    return dn2cpp_isa_vec<64>(_mm512_permutex2var_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), dn2cpp_isa_bits<__m512i>(a2)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_permutevar32x16x2_v512u16_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.PermuteVar32x16x2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftleftlogical128bitlane_v512i8_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftLeftLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_bslli_epi128(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftleftlogical128bitlane_v512i8_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftLeftLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftleftlogical128bitlane_v512u8_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftLeftLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_bslli_epi128(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftleftlogical128bitlane_v512u8_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftLeftLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftleftlogical_v512i16_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftLeftLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_slli_epi16(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftleftlogical_v512i16_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftleftlogical_v512i16_v128i16(const Dn2CppVector512& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftLeftLogical");
    return dn2cpp_isa_vec<64>(_mm512_sll_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftleftlogical_v512i16_v128i16(const Dn2CppVector512&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftleftlogical_v512u16_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftLeftLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_slli_epi16(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftleftlogical_v512u16_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftleftlogical_v512u16_v128u16(const Dn2CppVector512& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftLeftLogical");
    return dn2cpp_isa_vec<64>(_mm512_sll_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftleftlogical_v512u16_v128u16(const Dn2CppVector512&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftLeftLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftleftlogicalvariable_v512i16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<64>(_mm512_sllv_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftleftlogicalvariable_v512i16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftleftlogicalvariable_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftLeftLogicalVariable");
    return dn2cpp_isa_vec<64>(_mm512_sllv_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftleftlogicalvariable_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftLeftLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightarithmetic_v512i16_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightArithmetic");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_srai_epi16(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightarithmetic_v512i16_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightarithmetic_v512i16_v128i16(const Dn2CppVector512& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightArithmetic");
    return dn2cpp_isa_vec<64>(_mm512_sra_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightarithmetic_v512i16_v128i16(const Dn2CppVector512&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightArithmetic");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightarithmeticvariable_v512i16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightArithmeticVariable");
    return dn2cpp_isa_vec<64>(_mm512_srav_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightarithmeticvariable_v512i16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightArithmeticVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightlogical128bitlane_v512i8_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_bsrli_epi128(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightlogical128bitlane_v512i8_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightlogical128bitlane_v512u8_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightLogical128BitLane");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_bsrli_epi128(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightlogical128bitlane_v512u8_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightLogical128BitLane");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightlogical_v512i16_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_srli_epi16(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightlogical_v512i16_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightlogical_v512i16_v128i16(const Dn2CppVector512& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightLogical");
    return dn2cpp_isa_vec<64>(_mm512_srl_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightlogical_v512i16_v128i16(const Dn2CppVector512&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightlogical_v512u16_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightLogical");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_srli_epi16(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightlogical_v512u16_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightlogical_v512u16_v128u16(const Dn2CppVector512& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightLogical");
    return dn2cpp_isa_vec<64>(_mm512_srl_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m128i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightlogical_v512u16_v128u16(const Dn2CppVector512&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightLogical");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightlogicalvariable_v512i16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<64>(_mm512_srlv_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightlogicalvariable_v512i16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightlogicalvariable_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightLogicalVariable");
    return dn2cpp_isa_vec<64>(_mm512_srlv_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shiftrightlogicalvariable_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShiftRightLogicalVariable");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shuffle_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Shuffle");
    return dn2cpp_isa_vec<64>(_mm512_shuffle_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shuffle_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Shuffle");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shuffle_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Shuffle");
    return dn2cpp_isa_vec<64>(_mm512_shuffle_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shuffle_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Shuffle");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shufflehigh_v512i16_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShuffleHigh");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_shufflehi_epi16(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shufflehigh_v512i16_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShuffleHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shufflehigh_v512u16_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShuffleHigh");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_shufflehi_epi16(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shufflehigh_v512u16_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShuffleHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shufflelow_v512i16_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShuffleLow");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_shufflelo_epi16(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shufflelow_v512i16_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShuffleLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shufflelow_v512u16_u8(const Dn2CppVector512& a0, uint8_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.ShuffleLow");
    DN2CPP_ISA_IMM8_SWITCH(a1, dn2cpp_isa_vec<64>(_mm512_shufflelo_epi16(dn2cpp_isa_bits<__m512i>(a0), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_shufflelow_v512u16_u8(const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.ShuffleLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_store_pi16_v512i16(int16_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Store");
    _mm512_storeu_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_store_pi16_v512i16(int16_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_store_pi8_v512i8(int8_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Store");
    _mm512_storeu_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_store_pi8_v512i8(int8_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_store_pu16_v512u16(uint16_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Store");
    _mm512_storeu_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_store_pu16_v512u16(uint16_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_store_pu8_v512u8(uint8_t* a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Store");
    _mm512_storeu_si512((void*)a0, dn2cpp_isa_bits<__m512i>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_avx512bw_store_pu8_v512u8(uint8_t*, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_subtract_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Subtract");
    return dn2cpp_isa_vec<64>(_mm512_sub_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_subtract_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_subtract_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Subtract");
    return dn2cpp_isa_vec<64>(_mm512_sub_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_subtract_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_subtract_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Subtract");
    return dn2cpp_isa_vec<64>(_mm512_sub_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_subtract_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_subtract_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.Subtract");
    return dn2cpp_isa_vec<64>(_mm512_sub_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_subtract_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_subtractsaturate_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.SubtractSaturate");
    return dn2cpp_isa_vec<64>(_mm512_subs_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_subtractsaturate_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_subtractsaturate_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.SubtractSaturate");
    return dn2cpp_isa_vec<64>(_mm512_subs_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_subtractsaturate_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_subtractsaturate_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.SubtractSaturate");
    return dn2cpp_isa_vec<64>(_mm512_subs_epu16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_subtractsaturate_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_subtractsaturate_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.SubtractSaturate");
    return dn2cpp_isa_vec<64>(_mm512_subs_epu8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_subtractsaturate_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.SubtractSaturate");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_sumabsolutedifferences_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.SumAbsoluteDifferences");
    return dn2cpp_isa_vec<64>(_mm512_sad_epu8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_sumabsolutedifferences_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.SumAbsoluteDifferences");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_sumabsolutedifferencesinblock32_v512u8_v512u8_u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.SumAbsoluteDifferencesInBlock32");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<64>(_mm512_dbsad_epu8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_sumabsolutedifferencesinblock32_v512u8_v512u8_u8(const Dn2CppVector512&, const Dn2CppVector512&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.SumAbsoluteDifferencesInBlock32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_unpackhigh_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.UnpackHigh");
    return dn2cpp_isa_vec<64>(_mm512_unpackhi_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_unpackhigh_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_unpackhigh_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.UnpackHigh");
    return dn2cpp_isa_vec<64>(_mm512_unpackhi_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_unpackhigh_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_unpackhigh_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.UnpackHigh");
    return dn2cpp_isa_vec<64>(_mm512_unpackhi_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_unpackhigh_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_unpackhigh_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.UnpackHigh");
    return dn2cpp_isa_vec<64>(_mm512_unpackhi_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_unpackhigh_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_unpacklow_v512i16_v512i16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.UnpackLow");
    return dn2cpp_isa_vec<64>(_mm512_unpacklo_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_unpacklow_v512i16_v512i16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_unpacklow_v512i8_v512i8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.UnpackLow");
    return dn2cpp_isa_vec<64>(_mm512_unpacklo_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_unpacklow_v512i8_v512i8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_unpacklow_v512u16_v512u16(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.UnpackLow");
    return dn2cpp_isa_vec<64>(_mm512_unpacklo_epi16(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_unpacklow_v512u16_v512u16(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("avx512f,avx512bw,avx512dq,avx512vl") DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_unpacklow_v512u8_v512u8(const Dn2CppVector512& a0, const Dn2CppVector512& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Avx512BW, "System.Runtime.Intrinsics.X86.Avx512BW.UnpackLow");
    return dn2cpp_isa_vec<64>(_mm512_unpacklo_epi8(dn2cpp_isa_bits<__m512i>(a0), dn2cpp_isa_bits<__m512i>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector512 dn2cpp_isa_x86_avx512bw_unpacklow_v512u8_v512u8(const Dn2CppVector512&, const Dn2CppVector512&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Avx512BW.UnpackLow");
}
#endif
