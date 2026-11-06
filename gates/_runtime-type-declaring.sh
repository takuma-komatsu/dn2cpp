#!/usr/bin/env bash
# Retained declaring-owner route: native nested refusal is not CLR parity.
runtime_type_declaring_routes_gate() {
    local root="$1"; shift
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" corelib mode axis input out route index
    corelib=$(resolve_net10_corelib) || return $?
    dotnet "$fixture" --runtime-type-declaring-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$corelib" || return $?
    for mode in oracle nested-oracle; do
        input="$root/$mode-input"; mkdir -p "$input"; cp "$original/"* "$input/" || return $?
        dotnet "$fixture" "--runtime-type-declaring-$mode" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        gate_capture_ok "$root/$mode" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
    done
    for index in 0 1 2 3 4 5 6 7 8 9; do
        gate_output witness "$root/nested-oracle.stdout" "declaring-type-clr-nested-$index:True" --count 1 --policy crlf || return $?
    done
    for mode in all reflection noalloc; do
        input="$root/$mode-input"; mkdir -p "$input"; cp "$original/"* "$input/" || return $?
        local flag=--runtime-type-declaring; [[ "$mode" == all ]] || flag+="-$mode"
        dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"; local flags=("$@"); [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            grep -Fq 'dn2cpp_runtime_type_get_declaring_type(' "$out"/generated*.cpp || return $?
            if [[ "$mode" != reflection ]]; then
                grep -Eq 'RuntimeTypeCache_GetEnclosingType_m[0-9]+\(' "$out"/generated*.cpp || return $?
            fi
            cp gates/fixtures/runtime-type-declaring.cpp "$out/generated_declaring_checks.cpp" || return $?
            awk '
                BEGIN { print "void dn2cpp_declaring_type_boundary_checks();" }
                { print }
                /^[[:space:]]*dn2cpp_runtime_init\(/ { print "    dn2cpp_declaring_type_boundary_checks();"; calls++ }
                END { if (calls != 1) exit 1 }
            ' "$out/generated.cpp" > "$out/generated.checked.cpp.tmp" || return $?
            mv "$out/generated.checked.cpp.tmp" "$out/generated.cpp" || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then compile_console_wasm "$out" RuntimeDeclaring || return $?; runner=(node "$out/RuntimeDeclaring.js")
            else compile_console "$out" RuntimeDeclaring || return $?; runner=("$out/RuntimeDeclaring$EXE_EXT"); fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            gate_output witness "$out/native.stderr" "declaring-type-native-boundaries:passed" --count 1 --policy crlf || return $?
            local first=declaring-type-direct-0:True routes='direct calli delegate reflection'
            [[ "$mode" != reflection ]] || { first=declaring-type-reflection-0:True; routes=reflection; }
            gate_output prefix "$out/native.stdout" "$root/oracle.stdout" --before "$first" --policy crlf || return $?
            for route in $routes; do
                for index in 0 1 2 3 4 5 6 7 8 9 10 11 null nested-0 nested-1 nested-2 nested-3 nested-4 nested-5 nested-6 nested-7 nested-8 nested-9; do
                    gate_output witness "$out/native.stdout" "declaring-type-$route-$index:True" --count 1 --policy crlf || return $?
                done
            done
            [[ "$mode" == reflection ]] || gate_output witness "$out/native.stdout" 'declaring-type-cache-void-sentinel:True' --count 1 --policy crlf || return $?
        done
    done
}
runtime_type_declaring_negative_gate() {
    local root="$1" corelib mode input
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in return-type parameter-type instance generic byref modifier address-return-type address-parameter-type address-modifier; do
        input="$root/$mode-input"; mkdir -p "$input"; cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        dotnet "$fixture" --runtime-type-declaring-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
        gate_capture "$root/$mode" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/$mode-output" || return $?
        gate_output status "$(cat "$root/$mode.status")" 2 || return $?
        gate_output match-record "$root/$mode.stderr" '.*(GetDeclaringType|RuntimeTypeHandle declaring-type query).*' --count any --policy crlf || return $?
    done
}
