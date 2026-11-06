#!/usr/bin/env bash
# Number selectors preserve IEEE bits and real unsupported-type guards across callable routes.
probe_vector_number() {
    local root="$1" out profile axis emit
    local app="${DN2CPP_VECTOR_NUMBER_DLL:-gates/fixtures/vector-number/bin/$CONFIG/$TFM/VectorNumber.dll}"
    if [ -z "${DN2CPP_VECTOR_NUMBER_DLL:-}" ]; then
        build_gate_proj gates/fixtures/vector-number/VectorNumber.csproj || return 1
    fi
    mkdir -p "$root" || return 1
    out=$(mktemp -d "$root/number-XXXXXX") || return 1
    local clean=(env -u DOTNET_EnableHWIntrinsic -u DOTNET_EnableAVX -u DOTNET_EnableAVX2 -u DOTNET_EnableFMA
        -u COMPlus_EnableHWIntrinsic -u COMPlus_EnableAVX -u COMPlus_EnableAVX2 -u COMPlus_EnableFMA)
    for profile in default software; do
        local settings=()
        [ "$profile" != software ] || settings=(DOTNET_EnableHWIntrinsic=0)
        gate_capture_ok "$out/$profile" run_bounded "${clean[@]}" "${settings[@]}" dotnet "$app" || return 1
        [ ! -s "$out/$profile.stderr" ] || return 1
        gate_capture_ok "$out/$profile-contract" gate_python gates/fixtures/vector-number/check-oracles.py \
            "$out/$profile.stdout" --profile "$profile" --guard-oracle "$out/default.stdout" || return 1
        gate_output byte-witness "$out/$profile.stdout" 'vector-number:all-routes-passed' \
            'vector-number:managed-guards:start' 'vector-number:managed-guards-passed' || return 1
        gate_capture_ok "$out/$profile-capabilities" run_bounded "${clean[@]}" "${settings[@]}" dotnet "$app" --capabilities || return 1
        [ ! -s "$out/$profile-capabilities.stderr" ] || return 1
    done
    for profile in numerics vector64 vector128 vector256 vector512; do
        grep -Fx "$profile-hardware=False" "$out/software-capabilities.stdout" >/dev/null || return 1
    done
    for axis in raw diet; do
        emit=$(mktemp -d "$out/$axis-XXXXXX") || return 1
        local flags=() runner=()
        [ "$axis" != raw ] || flags+=(--no-ildiet)
        gate_capture_ok "$out/$axis-emit" invoke_cli "$app" -r "$_CG_CORELIB" --auto-ref "${flags[@]}" -o "$emit" || return 1
        if [ "$axis" = diet ]; then
            gate_capture_ok "$out/$axis-publication" gate_python - "$emit/ildiet" "$app" <<'PYDIET' || return 1
import hashlib
import json
import sys
from pathlib import Path
sys.path.insert(0, "gates")
from ildiet_manifest import manifest
result, generation = manifest(sys.argv[1])
assert result.get("fallback") == "none" and result.get("cutsValidated") == "true"
assembly = next(a for a in result.findall("assembly") if a.get("name") == "VectorNumber")
rewritten = Path(result.get("input"))
assert rewritten == generation / "VectorNumber.dll"
original_bytes, output_bytes = Path(sys.argv[2]).read_bytes(), rewritten.read_bytes()
changed = original_bytes != output_bytes
print(json.dumps({"mode": assembly.get("mode"), "fallback": result.get("fallback"),
                  "input_sha256": hashlib.sha256(original_bytes).hexdigest(),
                  "output_sha256": hashlib.sha256(output_bytes).hexdigest(), "bytes_changed": changed,
                  "remapped_token_coverage_claimed": False}, sort_keys=True))
assert assembly.get("mode") == "rewritten", "fixture was copied, not rewritten"
assert changed, "fixture bytes did not change; no remapped-token coverage"
PYDIET
        fi
        if [ "${WASM:-0}" = 1 ]; then
            compile_console_wasm "$emit" VectorNumber || return 1
            runner=(node "$emit/VectorNumber.js")
        else
            compile_console "$emit" VectorNumber || return 1
            runner=("$emit/VectorNumber$EXE_EXT")
        fi
        gate_capture_ok "$out/$axis-run" run_bounded "${runner[@]}" || return 1
        [ ! -s "$out/$axis-run.stderr" ] || return 1
        gate_capture_ok "$out/$axis-contract" gate_python gates/fixtures/vector-number/check-oracles.py \
            "$out/$axis-run.stdout" --profile native --guard-oracle "$out/default.stdout" || return 1
        gate_output byte-witness "$out/$axis-run.stdout" 'vector-number:all-routes-passed' \
            'vector-number:managed-guards:start' 'vector-number:managed-guards-passed' || return 1
    done
    source "$_DN2CPP_GATE_ROOT/gates/_vector-number-guards.sh" || return 1
    vector_number_authority_probe "$out/authority"
}
