#!/usr/bin/env bash
# Exact invariant Char overload; zero-argument and mutable-data policies stay separate.
all_datetime_patterns_routes_gate() {
    local root="$1" original="$2" mode="${3:-all}"; shift 3
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib flag=--all-datetime-patterns
    corelib=$(resolve_net10_corelib) || return $?
    [[ "$mode" == all ]] || flag="--all-datetime-patterns-$mode"
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" "$flag" "$root/input/AllDateTimePatterns.dll" "$corelib" || return $?
    local axis out
    for axis in raw diet; do
        out="$root/$axis"
        local flags=("$@") runner=() checker=()
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        [[ "$mode" != all && "$mode" != reflection ]] || flags+=(--link-xml gates/fixtures/all-datetime-patterns/link.xml)
        [[ "$mode" != minimal ]] || checker+=(--minimal)
        invoke_cli "$root/input/AllDateTimePatterns.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        "$(resolve_python)" gates/fixtures/all-datetime-patterns/check-generated.py "$out" "${checker[@]}" || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" AllDateTimePatterns || return $?
            runner=(node "$out/AllDateTimePatterns.js")
        else
            compile_console "$out" AllDateTimePatterns || return $?
            runner=("$out/AllDateTimePatterns$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        local routes=() route format witness
        case "$mode" in
            all) routes=(direct callvirt calli delegate reflection) ;;
            direct) routes=(direct) ;;
            reflection) routes=(reflection) ;;
            minimal)
                gate_output witness "$out/native.stdout" 'all-datetime-patterns-minimal:True' --count 1 --policy crlf || return $? ;;
            *) echo 'invalid all-patterns test mode' >&2; return 1 ;;
        esac
        for route in "${routes[@]}"; do
            local witnesses=()
            for format in d D f F g G m M o O r R s t T u U y Y; do
                for witness in ordered array isolated gc; do
                    witnesses+=("all-datetime-patterns-$route-$witness-$format:True")
                done
            done
            for format in 120 97 122 0 65535 55296 57343; do
                witnesses+=("all-datetime-patterns-$route-invalid-$format:True")
            done
            gate_output witness "$out/native.stdout" "${witnesses[@]}" --count 1 --policy crlf || return $?
        done
        if [[ "$mode" == all || "$mode" == direct ]]; then
            gate_output witness "$out/native.stdout" 'all-datetime-patterns-producer-constant:True' \
                'all-datetime-patterns-producer-throws:True' 'all-datetime-patterns-direct-end' --count 1 --policy crlf || return $?
            local rawroutes=(direct)
            [[ "$mode" != all ]] || rawroutes+=(calli)
            for route in "${rawroutes[@]}"; do
                for format in o O r R s u; do
                    gate_output witness "$out/native.stdout" "all-datetime-patterns-$route-null-constant-$format:True" --count 1 --policy crlf || return $?
                done
                for format in d D f F g G m M t T U y Y; do
                    gate_output witness "$out/native.stdout" "all-datetime-patterns-$route-null-dependent-$format:True" --count 1 --policy crlf || return $?
                done
                for format in 120 0 65535 55296; do
                    gate_output witness "$out/native.stdout" "all-datetime-patterns-$route-null-invalid-$format:True" --count 1 --policy crlf || return $?
                done
            done
        fi
        if [[ "$mode" == all || "$mode" == reflection ]]; then
            gate_output witness "$out/native.stdout" 'all-datetime-patterns-reflection-wrapped:True' --count 7 --policy crlf || return $?
            gate_output witness "$out/native.stdout" 'all-datetime-patterns-reflection-null-d:True' \
                'all-datetime-patterns-reflection-null-s:True' 'all-datetime-patterns-reflection-null-x:True' \
                'all-datetime-patterns-reflection-abi:True' 'all-datetime-patterns-reflection-end' --count 1 --policy crlf || return $?
        fi
        if [[ "$mode" == all ]]; then
            gate_output witness "$out/native.stdout" 'all-datetime-patterns-callvirt-null-d:True' \
                'all-datetime-patterns-callvirt-null-s:True' 'all-datetime-patterns-callvirt-null-x:True' \
                'all-datetime-patterns-delegate-abi:True' 'Invariant all DateTime patterns end' --count 1 --policy crlf || return $?
        fi
        if [[ "$mode" == reflection ]] && grep -Eq 'Program_(Direct|Virtual|Pointer|Address|DelegateRoute|ProducerRoute)_m[0-9]+\(' "$out/generated.h"; then
            echo 'FAIL: patterns reflection-only rooted another route' >&2; return 1
        fi
    done
}

all_datetime_patterns_negative_gate() {
    local root="$1" original="$2" mode axis corelib
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    corelib=$(resolve_net10_corelib) || return $?
    for mode in ${DN2CPP_ALL_DATETIME_PATTERNS_NEGATIVES:-argument return-element address-argument address-return-element}; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" --all-datetime-patterns-negative "$root/$mode/input/AllDateTimePatterns.dll" "$corelib" "$mode" || return $?
        for axis in raw diet; do
            local flags=() rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/AllDateTimePatterns.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || { echo "FAIL: malformed patterns $mode/$axis admitted" >&2; return 1; }
            local diagnostic='DateTimeFormatInfo GetAllDateTimePatterns(Char) requires its exact MemberRef signature'
            case "$mode" in
                arity|address-arity) diagnostic='System.Globalization.DateTimeFormatInfo: no method GetAllDateTimePatterns/2' ;;
            esac
            grep -Fq "$diagnostic" "$root/$mode/$axis.log" || return 1
        done
    done
}

all_datetime_patterns_noalloc_gate() {
    local root="$1" original="$2"; shift 2
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib axis
    local allocation_lines
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" --all-datetime-patterns-noalloc "$root/input/AllDateTimePatterns.dll" "$corelib" || return $?
    for axis in raw diet; do
        local flags=("$@") rc=0
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$root/input/AllDateTimePatterns.dll" --auto-ref "${flags[@]}" -o "$root/$axis-output" > "$root/$axis.log" 2>&1 || rc=$?
        printf '%s\n' "$rc" > "$root/$axis.status"
        [[ "$rc" == 2 ]] || return 1
        allocation_lines=$(grep -F '[HotPath(NoAlloc)] closure of AllDateTimePatternsSubset.Program::Direct allocates:' "$root/$axis.log") || return 1
        grep -Fq 'emits dn2cpp_newarr_' <<<"$allocation_lines" || return 1
    done
}

all_datetime_patterns_zeroarg_gate() {
    local root="$1" original="$2" fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib axis
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" --all-datetime-patterns-zeroarg "$root/input/AllDateTimePatterns.dll" "$corelib" || return $?
    for axis in raw diet; do
        local flags=() rc=0
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$root/input/AllDateTimePatterns.dll" --auto-ref "${flags[@]}" -o "$root/$axis-output" > "$root/$axis.log" 2>&1 || rc=$?
        printf '%s\n' "$rc" > "$root/$axis.status"
        [[ "$rc" == 2 ]] || return 1
        grep -Fq 'call to System.Globalization.DateTimeFormatInfo::GetAllDateTimePatterns() has no intrinsic mapping yet' "$root/$axis.log" || return 1
    done
}

all_datetime_patterns_focused_gate() {
    local root="$1" fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib model
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return $?
    build_proj gates/fixtures/all-datetime-patterns/AllDateTimePatterns.csproj || return $?
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root"
    model=$(mktemp -d "$root/model-XXXXXX") || return $?
    gate_capture_ok "$model/guards" run_bounded dotnet "$fixture" --all-datetime-patterns-guards "$(dirname "$DN2CPP_CLI_DLL")/Dn2Cpp.Transpiler.dll" "$corelib" "$model/input.dll" || return $?
    local original="gates/fixtures/all-datetime-patterns/bin/$CONFIG/$TFM"
    all_datetime_patterns_routes_gate "$root/all" "$original" all || return $?
    all_datetime_patterns_noalloc_gate "$root/noalloc" "$original" || return $?
    all_datetime_patterns_negative_gate "$root/malformed" "$original" || return $?
    all_datetime_patterns_zeroarg_gate "$root/zeroarg" "$original" || return $?
}
