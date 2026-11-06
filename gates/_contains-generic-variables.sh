#!/usr/bin/env bash
# Retained generic-variable queries preserve recursive type identity and checked handles.
contains_generic_route_asserts() {
    local output="$1" route="$2" label i
    for label in type-formal method-formal recursive-formal definition nested-definition open-constructed nested-constructed repeated; do
        gate_output witness "$output" "contains-generic-$route-$label:True" --count 1 --policy crlf || return $?
    done
    for ((i=0; i<9; i++)); do
        gate_output witness "$output" "contains-generic-$route-open-element-$i:True" --count 1 --policy crlf || return $?
    done
    for ((i=0; i<11; i++)); do
        gate_output witness "$output" "contains-generic-$route-closed-$i:True" --count 1 --policy crlf || return $?
    done
}

contains_generic_contract_asserts() {
    local output="$1"
    gate_output witness "$output" '== Contains generic variables contract ==' --count 1 --policy crlf || return $?
    contains_generic_route_asserts "$output" public || return $?
    gate_output witness "$output" 'contains-generic-public-null:True' --count 1 --policy crlf || return $?
    gate_output witness "$output" 'Contains generic variables contract end' --count 1 --policy crlf
}

contains_generic_asserts() {
    local output="$1" mode="$2" route
    if [[ "$mode" == handle ]]; then
        contains_generic_contract_asserts "$output" || return $?
        contains_generic_route_asserts "$output" retained-getter || return $?
        gate_output witness "$output" 'contains-generic-retained-getter-null:True' --count 1 --policy crlf || return $?
    fi
    local routes=(reflection)
    if [[ "$mode" == handle ]]; then routes+=(direct pointer delegate); fi
    for route in "${routes[@]}"; do
        contains_generic_route_asserts "$output" "$route" || return $?
        gate_output witness "$output" "contains-generic-$route-default:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'Contains generic variables reflection end' --count 1 --policy crlf || return $?
    if [[ "$mode" == handle ]]; then
        gate_output witness "$output" 'Contains generic variables handle routes end' --count 1 --policy crlf || return $?
    fi
}

contains_generic_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out
    shift
    mkdir -p "$root"
    for mode in handle reflection; do
        gate_capture_ok "$root/$mode-oracle" run_bounded dotnet "$original/SelfHostPrimSubset.dll" "contains-generic-$mode" || return $?
        contains_generic_asserts "$root/$mode-oracle.stdout" "$mode" || return $?
        input="$root/input-$mode"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
            "--contains-generic-$mode" "$input/SelfHostPrimSubset.dll" "$(resolve_net10_corelib)" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"
            local flags=("$@")
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" ContainsGeneric || return $?
                runner=(node "$out/ContainsGeneric.js")
            else
                compile_console "$out" ContainsGeneric || return $?
                runner=("$out/ContainsGeneric$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            contains_generic_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/$mode-oracle.stdout" "$out/native.stdout" --policy crlf || return $?
            if [[ "$mode" == handle ]]; then
                gate_capture_ok "$out/boundary" run_bounded "${runner[@]}" native-boundary || return $?
                contains_generic_asserts "$out/boundary.stdout" "$mode" || return $?
                gate_output witness "$out/boundary.stdout" 'contains-generic-delegate-identity:True' --count 1 --policy crlf || return $?
            fi
        done
    done
}
