// dn2cpp_cpu_features.cpp — CPU feature detection, positive ISA policy, and the
// DN2CPP_CPU_FEATURES narrowing mask behind every intrinsic IsSupported getter.
//
// The mask collects .NET's narrowing DOTNET_Enable<ISA> knobs in one variable:
// a family it excludes reports IsSupported == false together with every family
// that implies it. AVX10.2's positive opt-in is separate, and neither control
// can widen beyond what the hardware and OS provide.
#include "dn2cpp_cpu_features.h"
#include "platform/dn2cpp_pal.h"

#include <cctype>
#include <cstdio>
#include <cstring>

#if DN2CPP_TARGET_X64
#if defined(_MSC_VER) && !defined(__clang__)
#include <intrin.h>
#else
#include <cpuid.h>
#endif
#elif DN2CPP_TARGET_ARM64
#if defined(__APPLE__)
#include <sys/sysctl.h>
#elif defined(__linux__)
#include <sys/auxv.h>
#elif defined(_WIN32)
// NOMINMAX / WIN32_LEAN_AND_MEAN are project-wide compile definitions
// (runtime/CMakeLists.txt's MSVC arm), not repeated here.
#include <windows.h>
#endif
#endif

std::atomic<uint64_t> dn2cpp_cpu_features_cache{0};

namespace {

struct Dn2CppCpuFeatureInfo
{
    const char* name;
    uint64_t bit;
    uint64_t parents;
};

constexpr Dn2CppCpuFeatureInfo g_features[] = {
#define DN2CPP_CPU_FEATURE_ROW(id, name, arch, parents) { name, DN2CPP_CPU_##id, (parents) },
    DN2CPP_CPU_FEATURE_TABLE(DN2CPP_CPU_FEATURE_ROW)
#undef DN2CPP_CPU_FEATURE_ROW
};
constexpr int g_feature_count = (int)(sizeof(g_features) / sizeof(g_features[0]));

constexpr uint64_t dn2cpp_cpu_all_bits()
{
    uint64_t all = 0;
    for (int i = 0; i < g_feature_count; i++)
        all |= g_features[i].bit;
    return all;
}
constexpr uint64_t g_all_bits = dn2cpp_cpu_all_bits();

// .NET 10 exposes the AVX-512 F/BW/CD/DQ/VL types as one unit.
struct Dn2CppCpuFeatureAlias
{
    const char* name;
    uint64_t bit;
};
constexpr Dn2CppCpuFeatureAlias g_aliases[] = {
    { "Avx512BW", DN2CPP_CPU_X86_AVX512 },
    { "Avx512CD", DN2CPP_CPU_X86_AVX512 },
    { "Avx512DQ", DN2CPP_CPU_X86_AVX512 },
    { "Avx512VL", DN2CPP_CPU_X86_AVX512 },
};

// ---------------------------------------------------------------------------
// Detection
// ---------------------------------------------------------------------------

#if DN2CPP_TARGET_X64

struct Dn2CppCpuid
{
    uint32_t eax, ebx, ecx, edx;
};

Dn2CppCpuid dn2cpp_cpuid(uint32_t leaf, uint32_t subleaf)
{
    Dn2CppCpuid r;
#if defined(_MSC_VER) && !defined(__clang__)
    int info[4];
    __cpuidex(info, (int)leaf, (int)subleaf);
    r.eax = (uint32_t)info[0];
    r.ebx = (uint32_t)info[1];
    r.ecx = (uint32_t)info[2];
    r.edx = (uint32_t)info[3];
#else
    __cpuid_count(leaf, subleaf, r.eax, r.ebx, r.ecx, r.edx);
#endif
    return r;
}

// XCR0 via xgetbv. The compiler intrinsic needs the TU compiled with -mxsave on
// gcc/clang, which the baseline build is not, so the instruction is spelled out.
uint64_t dn2cpp_xgetbv0()
{
#if defined(_MSC_VER) && !defined(__clang__)
    return (uint64_t)_xgetbv(0);
#else
    uint32_t eax, edx;
    __asm__ volatile("xgetbv" : "=a"(eax), "=d"(edx) : "c"(0));
    return ((uint64_t)edx << 32) | eax;
#endif
}

// The bits are one per public type, but .NET 10's JIT gates several types as
// one instruction set (minipal cpufeatures.c sets the set only when every
// CPUID bit of the group is present, and the JIT maps each type onto its
// set). Detection sets such a group's bits only together, so on a CPU with
// part of a group every getter answers as .NET's does. The groups:
//   Sse42       Sse3, Ssse3, Sse41, Sse42, Popcnt
//   Aes         Aes, Pclmulqdq
//   Avx2        Avx2, Bmi1, Bmi2, Lzcnt, Fma, plus F16C and MOVBE (no type)
//   Avx512      F, BW, CD, DQ, VL (one family here; the aliases name it)
//   Avx512v2    Avx512Vbmi with AVX512-IFMA (no type)
//   Avx512v3    Avx512Vbmi2 with AVX512-VNNI, -BITALG and -VPOPCNTDQ (no type)
//   Vaes        Vaes, Vpclmulqdq
//   AvxVnniInt  AvxVnniInt8, AvxVnniInt16
//   Avx10v1     the Avx512v3 group with AVX512-BF16 and -FP16, an AVX10
//               enumeration leaf reporting the 128-, 256- and 512-bit widths,
//               and version >= 1; Avx10v2 the same with version >= 2
// Sse, Sse2, Avx, AvxVnni, Gfni and X86Serialize are sets of their own.
uint64_t dn2cpp_cpu_detect()
{
    uint64_t f = DN2CPP_CPU_X86_X86BASE;
    Dn2CppCpuid l0 = dn2cpp_cpuid(0, 0);
    uint32_t maxBasic = l0.eax;
    uint32_t maxExt = dn2cpp_cpuid(0x80000000u, 0).eax;
    if (maxBasic < 1)
        return f;

    Dn2CppCpuid l1 = dn2cpp_cpuid(1, 0);
    if (l1.edx & (1u << 25)) f |= DN2CPP_CPU_X86_SSE;
    if (l1.edx & (1u << 26)) f |= DN2CPP_CPU_X86_SSE2;

    bool sse42 = (l1.ecx & (1u << 0)) != 0     // SSE3
        && (l1.ecx & (1u << 9)) != 0           // SSSE3
        && (l1.ecx & (1u << 19)) != 0          // SSE4.1
        && (l1.ecx & (1u << 20)) != 0          // SSE4.2
        && (l1.ecx & (1u << 23)) != 0;         // POPCNT
    if (sse42)
        f |= DN2CPP_CPU_X86_SSE3 | DN2CPP_CPU_X86_SSSE3 | DN2CPP_CPU_X86_SSE41
            | DN2CPP_CPU_X86_SSE42 | DN2CPP_CPU_X86_POPCNT;

    if ((l1.ecx & (1u << 25)) != 0 && (l1.ecx & (1u << 1)) != 0)  // AESNI, PCLMULQDQ
        f |= DN2CPP_CPU_X86_AES | DN2CPP_CPU_X86_PCLMULQDQ;

    bool lzcnt = false;
    if (maxExt >= 0x80000001u)
    {
        Dn2CppCpuid e1 = dn2cpp_cpuid(0x80000001u, 0);
        lzcnt = (e1.ecx & (1u << 5)) != 0;
    }

    // The OS must save the YMM (XCR0 bits 1-2) and ZMM (bits 5-7) state, or
    // the registers are clobbered across context switches. Only the AVX-encoded
    // families depend on it; GFNI and SERIALIZE below do not.
    bool osxsave = (l1.ecx & (1u << 27)) != 0;
    uint64_t xcr0 = osxsave ? dn2cpp_xgetbv0() : 0;
    bool avxState = (xcr0 & 0x6) == 0x6;
#if defined(__APPLE__)
    // Match .NET's contract: macOS enables AVX-512 state lazily per thread,
    // and VEX-encoded mask moves do not trigger that enablement.
    bool avx512State = false;
#else
    bool avx512State = avxState && (xcr0 & 0xE0) == 0xE0;
#endif

    bool avx = avxState && (l1.ecx & (1u << 28)) != 0;
    if (avx)
        f |= DN2CPP_CPU_X86_AVX;

    if (maxBasic < 7)
        return f;

    Dn2CppCpuid l7 = dn2cpp_cpuid(7, 0);
    if (l7.ecx & (1u << 8))  f |= DN2CPP_CPU_X86_GFNI;
    if (l7.edx & (1u << 14)) f |= DN2CPP_CPU_X86_X86SERIALIZE;
    if (!avxState)
        return f;

    bool avx2 = avx
        && lzcnt
        && (l1.ecx & (1u << 12)) != 0          // FMA
        && (l1.ecx & (1u << 22)) != 0          // MOVBE
        && (l1.ecx & (1u << 29)) != 0          // F16C
        && (l7.ebx & (1u << 3)) != 0           // BMI1
        && (l7.ebx & (1u << 5)) != 0           // AVX2
        && (l7.ebx & (1u << 8)) != 0;          // BMI2
    if (avx2)
        f |= DN2CPP_CPU_X86_AVX2 | DN2CPP_CPU_X86_BMI1 | DN2CPP_CPU_X86_BMI2
            | DN2CPP_CPU_X86_LZCNT | DN2CPP_CPU_X86_FMA;

    if ((l7.ecx & (1u << 9)) != 0 && (l7.ecx & (1u << 10)) != 0)  // VAES, VPCLMULQDQ
        f |= DN2CPP_CPU_X86_VAES | DN2CPP_CPU_X86_VPCLMULQDQ;

    bool avx512 = avx512State
        && (l7.ebx & (1u << 16)) != 0    // F
        && (l7.ebx & (1u << 17)) != 0    // DQ
        && (l7.ebx & (1u << 28)) != 0    // CD
        && (l7.ebx & (1u << 30)) != 0    // BW
        && (l7.ebx & (1u << 31)) != 0;   // VL
    bool avx512v2 = avx512
        && (l7.ebx & (1u << 21)) != 0    // IFMA
        && (l7.ecx & (1u << 1)) != 0;    // VBMI
    bool avx512v3 = avx512v2
        && (l7.ecx & (1u << 6)) != 0     // VBMI2
        && (l7.ecx & (1u << 11)) != 0    // VNNI
        && (l7.ecx & (1u << 12)) != 0    // BITALG
        && (l7.ecx & (1u << 14)) != 0;   // VPOPCNTDQ
    if (avx512)   f |= DN2CPP_CPU_X86_AVX512;
    if (avx512v2) f |= DN2CPP_CPU_X86_AVX512VBMI;
    if (avx512v3) f |= DN2CPP_CPU_X86_AVX512VBMI2;
    bool avx512fp16 = (l7.edx & (1u << 23)) != 0;

    bool avx512bf16 = false, avx10 = false;
    if (l7.eax >= 1)
    {
        Dn2CppCpuid l71 = dn2cpp_cpuid(7, 1);
        if (l71.eax & (1u << 4)) f |= DN2CPP_CPU_X86_AVXVNNI;
        if ((l71.edx & (1u << 4)) != 0 && (l71.edx & (1u << 10)) != 0)  // INT8, INT16
            f |= DN2CPP_CPU_X86_AVXVNNIINT8 | DN2CPP_CPU_X86_AVXVNNIINT16;
        avx512bf16 = (l71.eax & (1u << 5)) != 0;
        avx10 = (l71.edx & (1u << 19)) != 0;
    }

    if (avx10 && avx512v3 && avx512fp16 && avx512bf16 && maxBasic >= 0x24)
    {
        Dn2CppCpuid l24 = dn2cpp_cpuid(0x24, 0);
        uint32_t version = l24.ebx & 0xFFu;
        bool allWidths = (l24.ebx & (1u << 16)) != 0    // 128-bit
            && (l24.ebx & (1u << 17)) != 0              // 256-bit
            && (l24.ebx & (1u << 18)) != 0;             // 512-bit
        // The _512 bits back the V512 tokens of the ISA contract. Because .NET
        // 10 requires every width for Avx10v1 at all, they coincide with the
        // V1/V2 bits; they stay separate bits so the token map needs no change
        // should the width rule ever diverge.
        if (allWidths && version >= 1)
            f |= DN2CPP_CPU_X86_AVX10V1 | DN2CPP_CPU_X86_AVX10V1_512;
        if (allWidths && version >= 2)
            f |= DN2CPP_CPU_X86_AVX10V2 | DN2CPP_CPU_X86_AVX10V2_512;
    }
    return f;
}

#elif DN2CPP_TARGET_ARM64

#if defined(__APPLE__)
bool dn2cpp_sysctl_flag(const char* key)
{
    int value = 0;
    size_t size = sizeof(value);
    if (sysctlbyname(key, &value, &size, nullptr, 0) != 0)
        return false;
    return value != 0;
}
#endif

uint64_t dn2cpp_cpu_detect()
{
    uint64_t f = DN2CPP_CPU_ARM_ARMBASE | DN2CPP_CPU_ARM_ADVSIMD;
#if defined(__APPLE__)
    if (dn2cpp_sysctl_flag("hw.optional.arm.FEAT_AES"))     f |= DN2CPP_CPU_ARM_AES;
    if (dn2cpp_sysctl_flag("hw.optional.armv8_crc32"))      f |= DN2CPP_CPU_ARM_CRC32;
    if (dn2cpp_sysctl_flag("hw.optional.arm.FEAT_SHA1"))    f |= DN2CPP_CPU_ARM_SHA1;
    if (dn2cpp_sysctl_flag("hw.optional.arm.FEAT_SHA256"))  f |= DN2CPP_CPU_ARM_SHA256;
    if (dn2cpp_sysctl_flag("hw.optional.arm.FEAT_DotProd")) f |= DN2CPP_CPU_ARM_DP;
    if (dn2cpp_sysctl_flag("hw.optional.arm.FEAT_RDM"))     f |= DN2CPP_CPU_ARM_RDM;
#elif defined(__linux__)
#ifndef HWCAP_AES
#define HWCAP_AES (1 << 3)
#endif
#ifndef HWCAP_SHA1
#define HWCAP_SHA1 (1 << 5)
#endif
#ifndef HWCAP_SHA2
#define HWCAP_SHA2 (1 << 6)
#endif
#ifndef HWCAP_CRC32
#define HWCAP_CRC32 (1 << 7)
#endif
#ifndef HWCAP_ASIMDRDM
#define HWCAP_ASIMDRDM (1 << 12)
#endif
#ifndef HWCAP_ASIMDDP
#define HWCAP_ASIMDDP (1 << 20)
#endif
    unsigned long hwcap = getauxval(AT_HWCAP);
    if (hwcap & HWCAP_AES)      f |= DN2CPP_CPU_ARM_AES;
    if (hwcap & HWCAP_SHA1)     f |= DN2CPP_CPU_ARM_SHA1;
    if (hwcap & HWCAP_SHA2)     f |= DN2CPP_CPU_ARM_SHA256;
    if (hwcap & HWCAP_CRC32)    f |= DN2CPP_CPU_ARM_CRC32;
    if (hwcap & HWCAP_ASIMDRDM) f |= DN2CPP_CPU_ARM_RDM;
    if (hwcap & HWCAP_ASIMDDP)  f |= DN2CPP_CPU_ARM_DP;
#elif defined(_WIN32)
    // Feature numbers spelled out: older SDKs lack the PF_ARM_* names.
    if (IsProcessorFeaturePresent(30)) // PF_ARM_V8_CRYPTO_INSTRUCTIONS_AVAILABLE
        f |= DN2CPP_CPU_ARM_AES | DN2CPP_CPU_ARM_SHA1 | DN2CPP_CPU_ARM_SHA256;
    if (IsProcessorFeaturePresent(31)) // PF_ARM_V8_CRC32_INSTRUCTIONS_AVAILABLE
        f |= DN2CPP_CPU_ARM_CRC32;
    // Windows has no Rdm query, so as in .NET the dot-product query answers for
    // Rdm too: FEAT_RDM is mandatory from Armv8.1 and FEAT_DotProd is Armv8.2.
    if (IsProcessorFeaturePresent(43)) // PF_ARM_V82_DP_INSTRUCTIONS_AVAILABLE
        f |= DN2CPP_CPU_ARM_DP | DN2CPP_CPU_ARM_RDM;
#endif
    return f;
}

#elif DN2CPP_TARGET_WASM32

// Compile-time: a module carrying SIMD instructions cannot instantiate on an
// engine without them, so the build flag is the whole answer.
uint64_t dn2cpp_cpu_detect()
{
#if defined(__wasm_simd128__)
    return DN2CPP_CPU_WASM_PACKEDSIMD;
#else
    return 0;
#endif
}

#else

uint64_t dn2cpp_cpu_detect()
{
    return 0;
}

#endif

// ---------------------------------------------------------------------------
// Mask
// ---------------------------------------------------------------------------

bool dn2cpp_token_equals(const char* tok, size_t len, const char* name)
{
    for (size_t i = 0; i < len; i++)
    {
        if (name[i] == '\0')
            return false;
        if (std::tolower((unsigned char)tok[i]) != std::tolower((unsigned char)name[i]))
            return false;
    }
    return name[len] == '\0';
}

// Every row the name matches, or 0 for an unknown name. `Aes` names both the
// X86 and the Arm family; a bit of another architecture is inert because
// detection never sets it, so no row shadows a same-named one.
uint64_t dn2cpp_lookup_feature(const char* tok, size_t len)
{
    uint64_t bits = 0;
    for (int i = 0; i < g_feature_count; i++)
    {
        if (dn2cpp_token_equals(tok, len, g_features[i].name))
            bits |= g_features[i].bit;
    }
    for (const Dn2CppCpuFeatureAlias& a : g_aliases)
    {
        if (dn2cpp_token_equals(tok, len, a.name))
            bits |= a.bit;
    }
    return bits;
}

// The families plus every family they imply, to a fixpoint.
uint64_t dn2cpp_with_ancestors(uint64_t f)
{
    for (;;)
    {
        uint64_t before = f;
        for (int i = 0; i < g_feature_count; i++)
        {
            if ((f & g_features[i].bit) != 0)
                f |= g_features[i].parents;
        }
        if (f == before)
            return f;
    }
}

bool dn2cpp_is_separator(char c)
{
    return c == ',' || std::isspace((unsigned char)c) != 0;
}

// Grammar: items separated by commas or whitespace, case-insensitive. A bare
// name allows that family and every family it implies, so `Avx2` means Avx2
// and what it needs; a leading '-' removes a family, and dn2cpp_closure then
// removes every family that implies it; `none` allows nothing (the baseline
// families included, like DOTNET_EnableHWIntrinsic=0) and `all` allows
// everything. Unknown items are reported once and ignored, never fatal.
uint64_t dn2cpp_parse_mask(const char* spec)
{
    uint64_t positive = 0, negative = 0;
    bool anyPositive = false, none = false;
    const char* p = spec;
    while (*p != '\0')
    {
        while (*p != '\0' && dn2cpp_is_separator(*p))
            p++;
        if (*p == '\0')
            break;
        const char* item = p;
        while (*p != '\0' && !dn2cpp_is_separator(*p))
            p++;
        int itemLen = (int)(p - item);
        const char* start = item;
        size_t len = (size_t)itemLen;
        bool exclude = false;
        if (start[0] == '-')
        {
            exclude = true;
            start++;
            len--;
        }
        uint64_t bits = len > 0 ? dn2cpp_lookup_feature(start, len) : 0;
        if (!exclude && dn2cpp_token_equals(start, len, "none"))
        {
            none = true;
        }
        else if (!exclude && dn2cpp_token_equals(start, len, "all"))
        {
            anyPositive = true;
            positive |= g_all_bits;
        }
        else if (bits != 0)
        {
            if (exclude)
                negative |= bits;
            else
            {
                anyPositive = true;
                positive |= dn2cpp_with_ancestors(bits);
            }
        }
        else
        {
            std::fprintf(stderr, "[dn2cpp] DN2CPP_CPU_FEATURES: unknown family '%.*s'\n", itemLen, item);
        }
    }
    if (none)
        return 0;
    if (anyPositive)
        return positive & ~negative;
    return g_all_bits & ~negative;
}

// Clear every feature whose parents are not all present, to a fixpoint.
uint64_t dn2cpp_closure(uint64_t f)
{
    for (;;)
    {
        uint64_t before = f;
        for (int i = 0; i < g_feature_count; i++)
        {
            if ((f & g_features[i].bit) != 0 && (f & g_features[i].parents) != g_features[i].parents)
                f &= ~g_features[i].bit;
        }
        if (f == before)
            return f;
    }
}

// ---------------------------------------------------------------------------
// Diagnostics
// ---------------------------------------------------------------------------

struct Dn2CppLineBuffer
{
    char text[1536];
    size_t len = 0;

    void put(const char* s)
    {
        size_t n = std::strlen(s);
        size_t room = sizeof(text) - 1 - len;
        if (n > room)
            n = room;
        std::memcpy(text + len, s, n);
        len += n;
        text[len] = '\0';
    }
};

void dn2cpp_put_names(Dn2CppLineBuffer& out, uint64_t f, const char* emptyText)
{
    bool any = false;
    for (int i = 0; i < g_feature_count; i++)
    {
        if ((f & g_features[i].bit) == 0)
            continue;
        if (any)
            out.put(",");
        out.put(g_features[i].name);
        any = true;
    }
    if (!any)
        out.put(emptyText);
}

void dn2cpp_print_diag(uint64_t detected, uint64_t allowed, uint64_t effective)
{
    Dn2CppLineBuffer line;
    line.put("[dn2cpp] cpu features: detected=");
    dn2cpp_put_names(line, detected, "(none)");
    line.put(" allowed=");
    if (allowed == g_all_bits)
        line.put("all");
    else
        dn2cpp_put_names(line, allowed, "none");
    line.put(" effective=");
    dn2cpp_put_names(line, effective, "(none)");
    std::fprintf(stderr, "%s\n", line.text);
}

} // namespace

uint64_t dn2cpp_cpu_features_resolve()
{
    uint64_t cached = dn2cpp_cpu_features_cache.load(std::memory_order_relaxed);
    if (cached != 0)
        return cached;
    uint64_t detected = dn2cpp_cpu_detect();
    uint64_t policy = detected;
    const char* avx10v2 = dn2cpp_pal_getenv("DN2CPP_ENABLE_AVX10V2");
    // .NET 10 keeps AVX10.2 and its V512 VNNI set behind the positive
    // EnableAVX10v2 switch. Only the explicit value 1 opts in here; the
    // narrowing mask below cannot turn a policy-disabled family back on.
    if (avx10v2 == nullptr || std::strcmp(avx10v2, "1") != 0)
        policy &= ~(DN2CPP_CPU_X86_AVX10V2 | DN2CPP_CPU_X86_AVX10V2_512);
    policy = dn2cpp_closure(policy);
    const char* spec = dn2cpp_pal_getenv("DN2CPP_CPU_FEATURES");
#if defined(DN2CPP_CPU_FEATURES_DEFAULT)
    if (spec == nullptr || spec[0] == '\0')
        spec = DN2CPP_CPU_FEATURES_DEFAULT;
#endif
    uint64_t allowed = (spec == nullptr || spec[0] == '\0') ? g_all_bits : dn2cpp_parse_mask(spec);
    uint64_t effective = dn2cpp_closure(policy & allowed);
    const char* diag = dn2cpp_pal_getenv("DN2CPP_CPU_FEATURES_DIAG");
    if (diag != nullptr && diag[0] != '\0')
        dn2cpp_print_diag(detected, allowed, effective);
    uint64_t result = effective | DN2CPP_CPU_RESOLVED;
    dn2cpp_cpu_features_cache.store(result, std::memory_order_relaxed);
    return result;
}
