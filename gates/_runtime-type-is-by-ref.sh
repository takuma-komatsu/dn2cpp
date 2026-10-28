#!/usr/bin/env bash
# Retained by-reference predicates inspect only the outer signature kind, with managed null checks.
runtime_type_is_by_ref_route_asserts() {
    local output="$1" route="$2" label
    for label in rank-one-shape category-relations object void value string enum array-base definition nested-definition closed-generic open-constructed \
        type-formal method-formal function-managed function-unmanaged byref-like byref-like-definition function-byref-signature array reference-array jagged-array mdarray-one mdarray-two pointer void-pointer byref \
        formal-array formal-mdarray-one formal-mdarray-two formal-pointer formal-byref method-array method-pointer method-byref open-array nested-compound \
        function-pointer function-byref function-array array-byref mdarray-byref pointer-byref byref-like-byref reference-byref array-pointer unmanaged-function-byref handle-type handle-array handle-byref repeated; do
        gate_output witness "$output" "is-by-ref-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_is_by_ref_null_asserts() {
    local output="$1" route="$2" label
    for label in null default-handle; do
        gate_output witness "$output" "is-by-ref-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_is_by_ref_contract_asserts() {
    local output="$1" label
    gate_output witness "$output" '== Runtime is-by-ref predicates ==' --count 1 --policy crlf || return $?
    runtime_type_is_by_ref_route_asserts "$output" public || return $?
    runtime_type_is_by_ref_null_asserts "$output" public || return $?
    for label in top-level generic; do
        gate_output witness "$output" "is-by-ref-$label:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'Runtime is-by-ref predicates end' --count 1 --policy crlf
}

runtime_type_is_by_ref_artifact_asserts() {
    local output="$1" mode="$2" route
    if [[ "$mode" == all ]]; then
        runtime_type_is_by_ref_contract_asserts "$output" || return $?
        for route in direct calli getter getter-calli virtual-getter constrained-getter address virtual-address flow-direct flow-virtual; do
            runtime_type_is_by_ref_route_asserts "$output" "$route" || return $?
        done
        for route in direct calli getter getter-calli virtual-getter constrained-getter address virtual-address flow-virtual; do
            runtime_type_is_by_ref_null_asserts "$output" "$route" || return $?
        done
        gate_output witness "$output" 'is-by-ref-virtual-address-bind-null:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'is-by-ref-flow-virtual-bind-null:True' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'Runtime is-by-ref routes end' --count 1 --policy crlf || return $?
    fi
    gate_output witness "$output" '== Runtime is-by-ref reflection ==' --count 1 --policy crlf || return $?
    runtime_type_is_by_ref_route_asserts "$output" reflection || return $?
    runtime_type_is_by_ref_null_asserts "$output" reflection || return $?
    gate_output witness "$output" 'Runtime is-by-ref reflection end' --count 1 --policy crlf
}

runtime_type_is_by_ref_policy_asserts() {
    local output="$1" mode="$2" route label
    runtime_type_is_by_ref_artifact_asserts "$output" "$mode" || return $?
    for label in native-safety-reflection native-reflection-row native-getter-lookup-unavailable; do
        gate_output witness "$output" "is-by-ref-$label:True" --count 1 --policy crlf || return $?
    done
    if [[ "$mode" == all ]]; then
        for route in direct calli getter address; do
            gate_output witness "$output" "is-by-ref-native-safety-$route:True" --count 1 --policy crlf || return $?
        done
        for label in native-address-row native-address-invoke flow-direct-bind-null; do
            gate_output witness "$output" "is-by-ref-$label:True" --count 1 --policy crlf || return $?
        done
        for route in getter-address flow-direct flow-virtual; do
            for label in row invoke; do
                gate_output witness "$output" "is-by-ref-native-$route-$label:True" --count 1 --policy crlf || return $?
            done
        done
    fi
}

runtime_type_is_by_ref_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out command flag
    shift
    mkdir -p "$root"
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        --runtime-type-is-by-ref-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" || return $?
    local corelib
    corelib=$(resolve_net10_corelib) || return $?
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    input="$root/oracle-public-input"
    mkdir -p "$input"
    cp "$original/"* "$input/" || return $?
    dotnet "$fixture" --runtime-type-is-by-ref-public "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture_ok "$root/public-only" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
    runtime_type_is_by_ref_contract_asserts "$root/public-only.stdout" || return $?
    for mode in all reflection; do
        flag=--runtime-type-is-by-ref
        local oracle=--runtime-type-is-by-ref-oracle
        if [[ "$mode" == reflection ]]; then flag+=-reflection; oracle+=-reflection; fi
        input="$root/oracle-$mode-input"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "$oracle" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        gate_capture_ok "$root/public-$mode" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
        runtime_type_is_by_ref_artifact_asserts "$root/public-$mode.stdout" "$mode" || return $?
        if [[ "$mode" == all ]]; then
            gate_output prefix "$root/public-$mode.stdout" "$root/public-only.stdout" \
                --before 'is-by-ref-direct-rank-one-shape:True' --policy crlf || return $?
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
            cp gates/fixtures/runtime-type-is-by-ref.cpp "$out/generated_is_by_ref_checks.cpp" || return $?
            awk '
                BEGIN { print "void dn2cpp_is_by_ref_runtime_checks();" }
                { print }
                /^[[:space:]]*dn2cpp_runtime_init\(/ {
                    print "    dn2cpp_is_by_ref_runtime_checks();"; calls++
                }
                END { if (calls != 1) exit 1 }
            ' "$out/generated.cpp" > "$out/generated.checked.cpp.tmp" || return $?
            mv "$out/generated.checked.cpp.tmp" "$out/generated.cpp" || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" RuntimeTypeIsByRef || return $?
                runner=(node "$out/RuntimeTypeIsByRef.js")
            else
                compile_console "$out" RuntimeTypeIsByRef || return $?
                runner=("$out/RuntimeTypeIsByRef$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            runtime_type_is_by_ref_artifact_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/public-$mode.stdout" "$out/native.stdout" --policy crlf || return $?
            gate_capture_ok "$out/policy" run_bounded "${runner[@]}" native-policy || return $?
            runtime_type_is_by_ref_policy_asserts "$out/policy.stdout" "$mode" || return $?
        done
    done
}

# Malformed references are compile-only inputs and never run on CLR or natively.
runtime_type_is_by_ref_negative_gate() {
    local root="$1" shape input out
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local corelib
    corelib=$(resolve_net10_corelib) || return $?
    for shape in return parameter hasthis vararg return-class address-return address-parameter \
        override-return override-static override-return-class address-override-return; do
        input="$root/input-$shape"
        out="$root/output-$shape"
        mkdir -p "$input"
        cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        dotnet "$fixture" "--runtime-type-is-by-ref-negative-$shape" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        gate_capture "$root/$shape" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$out" || return $?
        gate_output status "$(cat "$root/$shape.status")" 2 || return $?
        gate_output match-record "$root/$shape.stderr" \
            'error: .*(IsByRef|RuntimeType by-reference query requires its exact MemberRef signature).*' --count 1 --policy crlf || return $?
    done
}
