#!/usr/bin/env bash
# Exercise real gate invocation blocks with stale observations and failed setup.
set -euo pipefail
cd "$(dirname "$0")/../.."
_DN2CPP_GATE_ROOT="$PWD"
script="$PWD/gates/fixtures/capture-setup-failure.sh"
py="${1:-python3}"
eval "$(sed -n '/^gate_capture() {/,/^}/p; /^gate_output() {/,/^}/p' gates/_common.sh)"
resolve_python() { printf '%s\n' "$py"; }

if [ "${2:-}" = --case ]; then
    kind="$3" fixture="$4"
    out="$fixture/observations" OUT="$out"
    sig_out=unused sig_app=unused.dll PROJECT=unused APP=unused.dll
    run_bounded() {
        local side=native
        [ "$1" != dotnet ] || side=oracle
        : > "$fixture/ran-$side"
    }
    case "$kind" in
        limits)
            block=$(sed -n '/^sig_native=.*check-sig_native-1.stdout/,/^gate_output status "\$sig_native_rc"/p' gates/build-and-run-transpiler-limits.sh)
            ;;
        trim)
            block=$(sed -n '/^    native="\$OUT\/check-native-9.stdout"/,/^    gate_output status "\$native_code" "\$expected_code"/p' gates/build-and-run-trim-reflection.sh)
            ;;
    esac
    [ -n "$block" ] || exit 90
    # Explicit capture guards must work even when the caller disables errexit.
    set +e
    eval "$block"
    exit $?
fi

fixture=$(mktemp -d)
trap 'rm -rf "$fixture"' EXIT
for kind in limits trim; do
    for failure in none native oracle; do
        run="$fixture/$kind-$failure"
        mkdir -p "$run/observations"
        if [ "$kind" = limits ]; then
            native=check-sig_native-1 oracle=check-sig_expected-2
        else
            native=check-native-9 oracle=check-expected-10
        fi
        for prefix in "$native" "$oracle"; do
            : > "$run/observations/$prefix.stdout"
            printf '0\n' > "$run/observations/$prefix.status"
        done
        if [ "$failure" != none ]; then
            prefix="$native"
            [ "$failure" != oracle ] || prefix="$oracle"
            # A directory is unwritable as a file even for root and on Windows.
            mkdir "$run/observations/$prefix.stderr"
        fi
        status=0
        bash "$script" "$py" --case "$kind" "$run" > "$run/log" 2>&1 || status=$?
        if [ "$failure" = none ]; then
            [ "$status" -eq 0 ] && [ -f "$run/ran-native" ] && [ -f "$run/ran-oracle" ] || {
                cat "$run/log" >&2; echo "FAIL: $kind capture positive control" >&2; exit 1;
            }
        else
            [ "$status" -ne 0 ] && [ ! -e "$run/ran-$failure" ] || {
                cat "$run/log" >&2; echo "FAIL: $kind reused stale $failure observation" >&2; exit 1;
            }
        fi
    done
done
printf 'OK: real gate capture setup failures reject stale observations\n'
