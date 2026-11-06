#!/usr/bin/env bash
# Retained RuntimeType default construction shares native visibility, wrapping and normal Nullable boxing.
source gates/_ildiet.sh
runtime_type_default_ctor_route_asserts() {
    local output="$1" route="$2" n w label
    for n in 0 1; do for w in 0 1; do
        for label in class private throw-public throw-private plain explicit int nullable-int nullable-enum nullable-struct missing abstract interface open null; do
            gate_output witness "$output" "runtime-type-default-ctor-$route-$n-$w-$label:True" --count 1 --policy crlf || return $?
        done
    done; done
}
runtime_type_default_ctor_contract_asserts() {
    local output="$1" label
    gate_output witness "$output" '== RuntimeType default construction ==' --count 1 --policy crlf || return $?
    runtime_type_default_ctor_route_asserts "$output" public || return $?
    gate_output witness "$output" 'RuntimeType default construction end' --count 1 --policy crlf
}
runtime_type_default_ctor_artifact_asserts() {
    local output="$1" mode="$2" route n w
    if [[ "$mode" == all ]]; then
        runtime_type_default_ctor_contract_asserts "$output" || return $?
        for route in direct virtual calli address; do runtime_type_default_ctor_route_asserts "$output" "$route" || return $?; done
        for label in private public value generic unknown-0 unknown-1; do
            gate_output witness "$output" "runtime-type-default-ctor-planner-$label:True" --count 1 --policy crlf || return $?
        done
    fi
    runtime_type_default_ctor_route_asserts "$output" reflection || return $?
    gate_output witness "$output" 'RuntimeType default reflection end' --count 1 --policy crlf
}
runtime_type_default_ctor_routes_gate() {
    local root="$1" mode axis input out flag
    shift
    local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM"
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib
    corelib=$(resolve_net10_corelib) || return $?
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    dotnet "$fixture" --runtime-type-default-ctor-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" || return $?
    for mode in all reflection; do
        local oracle=--runtime-type-default-ctor-oracle flag=--runtime-type-default-ctor
        [[ "$mode" != reflection ]] || { oracle+=-reflection; flag+=-reflection; }
        input="$root/oracle-$mode-input"; mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "$oracle" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        gate_capture_ok "$root/oracle-$mode" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
        runtime_type_default_ctor_artifact_asserts "$root/oracle-$mode.stdout" "$mode" || return $?
        input="$root/input-$mode"; mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"
            local flags=("$@")
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            grep -Fq 'dn2cpp_activator_create_instance_default_ctor(' "$out"/generated*.cpp || return $?
            if [[ "$mode" == reflection ]] && grep -Eq 'DefaultCtorChecks_(Direct|Virtual|Pointer|Address)_m[0-9]+\(' "$out/generated.h"; then
                echo 'Direct RuntimeType factory routes unexpectedly retained in reflection-only fixture' >&2; return 1
            fi
            cp gates/fixtures/runtime-type-default-ctor.cpp "$out/generated_default_ctor_checks.cpp" || return $?
            awk '
                BEGIN { print "void dn2cpp_runtime_type_default_ctor_boundary_checks();" }
                { print }
                /^[[:space:]]*dn2cpp_runtime_init\(/ { print "    dn2cpp_runtime_type_default_ctor_boundary_checks();"; calls++ }
                END { if (calls != 1) exit 1 }
            ' "$out/generated.cpp" > "$out/generated.checked.cpp.tmp" || return $?
            mv "$out/generated.checked.cpp.tmp" "$out/generated.cpp" || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" RuntimeTypeDefaultCtor || return $?
                runner=(node "$out/RuntimeTypeDefaultCtor.js")
            else
                compile_console "$out" RuntimeTypeDefaultCtor || return $?
                runner=("$out/RuntimeTypeDefaultCtor$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            runtime_type_default_ctor_artifact_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/oracle-$mode.stdout" "$out/native.stdout" --policy crlf || return $?
            if [[ "$mode" == all ]]; then
                gate_capture_ok "$out/policy" run_bounded "${runner[@]}" native-policy || return $?
                for label in row invoke bound dynamic wrap; do
                    gate_output witness "$out/policy.stdout" "runtime-type-default-ctor-address-$label:True" --count 1 --policy crlf || return $?
                done
            fi
        done
    done
}

# Invalid metadata and NoAlloc are compile-only; private CLR factories are never executed.
runtime_type_default_ctor_negative_gate() {
    local root="$1" mode input corelib
    corelib=$(resolve_net10_corelib) || return $?
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    for mode in return-int return-modifier static explicit vararg named-bool bool-modifier address-return-int address-named-bool address-bool-modifier; do
        input="$root/$mode-input"; mkdir -p "$input"
        cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        dotnet "$fixture" --runtime-type-default-ctor-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
        gate_capture "$root/$mode" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/$mode-output" || return $?
        gate_output status "$(cat "$root/$mode.status")" 2 || return $?
        gate_output match-record "$root/$mode.stderr" 'error: .*RuntimeType.*' --count 1 --policy crlf || return $?
    done
    input="$root/noalloc-input"; mkdir -p "$input"
    cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
    dotnet "$fixture" --runtime-type-default-ctor-noalloc "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture "$root/noalloc" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/noalloc-output" || return $?
    gate_output status "$(cat "$root/noalloc.status")" 2 || return $?
    gate_output match-record "$root/noalloc.stderr" '.*NoAlloc.*dn2cpp_activator_create_instance.*' --count any --policy crlf
}

# This entry point has no dynamic-construction roots to mask receiver planning.
runtime_type_default_ctor_planner_gate() {
    local root="$1" axis input out corelib label
    shift
    corelib=$(resolve_net10_corelib) || return $?
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM"
    input="$root/oracle-input"; mkdir -p "$input"
    cp "$original/"* "$input/" || return $?
    dotnet "$fixture" --runtime-type-default-ctor-oracle-planner "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture_ok "$root/oracle" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
    for label in private public value generic unknown-0 unknown-1; do
        gate_output witness "$root/oracle.stdout" "runtime-type-default-ctor-planner-$label:True" --count 1 --policy crlf || return $?
    done
    input="$root/input"; mkdir -p "$input"
    cp "$original/"* "$input/" || return $?
    dotnet "$fixture" --runtime-type-default-ctor-planner "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@")
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        for label in PlannerPrivateOnly PlannerUnknownOnly GenericOnly; do
            [[ $(grep -Ec "${label}__ctor_m[0-9]+\\(.*\\);$" "$out/generated.h") == 1 ]] || {
                echo "Expected one executable default ctor: $label" >&2; return 1;
            }
        done
        if grep -Eq 'PlannerPublicOnly__ctor_m[0-9]+\(' "$out/generated.h"; then
            echo 'Public-only planning retained a private default ctor' >&2; return 1
        fi
        if [[ "$axis" == diet ]]; then
            local diet
            diet=$(ildiet_generation "$out/ildiet") || return $?
            dotnet "$fixture" --runtime-type-default-ctor-inspect-planner "$diet/SelfHostPrimSubset.dll" "$corelib" || return $?
        fi
        local runner=()
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" RuntimeTypeDefaultCtorPlanner || return $?
            runner=(node "$out/RuntimeTypeDefaultCtorPlanner.js")
        else
            compile_console "$out" RuntimeTypeDefaultCtorPlanner || return $?
            runner=("$out/RuntimeTypeDefaultCtorPlanner$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        gate_output compare "$root/oracle.stdout" "$out/native.stdout" --policy crlf || return $?
    done
}
