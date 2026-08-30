#!/usr/bin/env bash
# The X86 and Arm platform-ISA capability contracts, diffed against real .NET
# from one shared host-native PlatformIsaProbe. On the matching host every
# Lowered family answers from run-time CPU detection and executes the instruction
# results real .NET prints; on the foreign host every row is false and every
# representative call throws PlatformNotSupportedException. Both families run
# with all hardware intrinsics disabled, with their architecture-specific
# partial masks, and through native-only invalid-immediate boundaries. X86 also
# runs the AVX10.2 opt-in when the compiler can build those helpers. Rows,
# Lowered flags and boundary cases come from the generated tables, so the shared
# plan changes without a hand-maintained family list here.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_platform_isa.sh"

platform_isa_native_gate
