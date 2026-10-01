#!/usr/bin/env bash
# Consolidated char/text gate: System.Char classification & casing, the remaining
# char predicates (including every indexed-string classification sibling),
# char-in-concat lowering, UTF-16 surrogate predicates, sub-word ToString,
# primitive ToString, and Encoding.GetString. Diffed exactly vs real .NET.
# Former gates: char, char-rest, char-concat, char-issurrogate, subword-tostring,
# tostring, encoding-getstring.
# Indexed Char APIs preserve named null/index faults, UTF-16 messages, and fields
# across collection; classification and ConvertToUtf32 use their own index text.
source "$(dirname "$0")/_common.sh"

gate_extra_asserts() {
    local out="$1" native before prefix line
    native=$(run_bounded "./$out/CharText")
    native=$(strip_cr_win "$native")
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
