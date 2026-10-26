#!/usr/bin/env bash
# Failed locale probes retain the command, exit status and both output streams.
# The DEFAULT CultureInfo.CurrentCulture — the one a program that never names a
# culture formats and parses through — asserted against real .NET under
# several FORCED locales.
#
# WHY THIS GATE IS NOT A SECTION OF ANOTHER BUCKET. Every bucket driver in the
# tree pins CurrentCulture to invariant as its first statement, because a gate
# whose verdict moves with the developer's locale is not a gate (AGENTS.md,
# "Host culture"). This is the one program whose subject matter IS the
# developer's locale, so it cannot carry that pin — and it must not be a section
# inside a driver that does.
#
# WHY IT IS STILL DETERMINISTIC. It never asserts a fixed string. For each
# locale below it runs the native binary and `dotnet $app` under the SAME
# LC_ALL/LANG and requires them byte-identical, so the expectation is real .NET's
# answer on that host rather than any answer written down here. On POSIX the
# forced locales really move both sides (measured: ja-JP, de-DE, en-US, fr-FR all
# resolve and all match); on Windows neither side reads the environment, so the
# sweep collapses to four runs of the user's locale — still a real subject-vs-
# oracle diff, just one locale's worth. That is why the gate never asserts that
# the locales DIFFER: it would be asserting a property of the host.
# The unknown-region ZZ currency is frozen ICU data. Its symbol is checked by
# corelib_freeze_gate; the other foo-ZZ and zz-ZZ fields use the live CLR oracle.
#
# WHAT IT DOES NOT COVER. Per-culture DATE formatting is a standing carve-out
# (docs/STATUS.md): dn2cpp's date formatters take no NumberFormatInfo and render
# invariantly whatever the current culture is. The sample prints no date for
# that reason — a date line here would fail for a divergence this gate never
# claimed to close, and would say nothing about the one it did. The numeric half is the
# whole subject: name, the NumberFormatInfo symbols the table carries, and
# provider-less N/P/C/G formatting.
# The appended fallback section pins both managed cultures before constructing
# undefined-language and grandfathered identifiers. Their numeric profiles still
# depend on ICU's host language and the identifier's region. Each explicit name
# also runs in a fresh process to keep CultureData cache ordering out of the diff.
# Windows numeric user settings compare every modeled NumberFormatInfo field,
# constructor/cache order and mutation isolation against the live CLR. Other
# locales must retain ICU data even when their constructors enable overrides.
#
# It gate_skips only where real .NET itself has no cultures — an ICU-less host
# in invariant-globalization mode, where the oracle answers "" to every locale
# while the subject, which needs no ICU, answers the host's. That is not a
# divergence to report; it is an oracle that has been told there is nothing to
# ask about.
source "$(dirname "$0")/_common.sh"

gate_run_logs_init default_culture "DefaultCulture runs"

# Assign the raw stdout path; failed runs retain their bytes and command context.
default_culture_run() {
    local _culture_result="$1" _culture_side="$2" _culture_prefix _culture_code _culture_command
    shift 2
    _CULTURE_RUN_SEQUENCE=$((${_CULTURE_RUN_SEQUENCE:-0} + 1))
    _culture_prefix="$_GATE_RUN_LOG_DIR/${_CULTURE_RUN_SEQUENCE}-$_culture_side"
    printf -v "$_culture_result" '%s' "$_culture_prefix.stdout"
    printf -v _culture_command '%q ' "$@"
    gate_capture "$_culture_prefix" run_bounded_retry_status4 "${out:-$_GATE_RUN_LOG_DIR}" \
        "DefaultCulture $_culture_side: $_culture_command" "$@" || return 1
    printf '%s\n' "$_culture_command" > "$_culture_prefix.command"
    _culture_code=$(cat "$_culture_prefix.status")
    if [ "$_culture_code" -ne 0 ]; then
        echo "---- DefaultCulture $_culture_side: $_culture_command diagnostics ----" >&2
        echo "exit code: $_culture_code" >&2
        _gate_run_termination "$_culture_code" "$_culture_prefix.stderr"
        if [ -s "$_culture_prefix.stdout" ]; then
            echo 'stdout:' >&2; cat "$_culture_prefix.stdout" >&2
        else
            echo 'stdout: <empty>' >&2
        fi
        if [ -s "$_culture_prefix.stderr" ]; then
            echo 'stderr:' >&2; cat "$_culture_prefix.stderr" >&2
        else
            echo 'stderr: <empty>' >&2
        fi
        return "$_culture_code"
    fi
    cat "$_culture_prefix.stderr" >&2
}

run_locale_env() {
    local result="$1" side="$2" assigns=()
    shift 2
    read -r -a assigns <<< "$1"
    shift
    default_culture_run "$result" "$side" env -u LC_ALL -u LC_MESSAGES -u LANG ${assigns[@]+"${assigns[@]}"} "$@"
}

default_culture_diagnostics_self_test() {
    local side code actual actual_code diagnostics child quoted_args retry_counter retry_evidence
    child='
        [ "$#" -eq 5 ] && [ "$2" = "culture-fallback" ] \
            && [ "$3" = "foo QQ" ] && [ "$4" = "" ] \
            && [ "$5" = "$(printf "foo\047QQ")" ] \
            && [ "$LANG" = "de_DE.UTF-8" ] && [ "${LC_ALL+x}" != x ] \
            && [ "${LC_MESSAGES+x}" != x ] || exit 91
        printf "same output\n"
        printf "injected stderr\n" >&2
        exit "$1"
    '
    printf -v quoted_args '%q ' culture-fallback "foo QQ" "" "foo'QQ"
    for side in native oracle; do
        diagnostics="$_GATE_RUN_LOG_DIR/self-test-$side.stderr"
        LC_ALL=C LC_MESSAGES=C LANG=C run_locale_env actual "$side" "LANG=de_DE.UTF-8" bash -c "$child" _ 0 \
            culture-fallback "foo QQ" "" "foo'QQ" 2>"$diagnostics"
        gate_assert_text "$actual" "same output" \
            || { echo "FAIL: $side successful stdout, locale or quoted arguments changed" >&2; return 1; }
        grep -Fxq 'injected stderr' "$diagnostics" \
            || { echo "FAIL: $side successful stderr disappeared" >&2; return 1; }
        printf 'line\r\n\n\000tail\r' > "$_GATE_RUN_LOG_DIR/self-test.raw"
        default_culture_run actual "$side" bash -c 'printf "line\r\n\n\000tail\r"' 2>"$diagnostics"
        gate_output compare "$actual" "$_GATE_RUN_LOG_DIR/self-test.raw"
        code=23
        [ "$side" = native ] || code=29
        if LC_ALL=C LC_MESSAGES=C LANG=C run_locale_env actual "$side" "LANG=de_DE.UTF-8" bash -c "$child" _ "$code" \
            culture-fallback "foo QQ" "" "foo'QQ" 2>"$diagnostics"; then
            echo "FAIL: $side nonzero exit with matching stdout passed" >&2
            return 1
        else
            actual_code=$?
        fi
        [ "$actual_code" -eq "$code" ] \
            || { echo "FAIL: $side child exit code changed" >&2; return 1; }
        grep -Fq "DefaultCulture $side:" "$diagnostics" \
            && grep -Fq 'env -u LC_ALL -u LC_MESSAGES -u LANG LANG=de_DE.UTF-8 ' "$diagnostics" \
            && grep -Fq "$quoted_args" "$diagnostics" \
            && grep -Fxq "exit code: $code" "$diagnostics" \
            && grep -Fxq 'termination: nonzero exit' "$diagnostics" \
            && grep -Fxq 'stdout:' "$diagnostics" \
            && grep -Fxq 'same output' "$diagnostics" \
            && grep -Fxq 'stderr:' "$diagnostics" \
            && grep -Fxq 'injected stderr' "$diagnostics" \
            || { echo "FAIL: $side failed-run diagnostics are incomplete" >&2; cat "$diagnostics" >&2; return 1; }
        if default_culture_run actual "$side" bash -c 'exit 37' 2>"$diagnostics"; then
            echo "FAIL: $side empty failed run passed" >&2
            return 1
        else
            actual_code=$?
        fi
        [ "$actual_code" -eq 37 ] && [ ! -s "$actual" ] \
            && grep -Fq "DefaultCulture $side:" "$diagnostics" \
            && grep -Fxq 'exit code: 37' "$diagnostics" \
            && grep -Fxq 'stdout: <empty>' "$diagnostics" \
            && grep -Fxq 'stderr: <empty>' "$diagnostics" \
            || { echo "FAIL: $side empty failed-run diagnostics are incomplete" >&2; cat "$diagnostics" >&2; return 1; }
    done
    if [ "$DN2CPP_OS" = windows ]; then
        retry_counter="$_GATE_RUN_LOG_DIR/retry-counter"
        diagnostics="$_GATE_RUN_LOG_DIR/self-test-retry.stderr"
        child='
            if [ ! -e "$1" ]; then
                printf "1\n" >"$1"
                printf "first attempt\n"
                printf "first error\n" >&2
                exit 4
            fi
            printf "second attempt\n"
        '
        default_culture_run actual native bash -c "$child" _ "$retry_counter" 2>"$diagnostics"
        retry_evidence=("$_GATE_RUN_LOG_DIR"/retry4.*)
        gate_assert_text "$actual" 'second attempt' && [ "$(cat "$retry_counter")" = 1 ] \
            && [ "${#retry_evidence[@]}" -eq 1 ] \
            && grep -Fxq 'first attempt' "${retry_evidence[0]}/first.stdout" \
            && grep -Fxq 'first error' "${retry_evidence[0]}/first.stderr" \
            && grep -Fxq '4' "${retry_evidence[0]}/first.status" \
            && grep -Fq 'first run returned 4; retried once (second status 0' "$diagnostics" \
            || { echo 'FAIL: status-4 retry lost the first result or changed the second' >&2; return 1; }
        if default_culture_run actual native bash -c 'exit 4' 2>"$diagnostics"; then
            echo 'FAIL: a second status 4 passed' >&2
            return 1
        else
            actual_code=$?
        fi
        [ "$actual_code" -eq 4 ] \
            && grep -Fxq 'exit code: 4' "$diagnostics" \
            && grep -Fq 'first run returned 4; retried once (second status 4' "$diagnostics" \
            || { echo 'FAIL: a second status 4 was not reported' >&2; return 1; }
    fi
    echo "OK: DefaultCulture run diagnostics"
    return 0
}

default_culture_diagnostics_self_test
if [ "${1:-}" = --self-test ]; then
    exit 0
fi

# Only the frozen ZZ currency differs; every other byte remains oracle-owned.
default_culture_assert_zz() {
    local mode="$1" native="$2" oracle="$3" frozen="$4" python
    python=$(resolve_python) || return 1
    # resolve_python can return the Windows launcher "py -3".
    # shellcheck disable=SC2086
    $python - "$mode" "$native" "$oracle" "$frozen" <<'PYTHON' || return 1
from pathlib import Path
import re
import sys

mode, native, oracle, frozen = sys.argv[1:]
for filename, is_native in ((native, True), (oracle, False)):
    data = Path(filename).read_bytes()
    marker = b"    symbols=" if mode == "fallback" else b"cur="
    rows = [line for line in data.split(b"\n") if line.startswith(marker)]
    if len(rows) != 1:
        raise SystemExit("FAIL: missing or repeated ZZ currency symbol record")
    row = rows[0]
    if row.endswith(b"\r"):
        # This record came from split(LF), so only a CR paired with LF is removed.
        offset = data.find(row)
        if data[offset + len(row):offset + len(row) + 1] == b"\n":
            row = row[:-1]
    fields = row.split(b"|") if mode == "fallback" else row.split(b"=", 1)
    index = 5 if mode == "fallback" else 1
    if len(fields) <= index:
        raise SystemExit("FAIL: malformed ZZ currency symbol record")
    symbol = fields[index]
    if is_native and symbol != frozen.encode("utf-8"):
        raise SystemExit(f"FAIL: frozen ZZ currency {symbol!r}, expected {frozen!r}")
    if not is_native and re.fullmatch(b"[A-Z]{3}", symbol) is None:
        raise SystemExit(f"FAIL: CLR ZZ currency is not an ISO code: {symbol!r}")
    # Split on LF only: lone CR and other control bytes are payload, not records.
    lines = data.split(b"\n")
    for index, line in enumerate(lines):
        if mode == "fallback":
            if line.startswith((b"    symbols=", b"    C double=", b"    C2 double=")):
                lines[index] = line.replace(symbol, b"<currency>")
        elif line.startswith(b"cur="):
            lines[index] = b"cur=<currency>" + line[len(b"cur=") + len(symbol):]
        elif line.startswith(b"c="):
            lines[index] = line.replace(symbol, b"<currency>")
    Path(filename + ".currency-masked").write_bytes(b"\n".join(lines))
PYTHON
    gate_output compare "$native.currency-masked" "$oracle.currency-masked"
}

default_culture_assert_zz_fallback() {
    default_culture_assert_zz fallback "$@"
}

default_culture_assert_zz_default() {
    gate_output witness "$1" 'Name=zz-ZZ' --policy crlf
    gate_output witness "$2" 'Name=zz-ZZ' --policy crlf
    default_culture_assert_zz default "$@"
}

PROJECT=DefaultCulture
out="$(_corelib_gate_out "$PROJECT")"
_corelib_gate_core "$PROJECT" "$out"

# THE LOCALE LIST IS THE GATE. A list of locales the runtime's table already
# carries makes the one gate whose subject is the host's locale unable to see the
# failure it exists for — a host locale ABSENT from the table getting invariant
# symbols under a correct name. en_GB, pt_BR and sv_SE are on the
# list because they were absent from that table; keep at least one such locale
# here whenever the table changes, or this gate stops being able to see the
# difference between a table and a table with a hole in it. sv_SE earns its place
# twice: its negative sign is U+2212 and its group separator U+00A0, so it also
# fails if the row's non-ASCII columns are dropped rather than merely missing.
#
# hi_IN is the only locale here whose subject is the
# SHAPE of the modeled NumberFormatInfo rather than its values: hi-IN groups
# digits 3-then-2, which no row could express while the model carried one uniform
# group size — so the culture was a candidate the table refused, and a host set to
# it got invariant symbols under a correct name, the same hole en_GB stands for.
# It is a real macOS `locale -a` entry, so this is a locale a
# developer's machine can actually be in.
LOCALES="${DN2CPP_CULTURE_LOCALES:-en_US.UTF-8 de_DE.UTF-8 fr_FR.UTF-8 ja_JP.UTF-8 en_GB.UTF-8 pt_BR.UTF-8 sv_SE.UTF-8 hi_IN.UTF-8}"
FALLBACK_CASES=(
    und-US und-DE und-FR und-JP und-Latn-US
    und-US-u-nu-arab und-US-u-nu-latn und-US-u-nu-native
    und-US-u-nu-finance und-US-u-nu-traditio und-US-u-nu-foo
    foo foo-US foo-u-nu-default foo-US-u-nu-default
    de-FOO de-FOO-u-ca-gregory de-FOO-u-nu-latn de-FOO-u-nu-arab
    de-FOO-u-nu-native de-FOO-u-nu-finance de-FOO-u-nu-traditio
    de-FOO-u-nu-default de-FOO-u-nu-foo en-GB-oed en-GB-oed-u-nu-foo
    i-klingon i-klingon-u-nu-foo i-default i-default-u-nu-foo
    i-enochian-u-nu-foo i-mingo i-mingo-u-nu-foo
    zh-min zh-min-u-nu-foo zh-min-nan-u-nu-foo
    en-US de-DE ja-JP
    nv jbo cmn i-navajo-u-nu-native art-lojban-u-nu-native zh-guoyu-u-nu-native
    de-foo-bar-u-nu-latn de-foo-bar-US-u-nu-latn de-foo-Latn-US-u-nu-latn
    de-foo-bar-baz-US-u-nu-latn de-foo-bar-baz-zzz-US-u-nu-latn
    foo-QQ foo-999 und-QQ foo-ZZ i-default-US-u-nu-arab
    i-enochian-US-u-nu-arab i-mingo-US-u-nu-arab zh-min-US-u-nu-arab
    i-default-abcde-u-nu-arab
    i-klingon-Latn-US i-klingon-Latn-US-u-nu-latn zh-min-nan-Latn-US-u-nu-latn
    en-US-POSIX en-US-POSIX-u-nu-latn en-US-POSIX-u-nu-arab
    en-US-POSIX-u-nu-roman en-US-POSIX-u-nu-foo
)

# With libc's initial C locale, real .NET takes the first of LC_ALL, LC_MESSAGES
# and LANG that is SET, an empty one included; a value that names no locale (empty,
# C, POSIX, C.UTF-8, en_US_POSIX) leaves the default to the platform: the user's
# system preference on Apple, the invariant culture elsewhere. A lowercase c is
# a culture name, and .NET maps it to the invariant culture. Each row below
# unsets all three and sets only the listed ones. A and B are table locales
# other than the platform's own answer, so a row resolved through the wrong
# variable prints a different culture on every host. That answer is a host
# setting no variable carries, so it is cache context.
run_locale_env platform_default oracle "" dotnet "$_CG_APP"
gate_output view "$platform_default" "$platform_default.records" --policy crlf
IFS= read -r platform_default < "$platform_default.records"
decoys=()
for L in de_DE fr_FR sv_SE; do
    [ "$platform_default" = "Name=${L/_/-}" ] || decoys+=("$L.UTF-8")
done
A="${decoys[0]}" B="${decoys[1]}"
ENV_VARIANTS=(
    ""
    "LANG=$A"
    "LANG="
    "LANG=C"
    "LANG=c"
    "LANG=en_US_POSIX"
    "LANG=i-default"
    "LANG=en-US-POSIX-u-nu-arab"
    "LANG=en_US_POSIX_u_nu_arab"
    "LANG=en-US-POSIX-u-a"
    "LANG=en-US-POSIX-u-123456789"
    "LANG=en-US-POSIX-a-x-foo"
    "LANG=en-US-POSIX-abcdefghi-u-nu-arab"
    "LANG=en-US-POSIX-posix-u-nu-arab"
    "LANG=en-US-POSIX-u-ca-gregory"
    "LANG=en-US-POSIX-x-foo"
    "LANG=en-US-POSIX-a-foo"
    "LANG=en-US-POSIX-t-de-DE"
    "LANG=en-US-POSIX-u-ca-gregory-123456789"
    "LANG=en-US-POSIX-a-foo-u-a"
    "LANG=en-US-POSIX-u-00"
    "LANG=de_DE_"
    "LANG=de_DE@euro"
    "LANG=DE_de.UTF-8@euro"
    "LANG=eng.UTF-8"
    "LANG=und.UTF-8"
    "LANG=de_DE_phoneb.UTF-8"
    "LANG=en_US_u_nu_arab.UTF-8"
    "LANG=en-US-u-nu-arab.UTF-8"
    "LANG=en_US_u_ca_gregory_x_nu_arab.UTF-8"
    "LANG=en-US-x-u-nu-arab.UTF-8"
    "LANG=en_US_t_de_DE.UTF-8"
    "LANG=de_NO.UTF-8"
    "LANG=de_FOO.UTF-8"
    "LANG=de_FOO_u_nu_arab.UTF-8"
    "LANG=de-FOO-u-nu-arab.UTF-8"
    "LANG=de_foo_bar_US_u_nu_latn.UTF-8"
    "LANG=foo_US.UTF-8"
    "LANG=zz_ZZ.UTF-8"
    "LANG=zh_Hans_HK.UTF-8"
    "LC_MESSAGES=$B LANG=$A"
    "LC_MESSAGES= LANG=$A"
    "LC_MESSAGES=C LANG=$A"
    "LC_ALL=$B LC_MESSAGES=$A LANG=$A"
    "LC_ALL= LANG=$A"
    "LC_ALL=C LANG=$A"
    "LC_ALL=POSIX LANG=$A"
    "LC_ALL=C.UTF-8 LANG=$A"
    "LC_ALL=POSIX.UTF-8 LANG=$A"
)
POSIX_FALLBACK_VARIANTS=(
    "LANG=en-US-POSIX-abcde-abcde-u-nu-arab"
    "LANG=en-US-POSIX-1234-1234-u-nu-arab"
    "LANG=en-US-POSIX-abcde-u-a"
    "LANG=en-US-POSIX-zzzzz-u-a"
    "LANG=en-US-POSIX-zzzzz-u-nu-arab"
    "LANG=en-US-POSIX-abcde"
)
EXTENSION_LOCALES=(
    en-US-a-foo en-US-x-foo en-US-b-foo en-US-0-foo en-US-a-foo-b-bar
    en-US-u-nu-latn en-US-u-ca-gregory en-US-u-foo en-US-u-ca
    en-US-u-1234 en-US-u-00 en-US-u en-US-u-a en-US-u-123456789
    en-US-t-de-DE en-US-t-foo en-US-t-a en-US-t
    en-US-a-foo-u-nu-latn en-US-x-foo-u-nu-latn en-US-a-foo-u-a
    en-US-a-foo-t-de-DE en-US-u-ca-gregory-123456789 en-US-t-de-DE-123456789
    en-US-u-nu-latn-u-ca-gregory en-US-a-foo-a-bar-u-nu-latn
    en-US-U-NU-ARAB en-US-T-de-DE en-US-A-FOO de-DE-a-foo
    de-Latn-DE-a-foo i-klingon-a-foo en-GB-oed-a-foo en-US-POSIX-a-foo
    en-US-abcde en-US-zzzzz-abcde de-DE_phoneb
    en-US-x-foo-u-a en-US-u-a0 en-US-u-aa en-US-U-ca-gregory-u-nu-latn
    en-US-a-foo-a-bar-u-a en-US-u-co-phoneb-a-foo
    de-DE-u-co-phonebk-nu-arab en-US-a-foo-u-co-phoneb
    en-US-x-lvariant-foo en-US-x-foo-lvariant-bar
    en-US-abcde-x-lvariant-abcde en-US-POSIX-x-lvariant-foo
    en-US-zzzzz-x-lvariant-abcde en-US-abcde-x-lvariant-ABCDE
    en-US-abcde-x-lvariant-abcde-bar en-US-x-lvariant-foo-foo-bar-foo
    en-US-zzzzz-abcde-x-lvariant-abcde-bar-zzzzz en-US-POSIX-x-lvariant-POSIX-bar
    en-US-POSIX-x-foo-lvariant-POSIX-bar en-US-POSIX-a-foo-x-lvariant-POSIX-bar
    en-US-POSIX-u-nu-arab-x-lvariant-POSIX-bar
)
VARIANT_LOCALES=(
    en-US-POSIX-abcde-abcde-u-nu-arab en-US-abcde-POSIX-abcde-u-nu-arab
    en-US-POSIX-abcde-u-nu-arab en-US-abcde-POSIX-u-nu-arab
    en-US-zzzzz-POSIX-u-nu-latn en-US-POSIX-zzzzz-u-nu-latn
    en-US-POSIX-1234-u-ca-gregory en-US-1234-POSIX-u-ca-gregory
    en-US-POSIX-abcde-ab-u-nu-arab en-US-POSIX-abcde-abcdefghi-u-nu-arab
    en-US-POSIX-abcde--u-nu-arab en-US-POSIX-abcde-ABCDE-u-nu-arab
    en-US-POSIX-abcde-x-foo en-US-POSIX-abcde-a-foo
    en-US-POSIX-abcde-t-de-DE en-US-POSIX-abcde-u-ca-gregory
    en-US-POSIX-abcde-U-NU-ARAB en-US-POSIX-abcde-T-de-DE
    en-US-POSIX-abcde-t-de-DE-123456789 en-US-POSIX-abcde-u-ca-gregory-123456789
    en-US-POSIX-abcde-a-foo-u-nu-latn en-US-POSIX-abcde-x-foo-u-nu-latn
    en-US-POSIX-abcde en-US-abcde-POSIX en-US-POSIX-abcde-abcde
    en-US-POSIX-1234 en-US-1234-POSIX
    de-DE-zzzzz-abcde-u-ca-gregory de-DE-abcde-zzzzz-u-ca-gregory
    de-DE-zzzzz-abcde de-DE-abcde-zzzzz en-US-zzzzz-abcde-u-nu-latn
    en-US-POSIX-abcde-u-a de-Latn-DE-zzzzz-abcde-u-nu-latn
    de-foo-US-zzzzz-abcde-u-nu-latn i-klingon-Latn-US-zzzzz-abcde-u-nu-latn
    de-foo-US-zzzzz-abcde-abcde-u-nu-latn
    en-GB-oed-zzzzz-abcde-u-nu-latn en-US-zzzzz-abcde_phoneb
    en-GB-oed-zzzzz-abcde-abcde-u-nu-latn
    en-GB-oed-zzzzz-abcde-u-a en-GB-oed-zzzzz-abcde-x-foo
    i-klingon-US-zzzzz-abcde
)

fixture="$(dirname "$0")/fixtures/default-culture-locale"
zz_symbol=XAD
zz_snapshot="$(dirname "$0")/expected/default-culture-zz-posix.txt"
if [ "$DN2CPP_OS" = windows ]; then
    zz_symbol=XAG
    zz_snapshot="$(dirname "$0")/expected/default-culture-zz-windows.txt"
fi
if _corelib_gate_check "$out" "default_culture|$PROJECT|$LOCALES|${ENV_VARIANTS[*]}|${POSIX_FALLBACK_VARIANTS[*]}|culture-fallback foo-US|${FALLBACK_CASES[*]}|culture-variant-order ${VARIANT_LOCALES[*]}|culture-extension-identities ${EXTENSION_LOCALES[*]}|user-number-digits|user-numeric-settings|zz-frozen:$zz_symbol|$platform_default|query-and-initialize|$_CG_CORELIB" \
        "$fixture/CMakeLists.txt" "$fixture/locale.c" "$zz_snapshot"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/4 Compiling C++ and running (subject vs real .NET, per locale) =="
compile_console "$out" "$PROJECT"

# The oracle has to be able to answer at all. An invariant-globalization runtime
# reports the empty culture for every locale; diffing against it would report a
# dn2cpp bug for a .NET that has no culture data installed.
default_culture_run probe oracle env LC_ALL=de_DE.UTF-8 LANG=de_DE.UTF-8 dotnet "$_CG_APP"
gate_output view "$probe" "$probe.records" --policy crlf
IFS= read -r probe < "$probe.records"
if [ "$probe" = "Name=" ]; then
    gate_skip "the host's .NET reports no culture for LC_ALL=de_DE.UTF-8 (invariant globalization / no ICU), so the oracle cannot answer the question this gate asks"
fi

for L in $LOCALES; do
    echo "-- LC_ALL=$L"
    default_culture_run native native env LC_ALL="$L" LANG="$L" "./$out/$PROJECT$EXE_EXT"
    default_culture_run oracle oracle env LC_ALL="$L" LANG="$L" dotnet "$_CG_APP"
    gate_output compare "$native" "$oracle"
    witness="$native"
    gate_output witness "$native" 'default culture identities end' --count any --policy crlf \
        || { echo "FAIL: default culture identities did not run" >&2; exit 1; }
    gate_output witness "$native" 'culture host fallback data end' --count any --policy crlf \
        || { echo "FAIL: culture host fallback data did not run" >&2; exit 1; }
    gate_output witness "$native" 'culture variant order end' --count any --policy crlf \
        || { echo "FAIL: culture variant identities did not run" >&2; exit 1; }
    gate_output witness "$native" 'culture extension identities end' --count any --policy crlf \
        || { echo "FAIL: culture extension identities did not run" >&2; exit 1; }
    gate_output witness "$native" 'default culture user number digits end' --count any --policy crlf \
        || { echo "FAIL: user number digits did not run" >&2; exit 1; }
    if [ "$DN2CPP_OS" = windows ]; then
        gate_output witness "$native" 'Windows user numeric settings end' --count any --policy crlf \
            || { echo "FAIL: Windows user numeric settings did not run" >&2; exit 1; }
        default_culture_run before native env LC_ALL="$L" LANG="$L" "./$out/$PROJECT$EXE_EXT" before-user-numbers
        prefix="$witness.prefix"
        gate_output select "$witness" "$prefix" --before '== Windows user numeric settings ==' --policy crlf
        gate_output compare "$prefix" "$before"
        default_culture_run before oracle env LC_ALL="$L" LANG="$L" dotnet "$_CG_APP" before-user-numbers
        prefix="$oracle.prefix"
        gate_output select "$oracle" "$prefix" --before '== Windows user numeric settings ==' --policy crlf
        gate_output compare "$prefix" "$before"
    fi
    LC_ALL=C sed 's/^/   /' "$native"
    for name in "${FALLBACK_CASES[@]}"; do
        default_culture_run native native env LC_ALL="$L" LANG="$L" "./$out/$PROJECT$EXE_EXT" culture-fallback "$name"
        default_culture_run oracle oracle env LC_ALL="$L" LANG="$L" dotnet "$_CG_APP" culture-fallback "$name"
        if [ "$name" = foo-ZZ ]; then
            default_culture_assert_zz_fallback "$native" "$oracle" "$zz_symbol"
        else
            gate_output compare "$native" "$oracle"
        fi
        gate_output witness "$native" 'culture host fallback data end' --count any --policy crlf \
            || { echo "FAIL: fallback data for $name under $L did not run" >&2; exit 1; }
    done
done

ran=0
zz_env_checked=0
for V in "${ENV_VARIANTS[@]}"; do
    echo "-- env: ${V:-none of LC_ALL LC_MESSAGES LANG}"
    run_locale_env native native "$V" "./$out/$PROJECT$EXE_EXT"
    run_locale_env oracle oracle "$V" dotnet "$_CG_APP"
    if [ "$V" = LANG=zz_ZZ.UTF-8 ]; then
        zz_env_checked=1
    fi
    if [ "$V" = LANG=zz_ZZ.UTF-8 ] && [ "$DN2CPP_OS" != windows ]; then
        default_culture_assert_zz_default "$native" "$oracle" "$zz_symbol"
    else
        gate_output compare "$native" "$oracle"
    fi
    ran=$((ran + 1))
done
[ "$zz_env_checked" -eq 1 ] \
    || { echo 'FAIL: the zz-ZZ default-culture env row did not run' >&2; exit 1; }
echo 'OK: zz-ZZ default-culture env row'
platform_default="${platform_default#Name=}"
echo "env variants: $ran of ${#ENV_VARIANTS[@]} match real .NET except frozen ZZ currency (platform default: ${platform_default:-invariant})"

if [ "$DN2CPP_OS" = windows ]; then
    for name in "$platform_default" ja-JP en-US de-DE fr-FR hi-IN ca-AD ""; do
        default_culture_run lcid oracle dotnet "$_CG_APP" culture-user-lcid "$name"
        gate_output view "$lcid" "$lcid.records" --policy crlf
        lcid_file="$lcid"
        IFS= read -r lcid < "$lcid_file.records"
        [[ "$lcid" =~ ^[0-9]+$ ]] || { echo 'FAIL: invalid culture LCID' >&2; exit 1; }
        gate_assert_text "$lcid_file" "$lcid"
        for first in name-true name-false lcid-true lcid-false cached-name cached-lcid; do
            default_culture_run native native "./$out/$PROJECT$EXE_EXT" culture-user-numbers "$name" "$lcid" "$first"
            default_culture_run oracle oracle dotnet "$_CG_APP" culture-user-numbers "$name" "$lcid" "$first"
            gate_output compare "$native" "$oracle"
            gate_output witness "$native" 'Windows user numeric settings end' --count any --policy crlf \
                || { echo "FAIL: Windows numeric settings for $name ($first) did not finish" >&2; exit 1; }
        done
    done
fi

# Numeric resource fallback consumes the raw host's variant sequence independently
# of the managed default culture's variant ordering.
for V in "${POSIX_FALLBACK_VARIANTS[@]}"; do
    echo "-- numeric fallback env: $V"
    run_locale_env native native "$V" "./$out/$PROJECT$EXE_EXT" culture-fallback foo-US
    run_locale_env oracle oracle "$V" dotnet "$_CG_APP" culture-fallback foo-US
    gate_output compare "$native" "$oracle"
    gate_output witness "$native" 'culture host fallback data end' --count any --policy crlf \
        || { echo "FAIL: numeric fallback for $V did not run" >&2; exit 1; }
done

for name in "${VARIANT_LOCALES[@]}"; do
    echo "-- variant identity env: LANG=$name"
    run_locale_env native native "LANG=$name" "./$out/$PROJECT$EXE_EXT" culture-variant-order
    run_locale_env oracle oracle "LANG=$name" dotnet "$_CG_APP" culture-variant-order
    gate_output compare "$native" "$oracle"
    gate_output witness "$native" 'culture variant order end' --count any --policy crlf \
        || { echo "FAIL: default variant identities for $name did not run" >&2; exit 1; }
done

for name in "${EXTENSION_LOCALES[@]}"; do
    echo "-- extension identity env: LANG=$name"
    run_locale_env native native "LANG=$name" "./$out/$PROJECT$EXE_EXT" culture-extension-identities
    run_locale_env oracle oracle "LANG=$name" dotnet "$_CG_APP" culture-extension-identities
    gate_output compare "$native" "$oracle"
    gate_output witness "$native" 'culture extension identities end' --count any --policy crlf \
        || { echo "FAIL: default extension identities for $name did not run" >&2; exit 1; }
done

# A controlled libc query makes this branch independent of installed locales.
# Inject into both processes so the expectation still comes from real ICU/.NET.
if [ "$DN2CPP_OS" != windows ]; then
    "$CMAKE" -S "$fixture" -B "$out/locale-init-build" -G Ninja
    "$CMAKE" --build "$out/locale-init-build"
    if [ "$DN2CPP_OS" = macos ]; then
        injection="DYLD_INSERT_LIBRARIES=$PWD/$out/locale-init-build/libdefault_culture_locale.dylib"
    else
        injection="LD_PRELOAD=$PWD/$out/locale-init-build/libdefault_culture_locale.so"
    fi
    for query in "$B" C POSIX C.UTF-8; do
        echo "-- libc messages query: $query"
        run_locale_env native native "LANG=$A" env "$injection" "DN2CPP_GATE_QUERY_LOCALE=$query" "./$out/$PROJECT$EXE_EXT"
        run_locale_env oracle oracle "LANG=$A" env "$injection" "DN2CPP_GATE_QUERY_LOCALE=$query" dotnet "$_CG_APP"
        gate_output compare "$native" "$oracle"
        if [ "$query" = "$B" ]; then
            expected_name="${B%.UTF-8}"
            printf '%s\n' "Name=${expected_name/_/-}" > "$oracle.name.expected"
            gate_output prefix "$oracle" "$oracle.name.expected" --policy crlf
        fi
    done
    if [ "$DN2CPP_OS" = macos ]; then
        echo "-- initialized libc locale with empty LC_ALL"
        run_locale_env native native "LC_ALL= LANG=$A" env "$injection" DN2CPP_GATE_INITIALIZE_LOCALE=1 "./$out/$PROJECT$EXE_EXT"
        run_locale_env oracle oracle "LC_ALL= LANG=$A" env "$injection" DN2CPP_GATE_INITIALIZE_LOCALE=1 dotnet "$_CG_APP"
        gate_output compare "$native" "$oracle"
        expected_name="${A%.UTF-8}"
        printf '%s\n' "Name=${expected_name/_/-}" > "$oracle.name.expected"
        gate_output prefix "$oracle" "$oracle.name.expected" --policy crlf
    fi
fi

echo "-- frozen unknown-region currency: foo-ZZ and zz-ZZ"
(
    LC_ALL=en_US.UTF-8 LANG=en_US.UTF-8 \
        DN2CPP_OUT_SUFFIX=-zz-frozen DN2CPP_GATE_RUN_ARGS=culture-zz-currency \
        corelib_freeze_gate "$PROJECT" "$zz_snapshot"
)

gate_cache_commit
