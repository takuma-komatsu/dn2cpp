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

# WHICH VARIABLE DECIDES. Real .NET takes the first of LC_ALL, LC_MESSAGES and
# LANG that is SET, an empty one included; a value that names no locale (empty,
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

if _corelib_gate_check "$out" "default_culture|$PROJECT|$LOCALES|$platform_default|$_CG_CORELIB"; then
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
    printf '%s\n' "$native" | LC_ALL=C sed 's/^/   /'
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

gate_cache_commit
