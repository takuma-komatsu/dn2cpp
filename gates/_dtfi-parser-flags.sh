#!/usr/bin/env bash
# Three exact field-free parser flags over the existing invariant FormatFlags contract.
dtfi_parser_flags_routes_gate() {
    local root="$1" original="$2" mode="${3:-all}"; shift 3
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib flag=--dtfi-parser-flags
    corelib=$(resolve_net10_corelib) || return $?
    [[ "$mode" == all ]] || flag="--dtfi-parser-flags-$mode"
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" "$flag" "$root/input/DtfIParserFlags.dll" "$corelib" || return $?
    local axis out
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@") runner=() checker=()
        [[ "$mode" == minimal ]] || flags+=(--link-xml gates/fixtures/dtfi-parser-flags/link.xml)
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        [[ "$mode" != minimal ]] || checker+=(--minimal)
        invoke_cli "$root/input/DtfIParserFlags.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/dtfi-parser-flags/check-generated.py "$out" "${checker[@]}" || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" DtfIParserFlags || return $?
            runner=(node "$out/DtfIParserFlags.js")
        else
            compile_console "$out" DtfIParserFlags || return $?
            runner=("$out/DtfIParserFlags$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        "$(resolve_python)" gates/fixtures/dtfi-parser-flags/check-output.py "$out/native.stdout" --mode "$mode" || return $?
        if [[ "$mode" == reflection ]] && grep -Eq 'Program_(Direct|Virtual|Pointer|Address|Producer)(Month|Day|Adjustment)_m[0-9]+\(' "$out/generated.h"; then
            echo 'FAIL: parser flag reflection-only retained alternate routes' >&2; return 1
        fi
    done
}

dtfi_parser_flags_oracle_gate() {
    local root="$1" original="$2" corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" --dtfi-parser-flags-oracle "$root/input/DtfIParserFlags.dll" "$corelib" || return $?
    gate_capture_ok "$root/public" run_bounded dotnet "$root/input/DtfIParserFlags.dll" || return $?
    "$(resolve_python)" gates/fixtures/dtfi-parser-flags/check-output.py "$root/public.stdout" --mode oracle || return $?
}

dtfi_parser_flags_negative_gate() {
    local root="$1" original="$2" mode axis corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in ${DN2CPP_DTFI_PARSER_FLAGS_NEGATIVES:-month-int day-byref address-month-int address-day-byref}; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" --dtfi-parser-flags-negative "$root/$mode/input/DtfIParserFlags.dll" "$corelib" "$mode" || return $?
        local family=${mode#address-} getter
        case "$family" in
            month-*) getter=get_HasSpacesInMonthNames ;;
            day-*) getter=get_HasSpacesInDayNames ;;
            adjustment-*) getter=get_HasYearMonthAdjustment ;;
            *) echo 'unknown parser flag family' >&2; return 1 ;;
        esac
        for axis in raw diet; do
            local flags=() rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/DtfIParserFlags.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || return 1
            case "$mode" in
                *-arity) grep -Fq "System.Globalization.DateTimeFormatInfo: no method $getter/1" "$root/$mode/$axis.log" || return 1 ;;
                *) grep -Fq "DateTimeFormatInfo $getter requires its exact MemberRef signature" "$root/$mode/$axis.log" || return 1 ;;
            esac
        done
    done
}
