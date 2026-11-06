#!/usr/bin/env bash
# Real scalar bit-accessor IL, with raw storage and private-call CLR authority.
probe_scalar_numeric_leaves() {
    local root="$1" out axis emit fixture compiler diet
    fixture="${DN2CPP_SCALAR_LEAF_FIXTURE_DLL:-gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll}"
    compiler="$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll"
    diet="$(dirname "$compiler")/ildiet/ILDiet.dll"
    if [ -z "${DN2CPP_SCALAR_LEAF_FIXTURE_DLL:-}" ]; then
        build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return 1
    fi
    mkdir -p "$root" || return 1
    out=$(mktemp -d "$root/leaves-XXXXXX") || return 1
    gate_capture_ok "$out/guards" run_bounded dotnet "$fixture" --scalar-numeric-leaf-guards \
        "$compiler" "$_CG_CORELIB" "$out/model.dll" "$diet" || return 1
    grep -E '^scalar-leaf-authority-checks:[1-9][0-9]*$' "$out/guards.stdout" >/dev/null || return 1
    gate_capture_ok "$out/generate" run_bounded dotnet "$fixture" --scalar-numeric-leaf-fixture "$out/routes.dll" "$_CG_CORELIB" || return 1
    # DynamicMethods skip visibility checks but execute the installed CoreLib body.
    gate_capture_ok "$out/oracle" run_bounded dotnet "$fixture" --scalar-numeric-leaf-oracle || return 1
    [ ! -s "$out/oracle.stderr" ] || return 1
    for axis in raw diet; do
        emit=$(mktemp -d "$out/$axis-XXXXXX") || return 1
        local flags=() runner=()
        [ "$axis" != raw ] || flags+=(--no-ildiet --no-shared-generics)
        gate_capture_ok "$out/$axis-emit" invoke_cli "$out/routes.dll" -r "$_CG_CORELIB" --auto-ref ${flags[@]+"${flags[@]}"} -o "$emit" || return 1
        (
            if [ "$axis" = raw ]; then
                export DN2CPP_EXTRA_CMAKE_ARGS="${DN2CPP_EXTRA_CMAKE_ARGS:-} -DCMAKE_PROJECT_INCLUDE=$PWD/gates/fixtures/scalar-numeric-leaves/unoptimized.cmake"
            fi
            if [ "${WASM:-0}" = 1 ]; then compile_console_wasm "$emit" ScalarNumericLeaves
            else compile_console "$emit" ScalarNumericLeaves; fi
        ) || return 1
        if [ "${WASM:-0}" = 1 ]; then runner=(node "$emit/ScalarNumericLeaves.js")
        else runner=("$emit/ScalarNumericLeaves$EXE_EXT"); fi
        gate_capture_ok "$out/$axis-run" run_bounded "${runner[@]}" || return 1
        gate_output compare "$out/$axis-run.stdout" "$out/oracle.stdout" --policy crlf || return 1
        gate_output compare "$out/$axis-run.stderr" "$out/oracle.stderr" --policy crlf || return 1
    done
}
