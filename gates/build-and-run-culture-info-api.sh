#!/usr/bin/env bash
# CultureInfo's object model — the parts that answer from per-culture METADATA
# rather than from the NumberFormatInfo separators the formatting gates cover.
# NativeName / DisplayName, GetCultures(CultureTypes), CultureInfo.GetFormat(Type)
# (the direct IFormatProvider implementation on CultureInfo — a plain call token,
# NOT the interface slot), and Calendar.
#
# THREE SECTIONS ARE HERE FOR THEIR SUBJECT, NOT FOR THE HEADING, and each would
# look prunable to a reader tidying this bucket by theme:
#
#   -- LCID -- is a two-directional test of CultureInfo.LCID and the
#      GetCultureInfo(int) reverse lookup over the runtime's culture table. Its
#      point is the THIRD answer: a culture the table does not carry reports 4096
#      (LOCALE_CUSTOM_UNSPECIFIED), the value real .NET reports for a culture it
#      has no LCID for, rather than the invariant 127 that was indistinguishable
#      from actually being invariant.
#
#   -- table breadth -- is a NUMBER-FORMATTING test, despite sitting under an
#      object-model heading. It asserts the rendering — not merely the Name — of
#      cultures that the six-entry table used to answer with invariant SYMBOLS
#      under a correct NAME. That failure was only ever visible as "a diff gate
#      red on one developer's machine", so a name-only assert would have passed
#      throughout. Every value is produced with an EXPLICIT culture, so the host
#      cannot move it. It is also the only place the table's CLDR-REVISED rows can
#      be asserted at all — ur-PK's 3-then-2 grouping and its zero currency decimal
#      digits, which the ICU a host ships answers differently from the ICU the table
#      was cut from, so no live-diff bucket can hold them.
#
#   -- CurrentUICulture -- is the only place in the corpus that can see the second
#      culture axis at all. Every bucket driver pins it as its second statement,
#      which makes a working setter and a discarded-argument no-op produce
#      identical output everywhere else; this section sets it explicitly, asserts
#      it is a slot INDEPENDENT of CurrentCulture (an aliased getter or setter
#      fails here and nowhere else), and restores the pin before the next section.
#
# The existing prefix uses a frozen snapshot (gates/expected/culture-info-api.txt)
# for THREE deliberate divergences. Two are CultureInfo's own:
# dn2cpp constructs no Calendar object (the real getter reads intrinsic-mapped
# CultureInfo fields and a synthesized GregorianCalendar would pull the unmodeled
# CalendarData subtree), so it traps loudly (PlatformNotSupportedException) instead
# of returning a GregorianCalendar — the "a stripped type throws, never answers
# empty" discipline; and GetCultures / NativeName / DisplayName answer for the
# invariant culture alone, those being ICU inventory and display-name questions
# dn2cpp carries no table for. The third belongs to the escape sections below: the
# loud trap for SearchValues<T> prints a refusal where real .NET prints "ok"
# (Assembly/Module wrap instead, so their lines print the "ok" real .NET prints).
# Everything else is byte-identical against real .NET under en_US and de_DE.
# CoreLib only.
# The appended culture-name section diffs against live .NET: validation,
# canonical casing, supported variant formatting, cache identity, typed refusal
# fields/messages and CreateSpecificCulture's retry. Unmodeled regions assert
# identity only; script regressions prevent using an incompatible regional row.
# The locale-data tail covers aliases, alternate-sort metadata, numbering systems,
# regional numeric data and neutral-to-specific mappings. Uppercase constructor
# extensions run in a fresh process before lowercase CultureData cache entries.
# Separate override cases pin which constructor first owns each CultureData entry.
# Numbering data covers complete unknown Unicode values, locale-specific signs
# and patterns, and independent number, currency and percent grouping. Each
# numbering case also runs in a fresh process before any CultureData cache entry.
# Algorithmic numbering preserves ICU's invalid grouping data and checks the
# managed formatter's typed failure independently for each numeric format.
# Undefined-language and grandfathered names assert identity without reading
# host-dependent numeric data; the default-culture gate covers that data.
# Bare POSIX and unknown tags have host-dependent ICU acceptance, so they
# cannot be unconditional live-diff inputs.
#
# Its LAST TWO sections are not about CultureInfo's API at all; do not prune this
# bucket by theme.
#   - CultureEscapeSubset: the object-escape wrap for the HEADERLESS intrinsic
#     representation (CultureInfo/NumberFormatInfo/TextInfo/IFormatProvider all
#     lower to `const Dn2CppNumberFormatInfo*`) at the mainstream boundaries.
#   - CultureEscapeResidueSubset: the boundaries whose IL spells no
#     conversion — a type-erased ldind.ref/stind.ref byref, a covariant method
#     group AND the delegate VARIANCE conversion (which emits no IL at all, so
#     f_method carries one NFI-erased ABI), a `constrained.` GetType in a generic
#     body, the erased interface slot of a NON-GENERIC implementer (which failed
#     silently, with an empty CultureInfo.Name), and the reflection Invoke thunk
#     shape. Its tail asserts the other headerless representations:
#     Assembly/Module WRAP (f-asm/f-mod print real .NET's "ok"; the deep asserts
#     are ReflectTypes' AssemblyIdentitySubset), while SearchValues<T> — whose
#     .NET identity is a private per-shape subclass with no fixed handle to
#     carry — keeps the LOUD TRAP.
source "$(dirname "$0")/_common.sh"

# The appended section has parity semantics; the existing prefix retains its
# deliberate divergences and is still compared to the unchanged snapshot.
gate_extra_asserts() {
    local out="$1" native prefix
    native=$(run_bounded "./$out/CultureInfoApi")
    native=$(strip_cr_win "$native")
    prefix=$(awk '/^== culture name resolution ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(cat "$(dirname "$0")/expected/culture-info-api.txt")"
    grep -Fxq 'culture name resolution end' <<< "$native" \
        || { echo "FAIL: culture names section did not finish" >&2; return 1; }
}
DN2CPP_GATE_RUN_ARGS=before-culture-names
corelib_freeze_gate CultureInfoApi "$(dirname "$0")/expected/culture-info-api.txt" \
    --link-xml samples/dotnet/CultureInfoApi/link.xml

LOCALE_CASES=(
    culture-locale-upper
    culture-locale-cache-true
    culture-locale-cache-false
    culture-sort-strings
    culture-sort-upper-key
    culture-numbering-boundaries
    culture-wrapper-alias-first
    culture-wrapper-base-first
    culture-wrapper-sort-first
    culture-wrapper-unicode-first
    culture-wrapper-private-first
    culture-wrapper-canonical-first
    culture-primary-currency
)
NUMBERING_CASES=(
    en-US-u-nu-arab-foo
    en-US-u-nu-unknown
    hi-IN-u-nu-arab
    bn-IN-u-nu-arab
    fr-FR-u-nu-arab
    ja-JP-u-nu-arab
    en-US-u-nu
    en-US-u-nu-roman
    zh-CN-u-nu-hans
    ar-EG-u-nu-native
    ar-EG-u-nu-finance
    ja-JP-u-nu-traditio
    az-Latn-AZ-u-nu-arab
)
FALLBACK_CASES=(
    und-US und-DE und-FR und-JP und-Latn-US
    und-US-u-nu-arab und-US-u-nu-latn und-US-u-nu-native
    und-US-u-nu-finance und-US-u-nu-traditio und-US-u-nu-foo
    foo foo-US foo-u-nu-default foo-US-u-nu-default
    de-FOO de-FOO-u-ca-gregory de-FOO-u-nu-latn de-FOO-u-nu-arab
    de-FOO-u-nu-native de-FOO-u-nu-finance de-FOO-u-nu-traditio
    de-FOO-u-nu-default de-FOO-u-nu-foo en-GB-oed en-GB-oed-u-nu-foo
    i-klingon i-klingon-u-nu-foo i-default i-default-u-nu-foo
    i-enochian i-enochian-u-nu-foo i-mingo i-mingo-u-nu-foo
    zh-min zh-min-u-nu-foo zh-min-nan-u-nu-foo
    nv jbo cmn i-navajo-u-nu-native art-lojban-u-nu-native zh-guoyu-u-nu-native
    de-foo-bar-u-nu-latn de-foo-bar-US-u-nu-latn de-foo-Latn-US-u-nu-latn
    de-foo-bar-baz-US-u-nu-latn de-foo-bar-baz-zzz-US-u-nu-latn
    foo-QQ foo-999 und-QQ foo-ZZ i-default-US-u-nu-arab
    i-enochian-US-u-nu-arab i-mingo-US-u-nu-arab zh-min-US-u-nu-arab
    i-default-abcde-u-nu-arab i-enochian-US-U-NU-ARAB
    i-klingon-Latn-US i-klingon-Latn-US-u-nu-latn zh-min-nan-Latn-US-u-nu-latn
)
gate_extra_asserts() {
    local out="$1" native expected mode marker
    native=$(run_bounded "./$out/CultureInfoApi" culture-names)
    grep -Fxq 'culture locale data end' <<< "$(strip_cr_win "$native")" \
        || { echo "FAIL: culture locale data section did not finish" >&2; return 1; }
    grep -Fxq 'culture numbering data end' <<< "$(strip_cr_win "$native")" \
        || { echo "FAIL: culture numbering data section did not finish" >&2; return 1; }
    grep -Fxq 'culture fallback identities end' <<< "$(strip_cr_win "$native")" \
        || { echo "FAIL: culture fallback identities section did not finish" >&2; return 1; }
    grep -Fxq 'culture primary currency end' <<< "$(strip_cr_win "$native")" \
        || { echo "FAIL: culture primary currency section did not finish" >&2; return 1; }
    for mode in "${LOCALE_CASES[@]}"; do
        native=$(run_bounded "./$out/CultureInfoApi" "$mode")
        expected=$(run_bounded dotnet "$_CG_APP" "$mode")
        assert_output "$native" "$expected"
        case "$mode" in
            culture-locale-upper) marker='culture uppercase extensions end' ;;
            culture-locale-cache-*) marker='culture data cache end' ;;
            culture-sort-strings|culture-sort-upper-key) marker='culture sort strings end' ;;
            culture-numbering-boundaries) marker='culture numbering boundaries end' ;;
            culture-wrapper-*) marker='culture wrapper cache end' ;;
            culture-primary-currency) marker='culture primary currency end' ;;
        esac
        grep -Fxq "$marker" <<< "$(strip_cr_win "$native")" \
            || { echo "FAIL: $mode did not finish" >&2; return 1; }
    done
    for mode in "${NUMBERING_CASES[@]}"; do
        native=$(run_bounded "./$out/CultureInfoApi" culture-numbering-data "$mode")
        expected=$(run_bounded dotnet "$_CG_APP" culture-numbering-data "$mode")
        assert_output "$native" "$expected"
        grep -Fxq 'culture numbering data end' <<< "$(strip_cr_win "$native")" \
            || { echo "FAIL: numbering data for $mode did not finish" >&2; return 1; }
    done
    for mode in "${FALLBACK_CASES[@]}"; do
        native=$(run_bounded "./$out/CultureInfoApi" culture-fallback-identities "$mode")
        expected=$(run_bounded dotnet "$_CG_APP" culture-fallback-identities "$mode")
        assert_output "$native" "$expected"
        grep -Fxq 'culture fallback identities end' <<< "$(strip_cr_win "$native")" \
            || { echo "FAIL: fallback identities for $mode did not finish" >&2; return 1; }
    done
}
DN2CPP_GATE_RUN_ARGS=culture-names
DN2CPP_GATE_EXTRA_CONTEXT="${LOCALE_CASES[*]} ${NUMBERING_CASES[*]} ${FALLBACK_CASES[*]}"
DN2CPP_OUT_SUFFIX=-names
corelib_diff_gate CultureInfoApi --link-xml samples/dotnet/CultureInfoApi/link.xml
