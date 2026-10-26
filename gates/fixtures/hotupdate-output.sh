#!/usr/bin/env bash
# Local hot-update fixture adapters retain raw bytes and separate every observation.
set -euo pipefail
cd "$(dirname "$0")/../.."
_DN2CPP_GATE_ROOT="$PWD"
py="${1:-python3}"
resolve_python() { printf '%s\n' "$py"; }
eval "$(sed -n '/^gate_capture() {/,/^}/p; /^gate_output() {/,/^}/p' gates/_common.sh)"
eval "$(sed -n '/^hotupdate_capture() {/,/^}/p; /^hotupdate_merged()/p; /^hotupdate_fixture() {/,/^}/p; /^hotupdate_before() {/,/^}/p' gates/build-and-run-hotupdate-subset.sh)"
OUT=$(mktemp -d)
trap 'rm -rf "$OUT"' EXIT
_hotupdate_capture_index=0
fixture_output() { printf 'payload\r\n\n'; printf 'diagnostic\r\n' >&2; return 7; }
hotupdate_capture observed status stdout fixture_output
test "$status" = 7
first="$observed"
printf 'payload\r\n\n' > "$OUT/wanted.stdout"
printf 'diagnostic\r\n' > "$OUT/wanted.stderr"
cmp "$observed" "$OUT/wanted.stdout"
cmp "${observed%.stdout}.stderr" "$OUT/wanted.stderr"
hotupdate_capture observed status stdout fixture_output
test "$first" != "$observed"
cmp "$first" "$observed"
fixture_merged() { printf 'out\n'; printf 'err\n' >&2; printf 'tail\n'; }
hotupdate_capture merged status combined fixture_merged
printf 'out\nerr\ntail\n' > "$OUT/wanted.merged"
cmp "$merged" "$OUT/wanted.merged"
hotupdate_fixture expected --text 'header' --file "$first" --text 'footer'
printf 'header\npayload\r\n\nfooter\n' > "$OUT/wanted.fixture"
cmp "$expected" "$OUT/wanted.fixture"
printf 'payload\r\r\nMARK\r\ntail\n' > "$OUT/marked"
span=$(hotupdate_before "$OUT/marked" MARK)
other=$(hotupdate_before "$OUT/marked" tail)
test "$span" != "$other"
printf 'payload\r\r\n' > "$OUT/wanted.span"
cmp "$span" "$OUT/wanted.span"
# The literal CR preceding a CRLF pair remains payload after semantic selection.
gate_output witness "$span" $'payload\r' --count 1 --policy crlf
if gate_output witness "$span" payload --count 1 --policy crlf; then
    echo 'FAIL: a second normalization consumed a payload CR' >&2
    exit 1
fi
# The complete pre-patch span must contain only the base-ready record: a plain
# starts-with check would silently ignore extra records before the patch marker.
printf 'signature argument base ready\n' > "$OUT/base-ready.expected"
printf 'signature argument base ready\r\n== imported generic signature arguments ==\r\npatch\r\n' > "$OUT/signature-valid"
gate_output prefix "$OUT/signature-valid" "$OUT/base-ready.expected" \
    --before '== imported generic signature arguments ==' --policy crlf
printf 'signature argument base ready\r\nunexpected intermediate record\r\n== imported generic signature arguments ==\r\npatch\r\n' > "$OUT/signature-extra"
if gate_output prefix "$OUT/signature-extra" "$OUT/base-ready.expected" \
    --before '== imported generic signature arguments ==' --policy crlf; then
    echo 'FAIL: signature prefix ignored an intermediate record' >&2
    exit 1
fi
# Exercise the actual stale/no-entry consumer blocks, not only status helpers.
# The fixture command must run before any negative assertion can count as proof.
DN2CPP_OS=windows
stale_consumer=$(sed -n '/^hotupdate_capture stale_output /,/^echo "OK (stale BPI rejected)"/p' gates/build-and-run-hotupdate-subset.sh)
noentry_consumer=$(sed -n '/^hotupdate_capture noentry_run_output /,/^echo "OK (entry-less BPI)"/p' gates/build-and-run-hotupdate-subset.sh)
test -n "$stale_consumer" && test -n "$noentry_consumer"
fixture_run() {
    printf 'called\n' > "$OUT/invoked"
    printf '%s' "$fixture_stdout"
    printf '%s' "$fixture_stderr" >&2
    return "$fixture_status"
}
run_bounded() { fixture_run; }
check_consumer() {
    local consumer="$1" expected="$2" result
    rm -f "$OUT/invoked"
    if (eval "$consumer") > "$OUT/consumer.out" 2> "$OUT/consumer.err"; then
        result=pass
    else
        result=fail
    fi
    test -f "$OUT/invoked" || { echo 'FAIL: consumer never invoked its command' >&2; exit 1; }
    if [ "$result" != "$expected" ]; then
        cat "$OUT/consumer.out" "$OUT/consumer.err" >&2
        echo "FAIL: consumer result $result, expected $expected (status=$fixture_status host=$DN2CPP_OS)" >&2
        exit 1
    fi
}
for consumer_kind in stale noentry; do
    if [ "$consumer_kind" = stale ]; then
        consumer="$stale_consumer"
        diagnostic='Unhandled exception. System.NotSupportedException: BPI/base ABI mismatch: rebuild the patch against this base image'
    else
        consumer="$noentry_consumer"
        diagnostic='Unhandled exception. System.NotSupportedException: hotupdate: the BPI has no entry method'
    fi
    fixture_stdout=$'base: start\r\n'
    fixture_stderr="$diagnostic"$'\r\n'
    for fixture_status in 124 125 126 130 137 143 0 1; do
        check_consumer "$consumer" fail
    done
    fixture_status=127
    check_consumer "$consumer" pass
    DN2CPP_OS=linux
    check_consumer "$consumer" fail
    fixture_status=134
    check_consumer "$consumer" pass
    DN2CPP_OS=windows
    fixture_status=127
    for fixture_stderr in '' $'wrong diagnostic\n' "prefix $diagnostic"$'\n' "$diagnostic"$'\r\r\n'; do
        check_consumer "$consumer" fail
    done
    fixture_stderr="$diagnostic"$'\r\n'
    for fixture_stdout in '' $'wrong witness\n' $'base: start\nbase: start\n' $'base: start\r\r\n'; do
        check_consumer "$consumer" fail
    done
done
# Registration metadata failures use the separate raw stderr/empty-stdout path.
empty_consumer=$(sed -n '/^    case "$mutation" in$/,/^done$/p' gates/build-and-run-hotupdate-subset.sh | sed '$d')
test -n "$empty_consumer"
mutation=extent
malformed=fixture
EXE_EXT=''
diagnostic='Unhandled exception. System.NotSupportedException: BPI: method metadata section extent mismatch'
fixture_stdout=''
fixture_stderr="$diagnostic"$'\r\n'
for fixture_status in 124 125 126 130 137 143 0 1; do
    check_consumer "$empty_consumer" fail
done
fixture_status=127
for mutation in extent parameter-count return-type ownership; do
    case "$mutation" in
        extent) diagnostic='BPI: method metadata section extent mismatch' ;;
        parameter-count) diagnostic='BPI: invalid method metadata identity or signature' ;;
        return-type) diagnostic='BPI: interface implementation metadata signature mismatch' ;;
        ownership) diagnostic='BPI: interface implementation method ownership mismatch' ;;
    esac
    diagnostic="Unhandled exception. System.NotSupportedException: $diagnostic"
    fixture_stderr="$diagnostic"$'\r\n'
    check_consumer "$empty_consumer" pass
done
fixture_stdout=$'unexpected entry point\n'
check_consumer "$empty_consumer" fail
fixture_stdout=''
fixture_stderr=''
check_consumer "$empty_consumer" fail
fixture_stderr="$diagnostic"$'\n'
DN2CPP_OS=linux
check_consumer "$empty_consumer" fail
DN2CPP_OS=windows
# Opt-in must not leak into an ordinary capture, even with matching evidence.
ordinary_consumer='hotupdate_capture ordinary ordinary_status stdout run_bounded fixture'
fixture_stdout=$'base: start\n'
for fixture_status in 124 125 126 127 130 137 143; do
    check_consumer "$ordinary_consumer" fail
done
fixture_status=0
check_consumer "$ordinary_consumer" pass
fixture_status=7
check_consumer "$ordinary_consumer" pass
# Exercise the real early cache call with a receipt stub: on the same machine,
# only changing the declared host policy must miss a Windows abort receipt.
"$py" - "$OUT/cache-call" <<'PYCODE'
from pathlib import Path
import sys
gate_script = Path('gates/build-and-run-hotupdate-subset.sh').read_text(encoding='utf-8')
line = next(line for line in gate_script.splitlines() if line.startswith('if gate_cache_check '))
assert line.endswith(' \\')
Path(sys.argv[1]).write_text(line[len('if '):-2] + '\n', encoding='utf-8')
PYCODE
_gate_cli_hash() { printf cli; }
resolve_net10_corelib() { printf corelib; }
field_packed=packed
field_native=native
cached_context=''
cache_mode=save
gate_cache_check() {
    if [ "$cache_mode" = save ]; then
        cached_context="$2"
        return 1
    fi
    [ "$cached_context" = "$2" ]
}
DN2CPP_OS=windows
source "$OUT/cache-call" || :
cache_mode=read
source "$OUT/cache-call" || { echo 'FAIL: same-host cache control missed' >&2; exit 1; }
DN2CPP_OS=linux
if source "$OUT/cache-call"; then
    echo 'FAIL: host-only abort policy change reused cached Windows evidence' >&2
    exit 1
fi

printf 'OK: hot-update output adapters\n'
