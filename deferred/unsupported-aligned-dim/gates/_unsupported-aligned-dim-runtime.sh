#!/usr/bin/env bash
probe_unsupported_aligned_dim_runtime() {
    local out="$1" fixture="$2" axis kind emit
    local checker=gates/fixtures/ldftn-local/check-unsupported-aligned-dim.py
    gate_capture_ok "$out/unsupported-generate" run_bounded dotnet "$fixture" --unsupported-aligned-dim-fixture "$out/unsupported.dll" "$_CG_CORELIB" || return 1
    gate_capture_ok "$out/retained-generate" run_bounded dotnet "$fixture" --unsupported-aligned-dim-fixture "$out/retained.dll" "$_CG_CORELIB" retained-only || return 1
    gate_capture_ok "$out/unsupported-oracle" run_bounded env -u DOTNET_EnableHWIntrinsic -u COMPlus_EnableHWIntrinsic -u DOTNET_EnableAVX -u COMPlus_EnableAVX dotnet "$fixture" --unsupported-aligned-dim-oracle || return 1
    gate_capture_ok "$out/unsupported-oracle-software" run_bounded env -u COMPlus_EnableHWIntrinsic -u DOTNET_EnableAVX -u COMPlus_EnableAVX DOTNET_EnableHWIntrinsic=0 dotnet "$fixture" --unsupported-aligned-dim-oracle || return 1
    gate_capture_ok "$out/unsupported-oracle-128" run_bounded env -u DOTNET_EnableHWIntrinsic -u COMPlus_EnableHWIntrinsic DOTNET_EnableAVX=0 COMPlus_EnableAVX=0 dotnet "$fixture" --unsupported-aligned-dim-oracle || return 1
    local profile
    for profile in oracle oracle-software oracle-128; do
        [ ! -s "$out/unsupported-$profile.stderr" ] || return 1
        gate_capture_ok "$out/unsupported-$profile-check" python3 "$checker" "$out/unsupported-$profile.stdout" || return 1
    done
    gate_output compare "$out/unsupported-oracle-software.stdout" "$out/unsupported-oracle-128.stdout" --policy crlf || return 1
    # The default CLR may choose a wider Numerics target. The aligned16-not32
    # case deliberately distinguishes that profile; native uses supported byte Count.
    for axis in raw diet raw-unshared diet-unshared; do
        local flags=() runner=()
        [[ "$axis" = diet* ]] || flags+=(--no-ildiet)
        [[ "$axis" != *-unshared ]] || flags+=(--no-shared-generics)
        for kind in unsupported retained; do
            emit=$(mktemp -d "$out/$kind-$axis-XXXXXX") || return 1
            DN2CPP_STRICT_COMPLETION=1 gate_capture_ok "$out/$kind-$axis-emit" invoke_cli "$out/$kind.dll" -r "$_CG_CORELIB" --auto-ref "${flags[@]}" -o "$emit" || return 1
            if [[ "$axis" = diet* ]]; then
                gate_capture_ok "$out/$kind-$axis-diet" python3 - "$emit/ildiet" "$out/$kind.dll" "$out/$kind-$axis-diet-input.path" <<'PYUNSUPPORTED' || return 1
import hashlib, json, sys
from pathlib import Path
sys.path.insert(0, "gates")
from ildiet_manifest import manifest
result, generation = manifest(sys.argv[1])
assembly = next(a for a in result.findall("assembly") if a.get("name") == "UnsupportedAlignedDim")
rewritten = Path(result.get("input"))
assert rewritten == generation / "UnsupportedAlignedDim.dll"
original, output = Path(sys.argv[2]).read_bytes(), rewritten.read_bytes()
Path(sys.argv[3]).write_text(str(rewritten) + "\n")
assert result.get("fallback") == "none" and result.get("cutsValidated") == "true"
assert assembly.get("mode") == "rewritten"
print(json.dumps({"mode": assembly.get("mode"), "fallback": result.get("fallback"),
                  "input_sha256": hashlib.sha256(original).hexdigest(), "output_sha256": hashlib.sha256(output).hexdigest(),
                  "bytes_changed": original != output, "remapped_token_coverage_claimed": False}, sort_keys=True))
PYUNSUPPORTED
                gate_capture_ok "$out/$kind-$axis-input-check" run_bounded dotnet "$fixture" --unsupported-aligned-dim-inspect "$(cat "$out/$kind-$axis-diet-input.path")" || return 1
            fi
            gate_capture_ok "$out/$kind-$axis-bodies" python3 gates/fixtures/ldftn-local/check-unsupported-aligned-dim-bodies.py "$emit" || return 1
            if [ "${WASM:-0}" = 1 ]; then
                compile_console_wasm "$emit" UnsupportedAlignedDim || return 1
                runner=(node "$emit/UnsupportedAlignedDim.js")
            else
                compile_console "$emit" UnsupportedAlignedDim || return 1
                runner=("$emit/UnsupportedAlignedDim$EXE_EXT")
            fi
            gate_capture_ok "$out/$kind-$axis-run" run_bounded "${runner[@]}" || return 1
            [ ! -s "$out/$kind-$axis-run.stderr" ] || return 1
            if [ "$kind" = retained ]; then
                gate_capture_ok "$out/$kind-$axis-check" python3 "$checker" "$out/$kind-$axis-run.stdout" --retained-only || return 1
            else
                cat "$out/dim-$axis-run.stdout" "$out/$kind-$axis-run.stdout" > "$out/$kind-$axis-combined.stdout" || return 1
                gate_capture_ok "$out/$kind-$axis-check" python3 "$checker" "$out/$kind-$axis-combined.stdout" \
                    --prefix "$out/dim-oracle.stdout" --oracle "$out/unsupported-oracle-software.stdout" || return 1
            fi
        done
    done
}
