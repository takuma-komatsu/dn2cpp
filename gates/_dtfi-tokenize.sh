#!/usr/bin/env bash
# Exact native Tokenize refusal; never execute the rewritten private target on CLR.
dtfi_tokenize_routes_gate() {
    local root="$1" original="$2" mode="${3:-all}"; shift 3
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib flag=--dtfi-tokenize
    corelib=$(resolve_net10_corelib) || return $?
    [[ "$mode" == all ]] || flag="--dtfi-tokenize-$mode"
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" "$flag" "$root/input/DtfITokenize.dll" "$corelib" || return $?
    local axis out
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@") runner=() checker=(--mode "$mode")
        [[ "$mode" == minimal ]] || flags+=(--link-xml gates/fixtures/dtfi-tokenize/link.xml)
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$root/input/DtfITokenize.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/dtfi-tokenize/check-generated.py "$out" "${checker[@]}" || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" DtfITokenize || return $?
            runner=(node "$out/DtfITokenize.js")
        else
            compile_console "$out" DtfITokenize || return $?
            runner=("$out/DtfITokenize$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        "$(resolve_python)" gates/fixtures/dtfi-tokenize/check-output.py "$out/native.stdout" --mode "$mode" || return $?
        if [[ "$mode" == reflection ]] && grep -Eq 'Program_(Direct|Virtual|Pointer|Address|Producer|VirtualProducer)_m[0-9]+\(' "$out/generated.h"; then
            echo 'FAIL: Tokenize reflection-only retained alternate routes' >&2; return 1
        fi
    done
}

dtfi_tokenize_oracle_gate() {
    local root="$1" original="$2" corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" --dtfi-tokenize-oracle "$root/input/DtfITokenize.dll" "$corelib" || return $?
    gate_capture_ok "$root/public" run_bounded dotnet "$root/input/DtfITokenize.dll" || return $?
    "$(resolve_python)" gates/fixtures/dtfi-tokenize/check-output.py "$root/public.stdout" --mode oracle || return $?
}

dtfi_tokenize_negative_gate() {
    local root="$1" original="$2" mode axis corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in ${DN2CPP_DTFI_TOKENIZE_NEGATIVES:-mask-int parser-value address-mask-int address-parser-value}; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" --dtfi-tokenize-negative "$root/$mode/input/DtfITokenize.dll" "$corelib" "$mode" || return $?
        for axis in raw diet; do
            local flags=() rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/DtfITokenize.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || return 1
            if [[ "$mode" == *arity ]]; then
                grep -Fq 'System.Globalization.DateTimeFormatInfo: no method Tokenize/3' "$root/$mode/$axis.log" || return 1
            else
                grep -Fq 'DateTimeFormatInfo Tokenize requires its exact MemberRef signature' "$root/$mode/$axis.log" || return 1
            fi
        done
    done
}

# Direct is annotated in every native fixture; aggregate runtime already tests its refusal.
dtfi_tokenize_noalloc_gate() {
    local root="$1" original="$2" corelib axis
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" --dtfi-tokenize-noalloc "$root/input/DtfITokenize.dll" "$corelib" || return $?
    for axis in raw diet; do
        local flags=()
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$root/input/DtfITokenize.dll" --auto-ref "${flags[@]}" -o "$root/$axis" || return $?
        "$(resolve_python)" gates/fixtures/dtfi-tokenize/check-generated.py "$root/$axis" --mode minimal || return $?
    done
}
