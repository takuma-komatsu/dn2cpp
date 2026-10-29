#!/usr/bin/env bash
# Invariant scalar month names and exact Int32 exception payload.
_month_name_routes_gate() {
    local family="$1"; shift
    local assembly=AbbreviatedMonth minimal=MinimalMonth expected=Jan
    case "$family" in
        abbreviated) ;;
        full) assembly=FullMonth; minimal=MinimalFullMonth; expected=January ;;
        *) echo "invalid month-name test family" >&2; return 1 ;;
    esac
    local root="$1" original="$2" mode="${3:-all}"
    shift 3
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local corelib input oracle axis out flag="--$family-month" oracle_flag="--$family-month-oracle"
    corelib=$(resolve_net10_corelib) || return $?
    if [[ "$mode" != all ]]; then flag="--$family-month-$mode"; oracle_flag="$flag-oracle"; fi
    input="$root/input"; oracle="$root/oracle-input"
    mkdir -p "$input" "$oracle"
    cp -R "$original/." "$input/" || return $?
    cp -R "$original/." "$oracle/" || return $?
    dotnet "$fixture" "$flag" "$input/${assembly}.dll" "$corelib" || return $?
    dotnet "$fixture" "$oracle_flag" "$oracle/${assembly}.dll" "$corelib" || return $?
    gate_capture_ok "$root/oracle" run_bounded dotnet "$oracle/${assembly}.dll" || return $?
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@") runner=()
        # The Metadata helper is opaque to reflection dataflow; retain only this method.
        flags+=(--link-xml "gates/fixtures/$family-month/link.xml")
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$input/${assembly}.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/abbreviated-month/check-generated.py "$out" --name "$family"  || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" "$assembly" || return $?
            runner=(node "$out/$assembly.js")
        else
            compile_console "$out" "$assembly" || return $?
            runner=("$out/$assembly$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        gate_output compare "$root/oracle.stdout" "$out/native.stdout" --policy crlf || return $?
        if [[ "$mode" == reflection ]]; then
            if grep -Eq 'Program_(Direct|Pointer|Address|DelegateRoute)_m[0-9]+\(' "$out/generated.h"; then
                echo 'FAIL: reflection-only abbreviated-month fixture rooted another route' >&2
                return 1
            fi
        fi
    done
}

abbreviated_month_routes_gate() { _month_name_routes_gate abbreviated "$@"; }
full_month_routes_gate() { _month_name_routes_gate full "$@"; }

_month_name_noalloc_gate() {
    local family="$1"; shift
    local assembly=AbbreviatedMonth minimal=MinimalMonth expected=Jan
    case "$family" in
        abbreviated) ;;
        full) assembly=FullMonth; minimal=MinimalFullMonth; expected=January ;;
        *) echo "invalid month-name test family" >&2; return 1 ;;
    esac
    local root="$1" original="$2"
    shift 2
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib axis
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" "--$family-month-noalloc" "$root/input/${assembly}.dll" "$corelib" || return $?
    for axis in raw diet; do
        local flags=("$@") rc=0
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$root/input/${assembly}.dll" --auto-ref "${flags[@]}" -o "$root/$axis" > "$root/$axis.log" 2>&1 || rc=$?
        printf '%s\n' "$rc" > "$root/$axis.status"
        [[ "$rc" == 2 ]] || { echo 'FAIL: NoAlloc leaf with Int32 exception boxing accepted' >&2; return 1; }
        grep -Fq "[HotPath(NoAlloc)] closure of ${assembly}Subset.Program::Direct allocates: ${assembly}Subset.Program::Direct emits dn2cpp_box(" "$root/$axis.log" || return 1
    done
}

abbreviated_month_noalloc_gate() { _month_name_noalloc_gate abbreviated "$@"; }
full_month_noalloc_gate() { _month_name_noalloc_gate full "$@"; }

_month_name_minimal_gate() {
    local family="$1"; shift
    local assembly=AbbreviatedMonth minimal=MinimalMonth expected=Jan
    case "$family" in
        abbreviated) ;;
        full) assembly=FullMonth; minimal=MinimalFullMonth; expected=January ;;
        *) echo "invalid month-name test family" >&2; return 1 ;;
    esac
    local root="$1" original="$2"
    shift 2
    mkdir -p "$root"
    gate_capture_ok "$root/oracle" run_bounded dotnet "$original/${minimal}.dll" || return $?
    gate_output witness "$root/oracle.stdout" "$expected" --count 1 --policy crlf || return $?
    local axis
    for axis in raw diet; do
        local flags=("$@") out="$root/$axis" runner=()
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$original/${minimal}.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/abbreviated-month/check-generated.py "$out" --name "$family"  --minimal || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" "$minimal" || return $?
            runner=(node "$out/$minimal.js")
        else
            compile_console "$out" "$minimal" || return $?
            runner=("$out/$minimal$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        gate_output compare "$root/oracle.stdout" "$out/native.stdout" --policy crlf || return $?
    done
}

abbreviated_month_minimal_gate() { _month_name_minimal_gate abbreviated "$@"; }
full_month_minimal_gate() { _month_name_minimal_gate full "$@"; }

_month_name_negative_gate() {
    local family="$1"; shift
    local assembly=AbbreviatedMonth minimal=MinimalMonth expected=Jan
    case "$family" in
        abbreviated) ;;
        full) assembly=FullMonth; minimal=MinimalFullMonth; expected=January ;;
        *) echo "invalid month-name test family" >&2; return 1 ;;
    esac
    local root="$1" original="$2" mode axis rc
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib
    corelib=$(resolve_net10_corelib) || return $?
    for mode in ${DN2CPP_ABBREVIATED_MONTH_NEGATIVES:-return byref address-return address-byref}; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" "--$family-month-negative" "$root/$mode/input/${assembly}.dll" "$corelib" "$mode" || return $?
        for axis in raw diet; do
            local flags=(); rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/${assembly}.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || { echo "FAIL: malformed abbreviated-month $mode $axis admitted" >&2; return 1; }
            grep -Fq 'DateTimeFormatInfo month name requires its exact MemberRef signature' "$root/$mode/$axis.log" || return 1
        done
    done
}

abbreviated_month_negative_gate() { _month_name_negative_gate abbreviated "$@"; }
full_month_negative_gate() { _month_name_negative_gate full "$@"; }

abbreviated_month_focused_gate() {
    local root="$1" fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib model
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return $?
    build_proj gates/fixtures/abbreviated-month/AbbreviatedMonth.csproj || return $?
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root"
    model=$(mktemp -d "$root/model-XXXXXX") || return $?
    gate_capture_ok "$model/guards" run_bounded dotnet "$fixture" --abbreviated-month-guards "$(dirname "$DN2CPP_CLI_DLL")/Dn2Cpp.Transpiler.dll" "$corelib" "$model/input.dll" || return $?
    local app="gates/fixtures/abbreviated-month/bin/$CONFIG/$TFM"
    abbreviated_month_routes_gate "$root/all" "$app" all || return $?
    abbreviated_month_noalloc_gate "$root/noalloc" "$app" || return $?
    abbreviated_month_negative_gate "$root/malformed" "$app" || return $?
}

full_month_focused_gate() {
    local root="$1" fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib model
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return $?
    build_proj gates/fixtures/full-month/FullMonth.csproj || return $?
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root"
    model=$(mktemp -d "$root/model-XXXXXX") || return $?
    gate_capture_ok "$model/guards" run_bounded dotnet "$fixture" --abbreviated-month-guards "$(dirname "$DN2CPP_CLI_DLL")/Dn2Cpp.Transpiler.dll" "$corelib" "$model/input.dll" || return $?
    local app="gates/fixtures/full-month/bin/$CONFIG/$TFM"
    full_month_routes_gate "$root/all" "$app" all || return $?
    full_month_noalloc_gate "$root/noalloc" "$app" || return $?
    full_month_negative_gate "$root/malformed" "$app" || return $?
}
