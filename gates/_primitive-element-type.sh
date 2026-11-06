#!/usr/bin/env bash
# The element-code predicate keeps the input IL's masked shifts, independently of Type identity.
primitive_element_contract_asserts() {
    local output="$1" label
    gate_output witness "$output" '== Primitive element type contract ==' --count 1 --policy crlf || return $?
    for label in static-primitive static-nonprimitive public-types; do
        gate_output witness "$output" "primitive-element-$label:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'Primitive element type contract end' --count 1 --policy crlf
}

primitive_element_asserts() {
    local output="$1" mode="$2" route bits='' i
    local routes=(reflection)
    for ((i=0; i<8; i++)); do bits+='00111111111111000000000011000000'; done
    if [[ "$mode" == all ]]; then
        primitive_element_contract_asserts "$output" || return $?
        routes+=(direct calli)
        gate_output witness "$output" 'primitive-element-stack-normalization:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'Primitive element type routes end' --count 1 --policy crlf || return $?
    fi
    for route in "${routes[@]}"; do
        gate_output witness "$output" "primitive-element-$route-bits:$bits" --count 1 --policy crlf || return $?
        gate_output witness "$output" "primitive-element-$route-count:True" --count 1 --policy crlf || return $?
        gate_output witness "$output" "primitive-element-$route-aliases:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'primitive-element-reflection-row:True' --count 1 --policy crlf || return $?
    gate_output witness "$output" 'Primitive element type reflection end' --count 1 --policy crlf
}

primitive_element_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out oracle flag
    shift
    mkdir -p "$root"
    for mode in all reflection; do
        flag=--primitive-element
        [[ "$mode" != reflection ]] || flag+=-reflection
        oracle="$root/oracle-$mode"
        mkdir -p "$oracle"
        cp "$original/"* "$oracle/" || return $?
        dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
            "$flag-oracle" "$oracle/SelfHostPrimSubset.dll" "$(resolve_net10_corelib)" || return $?
        # Only the inspected token-free copy executes on CLR; private helper routes are native-only.
        gate_capture_ok "$oracle/result" run_bounded dotnet "$oracle/SelfHostPrimSubset.dll" || return $?
        primitive_element_asserts "$oracle/result.stdout" "$mode" || return $?
        input="$root/input-$mode"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
            "$flag" "$input/SelfHostPrimSubset.dll" "$(resolve_net10_corelib)" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"
            local flags=("$@")
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" PrimitiveElementType || return $?
                runner=(node "$out/PrimitiveElementType.js")
            else
                compile_console "$out" PrimitiveElementType || return $?
                runner=("$out/PrimitiveElementType$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            primitive_element_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$oracle/result.stdout" "$out/native.stdout" --policy crlf || return $?
        done
    done
}
