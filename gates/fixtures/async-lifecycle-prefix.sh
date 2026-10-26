#!/usr/bin/env bash
# The full raw prefix must end at one exception-message-dependent boundary.
set -euo pipefail
cd "$(dirname "$0")/../.."
python="${1:-python3}"
gate_output() { "$python" gates/gate-output.py "$@"; }
eval "$(sed -n '/^async_lifecycle_prefix() {/,/^}/p' gates/build-and-run-async-combinators.sh)"
fixture=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp_async_prefix.XXXXXX")
trap 'rm -rf "$fixture"' EXIT

must_fail() {
    if async_lifecycle_prefix "$fixture/actual" "$fixture/expected" "$fixture/prefix" \
            >"$fixture/diagnostic" 2>&1; then
        echo "FAIL: lifecycle prefix accepted $1" >&2
        exit 1
    fi
}

for newline in $'\n' $'\r\n'; do
    printf 'baseline%s%s' "$newline" "$newline" > "$fixture/expected"
    printf 'baseline%s%swhenall-null-seq: platform message%stail%s' \
        "$newline" "$newline" "$newline" "$newline" > "$fixture/actual"
    async_lifecycle_prefix "$fixture/actual" "$fixture/expected" "$fixture/prefix"

    printf 'baseline%s%sextra%swhenall-null-seq: platform message%s' \
        "$newline" "$newline" "$newline" "$newline" > "$fixture/actual"
    must_fail 'an extra record before the boundary'
    printf 'baseline%s%stail%s' "$newline" "$newline" "$newline" > "$fixture/actual"
    must_fail 'a missing boundary'
    printf 'baseline%s%swhenall-null-seq: first%swhenall-null-seq: second%s' \
        "$newline" "$newline" "$newline" "$newline" > "$fixture/actual"
    must_fail 'a duplicated boundary'
done

printf 'base\rline\n\n' > "$fixture/expected"
printf 'base\rline\n\nwhenall-null-seq: message\n' > "$fixture/actual"
async_lifecycle_prefix "$fixture/actual" "$fixture/expected" "$fixture/prefix"
printf 'baseline\n\nwhenall-null-seq: message\n' > "$fixture/actual"
must_fail 'a removed lone CR'
printf 'payload\0\r\n\r\r\n\n' > "$fixture/expected"
cat "$fixture/expected" > "$fixture/actual"
printf 'whenall-null-seq: platform message' >> "$fixture/actual"
async_lifecycle_prefix "$fixture/actual" "$fixture/expected" "$fixture/prefix"
printf 'payload\r\n\r\r\n\nwhenall-null-seq: platform message' > "$fixture/actual"
must_fail 'a removed NUL'
echo 'OK: async lifecycle raw prefix and unique boundary'
