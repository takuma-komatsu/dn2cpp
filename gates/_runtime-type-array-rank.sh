#!/usr/bin/env bash
# Exact retained RuntimeType rank queries reuse the public native array contract.
runtime_type_array_rank_route_asserts() {
    local output="$1" route="$2" label
    for label in rank1-shape sz md-one md-two md-three reference constructed-array formal-sz formal-md-one formal-md-two \
        method-sz method-md-three nonarray-object nonarray-array-base nonarray-void nonarray-int nonarray-enum \
        nonarray-definition nonarray-constructed nonarray-formal nonarray-method-formal nonarray-pointer \
        nonarray-byref nonarray-array-pointer nonarray-function nonarray-unmanaged-function handle repeat; do
        gate_output witness "$output" "array-rank-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_array_rank_null_asserts() {
    local output="$1" route="$2" label
    for label in null default-handle; do
        gate_output witness "$output" "array-rank-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_array_rank_contract_asserts() {
    local output="$1"
    gate_output witness "$output" '== Runtime array rank ==' --count 1 --policy crlf || return $?
    runtime_type_array_rank_route_asserts "$output" public || return $?
    runtime_type_array_rank_null_asserts "$output" public || return $?
    gate_output witness "$output" 'array-rank-generic:True' --count 1 --policy crlf || return $?
    gate_output witness "$output" 'Runtime array rank end' --count 1 --policy crlf
}

runtime_type_array_rank_artifact_asserts() {
    local output="$1" mode="$2" route label
    if [[ "$mode" == all ]]; then
        runtime_type_array_rank_contract_asserts "$output" || return $?
        for route in direct calli virtual virtual-calli constrained address virtual-address flow-direct flow-virtual; do
            runtime_type_array_rank_route_asserts "$output" "$route" || return $?
        done
        for route in direct calli virtual virtual-calli constrained virtual-address flow-virtual; do
            runtime_type_array_rank_null_asserts "$output" "$route" || return $?
        done
        for label in address-bind-null virtual-address-bind-null flow-direct-bind-null flow-virtual-bind-null; do
            gate_output witness "$output" "array-rank-$label:True" --count 1 --policy crlf || return $?
        done
        gate_output witness "$output" 'Runtime array rank routes end' --count 1 --policy crlf || return $?
    fi
    gate_output witness "$output" '== Runtime array rank reflection ==' --count 1 --policy crlf || return $?
    runtime_type_array_rank_route_asserts "$output" reflection || return $?
    runtime_type_array_rank_null_asserts "$output" reflection || return $?
    gate_output witness "$output" 'Runtime array rank reflection end' --count 1 --policy crlf
}

runtime_type_array_rank_policy_asserts() {
    local output="$1" mode="$2" route label
    runtime_type_array_rank_artifact_asserts "$output" "$mode" || return $?
    if [[ "$mode" == all ]]; then
        for route in address virtual-address flow-direct flow-virtual; do
            for label in row invoke; do
                gate_output witness "$output" "array-rank-native-$route-$label:True" --count 1 --policy crlf || return $?
            done
        done
    else
        for label in native-reflection-wrapper-row native-runtime-lookup-unavailable; do
            gate_output witness "$output" "array-rank-$label:True" --count 1 --policy crlf || return $?
        done
    fi
}

runtime_type_array_rank_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out flag
    shift
    mkdir -p "$root"
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib
    corelib=$(resolve_net10_corelib) || return $?
    dotnet "$fixture" --runtime-type-array-rank-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" || return $?
    input="$root/public-input"
    mkdir -p "$input"
    cp "$original/"* "$input/" || return $?
    dotnet "$fixture" --runtime-type-array-rank-public "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture_ok "$root/public" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
    runtime_type_array_rank_contract_asserts "$root/public.stdout" || return $?
    for mode in all reflection; do
        flag=--runtime-type-array-rank
        local oracle=--runtime-type-array-rank-oracle
        if [[ "$mode" == reflection ]]; then flag+=-reflection; oracle+=-reflection; fi
        input="$root/oracle-$mode-input"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "$oracle" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        gate_capture_ok "$root/oracle-$mode" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
        runtime_type_array_rank_artifact_asserts "$root/oracle-$mode.stdout" "$mode" || return $?
        if [[ "$mode" == all ]]; then
            gate_output prefix "$root/oracle-$mode.stdout" "$root/public.stdout" \
                --before 'array-rank-direct-rank1-shape:True' --policy crlf || return $?
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
                compile_console_wasm "$out" RuntimeTypeArrayRank || return $?
                runner=(node "$out/RuntimeTypeArrayRank.js")
            else
                compile_console "$out" RuntimeTypeArrayRank || return $?
                runner=("$out/RuntimeTypeArrayRank$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            runtime_type_array_rank_artifact_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/oracle-$mode.stdout" "$out/native.stdout" --policy crlf || return $?
            gate_capture_ok "$out/policy" run_bounded "${runner[@]}" native-policy || return $?
            runtime_type_array_rank_policy_asserts "$out/policy.stdout" "$mode" || return $?
        done
    done
}
