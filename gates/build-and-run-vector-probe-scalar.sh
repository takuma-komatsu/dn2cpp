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
case "${DN2CPP_VECTOR_FULL:-0}" in
    0|1) DN2CPP_VECTOR_FULL=${DN2CPP_VECTOR_FULL:-0} ;;
    *) echo 'DN2CPP_VECTOR_FULL must be 0 or 1' >&2; exit 1 ;;
esac
if [ "$DN2CPP_VECTOR_FULL" = 1 ]; then
    echo 'vector coverage: full route and preprocessing matrix'
    source "$(dirname "$0")/_vector-explicit-probe.sh"
    source "$(dirname "$0")/_vector-value-probe.sh"
    source "$(dirname "$0")/_vector-unsupported-probe.sh"
    source "$(dirname "$0")/_vector-fixed-value-probe.sh"
    source "$(dirname "$0")/_vector-concrete-elements-probe.sh"
    source "$(dirname "$0")/_vector-estimate-probe.sh"
    source "$(dirname "$0")/_vector-lerp-probe.sh"
    source "$(dirname "$0")/_vector-log-probe.sh"
    source "$(dirname "$0")/_vector-magnitude-probe.sh"
    source "$(dirname "$0")/_vector-number-probe.sh"
    source "$(dirname "$0")/_vector-as-probe.sh"
    source "$(dirname "$0")/_vector-alignment-probe.sh"
    source "$(dirname "$0")/_concrete-vector-reduction-probe.sh"
else
    echo 'vector coverage: representative full-driver parity'
    echo 'vector extended checks: skipped (not selected); not counted as passed'
    gate_extra_asserts() {
        gate_output witness "$1/native.stdout" '-- vector copy targets --' \
            '-- vector predicate/copy complete --' --count 1 --policy crlf
    }
fi

export SCALAR=1
# dn2cpp fixes Vector<T> at 128 bits. Keep the hardware-dependent real-.NET
# oracle at that width so exact output remains host-independent.
export DN2CPP_ORACLE_DOTNET_ENABLE_AVX=0
export DN2CPP_GATE_EXTRA_CONTEXT="dotnet-vector-width:128|explicit-raw-diet-retained-fill|block-repeat-low-bytes|vector-value-raw-diet|fixed-value-four-width-raw-diet|vector-unsupported-managed-storage-abi-raw-diet|concrete-elements-raw-diet-software-exceptions-authority|vector-number-selected-bits-fixed-managed-count-private-bridge-raw-rewritten-diet-authority|vector-magnitude-selected-bits-raw-rewritten-diet-authority|vector-log-typed-scalar-directed-mpfr-raw-rewritten-diet-authority|vector-lerp-nonfused-independent-oracle-raw-rewritten-diet-authority|vector-estimate-software-rounding-raw-diet-default-nohw-noavx2|generic-estimate-managed-count-guard-raw-diet-authority-il-negatives|fixed-generic-estimate-v64-descent-raw-diet-bridge-authority|guarded-as-source-target-order-raw-diet-context-authority|vector-alignment-real-il-15-property-bridges-raw-diet-authority|simd-alignment-retained-dim-raw-rewritten-diet-unshared-authority|concrete-reductions-real-il-padding-bits-raw-diet-authority|cli:$(_gate_cli_hash)"
export DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT}|vector-full:$DN2CPP_VECTOR_FULL"
export DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/_concrete-vector-reduction-probe.sh gates/ildiet_manifest.py gates/fixtures/ldftn-local/ConcreteVectorReductionFixture.cs gates/fixtures/ldftn-local/ConcreteVectorReductionGuards.cs gates/fixtures/ldftn-local/ConcreteVectorReductionParity.cs ${DN2CPP_CONCRETE_VECTOR_REDUCTION_FIXTURE_DLL:-} ${DN2CPP_CONCRETE_VECTOR_REDUCTION_COMPILER_DLL:-} gates/_vector-alignment-probe.sh gates/fixtures/ldftn-local/VectorAlignmentFixture.cs gates/fixtures/ldftn-local/VectorAlignmentGuards.cs gates/fixtures/ldftn-local/VectorAlignmentDimFixture.cs gates/fixtures/ldftn-local/VectorAlignmentDimGuards.cs ${DN2CPP_VECTOR_ALIGNMENT_FIXTURE_DLL:-} ${DN2CPP_VECTOR_ALIGNMENT_COMPILER_DLL:-} gates/_vector-as-probe.sh gates/fixtures/ldftn-local/VectorAsFixture.cs gates/fixtures/ldftn-local/VectorAsGuards.cs gates/fixtures/ldftn-local/VectorForwardingFixture.cs gates/fixtures/ldftn-local/VectorForwardingGuards.cs ${DN2CPP_VECTOR_AS_FIXTURE_DLL:-} ${DN2CPP_VECTOR_AS_COMPILER_DLL:-} gates/_vector-number-probe.sh gates/_vector-number-guards.sh gates/fixtures/ldftn-local/GenericVectorNumberGuards.cs gates/fixtures/ldftn-local/FixedVectorNumberGuards.cs gates/fixtures/vector-number gates/fixtures/ldftn-local/VectorMagnitudeFixture.cs gates/fixtures/ldftn-local/VectorMagnitudeGuards.cs ${DN2CPP_VECTOR_NUMBER_DLL:-} ${DN2CPP_VECTOR_NUMBER_FIXTURE_DLL:-} ${DN2CPP_VECTOR_NUMBER_COMPILER_DLL:-} gates/_vector-magnitude-probe.sh gates/_vector-magnitude-guards.sh gates/fixtures/vector-magnitude gates/fixtures/ldftn-local/VectorMagnitudeFixture.cs gates/fixtures/ldftn-local/VectorMagnitudeGuards.cs ${DN2CPP_VECTOR_MAGNITUDE_DLL:-} ${DN2CPP_VECTOR_MAGNITUDE_FIXTURE_DLL:-} ${DN2CPP_VECTOR_MAGNITUDE_COMPILER_DLL:-} gates/_vector-log-probe.sh gates/fixtures/vector-log gates/fixtures/ldftn-local/VectorLogFixture.cs gates/fixtures/ldftn-local/VectorLogGuards.cs ${DN2CPP_VECTOR_LOG_DLL:-} ${DN2CPP_VECTOR_LOG_FIXTURE_DLL:-} ${DN2CPP_VECTOR_LOG_COMPILER_DLL:-} gates/_vector-lerp-probe.sh gates/fixtures/vector-lerp gates/fixtures/ldftn-local/VectorLerpFixture.cs gates/fixtures/ldftn-local/VectorLerpGuards.cs ${DN2CPP_VECTOR_LERP_DLL:-} ${DN2CPP_VECTOR_LERP_FIXTURE_DLL:-} ${DN2CPP_VECTOR_LERP_COMPILER_DLL:-} gates/_vector-estimate-probe.sh gates/fixtures/ldftn-local/Program.cs gates/fixtures/ldftn-local/LdftnLocalFixture.csproj gates/fixtures/ldftn-local/GenericVectorEstimateFixture.cs gates/fixtures/ldftn-local/GenericVectorEstimateGuards.cs gates/fixtures/ldftn-local/FixedGenericVectorEstimateGuards.cs ${DN2CPP_GENERIC_VECTOR_ESTIMATE_FIXTURE_DLL:-} ${DN2CPP_GENERIC_VECTOR_ESTIMATE_COMPILER_DLL:-} gates/fixtures/vector-estimate ${DN2CPP_VECTOR_ESTIMATE_DLL:-} ${DN2CPP_VECTOR_ESTIMATE_FIXTURE_DLL:-} ${DN2CPP_VECTOR_ESTIMATE_COMPILER_DLL:-} gates/fixtures/ldftn-local/VectorEstimateGuards.cs gates/fixtures/ldftn-local/VectorEstimateFixture.cs gates/_vector-concrete-elements-probe.sh gates/fixtures/vector-concrete-elements ${DN2CPP_VECTOR_CONCRETE_ELEMENTS_DLL:-} ${DN2CPP_VECTOR_CONCRETE_ELEMENTS_FIXTURE_DLL:-} ${DN2CPP_VECTOR_CONCRETE_ELEMENTS_COMPILER_DLL:-} gates/fixtures/ldftn-local/ConcreteVectorElementsGuards.cs gates/fixtures/ldftn-local/ConcreteVectorElementsFixture.cs gates/_vector-fixed-value-probe.sh gates/fixtures/vector-fixed-value ${DN2CPP_FIXED_VECTOR_VALUE_FIXTURE_DLL:-} ${DN2CPP_FIXED_VECTOR_VALUE_COMPILER_DLL:-} gates/_vector-value-probe.sh gates/fixtures/vector-value gates/_vector-unsupported-probe.sh gates/fixtures/vector-unsupported ${DN2CPP_VECTOR_UNSUPPORTED_FIXTURE_DLL:-} ${DN2CPP_VECTOR_UNSUPPORTED_COMPILER_DLL:-} gates/_vector-explicit-probe.sh gates/fixtures/vector-explicit gates/fixtures/ldftn-local/VectorExplicitFixture.cs gates/fixtures/ldftn-local/VectorExplicitGuards.cs gates/fixtures/ldftn-local/VectorBlockFixture.cs"

corelib_diff_gate VectorProbe
