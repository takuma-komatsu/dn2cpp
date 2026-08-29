#!/usr/bin/env bash
# Shared helpers for the platform-ISA gates (build-and-run-platform-isa-*.sh).
# Sourced after _common.sh. The source of truth for the family set and its
# Lowered flags is the generated transpiler table; everything here reads it
# rather than keeping a second copy.

PLATFORM_ISA_TABLE=src/Dn2Cpp.Transpiler/CoreIntrinsics.PlatformIsa.g.cs
PLATFORM_ISA_PROJECT=PlatformIsaProbe

# One table row per line:
#   new(IsaArch.X86, "System.Runtime.Intrinsics.X86.Lzcnt+X64", "DN2CPP_ISA_X86_Lzcnt_X64", "System.Runtime.Intrinsics.X86.Lzcnt", false),
# Anchored on the whole line so a row the parser cannot read is a hard error
# below, never a silently dropped family.
_PLATFORM_ISA_ROW_RE='^[[:space:]]*new\(IsaArch\.(X86|Arm|Wasm), "System\.Runtime\.Intrinsics\.([A-Za-z0-9_.+]+)", "(DN2CPP_ISA_[A-Za-z0-9_]+)", ("[^"]*"|null), (true|false)\),?[[:space:]]*$'

# _platform_isa_table_fields — every row as `Arch<TAB>RowName<TAB>Token<TAB>lowered`
# in file order; RowName is the qualified name minus the namespace with `+`
# rewritten to `.` (X86.Lzcnt.X64), the spelling the probe and the CLI use.
_platform_isa_table_fields() {
    [ -f "$PLATFORM_ISA_TABLE" ] \
        || { echo "error: $PLATFORM_ISA_TABLE not found (the generated platform-ISA table)" >&2; return 1; }
    local parsed n_rows n_parsed
    # A literal tab in the replacement: BSD sed does not expand \t there.
    local tab=$'\t'
    parsed=$(sed -nE "s/$_PLATFORM_ISA_ROW_RE/\1$tab\2$tab\3$tab\5/p" "$PLATFORM_ISA_TABLE" | tr '+' '.')
    n_rows=$(grep -cE '^[[:space:]]*new\(IsaArch\.' "$PLATFORM_ISA_TABLE" || true)
    n_parsed=$(grep -c . <<<"$parsed" || true)
    if [ "$n_rows" -ne "$n_parsed" ]; then
        echo "error: $PLATFORM_ISA_TABLE has $n_rows IsaArch rows but only $n_parsed parse; a row's shape changed" >&2
        return 1
    fi
    printf '%s\n' "$parsed"
}

# platform_isa_table_rows ARCH — `RowName<TAB>lowered` for ARCH (x86|arm|wasm),
# table order.
platform_isa_table_rows() {
    local isa
    isa=$(platform_isa_arch_name "$1") || return 1
    _platform_isa_table_fields | awk -F'\t' -v a="$isa" '$1 == a { print $2 "\t" $4 }'
}

# platform_isa_arch_name ARCH — the table's spelling (X86 / Arm / Wasm).
platform_isa_arch_name() {
    case "$1" in
        x86)  printf 'X86\n' ;;
        arm)  printf 'Arm\n' ;;
        wasm) printf 'Wasm\n' ;;
        *) echo "error: unknown platform-ISA arch '$1' (x86|arm|wasm)" >&2; return 1 ;;
    esac
}

# platform_isa_host_arch — x86, arm, or empty for anything else.
platform_isa_host_arch() {
    case "$(uname -m)" in
        x86_64|amd64)  printf 'x86\n' ;;
        arm64|aarch64) printf 'arm\n' ;;
        *) printf '\n' ;;
    esac
}

# platform_isa_avx10v2_compiler_capable — whether the configured native C++
# compiler makes DN2CPP_HAS_X86_AVX10V2_INTRINSICS true. Keep this predicate in
# lockstep with runtime/core/dn2cpp_cpu_features.h: the oracle opt-in is enabled
# only when our generated helper bodies can exist.
platform_isa_avx10v2_compiler_capable() {
    local cxx="${CMAKE_CXX_COMPILER:-${CXX:-c++}}" version first major
    version=$("$cxx" --version 2>/dev/null || true)
    first="${version%%$'\n'*}"
    case "$first" in
        *clang\ version*) ;;
        *) return 1 ;;
    esac
    major=$(sed -nE 's/.*clang version ([0-9]+).*/\1/p' <<<"$first")
    [ -n "$major" ] && [ "$major" -ge 21 ]
}

# platform_isa_join SEP ITEM... — ITEMs joined by SEP; empty when none.
platform_isa_join() {
    local sep="$1"; shift
    local out="" item
    for item in "$@"; do
        out="${out:+$out$sep}$item"
    done
    printf '%s\n' "$out"
}

# platform_isa_contract_block TEXT — the `== contract ==` lines of a probe run.
platform_isa_contract_block() {
    awk '/^== contract ==$/ { c = 1; next } /^== / { c = 0 } c' <<<"$1"
}

_platform_isa_witness_signature() {
    local text="$1" block true_count false_count nested_count
    block=$(platform_isa_contract_block "$text")
    true_count=$(grep -c '^X86.X86Base=True$' <<<"$block" || true)
    false_count=$(grep -c '^X86.Avx=False$' <<<"$block" || true)
    nested_count=$(awk '
        $0 == "== X86.Avx10v2.V512 ==" {
            getline
            if ($0 == "probe=PlatformNotSupportedException") found++
        }
        END { print found + 0 }
    ' <<<"$text")
    printf '%s\ntrue=%s\nfalse=%s\nnested=%s\n' \
        "$block" "$true_count" "$false_count" "$nested_count"
}

_platform_isa_witness_normalization_self_check() {
    local lf crlf lf_normalized crlf_normalized lf_signature crlf_signature
    lf=$'prefix\n== contract ==\nX86.X86Base=True\nX86.Avx=False\n== X86.Avx10v2.V512 ==\nprobe=PlatformNotSupportedException\n'
    crlf=${lf//$'\n'/$'\r\n'}
    lf_normalized=$(tr -d '\r' <<<"$lf")
    crlf_normalized=$(tr -d '\r' <<<"$crlf")
    lf_signature=$(_platform_isa_witness_signature "$lf_normalized")
    crlf_signature=$(_platform_isa_witness_signature "$crlf_normalized")
    if [ "$lf_signature" != "$crlf_signature" ] \
            || [ "$(grep -c '^true=1$' <<<"$lf_signature" || true)" -ne 1 ] \
            || [ "$(grep -c '^false=1$' <<<"$lf_signature" || true)" -ne 1 ] \
            || [ "$(grep -c '^nested=1$' <<<"$lf_signature" || true)" -ne 1 ]; then
        echo "FAIL: LF and CRLF platform-ISA witness parsing disagree" >&2
        return 1
    fi
}

# ── The surface gate's set helpers ───────────────────────────────────────────
# Same shape as gates/build-and-run-doc-claims.sh's, which keeps its own local.
PLATFORM_ISA_FAILS=0
PLATFORM_ISA_CHECKS=0

platform_isa_ok()  { PLATFORM_ISA_CHECKS=$((PLATFORM_ISA_CHECKS + 1)); printf '  ok    %s\n' "$1"; }
platform_isa_bad() { PLATFORM_ISA_CHECKS=$((PLATFORM_ISA_CHECKS + 1)); PLATFORM_ISA_FAILS=$((PLATFORM_ISA_FAILS + 1)); printf '  FAIL  %s\n' "$1" >&2; }

# platform_isa_eq LABEL EXPECTED ACTUAL
platform_isa_eq() {
    if [ "$2" = "$3" ]; then
        platform_isa_ok "$1 = $3"
    else
        platform_isa_bad "$1: expected '$2', measured '$3'"
    fi
}

# platform_isa_set_eq LABEL LEFT_NAME LEFT RIGHT_NAME RIGHT — sorted,
# newline-separated sets; each side of the difference is reported by name.
# Callers export LC_ALL=C so `comm` and `sort` agree on the collation.
platform_isa_set_eq() {
    local label="$1" lname="$2" rname="$4" only_l only_r
    only_l=$(comm -23 <(printf '%s\n' "$3") <(printf '%s\n' "$5") | tr '\n' ' ')
    only_r=$(comm -13 <(printf '%s\n' "$3") <(printf '%s\n' "$5") | tr '\n' ' ')
    if [ -z "${only_l// }" ] && [ -z "${only_r// }" ]; then
        platform_isa_ok "$label (both sides identical)"
    else
        platform_isa_bad "$label: $lname only: [${only_l:-none}]; $rname only: [${only_r:-none}]"
    fi
}

# ── The native behaviour gate ────────────────────────────────────────────────
# platform_isa_native_gate ARCH — the X86 or Arm contract, diffed against real
# .NET. The gate sets PLATFORM_ISA_P_MASK (our DN2CPP_CPU_FEATURES value),
# PLATFORM_ISA_P_KNOBS (the oracle's DOTNET_* assignments, space-separated),
# PLATFORM_ISA_P_TRUE and PLATFORM_ISA_P_FALSE (a row the partial mask keeps
# and one it removes) before calling.
#
# Runs, each ours-vs-oracle on stdout and exit code:
#   F  foreign host only: every row of ARCH, no mask. Witness: no `=True`.
#   S  native host, Lowered rows only, no mask on either side. Witness: the
#      base family is true once it is Lowered.
#   M  always: every row, DN2CPP_CPU_FEATURES=none vs DOTNET_EnableHWIntrinsic=0.
#      Witnesses: no `=True`, and exactly one `effective=(none)` diag line.
#   P  native host, Lowered rows only, the partial mask vs the oracle's knobs.
#      Witnesses: PLATFORM_ISA_P_TRUE stays true, PLATFORM_ISA_P_FALSE goes false.
#   O  native x86 host, the AVX10.2 policy rows under the positive opt-in. The
#      oracle opts in only when the configured compiler has the helper surface;
#      otherwise both sides stay false and the nested throwing stubs are called.
# Rows that are not Lowered fold to false on our side whatever the CPU, so S
# and P select the Lowered set only; F and M expect false everywhere anyway.
platform_isa_native_gate() {
    local arch="$1" isa base
    _platform_isa_witness_normalization_self_check || exit 1
    isa=$(platform_isa_arch_name "$arch") || exit 1
    case "$arch" in
        x86) base="X86.X86Base" ;;
        arm) base="Arm.ArmBase" ;;
        *) echo "error: platform_isa_native_gate takes x86 or arm, not '$arch'" >&2; exit 1 ;;
    esac
    : "${PLATFORM_ISA_P_MASK:?the gate must set PLATFORM_ISA_P_MASK}"
    : "${PLATFORM_ISA_P_KNOBS:?the gate must set PLATFORM_ISA_P_KNOBS}"
    : "${PLATFORM_ISA_P_TRUE:?the gate must set PLATFORM_ISA_P_TRUE}"
    : "${PLATFORM_ISA_P_FALSE:?the gate must set PLATFORM_ISA_P_FALSE}"

    # An inherited runtime or JIT knob would narrow or widen the default runs.
    # Clear both .NET prefixes: COMPlus_ remains a supported alias for these
    # legacy runtime configuration keys.
    unset DN2CPP_CPU_FEATURES DN2CPP_CPU_FEATURES_DIAG DN2CPP_ENABLE_AVX10V2
    unset DOTNET_EnableHWIntrinsic COMPlus_EnableHWIntrinsic
    unset DOTNET_EnableAVX10v2 COMPlus_EnableAVX10v2
    local knob variable
    for knob in $PLATFORM_ISA_P_KNOBS; do
        variable="${knob%%=*}"
        unset "$variable"
        case "$variable" in
            DOTNET_*) unset "COMPlus_${variable#DOTNET_}" ;;
        esac
    done

    local project="$PLATFORM_ISA_PROJECT" out host_arch
    out="$(_corelib_gate_out "$project")-isa-$arch"
    host_arch=$(platform_isa_host_arch)

    _corelib_gate_core "$project" "$out"

    local rows name lowered_flag all=() lowered=()
    rows=$(platform_isa_table_rows "$arch") || exit 1
    [ -n "$rows" ] || { echo "error: $PLATFORM_ISA_TABLE lists no $isa family" >&2; exit 1; }
    while IFS=$'\t' read -r name lowered_flag; do
        all+=("$name")
        [ "$lowered_flag" = true ] && lowered+=("$name")
    done <<<"$rows"
    local all_csv lowered_csv
    all_csv=$(platform_isa_join , "${all[@]}")
    lowered_csv=$(platform_isa_join , ${lowered[@]+"${lowered[@]}"})

    local native_host=0 avx10v2_compiler=0
    [ "$host_arch" = "$arch" ] && native_host=1
    if [ "$host_arch" = x86 ] && platform_isa_avx10v2_compiler_capable; then
        avx10v2_compiler=1
    fi

    local optin_ctx="" ctx="platform_isa|$arch|host:${host_arch:-other}|$_CG_CORELIB"
    [ "$arch" = x86 ] && optin_ctx="+O"
    ctx="$ctx|families:$all_csv|lowered:$lowered_csv"
    ctx="$ctx|runs:F+S+M+P$optin_ctx|M=DN2CPP_CPU_FEATURES=none+DOTNET_EnableHWIntrinsic=0"
    ctx="$ctx|P=DN2CPP_CPU_FEATURES=$PLATFORM_ISA_P_MASK+$PLATFORM_ISA_P_KNOBS"
    ctx="$ctx|avx10v2-compiler:$avx10v2_compiler|O=DN2CPP_ENABLE_AVX10V2=1"
    ctx="$ctx|assert:foreign-all-false+base-true+avx10v2-default-off+apple-avx512-false+mask-diag$(_gate_ctx_extras)"
    if _corelib_gate_check "$out" "$ctx"; then
        gate_cache_hit_msg
        return 0
    fi

    echo "== 4/4 Compiling C++ and running the $isa contract (exact diff vs real .NET) =="
    compile_console "$out" "$project"

    gate_run_logs_init "platform_isa_$arch" "platform-isa-$arch"
    local log_dir="$_GATE_RUN_LOG_DIR"
    _PI_RUN_LABELS=()
    _PI_RUN_CODES=()
    _PI_RUN_ORACLE_CODES=()
    _PI_RUN_OUTS=()
    _PI_RUN_ORACLE_OUTS=()
    _PI_RUN_LOGS=()
    _PI_RUN_ORACLE_LOGS=()

    _pi_run_diag_file() {
        local label="$1" code="$2" stdout_file="$3" stderr_file="$4"
        echo "---- $label diagnostics ----" >&2
        echo "exit code: ${code:-<not run>}" >&2
        _gate_run_termination "$code" "$stderr_file"
        if [ -s "$stdout_file" ]; then
            echo "stdout tail (full output: $stdout_file):" >&2
            tail -n 40 "$stdout_file" >&2
        else
            echo "stdout: <empty>" >&2
        fi
        if [ -s "$stderr_file" ]; then
            echo "stderr:" >&2
            cat "$stderr_file" >&2
        else
            echo "stderr: <empty>" >&2
        fi
    }

    gate_run_diagnostics() {
        local i
        for ((i = 0; i < ${#_PI_RUN_LABELS[@]}; i++)); do
            _pi_run_diag_file "${_PI_RUN_LABELS[$i]} native" \
                "${_PI_RUN_CODES[$i]}" "${_PI_RUN_OUTS[$i]}" "${_PI_RUN_LOGS[$i]}"
            _pi_run_diag_file "${_PI_RUN_LABELS[$i]} oracle" \
                "${_PI_RUN_ORACLE_CODES[$i]}" "${_PI_RUN_ORACLE_OUTS[$i]}" \
                "${_PI_RUN_ORACLE_LOGS[$i]}"
        done
    }

    # _pi_run LABEL ARGV OURS_ENV ORACLE_ENV — one arm: ours under OURS_ENV vs
    # `dotnet` under ORACLE_ENV (space-separated NAME=VALUE lists, may be empty).
    # Sets _PI_OURS / _PI_LOG; fails the gate on any stdout or exit-code diff.
    _pi_run() {
        local label="$1" argv="$2" ours_env="$3" oracle_env="$4"
        local ours_code expected_code ours_out expected_out oracle_log
        _PI_LOG="$log_dir/$label.log"
        oracle_log="$log_dir/$label.oracle.log"
        ours_out="$log_dir/$label.out"
        expected_out="$log_dir/$label.oracle.out"
        # Scope each ISA environment in a subshell: `env` cannot invoke the
        # run_bounded shell function. Direct files avoid MSYS native-pipe loss.
        set +e
        # shellcheck disable=SC2086
        (if [ -n "$ours_env" ]; then export $ours_env; fi
         run_bounded "./$out/$project" "$argv") \
            >"$ours_out" 2>"$_PI_LOG"; ours_code=$?
        # shellcheck disable=SC2086
        (if [ -n "$oracle_env" ]; then export $oracle_env; fi
         run_bounded dotnet "$_CG_APP" "$argv") \
            >"$expected_out" 2>"$oracle_log"; expected_code=$?
        set -e
        _PI_RUN_LABELS+=("$label")
        _PI_RUN_CODES+=("$ours_code")
        _PI_RUN_ORACLE_CODES+=("$expected_code")
        _PI_RUN_OUTS+=("$ours_out")
        _PI_RUN_ORACLE_OUTS+=("$expected_out")
        _PI_RUN_LOGS+=("$_PI_LOG")
        _PI_RUN_ORACLE_LOGS+=("$oracle_log")
        echo "-- run $label: argv=$argv ours=[${ours_env:-<no ISA env>}] oracle=[${oracle_env:-<no ISA env>}]"
        local failed=0
        if cmp -s "$ours_out" "$expected_out"; then
            cat "$ours_out"
            echo "OK"
        else
            echo "FAIL: native stdout differs from real .NET:" >&2
            diff -u "$expected_out" "$ours_out" >&2 || true
            failed=1
        fi
        assert_exit_code "$ours_code" "$expected_code" || failed=1
        if [ "$failed" -ne 0 ]; then
            echo "FAIL: run $label of the $isa contract diverged from real .NET" >&2
            gate_run_diag_once
            exit 1
        fi
        # The raw files above enforce byte parity. This copy is text-only witness
        # input, so remove Windows CR without weakening that exact comparison.
        _PI_OURS=$(tr -d '\r' < "$ours_out")
    }

    # _pi_require_count LABEL WANT ACTUAL WHAT
    _pi_require_count() {
        if [ "$3" -ne "$2" ]; then
            echo "FAIL: run $1: expected $2 $4, found $3" >&2
            gate_run_diag_once
            exit 1
        fi
    }

    local block n
    if [ "$native_host" -eq 0 ]; then
        _pi_run F "$all_csv" "" ""
        block=$(platform_isa_contract_block "$_PI_OURS")
        n=$(grep -c '=True$' <<<"$block" || true)
        _pi_require_count F 0 "$n" "'=True' rows on a non-$isa host"
    fi

    if [ "$native_host" -eq 1 ] && [ -n "$lowered_csv" ]; then
        _pi_run S "$lowered_csv" "" ""
        case ",$lowered_csv," in
            *",$base,"*)
                n=$(grep -c "^$base=True\$" <<<"$_PI_OURS" || true)
                _pi_require_count S 1 "$n" "'$base=True' line (the base family is Lowered and this IS an $isa host)"
                ;;
        esac
        if [ "$arch" = x86 ]; then
            block=$(platform_isa_contract_block "$_PI_OURS")
            n=$(grep -Ec '^X86\.(Avx10v2(\.(V512(\.X64)?|X64))?|AvxVnniInt(8|16)\.V512)=False$' <<<"$block" || true)
            _pi_require_count S 6 "$n" "AVX10.2 policy rows reported False without DN2CPP_ENABLE_AVX10V2=1"
        fi
        # .NET deliberately reports AVX-512 unavailable on macOS: the OS enables
        # its register state lazily per thread, and VEX-encoded mask moves do not
        # trigger that enablement. Keep every family that needs that state false,
        # even on an Intel Mac whose CPUID advertises it.
        if [ "$arch" = x86 ] && [ "$(uname -s)" = Darwin ]; then
            block=$(platform_isa_contract_block "$_PI_OURS")
            n=$(grep -Ec '^X86\.(Avx512.*|Avx10.*|Pclmulqdq\.V512|Gfni\.V512|AvxVnniInt(8|16)\.V512)=True$' <<<"$block" || true)
            _pi_require_count S 0 "$n" "AVX-512-state rows reported True on macOS"
        fi
    fi

    if [ "$native_host" -eq 1 ] && [ "$arch" = x86 ]; then
        local optin_rows oracle_optin nested
        optin_rows="X86.Avx10v2,X86.Avx10v2.V512,X86.Avx10v2.V512.X64,X86.Avx10v2.X64,X86.AvxVnniInt8,X86.AvxVnniInt8.V512,X86.AvxVnniInt16,X86.AvxVnniInt16.V512"
        oracle_optin="DOTNET_EnableAVX10v2=0"
        [ "$avx10v2_compiler" -eq 1 ] && oracle_optin="DOTNET_EnableAVX10v2=1"
        _pi_run O "$optin_rows" "DN2CPP_ENABLE_AVX10V2=1" "$oracle_optin"
        if [ "$avx10v2_compiler" -eq 0 ]; then
            block=$(platform_isa_contract_block "$_PI_OURS")
            n=$(grep -Ec '^X86\.(Avx10v2(\.(V512(\.X64)?|X64))?|AvxVnniInt(8|16)\.V512)=False$' <<<"$block" || true)
            _pi_require_count O 6 "$n" "AVX10.2 rows kept False by the compiler capability"
            for nested in X86.Avx10v2.V512 X86.AvxVnniInt8.V512 X86.AvxVnniInt16.V512; do
                n=$(awk -v header="== $nested ==" '
                    $0 == header { getline; if ($0 == "probe=PlatformNotSupportedException") found++ }
                    END { print found + 0 }
                ' <<<"$_PI_OURS")
                _pi_require_count O 1 "$n" "'$nested' compiler-unavailable stub reached PlatformNotSupportedException"
            done
        fi
    fi

    _pi_run M "$all_csv" "DN2CPP_CPU_FEATURES=none DN2CPP_CPU_FEATURES_DIAG=1" "DOTNET_EnableHWIntrinsic=0"
    block=$(platform_isa_contract_block "$_PI_OURS")
    n=$(grep -c '=True$' <<<"$block" || true)
    _pi_require_count M 0 "$n" "'=True' rows under DN2CPP_CPU_FEATURES=none"
    n=$(grep -c 'effective=(none)' "$_PI_LOG" || true)
    _pi_require_count M 1 "$n" "'effective=(none)' diag line on stderr (DN2CPP_CPU_FEATURES_DIAG=1 prints exactly one)"

    if [ "$native_host" -eq 1 ] && [ -n "$lowered_csv" ]; then
        _pi_run P "$lowered_csv" "DN2CPP_CPU_FEATURES=$PLATFORM_ISA_P_MASK" "$PLATFORM_ISA_P_KNOBS"
        case ",$lowered_csv," in
            *",$PLATFORM_ISA_P_TRUE,"*)
                n=$(grep -c "^$PLATFORM_ISA_P_TRUE=True\$" <<<"$_PI_OURS" || true)
                _pi_require_count P 1 "$n" "'$PLATFORM_ISA_P_TRUE=True' line (kept by the partial mask)"
                ;;
        esac
        case ",$lowered_csv," in
            *",$PLATFORM_ISA_P_FALSE,"*)
                n=$(grep -c "^$PLATFORM_ISA_P_FALSE=False\$" <<<"$_PI_OURS" || true)
                _pi_require_count P 1 "$n" "'$PLATFORM_ISA_P_FALSE=False' line (removed by the partial mask)"
                ;;
        esac
    elif [ "$native_host" -eq 1 ]; then
        echo "runs S and P select the Lowered $isa families and none is Lowered yet: nothing to run, nothing omitted"
    fi

    if [ "$native_host" -eq 0 ]; then
        local rosetta="" native_runs="S and P"
        if [ "$arch" = x86 ]; then
            native_runs="S, O and P"
            rosetta=" Rosetta is excluded on purpose: its CPUID exposes a feature set no shipping CPU has, and the oracle would be an emulated .NET rather than the host's."
        fi
        # One line: run-all-gates.sh's summary takes the FIRST line of the reason.
        gate_expected_partial "runs $native_runs (the $isa families' supported, opt-in where applicable, and partially-masked paths) have no reachable state on this ${host_arch:-$(uname -m)} host: no software installable here creates an $isa CPU, so nothing this gate could do makes an $isa family's IsSupported true on either side.$rosetta Structural and permanent, not an absent prerequisite — which is why this is not a gate_skip/gate_partial. The uncovered surface IS asserted for real by this same gate, gates/build-and-run-platform-isa-$arch.sh, run on an $isa host (the linux-x64 and windows-x64 lanes for x86, macos-arm64 for arm), where $native_runs run end to end. Runs F (every $isa row false and every $isa family throwing PlatformNotSupportedException, exactly as real .NET does here) and M (DN2CPP_CPU_FEATURES=none vs DOTNET_EnableHWIntrinsic=0, with the one diag line) did run in this invocation and hold."
    fi

    gate_cache_commit
}
