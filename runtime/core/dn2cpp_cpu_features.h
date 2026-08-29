#pragma once
// dn2cpp_cpu_features.h — the runtime's answer to every
// System.Runtime.Intrinsics.X86/.Arm/.Wasm `IsSupported` getter.
//
// One bit per ISA family, resolved once from the CPU (and OS) plus an optional
// mask (DN2CPP_CPU_FEATURES, see dn2cpp_cpu_features.cpp). The generated code
// tests these bits through the DN2CPP_ISA_* tokens included at the end, so this
// header is part of every generated TU and deliberately pulls in no intrinsic
// header.

#include <stdint.h>
#include <atomic>

// Exactly one of the four target macros is 1.
#if defined(__x86_64__) || defined(_M_X64)
#define DN2CPP_TARGET_X64 1
#else
#define DN2CPP_TARGET_X64 0
#endif
#if defined(__aarch64__) || defined(_M_ARM64)
#define DN2CPP_TARGET_ARM64 1
#else
#define DN2CPP_TARGET_ARM64 0
#endif
#if defined(__wasm32__)
#define DN2CPP_TARGET_WASM32 1
#else
#define DN2CPP_TARGET_WASM32 0
#endif
#if DN2CPP_TARGET_X64 || DN2CPP_TARGET_ARM64 || DN2CPP_TARGET_WASM32
#define DN2CPP_TARGET_OTHER 0
#else
#define DN2CPP_TARGET_OTHER 1
#endif

// X(ID, "Name", ARCH, parentsMask). Name is the .NET type name and the token
// accepted by the DN2CPP_CPU_FEATURES mask. parentsMask lists the families a
// family implies: a child bit is only ever set when every parent bit is set,
// and masking a parent masks its children (as DOTNET_EnableAVX=0 does). The
// parents are .NET 10's instruction-set implications (InstructionSetDesc.txt:
// Lzcnt, Bmi1, Bmi2 and Fma belong to its AVX2 set, Popcnt to SSE42, Gfni
// implies SSE42, Avx10v1 implies the Vbmi2 set). The bits are one per public
// type, finer than .NET 10's JIT instruction sets, so detection sets the types
// .NET gates as one set only together (see dn2cpp_cpu_features.cpp); a wrong
// parent or a split group makes a getter true where .NET's is false, or vice
// versa — DOTNET_EnableAVX=0 turns Lzcnt off in .NET, so -Avx must here.
#define DN2CPP_CPU_FEATURE_TABLE(X) \
    X(X86_X86BASE,      "X86Base",      X86,  0) \
    X(X86_SSE,          "Sse",          X86,  DN2CPP_CPU_X86_X86BASE) \
    X(X86_SSE2,         "Sse2",         X86,  DN2CPP_CPU_X86_SSE) \
    X(X86_SSE3,         "Sse3",         X86,  DN2CPP_CPU_X86_SSE2) \
    X(X86_SSSE3,        "Ssse3",        X86,  DN2CPP_CPU_X86_SSE3) \
    X(X86_SSE41,        "Sse41",        X86,  DN2CPP_CPU_X86_SSSE3) \
    X(X86_SSE42,        "Sse42",        X86,  DN2CPP_CPU_X86_SSE41) \
    X(X86_POPCNT,       "Popcnt",       X86,  DN2CPP_CPU_X86_SSE42) \
    X(X86_AES,          "Aes",          X86,  DN2CPP_CPU_X86_SSE2) \
    X(X86_VAES,         "Vaes",         X86,  DN2CPP_CPU_X86_AES | DN2CPP_CPU_X86_AVX) \
    X(X86_PCLMULQDQ,    "Pclmulqdq",    X86,  DN2CPP_CPU_X86_SSE2) \
    X(X86_VPCLMULQDQ,   "Vpclmulqdq",   X86,  DN2CPP_CPU_X86_PCLMULQDQ | DN2CPP_CPU_X86_AVX) \
    X(X86_AVX,          "Avx",          X86,  DN2CPP_CPU_X86_SSE42) \
    X(X86_AVX2,         "Avx2",         X86,  DN2CPP_CPU_X86_AVX) \
    X(X86_FMA,          "Fma",          X86,  DN2CPP_CPU_X86_AVX2) \
    X(X86_BMI1,         "Bmi1",         X86,  DN2CPP_CPU_X86_AVX2) \
    X(X86_BMI2,         "Bmi2",         X86,  DN2CPP_CPU_X86_AVX2) \
    X(X86_LZCNT,        "Lzcnt",        X86,  DN2CPP_CPU_X86_AVX2) \
    X(X86_AVXVNNI,      "AvxVnni",      X86,  DN2CPP_CPU_X86_AVX2) \
    X(X86_AVXVNNIINT8,  "AvxVnniInt8",  X86,  DN2CPP_CPU_X86_AVX2) \
    X(X86_AVXVNNIINT16, "AvxVnniInt16", X86,  DN2CPP_CPU_X86_AVX2) \
    X(X86_AVX512,       "Avx512F",      X86,  DN2CPP_CPU_X86_AVX2) \
    X(X86_AVX512VBMI,   "Avx512Vbmi",   X86,  DN2CPP_CPU_X86_AVX512) \
    X(X86_AVX512VBMI2,  "Avx512Vbmi2",  X86,  DN2CPP_CPU_X86_AVX512VBMI) \
    X(X86_GFNI,         "Gfni",         X86,  DN2CPP_CPU_X86_SSE42) \
    X(X86_AVX10V1,      "Avx10v1",      X86,  DN2CPP_CPU_X86_AVX512VBMI2) \
    X(X86_AVX10V1_512,  "Avx10v1_V512", X86,  DN2CPP_CPU_X86_AVX10V1) \
    X(X86_AVX10V2,      "Avx10v2",      X86,  DN2CPP_CPU_X86_AVX10V1) \
    X(X86_AVX10V2_512,  "Avx10v2_V512", X86,  DN2CPP_CPU_X86_AVX10V2 | DN2CPP_CPU_X86_AVX10V1_512) \
    X(X86_X86SERIALIZE, "X86Serialize", X86,  DN2CPP_CPU_X86_X86BASE) \
    X(ARM_ARMBASE,      "ArmBase",      ARM,  0) \
    X(ARM_ADVSIMD,      "AdvSimd",      ARM,  DN2CPP_CPU_ARM_ARMBASE) \
    X(ARM_AES,          "Aes",          ARM,  DN2CPP_CPU_ARM_ARMBASE) \
    X(ARM_CRC32,        "Crc32",        ARM,  DN2CPP_CPU_ARM_ARMBASE) \
    X(ARM_SHA1,         "Sha1",         ARM,  DN2CPP_CPU_ARM_ARMBASE) \
    X(ARM_SHA256,       "Sha256",       ARM,  DN2CPP_CPU_ARM_ARMBASE) \
    X(ARM_DP,           "Dp",           ARM,  DN2CPP_CPU_ARM_ADVSIMD) \
    X(ARM_RDM,          "Rdm",          ARM,  DN2CPP_CPU_ARM_ADVSIMD) \
    X(WASM_PACKEDSIMD,  "PackedSimd",   WASM, 0)

enum Dn2CppCpuFeatureBit : int {
#define DN2CPP_CPU_FEATURE_BIT(id, name, arch, parents) DN2CPP_CPU_BIT_##id,
    DN2CPP_CPU_FEATURE_TABLE(DN2CPP_CPU_FEATURE_BIT)
#undef DN2CPP_CPU_FEATURE_BIT
    DN2CPP_CPU_FEATURE_COUNT
};

// Bit 63 is never a feature: resolve() sets it so a computed answer is nonzero
// even when no feature is present, which is what makes the zero cache mean
// "not yet resolved".
enum : uint64_t {
#define DN2CPP_CPU_FEATURE_MASK(id, name, arch, parents) DN2CPP_CPU_##id = 1ull << DN2CPP_CPU_BIT_##id,
    DN2CPP_CPU_FEATURE_TABLE(DN2CPP_CPU_FEATURE_MASK)
#undef DN2CPP_CPU_FEATURE_MASK
    DN2CPP_CPU_RESOLVED = 1ull << 63
};

static_assert(DN2CPP_CPU_FEATURE_COUNT < 63, "feature bits must stay below DN2CPP_CPU_RESOLVED");

// Detect, apply the mask, publish into the cache and return the result (with
// DN2CPP_CPU_RESOLVED set); once resolved, return the cached word without
// recomputing, so the DN2CPP_CPU_FEATURES diagnostics print once per process
// whichever caller comes first. A computed value is a pure function of the
// CPU and the environment, so first callers racing on a cold cache are
// benign. dn2cpp_runtime_init calls it at startup.
uint64_t dn2cpp_cpu_features_resolve();

extern std::atomic<uint64_t> dn2cpp_cpu_features_cache;

static inline uint64_t dn2cpp_cpu_features()
{
    uint64_t v = dn2cpp_cpu_features_cache.load(std::memory_order_relaxed);
    if (v == 0)
        v = dn2cpp_cpu_features_resolve();
    return v;
}

static inline int32_t dn2cpp_cpu_has_all(uint64_t mask)
{
    return (dn2cpp_cpu_features() & mask) == mask;
}

#include "isa/dn2cpp_isa_tokens.g.h"
