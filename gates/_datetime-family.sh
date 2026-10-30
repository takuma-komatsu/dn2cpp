#!/usr/bin/env bash
# Reuse leaf assertions and compiler-only controls; compile the aggregate twice.
_datetime_family_check_generated() {
    local out="$1" python
    python=$(resolve_python) || return $?
    "$python" gates/fixtures/abbreviated-day/check-generated.py "$out" --name abbreviated || return $?
    "$python" gates/fixtures/abbreviated-day/check-generated.py "$out" --name full || return $?
    "$python" gates/fixtures/abbreviated-month/check-generated.py "$out" --name abbreviated || return $?
    "$python" gates/fixtures/abbreviated-month/check-generated.py "$out" --name full || return $?
    local family
    for family in dtfi-calendar dtfi-calendar-setter internal-month era-name format-flags force-two-digit-years all-datetime-patterns dateonly-deconstruct; do
        "$python" "gates/fixtures/$family/check-generated.py" "$out" || return $?
    done
    "$python" gates/fixtures/general-short-time-pattern/check-generated.py "$out" || return $?
    "$python" gates/fixtures/general-short-time-pattern/check-generated.py "$out" --long || return $?
    "$python" gates/fixtures/general-short-time-pattern/check-generated.py "$out" --offset || return $?
}

datetime_family_aggregate_gate() {
    local root="$1"; shift
    local original="gates/fixtures/datetime-family/bin/$CONFIG/$TFM"
    local fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib
    local catalog=gates/fixtures/datetime-family/families.tsv
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root/input"
    shasum -a 256 "$original"/*.dll > "$root/original-before.sha256" || return $?
    cp -R "$original/." "$root/input/" || return $?
    dotnet "$fixture" --datetime-family-aggregate "$root/input/DateTimeFamily.dll" "$corelib" || return $?
    shasum -a 256 "$original"/*.dll > "$root/original-after.sha256" || return $?
    cmp "$root/original-before.sha256" "$root/original-after.sha256" || return $?
    local folder assembly namespace flag reference
    local flags=("$@")
    while IFS=$'\t' read -r folder assembly namespace flag; do
        [[ -n "$folder" && "$folder" != \#* ]] || continue
        [[ "$folder" != datetime-tchar ]] || continue
        flags+=(--link-xml "gates/fixtures/$folder/link.xml")
    done < "$catalog"
    while IFS= read -r reference; do flags+=(-r "$reference"); done < "$root/input/family-refs.txt"

    # These existing public target fixtures have exact CLR/native output parity.
    for folder in abbreviated-day full-day abbreviated-month full-month era-name dateonly-deconstruct; do
        case "$folder" in
            abbreviated-day) assembly=AbbreviatedDay ;; full-day) assembly=FullDay ;;
            abbreviated-month) assembly=AbbreviatedMonth ;; full-month) assembly=FullMonth ;;
            era-name) assembly=EraName ;;
            dateonly-deconstruct) assembly=DateOnlyDeconstruct ;;
        esac
        mkdir -p "$root/oracles/$assembly/input"
        cp -R "gates/fixtures/$folder/bin/$CONFIG/$TFM/." "$root/oracles/$assembly/input/" || return $?
        dotnet "$fixture" "--$folder-oracle" "$root/oracles/$assembly/input/$assembly.dll" "$corelib" || return $?
        gate_capture_ok "$root/oracles/$assembly/oracle" run_bounded dotnet "$root/oracles/$assembly/input/$assembly.dll" || return $?
    done
    local axis out
    for axis in raw diet; do
        out="$root/$axis"
        local axis_flags=("${flags[@]}") runner=()
        [[ "$axis" != raw ]] || axis_flags+=(--no-ildiet)
        invoke_cli "$root/input/DateTimeFamily.dll" --auto-ref "${axis_flags[@]}" -o "$out" || return $?
        _datetime_family_check_generated "$out" || return $?
        if [[ "${WASM:-0}" == 1 ]]; then
            compile_console_wasm "$out" DateTimeFamily || return $?
            runner=(node "$out/DateTimeFamily.js")
        else
            compile_console "$out" DateTimeFamily || return $?
            runner=("$out/DateTimeFamily$EXE_EXT")
        fi
        gate_capture_ok "$out/native" run_bounded "${runner[@]}" || return $?
        "$(resolve_python)" gates/fixtures/datetime-family/check-output.py "$out/native.stdout" "$out/sections" || return $?
        for assembly in AbbreviatedDay FullDay AbbreviatedMonth FullMonth EraName DateOnlyDeconstruct; do
            gate_output compare "$root/oracles/$assembly/oracle.stdout" "$out/sections/$assembly.stdout" --policy crlf || return $?
        done
    done
}

datetime_family_controls_gate() {
    local root="$1" fixture="gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" corelib flag model
    corelib=$(resolve_net10_corelib) || return $?
    mkdir -p "$root"
    # Shared day/month/three-pattern guards already enumerate all exact target names.
    for flag in abbreviated-day abbreviated-month dtfi-calendar dtfi-calendar-setter internal-month era-name format-flags force-two-digit-years general-short-time-pattern all-datetime-patterns dateonly-deconstruct datetime-tchar; do
        model=$(mktemp -d "$root/model-$flag-XXXXXX") || return $?
        gate_capture_ok "$model/guards" run_bounded dotnet "$fixture" "--$flag-guards" "$(dirname "$DN2CPP_CLI_DLL")/Dn2Cpp.Transpiler.dll" "$corelib" "$model/input.dll" || return $?
    done
    local folder assembly namespace flag stem original
    while IFS=$'\t' read -r folder assembly namespace flag; do
        [[ -n "$folder" && "$folder" != \#* ]] || continue
        stem=${folder//-/_}
        original="gates/fixtures/$folder/bin/$CONFIG/$TFM"
        "${stem}_negative_gate" "$root/malformed/$folder" "$original" || return $?
        case "$folder" in
            abbreviated-day|full-day|abbreviated-month|full-month|dtfi-calendar|internal-month|era-name|all-datetime-patterns)
                "${stem}_noalloc_gate" "$root/noalloc/$folder" "$original" || return $? ;;
        esac
    done < gates/fixtures/datetime-family/families.tsv
    datetime_tchar_closure_gate "$root/tchar-closure" "gates/fixtures/datetime-tchar/bin/$CONFIG/$TFM" || return $?
    all_datetime_patterns_zeroarg_gate "$root/zeroarg" "gates/fixtures/all-datetime-patterns/bin/$CONFIG/$TFM" || return $?
}

datetime_family_focused_gate() {
    local root="$1"
    build_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return $?
    # One build command/DAG, not a claim that sixteen managed projects disappeared.
    build_proj gates/fixtures/datetime-family/DateTimeFamily.csproj || return $?
    build_proj gates/fixtures/abbreviated-day/minimal/MinimalDay.csproj || return $?
    datetime_family_aggregate_gate "$root/aggregate" || return $?
    datetime_family_controls_gate "$root/controls" || return $?
    # Preserve the four previously persistent isolated native builds.
    abbreviated_day_routes_gate "$root/day-reflection" "gates/fixtures/abbreviated-day/bin/$CONFIG/$TFM" reflection || return $?
    abbreviated_day_minimal_gate "$root/day-minimal" "gates/fixtures/abbreviated-day/minimal/bin/$CONFIG/$TFM" || return $?
}
