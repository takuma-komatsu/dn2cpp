#!/usr/bin/env bash
# Full-byte operators and real retained Span.Fill, separate from direct bulk lowering.
vector_explicit_probe() {
    local out="$1" oracle="$2" input="$1/explicit-input" axis emit
    mkdir -p "$input"
    cp "$_CG_APP" "${_CG_APP%.dll}.runtimeconfig.json" "${_CG_APP%.dll}.deps.json" "$input/"
    build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return 1
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    gate_capture_ok "$out/explicit-rewrite" run_bounded dotnet "$fixture" --vector-explicit "$input/VectorProbe.dll" "$_CG_CORELIB" || return 1
    gate_capture_ok "$out/explicit-patched-oracle" run_bounded env DOTNET_EnableAVX=0 dotnet "$input/VectorProbe.dll" || return 1
    gate_output compare "$out/explicit-patched-oracle.stdout" "$oracle" || return 1
    gate_capture_ok "$out/explicit-guards" run_bounded dotnet "$fixture" --vector-explicit-guards \
        "src/Dn2Cpp.Transpiler/bin/$CONFIG/$TFM/Dn2Cpp.Transpiler.dll" "$_CG_CORELIB" "$input/VectorProbe.dll" || return 1
    for axis in raw diet; do
        emit="$out/explicit-$axis"
        local flags=()
        [ "$axis" != raw ] || flags+=(--no-ildiet)
        gate_capture_ok "$out/explicit-$axis-emit" invoke_cli "$input/VectorProbe.dll" --auto-ref --link-xml gates/fixtures/vector-explicit/retained.xml "${flags[@]}" -o "$emit" || return 1
        gate_capture_ok "$out/explicit-$axis-generated" python3 gates/fixtures/vector-explicit/check-generated.py "$emit" || return 1
        compile_console "$emit" VectorProbe || return 1
        gate_capture_ok "$out/explicit-$axis-run" run_bounded "./$emit/VectorProbe$EXE_EXT" || return 1
        gate_output compare "$out/explicit-$axis-run.stdout" "$oracle" || return 1
        local hardware=1; [ -z "${SCALAR:-}" ] || hardware=0
        [ "$(grep -Ec "^vector explicit callable and retained fill witnesses passed hw=$hardware pointer=(4|8)$" "$out/explicit-$axis-run.stderr")" = 1 ] || return 1
    done
    local mode negative
    for mode in instance vararg generic byref-input byref-result extra modopt wrong-source wrong-result bool char enum decimal custom open fixed-owner address-bool address-wrong-source address-byref-result; do
        negative="$out/explicit-negative-$mode"; mkdir -p "$negative"
        cp "$_CG_APP" "$negative/VectorProbe.dll"
        gate_capture_ok "$out/explicit-$mode-rewrite" run_bounded dotnet "$fixture" --vector-explicit "$negative/VectorProbe.dll" "$_CG_CORELIB" "$mode" || return 1
        gate_capture "$out/explicit-$mode-reject" invoke_cli "$negative/VectorProbe.dll" --auto-ref --no-ildiet -o "$negative/raw" || return 1
        gate_output status "$(cat "$out/explicit-$mode-reject.status")" 2 || return 1
        grep -E 'op_Explicit|generic parameter|generic type|Type argument' "$out/explicit-$mode-reject.stderr" >/dev/null || return 1
    done
    for mode in cross-element equal-width portable-source portable-result method-element instance bool; do
        negative="$out/block-negative-$mode"; mkdir -p "$negative"
        cp "$_CG_APP" "$negative/VectorProbe.dll"
        gate_capture_ok "$out/block-$mode-rewrite" run_bounded dotnet "$fixture" --vector-block-negative "$negative/VectorProbe.dll" "$_CG_CORELIB" "$mode" || return 1
        gate_capture "$out/block-$mode-reject" invoke_cli "$negative/VectorProbe.dll" --auto-ref --no-ildiet -o "$negative/raw" || return 1
        gate_output status "$(cat "$out/block-$mode-reject.status")" 2 || return 1
        grep -F 'Vector Create repetition requires' "$out/block-$mode-reject.stderr" >/dev/null || return 1
    done
}

gate_extra_asserts() {
    local out="$1"
    gate_capture_ok "$out/explicit-before" run_bounded env DOTNET_EnableAVX=0 dotnet "$_CG_APP" before-explicit || return 1
    gate_output select "$out/oracle.stdout" "$out/explicit-prefix.stdout" --before '-- vector explicit reinterprets --' --policy crlf || return 1
    gate_output compare "$out/explicit-prefix.stdout" "$out/explicit-before.stdout" || return 1
    gate_output witness "$out/oracle.stdout" 'explicit byte checks=1013' --count 1 --policy crlf || return 1
    gate_output witness "$out/oracle.stdout" 'block repeat and low-byte checks=24' --count 1 --policy crlf || return 1
    gate_output witness "$out/oracle.stdout" 'retained fill byte and reference canaries passed' --count 1 --policy crlf || return 1
    vector_explicit_probe "$out" "$out/oracle.stdout" || return 1
    vector_value_probe "$out" "$out/oracle.stdout" || return 1
    vector_concrete_elements_prefix "$out" || return 1
    vector_concrete_elements_probe "$out"
}
