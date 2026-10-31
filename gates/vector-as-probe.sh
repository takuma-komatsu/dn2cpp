#!/usr/bin/env bash
# Exact unsupported Vector.As guards and evaluated operand parity.
probe_guarded_vector_as() {
    local root="$1" out axis emit mode suite
    local fixture="${DN2CPP_VECTOR_AS_FIXTURE_DLL:-gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll}"
    local compiler="${DN2CPP_VECTOR_AS_COMPILER_DLL:-$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll}"
    if [ -z "${DN2CPP_VECTOR_AS_FIXTURE_DLL:-}" ]; then
        build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return 1
    fi
    mkdir -p "$root" || return 1
    out=$(mktemp -d "$root/guarded-as-XXXXXX") || return 1
    gate_capture_ok "$out/guards" run_bounded dotnet "$fixture" --vector-as-guards \
        "$compiler" "$_CG_CORELIB" "$out/model.dll" || return 1
    grep -E '^vector-as-guards:checks=[1-9][0-9]*$' "$out/guards.stdout" >/dev/null || return 1
    suite="$out"
    for mode in all-runtime numeric-runtime canonical-runtime; do
        out="$suite/$mode"
        mkdir -p "$out" || return 1
    gate_capture_ok "$out/generate" run_bounded dotnet "$fixture" --vector-as-fixture \
        "$out/runtime.dll" "$_CG_CORELIB" "$mode" || return 1
    gate_capture_ok "$out/oracle" run_bounded env -u DOTNET_EnableHWIntrinsic -u COMPlus_EnableHWIntrinsic \
        dotnet "$out/runtime.dll" || return 1
    gate_capture_ok "$out/oracle-nohw" run_bounded env -u COMPlus_EnableHWIntrinsic DOTNET_EnableHWIntrinsic=0 \
        dotnet "$out/runtime.dll" || return 1
    gate_output compare "$out/oracle.stdout" "$out/oracle-nohw.stdout" --policy crlf || return 1
    gate_output compare "$out/oracle.stderr" "$out/oracle-nohw.stderr" --policy crlf || return 1
    if [ "$mode" = all-runtime ]; then
        [ "$(wc -l < "$out/oracle.stdout")" -eq 360 ] || return 1
        [ "$(grep -c '^Case' "$out/oracle.stdout")" -eq 90 ] || return 1
        awk '
            NR % 4 == 1 {
                if ($0 !~ /^Case[0-9]+_/ || seen[$0]++) exit 1
                split($0, fields, "_")
                if (fields[1] != "Case" ((NR - 1) / 4)) exit 1
                count = ($0 ~ /runtime_and(_right_throw)?$/ ? "12" : "1")
                message = "Specified type is not supported"
                if ($0 ~ /runtime_throw$/) message = "operand"
                if ($0 ~ /runtime_and_left_throw$/) message = "left"
                if ($0 ~ /runtime_and_right_throw$/) message = "right"
                exception = (message == "Specified type is not supported" ? "System.NotSupportedException" : "System.InvalidOperationException")
            }
            NR % 4 == 2 && $0 != count { exit 1 }
            NR % 4 == 3 && $0 != exception { exit 1 }
            NR % 4 == 0 && $0 != message { exit 1 }
            END { if (NR != 360) exit 1 }
        ' "$out/oracle.stdout" || return 1
    elif [ "$mode" = numeric-runtime ]; then
        [ "$(wc -l < "$out/oracle.stdout")" -eq 12 ] || return 1
        [ "$(grep -c '^numeric:.*:pass$' "$out/oracle.stdout")" -eq 12 ] || return 1
    else
        [ "$(wc -l < "$out/oracle.stdout")" -eq 64 ] || return 1
        [ "$(grep -c '^Case' "$out/oracle.stdout")" -eq 16 ] || return 1
        awk '
            NR % 4 == 1 {
                if ($0 !~ /^Case[0-9]+:(mvar|var):(ToInt|FromInt)(Direct|AdapterCalli):(int|enum)$/ || seen[$0]++) exit 1
                split($0, fields, ":"); kind = fields[4]
                if (fields[1] != "Case" ((NR - 1) / 4)) exit 1
            }
            NR % 4 == 2 && $0 != "1" { exit 1 }
            NR % 4 == 3 && $0 != (kind == "int" ? "numeric" : "System.NotSupportedException") { exit 1 }
            NR % 4 == 0 {
                if ($0 != (kind == "int" ? "pass" : "Specified type is not supported")) exit 1
                if (kind == "int") numeric++; else unsupported++
            }
            END { if (NR != 64 || numeric != 8 || unsupported != 8) exit 1 }
        ' "$out/oracle.stdout" || return 1
    fi
    if grep -E 'UNEXPECTED|FAIL' "$out/oracle.stdout" >/dev/null; then return 1; fi
    for axis in raw diet; do
        emit=$(mktemp -d "$out/$axis-XXXXXX") || return 1
        local flags=() runner=()
        [ "$axis" != raw ] || flags+=(--no-ildiet)
        gate_capture_ok "$out/$axis-emit" invoke_cli "$out/runtime.dll" -r "$_CG_CORELIB" --auto-ref "${flags[@]}" -o "$emit" || return 1
        if [ "${WASM:-0}" = 1 ]; then
            compile_console_wasm "$emit" VectorAs || return 1
            runner=(node "$emit/VectorAs.js")
        else
            compile_console "$emit" VectorAs || return 1
            runner=("./$emit/VectorAs$EXE_EXT")
        fi
        gate_capture_ok "$out/$axis-run" run_bounded "${runner[@]}" || return 1
        gate_output compare "$out/$axis-run.stdout" "$out/oracle.stdout" --policy crlf || return 1
        gate_output compare "$out/$axis-run.stderr" "$out/oracle.stderr" --policy crlf || return 1
    done
    done
}
