#!/usr/bin/env bash
# Consolidated string-core gate. Merges the former per-feature string instance/
# static method subset gates into one multi-section program, transpiled once
# against the tree-shaken real CoreLib and diffed exactly against real .NET.
# Covers String methods (IndexOf/Contains/Replace/Substring/Trim/case/etc.),
# comparison/StartsWith/EndsWith, padding, Split, Join, string<->span,
# string.CopyTo(Span<char>), misc string constructors (char*, char[],
# ReadOnlySpan<char>), the broad string method surface, building strings from
# chars, and String.Create.
# Also covers string.Join/Concat over a List<T> that arrives as a CALL RESULT or
# a FIELD rather than a local (JoinCallResultSubset), and the
# StringComparer.CurrentCulture -> ordinal interception
# (OrdinalCultureComparerSubset).
# StringJoinSubset.RunSequences asserts string.Join and string.Concat over
# IEnumerable<string>: the ArgumentNullException a null List, collection or bare
# interface raises, Concat(object) formatting a collection argument as itself
# rather than joining its elements, and the enumerator disposed once the join
# ends; then the ArgumentNullException, with .NET's parameter name, that a null
# array raises in the array overloads of Join, Concat and StringBuilder.AppendJoin.
# StringJoinSubset.RunOperandShapes asserts that Join, Concat and AppendJoin take
# an IEnumerable<T> operand of any static type: a null literal's
# ArgumentNullException raised when the call runs, a conditional over an array
# and a list, a LINQ grouping, and a set seen through ISet<T> or IReadOnlySet<T>.
# AppendJoin preserves the partial builder on formatting failure, checks its
# receiver before values, and detects a List modified during enumeration.
# This project's reference set is deliberately NARROW: android-gdext,
# emit-order-stability, ios-sim-console and wasm-console each re-transpile
# StringCore with their own hand-written copy of it, so a fourth `-r` here is a
# fourth edit there and two of those are cross-compiles. The Join-over-a-concrete-
# IEnumerable section that would have needed System.Collections lives in
# DictCollections, which already carries it.
# Former gates: string-method, string-compare, string-pad, string-split,
# string-join, string-span, string-ctor-misc, string-surface, string-from-chars,
# string-create, join-callresult-subset, ordinal-culture-comparer-subset, plus
# the String-as-interface sections (LINQ over a string / CharEnumerator /
# IComparable-family dispatch / Intern), which need System.Linq.
# Indexed culture comparisons validate clamped windows before null ordering and options.
# Array CopyTo preserves the rejected Int32 and the bound in its argument faults.
# Insert and ToCharArray preserve unsigned start faults and signed window precedence.
source "$(dirname "$0")/_common.sh"

gate_extra_asserts() {
    local out="$1" native before prefix line
    native=$(run_bounded "./$out/StringCore")
    native=$(strip_cr_win "$native")
    before=$(dotnet "$_CG_APP" before-join-sequences)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== string sequences ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== string sequences ==' \
        '== string sequence operands ==' \
        'disposed: w1++w3 w1w3 2' \
        'sets: x,y pq 7|8 9 p+q' \
        '== AppendJoin failures ==' \
        'array: InvalidOperationException prefix:one,' \
        'span: InvalidOperationException prefix:one/' \
        'ienum array: InvalidOperationException prefix:one+' \
        'list: InvalidOperationException prefix:one-' \
        'changed list: InvalidOperationException prefix:first' \
        'null receiver strings: NullReferenceException' \
        'null receiver empty span: NullReferenceException'; do
        grep -Fxq "$line" <<< "$native" \
            || { echo "FAIL: sequence witness missing: $line" >&2; exit 1; }
    done
    before=$(dotnet "$_CG_APP" before-indexed-compare)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== indexed culture compare ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== indexed culture compare ==' 'bool fold=0' 'bool clamp=-1' \
        'options ordinal=-32' 'options ordinal-ci=0' \
        'bool evaluation=AIBJLFC' 'options evaluation=AIBJLCO' \
        'throwing culture evaluation=AIBJLC' 'indexed culture compare end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: indexed compare witness missing: $line" >&2; exit 1; }
    done
    before=$(dotnet "$_CG_APP" before-copyto-faults)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== string array copy faults ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== string array copy faults ==' 'source-window actual=Int32:4' \
        'source-past-end actual=Int32:0' 'destination-window actual=Int32:1' \
        'destination-room-before-sign actual=Int32:-1' \
        'count-minimum actual=Int32:-2147483648' \
        'source-window destination=....' 'copy-window destination=.bcd.' \
        'empty-at-end copied' 'null receiver evaluation=SIDJC' \
        'throwing count evaluation=SIDJC' 'string array copy faults end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: array CopyTo witness missing: $line" >&2; exit 1; }
    done
    before=$(dotnet "$_CG_APP" before-window-faults)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== string window faults ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== string window faults ==' 'insert-negative actual=UInt32:4294967295' \
        'insert-minimum actual=UInt32:2147483648' 'insert-null-before-index param=value' \
        'array-negative-start actual=UInt32:4294967295' 'array-window actual=Int32:1' \
        'array-minimum-length actual=Int32:0' 'array-negative-length actual=Int32:-1' \
        'insert-empty-identity result=True' 'insert-empty-source-identity result=True' \
        'array-empty-at-end result=0' 'independent array=abc:Qbc' \
        'empty array identities=True:True:True' 'empty array after GC=True:True' \
        'insert null evaluation=SIV' 'insert throwing value evaluation=SIV' \
        'array null evaluation=SIL' 'array throwing length evaluation=SIL' \
        'string window faults end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: string window witness missing: $line" >&2; exit 1; }
    done
}

corelib_diff_gate StringCore System.Linq
