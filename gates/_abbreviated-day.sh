#!/usr/bin/env bash
# Exact public invariant DTFI scalar day names, exception payload and callable routes.
abbreviated_day_routes_gate() {
    local root="$1" original="$2" mode="${3:-all}"
    shift 3
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local corelib input oracle axis out flag=--abbreviated-day oracle_flag=--abbreviated-day-oracle
    corelib=$(resolve_net10_corelib) || return $?
    if [[ "$mode" != all ]]; then flag="--abbreviated-day-$mode"; oracle_flag="$flag-oracle"; fi
    input="$root/input"; oracle="$root/oracle-input"
    mkdir -p "$input" "$oracle"
    cp -R "$original/." "$input/" || return $?
    cp -R "$original/." "$oracle/" || return $?
    dotnet "$fixture" "$flag" "$input/AbbreviatedDay.dll" "$corelib" || return $?
    dotnet "$fixture" "$oracle_flag" "$oracle/AbbreviatedDay.dll" "$corelib" || return $?
    gate_capture_ok "$root/oracle" run_bounded dotnet "$oracle/AbbreviatedDay.dll" || return $?
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@") runner=()
        # The Metadata helper is opaque to reflection dataflow; retain only this method.
        flags+=(--link-xml gates/fixtures/abbreviated-day/link.xml)
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$input/AbbreviatedDay.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/abbreviated-day/check-generated.py "$out" || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" AbbreviatedDay || return $?
            runner=(node "$out/AbbreviatedDay.js")
        else
            compile_console "$out" AbbreviatedDay || return $?
            runner=("$out/AbbreviatedDay$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        gate_output compare "$root/oracle.stdout" "$out/native.stdout" --policy crlf || return $?
        if [[ "$mode" == reflection ]]; then
            if grep -Eq 'Program_(Direct|Pointer|Address|DelegateRoute)_m[0-9]+\(' "$out/generated.h"; then
                echo 'FAIL: reflection-only abbreviated-day fixture rooted another route' >&2
                return 1
            fi
        fi
    done
}

abbreviated_day_noalloc_gate() {
    local root="$1" original="$2"
    shift 2
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib axis
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" --abbreviated-day-noalloc "$root/input/AbbreviatedDay.dll" "$corelib" || return $?
    for axis in raw diet; do
        local flags=("$@") rc=0
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$root/input/AbbreviatedDay.dll" --auto-ref "${flags[@]}" -o "$root/$axis" > "$root/$axis.log" 2>&1 || rc=$?
        printf '%s\n' "$rc" > "$root/$axis.status"
        [[ "$rc" == 2 ]] || { echo 'FAIL: NoAlloc leaf with enum exception boxing accepted' >&2; return 1; }
        grep -Fq '[HotPath(NoAlloc)] closure of AbbreviatedDaySubset.Program::Direct allocates: AbbreviatedDaySubset.Program::Direct emits dn2cpp_box(' "$root/$axis.log" || return 1
    done
}

abbreviated_day_minimal_gate() {
    local root="$1" original="$2"
    shift 2
    mkdir -p "$root"
    gate_capture_ok "$root/oracle" run_bounded dotnet "$original/MinimalDay.dll" || return $?
    gate_output witness "$root/oracle.stdout" 'Mon' --count 1 --policy crlf || return $?
    local axis
    for axis in raw diet; do
        local flags=("$@") out="$root/$axis" runner=()
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$original/MinimalDay.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/abbreviated-day/check-generated.py "$out" --minimal || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" MinimalDay || return $?
            runner=(node "$out/MinimalDay.js")
        else
            compile_console "$out" MinimalDay || return $?
            runner=("$out/MinimalDay$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        gate_output compare "$root/oracle.stdout" "$out/native.stdout" --policy crlf || return $?
    done
}

abbreviated_day_focused_gate() {
    local root="$1" fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib model
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return $?
    build_proj gates/fixtures/abbreviated-day/AbbreviatedDay.csproj || return $?
    build_proj gates/fixtures/abbreviated-day/minimal/MinimalDay.csproj || return $?
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root"
    model=$(mktemp -d "$root/model-XXXXXX") || return $?
    gate_capture_ok "$model/guards" run_bounded dotnet "$fixture" --abbreviated-day-guards "$(dirname "$DN2CPP_CLI_DLL")/Dn2Cpp.Transpiler.dll" "$corelib" "$model/input.dll" || return $?
    local app="gates/fixtures/abbreviated-day/bin/$CONFIG/$TFM"
    abbreviated_day_routes_gate "$root/all" "$app" all || return $?
    abbreviated_day_routes_gate "$root/reflection" "$app" reflection || return $?
    abbreviated_day_minimal_gate "$root/minimal" "gates/fixtures/abbreviated-day/minimal/bin/$CONFIG/$TFM" || return $?
    abbreviated_day_noalloc_gate "$root/noalloc" "$app" || return $?
    abbreviated_day_negative_gate "$root/malformed" "$app" || return $?
}

# Malformed metadata is transpiled only, never executed on either runtime.
abbreviated_day_negative_gate() {
    local root="$1" original="$2" mode axis rc
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib
    corelib=$(resolve_net10_corelib) || return $?
    for mode in ${DN2CPP_ABBREVIATED_DAY_NEGATIVES:-return byref address-return address-byref}; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" --abbreviated-day-negative "$root/$mode/input/AbbreviatedDay.dll" "$corelib" "$mode" || return $?
        for axis in raw diet; do
            local flags=(); rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/AbbreviatedDay.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || { echo "FAIL: malformed abbreviated-day $mode $axis admitted" >&2; return 1; }
            grep -Fq 'DateTimeFormatInfo abbreviated day name requires its exact MemberRef signature' "$root/$mode/$axis.log" || return 1
        done
    done
}
