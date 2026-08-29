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
# the same family. Bmi1 sits in .NET 10's AVX2 instruction set, which AVX
# implies, so removing AVX removes Bmi1 on both sides. Removing AVX keeps
# every SSE-level family, so the Lowered set S runs (X86Base, X86Serialize,
# Sse through Sse42, Ssse3, Popcnt, Pclmulqdq, Aes) is diffed unmasked in S and
# under the mask in P, with X86Base as the kept witness. Bmi1 is Lowered only
# once the AVX families it implies are, so the removed witness is checked from
# that point on; until then P proves the kept witness alone.
# The knob names are read off an x86-64 host's libclrjit (`strings
# libclrjit.so | grep '^Enable'`) and confirmed there — a wrong name fails
# loudly (oracle True vs ours False), never silently.
PLATFORM_ISA_P_MASK="-Avx"
PLATFORM_ISA_P_KNOBS="DOTNET_EnableAVX=0"
PLATFORM_ISA_P_TRUE="X86.X86Base"
PLATFORM_ISA_P_FALSE="X86.Bmi1"

platform_isa_native_gate x86
