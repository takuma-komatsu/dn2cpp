#!/usr/bin/env bash
# Compound type wrappers retain checked handles and interned pointer/byref identities.
compound_handle_route_asserts() {
    local output="$1" route="$2" label i
    for ((i=0; i<12; i++)); do
        gate_output witness "$output" "compound-handle-$route-shape-$i:True" --count 1 --policy crlf || return $?
    done
    for label in signature repeated byref-rejected; do
        gate_output witness "$output" "compound-handle-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

compound_handle_contract_asserts() {
    local output="$1" kind
    gate_output witness "$output" '== Compound type handle contract ==' --count 1 --policy crlf || return $?
    for kind in pointer byref; do
        compound_handle_route_asserts "$output" "public-$kind" || return $?
        gate_output witness "$output" "compound-handle-public-$kind-null:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'compound-handle-public-distinct:True' --count 1 --policy crlf || return $?
    gate_output witness "$output" 'Compound type handle contract end' --count 1 --policy crlf
}

compound_handle_asserts() {
    local output="$1" mode="$2" route kind
    if [[ "$mode" == handle ]]; then
        compound_handle_contract_asserts "$output" || return $?
        for route in retained virtual constrained public-address virtual-address flow-direct flow-virtual; do
            for kind in pointer byref; do
                compound_handle_route_asserts "$output" "$route-$kind" || return $?
                if [[ "$route" != public-address && "$route" != flow-direct ]]; then
                    gate_output witness "$output" "compound-handle-$route-$kind-null:True" --count 1 --policy crlf || return $?
                fi
            done
        done
    fi
    local routes=(reflection)
    if [[ "$mode" == handle ]]; then routes+=(direct calli delegate); fi
    for route in "${routes[@]}"; do
        for kind in pointer byref; do
            compound_handle_route_asserts "$output" "$route-$kind" || return $?
            gate_output witness "$output" "compound-handle-$route-$kind-default:True" --count 1 --policy crlf || return $?
        done
    done
    gate_output witness "$output" 'Compound type handle reflection end' --count 1 --policy crlf || return $?
    if [[ "$mode" == handle ]]; then
        gate_output witness "$output" 'Compound type handle routes end' --count 1 --policy crlf || return $?
    fi
}

compound_handle_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out kind i
    shift
    mkdir -p "$root"
    for mode in handle reflection; do
        gate_capture_ok "$root/$mode-oracle" run_bounded dotnet "$original/SelfHostPrimSubset.dll" "compound-$mode" || return $?
        compound_handle_asserts "$root/$mode-oracle.stdout" "$mode" || return $?
        input="$root/input-$mode"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
            "--compound-$mode" "$input/SelfHostPrimSubset.dll" "$(resolve_net10_corelib)" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"
            local flags=("$@")
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" CompoundHandle || return $?
                runner=(node "$out/CompoundHandle.js")
            else
                compile_console "$out" CompoundHandle || return $?
                runner=("$out/CompoundHandle$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            compound_handle_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/$mode-oracle.stdout" "$out/native.stdout" --policy crlf || return $?
            if [[ "$mode" == handle ]]; then
                gate_capture_ok "$out/boundary" run_bounded "${runner[@]}" native-boundary || return $?
                compound_handle_asserts "$out/boundary.stdout" "$mode" || return $?
                for kind in pointer byref; do
                    gate_output witness "$out/boundary.stdout" "compound-handle-delegate-identity-$kind:True" --count 1 --policy crlf || return $?
                    for ((i=0; i<4; i++)); do
                        gate_output witness "$out/boundary.stdout" "compound-handle-public-identity-$kind-$i:True" --count 1 --policy crlf || return $?
                    done
                done
            fi
        done
    done
}
