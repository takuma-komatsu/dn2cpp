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
#
# The SCALAR axis uses separate runtime/app build dirs and a -scalar output dir
# (gates/_common.sh), so this runs alongside the default gate without clobbering it.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/vector-explicit-probe.sh"
source "$(dirname "$0")/vector-value-probe.sh"
source "$(dirname "$0")/vector-unsupported-probe.sh"
source "$(dirname "$0")/vector-fixed-value-probe.sh"
source "$(dirname "$0")/vector-concrete-elements-probe.sh"
source "$(dirname "$0")/vector-estimate-probe.sh"

export SCALAR=1
# dn2cpp fixes Vector<T> at 128 bits. Keep the hardware-dependent real-.NET
# oracle at that width so exact output remains host-independent.
export DN2CPP_ORACLE_DOTNET_ENABLE_AVX=0
export DN2CPP_GATE_EXTRA_CONTEXT="dotnet-vector-width:128|explicit-raw-diet-retained-fill|block-repeat-low-bytes|vector-value-raw-diet|fixed-value-four-width-raw-diet|vector-unsupported-managed-storage-abi-raw-diet|concrete-elements-raw-diet-software-exceptions-authority|vector-estimate-software-rounding-raw-diet-default-nohw-noavx2|generic-estimate-managed-count-guard-raw-diet-authority-il-negatives|cli:$(_gate_cli_hash)"
export DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/vector-estimate-probe.sh gates/fixtures/ldftn-local/Program.cs gates/fixtures/ldftn-local/LdftnLocalFixture.csproj gates/fixtures/ldftn-local/GenericVectorEstimateFixture.cs gates/fixtures/ldftn-local/GenericVectorEstimateGuards.cs ${DN2CPP_GENERIC_VECTOR_ESTIMATE_FIXTURE_DLL:-} ${DN2CPP_GENERIC_VECTOR_ESTIMATE_COMPILER_DLL:-} gates/fixtures/vector-estimate ${DN2CPP_VECTOR_ESTIMATE_DLL:-} ${DN2CPP_VECTOR_ESTIMATE_FIXTURE_DLL:-} ${DN2CPP_VECTOR_ESTIMATE_COMPILER_DLL:-} gates/fixtures/ldftn-local/VectorEstimateGuards.cs gates/fixtures/ldftn-local/VectorEstimateFixture.cs gates/vector-concrete-elements-probe.sh gates/fixtures/vector-concrete-elements ${DN2CPP_VECTOR_CONCRETE_ELEMENTS_DLL:-} ${DN2CPP_VECTOR_CONCRETE_ELEMENTS_FIXTURE_DLL:-} ${DN2CPP_VECTOR_CONCRETE_ELEMENTS_COMPILER_DLL:-} gates/fixtures/ldftn-local/ConcreteVectorElementsGuards.cs gates/fixtures/ldftn-local/ConcreteVectorElementsFixture.cs gates/vector-fixed-value-probe.sh gates/fixtures/vector-fixed-value ${DN2CPP_FIXED_VECTOR_VALUE_FIXTURE_DLL:-} ${DN2CPP_FIXED_VECTOR_VALUE_COMPILER_DLL:-} gates/vector-value-probe.sh gates/fixtures/vector-value gates/vector-unsupported-probe.sh gates/fixtures/vector-unsupported ${DN2CPP_VECTOR_UNSUPPORTED_FIXTURE_DLL:-} ${DN2CPP_VECTOR_UNSUPPORTED_COMPILER_DLL:-} gates/vector-explicit-probe.sh gates/fixtures/vector-explicit gates/fixtures/ldftn-local/VectorExplicitFixture.cs gates/fixtures/ldftn-local/VectorExplicitGuards.cs gates/fixtures/ldftn-local/VectorBlockFixture.cs"

corelib_diff_gate VectorProbe
