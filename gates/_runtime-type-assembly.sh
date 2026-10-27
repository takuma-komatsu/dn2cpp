#!/usr/bin/env bash
# Managed assembly adapters preserve RuntimeAssembly wrappers and defining-type provenance.
runtime_type_assembly_route_asserts() {
    local output="$1" route="$2" label
    for label in object void value enum external open closed-app closed-core type-formal method-formal core-formal \
        sz-array md-array rank-one-shape rank-one-array pointer byref formal-array formal-pointer formal-byref function-argument function-return \
        function-unmanaged function-pointer-return function-byref-return function-nested-return function-core-generic-return \
        function-app-generic-return function-pointer-wrapper function-byref-wrapper function-array-wrapper handle recovery; do
        gate_output witness "$output" "runtime-type-assembly-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_assembly_null_asserts() {
    local output="$1" route="$2" label
    for label in null default-handle; do
        gate_output witness "$output" "runtime-type-assembly-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_assembly_contract_asserts() {
    local output="$1" label
    gate_output witness "$output" '== Runtime type assembly contract ==' --count 1 --policy crlf || return $?
    runtime_type_assembly_route_asserts "$output" public || return $?
    runtime_type_assembly_null_asserts "$output" public || return $?
    for label in assembly-origins generic function-shape; do
        gate_output witness "$output" "runtime-type-assembly-$label:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'Runtime type assembly contract end' --count 1 --policy crlf
}

runtime_type_assembly_artifact_asserts() {
    local output="$1" mode="$2" route
    if [[ "$mode" == all ]]; then
        runtime_type_assembly_contract_asserts "$output" || return $?
        for route in direct calli getter getter-calli virtual-getter constrained-getter address virtual-address virtual-address-local; do
            runtime_type_assembly_route_asserts "$output" "$route" || return $?
        done
        for route in getter-calli virtual-getter constrained-getter virtual-address virtual-address-local; do
            runtime_type_assembly_null_asserts "$output" "$route" || return $?
        done
        gate_output witness "$output" 'runtime-type-assembly-virtual-address-bind-null:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'runtime-type-assembly-virtual-address-local-bind-null:True' --count 1 --policy crlf || return $?
        for route in direct calli local address-covariant getter-address-covariant getter-address-local-covariant address-dynamic getter-address-dynamic; do
            gate_output witness "$output" "runtime-type-assembly-object-$route:True" --count 1 --policy crlf || return $?
        done
        gate_output witness "$output" 'Runtime type assembly routes end' --count 1 --policy crlf || return $?
    fi
    gate_output witness "$output" '== Runtime type assembly reflection ==' --count 1 --policy crlf || return $?
    runtime_type_assembly_route_asserts "$output" reflection || return $?
    gate_output witness "$output" 'runtime-type-assembly-object-reflection:True' --count 1 --policy crlf || return $?
    gate_output witness "$output" 'Runtime type assembly reflection end' --count 1 --policy crlf
}

runtime_type_assembly_policy_asserts() {
    local output="$1" mode="$2" route
    runtime_type_assembly_artifact_asserts "$output" "$mode" || return $?
    runtime_type_assembly_null_asserts "$output" native-reflection || return $?
    gate_output witness "$output" 'runtime-type-assembly-native-reflection-row:True' --count 1 --policy crlf || return $?
    gate_output witness "$output" 'runtime-type-assembly-native-getter-lookup-unavailable:True' --count 1 --policy crlf || return $?
    if [[ "$mode" == all ]]; then
        for route in direct calli getter address; do
            runtime_type_assembly_null_asserts "$output" "native-$route" || return $?
        done
        gate_output witness "$output" 'runtime-type-assembly-native-address-row:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'runtime-type-assembly-native-getter-address-row:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'runtime-type-assembly-native-getter-address-local-row:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'runtime-type-assembly-native-getter-address-invoke:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'runtime-type-assembly-native-getter-address-local-invoke:True' --count 1 --policy crlf || return $?
    fi
}

runtime_type_assembly_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out command flag
    shift
    mkdir -p "$root"
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        --runtime-type-assembly-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" || return $?
    for mode in all reflection; do
        command=runtime-type-assembly-artifact
        flag=--runtime-type-assembly
        if [[ "$mode" == reflection ]]; then
            command=runtime-type-assembly-reflection
            flag+=-reflection
        fi
        gate_capture_ok "$root/public-$mode" run_bounded dotnet "$original/SelfHostPrimSubset.dll" "$command" || return $?
        runtime_type_assembly_artifact_asserts "$root/public-$mode.stdout" "$mode" || return $?
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
                compile_console_wasm "$out" RuntimeTypeAssembly || return $?
                runner=(node "$out/RuntimeTypeAssembly.js")
            else
                compile_console "$out" RuntimeTypeAssembly || return $?
                runner=("$out/RuntimeTypeAssembly$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            runtime_type_assembly_artifact_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/public-$mode.stdout" "$out/native.stdout" --policy crlf || return $?
            gate_capture_ok "$out/policy" run_bounded "${runner[@]}" native-policy || return $?
            runtime_type_assembly_policy_asserts "$out/policy.stdout" "$mode" || return $?
        done
    done
}
