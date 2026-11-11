#!/usr/bin/env bash
# Scalar-axis variant of build-and-run-vector-probe.sh. Builds the runtime + the
# VectorProbe sample with the Highway SIMD backend opted out
# (-DDN2CPP_USE_HIGHWAY=OFF, set here via SCALAR=1), so every dn2cpp_vec_* op runs
# the portable scalar emulation (runtime/core/dn2cpp_vectors.h) instead of the
# Highway implementation (runtime/core/dn2cpp_vectors_hwy.h) the default build now
# carries.
#
# VectorProbe exercises the vec-op surface (add/sub/mul/min/max/and/or/xor/sum over
# integer lanes + equals/greater_than masks, plus direct Vector128 calls), and
# integer SIMD is bit-exact regardless of vectorization, so the output is diffed
# EXACT vs real .NET (corelib_diff_gate) — the same oracle as the default (Highway)
# vector-probe gate.
# FloatingPoint also compares NaN classification and exact float/double bits
# for Min/Max/Clamp, division, and fused multiply-add across vector widths.
#
# The SCALAR axis uses separate runtime/app build dirs and a -scalar output dir
# (gates/_common.sh), so this runs alongside the default gate without clobbering it.
# It transpiles the app's raw metadata (--no-ildiet); the default gate transpiles
# the ILDiet-rewritten metadata of the same program.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_vector-floating-point.sh"
source "$(dirname "$0")/_vector-corelib-checks.sh"

export SCALAR=1
# dn2cpp fixes Vector<T> at 128 bits. Keep the hardware-dependent real-.NET
# oracle at that width so exact output remains host-independent.
export DN2CPP_ORACLE_DOTNET_ENABLE_AVX=0
export DN2CPP_GATE_EXTRA_CONTEXT="dotnet-vector-width:128|floating-point-prefix-and-witnesses|concrete-elements:DOTNET_EnableHWIntrinsic=0"
export DN2CPP_GATE_EXTRA_INPUTS="gates/_vector-floating-point.sh gates/_vector-corelib-checks.sh"

source "$(dirname "$0")/_vector-regression-checks.sh"

corelib_diff_gate VectorProbe --no-ildiet
vector_section_witnesses "$_CG_OUT/native.stdout"
vector_concrete_elements_gate "$_CG_OUT" "$_CG_APP"
# A CLR may fuse MultiplyAddEstimate, so only the native nonfused profile is asserted.
gate_output witness "$_CG_OUT/native.stderr" 'vector estimate nonfused profile passed' --count 1 --policy crlf
# Sections after FloatingPoint leave the earlier output an unchanged prefix and
# all run to the final marker.
DOTNET_EnableAVX=0 gate_capture_ok "$_CG_OUT/before-explicit" run_bounded dotnet "$_CG_APP" before-explicit
gate_output prefix "$_CG_OUT/native.stdout" "$_CG_OUT/before-explicit.stdout" \
    --before '-- vector explicit reinterprets --' --policy crlf
