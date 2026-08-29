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
# the same family. The knob names are read off an x86-64 host's libclrjit
# (`strings libclrjit.so | grep '^Enable'`) and confirmed there when the first
# X86 family is Lowered — a wrong name fails loudly (oracle True vs ours False),
# never silently. Run P is unreachable until then.
PLATFORM_ISA_P_MASK="-Avx"
PLATFORM_ISA_P_KNOBS="DOTNET_EnableAVX=0"
PLATFORM_ISA_P_TRUE="X86.X86Base"
PLATFORM_ISA_P_FALSE="X86.Avx"

platform_isa_native_gate x86
