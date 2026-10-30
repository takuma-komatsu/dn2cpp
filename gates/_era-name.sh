#!/usr/bin/env bash
# Public invariant era names, exact InvalidEraValue exception payload.
_era_name_routes_gate() {
    local abbreviated="$1"; shift
    local fixture_prefix=era-name output_prefix=era-name
    [[ "$abbreviated" != 1 ]] || { fixture_prefix=abbreviated-era-name; output_prefix=abbreviated-era-name; }
    local assembly=EraName
    local root="$1" original="$2" mode="${3:-all}"
    shift 3
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"
    local corelib input oracle axis out flag="--$fixture_prefix" oracle_flag="--$fixture_prefix-oracle"
    corelib=$(resolve_net10_corelib) || return $?
    if [[ "$mode" != all ]]; then flag="--$fixture_prefix-$mode"; oracle_flag="$flag-oracle"; fi
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
        if [[ "$mode" == all || "$mode" == reflection ]]; then
            if [[ "$abbreviated" == 1 ]]; then flags+=(--link-xml gates/fixtures/era-name/link-abbreviated.xml)
            else flags+=(--link-xml gates/fixtures/era-name/link.xml); fi
        fi
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$input/${assembly}.dll" --auto-ref "${flags[@]}" -o "$out" || return $?
        local check=()
        [[ "$mode" != minimal ]] || check+=(--minimal)
        [[ "$abbreviated" != 1 ]] || check+=(--abbreviated)
        "$(resolve_python)" gates/fixtures/era-name/check-generated.py "$out" "${check[@]}" || return $?
        if [[ "$abbreviated/$mode" == 0/all ]]; then
            "$(resolve_python)" gates/fixtures/era-name/check-generated.py "$out" --abbreviated || return $?
        fi
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" "$assembly" || return $?
            runner=(node "$out/$assembly.js")
        else
            compile_console "$out" "$assembly" || return $?
            runner=("$out/$assembly$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        gate_output compare "$root/oracle.stdout" "$out/native.stdout" --policy crlf || return $?
        if [[ "$mode" == all ]]; then
            if [[ "$abbreviated" == 0 ]]; then
                gate_output witness "$out/native.stdout" '== Invariant era names ==' 'era-name-pointer-range-2:True' 'era-name-delegate-range-2:True' 'era-name-reflect-range-2:True' 'era-name-producer-order:True' 'era-name-producer-preempts-null:True' 'era-name-end:48' --count 1 --policy crlf || return $?
            fi
            gate_output witness "$out/native.stdout" '== Invariant abbreviated era names ==' 'abbreviated-era-name-end:61' 'abbreviated-era-name-callvirt-range-2:True' 'abbreviated-era-name-literal-identity:True' 'abbreviated-era-name-return-abi:True' --count 1 --policy crlf || return $?
        elif [[ "$mode" == reflection ]]; then
            gate_output witness "$out/native.stdout" "$output_prefix-reflect-wrapped-2:True" "$output_prefix-reflection-null-target:True" "$output_prefix-reflection-end" --count 1 --policy crlf || return $?
        elif [[ "$mode" == direct ]]; then
            gate_output witness "$out/native.stdout" "$output_prefix-direct-0:True" "$output_prefix-producer-order:True" "$output_prefix-producer-preempts-null:True" --count 1 --policy crlf || return $?
        elif [[ "$mode" == minimal ]]; then
            gate_output witness "$out/native.stdout" "$output_prefix-minimal:True" --count 1 --policy crlf || return $?
        else echo 'invalid era-name mode' >&2; return 1
        fi
        if [[ "$mode" == reflection ]]; then
            if grep -Eq 'Program_(Direct|Virtual|Pointer|Address|DelegateRoute|Delegate)(Abbreviated)?_m[0-9]+\(' "$out/generated.h"; then
                echo 'FAIL: reflection-only era-name fixture rooted another route' >&2
                return 1
            fi
        fi
    done
}

era_name_routes_gate() { _era_name_routes_gate 0 "$@"; }
abbreviated_era_name_routes_gate() { _era_name_routes_gate 1 "$@"; }

_era_name_noalloc_gate() {
    local abbreviated="$1"; shift
    local fixture_prefix=era-name method=Direct
    [[ "$abbreviated" != 1 ]] || { fixture_prefix=abbreviated-era-name; method=DirectAbbreviated; }
    local assembly=EraName
    local root="$1" original="$2"
    shift 2
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib axis
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root/input"
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" "--$fixture_prefix-noalloc" "$root/input/${assembly}.dll" "$corelib" || return $?
    for axis in raw diet; do
        local flags=("$@") rc=0
        [[ "$axis" != raw ]] || flags+=(--no-ildiet)
        invoke_cli "$root/input/${assembly}.dll" --auto-ref "${flags[@]}" -o "$root/$axis" > "$root/$axis.log" 2>&1 || rc=$?
        printf '%s\n' "$rc" > "$root/$axis.status"
        [[ "$rc" == 2 ]] || { echo 'FAIL: NoAlloc leaf with Int32 exception boxing accepted' >&2; return 1; }
        grep -Fq "[HotPath(NoAlloc)] closure of ${assembly}Subset.Program::$method allocates: ${assembly}Subset.Program::$method emits dn2cpp_box(" "$root/$axis.log" || return 1
    done
}

era_name_noalloc_gate() { _era_name_noalloc_gate 0 "$@"; }
abbreviated_era_name_noalloc_gate() { _era_name_noalloc_gate 1 "$@"; }

_era_name_negative_gate() {
    local abbreviated="$1"; shift
    local fixture_prefix=era-name target=GetEraName modes="${DN2CPP_ERA_NAME_NEGATIVES:-return byref address-return address-byref}"
    [[ "$abbreviated" != 1 ]] || { fixture_prefix=abbreviated-era-name; target=GetAbbreviatedEraName; modes="${DN2CPP_ABBREVIATED_ERA_NAME_NEGATIVES:-return argument enum byref static modifier generic arity-two address-return address-byref}"; }
    local assembly=EraName
    local root="$1" original="$2" mode axis rc
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib
    corelib=$(resolve_net10_corelib) || return $?
    for mode in $modes; do
        mkdir -p "$root/$mode/input"
        cp -R "$original/." "$root/$mode/input/" || return $?
        dotnet "$fixture" "--$fixture_prefix-negative" "$root/$mode/input/${assembly}.dll" "$corelib" "$mode" || return $?
        for axis in raw diet; do
            local flags=(); rc=0
            [[ "$axis" != raw ]] || flags+=(--no-ildiet)
            invoke_cli "$root/$mode/input/${assembly}.dll" --auto-ref "${flags[@]}" -o "$root/$mode/$axis" > "$root/$mode/$axis.log" 2>&1 || rc=$?
            printf '%s\n' "$rc" > "$root/$mode/$axis.status"
            [[ "$rc" == 2 ]] || { echo "FAIL: malformed era-name $mode $axis admitted" >&2; return 1; }
            local diagnostic='DateTimeFormatInfo era name requires its exact MemberRef signature'
            case "$mode" in
                arity-zero|address-arity-zero) diagnostic="System.Globalization.DateTimeFormatInfo: no method $target/0" ;;
                arity-two|address-arity-two) diagnostic="System.Globalization.DateTimeFormatInfo: no method $target/2" ;;
            esac
            grep -Fq "$diagnostic" "$root/$mode/$axis.log" || return 1
        done
    done
}

era_name_negative_gate() { _era_name_negative_gate 0 "$@"; }
abbreviated_era_name_negative_gate() { _era_name_negative_gate 1 "$@"; }

era_name_focused_gate() {
    local root="$1" fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib model
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return $?
    build_proj gates/fixtures/era-name/EraName.csproj || return $?
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root"
    model=$(mktemp -d "$root/model-XXXXXX") || return $?
    gate_capture_ok "$model/guards" run_bounded dotnet "$fixture" --era-name-guards "$(dirname "$DN2CPP_CLI_DLL")/Dn2Cpp.Transpiler.dll" "$corelib" "$model/input.dll" || return $?
    local app="gates/fixtures/era-name/bin/$CONFIG/$TFM"
    era_name_routes_gate "$root/all" "$app" all || return $?
    era_name_noalloc_gate "$root/noalloc" "$app" || return $?
    era_name_negative_gate "$root/malformed" "$app" || return $?
    abbreviated_era_name_noalloc_gate "$root/abbreviated-noalloc" "$app" || return $?
    abbreviated_era_name_negative_gate "$root/abbreviated-malformed" "$app" || return $?
}
