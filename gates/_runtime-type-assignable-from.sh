#!/usr/bin/env bash
# Exact retained RuntimeType assignable-from queries preserve existing type relations and raw/callvirt validation order.
runtime_type_assignable_from_route_asserts() {
    local output="$1" route="$2" label
    for label in identity base reverse-base interface reverse-interface missing-interface covariance contravariance wrong-variance \
        array array-variance md-array md-generic integral-array nullable reverse-nullable unbound definition-marker \
        open-constructed parameter-base parameter-interface null-source pointer byref done; do
        gate_output witness "$output" "assignable-from-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_assignable_from_null_asserts() {
    local output="$1" route="$2" label
    for label in null-runtime null-both nonnull-null; do
        gate_output witness "$output" "assignable-from-$route-$label:True" --count 1 --policy crlf || return $?
    done
}

runtime_type_assignable_from_contract_asserts() {
    local output="$1"
    gate_output witness "$output" '== Runtime assignable from ==' --count 1 --policy crlf || return $?
    runtime_type_assignable_from_route_asserts "$output" public || return $?
    runtime_type_assignable_from_null_asserts "$output" public || return $?
    gate_output witness "$output" 'Runtime assignable from end' --count 1 --policy crlf
}

runtime_type_assignable_from_artifact_asserts() {
    local output="$1" mode="$2" route label
    if [[ "$mode" == all ]]; then
        runtime_type_assignable_from_contract_asserts "$output" || return $?
        for route in direct calli virtual virtual-calli constrained address virtual-address flow-direct flow-virtual; do
            runtime_type_assignable_from_route_asserts "$output" "$route" || return $?
        done
        for route in direct calli virtual virtual-calli constrained virtual-address flow-virtual; do
            runtime_type_assignable_from_null_asserts "$output" "$route" || return $?
        done
        for label in address-bind-null virtual-address-bind-null flow-direct-bind-null flow-virtual-bind-null; do
            gate_output witness "$output" "assignable-from-$label:True" --count 1 --policy crlf || return $?
        done
        gate_output witness "$output" 'Runtime assignable from routes end' --count 1 --policy crlf || return $?
    fi
    gate_output witness "$output" '== Runtime assignable from reflection ==' --count 1 --policy crlf || return $?
    runtime_type_assignable_from_route_asserts "$output" reflection || return $?
    runtime_type_assignable_from_null_asserts "$output" reflection || return $?
    gate_output witness "$output" 'Runtime assignable from reflection end' --count 1 --policy crlf
}

runtime_type_assignable_from_policy_asserts() {
    local output="$1" mode="$2" route label
    runtime_type_assignable_from_artifact_asserts "$output" "$mode" || return $?
    if [[ "$mode" == all ]]; then
        for route in address virtual-address flow-direct flow-virtual; do
            for label in row invoke invoke-null-target bound dynamic; do
                gate_output witness "$output" "assignable-from-native-$route-$label:True" --count 1 --policy crlf || return $?
            done
        done
    else
        for label in native-reflection-wrapper-row native-runtime-lookup-unavailable; do
            gate_output witness "$output" "assignable-from-$label:True" --count 1 --policy crlf || return $?
        done
    fi
}

runtime_type_assignable_from_routes_gate() {
    local root="$1" original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" mode axis input out flag
    shift
    mkdir -p "$root"
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib
    corelib=$(resolve_net10_corelib) || return $?
    dotnet "$fixture" --runtime-type-assignable-from-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" || return $?
    dotnet "$fixture" --runtime-type-assignable-from-raw-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" || return $?
    input="$root/public-input"
    mkdir -p "$input"
    cp "$original/"* "$input/" || return $?
    dotnet "$fixture" --runtime-type-assignable-from-public "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture_ok "$root/public" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
    runtime_type_assignable_from_contract_asserts "$root/public.stdout" || return $?
    for mode in all reflection; do
        flag=--runtime-type-assignable-from
        local oracle=--runtime-type-assignable-from-oracle
        if [[ "$mode" == reflection ]]; then flag+=-reflection; oracle+=-reflection; fi
        input="$root/oracle-$mode-input"
        mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "$oracle" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        gate_capture_ok "$root/oracle-$mode" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
        runtime_type_assignable_from_artifact_asserts "$root/oracle-$mode.stdout" "$mode" || return $?
        if [[ "$mode" == all ]]; then
            gate_output prefix "$root/oracle-$mode.stdout" "$root/public.stdout" \
                --before 'assignable-from-direct-identity:True' --policy crlf || return $?
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
            local raw_query virtual_query
            raw_query=$(awk '
                { sub(/\r$/, "") }
                /^(inline )?int32_t AssignableFromChecks_ReflectionLeaf_m[0-9]+\(t_System_Type\* a0, ?t_System_Type\* a1\);$/ {
                    name = $0; sub(/^(inline )?int32_t /, "", name); sub(/\(.*/, "", name); count++
                }
                END { if (count != 1) exit 1; print name }
            ' "$out/generated.h") || return $?
            virtual_query=
            if [[ "$mode" == all ]]; then
                virtual_query=$(awk '
                    { sub(/\r$/, "") }
                    /^(inline )?int32_t AssignableFromChecks_Virtual_m[0-9]+\(t_System_Type\* a0, ?t_System_Type\* a1\);$/ {
                        name = $0; sub(/^(inline )?int32_t /, "", name); sub(/\(.*/, "", name); count++
                    }
                    END { if (count != 1) exit 1; print name }
                ' "$out/generated.h") || return $?
            else
                # Reflection-only closure must not be expanded to manufacture a virtual route.
                if grep -Eq 'AssignableFromChecks_Virtual_m[0-9]+\(' "$out/generated.h"; then
                    echo "Unexpected direct virtual route in reflection-only assignability fixture" >&2
                    return 1
                fi
            fi
            {
                printf '#define DN2CPP_ASSIGNABILITY_RAW_QUERY %s\n' "$raw_query"
                if [[ -n "$virtual_query" ]]; then
                    printf '#define DN2CPP_ASSIGNABILITY_VIRTUAL_QUERY %s\n' "$virtual_query"
                fi
                cat gates/fixtures/runtime-type-assignable-from.cpp
            } > "$out/generated_assignable_from_checks.cpp" || return $?
            awk '
                BEGIN { print "void dn2cpp_assignability_boundary_checks();" }
                { print }
                /^[[:space:]]*dn2cpp_runtime_init\(/ {
                    print "    dn2cpp_assignability_boundary_checks();"; calls++
                }
                END { if (calls != 1) exit 1 }
            ' "$out/generated.cpp" > "$out/generated.checked.cpp.tmp" || return $?
            mv "$out/generated.checked.cpp.tmp" "$out/generated.cpp" || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" RuntimeTypeAssignableFrom || return $?
                runner=(node "$out/RuntimeTypeAssignableFrom.js")
            else
                compile_console "$out" RuntimeTypeAssignableFrom || return $?
                runner=("$out/RuntimeTypeAssignableFrom$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            runtime_type_assignable_from_artifact_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/oracle-$mode.stdout" "$out/native.stdout" --policy crlf || return $?
            gate_capture_ok "$out/policy" run_bounded "${runner[@]}" native-policy || return $?
            runtime_type_assignable_from_policy_asserts "$out/policy.stdout" "$mode" || return $?
        done
    done
}

# Malformed metadata is compiled only; none of these artifacts executes on CLR/native.
runtime_type_assignable_from_negative_gate() {
    local root="$1" mode input
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib
    corelib=$(resolve_net10_corelib) || return $?
    for mode in return-int return-object parameter-string parameter-object parameter-byref parameter-modifier static explicit vararg return-modifier address-return-int; do
        input="$root/$mode-input"
        mkdir -p "$input"
        cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        dotnet "$fixture" --runtime-type-assignable-from-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
        gate_capture "$root/$mode" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/$mode-output" || return $?
        gate_output status "$(cat "$root/$mode.status")" 2 || return $?
        gate_output match-record "$root/$mode.stderr" 'error: .*(IsAssignableFrom|RuntimeType assignability query requires its exact MemberRef signature).*' --count 1 --policy crlf || return $?
    done
    # This sibling is valid managed IL, not the retained Type-overload adapter.
    input="$root/typeinfo-control-input"
    mkdir -p "$input"
    cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
    dotnet "$fixture" --runtime-type-assignable-from-negative "$input/SelfHostPrimSubset.dll" "$corelib" parameter-typeinfo || return $?
    local out="$root/typeinfo-control-output"
    gate_capture_ok "$root/typeinfo-control" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$out" || return $?
    grep -Eq 'RuntimeType_IsAssignableFrom_m[0-9]+\(t_System_RuntimeType\* [^,]+, t_System_Reflection_TypeInfo\*' "$out/generated.h" || return $?
    if grep -Fq 'dn2cpp_runtime_type_is_assignable_from(' "$out"/generated*.cpp "$out/generated.h"; then
        echo "TypeInfo sibling must not use the Type-overload adapter" >&2
        return 1
    fi

}
