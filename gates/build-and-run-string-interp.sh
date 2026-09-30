#!/usr/bin/env bash
# Consolidated string concat/interpolation gate: String.Concat (string/char/value
# operands, join-list), interpolated string handlers, interpolation via newobj,
# AppendFormat, and custom format providers. ConcatSequenceSubset, last, drives
# string.Concat<T> and Join<T> over an array, a List<T>, a concrete collection and a
# bare IEnumerable<T> over an array or a list: unsigned elements print unsigned on
# both bare-interface branches, and a null sequence raises ArgumentNullException.
#
# A LIVE `dotnet $app` diff. It was a frozen snapshot because the custom-format
# section is culture-dependent — percent and currency formatting, where real .NET
# used the host's culture and the transpiler a fixed invariant one. The driver
# pins CurrentCulture, which makes both sides invariant and removes the
# asymmetry, so the expectation moves
# from "whatever the transpiler printed" to "what real .NET prints".
# Former gates: concat, concat-value, concat-join-list, interp, interp-handler,
# interpolation-newobj, append-format, custom-format.
source "$(dirname "$0")/_common.sh"

gate_extra_asserts() {
    local out="$1" native before prefix line
    native=$(run_bounded "./$out/StringInterp")
    before=$(dotnet "$_CG_APP" before-concat-sequences)
    prefix=$(awk '/^== Concat<T> and Join<T> sequence shapes ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== Concat<T> and Join<T> sequence shapes ==' \
        'bare ulong[]: 18446744073709551615,2 | 184467440737095516152'; do
        grep -Fxq "$line" <<< "$native" \
            || { echo "FAIL: sequence witness missing: $line" >&2; exit 1; }
    done
}

corelib_diff_gate StringInterp
