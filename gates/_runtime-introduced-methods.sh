#!/usr/bin/env bash
# Verify real factory IL survived as a value ctor call, not a new refusal/empty shortcut.
runtime_introduced_methods_body_asserts() {
    local out="$1"
    awk '
        /^(inline )?[^ ]+ RuntimeTypeHandle_GetIntroducedMethods_m[0-9]+\(/ && $0 !~ /;$/ { body=1 }
        body { print }
        body && /^}$/ { exit }
    ' "$out/generated.h" "$out"/generated*.cpp > "$out/factory-body.txt" || return $?
    grep -Eq 'IntroducedMethodEnumerator__ctor_m[0-9]+\(&' "$out/factory-body.txt" || return $?
    grep -Eq 'return t[0-9]+;' "$out/factory-body.txt" || return $?
    if grep -Eq 'dn2cpp_throw|dn2cpp_null_check' "$out/factory-body.txt"; then return 1; fi
    awk '
        /^void IntroducedMethodEnumerator__ctor_m[0-9]+\(/ && $0 !~ /;$/ { body=1 }
        body { print }
        body && /^}$/ { exit }
    ' "$out/generated.h" "$out"/generated*.cpp > "$out/enumerator-ctor-body.txt" || return $?
    awk '
        /dn2cpp_throw_platform_not_supported.*GetFirstIntroducedMethod requires CoreCLR method descriptors/ { leaf=NR; leaves++ }
        /f__handle =/ { handle=NR }
        /f__firstCall =/ { first=NR }
        END { if (leaves != 1 || handle <= leaf || first <= handle) exit 1 }
    ' "$out/enumerator-ctor-body.txt" || return $?
}

runtime_introduced_methods_routes_gate() {
    local root="$1";shift
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" corelib mode axis input out label route
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root" || return $?
    dotnet "$fixture" --introduced-methods-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$corelib" "$root/model-input.dll" || return $?
    input="$root/oracle-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
    dotnet "$fixture" --introduced-methods-oracle "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture_ok "$root/oracle" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
    for label in methods ctors invoke delegate;do gate_output witness "$root/oracle.stdout" "introduced-factory-public-$label:True" --count 1 --policy crlf || return $?;done
    for mode in all reflection noalloc;do
        input="$root/$mode-input";mkdir -p "$input";cp "$original/"* "$input/" || return $?
        local flag=--introduced-methods;[[ "$mode" == all ]] || flag+="-$mode"
        dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        for axis in raw diet;do
            out="$root/$mode-$axis";local flags=("$@");[[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            runtime_introduced_methods_body_asserts "$out" || return $?
            # The declarations expose the actual nested value-return ABI.
            grep -Eq 'RuntimeTypeHandle_GetIntroducedMethods_m[0-9]+\(' "$out/generated.h" || return $?
            grep -Eq 'IntroducedMethodEnumerator__ctor_m[0-9]+\(' "$out/generated.h" || return $?
            if [[ "$mode" == reflection ]] && grep -Eq 'Program_(Direct|Pointer|DelegateRoute|Discard)_m[0-9]+\(' "$out/generated.h";then
                echo 'Direct factory roots leaked into reflection-only fixture' >&2;return 1
            fi
            local runner=()
            if [[ "${WASM:-0}" == 1 ]];then compile_console_wasm "$out" IntroducedFactory || return $?;runner=(node "$out/IntroducedFactory.js")
            else compile_console "$out" IntroducedFactory || return $?;runner=("$out/IntroducedFactory$EXE_EXT");fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            local routes='direct calli delegate discard reflection';[[ "$mode" != reflection ]] || routes=reflection
            for route in $routes;do for label in null type;do
                gate_output witness "$out/native.stdout" "introduced-factory-$route-$label:True" --count 1 --policy crlf || return $?
            done;done
            gate_output prefix "$out/native.stdout" "$root/oracle.stdout" --before "introduced-factory-${routes%% *}-type:True" --policy crlf || return $?
        done
    done
}
# Compile-only malformed MemberRefs; never execute on CLR or native.
runtime_introduced_methods_negative_gate() {
    local root="$1" corelib mode input
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in class-return object-return byref-return modifier argument instance generic address-class-return address-modifier;do
        input="$root/$mode-input";mkdir -p "$input";cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        dotnet "$fixture" --introduced-methods-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
        gate_capture "$root/$mode" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/$mode-output" || return $?
        gate_output status "$(cat "$root/$mode.status")" 2 || return $?
        gate_output match-record "$root/$mode.stderr" '.*(GetIntroducedMethods|RuntimeTypeHandle introduced-method factory requires its exact MemberRef signature).*' --count any --policy crlf || return $?
    done
}
