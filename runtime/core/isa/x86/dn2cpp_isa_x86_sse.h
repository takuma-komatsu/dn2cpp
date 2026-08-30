#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Sse: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_add_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.Add");
    return dn2cpp_isa_vec<16>(_mm_add_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_add_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.Add");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_addscalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.AddScalar");
    return dn2cpp_isa_vec<16>(_mm_add_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_addscalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.AddScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_and_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.And");
    return dn2cpp_isa_vec<16>(_mm_and_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_and_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.And");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_andnot_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.AndNot");
    return dn2cpp_isa_vec<16>(_mm_andnot_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_andnot_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.AndNot");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_compareequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpeq_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_compareequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparegreaterthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_cmpgt_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparegreaterthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparegreaterthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpge_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparegreaterthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparelessthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareLessThan");
    return dn2cpp_isa_vec<16>(_mm_cmplt_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparelessthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparelessthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_cmple_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparelessthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparenotequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareNotEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpneq_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparenotequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparenotgreaterthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareNotGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_cmpngt_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparenotgreaterthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareNotGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparenotgreaterthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareNotGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpnge_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparenotgreaterthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareNotGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparenotlessthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareNotLessThan");
    return dn2cpp_isa_vec<16>(_mm_cmpnlt_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparenotlessthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareNotLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparenotlessthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareNotLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpnle_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparenotlessthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareNotLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_compareordered_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareOrdered");
    return dn2cpp_isa_vec<16>(_mm_cmpord_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_compareordered_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareOrdered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpeq_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalargreaterthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_cmpgt_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalargreaterthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalargreaterthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpge_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalargreaterthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarlessthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarLessThan");
    return dn2cpp_isa_vec<16>(_mm_cmplt_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarlessthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarlessthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_cmple_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarlessthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarnotequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarNotEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpneq_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarnotequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarnotgreaterthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarNotGreaterThan");
    return dn2cpp_isa_vec<16>(_mm_cmpngt_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarnotgreaterthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarNotGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarnotgreaterthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarNotGreaterThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpnge_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarnotgreaterthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarNotGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarnotlessthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarNotLessThan");
    return dn2cpp_isa_vec<16>(_mm_cmpnlt_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarnotlessthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarNotLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarnotlessthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarNotLessThanOrEqual");
    return dn2cpp_isa_vec<16>(_mm_cmpnle_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarnotlessthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarNotLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarordered_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarOrdered");
    return dn2cpp_isa_vec<16>(_mm_cmpord_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarordered_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarOrdered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarorderedequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarOrderedEqual");
    return _mm_comieq_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarorderedequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarOrderedEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarorderedgreaterthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarOrderedGreaterThan");
    return _mm_comigt_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarorderedgreaterthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarOrderedGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarorderedgreaterthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarOrderedGreaterThanOrEqual");
    return _mm_comige_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarorderedgreaterthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarOrderedGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarorderedlessthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarOrderedLessThan");
    return _mm_comilt_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarorderedlessthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarOrderedLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarorderedlessthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarOrderedLessThanOrEqual");
    return _mm_comile_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarorderedlessthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarOrderedLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarorderednotequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarOrderedNotEqual");
    return _mm_comineq_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarorderednotequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarOrderedNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarunordered_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarUnordered");
    return dn2cpp_isa_vec<16>(_mm_cmpunord_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_comparescalarunordered_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarUnordered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarunorderedequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarUnorderedEqual");
    return _mm_ucomieq_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarunorderedequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarUnorderedEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarunorderedgreaterthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarUnorderedGreaterThan");
    return _mm_ucomigt_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarunorderedgreaterthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarUnorderedGreaterThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarunorderedgreaterthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarUnorderedGreaterThanOrEqual");
    return _mm_ucomige_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarunorderedgreaterthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarUnorderedGreaterThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarunorderedlessthan_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarUnorderedLessThan");
    return _mm_ucomilt_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarunorderedlessthan_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarUnorderedLessThan");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarunorderedlessthanorequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarUnorderedLessThanOrEqual");
    return _mm_ucomile_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarunorderedlessthanorequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarUnorderedLessThanOrEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarunorderednotequal_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareScalarUnorderedNotEqual");
    return _mm_ucomineq_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)) != 0;
}
#else
[[noreturn]] DN2CPP_ISA_INLINE bool dn2cpp_isa_x86_sse_comparescalarunorderednotequal_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareScalarUnorderedNotEqual");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_compareunordered_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.CompareUnordered");
    return dn2cpp_isa_vec<16>(_mm_cmpunord_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_compareunordered_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.CompareUnordered");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_convertscalartovector128single_v128f32_i32(const Dn2CppVector128& a0, int32_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.ConvertScalarToVector128Single");
    return dn2cpp_isa_vec<16>(_mm_cvtsi32_ss(dn2cpp_isa_bits<__m128>(a0), a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_convertscalartovector128single_v128f32_i32(const Dn2CppVector128&, int32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.ConvertScalarToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse_converttoint32_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.ConvertToInt32");
    return _mm_cvtss_si32(dn2cpp_isa_bits<__m128>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse_converttoint32_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.ConvertToInt32");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse_converttoint32withtruncation_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.ConvertToInt32WithTruncation");
    return _mm_cvttss_si32(dn2cpp_isa_bits<__m128>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse_converttoint32withtruncation_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.ConvertToInt32WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_divide_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.Divide");
    return dn2cpp_isa_vec<16>(_mm_div_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_divide_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.Divide");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_dividescalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.DivideScalar");
    return dn2cpp_isa_vec<16>(_mm_div_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_dividescalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.DivideScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_loadalignedvector128_pf32(float* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.LoadAlignedVector128");
    return dn2cpp_isa_vec<16>(_mm_load_ps(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_loadalignedvector128_pf32(float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.LoadAlignedVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_loadhigh_v128f32_pf32(const Dn2CppVector128& a0, float* a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.LoadHigh");
    return dn2cpp_isa_vec<16>(_mm_loadh_pi(dn2cpp_isa_bits<__m128>(a0), (const __m64*)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_loadhigh_v128f32_pf32(const Dn2CppVector128&, float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.LoadHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_loadlow_v128f32_pf32(const Dn2CppVector128& a0, float* a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.LoadLow");
    return dn2cpp_isa_vec<16>(_mm_loadl_pi(dn2cpp_isa_bits<__m128>(a0), (const __m64*)a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_loadlow_v128f32_pf32(const Dn2CppVector128&, float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.LoadLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_loadscalarvector128_pf32(float* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.LoadScalarVector128");
    return dn2cpp_isa_vec<16>(_mm_load_ss(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_loadscalarvector128_pf32(float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.LoadScalarVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_loadvector128_pf32(float* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.LoadVector128");
    return dn2cpp_isa_vec<16>(_mm_loadu_ps(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_loadvector128_pf32(float*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.LoadVector128");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_max_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.Max");
    return dn2cpp_isa_vec<16>(_mm_max_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_max_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.Max");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_maxscalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.MaxScalar");
    return dn2cpp_isa_vec<16>(_mm_max_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_maxscalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.MaxScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_min_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.Min");
    return dn2cpp_isa_vec<16>(_mm_min_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_min_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.Min");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_minscalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.MinScalar");
    return dn2cpp_isa_vec<16>(_mm_min_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_minscalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.MinScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_movehightolow_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.MoveHighToLow");
    return dn2cpp_isa_vec<16>(_mm_movehl_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_movehightolow_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.MoveHighToLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_movelowtohigh_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.MoveLowToHigh");
    return dn2cpp_isa_vec<16>(_mm_movelh_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_movelowtohigh_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.MoveLowToHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse_movemask_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.MoveMask");
    return _mm_movemask_ps(dn2cpp_isa_bits<__m128>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int32_t dn2cpp_isa_x86_sse_movemask_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.MoveMask");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_movescalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.MoveScalar");
    return dn2cpp_isa_vec<16>(_mm_move_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_movescalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.MoveScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_multiply_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.Multiply");
    return dn2cpp_isa_vec<16>(_mm_mul_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_multiply_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.Multiply");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_multiplyscalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.MultiplyScalar");
    return dn2cpp_isa_vec<16>(_mm_mul_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_multiplyscalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.MultiplyScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_or_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.Or");
    return dn2cpp_isa_vec<16>(_mm_or_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_or_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.Or");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_prefetch0_pvoid(void* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.Prefetch0");
    _mm_prefetch((const char*)a0, _MM_HINT_T0);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_prefetch0_pvoid(void*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.Prefetch0");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_prefetch1_pvoid(void* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.Prefetch1");
    _mm_prefetch((const char*)a0, _MM_HINT_T1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_prefetch1_pvoid(void*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.Prefetch1");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_prefetch2_pvoid(void* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.Prefetch2");
    _mm_prefetch((const char*)a0, _MM_HINT_T2);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_prefetch2_pvoid(void*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.Prefetch2");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_prefetchnontemporal_pvoid(void* a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.PrefetchNonTemporal");
    _mm_prefetch((const char*)a0, _MM_HINT_NTA);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_prefetchnontemporal_pvoid(void*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.PrefetchNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_reciprocal_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.Reciprocal");
    return dn2cpp_isa_vec<16>(_mm_rcp_ps(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_reciprocal_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.Reciprocal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_reciprocalscalar_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.ReciprocalScalar");
    return dn2cpp_isa_vec<16>(_mm_rcp_ss(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_reciprocalscalar_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.ReciprocalScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_reciprocalscalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.ReciprocalScalar");
    return dn2cpp_isa_vec<16>(_mm_move_ss(dn2cpp_isa_bits<__m128>(a0), _mm_rcp_ss(dn2cpp_isa_bits<__m128>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_reciprocalscalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.ReciprocalScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_reciprocalsqrt_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.ReciprocalSqrt");
    return dn2cpp_isa_vec<16>(_mm_rsqrt_ps(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_reciprocalsqrt_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.ReciprocalSqrt");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_reciprocalsqrtscalar_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.ReciprocalSqrtScalar");
    return dn2cpp_isa_vec<16>(_mm_rsqrt_ss(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_reciprocalsqrtscalar_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.ReciprocalSqrtScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_reciprocalsqrtscalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.ReciprocalSqrtScalar");
    return dn2cpp_isa_vec<16>(_mm_move_ss(dn2cpp_isa_bits<__m128>(a0), _mm_rsqrt_ss(dn2cpp_isa_bits<__m128>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_reciprocalsqrtscalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.ReciprocalSqrtScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_shuffle_v128f32_v128f32_u8(const Dn2CppVector128& a0, const Dn2CppVector128& a1, uint8_t a2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.Shuffle");
    DN2CPP_ISA_IMM8_SWITCH(a2, dn2cpp_isa_vec<16>(_mm_shuffle_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1), DN2CPP_IMM)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_shuffle_v128f32_v128f32_u8(const Dn2CppVector128&, const Dn2CppVector128&, uint8_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.Shuffle");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_sqrt_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.Sqrt");
    return dn2cpp_isa_vec<16>(_mm_sqrt_ps(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_sqrt_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.Sqrt");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_sqrtscalar_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.SqrtScalar");
    return dn2cpp_isa_vec<16>(_mm_sqrt_ss(dn2cpp_isa_bits<__m128>(a0)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_sqrtscalar_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.SqrtScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_sqrtscalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.SqrtScalar");
    return dn2cpp_isa_vec<16>(_mm_move_ss(dn2cpp_isa_bits<__m128>(a0), _mm_sqrt_ss(dn2cpp_isa_bits<__m128>(a1))));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_sqrtscalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.SqrtScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_store_pf32_v128f32(float* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.Store");
    _mm_storeu_ps(a0, dn2cpp_isa_bits<__m128>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_store_pf32_v128f32(float*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.Store");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_storealigned_pf32_v128f32(float* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.StoreAligned");
    _mm_store_ps(a0, dn2cpp_isa_bits<__m128>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_storealigned_pf32_v128f32(float*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.StoreAligned");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_storealignednontemporal_pf32_v128f32(float* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.StoreAlignedNonTemporal");
    _mm_stream_ps(a0, dn2cpp_isa_bits<__m128>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_storealignednontemporal_pf32_v128f32(float*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.StoreAlignedNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_storefence()
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.StoreFence");
    _mm_sfence();
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_storefence()
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.StoreFence");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_storehigh_pf32_v128f32(float* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.StoreHigh");
    _mm_storeh_pi((__m64*)a0, dn2cpp_isa_bits<__m128>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_storehigh_pf32_v128f32(float*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.StoreHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_storelow_pf32_v128f32(float* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.StoreLow");
    _mm_storel_pi((__m64*)a0, dn2cpp_isa_bits<__m128>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_storelow_pf32_v128f32(float*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.StoreLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_storescalar_pf32_v128f32(float* a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.StoreScalar");
    _mm_store_ss(a0, dn2cpp_isa_bits<__m128>(a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse_storescalar_pf32_v128f32(float*, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.StoreScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_subtract_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.Subtract");
    return dn2cpp_isa_vec<16>(_mm_sub_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_subtract_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.Subtract");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_subtractscalar_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.SubtractScalar");
    return dn2cpp_isa_vec<16>(_mm_sub_ss(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_subtractscalar_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.SubtractScalar");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_unpackhigh_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.UnpackHigh");
    return dn2cpp_isa_vec<16>(_mm_unpackhi_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_unpackhigh_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.UnpackHigh");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_unpacklow_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.UnpackLow");
    return dn2cpp_isa_vec<16>(_mm_unpacklo_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_unpacklow_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.UnpackLow");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_xor_v128f32_v128f32(const Dn2CppVector128& a0, const Dn2CppVector128& a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse, "System.Runtime.Intrinsics.X86.Sse.Xor");
    return dn2cpp_isa_vec<16>(_mm_xor_ps(dn2cpp_isa_bits<__m128>(a0), dn2cpp_isa_bits<__m128>(a1)));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_xor_v128f32_v128f32(const Dn2CppVector128&, const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse.Xor");
}
#endif
