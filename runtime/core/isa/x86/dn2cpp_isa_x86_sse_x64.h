#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Sse+X64: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_x64_convertscalartovector128single_v128f32_i64(const Dn2CppVector128& a0, int64_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse_X64, "System.Runtime.Intrinsics.X86.Sse+X64.ConvertScalarToVector128Single");
    return dn2cpp_isa_vec<16>(_mm_cvtsi64_ss(dn2cpp_isa_bits<__m128>(a0), a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse_x64_convertscalartovector128single_v128f32_i64(const Dn2CppVector128&, int64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse+X64.ConvertScalarToVector128Single");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_sse_x64_converttoint64_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse_X64, "System.Runtime.Intrinsics.X86.Sse+X64.ConvertToInt64");
    return _mm_cvtss_si64(dn2cpp_isa_bits<__m128>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_sse_x64_converttoint64_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse+X64.ConvertToInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse") DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_sse_x64_converttoint64withtruncation_v128f32(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse_X64, "System.Runtime.Intrinsics.X86.Sse+X64.ConvertToInt64WithTruncation");
    return _mm_cvttss_si64(dn2cpp_isa_bits<__m128>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_sse_x64_converttoint64withtruncation_v128f32(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse+X64.ConvertToInt64WithTruncation");
}
#endif
