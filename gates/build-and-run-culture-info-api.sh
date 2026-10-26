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
# Undefined-language identities use the target ICU platform, including private-use
# acceptance and empty-name cache ownership independently of invariant data.
# Legacy fallback follows the target reference: Linux retains complete rejected
# tails, Apple removes later u/t/x subtags, and other targets keep their profile.
# Rejected first keywords preserve the legacy base when extlang promotion would
# erase it; accepted keywords and nonempty promoted bases take the ICU path.
# TwoLetterISOLanguageName diffs against live .NET; und names, where Apple's ICU
# diverges, pin the standard ICU answer natively.
# Windows rejects some identifiers before ISO metadata is available; constructor
# and cache refusals each participate in the live comparison. Legacy Chinese
# names retain their identity with platform-specific numeric and LCID data.
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

gate_run_logs_init culture_info_api "CultureInfoApi runs"

# Assign the raw stdout path; failed runs retain their bytes and command context.
culture_info_run() {
    local _culture_result="$1" _culture_side="$2" _culture_prefix _culture_code _culture_command
    shift 2
    _CULTURE_RUN_SEQUENCE=$((${_CULTURE_RUN_SEQUENCE:-0} + 1))
    _culture_prefix="$_GATE_RUN_LOG_DIR/${_CULTURE_RUN_SEQUENCE}-$_culture_side"
    printf -v "$_culture_result" '%s' "$_culture_prefix.stdout"
    printf -v _culture_command '%q ' "$@"
    gate_capture "$_culture_prefix" run_bounded "$@" || return 1
    printf '%s\n' "$_culture_command" > "$_culture_prefix.command"
    _culture_code=$(cat "$_culture_prefix.status")
    if [ "$_culture_code" -ne 0 ]; then
        echo "---- CultureInfoApi $_culture_side: $_culture_command diagnostics ----" >&2
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

culture_info_diagnostics_self_test() {
    local side code actual actual_code diagnostics child quoted_args
    child='
        [ "$#" -eq 5 ] && [ "$2" = "culture-fallback-identities" ] \
            && [ "$3" = "foo QQ" ] && [ "$4" = "" ] \
            && [ "$5" = "$(printf "foo\047QQ")" ] || exit 91
        printf "same output\n"
        printf "injected stderr\n" >&2
        exit "$1"
    '
    printf -v quoted_args '%q ' culture-fallback-identities "foo QQ" "" "foo'QQ"
    for side in native oracle; do
        diagnostics="$_GATE_RUN_LOG_DIR/self-test-$side.stderr"
        culture_info_run actual "$side" bash -c "$child" _ 0 \
            culture-fallback-identities "foo QQ" "" "foo'QQ" 2>"$diagnostics"
        gate_assert_text "$actual" "same output" \
            || { echo "FAIL: $side successful stdout or quoted arguments changed" >&2; return 1; }
        grep -Fxq 'injected stderr' "$diagnostics" \
            || { echo "FAIL: $side successful stderr disappeared" >&2; return 1; }
        printf 'line\r\n\n\000tail\r' > "$_GATE_RUN_LOG_DIR/self-test.raw"
        culture_info_run actual "$side" bash -c 'printf "line\r\n\n\000tail\r"' 2>"$diagnostics"
        gate_output compare "$actual" "$_GATE_RUN_LOG_DIR/self-test.raw"
        code=23
        [ "$side" = native ] || code=29
        if culture_info_run actual "$side" bash -c "$child" _ "$code" \
            culture-fallback-identities "foo QQ" "" "foo'QQ" 2>"$diagnostics"; then
            echo "FAIL: $side nonzero exit with matching stdout passed" >&2
            return 1
        else
            actual_code=$?
        fi
        [ "$actual_code" -eq "$code" ] \
            || { echo "FAIL: $side child exit code changed" >&2; return 1; }
        grep -Fq "CultureInfoApi $side:" "$diagnostics" \
            && grep -Fq "$quoted_args" "$diagnostics" \
            && grep -Fxq "exit code: $code" "$diagnostics" \
            && grep -Fxq 'termination: nonzero exit' "$diagnostics" \
            && grep -Fxq 'stdout:' "$diagnostics" \
            && grep -Fxq 'same output' "$diagnostics" \
            && grep -Fxq 'stderr:' "$diagnostics" \
            && grep -Fxq 'injected stderr' "$diagnostics" \
            || { echo "FAIL: $side failed-run diagnostics are incomplete" >&2; cat "$diagnostics" >&2; return 1; }
    done
    echo "OK: CultureInfoApi run diagnostics"
    return 0
}

culture_info_executable_self_test() (
    # Model Windows naming on every host, with a stale foreign-host sibling.
    # Override build/cache work only; publishing and native execution stay real.
    local EXE_EXT=.exe DN2CPP_GATE_RUN_ARGS=host-executable
    local HIGHWAY= SCALAR= DN2CPP_OUT_SUFFIX= DN2CPP_ORACLE_DOTNET_ENABLE_AVX=
    local ANDROID= IOS_SIM= IOS_DEV= WASM=
    local actual axis
    mkdir -p "$_GATE_RUN_LOG_DIR/executable-choice/build" \
        "$_GATE_RUN_LOG_DIR/executable-choice/native"
    cd "$_GATE_RUN_LOG_DIR/executable-choice"
    # Create bare names first: MSYS aliases a missing bare name to its .exe.
    printf '#!/usr/bin/env bash\nexit 91\n' > build/CultureInfoApi
    chmod +x build/CultureInfoApi
    cp build/CultureInfoApi native/CultureInfoApi
    if [ "$DN2CPP_OS" = windows ]; then
        cp "$(type -P echo)" build/CultureInfoApi.exe
    else
        printf '#!/usr/bin/env bash\nprintf "%%s\\n" "$1"\n' > build/CultureInfoApi.exe
        chmod +x build/CultureInfoApi.exe
    fi
    if cmp -s build/CultureInfoApi build/CultureInfoApi.exe; then
        echo 'FAIL: the executable-choice fixture has aliased siblings' >&2
        return 1
    fi
    printf 'host-executable\n' > expected.txt

    publish_native_assets build native CultureInfoApi
    cmp build/CultureInfoApi.exe native/CultureInfoApi.exe \
        || { echo 'FAIL: the host executable was not published' >&2; return 1; }
    cmp build/CultureInfoApi native/CultureInfoApi \
        || { echo 'FAIL: publishing replaced the foreign-host sibling' >&2; return 1; }

    _corelib_gate_out() { printf 'native\n'; }
    _corelib_gate_core() { _CG_CORELIB=unused; _CG_APP=unused; }
    _corelib_gate_check() { return 1; }
    compile_console() { return 0; }
    gate_cache_commit() { return 0; }
    gate_run_oracle() { cat expected.txt; }
    corelib_freeze_gate CultureInfoApi expected.txt
    corelib_diff_gate CultureInfoApi
    culture_info_run actual native "./native/CultureInfoApi$EXE_EXT" host-executable
    gate_assert_text "$actual" host-executable
    echo 'OK: CultureInfoApi host executable wins over foreign-host sibling'

    mkdir cross
    printf 'unpublished-target\n' > cross/CultureInfoApi
    cp build/CultureInfoApi.exe cross/CultureInfoApi.exe
    for axis in ANDROID IOS_SIM IOS_DEV WASM; do
        printf 'unpublished-target\n' > cross/CultureInfoApi
        ( export "$axis=1"; publish_native_assets build cross CultureInfoApi )
        cmp build/CultureInfoApi cross/CultureInfoApi \
            || { echo "FAIL: $axis did not publish the target executable" >&2; return 1; }
        cmp build/CultureInfoApi.exe cross/CultureInfoApi.exe \
            || { echo "FAIL: $axis publishing replaced the host sibling" >&2; return 1; }
    done
    echo 'OK: cross-built executable publishing keeps target naming'
    return 0
)

culture_info_diagnostics_self_test
culture_info_executable_self_test
if [ "${1:-}" = --self-test ]; then
    exit 0
fi

# Manual/opt-in parity requires the exact reference; normal suites exclude it.
bash "$_DN2CPP_GATE_ROOT/gates/check-culture-reference.sh"

# The appended section has parity semantics; the existing prefix retains its
# deliberate divergences and is still compared to the unchanged snapshot.
# Full/mode outputs are byte containers: platform exception text may contain NUL.
# Completion records are text-only; this does not assert full-run CLR parity.
# culture_info_run still requires status 0, and all prefix/snapshot checks remain.
gate_extra_asserts() {
    local out="$1" native prefix
    culture_info_run native native "./$out/CultureInfoApi$EXE_EXT"

    prefix="$native.prefix"
    gate_output select "$native" "$prefix" --before '== culture name resolution ==' --policy crlf
    gate_output compare "$prefix" "$(dirname "$0")/expected/culture-info-api.txt" --policy crlf
    gate_output byte-witness "$native" 'culture name resolution end' --count any --policy crlf \
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
REJECTED_EXTENSION_CASES=(
    en-und-x-123456789 en-und-x-lvariant en-und-u-a en-und-t-a en-und-a-a
    en-und-x-foo en-und-u-nu-latn en-und-t-de-DE en-und-a-foo
    en-und-a-foo-u-a en-und-u-ca-gregory-u-a
    en-und-Latn-x-123456789 en-und-US-x-123456789 en-und-abcde-x-123456789
    en-und en-und-US en-und-abcde und-u-a en-abc-u-a
    en-und-abc-def-ghi-a-foo en-und-abc-def-a-foo EN-und-U-A
)
UNDEFINED_PLATFORM_CASES=(
    "" und und-a-foo und-u-a und-t-a und-u-nu-latn
    und-U-nu-latn und-u-co-phonebk und-a-a und-a-a-u-nu-latn
    und-Latn-u-nu-latn und-US-u-nu-latn und-FOOBAR-u-nu-latn
    und-x-foo und-x-a und-x-foo-u-nu-latn und-x-lvariant und-x-foo-lvariant
    und-x-123456789 und-a-foo-x-foo und-x-lvariant-FOOBAR en-und-a-foo
)
if [ "$DN2CPP_OS" = linux ]; then
    UNDEFINED_PLATFORM_CASES+=(
        en-und-x-lvariant en-und-x-foo-lvariant en-und-x-LVARIANT
        en-und-a-a-x-lvariant en-und-Latn-x-lvariant en-und-US-x-lvariant
        x-1-US-u-nu-latn_phoneb x-foo-US-t-de-DE_phoneb
        x-foo-u-nu-latn_phoneb x-1-US-U-NU-LATN_phoneb
        en-US-x-foo_phoneb abcdefghij-x-foo abcd1-x-foo e1-x-foo
        i-klingon-x-lvariant-fooba
        x-a-US-u-co-phonebk_phoneb x-a-US-u-Co-phonebk_phoneb
        x-a-US-u-co-phonebk-t-de-DE_phoneb x-a-US-u-co-phonebk-u-nu-latn_phoneb
    )
fi
UNDEFINED_PLATFORM_CACHE_ORDERS=(
    'und-x-foo||cached-first' '|und-x-foo|cached-first'
    'und-a-foo||cached-first' '|und-a-foo|cached-first'
    'und-U-nu-latn|und-u-nu-latn|constructor-first'
    'und-U-nu-latn|und-u-nu-latn|cached-first'
)
if [ "$DN2CPP_OS" = windows ]; then
    UNDEFINED_PLATFORM_CASES+=(
        x-foo X-FOO x-lvariant x-LVARIANT-foo x-lvariant-foo x-lvariant-US
        x-a-US-u-nu-latn x-foo-US-u-nu-latn x-foo-US-U-NU-LATN
        x-1-US-u-nu-latn_phoneb x-1-US-t-de-de_phoneb
        x-foo-u-nu-latn_phoneb x-1-US-U-NU-LATN_phoneb
    )
    for private_name in x-lvariant-foo x-lvariant-US x-a-US-u-nu-latn x-foo-US-u-nu-latn; do
        UNDEFINED_PLATFORM_CACHE_ORDERS+=(
            "$private_name|x-foo|constructor-first" "$private_name|x-foo|cached-first"
            "x-foo|$private_name|cached-first"
        )
    done
fi
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
    'und-a-a-u-nu-latn|und-A-A-U-NU-LATN'
    'und-A-A-U-NU-LATN|und-a-a-u-nu-latn'
    'en-und-a-a-u-nu-latn|en-UND-A-A-U-NU-LATN'
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
# The locale family compares complete raw streams before checking completion.
# Malformed locale names can emit NUL without changing the text-only marker.
culture_info_assert_locale_completion() {
    local actual="$1" mode="$2" marker
    case "$mode" in
        culture-locale-upper) marker='culture uppercase extensions end' ;;
        culture-locale-cache-*) marker='culture data cache end' ;;
        culture-sort-strings|culture-sort-upper-key) marker='culture sort strings end' ;;
        culture-numbering-boundaries) marker='culture numbering boundaries end' ;;
        culture-wrapper-*) marker='culture wrapper cache end' ;;
        culture-primary-currency) marker='culture primary currency end' ;;
        culture-missing-cache-*) marker='culture missing number format cache end' ;;
        culture-missing-upper-*) marker='culture missing number format uppercase cache end' ;;
        *) echo "FAIL: unknown culture locale mode: $mode" >&2; return 1 ;;
    esac
    gate_output byte-witness "$actual" "$marker" --count any --policy crlf \
        || { echo "FAIL: $mode did not finish" >&2; return 1; }
}

gate_extra_asserts() {
    local out="$1" native expected mode marker before prefix
    culture_info_run native native "./$out/CultureInfoApi$EXE_EXT"

    culture_info_run before native "./$out/CultureInfoApi$EXE_EXT" before-iso-language
    prefix="$native.prefix"
    gate_output select "$native" "$prefix" --before '== ISO language names ==' --policy crlf
    gate_output compare "$prefix" "$before"
    # The frozen divergences also change later CultureData cache identities in a
    # single process, so .NET proves its own full-run prefix unchanged.
    culture_info_run expected oracle dotnet "$_CG_APP"
    for mode in native oracle; do
        if [ "$mode" = native ]; then
            culture_info_run before native "./$out/CultureInfoApi$EXE_EXT" before-rejected-undefined-tails
            prefix="$native.prefix"
            gate_output select "$native" "$prefix" --before '== rejected undefined culture tails ==' --policy crlf
        else
            culture_info_run before oracle dotnet "$_CG_APP" before-rejected-undefined-tails
            prefix="$expected.prefix"
            gate_output select "$expected" "$prefix" --before '== rejected undefined culture tails ==' --policy crlf
        fi
        gate_output compare "$prefix" "$before"
    done
    gate_output byte-witness "$native" 'rejected undefined culture tails end' --count any --policy crlf \
        || { echo 'FAIL: rejected undefined tails did not finish' >&2; return 1; }
    prefix="$expected.prefix"
    gate_output select "$expected" "$prefix" --before '== ISO language names ==' --policy crlf
    culture_info_run before oracle dotnet "$_CG_APP" before-iso-language
    gate_output compare "$prefix" "$before"
    culture_info_run before oracle dotnet "$_CG_APP" before-legacy-chinese
    prefix="$expected.prefix"
    gate_output select "$expected" "$prefix" --before '== legacy Chinese cultures ==' --policy crlf
    gate_output compare "$prefix" "$before"
    culture_info_run before native "./$out/CultureInfoApi$EXE_EXT" before-legacy-chinese
    prefix="$native.prefix"
    gate_output select "$native" "$prefix" --before '== legacy Chinese cultures ==' --policy crlf
    gate_output compare "$prefix" "$before"
    culture_info_run before native "./$out/CultureInfoApi$EXE_EXT" before-private-use-names
    prefix="$native.prefix"
    gate_output select "$native" "$prefix" --before '== private-use culture names ==' --policy crlf
    gate_output compare "$prefix" "$before"
    culture_info_run before oracle dotnet "$_CG_APP" before-private-use-names
    prefix="$expected.prefix"
    gate_output select "$expected" "$prefix" --before '== private-use culture names ==' --policy crlf
    gate_output compare "$prefix" "$before"
    gate_output byte-witness "$native" 'private-use culture names end' --count any --policy crlf \
        || { echo "FAIL: private-use names section did not finish" >&2; return 1; }
    gate_output byte-witness "$expected" '== ISO language names ==' --count any --policy crlf \
        || { echo "FAIL: .NET ISO language section did not run" >&2; return 1; }
    culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-rejected-undefined-tails
    culture_info_run expected oracle dotnet "$_CG_APP" culture-rejected-undefined-tails
    gate_output compare "$native" "$expected"
    for marker in '== rejected undefined culture tails ==' \
        "  input='und-a-a-u-nu-latn'" "  input='en-und-a-a-u-nu-latn'" \
        'rejected undefined culture tails end'; do
        # Require this exact input to execute, whether the target oracle accepts
        # it or reports a typed refusal. The full diff above checks its details.
        if [[ "$marker" == "  input='"* ]] && gate_output byte-witness "$expected" "$marker: CultureNotFoundException" --count any --policy crlf 2>/dev/null; then
            marker="$marker: CultureNotFoundException"
        fi
        gate_output byte-witness "$native" "$marker" --count any --policy crlf \
            || { echo "FAIL: rejected undefined tail witness missing: $marker" >&2; return 1; }
    done
    culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-private-use-names
    culture_info_run expected oracle dotnet "$_CG_APP" culture-private-use-names
    gate_output compare "$native" "$expected"
    local private_u=x-1-US-u-nu-latn_phoneb private_t=x-1-US-t-de-de_phoneb
    if [ "$DN2CPP_OS" = linux ] || [ "$DN2CPP_OS" = windows ]; then
        private_u=x-1-US-U-NU-LATN-u-nu-latn_phoneb
        private_t=x-1-US-T-DE-DE-t-de-de_phoneb
    fi
    for marker in '== private-use culture names ==' \
        "  'en-US' name='en-US' value='en-US' iso='en' lcid=1033 neutral=False specific='en-US' cached='en-US' cached iso='en'" \
        "  'x-foo' name='' value='' iso='' lcid=4096 neutral=True specific='' cached='' cached iso=''" \
        "  'x-u' name='' value='' iso='' lcid=4096 neutral=True specific='' cached='' cached iso=''" \
        "  'x-t' name='' value='' iso='' lcid=4096 neutral=True specific='' cached='' cached iso=''" \
        "  'x-foo_phoneb' name='x-foo_phoneb' value='x-foo_phoneb' iso='x-foo' lcid=4096 neutral=True specific='' cached='x-foo_phoneb' cached iso='x-foo'" \
        "  'x-foo-Latn-US_phoneb' name='x-foo-Latn-US' value='x-foo-Latn-US_phoneb' iso='x-foo' lcid=4096 neutral=False specific='x-foo-Latn-US' cached='x-foo-Latn-US' cached iso='x-foo'" \
        "  'x-lvariant-foo' neutral=True specific lcid=127" \
        "  'x-lvariant-US' neutral=True specific lcid=127" \
        "  'x-a-US-u-nu-latn' neutral=True specific lcid=127" \
        "  'x-foo-US-u-nu-latn' neutral=True specific lcid=127" \
        "  'x-1-US-u-nu-latn_phoneb' name='$private_u' cached='$private_u' specific='$private_u' neutral=False" \
        "  'x-1-US-t-de-de_phoneb' name='$private_t' cached='$private_t' specific='$private_t' neutral=False" \
        'private-use culture names end'; do
        # Some target references reject private-use names accepted by other ICUs.
        # Keep each accepted witness, or require its explicit typed rejection on
        # BOTH sides. The full diff above also compares fault/cache/specific data.
        local rejected="${marker%%\' *}' constructor=CultureNotFoundException"
        if [[ "$marker" == "  '"* ]] && gate_output byte-witness "$expected" "$rejected" --count any --policy crlf 2>/dev/null; then
            marker="$rejected"
        fi
        gate_output byte-witness "$native" "$marker" --count any --policy crlf \
            || { echo "FAIL: private-use name witness missing: $marker" >&2; return 1; }
    done
    culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-iso-language
    culture_info_run expected oracle dotnet "$_CG_APP" culture-iso-language
    gate_output compare "$native" "$expected"

    for marker in '== ISO language names ==' "iso ''=iv:iv" "iso 'zul'=zu:zu" \
        "iso 'i-enochian'=:" "iso 'x-foo'=:" "iso 'i-foo'=i-foo:i-foo" \
        "iso 'x-foo_phoneb'=x-foo:x-foo" "iso 'zh-CHS'=zh:zh" \
        "iso 'x-foo-US_phoneb'=x-foo:x-foo" \
        'iso null=NullReferenceException' 'ISO language names end'; do
        gate_output byte-witness "$native" "$marker" --count any --policy crlf \
            || { echo "FAIL: ISO language witness missing: $marker" >&2; return 1; }
    done
    for mode in no-bok x-foobarbaz x-123456789; do
        if [ "$DN2CPP_OS" = windows ]; then
            gate_output view "$expected" "$expected.records" --policy crlf
            marker=$(grep -F "iso '$mode'=" "$expected.records")
        else
            case "$mode" in
                no-bok) marker="iso '$mode'=no:no" ;;
                *) marker="iso '$mode'=$mode:$mode" ;;
            esac
        fi
        [ -n "$marker" ] && gate_output byte-witness "$native" "$marker" --count any --policy crlf \
            || { echo "FAIL: ISO platform witness missing: $marker" >&2; return 1; }
    done
    # Apple's .NET keeps und for some of these names, so the standard ICU
    # answer is pinned without a host oracle.
    culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-iso-language-undefined
    if [ "$DN2CPP_OS" = windows ]; then
        culture_info_run expected oracle dotnet "$_CG_APP" culture-iso-language-undefined
        gate_output compare "$native" "$expected"
        gate_output byte-witness "$native" 'ISO undefined language names end' --count any --policy crlf \
            || { echo "FAIL: ISO undefined language section did not finish" >&2; return 1; }
    else
        gate_assert_text "$native" "iso 'und-US'=:
iso 'und-u-nu-arab'=:
ISO undefined language names end"
    fi
    for mode in constructor-first cached-first lcid-first; do
        culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-legacy-chinese "$mode"
        culture_info_run expected oracle dotnet "$_CG_APP" culture-legacy-chinese "$mode"
        gate_output compare "$native" "$expected"
        for marker in '== legacy Chinese cultures ==' "legacy 'zh-CHS'" "legacy 'zh-CHT'" \
            "legacy 'zh-CHS<NUL>ignored'" "legacy 'zh-CHT<NUL>ignored'" 'legacy Chinese cultures end'; do
            gate_output byte-witness "$native" "$marker" --count any --policy crlf \
                || { echo "FAIL: legacy Chinese witness missing: $marker" >&2; return 1; }
        done
    done
    culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-names
    gate_output byte-witness "$native" 'culture locale data end' --count any --policy crlf \
        || { echo "FAIL: culture locale data section did not finish" >&2; return 1; }
    gate_output byte-witness "$native" 'culture numbering data end' --count any --policy crlf \
        || { echo "FAIL: culture numbering data section did not finish" >&2; return 1; }
    gate_output byte-witness "$native" 'culture fallback identities end' --count any --policy crlf \
        || { echo "FAIL: culture fallback identities section did not finish" >&2; return 1; }
    gate_output byte-witness "$native" 'culture primary currency end' --count any --policy crlf \
        || { echo "FAIL: culture primary currency section did not finish" >&2; return 1; }
    gate_output byte-witness "$native" 'culture missing number format end' --count any --policy crlf \
        || { echo "FAIL: culture missing number format section did not finish" >&2; return 1; }
    gate_output byte-witness "$native" 'culture variant order end' --count any --policy crlf \
        || { echo "FAIL: culture variant order section did not finish" >&2; return 1; }
    gate_output byte-witness "$native" 'culture invariant lcid cache end' --count any --policy crlf \
        || { echo "FAIL: culture invariant LCID cache section did not finish" >&2; return 1; }
    gate_output byte-witness "$native" 'culture instance number format end' --count any --policy crlf \
        || { echo "FAIL: culture instance number format section did not finish" >&2; return 1; }
    gate_output byte-witness "$native" 'culture integer constructors end' --count any --policy crlf \
        || { echo "FAIL: culture integer constructor section did not finish" >&2; return 1; }
    gate_output byte-witness "$native" 'culture instance equality end' --count any --policy crlf \
        || { echo "FAIL: culture instance equality section did not finish" >&2; return 1; }
    gate_output byte-witness "$native" 'culture specific instances end' --count any --policy crlf \
        || { echo "FAIL: culture specific instances section did not finish" >&2; return 1; }
    gate_output byte-witness "$native" 'culture extension identities end' --count any --policy crlf \
        || { echo "FAIL: culture extension identities section did not finish" >&2; return 1; }
    gate_output byte-witness "$native" 'culture undefined extension metadata end' --count any --policy crlf \
        || { echo "FAIL: culture undefined extension metadata section did not finish" >&2; return 1; }
    gate_output byte-witness "$native" 'culture undefined platform identities end' --count any --policy crlf \
        || { echo "FAIL: undefined platform identities section did not finish" >&2; return 1; }
    gate_output byte-witness "$native" 'culture rejected extension identities end' --count any --policy crlf \
        || { echo "FAIL: rejected extension identities section did not finish" >&2; return 1; }
    gate_output byte-witness "$native" 'culture platform name validation end' --count any --policy crlf \
        || { echo "FAIL: platform name validation section did not finish" >&2; return 1; }
    culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-platform-name-validation
    culture_info_run expected oracle dotnet "$_CG_APP" culture-platform-name-validation
    gate_output compare "$native" "$expected"
    gate_output byte-witness "$native" 'culture platform name validation end' --count any --policy crlf \
        || { echo "FAIL: platform name validation mode did not finish" >&2; return 1; }
    for mode in "${LOCALE_CASES[@]}"; do
        culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" "$mode"
        culture_info_run expected oracle dotnet "$_CG_APP" "$mode"
        gate_output compare "$native" "$expected"
        culture_info_assert_locale_completion "$native" "$mode"
    done
    for mode in "${NUMBERING_CASES[@]}"; do
        culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-numbering-data "$mode"
        culture_info_run expected oracle dotnet "$_CG_APP" culture-numbering-data "$mode"
        gate_output compare "$native" "$expected"
        gate_output byte-witness "$native" 'culture numbering data end' --count any --policy crlf \
            || { echo "FAIL: numbering data for $mode did not finish" >&2; return 1; }
    done
    for mode in "${FALLBACK_CASES[@]}"; do
        culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-fallback-identities "$mode"
        culture_info_run expected oracle dotnet "$_CG_APP" culture-fallback-identities "$mode"
        gate_output compare "$native" "$expected"
        gate_output byte-witness "$native" 'culture fallback identities end' --count any --policy crlf \
            || { echo "FAIL: fallback identities for $mode did not finish" >&2; return 1; }
    done
    for mode in "${MISSING_NUMBER_CASES[@]}"; do
        culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-missing-number-format "$mode"
        culture_info_run expected oracle dotnet "$_CG_APP" culture-missing-number-format "$mode"
        gate_output compare "$native" "$expected"
        gate_output byte-witness "$native" 'culture missing number format end' --count any --policy crlf \
            || { echo "FAIL: missing number format for $mode did not finish" >&2; return 1; }
    done
    for mode in "${VARIANT_CASES[@]}"; do
        culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-variant-order "$mode"
        culture_info_run expected oracle dotnet "$_CG_APP" culture-variant-order "$mode"
        gate_output compare "$native" "$expected"
        gate_output byte-witness "$native" 'culture variant order end' --count any --policy crlf \
            || { echo "FAIL: variant order for $mode did not finish" >&2; return 1; }
    done
    for mode in "${INVARIANT_CACHE_ORDERS[@]}"; do
        culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-invariant-lcid-cache "$mode"
        culture_info_run expected oracle dotnet "$_CG_APP" culture-invariant-lcid-cache "$mode"
        gate_output compare "$native" "$expected"
        gate_output byte-witness "$native" 'culture invariant lcid cache end' --count any --policy crlf \
            || { echo "FAIL: invariant LCID cache order $mode did not finish" >&2; return 1; }
    done
    for mode in "${INSTANCE_CULTURES[@]}"; do
        culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-instance-number-format "$mode"
        culture_info_run expected oracle dotnet "$_CG_APP" culture-instance-number-format "$mode"
        gate_output compare "$native" "$expected"
        gate_output byte-witness "$native" 'culture instance number format end' --count any --policy crlf \
            || { echo "FAIL: culture instance number format for '$mode' did not finish" >&2; return 1; }
    done
    culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-integer-constructors
    culture_info_run expected oracle dotnet "$_CG_APP" culture-integer-constructors
    gate_output compare "$native" "$expected"
    gate_output byte-witness "$native" 'culture integer constructors end' --count any --policy crlf \
        || { echo "FAIL: culture integer constructor mode did not finish" >&2; return 1; }
    for mode in "${EQUALITY_PAIRS[@]}"; do
        culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-instance-equality "${mode%%|*}" "${mode#*|}"
        culture_info_run expected oracle dotnet "$_CG_APP" culture-instance-equality "${mode%%|*}" "${mode#*|}"
        gate_output compare "$native" "$expected"
        gate_output byte-witness "$native" 'culture instance equality end' --count any --policy crlf \
            || { echo "FAIL: culture equality pair '$mode' did not finish" >&2; return 1; }
        gate_output byte-witness "$native" 'culture specific instances end' --count any --policy crlf \
            || { echo "FAIL: culture specific instances for '$mode' did not finish" >&2; return 1; }
    done
    for mode in "${EXTENSION_CASES[@]}"; do
        culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-extension-identities "$mode"
        culture_info_run expected oracle dotnet "$_CG_APP" culture-extension-identities "$mode"
        gate_output compare "$native" "$expected"
        gate_output byte-witness "$native" 'culture extension identities end' --count any --policy crlf \
            || { echo "FAIL: extension identity '$mode' did not finish" >&2; return 1; }
    done
    for mode in "${UNDEFINED_EXTENSION_CASES[@]}"; do
        culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-undefined-extension-metadata "$mode"
        culture_info_run expected oracle dotnet "$_CG_APP" culture-undefined-extension-metadata "$mode"
        gate_output compare "$native" "$expected"
        gate_output byte-witness "$native" 'culture undefined extension metadata end' --count any --policy crlf \
            || { echo "FAIL: undefined extension metadata '$mode' did not finish" >&2; return 1; }
    done
    for mode in "${UNDEFINED_PLATFORM_CASES[@]}"; do
        culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-undefined-platform-identities "$mode"
        culture_info_run expected oracle dotnet "$_CG_APP" culture-undefined-platform-identities "$mode"
        gate_output compare "$native" "$expected"
        gate_output byte-witness "$native" 'culture undefined platform identities end' --count any --policy crlf \
            || { echo "FAIL: undefined platform identity '$mode' did not finish" >&2; return 1; }
    done
    local first second cache_order
    for mode in "${UNDEFINED_PLATFORM_CACHE_ORDERS[@]}"; do
        IFS='|' read -r first second cache_order <<< "$mode"
        culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-undefined-platform-cache "$first" "$second" "$cache_order"
        culture_info_run expected oracle dotnet "$_CG_APP" culture-undefined-platform-cache "$first" "$second" "$cache_order"
        gate_output compare "$native" "$expected"
        gate_output byte-witness "$native" 'culture undefined platform cache end' --count any --policy crlf \
            || { echo "FAIL: undefined platform cache order '$mode' did not finish" >&2; return 1; }
    done
    for mode in "${REJECTED_EXTENSION_CASES[@]}"; do
        culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-rejected-extension-identities "$mode"
        culture_info_run expected oracle dotnet "$_CG_APP" culture-rejected-extension-identities "$mode"
        gate_output compare "$native" "$expected"
        gate_output byte-witness "$native" 'culture rejected extension identities end' --count any --policy crlf \
            || { echo "FAIL: rejected extension identity '$mode' did not finish" >&2; return 1; }
    done
    local pair order
    for pair in "${EXTENSION_ORDER_PAIRS[@]}"; do
        for order in constructor-first cached-first; do
            culture_info_run native native "./$out/CultureInfoApi$EXE_EXT" culture-extension-order "${pair%%|*}" "${pair#*|}" "$order"
            culture_info_run expected oracle dotnet "$_CG_APP" culture-extension-order "${pair%%|*}" "${pair#*|}" "$order"
            gate_output compare "$native" "$expected"
            gate_output byte-witness "$native" 'culture extension cache order end' --count any --policy crlf \
                || { echo "FAIL: extension cache order '$pair' '$order' did not finish" >&2; return 1; }
        done
    done
}
DN2CPP_GATE_RUN_ARGS=culture-names
DN2CPP_GATE_EXTRA_CONTEXT="culture-rejected-undefined-tails before-rejected-undefined-tails culture-platform-name-validation culture-iso-language culture-iso-language-undefined culture-private-use-names before-private-use-names before-iso-language culture-legacy-chinese before-legacy-chinese constructor-first cached-first lcid-first"
DN2CPP_GATE_EXTRA_CONTEXT+=" ${LOCALE_CASES[*]} ${NUMBERING_CASES[*]} ${FALLBACK_CASES[*]} culture-missing-number-format ${MISSING_NUMBER_CASES[*]} culture-variant-order ${VARIANT_CASES[*]} culture-invariant-lcid-cache ${INVARIANT_CACHE_ORDERS[*]} culture-instance-number-format [${INSTANCE_CULTURES[*]}] culture-integer-constructors culture-instance-equality ${EQUALITY_PAIRS[*]} culture-extension-identities ${EXTENSION_CASES[*]} culture-undefined-extension-metadata ${UNDEFINED_EXTENSION_CASES[*]} culture-extension-order ${EXTENSION_ORDER_PAIRS[*]} constructor-first cached-first culture-undefined-platform-identities [${UNDEFINED_PLATFORM_CASES[*]}] culture-undefined-platform-cache ${UNDEFINED_PLATFORM_CACHE_ORDERS[*]} culture-rejected-extension-identities ${REJECTED_EXTENSION_CASES[*]}"
DN2CPP_OUT_SUFFIX=-names
corelib_diff_gate CultureInfoApi --link-xml samples/dotnet/CultureInfoApi/link.xml
