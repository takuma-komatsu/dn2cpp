#!/usr/bin/env bash
# Internal selector policy from installed IL + invariant arrays, never private CLR execution.
internal_month_routes_gate() {
    local root="$1" original="$2" mode="${3:-all}"; shift 3
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib flag=--internal-month
    corelib=$(resolve_net10_corelib) || return $?
    [[ "$mode" == all ]] || flag="--internal-month-$mode"
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" "$flag" "$root/input/InternalMonth.dll" "$corelib" || return $?
    local axis out
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@") runner=() check=()
        if [[ "$mode" == all || "$mode" == reflection ]]; then
            flags+=(--link-xml gates/fixtures/internal-month/link.xml)
        fi
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        [[ "$mode" != minimal ]] || check+=(--minimal)
        invoke_cli "$root/input/InternalMonth.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/internal-month/check-generated.py "$out" "${check[@]}" || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" InternalMonth || return $?
            runner=(node "$out/InternalMonth.js")
        else
            compile_console "$out" InternalMonth || return $?
            runner=("$out/InternalMonth$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        case "$mode" in
            all)
                gate_output witness "$out/native.stdout" 'Internal invariant month selector end' \
                    'internal-month-direct-names-2-True:True' 'internal-month-calli-names-2-True:True' \
                    'internal-month-delegate-names-2-True:True' 'internal-month-reflection-names-2-True:True' \
                    'internal-month-producer-order:True' 'internal-month-producer-throw:True' --count 1 --policy crlf || return $? ;;
            direct)
                gate_output witness "$out/native.stdout" 'internal-month-direct-end' 'internal-month-producer-order:True' 'internal-month-producer-throw:True' --count 1 --policy crlf || return $? ;;
            reflection)
                gate_output witness "$out/native.stdout" 'internal-month-reflection-end' 'internal-month-reflection-names-2-True:True' --count 1 --policy crlf || return $?
                if grep -Eq 'Program_(Direct|Pointer|Address|DelegateRoute)_m[0-9]+\(' "$out/generated.h"; then
                    echo 'FAIL: internal month reflection-only rooted alternate route' >&2; return 1
                fi ;;
            minimal) gate_output witness "$out/native.stdout" 'internal-month-minimal:True' --count 1 --policy crlf || return $? ;;
            *) echo 'invalid internal month mode' >&2; return 1 ;;
        esac
    done
}

internal_month_noalloc_gate() {
    local root="$1" original="$2"; shift 2
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib axis
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" --internal-month-noalloc "$root/input/InternalMonth.dll" "$corelib" || return $?
    for axis in raw diet; do
        local flags=("$@") rc=0
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$root/input/InternalMonth.dll" --auto-ref "${flags[@]}" -o "$root/$axis" > "$root/$axis.log" 2>&1 || rc=$?
        printf '%s\n' "$rc" > "$root/$axis.status"
        [[ "$rc" == 2 ]] || { echo 'FAIL: NoAlloc internal month boxing accepted' >&2; return 1; }
        grep -Fq '[HotPath(NoAlloc)] closure of InternalMonthSubset.Program::Direct allocates: InternalMonthSubset.Program::Direct emits dn2cpp_box(' "$root/$axis.log" || return 1
    done
}

internal_month_negative_gate() {
    local root="$1" original="$2" mode axis corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in ${DN2CPP_INTERNAL_MONTH_NEGATIVES:-style-int bool-int address-style-int address-bool-int}; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" --internal-month-negative "$root/$mode/input/InternalMonth.dll" "$corelib" "$mode" || return $?
        for axis in raw diet; do
            local flags=() rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/InternalMonth.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || { echo "FAIL: malformed internal month $mode/$axis admitted" >&2; return 1; }
            local diagnostic='DateTimeFormatInfo internal month name requires its exact MemberRef signature'
            case "$mode" in
                arity|address-arity) diagnostic='System.Globalization.DateTimeFormatInfo: no method InternalGetMonthName/2' ;;
                arity-four|address-arity-four) diagnostic='System.Globalization.DateTimeFormatInfo: no method InternalGetMonthName/4' ;;
            esac
            grep -Fq "$diagnostic" "$root/$mode/$axis.log" || return 1
        done
    done
}

internal_month_focused_gate() {
    local root="$1" fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib model
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return $?
    build_proj gates/fixtures/internal-month/InternalMonth.csproj || return $?
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root"
    model=$(mktemp -d "$root/model-XXXXXX") || return $?
    gate_capture_ok "$model/guards" run_bounded dotnet "$fixture" --internal-month-guards "$(dirname "$DN2CPP_CLI_DLL")/Dn2Cpp.Transpiler.dll" "$corelib" "$model/input.dll" || return $?
    local original="gates/fixtures/internal-month/bin/$CONFIG/$TFM"
    internal_month_routes_gate "$root/all" "$original" all || return $?
    internal_month_noalloc_gate "$root/noalloc" "$original" || return $?
    internal_month_negative_gate "$root/malformed" "$original" || return $?
}
