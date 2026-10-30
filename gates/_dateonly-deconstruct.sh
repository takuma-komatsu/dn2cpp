#!/usr/bin/env bash
# Public alias/partial-write parity; use as a batch probe, not another persistent native pair.
dateonly_deconstruct_routes_gate() {
    local root="$1" original="$2" mode="${3:-all}"; shift 3
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib flag=--dateonly-deconstruct
    corelib=$(resolve_net10_corelib) || return $?
    [[ "$mode" == all ]] || flag="--dateonly-deconstruct-$mode"
    mkdir -p "$root/input" "$root/oracle-input"
    cp -R "$original/." "$root/input/" || return $?
    cp -R "$original/." "$root/oracle-input/" || return $?
    dotnet "$fixture" "$flag" "$root/input/DateOnlyDeconstruct.dll" "$corelib" || return $?
    # The original stubs call only the public API and have identical observable outputs.
    if [[ "$mode" == reflection ]]; then
        dotnet "$fixture" --dateonly-deconstruct-reflection-oracle "$root/oracle-input/DateOnlyDeconstruct.dll" "$corelib" || return $?
    else
        dotnet "$fixture" --dateonly-deconstruct-oracle "$root/oracle-input/DateOnlyDeconstruct.dll" "$corelib" || return $?
    fi
    gate_capture_ok "$root/oracle" run_bounded dotnet "$root/oracle-input/DateOnlyDeconstruct.dll" || return $?
    local axis out
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@" --link-xml gates/fixtures/dateonly-deconstruct/link.xml) runner=()
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$root/input/DateOnlyDeconstruct.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/dateonly-deconstruct/check-generated.py "$out" || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" DateOnlyDeconstruct || return $?
            runner=(node "$out/DateOnlyDeconstruct.js")
        else
            compile_console "$out" DateOnlyDeconstruct || return $?
            runner=("$out/DateOnlyDeconstruct$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        gate_output compare "$root/oracle.stdout" "$out/native.stdout" --policy crlf || return $?
        gate_output witness "$out/native.stdout" 'dateonly-deconstruct-reflection-abi:True' \
            'dateonly-deconstruct-reflection-null-target:True' 'DateOnly Deconstruct reflection end' --count 1 --policy crlf || return $?
        if [[ "$mode" == all ]]; then
            gate_output witness "$out/native.stdout" 'dateonly-deconstruct-direct-all-1:True' \
                'dateonly-deconstruct-pointer-all-1:True' 'dateonly-deconstruct-delegate-all-1:True' \
                'dateonly-deconstruct-direct-null-receiver:True' 'dateonly-deconstruct-pointer-null-receiver:True' \
                'dateonly-deconstruct-producer-0:True' 'dateonly-deconstruct-producer-1:True' \
                'dateonly-deconstruct-producer-2:True' 'DateOnly Deconstruct alias end' --count 1 --policy crlf || return $?
        elif [[ "$mode" == reflection ]]; then
            if grep -Eq 'Program_(Direct|Pointer|Address|DelegateRoute|ProducerRoute)_m[0-9]+\(' "$out/generated.h"; then
                echo 'FAIL: DateOnly reflection-only rooted another route' >&2; return 1
            fi
        else
            echo 'invalid DateOnly test mode' >&2; return 1
        fi
    done
}

dateonly_deconstruct_negative_gate() {
    local root="$1" original="$2" mode axis corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in ${DN2CPP_DATEONLY_DECONSTRUCT_NEGATIVES:-value element address-value address-element}; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" --dateonly-deconstruct-negative "$root/$mode/input/DateOnlyDeconstruct.dll" "$corelib" "$mode" || return $?
        for axis in raw diet; do
            local flags=() rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/DateOnlyDeconstruct.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || return 1
            local diagnostic='DateOnly Deconstruct requires its exact MemberRef signature'
            case "$mode" in
                arity|address-arity) diagnostic='System.DateOnly: no method Deconstruct/2' ;;
            esac
            grep -Fq "$diagnostic" "$root/$mode/$axis.log" || return 1
        done
    done
}
