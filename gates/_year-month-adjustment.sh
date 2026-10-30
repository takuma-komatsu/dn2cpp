#!/usr/bin/env bash
# Native invariant adjustment policy; never execute rewritten private targets on CLR.
year_month_adjustment_routes_gate() {
    local root="$1" original="$2" mode="${3:-all}"; shift 3
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib flag=--year-month-adjustment
    corelib=$(resolve_net10_corelib) || return $?
    [[ "$mode" == all ]] || flag="--year-month-adjustment-$mode"
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" "$flag" "$root/input/YearMonthAdjustment.dll" "$corelib" || return $?
    local axis out
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@") runner=() check=()
        if [[ "$mode" == all || "$mode" == reflection ]]; then
            flags+=(--link-xml gates/fixtures/year-month-adjustment/link.xml)
        fi
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        [[ "$mode" != minimal ]] || check+=(--minimal)
        invoke_cli "$root/input/YearMonthAdjustment.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/year-month-adjustment/check-generated.py "$out" "${check[@]}" || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" YearMonthAdjustment || return $?
            runner=(node "$out/YearMonthAdjustment.js")
        else
            compile_console "$out" YearMonthAdjustment || return $?
            runner=("$out/YearMonthAdjustment$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        "$(resolve_python)" gates/fixtures/year-month-adjustment/check-output.py "$out/native.stdout" --mode "$mode" || return $?
        if [[ "$mode" == reflection ]] && grep -Eq 'Program_(Direct|Pointer|HasDirect|HasPointer|Address|Virtual|DelegateRoute|ProducerRoute)_m[0-9]+\(' "$out/generated.h"; then
            echo 'FAIL: year/month reflection-only retained an alternate route' >&2; return 1
        fi
    done
}

year_month_adjustment_negative_gate() {
    local root="$1" original="$2" mode axis corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in ${DN2CPP_YEAR_MONTH_ADJUSTMENT_NEGATIVES:-year-value bool-int address-year-value address-bool-int has-return address-has-return}; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" --year-month-adjustment-negative "$root/$mode/input/YearMonthAdjustment.dll" "$corelib" "$mode" || return $?
        for axis in raw diet; do
            local flags=() rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/YearMonthAdjustment.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || { echo "FAIL: malformed YearMonthAdjustment $mode/$axis admitted" >&2; return 1; }
            case "$mode" in
                has-return|has-static|address-has-return|address-has-static)
                    grep -Fq 'DateTimeFormatInfo get_HasYearMonthAdjustment requires its exact MemberRef signature' "$root/$mode/$axis.log" || return 1 ;;
                arity|address-arity) grep -Fq 'System.Globalization.DateTimeFormatInfo: no method YearMonthAdjustment/2' "$root/$mode/$axis.log" || return 1 ;;
                *) grep -Fq 'DateTimeFormatInfo YearMonthAdjustment requires its exact MemberRef signature' "$root/$mode/$axis.log" || return 1 ;;
            esac
        done
    done
}
