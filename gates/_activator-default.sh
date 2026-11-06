#!/usr/bin/env bash
# Retained parameterless Activator construction shares native visibility, wrapping and normal Nullable boxing.
activator_default_route_asserts() {
    local output="$1" route="$2" n w label
    for n in 0 1; do for w in 0 1; do
        for label in class private throw-public throw-private plain explicit int nullable-int nullable-enum nullable-struct missing abstract interface open null; do
            gate_output witness "$output" "activator-default-$route-$n-$w-$label:True" --count 1 --policy crlf || return $?
        done
    done; done
}
activator_default_contract_asserts() {
    local output="$1" label
    gate_output witness "$output" '== Activator default construction ==' --count 1 --policy crlf || return $?
    activator_default_route_asserts "$output" public || return $?
    for label in public-one-arg-nullable public-two-arg-nullable public-array-sibling public-array-sibling-address; do
        gate_output witness "$output" "activator-default-$label:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" 'Activator default construction end' --count 1 --policy crlf
}
activator_default_artifact_asserts() {
    local output="$1" mode="$2" route n w
    if [[ "$mode" == all ]]; then
        activator_default_contract_asserts "$output" || return $?
        for route in direct calli address; do activator_default_route_asserts "$output" "$route" || return $?; done
        for n in 0 1; do for w in 0 1; do
            gate_output witness "$output" "activator-default-planner-$n-$w:True" --count 1 --policy crlf || return $?
        done; done
    fi
    activator_default_route_asserts "$output" reflection || return $?
    gate_output witness "$output" 'Activator default reflection end' --count 1 --policy crlf
}
activator_default_routes_gate() {
    local root="$1" mode axis input out flag
    shift
    local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM"
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib
    corelib=$(resolve_net10_corelib) || return $?
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    dotnet "$fixture" --activator-default-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" || return $?
    for mode in all reflection; do
        local oracle=--activator-default-oracle flag=--activator-default
        [[ "$mode" != reflection ]] || { oracle+=-reflection; flag+=-reflection; }
        input="$root/oracle-$mode-input"; mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "$oracle" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        gate_capture_ok "$root/oracle-$mode" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
        activator_default_artifact_asserts "$root/oracle-$mode.stdout" "$mode" || return $?
        input="$root/input-$mode"; mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"
            local flags=("$@")
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            grep -Fq 'dn2cpp_activator_create_instance_wrapped(' "$out"/generated*.cpp || return $?
            if [[ "$mode" == reflection ]] && grep -Eq 'ActivatorChecks_(Direct|Pointer|Address)_m[0-9]+\(' "$out/generated.h"; then
                echo 'Direct Activator routes unexpectedly retained in reflection-only fixture' >&2; return 1
            fi
            cp gates/fixtures/activator-default.cpp "$out/generated_activator_checks.cpp" || return $?
            awk '
                BEGIN { print "void dn2cpp_activator_default_boundary_checks();" }
                { print }
                /^[[:space:]]*dn2cpp_runtime_init\(/ { print "    dn2cpp_activator_default_boundary_checks();"; calls++ }
                END { if (calls != 1) exit 1 }
            ' "$out/generated.cpp" > "$out/generated.checked.cpp.tmp" || return $?
            mv "$out/generated.checked.cpp.tmp" "$out/generated.cpp" || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" ActivatorDefault || return $?
                runner=(node "$out/ActivatorDefault.js")
            else
                compile_console "$out" ActivatorDefault || return $?
                runner=("$out/ActivatorDefault$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            activator_default_artifact_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/oracle-$mode.stdout" "$out/native.stdout" --policy crlf || return $?
            if [[ "$mode" == all ]]; then
                gate_capture_ok "$out/policy" run_bounded "${runner[@]}" native-policy || return $?
                for label in row invoke bound dynamic; do
                    gate_output witness "$out/policy.stdout" "activator-default-address-$label:True" --count 1 --policy crlf || return $?
                done
            fi
        done
    done
}

# Invalid metadata and NoAlloc are compile-only; private CLR factories are never executed.
activator_default_negative_gate() {
    local root="$1" mode input corelib
    corelib=$(resolve_net10_corelib) || return $?
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    for mode in return-int return-modifier instance explicit vararg type-valuetype named-bool bool-modifier address-return-int address-type-valuetype address-named-bool address-bool-modifier; do
        input="$root/$mode-input"; mkdir -p "$input"
        cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        dotnet "$fixture" --activator-default-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
        gate_capture "$root/$mode" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/$mode-output" || return $?
        gate_output status "$(cat "$root/$mode.status")" 2 || return $?
        gate_output match-record "$root/$mode.stderr" 'error: .*Activator.*' --count 1 --policy crlf || return $?
    done
    input="$root/nocore-input"; mkdir -p "$input"
    cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
    dotnet "$fixture" --activator-default-nocore "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture_ok "$root/nocore" invoke_cli "$input/SelfHostPrimSubset.dll" --no-ildiet -o "$root/nocore-output" || return $?
    grep -Fq 'dn2cpp_activator_create_instance(' "$root/nocore-output"/generated*.cpp || return $?
    input="$root/noalloc-input"; mkdir -p "$input"
    cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
    dotnet "$fixture" --activator-default-noalloc "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture "$root/noalloc" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/noalloc-output" || return $?
    gate_output status "$(cat "$root/noalloc.status")" 2 || return $?
    gate_output match-record "$root/noalloc.stderr" '.*NoAlloc.*dn2cpp_activator_create_instance.*' --count any --policy crlf
}
