#!/usr/bin/env bash
# Native capability policy, deliberately not Calendar object parity with CLR.
dtfi_calendar_routes_gate() {
    local root="$1" original="$2" mode="${3:-all}"; shift 3
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib flag=--dtfi-calendar
    corelib=$(resolve_net10_corelib) || return $?
    [[ "$mode" == all ]] || flag="--dtfi-calendar-$mode"
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" "$flag" "$root/input/DtfICalendar.dll" "$corelib" || return $?
    local axis out
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@" --link-xml gates/fixtures/dtfi-calendar/link.xml) runner=()
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$root/input/DtfICalendar.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/dtfi-calendar/check-generated.py "$out" || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" DtfICalendar || return $?
            runner=(node "$out/DtfICalendar.js")
        else
            compile_console "$out" DtfICalendar || return $?
            runner=("$out/DtfICalendar$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        case "$mode" in
            all)
                gate_output witness "$out/native.stdout" 'dtfi-calendar-direct-refused:True' 'dtfi-calendar-direct-null:True' \
                    'dtfi-calendar-callvirt-refused:True' 'dtfi-calendar-callvirt-null:True' \
                    'dtfi-calendar-calli-refused:True' 'dtfi-calendar-calli-null:True' 'dtfi-calendar-delegate-refused:True' \
                    'dtfi-calendar-delegate-return-abi:True' 'dtfi-calendar-culture-calendar-neighbor:True' 'DTFI Calendar native capability end' --count 1 --policy crlf || return $? ;;
            direct|noalloc)
                gate_output witness "$out/native.stdout" 'dtfi-calendar-direct-refused:True' 'dtfi-calendar-direct-null:True' 'dtfi-calendar-direct-end' --count 1 --policy crlf || return $? ;;
            reflection) ;;
            *) echo 'invalid Calendar test mode' >&2; return 1 ;;
        esac
        if [[ "$mode" == all || "$mode" == reflection ]]; then
            gate_output witness "$out/native.stdout" 'dtfi-calendar-reflection-wrapped:True' 'dtfi-calendar-reflection-refused:True' \
                'dtfi-calendar-reflection-null-target:True' 'dtfi-calendar-reflection-return-abi:True' 'dtfi-calendar-reflection-end' --count 1 --policy crlf || return $?
        fi
        if [[ "$mode" == reflection ]] && grep -Eq 'Program_(Direct|Virtual|Pointer|Address|DelegateRoute)_m[0-9]+\(' "$out/generated.h"; then
            echo 'FAIL: Calendar reflection-only fixture rooted a direct/address route' >&2; return 1
        fi
    done
}

dtfi_calendar_negative_gate() {
    local root="$1" original="$2" mode axis corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in ${DN2CPP_DTFI_CALENDAR_NEGATIVES:-return byref address-return address-byref}; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" --dtfi-calendar-negative "$root/$mode/input/DtfICalendar.dll" "$corelib" "$mode" || return $?
        for axis in raw diet; do
            local flags=() rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/DtfICalendar.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || { echo "FAIL: malformed Calendar $mode/$axis admitted" >&2; return 1; }
            local diagnostic='DateTimeFormatInfo Calendar requires its exact MemberRef signature'
            # Added parameters fail the exact arity lookup before descriptor matching.
            case "$mode" in
                argument|address-argument) diagnostic='System.Globalization.DateTimeFormatInfo: no method get_Calendar/1' ;;
            esac
            grep -Fq "$diagnostic" "$root/$mode/$axis.log" || return 1
        done
    done
}

dtfi_calendar_focused_gate() {
    local root="$1" fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib model
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return $?
    build_proj gates/fixtures/dtfi-calendar/DtfICalendar.csproj || return $?
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root"
    model=$(mktemp -d "$root/model-XXXXXX") || return $?
    gate_capture_ok "$model/guards" run_bounded dotnet "$fixture" --dtfi-calendar-guards "$(dirname "$DN2CPP_CLI_DLL")/Dn2Cpp.Transpiler.dll" "$corelib" "$model/input.dll" || return $?
    local original="gates/fixtures/dtfi-calendar/bin/$CONFIG/$TFM"
    dtfi_calendar_routes_gate "$root/all" "$original" all || return $?
    dtfi_calendar_noalloc_gate "$root/noalloc" "$original" || return $?
    dtfi_calendar_negative_gate "$root/malformed" "$original" || return $?
}

# Persistent NoAlloc admission; one-time routes mode noalloc also executes the boundary.
dtfi_calendar_noalloc_gate() {
    local root="$1" original="$2"; shift 2
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib axis
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" --dtfi-calendar-noalloc "$root/input/DtfICalendar.dll" "$corelib" || return $?
    for axis in raw diet; do
        local flags=("$@")
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        gate_capture_ok "$root/$axis" invoke_cli "$root/input/DtfICalendar.dll" --auto-ref "${flags[@]}" -o "$root/$axis-output" || return $?
        "$(resolve_python)" gates/fixtures/dtfi-calendar/check-generated.py "$root/$axis-output" || return $?
    done
}

dtfi_calendar_minimal_gate() {
    local root="$1" original="$2"; shift 2
    local axis
    mkdir -p "$root"
    for axis in raw diet; do
        local flags=("$@") out="$root/$axis" runner=()
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$original/MinimalCalendar.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/dtfi-calendar/check-generated.py "$out" --minimal || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" MinimalCalendar || return $?
            runner=(node "$out/MinimalCalendar.js")
        else
            compile_console "$out" MinimalCalendar || return $?
            runner=("$out/MinimalCalendar$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        gate_output witness "$out/native.stdout" 'dtfi-calendar-minimal:True' --count 1 --policy crlf || return $?
    done
}
