#!/usr/bin/env bash
# Exercise the real wrappers without SDK, compiler, runtime or cache prerequisites.
set -euo pipefail
cd "$(dirname "$0")/.."
script="$PWD/gates/test-gate-output-shell.sh"
_DN2CPP_GATE_ROOT="$PWD"
source gates/_python.sh
eval "$(sed -n '/^gate_capture() {/,/^}/p; /^gate_capture_ok() {/,/^}/p; /^gate_assert_text() {/,/^}/p; /^_gate_helpers_hash() {/,/^}/p; /^gate_output() {/,/^}/p; /^corelib_diff_gate() {/,/^}/p; /^corelib_diff_abort_gate() {/,/^}/p; /^_corelib_diff_gate() {/,/^}/p; /^corelib_diff_split_gate() {/,/^}/p; /^corelib_subset_gate() {/,/^}/p; /^corelib_freeze_gate() {/,/^}/p; /^wasm_corelib_diff_gate() {/,/^}/p' gates/_common.sh)"

if [ "${1:-}" = --case ]; then
    scenario="$2" fixture="$3" py="$4"
    DN2CPP_OS=linux
    EXE_EXT=
    export DN2CPP_PYTHON="$py"
    _corelib_gate_out() { printf '%s\n' "$fixture/run"; }
    _corelib_gate_core() { mkdir -p "$2"; _CG_APP=fixture.dll; _CG_CORELIB=Fixture; }
    _corelib_gate_check() { return 1; }
    gate_cache_check() { return 1; }
    gate_cache_commit() { : > "$fixture/committed"; }
    _gate_ctx_extras() { :; }
    _gate_run_argv() { _GATE_RUN_ARGV=(); }
    _gate_scratch_cleanup() { :; }
    _gate_extra_asserts() { :; }
    compile_console() { :; }
    compile_console_wasm() { :; }
    locate_corelib_cross_posix() { printf 'fixture-corelib\n'; }
    run_bounded() {
        local side=native
        [ "$1" != dotnet ] || side=oracle
        cat "$fixture/$side.input"
        cat "$fixture/$side.error" >&2
        return "$(cat "$fixture/$side.code")"
    }
    case "$scenario" in
        status4-cache)
            eval "$(sed -n '/^gate_cache_check() {/,/^}/p; /^gate_cache_commit() {/,/^}/p' gates/_common.sh)"
            DN2CPP_GATE_SCRIPT="$script"
            DN2CPP_GATE_CACHE=1
            DN2CPP_GATE_CACHE_DEBUG=1
            mkdir -p "$fixture/status4-cache"
            cd "$fixture/status4-cache"
            _gate_surface_lines() { printf 'fixture-surface\n'; }
            _gate_helpers_hash() { printf 'fixture-helpers\n'; }
            _gate_runtime_env_term() { :; }
            _gate_dotnet_env_term() { :; }
            _gate_runtime_hash() { printf 'fixture-runtime\n'; }
            _gate_dotnet_hash() { printf 'fixture-dotnet\n'; }
            first_line() { printf '%s\n' "$1"; }
            cc() { printf 'fixture-compiler\n'; }
            unset WASM IOS_SIM IOS_DEV DN2CPP_RETRY_STATUS4
            for selector in auto 1; do
                if [ "$selector" = auto ]; then unset DN2CPP_RETRY_STATUS4; else DN2CPP_RETRY_STATUS4=1; fi
                gate_cache_check run custom-context || :
                gate_cache_commit
                gate_cache_check run custom-context
                DN2CPP_RETRY_STATUS4=1 gate_cache_check run custom-context
                if DN2CPP_RETRY_STATUS4=0 gate_cache_check run custom-context; then
                    echo 'FAIL: retry-disabled run reused a retry-enabled cache receipt' >&2
                    exit 1
                fi
            done
            ;;
        status4-*)
            source gates/_ordinary-reflection.sh
            eval "$(sed -n '/^_gate_extra_asserts() {/,/^}/p; /^_corelib_gate_check() {/,/^}/p; /^run_bounded() {/,/^}/p; /^gate_run_logs_cleanup() {/,/^}/p' gates/_common.sh)"
            unset DN2CPP_RETRY_STATUS4
            DN2CPP_OS=windows
            DN2CPP_RUN_WATCHDOG_SECS=17
            CONFIG=Debug
            retry_root="$fixture/$scenario"
            mkdir -p "$retry_root/attempts"
            _CG_CORELIB_IN="$retry_root/System.Private.CoreLib.dll"
            native_first=4 oracle_first=4 second_code=0
            case "$scenario" in
                status4-repeated|status4-direct-repeated) second_code=4 ;;
                status4-other) native_first=7; oracle_first=7 ;;
                status4-second-error) oracle_first=0; second_code=7 ;;
                status4-disabled) DN2CPP_RETRY_STATUS4=0; oracle_first=0 ;;
                status4-direct-disabled) DN2CPP_RETRY_STATUS4=0 ;;
                status4-nonwindows) DN2CPP_OS=linux; oracle_first=0 ;;
                status4-unrelated) oracle_first=0 ;;
                status4-direct-enabled) DN2CPP_RETRY_STATUS4=1 ;;
            esac
            _corelib_gate_out() { printf '%s/run\n' "$retry_root"; }
            _gate_extra_inputs() { :; }
            gate_cache_check() { printf '%s\n' "${DN2CPP_RETRY_STATUS4:-1}" >> "$retry_root/contexts"; return 1; }
            gate_build_fixture() { mkdir -p "$2"; }
            invoke_cli() { :; }
            run_with_watchdog() {
                test "$1" = 17 || return 91
                shift
                local side=native variant=main counter count=0 code
                if [ "$1" = dotnet ]; then side=oracle; shift; fi
                shift
                if [ "$#" -gt 0 ]; then
                    test "$#" = 2 && test "$1" = 'before fixture' && test "$2" = '' || return 91
                    variant=extra
                fi
                counter="$retry_root/attempts/${DN2CPP_OUT_SUFFIX:-raw}-$side-$variant"
                [ ! -f "$counter" ] || count=$(cat "$counter")
                count=$((count + 1))
                printf '%s\n' "$count" > "$counter"
                code="$second_code"
                if [ "$count" -eq 1 ]; then
                    if [ "$side" = native ]; then code="$native_first"; else code="$oracle_first"; fi
                fi
                printf 'payload\000\r\n\n'
                if [[ "$scenario" == status4-direct-* ]]; then
                    printf 'attempt %s\000\r\n\n' "$count" >&2
                else
                    printf 'attempt %s\n' "$count" >&2
                fi
                return "$code"
            }
            if [ "$scenario" = status4-fixture ]; then
                gate_extra_asserts() {
                    gate_capture_ok "$1/extra-native" run_bounded "$1/OrdinaryReflectionTypeLeaves$EXE_EXT" 'before fixture' '' || return 1
                    gate_capture_ok "$1/extra-oracle" run_bounded dotnet "$_CG_APP" 'before fixture' '' || return 1
                }
                for mode in '' -diet -native; do
                    flags=(--no-ildiet)
                    case "$mode" in
                        -diet) flags=() ;;
                        -native) flags+=(--no-metadata-compression) ;;
                    esac
                    DN2CPP_OUT_SUFFIX="$mode" ordinary_fixture_diff_gate ReflectTypes OrdinaryReflectionTypeLeaves ${flags[@]+"${flags[@]}"}
                    test "${DN2CPP_RETRY_STATUS4+x}" != x
                done
                # Commands outside the final-run API retain their original status.
                single_shot() { printf 'ran\n' >> "$retry_root/single-shot-runs"; return 4; }
                if DN2CPP_RETRY_STATUS4=1 gate_capture_ok "$retry_root/single-shot" single_shot; then
                    echo 'FAIL: status 4 passed a success-only capture' >&2
                    exit 1
                fi
                test "$(cat "$retry_root/single-shot.status")" = 4
                test "$(cat "$retry_root/single-shot-runs")" = ran
            elif [[ "$scenario" == status4-direct-* ]]; then
                _CG_OUT="$retry_root/run"
                mkdir -p "$_CG_OUT"
                direct_code=0
                run_bounded native > "$_CG_OUT/direct.stdout" 2> "$_CG_OUT/direct.stderr" || direct_code=$?
                oracle_code=0
                run_bounded dotnet fixture.dll > "$_CG_OUT/direct-oracle.stdout" 2> "$_CG_OUT/direct-oracle.stderr" || oracle_code=$?
                expected_code=0 expected_attempt=2
                case "$scenario" in
                    status4-direct-disabled) expected_code=4; expected_attempt=1 ;;
                    status4-direct-repeated) expected_code=125 ;;
                esac
                test "$direct_code" = "$expected_code"
                test "$oracle_code" = "$expected_code"
                printf 'payload\000\r\n\n' > "$retry_root/expected.stdout"
                printf 'attempt %s\000\r\n\n' "$expected_attempt" > "$retry_root/expected.stderr"
                for stream in stdout stderr; do
                    cmp "$_CG_OUT/direct.$stream" "$retry_root/expected.$stream"
                    cmp "$_CG_OUT/direct.$stream" "$_CG_OUT/direct-oracle.$stream"
                done
            elif [ "$scenario" = status4-split ]; then
                corelib_diff_split_gate OtherFixture
                test "$(cat "$retry_root/run/native.stderr")" = 'attempt 2'
                test "$(cat "$retry_root/run/oracle.stderr")" = 'attempt 2'
            elif [[ "$scenario" == status4-cleanup* ]]; then
                _GATE_RUN_LOG_DIR="$retry_root/run"
                _GATE_RUN_LOG_LABEL=fixture
                _GATE_EXIT_RC=0
                capture_prefix="$_GATE_RUN_LOG_DIR/native"
                if [ "$scenario" = status4-cleanup-nested ]; then
                    capture_prefix="$_GATE_RUN_LOG_DIR/x64/native"
                fi
                gate_capture_ok "$capture_prefix" run_bounded native 2> "$retry_root/capture.log"
                cmp "$capture_prefix.retry.log" "$retry_root/capture.log"
                gate_run_logs_cleanup
                test -d "$_GATE_RUN_LOG_DIR"
                test -s "$capture_prefix.retry.log"
                _GATE_RUN_LOG_DIR="$retry_root/no-retries"
                mkdir -p "$_GATE_RUN_LOG_DIR/x64"
                printf 'ordinary output\n' > "$_GATE_RUN_LOG_DIR/x64/native.stdout"
                gate_run_logs_cleanup
                test ! -d "$_GATE_RUN_LOG_DIR"
                _GATE_RUN_LOG_DIR="$retry_root/scan-failure"
                mkdir -p "$_GATE_RUN_LOG_DIR"
                (
                    find() { return 1; }
                    gate_run_logs_cleanup
                    test -d "$_GATE_RUN_LOG_DIR"
                )
            else
                subject=OrdinaryReflectionTypeLeaves
                [ "$scenario" != status4-unrelated ] || subject=OtherFixture
                if [ "$scenario" = status4-oracle-hook ]; then
                    gate_run_oracle() { run_bounded dotnet "$@"; }
                fi
                ordinary_fixture_diff_gate ReflectTypes "$subject" --no-ildiet
            fi
            ;;
        brotli-reference)
            source gates/_compression-reference.sh
            resolve_net10_corelib() { printf '/runtime/%s/System.Private.CoreLib.dll\n' "$reference_version"; }
            for reference_version in 10.0.0 10.0.13 10.0.14 10.0.20 9.0.99 11.0.0 10.0.14-preview.1 malformed; do
                expected=1
                case "$reference_version" in 10.0.14|10.0.20) expected=0 ;; esac
                if compression_require_brotli_reference > "$fixture/reference.stdout" 2> "$fixture/reference.stderr"; then
                    actual=0
                else
                    actual=$?
                fi
                test "$actual" = "$expected" || {
                    echo "FAIL: Brotli reference $reference_version returned $actual, expected $expected" >&2
                    exit 1
                }
            done
            resolve_net10_corelib() { return 1; }
            if compression_require_brotli_reference; then
                echo 'FAIL: accepted an unresolved Brotli reference' >&2
                exit 1
            fi
            ;;
        brotli-security-*)
            source gates/_compression-reference.sh
            cat > "$fixture/native.stdout" <<'EOF'
unchanged prefix
Brotli invalid block switch oneshot rejected: True
Brotli invalid block switch chunked rejected: True
Brotli invalid block switch buffered rejected: True
brotli-invalid-block-switch-stream: threw=True type=InvalidOperationException
EOF
            cp "$fixture/native.stdout" "$fixture/oracle.stdout"
            case "$scenario" in
                brotli-security-missing) sed '/chunked/d' "$fixture/native.stdout" > "$fixture/oracle.stdout" ;;
                brotli-security-false) sed 's/rejected: True/rejected: False/' "$fixture/native.stdout" > "$fixture/oracle.stdout" ;;
                brotli-security-extra) printf 'unexpected record\n' >> "$fixture/native.stdout" ;;
                brotli-security-crlf) sed $'s/$/\r/' "$fixture/native.stdout" > "$fixture/oracle.stdout" ;;
            esac
            compression_assert_brotli_security "$fixture"
            ;;
        abort-diff|abort-no-leak|abort-wrong-host|abort-cache)
            [ "$scenario" = abort-wrong-host ] || DN2CPP_OS=windows
            DN2CPP_GATE_SCRIPT="$script"
            : > "$fixture/abort-contexts"
            _corelib_gate_check() { printf '%s\n' "$2" >> "$fixture/abort-contexts"; return 1; }
            corelib_diff_abort_gate Fixture payload 'Unhandled exception. System.InvalidOperationException: declared abort'
            test "$DN2CPP_GATE_SCRIPT" = "$script"
            if [ "$scenario" = abort-cache ]; then
                printf 'other witness\n' > "$fixture/native.input"
                cp "$fixture/native.input" "$fixture/oracle.input"
                printf '%s\n' 'Unhandled exception. System.InvalidOperationException: other abort' > "$fixture/native.error"
                cp "$fixture/native.error" "$fixture/oracle.error"
                corelib_diff_abort_gate Fixture 'other witness' 'Unhandled exception. System.InvalidOperationException: other abort' --fixture-argument
                for shape in first second; do
                    if [ "$shape" = first ]; then
                        witness='payload|diagnostic:tail'; diagnostic=after
                    else
                        witness=payload; diagnostic='tail|diagnostic:after'
                    fi
                    printf '%s\n' "$witness" > "$fixture/native.input"
                    cp "$fixture/native.input" "$fixture/oracle.input"
                    printf '%s\n' "$diagnostic" > "$fixture/native.error"
                    cp "$fixture/native.error" "$fixture/oracle.error"
                    corelib_diff_abort_gate Fixture "$witness" "$diagnostic"
                done
                # The same physical host can select a different target policy.
                # A prior Windows abort cache receipt must not bypass Linux rejection.
                DN2CPP_OS=linux
                if corelib_diff_abort_gate Fixture "$witness" "$diagnostic"; then
                    echo 'FAIL: host-only policy change accepted Windows status 127' >&2; exit 99
                fi
                DN2CPP_OS=windows
                printf 'different oracle diagnostic\n' > "$fixture/oracle.error"
                corelib_diff_abort_gate Fixture "$witness" "$diagnostic" \
                    --oracle-diagnostic 'different oracle diagnostic'
                gate_python - "$fixture/abort-contexts" <<'PY'
from pathlib import Path
import sys
contexts = Path(sys.argv[1]).read_bytes().splitlines()
assert len(contexts) == 6 and len(set(contexts)) == 6
assert contexts[5] == contexts[3].replace(b"|oracle-diagnostic:21:tail|diagnostic:after", b"|oracle-diagnostic:27:different oracle diagnostic")
assert contexts[4] == contexts[3].replace(b"|host:windows|", b"|host:linux|")
assert b'|status:abort|host:windows|witness:7:payload|diagnostic:' in contexts[0]
assert b'|status:abort|host:windows|witness:13:other witness|diagnostic:' in contexts[1]
assert b'other abort' in contexts[1] and b'--fixture-argument' in contexts[1]
PY
            fi
            if [ "$scenario" = abort-no-leak ]; then
                rm "$fixture/committed"
                if corelib_diff_gate Fixture; then
                    echo 'FAIL: abort policy leaked into ordinary parity' >&2; exit 99
                fi
                test ! -e "$fixture/committed"
            fi
            ;;
        capture)
            fixture_command() { printf 'a\0b\n\n'; printf 'error\r\n\n' >&2; return 7; }
            gate_capture "$fixture/captured" fixture_command
            case $- in *e*) ;; *) exit 91 ;; esac
            printf 'a\0b\n\n' > "$fixture/want.stdout"
            printf 'error\r\n\n' > "$fixture/want.stderr"
            cmp "$fixture/captured.stdout" "$fixture/want.stdout"
            cmp "$fixture/captured.stderr" "$fixture/want.stderr"
            test "$(cat "$fixture/captured.status")" = 7
            set +e
            gate_capture "$fixture/timeout" bash -c 'exit 124'
            case $- in *e*) exit 92 ;; esac
            test "$(cat "$fixture/timeout.status")" = 124 || exit 93
            set -e
            gate_capture "$fixture/missing" dn2cpp_fixture_command_that_does_not_exist
            test "$(cat "$fixture/missing.status")" = 127
            gate_capture "$fixture/options" bash -c 'cd /; set +u; exit 3'
            test "$(cat "$fixture/options.status")" = 3
            test "$PWD/gates/test-gate-output-shell.sh" = "$script"
            ;;
        weak-final-*)
            printf 'prefix\r\nimmortal weak targets: True\r\n' > "$fixture/weak.raw"
            if [ "$scenario" = weak-final-extra-tail ]; then
                printf 'unexpected tail\r\n' >> "$fixture/weak.raw"
            fi
            printf 'prefix\r\n' > "$fixture/weak.before"
            printf '%s\n' 'immortal weak targets: True' > "$fixture/weak.final"
            gate_output suffix "$fixture/weak.raw" "$fixture/weak.final" --policy crlf
            gate_output select "$fixture/weak.raw" "$fixture/weak.prefix" --before 'immortal weak targets: True' --policy crlf
            gate_output compare "$fixture/weak.prefix" "$fixture/weak.before"
            ;;
        range-*)
            start=metadata-assembly-begin
            end=metadata-assembly-end
            if [ "$scenario" = range-reflect ]; then
                start='== runtime delegate method identities =='
                end='runtime delegate method identities end'
            fi
            case "$scenario" in
                range-missing) printf '%s\nbody\n' "$start" > "$fixture/range.raw" ;;
                range-duplicate) printf '%s\n%s\nbody\n%s\n' "$start" "$start" "$end" > "$fixture/range.raw" ;;
                range-reversed) printf '%s\nbody\n%s\n' "$end" "$start" > "$fixture/range.raw" ;;
                *) printf 'head\r\n%s\r\npayload\000\r\r\n%s\r\ntail\n' "$start" "$end" > "$fixture/range.raw" ;;
            esac
            gate_output select "$fixture/range.raw" "$fixture/range.selected" \
                --from "$start" --through "$end" --policy crlf
            printf '%s\r\npayload\000\r\r\n%s\r\n' "$start" "$end" > "$fixture/range.expected"
            gate_output compare "$fixture/range.selected" "$fixture/range.expected"
            ;;
        isa-helpers)
            source gates/_platform_isa.sh
            _platform_isa_witness_normalization_self_check
            _platform_isa_native_plan_self_check
            _platform_isa_native_retry_self_check
            ;;
        isa-stack-reserve)
            source gates/_platform_isa.sh
            eval "$(sed -n '/^is_msvc_compiler() {/,/^}/p' gates/_common.sh)"
            # Observe the probe's build context before any SDK/native work, then
            # fail the gate to check that even its failure path restores callers.
            _platform_isa_witness_normalization_self_check() {
                printf '%s\n' "${DN2CPP_EXTRA_LINK_FLAGS:-}" > "$fixture/isa-link-flags"
                return 1
            }
            check_isa_stack_scope() {
                local DN2CPP_OS="$1" CMAKE_CXX_COMPILER="$2"
                local DN2CPP_EXTRA_LINK_FLAGS="$3" expected="$4"
                local DN2CPP_PLATFORM_ISA_FULL
                for DN2CPP_PLATFORM_ISA_FULL in 0 1; do
                    if platform_isa_native_gate; then
                        echo 'FAIL: ISA probe ignored a failed prerequisite check' >&2
                        return 1
                    fi
                    test "$(cat "$fixture/isa-link-flags")" = "$expected"
                    test "$DN2CPP_EXTRA_LINK_FLAGS" = "$3"
                done
            }
            check_isa_stack_scope windows cl '' '-STACK:8388608'
            check_isa_stack_scope windows cl.exe '-DEBUG -OPT:REF' '-DEBUG -OPT:REF -STACK:8388608'
            check_isa_stack_scope windows 'C:\MSVC\cl.exe' '-DEBUG' '-DEBUG -STACK:8388608'
            check_isa_stack_scope windows clang++ '-Wl,--gc-sections' '-Wl,--gc-sections'
            check_isa_stack_scope linux cl '-Wl,--gc-sections' '-Wl,--gc-sections'
            check_isa_stack_scope macos clang++ '-Wl,-dead_strip' '-Wl,-dead_strip'
            ;;
        cache-policy)
            _DN2CPP_GATE_ROOT="$fixture/cache-root"
            mkdir -p "$_DN2CPP_GATE_ROOT/gates" "$_DN2CPP_GATE_ROOT/docs"
            printf 'shell helper\n' > "$_DN2CPP_GATE_ROOT/gates/_fixture.sh"
            printf 'python helper\n' > "$_DN2CPP_GATE_ROOT/gates/gate-output.py"
            printf 'ISA parser\n' > "$_DN2CPP_GATE_ROOT/gates/platform-isa-output.py"
            printf 'policy\n' > "$_DN2CPP_GATE_ROOT/docs/GATE-OUTPUT.md"
            cd "$fixture"
            first=$(_gate_helpers_hash)
            printf 'python changed\n' >> "$_DN2CPP_GATE_ROOT/gates/gate-output.py"
            second=$(_gate_helpers_hash)
            test "$first" != "$second"
            printf 'ISA parser changed\n' >> "$_DN2CPP_GATE_ROOT/gates/platform-isa-output.py"
            isa_changed=$(_gate_helpers_hash)
            test "$second" != "$isa_changed"
            printf 'policy changed\n' >> "$_DN2CPP_GATE_ROOT/docs/GATE-OUTPUT.md"
            third=$(_gate_helpers_hash)
            test "$isa_changed" != "$third"
            mv "$_DN2CPP_GATE_ROOT/gates/gate-output.py" "$fixture/helper.saved"
            if _gate_helpers_hash; then exit 96; fi
            mv "$fixture/helper.saved" "$_DN2CPP_GATE_ROOT/gates/gate-output.py"
            mv "$_DN2CPP_GATE_ROOT/gates/platform-isa-output.py" "$fixture/isa.saved"
            if _gate_helpers_hash; then exit 98; fi
            mv "$fixture/isa.saved" "$_DN2CPP_GATE_ROOT/gates/platform-isa-output.py"
            rm "$_DN2CPP_GATE_ROOT/docs/GATE-OUTPUT.md"
            if _gate_helpers_hash; then exit 97; fi
            ;;
        literal) gate_assert_text "$fixture/native.input" payload ;;
        nonroot)
            cd "$fixture"
            gate_output status 0 0
            gate_output compare native.input oracle.input
            ;;
        capture-ok) gate_capture_ok "$fixture/ok" run_bounded native ;;
        capture-no-command) gate_capture "$fixture/no-command" ;;
        capture-stale-status)
            : > "$fixture/stale.stdout"
            printf '0\n' > "$fixture/stale.status"
            mkdir "$fixture/stale.stderr"
            fixture_command() { : > "$fixture/stale-command-ran"; }
            if gate_capture "$fixture/stale" fixture_command; then
                echo 'FAIL: capture accepted an unwritable stderr destination' >&2
                exit 94
            fi
            test ! -e "$fixture/stale.status"
            test ! -e "$fixture/stale-command-ran"
            ;;
        capture-write-error)
            mkdir "$fixture/blocked.stdout"
            gate_capture "$fixture/blocked" true
            ;;
        file-hook)
            gate_capture_diff_files() {
                test "$2" = Fixture
                cmp "$3" "$fixture/native.input"
                cmp "$4" "$fixture/oracle.input"
                : > "$fixture/file-hook"
            }
            gate_capture_diff_output() { echo 'legacy hook must not supersede file hook' >&2; return 98; }
            corelib_diff_gate Fixture
            test -f "$fixture/file-hook"
            ;;
        legacy-hook)
            gate_capture_diff_output() {
                test "$2" = Fixture
                test "$3" = payload
                test "$4" = payload
                : > "$fixture/legacy-hook"
            }
            corelib_diff_gate Fixture
            test -f "$fixture/legacy-hook"
            ;;
        diff) corelib_diff_gate Fixture ;;
        split) corelib_diff_split_gate Fixture ;;
        subset) corelib_subset_gate Fixture payload ;;
        freeze) corelib_freeze_gate Fixture "$fixture/oracle.input" ;;
        wasm) wasm_corelib_diff_gate Fixture ;;
        wasm-finalizers|wasm-finalizers-linux)
            [ "$scenario" = wasm-finalizers-linux ] || DN2CPP_OS=windows
            wasm_corelib_diff_gate Finalizers
            ;;
        wasm-cache-host)
            DN2CPP_OS=windows
            : > "$fixture/wasm-contexts"
            gate_cache_check() { printf '%s\n' "$2" >> "$fixture/wasm-contexts"; return 1; }
            wasm_corelib_diff_gate Finalizers
            DN2CPP_OS=linux
            if wasm_corelib_diff_gate Finalizers; then
                echo 'FAIL: Linux accepted Windows finalizer status 127' >&2; exit 99
            fi
            first=$(sed -n '1p' "$fixture/wasm-contexts")
            second=$(sed -n '2p' "$fixture/wasm-contexts")
            case "$first" in *'|host:windows') ;; *) exit 98 ;; esac
            case "$second" in *'|host:linux') ;; *) exit 97 ;; esac
            test "${first%|host:windows}" = "${second%|host:linux}"
            ;;
        *) echo "unknown fixture: $scenario" >&2; exit 99 ;;
    esac
    exit 0
fi

py="${1:-${DN2CPP_PYTHON:-}}"
fixture="$(mktemp -d)"
trap 'rm -rf "$fixture"' EXIT
run_case() {
    local expected="$1" scenario="$2" status
    rm -f "$fixture/committed"
    set +e
    bash "$script" --case "$scenario" "$fixture" "$py" > "$fixture/case.stdout" 2> "$fixture/case.stderr"
    status=$?
    set -e
    if [ "$status" -ne "$expected" ]; then
        cat "$fixture/case.stdout" "$fixture/case.stderr" >&2
        echo "FAIL: $scenario returned $status, expected $expected" >&2
        exit 1
    fi
    if [ "$expected" -ne 0 ] && [ -e "$fixture/committed" ]; then
        echo "FAIL: $scenario cached a failed observation" >&2
        exit 1
    fi
}
reset_inputs() {
    printf 'payload\n' > "$fixture/native.input"
    cp "$fixture/native.input" "$fixture/oracle.input"
    : > "$fixture/native.error"
    : > "$fixture/oracle.error"
    printf '0\n' > "$fixture/native.code"
    printf '0\n' > "$fixture/oracle.code"
}
run_case 0 capture
run_case 0 brotli-reference
run_case 0 brotli-security-valid
run_case 0 brotli-security-crlf
run_case 1 brotli-security-missing
run_case 1 brotli-security-false
run_case 1 brotli-security-extra
run_case 1 capture-no-command
run_case 1 capture-write-error
run_case 0 capture-stale-status
reset_inputs
run_case 0 weak-final-valid
run_case 1 weak-final-extra-tail
run_case 0 range-multiassembly
run_case 0 range-reflect
run_case 1 range-missing
run_case 1 range-duplicate
run_case 1 range-reversed
run_case 0 isa-helpers
run_case 0 isa-stack-reserve
run_case 0 cache-policy
run_case 0 literal
printf 'payload\r\n' > "$fixture/native.input"
run_case 0 literal
printf 'payload\n\n' > "$fixture/native.input"
run_case 1 literal
reset_inputs
run_case 0 nonroot
run_case 0 capture-ok
printf '7\n' > "$fixture/native.code"
run_case 1 capture-ok
reset_inputs
run_case 0 diff
printf 'payload' > "$fixture/oracle.input"
run_case 1 diff
printf 'payload\r\n' > "$fixture/oracle.input"
run_case 1 diff
printf 'payload\0\n\n' > "$fixture/native.input"
cp "$fixture/native.input" "$fixture/oracle.input"
run_case 0 file-hook
printf 'payload\n\n' > "$fixture/oracle.input"
run_case 1 diff
reset_inputs
run_case 0 legacy-hook
printf '7\n' > "$fixture/native.code"
run_case 1 diff
cp "$fixture/native.code" "$fixture/oracle.code"
run_case 0 diff
printf '127\n' > "$fixture/native.code"
printf '127\n' > "$fixture/oracle.code"
run_case 1 diff
printf '%s\n' 'Unhandled exception. System.InvalidOperationException: declared abort' > "$fixture/native.error"
cp "$fixture/native.error" "$fixture/oracle.error"
run_case 0 abort-diff
run_case 0 abort-no-leak
run_case 1 abort-wrong-host
printf 'command not found\n' > "$fixture/native.error"
run_case 1 abort-diff
cp "$fixture/oracle.error" "$fixture/native.error"
printf '124\n' > "$fixture/native.code"
printf '124\n' > "$fixture/oracle.code"
run_case 1 abort-diff
reset_inputs
printf '127\n' > "$fixture/native.code"
cp "$fixture/native.code" "$fixture/oracle.code"
printf '%s\n' 'Unhandled exception. System.InvalidOperationException: declared abort' > "$fixture/native.error"
cp "$fixture/native.error" "$fixture/oracle.error"
run_case 0 abort-cache
reset_inputs
run_case 0 split
printf 'diagnostic\n' > "$fixture/native.error"
run_case 1 split
reset_inputs
printf 'payload\r\n' > "$fixture/native.input"
run_case 0 subset
run_case 0 freeze
printf 'payload\r' > "$fixture/native.input"
run_case 1 subset
run_case 1 freeze
printf 'payload\n\n' > "$fixture/native.input"
run_case 1 subset
run_case 1 freeze
reset_inputs
printf '7\n' > "$fixture/native.code"
run_case 1 subset
run_case 1 freeze
reset_inputs
printf 'payload\r\n' > "$fixture/oracle.input"
run_case 0 wasm
printf '1\n' > "$fixture/native.code"
run_case 1 wasm
printf '134\n' > "$fixture/oracle.code"
run_case 0 wasm
printf '124\n' > "$fixture/native.code"
run_case 1 wasm
printf '1\n' > "$fixture/native.code"
printf 'payload\0\n' > "$fixture/native.input"
cp "$fixture/native.input" "$fixture/oracle.input"
run_case 1 wasm
reset_inputs
printf 'about to throw from finalizer\n' > "$fixture/native.input"
printf 'about to throw from finalizer\r\n' > "$fixture/oracle.input"
printf '1\n' > "$fixture/native.code"
printf '127\n' > "$fixture/oracle.code"
printf '%s\n' 'dn2cpp fatal: unhandled exception in finalizer: System.InvalidOperationException' > "$fixture/native.error"
printf '%s\r\n' 'Unhandled exception. System.InvalidOperationException: boom' > "$fixture/oracle.error"
run_case 0 wasm-finalizers
run_case 1 wasm-finalizers-linux
run_case 0 wasm-cache-host
printf 'command not found\n' > "$fixture/oracle.error"
run_case 1 wasm-finalizers
printf '%s\r\n' 'Unhandled exception. System.InvalidOperationException: boom' > "$fixture/oracle.error"
printf '127\n' > "$fixture/native.code"
run_case 1 wasm-finalizers
bash gates/fixtures/luacsharp-output.sh "$py"
bash gates/fixtures/capture-setup-failure.sh "$py"
DN2CPP_PYTHON="$py" bash gates/test-gate-python.sh
printf 'OK: gate output shell contract\n'
run_case 0 status4-cache
run_case 0 status4-fixture
run_case 1 status4-repeated
run_case 0 status4-other
run_case 1 status4-second-error
run_case 1 status4-disabled
run_case 0 status4-direct-default
run_case 0 status4-direct-enabled
run_case 0 status4-direct-disabled
run_case 0 status4-direct-repeated
run_case 1 status4-nonwindows
run_case 0 status4-unrelated
run_case 0 status4-split
run_case 0 status4-oracle-hook
run_case 0 status4-cleanup
run_case 0 status4-cleanup-nested
gate_python - "$fixture" <<'PY'
from pathlib import Path
import sys

root = Path(sys.argv[1])
payload = b'payload\0\r\n\n'
for mode in ('raw', '-diet', '-native'):
    fixture = root / 'status4-fixture'
    for side in ('native', 'oracle'):
        for variant in ('main', 'extra'):
            assert (fixture / 'attempts' / f'{mode}-{side}-{variant}').read_text().strip() == '2'
    evidence = list((fixture / ('run' + (mode if mode != 'raw' else ''))).glob('retry4.*'))
    assert len(evidence) == 4
    for attempt in evidence:
        assert (attempt / 'first.stdout').read_bytes() == payload
        assert (attempt / 'second.stdout').read_bytes() == payload
        assert (attempt / 'first.stderr').read_bytes() == b'attempt 1\n'
        assert (attempt / 'second.stderr').read_bytes() == b'attempt 2\n'
        assert (attempt / 'first.status').read_text().strip() == '4'
        assert (attempt / 'second.status').read_text().strip() == '0'
        assert 'OrdinaryReflectionTypeLeaves' in (attempt / 'command').read_text()
    assert sum("before\\ fixture ''" in (attempt / 'command').read_text() for attempt in evidence) == 2
contexts = (root / 'status4-fixture' / 'contexts').read_text().splitlines()
assert contexts == ['1', '1', '1']
for scenario in ('repeated', 'other', 'second-error', 'disabled', 'direct-default',
                 'direct-enabled', 'direct-disabled', 'direct-repeated', 'nonwindows',
                 'unrelated', 'split', 'oracle-hook', 'cleanup', 'cleanup-nested'):
    fixture = root / ('status4-' + scenario)
    evidence_root = fixture / 'run'
    if scenario == 'cleanup-nested':
        evidence_root /= 'x64'
    evidence = list(evidence_root.glob('retry4.*'))
    native_count = 1 if scenario in ('other', 'disabled', 'direct-disabled', 'nonwindows') else 2
    assert (fixture / 'attempts' / 'raw-native-main').read_text().strip() == str(native_count)
    if scenario.startswith('cleanup'):
        oracle_count = 0
    elif scenario.startswith('direct-'):
        oracle_count = native_count
    else:
        oracle_count = 2 if scenario in ('repeated', 'split', 'oracle-hook') else 1
    if oracle_count:
        assert (fixture / 'attempts' / 'raw-oracle-main').read_text().strip() == str(oracle_count)
    if native_count == 2:
        assert len(evidence) == sum(count == 2 for count in (native_count, oracle_count))
        for attempt in evidence:
            code = '4' if scenario in ('repeated', 'direct-repeated') else '7' if scenario == 'second-error' else '0'
            assert (attempt / 'first.status').read_text().strip() == '4'
            assert (attempt / 'second.status').read_text().strip() == code
            if scenario.startswith('direct-'):
                assert (attempt / 'first.stdout').read_bytes() == payload
                assert (attempt / 'second.stdout').read_bytes() == payload
                assert (attempt / 'first.stderr').read_bytes() == b'attempt 1\0\r\n\n'
                assert (attempt / 'second.stderr').read_bytes() == b'attempt 2\0\r\n\n'
                label = (attempt / 'label').read_text().strip()
                assert f'note: {label} first run returned 4;' in (attempt / 'retry.log').read_text()
        if scenario.startswith('direct-'):
            assert sorted((attempt / 'label').read_text().strip() for attempt in evidence) == ['dotnet fixture.dll', 'native']
    else:
        assert not evidence
    if scenario == 'repeated':
        for side in ('native', 'oracle'):
            assert (fixture / 'run' / f'{side}.status').read_text().strip() == '125'
PY
printf 'OK: Windows status-4 retry policy\n'
