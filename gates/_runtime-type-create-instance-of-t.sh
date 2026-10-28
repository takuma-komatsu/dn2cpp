#!/usr/bin/env bash
# The reference oracle covers only the public generic factory's reference-type path.
source gates/_ildiet.sh
runtime_type_create_instance_of_t_contract_asserts() {
    local output="$1" route="${2:-public}" label
    if [[ "$route" == public ]]; then
        gate_output witness "$output" '== RuntimeType CreateInstanceOfT ==' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'RuntimeType CreateInstanceOfT end' --count 1 --policy crlf || return $?
    fi
    for label in public private private-throw throw missing abstract interface null; do
        gate_output witness "$output" "runtime-type-default-ctor-oft-$route-$label:True" --count 1 --policy crlf || return $?
    done
}
runtime_type_create_instance_of_t_artifact_asserts() {
    local output="$1" mode="$2" route label
    if [[ "$mode" == all ]]; then
        for route in public direct virtual calli address; do runtime_type_create_instance_of_t_contract_asserts "$output" "$route" || return $?; done
    fi
    if [[ "$mode" != reflection ]]; then
        for label in public private generic generic-private; do
            gate_output witness "$output" "runtime-type-default-ctor-oft-planner-$label:True" --count 1 --policy crlf || return $?
        done
    fi
    if [[ "$mode" != planner ]]; then
        runtime_type_create_instance_of_t_contract_asserts "$output" reflection || return $?
        gate_output witness "$output" 'RuntimeType CreateInstanceOfT reflection end' --count 1 --policy crlf || return $?
    fi
}
runtime_type_create_instance_of_t_routes_gate() {
    local root="$1"; shift
    runtime_type_create_instance_of_t_artifacts_gate "$root" 'all reflection' "$@"
}
runtime_type_create_instance_of_t_planner_gate() {
    local root="$1"; shift
    runtime_type_create_instance_of_t_artifacts_gate "$root" planner "$@"
}
runtime_type_create_instance_of_t_artifacts_gate() {
    local root="$1" modes="$2" mode axis input out corelib label
    shift 2
    corelib=$(resolve_net10_corelib) || return $?
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM"
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    dotnet "$fixture" --runtime-type-create-instance-of-t-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" || return $?
    for mode in $modes; do
        local oracle=--runtime-type-create-instance-of-t-oracle flag=--runtime-type-create-instance-of-t
        [[ "$mode" == all ]] || { oracle+="-$mode"; flag+="-$mode"; }
        input="$root/oracle-$mode-input"; mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "$oracle" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        gate_capture_ok "$root/oracle-$mode" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
        runtime_type_create_instance_of_t_artifact_asserts "$root/oracle-$mode.stdout" "$mode" || return $?
        input="$root/input-$mode"; mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"
            local flags=("$@")
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            grep -Eq 'dn2cpp_activator_create_instance_default_ctor\([^;]*, 1, 1\)' "$out"/generated*.cpp || return $?
            if [[ "$mode" == reflection ]] && grep -Eq 'DefaultCtorChecks_OfT(Direct|Virtual|Pointer|Address)_m[0-9]+\(' "$out/generated.h"; then
                echo 'OfT direct routes leaked into reflection-only fixture' >&2; return 1
            fi
            if [[ "$mode" == planner ]]; then
                for label in OfTPlanPublic OfTPlanGeneric; do
                    [[ $(grep -Ec "${label}__ctor_m[0-9]+\\(.*\\);$" "$out/generated.h") == 1 ]] || return 1
                done
                if grep -Eq 'OfTPlan(Private|GenericPrivate)__ctor_m[0-9]+\(' "$out/generated.h"; then
                    echo 'OfT planner retained private constructor' >&2; return 1
                fi
                if [[ "$axis" == diet ]]; then
                    local diet
                    diet=$(ildiet_generation "$out/ildiet") || return $?
                    dotnet "$fixture" --runtime-type-create-instance-of-t-inspect-planner "$diet/SelfHostPrimSubset.dll" "$corelib" || return $?
                fi
            else
                cp gates/fixtures/runtime-type-default-ctor.cpp "$out/generated_default_ctor_checks.cpp" || return $?
                awk '
                    BEGIN { print "void dn2cpp_runtime_type_default_ctor_boundary_checks();" }
                    { print }
                    /^[[:space:]]*dn2cpp_runtime_init\(/ { print "    dn2cpp_runtime_type_default_ctor_boundary_checks();"; calls++ }
                    END { if (calls != 1) exit 1 }
                ' "$out/generated.cpp" > "$out/generated.checked.cpp.tmp" || return $?
                mv "$out/generated.checked.cpp.tmp" "$out/generated.cpp" || return $?
            fi
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" RuntimeTypeOfT || return $?
                runner=(node "$out/RuntimeTypeOfT.js")
            else
                compile_console "$out" RuntimeTypeOfT || return $?
                runner=("$out/RuntimeTypeOfT$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            runtime_type_create_instance_of_t_artifact_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/oracle-$mode.stdout" "$out/native.stdout" --policy crlf || return $?
            if [[ "$mode" == all ]]; then
                gate_capture_ok "$out/policy" run_bounded "${runner[@]}" native-policy || return $?
                for label in int plain explicit nullable nullable-enum nullable-struct row invoke bound dynamic wrap; do
                    gate_output witness "$out/policy.stdout" "runtime-type-default-ctor-oft-policy-$label:True" --count 1 --policy crlf || return $?
                done
            fi
        done
    done
}
runtime_type_create_instance_of_t_negative_gate() {
    local root="$1" mode input corelib
    corelib=$(resolve_net10_corelib) || return $?
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    for mode in return-int return-modifier static explicit vararg generic extra-argument address-generic address-return-int address-return-modifier address-extra-argument; do
        input="$root/$mode-input"; mkdir -p "$input"
        cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        dotnet "$fixture" --runtime-type-create-instance-of-t-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
        gate_capture "$root/$mode" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/$mode-output" || return $?
        gate_output status "$(cat "$root/$mode.status")" 2 || return $?
        gate_output match-record "$root/$mode.stderr" 'error: .*RuntimeType.*' --count 1 --policy crlf || return $?
    done
    input="$root/noalloc-input"; mkdir -p "$input"
    cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
    dotnet "$fixture" --runtime-type-create-instance-of-t-noalloc "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture "$root/noalloc" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/noalloc-output" || return $?
    gate_output status "$(cat "$root/noalloc.status")" 2 || return $?
    gate_output match-record "$root/noalloc.stderr" '.*NoAlloc.*dn2cpp_activator_create_instance.*' --count any --policy crlf
}
