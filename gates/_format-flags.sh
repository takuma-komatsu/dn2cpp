#!/usr/bin/env bash
# Native typed invariant flags; public-data derivation remains a separate CLR artifact.
format_flags_routes_gate() {
    local root="$1" original="$2" mode="${3:-all}"; shift 3
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib flag=--format-flags
    corelib=$(resolve_net10_corelib) || return $?
    [[ "$mode" == all ]] || flag="--format-flags-$mode"
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" "$flag" "$root/input/FormatFlags.dll" "$corelib" || return $?
    local axis out
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@") runner=() check=()
        if [[ "$mode" == all || "$mode" == reflection ]]; then
            flags+=(--link-xml gates/fixtures/format-flags/link.xml)
        fi
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        [[ "$mode" != minimal ]] || check+=(--minimal)
        invoke_cli "$root/input/FormatFlags.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/format-flags/check-generated.py "$out" "${check[@]}" || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" FormatFlags || return $?
            runner=(node "$out/FormatFlags.js")
        else
            compile_console "$out" FormatFlags || return $?
            runner=("$out/FormatFlags$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        case "$mode" in
            all)
                gate_output witness "$out/native.stdout" 'Invariant format flags end' 'format-flags-direct-null:True' 'format-flags-calli-null:True' \
                    'format-flags-receiver-once:True' 'format-flags-receiver-throws:True' 'format-flags-delegate-return:True' \
                    'format-flags-reflection-return:True' 'format-flags-reflection-null:True' --count 1 --policy crlf || return $?
                for route in direct calli delegate reflection; do
                    gate_output witness "$out/native.stdout" "format-flags-$route-zero:True" "format-flags-$route-actual-enum:True" "format-flags-$route-name:True" --count 2 --policy crlf || return $?
                done ;;
            direct|noalloc)
                gate_output witness "$out/native.stdout" 'format-flags-direct-end' 'format-flags-direct-null:True' 'format-flags-receiver-once:True' 'format-flags-receiver-throws:True' --count 1 --policy crlf || return $? ;;
            reflection)
                gate_output witness "$out/native.stdout" 'format-flags-reflection-end' 'format-flags-reflection-null:True' --count 1 --policy crlf || return $?
                gate_output witness "$out/native.stdout" 'format-flags-reflection-zero:True' 'format-flags-reflection-actual-enum:True' 'format-flags-reflection-name:True' --count 2 --policy crlf || return $?
                if grep -Eq 'Program_(Direct|Pointer|Address|DelegateRoute)_m[0-9]+\(' "$out/generated.h"; then
                    echo 'FAIL: FormatFlags reflection-only rooted alternate route' >&2; return 1
                fi ;;
            minimal) gate_output witness "$out/native.stdout" 'format-flags-minimal:True' --count 1 --policy crlf || return $? ;;
            *) echo 'invalid FormatFlags test mode' >&2; return 1 ;;
        esac
    done
}

format_flags_negative_gate() {
    local root="$1" original="$2" mode axis corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in ${DN2CPP_FORMAT_FLAGS_NEGATIVES:-int enum address-int address-enum}; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" --format-flags-negative "$root/$mode/input/FormatFlags.dll" "$corelib" "$mode" || return $?
        for axis in raw diet; do
            local flags=() rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/FormatFlags.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || { echo "FAIL: malformed FormatFlags $mode/$axis admitted" >&2; return 1; }
            case "$mode" in
                arity|address-arity) grep -Fq 'System.Globalization.DateTimeFormatInfo: no method get_FormatFlags/1' "$root/$mode/$axis.log" || return 1 ;;
                *) grep -Fq 'DateTimeFormatInfo FormatFlags requires its exact MemberRef signature' "$root/$mode/$axis.log" || return 1 ;;
            esac
        done
    done
}

format_flags_focused_gate() {
    local root="$1" fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib model
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return $?
    build_proj gates/fixtures/format-flags/FormatFlags.csproj || return $?
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root"
    model=$(mktemp -d "$root/model-XXXXXX") || return $?
    gate_capture_ok "$model/guards" run_bounded dotnet "$fixture" --format-flags-guards "$(dirname "$DN2CPP_CLI_DLL")/Dn2Cpp.Transpiler.dll" "$corelib" "$model/input.dll" || return $?
    local original="gates/fixtures/format-flags/bin/$CONFIG/$TFM"
    format_flags_routes_gate "$root/all" "$original" all || return $?
    format_flags_negative_gate "$root/malformed" "$original" || return $?
}
