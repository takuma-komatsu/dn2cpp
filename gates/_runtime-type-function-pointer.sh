#!/usr/bin/env bash
# Retained function-pointer predicates read top-level signature identity and preserve their declaration boundaries.
runtime_type_function_pointer_route_asserts() {
    local output="$1" route="$2" label
    for label in managed managed-explicit unmanaged cdecl stdcall thiscall fastcall suppress-gc cdecl-suppress-gc nested-managed \
        pointer-wrapper byref-wrapper array-wrapper pointer byref enum value void class array open type-formal method-formal closed \
        handle-managed handle-unmanaged repeated; do
        gate_output witness "$output" "function-pointer-predicate-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_function_pointer_null_asserts() {
    local output="$1" route="$2" kind="$3" label
    for label in null default-handle; do
        gate_output witness "$output" "function-pointer-predicate-$route-$kind-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_function_pointer_contract_asserts() {
    local output="$1"
    gate_output witness "$output" '== Runtime function-pointer predicates ==' --count 1 --policy crlf || return $?
    runtime_type_function_pointer_route_asserts "$output" public || return $?
    runtime_type_function_pointer_null_asserts "$output" public managed || return $?
    runtime_type_function_pointer_null_asserts "$output" public unmanaged || return $?
    gate_output witness "$output" 'function-pointer-predicate-generic:True' --count 1 --policy crlf || return $?
    gate_output witness "$output" 'Runtime function-pointer predicates end' --count 1 --policy crlf
}

runtime_type_function_pointer_artifact_asserts() {
    local output="$1" mode="$2" route
    if [[ "$mode" == all ]]; then
        runtime_type_function_pointer_contract_asserts "$output" || return $?
        for route in direct calli getter virtual-getter constrained-getter address virtual-address flow-direct flow-virtual; do
            runtime_type_function_pointer_route_asserts "$output" "$route" || return $?
            if [[ "$route" != flow-direct ]]; then
                runtime_type_function_pointer_null_asserts "$output" "$route" managed || return $?
            fi
        done
        runtime_type_function_pointer_null_asserts "$output" virtual-getter unmanaged || return $?
        runtime_type_function_pointer_null_asserts "$output" virtual-address unmanaged || return $?
        runtime_type_function_pointer_null_asserts "$output" constrained-getter unmanaged || return $?
        runtime_type_function_pointer_null_asserts "$output" flow-virtual unmanaged || return $?
        gate_output witness "$output" 'function-pointer-predicate-virtual-address-bind-null:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'function-pointer-predicate-flow-virtual-bind-null:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'Runtime function-pointer predicate routes end' --count 1 --policy crlf || return $?
    fi
    gate_output witness "$output" '== Runtime function-pointer reflection ==' --count 1 --policy crlf || return $?
    runtime_type_function_pointer_route_asserts "$output" reflection || return $?
    runtime_type_function_pointer_null_asserts "$output" reflection managed || return $?
    gate_output witness "$output" 'Runtime function-pointer reflection end' --count 1 --policy crlf
}

runtime_type_function_pointer_policy_asserts() {
    local output="$1" mode="$2" route
    runtime_type_function_pointer_artifact_asserts "$output" "$mode" || return $?
    gate_output witness "$output" 'function-pointer-predicate-native-safety-reflection:True' --count 1 --policy crlf || return $?
    gate_output witness "$output" 'function-pointer-predicate-native-reflection-row:True' --count 1 --policy crlf || return $?
    if [[ "$mode" == all ]]; then
        for route in direct calli getter address; do
            gate_output witness "$output" "function-pointer-predicate-native-safety-$route:True" --count 1 --policy crlf || return $?
        done
        gate_output witness "$output" 'function-pointer-predicate-native-address-row:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'function-pointer-predicate-native-getter-address-row:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'function-pointer-predicate-native-flow-address-row:True' --count 1 --policy crlf || return $?
    fi
}

runtime_type_function_pointer_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out command flag
    shift
    mkdir -p "$root"
    for mode in all reflection; do
        command=runtime-type-function-pointer-artifact
        flag=--runtime-type-function-pointer
        if [[ "$mode" == reflection ]]; then
            command=runtime-type-function-pointer-reflection
            flag+=-reflection
        fi
        gate_capture_ok "$root/public-$mode" run_bounded dotnet "$original/SelfHostPrimSubset.dll" "$command" || return $?
        runtime_type_function_pointer_artifact_asserts "$root/public-$mode.stdout" "$mode" || return $?
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
                compile_console_wasm "$out" RuntimeTypeFunctionPointer || return $?
                runner=(node "$out/RuntimeTypeFunctionPointer.js")
            else
                compile_console "$out" RuntimeTypeFunctionPointer || return $?
                runner=("$out/RuntimeTypeFunctionPointer$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            runtime_type_function_pointer_artifact_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/public-$mode.stdout" "$out/native.stdout" --policy crlf || return $?
            gate_capture_ok "$out/policy" run_bounded "${runner[@]}" native-policy || return $?
            runtime_type_function_pointer_policy_asserts "$out/policy.stdout" "$mode" || return $?
        done
    done
}
