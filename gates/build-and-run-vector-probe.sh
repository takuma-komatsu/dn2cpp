#!/usr/bin/env bash
# Regression gate for the portable-SIMD lowering of System.Runtime.Intrinsics /
# System.Numerics vectors (Vector128/256, Vector<T>). The transpiler emits no
# hardware intrinsics — the SIMD gates (Vector128.IsHardwareAccelerated, …) fold to
# a token the dn2cpp_vec_* layer resolves, and the default build backs those ops
# with Google Highway (runtime/core/dn2cpp_vectors_hwy.h). VectorProbe exercises a
# spread of those ops (abs / copysign / cos / exp / hypot / reductions over float +
# int lanes) and the output is diffed exact vs real .NET, so the lowering stays
# byte-faithful. QuaternionPlaneReinterprets covers the 16-byte System.Numerics
# struct <-> Vector128<float> reinterprets (AsVector128 / AsQuaternion / AsPlane)
# behind Quaternion/Plane Equals and the lane-wise ops. VectorCopyTargets covers
# both the fixed-width static-extension and System.Numerics instance CopyTo
# signatures over arrays, offsets, and spans. The scalar-axis twin
# (build-and-run-vector-probe-scalar.sh) covers the scalar emulation. The Ascii/UTF
# transcode + JSON paths reach this lowering indirectly; this is its direct,
# focused cover.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/vector-explicit-probe.sh"
source "$(dirname "$0")/vector-value-probe.sh"
source "$(dirname "$0")/vector-unsupported-probe.sh"
source "$(dirname "$0")/vector-fixed-value-probe.sh"

# dn2cpp fixes Vector<T> at 128 bits. Keep the hardware-dependent real-.NET
# oracle at that width so exact output remains host-independent.
export DN2CPP_ORACLE_DOTNET_ENABLE_AVX=0
export DN2CPP_GATE_EXTRA_CONTEXT="dotnet-vector-width:128|explicit-raw-diet-retained-fill|block-repeat-low-bytes|vector-value-raw-diet|fixed-value-four-width-raw-diet|vector-unsupported-managed-storage-abi-raw-diet|cli:$(_gate_cli_hash)"
export DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/vector-fixed-value-probe.sh gates/fixtures/vector-fixed-value ${DN2CPP_FIXED_VECTOR_VALUE_FIXTURE_DLL:-} ${DN2CPP_FIXED_VECTOR_VALUE_COMPILER_DLL:-} gates/vector-value-probe.sh gates/fixtures/vector-value gates/vector-unsupported-probe.sh gates/fixtures/vector-unsupported ${DN2CPP_VECTOR_UNSUPPORTED_FIXTURE_DLL:-} ${DN2CPP_VECTOR_UNSUPPORTED_COMPILER_DLL:-} gates/vector-explicit-probe.sh gates/fixtures/vector-explicit gates/fixtures/ldftn-local/VectorExplicitFixture.cs gates/fixtures/ldftn-local/VectorExplicitGuards.cs gates/fixtures/ldftn-local/VectorBlockFixture.cs"

corelib_diff_gate VectorProbe
