#!/usr/bin/env bash
# Exact retained RuntimeType type-code queries bypass CoreCLR cache state.
runtime_type_type_code_route_asserts() {
    local output="$1" route="$2" label
    for label in bool char sbyte byte short ushort int uint long ulong float double decimal datetime dbnull string \
        enum-sbyte enum-byte enum-short enum-ushort enum-int enum-uint enum-long enum-ulong typecode-enum nested-closed-enum \
        object void nint nuint enum-base value-base struct interface definition constructed nullable-int nullable-enum nullable-definition \
        formal method-formal array mdarray enum-array formal-array pointer byref formal-pointer method-byref function unmanaged-function delegate typed-reference handle repeat; do
        gate_output witness "$output" "type-code-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_type_code_null_asserts() {
    local output="$1" route="$2" label
    for label in null default-handle; do
        gate_output witness "$output" "type-code-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_type_code_contract_asserts() {
    local output="$1"
    gate_output witness "$output" '== Runtime type code ==' --count 1 --policy crlf || return $?
    runtime_type_type_code_route_asserts "$output" public || return $?
    runtime_type_type_code_null_asserts "$output" public || return $?
    gate_output witness "$output" 'type-code-static-null-empty:True' --count 1 --policy crlf || return $?
    gate_output witness "$output" 'type-code-generic:True' --count 1 --policy crlf || return $?
    gate_output witness "$output" 'Runtime type code end' --count 1 --policy crlf
}

runtime_type_type_code_artifact_asserts() {
    local output="$1" mode="$2" route label
    if [[ "$mode" == all ]]; then
        runtime_type_type_code_contract_asserts "$output" || return $?
        for route in direct calli virtual virtual-calli constrained address virtual-address flow-direct flow-virtual; do
            runtime_type_type_code_route_asserts "$output" "$route" || return $?
        done
        for route in direct calli virtual virtual-calli constrained virtual-address flow-virtual; do
            runtime_type_type_code_null_asserts "$output" "$route" || return $?
        done
        for label in address-bind-null virtual-address-bind-null flow-direct-bind-null flow-virtual-bind-null; do
            gate_output witness "$output" "type-code-$label:True" --count 1 --policy crlf || return $?
        done
        gate_output witness "$output" 'Runtime type code routes end' --count 1 --policy crlf || return $?
    fi
    gate_output witness "$output" '== Runtime type code reflection ==' --count 1 --policy crlf || return $?
    runtime_type_type_code_route_asserts "$output" reflection || return $?
    runtime_type_type_code_null_asserts "$output" reflection || return $?
    gate_output witness "$output" 'Runtime type code reflection end' --count 1 --policy crlf
}

runtime_type_type_code_policy_asserts() {
    local output="$1" mode="$2" route label
    runtime_type_type_code_artifact_asserts "$output" "$mode" || return $?
    gate_output witness "$output" 'type-code-native-open-enum-policy:True' --count 1 --policy crlf || return $?
    if [[ "$mode" == all ]]; then
        for route in address virtual-address flow-direct flow-virtual; do
            for label in row invoke; do
                gate_output witness "$output" "type-code-native-$route-$label:True" --count 1 --policy crlf || return $?
            done
        done
    else
        for label in native-reflection-wrapper-row native-runtime-lookup-unavailable; do
            gate_output witness "$output" "type-code-$label:True" --count 1 --policy crlf || return $?
        done
    fi
}

runtime_type_type_code_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out flag
    shift
    mkdir -p "$root"
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib
    corelib=$(resolve_net10_corelib) || return $?
    dotnet "$fixture" --runtime-type-type-code-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" || return $?
    input="$root/public-input"
    mkdir -p "$input"
    cp "$original/"* "$input/" || return $?
    dotnet "$fixture" --runtime-type-type-code-public "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture_ok "$root/public" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
    runtime_type_type_code_contract_asserts "$root/public.stdout" || return $?
    input="$root/public-scope-input"
    mkdir -p "$input"
    cp "$original/"* "$input/" || return $?
    dotnet "$fixture" --runtime-type-type-code-public-scope "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture_ok "$root/public-scope" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
    for mode in delegator override open-enum open-constructed; do
        gate_output witness "$root/public-scope.stdout" "type-code-public-scope-$mode:True" --count 1 --policy crlf || return $?
    done
    for mode in return-int address-return-int return-object return-enum return-class return-byref static explicit vararg return-modifier; do
        input="$root/negative-$mode-input"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" --runtime-type-type-code-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
        gate_capture "$root/negative-$mode" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/negative-$mode-output" || return $?
        gate_output status "$(cat "$root/negative-$mode.status")" 2 || return $?
        gate_output match-record "$root/negative-$mode.stderr" 'error: (.*GetTypeCodeImpl.*|RuntimeType type-code query requires its exact MemberRef signature)' --count 1 --policy crlf || return $?
    done
    for mode in all reflection; do
        flag=--runtime-type-type-code
        local oracle=--runtime-type-type-code-oracle
        if [[ "$mode" == reflection ]]; then flag+=-reflection; oracle+=-reflection; fi
        input="$root/oracle-$mode-input"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "$oracle" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        gate_capture_ok "$root/oracle-$mode" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
        runtime_type_type_code_artifact_asserts "$root/oracle-$mode.stdout" "$mode" || return $?
        if [[ "$mode" == all ]]; then
            gate_output prefix "$root/oracle-$mode.stdout" "$root/public.stdout" \
                --before 'type-code-direct-bool:True' --policy crlf || return $?
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
                compile_console_wasm "$out" RuntimeTypeTypeCode || return $?
                runner=(node "$out/RuntimeTypeTypeCode.js")
            else
                compile_console "$out" RuntimeTypeTypeCode || return $?
                runner=("$out/RuntimeTypeTypeCode$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            runtime_type_type_code_artifact_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/oracle-$mode.stdout" "$out/native.stdout" --policy crlf || return $?
            gate_capture_ok "$out/policy" run_bounded "${runner[@]}" native-policy || return $?
            runtime_type_type_code_policy_asserts "$out/policy.stdout" "$mode" || return $?
        done
    done
}
