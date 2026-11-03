#!/usr/bin/env bash
# Real ArraySortHelper factories retain constrained generic activation and interface sorting.
array_sort_factory_gate() {
    local parent="$1" fixture="$2" corelib="$3" root cli python app axis out label
    cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    python=$(resolve_python) || return $?
    mkdir -p "$parent" || return $?
    root=$(mktemp -d "$parent/run-XXXXXX") || return $?
    gate_capture_ok "$root/guards" run_bounded dotnet "$fixture" --array-sort-factory-guards \
        "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$corelib" "$root/model.dll" || return $?
    gate_output match-record "$root/guards.stdout" 'array-sort-factory-guards:checks=[1-9][0-9]*' --count 1 --policy crlf || return $?
    build_proj samples/dotnet/ArraySortFactory/ArraySortFactory.csproj || return $?
    app="samples/dotnet/ArraySortFactory/bin/$CONFIG/$TFM/ArraySortFactory.dll"
    gate_capture_ok "$root/oracle" run_bounded dotnet "$app" || return $?
    for label in unary-int unary-novel-value unary-novel-reference unary-contravariant-reference \
        unary-noncomparable-fallback pair-unconstrained-value pair-noncomparable-key-fallback \
        factory-fresh-default-cached factory-address runtime-wrapper-one-direct-address-reflection \
        runtime-wrapper-two-direct-address-reflection; do
        gate_output byte-witness "$root/oracle.stdout" "array-sort-factory:$label:pass" --count 1 --policy crlf || return $?
    done
    gate_output witness "$root/oracle.stdout" '== ArraySortHelper genuine factories ==' \
        'ArraySortHelper genuine factories end' --count 1 --policy crlf || return $?
    [[ ! -s "$root/oracle.stderr" ]] || return 1
    gate_capture_ok "$root/generate" run_bounded dotnet "$fixture" --array-sort-factory-fixture \
        "$app" "$corelib" "$root/input.dll" || return $?
    for axis in raw diet; do
        out="$root/$axis"
        local flags=() runner=()
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        gate_capture_ok "$root/$axis-emit" invoke_cli "$root/input.dll" -r "$corelib" --auto-ref \
            ${flags[@]+"${flags[@]}"} -o "$out" || return $?
        if [[ "$axis" == diet ]]; then
            # resolve_python may return the Windows launcher "py -3".
            # shellcheck disable=SC2086
            $python - "$out/ildiet" "$root/input.dll" <<'PY_ARRAY_SORT_FACTORY_DIET' || return $?
import sys
from pathlib import Path
sys.path.insert(0, 'gates')
from ildiet_manifest import manifest
result, generation = manifest(sys.argv[1])
assert result.get('fallback') == 'none' and result.get('cutsValidated') == 'true'
assembly = next(a for a in result.findall('assembly') if a.get('name') == 'ArraySortFactory')
assert assembly.get('mode') == 'rewritten'
rewritten = Path(result.get('input'))
assert rewritten == generation / 'ArraySortFactory.dll'
assert rewritten.read_bytes() != Path(sys.argv[2]).read_bytes()
PY_ARRAY_SORT_FACTORY_DIET
        fi
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" ArraySortFactory || return $?
            runner=(node "$out/ArraySortFactory.js")
        else
            compile_console "$out" ArraySortFactory || return $?
            runner=("$out/ArraySortFactory$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        gate_output compare "$root/oracle.stdout" "$out/native.stdout" --policy crlf || return $?
        gate_output compare "$root/oracle.stderr" "$out/native.stderr" --policy crlf || return $?
    done
}
