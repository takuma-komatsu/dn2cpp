#!/usr/bin/env bash
# The private delegate dispatcher preserves state, token identity and exceptions.
cancellation_invoke_gate() {
    local parent="$1" fixture="$2" corelib="$3" root cli axis out round
    cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    gate_python -c 'pass' >/dev/null || return $?
    mkdir -p "$parent" || return $?
    root=$(mktemp -d "$parent/run-XXXXXX") || return $?
    gate_capture_ok "$root/guards" run_bounded dotnet "$fixture" --cancellation-invoke-guards \
        "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$(dirname "$cli")/ildiet/ILDiet.dll" "$corelib" "$root/model.dll" routes || return $?
    gate_output match-record "$root/guards.stdout" 'cancellation-invoke-guards:checks=[1-9][0-9]*' --count 1 --policy crlf || return $?
    gate_capture_ok "$root/generate" run_bounded dotnet "$fixture" --cancellation-invoke-fixture "$root/input.dll" "$corelib" || return $?
    gate_capture_ok "$root/oracle" run_bounded dotnet "$fixture" --cancellation-invoke-oracle || return $?
    [[ "$(wc -l < "$root/oracle.stdout")" -eq 32 && ! -s "$root/oracle.stderr" ]] || return 1
    gate_capture_ok "$root/cold-oracle" run_bounded dotnet "$fixture" --cancellation-cold-first-use-oracle || return $?
    gate_output witness "$root/cold-oracle.stdout" 'cancellation-invoke:ctor:bool-true:cold-first-use:pass' --count 1 --policy crlf || return $?
    [[ "$(wc -l < "$root/cold-oracle.stdout")" -eq 1 && ! -s "$root/cold-oracle.stderr" ]] || return 1
    for axis in raw diet; do
        out="$root/$axis"
        local flags=() runner=()
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        gate_capture_ok "$root/$axis-emit" invoke_cli "$root/input.dll" -r "$corelib" --auto-ref ${flags[@]+"${flags[@]}"} -o "$out" || return $?
        if [[ "$axis" == diet ]]; then
            gate_python - "$out/ildiet" "$root/input.dll" <<'PY_CANCELLATION_DIET' || return $?
import sys
from pathlib import Path
sys.path.insert(0, 'gates')
from ildiet_manifest import manifest
result, generation = manifest(sys.argv[1])
assert result.get('fallback') == 'none' and result.get('cutsValidated') == 'true'
assembly = next(a for a in result.findall('assembly') if a.get('name') == 'CancellationInvoke')
assert assembly.get('mode') == 'rewritten'
rewritten = Path(result.get('input'))
assert rewritten == generation / 'CancellationInvoke.dll'
assert rewritten.read_bytes() != Path(sys.argv[2]).read_bytes()
PY_CANCELLATION_DIET
        fi
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" CancellationInvoke || return $?
            runner=(node "$out/CancellationInvoke.js")
        else
            compile_console "$out" CancellationInvoke || return $?
            runner=("$out/CancellationInvoke$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        gate_output compare "$root/oracle.stdout" "$out/native.stdout" --policy crlf || return $?
        gate_output compare "$root/oracle.stderr" "$out/native.stderr" --policy crlf || return $?
        # A new process is required: the default witness already warmed the source.
        for round in 1 2 3 4 5 6 7 8; do
            gate_capture_ok "$out/cold-$round" run_bounded "${runner[@]}" cold-first-use || return $?
            gate_output compare "$root/cold-oracle.stdout" "$out/cold-$round.stdout" --policy crlf || return $?
            gate_output compare "$root/cold-oracle.stderr" "$out/cold-$round.stderr" --policy crlf || return $?
        done
    done
}
