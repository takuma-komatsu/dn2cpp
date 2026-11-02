#!/usr/bin/env bash
# Exact Vector.As and forwarding-family guards with evaluated operand parity.
probe_guarded_vector_as() {
    local root="$1" family="${2:-as}" out axis emit mode suite cases canonical_cases
    case "$family" in as|explicit|alias) ;; *) return 1 ;; esac
    cases=90; canonical_cases=16
    if [ "$family" != as ]; then cases=231; canonical_cases=8; fi
    local fixture="${DN2CPP_VECTOR_AS_FIXTURE_DLL:-gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll}"
    local compiler="${DN2CPP_VECTOR_AS_COMPILER_DLL:-$(dirname "${DN2CPP_CLI_DLL:-src/Dn2Cpp.Cli/bin/$CONFIG/$TFM/dn2cpp.dll}")/Dn2Cpp.Transpiler.dll}"
    if [ -z "${DN2CPP_VECTOR_AS_FIXTURE_DLL:-}" ]; then
        build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj || return 1
    fi
    mkdir -p "$root" || return 1
    out=$(mktemp -d "$root/guarded-$family-XXXXXX") || return 1
    local guard_args=(--vector-as-guards "$compiler" "$_CG_CORELIB" "$out/model.dll")
    local guard_prefix=vector-as-guards fixture_option=--vector-as-fixture fixture_mode
    if [ "$family" != as ]; then
        guard_args=(--vector-forwarding-guards "$compiler" "$_CG_CORELIB" "$out/model.dll" "$family")
        guard_prefix="vector-forwarding-guards:$family"; fixture_option=--vector-forwarding-fixture
    fi
    gate_capture_ok "$out/guards" run_bounded dotnet "$fixture" "${guard_args[@]}" || return 1
    grep -E "^$guard_prefix:checks=[1-9][0-9]*$" "$out/guards.stdout" >/dev/null || return 1
    suite="$out"
    for mode in all-runtime numeric-runtime canonical-runtime; do
        out="$suite/$mode"
        mkdir -p "$out" || return 1
    fixture_mode="$mode"
    [ "$family" = as ] || fixture_mode="$family:$mode"
    gate_capture_ok "$out/generate" run_bounded dotnet "$fixture" "$fixture_option" \
        "$out/runtime.dll" "$_CG_CORELIB" "$fixture_mode" || return 1
    gate_capture_ok "$out/oracle" run_bounded env -u DOTNET_EnableHWIntrinsic -u COMPlus_EnableHWIntrinsic \
        dotnet "$out/runtime.dll" || return 1
    gate_capture_ok "$out/oracle-nohw" run_bounded env -u COMPlus_EnableHWIntrinsic DOTNET_EnableHWIntrinsic=0 \
        dotnet "$out/runtime.dll" || return 1
    gate_output compare "$out/oracle.stdout" "$out/oracle-nohw.stdout" --policy crlf || return 1
    gate_output compare "$out/oracle.stderr" "$out/oracle-nohw.stderr" --policy crlf || return 1
    [ ! -s "$out/oracle.stderr" ] || return 1
    if [ "$mode" = all-runtime ]; then
        [ "$(wc -l < "$out/oracle.stdout")" -eq "$((cases * 4))" ] || return 1
        [ "$(grep -c '^Case' "$out/oracle.stdout")" -eq "$cases" ] || return 1
        awk -v family="$family" -v cases="$cases" '
            NR % 4 == 1 {
                if ($0 !~ /^Case[0-9]+_/ || seen[$0]++) exit 1
                split($0, fields, "_")
                if (fields[1] != "Case" ((NR - 1) / 4)) exit 1
                if (family != "as") {
                    if (($0 ~ /runtime_throw$/) != (((NR - 1) / 4) >= cases - 3)) exit 1
                    if ($0 ~ /runtime_and/) exit 1
                }
                count = (family == "as" && $0 ~ /runtime_and(_right_throw)?$/ ? "12" : "1")
                message = "Specified type is not supported"
                if ($0 ~ /runtime_throw$/) message = "operand"
                if ($0 ~ /runtime_and_left_throw$/) message = "left"
                if ($0 ~ /runtime_and_right_throw$/) message = "right"
                exception = (message == "Specified type is not supported" ? "System.NotSupportedException" : "System.InvalidOperationException")
            }
            NR % 4 == 2 && $0 != count { exit 1 }
            NR % 4 == 3 && $0 != exception { exit 1 }
            NR % 4 == 0 && $0 != message { exit 1 }
            END { if (NR != cases * 4) exit 1 }
        ' "$out/oracle.stdout" || return 1
    elif [ "$mode" = numeric-runtime ]; then
        [ "$(wc -l < "$out/oracle.stdout")" -eq 12 ] || return 1
        awk '
            {
                if ($0 !~ /^numeric:(byte|sbyte|short|ushort|int|uint|long|ulong|nint|nuint|float|double):pass$/ || seen[$0]++) exit 1
            }
            END { if (NR != 12) exit 1 }
        ' "$out/oracle.stdout" || return 1
    else
        [ "$(wc -l < "$out/oracle.stdout")" -eq "$((canonical_cases * 4))" ] || return 1
        [ "$(grep -c '^Case' "$out/oracle.stdout")" -eq "$canonical_cases" ] || return 1
        awk -v family="$family" -v cases="$canonical_cases" '
            NR % 4 == 1 {
                if ($0 !~ /^Case[0-9]+:(mvar|var):(ToInt|FromInt)(Direct|AdapterCalli):(int|enum)$/ || seen[$0]++) exit 1
                if (family != "as" && $0 !~ /:ToInt(Direct|AdapterCalli):/) exit 1
                split($0, fields, ":"); kind = fields[4]
                if (fields[1] != "Case" ((NR - 1) / 4)) exit 1
            }
            NR % 4 == 2 && $0 != "1" { exit 1 }
            NR % 4 == 3 && $0 != (kind == "int" ? "numeric" : "System.NotSupportedException") { exit 1 }
            NR % 4 == 0 {
                if ($0 != (kind == "int" ? "pass" : "Specified type is not supported")) exit 1
                if (kind == "int") numeric++; else unsupported++
            }
            END { if (NR != cases * 4 || numeric != cases / 2 || unsupported != cases / 2) exit 1 }
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
    if [ "$family" = as ]; then
        probe_guarded_vector_as "$root" explicit || return 1
        probe_guarded_vector_as "$root" alias || return 1
    fi
}
