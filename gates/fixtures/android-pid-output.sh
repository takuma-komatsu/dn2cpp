#!/usr/bin/env bash
# Android process observations retain raw output and reject malformed or failed probes.
set -euo pipefail
cd "$(dirname "$0")/../.."
py=${1:-python3}
gate_output() { "$py" gates/gate-output.py "$@"; }
eval "$(sed -n '/^gate_capture() {/,/^}/p' gates/_common.sh)"
eval "$(sed -n '/^android_capture_pids() {/,/^}/p' gates/run-unrealsharp-android-minimal.sh)"
fixture=$(mktemp -d)
trap 'rm -rf "$fixture"' EXIT
mkdir "$fixture/bin"
cat > "$fixture/bin/adb" <<'SH'
#!/usr/bin/env bash
test "$*" = '-s fixture-device shell pidof fixture.package' || exit 2
cat "$ADB_TEST_STDOUT"
cat "$ADB_TEST_STDERR" >&2
exit "$ADB_TEST_STATUS"
SH
chmod +x "$fixture/bin/adb"
export PATH="$fixture/bin:$PATH"
export ADB_TEST_STDOUT="$fixture/input.stdout" ADB_TEST_STDERR="$fixture/input.stderr" ADB_TEST_STATUS=0
: > "$ADB_TEST_STDERR"

for ending in '' '\n' '\r\n'; do
    printf '123 456%b' "$ending" > "$ADB_TEST_STDOUT"
    android_capture_pids "$fixture/valid" fixture-device fixture.package
    test "$android_pid_list" = '123 456'
    test "${android_pid_list%% *}" = 123
    cmp "$ADB_TEST_STDOUT" "$fixture/valid.stdout"
    cmp "$ADB_TEST_STDERR" "$fixture/valid.stderr"
    test "$(cat "$fixture/valid.status")" = 0
done
ADB_TEST_STATUS=1
: > "$ADB_TEST_STDOUT"
android_capture_pids "$fixture/absent" fixture-device fixture.package
test -z "$android_pid_list"
test "$(cat "$fixture/absent.status")" = 1
test ! -s "$fixture/absent.stdout"
test ! -s "$fixture/absent.stderr"

rejection=0
reject_probe() {
    local prefix="$fixture/rejected-$rejection"
    rejection=$((rejection + 1))
    if android_capture_pids "$prefix" fixture-device fixture.package > "$fixture/check.stdout" 2> "$fixture/check.stderr"; then
        echo "FAIL: Android PID probe accepted status $ADB_TEST_STATUS, observation $prefix" >&2
        exit 1
    fi
    cmp "$ADB_TEST_STDOUT" "$prefix.stdout"
    cmp "$ADB_TEST_STDERR" "$prefix.stderr"
    test "$(cat "$prefix.status")" = "$ADB_TEST_STATUS"
    test -z "$android_pid_list"
}

ADB_TEST_STATUS=0
for payload in '' '0\n' '123\r\r\n' '12\r3\n' '123\0\n' '123\n456\n' '123\n\n' '123 extra\n'; do
    printf '%b' "$payload" > "$ADB_TEST_STDOUT"
    reject_probe
done
printf '123\n' > "$ADB_TEST_STDOUT"
for ADB_TEST_STATUS in 1 2 124 125 126 127 130 137 143; do
    reject_probe
done
ADB_TEST_STATUS=1
: > "$ADB_TEST_STDOUT"
printf 'adb: device offline\r\n' > "$ADB_TEST_STDERR"
reject_probe
: > "$ADB_TEST_STDERR"
printf '\n' > "$ADB_TEST_STDOUT"
reject_probe

touch "$fixture/not-a-directory"
if android_capture_pids "$fixture/not-a-directory/observation" fixture-device fixture.package; then
    echo 'FAIL: Android PID probe ignored a capture failure' >&2
    exit 1
fi
printf '%s\n' 'OK: Android PID observations preserve bytes and distinguish absence from failures'
