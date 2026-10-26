#!/usr/bin/env bash
# Console text and standard byte streams, formatting and path APIs, diffed vs .NET.
# Composite format faults preserve parser diagnostics, null-array semantics and argument effects.
source "$(dirname "$0")/_common.sh"

py=""
if [ "$DN2CPP_OS" != windows ]; then
    py=$(resolve_python) || gate_skip 'Python is required for nonblocking console pipe probes'
fi

gate_extra_asserts() {
    local out="$1" before prefix native expected line
    assert_exit_code "$2" 0
    assert_exit_code "$3" 0
    gate_capture_ok "$out/assert-before-1" run_bounded dotnet "$_CG_APP" before-standard-streams
    before="$out/assert-before-1.stdout"
    native="$out/native.out"
    prefix="$out/assert-prefix-6.stdout"
    gate_output select "$native" "$prefix" --before '== standard console streams ==' --policy crlf
    gate_output compare "$prefix" "$before"
    # Whole-stream parity above is exact; standard streams intentionally emit NUL.
    gate_output byte-witness "$native" 'standard console streams end' --count any --policy crlf \
        || { echo 'FAIL: standard console stream section did not run' >&2; return 1; }
    gate_capture_ok "$out/assert-before-2" run_bounded dotnet "$_CG_APP" before-format-diagnostics
    before="$out/assert-before-2.stdout"
    prefix="$out/assert-prefix-7.stdout"
    gate_output select "$native" "$prefix" --before '== Console composite format diagnostics ==' --policy crlf
    gate_output compare "$prefix" "$before"
    gate_capture_ok "$out/assert-before-3" run_bounded "./$out/ConsoleIo$EXE_EXT" before-format-diagnostics
    before="$out/assert-before-3.stdout"
    gate_output compare "$prefix" "$before"
    for line in '== Console composite format diagnostics ==' \
        'write-valid-one success trace= calls=0' 'write-valid-two success trace= calls=0' \
        'write-valid-three success trace= calls=0' 'write-valid-array success trace= calls=0' \
        'line-valid-one success trace= calls=0' 'line-valid-two success trace= calls=0' \
        'line-valid-three success trace= calls=0' 'line-valid-array success trace= calls=0' \
        'write-valid-spaces success trace= calls=0' \
        'write-null-array success trace= calls=0' 'line-null-array success trace= calls=0' \
        'write-null-value-spec success trace= calls=0' 'line-null-array-spec success trace= calls=0' \
        'write-value-repeated success trace=T1T1 calls=2' \
        'line-value-reordered success trace=T2T1 calls=2' \
        'write-evaluation success trace=F123T3T1T2 calls=3' \
        'write-argument-throw-before-grammar fault=InvalidOperationException:argument-fault trace=F1X calls=0' \
        'write-array-evaluation success trace=FAT1 calls=1' \
        'Console composite format diagnostics end'; do
        gate_output byte-witness "$native" "$line" --count any --policy crlf \
            || { echo "FAIL: console composite format witness missing: $line" >&2; return 1; }
    done
    printf ABCDEFGHI > "$out/stdin.bin"
    gate_capture_ok "$out/assert-native-4" run_bounded "./$out/ConsoleIo$EXE_EXT" standard-input < "$out/stdin.bin"
    native="$out/assert-native-4.stdout"
    gate_capture_ok "$out/assert-expected-5" run_bounded dotnet "$_CG_APP" standard-input < "$out/stdin.bin"
    expected="$out/assert-expected-5.stdout"

    gate_output compare "$native" "$expected"
    gate_output byte-witness "$native" 'standard input end' --count any --policy crlf \
        || { echo 'FAIL: redirected standard input section did not run' >&2; return 1; }
    if [ -n "$py" ]; then
        run_bounded "$py" gates/fixtures/console-pipes.py "./$out/ConsoleIo$EXE_EXT" "$_CG_APP"
        local handles_out="$out-handles"
        mkdir -p "$handles_out"
        cp gates/fixtures/console-handles.cpp "$handles_out/generated.cpp"
        printf '#pragma once\n' > "$handles_out/generated.h"
        compile_console "$handles_out" ConsoleHandles
        gate_capture_ok "$handles_out/handles" run_bounded "$handles_out/ConsoleHandles$EXE_EXT"
        gate_assert_text "$handles_out/handles.stdout" 'standard handle lifetime end'
    fi
}

DN2CPP_GATE_EXTRA_INPUTS="gates/fixtures/console-pipes.py gates/fixtures/console-handles.cpp"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|format-diagnostics-prefix-argv:before-format-diagnostics"
corelib_diff_split_gate ConsoleIo --auto-ref
