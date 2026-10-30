#!/usr/bin/env bash
# Calendar replacement is an explicit native capability boundary, not CLR parity.
dtfi_calendar_setter_routes_gate() {
    local root="$1" original="$2" mode="${3:-all}"; shift 3
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib flag=--dtfi-calendar-setter
    corelib=$(resolve_net10_corelib) || return $?
    [[ "$mode" == all ]] || flag="--dtfi-calendar-setter-$mode"
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" "$flag" "$root/input/DtfICalendarSetter.dll" "$corelib" || return $?
    local axis out
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@") runner=() checker=()
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        [[ "$mode" != all && "$mode" != reflection ]] || flags+=(--link-xml gates/fixtures/dtfi-calendar-setter/link.xml)
        [[ "$mode" != minimal ]] || checker+=(--minimal)
        invoke_cli "$root/input/DtfICalendarSetter.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/dtfi-calendar-setter/check-generated.py "$out" "${checker[@]}" || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" DtfICalendarSetter || return $?
            runner=(node "$out/DtfICalendarSetter.js")
        else
            compile_console "$out" DtfICalendarSetter || return $?
            runner=("$out/DtfICalendarSetter$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        case "$mode" in
            all|direct|noalloc)
                gate_output witness "$out/native.stdout" 'dtfi-calendar-setter-direct-readonly-null:True' \
                    'dtfi-calendar-setter-direct-readonly-value:True' 'dtfi-calendar-setter-direct-mutable-null:True' \
                    'dtfi-calendar-setter-direct-mutable-value:True' 'dtfi-calendar-setter-direct-real-calendar-type:True' \
                    'dtfi-calendar-setter-direct-null-null:True' 'dtfi-calendar-setter-direct-null-value:True' \
                    'dtfi-calendar-setter-producer-null:True' 'dtfi-calendar-setter-producer-throws:True' \
                    'dtfi-calendar-setter-receiver-producer-throws:True' 'dtfi-calendar-setter-direct-end' --count 1 --policy crlf || return $? ;;
            reflection) ;;
            minimal)
                gate_output witness "$out/native.stdout" 'dtfi-calendar-setter-minimal:True' --count 1 --policy crlf || return $? ;;
            *) echo 'invalid Calendar setter test mode' >&2; return 1 ;;
        esac
        if [[ "$mode" == all ]]; then
            gate_output witness "$out/native.stdout" 'dtfi-calendar-setter-callvirt-mutable-value:True' \
                'dtfi-calendar-setter-calli-mutable-value:True' 'dtfi-calendar-setter-delegate-mutable-value:True' \
                'dtfi-calendar-setter-delegate-readonly-null:True' 'dtfi-calendar-setter-delegate-abi:True' \
                'DTFI Calendar setter native capability end' --count 1 --policy crlf || return $?
        fi
        if [[ "$mode" == all || "$mode" == reflection ]]; then
            gate_output witness "$out/native.stdout" 'dtfi-calendar-setter-reflection-readonly-null:True' \
                'dtfi-calendar-setter-reflection-mutable-value:True' 'dtfi-calendar-setter-reflection-null-target:True' \
                'dtfi-calendar-setter-reflection-abi:True' 'dtfi-calendar-setter-reflection-end' --count 1 --policy crlf || return $?
            gate_output witness "$out/native.stdout" 'dtfi-calendar-setter-wrapped:True' --count 4 --policy crlf || return $?
        fi
        if [[ "$mode" == reflection ]] && grep -Eq 'Program_(Direct|Virtual|Pointer|Address|DelegateRoute|ProducerRoute)_m[0-9]+\(' "$out/generated.h"; then
            echo 'FAIL: Calendar setter reflection-only rooted another route' >&2; return 1
        fi
    done
}

dtfi_calendar_setter_negative_gate() {
    local root="$1" original="$2" mode axis corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in ${DN2CPP_DTFI_CALENDAR_SETTER_NEGATIVES:-return byref address-return address-byref}; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" --dtfi-calendar-setter-negative "$root/$mode/input/DtfICalendarSetter.dll" "$corelib" "$mode" || return $?
        for axis in raw diet; do
            local flags=() rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/DtfICalendarSetter.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || { echo "FAIL: malformed Calendar setter $mode/$axis admitted" >&2; return 1; }
            local diagnostic='DateTimeFormatInfo Calendar setter requires its exact MemberRef signature'
            case "$mode" in
                arity|address-arity) diagnostic='System.Globalization.DateTimeFormatInfo: no method set_Calendar/0' ;;
            esac
            grep -Fq "$diagnostic" "$root/$mode/$axis.log" || return 1
        done
    done
}

dtfi_calendar_setter_noalloc_gate() {
    local root="$1" original="$2"; shift 2
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib axis
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" --dtfi-calendar-setter-noalloc "$root/input/DtfICalendarSetter.dll" "$corelib" || return $?
    for axis in raw diet; do
        local flags=("$@")
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        gate_capture_ok "$root/$axis" invoke_cli "$root/input/DtfICalendarSetter.dll" --auto-ref "${flags[@]}" -o "$root/$axis-output" || return $?
        "$(resolve_python)" gates/fixtures/dtfi-calendar-setter/check-generated.py "$root/$axis-output" || return $?
    done
}

dtfi_calendar_setter_focused_gate() {
    local root="$1" fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib model
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return $?
    build_proj gates/fixtures/dtfi-calendar-setter/DtfICalendarSetter.csproj || return $?
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root"
    model=$(mktemp -d "$root/model-XXXXXX") || return $?
    gate_capture_ok "$model/guards" run_bounded dotnet "$fixture" --dtfi-calendar-setter-guards "$(dirname "$DN2CPP_CLI_DLL")/Dn2Cpp.Transpiler.dll" "$corelib" "$model/input.dll" || return $?
    local original="gates/fixtures/dtfi-calendar-setter/bin/$CONFIG/$TFM"
    dtfi_calendar_setter_routes_gate "$root/all" "$original" all || return $?
    dtfi_calendar_setter_negative_gate "$root/malformed" "$original" || return $?
}
