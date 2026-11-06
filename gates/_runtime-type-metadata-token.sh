#!/usr/bin/env bash
# Retained token queries preserve the native stamped/unstamped policy, not full CLR token parity.
runtime_type_metadata_token_route_asserts() {
    local output="$1" route="$2" label
    for label in type-formal method-formal closed-owner-formal recursive-formal definition nested-definition open-constructed closed-generic \
        formal-array formal-mdarray-one formal-mdarray-two formal-pointer formal-byref method-array method-pointer method-byref \
        open-array nested-compound object void value enum string array pointer byref function-managed function-unmanaged function-wrapper nested-enum nested-enum-definition \
        threadlocal-int threadlocal-string blockingcollection int-comparer string-comparer stamped-definition stamped-enum \
        handle-type handle-method repeated; do
        gate_output witness "$output" "metadata-token-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_metadata_token_null_asserts() {
    local output="$1" route="$2" label
    for label in null default-handle; do
        gate_output witness "$output" "metadata-token-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_metadata_token_contract_asserts() {
    local output="$1" label
    gate_output witness "$output" '== Runtime metadata-token predicates ==' --count 1 --policy crlf || return $?
    runtime_type_metadata_token_route_asserts "$output" public || return $?
    runtime_type_metadata_token_null_asserts "$output" public || return $?
    for label in generic; do
        gate_output witness "$output" "metadata-token-$label:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'Runtime metadata-token predicates end' --count 1 --policy crlf
}

runtime_type_metadata_token_artifact_asserts() {
    local output="$1" mode="$2" route
    if [[ "$mode" == all ]]; then
        runtime_type_metadata_token_contract_asserts "$output" || return $?
        for route in direct calli getter getter-calli virtual-getter constrained-getter address virtual-address flow-direct flow-virtual; do
            runtime_type_metadata_token_route_asserts "$output" "$route" || return $?
        done
        for route in getter-calli virtual-getter constrained-getter virtual-address flow-virtual; do
            runtime_type_metadata_token_null_asserts "$output" "$route" || return $?
        done
        gate_output witness "$output" 'metadata-token-virtual-address-bind-null:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'metadata-token-flow-virtual-bind-null:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'Runtime metadata-token routes end' --count 1 --policy crlf || return $?
    fi
    gate_output witness "$output" '== Runtime metadata-token reflection ==' --count 1 --policy crlf || return $?
    runtime_type_metadata_token_route_asserts "$output" reflection || return $?
    gate_output witness "$output" 'Runtime metadata-token reflection end' --count 1 --policy crlf
}

runtime_type_metadata_token_policy_asserts() {
    local output="$1" mode="$2" route label
    runtime_type_metadata_token_artifact_asserts "$output" "$mode" || return $?
    for label in native-safety-reflection native-reflection-row native-getter-lookup-unavailable; do
        gate_output witness "$output" "metadata-token-$label:True" --count 1 --policy crlf || return $?
    done
    if [[ "$mode" == all ]]; then
        for route in direct calli getter address; do
            gate_output witness "$output" "metadata-token-native-safety-$route:True" --count 1 --policy crlf || return $?
        done
        for label in native-address-row native-address-invoke; do
            gate_output witness "$output" "metadata-token-$label:True" --count 1 --policy crlf || return $?
        done
        for route in getter-address flow-direct flow-virtual; do
            for label in row invoke; do
                gate_output witness "$output" "metadata-token-native-$route-$label:True" --count 1 --policy crlf || return $?
            done
        done
    fi
}

runtime_type_metadata_token_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out command flag
    shift
    mkdir -p "$root"
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        --runtime-type-metadata-token-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" || return $?
    local corelib expected_definition expected_enum
    corelib=$(resolve_net10_corelib) || return $?
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    input="$root/oracle-public-input"
    mkdir -p "$input"
    cp "$original/"* "$input/" || return $?
    dotnet "$fixture" --runtime-type-metadata-token-public "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    dotnet "$fixture" --runtime-type-metadata-token-values "$input/SelfHostPrimSubset.dll" > "$root/public-only.tokens" || return $?
    read -r expected_definition expected_enum < "$root/public-only.tokens" || return $?
    gate_capture_ok "$root/public-only" run_bounded dotnet "$input/SelfHostPrimSubset.dll" tokens "$expected_definition" "$expected_enum" || return $?
    runtime_type_metadata_token_contract_asserts "$root/public-only.stdout" || return $?
    for mode in all reflection; do
        flag=--runtime-type-metadata-token
        local oracle=--runtime-type-metadata-token-oracle
        if [[ "$mode" == reflection ]]; then flag+=-reflection; oracle+=-reflection; fi
        input="$root/oracle-$mode-input"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "$oracle" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        dotnet "$fixture" --runtime-type-metadata-token-values "$input/SelfHostPrimSubset.dll" > "$root/public-$mode.tokens" || return $?
        read -r expected_definition expected_enum < "$root/public-$mode.tokens" || return $?
        gate_capture_ok "$root/public-$mode" run_bounded dotnet "$input/SelfHostPrimSubset.dll" tokens "$expected_definition" "$expected_enum" || return $?
        runtime_type_metadata_token_artifact_asserts "$root/public-$mode.stdout" "$mode" || return $?
        if [[ "$mode" == all ]]; then
            gate_output prefix "$root/public-$mode.stdout" "$root/public-only.stdout" \
                --before 'metadata-token-direct-type-formal:True' --policy crlf || return $?
        fi
        if [[ "$mode" == reflection ]]; then
            gate_capture_ok "$root/clr-policy" run_bounded dotnet "$input/SelfHostPrimSubset.dll" clr-policy "$expected_definition" "$expected_enum" || return $?
            runtime_type_metadata_token_policy_rows "$root/clr-policy.stdout" clr-public || return $?
        fi
        input="$root/input-$mode"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"
            local flags=("$@")
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            local token_input="$input/SelfHostPrimSubset.dll"
            [[ "$axis" != diet ]] || token_input="$out/ildiet/ildiet-result.xml"
            dotnet "$fixture" --runtime-type-metadata-token-values "$token_input" > "$out/expected.tokens" || return $?
            read -r expected_definition expected_enum < "$out/expected.tokens" || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" RuntimeTypeMetadataToken || return $?
                runner=(node "$out/RuntimeTypeMetadataToken.js")
            else
                compile_console "$out" RuntimeTypeMetadataToken || return $?
                runner=("$out/RuntimeTypeMetadataToken$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" tokens "$expected_definition" "$expected_enum" || return $?
            runtime_type_metadata_token_artifact_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/public-$mode.stdout" "$out/native.stdout" --policy crlf || return $?
            gate_capture_ok "$out/policy" run_bounded "${runner[@]}" native-policy "$expected_definition" "$expected_enum" || return $?
            runtime_type_metadata_token_policy_asserts "$out/policy.stdout" "$mode" || return $?
            runtime_type_metadata_token_policy_rows "$out/policy.stdout" native-reflection || return $?
            if [[ "$mode" == all ]]; then
                runtime_type_metadata_token_policy_rows "$out/policy.stdout" native-public || return $?
                runtime_type_metadata_token_policy_rows "$out/policy.stdout" native-direct || return $?
            fi
        done
    done
}

runtime_type_metadata_token_policy_rows() {
    local output="$1" route="$2" label
    for label in type-formal-policy method-formal-policy compound-policy-0 compound-policy-1 compound-policy-2 \
        compound-policy-3 compound-policy-4 compound-policy-5 nil-row-guard; do
        gate_output witness "$output" "metadata-token-$route-$label:True" --count 1 --policy crlf || return $?
    done
}
