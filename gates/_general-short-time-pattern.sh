#!/usr/bin/env bash
# Exact invariant composite-pattern getters, no mutable-cache parity.
_general_time_pattern_routes_gate() {
    local family="$1"; shift
    local assembly prefix title target
    case "$family" in
        short) assembly=GeneralShortTimePattern; prefix=general-short-time-pattern; title='Invariant general short-time pattern'; target=get_GeneralShortTimePattern ;;
        long) assembly=GeneralLongTimePattern; prefix=general-long-time-pattern; title='Invariant general long-time pattern'; target=get_GeneralLongTimePattern ;;
        offset) assembly=DateTimeOffsetPattern; prefix=datetime-offset-pattern; title='Invariant datetime offset pattern'; target=get_DateTimeOffsetPattern ;;
        *) echo 'invalid general time pattern test family' >&2; return 1 ;;
    esac
    local root="$1" original="$2" mode="${3:-all}"; shift 3
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib flag="--$prefix"
    corelib=$(resolve_net10_corelib) || return $?
    [[ "$mode" == all ]] || flag="--$prefix-$mode"
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" "$flag" "$root/input/${assembly}.dll" "$corelib" || return $?
    local axis out
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@") runner=() check=()
        [[ "$family" != long ]] || check+=(--long)
        [[ "$family" != offset ]] || check+=(--offset)
        if [[ "$mode" == all || "$mode" == reflection ]]; then
            flags+=(--link-xml "gates/fixtures/$prefix/link.xml")
        fi
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        [[ "$mode" != minimal ]] || check+=(--minimal)
        invoke_cli "$root/input/${assembly}.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/general-short-time-pattern/check-generated.py "$out" "${check[@]}" || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" "$assembly" || return $?
            runner=(node "$out/${assembly}.js")
        else
            compile_console "$out" "$assembly" || return $?
            runner=("$out/${assembly}$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        case "$mode" in
            all)
                gate_output witness "$out/native.stdout" "$title end" "${prefix}-direct-null:True" "${prefix}-calli-null:True" \
                    "${prefix}-rooted-after-collect:True" "${prefix}-receiver-once:True" "${prefix}-receiver-throws:True" "${prefix}-delegate-return:True" \
                    "${prefix}-reflection-return:True" "${prefix}-reflection-null:True" --count 1 --policy crlf || return $?
                for route in direct calli delegate reflection; do
                    gate_output witness "$out/native.stdout" "$prefix-$route-content:True" "$prefix-$route-actual-string:True" "$prefix-$route-rooted-identity:True" --count 2 --policy crlf || return $?
                done ;;
            direct|noalloc)
                gate_output witness "$out/native.stdout" "${prefix}-direct-end" "${prefix}-direct-null:True" "${prefix}-rooted-after-collect:True" "${prefix}-receiver-once:True" "${prefix}-receiver-throws:True" --count 1 --policy crlf || return $? ;;
            reflection)
                gate_output witness "$out/native.stdout" "${prefix}-reflection-end" "${prefix}-reflection-null:True" --count 1 --policy crlf || return $?
                gate_output witness "$out/native.stdout" "${prefix}-reflection-content:True" "${prefix}-reflection-actual-string:True" "${prefix}-reflection-rooted-identity:True" --count 2 --policy crlf || return $?
                if grep -Eq 'Program_(Direct|Pointer|Address|DelegateRoute)_m[0-9]+\(' "$out/generated.h"; then
                    echo 'FAIL: GeneralShortTimePattern reflection-only rooted alternate route' >&2; return 1
                fi ;;
            minimal) gate_output witness "$out/native.stdout" "${prefix}-minimal:True" --count 1 --policy crlf || return $? ;;
            *) echo 'invalid GeneralShortTimePattern test mode' >&2; return 1 ;;
        esac
    done
}


general_short_time_pattern_routes_gate() { _general_time_pattern_routes_gate short "$@"; }
general_long_time_pattern_routes_gate() { _general_time_pattern_routes_gate long "$@"; }
datetime_offset_pattern_routes_gate() { _general_time_pattern_routes_gate offset "$@"; }

_general_time_pattern_negative_gate() {
    local family="$1"; shift
    local assembly prefix title target
    case "$family" in
        short) assembly=GeneralShortTimePattern; prefix=general-short-time-pattern; title='Invariant general short-time pattern'; target=get_GeneralShortTimePattern ;;
        long) assembly=GeneralLongTimePattern; prefix=general-long-time-pattern; title='Invariant general long-time pattern'; target=get_GeneralLongTimePattern ;;
        offset) assembly=DateTimeOffsetPattern; prefix=datetime-offset-pattern; title='Invariant datetime offset pattern'; target=get_DateTimeOffsetPattern ;;
        *) echo 'invalid general time pattern test family' >&2; return 1 ;;
    esac
    local root="$1" original="$2" mode axis corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in ${DN2CPP_GENERAL_SHORT_TIME_PATTERN_NEGATIVES:-int enum address-int address-enum}; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" "--$prefix-negative" "$root/$mode/input/${assembly}.dll" "$corelib" "$mode" || return $?
        for axis in raw diet; do
            local flags=() rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/${assembly}.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || { echo "FAIL: malformed GeneralShortTimePattern $mode/$axis admitted" >&2; return 1; }
            case "$mode" in
                arity|address-arity) grep -Fq "System.Globalization.DateTimeFormatInfo: no method $target/1" "$root/$mode/$axis.log" || return 1 ;;
                *) grep -Fq "DateTimeFormatInfo $assembly requires its exact MemberRef signature" "$root/$mode/$axis.log" || return 1 ;;
            esac
        done
    done
}


general_short_time_pattern_negative_gate() { _general_time_pattern_negative_gate short "$@"; }
general_long_time_pattern_negative_gate() { _general_time_pattern_negative_gate long "$@"; }
datetime_offset_pattern_negative_gate() { _general_time_pattern_negative_gate offset "$@"; }

_general_time_pattern_focused_gate() {
    local family="$1"; shift
    local assembly prefix title target
    case "$family" in
        short) assembly=GeneralShortTimePattern; prefix=general-short-time-pattern; title='Invariant general short-time pattern'; target=get_GeneralShortTimePattern ;;
        long) assembly=GeneralLongTimePattern; prefix=general-long-time-pattern; title='Invariant general long-time pattern'; target=get_GeneralLongTimePattern ;;
        offset) assembly=DateTimeOffsetPattern; prefix=datetime-offset-pattern; title='Invariant datetime offset pattern'; target=get_DateTimeOffsetPattern ;;
        *) echo 'invalid general time pattern test family' >&2; return 1 ;;
    esac
    local root="$1" fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib model
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return $?
    build_proj "gates/fixtures/$prefix/$assembly.csproj" || return $?
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root"
    model=$(mktemp -d "$root/model-XXXXXX") || return $?
    gate_capture_ok "$model/guards" run_bounded dotnet "$fixture" --general-short-time-pattern-guards "$(dirname "$DN2CPP_CLI_DLL")/Dn2Cpp.Transpiler.dll" "$corelib" "$model/input.dll" || return $?
    local original="gates/fixtures/$prefix/bin/$CONFIG/$TFM"
    _general_time_pattern_routes_gate "$family" "$root/all" "$original" all || return $?
    _general_time_pattern_negative_gate "$family" "$root/malformed" "$original" || return $?
}


general_short_time_pattern_focused_gate() { _general_time_pattern_focused_gate short "$@"; }
general_long_time_pattern_focused_gate() { _general_time_pattern_focused_gate long "$@"; }
datetime_offset_pattern_focused_gate() { _general_time_pattern_focused_gate offset "$@"; }
