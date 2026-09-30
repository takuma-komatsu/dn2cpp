#!/usr/bin/env bash
# Consolidated string-build gate. Merges the former string formatting / building
# subset gates into one multi-section program, transpiled once against the
# tree-shaken real CoreLib and diffed exactly against real .NET. Covers
# String.Format (index/alignment/format specifiers, the params-span overloads, and
# the net8+ System.Text.CompositeFormat overload family) and StringBuilder
# (Append/AppendFormat/Insert/Remove/Replace and edit operations), including null
# receiver fault precedence and argument evaluation for calls and interpolation.
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
}

corelib_diff_gate StringBuild
