#!/usr/bin/env bash
# The retained IsSZArray helper preserves vector identity and the null Type contract.
runtime_type_szarray_route_asserts() {
    local output="$1" route="$2" label
    for label in value-sz reference-sz jagged rank-one-md rank-two-md zero-bound value class interface array-base open formal formal-sz pointer byref rank-one-distinct handle-sz handle-md null default-handle repeated; do
        gate_output witness "$output" "runtime-type-szarray-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_szarray_contract_asserts() {
    local output="$1"
    gate_output witness "$output" '== Runtime type SZ-array contract ==' --count 1 --policy crlf || return $?
    runtime_type_szarray_route_asserts "$output" public || return $?
    gate_output witness "$output" 'runtime-type-szarray-generic:True' --count 1 --policy crlf || return $?
    gate_output witness "$output" 'Runtime type SZ-array contract end' --count 1 --policy crlf
}

runtime_type_szarray_artifact_asserts() {
    local output="$1" route
    runtime_type_szarray_contract_asserts "$output" || return $?
    for route in direct pointer getter address reflection; do
        runtime_type_szarray_route_asserts "$output" "$route" || return $?
    done
    gate_output witness "$output" 'Runtime type SZ-array routes end' --count 1 --policy crlf
}

runtime_type_szarray_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" axis input out
    shift
    mkdir -p "$root"
    gate_capture_ok "$root/public-oracle" run_bounded dotnet "$original/SelfHostPrimSubset.dll" runtime-type-szarray-artifact || return $?
    runtime_type_szarray_artifact_asserts "$root/public-oracle.stdout" || return $?
    input="$root/input"
    mkdir -p "$input"
    cp "$original/"* "$input/" || return $?
    dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        --runtime-type-szarray "$input/SelfHostPrimSubset.dll" "$(resolve_net10_corelib)" || return $?
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@")
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        local runner=()
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" RuntimeTypeSzArray || return $?
            runner=(node "$out/RuntimeTypeSzArray.js")
        else
            compile_console "$out" RuntimeTypeSzArray || return $?
            runner=("$out/RuntimeTypeSzArray$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        runtime_type_szarray_artifact_asserts "$out/native.stdout" || return $?
        gate_output compare "$root/public-oracle.stdout" "$out/native.stdout" --policy crlf || return $?
        gate_capture_ok "$out/identity" run_bounded "${runner[@]}" native-identity || return $?
        runtime_type_szarray_artifact_asserts "$out/identity.stdout" || return $?
        gate_output witness "$out/identity.stdout" 'runtime-type-szarray-reflection-identity:True' --count 1 --policy crlf || return $?
    done
}
