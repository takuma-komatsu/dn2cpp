#!/usr/bin/env bash
# Field-free interpolation helpers: independent roots and consolidated runtime parity.
interpolation_helpers_gate() {
    local root="$1" fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib route axis out
    corelib=$(resolve_net10_corelib) || return $?
    build_gate_proj gates/fixtures/ldftn-local || return $?
    build_gate_proj gates/fixtures/interpolation-helpers || return $?
    local original="gates/fixtures/interpolation-helpers/bin/$CONFIG/$TFM"
    mkdir -p "$root"
    local guard_dir
    guard_dir=$(mktemp -d "$root/guards.XXXXXX") || return $?
    dotnet "$fixture" --interpolation-helpers-guards "src/Dn2Cpp.Transpiler/bin/$CONFIG/$TFM/Dn2Cpp.Transpiler.dll" "$corelib" "$guard_dir/input.dll" "src/ILDiet/bin/$CONFIG/$TFM/ILDiet.dll" || return $?
    for route in direct address reflection retained invoke literal all; do
        mkdir -p "$root/$route-input"
        cp -R "$original/." "$root/$route-input/" || return $?
        dotnet "$fixture" --interpolation-helpers "$root/$route-input/InterpolationHelpers.dll" "$corelib" "$route" || return $?
        if [[ "$route" == reflection || "$route" == invoke || "$route" == literal ]]; then
            gate_capture_ok "$root/$route-oracle" run_bounded dotnet "$root/$route-input/InterpolationHelpers.dll" || return $?
        fi
        for axis in raw diet; do
            out="$root/$route-$axis"
            local flags=()
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            [[ "$route" != retained ]] || flags+=(--link-xml gates/fixtures/interpolation-helpers/link.xml)
            invoke_cli "$root/$route-input/InterpolationHelpers.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
            if [[ "$route" != invoke && "$route" != literal ]]; then
                "$(resolve_python)" gates/fixtures/interpolation-helpers/check-generated.py "$out" --route "$route" || return $?
            fi
        done
    done
    interpolation_helpers_negative_gate "$root/negatives" "$original" || return $?
    cat "$root/reflection-oracle.stdout" "$root/reflection-oracle.stdout" "$root/reflection-oracle.stdout" \
        "$root/invoke-oracle.stdout" "$root/literal-oracle.stdout" > "$root/expected.stdout"
    for axis in raw diet; do
        out="$root/all-$axis"
        local runner=()
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" InterpolationHelpers || return $?
            runner=(node "$out/InterpolationHelpers.js")
        else
            compile_console "$out" InterpolationHelpers || return $?
            runner=("$out/InterpolationHelpers$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        gate_output compare "$root/expected.stdout" "$out/native.stdout" || return $?
    done
}

interpolation_helpers_negative_gate() {
    local root="$1" original="$2" profile="${3:-representative}" corelib route helper shape axis mode rc
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for route in direct address; do
        for helper in has length; do
            for shape in return byref modifier parammodifier instance generic varargs wrongtype arity spec; do
                mode="negative-$route-$helper-$shape"
                mkdir -p "$root/$mode/input"
                cp -R "$original/." "$root/$mode/input/" || return $?
                dotnet "$fixture" --interpolation-helpers "$root/$mode/input/InterpolationHelpers.dll" "$corelib" "$mode" || return $?
                local axes=(raw)
                if [[ "$profile" != raw ]]; then
                    case "$route-$helper-$shape" in
                        direct-has-return|address-has-return|direct-length-byref|address-length-byref) axes+=(diet) ;;
                    esac
                fi
                for axis in "${axes[@]}"; do
                    local flags=()
                    [[ "$axis" != raw ]] || flags+=(--no-ildiet)
                    rc=0
                    invoke_cli "$root/$mode/input/InterpolationHelpers.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
                    printf '%s\n' "$rc" > "$root/$mode/$axis.status"
                    [[ "$rc" == 2 ]] || return 1
                    grep -Eq 'DefaultInterpolatedStringHandler.*(exact MemberRef signature|no (generic )?method|has no intrinsic mapping yet)' "$root/$mode/$axis.log" || return 1
                done
            done
        done
    done
}
