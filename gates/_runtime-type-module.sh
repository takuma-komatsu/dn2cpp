#!/usr/bin/env bash
# Retained module adapters use existing single-module identity and managed wrappers.
runtime_type_module_route_asserts() {
    local output="$1" route="$2" label
    for label in object void value enum external open closed-app closed-core type-formal method-formal core-formal \
        sz-array md-array rank-one-shape rank-one-array pointer byref formal-array formal-pointer formal-byref function-argument function-return \
        function-unmanaged function-pointer-return function-byref-return function-nested-return function-core-generic-return \
        function-app-generic-return function-pointer-wrapper function-byref-wrapper function-array-wrapper handle recovery; do
        gate_output witness "$output" "runtime-type-module-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_module_null_asserts() {
    local output="$1" route="$2" label
    for label in null default-handle; do
        gate_output witness "$output" "runtime-type-module-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_module_contract_asserts() {
    local output="$1" label
    gate_output witness "$output" '== Runtime type module contract ==' --count 1 --policy crlf || return $?
    runtime_type_module_route_asserts "$output" public || return $?
    runtime_type_module_null_asserts "$output" public || return $?
    for label in module-origins generic function-shape; do
        gate_output witness "$output" "runtime-type-module-$label:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'Runtime type module contract end' --count 1 --policy crlf
}

runtime_type_module_artifact_asserts() {
    local output="$1" mode="$2" route
    if [[ "$mode" == all ]]; then
        runtime_type_module_contract_asserts "$output" || return $?
        for route in direct calli getter runtime-getter runtime-getter-calli getter-calli virtual-getter constrained-getter address virtual-address virtual-address-local; do
            runtime_type_module_route_asserts "$output" "$route" || return $?
        done
        for route in getter-calli virtual-getter constrained-getter virtual-address virtual-address-local; do
            runtime_type_module_null_asserts "$output" "$route" || return $?
        done
        gate_output witness "$output" 'runtime-type-module-virtual-address-bind-null:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'runtime-type-module-virtual-address-local-bind-null:True' --count 1 --policy crlf || return $?
        for route in direct calli local runtime-getter address-covariant getter-address-covariant getter-address-local-covariant address-dynamic getter-address-dynamic; do
            gate_output witness "$output" "runtime-type-module-object-$route:True" --count 1 --policy crlf || return $?
        done
        gate_output witness "$output" 'Runtime type module routes end' --count 1 --policy crlf || return $?
    fi
    gate_output witness "$output" '== Runtime type module reflection ==' --count 1 --policy crlf || return $?
    runtime_type_module_route_asserts "$output" reflection || return $?
    gate_output witness "$output" 'runtime-type-module-object-reflection:True' --count 1 --policy crlf || return $?
    gate_output witness "$output" 'Runtime type module reflection end' --count 1 --policy crlf
}

runtime_type_module_policy_asserts() {
    local output="$1" mode="$2" route
    runtime_type_module_artifact_asserts "$output" "$mode" || return $?
    runtime_type_module_null_asserts "$output" native-reflection || return $?
    gate_output witness "$output" 'runtime-type-module-native-reflection-row:True' --count 1 --policy crlf || return $?
    gate_output witness "$output" 'runtime-type-module-native-getter-lookup-unavailable:True' --count 1 --policy crlf || return $?
    if [[ "$mode" == all ]]; then
        for route in direct calli getter runtime-getter runtime-getter-calli address; do
            runtime_type_module_null_asserts "$output" "native-$route" || return $?
        done
        gate_output witness "$output" 'runtime-type-module-native-address-row:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'runtime-type-module-native-getter-address-row:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'runtime-type-module-native-getter-address-local-row:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'runtime-type-module-native-getter-address-invoke:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'runtime-type-module-native-getter-address-local-invoke:True' --count 1 --policy crlf || return $?
    fi
}

runtime_type_module_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out command flag
    shift
    mkdir -p "$root"
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        --runtime-type-module-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" || return $?
    local corelib
    corelib=$(resolve_net10_corelib) || return $?
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    input="$root/oracle-public-input"
    mkdir -p "$input"
    cp "$original/"* "$input/" || return $?
    dotnet "$fixture" --runtime-type-module-public "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture_ok "$root/public-only" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
    runtime_type_module_contract_asserts "$root/public-only.stdout" || return $?
    for mode in all reflection; do
        flag=--runtime-type-module
        local oracle=--runtime-type-module-oracle
        if [[ "$mode" == reflection ]]; then flag+=-reflection; oracle+=-reflection; fi
        input="$root/oracle-$mode-input"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "$oracle" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        gate_capture_ok "$root/public-$mode" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
        runtime_type_module_artifact_asserts "$root/public-$mode.stdout" "$mode" || return $?
        if [[ "$mode" == all ]]; then
            gate_output prefix "$root/public-$mode.stdout" "$root/public-only.stdout" \
                --before 'runtime-type-module-direct-rank-one-shape:True' --policy crlf || return $?
        fi
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
                compile_console_wasm "$out" RuntimeTypeModule || return $?
                runner=(node "$out/RuntimeTypeModule.js")
            else
                compile_console "$out" RuntimeTypeModule || return $?
                runner=("$out/RuntimeTypeModule$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            runtime_type_module_artifact_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/public-$mode.stdout" "$out/native.stdout" --policy crlf || return $?
            gate_capture_ok "$out/policy" run_bounded "${runner[@]}" native-policy || return $?
            runtime_type_module_policy_asserts "$out/policy.stdout" "$mode" || return $?
        done
    done
}

# Malformed references are compile-only inputs and never run on CLR or natively.
runtime_type_module_negative_gate() {
    local root="$1" shape input out
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local corelib
    corelib=$(resolve_net10_corelib) || return $?
    for shape in return parameter hasthis vararg return-valuetype address-return address-parameter \
        override-return override-static override-return-valuetype address-override-return internal-return internal-static internal-return-valuetype; do
        input="$root/input-$shape"
        out="$root/output-$shape"
        mkdir -p "$input"
        cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        dotnet "$fixture" "--runtime-type-module-negative-$shape" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        gate_capture "$root/$shape" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$out" || return $?
        gate_output status "$(cat "$root/$shape.status")" 2 || return $?
        gate_output match-record "$root/$shape.stderr" \
            'error: .*(GetModule|get_Module|GetRuntimeModule|RuntimeType module query requires its exact MemberRef signature).*' --count 1 --policy crlf || return $?
    done
}
