#!/usr/bin/env bash
# Private DTFI probes execute only in native output; the CLR oracle uses public CultureInfo.
dtfi_compare_info_routes_gate() {
    local root="$1" original="$2" mode="${3:-all}"; shift 3
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib flag=--dtfi-compare-info
    corelib=$(resolve_net10_corelib) || return $?
    [[ "$mode" == all ]] || flag="--dtfi-compare-info-$mode"
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" "$flag" "$root/input/DtfICompareInfo.dll" "$corelib" || return $?
    local axis out
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@") runner=()
        [[ "$mode" == minimal* ]] || flags+=(--link-xml gates/fixtures/dtfi-compare-info/link.xml)
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$root/input/DtfICompareInfo.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/dtfi-compare-info/check-generated.py "$out" --mode "$mode" || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" DtfICompareInfo || return $?
            runner=(node "$out/DtfICompareInfo.js")
        else
            compile_console "$out" DtfICompareInfo || return $?
            runner=("$out/DtfICompareInfo$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        "$(resolve_python)" gates/fixtures/dtfi-compare-info/check-output.py "$out/native.stdout" --mode "$mode" || return $?
    done
}

dtfi_compare_info_oracle_gate() {
    local root="$1" original="$2" corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" --dtfi-compare-info-oracle "$root/input/DtfICompareInfo.dll" "$corelib" || return $?
    gate_capture_ok "$root/public" run_bounded dotnet "$root/input/DtfICompareInfo.dll" || return $?
    "$(resolve_python)" gates/fixtures/dtfi-compare-info/check-output.py "$root/public.stdout" --mode oracle || return $?
}

dtfi_compare_info_negative_gate() {
    local root="$1" original="$2" mode axis corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in ${DN2CPP_DTFI_COMPARE_INFO_NEGATIVES:-dtfi-return culture-byref address-dtfi-return address-culture-byref}; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" --dtfi-compare-info-negative "$root/$mode/input/DtfICompareInfo.dll" "$corelib" "$mode" || return $?
        for axis in raw diet; do
            local flags=() rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/DtfICompareInfo.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || return 1
            grep -Fq 'Invariant CompareInfo requires its exact MemberRef signature' "$root/$mode/$axis.log" || return 1
        done
    done
}

dtfi_compare_info_noalloc_gate() {
    local root="$1" original="$2" owner axis corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for owner in dtfi culture; do
        mkdir -p "$root/$owner/input"
        cp -R "$original/." "$root/$owner/input/" || return $?
        dotnet "$fixture" "--dtfi-compare-info-noalloc-$owner" "$root/$owner/input/DtfICompareInfo.dll" "$corelib" || return $?
        for axis in raw diet; do
            local flags=() rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$owner/input/DtfICompareInfo.dll" --auto-ref "${flags[@]}" -o "$root/$owner/$axis" > "$root/$owner/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$owner/$axis.status"
            [[ "$rc" == 2 ]] || return 1
            local method=DirectDtfI
            [[ "$owner" != culture ]] || method=DirectCulture
            local diagnostic="error: [HotPath(NoAlloc)] closure of DtfICompareInfoSubset.Program::$method allocates: DtfICompareInfoSubset.Program::$method emits dn2cpp_invariant_compare_info_"
            [[ "$(grep -c '^error:' "$root/$owner/$axis.log")" == 1 ]] || return 1
            gate_output witness "$root/$owner/$axis.log" "$diagnostic" --count 1 --policy crlf || return $?
        done
    done
}

# Compile-only authority controls. Never execute the unsupported subclass fixture.
dtfi_compare_info_subclass_gate() {
    local root="$1" original="$2" mode axis corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in virtual address reflection base base-preserved loaded lookup-only; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        local fixture_mode="$mode"
        [[ "$mode" != base-preserved ]] || fixture_mode=base
        dotnet "$fixture" "--dtfi-compare-info-subclass-$fixture_mode" "$root/$mode/input/DtfICompareInfo.dll" "$corelib" || return $?
        for axis in raw diet; do
            local flags=() rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            if [[ "$mode" == reflection || "$mode" == base-preserved || "$mode" == lookup-only ]]; then
                flags+=(--link-xml gates/fixtures/dtfi-compare-info/link.xml)
            fi
            invoke_cli "$root/$mode/input/DtfICompareInfo.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            case "$mode" in
                base|base-preserved|loaded|lookup-only) [[ "$rc" == 0 ]] || return 1 ;;
                *) [[ "$rc" == 2 ]] || return 1
                   grep -Fq 'CultureInfo.CompareInfo override dispatch is not supported for ' "$root/$mode/$axis.log" || return 1 ;;
            esac
        done
    done
}
