#!/usr/bin/env bash
# Two shared invariant month tables; mutation scope is the finite seven-method family.
dtfi_name_arrays_routes_gate() {
    local root="$1" original="$2" mode="${3:-all}"; shift 3
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib flag=--dtfi-name-arrays
    corelib=$(resolve_net10_corelib) || return $?
    [[ "$mode" == all ]] || flag="--dtfi-name-arrays-$mode"
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" "$flag" "$root/input/DtfINameArrays.dll" "$corelib" || return $?
    local axis out
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@") runner=() checker=()
        [[ "$mode" == minimal ]] || flags+=(--link-xml gates/fixtures/dtfi-name-arrays/link.xml)
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        [[ "$mode" != minimal ]] || checker+=(--minimal)
        [[ "$mode" != reflection ]] || checker+=(--reflection)
        invoke_cli "$root/input/DtfINameArrays.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/dtfi-name-arrays/check-generated.py "$out" "${checker[@]}" || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" DtfINameArrays || return $?
            runner=(node "$out/DtfINameArrays.js")
        else
            compile_console "$out" DtfINameArrays || return $?
            runner=("$out/DtfINameArrays$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        "$(resolve_python)" gates/fixtures/dtfi-name-arrays/check-output.py "$out/native.stdout" --mode "$mode" || return $?
        if [[ "$mode" == reflection ]] && grep -Eq 'Program_(Direct|Virtual|Pointer|Address|Producer)(Genitive|Leap)_m[0-9]+\(' "$out/generated.h"; then
            echo 'FAIL: month-array reflection-only retained alternate routes' >&2; return 1
        fi
    done
}

dtfi_name_arrays_oracle_gate() {
    local root="$1" original="$2" corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" --dtfi-name-arrays-oracle "$root/input/DtfINameArrays.dll" "$corelib" || return $?
    gate_capture_ok "$root/public" run_bounded dotnet "$root/input/DtfINameArrays.dll" || return $?
    "$(resolve_python)" gates/fixtures/dtfi-name-arrays/check-output.py "$root/public.stdout" --mode oracle || return $?
}

dtfi_name_arrays_negative_gate() {
    local root="$1" original="$2" mode axis corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in ${DN2CPP_DTFI_NAME_ARRAYS_NEGATIVES:-genitive-bool-int leap-return address-genitive-bool-int address-leap-return}; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" --dtfi-name-arrays-negative "$root/$mode/input/DtfINameArrays.dll" "$corelib" "$mode" || return $?
        local family=${mode#address-} getter
        case "$family" in
            genitive-*) getter=InternalGetGenitiveMonthNames ;;
            leap-*) getter=InternalGetLeapYearMonthNames ;;
            *) echo 'unknown month-array family' >&2; return 1 ;;
        esac
        for axis in raw diet; do
            local flags=() rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/DtfINameArrays.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || return 1
            case "$mode" in
                *-arity) local arity=1; [[ "$getter" != InternalGetGenitiveMonthNames ]] || arity=2; grep -Fq "System.Globalization.DateTimeFormatInfo: no method $getter/$arity" "$root/$mode/$axis.log" || return 1 ;;
                *) grep -Fq "DateTimeFormatInfo $getter requires its exact MemberRef signature" "$root/$mode/$axis.log" || return 1 ;;
            esac
        done
    done
}


dtfi_name_arrays_noalloc_gate() {
    local root="$1" original="$2" corelib family axis
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for family in genitive leap; do
        mkdir -p "$root/$family/input"
        cp -R "$original/." "$root/$family/input/" || return $?
        dotnet "$fixture" "--dtfi-name-arrays-noalloc-$family" "$root/$family/input/DtfINameArrays.dll" "$corelib" || return $?
        for axis in raw diet; do
            local flags=() rc=0 suffix=Genitive
            [[ "$family" != leap ]] || suffix=Leap
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$family/input/DtfINameArrays.dll" --auto-ref "${flags[@]}" -o "$root/$family/$axis" > "$root/$family/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$family/$axis.status"
            [[ "$rc" == 2 ]] || return 1
            grep -Fq "[HotPath(NoAlloc)] closure of DtfINameArraysSubset.Program::Direct$suffix allocates: DtfINameArraysSubset.Program::Direct$suffix emits dn2cpp_dtfi_invariant_month_names(" "$root/$family/$axis.log" || return 1
        done
    done
}


dtfi_name_arrays_setters_gate() {
    local root="$1" original="$2" corelib family axis
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for family in month genitive; do
        mkdir -p "$root/$family/input"
        cp -R "$original/." "$root/$family/input/" || return $?
        dotnet "$fixture" "--dtfi-name-arrays-setter-$family" "$root/$family/input/DtfINameArrays.dll" "$corelib" || return $?
        for axis in raw diet; do
            local flags=() rc=0 target=set_AbbreviatedMonthNames
            [[ "$family" != genitive ]] || target=set_AbbreviatedMonthGenitiveNames
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$family/input/DtfINameArrays.dll" --auto-ref "${flags[@]}" -o "$root/$family/$axis" > "$root/$family/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$family/$axis.status"
            [[ "$rc" == 2 ]] || return 1
            grep -Fq "System.Globalization.DateTimeFormatInfo::$target(String[]) has no intrinsic mapping yet" "$root/$family/$axis.log" || return 1
            grep -Fq 'has no intrinsic mapping yet' "$root/$family/$axis.log" || return 1
        done
    done
}
