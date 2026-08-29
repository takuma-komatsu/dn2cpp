#!/usr/bin/env bash
# The Arm platform-ISA families' capability contract, diffed against real .NET:
# every Arm row false and every Arm family throwing PlatformNotSupportedException
# off an arm64 host; on one, the Lowered families answering per-CPU from
# run-time detection with the instruction results real .NET prints; all rows
# false under DN2CPP_CPU_FEATURES=none (oracle DOTNET_EnableHWIntrinsic=0) with
# exactly one diag line; and a partial mask keeping ArmBase+AdvSimd and removing
# the crypto, Crc32, Dp, Rdm and Sve families, witnessed on ArmBase and Crc32.
# Rows and Lowered flags come from the generated transpiler table, so a family
# promoted to Lowered joins the supported runs with no edit here.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_platform_isa.sh"

# The partial-mask pair for run P. .NET 10's arm64 JIT has a knob per optional
# family and none for AdvSimd (it is baseline there), so the oracle disables
# every optional family and our mask allows the two baseline ones. The
# witnesses are the two scalar families: both are Lowered, ArmBase is kept by
# the mask and Crc32 removed, and neither stops being a witness as the vector
# families become Lowered.
PLATFORM_ISA_P_MASK="ArmBase,AdvSimd"
PLATFORM_ISA_P_KNOBS="DOTNET_EnableArm64Aes=0 DOTNET_EnableArm64Crc32=0 DOTNET_EnableArm64Dp=0 DOTNET_EnableArm64Rdm=0 DOTNET_EnableArm64Sha1=0 DOTNET_EnableArm64Sha256=0 DOTNET_EnableArm64Sve=0 DOTNET_EnableArm64Sve2=0"
PLATFORM_ISA_P_TRUE="Arm.ArmBase"
PLATFORM_ISA_P_FALSE="Arm.Crc32"

platform_isa_native_gate arm
