#!/usr/bin/env bash
# Exercise the real wrappers without SDK, compiler, runtime or cache prerequisites.
set -euo pipefail
cd "$(dirname "$0")/.."
script="$PWD/gates/test-gate-output-shell.sh"
_DN2CPP_GATE_ROOT="$PWD"
eval "$(sed -n '/^gate_capture() {/,/^}/p; /^gate_capture_ok() {/,/^}/p; /^gate_assert_text() {/,/^}/p; /^_gate_helpers_hash() {/,/^}/p; /^gate_output() {/,/^}/p; /^corelib_diff_gate() {/,/^}/p; /^corelib_diff_abort_gate() {/,/^}/p; /^_corelib_diff_gate() {/,/^}/p; /^corelib_diff_split_gate() {/,/^}/p; /^corelib_subset_gate() {/,/^}/p; /^corelib_freeze_gate() {/,/^}/p; /^wasm_corelib_diff_gate() {/,/^}/p' gates/_common.sh)"

if [ "${1:-}" = --case ]; then
    scenario="$2" fixture="$3" py="$4"
    DN2CPP_OS=linux
    EXE_EXT=
    resolve_python() { printf '%s\n' "$py"; }
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
                "$py" - "$fixture/abort-contexts" <<'PY'
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

py="${1:-python3}"
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
printf 'OK: gate output shell contract\n'
