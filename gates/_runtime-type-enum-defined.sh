#!/usr/bin/env bash
# Exact retained RuntimeType enum-defined queries preserve strict boxed-type compatibility and raw/callvirt validation order.
runtime_type_enum_defined_route_asserts() {
    local output="$1" route="$2" label
    for label in range wide aliases undefined done; do
        gate_output witness "$output" "enum-defined-$route-$label:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" "enum-defined-$route-0-0:ArgumentNullException/value/80004003" --count 1 --policy crlf || return $?
    gate_output witness "$output" "enum-defined-$route-9-31:ArgumentException/enumType/80070057" --count 1 --policy crlf

}

runtime_type_enum_defined_null_asserts() {
    local output="$1" route="$2" label
    for label in null-valid null-value null-invalid; do
        gate_output witness "$output" "enum-defined-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_enum_defined_contract_asserts() {
    local output="$1"
    gate_output witness "$output" '== Runtime enum defined ==' --count 1 --policy crlf || return $?
    runtime_type_enum_defined_route_asserts "$output" public || return $?
    runtime_type_enum_defined_null_asserts "$output" public || return $?
    gate_output witness "$output" 'Runtime enum defined end' --count 1 --policy crlf
}

runtime_type_enum_defined_artifact_asserts() {
    local output="$1" mode="$2" route label
    if [[ "$mode" == all ]]; then
        runtime_type_enum_defined_contract_asserts "$output" || return $?
        for route in direct calli virtual virtual-calli constrained address virtual-address flow-direct flow-virtual; do
            runtime_type_enum_defined_route_asserts "$output" "$route" || return $?
        done
        for route in direct calli virtual virtual-calli constrained virtual-address flow-virtual; do
            runtime_type_enum_defined_null_asserts "$output" "$route" || return $?
        done
        for label in address-bind-null virtual-address-bind-null flow-direct-bind-null flow-virtual-bind-null; do
            gate_output witness "$output" "enum-defined-$label:True" --count 1 --policy crlf || return $?
        done
        gate_output witness "$output" 'Runtime enum defined routes end' --count 1 --policy crlf || return $?
    fi
    gate_output witness "$output" '== Runtime enum defined reflection ==' --count 1 --policy crlf || return $?
    runtime_type_enum_defined_route_asserts "$output" reflection || return $?
    runtime_type_enum_defined_null_asserts "$output" reflection || return $?
    gate_output witness "$output" 'Runtime enum defined reflection end' --count 1 --policy crlf
}

runtime_type_enum_defined_policy_asserts() {
    local output="$1" mode="$2" route label
    runtime_type_enum_defined_artifact_asserts "$output" "$mode" || return $?
    if [[ "$mode" == all ]]; then
        for route in address virtual-address flow-direct flow-virtual; do
            for label in row invoke; do
                gate_output witness "$output" "enum-defined-native-$route-$label:True" --count 1 --policy crlf || return $?
            done
        done
    else
        for label in native-reflection-wrapper-row native-runtime-lookup-unavailable; do
            gate_output witness "$output" "enum-defined-$label:True" --count 1 --policy crlf || return $?
        done
    fi
}

runtime_type_enum_defined_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out flag
    shift
    mkdir -p "$root"
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib
    corelib=$(resolve_net10_corelib) || return $?
    dotnet "$fixture" --runtime-type-enum-defined-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" || return $?
    input="$root/public-input"
    mkdir -p "$input"
    cp "$original/"* "$input/" || return $?
    dotnet "$fixture" --runtime-type-enum-defined-public "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture_ok "$root/public" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
    runtime_type_enum_defined_contract_asserts "$root/public.stdout" || return $?
    for mode in all reflection; do
        flag=--runtime-type-enum-defined
        local oracle=--runtime-type-enum-defined-oracle
        if [[ "$mode" == reflection ]]; then flag+=-reflection; oracle+=-reflection; fi
        input="$root/oracle-$mode-input"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "$oracle" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        gate_capture_ok "$root/oracle-$mode" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
        runtime_type_enum_defined_artifact_asserts "$root/oracle-$mode.stdout" "$mode" || return $?
        if [[ "$mode" == all ]]; then
            gate_output prefix "$root/oracle-$mode.stdout" "$root/public.stdout" \
                --before 'enum-defined-direct-0-0:ArgumentNullException/value/80004003' --policy crlf || return $?
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
                compile_console_wasm "$out" RuntimeTypeEnumDefined || return $?
                runner=(node "$out/RuntimeTypeEnumDefined.js")
            else
                compile_console "$out" RuntimeTypeEnumDefined || return $?
                runner=("$out/RuntimeTypeEnumDefined$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            runtime_type_enum_defined_artifact_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/oracle-$mode.stdout" "$out/native.stdout" --policy crlf || return $?
            gate_capture_ok "$out/policy" run_bounded "${runner[@]}" native-policy || return $?
            runtime_type_enum_defined_policy_asserts "$out/policy.stdout" "$mode" || return $?
        done
    done
}

# Malformed metadata is compiled only; none of these artifacts executes on CLR/native.
runtime_type_enum_defined_negative_gate() {
    local root="$1" mode input
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib
    corelib=$(resolve_net10_corelib) || return $?
    for mode in return-int return-object parameter-string parameter-byref static explicit vararg return-modifier address-return-int; do
        input="$root/$mode-input"
        mkdir -p "$input"
        cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        dotnet "$fixture" --runtime-type-enum-defined-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
        gate_capture "$root/$mode" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/$mode-output" || return $?
        gate_output status "$(cat "$root/$mode.status")" 2 || return $?
        gate_output match-record "$root/$mode.stderr" 'error: .*(IsEnumDefined|RuntimeType enum defined query requires its exact MemberRef signature).*' --count 1 --policy crlf || return $?
    done
}
