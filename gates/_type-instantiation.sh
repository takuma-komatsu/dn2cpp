#!/usr/bin/env bash
# Managed type-instantiation wrappers preserve null and distinct fresh array identities.
type_instantiation_route_asserts() {
    local output="$1" route="$2" i j
    for i in {0..8}; do
        gate_output witness "$output" "type-instantiation-$route-empty-$i:True" --count 1 --policy crlf || return $?
    done
    for i in {0..5}; do
        for j in length array fresh mutation store; do
            gate_output witness "$output" "type-instantiation-$route-$j-$i:True" --count 1 --policy crlf || return $?
        done
        gate_output witness "$output" "type-instantiation-$route-identity-$i-0:True" --count 1 --policy crlf || return $?
        if (( i >= 3 )); then
            gate_output witness "$output" "type-instantiation-$route-identity-$i-1:True" --count 1 --policy crlf || return $?
        fi
    done
    for j in formals default emptytypes; do
        gate_output witness "$output" "type-instantiation-$route-$j:True" --count 1 --policy crlf || return $?
    done
}

type_instantiation_contract_asserts() {
    local output="$1"
    gate_output witness "$output" '== Runtime type instantiation contract ==' --count 1 --policy crlf || return $?
    type_instantiation_route_asserts "$output" public-oracle || return $?
    gate_output witness "$output" 'type-instantiation-generic:True' --count 1 --policy crlf || return $?
    gate_output witness "$output" 'Runtime type instantiation contract end' --count 1 --policy crlf
}

type_instantiation_artifact_asserts() {
    local output="$1" mode="$2" route
    if [[ "$mode" == all ]]; then
        type_instantiation_contract_asserts "$output" || return $?
        for route in direct pointer delegate; do type_instantiation_route_asserts "$output" "$route" || return $?; done
        for route in empty closed formal root; do
            gate_output witness "$output" "type-instantiation-getter-$route:True" --count 1 --policy crlf || return $?
        done
        gate_output witness "$output" 'Runtime type instantiation routes end' --count 1 --policy crlf || return $?
    fi
    type_instantiation_route_asserts "$output" reflection || return $?
    gate_output witness "$output" 'Runtime type instantiation reflection end' --count 1 --policy crlf
}

type_instantiation_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" axis mode input out oracle
    shift
    mkdir -p "$root"
    for mode in all reflection; do
        oracle=type-instantiation-artifact
        [[ "$mode" == all ]] || oracle=type-instantiation-reflection
        gate_capture_ok "$root/oracle-$mode" run_bounded dotnet "$original/SelfHostPrimSubset.dll" "$oracle" || return $?
        type_instantiation_artifact_asserts "$root/oracle-$mode.stdout" "$mode" || return $?
        input="$root/input-$mode"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        local fixture=--type-instantiation
        [[ "$mode" == all ]] || fixture=--type-instantiation-reflection
        dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
            "$fixture" "$input/SelfHostPrimSubset.dll" "$(resolve_net10_corelib)" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"
            local flags=("$@")
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" TypeInstantiation || return $?
                runner=(node "$out/TypeInstantiation.js")
            else
                compile_console "$out" TypeInstantiation || return $?
                runner=("$out/TypeInstantiation$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            type_instantiation_artifact_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/oracle-$mode.stdout" "$out/native.stdout" --policy crlf || return $?
            if [[ "$mode" == all ]]; then
                gate_capture_ok "$out/identity" run_bounded "${runner[@]}" native-identity || return $?
                type_instantiation_artifact_asserts "$out/identity.stdout" "$mode" || return $?
                gate_output witness "$out/identity.stdout" 'type-instantiation-address-identity:True' --count 1 --policy crlf || return $?
            fi
        done
    done
}
