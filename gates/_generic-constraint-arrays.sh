#!/usr/bin/env bash
# Retained constraint wrappers preserve formal contexts and precise fresh Type arrays.
constraint_array_route_asserts() {
    local output="$1" route="$2" label
    for label in empty-Empty empty-Reference empty-Constructor type-formal method-formal value dependent composed recursive fresh repeated; do
        gate_output witness "$output" "constraint-array-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

constraint_array_shape_asserts() {
    local output="$1" route="$2" label
    for label in array-type store-type store-object store-isolation; do
        gate_output witness "$output" "constraint-array-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

constraint_array_contract_asserts() {
    local output="$1"
    gate_output witness "$output" '== Generic constraint array contract ==' --count 1 --policy crlf || return $?
    constraint_array_route_asserts "$output" public-payload || return $?
    gate_output witness "$output" 'Generic constraint array contract end' --count 1 --policy crlf
}

constraint_array_asserts() {
    local output="$1" mode="$2" route i
    if [[ "$mode" == handle ]]; then
        constraint_array_contract_asserts "$output" || return $?
        constraint_array_route_asserts "$output" retained-getter || return $?
        constraint_array_shape_asserts "$output" retained-getter || return $?
        for route in emptytypes null; do
            gate_output witness "$output" "constraint-array-retained-getter-$route:True" --count 1 --policy crlf || return $?
        done
        for ((i=0; i<6; i++)); do
            gate_output witness "$output" "constraint-array-retained-getter-nonformal-$i:True" --count 1 --policy crlf || return $?
        done
    fi
    local routes=(reflection)
    if [[ "$mode" == handle ]]; then routes+=(direct pointer delegate); fi
    for route in "${routes[@]}"; do
        constraint_array_route_asserts "$output" "$route" || return $?
        constraint_array_shape_asserts "$output" "$route" || return $?
        gate_output witness "$output" "constraint-array-$route-default:True" --count 1 --policy crlf || return $?
        for ((i=0; i<6; i++)); do
            gate_output witness "$output" "constraint-array-$route-nonformal-$i:True" --count 1 --policy crlf || return $?
        done
    done
    gate_output witness "$output" 'Generic constraint array reflection end' --count 1 --policy crlf || return $?
    if [[ "$mode" == handle ]]; then
        gate_output witness "$output" 'Generic constraint array handle routes end' --count 1 --policy crlf || return $?
    fi
}

constraint_array_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out
    shift
    mkdir -p "$root"
    for mode in handle reflection; do
        gate_capture_ok "$root/$mode-oracle" run_bounded dotnet "$original/SelfHostPrimSubset.dll" "constraint-array-$mode" || return $?
        constraint_array_asserts "$root/$mode-oracle.stdout" "$mode" || return $?
        input="$root/input-$mode"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
            "--constraint-array-$mode" "$input/SelfHostPrimSubset.dll" "$(resolve_net10_corelib)" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"
            local flags=("$@")
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" ConstraintArray || return $?
                runner=(node "$out/ConstraintArray.js")
            else
                compile_console "$out" ConstraintArray || return $?
                runner=("$out/ConstraintArray$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            constraint_array_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/$mode-oracle.stdout" "$out/native.stdout" --policy crlf || return $?
            if [[ "$mode" == handle ]]; then
                gate_capture_ok "$out/boundary" run_bounded "${runner[@]}" native-boundary || return $?
                constraint_array_asserts "$out/boundary.stdout" "$mode" || return $?
                gate_output witness "$out/boundary.stdout" 'constraint-array-delegate-identity:True' --count 1 --policy crlf || return $?
            fi
        done
    done
}
