#!/usr/bin/env bash
# Concrete-vector element access, callable targets and the bounded JIT exception contrast.
vector_concrete_elements_probe() {
    local out="$1/concrete-elements" axis emit
    local app="${DN2CPP_VECTOR_CONCRETE_ELEMENTS_DLL:-gates/fixtures/vector-concrete-elements/bin/$CONFIG/$TFM/VectorConcreteElements.dll}"
    if [ -z "${DN2CPP_VECTOR_CONCRETE_ELEMENTS_DLL:-}" ]; then
        build_gate_proj gates/fixtures/vector-concrete-elements/VectorConcreteElements.csproj || return 1
    fi
    mkdir -p "$out" || return 1
    gate_capture_ok "$out/clr-hardware" run_bounded env DOTNET_EnableHWIntrinsic=1 DOTNET_EnableAVX=0 dotnet "$app" || return 1
    gate_capture_ok "$out/clr-software" run_bounded env DOTNET_EnableHWIntrinsic=0 DOTNET_EnableAVX=0 dotnet "$app" || return 1
    gate_capture_ok "$out/oracle-contract" python3 gates/fixtures/vector-concrete-elements/check-oracles.py \
        "$out/clr-hardware.stdout" "$out/clr-hardware.stderr" \
        "$out/clr-software.stdout" "$out/clr-software.stderr" || return 1
    for axis in raw diet; do
        emit="$out/$axis"
        local flags=() runner=() compare_flags=()
        [ "$axis" != raw ] || flags+=(--no-ildiet)
        gate_capture_ok "$out/$axis-emit" invoke_cli "$app" -r "$_CG_CORELIB" --auto-ref "${flags[@]}" -o "$emit" || return 1
        if [ "${WASM:-0}" = 1 ]; then
            compile_console_wasm "$emit" VectorConcreteElements || return 1
            runner=(node "$emit/VectorConcreteElements.js")
            compare_flags+=(--policy crlf)
        else
            compile_console "$emit" VectorConcreteElements || return 1
            runner=("./$emit/VectorConcreteElements$EXE_EXT")
        fi
        gate_capture_ok "$out/$axis-run" run_bounded "${runner[@]}" || return 1
        gate_output compare "$out/$axis-run.stdout" "$out/clr-software.stdout" "${compare_flags[@]}" || return 1
        gate_output compare "$out/$axis-run.stderr" "$out/clr-software.stderr" "${compare_flags[@]}" || return 1
    done
    vector_concrete_elements_authority_probe "$out/authority"
}

vector_concrete_elements_prefix() {
    local out="$1"
    gate_capture_ok "$out/concrete-before" run_bounded env DOTNET_EnableAVX=0 dotnet "$_CG_APP" before-concrete-elements || return 1
    gate_output select "$out/oracle.stdout" "$out/concrete-prefix.stdout" --before '-- concrete vector elements --' --policy crlf || return 1
    gate_output compare "$out/concrete-prefix.stdout" "$out/concrete-before.stdout" || return 1
    gate_output witness "$out/oracle.stdout" 'concrete vector six delegate and calli targets passed' --count 1 --policy crlf || return 1
}

vector_concrete_elements_authority_probe() {
    local out="$1" method route mutation input case_out
    local fixture="${DN2CPP_VECTOR_CONCRETE_ELEMENTS_FIXTURE_DLL:-gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll}"
    local compiler="${DN2CPP_VECTOR_CONCRETE_ELEMENTS_COMPILER_DLL:-$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll}"
    if [ -z "${DN2CPP_VECTOR_CONCRETE_ELEMENTS_FIXTURE_DLL:-}" ]; then
        build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return 1
    fi
    mkdir -p "$out" || return 1
    gate_capture_ok "$out/guards" run_bounded dotnet "$fixture" --concrete-vector-elements-guards \
        "$compiler" "$_CG_CORELIB" "$out/model.dll" || return 1
    for method in get2 with2 get3 with3 get4 with4; do
        for route in direct address calli; do
            local mutations=()
            if [ "$route" = direct ]; then
                mutations=(instance vararg generic byref-source byref-result wrong-source wrong-result wrong-index \
                    extra modopt-source modopt-result modreq-source modopt-owner byref-owner generic-owner foreign-owner)
                case "$method" in with*) mutations+=(wrong-value);; esac
            elif [ "$method" = get2 ] || [ "$method" = with4 ]; then
                mutations=(generic generic-owner byref-owner modopt-owner modopt-source modopt-result modreq-source)
            else
                continue
            fi
            for mutation in "${mutations[@]}"; do
                case_out="$out/$method-$route-$mutation"
                mkdir -p "$case_out" || return 1
                input="$case_out/input.dll"
                gate_capture_ok "$case_out/generate" run_bounded dotnet "$fixture" --concrete-vector-elements-fixture \
                    "$input" "$_CG_CORELIB" "$method:$route:$mutation" || return 1
                gate_capture "$case_out/reject" invoke_cli "$input" -r "$_CG_CORELIB" --no-ildiet -o "$case_out/emitted" || return 1
                gate_output status "$(cat "$case_out/reject.status")" 2 || return 1
                if ! grep -Ei 'Concrete vector|System\.Numerics\.Vector.*(GetElement|WithElement)' "$case_out/reject.stderr" >/dev/null; then
                    case "$mutation" in
                        generic|generic-owner)
                            grep -Ei 'Type argument|generic parameter|generic type' "$case_out/reject.stderr" >/dev/null || return 1;;
                        foreign-owner)
                            grep -E 'Foreign\.Vector\.Owner|External type reference.*could not be resolved' "$case_out/reject.stderr" >/dev/null || return 1;;
                        *) return 1;;
                    esac
                fi
            done
        done
    done
}
