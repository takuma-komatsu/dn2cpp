#!/usr/bin/env bash
# Generic-variable predicates classify only the queried formal identity and retain their raw/public boundaries.
runtime_type_generic_variable_route_asserts() {
    local output="$1" route="$2" label
    for label in type-formal method-formal closed-owner-formal recursive-formal definition nested-definition open-constructed closed-generic \
        formal-array formal-mdarray-one formal-mdarray-two formal-pointer formal-byref method-array method-pointer method-byref \
        open-array nested-compound object void value enum string array pointer byref function-managed function-unmanaged function-wrapper \
        handle-type handle-method repeated; do
        gate_output witness "$output" "generic-variable-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_generic_variable_null_asserts() {
    local output="$1" route="$2" label
    for label in null default-handle; do
        gate_output witness "$output" "generic-variable-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_generic_variable_contract_asserts() {
    local output="$1" label
    gate_output witness "$output" '== Runtime generic-variable predicates ==' --count 1 --policy crlf || return $?
    runtime_type_generic_variable_route_asserts "$output" public || return $?
    runtime_type_generic_variable_null_asserts "$output" public || return $?
    for label in top-level generic; do
        gate_output witness "$output" "generic-variable-$label:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'Runtime generic-variable predicates end' --count 1 --policy crlf
}

runtime_type_generic_variable_artifact_asserts() {
    local output="$1" mode="$2" route
    if [[ "$mode" == all ]]; then
        runtime_type_generic_variable_contract_asserts "$output" || return $?
        for route in direct calli getter getter-calli virtual-getter constrained-getter address virtual-address flow-direct flow-virtual; do
            runtime_type_generic_variable_route_asserts "$output" "$route" || return $?
        done
        for route in getter-calli virtual-getter constrained-getter virtual-address flow-virtual; do
            runtime_type_generic_variable_null_asserts "$output" "$route" || return $?
        done
        gate_output witness "$output" 'generic-variable-virtual-address-bind-null:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'generic-variable-flow-virtual-bind-null:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'Runtime generic-variable routes end' --count 1 --policy crlf || return $?
    fi
    gate_output witness "$output" '== Runtime generic-variable reflection ==' --count 1 --policy crlf || return $?
    runtime_type_generic_variable_route_asserts "$output" reflection || return $?
    gate_output witness "$output" 'Runtime generic-variable reflection end' --count 1 --policy crlf
}

runtime_type_generic_variable_policy_asserts() {
    local output="$1" mode="$2" route label
    runtime_type_generic_variable_artifact_asserts "$output" "$mode" || return $?
    for label in native-safety-reflection native-reflection-row native-getter-lookup-unavailable; do
        gate_output witness "$output" "generic-variable-$label:True" --count 1 --policy crlf || return $?
    done
    if [[ "$mode" == all ]]; then
        for route in direct calli getter address; do
            gate_output witness "$output" "generic-variable-native-safety-$route:True" --count 1 --policy crlf || return $?
        done
        for label in native-address-row native-address-invoke; do
            gate_output witness "$output" "generic-variable-$label:True" --count 1 --policy crlf || return $?
        done
        for route in getter-address flow-direct flow-virtual; do
            for label in row invoke; do
                gate_output witness "$output" "generic-variable-native-$route-$label:True" --count 1 --policy crlf || return $?
            done
        done
    fi
}

runtime_type_generic_variable_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out command flag
    shift
    mkdir -p "$root"
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        --runtime-type-generic-variable-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" || return $?
    for mode in all reflection; do
        command=runtime-type-generic-variable-artifact
        flag=--runtime-type-generic-variable
        if [[ "$mode" == reflection ]]; then
            command=runtime-type-generic-variable-reflection
            flag+=-reflection
        fi
        gate_capture_ok "$root/public-$mode" run_bounded dotnet "$original/SelfHostPrimSubset.dll" "$command" || return $?
        runtime_type_generic_variable_artifact_asserts "$root/public-$mode.stdout" "$mode" || return $?
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
                compile_console_wasm "$out" RuntimeTypeGenericVariable || return $?
                runner=(node "$out/RuntimeTypeGenericVariable.js")
            else
                compile_console "$out" RuntimeTypeGenericVariable || return $?
                runner=("$out/RuntimeTypeGenericVariable$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            runtime_type_generic_variable_artifact_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/public-$mode.stdout" "$out/native.stdout" --policy crlf || return $?
            gate_capture_ok "$out/policy" run_bounded "${runner[@]}" native-policy || return $?
            runtime_type_generic_variable_policy_asserts "$out/policy.stdout" "$mode" || return $?
        done
    done
}
