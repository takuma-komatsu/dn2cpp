#!/usr/bin/env bash
# Consolidated string-build gate. Merges the former string formatting / building
# subset gates into one multi-section program, transpiled once against the
# tree-shaken real CoreLib and diffed exactly against real .NET. Covers
# String.Format (index/alignment/format specifiers, the params-span overloads, and
# the net8+ System.Text.CompositeFormat overload family) and StringBuilder
# (Append/AppendFormat/Insert/Remove/Replace and edit operations), including null
# receiver fault precedence, repeat and string-window validation, null object
# insertion, and argument evaluation for calls and interpolation.
# CopyTo preserves destination-first fault fields and unchecked room arithmetic.
# Former gates: string-format, stringbuilder, stringbuilder-edit.
source "$(dirname "$0")/_common.sh"

gate_extra_asserts() {
    local out="$1" native before prefix line
    native=$(run_bounded "./$out/StringBuild")
    native=$(strip_cr_win "$native")
    before=$(dotnet "$_CG_APP" before-null-receivers)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== StringBuilder null receivers ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== StringBuilder null receivers ==' \
        'append repeat zero: NullReferenceException' \
        'append format array: NullReferenceException' \
        'interpolated int: NullReferenceException' \
        'literal argument evaluation: []' \
        'formatted values: 0' \
        'interpolated custom value: NullReferenceException' \
        'interpolated throwing value: InvalidOperationException' \
        'interpolated formatters: 3' \
        'interpolated invalid int format: NullReferenceException' \
        'interpolated boxed span formatter: NullReferenceException' \
        'generic object null next evaluation: H' \
        'generic string null next evaluation: H' \
        'generic reference null next evaluation: H' \
        'generic nullable null next evaluation: H' \
        'interpolated enum: NullReferenceException' \
        'struct formatters: 4' \
        'generic object zero alignment next evaluation: [H]' \
        'generic object positive alignment next evaluation: []' \
        'generic nullable zero alignment next evaluation: [H]' \
        'generic nullable positive alignment next evaluation: []' \
        'argument evaluation: RSIC' \
        'throwing argument: InvalidOperationException' \
        'throwing argument evaluation: RT' \
        'recovery: ok!'; do
        grep -Fxq "$line" <<< "$native" \
            || { echo "FAIL: null receiver witness missing: $line" >&2; exit 1; }
    done
    before=$(dotnet "$_CG_APP" before-builder-ranges)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== StringBuilder ranges ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== StringBuilder ranges ==' \
        'repeat negative: ArgumentOutOfRangeException' \
        'repeat minimum: ArgumentOutOfRangeException' \
        'repeat evaluation: CN' \
        'repeat null receiver: NullReferenceException' \
        'repeat null evaluation: CN' \
        'repeat content: abxx' \
        'window null start: ArgumentNullException' \
        'window null count: ArgumentNullException' \
        'window zero beyond end: ok' \
        'window maximum count: ArgumentOutOfRangeException' \
        'window evaluation: SIN' \
        'window null receiver: NullReferenceException' \
        'window null evaluation: SIN' \
        'failed window content: abxx' \
        'window content: abxxcdexx' \
        'insert null identity: True' \
        'insert null maximum index: ok' \
        'insert evaluation: I' \
        'insert throwing formatter: InvalidOperationException' \
        'insert null receiver formatter: NullReferenceException' \
        'insert formatters: 2' \
        'final content: abxxcdexx' \
        'recovery identity: True' \
        'recovery content: abxxcdexx!'; do
        grep -Fxq "$line" <<< "$native" \
            || { echo "FAIL: builder range witness missing: $line" >&2; exit 1; }
    done
    before=$(dotnet "$_CG_APP" before-builder-copy-faults)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== StringBuilder copy faults ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== StringBuilder copy faults ==' \
        '0:-1:-1:-1:-1 param=destination' \
        '2:-1:-1:-1:-1 type=NullReferenceException' \
        '0:4:-1:-1:-1 param=destinationIndex' \
        '0:4:-1:-1:-1 actual=Int32:-1' \
        '0:4:-1:0:-1 param=count' '0:4:-1:0:-1 actual=Int32:-1' \
        '0:4:0:5:-1 type=ArgumentOutOfRangeException' '0:4:0:5:-1 param=' \
        '0:4:0:0:-2147483648 type=ArgumentException' \
        '0:4:2147483647:0:0 param=sourceIndex' \
        '0:4:2147483647:0:0 actual=null' \
        '0:4:3:0:4 message=Source string was not long enough. Check sourceIndex and count.' \
        '0:4:3:0:4 destination=002E 002E 002E 002E ' \
        '0:4:4:4:0 copied' '1:0:0:0:0 copied' \
        '0:4:0:0:4 destination=0061 0062 0063 0061 ' \
        'copy utf16=0041 0000 D800 005A ' \
        'copy null evaluation=RSADC' 'copy throwing count evaluation=RSADC' \
        'copy fault after GC param=destinationIndex' \
        'copy fault after GC actual=Int32:-7' 'StringBuilder copy faults end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: builder CopyTo fault witness missing: $line" >&2; exit 1; }
    done

}

corelib_diff_gate StringBuild
