#!/usr/bin/env bash
# Private CoreCLR activation ABI: native refusal only, no private CLR execution.
coreclr_activation_info_contract_asserts() {
    local output="$1" label
    for label in public-construction public-enum; do
        gate_output witness "$output" "activation-info-$label:True" --count 1 --policy crlf || return $?
    done
    gate_output witness "$output" '== CoreCLR activation boundary public controls ==' --count 1 --policy crlf || return $?
    gate_output witness "$output" 'CoreCLR activation boundary public controls end' --count 1 --policy crlf
}
coreclr_activation_info_routes_gate() {
    local root="$1"; shift
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local cli="${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}"
    local corelib mode axis input out label route
    corelib=$(resolve_net10_corelib) || return $?
    dotnet "$fixture" --coreclr-activation-info-guards "$(dirname "$cli")/Dn2Cpp.Transpiler.dll" "$corelib" || return $?
    input="$root/oracle-input"; mkdir -p "$input"
    cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
    dotnet "$fixture" --coreclr-activation-info-oracle "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
    gate_capture_ok "$root/oracle" run_bounded dotnet "$input/SelfHostPrimSubset.dll" || return $?
    coreclr_activation_info_contract_asserts "$root/oracle.stdout" || return $?
    for mode in all reflection noalloc; do
        input="$root/$mode-input"; mkdir -p "$input"
        cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        local flag=--coreclr-activation-info
        [[ "$mode" == all ]] || flag+="-$mode"
        dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"; local flags=("$@")
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            grep -Fq 'dn2cpp_throw_platform_not_supported("System.RuntimeTypeHandle.GetActivationInfo requires CoreCLR allocator and constructor function-pointer ABI.' "$out"/generated*.cpp || return $?
            local runner=()
            if [[ "${WASM:-0}" == 1 ]]; then
                compile_console_wasm "$out" CoreClrActivation || return $?
                runner=(node "$out/CoreClrActivation.js")
            else
                compile_console "$out" CoreClrActivation || return $?
                runner=("$out/CoreClrActivation$EXE_EXT")
            fi
            gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
            coreclr_activation_info_contract_asserts "$out/native.stdout" || return $?
            local first=activation-info-direct-sentinels-0:True
            [[ "$mode" != reflection ]] || first=activation-info-row:True
            gate_output prefix "$out/native.stdout" "$root/oracle.stdout" --before "$first" --policy crlf || return $?
            for label in row reflection-null-policy reflection-pointer-policy; do
                gate_output witness "$out/native.stdout" "activation-info-$label:True" --count 1 --policy crlf || return $?
            done
            if [[ "$mode" != reflection ]]; then
                for route in direct calli delegate; do
                    for label in sentinels-0 sentinels-1 nullrefs; do
                        gate_output witness "$out/native.stdout" "activation-info-$route-$label:True" --count 1 --policy crlf || return $?
                    done
                done
            fi
        done
    done
}
coreclr_activation_info_negative_gate() {
    local root="$1" mode input corelib
    corelib=$(resolve_net10_corelib) || return $?
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    for mode in allocator-return allocator-unmanaged value-byte flat-pointer bool-value modified address-allocator-return address-value-byte; do
        input="$root/$mode-input"; mkdir -p "$input"
        cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        dotnet "$fixture" --coreclr-activation-info-negative "$input/SelfHostPrimSubset.dll" "$corelib" "$mode" || return $?
        gate_capture "$root/$mode" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -o "$root/$mode-output" || return $?
        gate_output status "$(cat "$root/$mode.status")" 2 || return $?
        gate_output match-record "$root/$mode.stderr" '.*(GetActivationInfo|activation-info).*' --count any --policy crlf || return $?
    done
}

# The raw sibling retains existing import policy; generated artifacts are never executed.
coreclr_activation_info_import_gate() {
    local root="$1" mode axis input out corelib flag
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in direct address; do
        input="$root/$mode-input"; mkdir -p "$input"
        cp "samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/"* "$input/" || return $?
        flag=--coreclr-activation-info-raw
        [[ "$mode" != address ]] || flag+=-address
        dotnet "$fixture" "$flag" "$input/SelfHostPrimSubset.dll" "$corelib" || return $?
        for axis in raw diet; do
            out="$root/$mode-$axis"; local flags=()
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            gate_capture "$out/result" invoke_cli "$input/SelfHostPrimSubset.dll" --auto-ref "${flags[@]}" \
                --direct-pinvoke 'QCall!RuntimeTypeHandle_GetActivationInfo' -o "$out/emitted" || return $?
            if [[ "$mode" == direct ]]; then
                gate_output status "$(cat "$out/result.status")" 2 || return $?
                gate_output match-record "$out/result.stderr" 'error: .*GetActivationInfo.*ObjectHandleOnStack.*has no intrinsic mapping yet.*' --count 1 --policy crlf || return $?
            else
                gate_output status "$(cat "$out/result.status")" 0 || return $?
                gate_output witness "$out/emitted/pinvoke-symbols.txt" $'QCall\tRuntimeTypeHandle_GetActivationInfo' --count 1 --policy crlf || return $?
                gate_output witness "$out/emitted/pinvoke-libs.txt" QCall --count 1 --policy crlf || return $?
                if grep -Fq 'dn2cpp_throw_platform_not_supported("System.RuntimeTypeHandle.GetActivationInfo requires CoreCLR allocator' "$out/emitted"/generated*.cpp; then
                    echo 'Managed activation-info refusal intercepted the raw import' >&2; return 1
                fi
            fi
        done
    done
}
