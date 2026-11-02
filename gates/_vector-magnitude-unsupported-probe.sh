#!/usr/bin/env bash
# Unsupported magnitude and Number selectors preserve the managed exception and operand order.
probe_vector_magnitude_unsupported() {
    local root="$1" out profile axis emit
    local app="${DN2CPP_VECTOR_MAGNITUDE_UNSUPPORTED_DLL:-gates/fixtures/vector-magnitude-unsupported/bin/$CONFIG/$TFM/VectorMagnitudeUnsupported.dll}"
    if [ -z "${DN2CPP_VECTOR_MAGNITUDE_UNSUPPORTED_DLL:-}" ]; then
        build_gate_proj gates/fixtures/vector-magnitude-unsupported/VectorMagnitudeUnsupported.csproj || return 1
    fi
    mkdir -p "$root" || return 1
    out=$(mktemp -d "$root/magnitude-unsupported-XXXXXX") || return 1
    local clean=(env -u DOTNET_EnableHWIntrinsic -u DOTNET_EnableAVX -u DOTNET_EnableAVX2 -u DOTNET_EnableFMA
        -u COMPlus_EnableHWIntrinsic -u COMPlus_EnableAVX -u COMPlus_EnableAVX2 -u COMPlus_EnableFMA)
    for profile in default software; do
        local settings=()
        [ "$profile" != software ] || settings=(DOTNET_EnableHWIntrinsic=0)
        gate_capture_ok "$out/$profile" run_bounded "${clean[@]}" "${settings[@]}" dotnet "$app" || return 1
        [ ! -s "$out/$profile.stderr" ] || return 1
        gate_capture_ok "$out/$profile-capabilities" run_bounded "${clean[@]}" "${settings[@]}" dotnet "$app" --capabilities || return 1
        gate_capture_ok "$out/$profile-contract" python3 gates/fixtures/vector-magnitude-unsupported/check-output.py \
            "$out/default.stdout" --candidate "$out/$profile.stdout" || return 1
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
            python3 - "$emit/ildiet" "$app" <<'PYDIET' || return 1
import sys
from pathlib import Path
sys.path.insert(0, "gates")
from ildiet_manifest import manifest
result, generation = manifest(sys.argv[1])
assert result.get("fallback") == "none" and result.get("cutsValidated") == "true"
assembly = next(a for a in result.findall("assembly") if a.get("name") == "VectorMagnitudeUnsupported")
assert assembly.get("mode") == "rewritten", "fixture was copied, not rewritten"
rewritten = Path(result.get("input"))
assert rewritten == generation / "VectorMagnitudeUnsupported.dll"
assert rewritten.read_bytes() != Path(sys.argv[2]).read_bytes(), "fixture bytes did not change"
PYDIET
        fi
        if [ "${WASM:-0}" = 1 ]; then
            compile_console_wasm "$emit" VectorMagnitudeUnsupported || return 1
            runner=(node "$emit/VectorMagnitudeUnsupported.js")
        else
            compile_console "$emit" VectorMagnitudeUnsupported || return 1
            runner=("$emit/VectorMagnitudeUnsupported$EXE_EXT")
        fi
        gate_capture_ok "$out/$axis-run" run_bounded "${runner[@]}" || return 1
        [ ! -s "$out/$axis-run.stderr" ] || return 1
        gate_capture_ok "$out/$axis-contract" python3 gates/fixtures/vector-magnitude-unsupported/check-output.py \
            "$out/default.stdout" --candidate "$out/$axis-run.stdout" || return 1
    done
}
