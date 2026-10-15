#!/usr/bin/env bash
# Consolidated char/text gate: System.Char classification & casing, the remaining
# char predicates (including every indexed-string classification sibling),
# char-in-concat lowering, UTF-16 surrogate predicates, sub-word ToString,
# primitive ToString, and Encoding.GetString. Diffed exactly vs real .NET.
# Former gates: char, char-rest, char-concat, char-issurrogate, subword-tostring,
# tostring, encoding-getstring.
# Indexed Char APIs preserve named null/index faults, UTF-16 messages, and fields
# across collection; classification and ConvertToUtf32 use their own index text.
# String.GetHashCode(StringComparison) matches each comparer and rejects invalid values.
source "$(dirname "$0")/_common.sh"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|before-comparison-hashes"

gate_extra_asserts() {
    local out="$1" native before prefix line
    native=$(run_bounded "./$out/CharText")
    native=$(strip_cr_win "$native")
    before=$(run_bounded dotnet "$_CG_APP" before-comparison-hashes)
    prefix=$(awk '/^== String comparison hashes ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    before=$(run_bounded "./$out/CharText" before-comparison-hashes)
    assert_output "$prefix" "$(strip_cr_win "$before")"
    for line in '== String comparison hashes ==' 'hash en-US:0=True:True:True:True' \
        'hash tr-TR:5=True:True:True:True' 'unicode ordinal ignore case=True' \
        'hash fault=text:-2147483648:ArgumentException:comparisonType' 'hash null=4' \
        'hash null=6' 'String comparison hashes end'; do
        [ "$(grep -Fxc "$line" <<< "$native")" = 1 ] \
            || { echo "FAIL: string hash witness missing: $line" >&2; exit 1; }
    done
    before=$(run_bounded dotnet "$_CG_APP" before-encoding-receivers)
    prefix=$(awk '/^== encoding receivers ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    grep -Fxq 'encoding receivers end' <<< "$native" \
        || { echo "FAIL: Encoding receiver section did not run" >&2; exit 1; }
    before=$(dotnet "$_CG_APP" before-argument-fields)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== Char indexed validation fields ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== Char indexed validation fields ==' \
        'digit:0:2147483647 param=s' \
        'letter:2:-2147483648 param=index' \
        'utf32:2:2147483647 param=index' \
        'valid utf32 pair success=128578' \
        'valid surrogate pair success=True' \
        'char index after GC:0 param=s' \
        'char index after GC:1 param=index' \
        'char index after GC:2 param=index' \
        'Char indexed validation fields end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: CharText validation witness missing: $line" >&2; exit 1; }
    done
}


corelib_diff_gate CharText
