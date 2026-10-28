#!/usr/bin/env bash
# CAD row data only; constructor/argument blobs and arbitrary overrides are not promised.
custom_attribute_data_member_asserts() {
    local output="$1" route="$2" label
    for label in row-0 row-1 row-2 row-3 row-4 row-5 readonly-0 readonly-1 readonly-2 readonly-3 readonly-4 readonly-5 immutable-0 immutable-1 immutable-2 immutable-3 immutable-4 immutable-5 enumeration-0 enumeration-1 enumeration-2 enumeration-3 enumeration-4 enumeration-5 empty empty-unchanged empty-negative empty-mutations not-inherited no-construction null; do
        gate_output witness "$output" "cad-member-$route-$label:True" --count 1 --policy crlf || return $?
    done
}
custom_attribute_data_member_routes_gate() {
    local root="$1"; shift
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" corelib mode axis input out route
    corelib=$(resolve_net10_corelib) || return $?
    dotnet "$fixture" --cad-member-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$corelib" || return $?
    for mode in oracle custom-oracle; do
        input="$root/$mode-input"; mkdir -p "$input"; cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "--cad-member-$mode" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        gate_capture_ok "$root/$mode" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
    done
    custom_attribute_data_member_asserts "$root/oracle.stdout" public || return $?
    local empty_setter
    empty_setter=$(sed -n 's/^cad-member-public-empty-setter://p' "$root/oracle.stdout" | tr -d '\r')
    [[ "$empty_setter" == System.*Exception ]] || { echo 'Empty setter oracle did not record an exception' >&2; return 1; }
    gate_output witness "$root/custom-oracle.stdout" 'cad-member-custom-clr-override:True' --count 1 --policy crlf || return $?
    gate_output witness "$root/custom-oracle.stdout" 'cad-member-custom-clr-identity:True' --count 1 --policy crlf || return $?
    for mode in all reflection; do
        input="$root/$mode-input"; mkdir -p "$input"; cp "$original/"* "$input/" || return $?
        local flag=--cad-member; [[ "$mode" == all ]] || flag+=-reflection
        dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"; local flags=("$@"); [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            grep -Fq 'dn2cpp_custom_attribute_data_get_custom_attributes(' "$out"/generated*.cpp || return $?
            if [[ "$mode" == reflection ]] && grep -Eq 'DataChecks_(Direct|Pointer|DelegateRoute)_m[0-9]+\(' "$out/generated.h"; then
                echo 'CAD direct routes leaked into reflection-only retention test' >&2; return 1
            fi
            cp gates/fixtures/custom-attribute-data-member.cpp "$out/generated_cad_checks.cpp" || return $?
            awk '
                BEGIN { print "void dn2cpp_cad_member_boundary_checks();" }
                { print }
                /^[[:space:]]*dn2cpp_runtime_init\(/ { print "    dn2cpp_cad_member_boundary_checks();"; calls++ }
                END { if (calls != 1) exit 1 }
            ' "$out/generated.cpp" > "$out/generated.checked.cpp.tmp" || return $?
            mv "$out/generated.checked.cpp.tmp" "$out/generated.cpp" || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then compile_console_wasm "$out" CadMember || return $?; runner=(node "$out/CadMember.js")
            else compile_console "$out" CadMember || return $?; runner=("$out/CadMember$EXE_EXT"); fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            gate_output witness "$out/native.stderr" "cad-member-native-boundaries:passed" --count 1 --policy crlf || return $?
            local routes='direct calli delegate reflection'
            if [[ "$mode" == all ]]; then
                gate_output prefix "$out/native.stdout" "$root/oracle.stdout" --before 'cad-member-direct-row-0:True' --policy crlf || return $?
            else routes=reflection; fi
            for route in $routes; do
                custom_attribute_data_member_asserts "$out/native.stdout" "$route" || return $?
                gate_output witness "$out/native.stdout" "cad-member-$route-empty-setter:$empty_setter" --count 1 --policy crlf || return $?
                gate_output witness "$out/native.stdout" "cad-member-$route-custom-refused:True" --count 1 --policy crlf || return $?
            done
        done
    done
}
custom_attribute_data_member_negative_gate() {
    local root="$1" corelib mode input
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in return-array wrong-list wrong-element object byref instance generic modifier address-return-array address-wrong-list address-modifier; do
        input="$root/$mode-input"; mkdir -p "$input"; cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        dotnet "$fixture" --cad-member-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
        gate_capture "$root/$mode" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/$mode-output" || return $?
        gate_output status "$(cat "$root/$mode.status")" 2 || return $?
        gate_output match-record "$root/$mode.stderr" '.*(CustomAttributeData|GetCustomAttributes).*' --count any --policy crlf || return $?
    done
    input="$root/noalloc-input"; mkdir -p "$input"; cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
    dotnet "$fixture" --cad-member-noalloc "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture "$root/noalloc" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/noalloc-output" || return $?
    gate_output status "$(cat "$root/noalloc.status")" 2 || return $?
    gate_output match-record "$root/noalloc.stderr" '.*NoAlloc.*CustomAttributeData::GetCustomAttributes emits dn2cpp_alloc\(.*' --count any --policy crlf
}
