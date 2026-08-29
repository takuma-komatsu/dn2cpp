#!/usr/bin/env bash
# The X86 platform-ISA families' capability contract, diffed against real .NET:
# every X86 row false and every X86 family throwing PlatformNotSupportedException
# off an x86-64 host; on one, the Lowered families answering per-CPU from
# run-time detection with the instruction results real .NET prints; all rows
# false under DN2CPP_CPU_FEATURES=none (oracle DOTNET_EnableHWIntrinsic=0) with
# exactly one diag line; and a partial mask removing one family and keeping the
# rest. Rows and Lowered flags come from the generated transpiler table, so a
# family promoted to Lowered joins the supported runs with no edit here.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_platform_isa.sh"

# The partial-mask pair for run P: our mask vs the .NET JIT knob that removes
# the same families. AVX is the root of .NET 10's AVX-level, AVX-512 and AVX10
# instruction sets, so removing it removes everything above the SSE level on
# both sides — Avx, Avx2, Fma, AvxVnni, Lzcnt, Bmi1, Bmi2, AvxVnniInt8/Int16,
# the Avx512 families with Vbmi and Vbmi2, Avx10v1, Avx10v2 and the V256 / V512
# nested types of Gfni and Pclmulqdq — while every SSE-level family and Gfni
# itself stay. Every x86 family is Lowered, so S diffs the whole x86 surface
# unmasked and P the whole surface under the mask, with X86Base as the kept
# witness and Bmi1 as the removed one; _platform_isa.sh asserts each witness
# only while its family is in the Lowered set, and both are.
# The knob names are read off an x86-64 host's libclrjit (`strings
# libclrjit.so | grep '^Enable'`) and confirmed there — a wrong name fails
# loudly (oracle True vs ours False), never silently.
PLATFORM_ISA_P_MASK="-Avx"
PLATFORM_ISA_P_KNOBS="DOTNET_EnableAVX=0"
PLATFORM_ISA_P_TRUE="X86.X86Base"
PLATFORM_ISA_P_FALSE="X86.Bmi1"

platform_isa_native_gate x86
