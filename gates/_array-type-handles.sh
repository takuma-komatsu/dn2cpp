#!/usr/bin/env bash
# Array wrappers preserve native AOT limits and public/private rank validation order.
array_type_route_asserts() {
    local output="$1" route="$2" kind="$3" label i rank count=6
    local ranks=(1 2 32)
    if [[ "$kind" == sz ]]; then count=11; ranks=(1); fi
    for ((i=0; i<count; i++)); do
        for rank in "${ranks[@]}"; do
            gate_output witness "$output" "array-handle-$route-shape-$i-$rank:True" --count 1 --policy crlf || return $?
        done
    done
    for label in repeated byref byreflike open-byreflike; do
        gate_output witness "$output" "array-handle-$route-$label:True" --count 1 --policy crlf || return $?
    done
    if [[ "$kind" == rank ]]; then
        for label in rank33 rank-max non-sz; do
            gate_output witness "$output" "array-handle-$route-$label:True" --count 1 --policy crlf || return $?
        done
    fi
}

array_type_contract_asserts() {
    local output="$1" kind label
    gate_output witness "$output" '== Array type handle contract ==' --count 1 --policy crlf || return $?
    for kind in sz rank; do
        array_type_route_asserts "$output" "public-$kind" "$kind" || return $?
        gate_output witness "$output" "array-handle-public-$kind-null:True" --count 1 --policy crlf || return $?
    done
    for label in null-zero invalid-0 invalid--1 invalid--2147483648 unsigned; do
        gate_output witness "$output" "array-handle-public-rank-$label:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'Array type handle contract end' --count 1 --policy crlf
}

array_type_asserts() {
    local output="$1" mode="$2" route kind label
    if [[ "$mode" == handle ]]; then
        array_type_contract_asserts "$output" || return $?
        for route in retained virtual constrained public-address virtual-address flow-direct flow-virtual; do
            for kind in sz rank; do
                array_type_route_asserts "$output" "$route-$kind" "$kind" || return $?
                if [[ "$route" != public-address && "$route" != flow-direct ]]; then
                    gate_output witness "$output" "array-handle-$route-$kind-null:True" --count 1 --policy crlf || return $?
                    if [[ "$kind" == rank ]]; then
                        gate_output witness "$output" "array-handle-$route-$kind-null-zero:True" --count 1 --policy crlf || return $?
                    fi
                fi
            done
        done
        for route in retained virtual; do
            gate_output witness "$output" "array-handle-$route-rank-unsigned:True" --count 1 --policy crlf || return $?
        done
    fi
    local routes=(reflection)
    if [[ "$mode" == handle ]]; then routes+=(direct calli delegate); fi
    for route in "${routes[@]}"; do
        for kind in sz rank; do
            array_type_route_asserts "$output" "$route-$kind" "$kind" || return $?
            gate_output witness "$output" "array-handle-$route-$kind-default:True" --count 1 --policy crlf || return $?
        done
    done
    gate_output witness "$output" 'Array type handle reflection end' --count 1 --policy crlf || return $?
    if [[ "$mode" == handle ]]; then
        gate_output witness "$output" 'Array type handle routes end' --count 1 --policy crlf || return $?
    fi
}

array_type_boundary_asserts() {
    local output="$1" mode="$2" route kind rank label i
    local routes=(reflection)
    if [[ "$mode" == handle ]]; then routes+=(direct calli delegate); fi
    for route in "${routes[@]}"; do
        for kind in sz rank; do
            gate_output witness "$output" "array-handle-$route-$kind-missing:True" --count 1 --policy crlf || return $?
        done
        for rank in 1 2 32 33; do
            gate_output witness "$output" "array-handle-$route-rank-concrete-$rank:True" --count 1 --policy crlf || return $?
        done
        for rank in 0 -1 -2147483648; do
            for label in private-rank default-rank element-rank; do
                gate_output witness "$output" "array-handle-$route-rank-$label-$rank:True" --count 1 --policy crlf || return $?
            done
        done
    done
    if [[ "$mode" == handle ]]; then
        for label in private-rank-unsigned private-default-unsigned; do
            gate_output witness "$output" "array-handle-$label:True" --count 1 --policy crlf || return $?
        done
        for kind in sz rank; do
            gate_output witness "$output" "array-handle-delegate-identity-$kind:True" --count 1 --policy crlf || return $?
            for ((i=0; i<4; i++)); do
                gate_output witness "$output" "array-handle-public-identity-$kind-$i:True" --count 1 --policy crlf || return $?
            done
        done
    fi
}

array_type_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out
    shift
    mkdir -p "$root"
    for mode in handle reflection; do
        gate_capture_ok "$root/$mode-oracle" run_bounded dotnet "$original/SelfHostPrimSubset.dll" "array-type-$mode" || return $?
        array_type_asserts "$root/$mode-oracle.stdout" "$mode" || return $?
        input="$root/input-$mode"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
            "--array-type-$mode" "$input/SelfHostPrimSubset.dll" "$(resolve_net10_corelib)" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"
            local flags=("$@")
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" ArrayTypeHandle || return $?
                runner=(node "$out/ArrayTypeHandle.js")
            else
                compile_console "$out" ArrayTypeHandle || return $?
                runner=("$out/ArrayTypeHandle$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            array_type_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/$mode-oracle.stdout" "$out/native.stdout" --policy crlf || return $?
            # These AOT/private-loader boundaries deliberately do not claim CLR parity.
            gate_capture_ok "$out/boundary" run_bounded "${runner[@]}" native-boundary || return $?
            array_type_asserts "$out/boundary.stdout" "$mode" || return $?
            array_type_boundary_asserts "$out/boundary.stdout" "$mode" || return $?
        done
    done
}
