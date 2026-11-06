#!/usr/bin/env bash
# Real isolated managed cache layout; no private CLR cache execution.
runtime_type_cache_generic_definition_routes_gate() {
    local root="$1";shift
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" corelib mode axis input out route label
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root" || return $?
    dotnet "$fixture" --cache-generic-definition-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$corelib" "$root/model-input.dll" || return $?
    input="$root/oracle-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
    dotnet "$fixture" --cache-generic-definition-oracle "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture_ok "$root/oracle" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
    for label in 0 1 2 3 4 5 6 7 same-definition;do gate_output witness "$root/oracle.stdout" "cache-generic-definition-public-$label:True" --count 1 --policy crlf || return $?;done
    for mode in all reflection noalloc;do
        input="$root/$mode-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
        local flag=--cache-generic-definition;[[ "$mode" == all ]] || flag+="-$mode"
        dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        for axis in raw diet;do
            out="$root/$mode-$axis";local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            grep -Fq 'dn2cpp_type_get_generic_type_definition(' "$out"/generated*.cpp || return $?
            if [[ "$mode" == reflection ]] && grep -Eq 'DefinitionChecks_(Direct|Virtual|Pointer|Outer)_m[0-9]+\(' "$out/generated.h";then
                echo 'Direct cache generic-definition roots leaked into reflection-only test' >&2;return 1
            fi
            local helper
            helper=$(sed -nE 's/^DN2CPP_NOINLINE [^ ]+ (RuntimeTypeCache_[^ (]*CacheGenericDefinition[^ (]*)\(t_System_RuntimeType_RuntimeTypeCache\* a0\);$/\1/p' "$out/generated.h")
            [[ -n "$helper" && "$helper" != *$'\n'* ]] || return 1
            printf '#define DN2CPP_CACHE_HELPER %s\n#define DN2CPP_CACHE_TYPE t_System_RuntimeType_RuntimeTypeCache\n#define DN2CPP_CACHE_TYPEINFO ti_System_RuntimeType_RuntimeTypeCache\n' "$helper" > "$out/cache_generic_definition_symbols.h" || return $?
            awk -v fn="$helper" '
                $0 ~ (" " fn "\\(") && $0 !~ /;$/ { body=1 }
                body { print }
                body && /^}$/ { exit }
            ' "$out"/generated*.cpp > "$out/cache-helper-body.txt" || return $?
            grep -Fq 'f_m_runtimeType' "$out/cache-helper-body.txt" || return $?
            grep -Fq 'f__genericTypeDefinition' "$out/cache-helper-body.txt" || return $?
            grep -Fq 'dn2cpp_gc_write_barrier(' "$out/cache-helper-body.txt" || return $?
            cp gates/fixtures/runtime-type-cache-generic-definition.cpp "$out/generated_cache_definition_checks.cpp" || return $?
            awk '
                BEGIN { print "void dn2cpp_cache_generic_definition_boundary_checks();" }
                { print }
                /^[[:space:]]*dn2cpp_runtime_init\(/ { print "    dn2cpp_cache_generic_definition_boundary_checks();"; calls++ }
                END { if (calls != 1) exit 1 }
            ' "$out/generated.cpp" > "$out/generated.checked.cpp.tmp" || return $?
            mv "$out/generated.checked.cpp.tmp" "$out/generated.cpp" || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]];then compile_console_wasm "$out" CacheGenericDefinition || return $?;runner=(node "$out/CacheGenericDefinition.js")
            else compile_console "$out" CacheGenericDefinition || return $?;runner=("$out/CacheGenericDefinition$EXE_EXT");fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            gate_output witness "$out/native.stderr" 'cache-generic-definition-native-boundaries:passed' --count 1 --policy crlf || return $?
            local routes='direct virtual calli delegate reflection'
            if [[ "$mode" != reflection ]];then
                gate_output prefix "$out/native.stdout" "$root/oracle.stdout" --before 'cache-generic-definition-direct-0:True' --policy crlf || return $?
                for label in outer-first outer-short-circuit outer-invalid-short-circuit;do gate_output witness "$out/native.stdout" "cache-generic-definition-$label:True" --count 1 --policy crlf || return $?;done
            else routes=reflection;fi
            for route in $routes;do
                for label in 0 1 2 3 4 5 6 7 repeat-0 repeat-1 repeat-2 repeat-3 repeat-4 repeat-5 repeat-6 repeat-7 preserve-0 preserve-1 preserve-2 preserve-3 null-cache;do
                    gate_output witness "$out/native.stdout" "cache-generic-definition-$route-$label:True" --count 1 --policy crlf || return $?
                done
            done
        done
    done
}
runtime_type_cache_generic_definition_negative_gate() {
    local root="$1" corelib mode input
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in return-type static argument generic byref modifier address-return-type address-modifier;do
        input="$root/$mode-input";mkdir -p "$input";cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        dotnet "$fixture" --cache-generic-definition-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
        gate_capture "$root/$mode" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/$mode-output" || return $?
        gate_output status "$(cat "$root/$mode.status")" 2 || return $?
        gate_output match-record "$root/$mode.stderr" '.*(CacheGenericDefinition|RuntimeTypeCache generic-definition helper requires its exact MemberRef signature).*' --count any --policy crlf || return $?
    done
}
# The mutated framework copies are compile-only inputs, never CLR/native execution.
runtime_type_cache_generic_definition_field_negative_gate() {
    local root="$1" corelib mode input
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in unchanged owner-writable result-initonly owner-modifier result-type;do
        input="$root/$mode-input";mkdir -p "$input";cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        dotnet "$fixture" --cache-generic-definition-field-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
        gate_capture "$root/$mode" invoke_cli "$input/SelfHostPrimSubset.dll" -r "$input/System.Private.CoreLib.dll" --auto-ref --no-ildiet -o "$root/$mode-output" || return $?
        if [[ "$mode" == unchanged ]]; then
            gate_output status "$(cat "$root/$mode.status")" 0 || return $?
            awk '
                /^DN2CPP_NOINLINE .* RuntimeTypeCache_.*CacheGenericDefinition.*\(/ { body=1 }
                body { print }
                body && /^}$/ { exit }
            ' "$root/$mode-output"/generated*.cpp > "$root/unchanged-helper-body.txt" || return $?
            for fragment in f_m_runtimeType f__genericTypeDefinition 'dn2cpp_type_get_generic_type_definition(' 'dn2cpp_gc_write_barrier('; do
                grep -Fq "$fragment" "$root/unchanged-helper-body.txt" || return $?
            done
        else
            gate_output status "$(cat "$root/$mode.status")" 2 || return $?
            # Wrapper synthesis refuses malformed fields before emitting a callable body.
            gate_output match-record "$root/$mode.stderr" '^error: RuntimeTypeCache[.]<GetGenericTypeDefinition>g__CacheGenericDefinition[|]50_0: unsupported retained intrinsic body$' --count 1 --policy crlf || return $?
        fi
    done
}
