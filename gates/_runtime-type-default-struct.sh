#!/usr/bin/env bash
# Zero-init public generic oracle; in-place preservation is verified only in native-policy.
source gates/_ildiet.sh
runtime_type_default_struct_contract_asserts() {
    local output="$1" route="${2:-public}" label
    if [[ "$route" == public ]]; then
        gate_output witness "$output" '== RuntimeType default struct constructor ==' --count 1 --policy crlf || return $?
        gate_output witness "$output" 'RuntimeType default struct constructor end' --count 1 --policy crlf || return $?
    fi
    for label in int enum plain explicit nullable refstruct throw null; do
        gate_output witness "$output" "runtime-type-default-struct-$route-$label:True" --count 1 --policy crlf || return $?
    done
    if [[ "$route" != public ]]; then
        for label in private external-private; do
            gate_output witness "$output" "runtime-type-default-struct-$route-$label:True" --count 1 --policy crlf || return $?
        done
    fi
}
runtime_type_default_struct_artifact_asserts() {
    local output="$1" mode="$2" route label
    if [[ "$mode" == all ]]; then
        for route in public direct virtual calli address; do runtime_type_default_struct_contract_asserts "$output" "$route" || return $?; done
    fi
    if [[ "$mode" != reflection ]]; then
        for label in public generic private external-private implicit generic-implicit; do
            gate_output witness "$output" "runtime-type-default-struct-planner-$label:True" --count 1 --policy crlf || return $?
        done
    fi
    if [[ "$mode" != planner ]]; then
        gate_output witness "$output" 'runtime-type-default-struct-reflection-byte-writeback:True' --count 1 --policy crlf || return $?
    fi
}
runtime_type_default_struct_routes_gate() {
    local root="$1"; shift
    runtime_type_default_struct_artifacts_gate "$root" 'all reflection' "$@"
}
runtime_type_default_struct_planner_gate() {
    local root="$1"; shift
    runtime_type_default_struct_artifacts_gate "$root" planner "$@"
}
runtime_type_default_struct_artifacts_gate() {
    local root="$1" modes="$2" mode axis input out corelib label
    shift 2
    corelib=$(resolve_net10_corelib) || return $?
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM"
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    dotnet "$fixture" --runtime-type-default-struct-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" || return $?
    for mode in $modes; do
        local oracle=--runtime-type-default-struct-oracle flag=--runtime-type-default-struct
        [[ "$mode" == all ]] || { oracle+="-$mode"; flag+="-$mode"; }
        input="$root/oracle-$mode-input"; mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "$oracle" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        gate_capture_ok "$root/oracle-$mode" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
        runtime_type_default_struct_artifact_asserts "$root/oracle-$mode.stdout" "$mode" || return $?
        input="$root/input-$mode"; mkdir -p "$input"
        cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"
            local flags=("$@")
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" -r "$input/DefaultStructCtorLib.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            grep -Fq 'dn2cpp_runtime_type_call_default_struct_constructor(' "$out"/generated*.cpp || return $?
            if [[ "$mode" == reflection ]] && grep -Eq 'StructChecks_(Direct|Virtual|Pointer|Address)_m[0-9]+\(' "$out/generated.h"; then
                echo 'Direct struct routes leaked into reflection-only fixture' >&2; return 1
            fi
            if [[ "$mode" == planner ]]; then
                for label in PlanPublic PlanGeneric; do
                    [[ $(grep -Ec "${label}__ctor_m[0-9]+\\(.*\\);$" "$out/generated.h") == 1 ]] || return 1
                    grep -Eq "${label}__cctor_m[0-9]+\\(.*\\);$" "$out/generated.h" || return $?
                done
                if grep -Eq '(PlanPrivate|ExternalPrivate)__ctor_m[0-9]+\(' "$out/generated.h"; then
                    echo 'In-place planner retained private constructor body' >&2; return 1
                fi
                if grep -Eq '(PlanPrivate|PlanImplicit)__cctor_m[0-9]+\(' "$out/generated.h"; then
                    echo 'No-op/rejection planner retained a cctor' >&2; return 1
                fi
                if [[ "$axis" == diet ]]; then
                    local diet
                    diet=$(ildiet_generation "$out/ildiet") || return $?
                    dotnet "$fixture" --runtime-type-default-struct-inspect-planner "$diet/SelfHostPrimSubset.dll" "$corelib" || return $?
                fi
            else
                cp gates/fixtures/runtime-type-default-struct.cpp "$out/generated_default_struct_checks.cpp" || return $?
                awk '
                    BEGIN { print "void dn2cpp_default_struct_boundary_checks();" }
                    { print }
                    /^[[:space:]]*dn2cpp_runtime_init\(/ { print "    dn2cpp_default_struct_boundary_checks();"; calls++ }
                    END { if (calls != 1) exit 1 }
                ' "$out/generated.cpp" > "$out/generated.checked.cpp.tmp" || return $?
                mv "$out/generated.checked.cpp.tmp" "$out/generated.cpp" || return $?
            fi
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" RuntimeTypeDefaultStruct || return $?
                runner=(node "$out/RuntimeTypeDefaultStruct.js")
            else
                compile_console "$out" RuntimeTypeDefaultStruct || return $?
                runner=("$out/RuntimeTypeDefaultStruct$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            runtime_type_default_struct_artifact_asserts "$out/native.stdout" "$mode" || return $?
            gate_output compare "$root/oracle-$mode.stdout" "$out/native.stdout" --policy crlf || return $?
            if [[ "$mode" == all ]]; then
                gate_capture_ok "$out/policy" run_bounded "${runner[@]}" native-policy || return $?
                for label in null-noop null-explicit visibility-before-storage int-preserved plain-preserved nullable-preserved inplace-guards partial-write row invoke-writeback bound dynamic-writeback; do
                    gate_output witness "$out/policy.stdout" "runtime-type-default-struct-policy-$label:True" --count 1 --policy crlf || return $?
                done
            fi
        done
    done
}
runtime_type_default_struct_negative_gate() {
    local root="$1" mode input corelib
    corelib=$(resolve_net10_corelib) || return $?
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    for mode in return-object static explicit vararg value-byte byref-int modifier address-return-object address-byref-int address-modifier; do
        input="$root/$mode-input"; mkdir -p "$input"
        cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        dotnet "$fixture" --runtime-type-default-struct-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
        gate_capture "$root/$mode" invoke_cli "$input/SelfHostPrimSubset.dll" -r "$input/DefaultStructCtorLib.dll" --auto-ref --no-ildiet -o "$root/$mode-output" || return $?
        gate_output status "$(cat "$root/$mode.status")" 2 || return $?
        gate_output match-record "$root/$mode.stderr" 'error: .*RuntimeType.*' --count 1 --policy crlf || return $?
    done
    input="$root/noalloc-input"; mkdir -p "$input"
    cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
    dotnet "$fixture" --runtime-type-default-struct-noalloc "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture "$root/noalloc" invoke_cli "$input/SelfHostPrimSubset.dll" -r "$input/DefaultStructCtorLib.dll" --auto-ref --no-ildiet -o "$root/noalloc-output" || return $?
    gate_output status "$(cat "$root/noalloc.status")" 2 || return $?
    gate_output match-record "$root/noalloc.stderr" '.*NoAlloc.*dn2cpp_runtime_type_call_default_struct_constructor.*' --count any --policy crlf
}
