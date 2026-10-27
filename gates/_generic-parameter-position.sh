#!/usr/bin/env bash
# Generic parameter positions retain defining contexts and managed validation across native routes.
generic_position_route_asserts() {
    local output="$1" route="$2" label i
    for label in method typical type nested nested-method reordered repeated; do
        gate_output witness "$output" "generic-position-$route-$label:True" --count 1 --policy crlf || return $?
    done
    for ((i=0; i<12; i++)); do
        gate_output witness "$output" "generic-position-$route-nonformal-$i:True" --count 1 --policy crlf || return $?
    done
}

generic_position_contract_asserts() {
    local output="$1"
    gate_output witness "$output" '== Generic parameter position contract ==' --count 1 --policy crlf || return $?
    generic_position_route_asserts "$output" public || return $?
    gate_output witness "$output" 'generic-position-public-null:True' --count 1 --policy crlf || return $?
    gate_output witness "$output" 'Generic parameter position contract end' --count 1 --policy crlf
}

generic_position_asserts() {
    local output="$1" mode="$2" route label
    if [[ "$mode" != reflection ]]; then generic_position_contract_asserts "$output" || return $?; fi
    if [[ "$mode" == public ]]; then
        for route in direct virtual pointer address virtual-address flowing-address; do
            generic_position_route_asserts "$output" "$route" || return $?
        done
        for label in direct-null pointer-null virtual-null delegate-identity delegate-reflection delegate-reflection-null virtual-delegate-identity; do
            gate_output witness "$output" "generic-position-$label:True" --count 1 --policy crlf || return $?
        done
        gate_output witness "$output" 'Generic parameter position public routes end' --count 1 --policy crlf || return $?
    else
        local routes=(handle-reflection)
        if [[ "$mode" == handle ]]; then routes+=(handle-direct handle-pointer handle-delegate); fi
        for route in "${routes[@]}"; do
            generic_position_route_asserts "$output" "$route" || return $?
            gate_output witness "$output" "generic-position-$route-default:True" --count 1 --policy crlf || return $?
        done
        gate_output witness "$output" 'Generic parameter position reflection end' --count 1 --policy crlf || return $?
        if [[ "$mode" == handle ]]; then
            gate_output witness "$output" 'Generic parameter position handle routes end' --count 1 --policy crlf || return $?
        fi
    fi
}

generic_position_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out label
    shift
    mkdir -p "$root"
    for mode in public handle reflection; do
        gate_capture_ok "$root/$mode-oracle" run_bounded dotnet "$original/SelfHostPrimSubset.dll" "generic-position-$mode" || return $?
        generic_position_asserts "$root/$mode-oracle.stdout" "$mode" || return $?
        input="$root/input-$mode"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
            "--generic-position-$mode" "$input/SelfHostPrimSubset.dll" "$(resolve_net10_corelib)" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"
            local flags=("$@")
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" GenericPosition || return $?
                runner=(node "$out/GenericPosition.js")
            else
                compile_console "$out" GenericPosition || return $?
                runner=("$out/GenericPosition$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            generic_position_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/$mode-oracle.stdout" "$out/native.stdout" --policy crlf || return $?
            if [[ "$mode" != reflection ]]; then
                gate_capture_ok "$out/boundary" run_bounded "${runner[@]}" native-boundary || return $?
                generic_position_asserts "$out/boundary.stdout" "$mode" || return $?
                local labels=(handle-delegate-identity)
                if [[ "$mode" == public ]]; then labels=(virtual-address-null flowing-address-null); fi
                for label in "${labels[@]}"; do
                    gate_output witness "$out/boundary.stdout" "generic-position-$label:True" --count 1 --policy crlf || return $?
                done
            fi
        done
    done
}
