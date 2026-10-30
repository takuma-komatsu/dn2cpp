#!/usr/bin/env bash
# Native invariant composite-pattern policy; public-data derivation remains a separate CLR artifact.
general_short_time_pattern_routes_gate() {
    local root="$1" original="$2" mode="${3:-all}"; shift 3
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib flag=--general-short-time-pattern
    corelib=$(resolve_net10_corelib) || return $?
    [[ "$mode" == all ]] || flag="--general-short-time-pattern-$mode"
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" "$flag" "$root/input/GeneralShortTimePattern.dll" "$corelib" || return $?
    local axis out
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@") runner=() check=()
        if [[ "$mode" == all || "$mode" == reflection ]]; then
            flags+=(--link-xml gates/fixtures/general-short-time-pattern/link.xml)
        fi
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        [[ "$mode" != minimal ]] || check+=(--minimal)
        invoke_cli "$root/input/GeneralShortTimePattern.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/general-short-time-pattern/check-generated.py "$out" "${check[@]}" || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" GeneralShortTimePattern || return $?
            runner=(node "$out/GeneralShortTimePattern.js")
        else
            compile_console "$out" GeneralShortTimePattern || return $?
            runner=("$out/GeneralShortTimePattern$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        case "$mode" in
            all)
                gate_output witness "$out/native.stdout" 'Invariant general short-time pattern end' 'general-short-time-pattern-direct-null:True' 'general-short-time-pattern-calli-null:True' \
                    'general-short-time-pattern-rooted-after-collect:True' 'general-short-time-pattern-receiver-once:True' 'general-short-time-pattern-receiver-throws:True' 'general-short-time-pattern-delegate-return:True' \
                    'general-short-time-pattern-reflection-return:True' 'general-short-time-pattern-reflection-null:True' --count 1 --policy crlf || return $?
                for route in direct calli delegate reflection; do
                    gate_output witness "$out/native.stdout" "general-short-time-pattern-$route-content:True" "general-short-time-pattern-$route-actual-string:True" "general-short-time-pattern-$route-rooted-identity:True" --count 2 --policy crlf || return $?
                done ;;
            direct|noalloc)
                gate_output witness "$out/native.stdout" 'general-short-time-pattern-direct-end' 'general-short-time-pattern-direct-null:True' 'general-short-time-pattern-rooted-after-collect:True' 'general-short-time-pattern-receiver-once:True' 'general-short-time-pattern-receiver-throws:True' --count 1 --policy crlf || return $? ;;
            reflection)
                gate_output witness "$out/native.stdout" 'general-short-time-pattern-reflection-end' 'general-short-time-pattern-reflection-null:True' --count 1 --policy crlf || return $?
                gate_output witness "$out/native.stdout" 'general-short-time-pattern-reflection-content:True' 'general-short-time-pattern-reflection-actual-string:True' 'general-short-time-pattern-reflection-rooted-identity:True' --count 2 --policy crlf || return $?
                if grep -Eq 'Program_(Direct|Pointer|Address|DelegateRoute)_m[0-9]+\(' "$out/generated.h"; then
                    echo 'FAIL: GeneralShortTimePattern reflection-only rooted alternate route' >&2; return 1
                fi ;;
            minimal) gate_output witness "$out/native.stdout" 'general-short-time-pattern-minimal:True' --count 1 --policy crlf || return $? ;;
            *) echo 'invalid GeneralShortTimePattern test mode' >&2; return 1 ;;
        esac
    done
}

general_short_time_pattern_negative_gate() {
    local root="$1" original="$2" mode axis corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in ${DN2CPP_GENERAL_SHORT_TIME_PATTERN_NEGATIVES:-int enum address-int address-enum}; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" --general-short-time-pattern-negative "$root/$mode/input/GeneralShortTimePattern.dll" "$corelib" "$mode" || return $?
        for axis in raw diet; do
            local flags=() rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/GeneralShortTimePattern.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || { echo "FAIL: malformed GeneralShortTimePattern $mode/$axis admitted" >&2; return 1; }
            case "$mode" in
                arity|address-arity) grep -Fq 'System.Globalization.DateTimeFormatInfo: no method get_GeneralShortTimePattern/1' "$root/$mode/$axis.log" || return 1 ;;
                *) grep -Fq 'DateTimeFormatInfo GeneralShortTimePattern requires its exact MemberRef signature' "$root/$mode/$axis.log" || return 1 ;;
            esac
        done
    done
}

general_short_time_pattern_focused_gate() {
    local root="$1" fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib model
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return $?
    build_proj gates/fixtures/general-short-time-pattern/GeneralShortTimePattern.csproj || return $?
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root"
    model=$(mktemp -d "$root/model-XXXXXX") || return $?
    gate_capture_ok "$model/guards" run_bounded dotnet "$fixture" --general-short-time-pattern-guards "$(dirname "$DN2CPP_CLI_DLL")/Dn2Cpp.Transpiler.dll" "$corelib" "$model/input.dll" || return $?
    local original="gates/fixtures/general-short-time-pattern/bin/$CONFIG/$TFM"
    general_short_time_pattern_routes_gate "$root/all" "$original" all || return $?
    general_short_time_pattern_negative_gate "$root/malformed" "$original" || return $?
}
