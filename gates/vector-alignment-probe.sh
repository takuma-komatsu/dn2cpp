#!/usr/bin/env bash
# Alignment is target byte width, independently of T's Count support.
probe_vector_alignment() {
    local root="$1" out axis emit
    local fixture="${DN2CPP_VECTOR_ALIGNMENT_FIXTURE_DLL:-gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll}"
    local compiler="${DN2CPP_VECTOR_ALIGNMENT_COMPILER_DLL:-$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll}"
    if [ -z "${DN2CPP_VECTOR_ALIGNMENT_FIXTURE_DLL:-}" ]; then
        build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return 1
    fi
    mkdir -p "$root" || return 1
    out=$(mktemp -d "$root/alignment-XXXXXX") || return 1
    gate_capture_ok "$out/guards" run_bounded dotnet "$fixture" --vector-alignment-guards "$compiler" "$_CG_CORELIB" "$out/model.dll" || return 1
    grep -E '^vector-alignment-guards:checks=[1-9][0-9]*$' "$out/guards.stdout" >/dev/null || return 1
    gate_capture_ok "$out/generate" run_bounded dotnet "$fixture" --vector-alignment-fixture "$out/runtime.dll" "$_CG_CORELIB" || return 1
    # The oracle skips CLR visibility checks for internal/private properties. Both
    # profiles assert semantic relations rather than comparing host SIMD widths.
    gate_capture_ok "$out/oracle" run_bounded env -u DOTNET_EnableHWIntrinsic -u COMPlus_EnableHWIntrinsic dotnet "$fixture" --vector-alignment-oracle || return 1
    gate_capture_ok "$out/oracle-nohw" run_bounded env -u COMPlus_EnableHWIntrinsic DOTNET_EnableHWIntrinsic=0 dotnet "$fixture" --vector-alignment-oracle || return 1
    gate_output compare "$out/oracle.stdout" "$out/oracle-nohw.stdout" --policy crlf || return 1
    [ ! -s "$out/oracle.stderr" ] && [ ! -s "$out/oracle-nohw.stderr" ] || return 1
    [ "$(wc -l < "$out/oracle.stdout")" -eq 93 ] || return 1
    awk '/^vector-(alignment:helper:(direct|address|calli)|property:(0|64|128|256|512):(int|decimal):(Alignment|ElementCount|IsHardwareAccelerated):(direct|address|calli)):pass$/ { if (seen[$0]++) exit 1; next } { exit 1 } END { if (NR != 93) exit 1 }' "$out/oracle.stdout" || return 1
    # Native coverage is the exact helper only; private bridge coverage above is CLR-only.
    head -n 3 "$out/oracle.stdout" > "$out/native-expected.stdout" || return 1
    for axis in raw diet; do
        emit=$(mktemp -d "$out/$axis-XXXXXX") || return 1
        local flags=() runner=()
        [ "$axis" != raw ] || flags+=(--no-ildiet)
        gate_capture_ok "$out/$axis-emit" invoke_cli "$out/runtime.dll" -r "$_CG_CORELIB" --auto-ref "${flags[@]}" -o "$emit" || return 1
        if [ "${WASM:-0}" = 1 ]; then
            compile_console_wasm "$emit" VectorAlignment || return 1
            runner=(node "$emit/VectorAlignment.js")
        else
            compile_console "$emit" VectorAlignment || return 1
            runner=("$emit/VectorAlignment$EXE_EXT")
        fi
        gate_capture_ok "$out/$axis-run" run_bounded "${runner[@]}" || return 1
        gate_output compare "$out/$axis-run.stdout" "$out/native-expected.stdout" --policy crlf || return 1
        gate_output compare "$out/$axis-run.stderr" "$out/oracle.stderr" --policy crlf || return 1
    done
}
