#pragma once
// GENERATED FILE — do not edit by hand.
//
// Helpers for System.Runtime.Intrinsics.X86.Sse2+X64: one per public static method that has a map row.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
#include "../dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_x64_convertscalartovector128double_v128f64_i64(const Dn2CppVector128& a0, int64_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2_X64, "System.Runtime.Intrinsics.X86.Sse2+X64.ConvertScalarToVector128Double");
    return dn2cpp_isa_vec<16>(_mm_cvtsi64_sd(dn2cpp_isa_bits<__m128d>(a0), a1));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_x64_convertscalartovector128double_v128f64_i64(const Dn2CppVector128&, int64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2+X64.ConvertScalarToVector128Double");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_x64_convertscalartovector128int64_i64(int64_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2_X64, "System.Runtime.Intrinsics.X86.Sse2+X64.ConvertScalarToVector128Int64");
    return dn2cpp_isa_vec<16>(_mm_cvtsi64_si128(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_x64_convertscalartovector128int64_i64(int64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2+X64.ConvertScalarToVector128Int64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_x64_convertscalartovector128uint64_u64(uint64_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2_X64, "System.Runtime.Intrinsics.X86.Sse2+X64.ConvertScalarToVector128UInt64");
    return dn2cpp_isa_vec<16>(_mm_cvtsi64_si128((long long)a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE Dn2CppVector128 dn2cpp_isa_x86_sse2_x64_convertscalartovector128uint64_u64(uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2+X64.ConvertScalarToVector128UInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_sse2_x64_converttoint64_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2_X64, "System.Runtime.Intrinsics.X86.Sse2+X64.ConvertToInt64");
    return _mm_cvtsd_si64(dn2cpp_isa_bits<__m128d>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_sse2_x64_converttoint64_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2+X64.ConvertToInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_sse2_x64_converttoint64_v128i64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2_X64, "System.Runtime.Intrinsics.X86.Sse2+X64.ConvertToInt64");
    return _mm_cvtsi128_si64(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_sse2_x64_converttoint64_v128i64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2+X64.ConvertToInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_sse2_x64_converttoint64withtruncation_v128f64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2_X64, "System.Runtime.Intrinsics.X86.Sse2+X64.ConvertToInt64WithTruncation");
    return _mm_cvttsd_si64(dn2cpp_isa_bits<__m128d>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE int64_t dn2cpp_isa_x86_sse2_x64_converttoint64withtruncation_v128f64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2+X64.ConvertToInt64WithTruncation");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_sse2_x64_converttouint64_v128u64(const Dn2CppVector128& a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2_X64, "System.Runtime.Intrinsics.X86.Sse2+X64.ConvertToUInt64");
    return (uint64_t)_mm_cvtsi128_si64(dn2cpp_isa_bits<__m128i>(a0));
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_sse2_x64_converttouint64_v128u64(const Dn2CppVector128&)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2+X64.ConvertToUInt64");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_x64_storenontemporal_pi64_i64(int64_t* a0, int64_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2_X64, "System.Runtime.Intrinsics.X86.Sse2+X64.StoreNonTemporal");
    _mm_stream_si64((long long*)a0, (long long)a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_x64_storenontemporal_pi64_i64(int64_t*, int64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2+X64.StoreNonTemporal");
}
#endif

#if DN2CPP_TARGET_X64
DN2CPP_ISA_TARGET("sse2") DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_x64_storenontemporal_pu64_u64(uint64_t* a0, uint64_t a1)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_Sse2_X64, "System.Runtime.Intrinsics.X86.Sse2+X64.StoreNonTemporal");
    _mm_stream_si64((long long*)a0, (long long)a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_sse2_x64_storenontemporal_pu64_u64(uint64_t*, uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.Sse2+X64.StoreNonTemporal");
}
#endif
