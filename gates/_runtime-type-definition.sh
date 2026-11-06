#!/usr/bin/env bash
# Retained type-definition predicates distinguish declarations from compounds and constructed types.
runtime_type_definition_route_asserts() {
    local output="$1" route="$2" label
    for label in object void bool char sbyte byte short ushort int uint long ulong float double nint nuint string decimal \
        class interface array-base typed-reference generic-definition nested-definition nullable-definition enum-definition \
        threadlocal-definition blockingcollection-definition \
        type-formal method-formal closed-owner-formal recursive-formal open-constructed closed-generic nullable-closed nested-closed \
        formal-array formal-mdarray-one formal-mdarray-two formal-pointer formal-byref method-array method-pointer method-byref \
        open-array nested-compound array mdarray-one mdarray-two pointer byref function-managed function-unmanaged function-wrapper \
        threadlocal-int threadlocal-string blockingcollection-int blockingcollection-string threadlocal-object blockingcollection-object \
        mdarray-one-shape handle-definition handle-formal repeated null default-handle; do
        gate_output witness "$output" "type-definition-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_definition_contract_asserts() {
    local output="$1" label
    gate_output witness "$output" '== Runtime type-definition predicates ==' --count 1 --policy crlf || return $?
    runtime_type_definition_route_asserts "$output" public || return $?
    for label in top-level generic; do
        gate_output witness "$output" "type-definition-$label:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'Runtime type-definition predicates end' --count 1 --policy crlf
}

runtime_type_definition_artifact_asserts() {
    local output="$1" mode="$2" route
    if [[ "$mode" == all ]]; then
        runtime_type_definition_contract_asserts "$output" || return $?
        for route in direct calli getter getter-calli virtual-getter constrained-getter address virtual-address flow-direct flow-virtual; do
            runtime_type_definition_route_asserts "$output" "$route" || return $?
        done
        gate_output witness "$output" 'type-definition-virtual-address-bind-null:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'type-definition-flow-virtual-bind-null:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'type-definition-flow-direct-bind-null:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'Runtime type-definition routes end' --count 1 --policy crlf || return $?
    fi
    gate_output witness "$output" '== Runtime type-definition reflection ==' --count 1 --policy crlf || return $?
    runtime_type_definition_route_asserts "$output" reflection || return $?
    gate_output witness "$output" 'Runtime type-definition reflection end' --count 1 --policy crlf
}

runtime_type_definition_policy_asserts() {
    local output="$1" mode="$2" route label
    runtime_type_definition_artifact_asserts "$output" "$mode" || return $?
    for label in native-reflection-row native-getter-lookup-unavailable; do
        gate_output witness "$output" "type-definition-$label:True" --count 1 --policy crlf || return $?
    done
    local routes=(reflection)
    if [[ "$mode" == all ]]; then
        routes+=(public direct calli getter getter-calli virtual-getter constrained-getter address virtual-address flow-direct flow-virtual)
    fi
    for route in "${routes[@]}"; do
        for label in enum nested-enum closed-enum array-argument-enum enum-argument-enum int-comparer string-comparer; do
            gate_output witness "$output" "type-definition-native-$route-unsupported-$label:True" --count 1 --policy crlf || return $?
        done
    done
    if [[ "$mode" == all ]]; then
        for label in native-address-row native-address-invoke; do
            gate_output witness "$output" "type-definition-$label:True" --count 1 --policy crlf || return $?
        done
        for route in getter-address flow-direct flow-virtual; do
            for label in row invoke; do
                gate_output witness "$output" "type-definition-native-$route-$label:True" --count 1 --policy crlf || return $?
            done
        done
    fi
}

runtime_type_definition_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out flag oracle
    shift
    mkdir -p "$root"
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local corelib
    corelib=$(resolve_net10_corelib) || return $?
    dotnet "$fixture" --runtime-type-definition-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" || return $?

    # The public Type API remains unsupported natively; it is only the CLR oracle here.
    for mode in public public-scope; do
        input="$root/input-$mode"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "--runtime-type-definition-$mode" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        gate_capture_ok "$root/$mode" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
    done
    runtime_type_definition_contract_asserts "$root/public.stdout" || return $?
    for mode in delegator custom-false custom-true enum nested-enum closed-enum array-argument-enum enum-argument-enum int-comparer string-comparer; do
        gate_output witness "$root/public-scope.stdout" "type-definition-public-scope-$mode:True" --count 1 --policy crlf || return $?
    done
    gate_capture "$root/public-unsupported" invoke_cli "$root/input-public/SelfHostPrimSubset.dll" \
        --auto-ref --no-ildiet -o "$root/public-unsupported-output" || return $?
    gate_output status "$(cat "$root/public-unsupported.status")" 2 || return $?
    gate_output match-record "$root/public-unsupported.stderr" \
        'error: .*System.Type::get_IsTypeDefinition\(\) has no intrinsic mapping yet.*' --count 1 --policy crlf || return $?

    for mode in all reflection; do
        flag=--runtime-type-definition
        oracle=--runtime-type-definition-oracle
        if [[ "$mode" == reflection ]]; then
            flag+=-reflection
            oracle+=-reflection
        fi
        input="$root/oracle-$mode-input"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "$oracle" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        gate_capture_ok "$root/oracle-$mode" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
        runtime_type_definition_artifact_asserts "$root/oracle-$mode.stdout" "$mode" || return $?
        if [[ "$mode" == all ]]; then
            gate_output prefix "$root/oracle-$mode.stdout" "$root/public.stdout" \
                --before 'type-definition-direct-object:True' --policy crlf || return $?
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
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" RuntimeTypeDefinition || return $?
                runner=(node "$out/RuntimeTypeDefinition.js")
            else
                compile_console "$out" RuntimeTypeDefinition || return $?
                runner=("$out/RuntimeTypeDefinition$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            runtime_type_definition_artifact_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/oracle-$mode.stdout" "$out/native.stdout" --policy crlf || return $?
            gate_capture_ok "$out/policy" run_bounded "${runner[@]}" native-policy || return $?
            runtime_type_definition_policy_asserts "$out/policy.stdout" "$mode" || return $?
        done
    done
}
