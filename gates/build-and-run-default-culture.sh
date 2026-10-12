#!/usr/bin/env bash
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
#
# It gate_skips only where real .NET itself has no cultures — an ICU-less host
# in invariant-globalization mode, where the oracle answers "" to every locale
# while the subject, which needs no ICU, answers the host's. That is not a
# divergence to report; it is an oracle that has been told there is nothing to
# ask about.
source "$(dirname "$0")/_common.sh"

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
run_locale_env() {
    local assigns=()
    read -r -a assigns <<< "$1"
    shift
    run_bounded env -u LC_ALL -u LC_MESSAGES -u LANG ${assigns[@]+"${assigns[@]}"} "$@"
}
platform_default="$(run_locale_env "" dotnet "$_CG_APP")"
platform_default="${platform_default%%$'\n'*}"
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

fixture="$(dirname "$0")/fixtures/default-culture-locale"
if _corelib_gate_check "$out" "default_culture|$PROJECT|$LOCALES|${ENV_VARIANTS[*]}|${POSIX_FALLBACK_VARIANTS[*]}|culture-fallback foo-US|${FALLBACK_CASES[*]}|$platform_default|query-and-initialize|$_CG_CORELIB" \
        "$fixture/CMakeLists.txt" "$fixture/locale.c"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/4 Compiling C++ and running (subject vs real .NET, per locale) =="
compile_console "$out" "$PROJECT"

# The oracle has to be able to answer at all. An invariant-globalization runtime
# reports the empty culture for every locale; diffing against it would report a
# dn2cpp bug for a .NET that has no culture data installed.
probe="$(LC_ALL=de_DE.UTF-8 LANG=de_DE.UTF-8 dotnet "$_CG_APP")"
probe="${probe%%$'\n'*}"
if [ "$probe" = "Name=" ]; then
    gate_skip "the host's .NET reports no culture for LC_ALL=de_DE.UTF-8 (invariant globalization / no ICU), so the oracle cannot answer the question this gate asks"
fi

for L in $LOCALES; do
    echo "-- LC_ALL=$L"
    native="$(LC_ALL="$L" LANG="$L" run_bounded "./$out/$PROJECT")"
    oracle="$(LC_ALL="$L" LANG="$L" run_bounded dotnet "$_CG_APP")"
    assert_output "$native" "$oracle"
    witness=$(strip_cr_win "$native")
    grep -Fxq 'default culture identities end' <<< "$witness" \
        || { echo "FAIL: default culture identities did not run" >&2; exit 1; }
    grep -Fxq 'culture host fallback data end' <<< "$witness" \
        || { echo "FAIL: culture host fallback data did not run" >&2; exit 1; }
    printf '%s\n' "$native" | LC_ALL=C sed 's/^/   /'
    for name in "${FALLBACK_CASES[@]}"; do
        native="$(LC_ALL="$L" LANG="$L" run_bounded "./$out/$PROJECT" culture-fallback "$name")"
        oracle="$(LC_ALL="$L" LANG="$L" run_bounded dotnet "$_CG_APP" culture-fallback "$name")"
        assert_output "$native" "$oracle"
        grep -Fxq 'culture host fallback data end' <<< "$(strip_cr_win "$native")" \
            || { echo "FAIL: fallback data for $name under $L did not run" >&2; exit 1; }
    done
done

ran=0
for V in "${ENV_VARIANTS[@]}"; do
    echo "-- env: ${V:-none of LC_ALL LC_MESSAGES LANG}"
    native="$(run_locale_env "$V" "./$out/$PROJECT")"
    oracle="$(run_locale_env "$V" dotnet "$_CG_APP")"
    assert_output "$native" "$oracle"
    ran=$((ran + 1))
done
platform_default="${platform_default#Name=}"
echo "env variants: $ran of ${#ENV_VARIANTS[@]} match real .NET (platform default: ${platform_default:-invariant})"

# Numeric resource fallback consumes the raw host's variant sequence independently
# of the managed default culture's variant ordering.
for V in "${POSIX_FALLBACK_VARIANTS[@]}"; do
    echo "-- numeric fallback env: $V"
    native="$(run_locale_env "$V" "./$out/$PROJECT" culture-fallback foo-US)"
    oracle="$(run_locale_env "$V" dotnet "$_CG_APP" culture-fallback foo-US)"
    assert_output "$native" "$oracle"
    grep -Fxq 'culture host fallback data end' <<< "$(strip_cr_win "$native")" \
        || { echo "FAIL: numeric fallback for $V did not run" >&2; exit 1; }
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
        native=$(run_locale_env "LANG=$A" env "$injection" "DN2CPP_GATE_QUERY_LOCALE=$query" "./$out/$PROJECT")
        oracle=$(run_locale_env "LANG=$A" env "$injection" "DN2CPP_GATE_QUERY_LOCALE=$query" dotnet "$_CG_APP")
        assert_output "$native" "$oracle"
        if [ "$query" = "$B" ]; then
            expected_name="${B%.UTF-8}"
            assert_output "${oracle%%$'\n'*}" "Name=${expected_name/_/-}"
        fi
    done
    if [ "$DN2CPP_OS" = macos ]; then
        echo "-- initialized libc locale with empty LC_ALL"
        native=$(run_locale_env "LC_ALL= LANG=$A" env "$injection" DN2CPP_GATE_INITIALIZE_LOCALE=1 "./$out/$PROJECT")
        oracle=$(run_locale_env "LC_ALL= LANG=$A" env "$injection" DN2CPP_GATE_INITIALIZE_LOCALE=1 dotnet "$_CG_APP")
        assert_output "$native" "$oracle"
        expected_name="${A%.UTF-8}"
        assert_output "${oracle%%$'\n'*}" "Name=${expected_name/_/-}"
    fi
fi

gate_cache_commit
