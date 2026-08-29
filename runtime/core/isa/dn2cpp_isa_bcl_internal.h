#pragma once
// CoreLib calls a small set of ISA entry points that are deliberately absent
// from the public API surface the generator reads. They still pass through the
// platform-ISA intercept, so emitted BCL bodies need matching helpers on every
// target even though only x86-64 can execute them.

#include "dn2cpp_isa_common.h"

#if DN2CPP_TARGET_X64
// BSF/BSR leave their result undefined for zero. CoreLib preserves that
// contract on these internal methods and guards or adjusts zero at each caller.
DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_x86base_bitscanforward_u32(uint32_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_X86Base,
        "System.Runtime.Intrinsics.X86.X86Base.BitScanForward");
    return (uint32_t)__builtin_ctz(a0);
}

DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_x86base_bitscanreverse_u32(uint32_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_X86Base,
        "System.Runtime.Intrinsics.X86.X86Base.BitScanReverse");
    return 31u - (uint32_t)__builtin_clz(a0);
}

DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_x86base_x64_bitscanforward_u64(uint64_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_X86Base_X64,
        "System.Runtime.Intrinsics.X86.X86Base+X64.BitScanForward");
    return (uint64_t)__builtin_ctzll(a0);
}

DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_x86base_x64_bitscanreverse_u64(uint64_t a0)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_X86Base_X64,
        "System.Runtime.Intrinsics.X86.X86Base+X64.BitScanReverse");
    return 63u - (uint64_t)__builtin_clzll(a0);
}

DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_x64_bigmul_u64_u64(
    uint64_t a0, uint64_t a1, uint64_t* item1, uint64_t* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_X86Base_X64,
        "System.Runtime.Intrinsics.X86.X86Base+X64.BigMul");
    *item1 = a0 * a1;
    *item2 = dn2cpp_isa_umulh64(a0, a1);
}

DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_x64_bigmul_i64_i64(
    int64_t a0, int64_t a1, int64_t* item1, int64_t* item2)
{
    dn2cpp_isa_require(DN2CPP_ISA_X86_X86Base_X64,
        "System.Runtime.Intrinsics.X86.X86Base+X64.BigMul");
    *item1 = (int64_t)((uint64_t)a0 * (uint64_t)a1);
    *item2 = dn2cpp_isa_smulh64(a0, a1);
}
#else
[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_x86base_bitscanforward_u32(uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.X86Base.BitScanForward");
}

[[noreturn]] DN2CPP_ISA_INLINE uint32_t dn2cpp_isa_x86_x86base_bitscanreverse_u32(uint32_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.X86Base.BitScanReverse");
}

[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_x86base_x64_bitscanforward_u64(uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.X86Base+X64.BitScanForward");
}

[[noreturn]] DN2CPP_ISA_INLINE uint64_t dn2cpp_isa_x86_x86base_x64_bitscanreverse_u64(uint64_t)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.X86Base+X64.BitScanReverse");
}

[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_x64_bigmul_u64_u64(
    uint64_t, uint64_t, uint64_t*, uint64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.X86Base+X64.BigMul");
}

[[noreturn]] DN2CPP_ISA_INLINE void dn2cpp_isa_x86_x86base_x64_bigmul_i64_i64(
    int64_t, int64_t, int64_t*, int64_t*)
{
    dn2cpp_isa_not_lowered("System.Runtime.Intrinsics.X86.X86Base+X64.BigMul");
}
#endif
