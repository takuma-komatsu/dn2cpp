#!/usr/bin/env bash
# RuntimeType's retained getter preserves native formal owners without CoreCLR MethodDesc objects.
formal_declaring_contract_asserts() {
    local output="$1" label
    gate_output witness "$output" '== Formal declaring method contract ==' --count 1 --policy crlf || return $?
    for label in typical contexts positions type-formal nonformal null repeated; do
        gate_output witness "$output" "formal-declaring-public-$label:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'Formal declaring method contract end' --count 1 --policy crlf
}

formal_declaring_public_asserts() {
    local output="$1" route label
    formal_declaring_contract_asserts "$output" || return $?
    for route in direct pointer address virtual-address flowing-virtual-address; do
        for label in typical type-formal ordinary open closed array pointer byref repeated; do
            gate_output witness "$output" "formal-declaring-$route-$label:True" --count 1 --policy crlf || return $?
        done
    done
    for label in direct-null pointer-null delegate-reflection-owner delegate-reflection-typical delegate-reflection-type-formal delegate-reflection-ordinary delegate-reflection-null virtual-delegate-reflection flowing-delegate-reflection; do
        gate_output witness "$output" "formal-declaring-$label:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'Formal declaring method public routes end' --count 1 --policy crlf
}

formal_declaring_private_asserts() {
    local output="$1" route label
    formal_declaring_contract_asserts "$output" || return $?
    for route in direct pointer reflection; do
        for label in null ordinary type-formal method-formal; do
            gate_output witness "$output" "formal-declaring-private-$route-$label:True" --count 1 --policy crlf || return $?
        done
    done
    gate_output witness "$output" 'Formal declaring method private boundary end' --count 1 --policy crlf
}

formal_declaring_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out label
    mkdir -p "$root"
    gate_capture_ok "$root/public-oracle" run_bounded dotnet "$original/SelfHostPrimSubset.dll" formal-declaring-public || return $?
    gate_capture_ok "$root/contract-oracle" run_bounded dotnet "$original/SelfHostPrimSubset.dll" formal-declaring-contract || return $?
    formal_declaring_public_asserts "$root/public-oracle.stdout" || return $?
    for mode in public private; do
        input="$root/input-$mode"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
            "--formal-declaring-$mode" "$input/SelfHostPrimSubset.dll" "$(resolve_net10_corelib)" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"
            local flags=()
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" FormalDeclaringMethod || return $?
                runner=(node "$out/FormalDeclaringMethod.js")
            else
                compile_console "$out" FormalDeclaringMethod || return $?
                runner=("$out/FormalDeclaringMethod$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            if [[ "$mode" == public ]]; then
                formal_declaring_public_asserts "$out/native.stdout" || return $?
                gate_output compare "$root/public-oracle.stdout" "$out/native.stdout" --policy crlf || return $?
                gate_capture_ok "$out/boundary" run_bounded "${runner[@]}" native-boundary || return $?
                for label in address-null virtual-address-null flowing-virtual-address-null named-reflection-unavailable; do
                    gate_output witness "$out/boundary.stdout" "formal-declaring-$label:True" --count 1 --policy crlf || return $?
                done
                formal_declaring_public_asserts "$out/boundary.stdout" || return $?
            else
                formal_declaring_private_asserts "$out/native.stdout" || return $?
                gate_output prefix "$out/native.stdout" "$root/contract-oracle.stdout" \
                    --before 'formal-declaring-private-direct-null:True' --policy crlf || return $?
            fi
        done
    done
}
