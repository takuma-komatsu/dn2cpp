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
# Extension-only names follow ICU keyword acceptance while preserving legacy
# spellings. Fresh constructor/cache orders pin which casing owns culture data.
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
    culture-missing-cache-first
    culture-missing-cache-empty-first
    culture-missing-upper-constructor-first
    culture-missing-upper-cached-first
)
MISSING_NUMBER_CASES=(bare nul upper lower invariant und)
INVARIANT_CACHE_ORDERS=(
    lcid-empty-missing lcid-missing-empty
    empty-lcid-missing empty-missing-lcid
    missing-lcid-empty missing-empty-lcid
)
INSTANCE_CULTURES=(i-enochian en-US en "")
UNDEFINED_EXTENSION_CASES=(und und-u-a und-t-a und-u-123456789 und-a-a und-a-123456789)
EXTENSION_CASES=(
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
EXTENSION_ORDER_PAIRS=(
    'en-US-A-FOO|en-US-a-foo' 'en-US-U-NU-ARAB|en-US-u-nu-arab'
    'en-US-U-CA-GREGORY|en-US-u-ca-gregory' 'en-US-T-de-DE|en-US-t-de-DE'
    'en-US-a-foo|en-US' 'en-US|en-US-a-foo'
)
EQUALITY_PAIRS=(
    'en-US|en-US' 'en-US|EN-us' 'de-DE|de-DE_phoneb' 'de-DE_phoneb|DE-de_PHONEB'
    'i-enochian|' 'i-enochian|i-enochian' 'i-klingon|tlh'
    'en-US-u-nu-latn|en-US-u-nu-LATN' 'en-US-u-nu-latn|en-US-u-nu-arab'
    'en-US-POSIX-abcde-u-nu-latn|en-US-abcde-POSIX-u-nu-latn'
)
VARIANT_CASES=(
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
    grep -Fxq 'culture missing number format end' <<< "$(strip_cr_win "$native")" \
        || { echo "FAIL: culture missing number format section did not finish" >&2; return 1; }
    grep -Fxq 'culture variant order end' <<< "$(strip_cr_win "$native")" \
        || { echo "FAIL: culture variant order section did not finish" >&2; return 1; }
    grep -Fxq 'culture invariant lcid cache end' <<< "$(strip_cr_win "$native")" \
        || { echo "FAIL: culture invariant LCID cache section did not finish" >&2; return 1; }
    grep -Fxq 'culture instance number format end' <<< "$(strip_cr_win "$native")" \
        || { echo "FAIL: culture instance number format section did not finish" >&2; return 1; }
    grep -Fxq 'culture integer constructors end' <<< "$(strip_cr_win "$native")" \
        || { echo "FAIL: culture integer constructor section did not finish" >&2; return 1; }
    grep -Fxq 'culture instance equality end' <<< "$(strip_cr_win "$native")" \
        || { echo "FAIL: culture instance equality section did not finish" >&2; return 1; }
    grep -Fxq 'culture specific instances end' <<< "$(strip_cr_win "$native")" \
        || { echo "FAIL: culture specific instances section did not finish" >&2; return 1; }
    grep -Fxq 'culture extension identities end' <<< "$(strip_cr_win "$native")" \
        || { echo "FAIL: culture extension identities section did not finish" >&2; return 1; }
    grep -Fxq 'culture undefined extension metadata end' <<< "$(strip_cr_win "$native")" \
        || { echo "FAIL: culture undefined extension metadata section did not finish" >&2; return 1; }
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
            culture-missing-cache-*) marker='culture missing number format cache end' ;;
            culture-missing-upper-*) marker='culture missing number format uppercase cache end' ;;
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
    for mode in "${MISSING_NUMBER_CASES[@]}"; do
        native=$(run_bounded "./$out/CultureInfoApi" culture-missing-number-format "$mode")
        expected=$(run_bounded dotnet "$_CG_APP" culture-missing-number-format "$mode")
        assert_output "$native" "$expected"
        grep -Fxq 'culture missing number format end' <<< "$(strip_cr_win "$native")" \
            || { echo "FAIL: missing number format for $mode did not finish" >&2; return 1; }
    done
    for mode in "${VARIANT_CASES[@]}"; do
        native=$(run_bounded "./$out/CultureInfoApi" culture-variant-order "$mode")
        expected=$(run_bounded dotnet "$_CG_APP" culture-variant-order "$mode")
        assert_output "$native" "$expected"
        grep -Fxq 'culture variant order end' <<< "$(strip_cr_win "$native")" \
            || { echo "FAIL: variant order for $mode did not finish" >&2; return 1; }
    done
    for mode in "${INVARIANT_CACHE_ORDERS[@]}"; do
        native=$(run_bounded "./$out/CultureInfoApi" culture-invariant-lcid-cache "$mode")
        expected=$(run_bounded dotnet "$_CG_APP" culture-invariant-lcid-cache "$mode")
        assert_output "$native" "$expected"
        grep -Fxq 'culture invariant lcid cache end' <<< "$(strip_cr_win "$native")" \
            || { echo "FAIL: invariant LCID cache order $mode did not finish" >&2; return 1; }
    done
    for mode in "${INSTANCE_CULTURES[@]}"; do
        native=$(run_bounded "./$out/CultureInfoApi" culture-instance-number-format "$mode")
        expected=$(run_bounded dotnet "$_CG_APP" culture-instance-number-format "$mode")
        assert_output "$native" "$expected"
        grep -Fxq 'culture instance number format end' <<< "$(strip_cr_win "$native")" \
            || { echo "FAIL: culture instance number format for '$mode' did not finish" >&2; return 1; }
    done
    native=$(run_bounded "./$out/CultureInfoApi" culture-integer-constructors)
    expected=$(run_bounded dotnet "$_CG_APP" culture-integer-constructors)
    assert_output "$native" "$expected"
    grep -Fxq 'culture integer constructors end' <<< "$(strip_cr_win "$native")" \
        || { echo "FAIL: culture integer constructor mode did not finish" >&2; return 1; }
    for mode in "${EQUALITY_PAIRS[@]}"; do
        native=$(run_bounded "./$out/CultureInfoApi" culture-instance-equality "${mode%%|*}" "${mode#*|}")
        expected=$(run_bounded dotnet "$_CG_APP" culture-instance-equality "${mode%%|*}" "${mode#*|}")
        assert_output "$native" "$expected"
        grep -Fxq 'culture instance equality end' <<< "$(strip_cr_win "$native")" \
            || { echo "FAIL: culture equality pair '$mode' did not finish" >&2; return 1; }
        grep -Fxq 'culture specific instances end' <<< "$(strip_cr_win "$native")" \
            || { echo "FAIL: culture specific instances for '$mode' did not finish" >&2; return 1; }
    done
    for mode in "${EXTENSION_CASES[@]}"; do
        native=$(run_bounded "./$out/CultureInfoApi" culture-extension-identities "$mode")
        expected=$(run_bounded dotnet "$_CG_APP" culture-extension-identities "$mode")
        assert_output "$native" "$expected"
        grep -Fxq 'culture extension identities end' <<< "$(strip_cr_win "$native")" \
            || { echo "FAIL: extension identity '$mode' did not finish" >&2; return 1; }
    done
    for mode in "${UNDEFINED_EXTENSION_CASES[@]}"; do
        native=$(run_bounded "./$out/CultureInfoApi" culture-undefined-extension-metadata "$mode")
        expected=$(run_bounded dotnet "$_CG_APP" culture-undefined-extension-metadata "$mode")
        assert_output "$native" "$expected"
        grep -Fxq 'culture undefined extension metadata end' <<< "$(strip_cr_win "$native")" \
            || { echo "FAIL: undefined extension metadata '$mode' did not finish" >&2; return 1; }
    done
    local pair order
    for pair in "${EXTENSION_ORDER_PAIRS[@]}"; do
        for order in constructor-first cached-first; do
            native=$(run_bounded "./$out/CultureInfoApi" culture-extension-order "${pair%%|*}" "${pair#*|}" "$order")
            expected=$(run_bounded dotnet "$_CG_APP" culture-extension-order "${pair%%|*}" "${pair#*|}" "$order")
            assert_output "$native" "$expected"
            grep -Fxq 'culture extension cache order end' <<< "$(strip_cr_win "$native")" \
                || { echo "FAIL: extension cache order '$pair' '$order' did not finish" >&2; return 1; }
        done
    done
}
DN2CPP_GATE_RUN_ARGS=culture-names
DN2CPP_GATE_EXTRA_CONTEXT="${LOCALE_CASES[*]} ${NUMBERING_CASES[*]} ${FALLBACK_CASES[*]} culture-missing-number-format ${MISSING_NUMBER_CASES[*]} culture-variant-order ${VARIANT_CASES[*]} culture-invariant-lcid-cache ${INVARIANT_CACHE_ORDERS[*]} culture-instance-number-format [${INSTANCE_CULTURES[*]}] culture-integer-constructors culture-instance-equality ${EQUALITY_PAIRS[*]} culture-extension-identities ${EXTENSION_CASES[*]} culture-undefined-extension-metadata ${UNDEFINED_EXTENSION_CASES[*]} culture-extension-order ${EXTENSION_ORDER_PAIRS[*]} constructor-first cached-first"
DN2CPP_OUT_SUFFIX=-names
corelib_diff_gate CultureInfoApi --link-xml samples/dotnet/CultureInfoApi/link.xml
