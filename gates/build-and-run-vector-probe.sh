#!/usr/bin/env bash
# Regression gate for the portable-SIMD lowering of System.Runtime.Intrinsics /
# System.Numerics vectors (Vector64/128/256/512, Vector<T>). The transpiler emits no
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
# FloatingPoint covers IEEE NaN/zero ordering, division and fused rounding in
# every lane; the native fixture selects both available FMA implementations.
# MultiplyAddEstimate, Lerp, Log, magnitude selection, Vector.As and Vector2/3
# reductions run as further sections, and the native run must take the nonfused
# estimate profile. Malformed CoreLib references at the vector admission
# boundaries refuse with exit status 2.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_vector-floating-point.sh"
source "$(dirname "$0")/_vector-corelib-checks.sh"

# dn2cpp fixes Vector<T> at 128 bits. Keep the hardware-dependent real-.NET
# oracle at that width so exact output remains host-independent.
export DN2CPP_ORACLE_DOTNET_ENABLE_AVX=0
export DN2CPP_GATE_EXTRA_CONTEXT="dotnet-vector-width:128|floating-point-prefix-and-witnesses|concrete-elements:DOTNET_EnableHWIntrinsic=0"
export DN2CPP_GATE_EXTRA_INPUTS="gates/_vector-floating-point.sh gates/_vector-corelib-checks.sh"

source "$(dirname "$0")/_vector-regression-checks.sh"

corelib_diff_gate VectorProbe
vector_section_witnesses "$_CG_OUT/native.stdout"
vector_concrete_elements_gate "$_CG_OUT" "$_CG_APP"
# A CLR may fuse MultiplyAddEstimate, so only the native nonfused profile is asserted.
gate_output witness "$_CG_OUT/native.stderr" 'vector estimate nonfused profile passed' --count 1 --policy crlf
# Sections after FloatingPoint leave the earlier output an unchanged prefix and
# all run to the final marker.
DOTNET_EnableAVX=0 gate_capture_ok "$_CG_OUT/before-explicit" run_bounded dotnet "$_CG_APP" before-explicit
gate_output prefix "$_CG_OUT/native.stdout" "$_CG_OUT/before-explicit.stdout" \
    --before '-- vector explicit reinterprets --' --policy crlf
vector_fma_branches "$_CG_APP"

# Each malformed reference must refuse. The numeric alignment control proves
# admission and emitted callable guards, not runtime memory semantics.
# Backend-independent, so only this gate runs these checks.
vector_refusals() {
    local out=artifacts/vector-refusals
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj
    mkdir -p "$out"
    if gate_cache_check "$out" "vector-refusals|$_CG_CORELIB|cli:$(_gate_cli_hash)" \
            "$fixture" gates/fixtures/vector-alignment/check-generated.py; then
        gate_cache_hit_msg
        return 0
    fi
    local mode expected run
    run=$(mktemp -d "$out/run.XXXXXX")
    while IFS='|' read -r mode expected; do
        gate_capture_ok "$run/$mode-generate" run_bounded dotnet "$fixture" --vector-refusal "$run/$mode.dll" "$_CG_CORELIB" "$mode"
        gate_capture "$run/$mode" invoke_cli "$run/$mode.dll" -r "$_CG_CORELIB" --no-ildiet -o "$run/$mode-emit"
        gate_output status "$(cat "$run/$mode.status")" 2
        grep -F -- "$expected" "$run/$mode.stderr" >/dev/null \
            || { echo "FAIL: vector refusal $mode lacks: $expected" >&2; return 1; }
    done <<'MODES'
explicit|Vector<T>.op_Explicit requires its exact supported CoreLib reference
block|Vector Create repetition requires a supported same-element fixed-width generic overload
value|Vector<T> value methods require an exact supported closed CoreLib declaration and unmodified call signature
unsupported|Vector unsupported-type guard requires its exact closed CoreLib declaration and unmodified MethodSpec
element|Concrete vector element access requires its exact CoreLib reference
estimate|Vector MultiplyAddEstimate requires its exact floating-point CoreLib reference
log|Vector Log requires its exact floating-point CoreLib reference
lerp|Vector Lerp requires a nongeneric floating-point declaration
magnitude|Vector magnitude selection requires an exact floating-point generic CoreLib declaration and method specification
alignment|LoadAligned: ISimdVector.Alignment requires its exact CoreLib slot, numeric closure and concrete managed MethodImpl
alignment-helper|Fixture.Program.Main: ISimdVector.Alignment requires its exact CoreLib slot, numeric closure and concrete managed MethodImpl
as|Vector reinterpretation requires an exact closed CoreLib declaration and unmodified MethodSpec
reduction|Concrete vector reduction requires an exact nongeneric CoreLib declaration and unmodified reference
numerics-alignment|Vector.get_Alignment requires its exact CoreLib reference
MODES
    gate_capture_ok "$run/alignment-valid-generate" run_bounded dotnet "$fixture" \
        --vector-refusal "$run/alignment-valid.dll" "$_CG_CORELIB" alignment-valid
    gate_capture_ok "$run/alignment-valid" invoke_cli "$run/alignment-valid.dll" \
        -r "$_CG_CORELIB" --no-ildiet -o "$run/alignment-valid-emit"
    gate_python gates/fixtures/vector-alignment/check-generated.py "$run/alignment-valid-emit"
    gate_cache_commit
}
vector_refusals
