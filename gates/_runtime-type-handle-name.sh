#!/usr/bin/env bash
# Retained handle names: public formatting oracle, native-only capability refusals.
runtime_type_handle_name_routes_gate() {
    local root="$1"; shift
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local original="samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM" corelib mode axis input out route label
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    corelib=$(resolve_net10_corelib) || return $?
    dotnet "$fixture" --runtime-type-handle-name-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$corelib" || return $?
    input="$root/oracle-input"; mkdir -p "$input"; cp "$original/"* "$input/" || return $?
    dotnet "$fixture" --runtime-type-handle-name-oracle "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture_ok "$root/oracle" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
    for mode in all reflection; do
        input="$root/$mode-input"; mkdir -p "$input"; cp "$original/"* "$input/" || return $?
        local flag=--runtime-type-handle-name; [[ "$mode" == all ]] || flag+=-reflection
        dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"; local flags=("$@"); [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            grep -Fq 'dn2cpp_throw_platform_not_supported("RuntimeTypeHandle.ConstructName supports name, display, full and assembly-qualified modes only in the native type-name domain.' "$out"/generated*.cpp || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then compile_console_wasm "$out" TypeHandleName || return $?; runner=(node "$out/TypeHandleName.js")
            else compile_console "$out" TypeHandleName || return $?; runner=("$out/TypeHandleName$EXE_EXT"); fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            local first=type-name-direct:True routes='direct calli delegate reflection'
            [[ "$mode" != reflection ]] || { first=type-name-reflection:True; routes=reflection; }
            gate_output prefix "$out/native.stdout" "$root/oracle.stdout" --before "$first" --policy crlf || return $?
            for route in $routes; do gate_output witness "$out/native.stdout" "type-name-$route:True" --count 1 --policy crlf || return $?; done
            if [[ "$mode" == all ]]; then
                gate_capture_ok "$out/policy" run_bounded "${runner[@]}" native-policy || return $?
                for label in default-before-flags-0 default-before-flags-1 default-before-flags-3 default-before-flags-7 default-before-flags-64 null-storage-0 null-storage-64 unsupported-2 unsupported-4 unsupported-8 unsupported-64 unsupported-128 unsupported-256 unsupported--1 public-null-0 public-null-1 public-null-2 basic-0 basic-1 basic-2 domain-0-3 domain-0-7 domain-1-3 domain-1-7 domain-2-3 domain-2-7; do
                    gate_output witness "$out/policy.stdout" "type-name-$label:True" --count 1 --policy crlf || return $?
                done
            fi
        done
    done
}
runtime_type_handle_name_negative_gate() {
    local root="$1" mode input corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in wrong-enum primitive named-primitive modifier static byref generic address-wrong-enum address-modifier; do
        input="$root/$mode-input"; mkdir -p "$input"; cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        dotnet "$fixture" --runtime-type-handle-name-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
        gate_capture "$root/$mode" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/$mode-output" || return $?
        gate_output status "$(cat "$root/$mode.status")" 2 || return $?
        gate_output match-record "$root/$mode.stderr" '.*(ConstructName|RuntimeTypeHandle name construction).*' --count any --policy crlf || return $?
    done
    input="$root/noalloc-input"; mkdir -p "$input"; cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
    dotnet "$fixture" --runtime-type-handle-name-noalloc "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture "$root/noalloc" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/noalloc-output" || return $?
    gate_output status "$(cat "$root/noalloc.status")" 2 || return $?
    gate_output match-record "$root/noalloc.stderr" '.*NoAlloc.*dn2cpp_type_.*' --count any --policy crlf
}

# The raw sibling retains existing import policy; generated artifacts are never executed.
runtime_type_handle_name_import_gate() {
    local root="$1" mode axis input out corelib flag
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in direct address; do
        input="$root/$mode-input"; mkdir -p "$input"
        cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        flag=--runtime-type-handle-name-raw-import
        [[ "$mode" != address ]] || flag+=-address
        dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"; local flags=()
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            gate_capture "$out/result" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" \
                --direct-pinvoke 'QCall!RuntimeTypeHandle_ConstructName' -o "$out/emitted" || return $?
            if [[ "$mode" == direct ]]; then
                gate_output status "$(cat "$out/result.status")" 2 || return $?
                gate_output match-record "$out/result.stderr" 'error: .*ConstructName.*StringHandleOnStack.*has no intrinsic mapping yet.*' --count 1 --policy crlf || return $?
            else
                gate_output status "$(cat "$out/result.status")" 0 || return $?
                gate_output witness "$out/emitted/pinvoke-symbols.txt" $'QCall\tRuntimeTypeHandle_ConstructName' --count 1 --policy crlf || return $?
                gate_output witness "$out/emitted/pinvoke-libs.txt" QCall --count 1 --policy crlf || return $?
                if grep -Fq 'dn2cpp_throw_platform_not_supported("RuntimeTypeHandle.ConstructName supports name, display, full and assembly-qualified modes only in the native type-name domain.' "$out/emitted"/generated*.cpp; then
                    echo 'Managed name adapter intercepted the raw import' >&2; return 1
                fi
            fi
        done
    done
}
