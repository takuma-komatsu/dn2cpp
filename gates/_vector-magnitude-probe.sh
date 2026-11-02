#!/usr/bin/env bash
# Vector magnitude selection uses exact IEEE bit-order and callable identity.
probe_vector_magnitude() {
    local root="$1" out profile axis emit
    local app="${DN2CPP_VECTOR_MAGNITUDE_DLL:-gates/fixtures/vector-magnitude/bin/$CONFIG/$TFM/VectorMagnitude.dll}"
    if [ -z "${DN2CPP_VECTOR_MAGNITUDE_DLL:-}" ]; then
        build_gate_proj gates/fixtures/vector-magnitude/VectorMagnitude.csproj || return 1
    fi
    mkdir -p "$root" || return 1
    out=$(mktemp -d "$root/magnitude-XXXXXX") || return 1
    local clean=(env -u DOTNET_EnableHWIntrinsic -u DOTNET_EnableAVX -u DOTNET_EnableAVX2 -u DOTNET_EnableFMA
        -u COMPlus_EnableHWIntrinsic -u COMPlus_EnableAVX -u COMPlus_EnableAVX2 -u COMPlus_EnableFMA)
    for profile in default software; do
        local settings=()
        [ "$profile" != software ] || settings=(DOTNET_EnableHWIntrinsic=0)
        gate_capture_ok "$out/$profile" run_bounded "${clean[@]}" "${settings[@]}" dotnet "$app" || return 1
        [ ! -s "$out/$profile.stderr" ] || return 1
        gate_capture_ok "$out/$profile-contract" python3 gates/fixtures/vector-magnitude/check-oracles.py \
            "$out/$profile.stdout" --profile "$profile" || return 1
    done
    for axis in raw diet; do
        emit=$(mktemp -d "$out/$axis-XXXXXX") || return 1
        local flags=() runner=()
        [ "$axis" != raw ] || flags+=(--no-ildiet)
        gate_capture_ok "$out/$axis-emit" invoke_cli "$app" -r "$_CG_CORELIB" --auto-ref "${flags[@]}" -o "$emit" || return 1
        if [ "$axis" = diet ]; then
            python3 - "$emit/ildiet" "$app" <<'PYDIET' || return 1
import sys
from pathlib import Path
sys.path.insert(0, "gates")
from ildiet_manifest import manifest
result, generation = manifest(sys.argv[1])
assert result.get("fallback") == "none" and result.get("cutsValidated") == "true"
assembly = next(a for a in result.findall("assembly") if a.get("name") == "VectorMagnitude")
assert assembly.get("mode") == "rewritten", "fixture was copied, not rewritten"
rewritten = Path(result.get("input"))
assert rewritten == generation / "VectorMagnitude.dll"
assert rewritten.read_bytes() != Path(sys.argv[2]).read_bytes(), "fixture bytes did not change"
PYDIET
        fi
        if [ "${WASM:-0}" = 1 ]; then
            compile_console_wasm "$emit" VectorMagnitude || return 1
            runner=(node "$emit/VectorMagnitude.js")
        else
            compile_console "$emit" VectorMagnitude || return 1
            runner=("$emit/VectorMagnitude$EXE_EXT")
        fi
        gate_capture_ok "$out/$axis-run" run_bounded "${runner[@]}" || return 1
        [ ! -s "$out/$axis-run.stderr" ] || return 1
        gate_capture_ok "$out/$axis-contract" python3 gates/fixtures/vector-magnitude/check-oracles.py \
            "$out/$axis-run.stdout" --profile native || return 1
    done
    source "$_DN2CPP_GATE_ROOT/gates/_vector-magnitude-guards.sh" || return 1
    vector_magnitude_authority_probe "$out/authority" || return 1
    source "$_DN2CPP_GATE_ROOT/gates/_vector-magnitude-unsupported-probe.sh" || return 1
    probe_vector_magnitude_unsupported "$out"
}
