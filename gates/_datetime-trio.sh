#!/usr/bin/env bash
# Bounded DateTime arithmetic/private construction policy, separate public-only oracle.
datetime_trio_routes_gate() {
    local root="$1" original="$2" mode="${3:-all}"; shift 3
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib flag=--datetime-trio
    corelib=$(resolve_net10_corelib) || return $?
    [[ "$mode" == all ]] || flag="--datetime-trio-$mode"
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" "$flag" "$root/input/DateTimeTrio.dll" "$corelib" || return $?
    local axis out
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@") runner=()
        [[ "$mode" == minimal ]] || flags+=(--link-xml gates/fixtures/datetime-trio/link.xml)
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$root/input/DateTimeTrio.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" DateTimeTrio || return $?
            runner=(node "$out/DateTimeTrio.js")
        else
            compile_console "$out" DateTimeTrio || return $?
            runner=("$out/DateTimeTrio$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        "$(resolve_python)" gates/fixtures/datetime-trio/check-output.py "$out/native.stdout" --mode "$mode" || return $?
        if [[ "$mode" == reflection || "$mode" == lookup ]] && grep -Eq 'Program_(AddDirect|AddPointer|AddAddress|CreateDirect|CreatePointer|CreateAddress|CtorDirect|CtorPointer|CtorAddress|CtorNew)_m[0-9]+\(' "$out/generated.h"; then
            echo 'FAIL: DateTime trio reflection-only retained alternate target routes' >&2; return 1
        fi
        if [[ "$mode" == minimal ]] && grep -Eq 'Program_(AddPointer|AddAddress|AddMetadata|CreatePointer|CreateAddress|CreateMetadata|CtorPointer|CtorAddress|CtorMetadata|CtorNew)_m[0-9]+\(' "$out/generated.h"; then
            echo 'FAIL: DateTime trio minimal retained alternate target roots' >&2; return 1
        fi
    done
}

datetime_trio_oracle_gate() {
    local root="$1" original="$2" corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" --datetime-trio-oracle "$root/input/DateTimeTrio.dll" "$corelib" || return $?
    gate_capture_ok "$root/public" run_bounded dotnet "$root/input/DateTimeTrio.dll" || return $?
    "$(resolve_python)" gates/fixtures/datetime-trio/check-output.py "$root/public.stdout" --mode oracle || return $?
}

datetime_trio_negative_gate() {
    local root="$1" original="$2" corelib mode axis
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in ${DN2CPP_DATETIME_TRIO_NEGATIVES:-add-out-value create-out-other ctor-kind-int address-add-out-value address-create-out-other address-ctor-kind-int newobj-ctor-bool-int}; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" --datetime-trio-negative "$root/$mode/input/DateTimeTrio.dll" "$corelib" "$mode" || return $?
        local family="$mode" diagnostic
        family=${family#address-};family=${family#newobj-}
        case "$family" in
            add-*) diagnostic='DateTime TryAddTicks requires its exact MemberRef signature' ;;
            create-*) diagnostic='DateTime TryCreate requires its exact MemberRef signature' ;;
            ctor-*) diagnostic='DateTime .ctor requires its exact MemberRef signature' ;;
            *) echo 'unknown DateTime trio negative family' >&2; return 1 ;;
        esac
        for axis in raw diet; do
            local flags=() rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/DateTimeTrio.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || return 1
            grep -Fq "$diagnostic" "$root/$mode/$axis.log" || return 1
        done
    done
}
