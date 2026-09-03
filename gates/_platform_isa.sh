#!/usr/bin/env bash
# Shared helpers for the platform-ISA gates (build-and-run-platform-isa-*.sh).
# Sourced after _common.sh. The source of truth for the family set and its
# Lowered flags is the generated transpiler table; everything here reads it
# rather than keeping a second copy.

PLATFORM_ISA_TABLE=src/Dn2Cpp.Transpiler/CoreIntrinsics.PlatformIsa.g.cs
PLATFORM_ISA_PROJECT=PlatformIsaProbe
PLATFORM_ISA_EXERCISES=samples/dotnet/PlatformIsaProbe/Exercises.g.cs

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

PLATFORM_ISA_NATIVE_SHARDS="x86-base x86-avx x86-avx512 x86-avx10 arm"

platform_isa_shard_arch() {
    case "$1" in
        x86-*) printf 'x86\n' ;;
        arm)   printf 'arm\n' ;;
        *) echo "error: unknown native platform-ISA shard '$1'" >&2; return 1 ;;
    esac
}

platform_isa_exercise_body_markers() {
    local name="$1" family
    case "$name" in
        X86.X86Base|X86.Lzcnt|X86.Popcnt|X86.Bmi1|X86.Bmi2|X86.X86Serialize)
            family=${name#X86.}
            printf 'PlatformIsaProbe.X86Sections::%sExercise\n' "$family"
            ;;
        Arm.ArmBase|Arm.Crc32)
            family=${name#Arm.}
            printf 'PlatformIsaProbe.ArmSections::%sExercise\n' "$family"
            ;;
        *)
            printf 'PlatformIsaProbe.Exercises::%s\n' "${name//./}"
            ;;
    esac
    if [ "$name" = X86.Avx ]; then
        printf 'PlatformIsaProbe.X86Sections::AvxCompareScalarTrueModesExercise\n'
    fi
}

# platform_isa_contract_block TEXT — the `== contract ==` lines of a probe run.
platform_isa_contract_block() {
    awk '/^== contract ==$/ { c = 1; next } /^== / { c = 0 } c' <<<"$1"
}

# platform_isa_invalid_immediate_rows ARCH — family rows with a generated
# dn2cpp-only out-of-contract immediate exercise, in registry order.
platform_isa_invalid_immediate_rows() {
    local isa
    isa=$(platform_isa_arch_name "$1") || return 1
    [ -f "$PLATFORM_ISA_EXERCISES" ] \
        || { echo "error: $PLATFORM_ISA_EXERCISES not found" >&2; return 1; }
    tr -d '\r' < "$PLATFORM_ISA_EXERCISES" \
        | sed -nE "s/^[[:space:]]*exercises\.Add\(\"($isa\.[^\"]+)\", \(\) =>$/\1/p"
}

# platform_isa_invalid_immediate_registry — every generated native-only
# immediate boundary, in registry order. The native gate executes the X86 and
# Arm subsets; the combined cache keys the complete registry so a Wasm-only
# generator change cannot leave the shared PlatformIsaProbe result warm.
platform_isa_invalid_immediate_registry() {
    [ -f "$PLATFORM_ISA_EXERCISES" ] \
        || { echo "error: $PLATFORM_ISA_EXERCISES not found" >&2; return 1; }
    tr -d '\r' < "$PLATFORM_ISA_EXERCISES" \
        | sed -nE 's/^[[:space:]]*exercises\.Add\("((X86|Arm|Wasm)\.[^"]+)", \(\) =>$/\1/p'
}

# platform_isa_invalid_immediate_violations TEXT — one diagnostic per boundary
# result that disagrees with its token. Helpers require the token before their
# immediate dispatch: true therefore throws AOOE, false PNSE.
platform_isa_invalid_immediate_violations() {
    awk '
        /^== contract ==$/ { contract = 1; next }
        /^== / { contract = 0 }
        contract && /^[A-Za-z0-9.]+=(True|False)$/ {
            row = $0; sub(/=.*/, "", row)
            value = $0; sub(/.*=/, "", value)
            supported[row] = value
            next
        }
        /^== invalid [A-Za-z0-9.]+ ==$/ {
            row = $0; sub(/^== invalid /, "", row); sub(/ ==$/, "", row)
            sections++
            if (++seen[row] != 1) print "duplicate invalid-immediate section: " row
            if (!(row in supported)) print "invalid-immediate section has no contract row: " row
            if ((getline result) <= 0) {
                print "invalid-immediate section has no result: " row
                next
            }
            got = result; sub(/^.*=/, "", got)
            want = supported[row] == "True" \
                ? "ArgumentOutOfRangeException" : "PlatformNotSupportedException"
            if (got != want) print row ": expected " want ", got " got
        }
        END { if (sections == 0) print "no invalid-immediate sections" }
    ' <<<"$1"
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

# _platform_isa_configure_arch ARCH — the partial-mask contract for one run.
# These values select run-time policy only; both architectures are emitted into
# and linked from the same probe.
_platform_isa_configure_arch() {
    case "$1" in
        x86)
            # AVX is the root of .NET 10's AVX-level, AVX-512 and AVX10 sets.
            # X86Base is the kept witness and Bmi1 the removed witness.
            PLATFORM_ISA_P_MASK="-Avx"
            PLATFORM_ISA_P_KNOBS="DOTNET_EnableAVX=0"
            PLATFORM_ISA_P_TRUE="X86.X86Base"
            PLATFORM_ISA_P_FALSE="X86.Bmi1"
            ;;
        arm)
            # AdvSimd is baseline on arm64; the JIT exposes knobs only for the
            # optional families. ArmBase stays and Crc32 is removed.
            PLATFORM_ISA_P_MASK="ArmBase,AdvSimd"
            PLATFORM_ISA_P_KNOBS="DOTNET_EnableArm64Aes=0 DOTNET_EnableArm64Crc32=0 DOTNET_EnableArm64Dp=0 DOTNET_EnableArm64Rdm=0 DOTNET_EnableArm64Sha1=0 DOTNET_EnableArm64Sha256=0 DOTNET_EnableArm64Sve=0 DOTNET_EnableArm64Sve2=0"
            PLATFORM_ISA_P_TRUE="Arm.ArmBase"
            PLATFORM_ISA_P_FALSE="Arm.Crc32"
            ;;
        *) echo "error: no native platform-ISA configuration for '$1'" >&2; return 1 ;;
    esac
}

# _platform_isa_clear_runtime_env — default runs must not inherit a native or
# oracle mask. Clear the union of both architectures' knobs and their COMPlus_
# aliases before the one shared transpile/build and every sequential run.
_platform_isa_clear_runtime_env() {
    unset DN2CPP_CPU_FEATURES DN2CPP_CPU_FEATURES_DIAG DN2CPP_ENABLE_AVX10V2
    unset DOTNET_EnableHWIntrinsic COMPlus_EnableHWIntrinsic
    unset DOTNET_EnableAVX10v2 COMPlus_EnableAVX10v2
    local arch knob variable
    for arch in x86 arm; do
        _platform_isa_configure_arch "$arch" || return 1
        for knob in $PLATFORM_ISA_P_KNOBS; do
            variable="${knob%%=*}"
            unset "$variable"
            case "$variable" in
                DOTNET_*) unset "COMPlus_${variable#DOTNET_}" ;;
            esac
        done
    done
    unset PLATFORM_ISA_P_MASK PLATFORM_ISA_P_KNOBS
    unset PLATFORM_ISA_P_TRUE PLATFORM_ISA_P_FALSE
}

# _platform_isa_arch_context ARCH AVX10V2_COMPILER — the complete
# run/assert plan for one architecture. The combined cache concatenates both
# results, so neither half can change without invalidating the shared binary.
_platform_isa_arch_context() {
    local arch="$1" avx10v2_compiler="$2" isa rows name lowered_flag
    local all=() lowered=() all_csv lowered_csv invalid_rows invalid_csv invalid_count
    local optin_ctx="" ctx
    isa=$(platform_isa_arch_name "$arch") || return 1
    _platform_isa_configure_arch "$arch" || return 1
    rows=$(platform_isa_table_rows "$arch") || return 1
    [ -n "$rows" ] || { echo "error: $PLATFORM_ISA_TABLE lists no $isa family" >&2; return 1; }
    while IFS=$'\t' read -r name lowered_flag; do
        all+=("$name")
        [ "$lowered_flag" = true ] && lowered+=("$name")
    done <<<"$rows"
    all_csv=$(platform_isa_join , "${all[@]}")
    lowered_csv=$(platform_isa_join , ${lowered[@]+"${lowered[@]}"})
    invalid_rows=$(platform_isa_invalid_immediate_rows "$arch") || return 1
    invalid_csv=$(tr '\n' ',' <<<"$invalid_rows")
    invalid_csv=${invalid_csv%,}
    invalid_count=$(grep -c . <<<"$invalid_rows" || true)
    [ "$invalid_count" -gt 0 ] \
        || { echo "error: generated exercises list no $isa invalid-immediate boundary" >&2; return 1; }
    [ "$arch" = x86 ] && optin_ctx="+O"
    ctx="$arch|families:$all_csv|lowered:$lowered_csv"
    ctx="$ctx|runs:F+S+M+P+N+NM$optin_ctx|M=DN2CPP_CPU_FEATURES=none+DOTNET_EnableHWIntrinsic=0"
    ctx="$ctx|P=DN2CPP_CPU_FEATURES=$PLATFORM_ISA_P_MASK+$PLATFORM_ISA_P_KNOBS"
    ctx="$ctx|invalid-immediates:$invalid_csv|N=default+--invalid-immediates|NM=DN2CPP_CPU_FEATURES=none+--invalid-immediates"
    ctx="$ctx|avx10v2-compiler:$avx10v2_compiler|O=DN2CPP_ENABLE_AVX10V2=1"
    ctx="$ctx|assert:foreign-all-false+base-true+invalid-require-before-range+avx10v2-default-off+apple-avx512-false+mask-diag"
    printf '%s' "$ctx"
}

# The native probe is the exact union of the X86 and Arm plans. Assert that
# every table row and generated boundary is assigned once, and that each
# partial-mask witness remains in the corresponding Lowered set.
_platform_isa_native_plan_self_check() {
    local arch rows name lowered_flag combined_rows="" expected_rows
    local combined_invalid="" expected_invalid registry combined_registry=""
    local lowered_names witness_count
    for arch in x86 arm; do
        rows=$(platform_isa_table_rows "$arch") || return 1
        lowered_names=""
        while IFS=$'\t' read -r name lowered_flag; do
            combined_rows="$combined_rows$name"$'\n'
            [ "$lowered_flag" = true ] && lowered_names="$lowered_names$name"$'\n'
        done <<<"$rows"
        _platform_isa_configure_arch "$arch" || return 1
        witness_count=$(grep -Fxc "$PLATFORM_ISA_P_TRUE" <<<"$lowered_names" || true)
        [ "$witness_count" -eq 1 ] \
            || { echo "FAIL: $arch kept-mask witness is not exactly one Lowered row" >&2; return 1; }
        witness_count=$(grep -Fxc "$PLATFORM_ISA_P_FALSE" <<<"$lowered_names" || true)
        [ "$witness_count" -eq 1 ] \
            || { echo "FAIL: $arch removed-mask witness is not exactly one Lowered row" >&2; return 1; }
        rows=$(platform_isa_invalid_immediate_rows "$arch") || return 1
        combined_invalid="$combined_invalid$rows"$'\n'
    done
    combined_rows=$(printf '%s' "$combined_rows" | LC_ALL=C sort)
    expected_rows=$(_platform_isa_table_fields \
        | awk -F'\t' '$1 == "X86" || $1 == "Arm" { print $2 }' \
        | LC_ALL=C sort)
    [ "$combined_rows" = "$expected_rows" ] \
        || { echo "FAIL: combined native plan does not contain the exact X86+Arm table rows" >&2; return 1; }
    combined_invalid=$(printf '%s' "$combined_invalid" | LC_ALL=C sort)
    registry=$(platform_isa_invalid_immediate_registry) || return 1
    expected_invalid=$(awk '/^(X86|Arm)\./' <<<"$registry" | LC_ALL=C sort)
    [ "$combined_invalid" = "$expected_invalid" ] \
        || { echo "FAIL: combined native plan does not contain the exact X86+Arm invalid-immediate registry" >&2; return 1; }
    for arch in x86 arm wasm; do
        rows=$(platform_isa_invalid_immediate_rows "$arch") || return 1
        combined_registry="$combined_registry$rows"$'\n'
    done
    combined_registry=$(printf '%s' "$combined_registry" | LC_ALL=C sort)
    registry=$(printf '%s\n' "$registry" | LC_ALL=C sort)
    [ "$combined_registry" = "$registry" ] \
        || { echo "FAIL: platform-ISA invalid-immediate registry has an unassigned row" >&2; return 1; }
}

# ── The native behaviour gate ────────────────────────────────────────────────
# Compile-time shards execute the X86 and Arm contracts diffed against real
# .NET. A shard omits every other family's table and exercise registration, so
# the transpiler cannot retain their helpers through a run-time selection arm.
#
# Runs, each ours-vs-oracle on stdout and exit code:
#   F  foreign host only: every row of ARCH, no mask. Witness: no `=True`.
#   S  native host, every shard with exhaustive exercises enabled. Witness: the
#      base family is true once it is Lowered.
#   M  always: every row, DN2CPP_CPU_FEATURES=none vs DOTNET_EnableHWIntrinsic=0.
#      Witnesses: no `=True`, and exactly one `effective=(none)` diag line.
#   P  native host, the two partial-mask witnesses in their one shard. The true
#      witness executes its helper; the false witness executes its throwing probe.
#   O  native x86 host, the AVX10.2 policy rows under the positive opt-in. The
#      oracle opts in only when the configured compiler has the helper surface;
#      otherwise both sides stay false and the nested throwing stubs are called.
# Rows that are not Lowered fold to false on our side whatever the CPU.
# _platform_isa_run_native_shard LABEL SHARD ENV ATTEMPT_PREFIX OUT ERR CMD...
# runs one generated native shard and sets _PI_SHARD_CODE. Each attempt keeps
# its own account; only the accepted attempt reaches the aggregate OUT and ERR.
_platform_isa_run_native_shard() {
    local label="$1" shard="$2" ours_env="$3" attempt_prefix="$4"
    local accepted_stdout="$5" accepted_stderr="$6"
    shift 6
    local attempt attempt_stdout attempt_stderr

    for attempt in 1 2 3; do
        attempt_stdout="$attempt_prefix.attempt$attempt.out"
        attempt_stderr="$attempt_prefix.attempt$attempt.log"
        # shellcheck disable=SC2086
        if (if [ -n "$ours_env" ]; then export $ours_env; fi
            run_bounded "$@") >"$attempt_stdout" 2>"$attempt_stderr"; then
            _PI_SHARD_CODE=0
        else
            _PI_SHARD_CODE=$?
        fi

        # Windows can sporadically fail to create the process without reaching
        # the binary. Keep every attempt account, but accept only one stdout.
        if [ "$DN2CPP_OS" != windows ] || [ "$_PI_SHARD_CODE" -ne 127 ] \
            || [ "$attempt" -eq 3 ]; then
            break
        fi
        echo "note: platform-ISA run $label shard $shard launch attempt $attempt exited 127 on Windows; retrying in ${attempt}s (stdout: $attempt_stdout, stderr: $attempt_stderr)" >&2
        sleep "$attempt"
    done

    cat "$attempt_stdout" >>"$accepted_stdout"
    cat "$attempt_stderr" >>"$accepted_stderr"
}

_platform_isa_native_retry_self_check() (
    local test_dir case_dir actual expected next_attempt attempt retry_notes
    test_dir=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp-platform-isa-retry.XXXXXX") \
        || { echo "FAIL: could not create the platform-ISA retry self-check directory" >&2; return 1; }
    trap 'rm -rf "$test_dir"' EXIT

    # The real runner is always called in a subshell. Keep the attempt number in
    # a file so this stub exercises that boundary instead of bypassing it.
    run_bounded() {
        local attempt code
        attempt=$(cat "$_PI_TEST_COUNTER")
        attempt=$((attempt + 1))
        printf '%s\n' "$attempt" >"$_PI_TEST_COUNTER"
        printf 'stdout-attempt-%s\n' "$attempt"
        printf 'stderr-attempt-%s\n' "$attempt" >&2
        code="${_PI_TEST_CODES[$((attempt - 1))]}"
        return "$code"
    }
    sleep() {
        printf '%s\n' "$1" >>"$_PI_TEST_SLEEPS"
    }

    # _pi_retry_case NAME OS ACCEPTED_CODE N_ATTEMPTS CODE...
    _pi_retry_case() {
        local name="$1" DN2CPP_OS="$2" accepted_code="$3" n_attempts="$4"
        shift 4
        local _PI_TEST_CODES=("$@")
        local _PI_TEST_COUNTER _PI_TEST_SLEEPS prefix accepted_stdout accepted_stderr
        case_dir="$test_dir/$name"
        mkdir -p "$case_dir"
        _PI_TEST_COUNTER="$case_dir/count"
        _PI_TEST_SLEEPS="$case_dir/sleeps"
        prefix="$case_dir/native"
        accepted_stdout="$case_dir/accepted.out"
        accepted_stderr="$case_dir/accepted.log"
        printf '0\n' >"$_PI_TEST_COUNTER"
        : >"$_PI_TEST_SLEEPS"
        printf 'prior-stdout\n' >"$accepted_stdout"
        printf 'prior-stderr\n' >"$accepted_stderr"

        _platform_isa_run_native_shard T test "" "$prefix" \
            "$accepted_stdout" "$accepted_stderr" stub-command \
            2>"$case_dir/retry.log"

        if [ "$_PI_SHARD_CODE" -ne "$accepted_code" ]; then
            echo "FAIL: platform-ISA retry self-check $name accepted exit $_PI_SHARD_CODE, expected $accepted_code" >&2
            return 1
        fi
        actual=$(cat "$_PI_TEST_COUNTER")
        if [ "$actual" -ne "$n_attempts" ]; then
            echo "FAIL: platform-ISA retry self-check $name ran $actual attempts, expected $n_attempts" >&2
            return 1
        fi

        expected=$'prior-stdout\n'"stdout-attempt-$n_attempts"
        actual=$(cat "$accepted_stdout")
        if [ "$actual" != "$expected" ]; then
            echo "FAIL: platform-ISA retry self-check $name accepted the wrong stdout attempt" >&2
            return 1
        fi
        expected=$'prior-stderr\n'"stderr-attempt-$n_attempts"
        actual=$(cat "$accepted_stderr")
        if [ "$actual" != "$expected" ]; then
            echo "FAIL: platform-ISA retry self-check $name accepted the wrong stderr attempt" >&2
            return 1
        fi

        for ((attempt = 1; attempt <= n_attempts; attempt++)); do
            actual=$(cat "$prefix.attempt$attempt.out" 2>/dev/null || true)
            [ "$actual" = "stdout-attempt-$attempt" ] \
                || { echo "FAIL: platform-ISA retry self-check $name did not retain attempt $attempt stdout" >&2; return 1; }
            actual=$(cat "$prefix.attempt$attempt.log" 2>/dev/null || true)
            [ "$actual" = "stderr-attempt-$attempt" ] \
                || { echo "FAIL: platform-ISA retry self-check $name did not retain attempt $attempt stderr" >&2; return 1; }
        done
        if [ "$n_attempts" -lt 3 ]; then
            next_attempt=$((n_attempts + 1))
            if [ -e "$prefix.attempt$next_attempt.out" ] \
                || [ -e "$prefix.attempt$next_attempt.log" ]; then
                echo "FAIL: platform-ISA retry self-check $name created an extra attempt" >&2
                return 1
            fi
        fi

        retry_notes=$(grep -c 'retrying in' "$case_dir/retry.log" || true)
        if [ "$retry_notes" -ne $((n_attempts - 1)) ]; then
            echo "FAIL: platform-ISA retry self-check $name wrote $retry_notes retry notes, expected $((n_attempts - 1))" >&2
            return 1
        fi
        case "$n_attempts" in
            1) expected="" ;;
            2) expected="1" ;;
            3) expected=$'1\n2' ;;
        esac
        actual=$(cat "$_PI_TEST_SLEEPS")
        if [ "$actual" != "$expected" ]; then
            echo "FAIL: platform-ISA retry self-check $name used unexpected retry delays" >&2
            return 1
        fi
    }

    _pi_retry_case windows-recovers windows 0 2 127 0 || return 1
    _pi_retry_case windows-other-exit windows 23 1 23 || return 1
    _pi_retry_case nonwindows-127 linux 127 1 127 || return 1
    _pi_retry_case windows-exhausts windows 127 3 127 127 127 || return 1
)

_platform_isa_run_arch() {
    local arch="$1" host_arch="$2" avx10v2_compiler="$3"
    local isa base
    isa=$(platform_isa_arch_name "$arch") || exit 1
    case "$arch" in
        x86) base="X86.X86Base" ;;
        arm) base="Arm.ArmBase" ;;
        *) echo "error: _platform_isa_run_arch takes x86 or arm, not '$arch'" >&2; exit 1 ;;
    esac
    _platform_isa_configure_arch "$arch" || exit 1
    : "${PLATFORM_ISA_P_MASK:?the gate must set PLATFORM_ISA_P_MASK}"
    : "${PLATFORM_ISA_P_KNOBS:?the gate must set PLATFORM_ISA_P_KNOBS}"
    : "${PLATFORM_ISA_P_TRUE:?the gate must set PLATFORM_ISA_P_TRUE}"
    : "${PLATFORM_ISA_P_FALSE:?the gate must set PLATFORM_ISA_P_FALSE}"

    local rows name lowered_flag all=() lowered=()
    rows=$(platform_isa_table_rows "$arch") || exit 1
    [ -n "$rows" ] || { echo "error: $PLATFORM_ISA_TABLE lists no $isa family" >&2; exit 1; }
    while IFS=$'\t' read -r name lowered_flag; do
        all+=("$name")
        [ "$lowered_flag" = true ] && lowered+=("$name")
    done <<<"$rows"
    local all_csv lowered_csv invalid_rows invalid_count
    all_csv=$(platform_isa_join , "${all[@]}")
    lowered_csv=$(platform_isa_join , ${lowered[@]+"${lowered[@]}"})
    invalid_rows=$(platform_isa_invalid_immediate_rows "$arch") || exit 1
    invalid_count=$(grep -c . <<<"$invalid_rows" || true)
    [ "$invalid_count" -gt 0 ] \
        || { echo "error: generated exercises list no $isa invalid-immediate boundary" >&2; exit 1; }

    local shard_names=() shard_outs=() shard_apps=() i
    for ((i = 0; i < ${#PLATFORM_ISA_BUILT_SHARDS[@]}; i++)); do
        if [ "${PLATFORM_ISA_BUILT_ARCHES[$i]}" = "$arch" ]; then
            shard_names+=("${PLATFORM_ISA_BUILT_SHARDS[$i]}")
            shard_outs+=("${PLATFORM_ISA_BUILT_OUTS[$i]}")
            shard_apps+=("${PLATFORM_ISA_BUILT_APPS[$i]}")
        fi
    done
    [ "${#shard_names[@]}" -gt 0 ] \
        || { echo "error: no compiled platform-ISA shard for $arch" >&2; exit 1; }

    local p_shard="" p_plan p_true_here p_false_here
    for ((i = 0; i < ${#shard_names[@]}; i++)); do
        p_plan=$(dotnet "${shard_apps[$i]}" --dump-plan) || exit 1
        p_true_here=$(grep -Fxc "row=$PLATFORM_ISA_P_TRUE" <<<"$p_plan" || true)
        p_false_here=$(grep -Fxc "row=$PLATFORM_ISA_P_FALSE" <<<"$p_plan" || true)
        if [ "$p_true_here" -ne 0 ] || [ "$p_false_here" -ne 0 ]; then
            [ "$p_true_here" -eq 1 ] && [ "$p_false_here" -eq 1 ] \
                || { echo "error: partial-mask witnesses span native ISA shards" >&2; exit 1; }
            [ -z "$p_shard" ] \
                || { echo "error: partial-mask witnesses occur in multiple native ISA shards" >&2; exit 1; }
            p_shard="${shard_names[$i]}"
        fi
    done
    [ -n "$p_shard" ] \
        || { echo "error: no native ISA shard contains the partial-mask witnesses" >&2; exit 1; }

    local native_host=0
    [ "$host_arch" = "$arch" ] && native_host=1
    local log_dir="$_GATE_RUN_LOG_DIR/$arch"
    mkdir -p "$log_dir"
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
            if [ -n "${_PI_RUN_ORACLE_OUTS[$i]}" ]; then
                _pi_run_diag_file "${_PI_RUN_LABELS[$i]} oracle" \
                    "${_PI_RUN_ORACLE_CODES[$i]}" "${_PI_RUN_ORACLE_OUTS[$i]}" \
                    "${_PI_RUN_ORACLE_LOGS[$i]}"
            fi
        done
    }

    # _pi_run LABEL ARGV OURS_ENV ORACLE_ENV — one arm: ours under OURS_ENV vs
    # `dotnet` under ORACLE_ENV (space-separated NAME=VALUE lists, may be empty).
    # Sets _PI_OURS / _PI_LOG; fails the gate on any stdout or exit-code diff.
    _pi_run() {
        local label="$1" argv="$2" ours_env="$3" oracle_env="$4"
        local ours_code=0 expected_code=0 ours_out expected_out oracle_log
        local shard out app chunk_code expected_chunk_code executed_shards=0
        local run_args=()
        _PI_LOG="$log_dir/$label.log"
        oracle_log="$log_dir/$label.oracle.log"
        ours_out="$log_dir/$label.out"
        expected_out="$log_dir/$label.oracle.out"
        : >"$ours_out"
        : >"$expected_out"
        : >"$_PI_LOG"
        : >"$oracle_log"
        for ((i = 0; i < ${#shard_names[@]}; i++)); do
            shard="${shard_names[$i]}"
            out="${shard_outs[$i]}"
            app="${shard_apps[$i]}"
            if [ "$label" = O ] && [ "$shard" != x86-avx10 ]; then
                continue
            fi
            if [ "$label" = P ] && [ "$shard" != "$p_shard" ]; then
                continue
            fi
            if [ "$label" = O ] || [ "$label" = P ]; then
                run_args=("$argv")
            else
                run_args=(all)
            fi
            case "$label" in
                F|M) run_args+=(--contract-only) ;;
            esac
            executed_shards=$((executed_shards + 1))
            # The native helper scopes the ISA environment in a subshell because
            # `env` cannot invoke run_bounded. Direct files avoid MSYS pipe loss.
            _platform_isa_run_native_shard "$label" "$shard" "$ours_env" \
                "$log_dir/$label.$shard" "$ours_out" "$_PI_LOG" \
                "./$out/$PLATFORM_ISA_PROJECT" "${run_args[@]}"
            chunk_code=$_PI_SHARD_CODE
            set +e
            # shellcheck disable=SC2086
            (if [ -n "$oracle_env" ]; then export $oracle_env; fi
             run_bounded dotnet "$app" "${run_args[@]}") \
                >>"$expected_out" 2>>"$oracle_log"; expected_chunk_code=$?
            set -e
            [ "$ours_code" -ne 0 ] || ours_code=$chunk_code
            [ "$expected_code" -ne 0 ] || expected_code=$expected_chunk_code
            if [ "$chunk_code" -ne 0 ] || [ "$expected_chunk_code" -ne 0 ]; then
                break
            fi
        done
        _PI_RUN_LABELS+=("$label")
        _PI_RUN_CODES+=("$ours_code")
        _PI_RUN_ORACLE_CODES+=("$expected_code")
        _PI_RUN_OUTS+=("$ours_out")
        _PI_RUN_ORACLE_OUTS+=("$expected_out")
        _PI_RUN_LOGS+=("$_PI_LOG")
        _PI_RUN_ORACLE_LOGS+=("$oracle_log")
        echo "-- run $label: argv=$argv shards=$executed_shards ours=[${ours_env:-<no ISA env>}] oracle=[${oracle_env:-<no ISA env>}]"
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

    # _pi_run_native LABEL ARGV ENV — dn2cpp-only boundary mode. Managed JITs
    # are not an oracle for out-of-contract ConstantExpected values.
    _pi_run_native() {
        local label="$1" argv="$2" ours_env="$3" ours_code ours_out
        local shard out shard_code=0
        _PI_LOG="$log_dir/$label.log"
        ours_out="$log_dir/$label.out"
        : >"$ours_out"
        : >"$_PI_LOG"
        ours_code=0
        for ((i = 0; i < ${#shard_names[@]}; i++)); do
            shard="${shard_names[$i]}"
            out="${shard_outs[$i]}"
            _platform_isa_run_native_shard "$label" "$shard" "$ours_env" \
                "$log_dir/$label.$shard" "$ours_out" "$_PI_LOG" \
                "./$out/$PLATFORM_ISA_PROJECT" all --invalid-immediates
            shard_code=$_PI_SHARD_CODE
            [ "$ours_code" -ne 0 ] || ours_code=$shard_code
            [ "$shard_code" -eq 0 ] || break
        done
        _PI_RUN_LABELS+=("$label")
        _PI_RUN_CODES+=("$ours_code")
        _PI_RUN_ORACLE_CODES+=("")
        _PI_RUN_OUTS+=("$ours_out")
        _PI_RUN_ORACLE_OUTS+=("")
        _PI_RUN_LOGS+=("$_PI_LOG")
        _PI_RUN_ORACLE_LOGS+=("")
        echo "-- run $label: argv=$argv shards=${#shard_names[@]} --invalid-immediates ours=[${ours_env:-<no ISA env>}]"
        if [ "$ours_code" -ne 0 ]; then
            echo "FAIL: run $label of the $isa invalid-immediate contract exited $ours_code" >&2
            gate_run_diag_once
            exit 1
        fi
        cat "$ours_out"
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
    _pi_require_count M "${#shard_names[@]}" "$n" "'effective=(none)' diag lines on stderr (one per shard process)"

    if [ "$native_host" -eq 1 ] && [ -n "$lowered_csv" ]; then
        _pi_run P "$PLATFORM_ISA_P_TRUE,$PLATFORM_ISA_P_FALSE" \
            "DN2CPP_CPU_FEATURES=$PLATFORM_ISA_P_MASK" "$PLATFORM_ISA_P_KNOBS"
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

    local violations
    _pi_run_native N "$all_csv" ""
    n=$(grep -c '^== invalid [A-Za-z0-9.]* ==$' <<<"$_PI_OURS" || true)
    _pi_require_count N "$invalid_count" "$n" "generated invalid-immediate sections"
    violations=$(platform_isa_invalid_immediate_violations "$_PI_OURS")
    if [ -n "$violations" ]; then
        echo "FAIL: run N invalid-immediate token/exception contract:" >&2
        printf '%s\n' "$violations" >&2
        gate_run_diag_once
        exit 1
    fi

    _pi_run_native NM "$all_csv" "DN2CPP_CPU_FEATURES=none"
    n=$(grep -c '^== invalid [A-Za-z0-9.]* ==$' <<<"$_PI_OURS" || true)
    _pi_require_count NM "$invalid_count" "$n" "generated invalid-immediate sections"
    block=$(platform_isa_contract_block "$_PI_OURS")
    n=$(grep -c '=True$' <<<"$block" || true)
    _pi_require_count NM 0 "$n" "'=True' rows under DN2CPP_CPU_FEATURES=none"
    n=$(grep -c '=ArgumentOutOfRangeException$' <<<"$_PI_OURS" || true)
    _pi_require_count NM 0 "$n" "AOOE results while every token is false"
    n=$(grep -c '=PlatformNotSupportedException$' <<<"$_PI_OURS" || true)
    _pi_require_count NM "$invalid_count" "$n" "PNSE results before immediate dispatch"
    violations=$(platform_isa_invalid_immediate_violations "$_PI_OURS")
    if [ -n "$violations" ]; then
        echo "FAIL: run NM invalid-immediate token/exception contract:" >&2
        printf '%s\n' "$violations" >&2
        gate_run_diag_once
        exit 1
    fi

    if [ "$native_host" -eq 0 ]; then
        local rosetta="" native_runs="S and P"
        if [ "$arch" = x86 ]; then
            native_runs="S, O and P"
            rosetta=" Rosetta is excluded on purpose: its CPUID exposes a feature set no shipping CPU has, and the oracle would be an emulated .NET rather than the host's."
        fi
        # One line: run-all-gates.sh's summary takes the FIRST line of the reason.
        gate_expected_partial "runs $native_runs (the $isa families' supported, opt-in where applicable, and partially-masked paths) have no reachable state on this ${host_arch:-$(uname -m)} host: no software installable here creates an $isa CPU, so nothing this gate could do makes an $isa family's IsSupported true on either side.$rosetta Structural and permanent, not an absent prerequisite — which is why this is not a gate_skip/gate_partial. The uncovered surface IS asserted for real by this same gate, gates/build-and-run-platform-isa-native.sh, run on an $isa host (the linux-x64 and windows-x64 lanes for x86, macos-arm64 for arm), where $native_runs run end to end. Runs F (every $isa row false and every $isa family throwing PlatformNotSupportedException, exactly as real .NET does here) and M (DN2CPP_CPU_FEATURES=none vs DOTNET_EnableHWIntrinsic=0, with one diag line per shard process) did run in this invocation and hold."
    fi
}

# platform_isa_native_gate — compile each bounded reachability shard, then
# execute the combined native architecture contracts. A cache entry is
# committed only after every shard and native/oracle run has passed.
platform_isa_native_gate() {
    [ "$#" -eq 0 ] \
        || { echo "error: platform_isa_native_gate takes no arguments" >&2; return 1; }
    _platform_isa_witness_normalization_self_check || return 1
    _platform_isa_native_plan_self_check || return 1
    _platform_isa_native_retry_self_check || return 1
    _platform_isa_clear_runtime_env || return 1

    local project="$PLATFORM_ISA_PROJECT" host_arch avx10v2_compiler=0
    local x86_ctx arm_ctx registry registry_csv registry_count ctx
    local corelib project_file shard arch managed out app surface shard_surfaces=""
    local expected_rows actual_rows expected_exercises actual_exercises
    local expected_invalid actual_invalid first_out="" i
    local plan plan_rows plan_exercises plan_invalid misplaced
    local expected_body_markers actual_body_markers exercise marker
    local key_inputs=()
    host_arch=$(platform_isa_host_arch)
    if [ "$host_arch" = x86 ] && platform_isa_avx10v2_compiler_capable; then
        avx10v2_compiler=1
    fi

    echo "== 1/4 Locating the real CoreLib =="
    corelib=$(locate_corelib)
    _CG_CORELIB="$corelib"
    echo "corlib: $corelib"

    echo "== 2/4 Building compile-time reachability shards =="
    project_file="samples/dotnet/$project/$project.csproj"
    PLATFORM_ISA_BUILT_SHARDS=()
    PLATFORM_ISA_BUILT_ARCHES=()
    PLATFORM_ISA_BUILT_OUTS=()
    PLATFORM_ISA_BUILT_APPS=()
    for shard in $PLATFORM_ISA_NATIVE_SHARDS; do
        arch=$(platform_isa_shard_arch "$shard") || return 1
        managed="artifacts/platformisaprobe-managed-$shard"
        out="artifacts/platformisaprobe-isa-native-$shard"
        # The suite prebuild covers only the default all-family variant. These
        # distinct IL reachability roots are part of this gate's subject.
        dotnet build "$project_file" -c "$CONFIG" --nologo -v q \
            -p:PlatformIsaShard="$shard" \
            -p:OutputPath="../../../$managed/" \
            -p:IntermediateOutputPath="../../../$managed/obj/" || return 1
        app="$managed/$project.dll"
        [ -f "$app" ] \
            || { echo "error: $shard build produced no $app" >&2; return 1; }
        PLATFORM_ISA_BUILT_SHARDS+=("$shard")
        PLATFORM_ISA_BUILT_ARCHES+=("$arch")
        PLATFORM_ISA_BUILT_OUTS+=("$out")
        PLATFORM_ISA_BUILT_APPS+=("$app")
        key_inputs+=("$app" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json")
    done

    echo "== 3/4 Transpiling shards and checking their exact coverage =="
    expected_exercises=$(_platform_isa_table_fields \
        | awk -F'\t' '$1 != "Wasm" && $4 == "true" && $2 !~ /\.[^.]+\.[^.]+$/ { print $2 }' \
        | LC_ALL=C sort)
    actual_rows=""
    actual_exercises=""
    actual_invalid=""
    for ((i = 0; i < ${#PLATFORM_ISA_BUILT_SHARDS[@]}; i++)); do
        shard="${PLATFORM_ISA_BUILT_SHARDS[$i]}"
        out="${PLATFORM_ISA_BUILT_OUTS[$i]}"
        app="${PLATFORM_ISA_BUILT_APPS[$i]}"
        invoke_cli "$app" -r "$corelib" -o "$out" || return 1
        surface=$(_gate_surface_lines "$out") || return 1
        shard_surfaces="$shard_surfaces$shard:$(printf '%s\n' "$surface" | shasum -a 256 | awk '{print $1}')|"
        [ -n "$first_out" ] || first_out="$out"
        plan=$(dotnet "$app" --dump-plan) || return 1
        plan_rows=$(sed -n 's/^row=//p' <<<"$plan" | LC_ALL=C sort)
        plan_exercises=$(sed -n 's/^exercise=//p' <<<"$plan" | LC_ALL=C sort)
        plan_invalid=$(sed -n 's/^invalid=//p' <<<"$plan" | LC_ALL=C sort)
        misplaced=$(comm -23 <(printf '%s\n' "$plan_exercises") <(printf '%s\n' "$plan_rows"))
        [ -z "$misplaced" ] \
            || { echo "FAIL: $shard registers exercises outside its table: $misplaced" >&2; return 1; }
        misplaced=$(comm -23 <(printf '%s\n' "$plan_invalid") <(printf '%s\n' "$plan_rows"))
        [ -z "$misplaced" ] \
            || { echo "FAIL: $shard registers invalid-immediate witnesses outside its table: $misplaced" >&2; return 1; }
        expected_body_markers=""
        while IFS= read -r exercise; do
            [ -n "$exercise" ] || continue
            while IFS= read -r marker; do
                expected_body_markers="$expected_body_markers$marker"$'\n'
            done < <(platform_isa_exercise_body_markers "$exercise")
        done <<<"$plan_exercises"
        expected_body_markers=$(printf '%s' "$expected_body_markers" | sed '/^$/d' | LC_ALL=C sort)
        actual_body_markers=""
        while IFS= read -r exercise; do
            [ -n "$exercise" ] || continue
            while IFS= read -r marker; do
                if grep -Fqx -- "// $marker" "$out/generated.h" "$out"/generated*.cpp; then
                    actual_body_markers="$actual_body_markers$marker"$'\n'
                fi
            done < <(platform_isa_exercise_body_markers "$exercise")
        done <<<"$expected_exercises"
        actual_body_markers=$(printf '%s' "$actual_body_markers" | sed '/^$/d' | LC_ALL=C sort)
        [ "$actual_body_markers" = "$expected_body_markers" ] \
            || { echo "FAIL: $shard generated exercise bodies escape their reachability shard" >&2; return 1; }
        actual_rows="$actual_rows$plan_rows"$'\n'
        actual_exercises="$actual_exercises$plan_exercises"$'\n'
        actual_invalid="$actual_invalid$plan_invalid"$'\n'
    done
    actual_rows=$(printf '%s' "$actual_rows" | sed '/^$/d' | LC_ALL=C sort)
    expected_rows=$(_platform_isa_table_fields \
        | awk -F'\t' '$1 == "X86" || $1 == "Arm" { print $2 }' \
        | LC_ALL=C sort)
    [ "$actual_rows" = "$expected_rows" ] \
        || { echo "FAIL: native shards do not assign every X86+Arm row exactly once" >&2; return 1; }
    actual_exercises=$(printf '%s' "$actual_exercises" | sed '/^$/d' | LC_ALL=C sort)
    [ "$actual_exercises" = "$expected_exercises" ] \
        || { echo "FAIL: native shards do not assign every Lowered X86+Arm top-level exercise exactly once" >&2; return 1; }
    actual_invalid=$(printf '%s' "$actual_invalid" | sed '/^$/d' | LC_ALL=C sort)
    expected_invalid=$(platform_isa_invalid_immediate_registry \
        | awk '/^(X86|Arm)\./' | LC_ALL=C sort)
    [ "$actual_invalid" = "$expected_invalid" ] \
        || { echo "FAIL: native shards do not assign every X86+Arm invalid-immediate witness exactly once" >&2; return 1; }

    x86_ctx=$(_platform_isa_arch_context x86 "$avx10v2_compiler") || return 1
    arm_ctx=$(_platform_isa_arch_context arm "$avx10v2_compiler") || return 1
    registry=$(platform_isa_invalid_immediate_registry) || return 1
    registry_csv=$(tr '\n' ',' <<<"$registry")
    registry_csv=${registry_csv%,}
    registry_count=$(grep -c . <<<"$registry" || true)
    [ "$registry_count" -gt 0 ] \
        || { echo "error: generated exercises list no invalid-immediate boundaries" >&2; return 1; }
    ctx="platform_isa_native|host:${host_arch:-other}|$_CG_CORELIB"
    ctx="$ctx|shards:$PLATFORM_ISA_NATIVE_SHARDS|surfaces:$shard_surfaces"
    ctx="$ctx|invalid-registry-count:$registry_count|invalid-registry:$registry_csv"
    ctx="$ctx|$x86_ctx|$arm_ctx$(_gate_ctx_extras)"
    while IFS= read -r app; do key_inputs+=("$app"); done < <(_gate_extra_inputs)
    if gate_cache_check "$first_out" "$ctx" "${key_inputs[@]}"; then
        gate_cache_hit_msg
        return 0
    fi

    echo "== 4/4 Compiling bounded shards and running the X86 + Arm contracts (exact diff vs real .NET) =="
    for ((i = 0; i < ${#PLATFORM_ISA_BUILT_SHARDS[@]}; i++)); do
        echo "-- compile ${PLATFORM_ISA_BUILT_SHARDS[$i]}"
        compile_console "${PLATFORM_ISA_BUILT_OUTS[$i]}" "$project"
    done
    gate_run_logs_init "platform_isa_native" "platform-isa-native"
    _platform_isa_run_arch x86 "$host_arch" "$avx10v2_compiler"
    _platform_isa_run_arch arm "$host_arch" "$avx10v2_compiler"
    gate_cache_commit
}
