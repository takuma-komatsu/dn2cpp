#!/usr/bin/env bash
# The real Lua callback must propagate its CLR prefix selector and retain CRLF bytes.
set -euo pipefail
cd "$(dirname "$0")/../.."
_DN2CPP_GATE_ROOT="$PWD"
py="${1:-python3}"
fixture_root=$(mktemp -d)
fixture="$fixture_root/run"
mkdir -p "$fixture"
trap 'rm -rf "$fixture_root"' EXIT
resolve_python() { printf '%s\n' "$py"; }
eval "$(sed -n '/^gate_capture() {/,/^}/p; /^gate_capture_ok() {/,/^}/p; /^gate_output() {/,/^}/p; /^assert_exit_code() {/,/^}/p' gates/_common.sh)"
eval "$(sed -n '/^gate_extra_asserts() {/,/^}/p' gates/build-and-run-luacsharp.sh)"
_CG_APP=fixture.dll
_CG_CORELIB=fixture-corelib.dll
package_refs=()
markers="$fixture/markers"
: > "$markers"
: > "$fixture/expected.err"
: > "$fixture/native.err"
fixture_prefix() {
    printf '%s\r\n' 'language end' 'tables and libraries end' 'interop end' \
        'coroutines end' 'modules end' 'errors end' 'LuaCSharp end'
}
fixture_prefix > "$fixture/native.out"
printf '%s\r\n' '== Lua standard libraries ==' 'Lua standard libraries end' >> "$fixture/native.out"
run_bounded() { "$@"; }
dotnet() {
    printf '%s\n' "$@" > "$fixture/oracle.argv"
    [ "$#" = 2 ] && [ "$1" = fixture.dll ] && [ "$2" = before-standard-libraries ] \
        || { echo 'FAIL: Lua CLR prefix selector was not propagated' >&2; return 97; }
    fixture_prefix
}
invoke_cli() {
    local destination="${!#}"
    mkdir -p "$destination"
    : > "$destination/s0-gaps.tsv"
}
gate_extra_asserts "$fixture" 0 0
printf '%s\n' fixture.dll before-standard-libraries > "$fixture/expected.argv"
cmp "$fixture/oracle.argv" "$fixture/expected.argv"
fixture_prefix > "$fixture/expected.stdout"
cmp "$fixture/before-standard-libraries.dotnet.stdout" "$fixture/expected.stdout"
echo 'OK: Lua prefix caller argv and raw output'
