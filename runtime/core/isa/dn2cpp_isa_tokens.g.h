#pragma once
// GENERATED FILE — do not edit by hand.
//
// One IsSupported token per hardware-intrinsics family, as the transpiler emits it.
// Regenerate from System.Private.CoreLib with:
//
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
//
// On the family's own target, with every required compiler intrinsic available, the token
// asks the feature detector for every bit the family requires. Otherwise it is false, so
// the guarded arm is dead code the compiler drops. Nested X64/Arm64 types share their
// enclosing type's bits: every target here is 64-bit.
#include "dn2cpp_cpu_features.h"

// System.Runtime.Intrinsics.X86
#if DN2CPP_TARGET_X64
#define DN2CPP_ISA_X86_X86Base (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_X86BASE))
#define DN2CPP_ISA_X86_X86Base_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_X86BASE))
#define DN2CPP_ISA_X86_Lzcnt (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_LZCNT))
#define DN2CPP_ISA_X86_Lzcnt_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_LZCNT))
#define DN2CPP_ISA_X86_Popcnt (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_POPCNT))
#define DN2CPP_ISA_X86_Popcnt_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_POPCNT))
#define DN2CPP_ISA_X86_Bmi1 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_BMI1))
#define DN2CPP_ISA_X86_Bmi1_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_BMI1))
#define DN2CPP_ISA_X86_Bmi2 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_BMI2))
#define DN2CPP_ISA_X86_Bmi2_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_BMI2))
#define DN2CPP_ISA_X86_X86Serialize (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_X86SERIALIZE))
#define DN2CPP_ISA_X86_X86Serialize_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_X86SERIALIZE))
#define DN2CPP_ISA_X86_Sse (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_SSE))
#define DN2CPP_ISA_X86_Sse_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_SSE))
#define DN2CPP_ISA_X86_Sse2 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_SSE2))
#define DN2CPP_ISA_X86_Sse2_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_SSE2))
#define DN2CPP_ISA_X86_Sse3 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_SSE3))
#define DN2CPP_ISA_X86_Sse3_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_SSE3))
#define DN2CPP_ISA_X86_Ssse3 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_SSSE3))
#define DN2CPP_ISA_X86_Ssse3_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_SSSE3))
#define DN2CPP_ISA_X86_Sse41 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_SSE41))
#define DN2CPP_ISA_X86_Sse41_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_SSE41))
#define DN2CPP_ISA_X86_Sse42 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_SSE42))
#define DN2CPP_ISA_X86_Sse42_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_SSE42))
#define DN2CPP_ISA_X86_Pclmulqdq (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_PCLMULQDQ))
#define DN2CPP_ISA_X86_Pclmulqdq_V256 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_VPCLMULQDQ | DN2CPP_CPU_X86_AVX))
#define DN2CPP_ISA_X86_Pclmulqdq_V512 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_VPCLMULQDQ | DN2CPP_CPU_X86_AVX512))
#define DN2CPP_ISA_X86_Pclmulqdq_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_PCLMULQDQ))
#define DN2CPP_ISA_X86_Aes (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AES))
#define DN2CPP_ISA_X86_Aes_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AES))
#define DN2CPP_ISA_X86_Avx (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX))
#define DN2CPP_ISA_X86_Avx_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX))
#define DN2CPP_ISA_X86_Avx2 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX2))
#define DN2CPP_ISA_X86_Avx2_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX2))
#define DN2CPP_ISA_X86_Fma (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_FMA))
#define DN2CPP_ISA_X86_Fma_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_FMA))
#define DN2CPP_ISA_X86_AvxVnni (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVXVNNI))
#define DN2CPP_ISA_X86_AvxVnni_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVXVNNI))
#define DN2CPP_ISA_X86_Avx512F (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX512))
#define DN2CPP_ISA_X86_Avx512F_VL (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX512))
#define DN2CPP_ISA_X86_Avx512F_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX512))
#define DN2CPP_ISA_X86_Avx512BW (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX512))
#define DN2CPP_ISA_X86_Avx512BW_VL (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX512))
#define DN2CPP_ISA_X86_Avx512BW_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX512))
#define DN2CPP_ISA_X86_Avx512CD (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX512))
#define DN2CPP_ISA_X86_Avx512CD_VL (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX512))
#define DN2CPP_ISA_X86_Avx512CD_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX512))
#define DN2CPP_ISA_X86_Avx512DQ (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX512))
#define DN2CPP_ISA_X86_Avx512DQ_VL (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX512))
#define DN2CPP_ISA_X86_Avx512DQ_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX512))
#define DN2CPP_ISA_X86_Avx512Vbmi (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX512VBMI))
#define DN2CPP_ISA_X86_Avx512Vbmi_VL (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX512VBMI))
#define DN2CPP_ISA_X86_Avx512Vbmi_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX512VBMI))
#define DN2CPP_ISA_X86_Avx512Vbmi2 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX512VBMI2))
#define DN2CPP_ISA_X86_Avx512Vbmi2_VL (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX512VBMI2))
#define DN2CPP_ISA_X86_Avx512Vbmi2_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX512VBMI2))
#define DN2CPP_ISA_X86_Avx10v1 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX10V1))
#define DN2CPP_ISA_X86_Avx10v1_V512 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX10V1_512))
#define DN2CPP_ISA_X86_Avx10v1_V512_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX10V1_512))
#define DN2CPP_ISA_X86_Avx10v1_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX10V1))
#define DN2CPP_ISA_X86_Avx10v2 (DN2CPP_HAS_X86_AVX10V2_INTRINSICS && dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX10V2))
#define DN2CPP_ISA_X86_Avx10v2_V512 (DN2CPP_HAS_X86_AVX10V2_INTRINSICS && dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX10V2_512))
#define DN2CPP_ISA_X86_Avx10v2_V512_X64 (DN2CPP_HAS_X86_AVX10V2_INTRINSICS && dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX10V2_512))
#define DN2CPP_ISA_X86_Avx10v2_X64 (DN2CPP_HAS_X86_AVX10V2_INTRINSICS && dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVX10V2))
#define DN2CPP_ISA_X86_AvxVnniInt8 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVXVNNIINT8))
#define DN2CPP_ISA_X86_AvxVnniInt8_V512 (DN2CPP_HAS_X86_AVX10V2_INTRINSICS && dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVXVNNIINT8 | DN2CPP_CPU_X86_AVX10V2_512))
#define DN2CPP_ISA_X86_AvxVnniInt8_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVXVNNIINT8))
#define DN2CPP_ISA_X86_AvxVnniInt16 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVXVNNIINT16))
#define DN2CPP_ISA_X86_AvxVnniInt16_V512 (DN2CPP_HAS_X86_AVX10V2_INTRINSICS && dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVXVNNIINT16 | DN2CPP_CPU_X86_AVX10V2_512))
#define DN2CPP_ISA_X86_AvxVnniInt16_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_AVXVNNIINT16))
#define DN2CPP_ISA_X86_Gfni (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_GFNI))
#define DN2CPP_ISA_X86_Gfni_V256 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_GFNI | DN2CPP_CPU_X86_AVX))
#define DN2CPP_ISA_X86_Gfni_V512 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_GFNI | DN2CPP_CPU_X86_AVX512))
#define DN2CPP_ISA_X86_Gfni_X64 (dn2cpp_cpu_has_all(DN2CPP_CPU_X86_GFNI))
#else
#define DN2CPP_ISA_X86_X86Base 0
#define DN2CPP_ISA_X86_X86Base_X64 0
#define DN2CPP_ISA_X86_Lzcnt 0
#define DN2CPP_ISA_X86_Lzcnt_X64 0
#define DN2CPP_ISA_X86_Popcnt 0
#define DN2CPP_ISA_X86_Popcnt_X64 0
#define DN2CPP_ISA_X86_Bmi1 0
#define DN2CPP_ISA_X86_Bmi1_X64 0
#define DN2CPP_ISA_X86_Bmi2 0
#define DN2CPP_ISA_X86_Bmi2_X64 0
#define DN2CPP_ISA_X86_X86Serialize 0
#define DN2CPP_ISA_X86_X86Serialize_X64 0
#define DN2CPP_ISA_X86_Sse 0
#define DN2CPP_ISA_X86_Sse_X64 0
#define DN2CPP_ISA_X86_Sse2 0
#define DN2CPP_ISA_X86_Sse2_X64 0
#define DN2CPP_ISA_X86_Sse3 0
#define DN2CPP_ISA_X86_Sse3_X64 0
#define DN2CPP_ISA_X86_Ssse3 0
#define DN2CPP_ISA_X86_Ssse3_X64 0
#define DN2CPP_ISA_X86_Sse41 0
#define DN2CPP_ISA_X86_Sse41_X64 0
#define DN2CPP_ISA_X86_Sse42 0
#define DN2CPP_ISA_X86_Sse42_X64 0
#define DN2CPP_ISA_X86_Pclmulqdq 0
#define DN2CPP_ISA_X86_Pclmulqdq_V256 0
#define DN2CPP_ISA_X86_Pclmulqdq_V512 0
#define DN2CPP_ISA_X86_Pclmulqdq_X64 0
#define DN2CPP_ISA_X86_Aes 0
#define DN2CPP_ISA_X86_Aes_X64 0
#define DN2CPP_ISA_X86_Avx 0
#define DN2CPP_ISA_X86_Avx_X64 0
#define DN2CPP_ISA_X86_Avx2 0
#define DN2CPP_ISA_X86_Avx2_X64 0
#define DN2CPP_ISA_X86_Fma 0
#define DN2CPP_ISA_X86_Fma_X64 0
#define DN2CPP_ISA_X86_AvxVnni 0
#define DN2CPP_ISA_X86_AvxVnni_X64 0
#define DN2CPP_ISA_X86_Avx512F 0
#define DN2CPP_ISA_X86_Avx512F_VL 0
#define DN2CPP_ISA_X86_Avx512F_X64 0
#define DN2CPP_ISA_X86_Avx512BW 0
#define DN2CPP_ISA_X86_Avx512BW_VL 0
#define DN2CPP_ISA_X86_Avx512BW_X64 0
#define DN2CPP_ISA_X86_Avx512CD 0
#define DN2CPP_ISA_X86_Avx512CD_VL 0
#define DN2CPP_ISA_X86_Avx512CD_X64 0
#define DN2CPP_ISA_X86_Avx512DQ 0
#define DN2CPP_ISA_X86_Avx512DQ_VL 0
#define DN2CPP_ISA_X86_Avx512DQ_X64 0
#define DN2CPP_ISA_X86_Avx512Vbmi 0
#define DN2CPP_ISA_X86_Avx512Vbmi_VL 0
#define DN2CPP_ISA_X86_Avx512Vbmi_X64 0
#define DN2CPP_ISA_X86_Avx512Vbmi2 0
#define DN2CPP_ISA_X86_Avx512Vbmi2_VL 0
#define DN2CPP_ISA_X86_Avx512Vbmi2_X64 0
#define DN2CPP_ISA_X86_Avx10v1 0
#define DN2CPP_ISA_X86_Avx10v1_V512 0
#define DN2CPP_ISA_X86_Avx10v1_V512_X64 0
#define DN2CPP_ISA_X86_Avx10v1_X64 0
#define DN2CPP_ISA_X86_Avx10v2 0
#define DN2CPP_ISA_X86_Avx10v2_V512 0
#define DN2CPP_ISA_X86_Avx10v2_V512_X64 0
#define DN2CPP_ISA_X86_Avx10v2_X64 0
#define DN2CPP_ISA_X86_AvxVnniInt8 0
#define DN2CPP_ISA_X86_AvxVnniInt8_V512 0
#define DN2CPP_ISA_X86_AvxVnniInt8_X64 0
#define DN2CPP_ISA_X86_AvxVnniInt16 0
#define DN2CPP_ISA_X86_AvxVnniInt16_V512 0
#define DN2CPP_ISA_X86_AvxVnniInt16_X64 0
#define DN2CPP_ISA_X86_Gfni 0
#define DN2CPP_ISA_X86_Gfni_V256 0
#define DN2CPP_ISA_X86_Gfni_V512 0
#define DN2CPP_ISA_X86_Gfni_X64 0
#endif

// System.Runtime.Intrinsics.Arm
#if DN2CPP_TARGET_ARM64
#define DN2CPP_ISA_Arm_ArmBase (dn2cpp_cpu_has_all(DN2CPP_CPU_ARM_ARMBASE))
#define DN2CPP_ISA_Arm_ArmBase_Arm64 (dn2cpp_cpu_has_all(DN2CPP_CPU_ARM_ARMBASE))
#define DN2CPP_ISA_Arm_Crc32 (dn2cpp_cpu_has_all(DN2CPP_CPU_ARM_CRC32))
#define DN2CPP_ISA_Arm_Crc32_Arm64 (dn2cpp_cpu_has_all(DN2CPP_CPU_ARM_CRC32))
#define DN2CPP_ISA_Arm_AdvSimd (dn2cpp_cpu_has_all(DN2CPP_CPU_ARM_ADVSIMD))
#define DN2CPP_ISA_Arm_AdvSimd_Arm64 (dn2cpp_cpu_has_all(DN2CPP_CPU_ARM_ADVSIMD))
#define DN2CPP_ISA_Arm_Aes (dn2cpp_cpu_has_all(DN2CPP_CPU_ARM_AES))
#define DN2CPP_ISA_Arm_Aes_Arm64 (dn2cpp_cpu_has_all(DN2CPP_CPU_ARM_AES))
#define DN2CPP_ISA_Arm_Sha1 (dn2cpp_cpu_has_all(DN2CPP_CPU_ARM_SHA1))
#define DN2CPP_ISA_Arm_Sha1_Arm64 (dn2cpp_cpu_has_all(DN2CPP_CPU_ARM_SHA1))
#define DN2CPP_ISA_Arm_Sha256 (dn2cpp_cpu_has_all(DN2CPP_CPU_ARM_SHA256))
#define DN2CPP_ISA_Arm_Sha256_Arm64 (dn2cpp_cpu_has_all(DN2CPP_CPU_ARM_SHA256))
#define DN2CPP_ISA_Arm_Dp (dn2cpp_cpu_has_all(DN2CPP_CPU_ARM_DP))
#define DN2CPP_ISA_Arm_Dp_Arm64 (dn2cpp_cpu_has_all(DN2CPP_CPU_ARM_DP))
#define DN2CPP_ISA_Arm_Rdm (dn2cpp_cpu_has_all(DN2CPP_CPU_ARM_RDM))
#define DN2CPP_ISA_Arm_Rdm_Arm64 (dn2cpp_cpu_has_all(DN2CPP_CPU_ARM_RDM))
#else
#define DN2CPP_ISA_Arm_ArmBase 0
#define DN2CPP_ISA_Arm_ArmBase_Arm64 0
#define DN2CPP_ISA_Arm_Crc32 0
#define DN2CPP_ISA_Arm_Crc32_Arm64 0
#define DN2CPP_ISA_Arm_AdvSimd 0
#define DN2CPP_ISA_Arm_AdvSimd_Arm64 0
#define DN2CPP_ISA_Arm_Aes 0
#define DN2CPP_ISA_Arm_Aes_Arm64 0
#define DN2CPP_ISA_Arm_Sha1 0
#define DN2CPP_ISA_Arm_Sha1_Arm64 0
#define DN2CPP_ISA_Arm_Sha256 0
#define DN2CPP_ISA_Arm_Sha256_Arm64 0
#define DN2CPP_ISA_Arm_Dp 0
#define DN2CPP_ISA_Arm_Dp_Arm64 0
#define DN2CPP_ISA_Arm_Rdm 0
#define DN2CPP_ISA_Arm_Rdm_Arm64 0
#endif
// Scalable vectors are never lowered: experimental in .NET 10 and without a fixed
// register width, so IsSupported is false on every target.
#define DN2CPP_ISA_Arm_Sve 0
#define DN2CPP_ISA_Arm_Sve_Arm64 0
#define DN2CPP_ISA_Arm_Sve2 0
#define DN2CPP_ISA_Arm_Sve2_Arm64 0

// System.Runtime.Intrinsics.Wasm
#if DN2CPP_TARGET_WASM32
#define DN2CPP_ISA_Wasm_PackedSimd (dn2cpp_cpu_has_all(DN2CPP_CPU_WASM_PACKEDSIMD))
#else
#define DN2CPP_ISA_Wasm_PackedSimd 0
#endif
