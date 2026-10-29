#!/usr/bin/env bash
# Native invariant Gregorian year policy; public-data derivation remains a separate CLR artifact.
force_two_digit_years_routes_gate() {
    local root="$1" original="$2" mode="${3:-all}"; shift 3
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib flag=--force-two-digit-years
    corelib=$(resolve_net10_corelib) || return $?
    [[ "$mode" == all ]] || flag="--force-two-digit-years-$mode"
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" "$flag" "$root/input/ForceTwoDigitYears.dll" "$corelib" || return $?
    local axis out
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@") runner=() check=()
        if [[ "$mode" == all || "$mode" == reflection ]]; then
            flags+=(--link-xml gates/fixtures/force-two-digit-years/link.xml)
        fi
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        [[ "$mode" != minimal ]] || check+=(--minimal)
        invoke_cli "$root/input/ForceTwoDigitYears.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/force-two-digit-years/check-generated.py "$out" "${check[@]}" || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" ForceTwoDigitYears || return $?
            runner=(node "$out/ForceTwoDigitYears.js")
        else
            compile_console "$out" ForceTwoDigitYears || return $?
            runner=("$out/ForceTwoDigitYears$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        case "$mode" in
            all)
                gate_output witness "$out/native.stdout" 'Invariant force-two-digit years end' 'force-two-digit-years-direct-null:True' 'force-two-digit-years-calli-null:True' \
                    'force-two-digit-years-receiver-once:True' 'force-two-digit-years-receiver-throws:True' 'force-two-digit-years-delegate-return:True' \
                    'force-two-digit-years-reflection-return:True' 'force-two-digit-years-reflection-null:True' --count 1 --policy crlf || return $?
                for route in direct calli delegate reflection; do
                    gate_output witness "$out/native.stdout" "force-two-digit-years-$route-zero:True" "force-two-digit-years-$route-actual-bool:True" "force-two-digit-years-$route-name:True" --count 2 --policy crlf || return $?
                done ;;
            direct|noalloc)
                gate_output witness "$out/native.stdout" 'force-two-digit-years-direct-end' 'force-two-digit-years-direct-null:True' 'force-two-digit-years-receiver-once:True' 'force-two-digit-years-receiver-throws:True' --count 1 --policy crlf || return $? ;;
            reflection)
                gate_output witness "$out/native.stdout" 'force-two-digit-years-reflection-end' 'force-two-digit-years-reflection-null:True' --count 1 --policy crlf || return $?
                gate_output witness "$out/native.stdout" 'force-two-digit-years-reflection-zero:True' 'force-two-digit-years-reflection-actual-bool:True' 'force-two-digit-years-reflection-name:True' --count 2 --policy crlf || return $?
                if grep -Eq 'Program_(Direct|Pointer|Address|DelegateRoute)_m[0-9]+\(' "$out/generated.h"; then
                    echo 'FAIL: ForceTwoDigitYears reflection-only rooted alternate route' >&2; return 1
                fi ;;
            minimal) gate_output witness "$out/native.stdout" 'force-two-digit-years-minimal:True' --count 1 --policy crlf || return $? ;;
            *) echo 'invalid ForceTwoDigitYears test mode' >&2; return 1 ;;
        esac
    done
}

force_two_digit_years_negative_gate() {
    local root="$1" original="$2" mode axis corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in ${DN2CPP_FORCE_TWO_DIGIT_YEARS_NEGATIVES:-int enum address-int address-enum}; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" --force-two-digit-years-negative "$root/$mode/input/ForceTwoDigitYears.dll" "$corelib" "$mode" || return $?
        for axis in raw diet; do
            local flags=() rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/ForceTwoDigitYears.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || { echo "FAIL: malformed ForceTwoDigitYears $mode/$axis admitted" >&2; return 1; }
            case "$mode" in
                arity|address-arity) grep -Fq 'System.Globalization.DateTimeFormatInfo: no method get_HasForceTwoDigitYears/1' "$root/$mode/$axis.log" || return 1 ;;
                *) grep -Fq 'DateTimeFormatInfo HasForceTwoDigitYears requires its exact MemberRef signature' "$root/$mode/$axis.log" || return 1 ;;
            esac
        done
    done
}

force_two_digit_years_focused_gate() {
    local root="$1" fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib model
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return $?
    build_proj gates/fixtures/force-two-digit-years/ForceTwoDigitYears.csproj || return $?
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root"
    model=$(mktemp -d "$root/model-XXXXXX") || return $?
    gate_capture_ok "$model/guards" run_bounded dotnet "$fixture" --force-two-digit-years-guards "$(dirname "$DN2CPP_CLI_DLL")/Dn2Cpp.Transpiler.dll" "$corelib" "$model/input.dll" || return $?
    local original="gates/fixtures/force-two-digit-years/bin/$CONFIG/$TFM"
    force_two_digit_years_routes_gate "$root/all" "$original" all || return $?
    force_two_digit_years_negative_gate "$root/malformed" "$original" || return $?
}
