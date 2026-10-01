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
# StringBuilder.Remove and Replace preserve named faults and validation order.
# StringBuilder.Length, EnsureCapacity and indexer preserve distinct fault fields.
# StringBuilder.Insert preserves named index/count faults and count-first validation.
# Array Insert validates its index before the slice; Append validates slice signs first.
# StringBuilder range Append preserves value faults and zero-count shortcuts.
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
    before=$(dotnet "$_CG_APP" before-builder-edit-faults)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== StringBuilder edit faults ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== StringBuilder edit faults ==' \
        'remove:0:-1:-1 param=length' 'remove:0:-1:-1 actual=Int32:-1' \
        'remove:0:-1:0 param=startIndex' 'remove:0:-1:0 actual=Int32:-1' \
        'remove:0:2147483647:0 param=length' 'remove:0:2147483647:0 actual=null' \
        'remove:0:2147483647:0 content=0061 0062 0061 0063 0061 0000 D800 ' \
        'replace string:0:0:0:-1:-1 param=oldValue' \
        'replace string:0:1:0:-1:-1 param=oldValue' \
        'replace string:0:1:0:-1:-1 message=The value cannot be an empty string. (Parameter '\''oldValue'\'')' \
        'replace char:0:8:-1 param=startIndex' 'replace char:0:0:-1 param=count' \
        'replace char:0:7:0 success' \
        'replace string:0:2:2:0:7 content=0058 0059 0062 0058 0059 0063 0058 0059 0000 D800 ' \
        'replace null evaluation type=NullReferenceException' 'replace null evaluation=RONSC' \
        'remove throwing evaluation type=InvalidOperationException' 'remove throwing evaluation=RSL' \
        'edit fault after GC param=length' 'edit fault after GC actual=Int32:-7' \
        'StringBuilder edit faults end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: builder edit fault witness missing: $line" >&2; exit 1; }
    done
    before=$(dotnet "$_CG_APP" before-builder-state-faults)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== StringBuilder state faults ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== StringBuilder state faults ==' \
        'length:0:-2147483648 param=value' 'length:0:-2147483648 actual=Int32:-2147483648' \
        'ensure:0:-1 param=capacity' 'ensure:0:-1 actual=Int32:-1' \
        'set index:0:-1 param=index' 'set index:0:-1 actual=null' \
        'get index:0:-1 type=IndexOutOfRangeException' 'get index:0:-1 param=' \
        'length:0:1 content=0061 ' 'length:0:7 content=0061 0062 0000 D800 0000 0000 0000 ' \
        'set index:0:1 content=0061 D800 0000 D800 ' 'get unit=D800' 'ensure value=17' \
        'length null evaluation type=NullReferenceException' 'length null evaluation=RV' \
        'index throwing evaluation type=InvalidOperationException' 'index throwing evaluation=RIV' \
        'state fault after GC param=capacity' 'state fault after GC actual=Int32:-7' \
        'StringBuilder state faults end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: builder state fault witness missing: $line" >&2; exit 1; }
    done

    before=$(dotnet "$_CG_APP" before-builder-insert-faults)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== StringBuilder insert faults ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== StringBuilder insert faults ==' \
        'insert char:0:-2147483648 param=index' 'insert char:0:-2147483648 actual=null' \
        'insert string:0:0:-1 param=index' 'insert string:0:0:4 identity=True' \
        'insert repeat:0:0:-1:-1 param=count' 'insert repeat:0:0:-1:-1 actual=Int32:-1' \
        'insert repeat:0:0:-1:0 param=index' 'insert repeat:0:2:1:2 identity=True' \
        'insert repeat:0:2:1:2 content=0061 0058 0000 D800 0058 0000 D800 0062 0000 D800 ' \
        'insert overflow type=OutOfMemoryException' \
        'insert null maximum count identity=True' 'insert empty maximum count identity=True' \
        'insert null evaluation type=NullReferenceException' 'insert null evaluation=RIVC' \
        'insert throwing evaluation type=InvalidOperationException' 'insert throwing evaluation=RIVC' \
        'insert fault after GC param=count' 'insert fault after GC actual=Int32:-7' \
        'StringBuilder insert faults end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: builder insert fault witness missing: $line" >&2; exit 1; }
    done

    before=$(dotnet "$_CG_APP" before-builder-collection-faults)
    before=$(strip_cr_win "$before")
    prefix=$(awk '/^== StringBuilder collection faults ==$/ { exit } { print }' <<< "$native")
    assert_output "$prefix" "$before"
    for line in '== StringBuilder collection faults ==' \
        'array append:0:3:-1:-1 param=startIndex' 'array append:0:3:-1:-1 actual=Int32:-1' \
        'array append:0:3:0:-1 param=charCount' 'array append:0:3:1:0 param=value' \
        'array append:0:0:1:2147483647 param=charCount' \
        'array insert:0:3:-1:-1:-1 param=index' 'array insert:0:3:0:-1:-1 param=value' \
        'array insert:0:0:0:-1:-1 param=startIndex' 'array insert:0:0:0:0:-1 param=charCount' \
        'array insert:0:0:0:4:1 param=startIndex' \
        'array insert:0:0:1:1:1 content=0061 0000 0062 0000 D800 ' \
        'builder append:0:2:-1:-1 param=startIndex' 'builder append:0:2:0:-1 param=count' \
        'builder append:0:2:1:0 param=value' 'builder append:0:0:2147483647:0 identity=True' \
        'builder self append content=0061 0062 0000 D800 0062 0000 D800 ' \
        'array append null evaluation type=NullReferenceException' 'array append null evaluation=RASC' \
        'array insert throwing evaluation type=InvalidOperationException' 'array insert throwing evaluation=RIASC' \
        'builder append null evaluation type=NullReferenceException' 'builder append null evaluation=RBSC' \
        'array append fault after GC param=startIndex' 'array insert fault after GC param=charCount' \
        'builder append fault after GC actual=Int32:-7' 'StringBuilder collection faults end'; do
        grep -Fxq -- "$line" <<< "$native" \
            || { echo "FAIL: builder collection fault witness missing: $line" >&2; exit 1; }
    done

}

corelib_diff_gate StringBuild
